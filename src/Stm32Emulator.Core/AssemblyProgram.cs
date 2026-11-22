using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Stm32Emulator.Core;

public sealed class AssemblyException(int line, string message) : Exception($"Line {line}: {message}") { public int Line { get; } = line; }
public abstract record Operand;
public sealed record RegisterOperand(int Index, char Bank = 'R', bool Writeback = false) : Operand;
public sealed record ImmediateOperand(uint Value) : Operand;
public sealed record LaneOperand(RegisterOperand Register, int Lane) : Operand;
public sealed record NamedOperand(string Name) : Operand;
public sealed record ShiftOperand(string Kind, Operand Amount) : Operand;
public sealed record MemoryOperand(RegisterOperand Base, Operand Offset, ShiftOperand? Shift, bool Writeback, bool PostIndex, bool Subtract) : Operand;
public sealed record RegisterListOperand(RegisterOperand[] Registers) : Operand;
public sealed record Instruction(int Line, string Mnemonic, string[] Operands, uint? Address)
{
    public DecodedOperation Operation { get; init; } = null!;
    public Operand[] Arguments { get; set; } = [];
    public int Width { get; set; } = 4;
}

/// <summary>Two-pass, bounded source layout. Instructions are typed nodes, never binary firmware.</summary>
public sealed class AssemblyProgram
{
    public const int MaximumSourceBytes = 65_536, MaximumInstructions = 2_000;
    public const uint CodeBase = 0x08000000, CodeSize = 256 * 1024;
    public IReadOnlyList<Instruction> Instructions { get; }
    public IReadOnlyDictionary<uint, byte> InitialData { get; }
    public uint EndAddress { get; }
    private readonly Dictionary<string, uint> symbols;
    private readonly Dictionary<uint, int> addresses;
    public static readonly string[] Conditions = InstructionSet.Conditions;
    private AssemblyProgram(List<Instruction> instructions, Dictionary<string, uint> symbols, Dictionary<uint, byte> data)
    {
        Instructions = instructions; this.symbols = symbols; InitialData = data;
        addresses = instructions.Select((x, i) => (Address: x.Address!.Value, Index: i)).ToDictionary(x => x.Address, x => x.Index);
        EndAddress = instructions[^1].Address!.Value + (uint)instructions[^1].Width;
    }
    public static AssemblyProgram Parse(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (Encoding.UTF8.GetByteCount(source) > MaximumSourceBytes) throw new AssemblyException(1, "Source exceeds 64 KiB.");
        var instructions = new List<Instruction>(); var symbols = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        var pendingData = new List<(int Line, uint Address, int Size, string Value)>();
        var constants = new List<(int Line, string Name, string Value)>();
        var occupied = new HashSet<uint>(); var data = new Dictionary<uint, byte>();
        uint code = CodeBase, ram = MemoryBus.RamBase; var section = "TEXT";
        uint Location() => section == "TEXT" ? code : ram;
        void SetLocation(uint value) { if (section == "TEXT") code = value; else ram = value; }
        void Reserve(uint start, int length, int line)
        {
            var end = (ulong)start + (uint)length;
            if (length < 0 || !(start >= CodeBase && end <= CodeBase + CodeSize || start >= MemoryBus.RamBase && end <= MemoryBus.RamBase + MemoryBus.RamSize)) throw new AssemblyException(line, "Program allocation is outside the code/SRAM windows.");
            for (var j = 0; j < length; j++) if (!occupied.Add(start + (uint)j)) throw new AssemblyException(line, "Overlapping instruction or data allocation.");
        }
        var rows = source.Replace("\r", "").Split('\n');
        for (var n = 0; n < rows.Length; n++)
        {
            try
            {
                var text = Regex.Split(rows[n], @";|//")[0].Trim();
                if (text.Length == 0) continue;
                var label = Regex.Match(text, @"^([a-zA-Z_]\w*):");
                if (label.Success)
                {
                    if (!symbols.TryAdd(label.Groups[1].Value, Location())) throw new FormatException("Duplicate label.");
                    text = text[label.Length..].Trim(); if (text.Length == 0) continue;
                }
                uint? explicitAddress = null;
                var listing = Regex.Match(text, @"^(?:0x)?([0-9a-fA-F]{8}):?\s+");
                if (listing.Success) { explicitAddress = uint.Parse(listing.Groups[1].Value, NumberStyles.HexNumber); SetLocation(explicitAddress.Value); text = text[listing.Length..].Trim(); }
                var parts = text.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) throw new FormatException("Missing instruction.");
                var name = parts[0].ToUpperInvariant(); var body = parts.Length == 2 ? parts[1] : "";
                if (name is ".TEXT" or ".DATA" or ".BSS") { section = name[1..]; continue; }
                if (name is ".THUMB" or ".SYNTAX" or ".GLOBAL" or ".GLOBL" or ".TYPE" or ".SIZE" or ".CPU" or ".FPU") continue;
                if (name is ".EQU" or ".SET" or "EQU")
                { var args = SplitOperands(body); if (args.Length != 2 || symbols.ContainsKey(args[0]) || constants.Any(c => c.Name.Equals(args[0], StringComparison.OrdinalIgnoreCase))) throw new FormatException("Expected a unique constant name and value."); if (!Regex.IsMatch(args[0], @"^[a-zA-Z_]\w*$")) throw new FormatException("Invalid constant name."); try { symbols.Add(args[0], Expression(args[1], symbols)); } catch (FormatException) { constants.Add((n + 1, args[0], args[1])); } continue; }
                if (name is ".ALIGN" or ".BALIGN" or ".SPACE" or ".ZERO")
                {
                    var amount = Expression(body, symbols); uint next;
                    if (name is ".ALIGN" or ".BALIGN") { if (name == ".ALIGN" && amount > 16) throw new FormatException("Alignment exponent is too large."); var boundary = name == ".ALIGN" ? 1u << (int)amount : amount; if (boundary == 0 || (boundary & (boundary - 1)) != 0 || boundary > 65536) throw new FormatException("Alignment must be a bounded power of two."); next = checked((Location() + boundary - 1) & ~(boundary - 1)); }
                    else next = checked(Location() + amount);
                    var size = checked((int)(next - Location())); Reserve(Location(), size, n + 1);
                    for (uint a = Location(); a < next; a++) data[a] = 0; SetLocation(next); continue;
                }
                var elementSize = name switch { ".BYTE" or "DCB" => 1, ".HWORD" or ".SHORT" or "DCW" => 2, ".WORD" or ".LONG" or "DCD" or ".FLOAT" or ".SINGLE" => 4, _ => 0 };
                if (elementSize != 0)
                {
                    var args = SplitOperands(body); if (args.Length == 0) throw new FormatException("Data directive needs a value.");
                    foreach (var arg in args)
                    {
                        Reserve(Location(), elementSize, n + 1);
                        pendingData.Add((n + 1, Location(), elementSize, name is ".FLOAT" or ".SINGLE" ? $"#{BitConverter.SingleToUInt32Bits(float.Parse(arg, CultureInfo.InvariantCulture))}" : arg));
                        SetLocation(checked(Location() + (uint)elementSize));
                    }
                    continue;
                }
                if (section != "TEXT") throw new FormatException("Instructions must be in .text.");
                var operation = InstructionSet.Decode(name); var width = operation.Width;
                // Explicit listings carry their layout; use an adjacent two-byte slot for an unsuffixed operation.
                if (explicitAddress.HasValue && !name.Contains('.'))
                {
                    for (var k = n + 1; k < rows.Length; k++)
                    {
                        var next = Regex.Split(rows[k], @";|//")[0].Trim(); if (next.Length == 0) continue;
                        var m = Regex.Match(next, @"^(?:0x)?([0-9a-fA-F]{8}):?\s+");
                        if (m.Success && uint.Parse(m.Groups[1].Value, NumberStyles.HexNumber) == explicitAddress + 2) width = 2;
                        break;
                    }
                }
                if ((Location() & 1) != 0) throw new FormatException("Instruction addresses must be halfword aligned.");
                Reserve(Location(), width, n + 1);
                instructions.Add(new(n + 1, name, SplitOperands(body), Location()) { Operation = operation, Width = width });
                SetLocation(checked(Location() + (uint)width));
                if (instructions.Count > MaximumInstructions) throw new FormatException("Program exceeds 2,000 instructions.");
            }
            catch (Exception error) when (error is FormatException or OverflowException or ArgumentException)
            { throw new AssemblyException(n + 1, error.Message); }
        }
        if (instructions.Count == 0) throw new AssemblyException(1, "Enter at least one instruction.");
        var maximumPasses = constants.Count;
        for (var pass = 0; constants.Count > 0 && pass <= maximumPasses; pass++)
            foreach (var c in constants.ToArray())
            { try { symbols.Add(c.Name, Expression(c.Value, symbols)); constants.Remove(c); } catch (FormatException) { } }
        if (constants.Count != 0) throw new AssemblyException(constants[0].Line, "Unresolved or cyclic constant.");
        foreach (var d in pendingData)
        {
            uint value;
            try { value = Expression(d.Value, symbols); }
            catch (Exception e) when (e is FormatException or OverflowException) { throw new AssemblyException(d.Line, e.Message); }
            if (d.Size < 4 && value >= (1u << (d.Size * 8))) throw new AssemblyException(d.Line, "Data value does not fit directive width.");
            for (var j = 0; j < d.Size; j++) data[d.Address + (uint)j] = (byte)(value >> (8 * j));
        }
        var program = new AssemblyProgram(instructions, symbols, data); var itSlots = 0;
        foreach (var i in instructions)
        {
            try
            {
                if (i.Width == 2 && !InstructionSet.AllowsNarrow(i.Operation.Name)) throw new FormatException("Instruction does not have a narrow form.");
                i.Arguments = program.ParseArguments(i);
                InstructionValidation.Validate(i, program);
                if (i.Operation.Name.StartsWith("IT", StringComparison.Ordinal)) { if (itSlots != 0) throw new FormatException("Nested IT block."); itSlots = i.Operation.Name.Length - 1; }
                else if (itSlots > 0) { if (InstructionValidation.WritesPc(i) && itSlots != 1) throw new FormatException("A branch/PC write must be last in an IT block."); itSlots--; }
            }
            catch (Exception e) when (e is FormatException or OverflowException or ArgumentException) { throw new AssemblyException(i.Line, e.Message); }
        }
        if (itSlots != 0) throw new AssemblyException(instructions[^1].Line, "Incomplete IT block.");
        return program;
    }
    public static string[] SplitOperands(string text)
    {
        if (text.Length == 0) return [];
        var result = new List<string>(); var brackets = 0; var braces = 0; var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '[') brackets++; if (text[i] == ']') brackets--; if (text[i] == '{') braces++; if (text[i] == '}') braces--;
            if (brackets is < 0 or > 1 || braces is < 0 or > 1) throw new FormatException("Invalid operand delimiters.");
            if (text[i] == ',' && brackets == 0 && braces == 0) { result.Add(text[start..i].Trim()); start = i + 1; }
        }
        if (brackets != 0 || braces != 0) throw new FormatException("Unclosed operand.");
        result.Add(text[start..].Trim()); if (result.Any(string.IsNullOrWhiteSpace)) throw new FormatException("Missing operand."); return result.ToArray();
    }
    public static int Register(string token)
    {
        var r = ParseRegister(token); if (r.Bank != 'R') throw new FormatException("Expected an integer register."); return r.Index;
    }
    internal static RegisterOperand ParseRegister(string token)
    {
        token = token.Trim().ToUpperInvariant(); var writeback = token.EndsWith('!'); token = token.TrimEnd('!');
        token = token switch { "SP" => "R13", "LR" => "R14", "PC" => "R15", _ => token };
        if (token.Length >= 2 && token[0] is 'R' or 'S' or 'D' && int.TryParse(token[1..], out var i) && i >= 0 && i < (token[0] == 'S' ? 32 : 16)) return new(i, token[0], writeback);
        throw new FormatException($"Invalid register '{token}'.");
    }
    public static uint Number(string token) => Expression(token, new Dictionary<string, uint>());
    internal static uint Expression(string token, IReadOnlyDictionary<string, uint> symbols)
    {
        token = token.Trim().TrimStart('#', '=');
        if (symbols.TryGetValue(token, out var symbol)) return symbol;
        uint result = 0; var position = 0; var first = true;
        while (position < token.Length)
        {
            while (position < token.Length && char.IsWhiteSpace(token[position])) position++;
            var subtract = false;
            if (position < token.Length && token[position] is '+' or '-') { subtract = token[position] == '-'; position++; }
            else if (!first) throw new FormatException("Expected + or - in expression.");
            while (position < token.Length && char.IsWhiteSpace(token[position])) position++;
            var start = position;
            while (position < token.Length && (char.IsLetterOrDigit(token[position]) || token[position] is '_' or '.' or '$')) position++;
            if (start == position) throw new FormatException("Missing expression term.");
            var term = token[start..position]; uint value;
            if (!symbols.TryGetValue(term, out value))
            {
                ulong wide;
                try { wide = term.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? ulong.Parse(term[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture) : ulong.Parse(term, NumberStyles.None, CultureInfo.InvariantCulture); }
                catch (FormatException) { throw new FormatException($"Unknown symbol or invalid immediate '{term}'."); }
                if (wide > uint.MaxValue || first && subtract && wide > 0x80000000UL) throw new FormatException("Immediate outside the 32-bit range.");
                value = (uint)wide;
            }
            result = subtract ? unchecked(result - value) : unchecked(result + value); first = false;
        }
        if (first) throw new FormatException("Missing immediate."); return result;
    }
    private Operand[] ParseArguments(Instruction instruction)
    {
        Operand Parse(string text)
        {
            text = text.Trim();
            if (Regex.IsMatch(text, @"^(R\d+|SP|LR|PC|[SD]\d+)!?$", RegexOptions.IgnoreCase)) return ParseRegister(text);
            var lane = Regex.Match(text, @"^(D\d+)\[([01])\]$", RegexOptions.IgnoreCase);
            if (lane.Success) return new LaneOperand(ParseRegister(lane.Groups[1].Value), int.Parse(lane.Groups[2].Value));
            if (text.StartsWith('{'))
            {
                if (!text.EndsWith('}')) throw new FormatException("Unclosed register list.");
                var regs = new List<RegisterOperand>();
                foreach (var part in text[1..^1].Split(','))
                {
                    var range = part.Split('-'); var first = ParseRegister(range[0]); var last = range.Length == 2 ? ParseRegister(range[1]) : first;
                    if (first.Writeback || last.Writeback || range.Length > 2 || first.Bank != last.Bank || first.Index > last.Index) throw new FormatException("Invalid register range.");
                    for (var j = first.Index; j <= last.Index; j++) regs.Add(new(j, first.Bank));
                }
                if (regs.Count == 0 || regs.Distinct().Count() != regs.Count || regs.Select(r => r.Bank).Distinct().Count() != 1) throw new FormatException("Empty, duplicate, or mixed register list.");
                return new RegisterListOperand(regs.OrderBy(r => r.Index).ToArray());
            }
            if (text.StartsWith('['))
            {
                var writeback = text.EndsWith('!'); var inner = text.TrimEnd('!'); if (!inner.EndsWith(']')) throw new FormatException("Invalid memory operand.");
                var parts = SplitOperands(inner[1..^1]); if (parts.Length is < 1 or > 3) throw new FormatException("Invalid address operands.");
                var offset = parts.Length > 1 ? parts[1] : "#0"; var subtract = offset.StartsWith('-'); if (subtract) offset = offset[1..];
                return new MemoryOperand(ParseRegister(parts[0]), Parse(offset), parts.Length == 3 ? Parse(parts[2]) as ShiftOperand ?? throw new FormatException("Expected shift.") : null, writeback, false, subtract);
            }
            var shift = Regex.Match(text, @"^(LSL|LSR|ASR|ROR|RRX)(?:\s+(.+))?$", RegexOptions.IgnoreCase);
            if (shift.Success) return new ShiftOperand(shift.Groups[1].Value.ToUpperInvariant(), Parse(shift.Groups[2].Success ? shift.Groups[2].Value : "#1"));
            if (Regex.IsMatch(text, @"^(APSR(_NZCVQG|_NZCVQ|_NZCV|_G)?|IAPSR|EAPSR|XPSR|IPSR|EPSR|IEPSR|MSP|PSP|PRIMASK|BASEPRI(_MAX)?|FAULTMASK|CONTROL|FPSCR|SY|ST|ISH(ST)?|NSH(ST)?|OSH(ST)?|I|F|IF|FI|EQ|NE|CS|HS|CC|LO|MI|PL|VS|VC|HI|LS|GE|LT|GT|LE|AL)$", RegexOptions.IgnoreCase)) return new NamedOperand(text.ToUpperInvariant());
            if (instruction.Operation.Name == "VMOV" && text.StartsWith('#')) return new ImmediateOperand(BitConverter.SingleToUInt32Bits(float.Parse(text[1..], CultureInfo.InvariantCulture)));
            return new ImmediateOperand(Expression(text, symbols));
        }
        var args = instruction.Operands.Select(Parse).ToList();
        var mem = args.FindIndex(a => a is MemoryOperand);
        if (mem >= 0 && mem + 1 < args.Count)
        {
            var address = (MemoryOperand)args[mem];
            if (address.Writeback || address.Offset is not ImmediateOperand { Value: 0 } || args.Count != mem + 2 || args[mem + 1] is not (ImmediateOperand or RegisterOperand)) throw new FormatException("Invalid post-indexed address.");
            args[mem] = address with { Offset = args[mem + 1], Writeback = true, PostIndex = true }; args.RemoveAt(mem + 1);
        }
        return args.ToArray();
    }
    public uint Resolve(string token) => Expression(token, symbols);
    public int Target(string token) => RegisterTarget(Resolve(token));
    internal int RegisterTarget(uint value)
    {
        if (value == uint.MaxValue || value == EndAddress || value == (EndAddress | 1u)) return Instructions.Count;
        if (addresses.TryGetValue(value & ~1u, out var index)) return index;
        throw new FormatException($"No instruction at address 0x{value:X8}. Use a code address or label, not a source index.");
    }
    internal static bool IsConditionalBranch(string op) => op.Length > 1 && op[0] == 'B' && Conditions.Contains(op[1..]);
}
