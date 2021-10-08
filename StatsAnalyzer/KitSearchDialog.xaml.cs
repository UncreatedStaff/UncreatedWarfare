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
    public sealed partial class KitSearchDialog : ContentDialog
    {
        public KitSearchDialog()
        {
            this.InitializeComponent();
        }
        public object KitListSource { get => searchBox.ItemsSource; set => searchBox.ItemsSource = value; }
        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (searchBox.Text.Length == 0)
            {
                args.Cancel = true;
                return;
            }

            if (StatsPage.I.NetClient == null)
            {
                StatsPage.I.SendMessage("NO CONNECTION", "Not connected to TCP Server.").ConfigureAwait(false);
                return;
            }
            StatsPage.RequestKitData.Invoke(StatsPage.I.NetClient.connection, searchBox.Text.ToLower());
            this.Hide();
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            this.Hide();
        }

        private void autoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (sender.Text.Length == 0)
                sender.ItemsSource = StatsPage.I.KitList.Length == 0 ? new string[1] { "No kits found." } : StatsPage.I.KitList;
            List<string> suitableItems = new List<string>();
            string text = sender.Text.ToLower();
            foreach (string kit in StatsPage.I.KitList)
            {
                if (kit.Contains(text)) suitableItems.Add(kit);
            }
            if (suitableItems.Count == 0)
                suitableItems.Add("No kits found.");
            sender.ItemsSource = suitableItems;
        }
        private void autoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            string item = args.SelectedItem.ToString();
            if (item != "Awaiting kit list..." && item != "No kits found.")
                sender.Text = item;
        }
        private void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            searchBox.ItemsSource = StatsPage.I.KitList.Length == 0 ? new string[1] { "Awaiting kit list..." } : StatsPage.I.KitList;
            StatsPage.RequestKitList.Invoke(StatsPage.I.NetClient.connection);
        }
    }
}
