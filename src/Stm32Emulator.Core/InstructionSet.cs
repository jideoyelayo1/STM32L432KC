using System.Text.RegularExpressions;

namespace Stm32Emulator.Core;

public sealed record Operation(string Name, string Family, bool SetsFlags = false);
public sealed record DecodedOperation(string Name, string Condition, bool SetFlags, string[] Types, int Width);

/// <summary>ARMv7E-M / FPv4-SP-D16 source operations; aliases normalize here, not in execution.</summary>
public static class InstructionSet
{
    private static readonly Dictionary<string, Operation> operations = new(StringComparer.OrdinalIgnoreCase);
    public static IReadOnlyCollection<Operation> All => operations.Values;
    public static readonly string[] Conditions = ["EQ", "NE", "CS", "HS", "CC", "LO", "MI", "PL", "VS", "VC", "HI", "LS", "GE", "LT", "GT", "LE", "AL"];
    private static readonly HashSet<string> flaggable = new("ADC ADD AND ASR BIC EOR LSL LSR MOV MUL MVN ORN ORR ROR RRX RSB SBC SUB".Split(' '));
    private static readonly HashSet<string> narrowOnly = new("BKPT BX BLX CBZ CBNZ CPSID CPSIE IT SEV SVC WFE WFI YIELD".Split(' '));
    static InstructionSet()
    {
        Add("Scalar", "ADC ADD ADR AND ASR BFC BFI BIC CLZ CMN CMP EOR LSL LSR MOV MOVT MOVW MVN ORN ORR RBIT REV REV16 REVSH ROR RRX RSB SBC SBFX SSAT SSAT16 SUB SXTB SXTB16 SXTH SXTAB SXTAB16 SXTAH TEQ TST UBFX USAT USAT16 UXTB UXTB16 UXTH UXTAB UXTAB16 UXTAH PKHBT PKHTB SEL");
        Add("Memory", "LDM LDMDB LDR LDRB LDRBT LDRD LDREX LDREXB LDREXH LDRH LDRHT LDRSB LDRSBT LDRSH LDRSHT LDRT POP PUSH STM STMDB STR STRB STRBT STRD STREX STREXB STREXH STRH STRHT STRT CLREX PLD PLI");
        Add("Control", "B BL BLX BX CBNZ CBZ IT TBB TBH");
        Add("System", "BKPT CPSID CPSIE DMB DSB ISB MRS MSR NOP SEV SVC UDF WFE WFI YIELD");
        Add("Multiply", "MLA MLS MUL SDIV UDIV SMLABB SMLABT SMLATB SMLATT SMLAD SMLADX SMLAL SMLALBB SMLALBT SMLALTB SMLALTT SMLALD SMLALDX SMLAWB SMLAWT SMLSD SMLSDX SMLSLD SMLSLDX SMMLA SMMLAR SMMLS SMMLSR SMMUL SMMULR SMUAD SMUADX SMULBB SMULBT SMULTB SMULTT SMULL SMULWB SMULWT SMUSD SMUSDX UMAAL UMLAL UMULL USAD8 USADA8");
        Add("DSP", "QADD QDADD QDSUB QSUB");
        foreach (var prefix in new[] { "S", "Q", "SH", "U", "UQ", "UH" })
            Add("DSP", string.Join(' ', new[] { "ADD16", "ADD8", "ASX", "SAX", "SUB16", "SUB8" }.Select(s => prefix + s)));
        Add("Floating point", "VABS VADD VCMP VCMPE VCVT VCVTR VCVTB VCVTT VDIV VFMA VFMS VFNMA VFNMS VLDM VLDMDB VLDR VMLA VMLS VMOV VMRS VMSR VMUL VNEG VNMLA VNMLS VNMUL VPOP VPUSH VSQRT VSTM VSTMDB VSTR VSUB");
    }
    private static void Add(string family, string names)
    { foreach (var name in names.Split(' ', StringSplitOptions.RemoveEmptyEntries)) operations.Add(name, new(name, family, flaggable.Contains(name))); }
    public static DecodedOperation Decode(string text)
    {
        var parts = text.ToUpperInvariant().Split('.');
        var name = parts[0]; var condition = "AL"; var flags = false; var width = 0;
        var types = parts.Skip(1).Where(s => s is not ("W" or "N")).ToArray();
        if (parts.Contains("W")) width = 4;
        if (parts.Contains("N")) { if (width != 0) throw new FormatException("Conflicting width suffixes."); width = 2; }
        if (Regex.IsMatch(name, "^IT[TE]{0,3}$")) return new(name, condition, false, [], 2);
        var aliases = new Dictionary<string, string> { ["ADDW"] = "ADD", ["SUBW"] = "SUB", ["LDMIA"] = "LDM", ["LDMFD"] = "LDM", ["LDMEA"] = "LDMDB", ["STMIA"] = "STM", ["STMEA"] = "STM", ["STMFD"] = "STMDB", ["VLDMIA"] = "VLDM", ["VSTMIA"] = "VSTM", ["VSTMDB"] = "VSTMDB", ["VLDMDB"] = "VLDMDB" };
        // Prefer a complete name: TEQ, SMLALS, etc. must not lose a condition by accident.
        if (!operations.ContainsKey(name) && !aliases.ContainsKey(name))
        {
            foreach (var cond in Conditions.Where(c => c != "AL"))
                if (name.EndsWith(cond, StringComparison.Ordinal))
                {
                    var stem = name[..^2];
                    if (operations.ContainsKey(stem) || aliases.ContainsKey(stem) || stem.EndsWith('S') && flaggable.Contains(stem[..^1]))
                    { name = stem; condition = cond; break; }
                }
            if (name.EndsWith('S') && flaggable.Contains(name[..^1])) { flags = true; name = name[..^1]; }
        }
        if (aliases.TryGetValue(name, out var canonical)) name = canonical;
        if (name is "VSTMDB" or "VLDMDB") { }
        else if (!operations.ContainsKey(name)) throw new FormatException($"Unsupported instruction '{text}'. Expected an ARMv7E-M or FPv4-SP-D16 instruction.");
        if (types.Length > 0 && !name.StartsWith('V')) throw new FormatException("Only floating-point instructions accept type suffixes.");
        if (types.Any(t => t is not ("F32" or "F16" or "S32" or "U32" or "S16" or "U16" or "32" or "64"))) throw new FormatException("Unsupported floating-point type; double-precision arithmetic is not available on Cortex-M4.");
        if (width == 4 && narrowOnly.Contains(name)) throw new FormatException($"{name} has no wide form.");
        return new(name, condition, flags, types, width != 0 ? width : narrowOnly.Contains(name) ? 2 : 4);
    }
}
