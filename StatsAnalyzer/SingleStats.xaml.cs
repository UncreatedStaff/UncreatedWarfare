using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
        public async Task Load(WarfareStats stats)
        {
            lblSteam64.Text = stats.Steam64.ToString(StatsPage.Locale);
            FPlayerName names = await StatsPage.I.SQL.GetUsernames(stats.Steam64);
            lblPlayerName.Text = names.PlayerName;
            lblCharacterName.Text = names.CharacterName;
            lblNickName.Text = names.NickName;

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
        }
    }
}
