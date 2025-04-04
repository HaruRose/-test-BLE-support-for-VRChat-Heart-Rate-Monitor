using System.Drawing;

namespace VRChatHeartRateMonitor
{

    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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

        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.TabControl tabs;
        private System.Windows.Forms.TabPage tabMain;
        private System.Windows.Forms.TabPage tabVRChatSettings;
        private System.Windows.Forms.PictureBox pictureBoxHeartRateDisplay;
        private System.Windows.Forms.Label labelDevice;
        private System.Windows.Forms.Button buttonExecute;
        private System.Windows.Forms.Label labelDeviceInfo;
        private System.Windows.Forms.TabPage tabInfo;
        private System.Windows.Forms.Label labelHeartRateDisplay;
        private System.Windows.Forms.Panel panelHeartRateDisplay;
        private System.Windows.Forms.LinkLabel linkLabelFooterWebsite;
        private System.Windows.Forms.TextBox textBoxAvatarParameter;
        private System.Windows.Forms.TextBox textBoxWebServerPort;
        private System.Windows.Forms.Label labelWebServerPort;
        private System.Windows.Forms.Label labelAvatarParameter;
        private System.Windows.Forms.CheckBox checkBoxUseWebServer;
        private System.Windows.Forms.CheckBox checkBoxUseAvatar;
        private System.Windows.Forms.Panel panelAvatar;
        private System.Windows.Forms.Button buttonSaveVRChatSettings;
        private System.Windows.Forms.Button buttonAvatarParameterInfo;
        private System.Windows.Forms.Button buttonWebServerPortInfo;
        private System.Windows.Forms.ComboBox comboBoxDevices;
        private System.Windows.Forms.Panel panelChatbox;
        private System.Windows.Forms.Panel panelOsc;
        private System.Windows.Forms.Label labelChatboxAppearance;
        private System.Windows.Forms.Label labelOsc;
        private System.Windows.Forms.Label labelOscInfo;
        private System.Windows.Forms.ComboBox comboBoxChatboxAppearance;
        private System.Windows.Forms.Label labelBatteryLevel;
        private System.Windows.Forms.PictureBox pictureBoxFooterDiscord;
        private System.Windows.Forms.Panel panelInfoBottom;
        private System.Windows.Forms.LinkLabel linkLabelInfoProjectUrl;
        private System.Windows.Forms.LinkLabel linkLabelInfoAuthorUrl;
        private System.Windows.Forms.Label labelInfoAuthorName;
        private System.Windows.Forms.Label labelInfoAppName;
        private System.Windows.Forms.Panel panelInfoTop;
        private System.Windows.Forms.Panel panelFooter;
        private System.Windows.Forms.PictureBox pictureBoxInfoAuthor;
        private System.Windows.Forms.TabPage tabWebServerSettings;
        private System.Windows.Forms.Panel panelWebServer;
        private System.Windows.Forms.TabPage tabDiscordSettings;
        private System.Windows.Forms.TextBox textBoxOscAddress;
        private System.Windows.Forms.Button buttonOscAddressInfo;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.CheckBox checkBoxUseChatbox;
        private System.Windows.Forms.CheckBox checkBoxUseDiscord;
        private System.Windows.Forms.Label labelDiscordActiveText;
        private System.Windows.Forms.TextBox textBoxDiscordActiveText;
        private System.Windows.Forms.Panel panelDiscordActiveText;
        private System.Windows.Forms.Label labelDiscordStateText;
        private System.Windows.Forms.Panel panelDiscordIdleText;
        private System.Windows.Forms.TextBox textBoxDiscordIdleText;
        private System.Windows.Forms.Label labelDiscordIdleText;
        private System.Windows.Forms.Panel panelDiscordStateText;
        private System.Windows.Forms.TextBox textBoxDiscordStateText;
        private System.Windows.Forms.Label labelDiscordInfo;
        private System.Windows.Forms.Button buttonSaveWebServerSettings;
        private System.Windows.Forms.Button buttonSaveDiscordSettings;
        private System.Windows.Forms.Panel panelWebServerHtml;
        private System.Windows.Forms.TextBox textBoxWebServerHtml;
        private System.Windows.Forms.Label labelWebServerHtml;
    }
}
