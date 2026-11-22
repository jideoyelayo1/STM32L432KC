using System.Numerics;

namespace Stm32Emulator.Core;

public sealed partial class Emulator
{
    private long Saturate(long value, int bits, bool signed, bool setQ = true)
    {
        var min = signed ? -(1L << (bits - 1)) : 0;
        var max = signed ? (1L << (bits - 1)) - 1 : bits == 32 ? uint.MaxValue : (1L << bits) - 1;
        var result = Math.Clamp(value, min, max);
        if (setQ && value != result) Flags = Flags with { Q = true };
        return result;
    }
    private static long Lane(uint value, int lane, int bits, bool signed)
    { var raw = value >> (lane * bits) & (bits == 16 ? 65535u : 255u); return signed ? bits == 16 ? (short)raw : (sbyte)raw : raw; }
    private bool ExecuteDsp(Instruction i)
    {
        var op = i.Operation.Name; var a = i.Arguments;
        uint V(int n) => Value(a[n]); void Put(uint v) => Write((RegisterOperand)a[0], v);
        if (op is "SSAT" or "USAT" or "SSAT16" or "USAT16")
        {
            var x = Shifted(V(2), a.Length == 4 ? (ShiftOperand)a[3] : null, out _); var signed = op.StartsWith('S'); var bits = (int)V(1);
            if (op.EndsWith("16", StringComparison.Ordinal))
            { uint result = 0; for (var n = 0; n < 2; n++) result |= ((uint)Saturate(Lane(x, n, 16, true), bits, signed) & 65535) << (n * 16); Put(result); }
            else Put((uint)Saturate((int)x, bits, signed));
            return true;
        }
        if (op.StartsWith("SXT", StringComparison.Ordinal) || op.StartsWith("UXT", StringComparison.Ordinal))
        {
            var add = op.Contains('A'); var x = V(add ? 2 : 1); if (a[^1] is ShiftOperand s) x = Shifted(x, s, out _);
            var signed = op.StartsWith('S'); var half = op.EndsWith('H'); var dual = op.EndsWith("16", StringComparison.Ordinal);
            if (dual)
            {
                var low = signed ? (uint)(sbyte)x : x & 255; var high = signed ? (uint)(sbyte)(x >> 16) : x >> 16 & 255;
                if (add) { low += V(1) & 65535; high += V(1) >> 16; }
                Put(low & 65535 | high << 16);
            }
            else { var extended = signed ? half ? (uint)(short)x : (uint)(sbyte)x : half ? x & 65535 : x & 255; Put(unchecked(extended + (add ? V(1) : 0))); }
            return true;
        }
        if (op is "PKHBT" or "PKHTB")
        {
            var y = Shifted(V(2), a.Length == 4 ? (ShiftOperand)a[3] : null, out _);
            Put(op == "PKHBT" ? V(1) & 65535 | y & 0xffff0000 : V(1) & 0xffff0000 | y & 65535); return true;
        }
        if (op == "SEL")
        { uint result = 0; for (var n = 0; n < 4; n++) result |= (((Flags.Ge >> n & 1) != 0 ? V(1) : V(2)) & (255u << (n * 8))); Put(result); return true; }
        if (op is "QADD" or "QSUB" or "QDADD" or "QDSUB")
        {
            long y = (int)V(2); if (op.StartsWith("QD", StringComparison.Ordinal)) y = Saturate(y * 2, 32, true);
            Put((uint)Saturate((long)(int)V(1) + (op.EndsWith("SUB", StringComparison.Ordinal) ? -y : y), 32, true)); return true;
        }
        if (InstructionSet.All.First(x => x.Name == op).Family == "DSP")
        {
            var prefix = new[] { "UQ", "UH", "SH", "S", "U", "Q" }.First(p => op.StartsWith(p, StringComparison.Ordinal));
            var kind = op[prefix.Length..]; var width = kind.EndsWith('8') ? 8 : 16; var count = 32 / width;
            var signed = prefix is "S" or "SH" or "Q"; var saturating = prefix is "Q" or "UQ"; var halving = prefix is "SH" or "UH";
            uint result = 0; byte ge = 0; var mask = (1u << width) - 1;
            for (var n = 0; n < count; n++)
            {
                var exchange = kind is "ASX" or "SAX"; var yLane = exchange ? 1 - n : n;
                var x = Lane(V(1), n, width, signed); var y = Lane(V(2), yLane, width, signed);
                var subtract = kind.StartsWith("SUB", StringComparison.Ordinal) || kind == "ASX" && n == 0 || kind == "SAX" && n == 1;
                var wide = subtract ? x - y : x + y;
                if (prefix is "S" or "U")
                { var set = signed ? wide >= 0 : subtract ? wide >= 0 : wide > mask; if (set) ge |= (byte)((width == 16 ? 3 : 1) << (n * (width / 8))); }
                var lane = saturating ? Saturate(wide, width, signed, false) : halving ? wide >> 1 : wide;
                result |= ((uint)lane & mask) << (n * width);
            }
            if (prefix is "S" or "U") Flags = Flags with { Ge = ge }; Put(result); return true;
        }
        if (InstructionSet.All.First(x => x.Name == op).Family != "Multiply") return false;
        var longResult = op is "UMULL" or "SMULL" or "UMLAL" or "SMLAL" or "UMAAL" || op.StartsWith("SMLAL", StringComparison.Ordinal) || op.StartsWith("SMLSL", StringComparison.Ordinal);
        if (longResult)
        {
            var x = V(2); var y = V(3); long product;
            if (op is "UMULL" or "UMLAL" or "UMAAL") product = unchecked((long)((ulong)x * y));
            else if (op is "SMULL" or "SMLAL") product = (long)(int)x * (int)y;
            else if (op.StartsWith("SMLAL", StringComparison.Ordinal) && op.Length == 7) product = Lane(x, op[^2] == 'T' ? 1 : 0, 16, true) * Lane(y, op[^1] == 'T' ? 1 : 0, 16, true);
            else
            {
                if (op.EndsWith('X')) y = BitOperations.RotateRight(y, 16);
                var p0 = Lane(x, 0, 16, true) * Lane(y, 0, 16, true); var p1 = Lane(x, 1, 16, true) * Lane(y, 1, 16, true);
                product = op.StartsWith("SMLSL", StringComparison.Ordinal) ? p0 - p1 : p0 + p1;
            }
            ulong result = unchecked((ulong)product);
            if (op == "UMAAL") result = unchecked(result + V(0) + V(1));
            else if (op != "SMULL" && op != "UMULL") result = unchecked(result + (V(0) | (ulong)V(1) << 32));
            Write((RegisterOperand)a[0], (uint)result); Write((RegisterOperand)a[1], (uint)(result >> 32)); return true;
        }
        var left = V(a.Length == 2 ? 0 : 1); var right = V(a.Length == 2 ? 1 : 2); long valueResult;
        if (op is "SDIV" or "UDIV")
        { Put(right == 0 ? 0 : op == "UDIV" ? left / right : (uint)((long)(int)left / (int)right)); return true; }
        if (op is "MUL" or "MLA" or "MLS")
        { var product = unchecked(left * right); Put(op == "MLA" ? unchecked(product + V(3)) : op == "MLS" ? unchecked(V(3) - product) : product); if (i.Operation.SetFlags) Nz(unchecked(left * right)); return true; }
        if (op is "USAD8" or "USADA8")
        { uint result = 0; for (var n = 0; n < 4; n++) result += (uint)Math.Abs(Lane(left, n, 8, false) - Lane(right, n, 8, false)); Put(unchecked(result + (op == "USADA8" ? V(3) : 0))); return true; }
        if (op.StartsWith("SMM", StringComparison.Ordinal))
        {
            var product = (long)(int)left * (int)right; var value = op.StartsWith("SMMLA", StringComparison.Ordinal) ? unchecked(((long)(int)V(3) << 32) + product) : op.StartsWith("SMMLS", StringComparison.Ordinal) ? unchecked(((long)(int)V(3) << 32) - product) : product;
            if (op.EndsWith('R')) value = unchecked(value + 0x80000000L); Put((uint)(value >> 32)); return true;
        }
        if (op.StartsWith("SMULW", StringComparison.Ordinal) || op.StartsWith("SMLAW", StringComparison.Ordinal))
        { valueResult = (long)(int)left * Lane(right, op.EndsWith('T') ? 1 : 0, 16, true) >> 16; if (op.StartsWith("SMLA", StringComparison.Ordinal)) valueResult += (int)V(3); }
        else if (op.StartsWith("SMUL", StringComparison.Ordinal) || op.StartsWith("SMLA", StringComparison.Ordinal) && op.Length == 6)
        { valueResult = Lane(left, op[^2] == 'T' ? 1 : 0, 16, true) * Lane(right, op[^1] == 'T' ? 1 : 0, 16, true); if (op.StartsWith("SMLA", StringComparison.Ordinal)) valueResult += (int)V(3); }
        else
        {
            if (op.EndsWith('X')) right = BitOperations.RotateRight(right, 16);
            var p0 = Lane(left, 0, 16, true) * Lane(right, 0, 16, true); var p1 = Lane(left, 1, 16, true) * Lane(right, 1, 16, true);
            valueResult = op.StartsWith("SMUSD", StringComparison.Ordinal) || op.StartsWith("SMLSD", StringComparison.Ordinal) ? p0 - p1 : p0 + p1;
            if (a.Length == 4) valueResult += (int)V(3);
        }
        // SMUSD cannot overflow a signed 32-bit result; the accumulating/summing forms can.
        if (valueResult is > int.MaxValue or < int.MinValue) Flags = Flags with { Q = true };
        Put((uint)valueResult); return true;
    }
}
