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
            ConnectButton = new Button();
            ReadButton = new Button();
            SendButton = new Button();
            PortsComboBox = new ComboBox();
            OutputTextBox = new TextBox();
            StatusStrip = new StatusStrip();
            StatusLabel = new ToolStripStatusLabel();
            StatusTimer = new System.Windows.Forms.Timer(components);
            ComListBox = new ListBox();
            EnableTimerCheckBox = new CheckBox();
            IDButton = new Button();
            ADCButton = new Button();
            FramingErrorButton = new Button();
            StatusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // ExitButton
            // 
            ExitButton.Location = new Point(639, 360);
            ExitButton.Name = "ExitButton";
            ExitButton.Size = new Size(149, 65);
            ExitButton.TabIndex = 0;
            ExitButton.Text = "&Exit";
            ExitButton.UseVisualStyleBackColor = true;
            ExitButton.Click += ExitButton_Click;
            // 
            // ConnectButton
            // 
            ConnectButton.Location = new Point(484, 360);
            ConnectButton.Name = "ConnectButton";
            ConnectButton.Size = new Size(149, 65);
            ConnectButton.TabIndex = 1;
            ConnectButton.Text = "&Connect";
            ConnectButton.UseVisualStyleBackColor = true;
            ConnectButton.Click += ConnectButton_Click;
            // 
            // ReadButton
            // 
            ReadButton.Location = new Point(329, 360);
            ReadButton.Name = "ReadButton";
            ReadButton.Size = new Size(149, 65);
            ReadButton.TabIndex = 2;
            ReadButton.Text = "&Read";
            ReadButton.UseVisualStyleBackColor = true;
            ReadButton.Click += ReadButton_Click;
            // 
            // SendButton
            // 
            SendButton.Location = new Point(174, 360);
            SendButton.Name = "SendButton";
            SendButton.Size = new Size(149, 65);
            SendButton.TabIndex = 3;
            SendButton.Text = "&Send";
            SendButton.UseVisualStyleBackColor = true;
            SendButton.Click += SendButton_Click;
            // 
            // PortsComboBox
            // 
            PortsComboBox.FormattingEnabled = true;
            PortsComboBox.Location = new Point(12, 12);
            PortsComboBox.Name = "PortsComboBox";
            PortsComboBox.Size = new Size(182, 33);
            PortsComboBox.TabIndex = 4;
            // 
            // OutputTextBox
            // 
            OutputTextBox.Location = new Point(12, 64);
            OutputTextBox.Name = "OutputTextBox";
            OutputTextBox.Size = new Size(182, 31);
            OutputTextBox.TabIndex = 5;
            // 
            // StatusStrip
            // 
            StatusStrip.ImageScalingSize = new Size(24, 24);
            StatusStrip.Items.AddRange(new ToolStripItem[] { StatusLabel });
            StatusStrip.Location = new Point(0, 428);
            StatusStrip.Name = "StatusStrip";
            StatusStrip.Size = new Size(800, 22);
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
            ComListBox.Size = new Size(579, 304);
            ComListBox.TabIndex = 7;
            // 
            // EnableTimerCheckBox
            // 
            EnableTimerCheckBox.AutoSize = true;
            EnableTimerCheckBox.Location = new Point(12, 122);
            EnableTimerCheckBox.Name = "EnableTimerCheckBox";
            EnableTimerCheckBox.Size = new Size(139, 29);
            EnableTimerCheckBox.TabIndex = 8;
            EnableTimerCheckBox.Text = "Enable Timer";
            EnableTimerCheckBox.UseVisualStyleBackColor = true;
            // 
            // IDButton
            // 
            IDButton.Location = new Point(12, 375);
            IDButton.Name = "IDButton";
            IDButton.Size = new Size(112, 34);
            IDButton.TabIndex = 9;
            IDButton.Text = "Send ID";
            IDButton.UseVisualStyleBackColor = true;
            IDButton.Click += IDButton_Click;
            // 
            // ADCButton
            // 
            ADCButton.Location = new Point(12, 335);
            ADCButton.Name = "ADCButton";
            ADCButton.Size = new Size(112, 34);
            ADCButton.TabIndex = 11;
            ADCButton.Text = "Check ADC";
            ADCButton.UseVisualStyleBackColor = true;
            ADCButton.Click += ADCButton_Click;
            // 
            // FramingErrorButton
            // 
            FramingErrorButton.Location = new Point(12, 262);
            FramingErrorButton.Name = "FramingErrorButton";
            FramingErrorButton.Size = new Size(112, 34);
            FramingErrorButton.TabIndex = 12;
            FramingErrorButton.Text = "Framing";
            FramingErrorButton.UseVisualStyleBackColor = true;
            FramingErrorButton.Click += FramingErrorButton_Click;
            // 
            // SerialData
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(FramingErrorButton);
            Controls.Add(ADCButton);
            Controls.Add(IDButton);
            Controls.Add(EnableTimerCheckBox);
            Controls.Add(ComListBox);
            Controls.Add(StatusStrip);
            Controls.Add(OutputTextBox);
            Controls.Add(PortsComboBox);
            Controls.Add(SendButton);
            Controls.Add(ReadButton);
            Controls.Add(ConnectButton);
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
        private Button ConnectButton;
        private Button ReadButton;
        private Button SendButton;
        private ComboBox PortsComboBox;
        private TextBox OutputTextBox;
        private StatusStrip StatusStrip;
        private System.Windows.Forms.Timer StatusTimer;
        private ListBox ComListBox;
        private CheckBox EnableTimerCheckBox;
        private ToolStripStatusLabel StatusLabel;
        private Button IDButton;
        private Button ADCButton;
        private Button FramingErrorButton;
    }
}
