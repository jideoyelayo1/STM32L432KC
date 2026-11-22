using System.Text.Json;
using Stm32Emulator.Core;
using Xunit;
namespace Stm32Emulator.Tests;
public class FloatingPointTests
{
    [Fact]
    public void MatchesArmFpuInstructionsAndModes()
    {
        using var json=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures/fpu-instruction-vectors.json")));
        var failures=new List<string>();var count=0;var failedOperations=new HashSet<string>();
        foreach(var row in json.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var regs=row.GetProperty("registers").EnumerateArray().Select(x=>x.GetUInt32()).ToArray();var fpscr=row.GetProperty("fpscr").GetUInt32();var instruction=row.GetProperty("instruction").GetString()!;
            var setup=string.Join('\n',regs.Select((v,n)=>$"MOV r0,#0x{v:X8}\nVMOV s{n},r0"))+$"\nMOV r0,#0x{fpscr:X8}\nVMSR FPSCR,r0\n";
            var cpu=new Emulator();cpu.LoadProgram(setup+instruction);for(var n=0;n<12 && cpu.Status is not (ExecutionStatus.Completed or ExecutionStatus.Faulted);n++)cpu.Step();
            var snapshot=cpu.Snapshot();var expected=row.GetProperty("expected").GetUInt32();var expectedFlags=row.GetProperty("expected_fpscr").GetUInt32();
            if(cpu.Status!=ExecutionStatus.Completed || snapshot.FloatingRegisters[0]!=expected || snapshot.Fpscr!=expectedFlags)
            {
                var key=instruction+"/"+(fpscr>>24);
                if(failedOperations.Add(key)&&failures.Count<40)failures.Add($"{instruction} inputs {regs[0]:X8}/{regs[1]:X8}/{regs[2]:X8} FPSCR {fpscr:X8}: {snapshot.FloatingRegisters[0]:X8}/{snapshot.Fpscr:X8} expected {expected:X8}/{expectedFlags:X8}; {snapshot.Message}");
            }
            count++;
        }
        Assert.True(failures.Count==0,$"{count} cases; mismatched forms/modes: {failedOperations.Count}\n"+string.Join('\n',failures));
    }
    [Fact]
    public void MatchesIndependentSoftFloatVectors()
    {
        using var json=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures/float-vectors.json")));
        var failures=new List<string>(); var count=0;
        foreach (var v in json.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var op=v.GetProperty("op").GetString(); var x=v.GetProperty("a").GetUInt32(); var y=v.GetProperty("b").GetUInt32(); var z=v.GetProperty("c").GetUInt32(); var mode=v.GetProperty("mode").GetUInt32(); var status=mode<<22;
            var actual=op switch {
                "ADD"=>SoftFloat32.Add(x,y,ref status), "SUB"=>SoftFloat32.Subtract(x,y,ref status),
                "MUL"=>SoftFloat32.Multiply(x,y,ref status), "DIV"=>SoftFloat32.Divide(x,y,ref status), "SQRT"=>SoftFloat32.Sqrt(x,ref status),
                "FMA"=>SoftFloat32.Fused(z,x,y,false,false,ref status), "I32"=>SoftFloat32.ToInteger(x,true,32,0,true,ref status), "U32"=>SoftFloat32.ToInteger(x,false,32,0,true,ref status),
                "FROM_I32"=>SoftFloat32.FromInteger(x,true,32,0,ref status), "FROM_U32"=>SoftFloat32.FromInteger(x,false,32,0,ref status),
                "HALF"=>SoftFloat32.ToHalf(x,ref status), "FROM_HALF"=>SoftFloat32.FromHalf((ushort)x,ref status), _=>throw new Exception("Unknown reference operation")};
            var expected=v.GetProperty("result").GetUInt32(); var flags=v.GetProperty("flags").GetUInt32();
            if ((actual!=expected || (status&0x9f)!=flags) && failures.Count<30) failures.Add($"{op} {x:X8} {y:X8} {z:X8} mode {mode}: {actual:X8}/{status&0x9f:X2} expected {expected:X8}/{flags:X2}");
            count++;
        }
        Assert.True(failures.Count==0,$"{count} reference cases; mismatches:\n"+string.Join('\n',failures));
    }
}
