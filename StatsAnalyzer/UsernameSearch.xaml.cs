using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
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
    public sealed partial class UsernameSearch : ContentDialog
    {
        FPlayerName? selected = null;
        static readonly string[] EMPTY = new string[0];
        CancellationTokenSource searchCancel = new CancellationTokenSource();
        public UsernameSearch()
        {
            this.InitializeComponent();
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (selected.HasValue)
            {
                StatsPage.RequestPlayerData.Invoke(StatsPage.I.NetClient.connection, selected.Value.Steam64);
                selected = null;
            }
            else args.Cancel = true;
        }
        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {

        }
        private int lastLength = 0;
        private bool runSearch = true;
        private async void autoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            string text = sender.Text.ToLower();
            foreach (object o in sender.Items)
            {
                if (o.ToString().ToLower() == text)
                {
                    return;
                }
            }
            searchCancel.Cancel();
            if (sender.Text.Length < 3)
            {
                sender.ItemsSource = EMPTY;
                lastLength = sender.Text.Length;
                selected = null;
                return;
            } else if (sender.Text.Length == 3 || sender.Items.Count == 0)
            {
                runSearch = true;
            }
            if (!runSearch)
            {
                if (sender.Text.Length > lastLength)
                {
                    if (sender.Items.Count == 0) return;
                    List<object> newList = new List<object>();
                    foreach (object o in sender.Items)
                    {
                        string s = o.ToString().ToLower();
                        if (s.Contains(text))
                        {
                            newList.Add(o);
                        }
                    }
                    sender.ItemsSource = newList;
                    lastLength = sender.Text.Length;
                    selected = null;
                    return;
                }
                else
                {
                    runSearch = true;
                }
            } else
            {
                Debug.WriteLine("3");
                searchCancel.Dispose();
                searchCancel = new CancellationTokenSource();
                List<FPlayerName> suitableItems = await StatsPage.I.SQL.UsernameSearch(text, searchCancel.Token);
                sender.ItemsSource = suitableItems;
                selected = null;
                lastLength = sender.Text.Length;
                runSearch = false;
            }
        }
        private void autoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            object item = args.SelectedItem;
            if (item is FPlayerName name)
            {
                sender.Text = name.PlayerName;
                selected = name;
            }
        }
        private void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            searchBox.ItemsSource = EMPTY;
        }
    }
}
