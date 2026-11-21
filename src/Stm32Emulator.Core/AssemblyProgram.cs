using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Stm32Emulator.Core;

public sealed class AssemblyException(int line, string message) : Exception($"Line {line}: {message}")
{
    public int Line { get; } = line;
}

public sealed record Instruction(int Line, string Mnemonic, string[] Operands, uint? Address);

/// <summary>Parses an educational assembly listing, not machine code or an ARM binary.</summary>
public sealed class AssemblyProgram
{
    public const int MaximumSourceBytes = 65_536;
    public const int MaximumInstructions = 2_000;
    public IReadOnlyList<Instruction> Instructions { get; }
    private readonly Dictionary<string, int> labels;
    private readonly Dictionary<uint, int> addresses;
    private static readonly HashSet<string> Operations = new(StringComparer.OrdinalIgnoreCase)
    {
        "MOV", "MOVS", "MOVW", "MOVT", "LDR", "STR", "LDRB", "STRB",
        "ORR", "ORN", "BIC", "AND", "EOR", "ADD", "ADDS", "SUB", "SUBS", "ADC", "ADCS",
        "CMP", "LSL", "LSLS", "LSR", "LSRS", "ASR", "ASRS", "CLZ", "B", "BX",
        "BEQ", "BNE", "BPL", "BMI", "BGT", "BLT", "BLE", "BGE", "BHI", "BLS", "BCS", "BCC", "BVS", "BVC",
        "CBZ", "CBNZ", "VMRS", "VMSR", "NOP", "BKPT"
    };
    public static readonly string[] Conditions = ["EQ", "NE", "CS", "HS", "CC", "LO", "MI", "PL", "VS", "VC", "HI", "LS", "GE", "LT", "GT", "LE", "AL"];
    internal static bool IsConditionalBranch(string op) => op is "BEQ" or "BNE" or "BPL" or "BMI" or "BGT" or "BLT" or "BLE" or "BGE" or "BHI" or "BLS" or "BCS" or "BCC" or "BVS" or "BVC";

    private AssemblyProgram(List<Instruction> instructions, Dictionary<string, int> labels, Dictionary<uint, int> addresses)
    {
        Instructions = instructions.AsReadOnly();
        this.labels = labels;
        this.addresses = addresses;
    }

    public static AssemblyProgram Parse(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (Encoding.UTF8.GetByteCount(source) > MaximumSourceBytes) throw new AssemblyException(1, "Source exceeds 64 KiB.");
        var instructions = new List<Instruction>();
        var labels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var addresses = new Dictionary<uint, int>();
        var lines = source.Replace("\r", "").Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            var text = Regex.Split(lines[index], @";|//", RegexOptions.None, TimeSpan.FromSeconds(1))[0].Trim();
            var label = Regex.Match(text, @"^([a-zA-Z_]\w*):");
            if (label.Success)
            {
                if (!labels.TryAdd(label.Groups[1].Value, instructions.Count)) throw new AssemblyException(index + 1, "Duplicate label.");
                text = text[label.Length..].Trim();
            }
            if (text.Length == 0) continue;
            uint? address = null;
            var addressMatch = Regex.Match(text, @"^(?:0x)?([0-9a-fA-F]{8}):?\s+");
            if (addressMatch.Success)
            {
                address = uint.Parse(addressMatch.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                if (!addresses.TryAdd(address.Value, instructions.Count)) throw new AssemblyException(index + 1, "Duplicate listing address.");
                text = text[addressMatch.Length..].Trim();
            }
            var parts = text.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) throw new AssemblyException(index + 1, "Missing instruction after address.");
            var mnemonic = parts[0].ToUpperInvariant();
            if (mnemonic.EndsWith(".W", StringComparison.Ordinal)) mnemonic = mnemonic[..^2];
            if (!Operations.Contains(mnemonic) && !Regex.IsMatch(mnemonic, @"^IT[TE]{0,3}$"))
                throw new AssemblyException(index + 1, $"Unsupported instruction '{parts[0]}'. See the instruction reference.");
            var operands = SplitOperands(parts.Length == 2 ? parts[1] : "", index + 1);
            instructions.Add(new(index + 1, mnemonic, operands, address));
            if (instructions.Count > MaximumInstructions) throw new AssemblyException(index + 1, "Program exceeds 2,000 instructions.");
        }
        if (instructions.Count == 0) throw new AssemblyException(1, "Enter at least one instruction.");
        if (addresses.Count != 0 && addresses.Count != instructions.Count) throw new AssemblyException(1, "Give every instruction an address, or use an unaddressed listing with labels.");
        var program = new AssemblyProgram(instructions, labels, addresses);
        var conditionalSlots = 0;
        foreach (var instruction in instructions)
        {
            program.Validate(instruction);
            if (instruction.Mnemonic.StartsWith("IT", StringComparison.Ordinal))
            {
                if (conditionalSlots != 0) throw new AssemblyException(instruction.Line, "Nested IT blocks are unsupported.");
                conditionalSlots = instruction.Mnemonic.Length - 1;
            }
            else if (conditionalSlots > 0) conditionalSlots--;
        }
        if (conditionalSlots > 0) throw new AssemblyException(instructions[^1].Line, "Incomplete IT block.");
        return program;
    }

    private static string[] SplitOperands(string text, int line)
    {
        if (text.Length == 0) return [];
        var result = new List<string>();
        var depth = 0;
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '[') depth++;
            if (text[i] == ']') depth--;
            if (depth is < 0 or > 1) throw new AssemblyException(line, "Invalid memory operand.");
            if (text[i] == ',' && depth == 0) { result.Add(text[start..i].Trim()); start = i + 1; }
        }
        if (depth != 0) throw new AssemblyException(line, "Unclosed memory operand.");
        result.Add(text[start..].Trim());
        if (result.Any(string.IsNullOrWhiteSpace)) throw new AssemblyException(line, "Missing operand.");
        return result.ToArray();
    }

    public static int Register(string token)
    {
        token = token.Trim().ToUpperInvariant();
        if (token == "SP") return 13;
        if (token == "LR") return 14;
        if (token == "PC") return 15;
        if (token.StartsWith('R') && int.TryParse(token[1..], out var index) && index is >= 0 and <= 15) return index;
        throw new FormatException($"Invalid register '{token}'. Expected R0–R15, SP, LR, or PC.");
    }

    public static uint Number(string token)
    {
        token = token.Trim().TrimStart('#', '=');
        var negative = token.StartsWith('-');
        if (negative) token = token[1..];
        var value = token.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? ulong.Parse(token[2..], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture)
            : ulong.Parse(token, NumberStyles.None, CultureInfo.InvariantCulture);
        if (value > uint.MaxValue || negative && value > 0x80000000UL) throw new FormatException("Immediate is outside the 32-bit range.");
        return negative ? unchecked(0u - (uint)value) : (uint)value;
    }

    internal static void ValidateValue(string token)
    {
        if (Regex.IsMatch(token, @"^(?:r\d+|sp|lr|pc)$", RegexOptions.IgnoreCase)) Register(token);
        else Number(token);
    }

    internal static (int Register, string Offset) MemoryOperand(string token)
    {
        if (!token.StartsWith('[') || !token.EndsWith(']')) throw new FormatException("Expected [register, offset]. Writeback and post-indexing are unsupported.");
        var operands = token[1..^1].Split(',').Select(x => x.Trim()).ToArray();
        if (operands.Length is < 1 or > 2) throw new FormatException("Expected [register] or [register, offset].");
        if (operands.Length == 2) ValidateValue(operands[1]);
        return (Register(operands[0]), operands.Length == 2 ? operands[1] : "#0");
    }

    public int Target(string token)
    {
        if (labels.TryGetValue(token, out var index)) return index;
        if (addresses.Count == 0) throw new FormatException($"Unknown label '{token}'. Numeric branches require explicitly addressed listings.");
        if (addresses.TryGetValue(Number(token), out index)) return index;
        throw new FormatException($"No instruction at branch address '{token}'.");
    }

    internal int RegisterTarget(uint value)
    {
        if (value == uint.MaxValue) return Instructions.Count; // educational return sentinel
        if (addresses.Count != 0 && addresses.TryGetValue(value & ~1u, out var index)) return index;
        if (addresses.Count == 0 && value <= Instructions.Count) return (int)value;
        throw new FormatException("Register branch target does not identify an instruction.");
    }

    private void Validate(Instruction instruction)
    {
        var op = instruction.Mnemonic;
        var args = instruction.Operands;
        void Count(params int[] counts) { if (!counts.Contains(args.Length)) throw new FormatException($"{op} expects {string.Join(" or ", counts)} operands."); }
        try
        {
            if (op.StartsWith("IT", StringComparison.Ordinal))
            {
                Count(1);
                if (!Conditions.Contains(args[0].ToUpperInvariant()) || args[0].Equals("AL", StringComparison.OrdinalIgnoreCase)) throw new FormatException("Expected an IT condition such as EQ or NE.");
            }
            else if (op is "NOP" or "BKPT") { Count(0); }
            else if (op == "BX") { Count(1); Register(args[0]); }
            else if (op is "CBZ" or "CBNZ") { Count(2); Register(args[0]); Target(args[1]); }
            else if (op == "B" || IsConditionalBranch(op)) { Count(1); Target(args[0]); }
            else if (op is "VMRS" or "VMSR")
            {
                Count(2);
                Register(args[op == "VMRS" ? 0 : 1]);
                if (args[op == "VMRS" ? 1 : 0].ToUpperInvariant() is not ("FPSCR" or "FPSID" or "FPEXC")) throw new FormatException("Expected FPSCR, FPSID, or FPEXC.");
            }
            else if (op is "LDR" or "LDRB" or "STR" or "STRB")
            {
                Count(2); Register(args[0]);
                if (op == "LDR" && !args[1].StartsWith('[')) Number(args[1]);
                else MemoryOperand(args[1]);
            }
            else if (op is "MOV" or "MOVS" or "MOVW" or "MOVT" or "CMP" or "CLZ")
            {
                Count(2); Register(args[0]); ValidateValue(args[1]);
                if (op is "MOVW" or "MOVT" && Number(args[1]) > ushort.MaxValue) throw new FormatException("MOVW/MOVT requires a 16-bit immediate.");
                if (op == "CLZ") Register(args[1]);
            }
            else
            {
                Count(2, 3); Register(args[0]);
                if (args.Length == 3) Register(args[1]);
                ValidateValue(args[^1]);
                if (op is "LSL" or "LSLS" or "LSR" or "LSRS" or "ASR" or "ASRS")
                {
                    if (!Regex.IsMatch(args[^1], @"^(?:r\d+|sp|lr|pc)$", RegexOptions.IgnoreCase) && Number(args[^1]) > 32) throw new FormatException("Immediate shift must be between 0 and 32.");
                }
            }
        }
        catch (Exception error) when (error is FormatException or OverflowException)
        { throw new AssemblyException(instruction.Line, error.Message); }
    }
}
