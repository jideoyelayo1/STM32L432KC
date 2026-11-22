using Stm32Emulator.Core;
using Xunit;

namespace Stm32Emulator.Tests;

public class CoreTests
{
    private static Emulator Run(string source, int steps = 1000)
    {
        var cpu = new Emulator(); cpu.LoadProgram(source);
        for (var i = 0; i < steps && cpu.Status is not (ExecutionStatus.Completed or ExecutionStatus.Faulted); i++) cpu.Step();
        Assert.NotEqual(ExecutionStatus.Faulted, cpu.Status);
        return cpu;
    }

    [Theory]
    [InlineData("0xffffffff", "1", "ADDS", 0u, false, true, true, false)]
    [InlineData("0x7fffffff", "1", "ADDS", 0x80000000u, true, false, false, true)]
    [InlineData("0x80000000", "0x80000000", "ADDS", 0u, false, true, true, true)]
    [InlineData("0", "1", "SUBS", 0xffffffffu, true, false, false, false)]
    [InlineData("0x80000000", "1", "SUBS", 0x7fffffffu, false, false, true, true)]
    [InlineData("42", "42", "SUBS", 0u, false, true, true, false)]
    public void ArithmeticFlags(string left, string right, string op, uint result, bool n, bool z, bool c, bool v)
    {
        var cpu = Run($"MOV r0, #{left}\n{op} r0, r0, #{right}");
        Assert.Equal(result, cpu.Snapshot().Registers[0]); Assert.Equal(new(n, z, c, v), cpu.Flags);
    }

    [Fact] public void AdcConsumesCarryAndPreservesFlagsWithoutS()
    {
        var cpu = Run("MOV r0,#0xffffffff\nADDS r0,r0,#1\nADC r1,r0,#4\nADCS r2,r0,#0");
        Assert.Equal(5u, cpu.Snapshot().Registers[1]); Assert.Equal(1u, cpu.Snapshot().Registers[2]);
        Assert.False(cpu.Flags.C); Assert.False(cpu.Flags.Z);
    }

    [Theory]
    [InlineData("LSLS", 0, 0x80000001u, false)]
    [InlineData("LSLS", 1, 2u, true)]
    [InlineData("LSLS", 31, 0x80000000u, false)]
    [InlineData("LSLS", 32, 0u, true)]
    [InlineData("LSRS", 1, 0x40000000u, true)]
    [InlineData("LSRS", 31, 1u, false)]
    [InlineData("LSRS", 32, 0u, true)]
    [InlineData("ASRS", 1, 0xc0000000u, true)]
    [InlineData("ASRS", 32, 0xffffffffu, true)]
    public void ShiftBoundaries(string op, int shift, uint result, bool carry)
    {
        var cpu = Run($"MOV r0,#0x80000001\n{op} r0,r0,#{shift}");
        Assert.Equal(result, cpu.Snapshot().Registers[0]); Assert.Equal(carry, cpu.Flags.C);
    }

    [Fact] public void LargeRegisterShiftDoesNotUseCSharpModulo32()
    { var cpu = Run("MOV r0,#1\nMOV r1,#33\nLSLS r0,r0,r1"); Assert.Equal(0u, cpu.Snapshot().Registers[0]); Assert.False(cpu.Flags.C); }

    [Fact] public void MovesAliasesBitwiseAndClz()
    {
        var cpu = Run("movw r10,#0x1234\nMOVT r10,#0xABCD\nMOV r11,r10\nBIC r11,r11,#0xff\nORR r11,r11,#0x80\nAND r11,r11,#0xffff\nEOR r11,r11,#0x1200\nORN r12,r11,#0xffffffff\nMOVS r0,#0\nCLZ r1,r0\nMOV r2,sp");
        var regs = cpu.Snapshot().Registers;
        Assert.Equal(0xabcd1234u, regs[10]); Assert.Equal(0x80u, regs[11]); Assert.Equal(0x80u, regs[12]);
        Assert.Equal(32u, regs[1]); Assert.Equal(MemoryBus.RamBase + MemoryBus.RamSize, regs[2]); Assert.True(cpu.Flags.Z);
    }

    [Fact] public void CmpAndConditionalBranchesUseNamedFlags()
    {
        var cpu = Run("MOV r0,#0\nCMP r0,#1\nBGE wrong\nBPL wrong\nBLT good\nwrong: MOV r1,#99\nB end\ngood: MOV r1,#7\nend: NOP");
        Assert.Equal(7u, cpu.Snapshot().Registers[1]); Assert.True(cpu.Flags.N); Assert.False(cpu.Flags.Z);
    }

    [Fact] public void BranchesDoNotSkipDestinationAndBxChangesPc()
    {
        var cpu = Run("LDR r0,=target\nBX r0\nMOV r1,#99\nB end\ntarget: MOV r1,#7\nCBNZ r1,end\nMOV r1,#88\nend: BX lr");
        Assert.Equal(7u, cpu.Snapshot().Registers[1]); Assert.Equal(ExecutionStatus.Completed, cpu.Status);
    }

    [Fact] public void ExplicitListingAddressesAndWideSuffix()
    {
        var cpu = Run("0x08000000 MOV r0,#1\n0x08000004 B.W 0x0800000A\n0x08000008 MOV r0,#99\n0x0800000A ADD r0,r0,#1");
        Assert.Equal(2u, cpu.Snapshot().Registers[0]);
    }

    [Fact] public void ConditionalBlockSkipsInstructionsRatherThanChangingFlags()
    {
        var cpu = Run("MOV r0,#1\nCMP r0,#1\nITE EQ\nMOV r1,#7\nMOV r1,#99\nIT NE\nMOV r2,#99");
        Assert.Equal(7u, cpu.Snapshot().Registers[1]); Assert.Equal(0u, cpu.Snapshot().Registers[2]); Assert.True(cpu.Flags.Z);
    }

    [Fact] public void FloatingStatusTransfersAreStateOnly()
    {
        var cpu = Run("MOV r0,#0x3000000\nVMSR FPSCR,r0\nVMRS r10,FPSCR");
        Assert.Equal(0x3000000u, cpu.Snapshot().Registers[10]);
    }

    [Fact] public void ByteAccessIsLittleEndianAndPreservesAdjacentBytes()
    {
        var cpu = Run("LDR r1,=0x20000000\nMOV r0,#0x44332211\nSTR r0,[r1]\nLDRB r2,[r1,#1]\nMOV r3,#0xAA\nSTRB r3,[r1,#2]\nLDR r4,[r1]");
        Assert.Equal(0x22u, cpu.Snapshot().Registers[2]); Assert.Equal(0x44AA2211u, cpu.Snapshot().Registers[4]);
        Assert.Equal(4, cpu.Memory.AllocatedRamBytes);
    }

    [Theory]
    [InlineData("PUSH {pc}")]
    [InlineData("POP {sp}")]
    [InlineData("DCD unknown")]
    [InlineData("MOV r16,#1")]
    [InlineData("MOVT r0,#0x10000")]
    [InlineData("STR r0,[r1,#4]!,#4")]
    [InlineData("B missing")]
    [InlineData("IT EQ")]
    [InlineData("MOV r0,#0x100000000")]
    [InlineData("MOV r0,#-2147483649")]
    public void BadProgramsAreRejectedBeforeReplacingState(string source)
    {
        var cpu = Run("MOV r0,#7"); Assert.Throws<AssemblyException>(() => cpu.LoadProgram(source));
        Assert.Equal(7u, cpu.Snapshot().Registers[0]);
    }

    [Fact] public void InputBoundsAndDuplicateLabels()
    {
        Assert.Throws<AssemblyException>(() => AssemblyProgram.Parse(new string(';', 65_537)));
        Assert.Throws<AssemblyException>(() => AssemblyProgram.Parse(string.Join('\n', Enumerable.Repeat("NOP", 2001))));
        Assert.Throws<AssemblyException>(() => AssemblyProgram.Parse("loop: NOP\nloop: NOP"));
    }

    [Fact] public void MemoryFaultsAreLineSpecificAndStopExecution()
    {
        var cpu = new Emulator(); cpu.LoadProgram("MOV r0,#0\nLDR r1,[r0]\nMOV r2,#99"); cpu.Step(); cpu.Step();
        Assert.Equal(ExecutionStatus.Faulted, cpu.Status); Assert.Contains("Line 2", cpu.Message); cpu.Step(); Assert.Equal(0u, cpu.Snapshot().Registers[2]);
        Assert.Equal(0u, cpu.Memory.ReadWord(MemoryBus.RamBase + 1));
    }

    [Fact] public void SnapshotsAndSessionsAreIndependentAndResetIsComplete()
    {
        var cpu = Run("MOV r0,#42"); var snapshot = cpu.Snapshot(); snapshot.Registers[0] = 99;
        cpu.Memory.WriteByte(MemoryBus.RamBase, 12); cpu.Reset();
        Assert.Equal(0u, cpu.Snapshot().Registers[0]); Assert.Equal(0u, cpu.Memory.ReadWord(MemoryBus.RamBase)); Assert.Equal(0UL, cpu.Memory.Ticks);
        Assert.Equal(ExecutionStatus.Ready, cpu.Status); Assert.Equal(0, new Emulator().Memory.AllocatedRamBytes);
    }

    [Fact] public void LedExampleProducesExpectedOutputsWithoutPullDownRequirement()
    {
        var cpu = Run(DemoPrograms.All[0].Source); Assert.Equal(ExecutionStatus.Completed, cpu.Status);
        Assert.Equal(0x18u, cpu.Snapshot().Registers[0]); Assert.True(cpu.Memory.PinHigh('A', 3)); Assert.True(cpu.Memory.PinHigh('A', 4));
        Assert.False(cpu.Memory.PinHigh('A', 2));
    }

    [Fact] public void ButtonExampleRespondsToBothToggleDirections()
    {
        var cpu = Run(DemoPrograms.All[1].Source, 100); Assert.False(cpu.Memory.PinHigh('A', 3));
        cpu.Memory.SetInputPin('B', 0, true); for (var i = 0; i < 30; i++) cpu.Step(); Assert.True(cpu.Memory.PinHigh('A', 3));
        cpu.Memory.SetInputPin('B', 0, false); for (var i = 0; i < 30; i++) cpu.Step(); Assert.False(cpu.Memory.PinHigh('A', 3));
    }

    [Fact] public void ClockGatingAndAtomicGpioSetReset()
    {
        var bus = new MemoryBus(); bus.WriteWord(MemoryBus.GpioA, 0); Assert.Equal(0u, bus.ReadWord(MemoryBus.GpioA));
        bus.WriteWord(MemoryBus.RccAhb2Enr, 1); Assert.Equal(uint.MaxValue, bus.ReadWord(MemoryBus.GpioA));
        bus.WriteWord(MemoryBus.GpioA, 1); bus.WriteWord(MemoryBus.GpioA + 24, 1); Assert.True(bus.PinHigh('A', 0));
        bus.WriteWord(MemoryBus.GpioA + 24, 1u << 16); Assert.False(bus.PinHigh('A', 0));
    }

    [Fact] public void TimerAllowsZeroPrescalerLatchesUifAndStopsWhenDisabled()
    {
        var bus = new MemoryBus(); bus.WriteWord(MemoryBus.RccApb1Enr1, 1u << 5);
        bus.WriteWord(MemoryBus.Tim7 + 44, 2); bus.WriteWord(MemoryBus.Tim7, 1); bus.AdvanceTicks(2);
        Assert.Equal(2u, bus.ReadWord(MemoryBus.Tim7 + 36)); bus.AdvanceTicks(); Assert.Equal(0u, bus.ReadWord(MemoryBus.Tim7 + 36));
        Assert.Equal(1u, bus.ReadWord(MemoryBus.Tim7 + 16)); bus.AdvanceTicks(3); Assert.Equal(1u, bus.ReadWord(MemoryBus.Tim7 + 16));
        bus.WriteWord(MemoryBus.Tim7 + 16, 0); Assert.Equal(0u, bus.ReadWord(MemoryBus.Tim7 + 16));
        bus.WriteWord(MemoryBus.Tim7, 0); bus.AdvanceTicks(100); Assert.Equal(0u, bus.ReadWord(MemoryBus.Tim7 + 36));
    }

    [Fact] public void PwmExampleHasQuarterDutyAndUsesPa8()
    {
        var cpu = Run(DemoPrograms.All[2].Source, 150); var bus = cpu.Memory;
        Assert.True(bus.PwmEnabled); Assert.Equal(0.25, bus.PwmDuty);
        Assert.Equal(19u, bus.ReadWord(MemoryBus.Tim1 + 44)); Assert.Equal(5u, bus.ReadWord(MemoryBus.Tim1 + 52));
        var highTicks = 0; for (var i = 0; i < 20; i++) { if (bus.PinHigh('A', 8)) highTicks++; bus.AdvanceTicks(); }
        Assert.Equal(5, highTicks); Assert.Equal(1u, bus.ReadWord(MemoryBus.Tim7 + 16));
    }

    [Fact] public async Task RunnerCancelsAndDoesNotDoubleStart()
    {
        var cpu = new Emulator(); cpu.LoadProgram("loop: B loop"); await using var runner = new EmulatorRunner(cpu);
        var task = runner.RunAsync(10_000, () => { }); Assert.Same(task, runner.RunAsync(10_000, () => { }));
        await Task.Delay(50); await runner.PauseAsync(); var count = cpu.InstructionsExecuted;
        await Task.Delay(30); Assert.Equal(count, cpu.InstructionsExecuted); Assert.False(runner.IsRunning); Assert.True(count > 0);
    }

    [Fact] public async Task RunnerPausesAtItsBudgetAndCanResume()
    {
        var cpu = new Emulator(); cpu.LoadProgram("loop: B loop"); await using var runner = new EmulatorRunner(cpu);
        await runner.RunAsync(10_000, () => { });
        Assert.Equal((ulong)EmulatorRunner.InstructionBudget, cpu.InstructionsExecuted); Assert.Contains("100,000", runner.StopReason);
        var resumed = runner.RunAsync(10_000, () => { }); await Task.Delay(40); await runner.PauseAsync(); await resumed;
        Assert.True(cpu.InstructionsExecuted > (ulong)EmulatorRunner.InstructionBudget);
        cpu.Reset(); runner.ClearStopReason(); Assert.Equal("", runner.StopReason);
    }

    [Fact] public async Task BreakpointPausesBeforeTheNextInstructionAndResumes()
    {
        var cpu = new Emulator(); cpu.LoadProgram("BKPT\nMOV r0,#7"); await using var runner = new EmulatorRunner(cpu);
        await runner.RunAsync(10_000, () => { }); Assert.Equal(1, cpu.InstructionIndex); Assert.Equal(0u, cpu.Snapshot().Registers[0]);
        await runner.RunAsync(10_000, () => { }); Assert.Equal(ExecutionStatus.Completed, cpu.Status); Assert.Equal(7u, cpu.Snapshot().Registers[0]);
    }
}
