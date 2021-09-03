using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uncreated.Warfare.Stats;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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
    public sealed partial class Steam64Find : ContentDialog
    {
        public Steam64Find()
        {
            this.InitializeComponent();
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (Steam64TextBox.Text.StartsWith("765") && ulong.TryParse(Steam64TextBox.Text, System.Globalization.NumberStyles.Any, StatsPage.Locale, out ulong Steam64))
            {
                string filename = Steam64.ToString(StatsPage.Locale) + ".dat";
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(StatsPage.StatsDirectory);
                StorageFile file = null;
                if (folder != null)
                    try
                    {
                        file = await folder.GetFileAsync(filename);
                    }
                    catch (System.IO.FileNotFoundException)
                    {
                        file = null;
                    }
                if (file == null)
                {
                    ErrorText.Text = "No stats file at \"" + folder != null ? folder.Path : "null" + filename + "\".";
                    args.Cancel = true;
                } 
                else if (WarfareStats.IO.ReadFrom(file.Path, out WarfareStats stats))
                {
                    ErrorText.Text = string.Empty;
                    StatsPage.I.CurrentMode = EMode.SINGLE;
                    StatsPage.I.CurrentSingleOrA = stats;
                    StatsPage.I.CurrentB = null;
                    sender.Hide();
                }
                else
                {
                    ErrorText.Text = "Unable to find player.";
                    args.Cancel = true;
                }
            }
            else
            {
                ErrorText.Text = "Couldn't parse a Steam64 ID.";
                args.Cancel = true;
            }
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            sender.Hide();
        }
    }
}
