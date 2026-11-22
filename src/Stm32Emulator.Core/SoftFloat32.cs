using System.Numerics;

namespace Stm32Emulator.Core;

/// <summary>Exact integer/rational arithmetic. The host FPU is never used for instruction results.</summary>
internal static class SoftFloat32
{
    private const uint Ioc = 1, Dzc = 2, Ofc = 4, Ufc = 8, Ixc = 16, Idc = 128;
    private const uint Quiet = 0x7fc00000;
    private readonly record struct Finite(bool Sign, BigInteger Magnitude, int Exponent);
    private static bool Nan(uint x) => (x & 0x7fffffff) > 0x7f800000;
    private static bool Inf(uint x) => (x & 0x7fffffff) == 0x7f800000;
    private static bool Zero(uint x) => (x & 0x7fffffff) == 0;
    private static uint Sign(bool sign) => sign ? 0x80000000u : 0;
    internal static uint Input(uint x, ref uint status)
    { if ((x & 0x7f800000) == 0 && (x & 0x007fffff) != 0 && (status & 1u << 24) != 0) { status |= Idc; return x & 0x80000000; } return x; }
    private static Finite Unpack(uint x)
    { var e = (int)(x >> 23 & 255); return new((x >> 31) != 0, e == 0 ? x & 0x7fffff : (x & 0x7fffff) | 0x800000, e == 0 ? -149 : e - 150); }
    private static uint ProcessNaNs(ref uint status, params uint[] values)
    {
        var signaling = values.FirstOrDefault(v => Nan(v) && (v & 0x400000) == 0);
        if (signaling != 0) status |= Ioc;
        if ((status & 1u << 25) != 0) return Quiet;
        return (signaling != 0 ? signaling : values.First(Nan)) | 0x400000;
    }
    private static uint Invalid(ref uint status) { status |= Ioc; return Quiet; }
    private static int Log2(BigInteger n, BigInteger d)
    {
        var e = (int)(n.GetBitLength() - d.GetBitLength());
        if (e >= 0 ? n < (d << e) : (n << -e) < d) e--;
        return e;
    }
    private static BigInteger RoundInteger(BigInteger n, BigInteger d, bool sign, int mode, out bool inexact)
    {
        var q = BigInteger.DivRem(n, d, out var rem); inexact = !rem.IsZero;
        if (!inexact) return q;
        var increment = mode switch { 0 => rem * 2 > d || rem * 2 == d && !q.IsEven, 1 => !sign, 2 => sign, _ => false };
        return increment ? q + 1 : q;
    }
    private static uint Overflow(bool sign, int mode, ref uint status)
    { status |= Ofc | Ixc; return Sign(sign) | (mode == 0 || mode == 1 && !sign || mode == 2 && sign ? 0x7f800000u : 0x7f7fffffu); }
    private static uint Round(bool sign, BigInteger n, BigInteger d, int exponent, ref uint status)
    {
        if (n.IsZero) return Sign(sign);
        var e = Log2(n, d) + exponent; var mode = (int)(status >> 22 & 3);
        if (e > 127) return Overflow(sign, mode, ref status);
        if (e < -126 && (status & 1u << 24) != 0) { status |= Ufc; return Sign(sign); }
        var step = Math.Max(e - 23, -149); var shift = exponent - step;
        if (shift >= 0) n <<= shift; else d <<= -shift;
        var q = RoundInteger(n, d, sign, mode, out var inexact);
        if (inexact) { status |= Ixc; if (e < -126) status |= Ufc; }
        if (q >= (BigInteger.One << 24)) { q >>= 1; step++; }
        var finalExponent = step + 23;
        if (finalExponent > 127) return Overflow(sign, mode, ref status);
        var bits = (uint)q;
        return Sign(sign) | (bits < 0x800000 ? bits : (uint)(finalExponent + 127) << 23 | bits & 0x7fffff);
    }
    internal static uint Add(uint x, uint y, ref uint status)
    {
        x = Input(x, ref status); y = Input(y, ref status);
        if (Nan(x) || Nan(y)) return ProcessNaNs(ref status, x, y);
        if (Inf(x) || Inf(y)) return Inf(x) && Inf(y) && (x ^ y) >> 31 != 0 ? Invalid(ref status) : Inf(x) ? x : y;
        var a = Unpack(x); var b = Unpack(y); var e = Math.Min(a.Exponent, b.Exponent);
        var sum = (a.Sign ? -a.Magnitude : a.Magnitude) * (BigInteger.One << (a.Exponent - e)) + (b.Sign ? -b.Magnitude : b.Magnitude) * (BigInteger.One << (b.Exponent - e));
        var negative = sum.Sign < 0 || sum.IsZero && (Zero(x) && Zero(y) && a.Sign == b.Sign ? a.Sign : (status >> 22 & 3) == 2);
        return Round(negative, BigInteger.Abs(sum), BigInteger.One, e, ref status);
    }
    internal static uint Subtract(uint x, uint y, ref uint status)
    { if (Nan(x) || Nan(y)) { x = Input(x, ref status); y = Input(y, ref status); return ProcessNaNs(ref status, x, y); } return Add(x, y ^ 0x80000000, ref status); }
    internal static uint Multiply(uint x, uint y, ref uint status)
    {
        x = Input(x, ref status); y = Input(y, ref status);
        if (Nan(x) || Nan(y)) return ProcessNaNs(ref status, x, y);
        if (Inf(x) && Zero(y) || Inf(y) && Zero(x)) return Invalid(ref status);
        var sign = ((x ^ y) >> 31) != 0;
        if (Inf(x) || Inf(y)) return Sign(sign) | 0x7f800000;
        var a = Unpack(x); var b = Unpack(y); return Round(sign, a.Magnitude * b.Magnitude, BigInteger.One, a.Exponent + b.Exponent, ref status);
    }
    internal static uint Divide(uint x, uint y, ref uint status)
    {
        x = Input(x, ref status); y = Input(y, ref status);
        if (Nan(x) || Nan(y)) return ProcessNaNs(ref status, x, y);
        if (Inf(x) && Inf(y) || Zero(x) && Zero(y)) return Invalid(ref status);
        var sign = ((x ^ y) >> 31) != 0;
        if (Inf(x)) return Sign(sign) | 0x7f800000;
        if (Inf(y)) return Sign(sign);
        if (Zero(y)) { status |= Dzc; return Sign(sign) | 0x7f800000; }
        var a = Unpack(x); var b = Unpack(y); return Round(sign, a.Magnitude, b.Magnitude, a.Exponent - b.Exponent, ref status);
    }
    internal static uint Fused(uint addend, uint x, uint y, bool negateProduct, bool negateAddend, ref uint status)
    {
        if (negateProduct) x ^= 0x80000000; if (negateAddend) addend ^= 0x80000000;
        addend = Input(addend, ref status); x = Input(x, ref status); y = Input(y, ref status);
        var badProduct = Inf(x) && Zero(y) || Inf(y) && Zero(x);
        if (Nan(addend) || Nan(x) || Nan(y)) { if (badProduct) status |= Ioc; return ProcessNaNs(ref status, addend, x, y); }
        if (badProduct) return Invalid(ref status);
        var productSign = (x ^ y) >> 31 != 0; var addSign = addend >> 31 != 0;
        if (Inf(x) || Inf(y)) return Inf(addend) && productSign != addSign ? Invalid(ref status) : Sign(productSign) | 0x7f800000;
        if (Inf(addend)) return Sign(addSign) | 0x7f800000;
        var a = Unpack(x); var b = Unpack(y); var c = Unpack(addend); var pe = a.Exponent + b.Exponent; var e = Math.Min(pe, c.Exponent);
        var product = a.Magnitude * b.Magnitude << (pe - e); var acc = c.Magnitude << (c.Exponent - e);
        var sum = (productSign ? -product : product) + (addSign ? -acc : acc);
        var sign = sum.Sign < 0 || sum.IsZero && (product.IsZero && acc.IsZero && productSign == addSign ? productSign : (status >> 22 & 3) == 2);
        return Round(sign, BigInteger.Abs(sum), BigInteger.One, e, ref status);
    }
    private static BigInteger SqrtInteger(BigInteger n)
    {
        if (n <= 1) return n;
        var x = BigInteger.One << (int)((n.GetBitLength() + 1) / 2);
        while (true) { var next = (x + n / x) >> 1; if (next >= x) return x; x = next; }
    }
    internal static uint Sqrt(uint x, ref uint status)
    {
        x = Input(x, ref status); if (Nan(x)) return ProcessNaNs(ref status, x); if (Zero(x)) return x;
        if ((x >> 31) != 0) return Invalid(ref status); if (Inf(x)) return x;
        var a = Unpack(x); var e = (int)Math.Floor((Log2(a.Magnitude, BigInteger.One) + a.Exponent) / 2.0); var step = e - 23;
        var shift = a.Exponent - 2 * step; var n = a.Magnitude; var d = BigInteger.One; if (shift >= 0) n <<= shift; else d <<= -shift;
        var q = SqrtInteger(n / d); var exact = q * q * d == n; var mode = (int)(status >> 22 & 3);
        if (!exact)
        {
            status |= Ixc; var comparison = (n * 4).CompareTo((q * 2 + 1) * (q * 2 + 1) * d);
            if (mode == 1 || mode == 0 && (comparison > 0 || comparison == 0 && !q.IsEven)) q++;
        }
        return Round(false, q, BigInteger.One, step, ref status);
    }
    internal static void Compare(uint x, uint y, bool signalAllNaNs, ref uint status)
    {
        x = Input(x, ref status); y = Input(y, ref status); uint flags;
        if (Nan(x) || Nan(y)) { if (signalAllNaNs || Nan(x) && (x & 0x400000) == 0 || Nan(y) && (y & 0x400000) == 0) status |= Ioc; flags = 0x30000000; }
        else if (x == y || Zero(x) && Zero(y)) flags = 0x60000000;
        else { var less = (x >> 31) != (y >> 31) ? (x >> 31) != 0 : (x >> 31) != 0 ? x > y : x < y; flags = less ? 0x80000000u : 0x20000000u; }
        status = status & 0x0fffffff | flags;
    }
    internal static uint FromInteger(uint x, bool signed, int width, int fractionBits, ref uint status)
    {
        var value = signed ? width == 16 ? new BigInteger((short)x) : new BigInteger((int)x) : new BigInteger(width == 16 ? x & 65535 : x);
        if (fractionBits == 0) return Round(value.Sign < 0, BigInteger.Abs(value), BigInteger.One, 0, ref status);
        // Fixed-point-to-float conversions always use round-to-nearest, independently of FPSCR.RMode.
        var nearestStatus = status & ~(3u << 22); var result = Round(value.Sign < 0, BigInteger.Abs(value), BigInteger.One, -fractionBits, ref nearestStatus);
        status |= nearestStatus & 0x9f; return result;
    }
    internal static uint ToInteger(uint x, bool signed, int width, int fractionBits, bool currentRounding, ref uint status)
    {
        x = Input(x, ref status); var min = signed ? -(BigInteger.One << (width - 1)) : BigInteger.Zero; var max = (BigInteger.One << (signed ? width - 1 : width)) - 1;
        if (Nan(x)) { status |= Ioc; return 0; }
        if (Inf(x)) { status |= Ioc; return (uint)((x >> 31) != 0 ? min & uint.MaxValue : max); }
        var a = Unpack(x); var e = a.Exponent + fractionBits; var n = a.Magnitude; var d = BigInteger.One; if (e >= 0) n <<= e; else d <<= -e;
        var q = RoundInteger(n, d, a.Sign, currentRounding ? (int)(status >> 22 & 3) : 3, out var inexact); if (a.Sign) q = -q;
        if (q < min || q > max) { status |= Ioc; q = BigInteger.Max(min, BigInteger.Min(max, q)); }
        else if (inexact) status |= Ixc;
        return (uint)(q & uint.MaxValue);
    }
    internal static uint FromHalf(ushort x, ref uint status)
    {
        var sign = (x >> 15) != 0; var e = x >> 10 & 31; var fraction = x & 1023; var alternate = (status & 1u << 26) != 0;
        if (e == 31 && !alternate)
        { if (fraction == 0) return Sign(sign) | 0x7f800000; return ProcessNaNs(ref status, Sign(sign) | 0x7f800000 | (uint)fraction << 13); }
        return Round(sign, e == 0 ? fraction : fraction | 1024, BigInteger.One, e == 0 ? -24 : e - 25, ref status);
    }
    internal static ushort ToHalf(uint x, ref uint status)
    {
        x = Input(x, ref status); var sign = (x >> 31) != 0; var signBits = sign ? 0x8000u : 0u; var alternate = (status & 1u << 26) != 0;
        if (Nan(x)) { if (alternate) { status |= Ioc; return (ushort)signBits; } var nan = ProcessNaNs(ref status, x); return (ushort)((nan >> 16 & 0x8000) | 0x7c00 | (nan >> 13 & 1023)); }
        if (Inf(x)) { if (alternate) status |= Ioc; return (ushort)(signBits | (alternate ? 0x7fffu : 0x7c00u)); }
        var a = Unpack(x); if (a.Magnitude.IsZero) return (ushort)signBits;
        var e = Log2(a.Magnitude, BigInteger.One) + a.Exponent; var step = Math.Max(e - 10, -24); var n = a.Magnitude; var d = BigInteger.One; var shift = a.Exponent - step; if (shift >= 0) n <<= shift; else d <<= -shift;
        var mode = (int)(status >> 22 & 3); var q = RoundInteger(n, d, sign, mode, out var inexact);
        if (q >= 2048) { q >>= 1; step++; }
        var exponent = step + 10;
        if (exponent > (alternate ? 16 : 15))
        { if (alternate) { status |= Ioc; return (ushort)(signBits | 0x7fff); } status |= Ofc | Ixc; return (ushort)(signBits | (mode == 0 || mode == 1 && !sign || mode == 2 && sign ? 0x7c00u : 0x7bffu)); }
        if (inexact) { status |= Ixc; if (e < -14) status |= Ufc; }
        return (ushort)(signBits | ((uint)q < 1024 ? (uint)q : (uint)(exponent + 15) << 10 | (uint)q & 1023));
    }
}
