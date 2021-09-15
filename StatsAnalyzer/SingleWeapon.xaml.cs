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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace StatsAnalyzer
{
    public sealed partial class SingleWeapon : UserControl
    {
        public double TitleSize
        {
            get => (double)GetValue(TitleSizeProperty);
            set => SetValue(TitleSizeProperty, value);
        }
        public static readonly DependencyProperty TitleSizeProperty =
            DependencyProperty.Register("TitleSize", typeof(double), typeof(SingleWeapon), new PropertyMetadata(32));
        public double Header2Size
        {
            get => (double)GetValue(Header2SizeProperty);
            set => SetValue(Header2SizeProperty, value);
        }
        public static readonly DependencyProperty Header2SizeProperty =
            DependencyProperty.Register("Header2Size", typeof(double), typeof(SingleWeapon), new PropertyMetadata(24));
        public double Header3Size
        {
            get => (double)GetValue(Header3SizeProperty);
            set => SetValue(Header3SizeProperty, value);
        }
        public static readonly DependencyProperty Header3SizeProperty =
            DependencyProperty.Register("Header3Size", typeof(double), typeof(SingleWeapon), new PropertyMetadata(18));
        public double ValueSize
        {
            get => (double)GetValue(ValueSizeProperty);
            set => SetValue(ValueSizeProperty, value);
        }
        public static readonly DependencyProperty ValueSizeProperty =
            DependencyProperty.Register("ValueSize", typeof(double), typeof(SingleWeapon), new PropertyMetadata(18));
        public SingleWeapon()
        {
            this.InitializeComponent();
        }
        public void Load(WarfareWeapon weapon, string name, string kitname, BitmapImage image)
        {
            if (image == null)
            {
                if (!StatsPage.I.IsCached(weapon.ID, false, out image))
                    image = null;
            }
            else 
                StatsPage.I.Cache(weapon.ID, false, image);
            if (image != null)
            {
                imgBorder.Width = image.PixelWidth / 8;
                imgBorder.Height = image.PixelHeight / 8;
            }
            imgWeaponIcon.Source = image;
            lblWeaponName.Text = $"{name} - {weapon.ID} - {weapon.KitID}";
            lblKitClass.Text = kitname;
            lblTotalKills.Text = weapon.Kills.ToString(StatsPage.Locale);
            lblTotalDeaths.Text = weapon.Deaths.ToString(StatsPage.Locale);
            lblTotalDowns.Text = weapon.Downs.ToString(StatsPage.Locale);
            lblAverageGunKillDistance.Text = weapon.AverageKillDistance.ToString("N2", StatsPage.Locale);
            lblSkullKills.Text = weapon.SkullKills.ToString(StatsPage.Locale);
            lblBodyKills.Text = weapon.BodyKills.ToString(StatsPage.Locale);
            lblArmKills.Text = weapon.ArmKills.ToString(StatsPage.Locale);
            lblLegKills.Text = weapon.LegKills.ToString(StatsPage.Locale);
        }
        /// <summary>Includes the gun icon.</summary>
        internal static readonly NetCallRaw<WarfareWeapon, string, string, byte[]> SendWeaponData =
            new NetCallRaw<WarfareWeapon, string, string, byte[]>(2017, WarfareWeapon.Read, R => R.ReadString(), R => R.ReadString(), F.ReadByteArray, WarfareWeapon.Write, (W, S) => W.Write(S), (W, S) => W.Write(S), F.WriteByteArray);

        [NetCall(ENetCall.FROM_SERVER, 2017)]
        internal static void ReceiveWeaponData(in IConnection connection, WarfareWeapon weapon, string name, string kitname, byte[] png)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                BitmapImage img = png.Length > 0 ? await F.DecodeImage(png) : null;
                await StatsPage.I.UpdateWeapon(weapon, name, kitname, img);
            }).AsTask().ConfigureAwait(false);
        }
    }
}
