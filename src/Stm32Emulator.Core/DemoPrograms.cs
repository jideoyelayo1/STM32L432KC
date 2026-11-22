namespace Stm32Emulator.Core;

public sealed record DemoProgram(string Id, string Title, string Summary, string Expected, string Source);

public static class DemoPrograms
{
    public static IReadOnlyList<DemoProgram> All { get; } = new[]
    {
        new DemoProgram("leds", "Light the LEDs", "Follow memory-mapped writes from the CPU to two virtual GPIO outputs.",
            "After completion, PA3 and PA4 are high. R0 = 0x18. GPIOA ODR = 0x18.", """
            ; Enable GPIOA's clock.
            LDR r1, =0x4002104C
            MOV r0, #1
            STR r0, [r1]

            ; Set PA3 and PA4 to output mode (01).
            LDR r1, =0x48000000
            LDR r0, [r1]
            BIC r0, r0, #0x3C0
            ORR r0, r0, #0x140
            STR r0, [r1]

            ; Drive PA3 and PA4 high.
            MOV r0, #0x18
            STR r0, [r1, #0x14]
            """),
        new DemoProgram("buttons", "Read a button", "Toggle PB0 and watch the running program copy its input to PA3.",
            "While running, PB0 high lights PA3; PB0 low turns it off. Pause to inspect the loop.", """
            ; Enable GPIOA and GPIOB clocks.
            LDR r1, =0x4002104C
            MOV r0, #3
            STR r0, [r1]

            ; PA3 output.
            LDR r4, =0x48000000
            LDR r0, [r4]
            BIC r0, r0, #0xC0
            ORR r0, r0, #0x40
            STR r0, [r4]

            ; PB0 input.
            LDR r5, =0x48000400
            LDR r0, [r5]
            BIC r0, r0, #3
            STR r0, [r5]

            loop:
            LDR r0, [r5, #0x10]
            AND r0, r0, #1
            CMP r0, #0
            BEQ off
            MOV r2, #8
            STR r2, [r4, #0x14]
            B loop
            off:
            MOV r2, #0
            STR r2, [r4, #0x14]
            B loop
            """),
        new DemoProgram("pwm", "Explore timers & PWM", "Configure TIM1 channel 1 on PA8, then observe its counter and 25% duty cycle.",
            "TIM1 ARR = 19, CCR1 = 5: 25% duty. PA8 uses alternate function 1. TIM7 also counts and latches UIF on rollover.", """
            ; GPIOA clock and PA8 alternate function 1.
            LDR r1, =0x4002104C
            MOV r0, #1
            STR r0, [r1]
            LDR r1, =0x48000000
            LDR r0, [r1]
            BIC r0, r0, #0x30000
            ORR r0, r0, #0x20000
            STR r0, [r1]
            MOV r0, #1
            STR r0, [r1, #0x24]

            ; Enable TIM1 clock.
            LDR r1, =0x40021060
            MOV r0, #0x800
            STR r0, [r1]
            LDR r1, =0x40012C00
            MOV r0, #0
            STR r0, [r1, #0x28]
            MOV r0, #19
            STR r0, [r1, #0x2C]
            MOV r0, #5
            STR r0, [r1, #0x34]
            MOV r0, #0x60
            STR r0, [r1, #0x18]
            MOV r0, #1
            STR r0, [r1, #0x20]
            MOV r0, #0x8000
            STR r0, [r1, #0x44]
            MOV r0, #1
            STR r0, [r1]

            ; TIM7: prescale by 4, roll over after 10 counts.
            LDR r2, =0x40021058
            MOV r0, #0x20
            STR r0, [r2]
            LDR r2, =0x40001400
            MOV r0, #3
            STR r0, [r2, #0x28]
            MOV r0, #9
            STR r0, [r2, #0x2C]
            MOV r0, #1
            STR r0, [r2]
            loop:
            B loop
            """),
        new DemoProgram("calls", "Call a function", "Follow BL, stack saves and POP {pc} through a nested function call.",
            "R0 finishes at 12. SP returns to 0x20010000; LR and PC contain code addresses.", """
            MOV r0,#7
            BL outer
            B finished
            outer:
            PUSH {lr}
            BL inner
            POP {pc}
            inner:
            ADD r0,r0,#5
            BX lr
            finished:
            NOP
            """),
        new DemoProgram("dsp", "Explore DSP saturation", "Compare signed packed arithmetic with saturating arithmetic and inspect Q and GE.",
            "R2 is 0x7FFF7FFF after QADD16. SSAT clips R3 to 127 and sets sticky Q. SADD16 updates GE.", """
            LDR r0,=0x7fff7fff
            LDR r1,=0x00010001
            QADD16 r2,r0,r1
            MOV r3,#300
            SSAT r3,#8,r3
            SADD16 r4,r0,r1
            SEL r5,r0,r1
            """),
        new DemoProgram("float", "Inspect floating-point rounding", "Change FPSCR rounding and compare rounded conversions with exact fused arithmetic.",
            "S2 is 3.0. S3 contains the integer bits for 2 after rounding 1.5 upward; S4 contains 1 after rounding toward zero. Inspect FPSCR flags and raw bits.", """
            VMOV.F32 s0,#1.0
            VMOV.F32 s1,#2.0
            VADD.F32 s2,s0,s1
            VMOV.F32 s3,#1.5
            MOV r0,#0x00400000
            VMSR FPSCR,r0
            VCVTR.S32.F32 s3,s3
            VMOV.F32 s4,#1.5
            VCVT.S32.F32 s4,s4
            VMRS r1,FPSCR
            """),
        new DemoProgram("waits", "Wake a waiting program", "Use the event and interrupt controls to resume WFE and WFI, then inspect a service-call trap.",
            "Signal an event, Run, signal an interrupt, then Run. SVC #42 pauses explicitly; another Run finishes with R0 = 3. No exception handler runs.", """
            WFE
            MOV r0,#1
            WFI
            MOV r0,#2
            SVC #42
            MOV r0,#3
            """)
    };
}
