﻿using System.Diagnostics;

namespace test1;

public partial class TimerWindow : Form
{
    private bool InstrClk1, InstrClk2, InstrClk3;
    private bool TimClk1, TimClk2, TimClk3, TimClk4;
    public TimerWindow()
    {
        InitializeComponent();
        Startloading_Tim1();
    }

    private async void Startloading_Tim1()
    {
        while (true)
        {
            TIM1_textbox.Text = "TIM1: " + Convert.ToString(STM32.LoadMem(0x40012c10)) + "\n";
            TIM1_textbox.Text += "\nTIM2: " + Convert.ToString(STM32.LoadMem(0x40000010)) + "\n";
            TIM1_textbox.Text += "\nTIM3: " + Convert.ToString(STM32.LoadMem(0x40000410)) + "\n";
            TIM1_textbox.Text += "\nTIM6: " + Convert.ToString(STM32.LoadMem(0x40010c10)) + "\n";
            TIM1_textbox.Text += "\nTIM7: " + Convert.ToString(STM32.LoadMem(0x40014c10)) + "\n";
            TIM1_textbox.Text += "\nTIM15: " + Convert.ToString(STM32.LoadMem(0x40014010)) + "\n";
            TIM1_textbox.Text += "\nTIM16: " + Convert.ToString(STM32.LoadMem(0x40014410)) + "\n";
            if (STM32.AreTheTimersOn()[0] == '1')
            {
                if (STM32.LoadMem(0x40012c10) == 1) TIM_1_button.BackColor = Color.Green; //TIM1
                else TIM_1_button.BackColor = Color.DarkOrange;
            }

            if (STM32.AreTheTimersOn()[1] == '1')
            {
                if (STM32.LoadMem(0x40000010) == 1) TIM_2_button.BackColor = Color.Green; //TIM2
                else TIM_2_button.BackColor = Color.DarkOrange;
            }
            if (STM32.AreTheTimersOn()[2] == '1')
            {
                if (STM32.LoadMem(0x40000410) == 1) TIM_3_button.BackColor = Color.Green; //TIM3
                else TIM_3_button.BackColor = Color.DarkOrange;
            }
            if (STM32.AreTheTimersOn()[3] == '1')
            {
                if (STM32.LoadMem(0x40010c10) == 1) TIM_6_button.BackColor = Color.Green; //TIM6
                else TIM_6_button.BackColor = Color.DarkOrange;
            }
            if (STM32.AreTheTimersOn()[4] == '1')
            {
                if (STM32.LoadMem(0x40014c10) == 1) TIM_7_button.BackColor = Color.Green; //TIM7
                else TIM_7_button.BackColor = Color.DarkOrange;
            }
            if (STM32.AreTheTimersOn()[5] == '1')
            {
                if (STM32.LoadMem(0x40014010) == 1) TIM_15_button.BackColor = Color.Green; //TIM15
                else TIM_15_button.BackColor = Color.DarkOrange;
            }
            if (STM32.AreTheTimersOn()[6] == '1')
            {
                if (STM32.LoadMem(0x40014410) == 1) TIM_16_button.BackColor = Color.Green; //TIM16
                else TIM_16_button.BackColor = Color.DarkOrange;
            }

            UpdateSpeeds();
            await Task.Delay(20);

        }
    }

    private void Clear_Memory_box_Click(object sender, EventArgs e)
    {
        MemoryOutputTextBox.Text = "";
    }

    private void Load_This_Mem_Click(object sender, EventArgs e)
    {
        if (MemoryTextInput.Text.Count(x => x == 'x') == 1)
        {
            char[] AcceptableValues = { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', 'a', 'b', 'c', 'd', 'e', 'f', 'A', 'B', 'C', 'D', 'E', 'F' };
            var NumOfAccVals = 0;
            var temp = MemoryTextInput.Text;
            var PosOfX = temp.IndexOf('x');
            temp = temp[(PosOfX + 1)..];
            temp = string.Concat(temp.Where(c => !char.IsWhiteSpace(c)));
            foreach (var c in temp)
                if (AcceptableValues.Contains(c))
                    NumOfAccVals++;

            if (NumOfAccVals == temp.Length)
            {
                var AddressToSearch = Convert.ToInt64(temp, 16);
                if (AddressToSearch <= 0xffffffff)
                    MemoryOutputTextBox.Text += "0x" + Convert.ToString(AddressToSearch, 16) + ": " + Convert.ToString(STM32.LoadMem(AddressToSearch), 16) + "\n";
                else
                    MemoryTextInput.Text = "Invalid Input";

            }
            else
            {
                MemoryTextInput.Text = "Invalid Input";
            }


        }
        else if (MemoryTextInput.Text == "Invalid Input")
        {
            MemoryTextInput.Text = "";
        }
        else
        {
            MemoryTextInput.Text = "Invalid Input";
        }

    }

    private void Timer_clockspeed3_Click(object sender, EventArgs e)
    {
        TimClk1 = false;
        TimClk2 = false;
        TimClk3 = true;
        TimClk4 = false;
    }

    private void Timer_clockspeed4_Click(object sender, EventArgs e)
    {
        TimClk1 = false;
        TimClk2 = false;
        TimClk3 = false;
        TimClk4 = true;
    }

    private void Instru_clockspeed1_Click(object sender, EventArgs e)
    {
        InstrClk1 = true;
        InstrClk2 = false;
        InstrClk3 = false;
    }

    private void Instru_clockspeed2_Click(object sender, EventArgs e)
    {
        InstrClk1 = false;
        InstrClk2 = true;
        InstrClk3 = false;
    }

    private void Instru_clockspeed3_Click(object sender, EventArgs e)
    {
        InstrClk1 = false;
        InstrClk2 = false;
        InstrClk3 = true;
    }

    private void Timer_clockspeed2_Click(object sender, EventArgs e)
    {
        TimClk1 = false;
        TimClk2 = true;
        TimClk3 = false;
        TimClk4 = false;
    }

    private void button1_Click(object sender, EventArgs e)
    {
        string fname = null;
        var openFileDialog = new OpenFileDialog();
        if (openFileDialog.ShowDialog() == DialogResult.OK)
            fname = openFileDialog.FileName;
        if (fname != null)
            FixingfileText(fname);
    }
    private void FixingfileText(string fname)
    {
        var lines = File.ReadLines(fname).ToArray();
        var temp = new string[lines.Length];

        for (var i = 0; i < lines.Length; i++)
            if (lines[i].Length > 21 && lines[i].Contains("0x080"))
                temp[i] = lines[i][21..];
        var output = "";
        for (var i = 0; i < temp.Length; i++)
            output += temp[i] + "\n";
        for (var i = 0; i < 5; i++)
            output = output.Replace("\n\n", "\n"); //remove empty lines

        var saveFileDialog = new SaveFileDialog();
        if (saveFileDialog.ShowDialog() == DialogResult.OK)
            File.WriteAllText(saveFileDialog.FileName, output);


    }

    private void button2_Click(object sender, EventArgs e)
    {
        var url = "https://github.com/jideoyelayo1/STM32L432KC";
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
        button2.ForeColor = Color.Purple;

    }

    private void Timer_clockspeed1_Click(object sender, EventArgs e)
    {
        TimClk1 = true;
        TimClk2 = false;
        TimClk3 = false;
        TimClk4 = false;

    }

    private void UpdateSpeeds()
    {

        #region Timer Clock

        if (TimClk1)
        {
            STM32.clockspeed = 10;
            Timer_clockspeed1.BackColor = Color.Aquamarine;
            Timer_clockspeed2.BackColor = Color.White;
            Timer_clockspeed3.BackColor = Color.White;
            Timer_clockspeed4.BackColor = Color.White;
        }
        else if (TimClk2)
        {
            STM32.clockspeed = 50;
            Timer_clockspeed1.BackColor = Color.White;
            Timer_clockspeed2.BackColor = Color.Aquamarine;
            Timer_clockspeed3.BackColor = Color.White;
            Timer_clockspeed4.BackColor = Color.White;
        }
        else if (TimClk3)
        {
            STM32.clockspeed = 100;
            Timer_clockspeed1.BackColor = Color.White;
            Timer_clockspeed2.BackColor = Color.White;
            Timer_clockspeed3.BackColor = Color.Aquamarine;
            Timer_clockspeed4.BackColor = Color.White;
        }
        else if (TimClk4)
        {
            STM32.clockspeed = 200;
            Timer_clockspeed1.BackColor = Color.White;
            Timer_clockspeed2.BackColor = Color.White;
            Timer_clockspeed3.BackColor = Color.White;
            Timer_clockspeed4.BackColor = Color.Aquamarine;
        }

        #endregion

        #region Instruction Clock

        if (InstrClk1)
        {
            STM32.ClockSpeed = 10;
            Instru_clockspeed1.BackColor = Color.Plum;
            Instru_clockspeed2.BackColor = Color.White;
            Instru_clockspeed3.BackColor = Color.White;
        }
        else if (InstrClk2)
        {
            STM32.ClockSpeed = 50;
            Instru_clockspeed1.BackColor = Color.White;
            Instru_clockspeed2.BackColor = Color.Plum;
            Instru_clockspeed3.BackColor = Color.White;
        }
        else if (InstrClk3)
        {
            STM32.ClockSpeed = 100;
            Instru_clockspeed1.BackColor = Color.White;
            Instru_clockspeed2.BackColor = Color.White;
            Instru_clockspeed3.BackColor = Color.Plum;
        }

        #endregion


    }
}