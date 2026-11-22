using System.Numerics;

namespace Stm32Emulator.Core;

public readonly record struct CpuFlags(bool N, bool Z, bool C, bool V, bool Q = false, byte Ge = 0)
{
    public uint Packed => (N ? 1u << 31 : 0) | (Z ? 1u << 30 : 0) | (C ? 1u << 29 : 0) | (V ? 1u << 28 : 0) | (Q ? 1u << 27 : 0) | (uint)(Ge & 15) << 16;
}
public enum ExecutionStatus { Empty, Ready, Paused, Completed, Faulted, Waiting }
public enum ExecutionStop { None, Step, Breakpoint, ServiceCall, UndefinedInstruction, WaitForEvent, WaitForInterrupt, Fault, Completed, Yield }
public sealed record SystemSnapshot(uint Msp, uint Psp, uint Primask, uint Basepri, uint Faultmask, uint Control, bool EventPending, bool InterruptPending);
public sealed record EmulatorSnapshot(uint[] Registers, CpuFlags Flags, int InstructionIndex, int? SourceLine, ulong InstructionsExecuted, ExecutionStatus Status, string Message, ulong Ticks)
{
    public uint[] FloatingRegisters { get; init; } = [];
    public uint Fpscr { get; init; }
    public SystemSnapshot System { get; init; } = null!;
    public ExecutionStop Stop { get; init; }
    public uint? TrapImmediate { get; init; }
}

public sealed partial class Emulator
{
    private readonly uint[] registers = new uint[16];
    internal readonly uint[] floating = new uint[32];
    internal uint fpscr;
    private readonly Queue<(string Condition, bool Invert)> conditionalExecution = new();
    private int nextIndex;
    private uint msp, psp, primask, basepri, faultmask, control;
    private bool eventPending, interruptPending;
    public AssemblyProgram? Program { get; private set; }
    public MemoryBus Memory { get; } = new();
    public CpuFlags Flags { get; private set; }
    public ExecutionStatus Status { get; private set; } = ExecutionStatus.Empty;
    public ExecutionStop Stop { get; private set; }
    public uint? TrapImmediate { get; private set; }
    public int InstructionIndex { get; private set; }
    public ulong InstructionsExecuted { get; private set; }
    public string Message { get; private set; } = "Load an example or enter assembly.";
    public void LoadProgram(string source) { var parsed = AssemblyProgram.Parse(source); Program = parsed; Reset(); }
    public void Reset()
    {
        Array.Clear(registers); Array.Clear(floating); fpscr = 0;
        msp = psp = MemoryBus.RamBase + MemoryBus.RamSize; primask = basepri = faultmask = control = 0; eventPending = interruptPending = false;
        registers[14] = uint.MaxValue; Memory.Reset(); if (Program is not null) Memory.LoadData(Program.InitialData);
        Flags = default; conditionalExecution.Clear(); InstructionIndex = 0; InstructionsExecuted = 0; TrapImmediate = null; Stop = ExecutionStop.None;
        Status = Program is null ? ExecutionStatus.Empty : ExecutionStatus.Ready;
        Message = Program is null ? "Load a program." : "Ready. One Step executes one instruction and one simulated tick."; UpdatePc();
    }
    private void UpdatePc()
    { registers[13] = (control & 2) == 0 ? msp : psp; registers[15] = Program is null ? 0 : InstructionIndex < Program.Instructions.Count ? Program.Instructions[InstructionIndex].Address!.Value : Program.EndAddress; }
    public EmulatorSnapshot Snapshot() { UpdatePc(); return new((uint[])registers.Clone(), Flags, InstructionIndex, Program is not null && InstructionIndex < Program.Instructions.Count ? Program.Instructions[InstructionIndex].Line : null, InstructionsExecuted, Status, Message, Memory.Ticks) { FloatingRegisters = (uint[])floating.Clone(), Fpscr = fpscr, System = new(msp, psp, primask, basepri, faultmask, control, eventPending, interruptPending), Stop = Stop, TrapImmediate = TrapImmediate }; }
    internal uint Read(RegisterOperand r)
    {
        if (r.Bank == 'S') return floating[r.Index];
        if (r.Bank != 'R') throw new FormatException("Expected a single register.");
        return r.Index == 15 ? registers[15] + 4 : r.Index == 13 ? (control & 2) == 0 ? msp : psp : registers[r.Index];
    }
    internal uint Value(Operand a) => a switch { RegisterOperand r => Read(r), ImmediateOperand v => v.Value, _ => throw new FormatException("Expected a value operand.") };
    internal void Write(RegisterOperand r, uint value)
    {
        if (r.Bank == 'S') { floating[r.Index] = value; return; }
        if (r.Bank != 'R') throw new FormatException("Expected integer/single register.");
        if (r.Index == 15) nextIndex = Program!.RegisterTarget(value);
        else if (r.Index == 13) { if ((control & 2) == 0) msp = value & ~3u; else psp = value & ~3u; }
        else registers[r.Index] = value;
    }
    internal ulong ReadDouble(RegisterOperand r) => floating[r.Index * 2] | (ulong)floating[r.Index * 2 + 1] << 32;
    internal void WriteDouble(RegisterOperand r, ulong v) { floating[r.Index * 2] = (uint)v; floating[r.Index * 2 + 1] = (uint)(v >> 32); }
    public bool Condition(string c) => c.ToUpperInvariant() switch
    {
        "EQ" => Flags.Z,
        "NE" => !Flags.Z,
        "CS" or "HS" => Flags.C,
        "CC" or "LO" => !Flags.C,
        "MI" => Flags.N,
        "PL" => !Flags.N,
        "VS" => Flags.V,
        "VC" => !Flags.V,
        "HI" => Flags.C && !Flags.Z,
        "LS" => !Flags.C || Flags.Z,
        "GE" => Flags.N == Flags.V,
        "LT" => Flags.N != Flags.V,
        "GT" => !Flags.Z && Flags.N == Flags.V,
        "LE" => Flags.Z || Flags.N != Flags.V,
        "AL" => true,
        _ => throw new FormatException("Unknown condition.")
    };
    private void Nz(uint v) => Flags = Flags with { N = (v & 0x80000000) != 0, Z = v == 0 };
    private uint Add(uint x, uint y, bool carry, bool update)
    {
        var wide = (ulong)x + y + (carry ? 1u : 0u); var signed = (long)(int)x + (int)y + (carry ? 1 : 0); var result = (uint)wide;
        if (update) { Nz(result); Flags = Flags with { C = wide > uint.MaxValue, V = signed is > int.MaxValue or < int.MinValue }; }
        return result;
    }
    private uint Shift(string kind, uint x, uint amount, out bool carry)
    {
        carry = Flags.C; amount &= 255; if (kind == "RRX") { carry = (x & 1) != 0; return (Flags.C ? 0x80000000u : 0) | x >> 1; }
        if (amount == 0) return x;
        switch (kind)
        {
            case "LSL": carry = amount <= 32 && ((x >> (int)(32 - amount)) & 1) != 0; return amount < 32 ? x << (int)amount : 0;
            case "LSR": carry = amount <= 32 && ((x >> (int)(amount - 1)) & 1) != 0; return amount < 32 ? x >> (int)amount : 0;
            case "ASR": carry = amount >= 32 ? (x & 0x80000000) != 0 : ((x >> (int)(amount - 1)) & 1) != 0; return amount < 32 ? (uint)((int)x >> (int)amount) : (x & 0x80000000) != 0 ? uint.MaxValue : 0;
            case "ROR": var result = BitOperations.RotateRight(x, (int)amount); carry = (result & 0x80000000) != 0; return result;
            default: throw new FormatException("Invalid shift.");
        }
    }
    private uint Shifted(uint value, ShiftOperand? shift, out bool carry)
    { carry = Flags.C; return shift is null ? value : Shift(shift.Kind, value, Value(shift.Amount), out carry); }
    internal (uint Address, uint Updated) Address(MemoryOperand m)
    {
        var b = Read(m.Base); if (m.Base.Index == 15) b &= ~3u;
        var offset = Shifted(Value(m.Offset), m.Shift, out _); var updated = m.Subtract ? unchecked(b - offset) : unchecked(b + offset);
        return (m.PostIndex ? b : updated, updated);
    }
    public void SignalEvent() { eventPending = true; if (Status == ExecutionStatus.Waiting && Stop == ExecutionStop.WaitForEvent) { eventPending = false; Wake(); } }
    public void SignalInterrupt() { interruptPending = true; Memory.ClearExclusive(); if (Status == ExecutionStatus.Waiting) { interruptPending = false; Wake(); } }
    private void Wake() { Status = ExecutionStatus.Paused; Stop = ExecutionStop.Step; Message = "Woken. Step or Run to continue."; }
    public EmulatorSnapshot Step()
    {
        if (Program is null) throw new InvalidOperationException("Load a program first.");
        if (Status is ExecutionStatus.Completed or ExecutionStatus.Faulted or ExecutionStatus.Waiting) return Snapshot();
        if (InstructionIndex >= Program.Instructions.Count) { Status = ExecutionStatus.Completed; Stop = ExecutionStop.Completed; Message = "Program completed."; return Snapshot(); }
        var i = Program.Instructions[InstructionIndex]; nextIndex = -1; Stop = ExecutionStop.Step; TrapImmediate = null; Message = "Paused. Step or Run to continue.";
        try
        {
            var execute = Condition(i.Operation.Condition);
            if (conditionalExecution.Count != 0) { var predicate = conditionalExecution.Dequeue(); execute &= Condition(predicate.Condition) != predicate.Invert; }
            Status = ExecutionStatus.Paused;
            if (execute) Execute(i);
            Memory.AdvanceTicks(); InstructionsExecuted++; if (nextIndex == -1) nextIndex = Program.RegisterTarget(i.Address!.Value + (uint)i.Width); InstructionIndex = nextIndex; UpdatePc();
            if (Status != ExecutionStatus.Waiting && Stop is ExecutionStop.Step or ExecutionStop.Yield && InstructionIndex >= Program.Instructions.Count) { Status = ExecutionStatus.Completed; Stop = ExecutionStop.Completed; Message = "Program completed."; }
        }
        catch (Exception e) when (e is FormatException or InvalidOperationException or OverflowException or ArgumentException)
        { Status = ExecutionStatus.Faulted; if (Stop != ExecutionStop.UndefinedInstruction) Stop = ExecutionStop.Fault; Message = new AssemblyException(i.Line, e.Message).Message; }
        return Snapshot();
    }
    private void Execute(Instruction i)
    {
        var op = i.Operation.Name; var a = i.Arguments; var flags = i.Operation.SetFlags;
        RegisterOperand R(int n) => (RegisterOperand)a[n]; uint V(int n) => Value(a[n]);
        if (op.StartsWith("IT", StringComparison.Ordinal)) { var condition = ((NamedOperand)a[0]).Name; conditionalExecution.Enqueue((condition, false)); foreach (var c in op[2..]) conditionalExecution.Enqueue((condition, c == 'E')); return; }
        if (op.StartsWith('V')) { FloatingPoint.Execute(this, i); return; }
        if (ExecuteSystem(i)) return;
        if (op is "B" or "BL" or "BX" or "BLX")
        {
            var target = Program!.RegisterTarget(V(0)); if (op is "BL" or "BLX") registers[14] = (i.Address!.Value + (uint)i.Width) | 1u; nextIndex = target; return;
        }
        if (op is "CBZ" or "CBNZ") { if ((V(0) == 0) == (op == "CBZ")) nextIndex = Program!.RegisterTarget(V(1)); return; }
        if (op is "TBB" or "TBH")
        {
            var m = (MemoryOperand)a[0]; var index = Value(m.Offset); var address = Read(m.Base) + (op == "TBH" ? index * 2 : index); var offset = op == "TBH" ? Memory.ReadHalf(address) : Memory.ReadByte(address); nextIndex = Program!.RegisterTarget(i.Address!.Value + 4 + 2u * offset); return;
        }
        if (ExecuteMemory(i)) return;
        if (ExecuteDsp(i)) return;
        if (op == "ADR") { Write(R(0), V(1)); return; }
        if (op is "CLZ" or "RBIT" or "REV" or "REV16" or "REVSH")
        {
            var x = V(1); uint result = op switch { "CLZ" => (uint)BitOperations.LeadingZeroCount(x), "REV" => System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(x), "REV16" => (x & 0x00ff00ff) << 8 | (x & 0xff00ff00) >> 8, "REVSH" => (uint)(short)((x & 255) << 8 | (x >> 8 & 255)), _ => ReverseBits(x) }; Write(R(0), result); return;
        }
        if (op is "BFC" or "BFI" or "SBFX" or "UBFX")
        {
            var l = (int)V(op == "BFC" ? 1 : 2); var width = (int)V(op == "BFC" ? 2 : 3); var mask = uint.MaxValue >> (32 - width);
            uint result = op switch { "BFC" => V(0) & ~(mask << l), "BFI" => (V(0) & ~(mask << l)) | (V(1) & mask) << l, "SBFX" => (uint)((int)(V(1) << (32 - l - width)) >> (32 - width)), _ => V(1) >> l & mask }; Write(R(0), result); return;
        }
        if (op == "MOVT") { Write(R(0), (V(0) & 65535) | V(1) << 16); return; }
        if (op is "MOV" or "MVN" or "MOVW") { var value = Shifted(V(1), a.Length == 3 ? (ShiftOperand)a[2] : null, out var carry); if (op == "MVN") value = ~value; Write(R(0), value); if (flags) { Nz(value); Flags = Flags with { C = carry }; } return; }
        if (op is "CMP" or "CMN" or "TST" or "TEQ")
        { var y = Shifted(V(1), a.Length == 3 ? (ShiftOperand)a[2] : null, out var carry); if (op == "CMP") Add(V(0), ~y, true, true); else if (op == "CMN") Add(V(0), y, false, true); else { Nz(op == "TST" ? V(0) & y : V(0) ^ y); Flags = Flags with { C = carry }; } return; }
        if (op == "RRX") { var result = Shift(op, V(1), 1, out var c); Write(R(0), result); if (flags) { Nz(result); Flags = Flags with { C = c }; } return; }
        var left = V(a.Length == 2 ? 0 : 1); var right = Shifted(V(a.Length == 2 ? 1 : 2), a.Length == 4 ? (ShiftOperand)a[3] : null, out var shiftedCarry);
        uint valueResult;
        switch (op)
        {
            case "ADD": valueResult = Add(left, right, false, flags); break;
            case "ADC": valueResult = Add(left, right, Flags.C, flags); break;
            case "SUB": valueResult = Add(left, ~right, true, flags); break;
            case "SBC": valueResult = Add(left, ~right, Flags.C, flags); break;
            case "RSB": valueResult = Add(right, ~left, true, flags); break;
            case "LSL": case "LSR": case "ASR": case "ROR": valueResult = Shift(op, left, right, out var c); if (flags) { Nz(valueResult); Flags = Flags with { C = c }; } break;
            case "AND":
            case "ORR":
            case "ORN":
            case "EOR":
            case "BIC":
                valueResult = op switch { "AND" => left & right, "ORR" => left | right, "ORN" => left | ~right, "EOR" => left ^ right, _ => left & ~right }; if (flags) { Nz(valueResult); Flags = Flags with { C = shiftedCarry }; }
                break;
            default: throw new InvalidOperationException($"Instruction {op} has no execution implementation.");
        }
        Write(R(0), valueResult);
    }
    private static uint ReverseBits(uint x) { x = (x >> 1 & 0x55555555) | (x & 0x55555555) << 1; x = (x >> 2 & 0x33333333) | (x & 0x33333333) << 2; x = (x >> 4 & 0x0f0f0f0f) | (x & 0x0f0f0f0f) << 4; return System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(x); }
    private bool ExecuteSystem(Instruction i)
    {
        var a = i.Arguments; var op = i.Operation.Name;
        switch (op)
        {
            case "NOP": case "DBG": case "DMB": case "DSB": case "ISB": return true;
            case "YIELD": Stop = ExecutionStop.Yield; return true;
            case "CLREX": Memory.ClearExclusive(); return true;
            case "SEV": SignalEvent(); return true;
            case "WFE": if (eventPending) eventPending = false; else { Status = ExecutionStatus.Waiting; Stop = ExecutionStop.WaitForEvent; Message = "Waiting for event. Signal an event to wake."; } return true;
            case "WFI": if (interruptPending) interruptPending = false; else { Status = ExecutionStatus.Waiting; Stop = ExecutionStop.WaitForInterrupt; Message = "Waiting for interrupt. Signal an interrupt to wake; no handler is dispatched."; } return true;
            case "BKPT":
            case "SVC":
                Stop = op == "BKPT" ? ExecutionStop.Breakpoint : ExecutionStop.ServiceCall; TrapImmediate = a.Length == 0 ? 0 : Value(a[0]); Memory.ClearExclusive(); Message = op == "BKPT" ? "Breakpoint reached." : $"Service call #{TrapImmediate}. Step or Run to continue without dispatching a handler."; return true;
            case "UDF": Stop = ExecutionStop.UndefinedInstruction; TrapImmediate = a.Length == 0 ? 0 : Value(a[0]); throw new InvalidOperationException("Undefined instruction trap.");
            case "CPSID":
            case "CPSIE":
                if ((control & 1) == 0) { var mask = ((NamedOperand)a[0]).Name; if (mask.Contains('I')) primask = op == "CPSID" ? 1u : 0; if (mask.Contains('F')) faultmask = op == "CPSID" ? 1u : 0; }
                return true;
            case "MRS": Write((RegisterOperand)a[0], ReadSystem(((NamedOperand)a[1]).Name)); return true;
            case "MSR": WriteSystem(((NamedOperand)a[0]).Name, Value(a[1])); return true;
            default: return false;
        }
    }
    private uint ReadSystem(string name) => name switch
    {
        "APSR" or "APSR_NZCVQG" or "APSR_NZCVQ" or "APSR_G" or "IAPSR" => Flags.Packed,
        "XPSR" or "EAPSR" => Flags.Packed,
        "IPSR" or "EPSR" or "IEPSR" => 0,
        "MSP" => (control & 1) == 0 ? msp : 0,
        "PSP" => (control & 1) == 0 ? psp : 0,
        "PRIMASK" => (control & 1) == 0 ? primask : 0,
        "BASEPRI" or "BASEPRI_MAX" => (control & 1) == 0 ? basepri : 0,
        "FAULTMASK" => (control & 1) == 0 ? faultmask : 0,
        "CONTROL" => control,
        _ => throw new FormatException("Unsupported special register.")
    };
    internal void SetApsr(uint v, bool nzcvq = true, bool ge = false)
    { if (nzcvq) Flags = Flags with { N = (v & 0x80000000) != 0, Z = (v & 0x40000000) != 0, C = (v & 0x20000000) != 0, V = (v & 0x10000000) != 0, Q = (v & 0x08000000) != 0 }; if (ge) Flags = Flags with { Ge = (byte)(v >> 16 & 15) }; }
    private void WriteSystem(string name, uint v)
    {
        if (name is "APSR" or "APSR_NZCVQG" or "APSR_NZCVQ" or "APSR_G") { SetApsr(v, name != "APSR_G", name != "APSR_NZCVQ"); return; }
        if ((control & 1) != 0) return;
        switch (name)
        {
            case "MSP": msp = v & ~3u; break;
            case "PSP": psp = v & ~3u; break;
            case "PRIMASK": primask = v & 1; break;
            case "FAULTMASK": faultmask = v & 1; break;
            case "BASEPRI": basepri = v & 255; break;
            case "BASEPRI_MAX": if ((v & 255) != 0 && (basepri == 0 || (v & 255) < basepri)) basepri = v & 255; break;
            case "CONTROL": control = v & 7; break;
            default: throw new FormatException("Special register is read-only.");
        }
    }
    private bool ExecuteMemory(Instruction i)
    {
        var op = i.Operation.Name; var a = i.Arguments;
        if (op is "PLD" or "PLI") return true;
        if (op is "PUSH" or "POP" or "STM" or "STMDB" or "LDM" or "LDMDB")
        {
            var stack = op is "PUSH" or "POP"; var load = op is "POP" or "LDM" or "LDMDB"; var dec = op is "PUSH" or "STMDB" or "LDMDB";
            var b = stack ? new RegisterOperand(13, Writeback: true) : (RegisterOperand)a[0]; var list = ((RegisterListOperand)a[stack ? 0 : 1]).Registers;
            var before = Read(b); var bytes = (uint)(list.Length * 4); var start = dec ? unchecked(before - bytes) : before;
            var locations = Enumerable.Range(0, list.Length).Select(n => start + (uint)n * 4).ToArray(); foreach (var location in locations) Memory.ValidateAccess(location, 4, !load, true);
            var values = load ? locations.Select(Memory.ReadWord).ToArray() : list.Select(Read).ToArray();
            if (load && list.Any(r => r.Index == 15)) Program!.RegisterTarget(values[Array.FindIndex(list, r => r.Index == 15)]);
            if (load) { for (var j = 0; j < list.Length; j++) Write(list[j], values[j]); }
            else Memory.WriteWords(locations.Select((location, j) => (location, values[j])).ToArray());
            if (b.Writeback) Write(b, dec ? before - bytes : before + bytes); return true;
        }
        if (!(op.StartsWith("LDR", StringComparison.Ordinal) || op.StartsWith("STR", StringComparison.Ordinal))) return false;
        var loadMemory = op.StartsWith("LDR", StringComparison.Ordinal); var exclusive = op.Contains("EX", StringComparison.Ordinal); var pair = op is "LDRD" or "STRD";
        if (op == "LDR" && a[1] is ImmediateOperand literal) { Write((RegisterOperand)a[0], i.Operands[1].StartsWith('=') ? literal.Value : Memory.ReadWord(literal.Value)); return true; }
        var m = (MemoryOperand)a[^1]; var (address, updated) = Address(m); var width = op.EndsWith('B') || op.EndsWith("BT", StringComparison.Ordinal) || op is "LDRSB" or "LDRSBT" ? 1 : op.EndsWith('H') || op.EndsWith("HT", StringComparison.Ordinal) ? 2 : 4;
        Memory.ValidateAccess(address, pair ? 8 : width, !loadMemory, pair || exclusive);
        if (exclusive && (address < MemoryBus.RamBase || address - MemoryBus.RamBase >= MemoryBus.RamSize)) throw new InvalidOperationException("Exclusive accesses require SRAM.");
        if (loadMemory)
        {
            uint v = width == 1 ? Memory.ReadByte(address) : width == 2 ? Memory.ReadHalf(address) : Memory.ReadWord(address);
            if (op.StartsWith("LDRS", StringComparison.Ordinal)) v = width == 1 ? (uint)(sbyte)v : (uint)(short)v;
            if (a[0] is RegisterOperand { Index: 15 }) Program!.RegisterTarget(v);
            var second = pair ? Memory.ReadWord(address + 4) : 0;
            Write((RegisterOperand)a[0], v); if (pair) Write((RegisterOperand)a[1], second); if (exclusive) Memory.ReserveExclusive(address, width);
        }
        else
        {
            var value = Value(a[exclusive ? 1 : 0]); var second = pair ? Value(a[1]) : 0;
            var success = !exclusive || Memory.ConsumeExclusive(address, width);
            if (success) { if (pair) Memory.WriteWords(new[] { (address, value), (address + 4, second) }); else if (width == 1) Memory.WriteByte(address, (byte)value); else if (width == 2) Memory.WriteHalf(address, (ushort)value); else Memory.WriteWord(address, value); }
            if (exclusive) Write((RegisterOperand)a[0], success ? 0u : 1u);
        }
        if (m.Writeback) Write(m.Base, updated); return true;
    }
}
