using System.Diagnostics;
using Stm32Emulator.Core;

namespace test1;

/// <summary>Inspector bound to the owner's live session; its UI timer is disposed with the window.</summary>
public sealed class TimerWindow : Form
{
    private readonly Emulator emulator;
    private readonly System.Windows.Forms.Timer refreshTimer = new() { Interval = 100 };
    private readonly TextBox state = new() { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, Font = new Font(FontFamily.GenericMonospace, 11), ScrollBars = ScrollBars.Vertical, WordWrap = false };

    public TimerWindow(Emulator emulator, Action<int> setPace, Action changed, Action<DemoProgram> selectExample)
    {
        this.emulator = emulator; Text = "Processor, timer and memory inspector"; Size = new Size(760, 700);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, Padding = new Padding(12) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 145));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        var note = new Label { Text = "Educational model: TIM1 PWM1 channel 1 and TIM7 up-counter. One instruction = one simulated tick; explicit wait/wake signals without exception handlers or hardware timing.", Dock = DockStyle.Fill, AutoSize = true };
        layout.Controls.Add(note, 0, 0);
        var controls = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var address = new TextBox { Text = "0x20000000", Width = 150, AccessibleName = "Memory address" };
        var read = new Button { Text = "Read word", AutoSize = true };
        read.Click += (_, _) =>
        {
            try { var location = AssemblyProgram.Number(address.Text); MessageBox.Show(this, $"0x{location:X8}: 0x{emulator.Memory.ReadWord(location):X8}", "Memory word"); }
            catch (Exception error) when (error is FormatException or OverflowException or InvalidOperationException) { MessageBox.Show(this, error.Message, "Invalid memory address"); }
        };
        var pace = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130, AccessibleName = "Execution pace" };
        pace.Items.AddRange(["5 steps/s", "20 steps/s", "100 steps/s", "1,000 steps/s"]); pace.SelectedIndex = 1;
        pace.SelectedIndexChanged += (_, _) => setPace(new[] { 5, 20, 100, 1000 }[pace.SelectedIndex]);
        var signalEvent = new Button { Text = "Signal event", AutoSize = true };
        signalEvent.Click += (_, _) => { emulator.SignalEvent(); changed(); RefreshState(); };
        var signalInterrupt = new Button { Text = "Signal interrupt", AutoSize = true };
        signalInterrupt.Click += (_, _) => { emulator.SignalInterrupt(); changed(); RefreshState(); };
        var examples = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300, AccessibleName = "Teaching example", DisplayMember = "Title" };
        examples.Items.AddRange(DemoPrograms.All.Cast<object>().ToArray());
        examples.SelectedIndexChanged += (_, _) => { if (examples.SelectedItem is DemoProgram example) selectExample(example); };
        controls.Controls.AddRange([address, read, pace, signalEvent, signalInterrupt, examples]); layout.Controls.Add(controls, 0, 1); layout.Controls.Add(state, 0, 2);
        var source = new LinkLabel { Text = "Model documentation and source", Dock = DockStyle.Fill };
        source.LinkClicked += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo("https://github.com/jideoyelayo1/STM32L432KC/blob/main/docs/model.md") { UseShellExecute = true }); }
            catch (System.ComponentModel.Win32Exception error) { MessageBox.Show(this, error.Message, "Could not open browser"); }
        };
        layout.Controls.Add(source, 0, 3); Controls.Add(layout);
        refreshTimer.Tick += (_, _) => RefreshState(); refreshTimer.Start(); RefreshState();
    }

    private void RefreshState()
    {
        var bus = emulator.Memory;
        var snapshot = emulator.Snapshot();
        state.Text = $"Simulated ticks: {bus.Ticks}\r\nPWM duty: {bus.PwmDuty:P0}\r\n\r\n" + string.Join("\r\n", new[] { ("TIM1", MemoryBus.Tim1), ("TIM7", MemoryBus.Tim7) }.Select(pair => $"{pair.Item1}: CR1={bus.ReadWord(pair.Item2):X8} PSC={bus.ReadWord(pair.Item2 + 40)} ARR={bus.ReadWord(pair.Item2 + 44)} CNT={bus.ReadWord(pair.Item2 + 36)} UIF={bus.ReadWord(pair.Item2 + 16)}"));
        state.Text += $"\r\n\r\nStop: {snapshot.Stop}  {snapshot.Message}\r\nQ={snapshot.Flags.Q} GE={snapshot.Flags.Ge:X1} FPSCR={snapshot.Fpscr:X8}\r\n" +
            $"MSP={snapshot.System.Msp:X8} PSP={snapshot.System.Psp:X8} CONTROL={snapshot.System.Control:X8}\r\n" +
            $"PRIMASK={snapshot.System.Primask} BASEPRI={snapshot.System.Basepri:X2} FAULTMASK={snapshot.System.Faultmask}\r\n" +
            $"Event={snapshot.System.EventPending} Interrupt={snapshot.System.InterruptPending}\r\n\r\nS registers: raw hexadecimal / F32 display\r\n" +
            string.Join("\r\n", snapshot.FloatingRegisters.Select((bits,index) => $"S{index,-2}: {bits:X8}  {BitConverter.UInt32BitsToSingle(bits):G9}"));

    }
    protected override void Dispose(bool disposing)
    { if (disposing) { refreshTimer.Stop(); refreshTimer.Dispose(); } base.Dispose(disposing); }
}
