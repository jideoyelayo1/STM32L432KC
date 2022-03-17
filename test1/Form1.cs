using System.Text;

namespace test1;

public partial class STM32 : Form
{
    private static bool LookAtInputTextBox;
    private static bool PB3_bool;
    private static bool PB4_bool;
    private static bool PB5_bool;
    private static bool PB7_bool;
    private static bool PB0_bool;
    private static bool PB1_bool;
    public STM32()
    {
        InitializeComponent();
        PC_FromKeil[0x08000346] = 0;
    }

    private void Load_file_Click(object sender, EventArgs e)
    {
        var openFileDialog = new OpenFileDialog();
        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            LookAtInputTextBox = false;
            fileToRun = openFileDialog.FileName;
            Update_FileName_Textbox("File Loaded");
            startingPC = 0;
        }
    }

    private void StartButton_Click(object sender, EventArgs e)
    {
        if (fileToRun != null && !LookAtInputTextBox)
        {
            if (LookAtInputTextBox == false)
                Run(fileToRun);
            else Run(InputBox.Text);
            Update_FileName_Textbox("Running");

        }
        else
        {
            Update_FileName_Textbox("No File Added");

        }
    }
    


    private void Step_button_Click(object sender, EventArgs e)
    {
        if (fileToRun != null && !LookAtInputTextBox)
        {
            if (LookAtInputTextBox == false)
                Step(fileToRun);
            else Step(InputBox.Text);
            Update_FileName_Textbox("Running");

        }
        else
        {
            Update_FileName_Textbox("No File Added");

        }
    }

    private void Restart_button_Click(object sender, EventArgs e)
    {
        Application.Restart();
    }

    private void Pause_Button_Click(object sender, EventArgs e)
    {
        if (Pause_Bool) // Pause is on
        {
            Pause_Bool = false;
            Pause_Button.BackColor = Color.LightGray;
            Pause_Button.Text = "Pause";
        }
        else if (!Pause_Bool) // pause is off
        {
            Pause_Bool = true;
            Pause_Button.BackColor = Color.Green;
            Pause_Button.Text = "Paused";
        }

    }

    private void STM32_Load(object sender, EventArgs e)
    {

    }
    private void Input_TextBox_Click(object sender, EventArgs e)
    {
        if (Input_TextBox.Text != "")
        {
            LookAtInputTextBox = true;
            Run(InputBox.Text);
        }
        else if (Input_TextBox.Text == "Invalid Input")
        {
            Input_TextBox.Text = "";
        }
        else
        {
            Input_TextBox.Text = "Invalid Input";
        }
    }
    private void PB3_Click(object sender, EventArgs e)
    {
        if (!PB3_bool)
        {
            PB3_bool = true;
            PB3.BackColor = Color.Blue;
        }
        else if (PB3_bool)
        {
            PB3_bool = false;
            PB3.BackColor = Color.Red;
        }
    }
    private void PB4_Click(object sender, EventArgs e)
    {
        if (!PB4_bool)
        {
            PB4_bool = true;
            PB4.BackColor = Color.Blue;
        }
        else if (PB4_bool)
        {
            PB4_bool = false;
            PB4.BackColor = Color.Red;
        }
    }
    private void PB5_Click(object sender, EventArgs e)
    {
        if (!PB5_bool)
        {
            PB5_bool = true;
            PB5.BackColor = Color.Blue;
        }
        else if (PB5_bool)
        {
            PB5_bool = false;
            PB5.BackColor = Color.Red;
        }
    }
    private void PB7_Click(object sender, EventArgs e)
    {
        if (!PB7_bool)
        {
            PB7_bool = true;
            PB7.BackColor = Color.Blue;
        }
        else if (PB7_bool)
        {
            PB7_bool = false;
            PB7.BackColor = Color.Red;
        }
    }
    private void PB0_Click(object sender, EventArgs e)
    {
        if (!PB0_bool)
        {
            PB0_bool = true;
            PB0.BackColor = Color.Blue;
        }
        else if (!PB0_bool)
        {
            PB0_bool = false;
            PB0.BackColor = Color.Red;
        }
    }
    private void PB1_Click(object sender, EventArgs e)
    {
        if (!PB1_bool)
        {
            PB1_bool = true;
            PB1.BackColor = Color.Blue;
        }
        else if (PB1_bool)
        {
            PB1_bool = false;
            PB1.BackColor = Color.Red;
        }
    }


    #region FrontEnd

    public static string fileToRun;
    public static int ClockSpeed = 100; // Clockspeed of Instructions
    public static int clockspeed = 10; // Clockspeed of timers

    public static string CurrInstru = "";
    public static string NextInstru = "";

    public static bool Pause_Bool;

    public void Update_Intr()
    {
        //if(NextInstru != null )
        Update_Next_Instr_textbox(NextInstru);
        //if( CurrInstru != null )
        Update_Current_Instr_textbox(CurrInstru);
    }

    #endregion


    #region BackEnd

    public static string[] lines;
    private async void Run(string fname)
    {
        REG[15] = startingPC;
        int yLen;
        if (LookAtInputTextBox)
        {
            var a = InputBox.Text;
            lines = a.Split("\n").ToArray();
            yLen = lines.Length;
        }
        else
        {
            lines = File.ReadLines(fname).ToArray();
            yLen = lines.Length;
        }
        SetUp(fname);

        while (REG[15] < yLen - startingPC)
        {
            // Adding a pause button
            while (true)
            {
                if (!Pause_Bool)
                    break;

                await Task.Delay(20);

            }
            //Update varibles
            Update();
            UpdateButtons();

            // Update Instruction TextBoxes
            CurrInstru = lines[REG[15]];
            if (REG[15] < yLen - 1) NextInstru = lines[REG[15] + 1];
            else NextInstru = "";
            Update_Intr();

            //Run line actions
            Running(lines[REG[15]]);
            UpdatingOutputs();
            REG[15]++;
            Update();
            UpdateButtons();

            // Update Instruction TextBoxes
            if (REG[15] < yLen) CurrInstru = lines[REG[15]];
            else CurrInstru = "";
            if (REG[15] < yLen - 1) NextInstru = lines[REG[15] + 1];
            else NextInstru = "";
            Update_Intr();

            if (REG[15] < lines.Length)
                await Task.Delay(ClockSpeed * CycleTiming(lines[REG[15]]));
        }
    }
    private async void Step(string fname)
    {
        REG[15] = startingPC;
        lines = File.ReadLines(fname).ToArray();
        SetUp(fname);
        REG[15] = startingPC;
        var yLen = lines.Length;
        if (lines.Length > startingPC && lines[REG[15]] != string.Empty)
        {
            //Update varibles
            Update();
            UpdateButtons();

            // Update Instruction TextBoxes
            CurrInstru = lines[REG[15]];
            if (REG[15] < yLen - 1) NextInstru = lines[REG[15] + 1];
            else NextInstru = "";
            Update_Intr();

            //Run line actions
            Running(lines[REG[15]]);
            UpdatingOutputs();
            REG[15]++;
            startingPC = REG[15];
            Update();
            UpdateButtons();
            UpdatingOutputs();

            // Update Instruction TextBoxes
            if (REG[15] < yLen) CurrInstru = lines[REG[15]];
            else CurrInstru = "";
            if (REG[15] < yLen - 1) NextInstru = lines[REG[15] + 1];
            else NextInstru = "";
            Update_Intr();
            if (REG[15] < lines.Length)
                await Task.Delay(1);

        }


    }


    public static uint startingPC;

    //                           0 1           2           3           4  5           6  7  8  9 10       11     12         SP            LR         PC
    public static uint[] REG = { 0, 0x20000060, 0x4002104C, 0x20000260, 0, 0x20000000, 0, 0, 0, 0, 0x08000690, 0, 0x20000040, 0x2000064C, 0x080001FF, 0 };

    //public static Dictionary<uint, uint> MemoryMap = new Dictionary<uint, uint>();
    public static uint[] Memory = new uint[0x3FFFFFFF]; // Memory[addr,value] max array size is 0X7FEFFFFF

    public static void UpdateMem(long value, long addr)
    {
        if (addr <= 0xffffffff && addr % 4 == 0)
            Memory[addr / 4] = (uint)value;
        if (addr > 0xffffffff)
            throw new InvalidOperationException("address larger than memory size");

    }
    public static uint LoadMem(long addr)
    {
        if (addr <= 0xffffffff && addr % 4 == 0)
            return Memory[addr / 4];
        //throw new InvalidOperationException("Value to large to be stored in memory Address:" + Convert.ToString(addr,16));
        // ^ should not exist
        return 0;
    }
    public static string bitTrim(string val, int len)
    {
        for (var i = len - val.Length; i > 0; i--)
            val = '0' + val;
        return val;
    }
    public void UpdatingOutputs()
    {
        ODR_A_LEDs();
        UpdateButtons();
        UpdatePWM();


        UpdateTimers();

        // N(negative) Z(zero) C(carry/borrow) V(overflow) Q(sticky saturation)
        TIM1_textbox.Text = "\n\nFlags:\n\n     ";
        if (xPSR[0]) TIM1_textbox.Text += "\n\nN:1\n\n     ";
        else TIM1_textbox.Text += "\n\nN:0\n\n     ";
        if (xPSR[1]) TIM1_textbox.Text += "\n\nZ:1\n\n     ";
        else TIM1_textbox.Text += "\n\nZ:0\n\n     ";
        if (xPSR[2]) TIM1_textbox.Text += "\n\nC:1\n\n     ";
        else TIM1_textbox.Text += "\n\nC:0\n\n     ";
        if (xPSR[3]) TIM1_textbox.Text += "\n\nV:1\n\n     ";
        else TIM1_textbox.Text += "\n\nV:0\n\n          ";
    }

    public static string ODR_output = "0000000000000000";
    public static string IDR_input = "0000000000000000";
    public void ODR_A_LEDs()
    {
        var ODR_out = "0000000000000000";
        var AHB2ENR = Convert.ToString(Memory[0x4002104c / 4], 2);
        var MODER = Convert.ToString(Memory[0x48000000 / 4], 2);
        var PUPDR = Convert.ToString(Memory[0x4800000c / 4], 2);
        var ODR = Convert.ToString(Memory[0x48000014 / 4], 2);

        AHB2ENR = bitTrim(AHB2ENR, 32);
        MODER = bitTrim(MODER, 32);
        PUPDR = bitTrim(PUPDR, 32);
        ODR = bitTrim(ODR, 16);

        if (AHB2ENR[^1] == '1')
            for (var i = 0; i < 16; i++)
            {
                if (ODR[^1] == '1' && MODER[^2..] == "01" && PUPDR[^2..] == "10")
                    ODR_out = ReplaceAtIndex(ODR_out, i, '1');
                else
                    ODR_out = ReplaceAtIndex(ODR_out, i, '0');
                ODR = ODR[..^1];
                MODER = MODER[..^2];
                PUPDR = PUPDR[..^2];
            }


        ODR_output = ODR_out;
    }
    public static string B_IDR_Output = "0000000000000000";
    private static void UpdateButtons()
    {


        var B_IDR = "";
        if (PB0_bool) B_IDR += '1'; //PB0
        else B_IDR += '0';
        if (PB1_bool) B_IDR += '1'; //PB1
        else B_IDR += '0';
        B_IDR += '0'; //PB2
        if (PB3_bool) B_IDR += '1'; //PB3
        else B_IDR += '0';
        if (PB4_bool) B_IDR += '1'; //PB4
        else B_IDR += '0';
        if (PB5_bool) B_IDR += '1'; //PB5
        else B_IDR += '0';
        B_IDR += '0'; //PB6
        if (PB7_bool) B_IDR += '1'; //PB7
        else B_IDR += '0';
        B_IDR += "00000000";
        B_IDR_Output = B_IDR;
        ODR_B_Buttons();


    }
    public static void ODR_B_Buttons()
    {
        var IDR_out = "0000000000000000";
        var AHB2ENR = Convert.ToString(LoadMem(0x4002104c), 2);
        var MODER = Convert.ToString(LoadMem(0x48000400), 2);
        var PUPDR = Convert.ToString(LoadMem(0x4800040c), 2);
        var IDR = B_IDR_Output;

        AHB2ENR = bitTrim(AHB2ENR, 32);
        MODER = bitTrim(MODER, 32);
        PUPDR = bitTrim(PUPDR, 32);
        IDR = bitTrim(IDR, 16);

        if (AHB2ENR[^2] == '1')
            for (var i = 0; i < IDR_out.Length; i++)
            {
                if (IDR[^1] == '1' && MODER[^2..] == "00" && PUPDR[^2..] == "01" && B_IDR_Output[^1] == '1')
                    IDR_out = ReplaceAtIndex(IDR_out, i, '1');
                else
                    IDR_out = ReplaceAtIndex(IDR_out, i, '0');
                IDR = IDR[..^1];
                MODER = MODER[..^2];
                PUPDR = PUPDR[..^2];
            }


        IDR_input = ReverseAString(IDR_out);
        var temp = (uint)Convert.ToInt64(B_IDR_Output, 2);
        Memory[0x48000410 / 4] = temp;


    }

    public static string ReverseAString(string s)
    {
        var charArray = s.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }
    public static uint NOT(uint num)
    {
        var b = Convert.ToString(num, 2);
        var bNum = b.Length;
        for (var i = 32 - bNum; i > 0; i--)
            b = "0" + b;
        for (var i = 0; i < b.Length; i++)
            switch (b[i])
            {
                case '1':
                    b = ReplaceAtIndex(b, i, '0');
                    break;
                case '0':
                    b = ReplaceAtIndex(b, i, '1');
                    break;
            }
        return (uint)Convert.ToInt64(b, 2);

    }
    public static string ReplaceAtIndex(string text, int index, char c)
    {
        var stringBuilder = new StringBuilder(text);
        stringBuilder[index] = c;
        return stringBuilder.ToString();
    }
    public static int charToInt(char c)
    {
        return c switch
        {
            '0' => 0,
            '1' => 1,
            '2' => 2,
            '3' => 3,
            '4' => 4,
            '5' => 5,
            '6' => 6,
            '7' => 7,
            '8' => 8,
            '9' => 9,
            'p' => 13,
            'r' => 14,
            'c' => 15,
            _ => 0
        };

    }

    private static void Running(string line)
    {
        var temp = line;

        if (line.Contains("LDR")) //LDR
        {

            if (line.Contains("LDRB"))
            {
                var IndexOfS = temp.IndexOf('s');
                var SIndex = temp[IndexOfS + 1];
                var IndexOfR = temp.IndexOf('r');
                var RIndex = temp[IndexOfR + 1];
                var IndexOfVal = temp.IndexOf('x');
                temp = temp[(IndexOfVal + 1)..];
                temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                if (temp.Contains(']'))
                    temp = temp[..^2];
                var val = Convert.ToInt64(temp, 16);


                if (RIndex > IndexOfS)
                {

                    var tempReg = Convert.ToString(REG[13], 2);
                    tempReg = bitTrim(tempReg, 32);
                    var address = REG[charToInt(RIndex)] + val;
                    var remainder = address % 4;
                    var replacer = bitTrim(Convert.ToString(LoadMem(address), 2), 32);

                    if (remainder == 0)
                        for (var i = 0; i < 8; i++)
                            tempReg = ReplaceAtIndex(tempReg, i + 24, replacer[i + 24]);
                    else if (remainder == 1)
                        for (var i = 0; i < 8; i++)
                            tempReg = ReplaceAtIndex(tempReg, i + 16, replacer[i + 16]);
                    else if (remainder == 2)
                        for (var i = 0; i < 8; i++)
                            tempReg = ReplaceAtIndex(tempReg, i + 8, replacer[i + 8]);
                    else if (remainder == 3)
                        for (var i = 0; i < 8; i++)
                            tempReg = ReplaceAtIndex(tempReg, i, replacer[i]);


                    REG[13] = (uint)Convert.ToInt64(tempReg, 2);
                }
                else if (RIndex < IndexOfS)
                {
                    var tempReg = Convert.ToString(REG[charToInt(RIndex)], 2);
                    tempReg = bitTrim(tempReg, 32);
                    var address = REG[13] + val;
                    var remainder = address % 4;
                    var replacer = bitTrim(Convert.ToString(address, 2), 32);

                    if (remainder == 0)
                        for (var i = 0; i < 8; i++)
                            tempReg = ReplaceAtIndex(tempReg, i + 24, replacer[i + 24]);
                    else if (remainder == 1)
                        for (var i = 0; i < 8; i++)
                            tempReg = ReplaceAtIndex(tempReg, i + 16, replacer[i + 16]);
                    else if (remainder == 2)
                        for (var i = 0; i < 8; i++)
                            tempReg = ReplaceAtIndex(tempReg, i + 8, replacer[i + 8]);
                    else if (remainder == 3)
                        for (var i = 0; i < 8; i++)
                            tempReg = ReplaceAtIndex(tempReg, i, replacer[i]);


                    REG[charToInt(RIndex)] = (uint)Convert.ToInt64(tempReg, 2);
                }

            }
            else
            {
                if (line.Contains('['))
                {

                    if (line.Contains("sp") && !line.Contains('r'))
                    {
                        var IdxOfFirstReg = line.IndexOf('r');
                        var regNo1 = line[IdxOfFirstReg + 1];
                        temp = temp[(IdxOfFirstReg + 3)..];
                        char regNo2;

                        var IdxOfSecondReg = temp.IndexOf('s');
                        regNo2 = temp[IdxOfSecondReg + 1];

                        temp = temp[1..];
                        var lastBracketIdx = temp.IndexOf(']');
                        temp = temp[..lastBracketIdx];
                        var valStartsHere = temp.IndexOf('x');
                        temp = temp[(valStartsHere + 1)..];
                        temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                        //Console.WriteLine(temp);
                        var val = Convert.ToInt64(temp, 16);

                        REG[13] = LoadMem(REG[charToInt(regNo2)] + val); //Stack Pointer
                    }
                    else if (line.Contains("sp") && line.Contains('r'))
                    {
                        var IdxOfFirstReg = line.IndexOf('r');
                        var regNo1 = line[IdxOfFirstReg + 1];
                        temp = temp[(IdxOfFirstReg + 3)..];

                        var IndexOfS = temp.IndexOf('s');
                        var SIndex = temp[IndexOfS + 1];
                        var IndexOfR = temp.IndexOf('r');
                        var RIndex = temp[IndexOfR + 1];
                        var IndexOfVal = temp.IndexOf('x');
                        temp = temp[(IndexOfVal + 1)..];
                        temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                        if (temp.Contains(']'))
                            temp = temp[..^2];
                        var val = Convert.ToInt64(temp, 16);

                        if (RIndex > IndexOfS)
                            UpdateMem(REG[charToInt(RIndex)] + val, REG[13]);
                        else if (RIndex < IndexOfS)
                            REG[charToInt(RIndex)] = LoadMem(REG[13] + val);
                    }
                    else
                    {
                        var IdxOfFirstReg = line.IndexOf('r');
                        var regNo1 = line[IdxOfFirstReg + 1];
                        temp = temp[(IdxOfFirstReg + 3)..];
                        char regNo2;
                        var IdxOfSecondReg = temp.IndexOf('r');
                        regNo2 = temp[IdxOfSecondReg + 1];
                        temp = temp[1..];
                        var lastBracketIdx = temp.IndexOf(']');
                        temp = temp[..lastBracketIdx];
                        var valStartsHere = temp.IndexOf('x');
                        temp = temp[(valStartsHere + 1)..];
                        temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                        var val = Convert.ToInt64(temp, 16);

                        Console.WriteLine("error is {0}", Convert.ToString(REG[charToInt(regNo2)], 16));

                        REG[charToInt(regNo1)] = LoadMem(REG[charToInt(regNo2)] + val);
                    }


                }
            }


        }
        else if (line.Contains("ORR")) //ORR
        {
            var IdxOfFirstReg = line.IndexOf('r');
            var regNo1 = line[IdxOfFirstReg + 1];
            temp = temp[(IdxOfFirstReg + 3)..];
            //Console.WriteLine(temp);

            var IdxOfSecondReg = temp.IndexOf('r');
            var regNo2 = temp[IdxOfSecondReg + 1];
            temp = temp[1..];

            var valStartsHere = temp.IndexOf('x');
            temp = temp[(valStartsHere + 1)..];

            temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
            var val = Convert.ToInt64(temp, 16);
            //Console.WriteLine(REG[charToInt(regNo2)] | val);
            Console.WriteLine("{0}|{1}", Convert.ToString(REG[charToInt(regNo2)], 16), Convert.ToString(val, 16));

            REG[charToInt(regNo1)] = (uint)(REG[charToInt(regNo2)] | val);

        }
        else if (line.Contains("STR")) //STR
        {
            if (line.Contains("STRB"))
            {
                var IndexOfS = temp.IndexOf('s');
                var SIndex = temp[IndexOfS + 1];
                var IndexOfR = temp.IndexOf('r');
                var RIndex = temp[IndexOfR + 1];
                var IndexOfVal = temp.IndexOf('x');
                temp = temp[(IndexOfVal + 1)..];
                temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                if (temp.Contains(']'))
                    temp = temp[..^2];
                var val = Convert.ToInt64(temp, 16);

                if (RIndex > IndexOfS)
                {
                    var tempReg = Convert.ToString(REG[13], 2);
                    tempReg = bitTrim(tempReg, 32);
                    var address = REG[charToInt(RIndex)] + val;
                    var remainder = address % 4;
                    var replacer = bitTrim(Convert.ToString(LoadMem(address), 2), 32);

                    if (remainder == 0)
                        for (var i = 0; i < 8; i++)
                            replacer = ReplaceAtIndex(replacer, i + 24, tempReg[i + 24]);
                    else if (remainder == 1)
                        for (var i = 0; i < 8; i++)
                            replacer = ReplaceAtIndex(replacer, i + 16, tempReg[i + 16]);
                    else if (remainder == 2)
                        for (var i = 0; i < 8; i++)
                            replacer = ReplaceAtIndex(replacer, i + 8, tempReg[i + 8]);
                    else if (remainder == 3)
                        for (var i = 0; i < 8; i++)
                            replacer = ReplaceAtIndex(replacer, i, tempReg[i]);


                    UpdateMem(Convert.ToInt64(replacer, 2), address);
                }
                else if (RIndex < IndexOfS)
                {
                    var tempReg = Convert.ToString(REG[13], 2);
                    tempReg = bitTrim(tempReg, 32);
                    var address = REG[charToInt(RIndex)] + val;
                    var remainder = address % 4;
                    var replacer = bitTrim(Convert.ToString(LoadMem(address), 2), 32);

                    if (remainder == 0)
                        for (var i = 0; i < 8; i++)
                            replacer = ReplaceAtIndex(replacer, i + 24, tempReg[i + 24]);
                    else if (remainder == 1)
                        for (var i = 0; i < 8; i++)
                            replacer = ReplaceAtIndex(replacer, i + 16, tempReg[i + 16]);
                    else if (remainder == 2)
                        for (var i = 0; i < 8; i++)
                            replacer = ReplaceAtIndex(replacer, i + 8, tempReg[i + 8]);
                    else if (remainder == 3)
                        for (var i = 0; i < 8; i++)
                            replacer = ReplaceAtIndex(replacer, i, tempReg[i]);


                    UpdateMem(Convert.ToInt64(replacer, 2), address);

                }
                // In program nothing happens


            }
            else
            {
                if (line.Contains('['))
                {

                    if (line.Contains("sp") && !line.Contains('r'))
                    {
                        var IdxOfFirstReg = line.IndexOf('r');
                        var regNo1 = line[IdxOfFirstReg + 1];
                        temp = temp[(IdxOfFirstReg + 3)..];
                        char regNo2;

                        var IdxOfSecondReg = temp.IndexOf('s');
                        regNo2 = temp[IdxOfSecondReg + 1];

                        temp = temp[1..];
                        var lastBracketIdx = temp.IndexOf(']');
                        temp = temp[..lastBracketIdx];
                        var valStartsHere = temp.IndexOf('x');
                        temp = temp[(valStartsHere + 1)..];
                        temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                        var val = Convert.ToInt64(temp, 16);

                        UpdateMem(REG[13], REG[charToInt(regNo2)] + val); //Stack Pointer
                    }
                    else if (line.Contains("sp") && line.Contains('r'))
                    {
                        var IndexOfS = temp.IndexOf('s');
                        var SIndex = temp[IndexOfS + 1];
                        var IndexOfR = temp.IndexOf('r');
                        var RIndex = temp[IndexOfR + 1];
                        var IndexOfVal = temp.IndexOf('x');
                        temp = temp[(IndexOfVal + 1)..];
                        temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                        if (temp.Contains(']'))
                            temp = temp[..^2];
                        var val = Convert.ToInt64(temp, 16);

                        if (RIndex > IndexOfS)
                            UpdateMem(REG[13], REG[charToInt(RIndex)] + val);
                        else if (RIndex < IndexOfS)
                            UpdateMem(REG[charToInt(RIndex)], REG[13] + val);


                    }
                    else
                    {
                        var IdxOfFirstReg = line.IndexOf('r');
                        var regNo1 = line[IdxOfFirstReg + 1];
                        temp = temp[(IdxOfFirstReg + 3)..];
                        char regNo2;
                        var IdxOfSecondReg = temp.IndexOf('r');
                        regNo2 = temp[IdxOfSecondReg + 1];
                        temp = temp[1..];
                        var lastBracketIdx = temp.IndexOf(']');
                        temp = temp[..lastBracketIdx];
                        var valStartsHere = temp.IndexOf('x');
                        temp = temp[(valStartsHere + 1)..];
                        temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                        var val = Convert.ToInt64(temp, 16);

                        UpdateMem(REG[charToInt(regNo1)], REG[charToInt(regNo2)] + val);

                    }
                }
            }

        }
        else if (line.Contains("MOVS"))
        {
            var IdxOfFirstReg = line.IndexOf('r');
            var regNo1 = line[IdxOfFirstReg + 1];
            temp = temp[(IdxOfFirstReg + 1)..];


            if (line.Contains('x'))
            {
                var valStartsHere = temp.IndexOf('x');
                temp = temp[(valStartsHere + 1)..];
                temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                for (var i = 4 - temp.Length; i > 0; i--)
                    temp = '0' + temp;

                var val = Convert.ToInt64(temp, 16);

                var tempREG = Convert.ToString(REG[charToInt(regNo1)], 16);
                for (var i = 8 - tempREG.Length; i > 0; i--)
                    tempREG = "0" + tempREG;

                if (REG[charToInt(regNo1)] - val < 0)
                    xPSR[0] = false;
                else
                    xPSR[0] = true;

                REG[charToInt(regNo1)] = (uint)Convert.ToInt64(tempREG[^4..] + temp[..4], 16);
            }
            else if (temp.Contains('r'))
            {
                var valStartsHere = temp.IndexOf('r');
                var regNo2 = temp[valStartsHere + 1];
                temp = Convert.ToString(REG[charToInt(regNo2)], 16);
                for (var i = 4 - temp.Length; i > 0; i--)
                    temp = '0' + temp;

                var val = Convert.ToInt64(temp, 16);

                var tempREG = Convert.ToString(REG[charToInt(regNo1)], 16);
                for (var i = 8 - tempREG.Length; i > 0; i--)
                    tempREG = "0" + tempREG;
                REG[charToInt(regNo1)] = (uint)Convert.ToInt64(tempREG[^4..] + temp, 16);
            }


        }
        else if (line.Contains("MOVT"))
        {
            var IdxOfFirstReg = line.IndexOf('r');
            var regNo1 = line[IdxOfFirstReg + 1];
            temp = temp[(IdxOfFirstReg + 3)..];


            var valStartsHere = temp.IndexOf('x');
            temp = temp[(valStartsHere + 1)..];
            temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
            var val = Convert.ToInt64(temp, 16);

            var tempREG = Convert.ToString(REG[charToInt(regNo1)], 16);
            for (var i = 8 - tempREG.Length; i > 0; i--)
                tempREG = "0" + tempREG;
            if (REG[charToInt(regNo1)] - val < 0)
                xPSR[0] = false;
            else
                xPSR[0] = true;
            REG[charToInt(regNo1)] = (uint)Convert.ToInt64(temp + tempREG[^4..], 16);

        }
        else if (line.Contains("BIC"))
        {
            var IdxOfFirstReg = line.IndexOf('r');
            var regNo1 = line[IdxOfFirstReg + 1];
            temp = temp[(IdxOfFirstReg + 3)..];
            //Console.WriteLine(temp);

            var IdxOfSecondReg = temp.IndexOf('r');
            var regNo2 = temp[IdxOfSecondReg + 1];
            temp = temp[1..];


            if (temp.Contains('x'))
            {
                var valStartsHere = temp.IndexOf('x');
                temp = temp[(valStartsHere + 1)..];
                temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                var val = Convert.ToInt64(temp, 16);
                REG[charToInt(regNo1)] = REG[charToInt(regNo2)] & NOT((uint)val);
            }
            else if (temp.Contains('r'))
            {
                var IdxOfThirdReg = temp.IndexOf('r');
                var regNo3 = temp[IdxOfThirdReg + 1];
                temp = temp[1..];

                REG[charToInt(regNo1)] = REG[charToInt(regNo2)] & NOT(REG[charToInt(regNo3)]);
            }

        }
        else if (line.Contains("MOVW"))
        {
            var IdxOfFirstReg = line.IndexOf('r');
            var regNo1 = line[IdxOfFirstReg + 1];
            temp = temp[(IdxOfFirstReg + 3)..];

            var valStartsHere = temp.IndexOf('x');
            temp = temp[(valStartsHere + 1)..];
            temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
            var val = Convert.ToInt64(temp, 16);

            for (var i = 4 - temp.Length; i > 0; i--)
                temp = '0' + temp;
            var tempREG = Convert.ToString(REG[charToInt(regNo1)], 16);

            for (var i = 8 - tempREG.Length; i > 0; i--)
                tempREG = '0' + tempREG;
            REG[charToInt(regNo1)] = (uint)Convert.ToInt64(tempREG[..4] + temp, 16);
        }
        else if (line.Contains("MOV"))
        {
            var IdxOfFirstReg = line.IndexOf('r');
            var regNo1 = line[IdxOfFirstReg + 1];
            temp = temp[(IdxOfFirstReg + 3)..];


            var valStartsHere = temp.IndexOf('x');
            temp = temp[(valStartsHere + 1)..];
            temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
            var val = Convert.ToInt64(temp, 16);

            REG[charToInt(regNo1)] = (uint)val;

        }
        else if (line.Contains("AND"))
        {
            var IdxOfFirstReg = line.IndexOf('r');
            var regNo1 = line[IdxOfFirstReg + 1];
            temp = temp[(IdxOfFirstReg + 3)..];
            //Console.WriteLine(temp);

            var IdxOfSecondReg = temp.IndexOf('r');
            var regNo2 = temp[IdxOfSecondReg + 1];
            temp = temp[1..];

            var valStartsHere = temp.IndexOf('x');
            temp = temp[(valStartsHere + 1)..];

            temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
            var val = Convert.ToInt64(temp, 16);
            //Console.WriteLine(REG[charToInt(regNo2)] | val);

            REG[charToInt(regNo1)] = (uint)(REG[charToInt(regNo2)] & val);
        }
        else if (line.Contains("EOR"))
        {
            var IdxOfFirstReg = line.IndexOf('r');
            var regNo1 = line[IdxOfFirstReg + 1];
            temp = temp[(IdxOfFirstReg + 3)..];
            //Console.WriteLine(temp);

            var IdxOfSecondReg = temp.IndexOf('r');
            var regNo2 = temp[IdxOfSecondReg + 1];
            temp = temp[1..];

            var valStartsHere = temp.IndexOf('x');
            temp = temp[(valStartsHere + 1)..];

            temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
            var val = Convert.ToInt64(temp, 16);
            //Console.WriteLine(REG[charToInt(regNo2)] | val);

            REG[charToInt(regNo1)] = (uint)(REG[charToInt(regNo2)] ^ val);

        }
        else if (line.Contains("ORN"))
        {
            var IdxOfFirstReg = line.IndexOf('r');
            var regNo1 = line[IdxOfFirstReg + 1];
            temp = temp[(IdxOfFirstReg + 3)..];
            //Console.WriteLine(temp);

            var IdxOfSecondReg = temp.IndexOf('r');
            var regNo2 = temp[IdxOfSecondReg + 1];
            temp = temp[1..];

            var valStartsHere = temp.IndexOf('x');
            temp = temp[(valStartsHere + 1)..];

            temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
            var val = Convert.ToInt64(temp, 16);

            REG[charToInt(regNo1)] = REG[charToInt(regNo2)] | NOT((uint)val);
        }
        else if (line.Contains("CMP"))
        {
            // N(negative) Z(zero) C(carry/borrow) V(overflow) Q(sticky saturation)
            long CMPtemp = 0;
            var IdxOfFirstReg = line.IndexOf('r');
            var regNo1 = line[IdxOfFirstReg + 1];
            temp = temp[(IdxOfFirstReg + 1)..];

            if (line.Contains("0x"))
            {
                var valStartsHere = temp.IndexOf('x');
                temp = temp[(valStartsHere + 1)..];
                temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                var val = Convert.ToInt64(temp, 16);
                CMPtemp = REG[charToInt(regNo1)] - val;

                if (Convert.ToString(val, 2).Length == Convert.ToString(REG[charToInt(regNo1)] + val, 2).Length)
                    xPSR[2] = true;
                else
                    xPSR[2] = false;
            }
            else if (temp.Contains("r"))
            {
                var IdxOfSecondReg = temp.IndexOf('r');
                var regNo2 = temp[IdxOfSecondReg + 1];

                CMPtemp = (int)(REG[charToInt(regNo1)] - REG[charToInt(regNo2)]);

                if (Convert.ToString(REG[charToInt(regNo1)], 2).Length == Convert.ToString(REG[charToInt(regNo1)] + REG[charToInt(regNo2)], 2).Length)
                    xPSR[2] = true;
                else
                    xPSR[2] = false;
            }
            // N(negative) Z(zero) C(carry/borrow) V(overflow) Q(sticky saturation)
            if (CMPtemp < 0)
                xPSR[1] = true;
            else
                xPSR[1] = false;
            if (CMPtemp == 0)
                xPSR[2] = true;
            else
                xPSR[2] = false;

            // No overflow added (was not affected in test programs)

        }
        else if (line.Contains("LSL"))
        {
            var IdxOfFirstReg = line.IndexOf('r');
            var regNo1 = line[IdxOfFirstReg + 1];
            temp = temp[(IdxOfFirstReg + 3)..];
            //Console.WriteLine(temp);

            var IdxOfSecondReg = temp.IndexOf('r');
            var regNo2 = temp[IdxOfSecondReg + 1];
            temp = temp[1..];

            var valStartsHere = temp.IndexOf('#');
            temp = temp[(valStartsHere + 1)..];

            temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
            var val = Convert.ToInt32(temp, 10);
            //Console.WriteLine(REG[charToInt(regNo2)] | val);
            var REGbefore = REG[charToInt(regNo2)];
            //Console.WriteLine("*****************************************************************************************************");
            if (REG[charToInt(regNo2)] >> val > 0xFFFFFFFF)
                throw new InvalidOperationException("REG value is over 0xFFFFFFFF");

            REG[charToInt(regNo1)] = REG[charToInt(regNo2)] << val;
            if (line.Contains("LSLS"))
            {
                if (REGbefore - REG[charToInt(regNo1)] < 0) xPSR[1] = true;
                else xPSR[1] = false;
                if (REGbefore - REG[charToInt(regNo1)] == 0) xPSR[2] = true;
                else xPSR[2] = false;
                //Console.WriteLine("Carry:{0} ",xPSR[2]);
                if (val != 0)
                {
                    if (Convert.ToString(REGbefore, 2)[^1] == '1') xPSR[2] = true;
                    else xPSR[2] = false;
                }
                //Console.WriteLine("Carry:{0} ",xPSR[2]);


            }

        }
        else if (line.Contains("LSR"))
        {
            var IdxOfFirstReg = line.IndexOf('r');
            var regNo1 = line[IdxOfFirstReg + 1];
            temp = temp[(IdxOfFirstReg + 3)..];
            //Console.WriteLine(temp);

            var IdxOfSecondReg = temp.IndexOf('r');
            var regNo2 = temp[IdxOfSecondReg + 1];
            temp = temp[1..];

            var valStartsHere = temp.IndexOf('#');
            temp = temp[(valStartsHere + 1)..];

            temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
            var val = Convert.ToInt32(temp, 10);
            //Console.WriteLine(REG[charToInt(regNo2)] | val);
            var REGbefore = REG[charToInt(regNo2)];

            REG[charToInt(regNo1)] = REG[charToInt(regNo2)] >> val;
            if (line.Contains("LSRS"))
            {
                if (REGbefore - REG[charToInt(regNo1)] < 0) xPSR[1] = true;
                else xPSR[1] = false;
                if (REGbefore - REG[charToInt(regNo1)] == 0) xPSR[2] = true;
                else xPSR[2] = false;

            }

        }
        else if (line.Contains("CBNZ"))
        {
            var IdxOfFirstReg = line.IndexOf('r');
            var regNo1 = line[IdxOfFirstReg + 1];
            temp = temp[(IdxOfFirstReg + 3)..];
            //Console.WriteLine(temp);

            var valStartsHere = temp.IndexOf(',');
            temp = temp[(valStartsHere + 1)..];

            temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));

            if (REG[charToInt(regNo1)] != 0)
                for (var i = 0; i < lines.Length; i++)
                    if (lines.Contains(temp))
                        REG[15] = (uint)i;

        }
        else if (line.Contains("CBZ"))
        {
            var IdxOfFirstReg = line.IndexOf('r');
            var regNo1 = line[IdxOfFirstReg + 1];
            temp = temp[(IdxOfFirstReg + 3)..];
            //Console.WriteLine(temp);

            var valStartsHere = temp.IndexOf(',');
            temp = temp[(valStartsHere + 1)..];

            temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));

            if (REG[charToInt(regNo1)] == 0)
                for (var i = 0; i < lines.Length; i++)
                    if (lines.Contains(temp))
                        REG[15] = (uint)i;

        }
        else if (line.Contains("VMRS"))
        {
            if (line.Contains("FPSCR"))
            {
                var IdxOfFirstReg = line.IndexOf('r');
                var regNo1 = line[IdxOfFirstReg + 1];
                var valStartsHere = temp.IndexOf(',');
                if (IdxOfFirstReg < valStartsHere)
                    REG[charToInt(regNo1)] = FPSCR;
                else if (IdxOfFirstReg > valStartsHere)
                    FPSCR = REG[charToInt(regNo1)];

            }
            else if (line.Contains("FPSID"))
            {
                var IdxOfFirstReg = line.IndexOf('r');
                var regNo1 = line[IdxOfFirstReg + 1];
                temp = temp[(IdxOfFirstReg + 3)..];
                var valStartsHere = temp.IndexOf(',');
                if (IdxOfFirstReg < valStartsHere)
                    REG[charToInt(regNo1)] = FPSID;
                else if (IdxOfFirstReg > valStartsHere)
                    FPSID = REG[charToInt(regNo1)];
            }
            else if (line.Contains("FPEXC"))
            {
                var IdxOfFirstReg = line.IndexOf('r');
                var regNo1 = line[IdxOfFirstReg + 1];
                temp = temp[(IdxOfFirstReg + 3)..];
                var valStartsHere = temp.IndexOf(',');
                if (IdxOfFirstReg < valStartsHere)
                    REG[charToInt(regNo1)] = FPEXC;
                else if (IdxOfFirstReg > valStartsHere)
                    FPEXC = REG[charToInt(regNo1)];
            }
        }
        else if (line.Contains("VMSR"))
        {
            if (line.Contains("FPSCR"))
            {
                var IdxOfFirstReg = line.IndexOf('r');
                var regNo1 = line[IdxOfFirstReg + 1];
                var valStartsHere = temp.IndexOf(',');
                if (IdxOfFirstReg < valStartsHere)
                    REG[charToInt(regNo1)] = FPSCR;
                else if (IdxOfFirstReg > valStartsHere)
                    FPSCR = REG[charToInt(regNo1)];

            }
            else if (line.Contains("FPSID"))
            {
                var IdxOfFirstReg = line.IndexOf('r');
                var regNo1 = line[IdxOfFirstReg + 1];
                temp = temp[(IdxOfFirstReg + 3)..];
                var valStartsHere = temp.IndexOf(',');
                if (IdxOfFirstReg < valStartsHere)
                    REG[charToInt(regNo1)] = FPSID;
                else if (IdxOfFirstReg > valStartsHere)
                    FPSID = REG[charToInt(regNo1)];
            }
            else if (line.Contains("FPEXC"))
            {
                var IdxOfFirstReg = line.IndexOf('r');
                var regNo1 = line[IdxOfFirstReg + 1];
                temp = temp[(IdxOfFirstReg + 3)..];
                var valStartsHere = temp.IndexOf(',');
                if (IdxOfFirstReg < valStartsHere)
                    REG[charToInt(regNo1)] = FPEXC;
                else if (IdxOfFirstReg > valStartsHere)
                    FPEXC = REG[charToInt(regNo1)];
            }
        }
        else if (line.Contains("BPL")) //Branch if PLus
        {
            if (xPSR[0] == false && xPSR[1] == false)
            {
                var valStartsHere = temp.IndexOf('x');
                temp = temp[(valStartsHere + 1)..];
                temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                var val = Convert.ToInt64(temp, 16);
                REG[15] = PC_FromKeil[val];
            }
        }
        else if (line.Contains("BMI")) //Branch if PLus
        {
            if (xPSR[0])
            {
                var valStartsHere = temp.IndexOf('x');
                temp = temp[(valStartsHere + 1)..];
                temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                var val = Convert.ToInt64(temp, 16);
                if (PC_FromKeil[val] != 0)
                    //Console.WriteLine("before change: {0}",REG[15]);
                    REG[15] = PC_FromKeil[val];
                //Console.WriteLine("after change: {0}",REG[15]);
            }
        }
        else if (line.Contains("BEQ")) //Branch if PLus 
        {

            if (xPSR[1])
            {
                var valStartsHere = temp.IndexOf('x');
                temp = temp[(valStartsHere + 1)..];
                temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                var val = Convert.ToInt64(temp, 16);
                REG[15] = PC_FromKeil[val];
            }
        }
        else if (line.Contains("BNE")) //Branch neative
        {
            if (xPSR[0])
            {
                var valStartsHere = temp.IndexOf('x');
                temp = temp[(valStartsHere + 1)..];
                temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                var val = Convert.ToInt64(temp, 16);

                //Console.WriteLine("before change: {0}",REG[15]);
                REG[15] = PC_FromKeil[val];
            }
        }
        else if (line.Contains("PUSH"))
        {
            //Optional

        }
        else if (line.Contains("SUB"))
        {
            if (line.Contains("0x"))
            {
                var IdxOfFirstReg = line.IndexOf('r');
                var regNo1 = line[IdxOfFirstReg + 1];
                temp = temp[(IdxOfFirstReg + 3)..];
                //Console.WriteLine(temp);

                var IdxOfSecondReg = temp.IndexOf('r');
                var regNo2 = temp[IdxOfSecondReg + 1];
                temp = temp[1..];

                var valStartsHere = temp.IndexOf('x');
                temp = temp[(valStartsHere + 1)..];

                temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                var val = Convert.ToInt64(temp, 16);
                //Console.WriteLine(REG[charToInt(regNo2)] | val);
                if (line.Contains("SUBS"))
                {
                    if (val - REG[charToInt(regNo2)] < 0) xPSR[1] = true;
                    else xPSR[1] = false;
                    if (val - REG[charToInt(regNo2)] == 0) xPSR[2] = true;
                    else xPSR[2] = false;
                    if (Convert.ToString(val, 2).Length == Convert.ToString(val + REG[charToInt(regNo2)], 2).Length)
                        xPSR[2] = true;
                    else
                        xPSR[2] = false;

                }

                REG[charToInt(regNo1)] = (uint)(REG[charToInt(regNo2)] - val);
            }
            else if (line.Contains("#"))
            {
                var IdxOfFirstReg = line.IndexOf('r');
                var regNo1 = line[IdxOfFirstReg + 1];
                temp = temp[(IdxOfFirstReg + 3)..];
                //Console.WriteLine(temp);

                var IdxOfSecondReg = temp.IndexOf('r');
                var regNo2 = temp[IdxOfSecondReg + 1];
                temp = temp[1..];

                var valStartsHere = temp.IndexOf('#');
                temp = temp[(valStartsHere + 1)..];

                temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                uint val;
                if (temp.Contains('x'))
                {
                    temp = temp[2..];
                    val = (uint)Convert.ToInt64(temp, 16);
                }
                else
                {
                    val = (uint)Convert.ToInt64(temp, 10);
                }
                //Console.WriteLine(REG[charToInt(regNo2)] | val);
                {
                    if (val - REG[charToInt(regNo2)] < 0) xPSR[1] = true;
                    else xPSR[1] = false;
                    if (val - REG[charToInt(regNo2)] == 0) xPSR[2] = true;
                    else xPSR[2] = false;

                }

                REG[charToInt(regNo1)] = REG[charToInt(regNo2)] - val;

            }
            else
            {
                var IdxOfFirstReg = line.IndexOf('r');
                var regNo1 = line[IdxOfFirstReg + 1];
                temp = temp[(IdxOfFirstReg + 3)..];
                //Console.WriteLine(temp);

                var IdxOfSecondReg = temp.IndexOf('r');
                var regNo2 = temp[IdxOfSecondReg + 1];
                temp = temp[1..];

                var valStartsHere = temp.IndexOf('r');
                var regNo3 = temp[valStartsHere + 1];
                {
                    if (REG[charToInt(regNo3)] - REG[charToInt(regNo2)] < 0) xPSR[1] = true;
                    else xPSR[1] = false;
                    if (REG[charToInt(regNo3)] - REG[charToInt(regNo2)] == 0) xPSR[2] = true;
                    else xPSR[2] = false;

                }

                REG[charToInt(regNo1)] = REG[charToInt(regNo2)] - REG[charToInt(regNo3)];
            }

        }
        else if (line.Contains("POP"))
        {
            //Optional
        }
        else if (line.Contains("DCW"))
        {
            //Optional

        }
        else if (line.Contains("DCD"))
        {
            //Optional
        }
        else if (line.Contains("ADD"))
        {
            if (line.Contains("0x"))
            {
                var IdxOfFirstReg = line.IndexOf('r');
                var regNo1 = line[IdxOfFirstReg + 1];
                temp = temp[(IdxOfFirstReg + 3)..];
                //Console.WriteLine(temp);

                var IdxOfSecondReg = temp.IndexOf('r');
                var regNo2 = temp[IdxOfSecondReg + 1];
                temp = temp[1..];

                var valStartsHere = temp.IndexOf('x');
                temp = temp[(valStartsHere + 1)..];

                temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                var val = Convert.ToInt64(temp, 16);
                //Console.WriteLine(REG[charToInt(regNo2)] | val);
                if (line.Contains("ADDS"))
                {
                    if (val - REG[charToInt(regNo2)] > 0) xPSR[1] = true;
                    else xPSR[1] = false;
                    if (val - REG[charToInt(regNo2)] == 0) xPSR[2] = true;
                    else xPSR[2] = false;
                    if (Convert.ToString(val, 2).Length == Convert.ToString(val + REG[charToInt(regNo2)], 2).Length)
                        xPSR[2] = true;
                    else
                        xPSR[2] = false;

                }

                REG[charToInt(regNo1)] = (uint)(REG[charToInt(regNo2)] + val);
            }
            else if (line.Contains("#"))
            {
                var IdxOfFirstReg = line.IndexOf('r');
                var regNo1 = line[IdxOfFirstReg + 1];
                temp = temp[(IdxOfFirstReg + 3)..];
                //Console.WriteLine(temp);

                var IdxOfSecondReg = temp.IndexOf('r');
                var regNo2 = temp[IdxOfSecondReg + 1];
                temp = temp[1..];

                var valStartsHere = temp.IndexOf('#');
                temp = temp[(valStartsHere + 1)..];

                temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                uint val;
                if (temp.Contains('x'))
                {
                    temp = temp[2..];
                    val = (uint)Convert.ToInt64(temp, 16);
                }
                else
                {
                    val = (uint)Convert.ToInt64(temp, 10);
                }
                //Console.WriteLine(REG[charToInt(regNo2)] | val);
                {
                    if (val - REG[charToInt(regNo2)] > 0) xPSR[1] = true;
                    else xPSR[1] = false;
                    if (val - REG[charToInt(regNo2)] == 0) xPSR[2] = true;
                    else xPSR[2] = false;

                }

                REG[charToInt(regNo1)] = REG[charToInt(regNo2)] + val;

            }
            else
            {
                var IdxOfFirstReg = line.IndexOf('r');
                var regNo1 = line[IdxOfFirstReg + 1];
                temp = temp[(IdxOfFirstReg + 3)..];
                //Console.WriteLine(temp);

                var IdxOfSecondReg = temp.IndexOf('r');
                var regNo2 = temp[IdxOfSecondReg + 1];
                temp = temp[1..];

                var valStartsHere = temp.IndexOf('r');
                var regNo3 = temp[valStartsHere + 1];
                {
                    if (REG[charToInt(regNo3)] - REG[charToInt(regNo2)] > 0) xPSR[1] = true;
                    else xPSR[1] = false;
                    if (REG[charToInt(regNo3)] - REG[charToInt(regNo2)] == 0) xPSR[2] = true;
                    else xPSR[2] = false;

                }

                REG[charToInt(regNo1)] = REG[charToInt(regNo2)] + REG[charToInt(regNo3)];

            }


        }
        else if (line.Contains("BLT"))
        {
            if (xPSR[0] == false && xPSR[4] || xPSR[0] && xPSR[4] == false)
            {
                var valStartsHere = temp.IndexOf('x');
                temp = temp[(valStartsHere + 1)..];
                temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                var val = Convert.ToInt64(temp, 16);
                if (PC_FromKeil[val] != 0)
                    //Console.WriteLine("before change: {0}",REG[15]);
                    REG[15] = PC_FromKeil[val];
                //Console.WriteLine("after change: {0}",REG[15]);
            }
        }
        else if (line.Contains("BGT"))
        {
            if (xPSR[0] == false && xPSR[1] == false && xPSR[3] == false || xPSR[0] && xPSR[1] == false && xPSR[3])
            {
                var valStartsHere = temp.IndexOf('x');
                temp = temp[(valStartsHere + 1)..];
                temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                var val = Convert.ToInt64(temp, 16);
                if (PC_FromKeil[val] != 0)
                    //Console.WriteLine("before change: {0}",REG[15]);
                    REG[15] = PC_FromKeil[val];
                //Console.WriteLine("after change: {0}",REG[15]);
            }
        }
        else if (line.Contains("BLE"))
        {
            if (xPSR[1] || xPSR[0] == false && xPSR[3] || xPSR[0] && xPSR[3] == false)
            {
                var valStartsHere = temp.IndexOf('x');
                temp = temp[(valStartsHere + 1)..];
                temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                var val = Convert.ToInt64(temp, 16);
                if (PC_FromKeil[val] != 0)
                    //Console.WriteLine("before change: {0}",REG[15]);
                    REG[15] = PC_FromKeil[val];
                //Console.WriteLine("after change: {0}",REG[15]);
            }

        }
        else if (line.Contains("ASR"))
        {
            var IdxOfFirstReg = line.IndexOf('r');
            var regNo1 = line[IdxOfFirstReg + 1];
            temp = temp[(IdxOfFirstReg + 3)..];
            //Console.WriteLine(temp);

            var IdxOfSecondReg = temp.IndexOf('r');
            var regNo2 = temp[IdxOfSecondReg + 1];
            temp = temp[1..];

            var valStartsHere = temp.IndexOf('#');
            temp = temp[(valStartsHere + 1)..];

            temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
            var val = Convert.ToInt32(temp, 10);
            var REGbefore = REG[charToInt(regNo2)];
            //Console.WriteLine("*****************************************************************************************************");

            REG[charToInt(regNo1)] = REG[charToInt(regNo2)] >> val;
            if (line.Contains("ASRS"))
            {
                if (REGbefore - REG[charToInt(regNo1)] < 0) xPSR[1] = true;
                else xPSR[1] = false;
                if (REGbefore - REG[charToInt(regNo1)] == 0) xPSR[2] = true;
                else xPSR[2] = false;
                //Console.WriteLine("Carry:{0} ",xPSR[2]);
                if (val != 0)
                {
                    if (Convert.ToString(REGbefore, 2)[^1] == '1') xPSR[2] = true;
                    else xPSR[2] = false;
                }
                if (Convert.ToString(val, 2).Length == Convert.ToString(val + REG[charToInt(regNo2)], 2).Length)
                    xPSR[2] = true;
                else
                    xPSR[2] = false;
                //Console.WriteLine("Carry:{0} ",xPSR[2]);
            }
        }
        else if (line.Contains("ADC"))
        {
            var regNo1 = '\0';
            var regNo2 = '\0';

            if (temp.Count(x => 'r' == x) == 2)
            {
                var IdxOfFirstReg = line.IndexOf('r');
                regNo1 = line[IdxOfFirstReg + 1];
                temp = temp[(IdxOfFirstReg + 3)..];
                //Console.WriteLine(temp);

                var IdxOfSecondReg = temp.IndexOf('r');
                regNo2 = temp[IdxOfSecondReg + 1];

            }
            else if (temp.Count(x => 'r' == x) == 1)
            {
                var IdxOfFirstReg = line.IndexOf('r');

                var IdxOfSecondReg = temp.IndexOf('s');

                if (IdxOfSecondReg > IdxOfFirstReg)
                {
                    regNo1 = line[IdxOfFirstReg + 1];
                    regNo2 = temp[IdxOfSecondReg + 1];
                }
                else if (IdxOfSecondReg < IdxOfFirstReg)
                {
                    regNo1 = line[IdxOfSecondReg + 1];
                    regNo2 = temp[IdxOfFirstReg + 1];
                }


            }
            else
            {
                var IdxOfFirstReg = line.IndexOf('s');
                regNo1 = line[IdxOfFirstReg + 1];
                temp = temp[(IdxOfFirstReg + 3)..];
                //Console.WriteLine(temp);

                var IdxOfSecondReg = temp.IndexOf('s');
                regNo2 = temp[IdxOfSecondReg + 1];
            }

            if (line.Contains("0x"))
            {


                var valStartsHere = temp.IndexOf('x');
                temp = temp[(valStartsHere + 1)..];

                temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                var val = Convert.ToInt64(temp, 16);
                //Console.WriteLine(REG[charToInt(regNo2)] | val);


                if (xPSR[2]) REG[charToInt(regNo1)] = (uint)(REG[charToInt(regNo2)] + val + 1);
                else REG[charToInt(regNo1)] = (uint)(REG[charToInt(regNo2)] + val);

            }
            else if (line.Contains("#"))
            {
                var valStartsHere = temp.IndexOf('#');
                temp = temp[(valStartsHere + 1)..];

                temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                uint val = val = (uint)Convert.ToInt64(temp, 10);
                //Console.WriteLine(REG[charToInt(regNo2)] | val);
                if (line.Contains("ADCS"))
                {
                    if (val - REG[charToInt(regNo2)] > 0) xPSR[1] = true;
                    else xPSR[1] = false;
                    if (val - REG[charToInt(regNo2)] == 0) xPSR[2] = true;
                    else xPSR[2] = false;
                }

                if (xPSR[2]) REG[charToInt(regNo1)] = REG[charToInt(regNo2)] + val + 1;
                else REG[charToInt(regNo1)] = REG[charToInt(regNo2)] + val;


            }
            else
            {

                var valStartsHere = temp.IndexOf('r');
                var regNo3 = temp[valStartsHere + 1];
                {
                    if (REG[charToInt(regNo3)] - REG[charToInt(regNo2)] > 0) xPSR[1] = true;
                    else xPSR[1] = false;
                    if (REG[charToInt(regNo3)] - REG[charToInt(regNo2)] == 0) xPSR[2] = true;
                    else xPSR[2] = false;

                }

                if (xPSR[2]) REG[charToInt(regNo1)] = REG[charToInt(regNo2)] + REG[charToInt(regNo3)] + 1;
                else REG[charToInt(regNo1)] = REG[charToInt(regNo2)] + REG[charToInt(regNo3)];


            }
        }
        else if (line.Contains("IT"))
        { /*
            // Z
            if (line.Contains("EQ")) xPSR[1] = true;
            if (line.Contains("NE")) xPSR[1] = false;
            // C
            if (line.Contains("CS") || line.Contains("HS")) xPSR[2] = true;
            if (line.Contains("CC") || line.Contains("LO")) xPSR[2] = false;
            //N
            if (line.Contains("MI")) xPSR[0] = true;
            if (line.Contains("PL")) xPSR[0] = false;
            // V
            if (line.Contains("VS")) xPSR[3] = true;
            if (line.Contains("VC")) xPSR[3] = false;
            // C and V
            if (line.Contains("HI"))
            {
                xPSR[1] = false;
                xPSR[2] = true;
            }
            if (line.Contains("LS"))
            {
                xPSR[1] = true;
                xPSR[2] = false;
            }
            // N and V
            if (line.Contains("GE"))
                xPSR[4] = xPSR[2];
            if (line.Contains("LT"))
                xPSR[4] = !xPSR[2];
            // Z N V
            if (line.Contains("GE"))
            {
                xPSR[1] = false;
                xPSR[4] = xPSR[2];
            }
            if (line.Contains("LT"))
            {
                xPSR[1] = true;
                xPSR[4] = !xPSR[2];
            }
            */

        }
        else if (line.Contains("CLZ"))
        {

            long c(long x)
            {
                return x < 0 ? 0 : c(x - ~x) + 1;
            } //  recursive

            var r1 = line.IndexOf('r');
            var regNo1 = line[r1 + 1];
            temp = temp[(r1 + 3)..];

            var r2 = temp.IndexOf('r');
            var regNo2 = temp[r2 + 1];

            REG[charToInt(regNo1)] = (uint)c(REG[charToInt(regNo2)]);


        }
        else if (line.Contains("BX") || line.Contains('B') && !line.Contains("BPL"))
        { //Need to fix branching


            uint val = 0;

            if (line.Contains("BX"))
            {
                if (line.Contains("lr"))
                {
                    val = REG[^2];
                    //Console.WriteLine("#####     {0}     #####",val);
                }
                else if (line.Contains('r'))
                {
                    var valStartsHere = temp.IndexOf('r');
                    var regNo = temp[valStartsHere + 1];
                    val = REG[charToInt(regNo)];
                }
            }
            else if (line.Contains('B'))
            {
                var valStartsHere = temp.IndexOf('x');
                temp = temp[(valStartsHere + 1)..];
                temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                val = (uint)Convert.ToInt64(temp, 16);


                //Console.WriteLine("before change: {0}",REG[15]);
                REG[15] = PC_FromKeil[val];
                //Console.WriteLine("after change: {0}",REG[15]);
            }

            //Console.WriteLine("-----------------------------------Current PC:{0} Next PC:{1}-----------------------------------",Convert.ToString(val-0x08000346,16),Convert.ToString(PC_FromKeil[val],10));
            //Console.WriteLine("-----------------------------------------------------------------------------------------------------------------------------");

        }
        // ".W"

    }


    public static bool[] xPSR = { false, true, true, false, false }; // N(negative) Z(zero) C(carry/borrow) V(overflow) Q(sticky saturation)
    public static uint FPSCR = 0x03000000, FPSID, FPEXC;
    public static uint[] PC_FromKeil = new uint[0x9000000];
    public static uint KeilPC = 0x08000346;
    private static void Cycles(string Instr_Line)
    {
        PC_FromKeil[KeilPC] = REG[15]; // Translating Keil PC to my PC
        //Console.WriteLine("Cycle:{0} REG:{2} Line:{1} ",Convert.ToString(KeilPC,16),Instr_Line,REG[15]);

        if (Instr_Line == "" || Instr_Line == "\n") KeilPC += 0;
        else if (Instr_Line.Contains("BEQ.W")) KeilPC += 4;
        else if (Instr_Line.Contains("NOP.W")) KeilPC += 4;
        else if (Instr_Line.Contains("ADR.W")) KeilPC += 4;
        else if (Instr_Line.Contains("ADDS")) KeilPC += 2;
        else if (Instr_Line.Contains("ASRS")) KeilPC += 2;
        else if (Instr_Line.Contains("VMSR")) KeilPC += 4;
        else if (Instr_Line.Contains("VMRS")) KeilPC += 4;
        else if (Instr_Line.Contains("CBNZ")) KeilPC += 2;
        else if (Instr_Line.Contains("LDRB")) KeilPC += 4;
        else if (Instr_Line.Contains("LSRS")) KeilPC += 2;
        else if (Instr_Line.Contains("STRB")) KeilPC += 4;
        else if (Instr_Line.Contains("ANDS")) KeilPC += 2;
        else if (Instr_Line.Contains("MOVT")) KeilPC += 4;
        else if (Instr_Line.Contains("MOVW")) KeilPC += 4;
        else if (Instr_Line.Contains("BKPT")) KeilPC += 2;
        else if (Instr_Line.Contains("PUSH")) KeilPC += 4;
        else if (Instr_Line.Contains("LSLS")) KeilPC += 2;
        else if (Instr_Line.Contains("MOVS")) KeilPC += 2;
        else if (Instr_Line.Contains("SUBS")) KeilPC += 2;
        else if (Instr_Line.Contains("BL.W")) KeilPC += 4;
        else if (Instr_Line.Contains("ADD")) KeilPC += 4;
        else if (Instr_Line.Contains("BCS")) KeilPC += 2;
        else if (Instr_Line.Contains("CMP")) KeilPC += 4;
        else if (Instr_Line.Contains("MLA")) KeilPC += 4;
        else if (Instr_Line.Contains("DCD")) KeilPC += 4;
        else if (Instr_Line.Contains("CBZ")) KeilPC += 2;
        else if (Instr_Line.Contains("CLZ")) KeilPC += 4;
        else if (Instr_Line.Contains("BEQ")) KeilPC += 2;
        else if (Instr_Line.Contains("AND")) KeilPC += 4;
        else if (Instr_Line.Contains("EOR")) KeilPC += 4;
        else if (Instr_Line.Contains("BPL")) KeilPC += 2;
        else if (Instr_Line.Contains("B.W")) KeilPC += 4;
        else if (Instr_Line.Contains("BIC")) KeilPC += 4;
        else if (Instr_Line.Contains("BLX")) KeilPC += 2;
        else if (Instr_Line.Contains("LDR")) KeilPC += 2;
        else if (Instr_Line.Contains("MOV")) KeilPC += 4;
        else if (Instr_Line.Contains("POP")) KeilPC += 4;
        else if (Instr_Line.Contains("STR")) KeilPC += 2;
        else if (Instr_Line.Contains("BHI")) KeilPC += 2;
        else if (Instr_Line.Contains("STM")) KeilPC += 4;
        else if (Instr_Line.Contains("ORR")) KeilPC += 4;
        else if (Instr_Line.Contains("TST")) KeilPC += 4;
        else if (Instr_Line.Contains("BNE")) KeilPC += 2;
        else if (Instr_Line.Contains("CMP")) KeilPC += 2;
        else if (Instr_Line.Contains("SUB")) KeilPC += 2;
        else if (Instr_Line.Contains("ADD")) KeilPC += 2;
        else if (Instr_Line.Contains("LDM")) KeilPC += 4;
        else if (Instr_Line.Contains("ADR")) KeilPC += 2;
        else if (Instr_Line.Contains("DCW")) KeilPC += 2;
        else if (Instr_Line.Contains("BX")) KeilPC += 2;
        else if (Instr_Line.Contains("IT")) KeilPC += 2;
        else if (Instr_Line.Contains('B')) KeilPC += 2;


    }

    private static int CycleTiming(string Instr_Line)
    {
        if (Instr_Line == "") return 1;
        if (Instr_Line.Contains("BEQ.W")) return 4;
        if (Instr_Line.Contains("NOP.W")) return 4;
        if (Instr_Line.Contains("ADR.W")) return 4;
        if (Instr_Line.Contains("ADDS")) return 2;
        if (Instr_Line.Contains("ASRS")) return 2;
        if (Instr_Line.Contains("VMSR")) return 4;
        if (Instr_Line.Contains("VMRS")) return 4;
        if (Instr_Line.Contains("CBNZ")) return 2;
        if (Instr_Line.Contains("LDRB")) return 4;
        if (Instr_Line.Contains("LSRS")) return 2;
        if (Instr_Line.Contains("STRB")) return 4;
        if (Instr_Line.Contains("ANDS")) return 2;
        if (Instr_Line.Contains("MOVT")) return 4;
        if (Instr_Line.Contains("MOVW")) return 4;
        if (Instr_Line.Contains("BKPT")) return 2;
        if (Instr_Line.Contains("PUSH")) return 4;
        if (Instr_Line.Contains("LSLS")) return 2;
        if (Instr_Line.Contains("MOVS")) return 2;
        if (Instr_Line.Contains("SUBS")) return 2;
        if (Instr_Line.Contains("BL.W")) return 4;
        if (Instr_Line.Contains("ADD")) return 4;
        if (Instr_Line.Contains("BCS")) return 2;
        if (Instr_Line.Contains("CMP")) return 4;
        if (Instr_Line.Contains("MLA")) return 4;
        if (Instr_Line.Contains("DCD")) return 4;
        if (Instr_Line.Contains("CBZ")) return 2;
        if (Instr_Line.Contains("CLZ")) return 4;
        if (Instr_Line.Contains("BEQ")) return 2;
        if (Instr_Line.Contains("AND")) return 4;
        if (Instr_Line.Contains("EOR")) return 4;
        if (Instr_Line.Contains("BPL")) return 2;
        if (Instr_Line.Contains("B.W")) return 4;
        if (Instr_Line.Contains("BIC")) return 4;
        if (Instr_Line.Contains("BLX")) return 2;
        if (Instr_Line.Contains("LDR")) return 2;
        if (Instr_Line.Contains("MOV")) return 4;
        if (Instr_Line.Contains("POP")) return 4;
        if (Instr_Line.Contains("STR")) return 2;
        if (Instr_Line.Contains("BHI")) return 2;
        if (Instr_Line.Contains("STM")) return 4;
        if (Instr_Line.Contains("ORR")) return 4;
        if (Instr_Line.Contains("TST")) return 4;
        if (Instr_Line.Contains("BNE")) return 2;
        if (Instr_Line.Contains("CMP")) return 2;
        if (Instr_Line.Contains("SUB")) return 2;
        if (Instr_Line.Contains("ADD")) return 2;
        if (Instr_Line.Contains("LDM")) return 4;
        if (Instr_Line.Contains("ADR")) return 2;
        if (Instr_Line.Contains("DCW")) return 2;
        if (Instr_Line.Contains("BX")) return 2;
        if (Instr_Line.Contains("IT")) return 2;
        if (Instr_Line.Contains("B")) return 2;
        return 1;
    }
    private static void SetUp(string fname)
    {
        if (LookAtInputTextBox != true)
        {


            lines = File.ReadLines(fname).ToArray();
            var yLen = lines.Length;
            REG[15] = 0;

            while (REG[15] < yLen)
            {
                Cycles(lines[REG[15]]); //Console.WriteLine(lines[REG[15]]);
                REG[15]++;
            }
            REG[15] = 0;

            #region MemorySetUp

            #endregion

        }
    }

    #endregion

    #region Pins

    #endregion

    #region Timers

    //TIM2 TIM3 TIM15 TIM16 TIM1 TIM6 TIM7
    private static bool TIM1_IsOn;
    private static readonly bool TIM2_IsOn = false;
    private static readonly bool TIM3_IsOn = false;
    private static bool TIM6_IsOn;
    private static bool TIM7_IsOn;
    private static readonly bool TIM15_IsOn = false;
    private static bool TIM16_IsOn;

    private void UpdateTimers()
    {

        Update_TIM1();
        Update_TIM6();
        Update_TIM7();
        Update_TIM16();
        Update_TIM15();
        Update_TIM3();
        Update_TIM2();

    }
    private string uintToString32(uint number)
    {

        var b = Convert.ToString(number, 2);
        var bNum = b.Length;
        for (var i = 32 - bNum; i > 0; i--)
            b = "0" + b;
        return b;
    }

    #region TIM1

    // PWM Timer 1
    private uint TIM1_PSC;
    private uint TIM1_ARR;
    private uint TIM1_CNT;
    private uint TIM1_CCMR1;
    private uint TIM1_CCER;
    private uint TIM1_CCR1;
    private uint Old_TIM1_CCR1;
    private uint TIM1_BDTR;
    private static uint TIM1_CR1;
    private uint TIM1_SR;

    public static string AreTheTimersOn()
    {
        var res = "";
        if (TIM1_CR1 != 0) res += "1";
        else res += "0";
        if (TIM2_CR1 != 0) res += "1";
        else res += "0";
        if (TIM3_CR1 != 0) res += "1";
        else res += "0";
        if (TIM6_CR1 != 0) res += "1";
        else res += "0";
        if (TIM7_CR1 != 0) res += "1";
        else res += "0";
        if (TIM15_CR1 != 0) res += "1";
        else res += "0";
        if (TIM16_CR1 != 0) res += "1";
        else res += "0";
        return res;

    }
    private void Update_TIM1()
    {
        TIM1_PSC = Memory[0x40012c28 / 4];
        TIM1_ARR = Memory[0x40012c2c / 4];
        TIM1_CR1 = Memory[0x40012c00 / 4];
        Run_TIM1();
    }
    private void UpdatePWM()
    {
        TIM1_CCR1 = LoadMem(0x40012c34);
        if (TIM1_CCR1 != Old_TIM1_CCR1)
        {
            Old_TIM1_CCR1 = TIM1_CCR1;
            RotatePWM();
        }
    }
    private async void Run_TIM1()
    {
        if (!TIM1_IsOn)
            if (TIM1_PSC != 0 && TIM1_ARR != 0 && TIM1_CR1 != 0) // Only for PB6
            {
                TIM1_IsOn = true;
                while (true)
                {
                    if (TIM1_ARR >= TIM1_CNT)
                    {
                        if (TIM1_SR == 1) TIM1_SR = 0;
                        else if (TIM1_SR == 0) TIM1_SR = 1;
                        UpdateMem(TIM1_SR, 0x40012c10);
                        TIM1_CNT = 0;
                    }
                    await Task.Delay((int)(TIM1_PSC * clockspeed));

                    TIM1_CNT++;
                }
            }
    }

    #endregion

    #region TIM7

    //  Timer 7
    private uint TIM7_PSC;
    private uint TIM7_ARR;
    private uint TIM7_CNT;
    private static uint TIM7_CR1;
    private uint TIM7_SR;

    private void Update_TIM7()
    {
        TIM7_PSC = Memory[0x40001428 / 4];
        TIM7_ARR = Memory[0x4000142c / 4];
        TIM7_CR1 = Memory[0x40001400 / 4];
        Run_TIM7();
    }
    private async void Run_TIM7()
    {
        if (!TIM7_IsOn)
            if (TIM7_PSC != 0 && TIM7_ARR != 0 && TIM7_CR1 != 0)
            {
                TIM7_IsOn = true;
                while (true)
                {

                    if (TIM7_ARR >= TIM7_CNT)
                    {
                        if (TIM7_SR == 1) TIM7_SR = 0;
                        else if (TIM7_SR == 0) TIM7_SR = 1;
                        UpdateMem(TIM7_SR, 0x40014c10);
                        TIM7_CNT = 0;
                    }
                    if (clockspeed * 1000 > (int)(TIM7_PSC * clockspeed) && (int)(TIM7_PSC * clockspeed) > 0)
                        await Task.Delay((int)(TIM7_PSC * clockspeed));
                    else
                        await Task.Delay(clockspeed);
                    TIM7_CNT++;

                }
            }
    }

    #endregion

    #region TIM6

    //  Timer 6
    private uint TIM6_PSC;
    private uint TIM6_ARR;
    private uint TIM6_CNT;
    private static uint TIM6_CR1;
    private uint TIM6_SR;

    private void Update_TIM6()
    {
        TIM6_PSC = Memory[0x40001028 / 4];
        TIM6_ARR = Memory[0x4000102c / 4];
        TIM6_CR1 = Memory[0x40001000 / 4];
        Run_TIM6();
    }
    private async void Run_TIM6()
    {
        if (!TIM6_IsOn)
            if (TIM6_PSC > 0 && TIM6_ARR != 0 && TIM6_CR1 != 0)
            {
                TIM6_IsOn = true;
                while (true)
                {
                    if (TIM6_ARR >= TIM6_CNT)
                    {
                        if (TIM6_SR == 1) TIM6_SR = 0;
                        else if (TIM6_SR == 0) TIM6_SR = 1;
                        UpdateMem(TIM6_SR, 0x40010c10);
                        TIM6_CNT = 0;
                    }
                    var temp = (int)(TIM6_PSC * clockspeed);
                    if (temp > 0)
                        await Task.Delay(temp);
                    else
                        await Task.Delay(50);

                    TIM6_CNT++;
                }
            }
    }

    #endregion

    #region TIM16

    //  Timer 16
    private uint TIM16_PSC;
    private uint TIM16_ARR;
    private uint TIM16_CNT;
    private uint TIM16_CCMR1;
    private uint TIM16_CCER;
    private uint TIM16_CCR1;
    private uint Old_TIM16_CCR1;
    private uint TIM16_BDTR;
    private static uint TIM16_CR1;
    private uint TIM16_SR;

    private void Update_TIM16()
    {
        TIM16_PSC = Memory[0x40014428 / 4];
        TIM16_ARR = Memory[0x4001442c / 4];
        TIM16_CCR1 = Memory[0x40014434 / 4];
        TIM16_CR1 = Memory[0x40014400 / 4];
        Run_TIM16();

    }
    private async void Run_TIM16()
    {
        if (!TIM16_IsOn)
            if (TIM16_PSC != 0 && TIM16_ARR != 0 && TIM16_CR1 != 0)
            {
                TIM16_IsOn = true;
                while (true)
                {

                    if (TIM16_ARR >= TIM16_CNT)
                    {
                        if (TIM16_SR == 1) TIM16_SR = 0;
                        else if (TIM16_SR == 0) TIM16_SR = 1;
                        UpdateMem(TIM16_SR, 0x40014410);
                        TIM16_CNT = 0;
                    }
                    await Task.Delay((int)(TIM16_PSC * clockspeed));

                    TIM16_CNT++;

                }
            }
    }

    #endregion

    #region TIM15

    //  Timer 15
    private uint TIM15_PSC;
    private uint TIM15_ARR;
    private uint TIM15_CNT;
    private uint TIM15_CCMR1;
    private uint TIM15_CCER;
    private uint TIM15_CCR1;
    private uint Old_TIM15_CCR1;
    private uint TIM15_BDTR;
    private static uint TIM15_CR1;
    private uint TIM15_SR;

    private void Update_TIM15()
    {
        TIM15_PSC = Memory[0x40014028 / 4];
        TIM15_ARR = Memory[0x4001402c / 4];
        TIM15_CCR1 = Memory[0x40014034 / 4];
        TIM15_CR1 = Memory[0x40014000 / 4];
        Run_TIM15();

    }
    private async void Run_TIM15()
    {
        if (!TIM15_IsOn)
            if (TIM15_PSC != 0 && TIM15_ARR != 0 && TIM15_CR1 != 0)
                while (true)
                {
                    if (TIM15_ARR >= TIM15_CNT)
                    {
                        if (TIM15_SR == 1) TIM15_SR = 0;
                        else if (TIM15_SR == 0) TIM15_SR = 1;
                        UpdateMem(TIM15_SR, 0x40014010);
                        TIM15_CNT = 0;
                    }
                    await Task.Delay((int)(TIM15_PSC * clockspeed));

                    TIM15_CNT++;
                }
    }

    #endregion

    #region TIM3

    //  Timer 3
    private uint TIM3_PSC;
    private uint TIM3_ARR;
    private uint TIM3_CNT;
    private uint TIM3_CCMR1;
    private uint TIM3_CCER;
    private uint TIM3_CCR1;
    private uint Old_TIM3_CCR1;
    private uint TIM3_BDTR;
    private static uint TIM3_CR1;
    private uint TIM3_SR;

    private void Update_TIM3()
    {
        TIM3_PSC = Memory[0x40000428 / 4];
        TIM3_ARR = Memory[0x4000042c / 4];
        TIM3_CNT = Memory[0x40000424 / 4];
        TIM3_CCR1 = Memory[0x40000434 / 4];
        TIM3_CR1 = Memory[0x40000400 / 4];
        Run_TIM3();

    }
    private async void Run_TIM3()
    {
        if (!TIM3_IsOn)
            if (TIM3_PSC != 0 && TIM3_ARR != 0 && TIM3_CR1 != 0)
                while (true)
                {
                    if (TIM3_ARR >= TIM3_CNT)
                    {
                        if (TIM3_SR == 1) TIM3_SR = 0;
                        else if (TIM3_SR == 0) TIM3_SR = 1;
                        UpdateMem(TIM3_SR, 0x40000410);
                        TIM3_CNT = 0;
                    }
                    await Task.Delay((int)(TIM3_PSC * clockspeed));

                    TIM3_CNT++;
                }
    }

    #endregion

    #region TIM2

    //  Timer 2
    private uint TIM2_PSC;
    private uint TIM2_ARR;
    private uint TIM2_CNT;
    private uint TIM2_CCMR1;
    private uint TIM2_CCER;
    private uint TIM2_CCR1;
    private uint Old_TIM2_CCR1;
    private uint TIM2_BDTR;
    private static uint TIM2_CR1;
    private uint TIM2_SR;


    private void TimerOpenButton_Click(object sender, EventArgs e)
    {
        var Tim_Win = new TimerWindow();
        Tim_Win.Show();
    }

    private void Update_TIM2()
    {
        TIM2_PSC = Memory[0x40000028 / 4];
        TIM2_ARR = Memory[0x4000002c / 4];
        TIM2_CCR1 = Memory[0x40000034 / 4];
        TIM2_CR1 = Memory[0x40000000 / 4];
        Run_TIM2();

    }


    private async void Run_TIM2()
    {
        if (!TIM2_IsOn)
            if (TIM2_PSC != 0 && TIM2_ARR != 0 && TIM2_CR1 != 0)
                while (true)
                {

                    if (TIM2_ARR >= TIM2_CNT)
                    {
                        if (TIM2_SR == 1) TIM2_SR = 0;
                        else if (TIM2_SR == 0) TIM2_SR = 1;
                        UpdateMem(TIM2_SR, 0x40000010);
                        TIM2_CNT = 0;
                    }
                    await Task.Delay((int)(TIM2_PSC * clockspeed));

                    TIM2_CNT++;
                }
    }

    #endregion

    #endregion
}