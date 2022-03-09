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
            this.TIM1_textbox.Location = new System.Drawing.Point(356, 250);
            this.TIM1_textbox.Multiline = true;
            this.TIM1_textbox.Name = "TIM1_textbox";
            this.TIM1_textbox.Size = new System.Drawing.Size(74, 188);
            this.TIM1_textbox.TabIndex = 25;
            // 
            // TimerWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
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
    }
}