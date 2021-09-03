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
namespace StatsAnalyzer
{
    public sealed partial class StatsPage : Page
    {
        public static readonly IFormatProvider Locale = new System.Globalization.CultureInfo("en-US");
        public static readonly string SaveDirectory = Environment.GetEnvironmentVariable("APPDATA") + @"\Uncreated\";
        public static readonly string StatsDirectory = SaveDirectory + @"Players\";
        public static readonly string WeaponsDirectory = SaveDirectory + @"Weapons\";
        public static readonly string KitsDirectory = SaveDirectory + @"Kits\";
        public static StatsPage I;
        public EMode CurrentMode;
        public WarfareStats CurrentSingleOrA;
        public WarfareStats CurrentB;
        public StatsPage()
        {
            InitializeComponent();
            I = this;
        }

        private void ClickApplication_Exit(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }
        private async void ClickSearch_Find(object sender, RoutedEventArgs e)
        {
            Steam64Find find = new Steam64Find();
            await find.ShowAsync();
        }
        private void ClickSearch_Compare(object sender, RoutedEventArgs e)
        {

        }
        private void Click_Query(object sender, RoutedEventArgs e)
        {

        }
    }
    public enum EMode : byte
    {
        SINGLE,
        COMPARE,
        QUERY
    }
}
