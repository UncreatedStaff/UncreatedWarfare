using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Uncreated.Networking;
using Uncreated.Warfare.Stats;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
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
        public double ConsoleInputHeight { get => consoleInput.ActualHeight; }
        internal void AddLog(LogItem item) => log.Items.Add(item);
        public Windows.UI.Xaml.Controls.Primitives.ScrollBar ScrollBar => logScroller;
        ~StatsPage()
        {
            LogStack.SetConsoleLogging.Invoke(NetClient.connection, false);
            System.Threading.Thread.Sleep(1000);
            SQL.Dispose();
            NetClient.Dispose();
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
        public bool IsCached(ushort ID, bool Vehicle, out BitmapImage Image)
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
        public void Cache(ushort ID, bool Vehicle, BitmapImage Image)
        {
            for (int i = 0; i < ImageCache.Count; i++)
            {
                if (ImageCache[i].ID == ID && ImageCache[i].Vehicle == Vehicle)
                {
                    ImageCache[i] = new ImageCache(ID, Vehicle, Image);
                    return;
                }
            }
            if (ImageCache.Count > 50)
            {
                ImageCache.RemoveAt(0);
            }
            ImageCache.Add(new ImageCache(ID, Vehicle, Image));
        }
        public Steam64Find S64Search = new Steam64Find();
        public NoAccessToFileSystem FSWarn = new NoAccessToFileSystem();
        public UsernameSearch UsernameFind = new UsernameSearch();
        public SettingsDialog SettingsDialog = new SettingsDialog();
        public IDSearch IDSearchDialog = new IDSearch()
        {
            OnOK = OnIDSearchOK
        };
        private static void OnIDSearchOK(ushort id, IDSearch search, ContentDialogButtonClickEventArgs args)
        {
            if (search.IsVehicle)
            {
                RequestVehicleData.Invoke(I.NetClient.connection, id, I.IsCached(id, true));
            }
            else
            {
                RequestAllWeapons.Invoke(I.NetClient.connection, id);
            }
        }
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
            I = this;
            InitializeComponent();
            this.Unloaded += StatsPage_Unloaded;
            Loaded += StatsPage_Loaded;
            Logging.OnLog += (M, C) => Debug.WriteLine(" -==- INF: " + M);
            Logging.OnLogWarning += (M, C) => Debug.WriteLine(" -==- WRN: " + M);
            Logging.OnLogError += (M, C) => Debug.WriteLine(" -==- ERR: " + M);
            Logging.OnLogException += (M, C) => Debug.WriteLine(" -==- EXC: " + M.ToString());
            NetFactory.RegisterNetMethods(System.Reflection.Assembly.GetExecutingAssembly(), ENetCall.FROM_SERVER);
            logStack.Init();
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
        private void ClickApplication_RefreshConsole(object sender, RoutedEventArgs e)
        {
            LogStack.SetConsoleLogging.Invoke(NetClient.connection, true);
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
        private async void ClickRequest_Vehicle(object sender, RoutedEventArgs e)
        {
            IDSearchDialog.IsVehicle = true;
            await IDSearchDialog.ShowAsync();
        }
        private async void ClickRequest_Weapon(object sender, RoutedEventArgs e)
        {
            IDSearchDialog.IsVehicle = false;
            await IDSearchDialog.ShowAsync();
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
            SingleWeaponPage.Height = e.NewSize.Height - NavBar.Height - SingleWeaponPage.Margin.Top - SingleWeaponPage.Margin.Bottom;
            WeaponList.Height = e.NewSize.Height - NavBar.Height - WeaponList.Margin.Top - WeaponList.Margin.Bottom;
            WeaponList.Width = e.NewSize.Width - WeaponList.Margin.Left - WeaponList.Margin.Right;
            logStack.CalculateLogCount();
        }

        private void log_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListView box && box.SelectedIndex > 0 && box.Items[box.SelectedIndex] is LogItem i)
            {
                LogItem.LogItem_PointerPressed(i, null);
                if (box.SelectedItems.Count > 0)
                    (box.SelectedItems[0] as LogItem).IsSelected = false;
            }
        }
        private void ScrollBar_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (LogStack.ignoreScroll) return;
            double pct = 1 - e.NewValue / ScrollBar.Maximum;
            logStack.ArtificialScroll(pct);
        }

        private void consoleInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter && sender is TextBox box)
            {
                e.Handled = true;
                string command = box.Text;
                box.Text = string.Empty;
                if (NetClient.connection.IsActive)
                    LogStack.SendCommand.Invoke(NetClient.connection, command);
                else 
                    ReceiveNoServer(null);
            }
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
        public BitmapImage Image;
        public ImageCache(ushort ID, bool Vehicle, BitmapImage Image)
        {
            this.ID = ID;
            this.Vehicle = Vehicle;
            this.Image = Image;
        }
    }
}
