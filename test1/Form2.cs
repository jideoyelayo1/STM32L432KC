using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Syncfusion.UI.Xaml.Charts;
using System.Windows.Forms.DataVisualization;


namespace test1
{
    public partial class TimerWindow : Form
    {
        public TimerWindow()
        {
            InitializeComponent();
            Startloading_Tim1();
        }
        SfChart chart = new SfChart() { Header = "Chart" };
        

        private void LoadTimers_Click(object sender, EventArgs e)
        {

        }
        private async void Startloading_Tim1()
        {
            while (true)
            {
                this.TIM1_textbox.Text = "TIM1: " +    Convert.ToString(STM32.LoadMem(0x40012c10)) + "\n";
                this.TIM1_textbox.Text += "\nTIM2: " + Convert.ToString(STM32.LoadMem(0x40000010)) + "\n";
                this.TIM1_textbox.Text += "\nTIM3: " + Convert.ToString(STM32.LoadMem(0x40000410)) + "\n";
                this.TIM1_textbox.Text += "\nTIM6: " + Convert.ToString(STM32.LoadMem(0x40010c10)) + "\n";
                this.TIM1_textbox.Text += "\nTIM7: " + Convert.ToString(STM32.LoadMem(0x40014c10)) + "\n";
                this.TIM1_textbox.Text += "\nTIM15: " + Convert.ToString(STM32.LoadMem(0x40014010)) + "\n";
                this.TIM1_textbox.Text += "\nTIM16: " + Convert.ToString(STM32.LoadMem(0x40014410)) + "\n";

                if(STM32.LoadMem(0x40012c10) == 1) this.TIM_1_button.BackColor = Color.Green;//TIM1
                else this.TIM_1_button.BackColor = Color.DarkOrange;
                if (STM32.LoadMem(0x40000010) == 1) this.TIM_2_button.BackColor = Color.Green;//TIM2
                else this.TIM_2_button.BackColor = Color.DarkOrange;
                if (STM32.LoadMem(0x40000410) == 1) this.TIM_3_button.BackColor = Color.Green;//TIM3
                else this.TIM_3_button.BackColor = Color.DarkOrange;
                if (STM32.LoadMem(0x40010c10) == 1) this.TIM_6_button.BackColor = Color.Green;//TIM6
                else this.TIM_6_button.BackColor = Color.DarkOrange;
                if (STM32.LoadMem(0x40014c10) == 1) this.TIM_7_button.BackColor = Color.Green;//TIM7
                else this.TIM_7_button.BackColor = Color.DarkOrange;
                if (STM32.LoadMem(0x40014010) == 1) this.TIM_15_button.BackColor = Color.Green;//TIM15
                else this.TIM_15_button.BackColor = Color.DarkOrange;
                if (STM32.LoadMem(0x40014410) == 1) this.TIM_16_button.BackColor = Color.Green;//TIM16
                else this.TIM_16_button.BackColor = Color.DarkOrange;

                await Task.Delay(10);
            }
        }

    }
}
