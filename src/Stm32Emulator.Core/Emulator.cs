using System.Numerics;
using System.Text.RegularExpressions;

namespace Stm32Emulator.Core;

public readonly record struct CpuFlags(bool N, bool Z, bool C, bool V);
public enum ExecutionStatus { Empty, Ready, Paused, Completed, Faulted }
public sealed record EmulatorSnapshot(uint[] Registers, CpuFlags Flags, int InstructionIndex, int? SourceLine,
    ulong InstructionsExecuted, ExecutionStatus Status, string Message, ulong Ticks);

public sealed class Emulator
{
    private readonly uint[] registers = new uint[16];
    private readonly Dictionary<string, uint> floatingStatus = new(StringComparer.OrdinalIgnoreCase);
    private readonly Queue<(string Condition, bool Invert)> conditionalExecution = new();
    private int nextIndex;
    public AssemblyProgram? Program { get; private set; }
    public MemoryBus Memory { get; } = new();
    public CpuFlags Flags { get; private set; }
    public ExecutionStatus Status { get; private set; } = ExecutionStatus.Empty;
    public int InstructionIndex { get; private set; }
    public ulong InstructionsExecuted { get; private set; }
    public string Message { get; private set; } = "Load an example or enter assembly.";

    public void LoadProgram(string source)
    {
        var parsed = AssemblyProgram.Parse(source); // validate everything before replacing state
        Program = parsed; Reset();
    }

    public void Reset()
    {
        Array.Clear(registers); registers[13] = MemoryBus.RamBase + MemoryBus.RamSize; registers[14] = uint.MaxValue;
        floatingStatus.Clear(); floatingStatus["FPSCR"] = 0; floatingStatus["FPSID"] = 0; floatingStatus["FPEXC"] = 0;
        Memory.Reset(); Flags = default; conditionalExecution.Clear(); InstructionIndex = 0; InstructionsExecuted = 0;
        Status = Program is null ? ExecutionStatus.Empty : ExecutionStatus.Ready;
        Message = Program is null ? "Load an example or enter assembly." : "Ready. One Step executes one source instruction and one simulated tick.";
        UpdatePc();
    }

    public EmulatorSnapshot Snapshot() => new((uint[])registers.Clone(), Flags, InstructionIndex,
        Program is not null && InstructionIndex < Program.Instructions.Count ? Program.Instructions[InstructionIndex].Line : null,
        InstructionsExecuted, Status, Message, Memory.Ticks);

    private void UpdatePc() => registers[15] = Program is not null && InstructionIndex < Program.Instructions.Count
        ? Program.Instructions[InstructionIndex].Address ?? (uint)InstructionIndex : (uint)InstructionIndex;

    private uint Value(string operand) => Regex.IsMatch(operand, @"^(?:r\d+|sp|lr|pc)$", RegexOptions.IgnoreCase)
        ? registers[AssemblyProgram.Register(operand)] : AssemblyProgram.Number(operand);

    private uint Address(string operand)
    {
        var (reg, offset) = AssemblyProgram.MemoryOperand(operand);
        var displacement = Value(offset);
        // Negative offsets are 32-bit two's complement; arithmetic wraps like a 32-bit address calculation.
        return unchecked(registers[reg] + displacement);
    }

    private void Set(string register, uint value)
    {
        var index = AssemblyProgram.Register(register);
        if (index == 15) nextIndex = Program!.RegisterTarget(value);
        else registers[index] = value;
    }

    public bool Condition(string condition) => condition.ToUpperInvariant() switch
    {
        "EQ" => Flags.Z, "NE" => !Flags.Z, "CS" or "HS" => Flags.C, "CC" or "LO" => !Flags.C,
        "MI" => Flags.N, "PL" => !Flags.N, "VS" => Flags.V, "VC" => !Flags.V,
        "HI" => Flags.C && !Flags.Z, "LS" => !Flags.C || Flags.Z,
        "GE" => Flags.N == Flags.V, "LT" => Flags.N != Flags.V,
        "GT" => !Flags.Z && Flags.N == Flags.V, "LE" => Flags.Z || Flags.N != Flags.V, "AL" => true,
        _ => throw new FormatException("Unknown condition.")
    };

    private void Nz(uint result) => Flags = Flags with { N = (result & 0x80000000) != 0, Z = result == 0 };
    private uint Add(uint left, uint right, bool carry, bool update)
    {
        var wide = (ulong)left + right + (carry ? 1UL : 0UL);
        var signed = (long)(int)left + (int)right + (carry ? 1L : 0L);
        var result = unchecked((uint)wide);
        if (update) { Nz(result); Flags = Flags with { C = wide > uint.MaxValue, V = signed is > int.MaxValue or < int.MinValue }; }
        return result;
    }
    private uint Subtract(uint left, uint right, bool update)
    {
        var result = unchecked(left - right);
        if (update) { Nz(result); Flags = Flags with { C = left >= right, V = ((left ^ right) & (left ^ result) & 0x80000000) != 0 }; }
        return result;
    }

    private uint Shift(string operation, uint value, uint amount, bool update)
    {
        amount &= 0xFF; // register-specified shifts use the low byte
        var carry = Flags.C;
        uint result = value;
        if (amount != 0)
        {
            switch (operation)
            {
                case "LSL":
                    carry = amount <= 32 && ((value >> (int)(32 - amount)) & 1) != 0;
                    result = amount < 32 ? value << (int)amount : 0; break;
                case "LSR":
                    carry = amount <= 32 && ((value >> (int)(amount - 1)) & 1) != 0;
                    result = amount < 32 ? value >> (int)amount : 0; break;
                case "ASR":
                    carry = amount >= 32 ? (value & 0x80000000) != 0 : ((value >> (int)(amount - 1)) & 1) != 0;
                    result = amount >= 32 ? ((value & 0x80000000) != 0 ? uint.MaxValue : 0) : unchecked((uint)((int)value >> (int)amount)); break;
            }
        }
        if (update) { Nz(result); Flags = Flags with { C = carry }; }
        return result;
    }

    public EmulatorSnapshot Step()
    {
        if (Program is null) throw new InvalidOperationException("Load a program first.");
        if (Status is ExecutionStatus.Completed or ExecutionStatus.Faulted) return Snapshot();
        var instruction = Program.Instructions[InstructionIndex];
        nextIndex = InstructionIndex + 1;
        Message = "";
        try
        {
            var execute = true;
            if (conditionalExecution.Count != 0)
            {
                var predicate = conditionalExecution.Dequeue();
                execute = Condition(predicate.Condition) != predicate.Invert;
            }
            if (execute) Execute(instruction);
            Memory.AdvanceTicks(); InstructionsExecuted++;
            InstructionIndex = nextIndex; UpdatePc();
            if (InstructionIndex >= Program.Instructions.Count) { Status = ExecutionStatus.Completed; Message = "Program completed."; }
            else { Status = ExecutionStatus.Paused; if (Message != "Breakpoint reached.") Message = "Paused. Step or Run to continue."; }
        }
        catch (Exception error) when (error is FormatException or InvalidOperationException or OverflowException)
        { Status = ExecutionStatus.Faulted; Message = new AssemblyException(instruction.Line, error.Message).Message; }
        return Snapshot();
    }

    private void Execute(Instruction instruction)
    {
        var op = instruction.Mnemonic;
        var a = instruction.Operands;
        if (op.StartsWith("IT", StringComparison.Ordinal))
        {
            conditionalExecution.Enqueue((a[0], false));
            foreach (var letter in op[2..]) conditionalExecution.Enqueue((a[0], letter == 'E'));
            return;
        }
        if (op == "NOP") return;
        if (op == "BKPT") { Message = "Breakpoint reached."; return; }
        if (op == "B") { nextIndex = Program!.Target(a[0]); return; }
        if (op == "BX") { nextIndex = Program!.RegisterTarget(Value(a[0])); return; }
        if (op is "CBZ" or "CBNZ") { if ((Value(a[0]) == 0) == (op == "CBZ")) nextIndex = Program!.Target(a[1]); return; }
        if (AssemblyProgram.IsConditionalBranch(op)) { if (Condition(op[1..])) nextIndex = Program!.Target(a[0]); return; }
        if (op == "VMRS") { Set(a[0], floatingStatus[a[1]]); return; }
        if (op == "VMSR") { floatingStatus[a[0]] = Value(a[1]); return; }
        if (op is "LDR" or "LDRB") { Set(a[0], !a[1].StartsWith('[') ? Value(a[1]) : op == "LDR" ? Memory.ReadWord(Address(a[1])) : Memory.ReadByte(Address(a[1]))); return; }
        if (op is "STR" or "STRB") { if (op == "STR") Memory.WriteWord(Address(a[1]), Value(a[0])); else Memory.WriteByte(Address(a[1]), (byte)Value(a[0])); return; }
        if (op == "CMP") { Subtract(Value(a[0]), Value(a[1]), true); return; }
        if (op is "MOV" or "MOVS") { var value = Value(a[1]); Set(a[0], value); if (op == "MOVS") Nz(value); return; }
        if (op == "MOVW") { Set(a[0], Value(a[1])); return; }
        if (op == "MOVT") { Set(a[0], (Value(a[0]) & 0xFFFF) | Value(a[1]) << 16); return; }
        if (op == "CLZ") { Set(a[0], (uint)BitOperations.LeadingZeroCount(Value(a[1]))); return; }
        var left = Value(a.Length == 3 ? a[1] : a[0]);
        var right = Value(a[^1]);
        uint result = op switch
        {
            "ORR" => left | right, "ORN" => left | ~right, "BIC" => left & ~right,
            "AND" => left & right, "EOR" => left ^ right,
            "ADD" or "ADDS" => Add(left, right, false, op == "ADDS"),
            "ADC" or "ADCS" => Add(left, right, Flags.C, op == "ADCS"),
            "SUB" or "SUBS" => Subtract(left, right, op == "SUBS"),
            "LSL" or "LSLS" => Shift("LSL", left, right, op == "LSLS"),
            "LSR" or "LSRS" => Shift("LSR", left, right, op == "LSRS"),
            "ASR" or "ASRS" => Shift("ASR", left, right, op == "ASRS"),
            _ => throw new InvalidOperationException($"Unsupported instruction {op}.")
        };
        Set(a[0], result);
    }
}
