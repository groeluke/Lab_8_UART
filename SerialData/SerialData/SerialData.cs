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

        private void ReadButton_Click(object sender, EventArgs e)
        {
            SerialRead();
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

        private void ADCButton_Click(object sender, EventArgs e)
        {

        }

        private void IDLabelButton_Click(object sender, EventArgs e)
        {

        }

        private void IDButton_Click(object sender, EventArgs e)
        {

        }
    }
}