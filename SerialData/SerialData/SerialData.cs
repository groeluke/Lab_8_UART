using System.IO.Ports;
using System.Text;
using System.Text.Json;

namespace SerialData
{
    public partial class SerialData : Form
    {
        // Connection state
        private SerialPort? serialPort;
        private readonly List<byte> rxBuffer = new List<byte>();
        private bool bridgeFound = false;
        private bool discovering = false;
        private bool timerBusy = false;
        private bool everConnected = false;
        private string verifiedPort = "";
        private string currentRequest = "";
        private DateTime nextSearchTime = DateTime.MinValue;
        private DateTime nextIdentityCheck = DateTime.MinValue;
        private readonly HashSet<string> reportedPorts = new HashSet<string>();

        // Only one request may be on the serial link at a time.
        private readonly SemaphoreSlim requestLock = new SemaphoreSlim(1, 1);

        private static readonly byte[] IdentityFrame = { 0x24, 0x49, 0x00 };
        private static readonly byte[] IdentityReply = { 0x24, 0x49, 0x02, 0x33, 0x75 };
        private const int ReplyTimeoutMs = 500;
        private bool scanning = false;
        private List<SavedCommand> savedCommands = new List<SavedCommand>();

        // Saved commands are kept in your Documents folder so they survive restarts.
        private readonly string savedFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "I2CBridgeSaved.json");

        // The log file is saved in your Documents folder.
        private readonly string logFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "I2CBridgeLog.txt");

        public SerialData()
        {
            InitializeComponent();

            OperationComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            OperationComboBox.Items.AddRange(
                new object[] { "Identity", "Probe", "Write", "Read", "Raw frame" });
            OperationComboBox.SelectedIndex = 0;

            ModeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            ModeComboBox.Items.AddRange(new object[] { "Hex", "Text (UTF-8)" });
            ModeComboBox.SelectedIndex = 0;

            AddressTextBox.PlaceholderText = "Address (hex, e.g. 20)";
            DataTextBox.PlaceholderText = "Write data (hex 01 41, or text)";
            RawFrameTextBox.PlaceholderText = "Raw frame (hex)";
            SaveNameTextBox.PlaceholderText = "Name";

            CountNumeric.Minimum = 0;
            CountNumeric.Maximum = 255;
            PreviewTextBox.ReadOnly = true;
            WarningLabel.ForeColor = Color.Firebrick;
            WarningLabel.Text = "";
            ScanStatusLabel.Text = "";
            ComListBox.HorizontalScrollbar = true;

            // Update the preview whenever an input changes.
            OperationComboBox.SelectedIndexChanged += (s, e) => UpdatePreview();
            ModeComboBox.SelectedIndexChanged += (s, e) => UpdatePreview();
            AddressTextBox.TextChanged += (s, e) => UpdatePreview();
            DataTextBox.TextChanged += (s, e) => UpdatePreview();
            CountNumeric.ValueChanged += (s, e) => UpdatePreview();
            RawFrameTextBox.TextChanged += (s, e) => UpdatePreview();

            LogEvent("Application started. Log file: " + logFilePath);
            UpdateStatus();
            UpdatePreview();

            StatusTimer.Interval = 100;
            StatusTimer.Start();
        }

        // MAIN TIMER: connection check, identity check, reconnect
        private async void StatusTimer_Tick(object? sender, EventArgs e)
        {
            if (timerBusy)
                return;

            timerBusy = true;

            try
            {
                if (bridgeFound)
                {
                    bool portExists = SerialPort.GetPortNames().Any(p =>
                        string.Equals(p, verifiedPort,
                            StringComparison.OrdinalIgnoreCase));

                    if (!portExists)
                    {
                        Disconnect("COM port is no longer present");
                    }
                    else if (DateTime.Now >= nextIdentityCheck)
                    {
                        nextIdentityCheck = DateTime.Now.AddSeconds(1);
                        await IdentityCheckAsync();
                    }
                }

                if (!bridgeFound && !discovering &&
                    DateTime.Now >= nextSearchTime)
                {
                    await DiscoverBridgeAsync();
                }
            }
            catch (Exception ex)
            {
                LogEvent("Connection check failed: " + ex.Message);

                if (bridgeFound)
                    Disconnect("exception in connection check");
            }
            finally
            {
                timerBusy = false;
            }
        }

        private async Task IdentityCheckAsync()
        {
            // skipIfBusy: if another request is active, the check is deferred.
            var result = await SendAndWaitAsync(
                IdentityFrame, "Automatic identity check", true);

            if (!result.Sent)
                return;

            if (result.Reply == null ||
                !result.Reply.SequenceEqual(IdentityReply))
            {
                Disconnect("no valid reply to the automatic identity check");
            }
        }

        // FIND THE BRIDGE: accept a port only after the exact identity reply
        private async Task DiscoverBridgeAsync()
        {
            if (discovering)
                return;

            discovering = true;

            try
            {
                foreach (string name in SerialPort.GetPortNames())
                {
                    CloseAndDispose();

                    try
                    {
                        serialPort = new SerialPort(
                            name, 9600, Parity.None, 8, StopBits.One);
                        serialPort.Handshake = Handshake.None;
                        serialPort.ReadTimeout = 100;
                        serialPort.WriteTimeout = 250;
                        serialPort.Open();
                        serialPort.DiscardInBuffer();
                        serialPort.DiscardOutBuffer();
                        rxBuffer.Clear();

                        serialPort.Write(IdentityFrame, 0, IdentityFrame.Length);
                        LogFrame(name, "TX", IdentityFrame,
                            "Discovery identity request");

                        bool valid = false;
                        DateTime end =
                            DateTime.Now.AddMilliseconds(ReplyTimeoutMs);

                        while (DateTime.Now < end && !valid)
                        {
                            ReadAvailable();

                            while (TryExtractPacket(rxBuffer, out byte[] packet))
                            {
                                LogFrame(name, "RX", packet,
                                    "Discovery reply: " + DecodeReply(packet));

                                if (packet.SequenceEqual(IdentityReply))
                                {
                                    valid = true;
                                    break;
                                }
                            }

                            if (!valid)
                                await Task.Delay(10);
                        }

                        if (!valid)
                        {
                            if (reportedPorts.Add(name))
                                LogEvent(name + " is not the bridge");

                            CloseAndDispose();
                            continue;
                        }

                        bridgeFound = true;
                        verifiedPort = name;
                        rxBuffer.Clear();
                        nextIdentityCheck = DateTime.Now.AddSeconds(1);

                        LogEvent(everConnected
                            ? "Reconnected to the bridge on " + name
                            : "Bridge connected on " + name);

                        everConnected = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (reportedPorts.Add(name))
                            LogEvent(name + " unavailable: " + ex.Message);

                        CloseAndDispose();
                    }
                }

                if (!bridgeFound)
                    nextSearchTime = DateTime.Now.AddSeconds(1);
            }
            finally
            {
                discovering = false;
                UpdateStatus();
            }
        }

        // SEND ONE REQUEST AND WAIT FOR ITS REPLY
        // Every part of the program that uses the link goes through here.
        private async Task<(bool Sent, byte[]? Reply)> SendAndWaitAsync(
            byte[] frame, string meaning, bool skipIfBusy = false)
        {
            if (skipIfBusy)
            {
                if (!await requestLock.WaitAsync(0))
                    return (false, null);
            }
            else
            {
                await requestLock.WaitAsync();
            }

            try
            {
                if (!bridgeFound || serialPort == null || !serialPort.IsOpen)
                {
                    LogEvent("Not sent (bridge not connected): " + meaning);
                    return (false, null);
                }

                currentRequest = meaning;

                // Log anything the bridge sent on its own (for example
                // framing or overrun error packets).
                LogUnsolicited();

                serialPort.Write(frame, 0, frame.Length);
                LogFrame(verifiedPort, "TX", frame, meaning);

                byte expectedCommand = frame.Length >= 2 ? frame[1] : (byte)0;
                DateTime deadline =
                    DateTime.Now.AddMilliseconds(ReplyTimeoutMs);

                while (DateTime.Now < deadline)
                {
                    ReadAvailable();

                    while (TryExtractPacket(rxBuffer, out byte[] packet))
                    {
                        bool isReply = packet.Length >= 2 &&
                            (packet[1] == expectedCommand || packet[1] == 0x45);

                        if (isReply)
                        {
                            LogFrame(verifiedPort, "RX", packet,
                                DecodeReply(packet));
                            return (true, packet);
                        }

                        LogFrame(verifiedPort, "RX", packet,
                            "Ignored (not a reply to this request): " +
                            DecodeReply(packet));
                    }

                    await Task.Delay(5);
                }

                LogEvent(verifiedPort + " | TIMEOUT | no reply to: " + meaning);
                return (true, null);
            }
            catch (Exception ex)
            {
                LogEvent("Serial error during '" + meaning + "': " + ex.Message);
                Disconnect("serial exception");
                return (true, null);
            }
            finally
            {
                currentRequest = "";
                requestLock.Release();
            }
        }

        private void ReadAvailable()
        {
            if (serialPort == null || !serialPort.IsOpen)
                return;

            int count = serialPort.BytesToRead;

            if (count <= 0)
                return;

            byte[] data = new byte[count];
            int read = serialPort.Read(data, 0, count);

            for (int i = 0; i < read; i++)
                rxBuffer.Add(data[i]);
        }

        private void LogUnsolicited()
        {
            ReadAvailable();

            while (TryExtractPacket(rxBuffer, out byte[] packet))
            {
                LogFrame(verifiedPort, "RX", packet,
                    "Unsolicited: " + DecodeReply(packet));
            }

            rxBuffer.Clear();
        }

        // Packet format: 24 COMMAND LENGTH PAYLOAD (payload is at most 24 bytes)
        private static bool TryExtractPacket(
            List<byte> buffer, out byte[] packet)
        {
            packet = Array.Empty<byte>();

            while (true)
            {
                while (buffer.Count > 0 && buffer[0] != 0x24)
                    buffer.RemoveAt(0);

                if (buffer.Count < 3)
                    return false;

                if (buffer[2] > 24)
                {
                    buffer.RemoveAt(0);
                    continue;
                }

                int total = 3 + buffer[2];

                if (buffer.Count < total)
                    return false;

                packet = buffer.GetRange(0, total).ToArray();
                buffer.RemoveRange(0, total);
                return true;
            }
        }

        // DECODE REPLIES
        private static string DecodeReply(byte[] packet)
        {
            if (packet.Length < 3 || packet.Length != 3 + packet[2])
                return "Invalid packet";

            byte command = packet[1];
            int payloadLength = packet[2];

            if (command == 0x45)
            {
                if (payloadLength != 2)
                    return "Error packet with unexpected length";

                byte category = packet[3];
                byte detail = packet[4];

                switch (category)
                {
                    case 0x01:
                        return "ERROR: UART framing error (Lab 8)";
                    case 0x02:
                        return "ERROR: UART receive overrun (Lab 8)";
                    case 0x04:
                        return "ERROR: I2C - " + I2CErrorText(detail);
                    case 0x05:
                        return "ERROR: bridge request - " + BridgeErrorText(detail);
                    default:
                        return "ERROR: unknown category " + category.ToString("X2");
                }
            }

            if (command == 0x49)
            {
                return (payloadLength == 2 && packet[3] == 0x33 && packet[4] == 0x75)
                    ? "Identity OK (33 75)"
                    : "Identity reply with unexpected payload";
            }

            if (command == 0x50)
            {
                return (payloadLength == 1 && packet[3] == 0x00)
                    ? "Probe: device ACKed"
                    : "Probe reply with unexpected payload";
            }

            if (command == 0x57)
            {
                return (payloadLength == 1 && packet[3] == 0x00)
                    ? "Write OK (all bytes ACKed)"
                    : "Write reply with unexpected payload";
            }

            if (command == 0x52)
            {
                if (payloadLength >= 2 && packet[3] == 0x00)
                {
                    byte[] data = packet.Skip(4).ToArray();
                    return "Read OK, " + data.Length + " byte(s): " +
                        BitConverter.ToString(data).Replace("-", " ");
                }

                return "Read reply with unexpected payload";
            }

            return "Unknown command " + command.ToString("X2");
        }

        private static string I2CErrorText(byte detail)
        {
            switch (detail)
            {
                case 0x01: return "NACK (address or data byte not acknowledged)";
                case 0x02: return "timeout";
                case 0x03: return "bus LOW / not idle";
                case 0x04: return "SCL held LOW";
                default: return "unknown detail " + detail.ToString("X2");
            }
        }

        private static string BridgeErrorText(byte detail)
        {
            switch (detail)
            {
                case 0x01: return "invalid length or count";
                case 0x02: return "unknown command";
                case 0x03: return "invalid 7-bit address";
                default: return "unknown detail " + detail.ToString("X2");
            }
        }

        // TRANSMIT PANEL: build the frame from the inputs
        private bool BuildFrame(
            out byte[] frame, out string warnings, out string error)
        {
            frame = Array.Empty<byte>();
            warnings = "";
            error = "";

            string operation =
                OperationComboBox.SelectedItem?.ToString() ?? "";

            if (operation == "Raw frame")
            {
                if (!TryParseHexBytes(RawFrameTextBox.Text, out byte[] raw))
                {
                    error = "Raw frame must be hex bytes, for example 24 49 00";
                    return false;
                }

                if (raw.Length == 0)
                {
                    error = "Enter a raw frame";
                    return false;
                }

                frame = raw;
                return true;
            }

            if (operation == "Identity")
            {
                frame = new byte[] { 0x24, 0x49, 0x00 };
                return true;
            }

            if (!TryParseHexBytes(AddressTextBox.Text, out byte[] addr) ||
                addr.Length != 1)
            {
                error = "Address must be one hex byte, for example 20";
                return false;
            }

            byte address = addr[0];
            List<string> warn = new List<string>();

            if (address < 0x08 || address > 0x77)
                warn.Add("address is outside 08-77 (bridge should reject it)");

            byte command;
            List<byte> payload = new List<byte> { address };

            if (operation == "Probe")
            {
                command = 0x50;
            }
            else if (operation == "Write")
            {
                command = 0x57;
                byte[] data;

                if (ModeComboBox.SelectedIndex == 1)
                {
                    data = Encoding.UTF8.GetBytes(DataTextBox.Text);
                }
                else if (!TryParseHexBytes(DataTextBox.Text, out data))
                {
                    error = "Write data must be hex bytes, for example 01 41";
                    return false;
                }

                payload.AddRange(data);

                if (data.Length == 0)
                    warn.Add("no data bytes (bridge should reject an address-only write)");
            }
            else if (operation == "Read")
            {
                command = 0x52;
                payload.Add((byte)CountNumeric.Value);

                if (CountNumeric.Value == 0 || CountNumeric.Value > 23)
                    warn.Add("read count outside 1-23 (bridge should reject it)");
            }
            else
            {
                error = "Choose an operation";
                return false;
            }

            if (payload.Count > 255)
            {
                error = "A payload of " + payload.Count +
                    " bytes cannot be represented in the one-byte length field";
                return false;
            }

            if (payload.Count > 24)
                warn.Add("payload is " + payload.Count +
                    " bytes, the bridge maximum is 24 (bridge should reject it)");

            List<byte> bytes = new List<byte> { 0x24, command, (byte)payload.Count };
            bytes.AddRange(payload);

            frame = bytes.ToArray();
            warnings = string.Join("; ", warn);
            return true;
        }

        private static bool TryParseHexBytes(string text, out byte[] bytes)
        {
            bytes = Array.Empty<byte>();
            List<byte> list = new List<byte>();

            string[] tokens = text.Split(
                new[] { ' ', ',', ';', '\t', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (string token in tokens)
            {
                string t = token;

                if (t.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    t = t.Substring(2);

                if (t.Length == 0 || t.Length > 2 ||
                    !byte.TryParse(t,
                        System.Globalization.NumberStyles.HexNumber,
                        null, out byte value))
                {
                    return false;
                }

                list.Add(value);
            }

            bytes = list.ToArray();
            return true;
        }

        private void UpdatePreview()
        {
            string operation =
                OperationComboBox.SelectedItem?.ToString() ?? "";

            bool raw = operation == "Raw frame";
            bool identity = operation == "Identity";

            AddressTextBox.Enabled = !raw && !identity;
            ModeComboBox.Enabled = operation == "Write";
            DataTextBox.Enabled = operation == "Write";
            CountNumeric.Enabled = operation == "Read";
            RawFrameTextBox.Enabled = raw;

            if (BuildFrame(out byte[] frame, out string warnings, out string error))
            {
                string hex = BitConverter.ToString(frame).Replace("-", " ");
                int payload = frame.Length >= 3 ? frame.Length - 3 : 0;

                PreviewTextBox.Text = "TX: " + hex + "   (" + frame.Length +
                    " bytes, payload " + payload + ")";

                WarningLabel.Text =
                    warnings == "" ? "" : "Warning: " + warnings;
            }
            else
            {
                PreviewTextBox.Text = "";
                WarningLabel.Text = error;
            }
        }

        private async void SendButton_Click(object? sender, EventArgs e)
        {
            await SendCurrentAsync("Manual");
        }

        // Builds a frame from the current controls and sends it.
        private async Task SendCurrentAsync(string label)
        {
            if (!BuildFrame(out byte[] frame, out string warnings, out string error))
            {
                WarningLabel.Text = error;
                LogEvent("Not sent: " + error);
                return;
            }

            SendButton.Enabled = false;
            ResendButton.Enabled = false;

            try
            {
                string meaning = label + " " +
                    (OperationComboBox.SelectedItem?.ToString() ?? "request");

                if (warnings != "")
                    meaning += " [" + warnings + "]";

                // Deliberate boundary tests are sent as typed, never changed.
                await SendAndWaitAsync(frame, meaning);
            }
            finally
            {
                UpdateStatus();
            }
        }

        private void UpdateStatus()
        {
            StatusLabel.Text = bridgeFound
                ? "Connected: " + verifiedPort
                : "Disconnected - searching for the bridge...";

            bool ready = bridgeFound && !scanning;
            SendButton.Enabled = ready;
            ResendButton.Enabled = ready;
            ScanButton.Enabled = ready;
        }

        // DISCONNECT, STATUS, LOGGING
        private void Disconnect(string reason)
        {
            bool wasConnected = bridgeFound;
            string failed = currentRequest;
            string port = verifiedPort;

            bridgeFound = false;
            rxBuffer.Clear();
            CloseAndDispose();
            reportedPorts.Clear();
            nextSearchTime = DateTime.Now.AddSeconds(1);

            if (wasConnected)
            {
                LogEvent("Bridge disconnected on " + port + " (" + reason + ")" +
                    (failed != "" ? ". Failed request: " + failed : ""));
            }

            verifiedPort = "";
            UpdateStatus();
        }

        private void CloseAndDispose()
        {
            try
            {
                if (serialPort != null)
                {
                    if (serialPort.IsOpen)
                        serialPort.Close();

                    serialPort.Dispose();
                }
            }
            catch
            {
                // The adapter may already be unplugged.
            }

            serialPort = null;
        }

        private void LogFrame(
            string port, string direction, byte[] data, string decoded)
        {
            string hex = BitConverter.ToString(data).Replace("-", " ");
            int payloadLength = data.Length >= 3 ? data.Length - 3 : 0;

            WriteLog(port + " | " + direction + " | " + hex +
                " | payload=" + payloadLength + " | " + decoded);
        }

        private void LogEvent(string message)
        {
            WriteLog("EVENT | " + message);
        }

        private void WriteLog(string text)
        {
            string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") +
                " | " + text;

            ComListBox.Items.Add(line);

            if (ComListBox.Items.Count > 3000)
                ComListBox.Items.RemoveAt(0);

            ComListBox.TopIndex = ComListBox.Items.Count - 1;

            try
            {
                File.AppendAllText(logFilePath, line + Environment.NewLine);
            }
            catch
            {
                // Never let a log-file problem stop the program.
            }
        }

        // ------------------------------------------------
        // ADDRESS SCAN: all 128 values, reserved ones are not probed
        // ------------------------------------------------
        private async void ScanButton_Click(object? sender, EventArgs e)
        {
            if (!bridgeFound || scanning)
                return;

            scanning = true;
            UpdateStatus();

            ScanProgressBar.Minimum = 0;
            ScanProgressBar.Maximum = 128;
            ScanProgressBar.Value = 0;

            int acked = 0;
            int nacked = 0;
            int failed = 0;
            bool completed = true;

            LogEvent("Address scan started (00-7F)");

            try
            {
                for (int address = 0; address <= 0x7F; address++)
                {
                    string hex = address.ToString("X2");
                    ScanStatusLabel.Text = "Scanning " + hex + " ...";

                    // Reserved addresses are marked, but nothing is sent.
                    if (address <= 0x07 || address >= 0x78)
                    {
                        LogEvent("Scan " + hex + ": Reserved (not probed)");
                        ScanProgressBar.Value = address + 1;
                        continue;
                    }

                    if (!bridgeFound)
                    {
                        completed = false;
                        break;
                    }

                    byte[] frame = { 0x24, 0x50, 0x01, (byte)address };
                    var result = await SendAndWaitAsync(frame, "Scan probe " + hex);

                    if (!result.Sent)
                    {
                        completed = false;
                        break;
                    }

                    byte[]? reply = result.Reply;
                    string outcome;

                    if (reply != null && reply.Length == 5 &&
                        reply[1] == 0x50 && reply[3] == 0x00)
                    {
                        outcome = "ACK";
                        acked++;
                    }
                    else if (reply != null && reply.Length == 5 &&
                             reply[1] == 0x45 && reply[3] == 0x04 && reply[4] == 0x01)
                    {
                        outcome = "NACK";
                        nacked++;
                    }
                    else
                    {
                        outcome = "communication error";
                        failed++;
                    }

                    LogEvent("Scan " + hex + ": " + outcome);
                    ScanProgressBar.Value = address + 1;
                }
            }
            finally
            {
                scanning = false;
                UpdateStatus();
            }

            string summary = (completed ? "Scan complete: " : "Scan stopped early: ") +
                acked + " ACK, " + nacked + " NACK, " + failed + " error(s)";

            ScanStatusLabel.Text = summary;
            LogEvent(summary);
        }

        // ------------------------------------------------
        // SAVED TRANSMISSIONS
        // ------------------------------------------------
        public class SavedCommand
        {
            public string Name { get; set; } = "";
            public string Operation { get; set; } = "Identity";
            public string Address { get; set; } = "";
            public string Mode { get; set; } = "Hex";
            public string Data { get; set; } = "";
            public int Count { get; set; } = 0;
            public string RawFrame { get; set; } = "";

            public override string ToString()
            {
                return Name + "  [" + Operation + "]";
            }
        }

        private SavedCommand CaptureCurrent(string name)
        {
            return new SavedCommand
            {
                Name = name,
                Operation = OperationComboBox.SelectedItem?.ToString() ?? "Identity",
                Address = AddressTextBox.Text,
                Mode = ModeComboBox.SelectedItem?.ToString() ?? "Hex",
                Data = DataTextBox.Text,
                Count = (int)CountNumeric.Value,
                RawFrame = RawFrameTextBox.Text
            };
        }

        private void ApplyToControls(SavedCommand c)
        {
            int operation = OperationComboBox.Items.IndexOf(c.Operation);
            OperationComboBox.SelectedIndex = operation >= 0 ? operation : 0;

            AddressTextBox.Text = c.Address;

            int mode = ModeComboBox.Items.IndexOf(c.Mode);
            ModeComboBox.SelectedIndex = mode >= 0 ? mode : 0;

            DataTextBox.Text = c.Data;
            CountNumeric.Value = Math.Max(0, Math.Min(255, c.Count));
            RawFrameTextBox.Text = c.RawFrame;
            SaveNameTextBox.Text = c.Name;
        }

        private void LoadSaved()
        {
            try
            {
                if (File.Exists(savedFilePath))
                {
                    string json = File.ReadAllText(savedFilePath);
                    List<SavedCommand>? list =
                        JsonSerializer.Deserialize<List<SavedCommand>>(json);

                    if (list != null)
                        savedCommands = list;
                }
            }
            catch (Exception ex)
            {
                LogEvent("Could not load saved commands: " + ex.Message);
            }

            RefreshSavedList("");
        }

        private void SaveToDisk()
        {
            try
            {
                File.WriteAllText(savedFilePath, JsonSerializer.Serialize(
                    savedCommands, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                LogEvent("Could not save commands: " + ex.Message);
            }
        }

        private void RefreshSavedList(string selectName)
        {

        }

        // Save adds a new command, or updates it if the name already exists.
        private void SaveButton_Click(object? sender, EventArgs e)
        {
            string name = SaveNameTextBox.Text.Trim();

            if (name == "")
            {
                WarningLabel.Text = "Enter a name to save this command";
                return;
            }

            SavedCommand command = CaptureCurrent(name);

            int index = savedCommands.FindIndex(c =>
                string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

            if (index >= 0)
            {
                savedCommands[index] = command;
                LogEvent("Saved command updated: " + name);
            }
            else
            {
                savedCommands.Add(command);
                LogEvent("Saved command added: " + name);
            }

            SaveToDisk();
            RefreshSavedList(name);
        }

        // BUTTONS AND SHUTDOWN
        private void ExitButton_Click(object? sender, EventArgs e)
        {
            this.Close();
        }

        private void ComListBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            StatusTimer.Stop();
            CloseAndDispose();
            base.OnFormClosed(e);
        }
    }
}