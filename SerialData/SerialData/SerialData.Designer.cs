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
            OperationComboBox = new ComboBox();
            AddressTextBox = new TextBox();
            StatusStrip = new StatusStrip();
            StatusLabel = new ToolStripStatusLabel();
            StatusTimer = new System.Windows.Forms.Timer(components);
            ComListBox = new ListBox();
            SendButton = new Button();
            ModeComboBox = new ComboBox();
            DataTextBox = new TextBox();
            CountNumeric = new NumericUpDown();
            RawFrameTextBox = new TextBox();
            PreviewTextBox = new TextBox();
            WarningLabel = new Label();
            SaveNameTextBox = new TextBox();
            SaveButton = new Button();
            DeleteButton = new Button();
            ResendButton = new Button();
            ScanButton = new Button();
            ScanProgressBar = new ProgressBar();
            ScanStatusLabel = new Label();
            StatusStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)CountNumeric).BeginInit();
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
            // OperationComboBox
            // 
            OperationComboBox.FormattingEnabled = true;
            OperationComboBox.Location = new Point(12, 12);
            OperationComboBox.Name = "OperationComboBox";
            OperationComboBox.Size = new Size(248, 33);
            OperationComboBox.TabIndex = 4;
            // 
            // AddressTextBox
            // 
            AddressTextBox.Location = new Point(12, 90);
            AddressTextBox.Name = "AddressTextBox";
            AddressTextBox.Size = new Size(248, 31);
            AddressTextBox.TabIndex = 5;
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
            ComListBox.Location = new Point(266, 12);
            ComListBox.Name = "ComListBox";
            ComListBox.Size = new Size(619, 229);
            ComListBox.TabIndex = 7;
            ComListBox.SelectedIndexChanged += ComListBox_SelectedIndexChanged;
            // 
            // SendButton
            // 
            SendButton.Location = new Point(172, 373);
            SendButton.Name = "SendButton";
            SendButton.Size = new Size(152, 36);
            SendButton.TabIndex = 9;
            SendButton.Text = "Send";
            SendButton.UseVisualStyleBackColor = true;
            SendButton.Click += SendButton_Click;
            // 
            // ModeComboBox
            // 
            ModeComboBox.FormattingEnabled = true;
            ModeComboBox.Location = new Point(12, 51);
            ModeComboBox.Name = "ModeComboBox";
            ModeComboBox.Size = new Size(248, 33);
            ModeComboBox.TabIndex = 10;
            // 
            // DataTextBox
            // 
            DataTextBox.Location = new Point(12, 127);
            DataTextBox.Name = "DataTextBox";
            DataTextBox.Size = new Size(248, 31);
            DataTextBox.TabIndex = 11;
            // 
            // CountNumeric
            // 
            CountNumeric.Location = new Point(172, 337);
            CountNumeric.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            CountNumeric.Name = "CountNumeric";
            CountNumeric.Size = new Size(180, 31);
            CountNumeric.TabIndex = 12;
            // 
            // RawFrameTextBox
            // 
            RawFrameTextBox.Location = new Point(12, 164);
            RawFrameTextBox.Name = "RawFrameTextBox";
            RawFrameTextBox.Size = new Size(248, 31);
            RawFrameTextBox.TabIndex = 13;
            // 
            // PreviewTextBox
            // 
            PreviewTextBox.Location = new Point(266, 251);
            PreviewTextBox.Multiline = true;
            PreviewTextBox.Name = "PreviewTextBox";
            PreviewTextBox.ReadOnly = true;
            PreviewTextBox.Size = new Size(619, 31);
            PreviewTextBox.TabIndex = 14;
            // 
            // WarningLabel
            // 
            WarningLabel.AutoSize = true;
            WarningLabel.Location = new Point(378, 323);
            WarningLabel.Name = "WarningLabel";
            WarningLabel.Size = new Size(59, 25);
            WarningLabel.TabIndex = 15;
            WarningLabel.Text = "label1";
            // 
            // SaveNameTextBox
            // 
            SaveNameTextBox.Location = new Point(14, 201);
            SaveNameTextBox.Name = "SaveNameTextBox";
            SaveNameTextBox.Size = new Size(246, 31);
            SaveNameTextBox.TabIndex = 17;
            // 
            // SaveButton
            // 
            SaveButton.Location = new Point(12, 243);
            SaveButton.Name = "SaveButton";
            SaveButton.Size = new Size(152, 39);
            SaveButton.TabIndex = 18;
            SaveButton.Text = "Save";
            SaveButton.UseVisualStyleBackColor = true;
            // 
            // DeleteButton
            // 
            DeleteButton.Location = new Point(14, 333);
            DeleteButton.Name = "DeleteButton";
            DeleteButton.Size = new Size(152, 36);
            DeleteButton.TabIndex = 19;
            DeleteButton.Text = "Delete";
            DeleteButton.UseVisualStyleBackColor = true;
            // 
            // ResendButton
            // 
            ResendButton.Location = new Point(14, 373);
            ResendButton.Name = "ResendButton";
            ResendButton.Size = new Size(152, 36);
            ResendButton.TabIndex = 20;
            ResendButton.Text = "Resend";
            ResendButton.UseVisualStyleBackColor = true;
            // 
            // ScanButton
            // 
            ScanButton.Location = new Point(14, 288);
            ScanButton.Name = "ScanButton";
            ScanButton.Size = new Size(152, 39);
            ScanButton.TabIndex = 21;
            ScanButton.Text = "Scan";
            ScanButton.UseVisualStyleBackColor = true;
            // 
            // ScanProgressBar
            // 
            ScanProgressBar.Location = new Point(172, 293);
            ScanProgressBar.Name = "ScanProgressBar";
            ScanProgressBar.Size = new Size(200, 34);
            ScanProgressBar.TabIndex = 22;
            // 
            // ScanStatusLabel
            // 
            ScanStatusLabel.AutoSize = true;
            ScanStatusLabel.Location = new Point(378, 288);
            ScanStatusLabel.Name = "ScanStatusLabel";
            ScanStatusLabel.Size = new Size(59, 25);
            ScanStatusLabel.TabIndex = 24;
            ScanStatusLabel.Text = "label1";
            // 
            // SerialData
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(897, 450);
            Controls.Add(ScanStatusLabel);
            Controls.Add(ScanProgressBar);
            Controls.Add(ScanButton);
            Controls.Add(ResendButton);
            Controls.Add(DeleteButton);
            Controls.Add(SaveButton);
            Controls.Add(SaveNameTextBox);
            Controls.Add(WarningLabel);
            Controls.Add(PreviewTextBox);
            Controls.Add(RawFrameTextBox);
            Controls.Add(CountNumeric);
            Controls.Add(DataTextBox);
            Controls.Add(ModeComboBox);
            Controls.Add(SendButton);
            Controls.Add(ComListBox);
            Controls.Add(StatusStrip);
            Controls.Add(AddressTextBox);
            Controls.Add(OperationComboBox);
            Controls.Add(ExitButton);
            Name = "SerialData";
            Text = "Serial Data";
            StatusStrip.ResumeLayout(false);
            StatusStrip.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)CountNumeric).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button ExitButton;
        private ComboBox OperationComboBox;
        private TextBox AddressTextBox;
        private StatusStrip StatusStrip;
        private System.Windows.Forms.Timer StatusTimer;
        private ListBox ComListBox;
        private ToolStripStatusLabel StatusLabel;
        private Button SendButton;
        private ComboBox ModeComboBox;
        private TextBox DataTextBox;
        private NumericUpDown CountNumeric;
        private TextBox RawFrameTextBox;
        private TextBox PreviewTextBox;
        private Label WarningLabel;
        private TextBox SaveNameTextBox;
        private Button SaveButton;
        private Button DeleteButton;
        private Button ResendButton;
        private Button ScanButton;
        private ProgressBar ScanProgressBar;
        private Label ScanStatusLabel;
    }
}
