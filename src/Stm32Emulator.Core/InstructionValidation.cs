namespace Stm32Emulator.Core;

internal static class InstructionValidation
{
    internal static bool WritesPc(Instruction i) => i.Operation.Name is "B" or "BL" or "BX" or "BLX" or "CBZ" or "CBNZ" or "TBB" or "TBH" || i.Arguments.FirstOrDefault() is RegisterOperand { Bank: 'R', Index: 15 } || i.Arguments.Any(a => a is RegisterListOperand list && list.Registers.Any(r => r.Bank == 'R' && r.Index == 15));
    internal static void Validate(Instruction i, AssemblyProgram program)
    {
        var a = i.Arguments; var op = i.Operation.Name;
        void Count(params int[] counts) { if (!counts.Contains(a.Length)) throw new FormatException($"{op} expects {string.Join(" or ", counts)} operands."); }
        RegisterOperand Reg(int index, char bank = 'R') { if (a[index] is not RegisterOperand r || r.Bank != bank) throw new FormatException($"Operand {index + 1} requires a {bank} register."); return r; }
        uint Imm(int index, uint maximum = uint.MaxValue) { if (a[index] is not ImmediateOperand v || v.Value > maximum) throw new FormatException($"Operand {index + 1} requires an immediate no greater than {maximum}."); return v.Value; }
        void Value(int index) { if (a[index] is not ImmediateOperand && a[index] is not RegisterOperand { Bank: 'R', Writeback: false }) throw new FormatException("Expected integer register or immediate."); }
        void Shift(int index, int maximum = 32)
        {
            if (a[index] is not ShiftOperand s || s.Amount is not ImmediateOperand v || v.Value > maximum) throw new FormatException("Expected an immediate shift in range.");
            if (s.Kind == "RRX" && v.Value != 1) throw new FormatException("RRX takes no shift count.");
        }
        MemoryOperand Memory(int index)
        {
            if (a[index] is not MemoryOperand m || m.Base.Bank != 'R' || m.Base.Writeback || m.Offset is not (ImmediateOperand or RegisterOperand { Bank: 'R', Index: < 15, Writeback: false })) throw new FormatException("Expected an integer memory address.");
            if (m.Base.Index == 15 && m.Writeback) throw new FormatException("PC cannot be a writeback base.");
            if (m.Shift is not null && (m.Shift.Kind != "LSL" || m.Shift.Amount is not ImmediateOperand { Value: <= 3 })) throw new FormatException("Memory register offsets allow LSL #0–3.");
            return m;
        }
        RegisterListOperand List(int index, char bank = 'R')
        { if (a[index] is not RegisterListOperand list || list.Registers[0].Bank != bank) throw new FormatException("Invalid register list."); return list; }
        if (op.StartsWith("IT", StringComparison.Ordinal)) { Count(1); if (a[0] is not NamedOperand c || c.Name == "AL" || !InstructionSet.Conditions.Contains(c.Name)) throw new FormatException("Expected IT condition."); return; }
        if (op is "NOP" or "SEV" or "WFE" or "WFI" or "YIELD" or "CLREX") { Count(0); return; }
        if (op is "BKPT" or "SVC" or "UDF") { Count(0, 1); if (a.Length == 1) Imm(0, op == "UDF" ? 65535u : 255u); return; }
        if (op == "DBG") { Count(1); Imm(0,15); return; }
        if (op is "DMB" or "DSB" or "ISB") { Count(0, 1); if (a.Length == 1 && a[0] is not NamedOperand && a[0] is not ImmediateOperand { Value: <= 15 }) throw new FormatException("Invalid barrier option."); return; }
        if (op is "CPSID" or "CPSIE") { Count(1); if (a[0] is not NamedOperand { Name: "I" or "F" or "IF" or "FI" }) throw new FormatException("Expected interrupt mask I/F."); return; }
        if (op is "MRS" or "MSR")
        {
            Count(2); if (Reg(op == "MRS" ? 0 : 1).Index >= 13) throw new FormatException("System transfers require R0–R12.");
            var allowed = op == "MRS" ? "APSR IAPSR EAPSR XPSR IPSR EPSR IEPSR MSP PSP PRIMASK BASEPRI FAULTMASK CONTROL" : "APSR APSR_NZCVQ APSR_NZCVQG APSR_G MSP PSP PRIMASK BASEPRI BASEPRI_MAX FAULTMASK CONTROL";
            if (a[op == "MRS" ? 1 : 0] is not NamedOperand s || !allowed.Split(' ').Contains(s.Name)) throw new FormatException("Invalid or read-only ARM special register."); return;
        }
        if (op is "B" or "BL") { Count(1); program.RegisterTarget(Imm(0)); return; }
        if (op is "BX" or "BLX") { Count(1); if (Reg(0).Index == 15) throw new FormatException("PC cannot be a BX/BLX operand."); return; }
        if (op is "CBZ" or "CBNZ") { Count(2); if (Reg(0).Index > 7) throw new FormatException("CBZ/CBNZ require R0–R7."); program.RegisterTarget(Imm(1)); return; }
        if (op is "TBB" or "TBH") { Count(1); var m = Memory(0); if (m.Offset is not RegisterOperand || m.Writeback || m.Subtract || op == "TBB" && m.Shift is not null || op == "TBH" && m.Shift is not null and not ShiftOperand { Kind: "LSL", Amount: ImmediateOperand { Value: 1 } }) throw new FormatException("Table branches require [base, index]."); return; }
        if (op is "PUSH" or "POP") { Count(1); var l = List(0); if (l.Registers.Any(r => r.Index == 13 || op == "PUSH" && r.Index == 15)) throw new FormatException("Invalid stack register list."); return; }
        if (op is "LDM" or "LDMDB" or "STM" or "STMDB")
        { Count(2); var b = Reg(0); var l = List(1); if (b.Index == 15 || b.Writeback && l.Registers.Any(r => r.Index == b.Index) || l.Registers.Any(r => r.Index == 13)) throw new FormatException("Invalid multiple-transfer base/list."); return; }
        if (op is "PLD" or "PLI") { Count(1); Memory(0); return; }
        if (op.StartsWith("LDR", StringComparison.Ordinal) || op.StartsWith("STR", StringComparison.Ordinal) || op.StartsWith("LDREX", StringComparison.Ordinal) || op.StartsWith("STREX", StringComparison.Ordinal))
        {
            var pair = op is "LDRD" or "STRD"; var exclusiveStore = op.StartsWith("STREX", StringComparison.Ordinal); Count(pair || exclusiveStore ? 3 : 2); Reg(0);
            if (pair || exclusiveStore) Reg(1);
            if (op == "LDR" && a[1] is ImmediateOperand) return; // literal pseudo-load
            var m = Memory(a.Length - 1);
            if (m.Writeback && a.Take(a.Length - 1).OfType<RegisterOperand>().Any(r => r.Index == m.Base.Index)) throw new FormatException("Load destination overlaps writeback base.");
            if ((pair || op.Contains("EX", StringComparison.Ordinal)) && (a[0] is RegisterOperand { Index: >= 13 } || pair && a[1] is RegisterOperand { Index: >= 13 })) throw new FormatException("Invalid double/exclusive register.");
            if (op.Contains("EX", StringComparison.Ordinal) && (m.PostIndex || m.Writeback || m.Offset is not ImmediateOperand)) throw new FormatException("Exclusive accesses require a simple address.");
            if (pair && ((RegisterOperand)a[0]).Index == ((RegisterOperand)a[1]).Index) throw new FormatException("Double-transfer registers must differ.");
            if (exclusiveStore && (((RegisterOperand)a[1]).Index >= 13 || ((RegisterOperand)a[0]).Index == ((RegisterOperand)a[1]).Index || ((RegisterOperand)a[0]).Index == m.Base.Index)) throw new FormatException("Exclusive status register must differ from source and base.");
            return;
        }
        if (op.StartsWith('V')) { FloatingPoint.Validate(i); return; }
        if (op is "MOV" or "MVN" or "MOVW" or "MOVT" or "CMP" or "CMN" or "TST" or "TEQ")
        { Count(2, 3); Reg(0); Value(1); if (op is "MOVW" or "MOVT") Imm(1, 65535); if (a.Length == 3) Shift(2); return; }
        if (op == "ADR") { Count(2); Reg(0); Imm(1); return; }
        if (op is "CLZ" or "RBIT" or "REV" or "REV16" or "REVSH") { Count(2); Reg(0); Reg(1); return; }
        if (op == "BFC") { Count(3); Reg(0); var l = Imm(1, 31); var w = Imm(2, 32); if (w == 0 || l + w > 32) throw new FormatException("Invalid bitfield range."); return; }
        if (op is "BFI" or "SBFX" or "UBFX") { Count(4); Reg(0); Reg(1); var l = Imm(2, 31); var w = Imm(3, 32); if (w == 0 || l + w > 32) throw new FormatException("Invalid bitfield range."); return; }
        if (op is "SSAT" or "USAT" or "SSAT16" or "USAT16")
        { Count(3, 4); Reg(0); var sat = Imm(1, op.Contains("16", StringComparison.Ordinal) ? 16u : 32u); if (op.StartsWith('S') && sat == 0 || op.StartsWith('U') && sat >= (op.Contains("16", StringComparison.Ordinal) ? 16u : 32u)) throw new FormatException("Invalid saturation width."); Reg(2); if (a.Length == 4) Shift(3); return; }
        if (op.StartsWith("SXT", StringComparison.Ordinal) || op.StartsWith("UXT", StringComparison.Ordinal))
        { var add = op.Contains('A'); Count(add ? 3 : 2, add ? 4 : 3); Reg(0); Reg(1); if (add) Reg(2); if (a.Length == (add ? 4 : 3)) { Shift(a.Length - 1, 24); if (a[^1] is not ShiftOperand { Kind: "ROR", Amount: ImmediateOperand v } || v.Value % 8 != 0) throw new FormatException("Extension rotation must be ROR #0/8/16/24."); } return; }
        if (op is "UMULL" or "SMULL" or "UMLAL" or "SMLAL" or "UMAAL" || op.StartsWith("SMLAL", StringComparison.Ordinal) || op.StartsWith("SMLSL", StringComparison.Ordinal))
        { Count(4); for (var j = 0; j < 4; j++) if (Reg(j).Index >= 13) throw new FormatException("Long multiply requires R0–R12."); if (Reg(0).Index == Reg(1).Index) throw new FormatException("Long multiply destinations must differ."); return; }
        var multiply = InstructionSet.All.First(x => x.Name == op).Family == "Multiply";
        if (multiply)
        {
            if (op is "MLA" or "MLS" or "USADA8" || op.StartsWith("SML", StringComparison.Ordinal) || op.StartsWith("SMML", StringComparison.Ordinal)) Count(4);
            else Count(op == "MUL" ? 2 : 3, 3);
            for (var j = 0; j < a.Length; j++) if (Reg(j).Index >= 13) throw new FormatException("Multiply/divide requires R0–R12."); return;
        }
        if (op == "RRX") { Count(2); Reg(0); Reg(1); return; }
        Count(2, 3, 4); Reg(0); if (a.Length > 2) Reg(1); Value(a.Length == 2 ? 1 : 2); if (a.Length == 4) Shift(3);
        if (op is "LSL" or "LSR" or "ASR" or "ROR") { if (a.Length == 4) throw new FormatException("A shift cannot itself take a shifted operand."); if (a[^1] is ImmediateOperand v && v.Value > 32) throw new FormatException("Shift count outside 0–32."); }
    }
}
