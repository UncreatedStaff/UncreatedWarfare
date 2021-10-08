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
    public sealed partial class SingleVehicle : UserControl
    {
        public double TitleSize
        {
            get => (double)GetValue(TitleSizeProperty);
            set => SetValue(TitleSizeProperty, value);
        }
        public static readonly DependencyProperty TitleSizeProperty =
            DependencyProperty.Register("TitleSize", typeof(double), typeof(SingleVehicle), new PropertyMetadata(32));
        public double Header2Size
        {
            get => (double)GetValue(Header2SizeProperty);
            set => SetValue(Header2SizeProperty, value);
        }
        public static readonly DependencyProperty Header2SizeProperty =
            DependencyProperty.Register("Header2Size", typeof(double), typeof(SingleVehicle), new PropertyMetadata(24));
        public double Header3Size
        {
            get => (double)GetValue(Header3SizeProperty);
            set => SetValue(Header3SizeProperty, value);
        }
        public static readonly DependencyProperty Header3SizeProperty =
            DependencyProperty.Register("Header3Size", typeof(double), typeof(SingleVehicle), new PropertyMetadata(18));
        public double ValueSize
        {
            get => (double)GetValue(ValueSizeProperty);
            set => SetValue(ValueSizeProperty, value);
        }
        public static readonly DependencyProperty ValueSizeProperty =
            DependencyProperty.Register("ValueSize", typeof(double), typeof(SingleVehicle), new PropertyMetadata(18));
        public SingleVehicle()
        {
            this.InitializeComponent();
        }
        public void Load(WarfareVehicle vehicle, string name, BitmapImage image)
        {
            if (image == null)
            {
                if (!StatsPage.I.IsCached(vehicle.ID, true, out image))
                    image = null;
            }
            else 
                StatsPage.I.Cache(vehicle.ID, true, image);
            if (image != null)
            {
                imgBorder.Width = image.PixelWidth / 8;
                imgBorder.Height = image.PixelHeight / 8;
            }
            imgVehicleIcon.Source = image;
            lblVehicleName.Text = $"{name} - {vehicle.ID}";
            lblTotalRequests.Text = vehicle.TimesRequested.ToString(StatsPage.Locale);
            lblTotalDestroys.Text = vehicle.TimesDestroyed.ToString(StatsPage.Locale);
            lblTotalGunnerKills.Text = vehicle.KillsWithGunner.ToString(StatsPage.Locale);
        }
        /// <summary>Includes the vehicle icon.</summary>
        internal static readonly NetCallRaw<WarfareVehicle, string, byte[]> SendVehicleData =
            new NetCallRaw<WarfareVehicle, string, byte[]>(2018, WarfareVehicle.Read, R => R.ReadString(), F.ReadByteArray, WarfareVehicle.Write, (W, S) => W.Write(S), F.WriteByteArray);

        [NetCall(ENetCall.FROM_SERVER, 2017)]
        internal static void ReceiveVehicleData(in IConnection connection, WarfareVehicle weapon, string name, byte[] png)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                BitmapImage img = png.Length > 0 ? await F.DecodeImage(png) : null;
                await StatsPage.I.UpdateVehicle(weapon, name, img);
            }).AsTask().ConfigureAwait(false);
        }
    }
}
