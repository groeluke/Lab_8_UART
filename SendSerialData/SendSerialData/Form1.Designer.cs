namespace SendSerialData
{
    partial class Form1
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
            PortsComboBox = new ComboBox();
            OutputTextBox = new TextBox();
            EnableTimerCheckBox = new CheckBox();
            ComListBox = new ListBox();
            ExitButton = new Button();
            ConnectButton = new Button();
            ReadButton = new Button();
            SendButton = new Button();
            //SuspendLayout();
            // 
            // PortsComboBox
            // 
            PortsComboBox.FormattingEnabled = true;
            PortsComboBox.Location = new Point(12, 12);
            PortsComboBox.Name = "PortsComboBox";
            PortsComboBox.Size = new Size(166, 33);
            PortsComboBox.TabIndex = 0;
            // 
            // OutputTextBox
            // 
            OutputTextBox.Location = new Point(12, 78);
            OutputTextBox.Name = "OutputTextBox";
            OutputTextBox.Size = new Size(166, 31);
            OutputTextBox.TabIndex = 1;
            // 
            // EnableTimerCheckBox
            // 
            EnableTimerCheckBox.AutoSize = true;
            EnableTimerCheckBox.Location = new Point(12, 155);
            EnableTimerCheckBox.Name = "EnableTimerCheckBox";
            EnableTimerCheckBox.Size = new Size(139, 29);
            EnableTimerCheckBox.TabIndex = 2;
            EnableTimerCheckBox.Text = "Enable Timer";
            EnableTimerCheckBox.UseVisualStyleBackColor = true;
            // 
            // ComListBox
            // 
            ComListBox.FormattingEnabled = true;
            ComListBox.Location = new Point(184, 10);
            ComListBox.Name = "ComListBox";
            ComListBox.Size = new Size(604, 329);
            ComListBox.TabIndex = 3;
            // 
            // ExitButton
            // 
            ExitButton.Location = new Point(625, 367);
            ExitButton.Name = "ExitButton";
            ExitButton.Size = new Size(163, 71);
            ExitButton.TabIndex = 4;
            ExitButton.Text = "&Exit";
            ExitButton.UseVisualStyleBackColor = true;
            ExitButton.Click += ExitButton_Click;
            // 
            // ConnectButton
            // 
            ConnectButton.Location = new Point(456, 367);
            ConnectButton.Name = "ConnectButton";
            ConnectButton.Size = new Size(163, 71);
            ConnectButton.TabIndex = 5;
            ConnectButton.Text = "&Connect";
            ConnectButton.UseVisualStyleBackColor = true;
            ConnectButton.Click += ConnectButton_Click;
            // 
            // ReadButton
            // 
            ReadButton.Location = new Point(287, 367);
            ReadButton.Name = "ReadButton";
            ReadButton.Size = new Size(163, 71);
            ReadButton.TabIndex = 6;
            ReadButton.Text = "&Read";
            ReadButton.UseVisualStyleBackColor = true;
            ReadButton.Click += ReadButton_Click;
            // 
            // SendButton
            // 
            SendButton.Location = new Point(118, 367);
            SendButton.Name = "SendButton";
            SendButton.Size = new Size(163, 71);
            SendButton.TabIndex = 7;
            SendButton.Text = "&Send";
            SendButton.UseVisualStyleBackColor = true;
            SendButton.Click += SendButton_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(SendButton);
            Controls.Add(ReadButton);
            Controls.Add(ConnectButton);
            Controls.Add(ExitButton);
            Controls.Add(ComListBox);
            Controls.Add(EnableTimerCheckBox);
            Controls.Add(OutputTextBox);
            Controls.Add(PortsComboBox);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ComboBox PortsComboBox;
        private TextBox OutputTextBox;
        private CheckBox EnableTimerCheckBox;
        private ListBox ComListBox;
        private Button ExitButton;
        private Button ConnectButton;
        private Button ReadButton;
        private Button SendButton;
    }
}
