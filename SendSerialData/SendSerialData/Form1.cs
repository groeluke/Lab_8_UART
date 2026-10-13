using System.IO.Ports;
using System.Xml.Serialization;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace SerialExample
{
    public partial class SerialForm : Form
    {
        public SerialForm()
        {
            InitializeComponent();
            UpdatePortSelection();
        }

        SerialPort _serialPort = new SerialPort();
        void SerialPortSetup()
        {
            _serialPort.Close();
            _serialPort = new SerialPort();
            _serialPort.PortName = "COM6"; // Set your COM port here
            _serialPort.BaudRate = 9600; // Set your baud rate here
            _serialPort.DataBits = 8;
            _serialPort.Parity = Parity.None;
            //_serialPort.StopBits = StopBits.One;

        }

        void SerialConnect()
        {
            _serialPort.Close();
            _serialPort.Open();
        }

        void SerialSend()
        {
            _serialPort.Write(OutputTextBox.Text);
        }

        void SerialRead()
        {
            //SerialTextBox.Text = _serialPort.ReadExisting();
            byte[] RXJim = new byte[_serialPort.BytesToRead];
            int byteNumber = 0;
            _serialPort.Read(RXJim, 0, RXJim.Length);

            foreach (byte b in RXJim)
            {
                byteNumber++;
                ComListBox.Items.Add($"{byteNumber}: {b} : {b:X2} {(char)b}");
            }
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

        void TestQyAtBoard()
        {
            byte[] thingy = { 0xF0 };
            _serialPort.Write(thingy, 0, 1);
        }

        void WriteToDigitalOutputs()
        {
            // Safer conversion (prevents crashes if the text is invalid)
            if (byte.TryParse(OutputTextBox.Text, out byte result))
            {
                byte myByte = byte.Parse(OutputTextBox.Text);
                byte[] Whatsit = { 0x20, myByte };
                _serialPort.Write(Whatsit, 0, 2);
            }
            else
            {
                MessageBox.Show("Please enter a valid number between 0 and 255.");
                OutputTextBox.Text = "0"; // Reset to a default value
            }
        }

        // Event Handlers Below Here ---------------------------------------------
        private void ExitButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            SerialPortSetup();
            SerialConnect();
        }

        private void WriteButton_Click(object sender, EventArgs e)
        {
            //SerialSend();
            //TestQyAtBoard();
            WriteToDigitalOutputs();
        }

        private void ReadButton_Click(object sender, EventArgs e)
        {
            SerialRead();
        }

        private void StatusStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            // Do nothing for now
        }

        private void StatusTimer_Tick(object sender, EventArgs e)
        {
            string portName;
            int rxBuffer, txBuffer;
            if (_serialPort.IsOpen)
            {
                portName = _serialPort.PortName;
                rxBuffer = _serialPort.BytesToRead;
                txBuffer = _serialPort.BytesToWrite;
            }
            else
            {
                portName = "none";
                rxBuffer = 0;
                txBuffer = 0;
            }

            StatusLabel.Text = $"Port: {portName} tx:{txBuffer} rx:{rxBuffer}";
        }

        private void SendButton_Click(object sender, EventArgs e)
        {
            SerialSend();
        }
    }
}