using System.Text.Json;
using Stm32Emulator.Core;
using Xunit;
namespace Stm32Emulator.Tests;
public class InstructionReferenceTests
{
    [Fact]
    public void MatchesQemuIntegerAndDspResults()
    {
        using var json=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures/cpu-vectors.json")));
        var failures=new List<string>();var count=0;
        foreach (var row in json.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var initial=row.GetProperty("registers").EnumerateArray().Select(x=>x.GetUInt32()).ToArray();var apsr=row.GetProperty("apsr").GetUInt32();
            var setup=string.Join('\n',initial.Select((v,n)=>$"MOV r{n},#0x{v:X8}"))+$"\nMOV r12,#0x{apsr:X8}\nMSR APSR_NZCVQ,r12\nMSR APSR_G,r12\n";
            var instruction=row.GetProperty("instruction").GetString()!;var cpu=new Emulator();
            try {cpu.LoadProgram(setup+instruction);for(var n=0;n<20 && cpu.Status!=ExecutionStatus.Completed && cpu.Status!=ExecutionStatus.Faulted;n++)cpu.Step();}
            catch(Exception e){if(failures.Count<30)failures.Add(instruction+": "+e.Message);continue;}
            var expected=row.GetProperty("expected").EnumerateArray().Select(x=>x.GetUInt32()).ToArray();var snapshot=cpu.Snapshot();
            var actualFlags=snapshot.Flags.Packed&0xf80f0000;var expectedFlags=row.GetProperty("expected_apsr").GetUInt32();
            if((cpu.Status!=ExecutionStatus.Completed || !snapshot.Registers.Take(12).SequenceEqual(expected) || actualFlags!=expectedFlags) && failures.Count<30)
                failures.Add($"{instruction} ({initial[0]:X8},{initial[1]:X8},{initial[2]:X8},{initial[3]:X8}) initial flags {apsr:X8}: r0={snapshot.Registers[0]:X8}/r1={snapshot.Registers[1]:X8}/flags={actualFlags:X8}; expected {expected[0]:X8}/{expected[1]:X8}/{expectedFlags:X8}; {snapshot.Message}");
            count++;
        }
        Assert.True(failures.Count==0,$"{count} cases; mismatches:\n"+string.Join('\n',failures));
    }
}
