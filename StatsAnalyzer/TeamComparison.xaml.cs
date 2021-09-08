using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uncreated.Networking;
using Uncreated.Warfare.Stats;
using Windows.ApplicationModel.Core;
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
    public sealed partial class TeamComparison : UserControl
    {




        public Brush WinnerBackground
        {
            get => (Brush)GetValue(WinnerBackgroundProperty);
            set => SetValue(WinnerBackgroundProperty, value);
        }
        public static readonly DependencyProperty WinnerBackgroundProperty =
            DependencyProperty.Register("WinnerBackground", typeof(Brush), typeof(TeamComparison), new PropertyMetadata(new AcrylicBrush() { TintColor = Windows.UI.Color.FromArgb(153, 156, 182, 164) }));
        public Brush LoserBackground
        {
            get => (Brush)GetValue(LoserBackgroundProperty);
            set => SetValue(LoserBackgroundProperty, value);
        }
        public static readonly DependencyProperty LoserBackgroundProperty =
            DependencyProperty.Register("LoserBackground", typeof(Brush), typeof(TeamComparison), new PropertyMetadata(new AcrylicBrush() { TintColor = Windows.UI.Color.FromArgb(153, 255, 112, 125) }));
        public Brush HeaderForeground
        {
            get => (Brush)GetValue(HeaderForegroundProperty);
            set => SetValue(HeaderForegroundProperty, value);
        }
        public static readonly DependencyProperty HeaderForegroundProperty =
            DependencyProperty.Register("HeaderForeground", typeof(Brush), typeof(TeamComparison), new PropertyMetadata(new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255))));

        public Brush ValuesForeground
        {
            get => (Brush)GetValue(ValuesForegroundProperty);
            set => SetValue(ValuesForegroundProperty, value);
        }
        public static readonly DependencyProperty ValuesForegroundProperty =
            DependencyProperty.Register("ValuesForeground", typeof(Brush), typeof(TeamComparison), new PropertyMetadata(new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0))));

        public TeamComparison()
        {
            this.InitializeComponent();
        }
        public void Load(WarfareTeam t1, WarfareTeam t2)
        {
            Load(lblT1Kills, lblT2Kills, gridT1Kills, gridT2Kills, t1.Kills, t2.Kills);
            Load(lblT1Deaths, lblT2Deaths, gridT1Deaths, gridT2Deaths, t1.Deaths, t2.Deaths);
            Load(lblT1KD, lblT2KD, gridT1KD, gridT2KD, (float)t1.Kills / t1.Deaths, (float)t2.Kills / t2.Deaths);
            Load(lblT1Teamkills, lblT2Teamkills, gridT1Teamkills, gridT2Teamkills, t1.Teamkills, t2.Teamkills);
            Load(lblT1Downs, lblT2Downs, gridT1Downs, gridT2Downs, t1.Downs, t2.Downs);
            Load(lblT1Revives, lblT2Revives, gridT1Revives, gridT2Revives, t1.Revives, t2.Revives);
            Load(lblT1Wins, lblT2Revives, gridT1Wins, gridT2Wins, t1.Wins, t2.Wins);
            Load(lblT1Losses, lblT2Losses, gridT1Losses, gridT2Losses, t1.Losses, t2.Losses);
            Load(lblT1VehiclesRequested, lblT2VehiclesRequested, gridT1VehiclesRequested, gridT2VehiclesRequested, t1.VehiclesRequested, t2.VehiclesRequested);
            Load(lblT1VehiclesDestroyed, lblT2VehiclesDestroyed, gridT1VehiclesDestroyed, gridT2VehiclesDestroyed, t1.VehiclesDestroyed, t2.VehiclesDestroyed);
            Load(lblT1FlagsCaptured, lblT2FlagsCaptured, gridT1FlagsCaptured, gridT2FlagsCaptured, t1.FlagsCaptured, t2.FlagsCaptured);
            Load(lblT1FlagsLost, lblT2FlagsLost, gridT1FlagsLost, gridT2FlagsLost, t1.FlagsLost, t2.FlagsLost);
            Load(lblT1FOBsBuilt, lblT2FOBsBuilt, gridT1FOBsBuilt, gridT2FOBsBuilt, t1.FobsBuilt, t2.FobsBuilt);
            Load(lblT1FOBsDestroyed, lblT2FOBsDestroyed, gridT1FOBsDestroyed, gridT2FOBsDestroyed, t1.FobsDestroyed, t2.FobsDestroyed);
            Load(lblT1EmplacementsBuilt, lblT2EmplacementsBuilt, gridT1EmplacementsBuilt, gridT2EmplacementsBuilt, t1.EmplacementsBuilt, t2.EmplacementsBuilt);
            Load(lblT1FortificationsBuilt, lblT2FortificationsBuilt, gridT1FortificationsBuilt, gridT2FortificationsBuilt, t1.FortificationsBuilt, t2.FortificationsBuilt);
            Load(lblT1AveragePlayers, lblT2AveragePlayers, gridT1AveragePlayers, gridT2AveragePlayers, t1.AveragePlayers, t2.AveragePlayers);
        }
        private void Load(TextBlock tb1, TextBlock tb2, Grid g1, Grid g2, uint t1v, uint t2v)
        {
            tb1.Text = t1v.ToString(StatsPage.Locale);
            tb2.Text = t2v.ToString(StatsPage.Locale);
            if (t1v > t2v)
            {
                g1.Background = WinnerBackground;
                g2.Background = LoserBackground;
            } 
            else if (t1v == t2v)
            {
                g1.Background = WinnerBackground;
                g2.Background = WinnerBackground;
            } else
            {
                g1.Background = LoserBackground;
                g2.Background = WinnerBackground;
            }
        }
        private void Load(TextBlock tb1, TextBlock tb2, Grid g1, Grid g2, float t1v, float t2v)
        {
            tb1.Text = t1v.ToString("N3", StatsPage.Locale);
            tb2.Text = t2v.ToString("N3", StatsPage.Locale);
            if (t1v > t2v)
            {
                g1.Background = WinnerBackground;
                g2.Background = LoserBackground;
            } 
            else if (t1v == t2v)
            {
                g1.Background = WinnerBackground;
                g2.Background = WinnerBackground;
            } else
            {
                g1.Background = LoserBackground;
                g2.Background = WinnerBackground;
            }
        }

        internal static NetCallRaw<WarfareTeam, WarfareTeam> SendTeams =
            new NetCallRaw<WarfareTeam, WarfareTeam>(2013, WarfareTeam.Read, WarfareTeam.Read, WarfareTeam.Write, WarfareTeam.Write);
        [NetCall(ENetCall.FROM_SERVER, 2013)]
        internal static void ReceiveTeams(in IConnection connection, WarfareTeam team1, WarfareTeam team2)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => {
                await StatsPage.I.UpdateTeams(team1, team2);
            }).AsTask().ConfigureAwait(false);
        }
    }
}
