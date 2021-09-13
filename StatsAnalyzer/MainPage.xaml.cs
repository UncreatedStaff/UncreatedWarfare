using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Uncreated.Networking;
using Uncreated.Warfare.Stats;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
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
        public WarfareKit CurrentKitA;
        public WarfareTeam CurrentTeam1;
        public WarfareTeam CurrentTeam2;
        public DatabaseManager SQL;
        public MessageBox Messager = new MessageBox();
        public Client NetClient;
        public List<ImageCache> ImageCache = new List<ImageCache>();
        ~StatsPage()
        {
            SQL.Dispose();
            NetClient.Dispose();
        }
        public void AddCache(ImageCache cache)
        {
            if (ImageCache.Count > 30) // keep list < 30
            {
                ImageCache.RemoveAt(0);
            }
            ImageCache.Add(cache);
        }
        public async void SendMessage(string title, string message)
        {
            Messager.Title = title;
            Messager.Message = message;
            Messager.PrimaryButtonText = string.Empty;
            await Messager.ShowAsync();
        }
        internal static NetCall SendNoServer = new NetCall(2014);
        [NetCall(ENetCall.FROM_SERVER, 2014)]
        internal static void ReceiveNoServer(in IConnection connection)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                I.SendMessage("Not Connected", "The intermediary server has no connection to Uncreated Warfare.");
            }).AsTask().ConfigureAwait(false);
        }
        public void ClearCache() => ImageCache.Clear();
        public bool IsCached(ushort ID, bool Vehicle, out Image Image)
        {
            for (int i = 0; i < ImageCache.Count; i++)
            {
                if (ImageCache[i].ID == ID && ImageCache[i].Vehicle == Vehicle)
                {
                    Image = ImageCache[i].Image;
                    return true;
                }
            }
            Image = null;
            return false;
        }
        public bool IsCached(ushort ID, bool Vehicle)
        {
            for (int i = 0; i < ImageCache.Count; i++)
            {
                if (ImageCache[i].ID == ID && ImageCache[i].Vehicle == Vehicle)
                {
                    return true;
                }
            }
            return false;
        }
        public Steam64Find S64Search = new Steam64Find();
        public NoAccessToFileSystem FSWarn = new NoAccessToFileSystem();
        public UsernameSearch UsernameFind = new UsernameSearch();
        public SettingsDialog SettingsDialog = new SettingsDialog();
        public string[] KitList = new string[0];
        public KitSearchDialog KitSearch = new KitSearchDialog();
        public Dictionary<EClass, ClassConfig> Classes = new Dictionary<EClass, ClassConfig>
            {
                { EClass.NONE, new ClassConfig('±', 36101, 36131) },
                { EClass.UNARMED, new ClassConfig('±', 36101, 36131) },
                { EClass.SQUADLEADER, new ClassConfig('¦', 36102, 36132) },
                { EClass.RIFLEMAN, new ClassConfig('¡', 36103, 36133) },
                { EClass.MEDIC, new ClassConfig('¢', 36104, 36134) },
                { EClass.BREACHER, new ClassConfig('¤', 36105, 36135) },
                { EClass.AUTOMATIC_RIFLEMAN, new ClassConfig('¥', 36106, 36136) },
                { EClass.GRENADIER, new ClassConfig('¬', 36107, 36137) },
                { EClass.MACHINE_GUNNER, new ClassConfig('«', 36108, 36138) },
                { EClass.LAT, new ClassConfig('®', 36109, 36139) },
                { EClass.HAT, new ClassConfig('¯', 36110, 36140) },
                { EClass.MARKSMAN, new ClassConfig('¨', 36111, 36141) },
                { EClass.SNIPER, new ClassConfig('£', 36112, 36142) },
                { EClass.AP_RIFLEMAN, new ClassConfig('©', 36113, 36143) },
                { EClass.COMBAT_ENGINEER, new ClassConfig('ª', 36114, 36144) },
                { EClass.CREWMAN, new ClassConfig('§', 36115, 36145) },
                { EClass.PILOT, new ClassConfig('°', 36116, 36146) },
                { EClass.SPEC_OPS, new ClassConfig('À', 36117, 36147) },
            };
        public StatsPage()
        {
            InitializeComponent();
            this.Unloaded += StatsPage_Unloaded;
            I = this;
            Loaded += StatsPage_Loaded;
            Logging.OnLog += (M, C) => Debug.WriteLine(" -==- INF: " + M);
            Logging.OnLogWarning += (M, C) => Debug.WriteLine(" -==- WRN: " + M);
            Logging.OnLogError += (M, C) => Debug.WriteLine(" -==- ERR: " + M);
            Logging.OnLogException += (M, C) => Debug.WriteLine(" -==- EXC: " + M.ToString());
            NetFactory.RegisterNetMethods(System.Reflection.Assembly.GetExecutingAssembly(), ENetCall.FROM_SERVER);
        }

        private void StatsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            SQL.Dispose();
            NetClient.Dispose();
        }
        public void ReloadTCP()
        {
            if (NetClient != null)
            {
                NetClient.connection.Close();
                NetClient.Dispose();
            }
            Debug.WriteLine("Attempting a connection to a TCP server.");
            NetClient = new Client(Settings.TCPServerIP, Settings.TCPServerPort, Settings.Identity);
            NetClient.AssertConnected();
            NetClient.connection.OnReceived += ClientReceived;
        }
        private void ClientReceived(byte[] bytes, IConnection connection)
        {
            Debug.WriteLine("Received " + bytes.Length + " bytes over " + connection.Identity);
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
            if (Settings.Identity.Length > 0)
            {
                ReloadTCP();
            }
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
            S64Search.OkCallback = Steam64SearchOK;
            await S64Search.ShowAsync();
        }
        private void ClickSearch_Compare(object sender, RoutedEventArgs e)
        {

        }
        private void Click_Query(object sender, RoutedEventArgs e)
        {

        }
        private async void ClickRequest_Player(object sender, RoutedEventArgs e)
        {
            S64Search.OkCallback = Steam64NetSearch;
            await S64Search.ShowAsync();
        }
        private async void ClickRequest_PlayerName(object sender, RoutedEventArgs e)
        {
            await UsernameFind.ShowAsync();
        }
        private async void ClickRequest_Kit(object sender, RoutedEventArgs e)
        {
            await KitSearch.ShowAsync();
        }
        private void ClickRequest_Team(object sender, RoutedEventArgs e)
        {
            RequestTeamsData.Invoke(NetClient.connection);
        }
        private void ClickRequest_Vehicle(object sender, RoutedEventArgs e)
        {
            RequestVehicleData.Invoke(NetClient.connection, 140, true);
        }
        private void ClickRequest_Weapon(object sender, RoutedEventArgs e)
        {
            RequestAllWeapons.Invoke(NetClient.connection, 31308);
            //RequestWeaponData.Invoke(NetClient.connection, 31301, "rurif3", true);
        }
        public async Task UpdateWeaponList(WarfareWeapon[] weapons, string weaponname, string[] kitnames)
        {
            CurrentMode = EMode.SINGLE;
            SingleKitPage.Visibility = Visibility.Collapsed;
            TeamComparePage.Visibility = Visibility.Collapsed;
            SingleWeaponPage.Visibility = Visibility.Collapsed;
            SingleStatPage.Visibility = Visibility.Collapsed;
            WeaponList.Load(weapons, weaponname, kitnames);
            WeaponList.Visibility = Visibility.Visible;
            await Task.Yield();
        }
        public async Task UpdateSinglePlayer(WarfareStats stats, bool isOnline)
        {
            CurrentMode = EMode.SINGLE;
            SingleKitPage.Visibility = Visibility.Collapsed;
            TeamComparePage.Visibility = Visibility.Collapsed;
            SingleWeaponPage.Visibility = Visibility.Collapsed;
            WeaponList.Visibility = Visibility.Collapsed;
            await SingleStatPage.Load(stats, isOnline);
            SingleStatPage.Visibility = Visibility.Visible;
        }
        public async Task UpdateKit(WarfareKit kit, string signtext, EClass @class)
        {
            CurrentMode = EMode.KIT;
            SingleStatPage.Visibility = Visibility.Collapsed;
            TeamComparePage.Visibility = Visibility.Collapsed;
            SingleWeaponPage.Visibility = Visibility.Collapsed;
            WeaponList.Visibility = Visibility.Collapsed;
            SingleKitPage.Load(kit, signtext, @class);
            SingleKitPage.Visibility = Visibility.Visible;
            await Task.Yield();
        }
        public async Task UpdateTeams(WarfareTeam team1, WarfareTeam team2)
        {
            CurrentMode = EMode.TEAMS;
            CurrentKitA = null;
            SingleStatPage.Visibility = Visibility.Collapsed;
            SingleKitPage.Visibility = Visibility.Collapsed;
            SingleWeaponPage.Visibility = Visibility.Collapsed;
            WeaponList.Visibility = Visibility.Collapsed;
            TeamComparePage.Visibility = Visibility.Visible;
            TeamComparePage.Load(team1, team2);
            if (team2.Wins == 0) team2.Wins = team1.Losses;
            if (team1.Wins == 0) team1.Wins = team2.Losses;
            if (team2.Losses == 0) team2.Losses = team1.Wins;
            if (team1.Losses == 0) team1.Losses = team2.Wins;
            CurrentTeam1 = team1;
            CurrentTeam2 = team2;
            await Task.Yield();
        }
        public async Task UpdateWeapon(WarfareWeapon weapon, string name, string kitname, BitmapImage image)
        {
            CurrentMode = EMode.TEAMS;
            SingleStatPage.Visibility = Visibility.Collapsed;
            SingleKitPage.Visibility = Visibility.Collapsed;
            TeamComparePage.Visibility = Visibility.Collapsed;
            WeaponList.Visibility = Visibility.Collapsed;
            SingleWeaponPage.Visibility = Visibility.Visible;
            SingleWeaponPage.Load(weapon, name, kitname, image);
            await Task.Yield();
        }
        public async Task Steam64SearchOK(Steam64Find sender, ContentDialogButtonClickEventArgs args)
        {
            if (sender.TextBoxText.StartsWith("765") && ulong.TryParse(sender.TextBoxText, System.Globalization.NumberStyles.Any, StatsPage.Locale, out ulong Steam64))
            {
                string filename = Steam64.ToString(Locale) + ".dat";
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(StatsDirectory);
                StorageFile file = null;
                if (folder != null)
                    try
                    {
                        file = await folder.GetFileAsync(filename);
                    }
                    catch (FileNotFoundException)
                    {
                        file = null;
                    }
                if (file == null)
                {
                    sender.Error = "No stats file at \"" + folder != null ? folder.Path : "null" + filename + "\".";
                    args.Cancel = true;
                    return;
                }
                else
                {
                    WarfareStats stats = await WarfareStats.IO.ReadFrom(file);
                    if (stats == null)
                    {
                        sender.Error = "Unable to find player.";
                        args.Cancel = true;
                        return;
                    }
                    else
                    {
                        sender.Error = string.Empty;
                        Settings.LastSteam64 = Steam64;
                        await SaveSettings();
                        await UpdateSinglePlayer(stats, false);
                        sender.Hide();
                    }
                }
            }
            else
            {
                sender.Error = "Couldn't parse a Steam64 ID.";
                args.Cancel = true;
                return;
            }
        }
        public async Task Steam64NetSearch(Steam64Find sender, ContentDialogButtonClickEventArgs args)
        {
            if (sender.TextBoxText.StartsWith("765") && ulong.TryParse(sender.TextBoxText, System.Globalization.NumberStyles.Any, Locale, out ulong Steam64))
            {
                RequestPlayerData.Invoke(NetClient.connection, Steam64);
                Settings.LastSteam64 = Steam64;
                await SaveSettings();
            }
            else
            {
                sender.Error = "Couldn't parse a Steam64 ID.";
                args.Cancel = true;
                return;
            }
            await Task.Yield();
        }


        
        internal static readonly NetCall<ulong> RequestPlayerData = new NetCall<ulong>(2000);
        internal static readonly NetCall<string> RequestKitData = new NetCall<string>(2002);
        internal static readonly NetCall<byte> RequestTeamData = new NetCall<byte>(2004);
        internal static readonly NetCall<ushort, string, bool> RequestWeaponData = new NetCall<ushort, string, bool>(2015);
        internal static readonly NetCall<ushort, bool> RequestVehicleData = new NetCall<ushort, bool>(2016);
        internal static readonly NetCall RequestKitList = new NetCall(2010);
        internal static readonly NetCall RequestTeamsData = new NetCall(2012);
        internal static readonly NetCall<ushort> RequestAllWeapons = new NetCall<ushort>(2020);


        internal static readonly NetCallRaw<string[]> SendKitList =
            new NetCallRaw<string[]>(2011, R =>
            {
                int length = R.ReadUInt16();
                string[] strings = new string[length];
                for (int i = 0; i < length; i++)
                    strings[i] = R.ReadString();
                return strings;
            },
            (W, SA) =>
            {
                W.Write((ushort)SA.Length);
                for (int i = 0; i < SA.Length; i++)
                    W.Write(SA[i]);
            });
        [NetCall(ENetCall.FROM_SERVER, 2011)]
        internal static void ReceiveKitList(in IConnection connection, string[] kits)
        {
            I.KitList = kits;

            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                I.KitSearch.KitListSource = I.KitList;
            }).AsTask().ConfigureAwait(false);
        }

        private void page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SingleStatPage.Height = e.NewSize.Height - NavBar.Height - SingleStatPage.Margin.Top - SingleStatPage.Margin.Bottom;
            SingleKitPage.Height = e.NewSize.Height - NavBar.Height - SingleKitPage.Margin.Top - SingleKitPage.Margin.Bottom;
            TeamComparePage.Height = e.NewSize.Height - NavBar.Height - TeamComparePage.Margin.Top - TeamComparePage.Margin.Bottom;
        }
    }
    public enum EMode : byte
    {
        SINGLE,
        COMPARE,
        QUERY,
        KIT,
        WEAPON,
        VEHICLE,
        TEAM,
        TEAMS
    }
    public struct ClassConfig
    {
        public char Icon;
        public ushort MarkerEffect;
        public ushort SquadLeaderMarkerEffect;
        public ClassConfig(char Icon, ushort MarkerEffect, ushort SquadLeaderMarkerEffect)
        {
            this.Icon = Icon;
            this.MarkerEffect = MarkerEffect;
            this.SquadLeaderMarkerEffect = SquadLeaderMarkerEffect;
        }
    }
    public struct ImageCache
    {
        public ushort ID;
        public bool Vehicle;
        public Image Image;
        public ImageCache(ushort ID, bool Vehicle, Image Image)
        {
            this.ID = ID;
            this.Vehicle = Vehicle;
            this.Image = Image;
        }
    }
}
