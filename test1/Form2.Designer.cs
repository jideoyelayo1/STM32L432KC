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
            this.LoadTimers = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
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
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // LoadTimers
            // 
            this.LoadTimers.Location = new System.Drawing.Point(663, 390);
            this.LoadTimers.Name = "LoadTimers";
            this.LoadTimers.Size = new System.Drawing.Size(99, 29);
            this.LoadTimers.TabIndex = 23;
            this.LoadTimers.Text = "Generate Graphs";
            this.LoadTimers.UseVisualStyleBackColor = true;
            this.LoadTimers.Click += new System.EventHandler(this.LoadTimers_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(32, 250);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.Size = new System.Drawing.Size(300, 188);
            this.dataGridView1.TabIndex = 24;
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
            this.TIM1_textbox.Location = new System.Drawing.Point(356, 234);
            this.TIM1_textbox.Multiline = true;
            this.TIM1_textbox.Name = "TIM1_textbox";
            this.TIM1_textbox.Size = new System.Drawing.Size(74, 204);
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
            this.TIM_1_button.Location = new System.Drawing.Point(436, 199);
            this.TIM_1_button.Name = "TIM_1_button";
            this.TIM_1_button.Size = new System.Drawing.Size(94, 29);
            this.TIM_1_button.TabIndex = 26;
            this.TIM_1_button.Text = "Timer 1";
            this.TIM_1_button.UseVisualStyleBackColor = true;
            // 
            // TIM_2_button
            // 
            this.TIM_2_button.Location = new System.Drawing.Point(436, 234);
            this.TIM_2_button.Name = "TIM_2_button";
            this.TIM_2_button.Size = new System.Drawing.Size(94, 29);
            this.TIM_2_button.TabIndex = 27;
            this.TIM_2_button.Text = "Timer 2";
            this.TIM_2_button.UseVisualStyleBackColor = true;
            // 
            // TIM_3_button
            // 
            this.TIM_3_button.Location = new System.Drawing.Point(436, 269);
            this.TIM_3_button.Name = "TIM_3_button";
            this.TIM_3_button.Size = new System.Drawing.Size(94, 29);
            this.TIM_3_button.TabIndex = 28;
            this.TIM_3_button.Text = "Timer 3";
            this.TIM_3_button.UseVisualStyleBackColor = true;
            // 
            // TIM_6_button
            // 
            this.TIM_6_button.Location = new System.Drawing.Point(436, 304);
            this.TIM_6_button.Name = "TIM_6_button";
            this.TIM_6_button.Size = new System.Drawing.Size(94, 29);
            this.TIM_6_button.TabIndex = 29;
            this.TIM_6_button.Text = "Timer 6";
            this.TIM_6_button.UseVisualStyleBackColor = true;
            // 
            // TIM_7_button
            // 
            this.TIM_7_button.Location = new System.Drawing.Point(436, 339);
            this.TIM_7_button.Name = "TIM_7_button";
            this.TIM_7_button.Size = new System.Drawing.Size(94, 29);
            this.TIM_7_button.TabIndex = 30;
            this.TIM_7_button.Text = "Timer 7";
            this.TIM_7_button.UseVisualStyleBackColor = true;
            // 
            // TIM_15_button
            // 
            this.TIM_15_button.Location = new System.Drawing.Point(436, 374);
            this.TIM_15_button.Name = "TIM_15_button";
            this.TIM_15_button.Size = new System.Drawing.Size(94, 29);
            this.TIM_15_button.TabIndex = 31;
            this.TIM_15_button.Text = "Timer 15";
            this.TIM_15_button.UseVisualStyleBackColor = true;
            // 
            // TIM_16_button
            // 
            this.TIM_16_button.Location = new System.Drawing.Point(436, 409);
            this.TIM_16_button.Name = "TIM_16_button";
            this.TIM_16_button.Size = new System.Drawing.Size(94, 29);
            this.TIM_16_button.TabIndex = 32;
            this.TIM_16_button.Text = "Timer 16";
            this.TIM_16_button.UseVisualStyleBackColor = true;
            // 
            // TimerWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.TIM_16_button);
            this.Controls.Add(this.TIM_15_button);
            this.Controls.Add(this.TIM_7_button);
            this.Controls.Add(this.TIM_6_button);
            this.Controls.Add(this.TIM_3_button);
            this.Controls.Add(this.TIM_2_button);
            this.Controls.Add(this.TIM_1_button);
            this.Controls.Add(this.TIM1_textbox);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.LoadTimers);
            this.Name = "TimerWindow";
            this.Text = "TimerWindow";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Button LoadTimers;
        private DataGridView dataGridView1;
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
    }
}