using Stm32Emulator.Core;
using Xunit;
namespace Stm32Emulator.Tests;
public class ExpandedCoreTests
{
    private static Emulator Run(string source,int limit=200)
    {var cpu=new Emulator();cpu.LoadProgram(source);for(var n=0;n<limit && cpu.Status is not (ExecutionStatus.Completed or ExecutionStatus.Faulted or ExecutionStatus.Waiting);n++)cpu.Step();Assert.NotEqual(ExecutionStatus.Faulted,cpu.Status);return cpu;}
    [Fact] public void EveryTeachingExampleLoadsAndRunsWithoutFaults()
    {foreach(var example in DemoPrograms.All) {var cpu=Run(example.Source,1000);Assert.NotEqual(ExecutionStatus.Faulted,cpu.Status);}}
    [Fact] public void HintsExecuteAndUndefinedInstructionsTrapExplicitly()
    {
        var cpu=Run("DBG #7\nDMB SY\nDSB SY\nISB SY\nYIELD\nPLD [r0]\nPLI [r0]\nMOV r0,#7");Assert.Equal(7u,cpu.Snapshot().Registers[0]);
        cpu=new Emulator();cpu.LoadProgram("UDF #42");cpu.Step();Assert.Equal(ExecutionStatus.Faulted,cpu.Status);Assert.Equal(ExecutionStop.UndefinedInstruction,cpu.Stop);Assert.Equal(42u,cpu.TrapImmediate);
    }
    [Theory]
    [InlineData("LDR",0x80ff7f01u)][InlineData("LDRT",0x80ff7f01u)]
    [InlineData("LDRB",1u)][InlineData("LDRBT",1u)][InlineData("LDRSB",1u)][InlineData("LDRSBT",1u)]
    [InlineData("LDRH",0x7f01u)][InlineData("LDRHT",0x7f01u)][InlineData("LDRSH",0x7f01u)][InlineData("LDRSHT",0x7f01u)]
    public void EveryLoadWidthAndUnprivilegedFormReadsTheSpecifiedValue(string op,uint expected)
    {var cpu=Run($"LDR r4,=values\n{op} r0,[r4]\n.data\nvalues: .word 0x80ff7f01");Assert.Equal(expected,cpu.Snapshot().Registers[0]);}
    [Theory]
    [InlineData("STR",0xaabbccddu)][InlineData("STRT",0xaabbccddu)]
    [InlineData("STRB",0x112233ddu)][InlineData("STRBT",0x112233ddu)]
    [InlineData("STRH",0x1122ccddu)][InlineData("STRHT",0x1122ccddu)]
    public void EveryStoreWidthPreservesAdjacentBytes(string op,uint expected)
    {var cpu=Run($"LDR r4,=values\nMOV r0,#0xaabbccdd\n{op} r0,[r4]\n.data\nvalues: .word 0x11223344");Assert.Equal(expected,cpu.Memory.ReadWord(MemoryBus.RamBase));}
    [Fact] public void LdmDecrementBeforeLoadsAndUpdatesTheBase()
    {var cpu=Run("LDR r4,=values+8\nLDMDB r4!,{r0-r1}\n.data\nvalues: .word 11,22");Assert.Equal(11u,cpu.Snapshot().Registers[0]);Assert.Equal(22u,cpu.Snapshot().Registers[1]);Assert.Equal(MemoryBus.RamBase,cpu.Snapshot().Registers[4]);}
    [Fact] public void RegisterCallsAndCompareBranchesUseAddresses()
    {var cpu=Run("LDR r4,=function+1\nBLX r4\nCBNZ r0,good\nMOV r1,#99\ngood: CBZ r1,end\nMOV r1,#88\nfunction: MOV r0,#7\nBX lr\nend: NOP");Assert.Equal(7u,cpu.Snapshot().Registers[0]);Assert.Equal(0u,cpu.Snapshot().Registers[1]);}
    [Theory][InlineData("S",4)][InlineData("D",8)]
    public void FloatingMemoryTransfersPreserveAllBits(string bank,int width)
    {
        var cpu=Run($"LDR r4,=values\nVLDR {bank.ToLower()}0,[r4]\nVSTR {bank.ToLower()}0,[r4,#{width}]\n.data\nvalues: .word 0x11223344,0x55667788,0,0");
        Assert.Equal(0x11223344u,cpu.Memory.ReadWord(MemoryBus.RamBase+(uint)width));if(width==8)Assert.Equal(0x55667788u,cpu.Memory.ReadWord(MemoryBus.RamBase+12));
    }
    [Theory][InlineData("VLDM","VSTM",false)][InlineData("VLDMDB","VSTMDB",true)][InlineData("VLDMFD","VSTMEA",false)][InlineData("VLDMEA","VSTMFD",true)]
    public void FloatingMultipleTransfersAndWriteback(string load,string store,bool decrement)
    {
        var cpu=Run($"LDR r4,=values{(decrement?"+8":"")}\n{load} r4!,{{s0-s1}}\nLDR r4,=destination{(decrement?"+8":"")}\n{store} r4!,{{s0-s1}}\n.data\nvalues: .word 0x11223344,0x55667788\ndestination: .word 0,0");
        Assert.Equal(0x11223344u,cpu.Memory.ReadWord(MemoryBus.RamBase+8));Assert.Equal(0x55667788u,cpu.Memory.ReadWord(MemoryBus.RamBase+12));
    }
    [Fact] public void FloatingScalarLanesAndLiteralData()
    {var cpu=Run("MOV r0,#0x11223344\nVMOV.32 d0[1],r0\nVMOV.32 r1,d0[1]\nVLDR s0,number\nB end\n.align 2\nnumber: .float 1.5\nend: NOP");Assert.Equal(0x11223344u,cpu.Snapshot().Registers[1]);Assert.Equal(0x3fc00000u,cpu.Snapshot().FloatingRegisters[0]);}
    [Fact] public void AnInterruptSignalWakesExactlyOneWait()
    {var cpu=Run("WFI\nWFI\nMOV r0,#7");cpu.SignalInterrupt();cpu.Step();Assert.Equal(ExecutionStatus.Waiting,cpu.Status);Assert.Equal(ExecutionStop.WaitForInterrupt,cpu.Stop);cpu.SignalInterrupt();cpu.Step();Assert.Equal(7u,cpu.Snapshot().Registers[0]);}
    [Theory][InlineData("VADD.N.F32 s0,s1,s2")][InlineData("MOV.W.N r0,#1")][InlineData("ADD r0!,r1,r2")][InlineData("PUSH {r0!}")][InlineData("MULS r0,r1,r2")][InlineData("CBZ r8,end\nend: NOP")][InlineData("QADD r0,r1")][InlineData("UADD8 r0,r1")][InlineData("SEL r0,r1")][InlineData("PKHBT r0,r1")][InlineData("PKHTB r0,r1,r2,LSL #1")][InlineData("SSAT16 r0,#8,r1,LSL #1")]
    public void MalformedWidthsAndRegisterFormsAreRejected(string source)
    {Assert.Throws<AssemblyException>(()=>AssemblyProgram.Parse(source));}
    [Fact] public void StackPointerWritesDiscardReservedAlignmentBits()
    {var cpu=Run("LDR r0,=0x20000003\nMOV sp,r0\nMOV r1,sp");Assert.Equal(0x20000000u,cpu.Snapshot().Registers[1]);}
    [Fact] public void AlwaysConditionSuffixesAndCaseInsensitiveConditionsAreAccepted()
    {var cpu=Run("MOVSAL r0,#0\nADDAL r1,r0,#7\nBAL end\nMOV r1,#99\nend: NOP");Assert.Equal(7u,cpu.Snapshot().Registers[1]);Assert.True(cpu.Condition("eq"));}
    [Fact] public void CallsStackAndPopPcRestoreTheCaller()
    {
        var cpu=Run("MOV r0,#7\nBL outer\nB end\nouter: PUSH {lr}\nBL inner\nPOP {pc}\ninner: ADD r0,r0,#5\nBX lr\nend: NOP");
        Assert.Equal(12u,cpu.Snapshot().Registers[0]);Assert.Equal(MemoryBus.RamBase+MemoryBus.RamSize,cpu.Snapshot().Registers[13]);Assert.Equal(ExecutionStatus.Completed,cpu.Status);
    }
    [Fact] public void DataSymbolsLiteralReadsAndResetRestoreTheInitialImage()
    {
        var cpu=Run(".text\nLDR r0,constant\nLDR r1,=values\nLDR r2,[r1]\nB end\n.align 2\nconstant: .word 0x12345678\nend: NOP\n.data\nvalues: .word 0x80ff7f01");
        Assert.Equal(0x12345678u,cpu.Snapshot().Registers[0]);Assert.Equal(0x80ff7f01u,cpu.Snapshot().Registers[2]);
        cpu.Memory.WriteWord(MemoryBus.RamBase,0);cpu.Reset();Assert.Equal(0x80ff7f01u,cpu.Memory.ReadWord(MemoryBus.RamBase));
    }
    [Fact] public void HalfwordsSignedLoadsAndWriteback()
    {
        var cpu=Run("LDR r1,=values\nLDRH r0,[r1],#2\nLDRSH r2,[r1]\nLDRSB r3,[r1,#1]\nMOV r4,#0x1234\nSTRH r4,[r1,#2]!\nLDR r5,[r1,#-1]\n.data\nvalues: .word 0x80ff7f01\n.word 0");
        var r=cpu.Snapshot().Registers;Assert.Equal(0x7f01u,r[0]);Assert.Equal(0xffff80ffu,r[2]);Assert.Equal(0xffffff80u,r[3]);Assert.Equal(MemoryBus.RamBase+4,r[1]);Assert.Equal(0x00123480u,r[5]);
    }
    [Fact] public void MultipleTransferOrderingAndDecrementBefore()
    {
        var cpu=Run("LDR r4,=0x20000020\nMOV r0,#11\nMOV r1,#22\nMOV r2,#33\nSTMDB r4!,{r0-r2}\nMOV r0,#0\nMOV r1,#0\nMOV r2,#0\nLDM r4!,{r0-r2}");
        Assert.Equal(new uint[]{11,22,33},cpu.Snapshot().Registers.Take(3));Assert.Equal(0x20000020u,cpu.Snapshot().Registers[4]);
    }
    [Theory][InlineData("LDREX","STREX",4)][InlineData("LDREXH","STREXH",2)][InlineData("LDREXB","STREXB",1)]
    public void ExclusiveStoreSucceedsOnceAndFailsAfterClear(string load,string store,int width)
    {
        var cpu=Run($"LDR r4,=0x20000000\nMOV r1,#7\n{load} r0,[r4]\n{store} r2,r1,[r4]\n{store} r3,r1,[r4]");
        Assert.Equal(0u,cpu.Snapshot().Registers[2]);Assert.Equal(1u,cpu.Snapshot().Registers[3]);Assert.Equal(7u,cpu.Memory.ReadWord(MemoryBus.RamBase));
        cpu=Run($"LDR r4,=0x20000000\n{load} r0,[r4]\nCLREX\n{store} r2,r1,[r4]");Assert.Equal(1u,cpu.Snapshot().Registers[2]);Assert.True(width>0);
    }
    [Fact] public void OverlappingWriteInvalidatesExclusiveButOtherWordsDoNot()
    {
        var cpu=Run("LDR r4,=0x20000000\nLDREX r0,[r4]\nMOV r1,#9\nSTR r1,[r4,#4]\nSTREX r2,r1,[r4]\nLDREX r0,[r4]\nSTRB r1,[r4,#1]\nSTREX r3,r1,[r4]");
        Assert.Equal(0u,cpu.Snapshot().Registers[2]);Assert.Equal(1u,cpu.Snapshot().Registers[3]);
    }
    [Fact] public void DoubleTransfersAndPreIndexing()
    {
        var cpu=Run("LDR r4,=0x20000000\nMOV r0,#11\nMOV r1,#22\nSTRD r0,r1,[r4,#8]!\nLDRD r2,r3,[r4],#8");
        Assert.Equal(11u,cpu.Snapshot().Registers[2]);Assert.Equal(22u,cpu.Snapshot().Registers[3]);Assert.Equal(0x20000010u,cpu.Snapshot().Registers[4]);
    }
    [Theory][InlineData("TBB", ".byte 0,2", "")][InlineData("TBH", ".hword 0,2", ",LSL #1")]
    public void TableBranchUsesPcRelativeHalfwordOffsets(string instruction,string data,string shift)
    {
        var cpu=Run($"ADR r1,table\nMOV r2,#1\n{instruction} [r1,r2{shift}]\nMOV r0,#99\nMOV r0,#7\nB end\n.align 2\ntable: {data}\n.align 2\nend: NOP");Assert.Equal(7u,cpu.Snapshot().Registers[0]);
    }
    [Fact] public void SystemMasksAndBankedStackSelection()
    {
        var cpu=Run("CPSID IF\nMRS r0,PRIMASK\nMRS r1,FAULTMASK\nLDR r2,=0x20000080\nMSR PSP,r2\nMOV r2,#2\nMSR CONTROL,r2\nMOV r3,sp\nCPSIE IF\nMRS r4,PRIMASK");
        Assert.Equal(1u,cpu.Snapshot().Registers[0]);Assert.Equal(1u,cpu.Snapshot().Registers[1]);Assert.Equal(0x20000080u,cpu.Snapshot().Registers[3]);Assert.Equal(0u,cpu.Snapshot().Registers[4]);
    }
    [Fact] public void UnprivilegedCodeCannotElevateItself()
    {
        var cpu=Run("MOV r0,#1\nMSR CONTROL,r0\nMOV r0,#0\nMSR CONTROL,r0\nCPSID I\nMRS r1,CONTROL\nMRS r2,PRIMASK");Assert.Equal(1u,cpu.Snapshot().Registers[1]);Assert.Equal(0u,cpu.Snapshot().Registers[2]);
    }
    [Fact] public void EventWaitAndInterruptWaitHaveExplicitWakeStates()
    {
        var cpu=Run("SEV\nWFE\nWFE\nMOV r0,#7\nWFI\nMOV r1,#9");Assert.Equal(ExecutionStatus.Waiting,cpu.Status);Assert.Equal(ExecutionStop.WaitForEvent,cpu.Stop);
        var count=cpu.InstructionsExecuted;cpu.Step();Assert.Equal(count,cpu.InstructionsExecuted);cpu.SignalEvent();Assert.Equal(ExecutionStatus.Paused,cpu.Status);cpu.Step();cpu.Step();Assert.Equal(ExecutionStop.WaitForInterrupt,cpu.Stop);cpu.SignalInterrupt();cpu.Step();Assert.Equal(9u,cpu.Snapshot().Registers[1]);Assert.Equal(ExecutionStatus.Completed,cpu.Status);
    }
    [Fact] public async Task RunnerStopsAtServiceCallAndCanResume()
    {
        var cpu=new Emulator();cpu.LoadProgram("SVC #42\nMOV r0,#7");await using var runner=new EmulatorRunner(cpu);await runner.RunAsync(10000,()=>{});Assert.Equal(ExecutionStop.ServiceCall,cpu.Stop);Assert.Equal(42u,cpu.TrapImmediate);Assert.Equal(0u,cpu.Snapshot().Registers[0]);await runner.RunAsync(10000,()=>{});Assert.Equal(7u,cpu.Snapshot().Registers[0]);
    }
    [Fact] public void WaitingAsLastInstructionCanWakeAndFinish()
    {var cpu=Run("WFE");cpu.SignalEvent();cpu.Step();Assert.Equal(ExecutionStatus.Completed,cpu.Status);}
    [Fact] public void SourceIndexTargetsAreRejectedAndSnapshotPcUsesAddresses()
    {var cpu=Run("MOV r0,pc");Assert.Equal(AssemblyProgram.CodeBase+4,cpu.Snapshot().Registers[0]);Assert.Throws<AssemblyException>(()=>AssemblyProgram.Parse("MOV r0,#4\nB 4"));}
    [Fact] public void CrossingSramBoundaryCannotPartiallyStoreOrChangeSp()
    {
        var cpu=new Emulator();cpu.LoadProgram("LDR r4,=0x2000fffc\nMOV r0,#7\nMOV r1,#9\nSTM r4!,{r0,r1}");for(var n=0;n<4;n++)cpu.Step();Assert.Equal(ExecutionStatus.Faulted,cpu.Status);Assert.Equal(0u,cpu.Memory.ReadWord(0x2000fffc));Assert.Equal(0x2000fffcu,cpu.Snapshot().Registers[4]);
    }
    [Fact] public void PeripheralBatchRollsBackIfALaterWriteFails()
    {
        var bus=new MemoryBus();bus.WriteWord(MemoryBus.RccApb2Enr,1<<11);bus.WriteWord(MemoryBus.Tim1+36,13);
        Assert.Throws<InvalidOperationException>(()=>bus.WriteWords(new[]{(MemoryBus.Tim1+36,7u),(MemoryBus.Tim1+48,1u)}));Assert.Equal(13u,bus.ReadWord(MemoryBus.Tim1+36));
    }
    [Theory][InlineData("PUSH {r0,r0}")][InlineData("LDM r0!,{r0-r2}")][InlineData("LDR r0,[r0,#4]!")][InlineData("VCVT.F64.F32 s0,s1")][InlineData("VADD.F32 d0,d1,d2")][InlineData("IT EQ\nB end\nend: NOP\nITE EQ\nB end\nNOP")]
    public void InvalidFormsLeaveTheExistingProgramIntact(string source)
    {var cpu=Run("MOV r0,#7");Assert.Throws<AssemblyException>(()=>cpu.LoadProgram(source));Assert.Equal(7u,cpu.Snapshot().Registers[0]);}
    [Fact] public void FloatingPointArithmeticTransfersAndFlagsAreVisible()
    {
        var cpu=Run("VMOV.F32 s0,#1.0\nVMOV.F32 s1,#2.0\nVADD.F32 s2,s0,s1\nVCMP.F32 s2,s1\nVMRS APSR_NZCV,FPSCR\nVMOV r0,s2");Assert.Equal(0x40400000u,cpu.Snapshot().Registers[0]);Assert.True(cpu.Flags.C);Assert.False(cpu.Flags.Z);Assert.Equal(0x40400000u,cpu.Snapshot().FloatingRegisters[2]);
    }
    [Fact] public void FloatingRegisterPairsAndStackAliasesPreserveBits()
    {
        var cpu=Run("MOV r0,#0x11223344\nMOV r1,#0x55667788\nVMOV d0,r0,r1\nVPUSH {d0}\nVMOV.F32 s0,#1.0\nVPOP {d1}\nVMOV r2,r3,d1");Assert.Equal(0x11223344u,cpu.Snapshot().Registers[2]);Assert.Equal(0x55667788u,cpu.Snapshot().Registers[3]);Assert.Equal(MemoryBus.RamBase+MemoryBus.RamSize,cpu.Snapshot().Registers[13]);
    }
}
