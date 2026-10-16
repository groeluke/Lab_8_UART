using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SerialData
{
    public partial class SerialData : Form
    {
        private SerialPort serialPort = new SerialPort();
        private List<byte> rxBuffer = new List<byte>();
        private bool picFound = false;
        private bool waiting = false;
        private DateTime replyDeadline;
        private DateTime nextTempTime;
        private DateTime nextSearchTime;

        private bool discovering = false;
        private bool timerBusy = false;
        private bool startupServoSent = false;

        private string verifiedPort = "";

        private int lastServoDegrees = -1;

        // Change this to your measured reference voltage.
        private const double VREF = 5.00;

        public SerialData()
        {
            InitializeComponent();

            StatusTimer.Stop();
            StatusTimer.Interval = 50;

            TempTextBox.ReadOnly = true;
            TempTextBox.Text = "--- °F";

            PortsComboBox.DropDownStyle =
                ComboBoxStyle.DropDownList;

            ServoScrollBar.Minimum = 0;
            ServoScrollBar.Maximum = 180;
            ServoScrollBar.SmallChange = 1;
            ServoScrollBar.LargeChange = 1;
            ServoScrollBar.Value = 90;

            StatusLabel.Text = "Searching for PIC...";

            // The timer will search automatically.
            StatusTimer.Start();
        }

        // ------------------------------------------------
        // MAIN TIMER
        // ------------------------------------------------

        private async void StatusTimer_Tick(object sender, EventArgs e)
        {
            if (timerBusy)
                return;

            timerBusy = true;

            try
            {
                if (picFound)
                {
                    // Check whether Windows still lists the PIC's COM port.
                    bool portExists = SerialPort.GetPortNames()
                        .Any(p => string.Equals(
                            p, verifiedPort,
                            StringComparison.OrdinalIgnoreCase));

                    if (!portExists)
                    {
                        DisconnectPic();
                    }
                    else
                    {
                        ReadSerialData();

                        // Check whether communication failed.
                        if (picFound && waiting &&
                            DateTime.Now >= replyDeadline)
                        {
                            DisconnectPic();
                        }

                        // Request temperature every second.
                        if (picFound && !waiting &&
                            DateTime.Now >= nextTempTime)
                        {
                            SendRequest(
                                new byte[] { 0x24, 0x41, 0x01, 0x00 },
                                "Automatic temperature request");

                            nextTempTime =
                                DateTime.Now.AddSeconds(1);
                        }
                    }
                }

                // Automatically search again after disconnecting.
                if (!picFound && !discovering &&
                    DateTime.Now >= nextSearchTime)
                {
                    await DiscoverPic();
                }
            }
            catch (Exception ex)
            {
                AddEvent("Connection check failed: " + ex.Message);

                if (picFound)
                    DisconnectPic();
            }
            finally
            {
                timerBusy = false;
            }
        }

        // ------------------------------------------------
        // FIND THE PIC AUTOMATICALLY
        // ------------------------------------------------

        private async Task DiscoverPic()
        {
            if (discovering)
                return;

            discovering = true;
            bool found = false;

            try
            {
                string[] ports = SerialPort.GetPortNames();

                foreach (string name in ports)
                {
                    try
                    {
                        ClosePort();

                        // Recreate the serial port after a disconnect.
                        if (serialPort == null)
                            serialPort = new SerialPort();

                        serialPort.PortName = name;
                        serialPort.BaudRate = 9600;
                        serialPort.DataBits = 8;
                        serialPort.Parity = Parity.None;
                        serialPort.StopBits = StopBits.One;
                        serialPort.Handshake = Handshake.None;
                        serialPort.ReadTimeout = 100;
                        serialPort.WriteTimeout = 250;

                        serialPort.Open();
                        serialPort.DiscardInBuffer();
                        serialPort.DiscardOutBuffer();

                        rxBuffer.Clear();

                        byte[] idRequest = { 0x24, 0x49, 0x00 };
                        serialPort.Write(
                            idRequest, 0, idRequest.Length);

                        AddLog(name, "TX", idRequest,
                            "PIC ID request");

                        DateTime endTime =
                            DateTime.Now.AddMilliseconds(500);

                        bool validId = false;

                        while (DateTime.Now < endTime)
                        {
                            while (serialPort.IsOpen &&
                                   serialPort.BytesToRead > 0)
                            {
                                int count = serialPort.BytesToRead;
                                byte[] data = new byte[count];

                                int read = serialPort.Read(
                                    data, 0, data.Length);

                                for (int i = 0; i < read; i++)
                                    rxBuffer.Add(data[i]);
                            }

                            while (TryExtractPacket(
                                rxBuffer, out byte[] packet))
                            {
                                AddLog(name, "RX", packet,
                                    "Discovery response");

                                if (packet.SequenceEqual(
                                    new byte[]
                                    {
                                        0x24, 0x49, 0x02, 0x33, 0x75
                                    }))
                                {
                                    validId = true;
                                    break;
                                }
                            }

                            if (validId)
                                break;

                            // Wait without freezing the window.
                            await Task.Delay(10);
                        }

                        if (!validId)
                        {
                            AddEvent(name + " is not the PIC");
                            ClosePort();
                            continue;
                        }

                        // Correct PIC found.
                        picFound = true;
                        found = true;
                        verifiedPort = name;
                        waiting = false;
                        rxBuffer.Clear();

                        PortsComboBox.Items.Clear();
                        PortsComboBox.Items.Add(name);
                        PortsComboBox.SelectedIndex = 0;

                        AddEvent("PIC verified on " + name);

                        // Send the centered command only once
                        // during this application's lifetime.
                        if (!startupServoSent)
                        {
                            startupServoSent = true;
                            SendServo(90);
                        }

                        nextTempTime =
                            DateTime.Now.AddSeconds(1);

                        break;
                    }
                    catch (Exception ex)
                    {
                        AddEvent(name + " unavailable: " + ex.Message);
                        ClosePort();
                    }
                }

                if (!found)
                {
                    picFound = false;
                    verifiedPort = "";
                    TempTextBox.Text = "--- °F";
                    nextSearchTime =
                        DateTime.Now.AddSeconds(1);
                }
            }
            finally
            {
                discovering = false;
                UpdateStatus();
            }
        }

        // ------------------------------------------------
        // SEND REQUESTS THAT EXPECT A REPLY
        // ------------------------------------------------

        private void SendRequest(byte[] packet, string meaning)
        {
            if (!picFound || !serialPort.IsOpen || waiting)
                return;

            try
            {
                serialPort.Write(packet, 0, packet.Length);

                AddLog(verifiedPort, "TX", packet, meaning);

                waiting = true;
                replyDeadline =
                    DateTime.Now.AddMilliseconds(500);
            }
            catch (Exception ex)
            {
                AddEvent("Send failed: " + ex.Message);
                DisconnectPic();
            }
        }

        // ------------------------------------------------
        // SERVO
        // ------------------------------------------------

        private void SendServo(int degrees)
        {
            if (!picFound || !serialPort.IsOpen)
                return;

            degrees = Math.Max(0, Math.Min(180, degrees));

            if (degrees == lastServoDegrees)
                return;

            // 0 degrees = 500 us
            // 90 degrees = 1500 us
            // 180 degrees = 2500 us
            int pulseWidth = 500 +
                (int)Math.Round(degrees * (2000.0 / 180.0));

            int match = 0xB1E0 + pulseWidth;

            byte[] packet =
            {
                0x24,
                0x53,
                0x02,
                (byte)(match >> 8),
                (byte)(match & 0xFF)
            };

            try
            {
                serialPort.Write(packet, 0, packet.Length);

                AddLog(verifiedPort, "TX", packet,
                    "Servo " + degrees + " degrees, " +
                    pulseWidth + " us");

                lastServoDegrees = degrees;
            }
            catch (Exception ex)
            {
                AddEvent("Servo send failed: " + ex.Message);
                DisconnectPic();
            }
        }

        // ------------------------------------------------
        // RECEIVE AND PROCESS SERIAL DATA
        // ------------------------------------------------

        private void ReadSerialData()
        {
            try
            {
                while (serialPort.IsOpen &&
                       serialPort.BytesToRead > 0)
                {
                    int count = serialPort.BytesToRead;
                    byte[] data = new byte[count];

                    int read = serialPort.Read(
                        data, 0, data.Length);

                    for (int i = 0; i < read; i++)
                        rxBuffer.Add(data[i]);
                }

                while (TryExtractPacket(
                    rxBuffer, out byte[] packet))
                {
                    string meaning = DecodePacket(packet);

                    AddLog(verifiedPort, "RX", packet, meaning);

                    if (waiting)
                    {
                        waiting = false;
                    }
                }
            }
            catch (Exception ex)
            {
                AddEvent("Receive failed: " + ex.Message);
                DisconnectPic();
            }
        }

        // Packet format: 24 COMMAND LENGTH PAYLOAD

        private bool TryExtractPacket(
            List<byte> buffer, out byte[] packet)
        {
            packet = Array.Empty<byte>();

            // Discard noise until the start byte is found.
            while (buffer.Count > 0 && buffer[0] != 0x24)
                buffer.RemoveAt(0);

            if (buffer.Count < 3)
                return false;

            int totalLength = 3 + buffer[2];

            if (totalLength > 64)
            {
                buffer.RemoveAt(0);
                return false;
            }

            if (buffer.Count < totalLength)
                return false;

            packet = buffer.GetRange(0, totalLength).ToArray();
            buffer.RemoveRange(0, totalLength);

            return true;
        }

        private string DecodePacket(byte[] packet)
        {
            if (packet.Length < 3 ||
                packet.Length != 3 + packet[2])
            {
                return "Invalid packet";
            }

            byte command = packet[1];

            // Device ID response.
            if (command == 0x49)
            {
                return "PIC ID response";
            }

            // ADC / LM34 response: 24 41 03 00 HH LL
            if (command == 0x41 && packet.Length == 6)
            {
                int channel = packet[3];

                int adc =
                    ((packet[4] & 0x03) << 8) | packet[5];

                if (channel == 0)
                {
                    double volts = adc * VREF / 1024.0;
                    double tempF = volts / 0.010;

                    TempTextBox.Text =
                        tempF.ToString("F1") + " °F";

                    return "ADC=" + adc +
                        ", sensor=" + volts.ToString("F3") +
                        " V, temperature=" + tempF.ToString("F1") +
                        " °F";
                }

                return "ADC channel " + channel;
            }

            // PIC error response.
            if (command == 0x45)
            {
                if (packet.Length >= 5)
                {
                    if (packet[3] == 1)
                        return "Framing error";

                    if (packet[3] == 2)
                        return "Overrun error";

                    if (packet[3] == 3)
                        return "Servo range error";
                }

                return "PIC error response";
            }

            return "Unknown command";
        }

        // ------------------------------------------------
        // DISCONNECTION AND RECONNECTION
        // ------------------------------------------------

        private void DisconnectPic()
        {
            bool wasConnected = picFound;

            picFound = false;
            waiting = false;
            lastServoDegrees = -1;
            rxBuffer.Clear();

            TempTextBox.Text = "--- °F";
            PortsComboBox.SelectedIndex = -1;
            UpdateStatus();

            try
            {
                if (serialPort != null)
                {
                    if (serialPort.IsOpen)
                        serialPort.Close();

                    serialPort.Dispose();
                    serialPort = null;
                }
            }
            catch
            {
                serialPort = null;
            }

            nextSearchTime = DateTime.Now.AddSeconds(1);

            if (wasConnected)
                AddLog("PIC disconnected. Searching for device...");
        }

        private void ClosePort()
        {
            try
            {
                if (serialPort != null && serialPort.IsOpen)
                    serialPort.Close();
            }
            catch
            {
                // The adapter may already be unplugged.
            }
        }

        private void UpdateStatus()
        {
            if (picFound)
                StatusLabel.Text = "Connected: " + verifiedPort;
            else
                StatusLabel.Text = "Disconnected - Searching...";
        }

        private void AddLog(string message)
        {
            if (ComListBox.InvokeRequired)
            {
                ComListBox.BeginInvoke(new Action(() => AddLog(message)));
                return;
            }

            ComListBox.Items.Add(
                DateTime.Now.ToString("HH:mm:ss") + " " + message);

            // Keep the newest message visible.
            if (ComListBox.Items.Count > 0)
                ComListBox.TopIndex = ComListBox.Items.Count - 1;
        }
        // ------------------------------------------------
        // LOGGING
        // ------------------------------------------------

        private void AddLog(
            string portName, string direction,
            byte[] data, string meaning)
        {
            string hex = BitConverter.ToString(data)
                .Replace("-", " ");

            AddEvent(portName + " | " + direction +
                " | " + hex + " | " + meaning);
        }

        private void AddEvent(string message)
        {
            ComListBox.Items.Add(
                DateTime.Now.ToString("HH:mm:ss.fff") +
                " | " + message);

            if (ComListBox.Items.Count > 0)
                ComListBox.TopIndex = ComListBox.Items.Count - 1;
        }

        // ------------------------------------------------
        // YOUR EXISTING BUTTONS
        // ------------------------------------------------

        private void ExitButton_Click(object sender, EventArgs e)
        {
            StatusTimer.Stop();
            ClosePort();
            Close();
        }

        private void ReadButton_Click(object sender, EventArgs e)
        {
            SendRequest(
                new byte[] { 0x24, 0x41, 0x01, 0x00 },
                "Manual ADC request");
        }

        private void ADCButton_Click(object sender, EventArgs e)
        {
            SendRequest(
                new byte[] { 0x24, 0x41, 0x01, 0x00 },
                "Manual ADC request");
        }

        private void IDButton_Click(object sender, EventArgs e)
        {
            SendRequest(
                new byte[] { 0x24, 0x49, 0x00 },
                "Manual ID request");
        }


        private void ServoScrollBar_Scroll(
            object sender, ScrollEventArgs e)
        {
            int degrees = ServoScrollBar.Value;

            Text = "Serial Data - Servo: " + degrees + "°";

            SendServo(degrees);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            StatusTimer.Stop();
            ClosePort();

            if (serialPort != null)
            {
                serialPort.Dispose();
                serialPort = null;
            }

            base.OnFormClosed(e);
        }
    }
}