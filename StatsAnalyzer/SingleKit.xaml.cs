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
    public sealed partial class SingleKit : UserControl
    {
        public double TitleSize
        {
            get => (double)GetValue(TitleSizeProperty);
            set => SetValue(TitleSizeProperty, value);
        }
        public static readonly DependencyProperty TitleSizeProperty =
            DependencyProperty.Register("TitleSize", typeof(double), typeof(SingleKit), new PropertyMetadata(32));
        public double Header2Size
        {
            get => (double)GetValue(Header2SizeProperty);
            set => SetValue(Header2SizeProperty, value);
        }
        public static readonly DependencyProperty Header2SizeProperty =
            DependencyProperty.Register("Header2Size", typeof(double), typeof(SingleKit), new PropertyMetadata(24));
        public double Header3Size
        {
            get => (double)GetValue(Header3SizeProperty);
            set => SetValue(Header3SizeProperty, value);
        }
        public static readonly DependencyProperty Header3SizeProperty =
            DependencyProperty.Register("Header3Size", typeof(double), typeof(SingleKit), new PropertyMetadata(18));
        public double ValueSize
        {
            get => (double)GetValue(ValueSizeProperty);
            set => SetValue(ValueSizeProperty, value);
        }
        public static readonly DependencyProperty ValueSizeProperty =
            DependencyProperty.Register("ValueSize", typeof(double), typeof(SingleKit), new PropertyMetadata(18));
        public SingleKit()
        {
            this.InitializeComponent();
        }
        public void Load(WarfareKit kit, string signtext, EClass @class)
        {
            lblKitClass.Text = @class.ToString();
            if (StatsPage.I.Classes.TryGetValue(@class, out ClassConfig val))
                lblClassIcon.Text = val.Icon.ToString();
            else 
                lblClassIcon.Text = string.Empty;
            lblKitName.Text = kit.KitID + " - " + signtext;
            lblTotalKills.Text = kit.Kills.ToString(StatsPage.Locale);
            lblTotalDeaths.Text = kit.Deaths.ToString(StatsPage.Locale);
            lblRequests.Text = kit.TimesRequested.ToString(StatsPage.Locale);
            lblAverageGunKilDistance.Text = kit.AverageGunKillDistance.ToString("N2", StatsPage.Locale);
            lblFlagsCaptured.Text = kit.FlagsCaptured.ToString(StatsPage.Locale);
        }




        internal static readonly NetCallRaw<WarfareKit, string, byte> SendKitData =
            new NetCallRaw<WarfareKit, string, byte>(2003, WarfareKit.Read, R => R.ReadString(),
                R => R.ReadUInt8(), WarfareKit.Write, (W, S) => W.Write(S), (W, E) => W.Write(E));
        [NetCall(ENetCall.FROM_SERVER, 2003)]
        internal static void ReceiveKitData(in IConnection connection, WarfareKit kit, string signtext, byte @class)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => {
                await StatsPage.I.UpdateKit(kit, signtext, (EClass)@class);
            }).AsTask().ConfigureAwait(false);
        }
    }
}
