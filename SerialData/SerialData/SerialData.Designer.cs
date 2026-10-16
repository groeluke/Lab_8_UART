namespace SerialData
{
    partial class SerialData
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            ExitButton = new Button();
            ReadButton = new Button();
            PortsComboBox = new ComboBox();
            TempTextBox = new TextBox();
            StatusStrip = new StatusStrip();
            StatusLabel = new ToolStripStatusLabel();
            StatusTimer = new System.Windows.Forms.Timer(components);
            ComListBox = new ListBox();
            IDButton = new Button();
            ADCButton = new Button();
            ServoScrollBar = new HScrollBar();
            StatusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // ExitButton
            // 
            ExitButton.Location = new Point(736, 344);
            ExitButton.Name = "ExitButton";
            ExitButton.Size = new Size(149, 65);
            ExitButton.TabIndex = 0;
            ExitButton.Text = "&Exit";
            ExitButton.UseVisualStyleBackColor = true;
            ExitButton.Click += ExitButton_Click;
            // 
            // ReadButton
            // 
            ReadButton.Location = new Point(581, 344);
            ReadButton.Name = "ReadButton";
            ReadButton.Size = new Size(149, 65);
            ReadButton.TabIndex = 2;
            ReadButton.Text = "&Read";
            ReadButton.UseVisualStyleBackColor = true;
            ReadButton.Click += ReadButton_Click;
            // 
            // PortsComboBox
            // 
            PortsComboBox.FormattingEnabled = true;
            PortsComboBox.Location = new Point(12, 12);
            PortsComboBox.Name = "PortsComboBox";
            PortsComboBox.Size = new Size(182, 33);
            PortsComboBox.TabIndex = 4;
            // 
            // TempTextBox
            // 
            TempTextBox.Location = new Point(12, 64);
            TempTextBox.Name = "TempTextBox";
            TempTextBox.Size = new Size(182, 31);
            TempTextBox.TabIndex = 5;
            // 
            // StatusStrip
            // 
            StatusStrip.ImageScalingSize = new Size(24, 24);
            StatusStrip.Items.AddRange(new ToolStripItem[] { StatusLabel });
            StatusStrip.Location = new Point(0, 428);
            StatusStrip.Name = "StatusStrip";
            StatusStrip.Size = new Size(897, 22);
            StatusStrip.TabIndex = 6;
            StatusStrip.Text = "statusStrip1";
            // 
            // StatusLabel
            // 
            StatusLabel.Name = "StatusLabel";
            StatusLabel.Size = new Size(0, 15);
            // 
            // StatusTimer
            // 
            StatusTimer.Enabled = true;
            StatusTimer.Interval = 250;
            StatusTimer.Tick += StatusTimer_Tick;
            // 
            // ComListBox
            // 
            ComListBox.FormattingEnabled = true;
            ComListBox.Location = new Point(209, 12);
            ComListBox.Name = "ComListBox";
            ComListBox.Size = new Size(676, 304);
            ComListBox.TabIndex = 7;
            // 
            // IDButton
            // 
            IDButton.Location = new Point(463, 375);
            IDButton.Name = "IDButton";
            IDButton.Size = new Size(112, 34);
            IDButton.TabIndex = 9;
            IDButton.Text = "Send ID";
            IDButton.UseVisualStyleBackColor = true;
            IDButton.Click += IDButton_Click;
            // 
            // ADCButton
            // 
            ADCButton.Location = new Point(463, 334);
            ADCButton.Name = "ADCButton";
            ADCButton.Size = new Size(112, 34);
            ADCButton.TabIndex = 11;
            ADCButton.Text = "Check ADC";
            ADCButton.UseVisualStyleBackColor = true;
            ADCButton.Click += ADCButton_Click;
            // 
            // ServoScrollBar
            // 
            ServoScrollBar.Location = new Point(37, 357);
            ServoScrollBar.Maximum = 180;
            ServoScrollBar.Name = "ServoScrollBar";
            ServoScrollBar.Size = new Size(395, 39);
            ServoScrollBar.TabIndex = 13;
            ServoScrollBar.Scroll += ServoScrollBar_Scroll;
            // 
            // SerialData
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(897, 450);
            Controls.Add(ServoScrollBar);
            Controls.Add(ADCButton);
            Controls.Add(IDButton);
            Controls.Add(ComListBox);
            Controls.Add(StatusStrip);
            Controls.Add(TempTextBox);
            Controls.Add(PortsComboBox);
            Controls.Add(ReadButton);
            Controls.Add(ExitButton);
            Name = "SerialData";
            Text = "Serial Data";
            StatusStrip.ResumeLayout(false);
            StatusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button ExitButton;
        private Button ReadButton;
        private ComboBox PortsComboBox;
        private TextBox TempTextBox;
        private StatusStrip StatusStrip;
        private System.Windows.Forms.Timer StatusTimer;
        private ListBox ComListBox;
        private ToolStripStatusLabel StatusLabel;
        private Button IDButton;
        private Button ADCButton;
        private HScrollBar ServoScrollBar;
    }
}
