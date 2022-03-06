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
        string[] lines;
        long yLen;
        if (LookAtInputTextBox) {
            var a = this.InputBox.Text;
             lines = a.Split("\n").ToArray();
             yLen = lines.Length;
                }else {
             lines = Enumerable.ToArray(File.ReadLines(fname));
             yLen = lines.Length;
        }

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
            Update(); UpdateButtons();

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
            Update(); UpdateButtons();
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
            Update(); UpdateButtons();
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

    #region Timers
    //TIM2 TIM3 TIM15 TIM16 TIM1 TIM6 TIM7
    void UpdateTimers()
    {
        Update_TIM1();
        Update_TIM6();
        Update_TIM7();
        Update_TIM16();
        Update_TIM15();
        Update_TIM3();
        Update_TIM2();

    }
    string LongToString32(long number)
    {

        string b = Convert.ToString(number, 2);
        var bNum = b.Length;
        for (var i = 32 - bNum; i > 0; i--)
        {
            b = "0" + b;
        }
        return b;
    }
    #region TIM1

    // PWM Timer 1
    long TIM1_PSC;
    long TIM1_ARR;
    long TIM1_CNT;
    long TIM1_CCMR1;
    long TIM1_CCER;
    long TIM1_CCR1; long Old_TIM1_CCR1;
    long TIM1_BDTR;
    long TIM1_CR1;
    long TIM1_SR;

    void Update_TIM1()
    {
         TIM1_PSC = Memory[0x40012c28];
         TIM1_ARR = Memory[0x40012c2c];
         TIM1_CNT = Memory[0x40012c24];
         TIM1_CCR1= Memory[0x40012c34];
         TIM1_CR1 = Memory[0x40012c00];
         Run_TIM1();
        if( TIM1_CCR1 != Old_TIM1_CCR1 )
        {
            RotatePWM(TIM1_CCR1);
            Old_TIM1_CCR1 = TIM1_CCR1;

        }
    }
    async void Run_TIM1()
    {
        
        if (TIM1_PSC != 0 && TIM1_ARR != 0 && TIM1_CR1 != 0&& LongToString32(Memory[0x48000420])[4..8] == "0001") // Only for PB6
        {
            Begin_TIM1:
            int freq = (int)( (TIM1_PSC * TIM1_ARR*4)/ 16000);
            TIM1_SR = 0; Memory[0x40012c10] &= 0;//Update SR
            await Task.Delay((int)(TIM1_CNT - 0) * (freq));
            TIM1_SR = 1; Memory[0x40012c10] |= 1;
            await Task.Delay((int)(TIM1_ARR-TIM1_CNT ) * (freq));
            TIM1_SR = 0; Memory[0x40012c10] &= 0;
            await Task.Delay((int)(TIM1_PSC-TIM1_ARR) * (freq));
            goto Begin_TIM1;
        }
    }

    #endregion
    #region TIM7

    // PWM Timer 1
    long TIM7_PSC;
    long TIM7_ARR;
    long TIM7_CNT;
    long TIM7_CR1;
    long TIM7_SR;

    void Update_TIM7()
    {
        TIM7_PSC = Memory[0x40001428];
        TIM7_ARR = Memory[0x4000142c];
        TIM7_CNT = Memory[0x40001424];
        TIM7_CR1 = Memory[0x40001400];
        Run_TIM7();
    }
    async void Run_TIM7()
    {

        if (TIM7_PSC != 0 && TIM7_ARR != 0 && TIM7_CR1 != 0)
        {
        Begin_TIM7:
            int freq = (int)((TIM7_PSC * TIM7_ARR * 4) / 16000);
            TIM7_SR = 0; Memory[0x40014c10] &= 0;//Update SR
            await Task.Delay((int)(TIM7_CNT - 0) * (freq));
            TIM7_SR = 1; Memory[0x40014c10] |= 1;
            await Task.Delay((int)(TIM7_ARR - TIM7_CNT) * (freq));
            TIM7_SR = 0; Memory[0x40014c10] &= 0;
            await Task.Delay((int)(TIM7_PSC - TIM7_ARR) * (freq));
            goto Begin_TIM7;
        }
    }



    #endregion
    #region TIM6

    // PWM Timer 1
    long TIM6_PSC;
    long TIM6_ARR;
    long TIM6_CNT;
    long TIM6_CR1;
    long TIM6_SR;

    void Update_TIM6()
    {
        TIM6_PSC = Memory[0x40001028];
        TIM6_ARR = Memory[0x4000102c];
        TIM6_CNT = Memory[0x40001024];
        TIM6_CR1 = Memory[0x40001000];
        Run_TIM6();
    }
    async void Run_TIM6()
    {

        if (TIM6_PSC != 0 && TIM6_ARR != 0 && TIM6_CR1 != 0)
        {
        Begin_TIM6:
            int freq = (int)((TIM6_PSC * TIM6_ARR * 4) / 16000);
            TIM6_SR = 0; Memory[0x40010c10] &= 0;//Update SR
            await Task.Delay((int)(TIM6_CNT - 0) * (freq));
            TIM6_SR = 1; Memory[0x40010c10] |= 1;
            await Task.Delay((int)(TIM6_ARR - TIM6_CNT) * (freq));
            TIM6_SR = 0; Memory[0x40010c10] &= 0;
            await Task.Delay((int)(TIM6_PSC - TIM6_ARR) * (freq));
            goto Begin_TIM6;
        }
    }



    #endregion
    #region TIM16

    // PWM Timer 1
    long TIM16_PSC;
    long TIM16_ARR;
    long TIM16_CNT;
    long TIM16_CCMR1;
    long TIM16_CCER;
    long TIM16_CCR1; long Old_TIM16_CCR1;
    long TIM16_BDTR;
    long TIM16_CR1;
    long TIM16_SR;

    void Update_TIM16()
    {
        TIM16_PSC = Memory[0x40014428];
        TIM16_ARR = Memory[0x4001442c];
        TIM16_CNT = Memory[0x40014424];
        TIM16_CCR1 = Memory[0x40014434];
        TIM16_CR1 = Memory[0x40014400];
        Run_TIM16();

    }
    async void Run_TIM16()
    {

        if (TIM16_PSC != 0 && TIM16_ARR != 0 && TIM16_CR1 != 0)
        {
        Begin_TIM16:
            int freq = (int)((TIM16_PSC * TIM16_ARR * 4) / 16000);
            TIM16_SR = 0; Memory[0x40014410] &= 0;//Update SR
            await Task.Delay((int)(TIM16_CNT - 0) * (freq));
            TIM16_SR = 1; Memory[0x40014410] |= 1;
            await Task.Delay((int)(TIM16_ARR - TIM16_CNT) * (freq));
            TIM16_SR = 0; Memory[0x40014410] &= 0;
            await Task.Delay((int)(TIM16_PSC - TIM16_ARR) * (freq));
            goto Begin_TIM16;
        }
    }
    #endregion
    #region TIM15

    // PWM Timer 1
    long TIM15_PSC;
    long TIM15_ARR;
    long TIM15_CNT;
    long TIM15_CCMR1;
    long TIM15_CCER;
    long TIM15_CCR1; long Old_TIM15_CCR1;
    long TIM15_BDTR;
    long TIM15_CR1;
    long TIM15_SR;

    void Update_TIM15()
    {
        TIM15_PSC = Memory[0x40014028];
        TIM15_ARR = Memory[0x4001402c];
        TIM15_CNT = Memory[0x40014024];
        TIM15_CCR1 = Memory[0x40014034];
        TIM15_CR1 = Memory[0x40014000];
        Run_TIM15();

    }
    async void Run_TIM15()
    {

        if (TIM15_PSC != 0 && TIM15_ARR != 0 && TIM15_CR1 != 0)
        {
        Begin_TIM15:
            int freq = (int)((TIM15_PSC * TIM15_ARR * 4) / 16000);
            TIM15_SR = 0; Memory[0x40014010] &= 0;//Update SR
            await Task.Delay((int)(TIM15_CNT - 0) * (freq));
            TIM15_SR = 1; Memory[0x40014010] |= 1;
            await Task.Delay((int)(TIM15_ARR - TIM15_CNT) * (freq));
            TIM15_SR = 0; Memory[0x40014010] &= 0;
            await Task.Delay((int)(TIM15_PSC - TIM15_ARR) * (freq));
            goto Begin_TIM15;
        }
    }
    #endregion
    #region TIM3

    // PWM Timer 1
    long TIM3_PSC;
    long TIM3_ARR;
    long TIM3_CNT;
    long TIM3_CCMR1;
    long TIM3_CCER;
    long TIM3_CCR1; long Old_TIM3_CCR1;
    long TIM3_BDTR;
    long TIM3_CR1;
    long TIM3_SR;

    void Update_TIM3()
    {
        TIM3_PSC = Memory[0x40000428];
        TIM3_ARR = Memory[0x4000042c];
        TIM3_CNT = Memory[0x40000424];
        TIM3_CCR1 = Memory[0x40000434];
        TIM3_CR1 = Memory[0x40000400];
        Run_TIM3();

    }
    async void Run_TIM3()
    {

        if (TIM3_PSC != 0 && TIM3_ARR != 0 && TIM3_CR1 != 0)
        {
        Begin_TIM3:
            int freq = (int)((TIM3_PSC * TIM3_ARR * 4) / 16000);
            TIM3_SR = 0; Memory[0x40000410] &= 0;//Update SR
            await Task.Delay((int)(TIM3_CNT - 0) * (freq));
            TIM3_SR = 1; Memory[0x40000410] |= 1;
            await Task.Delay((int)(TIM3_ARR - TIM3_CNT) * (freq));
            TIM3_SR = 0; Memory[0x40000410] &= 0;
            await Task.Delay((int)(TIM3_PSC - TIM3_ARR) * (freq));
            goto Begin_TIM3;
        }
    }
    #endregion
    #region TIM2

    // PWM Timer 1
    long TIM2_PSC;
    long TIM2_ARR;
    long TIM2_CNT;
    long TIM2_CCMR1;
    long TIM2_CCER;
    long TIM2_CCR1; long Old_TIM2_CCR1;
    long TIM2_BDTR;
    long TIM2_CR1;
    long TIM2_SR;

    void Update_TIM2()
    {
        TIM2_PSC = Memory[0x40000028];
        TIM2_ARR = Memory[0x4000002c];
        TIM2_CNT = Memory[0x40000024];
        TIM2_CCR1 = Memory[0x40000034];
        TIM2_CR1 = Memory[0x40000000];
        Run_TIM2();

    }
    async void Run_TIM2()
    {

        if (TIM2_PSC != 0 && TIM2_ARR != 0 && TIM2_CR1 != 0)
        {
        Begin_TIM2:
            int freq = (int)((TIM2_PSC * TIM2_ARR * 4) / 16000);
            TIM2_SR = 0; Memory[0x40000010] &= 0;//Update SR
            await Task.Delay((int)(TIM2_CNT - 0) * (freq));
            TIM2_SR = 1; Memory[0x40000010] |= 1;
            await Task.Delay((int)(TIM2_ARR - TIM2_CNT) * (freq));
            TIM2_SR = 0; Memory[0x40000010] &= 0;
            await Task.Delay((int)(TIM2_PSC - TIM2_ARR) * (freq));
            goto Begin_TIM2;
        }
    }
    #endregion
    #endregion



    private void Load_file_Click(object sender, EventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            LookAtInputTextBox = false;
            fileToRun = openFileDialog.FileName;
            Update_FileName_Textbox("File Loaded");
        }
    }

    private  void StartButton_Click(object sender, EventArgs e)
    {
        if(fileToRun != null && !LookAtInputTextBox)
        {
            if (LookAtInputTextBox == false)
                Run(fileToRun);
            else Run(this.InputBox.Text);
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
        if (fileToRun != null && !LookAtInputTextBox)
        {
            if (LookAtInputTextBox == false)
                Step(fileToRun);
            else Step(this.InputBox.Text);
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

    private void STM32_Load(object sender, EventArgs e)
    {

    }
    private void UpdateButtons()
    {
        string B_IDR = "";
        if (PB0_bool) B_IDR += '1';//PB0
        else B_IDR += '0';
        if (PB0_bool) B_IDR += '1';//PB1
        else B_IDR += '0';
        B_IDR += '0';//PB2
        if (PB0_bool) B_IDR += '1';//PB3
        else B_IDR += '0';
        if (PB0_bool) B_IDR += '1';//PB4
        else B_IDR += '0';
        if (PB0_bool) B_IDR += '1';//PB5
        else B_IDR += '0';
        B_IDR += '0';//PB6
        if (PB0_bool) B_IDR += '1';//PB7
        else B_IDR += '0';
        Memory[0x48000410] = Convert.ToInt64(B_IDR,16);

    }
    bool LookAtInputTextBox = false;
    private void Input_TextBox_Click(object sender, EventArgs e)
    {
        LookAtInputTextBox = true;
        Run(this.InputBox.Text);
    }
    bool PB3_bool = false;
    private void PB3_Click(object sender, EventArgs e)
    {
        if (!PB3_bool)
        {
            PB3_bool = true;
            this.PB3.BackColor = Color.Blue;
        }
        else
        {
            PB3_bool = false;
            this.PB3.BackColor = Color.Red;
        }
    }
    bool PB4_bool = false;
    private void PB4_Click(object sender, EventArgs e)
    {
        if (!PB4_bool)
        {
            PB4_bool = true;
            this.PB4.BackColor = Color.Blue;
        }
        else
        {
            PB4_bool = false;
            this.PB4.BackColor = Color.Red;
        }
    }
    bool PB5_bool = false;
    private void PB5_Click(object sender, EventArgs e)
    {
        if (!PB5_bool)
        {
            PB5_bool = true;
            this.PB5.BackColor = Color.Blue;
        }
        else
        {
            PB5_bool = false;
            this.PB5.BackColor = Color.Red;
        }
    }
    bool PB7_bool = false;
    private void PB7_Click(object sender, EventArgs e)
    {
        if (!PB7_bool)
        {
            PB7_bool = true;
            this.PB7.BackColor = Color.Blue;
        }
        else
        {
            PB7_bool = false;
            this.PB7.BackColor = Color.Red;
        }
    }
    bool PB0_bool = false;
    private void PB0_Click(object sender, EventArgs e)
    {
        if (!PB0_bool)
        {
            PB0_bool = true;
            this.PB0.BackColor = Color.Blue;
        }
        else
        {
            PB0_bool = false;
            this.PB0.BackColor = Color.Red;
        }
    }
    bool PB1_bool = false;
    private void PB1_Click(object sender, EventArgs e)
    {
        if (!PB1_bool)
        {
            PB1_bool = true;
            this.PB1.BackColor = Color.Blue;
        }
        else
        {
            PB1_bool = false;
            this.PB1.BackColor = Color.Red;
        }
    }
}