using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Net;

namespace VRChatHeartRateMonitor
{
    public partial class MainForm : Form
    {
        private PrivateFontCollection _privateFonts = new PrivateFontCollection();
        private UpdateHandler _updateHandler;
        private System.Windows.Forms.Timer _heartbeatEffectTimer;
        private IDeviceHandler _deviceHandler;
        private Dictionary<ulong, string> _deviceMap = new Dictionary<ulong, string>();
        private BleHandler bleHandler;
        private CancellationTokenSource _autoConnectCountdownCancellationToken;
        private VRChatOscHandler _vrchatOscHandler;
        private WebServerHandler _webServerHandler;
        private DiscordHandler _discordHandler;
        private List<Icon> _icons = new List<Icon>();
        private string _lastConnectedDeviceAddress = "";
        private string _oscAddress = "127.0.0.1:9000";
        private bool _useChatbox = true;
        private ushort _chatboxAppearance = 0;
        private bool _useAvatar = false;
        private string _avatarParameter = "heartRate";
        private bool _useWebServer = false;
        private ushort _webServerPort = 6969;
        private string _webServerHtml = "<!-- Use own HTML/JS \"{0}\" will be replaced with HR value -->\r\n<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"200\" height=\"200\" viewBox=\"0 0 24 [...]";
        private bool _useDiscord = true;
        private string _discordActiveText = "Using my VRC Heart Rate Monitor!";
        private string _discordIdleText = "Ideling with my VRC Heart Rate Monitor!";
        private string _discordStateText = "{0} BPM";
        private bool isScanning = false;
        private CheckBox checkBoxSwitchBLE;

        public MainForm()
        {
            InitializeComponent();
            InitializeFont();
            InitializeConfig();
            InitializeIcons();
            InitializeForm();
            InitializeUpdate();
            InitializeHeartbeatEffect();
            InitializeDeviceHandler();
            InitializeVRChatOscHandler();
            InitializeWebServerHandler();
            InitializeDiscordHandler();

            if (checkBoxSwitchBLE.Checked)
            {
                // Initialize BLE (GATT) handler
                _deviceHandler = (IDeviceHandler)new BleHandler();
            }
            else
            {
                // Initialize Bluetooth Adapter (BA) handler
                _deviceHandler = (IDeviceHandler)new DeviceHandler();
            }
            InitializeDeviceHandler();
            InitializeVRChatOscHandler();
            InitializeWebServerHandler();
            InitializeDiscordHandler();

            // Initialize the new BLE switch checkbox
            checkBoxSwitchBLE = new CheckBox
            {
                Location = new Point(200, 20), // Adjust the location as needed
                Size = new Size(150, 30), // Adjust the size as needed
                Text = "Use BLE",
                Appearance = Appearance.Normal,
                TextAlign = ContentAlignment.MiddleLeft,
                Checked = false // Default to BA mode
            };
            checkBoxSwitchBLE.CheckedChanged += new EventHandler(checkBoxSwitchBLE_CheckedChanged);
            this.Controls.Add(checkBoxSwitchBLE);


            // Initialize BleHandler
            bleHandler = new BleHandler();
            bleHandler.HeartRateUpdated += OnHeartRateUpdated;
        }

        private void checkBoxSwitchBLE_CheckedChanged(object sender, EventArgs e)
        {
            // Re-initialize the device handler based on the selected mode
            InitializeDeviceHandler();
        }
        private void SafeInvoke(Action action)
        {
            if (InvokeRequired)
                try
                {
                    Invoke(action);
                }
                catch (ObjectDisposedException) { }
            else
                action();
        }

        private void SwitchToBLEFunction()
        {
            StopHandlers();
            StartBLEHandlers();
            MessageBox.Show("Switched to BLE Functionality");
        }

        private void StartBLEHandlers()
        {
            bleHandler.StartBLE();
        }

        private void OnHeartRateUpdated(ushort heartRate)
        {
            // Handle the heart rate update (e.g., update the UI or log the value)
            Console.WriteLine($"Heart Rate: {heartRate}");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Ensure BLE resources are cleaned up
            bleHandler.Disconnect();
            base.OnFormClosing(e);
        }

        private void InitializeFont()
        {
            int fontLength = Properties.Resources.CascadiaMono.Length;

            IntPtr data = Marshal.AllocCoTaskMem(fontLength);

            Marshal.Copy(Properties.Resources.CascadiaMono, 0, data, fontLength);

            _privateFonts.AddMemoryFont(data, fontLength);

            Marshal.FreeCoTaskMem(data);

            OverRideControlFont(this);
        }

        private void OverRideControlFont(Control parentControl)
        {
            foreach (Control control in parentControl.Controls)
            {
                if (control.Font.Name == "Cascadia Mono")
                    control.Font = new Font(_privateFonts.Families[0], control.Font.Size, control.Font.Style);

                if (control.Controls.Count > 0)
                    OverRideControlFont(control);
            }
        }

        private void InitializeConfig()
        {
            _lastConnectedDeviceAddress = RegistryHelper.GetValue("last_connected_device_address", _lastConnectedDeviceAddress);

            _oscAddress = RegistryHelper.GetValue("osc_address", _oscAddress);

            _useChatbox = RegistryHelper.GetValue("use_chatbox", _useChatbox);
            _chatboxAppearance = ushort.Parse(RegistryHelper.GetValue("chatbox_appearance", _chatboxAppearance.ToString()));

            _useAvatar = RegistryHelper.GetValue("use_avatar", _useAvatar);
            _avatarParameter = RegistryHelper.GetValue("avatar_parameter", _avatarParameter);

            _useWebServer = RegistryHelper.GetValue("use_web_server", _useWebServer);
            _webServerPort = ushort.Parse(RegistryHelper.GetValue("web_server_port", _webServerPort.ToString()));
            _webServerHtml = RegistryHelper.GetValue("web_server_html", _webServerHtml);

            _useDiscord = RegistryHelper.GetValue("use_discord", _useDiscord);
            _discordActiveText = RegistryHelper.GetValue("discord_active_text", _discordActiveText);
            _discordIdleText = RegistryHelper.GetValue("discord_idle_text", _discordIdleText);
            _discordStateText = RegistryHelper.GetValue("discord_state_text", _discordStateText);
        }

        private void InitializeIcons()
        {
            Image mainIcon = Properties.Resources.heart;

            for (int i = 0; i <= 254; i++)
            {
                Image smalIcon = ScaleImage(mainIcon, 32, 32);

                if (i > 0)
                {
                    using (Graphics graphics = Graphics.FromImage(smalIcon))
                    using (Font font = new Font(_privateFonts.Families[0], 14, FontStyle.Bold))
                        graphics.DrawString(i.ToString(), font, Brushes.Yellow, new PointF((i >= 100 ? -4 : 3), 4));
                }

                _icons.Add(Icon.FromHandle(((Bitmap)smalIcon).GetHicon()));
            }

            ResetIcons();
        }

        private Image ScaleImage(Image image, int width, int height)
        {
            Bitmap scaledImage = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(scaledImage))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(image, 0, 0, width, height);
            }
            return scaledImage;
        }

        private void ResetIcons()
        {
            this.Icon = _icons.ElementAt(0);
            labelHeartRateDisplay.Visible = false;
            notifyIcon.Icon = _icons.ElementAt(0);
            notifyIcon.Text = "Status: Inactive";
        }

        private void SetIcons(ushort heartRate)
        {
            this.Icon = _icons.ElementAt(heartRate);
            labelHeartRateDisplay.Visible = true;
            labelHeartRateDisplay.Text = heartRate.ToString();
            notifyIcon.Icon = _icons.ElementAt(heartRate);
            notifyIcon.Text = "Status: Active\nHeart Rate: " + heartRate;
        }

        private void InitializeForm()
        {
            this.FormClosing += MainForm_FormClosing;
            this.Text = HeartRateMonitor.GetAssemblyTitle();
            labelInfoAppName.Text = HeartRateMonitor.GetAssemblyTitle();

            textBoxOscAddress.Text = _oscAddress;
            textBoxOscAddress.KeyPress += new KeyPressEventHandler(textBox_KeyPressIp);
            textBoxOscAddress.Leave += new EventHandler(textBoxOscAddress_Leave);

            checkBoxUseChatbox.Checked = _useChatbox;
            checkBoxUseChatbox_CheckedChanged(checkBoxUseChatbox, EventArgs.Empty);

            comboBoxChatboxAppearance.SelectedIndex = _chatboxAppearance;

            checkBoxUseAvatar.Checked = _useAvatar;
            checkBoxUseAvatar_CheckedChanged(checkBoxUseAvatar, EventArgs.Empty);

            textBoxAvatarParameter.Text = _avatarParameter;
            textBoxAvatarParameter.KeyPress += new KeyPressEventHandler(textBox_KeyPressAlphanumeric);

            checkBoxUseWebServer.Checked = _useWebServer;
            checkBoxUseWebServer_CheckedChanged(checkBoxUseWebServer, EventArgs.Empty);

            textBoxWebServerPort.Text = _webServerPort.ToString();
            textBoxWebServerPort.KeyPress += new KeyPressEventHandler(textBox_KeyPressNumeric);
            textBoxWebServerPort.Leave += new EventHandler(textBoxWebServerPort_Leave);

            textBoxWebServerHtml.Text = _webServerHtml;

            checkBoxUseDiscord.Checked = _useDiscord;
            checkBoxUseDiscord_CheckedChanged(checkBoxUseDiscord, EventArgs.Empty);

            textBoxDiscordActiveText.Text = _discordActiveText;
            textBoxDiscordIdleText.Text = _discordIdleText;
            textBoxDiscordStateText.Text = _discordStateText;

            notifyIcon.MouseClick += NotifyIcon_MouseClick;

            labelHeartRateDisplay.Parent = pictureBoxHeartRateDisplay;
            labelBatteryLevel.Parent = labelHeartRateDisplay;
            labelBatteryLevel.Text = "";
        }

        private void InitializeUpdate()
        {
            _updateHandler = new UpdateHandler();
            _updateHandler.UpdateAvailable += UpdateHandler_UpdateAvailable;

            _updateHandler.CheckForUpdates();
        }

        private void UpdateHandler_UpdateAvailable(string updateUrl, string currentVersion, string latestVersion)
        {
            SafeInvoke(() => {
                if (HeartRateMonitor.ActionMessageBox($"There's a new update available!\n\nWould You like to update from version {currentVersion} to {latestVersion}?"))
                {
                    if (_updateHandler.Update(updateUrl))
                        this.Close();
                    else
                        HeartRateMonitor.ErrorMessageBox("An error occurred during the update process. Please try again or download the application again from the official source: " + updateUrl);
                }
            });
        }

        private void InitializeHeartbeatEffect()
        {
            _heartbeatEffectTimer = new System.Windows.Forms.Timer();
            _heartbeatEffectTimer.Interval = 300;
            _heartbeatEffectTimer.Tick += HeartbeatEffectTimer_Tick;
        }

        private async void HeartbeatEffectTimer_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i <= 1; i++)
            {
                int paddingValue = (i == 1 ? 0 : 4);
                panelHeartRateDisplay.Padding = new Padding(0, paddingValue, 0, paddingValue);
                await Task.Delay(_heartbeatEffectTimer.Interval / 2);
            }
        }

        private void InitializeDeviceHandler()
        {
            if (checkBoxSwitchBLE.Checked)
            {
                // Initialize BLE (GATT) handler
                _deviceHandler = bleHandler;
            }
            else
            {
                // Initialize Bluetooth Adapter (BA) handler
                _deviceHandler = new DeviceHandler();
            }

            _deviceHandler.AdapterError += DeviceManager_AdapterError;
            _deviceHandler.DeviceFound += DeviceManager_DeviceFound;
            _deviceHandler.DeviceConnecting += DeviceManager_DeviceConnecting;
            _deviceHandler.DeviceConnected += DeviceManager_DeviceConnected;
            _deviceHandler.DeviceDisconnecting += DeviceManager_DeviceDisconnecting;
            _deviceHandler.DeviceDisconnected += DeviceManager_DeviceDisconnected;
            _deviceHandler.DeviceError += DeviceManager_DeviceError;
            _deviceHandler.HeartRateUpdated += DeviceManager_HeartRateUpdated;
            _deviceHandler.BatteryLevelUpdated += DeviceManager_BatteryLevelUpdated;

            comboBoxDevices.Items.Clear();
            comboBoxDevices.Items.Add("Scanning for Bluetooth Devices...");
            comboBoxDevices.SelectedIndex = 0;

            _deviceHandler.StartScanning();
        }

        private void DeviceManager_AdapterError()
        {
            SafeInvoke(() => {
                if (HeartRateMonitor.ActionMessageBox("Bluetooth adapter couldn't handle connection, make sure it's enabled in your system and has full Bluetooth Low Energy compatibility!\n\nWould you like to retry?"))
                    _deviceHandler.StartScanning();
                else
                    this.Close();
            });
        }

        private void DeviceManager_DeviceFound(ulong bluetoothDeviceAddress, string bluetoothDeviceName)
        {
            SafeInvoke(() => AddDeviceToComboBox(bluetoothDeviceAddress, bluetoothDeviceName));
        }

        private void DeviceManager_DeviceConnecting()
        {
            SafeInvoke(() => {
                tabs.SelectedTab = tabMain;
                tabs.Enabled = false;
                buttonExecute.Text = "CONNECTING...";
                buttonExecute.Enabled = false;
                comboBoxDevices.Enabled = false;
            });
        }

        private void DeviceManager_DeviceConnected(ulong bluetoothDeviceAddress)
        {
            SafeInvoke(() => {
                tabs.Enabled = true;
                buttonExecute.Text = "DISCONNECT";
                buttonExecute.Enabled = true;
                _heartbeatEffectTimer.Start();
                _deviceHandler.StopScanning();

                StartHandlers();

                _lastConnectedDeviceAddress = _deviceHandler.BluetoothAddressToString(bluetoothDeviceAddress);
                RegistryHelper.SetValue("last_connected_device_address", _lastConnectedDeviceAddress);
            });
        }

        private void DeviceManager_DeviceDisconnecting()
        {
            SafeInvoke(() => {
                tabs.SelectedTab = tabMain;
                tabs.Enabled = false;
                buttonExecute.Text = "DISCONNECTING...";
                buttonExecute.Enabled = false;
            });
        }

        private void DeviceManager_DeviceDisconnected()
        {
            SafeInvoke(() => {
                tabs.Enabled = true;
                buttonExecute.Text = "CONNECT";
                buttonExecute.Enabled = true;
                _heartbeatEffectTimer.Stop();
                _deviceHandler.StartScanning();

                StopHandlers();

                comboBoxDevices.Enabled = true;
                labelBatteryLevel.Text = "";

                ResetIcons();
            });
        }

        private void DeviceManager_DeviceError(string message)
        {
            SafeInvoke(() => {
                message += "\n\nPlease try reconnecting. If the issue persists, follow these simple troubleshooting steps:\n\n";
                message += "1. Restart the app.\n";
                message += "2. Make sure your HR monitor isn't already connected to a different device, for example, your phone, etc.\n";
                message += "3. Toggle Bluetooth off, wait 10 seconds, then turn it back on.\n";
                message += "4. Restart your heart rate monitor.\n\n";
                message += "Note: Even if you resolve the issue, please report it on our Discord server to help us improve the app.";

                HeartRateMonitor.ErrorMessageBox(message);
            });
        }

        private void DeviceManager_HeartRateUpdated(ushort heartRate)
        {
            SafeInvoke(() => {
                SetIcons(heartRate);
            });
        }

        private void DeviceManager_BatteryLevelUpdated(ushort batteryLevel)
        {
            SafeInvoke(() => {
                labelBatteryLevel.Text = $"⚡{batteryLevel}%";
            });
        }

        private void AddDeviceToComboBox(ulong bluetoothDeviceAddress, string bluetoothDeviceName)
        {
            if (_deviceMap.Count == 0)
            {
                comboBoxDevices.Items.RemoveAt(0);
                buttonExecute.Enabled = true;
            }

            if (!_deviceMap.ContainsKey(bluetoothDeviceAddress))
            {
                _deviceMap.Add(bluetoothDeviceAddress, bluetoothDeviceName);
                comboBoxDevices.Items.Add(bluetoothDeviceName);

                if (comboBoxDevices.DroppedDown)
                {
                    comboBoxDevices.DroppedDown = false;
                    comboBoxDevices.DroppedDown = true;
                }

                if (_lastConnectedDeviceAddress == _deviceHandler.BluetoothAddressToString(bluetoothDeviceAddress))
                {
                    comboBoxDevices.SelectedIndex = comboBoxDevices.Items.Count - 1;
                    StartAutoConnectCountdown(bluetoothDeviceAddress);
                    comboBoxDevices.DroppedDown = false;
                }
                else if (comboBoxDevices.Items.Count == 1)
                {
                    comboBoxDevices.SelectedIndex = 0;
                }
            }
        }

        private void InitializeVRChatOscHandler()
        {
            _vrchatOscHandler = new VRChatOscHandler();
            _vrchatOscHandler.RequestHeartRate = (() => _deviceHandler.GetHeartRate());
        }

        private void InitializeWebServerHandler()
        {
            _webServerHandler = new WebServerHandler();
            _webServerHandler.RequestHeartRate = (() => _deviceHandler.GetHeartRate());
        }

        private void InitializeDiscordHandler()
        {
            _discordHandler = new DiscordHandler();
            _discordHandler.RequestHeartRate = (() => _deviceHandler.GetHeartRate());
        }

        private void StartHandlers()
        {
            if (_useChatbox || _useAvatar)
                _vrchatOscHandler.Start(_useChatbox, _chatboxAppearance, _useAvatar, _avatarParameter, _oscAddress);

            if (_useWebServer)
                _webServerHandler.Start(_webServerPort, _webServerHtml);

            if (_useDiscord)
                _discordHandler.Start(_discordActiveText, _discordIdleText, _discordStateText);
        }

        private void StopHandlers()
        {
            if (_useChatbox || _useAvatar)
                _vrchatOscHandler.Stop();

            if (_useWebServer)
                _webServerHandler.Stop();

            if (_useDiscord)
                _discordHandler.Stop();
        }

        private async void StartAutoConnectCountdown(ulong bluetoothDeviceAddress)
        {
            _autoConnectCountdownCancellationToken = new CancellationTokenSource();

            try
            {
                SafeInvoke(() => {
                    tabs.SelectedTab = tabMain;
                    tabs.Enabled = false;
                });

                for (int i = 3; i > 0; i--)
                {
                    SafeInvoke(() => buttonExecute.Text = ("AUTO-CONNECT IN " + i + "S..."));

                    await Task.Delay(1000, _autoConnectCountdownCancellationToken.Token);
                }

                _deviceHandler.SubscribeToDevice(bluetoothDeviceAddress);
            }
            catch (TaskCanceledException)
            {
                DeviceManager_DeviceDisconnected();
            }

            _autoConnectCountdownCancellationToken = null;
        }

        private void buttonExecute_Click(object sender, EventArgs e)
        {
            if (_autoConnectCountdownCancellationToken != null)
            {
                _autoConnectCountdownCancellationToken.Cancel();
            }
            else if (_deviceHandler.IsListening())
            {
                _deviceHandler.UnsubscribeFromDevice();
            }
            else if (_deviceHandler.CanConnect())
            {
                ulong? selectedBluetoothDeviceAddress = _deviceMap.FirstOrDefault(d => d.Value == comboBoxDevices.SelectedItem.ToString()).Key;

                if (comboBoxDevices.SelectedItem == null || selectedBluetoothDeviceAddress == null)
                {
                    HeartRateMonitor.ErrorMessageBox("Please select device from the list!");
                    return;
                }

                _deviceHandler.SubscribeToDevice((ulong)selectedBluetoothDeviceAddress);
            }
        }

        private void checkBoxUseChatbox_CheckedChanged(object sender, EventArgs e)
        {
            comboBoxChatboxAppearance.Enabled = checkBoxUseChatbox.Checked;
            textBoxOscAddress.ReadOnly = !(checkBoxUseChatbox.Checked || checkBoxUseAvatar.Checked);
        }

        private void checkBoxUseAvatar_CheckedChanged(object sender, EventArgs e)
        {
            textBoxAvatarParameter.ReadOnly = !checkBoxUseAvatar.Checked;
            textBoxOscAddress.ReadOnly = !(checkBoxUseChatbox.Checked || checkBoxUseAvatar.Checked);
        }

        private void checkBoxUseWebServer_CheckedChanged(object sender, EventArgs e)
        {
            textBoxWebServerPort.ReadOnly = !checkBoxUseWebServer.Checked;
            textBoxWebServerHtml.ReadOnly = !checkBoxUseWebServer.Checked;
        }

        private void textBoxWebServerPort_Leave(object sender, EventArgs e)
        {
            if (!ushort.TryParse(textBoxWebServerPort.Text, out ushort result) || result < 1 || result > 65535)
            {
                textBoxWebServerPort.Text = _webServerPort.ToString();
                textBoxWebServerPort.Focus();
                HeartRateMonitor.InfoMessageBox("Web server can only be running on ports from 1 to 65535, please provide a valid port!");
            }
        }

        private void textBoxOscAddress_Leave(object sender, EventArgs e)
        {

            string[] parts = textBoxOscAddress.Text.Split(':');

            if (parts.Length == 2)
            {
                string ipPart = parts[0];
                string portPart = parts[1];

                if (IPAddress.TryParse(ipPart, out IPAddress ip) && ip.ToString() == ipPart)
                    if (int.TryParse(portPart, out int port) && port >= 1 && port <= 65535)
                        return;
            }

            textBoxOscAddress.Text = _oscAddress.ToString();
            textBoxOscAddress.Focus();
            HeartRateMonitor.InfoMessageBox("Invalid format for VRChat OSC address IP and PORT!");
        }

        private void checkBoxUseDiscord_CheckedChanged(object sender, EventArgs e)
        {
            textBoxDiscordActiveText.ReadOnly = !checkBoxUseDiscord.Checked;
            textBoxDiscordIdleText.ReadOnly = !checkBoxUseDiscord.Checked;
            textBoxDiscordStateText.ReadOnly = !checkBoxUseDiscord.Checked;
        }

        private void textBox_KeyPressAlphanumeric(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsLetterOrDigit(e.KeyChar))
                e.Handled = true;
        }

        private void textBox_KeyPressNumeric(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void textBox_KeyPressIp(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != ':' && e.KeyChar != '.')
                e.Handled = true;
        }

        private void buttonAvatarParameterInfo_Click(object sender, EventArgs e)
        {
            HeartRateMonitor.InfoMessageBox($"To use this option you need to edit your VRChat avatar in Unity. New float paramater /avatar/parameters/{_avatarParameter} (from -1.0 to 1.0 - precision of 1/127) will be send through VRChat OSC. You need to add it to parameters, make sure that it's syncedm but not saved, and create animation which displays the heart rate. For more details about avatar configuration check Richard's video ;)");
        }

        private void buttonOscAddressInfo_Click(object sender, EventArgs e)
        {
            HeartRateMonitor.InfoMessageBox($"According to documentation, VRChat receives data via OSC on port 9000.\n\nIn most cases, you should keep the default address 127.0.0.1:9000 unless you have a specific reason to change it.\n\nMore info: https://docs.vrchat.com/docs/osc-overview");
        }

        private void buttonWebServerPortInfo_Click(object sender, EventArgs e)
        {
            HeartRateMonitor.InfoMessageBox($"This option is useful for streamers or more advanced users who wants to do something more with capetured heart rate data, it'll be availabe at http://localohst:{_webServerPort} (make sure to configure port that isn't already taken) so you can display it in OBS or do whatever you want with it.");
        }

        private void buttonSaveSettings_Click(object sender, EventArgs e)
        {
            if (_deviceHandler.IsListening())
            {
                StopHandlers();
            }

            _oscAddress = textBoxOscAddress.Text;
            RegistryHelper.SetValue("osc_address", _oscAddress);

            _useChatbox = checkBoxUseChatbox.Checked;
            RegistryHelper.SetValue("use_chatbox", _useChatbox);

            _chatboxAppearance = (ushort)comboBoxChatboxAppearance.SelectedIndex;
            RegistryHelper.SetValue("chatbox_appearance", _chatboxAppearance);

            _useAvatar = checkBoxUseAvatar.Checked;
            RegistryHelper.SetValue("use_avatar", _useAvatar);

            _avatarParameter = textBoxAvatarParameter.Text;
            RegistryHelper.SetValue("avatar_parameter", _avatarParameter);

            _useWebServer = checkBoxUseWebServer.Checked;
            RegistryHelper.SetValue("use_web_server", _useWebServer);

            _webServerPort = ushort.Parse(textBoxWebServerPort.Text);
            RegistryHelper.SetValue("web_server_port", _webServerPort);

            _webServerHtml = textBoxWebServerHtml.Text;
            RegistryHelper.SetValue("web_server_html", _webServerHtml);

            _useDiscord = checkBoxUseDiscord.Checked;
            RegistryHelper.SetValue("use_discord", _useDiscord);

            _discordActiveText = textBoxDiscordActiveText.Text;
            RegistryHelper.SetValue("discord_active_text", _discordActiveText);

            _discordIdleText = textBoxDiscordIdleText.Text;
            RegistryHelper.SetValue("discord_idle_text", _discordIdleText);

            _discordStateText = textBoxDiscordStateText.Text;
            RegistryHelper.SetValue("discord_state_text", _discordStateText);

            HeartRateMonitor.SuccessMessageBox("Configuration saved!");

            if (_deviceHandler.IsListening())
            {
                StartHandlers();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopHandlers();

            _vrchatOscHandler.RequestHeartRate = null;
            _vrchatOscHandler = null;

            _webServerHandler.RequestHeartRate = null;
            _webServerHandler = null;

            _discordHandler.RequestHeartRate = null;
            _discordHandler = null;

            _deviceHandler.AdapterError -= DeviceManager_AdapterError;
            _deviceHandler.DeviceFound -= DeviceManager_DeviceFound;
            _deviceHandler.DeviceConnecting -= DeviceManager_DeviceConnecting;
            _deviceHandler.DeviceConnected -= DeviceManager_DeviceConnected;
            _deviceHandler.DeviceDisconnecting -= DeviceManager_DeviceDisconnecting;
            _deviceHandler.DeviceDisconnected -= DeviceManager_DeviceDisconnected;
            _deviceHandler.DeviceError -= DeviceManager_DeviceError;
            _deviceHandler.HeartRateUpdated -= DeviceManager_HeartRateUpdated;
            _deviceHandler.BatteryLevelUpdated -= DeviceManager_BatteryLevelUpdated;

            if (_deviceHandler.IsListening())
                _deviceHandler.UnsubscribeFromDevice();
            else
                _deviceHandler.StopScanning();

            _deviceHandler = null;
        }

        private void NotifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (this.WindowState == FormWindowState.Minimized)
                    this.WindowState = FormWindowState.Normal;

                this.Show();
                this.Activate();
                this.TopMost = true;
                this.TopMost = false;
            }
        }

        private void linkLabelFooterWebsite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://virgosky.com") { UseShellExecute = true });
        }

        private void pictureBoxFooterDiscord_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://virgosky.com/dc") { UseShellExecute = true });
        }

        private void linkLabelInfoAuthorUrl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://virgosky.com") { UseShellExecute = true });
        }

        private void linkLabelInfoProjectUrl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/RichardVirgosky/VRChat-Heart-Rate-Monitor") { UseShellExecute = true });
        }
    }
}
