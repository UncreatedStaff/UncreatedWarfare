using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Uncreated.Networking;
using Uncreated.Warfare.Stats;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace StatsAnalyzer
{
    public sealed partial class SingleStats : UserControl
    {
        public double TitleSize
        {
            get => (double)GetValue(TitleSizeProperty);
            set => SetValue(TitleSizeProperty, value);
        }
        public static readonly DependencyProperty TitleSizeProperty =
            DependencyProperty.Register("TitleSize", typeof(double), typeof(SingleStats), new PropertyMetadata(32));
        public double Header2Size
        {
            get => (double)GetValue(Header2SizeProperty);
            set => SetValue(Header2SizeProperty, value);
        }
        public static readonly DependencyProperty Header2SizeProperty =
            DependencyProperty.Register("Header2Size", typeof(double), typeof(SingleStats), new PropertyMetadata(24));
        public double Header3Size
        {
            get => (double)GetValue(Header3SizeProperty);
            set => SetValue(Header3SizeProperty, value);
        }
        public static readonly DependencyProperty Header3SizeProperty =
            DependencyProperty.Register("Header3Size", typeof(double), typeof(SingleStats), new PropertyMetadata(18));
        public double ValueSize
        {
            get => (double)GetValue(ValueSizeProperty);
            set => SetValue(ValueSizeProperty, value);
        }
        public static readonly DependencyProperty ValueSizeProperty =
            DependencyProperty.Register("ValueSize", typeof(double), typeof(SingleStats), new PropertyMetadata(18));



        public SingleStats()
        {
            this.InitializeComponent();
        }
        const int COLUMN_COUNT = 4;
        public async Task Load(WarfareStats stats, bool online = false)
        {
            pnlKits.Children.Clear();
            lblSteam64.Text = stats.Steam64.ToString(StatsPage.Locale) + (online ? " - ONLINE" : string.Empty);
            try
            {
                if (StatsPage.I.SQL != null)
                {
                    FPlayerName names = await StatsPage.I.SQL.GetUsernames(stats.Steam64);
                    lblPlayerName.Text = names.PlayerName;
                    lblCharacterName.Text = names.CharacterName;
                    lblNickName.Text = names.NickName;
                } 
                else
                {
                    lblPlayerName.Text = "No SQL Connection";
                    lblCharacterName.Text = "No SQL Connection";
                    lblNickName.Text = "No SQL Connection";
                }
            } 
            catch
            {
                lblPlayerName.Text = "Not connected.";
                lblCharacterName.Text = "Not connected.";
                lblNickName.Text = "Not connected.";
            }

            lblTotalKills.Text = stats.Kills.ToString(StatsPage.Locale);
            lblTotalDeaths.Text = stats.Deaths.ToString(StatsPage.Locale);
            lblTotalTeamkills.Text = stats.Teamkills.ToString(StatsPage.Locale);
            lblTotalDowns.Text = stats.Downs.ToString(StatsPage.Locale);
            lblTotalRevives.Text = stats.Revives.ToString(StatsPage.Locale);
            lblAttackKills.Text = stats.KillsWhileAttackingFlags.ToString(StatsPage.Locale);
            lblDefenceKills.Text = stats.KillsWhileDefendingFlags.ToString(StatsPage.Locale);
            lblWins.Text = stats.Wins.ToString(StatsPage.Locale);
            lblLosses.Text = stats.Losses.ToString(StatsPage.Locale);
            lblVehiclesRequested.Text = stats.VehiclesRequested.ToString(StatsPage.Locale);
            lblVehiclesDestroyed.Text = stats.VehiclesDestroyed.ToString(StatsPage.Locale);
            lblFlagCaptures.Text = stats.FlagsCaptured.ToString(StatsPage.Locale);
            lblFlagLosses.Text = stats.FlagsLost.ToString(StatsPage.Locale);
            lblFOBsBuilt.Text = stats.FobsBuilt.ToString(StatsPage.Locale);
            lblFOBsDestroyed.Text = stats.FobsDestroyed.ToString(StatsPage.Locale);
            lblEmplacementsBuilt.Text = stats.EmplacementsBuilt.ToString(StatsPage.Locale);
            lblFortificationsBuilt.Text = stats.FortificationsBuilt.ToString(StatsPage.Locale);
            lblTotalPlaytime.Text = F.GetTimeFromMinutes(stats.PlaytimeMinutes);
            pnlKits.ColumnDefinitions.Clear();
            pnlKits.RowDefinitions.Clear();
            int columns = Math.Min(stats.Kits.Count, COLUMN_COUNT);
            int rows = (stats.Kits.Count / COLUMN_COUNT) + 1;
            for (int i = 0; i < columns; i++)
                pnlKits.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < rows; i++)
                pnlKits.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < stats.Kits.Count; i++)
            {
                KitData dataCtrl = new KitData()
                {
                    //Margin = new Thickness(4, 0, 4, 0),
                    Padding = new Thickness(0, 0, 10, 10),
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                dataCtrl.PointerReleased += KitPointerReleased;
                dataCtrl.PointerEntered += KitPointerEntered;
                dataCtrl.PointerExited += KitPointerExited;
                dataCtrl.Load(stats.Kits[i], stats.Steam64);
                pnlKits.Children.Add(dataCtrl);
                Grid.SetRow(dataCtrl, i / COLUMN_COUNT);
                Grid.SetColumn(dataCtrl, i % COLUMN_COUNT);
            }
        }

        private void KitPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is KitData data)
                data.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(50, 255, 255, 255));
        }
        private void KitPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is KitData data)
                data.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(25, 255, 255, 255));
        }
        private void KitPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (StatsPage.I.NetClient == null)
            {
                StatsPage.I.SendMessage("NO CONNECTION", "Not connected to TCP Server.").ConfigureAwait(false);
                return;
            }
            if (sender is KitData data && data.KitName != null)
                StatsPage.RequestKitData.Invoke(StatsPage.I.NetClient.connection, data.KitName);
        }

        internal static readonly NetCallRaw<WarfareStats, bool> SendPlayerData =
            new NetCallRaw<WarfareStats, bool>(2001, WarfareStats.Read, R => R.ReadBool(), WarfareStats.Write, (W, B) => W.Write(B));
        [NetCall(ENetCall.FROM_SERVER, 2001)]
        internal static void ReceivePlayerData(in IConnection connection, WarfareStats stats, bool isOnline)
        {
            Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => {
                await StatsPage.I.UpdateSinglePlayer(stats, isOnline);
            }).AsTask().ConfigureAwait(false);
        }
    }
}
