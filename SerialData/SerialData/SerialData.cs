using System.IO.Ports;
using System.Xml.Serialization;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace SerialData
{
    public partial class SerialData : Form
    {
        public SerialData()
        {
            InitializeComponent();
            UpdatePortSelection();
            StatusTimer.Interval = 20;             // timer ticks every 20 ms
            StatusTimer.Tick += StatusTimer_Tick;  // connect the timer to its method
            StatusTimer.Start();                   // start the timer
        }

        SerialPort _serialPort = new SerialPort();
        byte[] rxBuffer = new byte[10];    // bytes received so far
        int rxCount = 0;                   // how many bytes are in rxBuffer
        bool waiting = false;              // true while we wait for a reply
        DateTime deadline;                 // when we give up waiting

        void SerialPortSetup()
        {
            _serialPort.Close();
            _serialPort = new SerialPort();
            _serialPort.PortName = PortsComboBox.Text;   // use the port picked in the drop-down
            _serialPort.BaudRate = 9600;                 // use 4800 only for the framing error test
            _serialPort.DataBits = 8;
            _serialPort.Parity = Parity.None;
            _serialPort.StopBits = StopBits.One;
        }

        void SerialConnect()
        {
            _serialPort.Close();
            _serialPort.Open();
        }

        // Send bytes to the PIC, then start waiting for a reply
        void SendBytes(byte[] data)
        {
            if (_serialPort.IsOpen == false)
            {
                MessageBox.Show("Connect first.");
                return;
            }
            if (waiting == true)
            {
                return;                               // still waiting on the last request
            }

            _serialPort.DiscardInBuffer();            // throw away old leftover bytes
            rxCount = 0;                              // empty our receive buffer
            _serialPort.Write(data, 0, data.Length);  // send the real byte values
            AddToList("TX", data, data.Length, "");

            waiting = true;
            deadline = DateTime.Now.AddMilliseconds(500);   // wait up to 0.5 seconds
        }

        // Turn the typed hex text (like "24 49 00") into bytes and send them
        void SerialSend()
        {
            string[] parts = OutputTextBox.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            byte[] data = new byte[parts.Length];
            try
            {
                for (int i = 0; i < parts.Length; i++)
                {
                    data[i] = Convert.ToByte(parts[i], 16);   // 16 = text is hexadecimal
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Type hex like: 24 49 00");
                return;
            }
            SendBytes(data);
        }

        // ---------- showing things in the list ----------

        // Add a line: TX or RX, the bytes in hex, and a note
        void AddToList(string direction, byte[] data, int count, string note)
        {
            string hex = "";
            for (int i = 0; i < count; i++)
            {
                hex = hex + data[i].ToString("X2") + " ";   // each byte as 2 hex digits
            }
            ComListBox.Items.Add(direction + "  " + hex + " " + note);
            ComListBox.TopIndex = ComListBox.Items.Count - 1;   // scroll to the newest line
        }

        // A full reply has arrived, so decode it
        string Decode()
        {
            // ID reply: 24 49 02 33 75
            if (rxBuffer[1] == 0x49)
            {
                int id = rxBuffer[3] * 256 + rxBuffer[4];   // high byte first
                return "Device ID = 0x" + id.ToString("X4");
            }
            // ADC reply: 24 41 03 00 HH LL
            if (rxBuffer[1] == 0x41)
            {
                int adc = rxBuffer[4] * 256 + rxBuffer[5];  // 10-bit value, 0 to 1023
                ADCButton.Text = "ADC: " + adc;
                return "ADC = " + adc;
            }
            // Error reply: 24 45 02 code 00
            if (rxBuffer[1] == 0x45)
            {
                if (rxBuffer[3] == 1) return "FRAMING ERROR";
                if (rxBuffer[3] == 2) return "OVERRUN ERROR";
            }
            return "Unknown reply";
        }

        // ---------- timer: runs every 20 ms, never blocks ----------
        private void StatusTimer_Tick(object sender, EventArgs e)
        {
            string portName = "none";
            int rxWaiting = 0;
            int txWaiting = 0;

            if (_serialPort.IsOpen)
            {
                // 1. Move any new bytes into rxBuffer
                while (_serialPort.BytesToRead > 0 && rxCount < rxBuffer.Length)
                {
                    rxBuffer[rxCount] = (byte)_serialPort.ReadByte();
                    rxCount++;
                }

                // 2. Is a whole packet here? Total length = 3 + the LENGTH byte (byte 2)
                if (waiting == true && rxCount >= 3)
                {
                    int total = 3 + rxBuffer[2];
                    if (rxCount >= total)
                    {
                        AddToList("RX", rxBuffer, total, Decode());
                        rxCount = 0;
                        waiting = false;
                    }
                }

                // 3. Give up if we waited too long
                if (waiting == true && DateTime.Now > deadline)
                {
                    ComListBox.Items.Add("TIMEOUT: no reply");
                    ComListBox.TopIndex = ComListBox.Items.Count - 1;
                    waiting = false;
                }

                portName = _serialPort.PortName;
                rxWaiting = _serialPort.BytesToRead;
                txWaiting = _serialPort.BytesToWrite;
            }

            StatusLabel.Text = $"Port: {portName} tx:{txWaiting} rx:{rxWaiting}";
        }

        void ForceAFramingError()
        {
            if (_serialPort.IsOpen == false)
            {
                MessageBox.Show("Connect first.");
                return;
            }

            // Switch to wrong baud
            _serialPort.BaudRate = 4800;

            // Send one byte
            byte[] bad = { 0x00 };
            _serialPort.Write(bad, 0, 1);
            AddToList("TX", bad, 1, "Sent at 4800 to force FRAMING");

            // Switch back immediately
            _serialPort.BaudRate = 9600;

            // Clear any garbage and wait
            System.Threading.Thread.Sleep(20);
            _serialPort.DiscardInBuffer();
            rxCount = 0;

            waiting = true;
            deadline = DateTime.Now.AddMilliseconds(1000);
        }

        string[] GetSerialPorts()
        {
            return SerialPort.GetPortNames();
        }

        void UpdatePortSelection()
        {
            foreach (string port in GetSerialPorts())
            {
                PortsComboBox.Items.Add(port);
            }

            if (PortsComboBox.Items.Count > 0)
            {
                PortsComboBox.SelectedIndex = 0; // Select the first port by default
            }
        }

        // Event Handlers Below Here *****************************************
        private void ExitButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            SerialPortSetup();
            SerialConnect();
        }

        private void ReadButton_Click(object sender, EventArgs e)
        {
            // Same AN0 request as the Check ADC button
            SendBytes(new byte[] { 0x24, 0x41, 0x01, 0x00 });
        }

        private void SendButton_Click(object sender, EventArgs e)
        {
            SerialSend();    // sends the hex typed in the text box
        }

        private void ADCButton_Click(object sender, EventArgs e)
        {
            SendBytes(new byte[] { 0x24, 0x41, 0x01, 0x00 });   // AN0 request
        }

        private void IDButton_Click(object sender, EventArgs e)
        {
            SendBytes(new byte[] { 0x24, 0x49, 0x00 });          // Device ID request
        }

        private void FramingErrorButton_Click(object sender, EventArgs e)
        {
            ForceAFramingError();
        }
    }
}