using System.Diagnostics;
using Stm32Emulator.Core;

namespace test1;

/// <summary>Inspector bound to the owner's live session; its UI timer is disposed with the window.</summary>
public sealed class TimerWindow : Form
{
    private readonly Emulator emulator;
    private readonly System.Windows.Forms.Timer refreshTimer = new() { Interval = 100 };
    private readonly TextBox state = new() { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, Font = new Font(FontFamily.GenericMonospace, 11) };

    public TimerWindow(Emulator emulator, Action<int> setPace)
    {
        this.emulator = emulator; Text = "Timer and memory inspector"; Size = new Size(650, 520);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, Padding = new Padding(12) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        var note = new Label { Text = "Educational model: TIM1 PWM1 channel 1 and TIM7 up-counter. One instruction = one simulated tick; no interrupt or hardware clock model.", Dock = DockStyle.Fill, AutoSize = true };
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
        controls.Controls.AddRange([address, read, pace]); layout.Controls.Add(controls, 0, 1); layout.Controls.Add(state, 0, 2);
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
        state.Text = $"Simulated ticks: {bus.Ticks}\r\nPWM duty: {bus.PwmDuty:P0}\r\n\r\n" + string.Join("\r\n", new[] { ("TIM1", MemoryBus.Tim1), ("TIM7", MemoryBus.Tim7) }.Select(pair => $"{pair.Item1}: CR1={bus.ReadWord(pair.Item2):X8} PSC={bus.ReadWord(pair.Item2 + 40)} ARR={bus.ReadWord(pair.Item2 + 44)} CNT={bus.ReadWord(pair.Item2 + 36)} UIF={bus.ReadWord(pair.Item2 + 16)}"));
    }
    protected override void Dispose(bool disposing)
    { if (disposing) { refreshTimer.Stop(); refreshTimer.Dispose(); } base.Dispose(disposing); }
}
