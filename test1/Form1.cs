using System.ComponentModel;
using System.Text;

namespace test1;

public partial class STM32 : Form
{
    public STM32()
    {
        InitializeComponent();
    }

    #region FrontEnd
    public static string fileToRun;
    public static int ClockSpeed = 1000; // milliseconds

    public static string CurrInstru = "";
    public static string NextInstru = "";

    public static bool Pause_Bool = false;

    public  void Update_Intr()
    {
        //if(NextInstru != null )
            Update_Next_Instr_textbox(NextInstru);
        //if( CurrInstru != null )
            Update_Current_Instr_textbox(CurrInstru);
    }
    

    
    

    #endregion


    #region BackEnd
    async void Run(string fname)
    {
        REG[15] = startingPC;
        var lines = Enumerable.ToArray(File.ReadLines(fname));
        var yLen = lines.Length;

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

            // Update Instruction TextBoxes
            if (REG[15] < yLen) CurrInstru = lines[REG[15]];
            else CurrInstru = "";
            if (REG[15] < yLen - 1) NextInstru = lines[REG[15] + 1];
            else NextInstru = "";
            Update_Intr();


            await Task.Delay(ClockSpeed);
        }
    }
     async void Step(string fname)
    {
        REG[15] = startingPC;
        var lines = Enumerable.ToArray(File.ReadLines(fname));
        var yLen = lines.Length;
        if (lines.Length > startingPC && lines[startingPC] != String.Empty)
        {
            Update();
            REG[15] = startingPC;

            // Update Instruction TextBoxes
            if (REG[15] < yLen) CurrInstru = lines[REG[15]];
            else CurrInstru = "";
            if (REG[15] < yLen - 1) NextInstru = lines[REG[15] + 1];
            else NextInstru = "";
            Update_Intr();

            Running(lines[REG[15]]);
            UpdatingOutputs();
            REG[15]++;
            startingPC = REG[15];
            Update();
            await Task.Delay(ClockSpeed);
        }


    }


    public static long startingPC = 0;
    //                           0           1                    2  3  4  5  6  7  8  9 10 11 12 SP  LR  PC
    public static long[] REG = { 0x20000060, 0x20000060, 0x20000060, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
    //public static Dictionary<long, long> MemoryMap = new Dictionary<long, long>();
    public static long[] Memory = new long[0X7FEFFFFF]; // Memory[addr,value] max array size is 0X7FEFFFFF
    public static string bitTrim(string val, int len)
    {
        for (int i = len - val.Length; i > 0; i--)
        {
            val = '0' + val;
        }
        return val;
    }
    public static void UpdatingOutputs()
    {
        ODR_A_LEDs();
    }

    public static string ODR_output = "0000000000000000";
    public static void ODR_A_LEDs()
    {
        var ODR_out = "0000000000000000";
        var AHB2ENR = Convert.ToString(Memory[0x4002104c], 2);
        var MODER = Convert.ToString(Memory[0x48000000], 2);
        var PUPDR = Convert.ToString(Memory[0x4800000c], 2);
        var ODR = Convert.ToString(Memory[0x48000014], 2);

        AHB2ENR = bitTrim(AHB2ENR, 32);
        MODER = bitTrim(MODER, 32);
        PUPDR = bitTrim(PUPDR, 32);
        ODR = bitTrim(ODR, 16);

        if (AHB2ENR[^1] == '1')
        {
            for (int i = 0; i < ODR_out.Length; i++)
            {
                if (ODR[^1] == '1' && MODER[^2..] == "01" && PUPDR[^2..] == "10")
                {
                    ODR_out = ReplaceAtIndex(ODR_out, i, '1');
                }
                else
                {
                    ODR_out = ReplaceAtIndex(ODR_out, i, '0');
                }
                ODR = ODR[..^1];
                MODER = MODER[..^2];
                PUPDR = PUPDR[..^2];
            }
        }

        ODR_output = ODR_out;
    }
    public static string ODR_A_Buttons()
    {
        var ODR_out = "0000000000000000";
        var AHB2ENR = Convert.ToString(Memory[0x4002104c], 2);
        var MODER = Convert.ToString(Memory[0x48000000], 2);
        var PUPDR = Convert.ToString(Memory[0x4800000c], 2);
        var ODR = Convert.ToString(Memory[0x48000014], 2);

        AHB2ENR = bitTrim(AHB2ENR, 32);
        MODER = bitTrim(MODER, 32);
        PUPDR = bitTrim(PUPDR, 32);
        ODR = bitTrim(ODR, 16);

        if (AHB2ENR[^1] == '1')
        {
            for (int i = 0; i < ODR_out.Length; i++)
            {
                if (ODR[^1] == '1' && MODER[^2..] == "10" && PUPDR[^2..] == "10")
                {
                    ODR_out = ReplaceAtIndex(ODR_out, i, '1');
                }
                else
                {
                    ODR_out = ReplaceAtIndex(ODR_out, i, '0');
                }
                ODR = ODR[..^1];
                MODER = MODER[..^2];
                PUPDR = PUPDR[..^2];
            }
        }

        return ODR_out;
    }
    public static void UpdateMem(long value, long addr)
    {
        
        Memory[addr] = value;
    }

    public static long NOT(long num)
    {
        string b = Convert.ToString(num, 2);
        var bNum = b.Length;
        for (var i = 32 - bNum; i > 0; i--)
        {
            b = "0" + b;
        }
        for (var i = 0; i < b.Length; i++)
        {
            switch (b[i])
            {
                case '1':
                    ReplaceAtIndex(b, i, '0');
                    break;
                case '0':
                    ReplaceAtIndex(b, i, '1');
                    break;
            }

        }
        return Convert.ToInt64(b, 2);

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
            _ => 0
        };

    }
    static void Running(string line)
    {
        var temp = line;

        if (line.Contains("LDR")) //LDR
        {
            if (line.Contains('['))
            {
                int IdxOfFirstReg = line.IndexOf('r');
                var regNo1 = line[(IdxOfFirstReg + 1)];
                temp = temp[(IdxOfFirstReg + 3)..];
                int IdxOfSecondReg = temp.IndexOf('r');
                var regNo2 = temp[(IdxOfSecondReg + 1)];
                temp = temp[1..];
                int lastBracketIdx = temp.IndexOf(']');
                temp = temp[..lastBracketIdx];
                int valStartsHere = temp.IndexOf('x');
                temp = temp[(valStartsHere + 1)..];
                temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                long val = Convert.ToInt64(temp, 16);

                REG[charToInt(regNo1)] = Memory[charToInt(regNo2)];




            }

        }
        else if (line.Contains("ORR"))//ORR
        {
            int IdxOfFirstReg = line.IndexOf('r');
            var regNo1 = line[(IdxOfFirstReg + 1)];
            temp = temp[(IdxOfFirstReg + 3)..];
            //Console.WriteLine(temp);

            int IdxOfSecondReg = temp.IndexOf('r');
            var regNo2 = temp[(IdxOfSecondReg + 1)];
            temp = temp[1..];

            int valStartsHere = temp.IndexOf('x');
            temp = temp[(valStartsHere + 1)..];

            temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
            long val = Convert.ToInt64(temp, 16);
            //Console.WriteLine(REG[charToInt(regNo2)] | val);

            REG[charToInt(regNo1)] = REG[charToInt(regNo2)] | val;

        }
        else if (line.Contains("STR")) //STR
        {
            if (line.Contains('['))
            {
                int IdxOfFirstReg = line.IndexOf('r');
                var regNo1 = line[(IdxOfFirstReg + 1)];
                temp = temp[(IdxOfFirstReg + 3)..];
                int IdxOfSecondReg = temp.IndexOf('r');
                var regNo2 = temp[(IdxOfSecondReg + 1)];
                temp = temp[1..];
                int lastBracketIdx = temp.IndexOf(']');
                temp = temp[..lastBracketIdx];
                int valStartsHere = temp.IndexOf('x');
                temp = temp[(valStartsHere + 1)..];
                temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                long val = Convert.ToInt64(temp, 16);

                //Console.WriteLine(REG[charToInt(regNo1)]);
                UpdateMem(REG[charToInt(regNo1)], REG[charToInt(regNo2)]);



            }

        }
        else if (line.Contains("MOVS"))
        {
            int IdxOfFirstReg = line.IndexOf('r');
            var regNo1 = line[(IdxOfFirstReg + 1)];
            temp = temp[(IdxOfFirstReg + 3)..];



            int valStartsHere = temp.IndexOf('x');
            temp = temp[(valStartsHere + 1)..];
            temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
            for (int i = 4 - temp.Length; i > 0; i--)
            {
                temp = '0' + temp;
            }
            long val = Convert.ToInt64(temp, 16);

            var tempREG = Convert.ToString(REG[charToInt(regNo1)], 16);
            for (int i = 8 - tempREG.Length; i > 0; i--)
            {
                tempREG = "0" + tempREG;
            }
            //Console.WriteLine("--------{0}----------",tempREG[^4..] );
            REG[charToInt(regNo1)] = Convert.ToInt64(tempREG[^4..] + temp, 16);

        }
        else if (line.Contains("MOVT"))
        {
            int IdxOfFirstReg = line.IndexOf('r');
            var regNo1 = line[(IdxOfFirstReg + 1)];
            temp = temp[(IdxOfFirstReg + 3)..];



            int valStartsHere = temp.IndexOf('x');
            temp = temp[(valStartsHere + 1)..];
            temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
            long val = Convert.ToInt64(temp, 16);

            var tempREG = Convert.ToString(REG[charToInt(regNo1)], 16);
            for (int i = 8 - tempREG.Length; i > 0; i--)
            {
                tempREG = "0" + tempREG;
            }

            REG[charToInt(regNo1)] = Convert.ToInt64(temp + tempREG[^4..], 16);

        }
        else if (line.Contains("BIC"))
        {
            int IdxOfFirstReg = line.IndexOf('r');
            var regNo1 = line[(IdxOfFirstReg + 1)];
            temp = temp[(IdxOfFirstReg + 3)..];
            //Console.WriteLine(temp);

            int IdxOfSecondReg = temp.IndexOf('r');
            var regNo2 = temp[(IdxOfSecondReg + 1)];
            temp = temp[1..];

            int valStartsHere = temp.IndexOf('x');
            temp = temp[(valStartsHere + 1)..];

            temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
            long val = Convert.ToInt64(temp, 16);



            REG[charToInt(regNo1)] = REG[charToInt(regNo2)] | NOT(val);

        }
        else if (line.Contains("MOVW"))
        {
            int IdxOfFirstReg = line.IndexOf('r');
            var regNo1 = line[(IdxOfFirstReg + 1)];
            temp = temp[(IdxOfFirstReg + 3)..];

            int valStartsHere = temp.IndexOf('x');
            temp = temp[(valStartsHere + 1)..];
            temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
            long val = Convert.ToInt64(temp, 16);

            for (int i = 4 - temp.Length; i > 0; i--)
            {
                temp = '0' + temp;
            }
            var tempREG = Convert.ToString(REG[charToInt(regNo1)], 16);

            for (int i = 8 - tempREG.Length; i > 0; i--)
            {
                tempREG = '0' + tempREG;
            }
            REG[charToInt(regNo1)] = Convert.ToInt64(tempREG[..4] + temp, 16);
        }
        else if (line.Contains("MOV"))
        {
            int IdxOfFirstReg = line.IndexOf('r');
            var regNo1 = line[(IdxOfFirstReg + 1)];
            temp = temp[(IdxOfFirstReg + 3)..];



            int valStartsHere = temp.IndexOf('x');
            temp = temp[(valStartsHere + 1)..];
            temp = string.Join("", temp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
            long val = Convert.ToInt64(temp, 16);

            REG[charToInt(regNo1)] = val;
            Console.WriteLine("THIS MOV:{0}", Convert.ToString(val, 16));

        }
    }

    #endregion

    #region Pins

    #endregion

    private void Load_file_Click(object sender, EventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            fileToRun = openFileDialog.FileName;
            Update_FileName_Textbox("File Loaded");
        }
    }

    private  void StartButton_Click(object sender, EventArgs e)
    {
        if(fileToRun != null)
        {
            Run(fileToRun);
            Update_FileName_Textbox("Running");
            
        }
        else { Update_FileName_Textbox("No File Added");
            
        }
    }

    private void Reset_Button_Click(object sender, EventArgs e)
    {
        Update_FileName_Textbox("Please Wait clearing Memory");
        Pause_Bool = false;
        if (REG[15] != 0)
        {
            
            CurrInstru = "";
            NextInstru = "";
            startingPC = 0;
            ODR_output = "0000000000000000";
            Update(); Update_Intr(); UpdatingOutputs();


            for (var i = 0; i < REG.Length; i++)
            {
                if (i < 3) REG[i] = 0x20000060;
                else REG[i] = 0;
            }

            //Array.Clear(Memory, 0, Memory.Length);
            
            Update_FileName_Textbox("Reset Finished");
        }
    }


    private void Step_button_Click(object sender, EventArgs e)
    {
        if (fileToRun != null)
        {
            Step(fileToRun);
            Update_FileName_Textbox("Running");

        }
        else
        {
            Update_FileName_Textbox("No File Added");

        }
    }

    private void Restart_button_Click(object sender, EventArgs e)
    {
        System.Windows.Forms.Application.Restart();
    }
    
    private void Pause_Button_Click(object sender, EventArgs e)
    {
        if (Pause_Bool) // Pause is on
        {
            Pause_Bool = false;
        }
        else if (!Pause_Bool) // pause is off
        {
            Pause_Bool = true;
        }
        
    }
}