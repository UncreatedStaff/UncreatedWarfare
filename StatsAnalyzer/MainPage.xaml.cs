using System;
using System.Collections.Generic;
using System.Diagnostics;
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
namespace StatsAnalyzer
{
    public sealed partial class StatsPage : Page
    {
        public static readonly IFormatProvider Locale = new System.Globalization.CultureInfo("en-US");
        public static readonly string SaveDirectory = Environment.GetEnvironmentVariable("APPDATA") + @"\Uncreated\";
        public static readonly string StatsDirectory = SaveDirectory + @"Players\";
        public static readonly string WeaponsDirectory = SaveDirectory + @"Weapons\";
        public static readonly string KitsDirectory = SaveDirectory + @"Kits\";
        public Settings Settings;
        public static StatsPage I;
        public EMode CurrentMode;
        public WarfareStats CurrentSingleOrA;
        public WarfareStats CurrentB;
        public DatabaseManager SQL;

        public Steam64Find S64Search = new Steam64Find();
        public SettingsDialog SettingsDialog = new SettingsDialog();
        public StatsPage()
        {
            InitializeComponent(); 
            I = this;
            this.Loaded += StatsPage_Loaded;
        }

        private async void StatsPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadSettings();
            if (Settings.SQL.Database.Length != 0 && Settings.SQL.Username.Length != 0 && Settings.SQL.Password.Length != 0)
            {
                SQL = new DatabaseManager(Settings.SQL, true);
                await SQL.Open();
            }
            if (Settings.LastSteam64 != 0)
                S64Search.TextBoxText = Settings.LastSteam64.ToString(Locale);
        }

        public async Task LoadSettings()
        {

            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = null;
            if (folder != null)
                try
                {
                    file = await folder.GetFileAsync("settings.dat");
#if false //reset settings
                    await Settings.IO.WriteTo(Settings.Default, folder, "settings.dat");
                    return;
#endif
                }
                catch (FileNotFoundException)
                {
                    file = null;
                }
            if (file == null)
            {
                await Settings.IO.WriteTo(Settings.Default, folder, "settings.dat");
                try 
                {
                    file = await folder.GetFileAsync("settings.dat");
                }
                catch (FileNotFoundException)
                {
                    return;
                }
            }
            Settings = await Settings.IO.ReadFrom(file);
            if (Settings.DATA_VERSION != Settings.CURRENT_DATA_VERSION)
            { 
                Settings.DATA_VERSION = Settings.CURRENT_DATA_VERSION;
                await Settings.IO.WriteTo(Settings, folder, "settings.dat");
            }
        } 
        public async Task SaveSettings()
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            if (folder != null)
                await Settings.IO.WriteTo(Settings, folder, "settings.dat");
        }
        private void ClickApplication_Exit(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }
        private async void ClickApplication_Settings(object sender, RoutedEventArgs e)
        {
            SettingsDialog.Load();
            await SettingsDialog.ShowAsync();
        }
        private async void ClickSearch_Find(object sender, RoutedEventArgs e)
        {
            await S64Search.ShowAsync();
        }
        private void ClickSearch_Compare(object sender, RoutedEventArgs e)
        {

        }
        private void Click_Query(object sender, RoutedEventArgs e)
        {

        }
        public async Task Update()
        {
            Debug.WriteLine("Updating UI");
            if (CurrentMode == EMode.SINGLE)
            {
                if (CurrentSingleOrA == null)
                {
                    Debug.WriteLine("Current is null");
                    return;
                }
                await SingleStatPage.Load(CurrentSingleOrA);
                SingleStatPage.Visibility = Visibility.Visible;
            }
        }
    }
    public enum EMode : byte
    {
        SINGLE,
        COMPARE,
        QUERY
    }
}
