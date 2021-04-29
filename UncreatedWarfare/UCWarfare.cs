using Rocket.Core.Plugins;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Rocket.Core.Logging.Logger;
using System.IO;
using UncreatedWarfare.Flags;
using UncreatedWarfare.Teams;
using UnityEngine;
using Rocket.Core;
using Rocket.Unturned;

namespace UncreatedWarfare
{
    public partial class UCWarfare : RocketPlugin<Config>
    {
        public static UCWarfare Instance;
        public static UCWarfare I { get => Instance; }
        public static Config Config { get => Instance.Configuration.Instance; }
        public event EventHandler UCWarfareLoaded;
        public event EventHandler UCWarfareUnloading;
        public KitManager KitManager;
        public FlagManager FlagManager;
        public TeamManager TeamManager;
        public WebInterface WebInterface;
        public const string DataDirectory = @"Plugins\UncreatedWarfare\";
        public static readonly string FlagStorage = DataDirectory + @"Flags\Presets\";
        public static readonly string KitsStorage = DataDirectory + @"Kits\";
        public static readonly string LangStorage = DataDirectory + @"Lang\";
        public Dictionary<string, Color> Colors;
        public Dictionary<string, string> ColorsHex;
        public Dictionary<string, string> Localization;
        public Dictionary<EXPGainType, int> XPData;
        public Dictionary<ECreditsGainType, int> CreditsData;
        public DatabaseManager DB { get; private set; }
        private bool InitialLoadEventSubscription;
        private void CheckDir(string path)
        {
            if (!System.IO.Directory.Exists(path))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(path);
                    CommandWindow.Log("Created directory: \"" + path + "\".");
                }
                catch (Exception ex)
                {
                    CommandWindow.LogError("Unable to create data directory " + path + ". Check permissions: " + ex.Message);
                    UnloadPlugin();
                }
            }
        }
        protected override void Load()
        {
            Coroutines = new List<IEnumerator<WaitForSeconds>> { CheckPlayers() };
            CommandWindow.LogWarning("Started loading " + Name + " - By BlazingFlame and 420DankMeister. If this is not running on an official Uncreated Server than it has been obtained illigimately. " +
                "Please stop using this plugin now.");
            Instance = this;
            DB = new DatabaseManager();
            WebInterface = new WebInterface();
            TeamManager = new TeamManager();

            CheckDir(DataDirectory);
            CheckDir(FlagStorage);
            CheckDir(LangStorage);
            CheckDir(KitsStorage);
            Colors = JSONMethods.LoadColors(out ColorsHex);
            XPData = JSONMethods.LoadXP();
            CreditsData = JSONMethods.LoadCredits();
            Localization = JSONMethods.LoadTranslations();
            if (Config.Modules.Flags)
            {
                FlagManager = new FlagManager(Config.FlagSettings.CurrentGamePreset);
            }
            if (Config.Modules.Kits)
            {
                KitManager = new KitManager();
            }
            CommandWindow.Log("Starting Coroutines...");
            if (Level.isLoaded)
            {
                StartAllCoroutines();
                Log("Subscribing to events...");
                InitialLoadEventSubscription = true;
                U.Events.OnPlayerConnected += OnPlayerConnected;
                U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
            } else
            {
                InitialLoadEventSubscription = false;
                R.Plugins.OnPluginsLoaded += OnPluginsLoaded;
            }
            WebInterface.SendPlayerList();
            base.Load();
            UCWarfareLoaded?.Invoke(this, EventArgs.Empty);
        }
        private void OnPluginsLoaded()
        {
            StartAllCoroutines();
            Log("Subscribing to events...");
            U.Events.OnPlayerConnected += OnPlayerConnected;
            U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
        }
        private void OnPlayerConnected(Rocket.Unturned.Player.UnturnedPlayer player)
        {
            F.Broadcast("player_connected", Colors["join_message_background"], player.Player.channel.owner.playerID.playerName, ColorsHex["join_message_name"]);
            TeamManager?.PlayerJoinProcess(player.Player.channel.owner);
            WebInterface?.SendPlayerJoined(player.Player.channel.owner);
        }
        private void OnPlayerDisconnected(Rocket.Unturned.Player.UnturnedPlayer player)
        {
            F.Broadcast("player_disconnected", Colors["leave_message_background"], player.Player.channel.owner.playerID.playerName, ColorsHex["leave_message_name"]);
            TeamManager?.PlayerLeaveProcess(player.Player.channel.owner);
            WebInterface?.SendPlayerLeft(player.Player.channel.owner);
        }

        protected override void Unload()
        {
            UCWarfareUnloading?.Invoke(this, EventArgs.Empty);

            WebInterface?.Dispose();
            CommandWindow.LogWarning("Unloading " + Name);
            CommandWindow.Log("Stopping Coroutines...");
            StopAllCoroutines();
            CommandWindow.Log("Unsubscribing from events...");
            U.Events.OnPlayerConnected -= OnPlayerConnected;
            U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;
            if(!InitialLoadEventSubscription) R.Plugins.OnPluginsLoaded -= OnPluginsLoaded;
            FlagManager.Dispose();
        }
    }
}
