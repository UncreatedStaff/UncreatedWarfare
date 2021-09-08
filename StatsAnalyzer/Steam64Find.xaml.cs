using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
        public string TextBoxText { get => Steam64TextBox.Text; set => Steam64TextBox.Text = value; }
        public delegate Task ClickedCallback(Steam64Find box, ContentDialogButtonClickEventArgs args);
        public ClickedCallback OkCallback { get; set; }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            try
            {
                await OkCallback.Invoke(this, args);
            }
            catch (UnauthorizedAccessException)
            {
                await StatsPage.I.FSWarn.ShowAsync();
            }
        }
        public string Error { get => ErrorText.Text; set => ErrorText.Text = value; }
        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            sender.Hide();
        }
    }
}
