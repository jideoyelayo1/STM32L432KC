using Stm32Emulator.Core;

namespace test1;

/// <summary>Windows Forms adapter for the same core used in the browser demo.</summary>
public partial class STM32 : Form
{
    private readonly Emulator emulator = new();
    private readonly EmulatorRunner runner;
    private readonly Dictionary<int, bool> inputs = new() { [0] = false, [1] = false, [3] = false, [4] = false, [5] = false, [7] = false };
    private string loadedSource = "";
    private int speed = 20;
    private bool closing;

    public STM32()
    {
        InitializeComponent();
        runner = new EmulatorRunner(emulator);
        Text = "STM32L432KC educational emulator · shared C# core";
        InputBox.Text = DemoPrograms.All[0].Source;
        Input_TextBox.Text = "Load text";
        Restart_button.Text = "Reset";
        foreach (var output in new[] { REG_txtBox0, REG_txtBox1, REG_txtBox2, REG_txtBox3, Current_Instr_textBox, Next_Instru_textBox, FileTextBox, TIM1_textbox }) output.ReadOnly = true;
        PWM.Image = null;
        PWM.Paint += (_, e) =>
        {
            e.Graphics.Clear(Color.FromArgb(20, 30, 40));
            var width = (int)(PWM.ClientSize.Width * emulator.Memory.PwmDuty);
            e.Graphics.FillRectangle(Brushes.LightGreen, 0, 0, width, PWM.ClientSize.Height);
        };
        LoadSource();
        FormClosing += HandleClosing;
    }

    private bool LoadSource()
    {
        try
        {
            emulator.LoadProgram(InputBox.Text); runner.ClearStopReason(); loadedSource = InputBox.Text;
            foreach (var pin in inputs.Keys.ToArray()) inputs[pin] = false;
            RefreshView(); return true;
        }
        catch (AssemblyException error) { FileTextBox.Text = error.Message; MessageBox.Show(this, error.Message, "Assembly validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
    }

    private bool EnsureProgram() => InputBox.Text == loadedSource || LoadSource();

    private void Load_file_Click(object sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog { Filter = "Assembly text (*.txt;*.s;*.asm)|*.txt;*.s;*.asm|All files (*.*)|*.*" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (InputBox.Text != loadedSource && MessageBox.Show(this, "Replace your edited source with this file?", "Unsaved source", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            if (new FileInfo(dialog.FileName).Length > AssemblyProgram.MaximumSourceBytes) throw new IOException("Source exceeds 64 KiB.");
            InputBox.Text = File.ReadAllText(dialog.FileName); LoadSource();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        { MessageBox.Show(this, error.Message, "Could not read assembly", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async void StartButton_Click(object sender, EventArgs e)
    {
        if (runner.IsRunning || !EnsureProgram()) return;
        var running = runner.RunAsync(speed, RefreshView); RefreshView(); await running;
    }
    private void Step_button_Click(object sender, EventArgs e)
    { if (!runner.IsRunning && EnsureProgram()) { emulator.Step(); RefreshView(); } }
    private async void Restart_button_Click(object sender, EventArgs e)
    {
        await runner.PauseAsync(); emulator.Reset(); runner.ClearStopReason();
        foreach (var pin in inputs.Keys.ToArray()) inputs[pin] = false;
        RefreshView();
    }
    private async void Pause_Button_Click(object sender, EventArgs e)
    { await runner.PauseAsync(); RefreshView(); }
    private void Input_TextBox_Click(object sender, EventArgs e) { if (!runner.IsRunning) LoadSource(); }
    private void STM32_Load(object sender, EventArgs e) => RefreshView();
    private void PB0_Click(object sender, EventArgs e) => Toggle(0);
    private void PB1_Click(object sender, EventArgs e) => Toggle(1);
    private void PB3_Click(object sender, EventArgs e) => Toggle(3);
    private void PB4_Click(object sender, EventArgs e) => Toggle(4);
    private void PB5_Click(object sender, EventArgs e) => Toggle(5);
    private void PB7_Click(object sender, EventArgs e) => Toggle(7);
    private void Toggle(int pin) { inputs[pin] = !inputs[pin]; emulator.Memory.SetInputPin('B', pin, inputs[pin]); RefreshView(); }
    private void TimerOpenButton_Click(object sender, EventArgs e) => new TimerWindow(emulator, value => speed = value).Show(this);

    private void RefreshView()
    {
        if (IsDisposed || Disposing) return;
        var state = emulator.Snapshot();
        var boxes = new[] { REG_txtBox0, REG_txtBox1, REG_txtBox2, REG_txtBox3 };
        for (var group = 0; group < 4; group++) boxes[group].Text = string.Join("  ", Enumerable.Range(group * 4, 4).Select(i => $"R{i}: 0x{state.Registers[i]:X8}"));
        var current = emulator.Program is not null && state.InstructionIndex < emulator.Program.Instructions.Count ? emulator.Program.Instructions[state.InstructionIndex] : null;
        Current_Instr_textBox.Text = current is null ? "No next instruction" : $"Next (line {current.Line}): {current.Mnemonic} {string.Join(", ", current.Operands)}";
        Next_Instru_textBox.Text = $"{state.InstructionsExecuted} instructions · {state.Ticks} simulated ticks · PC is source position";
        FileTextBox.Text = runner.IsRunning ? "Running" : runner.StopReason.Length > 0 ? runner.StopReason : state.Message;
        TIM1_textbox.Text = $"N:{Convert.ToInt32(state.Flags.N)} Z:{Convert.ToInt32(state.Flags.Z)} C:{Convert.ToInt32(state.Flags.C)} V:{Convert.ToInt32(state.Flags.V)}\r\nPWM: {emulator.Memory.PwmDuty:P0}";
        var outputs = new[] { PA0, PA1, PA2, PA3, PA4, PA5, PA6, PA7, PA8, PA9, PA10, PA11, PA12 };
        for (var pin = 0; pin < outputs.Length; pin++) outputs[pin].BackColor = emulator.Memory.PinHigh('A', pin) ? Color.LightGreen : Color.LightGray;
        foreach (var pair in new[] { (PB0, 0), (PB1, 1), (PB3, 3), (PB4, 4), (PB5, 5), (PB7, 7) }) pair.Item1.BackColor = inputs[pair.Item2] ? Color.LightBlue : Color.LightGray;
        StartButton.Enabled = Step_button.Enabled = Load_file.Enabled = Input_TextBox.Enabled = InputBox.Enabled = !runner.IsRunning;
        Pause_Button.Enabled = runner.IsRunning; PWM.Invalidate();
    }

    private async void HandleClosing(object? sender, FormClosingEventArgs e)
    {
        if (closing || !runner.IsRunning) return;
        e.Cancel = true; closing = true; await runner.PauseAsync(); Close();
    }
}
