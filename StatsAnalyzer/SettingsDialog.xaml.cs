using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace StatsAnalyzer
{
    public sealed partial class SettingsDialog : ContentDialog
    {
        public SettingsDialog()
        {
            this.InitializeComponent();
        }
        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            bool reloadSQL =
                StatsPage.I.Settings.SQL.Host != txtIPAddress.Text ||
                StatsPage.I.Settings.SQL.Database != txtDatabase.Text ||
                StatsPage.I.Settings.SQL.Username != txtUsername.Text ||
                StatsPage.I.Settings.SQL.Password != txtPassword.Text ||
                StatsPage.I.Settings.SQL.CharSet != txtCharset.Text ||
                StatsPage.I.Settings.SQL.Port.ToString() != txtPort.Text;
            bool reloadTCP =
                StatsPage.I.Settings.Identity != txtIdentity.Text ||
                StatsPage.I.Settings.TCPServerIP != txtTCPIP.Text ||
                StatsPage.I.Settings.TCPServerPort.ToString() != txtTCPPort.Text;
            StatsPage.I.Settings.SQL.Host = txtIPAddress.Text;
            _ = ushort.TryParse(txtPort.Text, System.Globalization.NumberStyles.Any, StatsPage.Locale, out StatsPage.I.Settings.SQL.Port);
            StatsPage.I.Settings.SQL.Database = txtDatabase.Text;
            StatsPage.I.Settings.SQL.Username = txtUsername.Text;
            StatsPage.I.Settings.SQL.Password = txtPassword.Text;
            StatsPage.I.Settings.SQL.CharSet = txtCharset.Text;
            StatsPage.I.Settings.Identity = txtIdentity.Text;
            StatsPage.I.Settings.TCPServerIP = txtTCPIP.Text;
            _ = ushort.TryParse(txtTCPPort.Text, System.Globalization.NumberStyles.Any, StatsPage.Locale, out StatsPage.I.Settings.TCPServerPort);
            await StatsPage.I.SaveSettings();
            if (reloadSQL)
            {
                await StatsPage.I.SQL?.DisposeAsync();
                StatsPage.I.SQL = new DatabaseManager(StatsPage.I.Settings.SQL, true);
                await StatsPage.I.SQL.Open();
            }
            if (reloadTCP)
            {
                StatsPage.I.ReloadTCP();
            }
        }
        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            this.Hide();
        }
        public void Load()
        {
            txtIPAddress.Text = StatsPage.I.Settings.SQL.Host;
            txtPort.Text = StatsPage.I.Settings.SQL.Port.ToString();
            txtDatabase.Text = StatsPage.I.Settings.SQL.Database;
            txtUsername.Text = StatsPage.I.Settings.SQL.Username;
            txtPassword.Text = StatsPage.I.Settings.SQL.Password;
            txtCharset.Text = StatsPage.I.Settings.SQL.CharSet;
            txtIdentity.Text = StatsPage.I.Settings.Identity;
            txtTCPIP.Text = StatsPage.I.Settings.TCPServerIP;
            txtTCPPort.Text = StatsPage.I.Settings.TCPServerPort.ToString();
        }
    }
}
