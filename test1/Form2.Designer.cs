using Syncfusion.UI.Xaml.Charts;
using System.Windows.Forms.DataVisualization.Charting;

namespace test1
{
    partial class TimerWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.sqlCommand1 = new Microsoft.Data.SqlClient.SqlCommand();
            this.TIM1_textbox = new System.Windows.Forms.TextBox();
            this.sqlCommand2 = new Microsoft.Data.SqlClient.SqlCommand();
            this.TIM_1_button = new System.Windows.Forms.Button();
            this.TIM_2_button = new System.Windows.Forms.Button();
            this.TIM_3_button = new System.Windows.Forms.Button();
            this.TIM_6_button = new System.Windows.Forms.Button();
            this.TIM_7_button = new System.Windows.Forms.Button();
            this.TIM_15_button = new System.Windows.Forms.Button();
            this.TIM_16_button = new System.Windows.Forms.Button();
            this.Load_This_Mem = new System.Windows.Forms.Button();
            this.MemoryTextInput = new System.Windows.Forms.TextBox();
            this.MemoryOutputTextBox = new System.Windows.Forms.RichTextBox();
            this.Clear_Memory_box = new System.Windows.Forms.Button();
            this.sqlCommand3 = new Microsoft.Data.SqlClient.SqlCommand();
            this.Instru_clockspeed3 = new System.Windows.Forms.Button();
            this.Instru_clockspeed2 = new System.Windows.Forms.Button();
            this.Instru_clockspeed1 = new System.Windows.Forms.Button();
            this.Timer_clockspeed4 = new System.Windows.Forms.Button();
            this.Timer_clockspeed3 = new System.Windows.Forms.Button();
            this.Timer_clockspeed2 = new System.Windows.Forms.Button();
            this.Timer_clockspeed1 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // sqlCommand1
            // 
            this.sqlCommand1.CommandTimeout = 30;
            this.sqlCommand1.Connection = null;
            this.sqlCommand1.Notification = null;
            this.sqlCommand1.Transaction = null;
            // 
            // TIM1_textbox
            // 
            this.TIM1_textbox.Location = new System.Drawing.Point(260, 203);
            this.TIM1_textbox.Multiline = true;
            this.TIM1_textbox.Name = "TIM1_textbox";
            this.TIM1_textbox.Size = new System.Drawing.Size(74, 239);
            this.TIM1_textbox.TabIndex = 25;
            // 
            // sqlCommand2
            // 
            this.sqlCommand2.CommandTimeout = 30;
            this.sqlCommand2.Connection = null;
            this.sqlCommand2.Notification = null;
            this.sqlCommand2.Transaction = null;
            // 
            // TIM_1_button
            // 
            this.TIM_1_button.Location = new System.Drawing.Point(340, 203);
            this.TIM_1_button.Name = "TIM_1_button";
            this.TIM_1_button.Size = new System.Drawing.Size(94, 29);
            this.TIM_1_button.TabIndex = 26;
            this.TIM_1_button.Text = "Timer 1";
            this.TIM_1_button.UseVisualStyleBackColor = true;
            // 
            // TIM_2_button
            // 
            this.TIM_2_button.Location = new System.Drawing.Point(340, 238);
            this.TIM_2_button.Name = "TIM_2_button";
            this.TIM_2_button.Size = new System.Drawing.Size(94, 29);
            this.TIM_2_button.TabIndex = 27;
            this.TIM_2_button.Text = "Timer 2";
            this.TIM_2_button.UseVisualStyleBackColor = true;
            // 
            // TIM_3_button
            // 
            this.TIM_3_button.Location = new System.Drawing.Point(340, 273);
            this.TIM_3_button.Name = "TIM_3_button";
            this.TIM_3_button.Size = new System.Drawing.Size(94, 29);
            this.TIM_3_button.TabIndex = 28;
            this.TIM_3_button.Text = "Timer 3";
            this.TIM_3_button.UseVisualStyleBackColor = true;
            // 
            // TIM_6_button
            // 
            this.TIM_6_button.Location = new System.Drawing.Point(340, 308);
            this.TIM_6_button.Name = "TIM_6_button";
            this.TIM_6_button.Size = new System.Drawing.Size(94, 29);
            this.TIM_6_button.TabIndex = 29;
            this.TIM_6_button.Text = "Timer 6";
            this.TIM_6_button.UseVisualStyleBackColor = true;
            // 
            // TIM_7_button
            // 
            this.TIM_7_button.Location = new System.Drawing.Point(340, 343);
            this.TIM_7_button.Name = "TIM_7_button";
            this.TIM_7_button.Size = new System.Drawing.Size(94, 29);
            this.TIM_7_button.TabIndex = 30;
            this.TIM_7_button.Text = "Timer 7";
            this.TIM_7_button.UseVisualStyleBackColor = true;
            // 
            // TIM_15_button
            // 
            this.TIM_15_button.Location = new System.Drawing.Point(340, 378);
            this.TIM_15_button.Name = "TIM_15_button";
            this.TIM_15_button.Size = new System.Drawing.Size(94, 29);
            this.TIM_15_button.TabIndex = 31;
            this.TIM_15_button.Text = "Timer 15";
            this.TIM_15_button.UseVisualStyleBackColor = true;
            // 
            // TIM_16_button
            // 
            this.TIM_16_button.Location = new System.Drawing.Point(340, 413);
            this.TIM_16_button.Name = "TIM_16_button";
            this.TIM_16_button.Size = new System.Drawing.Size(94, 29);
            this.TIM_16_button.TabIndex = 32;
            this.TIM_16_button.Text = "Timer 16";
            this.TIM_16_button.UseVisualStyleBackColor = true;
            // 
            // Load_This_Mem
            // 
            this.Load_This_Mem.Location = new System.Drawing.Point(154, 159);
            this.Load_This_Mem.Name = "Load_This_Mem";
            this.Load_This_Mem.Size = new System.Drawing.Size(94, 29);
            this.Load_This_Mem.TabIndex = 33;
            this.Load_This_Mem.Text = "Load This Address";
            this.Load_This_Mem.UseVisualStyleBackColor = true;
            this.Load_This_Mem.Click += new System.EventHandler(this.Load_This_Mem_Click);
            // 
            // MemoryTextInput
            // 
            this.MemoryTextInput.Location = new System.Drawing.Point(23, 161);
            this.MemoryTextInput.Name = "MemoryTextInput";
            this.MemoryTextInput.Size = new System.Drawing.Size(125, 27);
            this.MemoryTextInput.TabIndex = 34;
            // 
            // MemoryOutputTextBox
            // 
            this.MemoryOutputTextBox.Location = new System.Drawing.Point(23, 209);
            this.MemoryOutputTextBox.Name = "MemoryOutputTextBox";
            this.MemoryOutputTextBox.Size = new System.Drawing.Size(125, 118);
            this.MemoryOutputTextBox.TabIndex = 35;
            this.MemoryOutputTextBox.Text = "";
            // 
            // Clear_Memory_box
            // 
            this.Clear_Memory_box.Location = new System.Drawing.Point(154, 208);
            this.Clear_Memory_box.Name = "Clear_Memory_box";
            this.Clear_Memory_box.Size = new System.Drawing.Size(94, 29);
            this.Clear_Memory_box.TabIndex = 36;
            this.Clear_Memory_box.Text = "Clear Memory Box";
            this.Clear_Memory_box.UseVisualStyleBackColor = true;
            this.Clear_Memory_box.Click += new System.EventHandler(this.Clear_Memory_box_Click);
            // 
            // sqlCommand3
            // 
            this.sqlCommand3.CommandTimeout = 30;
            this.sqlCommand3.Connection = null;
            this.sqlCommand3.Notification = null;
            this.sqlCommand3.Transaction = null;
            // 
            // Instru_clockspeed3
            // 
            this.Instru_clockspeed3.Location = new System.Drawing.Point(467, 413);
            this.Instru_clockspeed3.Name = "Instru_clockspeed3";
            this.Instru_clockspeed3.Size = new System.Drawing.Size(209, 29);
            this.Instru_clockspeed3.TabIndex = 43;
            this.Instru_clockspeed3.Text = "Instruction Clockspeed = 100";
            this.Instru_clockspeed3.UseVisualStyleBackColor = true;
            this.Instru_clockspeed3.Click += new System.EventHandler(this.Instru_clockspeed3_Click);
            // 
            // Instru_clockspeed2
            // 
            this.Instru_clockspeed2.Location = new System.Drawing.Point(467, 378);
            this.Instru_clockspeed2.Name = "Instru_clockspeed2";
            this.Instru_clockspeed2.Size = new System.Drawing.Size(209, 29);
            this.Instru_clockspeed2.TabIndex = 42;
            this.Instru_clockspeed2.Text = "Instruction Clockspeed = 50";
            this.Instru_clockspeed2.UseVisualStyleBackColor = true;
            this.Instru_clockspeed2.Click += new System.EventHandler(this.Instru_clockspeed2_Click);
            // 
            // Instru_clockspeed1
            // 
            this.Instru_clockspeed1.Location = new System.Drawing.Point(467, 343);
            this.Instru_clockspeed1.Name = "Instru_clockspeed1";
            this.Instru_clockspeed1.Size = new System.Drawing.Size(209, 29);
            this.Instru_clockspeed1.TabIndex = 41;
            this.Instru_clockspeed1.Text = "Instruction Clockspeed = 10";
            this.Instru_clockspeed1.UseVisualStyleBackColor = true;
            this.Instru_clockspeed1.Click += new System.EventHandler(this.Instru_clockspeed1_Click);
            // 
            // Timer_clockspeed4
            // 
            this.Timer_clockspeed4.Location = new System.Drawing.Point(467, 308);
            this.Timer_clockspeed4.Name = "Timer_clockspeed4";
            this.Timer_clockspeed4.Size = new System.Drawing.Size(209, 29);
            this.Timer_clockspeed4.TabIndex = 40;
            this.Timer_clockspeed4.Text = "Timer ClockSpeed = 200";
            this.Timer_clockspeed4.UseVisualStyleBackColor = true;
            this.Timer_clockspeed4.Click += new System.EventHandler(this.Timer_clockspeed4_Click);
            // 
            // Timer_clockspeed3
            // 
            this.Timer_clockspeed3.Location = new System.Drawing.Point(467, 273);
            this.Timer_clockspeed3.Name = "Timer_clockspeed3";
            this.Timer_clockspeed3.Size = new System.Drawing.Size(209, 29);
            this.Timer_clockspeed3.TabIndex = 39;
            this.Timer_clockspeed3.Text = "Timer Clockspeed = 100";
            this.Timer_clockspeed3.UseVisualStyleBackColor = true;
            this.Timer_clockspeed3.Click += new System.EventHandler(this.Timer_clockspeed3_Click);
            // 
            // Timer_clockspeed2
            // 
            this.Timer_clockspeed2.Location = new System.Drawing.Point(467, 238);
            this.Timer_clockspeed2.Name = "Timer_clockspeed2";
            this.Timer_clockspeed2.Size = new System.Drawing.Size(209, 29);
            this.Timer_clockspeed2.TabIndex = 38;
            this.Timer_clockspeed2.Text = "Timer Clockspeed = 50";
            this.Timer_clockspeed2.UseVisualStyleBackColor = true;
            this.Timer_clockspeed2.Click += new System.EventHandler(this.Timer_clockspeed2_Click);
            // 
            // Timer_clockspeed1
            // 
            this.Timer_clockspeed1.Location = new System.Drawing.Point(467, 203);
            this.Timer_clockspeed1.Name = "Timer_clockspeed1";
            this.Timer_clockspeed1.Size = new System.Drawing.Size(209, 29);
            this.Timer_clockspeed1.TabIndex = 37;
            this.Timer_clockspeed1.Text = "Timer ClockSpeed = 10";
            this.Timer_clockspeed1.UseVisualStyleBackColor = true;
            this.Timer_clockspeed1.Click += new System.EventHandler(this.Timer_clockspeed1_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(260, 158);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(416, 29);
            this.button1.TabIndex = 44;
            this.button1.Text = "Convert Assembly Code to Readable Assembly code";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.ForeColor = System.Drawing.Color.Blue;
            this.button2.Location = new System.Drawing.Point(467, 111);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(209, 29);
            this.button2.TabIndex = 45;
            this.button2.Text = "More Information";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // TimerWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(690, 450);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.Instru_clockspeed3);
            this.Controls.Add(this.Instru_clockspeed2);
            this.Controls.Add(this.Instru_clockspeed1);
            this.Controls.Add(this.Timer_clockspeed4);
            this.Controls.Add(this.Timer_clockspeed3);
            this.Controls.Add(this.Timer_clockspeed2);
            this.Controls.Add(this.Timer_clockspeed1);
            this.Controls.Add(this.Clear_Memory_box);
            this.Controls.Add(this.MemoryOutputTextBox);
            this.Controls.Add(this.MemoryTextInput);
            this.Controls.Add(this.Load_This_Mem);
            this.Controls.Add(this.TIM_16_button);
            this.Controls.Add(this.TIM_15_button);
            this.Controls.Add(this.TIM_7_button);
            this.Controls.Add(this.TIM_6_button);
            this.Controls.Add(this.TIM_3_button);
            this.Controls.Add(this.TIM_2_button);
            this.Controls.Add(this.TIM_1_button);
            this.Controls.Add(this.TIM1_textbox);
            this.Name = "TimerWindow";
            this.Text = "TimerWindow";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private Microsoft.Data.SqlClient.SqlCommand sqlCommand1;
        private TextBox TIM1_textbox;
        private Microsoft.Data.SqlClient.SqlCommand sqlCommand2;
        private SfChart chartDisplay;
        private Button TIM_1_button;
        private Button TIM_2_button;
        private Button TIM_3_button;
        private Button TIM_6_button;
        private Button TIM_7_button;
        private Button TIM_15_button;
        private Button TIM_16_button;
        private Button Load_This_Mem;
        private TextBox MemoryTextInput;
        private RichTextBox MemoryOutputTextBox;
        private Button Clear_Memory_box;
        private Microsoft.Data.SqlClient.SqlCommand sqlCommand3;
        private Button Instru_clockspeed3;
        private Button Instru_clockspeed2;
        private Button Instru_clockspeed1;
        private Button Timer_clockspeed4;
        private Button Timer_clockspeed3;
        private Button Timer_clockspeed2;
        private Button Timer_clockspeed1;
        private Button button1;
        private Button button2;
    }
}