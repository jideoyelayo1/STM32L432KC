# Educational model and verification

This application interprets assembly **text**. It is a teaching model of selected STM32L432KC behavior, not a binary decoder, firmware runner, complete Thumb-2 implementation, or cycle-accurate hardware emulator.

## Ownership and execution

`Stm32Emulator.Core` owns parsing, the register file, flags, SRAM and peripheral state. The browser and Windows Forms adapters use the same instance API: `LoadProgram`, `Step`, `Reset`, `Snapshot`, and `Memory.SetInputPin` / `AdvanceTicks`. `EmulatorRunner` owns one cancellable run loop and a 100,000-instruction budget. No timers create independent endless tasks.

Each source instruction, including one skipped by an IT condition, advances one simulated peripheral tick. UI pace controls playback; it does not represent the STM32's clock rate. On completion, fault, breakpoint, cancellation or budget exhaustion the run loop stops. Inputs can change while running. Reset clears CPU, memory, peripheral counters, inputs and instruction state while retaining the loaded program. Snapshots copy registers.

Parsing validates the entire listing before replacing a session. Limits are 64 KiB UTF-8 source and 2,000 executable instructions. Instructions execute in batches of at most 160 between UI yields. A memory error faults the program with its source line; unsupported instructions are never silently skipped.

## Assembly contract

Case-insensitive mnemonics and R0–R15 are supported, along with SP/R13, LR/R14 and PC/R15. Immediates accept decimal, `0x` hexadecimal, optional `#`, and signed negative values within the 32-bit range. `;` and `//` introduce comments. `LDR r0, =0x20000000` is an educational literal-load pseudo instruction.

| Family | Operations | Forms |
| --- | --- | --- |
| Move | MOV, MOVS, MOVW, MOVT | destination, register/immediate; MOVW/MOVT require a 16-bit immediate |
| Memory | LDR, STR, LDRB, STRB | register, [base] or [base, register/immediate offset]; no writeback/post-indexing |
| Arithmetic | ADD, ADDS, SUB, SUBS, ADC, ADCS | destination, source, value; two-operand shorthand uses destination as source |
| Bitwise | AND, ORR, ORN, EOR, BIC | same two-/three-operand forms |
| Compare/count | CMP, CLZ | CMP register,value; CLZ destination,source-register |
| Shift | LSL/LSLS, LSR/LSRS, ASR/ASRS | two-/three-operand forms; immediate count 0–32 or low byte of register count |
| Branch | B, BX, BEQ/BNE/BPL/BMI/BGT/BLT/BLE/BGE/BHI/BLS/BCS/BCC/BVS/BVC, CBZ/CBNZ | label/explicit address; BX uses a register |
| Conditional | IT, ITT, ITE and up to four-slot T/E variants | condition name; conditions are evaluated against the current flags for each slot |
| Status storage | VMRS, VMSR | FPSCR, FPSID, FPEXC transfers only; no floating-point arithmetic |
| Execution | NOP, BKPT | no operands; breakpoint pauses before the following instruction |

MOVS updates N/Z while preserving C/V. ADDS/ADCS/SUBS/CMP set N/Z/C/V. Flag-setting shifts update N/Z/C and preserve V. Arithmetic wraps at 32 bits; subtraction C means **no borrow**. ASR sign-extends. CLZ(0) is 32. A shift count of zero is treated as no shift in this source interpreter; no encoded-Thumb alias rules apply.

PC identifies a source instruction index in label-based programs, or a supplied address in explicitly addressed listings. Pipeline offsets and exception semantics are not modeled. SP starts at 0x20010000 and LR at 0xFFFFFFFF, an educational return sentinel for BX LR. `.W` is an accepted listing annotation and does not infer an instruction size.

Use labels in new programs. A numeric branch target requires an explicit address on **every** instruction, for example:

```asm
0x08000000 MOV r0,#1
0x08000004 B 0x0800000A
0x08000008 MOV r0,#99
0x0800000A ADD r0,r0,#1
```

The original `testingLEDs.txt` and `testingPWMandTimers.txt` are preserved as historical inputs. The latter relies on guessed instruction widths and includes unsupported directives, so it is not a compatibility promise. Use the corrected browser examples or annotate a listing with its actual addresses. PUSH/POP, DCW/DCD, unlisted opcodes, stack operations and binary firmware loading are unsupported.

## Memory and peripherals

SRAM is a bounded, sparse, little-endian byte store at 0x20000000–0x2000FFFF. This is one educational 64 KiB window, not the physical SRAM1/SRAM2 split or its aliases. Word accesses must be four-byte aligned; byte accesses preserve adjacent bytes. Unmapped addresses fault. Flash contents and instructions are not stored as binary data in this bus.

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

## Verification

Run `dotnet test tests/Stm32Emulator.Core.Tests`. The tests use expected values for arithmetic carry/borrow/overflow, shift boundaries, register aliases, branches, conditional execution, little-endian byte writes, mapped-memory faults, GPIO gating, both directions of PB0 toggling, timer rollover/UIF, PWM duty, reset isolation, cancellation, breakpoints, and the run budget. All three example programs have assertions on their resulting CPU/peripheral state.

These are regression tests of the documented subset. They are not a hardware differential/conformance suite or a benchmark. Windows CI compiles the desktop adapter; browser acceptance is checked against the published static output at `/demos/stm32/`. Native Windows UI runtime verification remains separate from compilation.
