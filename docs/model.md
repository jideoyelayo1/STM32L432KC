# Assembly interpreter model and verification — version 2

This application executes assembly **text** with ARMv7E-M integer/DSP and FPv4-SP-D16 operations. It is an educational interpreter with selected STM32L432KC peripherals. It does not decode binaries, boot compiled firmware, execute exception handlers, implement an MPU/NVIC, or reproduce hardware timing. The browser and Windows applications share one C# core.

## Instruction catalogue

The catalogue contains **229 canonical source mnemonics**. Condition codes, optional S suffixes, IT masks, width suffixes and equivalent stack/addressing aliases are not counted separately. VLDMDB/VSTMDB and LDMDB/STMDB have separate source entries because their addressing behavior differs from increment-after transfers. The [catalogue](instruction-catalogue.md) lists the exact names and their verification categories.

The baseline is ARMv7E-M and FPv4-SP-D16 as described in [ST PM0214, chapter 3](https://www.st.com/resource/en/programming_manual/DM00046982-.pdf), cross-checked against its detailed instruction descriptions. Summary tables alone omit or conflate some names and variants; the earlier approximate missing-instruction count was not a coverage contract.

| Family | Behavior |
| --- | --- |
| Scalar | Add/subtract with carry/borrow, comparisons/tests, bitwise and shifted operands, moves, reversal, extension, packing, bitfields, saturation and selection |
| Multiply/DSP | Signed/unsigned divide; short, long and accumulating multiply; halfword/dual/exchanged/rounded multiply; packed signed/unsigned arithmetic, halving and saturation |
| Memory | Byte/halfword/word/doubleword and signed loads; unprivileged forms; pre/post indexing and writeback; register lists, stack aliases and exclusives |
| Control | Conditional branches, calls, register returns, CBZ/CBNZ, table branches and one- through four-slot IT blocks |
| System | APSR/system-register transfers, masks and stack selection, explicit service/breakpoint/undefined traps, event/interrupt waits and architectural hints |
| FPU | Single-precision arithmetic, fused/non-fused operations, square root, comparisons, integer/fixed/half conversions, raw transfers and multiple/stack transfers |

This is a source-language model, not a binary assembler. It accepts existing educational conveniences such as arbitrary 32-bit MOV immediates, `LDR register,=value`, explicit condition suffixes, and zero-immediate shift notation. It does not prove that every accepted source expression has a particular Thumb encoding. Invalid registers, operand shapes, unsupported types, impossible instruction widths and unmapped accesses produce line-specific diagnostics.

## Parsing, layout and compatibility

Parsing validates the entire listing before replacing a session. Source is limited to 64 KiB UTF-8 and 2,000 instructions. Operands become typed register, immediate, shifted, memory, lane or register-list nodes before execution. Unknown mnemonics are rejected rather than skipped. Constants and layout arithmetic remain bounded to the declared address windows.

- Register names are case-insensitive: R0–R15, SP/R13, LR/R14, PC/R15, S0–S31 and D0–D15. Integers accept decimal, hexadecimal, a leading `#`, signs, symbols and additive offsets. `;` and `//` start comments.
- Code starts at `0x08000000`. Instructions use a deterministic wide slot when a wide form exists; narrow-only instructions use two bytes. `.N`/`.W` select source layout widths. Explicit listings preserve supplied addresses and infer adjacent two-byte slots where possible. These addresses describe this parsed listing, not a generated firmware image.
- PC and LR now contain **code addresses**, replacing version 1's source-index PC. A PC operand reads the current address plus four; literal memory bases use the required word alignment. BL/BLX save the following address with its Thumb bit in LR. Branch lookup resolves the address with bit zero removed. Falling into a non-instruction location faults; reaching the source end completes execution. The existing `BX LR` all-ones return sentinel remains a documented teaching convenience.
- Prefer labels: replace `MOV r0,#4; BX r0` source-index programs with `LDR r0,=target; BX r0; ...; target: ...`. Numeric branch targets must name code addresses.
- Memory supports `[base]`, `[base, offset]`, shifted register offsets, `[...]!`, and `[base], offset`. Register lists accept ranges such as `{r0-r3,lr}` and `{s0-s7}`; VFP lists are consecutive. Multiple-transfer writeback cannot overlap the loaded register list.
- `.text`, `.data` and `.bss` select code or the shared SRAM data cursor. `.word/.long/DCD`, `.hword/.short/DCW`, `.byte/DCB`, `.float/.single`, `.space/.zero`, `.align/.balign`, and `.equ/.set` support bounded data layout. `.align n` aligns to 2^n; `.balign n` aligns to a power-of-two byte count. Layout sizes must already be resolvable. Normal symbol/data references can resolve forward.
- `LDR r0,=label` loads a value/address directly as an educational pseudo-instruction; `LDR r0,label` reads declared literal data. VLDR can likewise read a declared literal label. No hidden binary literal-pool encoding is generated. Common `.thumb`, `.syntax`, `.cpu`, `.fpu`, `.global`, `.type` and `.size` annotations do not generate machine code.
- Reset restores the parsed initial data image and clears registers, flags, reservations, masks, inputs, events and peripheral counters while retaining the loaded source. Failed Load preserves the previous session. UI editor drafts remain local to the page; copy source before leaving.

Version 1's `VMRS/VMSR FPSID/FPEXC` arbitrary storage shortcuts have been removed. The Cortex-M4 status-transfer interface uses FPSCR; unsupported profile/type forms are rejected. Historical listings in the repository are examples of the original implementation, not a binary compatibility promise.

## CPU state and execution

The register file includes named N/Z/C/V flags, sticky Q and four GE bits. Addition uses full-width carry; subtraction carry means **no borrow**. Arithmetic wraps at the destination width. Saturation and packed arithmetic update Q/GE only where the instruction specifies it. Optional flag-setting shifts/logical operations preserve unaffected flags. Register shifts use the low byte of the count; zero is a no-shift source operation, independently of encoded-Thumb alias rules.

MSP and PSP are separate and discard their reserved low alignment bits on writes; CONTROL selects SP and tracks privilege. MRS/MSR implement supported application/system register accesses and privilege restrictions. Execution remains Thread mode without exceptions; IPSR and the execution-state fields unavailable to MRS read as zero. There is no interrupt priority arbitration or memory-protection model. Ordinary unprivileged load/store forms therefore access the same mapped SRAM as privileged forms.

`LoadProgram`, `Step`, `Reset` and `Snapshot` remain the session interface. Snapshots copy integer and S-register arrays and expose FPSCR, system state, a typed stop reason and any trap immediate. `SignalEvent()` and `SignalInterrupt()` provide explicit teaching inputs; neither dispatches a handler.

- BKPT and SVC advance past the instruction and pause; SVC exposes its immediate. Step/Run resumes after the trap. UDF enters a typed fault and requires Reset/Load.
- SEV sets a local event latch. WFE consumes an existing event or waits; an event signal wakes a waiting WFE. WFI consumes a pending explicit interrupt signal or waits. Each signal wakes one wait; waking leaves execution paused until Step/Run.
- An interrupt signal also wakes WFE and clears the exclusive reservation. GPIO buttons do not implicitly generate interrupts. Masks are inspectable architectural state; the explicit wake control does not simulate NVIC routing or handler entry.
- DMB/DSB/ISB have no additional observable effect in this single-core, synchronous memory model. DBG, prefetch and scheduling hints do not invoke external debug/cache/scheduler devices. They still count as executed source instructions.
- One cancellable runner owns execution in each frontend. It yields at least every 160 instructions and stops at traps, waits, faults, completion, cancellation or the 100,000-instruction Run budget. A wait does not spin or consume more instructions. Run may resume after budget exhaustion.
- Every executed or conditionally skipped instruction advances one simulated peripheral tick. Playback pace is independent of hardware clock frequency. Waiting freezes instruction-driven peripheral ticks.

## Floating point

S0–S31 store raw binary32 words. D0–D15 alias adjacent S-register pairs for supported 64-bit transfers; they do not add double-precision arithmetic. VMOV transfers preserve bits. VABS/VNEG apply their architectural sign-bit operations. VMRS can transfer FPSCR's N/Z/C/V to APSR without changing Q/GE.

Arithmetic uses exact integer/rational operations and explicit rounding in managed C#; instruction results do not depend on the host's floating-point evaluation. The model includes four FPSCR rounding modes, signed zero, infinities, signaling/quiet NaNs, default-NaN, flush-to-zero, alternative half precision, sticky exception flags, and the distinction between fused and non-fused multiply/accumulate. Fixed-point-to-float conversion uses its architecturally mandated nearest rounding rather than FPSCR.RMode. Float-to-integer VCVT truncates; VCVTR uses the selected rounding direction.

The inspectors display both raw bits and an F32 interpretation. Integer conversion results remain integer bits in S registers, so their F32 display can look tiny; this is intentional. FPSCR treats reserved or unsupported fields as read-as-zero/write-ignored through the implemented mask. There is no floating-point exception-handler dispatch, lazy exception stacking, NEON, FP16 arithmetic, or F64 arithmetic.

## Memory and peripherals

SRAM is a bounded, sparse, little-endian byte store at 0x20000000–0x2000FFFF. This is one educational 64 KiB window, not the physical SRAM1/SRAM2 split or its aliases. Ordinary SRAM single-word/halfword accesses allow unaligned addresses. Multiple, doubleword and exclusive accesses require the documented natural alignment; peripheral accesses remain naturally aligned. Byte/halfword stores preserve adjacent SRAM bytes. Unmapped addresses fault. Declared code-section data and literals are readable in a bounded, read-only 256 KiB window starting at 0x08000000. Instruction slots have no encoded opcode bytes. Exclusive reservations apply only to SRAM and are cleared by overlapping writes, CLREX, traps, reset and explicit interrupt signals. Multiple stores are transactional in this teaching model: a fault restores all affected memory/peripheral state and suppresses base writeback.

| Block | Address | Modeled behavior |
| --- | --- | --- |
| RCC AHB2ENR | 0x4002104C | GPIOA/B clock gating, bits 0/1 |
| RCC APB1ENR1 | 0x40021058 | TIM7 clock gating, bit 5 |
| RCC APB2ENR | 0x40021060 | TIM1 clock gating, bit 11 |
| GPIOA/B | 0x48000000 / 0x48000400 | MODER, OTYPER, OSPEEDR, PUPDR, IDR, ODR, BSRR, AFRL/AFRH storage; digital input/output and PA8 AF1 |
| TIM1 | 0x40012C00 | CR1, DIER, SR, EGR, CCMR1, CCER, CNT, PSC, ARR, RCR=0, CCR1, BDTR; PWM1 channel 1 |
| TIM7 | 0x40001400 | CR1, DIER, SR, EGR, CNT, PSC, ARR |

GPIO clock-disabled writes are ignored. Output mode uses ODR directly; an output does not require a pull-down configuration to light a virtual LED. Virtual buttons explicitly drive high/low. Analog behavior, floating electrical inputs, pull resistor electrical effects, open-drain behavior and slew rate are not simulated. BSRR sets/resets ODR bits atomically; IDR is read-only.

Timers are 16-bit edge-aligned up-counters. A counter increment occurs after PSC+1 simulated ticks; after ARR+1 counts it wraps to zero and latches UIF. Writing zero clears UIF; EGR.UG resets CNT/prescaler and sets UIF. Disabling the timer/clock freezes it. Register changes take effect immediately even with ARPE set; shadow/preload registers, repetition counting and interrupts are not modeled. TIM1 CCMR1 accepts disabled or PWM1 channel 1 (0x60); other PWM modes fault. DIER is stored but never dispatches an interrupt.

PWM output requires the TIM1 clock, CR1.CEN, CCER.CC1E, BDTR.MOE and CCMR1.OC1M=6. Duty is clamped CCR1/(ARR+1). PA8 must be in alternate-function mode with AF1 to expose that signal. The browser waveform illustrates configured duty rather than displaying a measured trace. TIM3 is not exposed by this model.

Register addresses and PA8/TIM1_CH1 mapping follow ST's [STM32L432KC datasheet](https://www.st.com/resource/en/datasheet/stm32l432kc.pdf), [RM0394 reference manual](https://www.st.com/resource/en/reference_manual/rm0394-stm32l41xxx42xxx43xxx44xxx45xxx46xxx-advanced-armbased-32bit-mcus-stmicroelectronics.pdf), and [CMSIS device definitions](https://github.com/STMicroelectronics/cmsis-device-l4/blob/master/Include/stm32l432xx.h). This document explicitly narrows that hardware behavior to the supported teaching model.

## Verification and release

Run `dotnet test tests/Stm32Emulator.Core.Tests -c Release`. The suite includes independent expected-state integration tests, the original teaching-program regressions, source/error isolation, alignment/bounds, atomic multi-store faults, register lists, stack/calls, exclusives, system privilege, wait/wake/trap lifecycle, cancellation and Run budget behavior.

The committed independent fixtures contain 3,200 QEMU integer/DSP/system cases, 26,480 Berkeley SoftFloat numerical cases, and 50,688 QEMU FPv4-SP-D16 instruction/mode cases. Their generators, pinned numerical reference digest, tooling and license notes are in [scripts/reference](../scripts/reference/README.md). Fixtures are checked offline by the ordinary test suite and regenerated/compared in CI. Reference checks are differential regression evidence, not an exhaustive hardware conformance proof or performance benchmark.

After publishing the browser output into `artifacts/browser`, run `npm ci`, `npx playwright install chromium webkit`, and `npm run test:browser`. The browser suite exercises actual WebAssembly in Chromium and WebKit at 1280px, 390px and 320px widths, including expected outputs, every new teaching example, source retention, GPIO/PWM, waits/traps, sampled native/reference FPU agreement, overflow and touch target sizes. Automated emulation is distinct from physical-phone testing. Windows compilation is distinct from a native Windows UI runtime test.

`demo-v2.0.0` is a compatibility release because PC/LR and initial data semantics changed. Release packaging retains `/demos/stm32/` as the asset base. The portfolio pins an immutable archive, source revision and SHA-256; rollback reinstalls the previous manifest values. Instruction coverage does not expand the documented peripheral/hardware scope.
