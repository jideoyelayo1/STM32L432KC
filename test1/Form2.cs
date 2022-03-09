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

        private void LoadTimers_Click(object sender, EventArgs e)
        {

        }
        private void GenerateTimer1()
        {
            
            //var objChart = chart.ChartAreas[0];                    
            

        }
        private async void Startloading_Tim1()
        {
            while (true)
            {
                this.TIM1_textbox.Text = "TIM1: " + Convert.ToString(STM32.LoadMem(0x40012c10)) + "\n";
                this.TIM1_textbox.Text += "\nTIM2: " + Convert.ToString(STM32.LoadMem(0x40000010)) + "\n";
                this.TIM1_textbox.Text += "\nTIM3: " + Convert.ToString(STM32.LoadMem(0x40000410)) + "\n";
                this.TIM1_textbox.Text += "\nTIM6: " + Convert.ToString(STM32.LoadMem(0x40010c10)) + "\n";
                this.TIM1_textbox.Text += "\nTIM7: " + Convert.ToString(STM32.LoadMem(0x40014c10)) + "\n";
                this.TIM1_textbox.Text += "\nTIM15: " + Convert.ToString(STM32.LoadMem(0x40014010)) + "\n";
                this.TIM1_textbox.Text += "\nTIM16: " + Convert.ToString(STM32.LoadMem(0x40014410)) + "\n";
                await Task.Delay(10);
            }
        }
    }
}
