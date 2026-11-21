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
            """)
    };
}
