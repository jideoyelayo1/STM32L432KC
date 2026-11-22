# Version 2 instruction catalogue

The interpreter exposes **229 canonical source mnemonics**. This inventory follows the ARMv7E-M and FPv4-SP-D16 instruction descriptions in [ST PM0214, chapter 3](https://www.st.com/resource/en/programming_manual/DM00046982-.pdf). It is the explicit profile contract; the approximate count previously derived from the summary table is superseded.

S suffixes, condition codes, IT masks and equivalent assembler aliases are not additional entries. Addressing variants with distinct source names (such as LDMDB and VLDMDB) are listed separately. See [model.md](model.md) for operand grammar, data directives, code addresses, permitted educational conveniences, system traps/waits and hardware exclusions.

Verification labels identify independent expected-result fixtures where available. The remaining entries have expected-state integration tests in [CoreTests](../tests/Stm32Emulator.Core.Tests/CoreTests.cs) and [ExpandedCoreTests](../tests/Stm32Emulator.Core.Tests/ExpandedCoreTests.cs); system behavior is deliberately the documented teaching model. An independent fixture does not imply exhaustive instruction-encoding or hardware conformance.

## Scalar (52)

| Mnemonic | Optional flag suffix | Verification |
| --- | --- | --- |
| `ADC` | `S` | QEMU integer/DSP |
| `ADD` | `S` | QEMU integer/DSP |
| `ADR` | — | Expected-state integration |
| `AND` | `S` | QEMU integer/DSP |
| `ASR` | `S` | QEMU integer/DSP |
| `BFC` | — | QEMU integer/DSP |
| `BFI` | — | QEMU integer/DSP |
| `BIC` | `S` | QEMU integer/DSP |
| `CLZ` | — | QEMU integer/DSP |
| `CMN` | — | QEMU integer/DSP |
| `CMP` | — | QEMU integer/DSP |
| `EOR` | `S` | QEMU integer/DSP |
| `LSL` | `S` | QEMU integer/DSP |
| `LSR` | `S` | QEMU integer/DSP |
| `MOV` | `S` | QEMU integer/DSP |
| `MOVT` | — | Expected-state integration |
| `MOVW` | — | Expected-state integration |
| `MVN` | `S` | QEMU integer/DSP |
| `ORN` | `S` | QEMU integer/DSP |
| `ORR` | `S` | QEMU integer/DSP |
| `PKHBT` | — | QEMU integer/DSP |
| `PKHTB` | — | QEMU integer/DSP |
| `RBIT` | — | QEMU integer/DSP |
| `REV` | — | QEMU integer/DSP |
| `REV16` | — | QEMU integer/DSP |
| `REVSH` | — | QEMU integer/DSP |
| `ROR` | `S` | QEMU integer/DSP |
| `RRX` | `S` | QEMU integer/DSP |
| `RSB` | `S` | QEMU integer/DSP |
| `SBC` | `S` | QEMU integer/DSP |
| `SBFX` | — | QEMU integer/DSP |
| `SEL` | — | QEMU integer/DSP |
| `SSAT` | — | QEMU integer/DSP |
| `SSAT16` | — | QEMU integer/DSP |
| `SUB` | `S` | QEMU integer/DSP |
| `SXTAB` | — | QEMU integer/DSP |
| `SXTAB16` | — | QEMU integer/DSP |
| `SXTAH` | — | QEMU integer/DSP |
| `SXTB` | — | QEMU integer/DSP |
| `SXTB16` | — | QEMU integer/DSP |
| `SXTH` | — | QEMU integer/DSP |
| `TEQ` | — | QEMU integer/DSP |
| `TST` | — | QEMU integer/DSP |
| `UBFX` | — | QEMU integer/DSP |
| `USAT` | — | QEMU integer/DSP |
| `USAT16` | — | QEMU integer/DSP |
| `UXTAB` | — | QEMU integer/DSP |
| `UXTAB16` | — | QEMU integer/DSP |
| `UXTAH` | — | QEMU integer/DSP |
| `UXTB` | — | QEMU integer/DSP |
| `UXTB16` | — | QEMU integer/DSP |
| `UXTH` | — | QEMU integer/DSP |

## Memory (33)

| Mnemonic | Optional flag suffix | Verification |
| --- | --- | --- |
| `CLREX` | — | Expected-state integration |
| `LDM` | — | Expected-state integration |
| `LDMDB` | — | Expected-state integration |
| `LDR` | — | Expected-state integration |
| `LDRB` | — | Expected-state integration |
| `LDRBT` | — | Expected-state integration |
| `LDRD` | — | Expected-state integration |
| `LDREX` | — | Expected-state integration |
| `LDREXB` | — | Expected-state integration |
| `LDREXH` | — | Expected-state integration |
| `LDRH` | — | Expected-state integration |
| `LDRHT` | — | Expected-state integration |
| `LDRSB` | — | Expected-state integration |
| `LDRSBT` | — | Expected-state integration |
| `LDRSH` | — | Expected-state integration |
| `LDRSHT` | — | Expected-state integration |
| `LDRT` | — | Expected-state integration |
| `PLD` | — | Expected-state integration |
| `PLI` | — | Expected-state integration |
| `POP` | — | Expected-state integration |
| `PUSH` | — | Expected-state integration |
| `STM` | — | Expected-state integration |
| `STMDB` | — | Expected-state integration |
| `STR` | — | Expected-state integration |
| `STRB` | — | Expected-state integration |
| `STRBT` | — | Expected-state integration |
| `STRD` | — | Expected-state integration |
| `STREX` | — | Expected-state integration |
| `STREXB` | — | Expected-state integration |
| `STREXH` | — | Expected-state integration |
| `STRH` | — | Expected-state integration |
| `STRHT` | — | Expected-state integration |
| `STRT` | — | Expected-state integration |

## Control (9)

| Mnemonic | Optional flag suffix | Verification |
| --- | --- | --- |
| `B` | — | Expected-state integration |
| `BL` | — | Expected-state integration |
| `BLX` | — | Expected-state integration |
| `BX` | — | Expected-state integration |
| `CBNZ` | — | Expected-state integration |
| `CBZ` | — | Expected-state integration |
| `IT` | — | Expected-state integration |
| `TBB` | — | Expected-state integration |
| `TBH` | — | Expected-state integration |

## System (16)

| Mnemonic | Optional flag suffix | Verification |
| --- | --- | --- |
| `BKPT` | — | Expected-state integration |
| `CPSID` | — | Expected-state integration |
| `CPSIE` | — | Expected-state integration |
| `DBG` | — | Expected-state integration |
| `DMB` | — | Expected-state integration |
| `DSB` | — | Expected-state integration |
| `ISB` | — | Expected-state integration |
| `MRS` | — | QEMU integer/DSP |
| `MSR` | — | Expected-state integration |
| `NOP` | — | Expected-state integration |
| `SEV` | — | Expected-state integration |
| `SVC` | — | Expected-state integration |
| `UDF` | — | Expected-state integration |
| `WFE` | — | Expected-state integration |
| `WFI` | — | Expected-state integration |
| `YIELD` | — | Expected-state integration |

## Multiply (46)

| Mnemonic | Optional flag suffix | Verification |
| --- | --- | --- |
| `MLA` | — | QEMU integer/DSP |
| `MLS` | — | QEMU integer/DSP |
| `MUL` | `S` | QEMU integer/DSP |
| `SDIV` | — | QEMU integer/DSP |
| `SMLABB` | — | QEMU integer/DSP |
| `SMLABT` | — | QEMU integer/DSP |
| `SMLAD` | — | QEMU integer/DSP |
| `SMLADX` | — | QEMU integer/DSP |
| `SMLAL` | — | QEMU integer/DSP |
| `SMLALBB` | — | QEMU integer/DSP |
| `SMLALBT` | — | QEMU integer/DSP |
| `SMLALD` | — | QEMU integer/DSP |
| `SMLALDX` | — | QEMU integer/DSP |
| `SMLALTB` | — | QEMU integer/DSP |
| `SMLALTT` | — | QEMU integer/DSP |
| `SMLATB` | — | QEMU integer/DSP |
| `SMLATT` | — | QEMU integer/DSP |
| `SMLAWB` | — | QEMU integer/DSP |
| `SMLAWT` | — | QEMU integer/DSP |
| `SMLSD` | — | QEMU integer/DSP |
| `SMLSDX` | — | QEMU integer/DSP |
| `SMLSLD` | — | QEMU integer/DSP |
| `SMLSLDX` | — | QEMU integer/DSP |
| `SMMLA` | — | QEMU integer/DSP |
| `SMMLAR` | — | QEMU integer/DSP |
| `SMMLS` | — | QEMU integer/DSP |
| `SMMLSR` | — | QEMU integer/DSP |
| `SMMUL` | — | QEMU integer/DSP |
| `SMMULR` | — | QEMU integer/DSP |
| `SMUAD` | — | QEMU integer/DSP |
| `SMUADX` | — | QEMU integer/DSP |
| `SMULBB` | — | QEMU integer/DSP |
| `SMULBT` | — | QEMU integer/DSP |
| `SMULL` | — | QEMU integer/DSP |
| `SMULTB` | — | QEMU integer/DSP |
| `SMULTT` | — | QEMU integer/DSP |
| `SMULWB` | — | QEMU integer/DSP |
| `SMULWT` | — | QEMU integer/DSP |
| `SMUSD` | — | QEMU integer/DSP |
| `SMUSDX` | — | QEMU integer/DSP |
| `UDIV` | — | QEMU integer/DSP |
| `UMAAL` | — | QEMU integer/DSP |
| `UMLAL` | — | QEMU integer/DSP |
| `UMULL` | — | QEMU integer/DSP |
| `USAD8` | — | QEMU integer/DSP |
| `USADA8` | — | QEMU integer/DSP |

## DSP (40)

| Mnemonic | Optional flag suffix | Verification |
| --- | --- | --- |
| `QADD` | — | QEMU integer/DSP |
| `QADD16` | — | QEMU integer/DSP |
| `QADD8` | — | QEMU integer/DSP |
| `QASX` | — | QEMU integer/DSP |
| `QDADD` | — | QEMU integer/DSP |
| `QDSUB` | — | QEMU integer/DSP |
| `QSAX` | — | QEMU integer/DSP |
| `QSUB` | — | QEMU integer/DSP |
| `QSUB16` | — | QEMU integer/DSP |
| `QSUB8` | — | QEMU integer/DSP |
| `SADD16` | — | QEMU integer/DSP |
| `SADD8` | — | QEMU integer/DSP |
| `SASX` | — | QEMU integer/DSP |
| `SHADD16` | — | QEMU integer/DSP |
| `SHADD8` | — | QEMU integer/DSP |
| `SHASX` | — | QEMU integer/DSP |
| `SHSAX` | — | QEMU integer/DSP |
| `SHSUB16` | — | QEMU integer/DSP |
| `SHSUB8` | — | QEMU integer/DSP |
| `SSAX` | — | QEMU integer/DSP |
| `SSUB16` | — | QEMU integer/DSP |
| `SSUB8` | — | QEMU integer/DSP |
| `UADD16` | — | QEMU integer/DSP |
| `UADD8` | — | QEMU integer/DSP |
| `UASX` | — | QEMU integer/DSP |
| `UHADD16` | — | QEMU integer/DSP |
| `UHADD8` | — | QEMU integer/DSP |
| `UHASX` | — | QEMU integer/DSP |
| `UHSAX` | — | QEMU integer/DSP |
| `UHSUB16` | — | QEMU integer/DSP |
| `UHSUB8` | — | QEMU integer/DSP |
| `UQADD16` | — | QEMU integer/DSP |
| `UQADD8` | — | QEMU integer/DSP |
| `UQASX` | — | QEMU integer/DSP |
| `UQSAX` | — | QEMU integer/DSP |
| `UQSUB16` | — | QEMU integer/DSP |
| `UQSUB8` | — | QEMU integer/DSP |
| `USAX` | — | QEMU integer/DSP |
| `USUB16` | — | QEMU integer/DSP |
| `USUB8` | — | QEMU integer/DSP |

## Floating point (33)

| Mnemonic | Optional flag suffix | Verification |
| --- | --- | --- |
| `VABS` | — | QEMU ARM FPU |
| `VADD` | — | QEMU ARM FPU |
| `VCMP` | — | QEMU ARM FPU |
| `VCMPE` | — | QEMU ARM FPU |
| `VCVT` | — | QEMU ARM FPU |
| `VCVTB` | — | QEMU ARM FPU |
| `VCVTR` | — | QEMU ARM FPU |
| `VCVTT` | — | QEMU ARM FPU |
| `VDIV` | — | QEMU ARM FPU |
| `VFMA` | — | QEMU ARM FPU |
| `VFMS` | — | QEMU ARM FPU |
| `VFNMA` | — | QEMU ARM FPU |
| `VFNMS` | — | QEMU ARM FPU |
| `VLDM` | — | Expected-state integration |
| `VLDMDB` | — | Expected-state integration |
| `VLDR` | — | Expected-state integration |
| `VMLA` | — | QEMU ARM FPU |
| `VMLS` | — | QEMU ARM FPU |
| `VMOV` | — | Expected-state integration |
| `VMRS` | — | Expected-state integration |
| `VMSR` | — | Expected-state integration |
| `VMUL` | — | QEMU ARM FPU |
| `VNEG` | — | QEMU ARM FPU |
| `VNMLA` | — | QEMU ARM FPU |
| `VNMLS` | — | QEMU ARM FPU |
| `VNMUL` | — | QEMU ARM FPU |
| `VPOP` | — | Expected-state integration |
| `VPUSH` | — | Expected-state integration |
| `VSQRT` | — | QEMU ARM FPU |
| `VSTM` | — | Expected-state integration |
| `VSTMDB` | — | Expected-state integration |
| `VSTR` | — | Expected-state integration |
| `VSUB` | — | QEMU ARM FPU |

## Aliases and explicit boundaries

`ADDW/SUBW`, `LDMIA/LDMFD`, `LDMEA`, `STMIA/STMEA`, `STMFD`, and VFP IA/FD/EA stack spellings normalize to their corresponding operations. IT accepts up to four slots and ordinary condition suffixes are evaluated using named APSR flags. `.N/.W` control source layout; this application does not emit instruction bytes.

D-register transfers alias S-register pairs; F64 arithmetic and NEON are outside FPv4-SP-D16. BKPT/SVC pause, UDF faults, WFE/WFI wait for explicit signals, and architectural hints have no extra device/scheduler effects in this model. No catalogue entry implies exception-handler dispatch, firmware loading, additional STM32 peripherals or hardware timing.
