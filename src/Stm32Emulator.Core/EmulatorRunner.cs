namespace Stm32Emulator.Core;

/// <summary>One cancellable runner, shared by both UIs. Every UI update yields to its synchronization context.</summary>
public sealed class EmulatorRunner(Emulator emulator) : IAsyncDisposable
{
    public const int InstructionBudget = 100_000;
    private CancellationTokenSource? cancellation;
    private Task? task;
    public bool IsRunning => cancellation is not null;
    public string StopReason { get; private set; } = "";

    public Task RunAsync(int instructionsPerSecond, Action changed)
    {
        ArgumentNullException.ThrowIfNull(changed);
        if (IsRunning) return task!;
        if (instructionsPerSecond is < 1 or > 10_000) throw new ArgumentOutOfRangeException(nameof(instructionsPerSecond));
        if (emulator.Program is null || emulator.Status is ExecutionStatus.Faulted or ExecutionStatus.Completed or ExecutionStatus.Waiting) return Task.CompletedTask;
        cancellation = new CancellationTokenSource();
        StopReason = "";
        task = RunLoopAsync(instructionsPerSecond, changed, cancellation);
        return task;
    }

    private async Task RunLoopAsync(int speed, Action changed, CancellationTokenSource source)
    {
        try
        {
            var remaining = InstructionBudget;
            double pending = 0;
            while (!source.IsCancellationRequested)
            {
                // At most 160 instructions between yields, even at the maximum speed.
                await Task.Delay(16, source.Token);
                pending += speed * 0.016;
                var count = Math.Min((int)pending, remaining);
                pending -= count;
                for (var i = 0; i < count && !source.IsCancellationRequested; i++)
                {
                    var snapshot = emulator.Step(); remaining--;
                    if (snapshot.Status is ExecutionStatus.Completed or ExecutionStatus.Faulted or ExecutionStatus.Waiting || snapshot.Stop is ExecutionStop.Breakpoint or ExecutionStop.ServiceCall) return;
                }
                changed();
                if (remaining == 0) { StopReason = "Paused after 100,000 instructions. Run again to continue."; return; }
            }
        }
        catch (OperationCanceledException) when (source.IsCancellationRequested) { }
        finally { cancellation = null; source.Dispose(); changed(); }
    }

    public void ClearStopReason() => StopReason = "";

    public async Task PauseAsync()
    {
        cancellation?.Cancel();
        if (task is not null) await task;
    }

    public async ValueTask DisposeAsync() => await PauseAsync();
}
