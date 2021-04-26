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
        public FlagManager FlagManager;
        public const string DataDirectory = @"Plugins\UCData\";
        public static readonly string FlagStorage = DataDirectory + @"Flags\Presets\";
        public Team T1 { get => Teams.Count > 0 ? Teams[0] : null; }
        public Team T2 { get => Teams.Count > 1 ? Teams[1] : null; }
        public List<Team> Teams;
        public Dictionary<string, Color> Colors;
        public Dictionary<string, string> ColorsHex;
        private bool InitialLoadEventSubscription;
        protected override void Load()
        {
            Coroutines = new List<IEnumerator<WaitForSeconds>> { CheckPlayers() };
            CommandWindow.LogWarning("Started loading " + Name + " - By BlazingFlame and 420DankMeister. If this is not running on an official Uncreated Server than it has been obtained illigimately. " +
                "Please stop using this plugin now.");
            Instance = this;
            if (!System.IO.Directory.Exists(DataDirectory))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(DataDirectory);
                } catch (Exception ex)
                {
                    CommandWindow.LogError("Unable to create data directory " + DataDirectory + ". Check permissions: " + ex.Message);
                    UnloadPlugin();
                }
            }
            if(Config.Modules.Flags)
            {
                FlagManager = new FlagManager(Config.FlagSettings.CurrentGamePreset);
            }
            Colors = JSONMethods.LoadColors(out ColorsHex);
            CommandWindow.Log("Starting Coroutines...");
            if(Level.isLoaded)
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

        private void OnPlayerDisconnected(Rocket.Unturned.Player.UnturnedPlayer player)
        {
            throw new NotImplementedException();
        }

        private void OnPlayerConnected(Rocket.Unturned.Player.UnturnedPlayer player)
        {
            throw new NotImplementedException();
        }

        protected override void Unload()
        {
            UCWarfareUnloading?.Invoke(this, EventArgs.Empty);

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
