namespace Stm32Emulator.Core;

internal static class FloatingPoint
{
    private static readonly HashSet<string> arithmetic = new("VADD VSUB VMUL VDIV VMLA VMLS VNMLA VNMLS VNMUL VFMA VFMS VFNMA VFNMS".Split(' '));
    internal static void Validate(Instruction i)
    {
        var a = i.Arguments; var op = i.Operation.Name; var types = i.Operation.Types;
        void Count(params int[] counts) { if (!counts.Contains(a.Length)) throw new FormatException($"{op} has invalid operand count."); }
        RegisterOperand Reg(int n, params char[] banks)
        { if (a[n] is not RegisterOperand r || !banks.Contains(r.Bank) || r.Writeback) throw new FormatException("Invalid floating-point/core register operand."); return r; }
        if (op is "VMRS" or "VMSR")
        {
            Count(2); var s = op == "VMRS" ? 1 : 0;
            if (a[s] is not NamedOperand { Name: "FPSCR" }) throw new FormatException("Cortex-M4 status transfers use FPSCR.");
            if (!(op == "VMRS" && a[0] is NamedOperand { Name: "APSR_NZCV" })) { if (Reg(1 - s, 'R').Index >= 13) throw new FormatException("Status transfers require R0–R12 or VMRS APSR_NZCV."); } return;
        }
        if (op is "VLDR" or "VSTR")
        {
            Count(2); var transferRegister = Reg(0, 'S', 'D');
            if (types.Length > 1 || types.Length == 1 && !(transferRegister.Bank == 'D' ? types[0] is "F64" or "64" : types[0] is "F32" or "32")) throw new FormatException("Transfer type must match the S/D register width.");
            if (op == "VLDR" && a[1] is ImmediateOperand) return;
            if (a[1] is not MemoryOperand m || m.Base.Bank != 'R' || m.Writeback || m.PostIndex || m.Shift != null || m.Offset is not ImmediateOperand { Value: <= 1020 } v || v.Value % 4 != 0) throw new FormatException("VFP memory uses a word-aligned immediate offset without writeback."); return;
        }
        if (op is "VLDM" or "VSTM" or "VLDMDB" or "VSTMDB" or "VPOP" or "VPUSH")
        {
            var stack = op is "VPOP" or "VPUSH"; Count(stack ? 1 : 2);
            if (!stack && (a[0] is not RegisterOperand { Bank: 'R', Index: < 15 })) throw new FormatException("Invalid VFP transfer base.");
            if (a[^1] is not RegisterListOperand list || list.Registers[0].Bank == 'R' || list.Registers.Select((r, n) => r.Index == list.Registers[0].Index + n).Any(ok => !ok)) throw new FormatException("VFP register lists must be consecutive S or D registers."); return;
        }
        if (op == "VMOV")
        {
            Count(2, 3, 4);
            if (a.Length == 2)
            {
                if (a[0] is LaneOperand lane && a[1] is RegisterOperand { Bank: 'R', Index: < 13 } || a[1] is LaneOperand lane2 && a[0] is RegisterOperand { Bank: 'R', Index: < 13 }) return;
                var dest = Reg(0, 'S', 'R');
                if (a[1] is ImmediateOperand imm)
                { var e = imm.Value >> 23 & 255; if (dest.Bank != 'S' || (imm.Value & 0x7ffff) != 0 || e < 124 || e > 131) throw new FormatException("VMOV immediate must be an encodable VFP constant; use a data literal for other values."); }
                else { var src = Reg(1, 'S', 'R'); if (dest.Bank == 'R' && src.Bank == 'R' || dest.Bank == 'R' && dest.Index >= 13 || src.Bank == 'R' && src.Index >= 13) throw new FormatException("Invalid VMOV transfer."); }
            }
            else if (a.Length == 3)
            { if (a[0] is RegisterOperand { Bank: 'D' }) { Reg(0, 'D'); Reg(1, 'R'); Reg(2, 'R'); } else { Reg(0, 'R'); Reg(1, 'R'); Reg(2, 'D'); } }
            else
            { var bank = Reg(0, 'R', 'S').Bank; var r1 = Reg(1, bank); var bank2 = bank == 'R' ? 'S' : 'R'; var r2 = Reg(2, bank2); var r3 = Reg(3, bank2); if (bank == 'S' ? r1.Index != ((RegisterOperand)a[0]).Index + 1 : r3.Index != r2.Index + 1) throw new FormatException("VMOV requires a consecutive S register pair."); }
            return;
        }
        if (op is "VCVT" or "VCVTR" or "VCVTB" or "VCVTT")
        {
            Count(2, 3); Reg(0, 'S'); Reg(1, 'S');
            if (types.Length != 2) throw new FormatException("Conversion requires destination/source type suffixes.");
            if (op is "VCVTB" or "VCVTT") { if (a.Length != 2 || !(types.SequenceEqual(new[] { "F32", "F16" }) || types.SequenceEqual(new[] { "F16", "F32" }))) throw new FormatException("Half conversions require F32.F16 or F16.F32."); }
            else
            {
                var integerType = types[0] == "F32" ? types[1] : types[0];
                if (!types.Contains("F32") || integerType is not ("S32" or "U32" or "S16" or "U16") || op == "VCVTR" && (types[0] == "F32" || a.Length != 2 || integerType.EndsWith("16", StringComparison.Ordinal))) throw new FormatException("Invalid conversion types.");
                if (a.Length == 3) { var width = integerType.EndsWith("16", StringComparison.Ordinal) ? 16 : 32; if (a[2] is not ImmediateOperand v || v.Value == 0 || v.Value > width || ((RegisterOperand)a[0]).Index != ((RegisterOperand)a[1]).Index) throw new FormatException("Fixed-point conversion requires the same S register and a valid fractional width."); }
                else if (integerType.EndsWith("16", StringComparison.Ordinal)) throw new FormatException("16-bit fixed-point conversion requires fractional bits.");
            }
            return;
        }
        if (types.Length != 1 || types[0] != "F32") throw new FormatException("Floating-point arithmetic requires .F32.");
        if (op is "VCMP" or "VCMPE") { Count(2); Reg(0, 'S'); if (a[1] is not ImmediateOperand { Value: 0 }) Reg(1, 'S'); return; }
        if (op is "VABS" or "VNEG" or "VSQRT") { Count(2); Reg(0, 'S'); Reg(1, 'S'); return; }
        if (arithmetic.Contains(op)) { Count(2, 3); foreach (var arg in a) if (arg is not RegisterOperand { Bank: 'S', Writeback: false }) throw new FormatException("Arithmetic requires S registers."); return; }
        throw new FormatException($"No floating-point validation for {op}.");
    }
    internal static void Execute(Emulator cpu, Instruction i)
    {
        var a = i.Arguments; var op = i.Operation.Name; var types = i.Operation.Types;
        RegisterOperand R(int n) => (RegisterOperand)a[n]; uint V(int n) => cpu.Value(a[n]); void Put(uint v) => cpu.Write(R(0), v);
        if (op == "VMSR") { cpu.fpscr = V(1) & 0xf7c0009f; return; }
        if (op == "VMRS") { if (a[0] is NamedOperand) cpu.SetApsr(cpu.fpscr & 0xf0000000 | cpu.Flags.Packed & 0x08000000); else Put(cpu.fpscr); return; }
        if (op == "VMOV")
        {
            if (a.Length == 2)
            {
                if (a[0] is LaneOperand dest) cpu.floating[dest.Register.Index * 2 + dest.Lane] = V(1);
                else if (a[1] is LaneOperand src) Put(cpu.floating[src.Register.Index * 2 + src.Lane]);
                else Put(V(1));
            }
            else if (a.Length == 3)
            { if (R(0).Bank == 'D') cpu.WriteDouble(R(0), V(1) | (ulong)V(2) << 32); else { var v = cpu.ReadDouble(R(2)); Put((uint)v); cpu.Write(R(1), (uint)(v >> 32)); } }
            else { var low = V(2); var high = V(3); Put(low); cpu.Write(R(1), high); }
            return;
        }
        if (op is "VLDR" or "VSTR")
        {
            var address = a[1] is ImmediateOperand literal ? literal.Value : cpu.Address((MemoryOperand)a[1]).Address; var width = R(0).Bank == 'D' ? 8 : 4; cpu.Memory.ValidateAccess(address, width, op == "VSTR", true);
            if (op == "VLDR") { var low = cpu.Memory.ReadWord(address); if (width == 8) cpu.WriteDouble(R(0), low | (ulong)cpu.Memory.ReadWord(address + 4) << 32); else Put(low); }
            else { var value = width == 8 ? cpu.ReadDouble(R(0)) : V(0); if (width == 8) cpu.Memory.WriteWords(new[] { (address, (uint)value), (address + 4, (uint)(value >> 32)) }); else cpu.Memory.WriteWord(address, (uint)value); } return;
        }
        if (op is "VLDM" or "VSTM" or "VLDMDB" or "VSTMDB" or "VPOP" or "VPUSH")
        {
            var stack = op is "VPOP" or "VPUSH"; var load = op is "VLDM" or "VLDMDB" or "VPOP"; var dec = op is "VLDMDB" or "VSTMDB" or "VPUSH";
            var b = stack ? new RegisterOperand(13, Writeback: true) : R(0); var list = ((RegisterListOperand)a[^1]).Registers; var stride = list[0].Bank == 'D' ? 8 : 4;
            var before = cpu.Read(b); var size = (uint)(list.Length * stride); var start = dec ? before - size : before;
            for (var n = 0; n < list.Length; n++) cpu.Memory.ValidateAccess(start + (uint)(n * stride), stride, !load, true);
            var values = list.Select((r, n) => load ? cpu.Memory.ReadWord(start + (uint)(n * stride)) | (stride == 8 ? (ulong)cpu.Memory.ReadWord(start + (uint)(n * stride) + 4) << 32 : 0) : stride == 8 ? cpu.ReadDouble(r) : cpu.Read(r)).ToArray();
            if (load)
                for (var n = 0; n < list.Length; n++) { if (stride == 8) cpu.WriteDouble(list[n], values[n]); else cpu.Write(list[n], (uint)values[n]); }
            else
            {
                var writes = new List<(uint, uint)>();
                for (var n = 0; n < list.Length; n++) { writes.Add((start + (uint)(n * stride), (uint)values[n])); if (stride == 8) writes.Add((start + (uint)(n * stride) + 4, (uint)(values[n] >> 32))); }
                cpu.Memory.WriteWords(writes);
            }
            if (b.Writeback) cpu.Write(b, dec ? before - size : before + size); return;
        }
        if (op is "VCVT" or "VCVTR")
        {
            var toFloat = types[0] == "F32"; var integer = types[toFloat ? 1 : 0]; var bits = integer.EndsWith("16", StringComparison.Ordinal) ? 16 : 32; var fraction = a.Length == 3 ? (int)V(2) : 0;
            Put(toFloat ? SoftFloat32.FromInteger(V(1), integer[0] == 'S', bits, fraction, ref cpu.fpscr) : SoftFloat32.ToInteger(V(1), integer[0] == 'S', bits, fraction, op == "VCVTR", ref cpu.fpscr)); return;
        }
        if (op is "VCVTB" or "VCVTT")
        {
            var shift = op == "VCVTT" ? 16 : 0;
            if (types[0] == "F32") Put(SoftFloat32.FromHalf((ushort)(V(1) >> shift), ref cpu.fpscr));
            else { var half = SoftFloat32.ToHalf(V(1), ref cpu.fpscr); Put(V(0) & ~(65535u << shift) | (uint)half << shift); } return;
        }
        if (op is "VCMP" or "VCMPE") { SoftFloat32.Compare(V(0), V(1), op == "VCMPE", ref cpu.fpscr); return; }
        if (op == "VABS") { Put(V(1) & 0x7fffffff); return; }
        if (op == "VNEG") { Put(V(1) ^ 0x80000000); return; }
        if (op == "VSQRT") { Put(SoftFloat32.Sqrt(V(1), ref cpu.fpscr)); return; }
        var x = V(a.Length == 2 ? 0 : 1); var y = V(a.Length == 2 ? 1 : 2); uint result;
        if (op is "VFMA" or "VFMS" or "VFNMA" or "VFNMS") result = SoftFloat32.Fused(V(0), x, y, op is "VFMS" or "VFNMA", op is "VFNMA" or "VFNMS", ref cpu.fpscr);
        else if (op is "VMLA" or "VMLS" or "VNMLA" or "VNMLS")
        { var product = SoftFloat32.Multiply(x, y, ref cpu.fpscr); result = SoftFloat32.Add(V(0) ^ (op is "VNMLA" or "VNMLS" ? 0x80000000u : 0), product ^ (op is "VMLS" or "VNMLA" ? 0x80000000u : 0), ref cpu.fpscr); }
        else result = op switch { "VADD" => SoftFloat32.Add(x, y, ref cpu.fpscr), "VSUB" => SoftFloat32.Subtract(x, y, ref cpu.fpscr), "VMUL" or "VNMUL" => SoftFloat32.Multiply(x, y, ref cpu.fpscr) ^ (op == "VNMUL" ? 0x80000000u : 0), "VDIV" => SoftFloat32.Divide(x, y, ref cpu.fpscr), _ => throw new InvalidOperationException($"No floating-point execution for {op}.") };
        Put(result);
    }
}
