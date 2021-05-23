using Rocket.Core.Plugins;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using UncreatedWarfare.Flags;
using UncreatedWarfare.Teams;
using UncreatedWarfare.Kits;
using UncreatedWarfare.Vehicles;
using UncreatedWarfare.FOBs;
using UnityEngine;
using Rocket.Core;
using Rocket.Unturned;
using UncreatedWarfare.Stats;
using UncreatedWarfare.Revives;
using System.Threading;
using Rocket.Unturned.Player;
using Newtonsoft.Json;
using Steamworks;
using System.ComponentModel;
using UncreatedWarfare.Components;

namespace UncreatedWarfare
{
    public partial class UCWarfare : RocketPlugin<Config>
    {
        public static UCWarfare Instance;
        public static UCWarfare I { get => Instance; }
        public static Config Config { get => Instance.Configuration.Instance; }
        private MySqlData _sqlElsewhere;
        public MySqlData SQL { 
            get
            {
                if (LoadMySQLDataFromElsewhere && (!_sqlElsewhere.Equals(null))) return _sqlElsewhere;
                else return Configuration.Instance.SQL;
            }
        }
        public const bool LoadMySQLDataFromElsewhere = true;
        public event EventHandler UCWarfareLoaded;
        public event EventHandler UCWarfareUnloading;
        public bool CoroutineTiming = false;
        private bool InitialLoadEventSubscription;
        protected override void Load()
        {
            Coroutines = new List<IEnumerator<WaitForSeconds>> { CheckPlayers() };
            Instance = this;

            F.Log("Started loading " + Name + " - By BlazingFlame and 420DankMeister. If this is not running on an official Uncreated Server than it has been obtained illigimately. " +
                "Please stop using this plugin now.", ConsoleColor.Green);

            F.Log("Patching methods...", ConsoleColor.Magenta);
            Patches.InternalPatches.DoPatching();

            if(LoadMySQLDataFromElsewhere)
            {
                if (!File.Exists(Data.ElseWhereSQLPath))
                {
                    TextWriter w = File.CreateText(Data.ElseWhereSQLPath);
                    JsonTextWriter wr = new JsonTextWriter(w);
                    JsonSerializer s = new JsonSerializer { Formatting = Formatting.Indented };
                    s.Serialize(wr, Config.SQL);
                    wr.Close();
                    w.Close();
                    w.Dispose();
                    _sqlElsewhere = Config.SQL;
                } else
                {
                    string json = File.ReadAllText(Data.ElseWhereSQLPath);
                    _sqlElsewhere = JsonConvert.DeserializeObject<MySqlData>(json);
                }
            }
            F.Log("Validating directories...", ConsoleColor.Magenta);
            F.CheckDir(Data.DataDirectory, out _, true);
            F.CheckDir(Data.FlagStorage, out _, true);
            F.CheckDir(Data.LangStorage, out _, true);
            F.CheckDir(Data.KitsStorage, out _, true);
            F.CheckDir(Data.VehicleStorage, out _, true);
            F.CheckDir(Data.FOBStorage, out _, true);
            F.CheckDir(Data.TeamStorage, out _, true);
            F.CheckDir(Data.FOBStorage, out _, true);
            void DuplicateKeyError(Exception ex)
            {
                string[] stuff = ex.Message.Split(':');
                string badKey = "unknown";
                if (stuff.Length >= 2) badKey = stuff[1].Trim();
                F.LogError("\"" + badKey + "\" has a duplicate key in default translations, unable to load them. Unloading...");
                Level.onLevelLoaded += (int level) =>
                {
                    F.LogError("!!UNCREATED WARFARE DID NOT LOAD!!!");
                    F.LogError("\"" + badKey + "\" has a duplicate key in default translations, unable to load them. Unloading...");
                };
                UnloadPlugin();
            }
            F.Log("Loading JSON Data...", ConsoleColor.Magenta);
            try
            {
                JSONMethods.CreateDefaultTranslations();
            } catch (TypeInitializationException ex)
            {
                DuplicateKeyError(ex);
                return;
            } catch (ArgumentException ex)
            {
                DuplicateKeyError(ex);
                return;
            }

            Data.Colors = JSONMethods.LoadColors(out Data.ColorsHex);
            Data.XPData = JSONMethods.LoadXP();
            Data.CreditsData = JSONMethods.LoadCredits();
            Data.Localization = JSONMethods.LoadTranslations(out Data.DeathLocalization, out Data.LimbLocalization);
            Data.ExtraPoints = JSONMethods.LoadExtraPoints(Config.FlagSettings.CurrentGamePreset);
            Data.TableData = JSONMethods.LoadTables();
            Data.NodeCalls = JSONMethods.LoadCalls();
            Data.Languages = JSONMethods.LoadLanguagePreferences();
            Data.LanguageAliases = JSONMethods.LoadLangAliases();

            // Managers
            F.Log("Instantiating Framework...", ConsoleColor.Magenta);
            Data.DatabaseManager = new AsyncDatabase();
            Data.DatabaseManager.OpenAsync(AsyncDatabaseCallbacks.OpenedOnLoad);
            Data.WebInterface = new WebInterface();
            Data.ListenerThread = new Thread(StartListening);
            CommandWindow.shouldLogDeaths = false;


            Data.FlagManager = new FlagManager(Config.FlagSettings.CurrentGamePreset);
            Data.FlagManager.OnReady += OnFlagManagerReady;
            if (Config.Modules.Kits)
            {
                Data.KitManager = new KitManager();
            }
            if (Config.Modules.VehicleSpawning)
            {
                Data.VehicleSpawnSaver = new VehicleSpawnSaver();
                Data.VehicleBay = new VehicleBay();
            }
            if (Config.Modules.FOBs)
            {
                Data.FOBManager = new FOBManager();
                Data.BuildManager = new BuildManager();
            }
            if (Config.Modules.Revives)
            {
                Data.ReviveManager = new ReviveManager();
            }
            F.Log("Starting Coroutines...", ConsoleColor.Magenta);
            if (Level.isLoaded)
            {
                StartAllCoroutines();
                if(Configuration.Instance.SendAssetsOnStartup)
                {
                    F.Log("Sending assets...", ConsoleColor.Magenta);
                    Data.WebInterface.SendAssetUpdate();
                }
                F.Log("Subscribing to events...", ConsoleColor.Magenta);
                InitialLoadEventSubscription = true;
                SubscribeToEvents();
                Data.TeamManager = new TeamManager();
                Data.ExtraZones = JSONMethods.LoadExtraZones(Config.FlagSettings.CurrentGamePreset);
                Data.FlagManager.Load();
                // Start new game.

                Data.GameStats = gameObject.AddComponent<WarStatsTracker>();
            } else
            {
                InitialLoadEventSubscription = false;
                Level.onLevelLoaded += OnLevelLoaded;
                R.Plugins.OnPluginsLoaded += OnPluginsLoaded;
            }
            base.Load();
            UCWarfareLoaded?.Invoke(this, EventArgs.Empty);

        }
        private void OnFlagManagerReady(object sender, EventArgs e)
        {
            Data.FlagManager.StartNextGame();
        }
        private void SubscribeToEvents()
        {
            U.Events.OnPlayerConnected += OnPostPlayerConnected;
            U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
            Provider.onCheckValidWithExplanation += OnPrePlayerConnect;
            Commands.LangCommand.OnPlayerChangedLanguage += LangCommand_OnPlayerChangedLanguage;
            Commands.ReloadCommand.OnTranslationsReloaded += ReloadCommand_onTranslationsReloaded;
            BarricadeManager.onDeployBarricadeRequested += OnBarricadeTryPlaced;
            Rocket.Unturned.Events.UnturnedPlayerEvents.OnPlayerDeath += OnPlayerDeath;
            UseableGun.onBulletSpawned += BulletSpawned;
            UseableGun.onProjectileSpawned += ProjectileSpawned;
            UseableThrowable.onThrowableSpawned += ThrowableSpawned;
            Patches.InternalPatches.OnLandmineExplode += OnLandmineExploded;
            Patches.BarricadeSpawnedHandler += OnBarricadePlaced;
            Patches.BarricadeDestroyedHandler += OnBarricadeDestroyed;
        }
        private void UnsubscribeFromEvents()
        {
            Commands.ReloadCommand.OnTranslationsReloaded -= ReloadCommand_onTranslationsReloaded;
            U.Events.OnPlayerConnected -= OnPostPlayerConnected;
            U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;
            if (Data.ListenServer != null) Data.ListenServer.ListenerResultHeard -= ReceivedResponeFromListenServer;
            Commands.LangCommand.OnPlayerChangedLanguage -= LangCommand_OnPlayerChangedLanguage;
            BarricadeManager.onDeployBarricadeRequested -= OnBarricadeTryPlaced;
            Rocket.Unturned.Events.UnturnedPlayerEvents.OnPlayerDeath -= OnPlayerDeath;
            UseableGun.onBulletSpawned -= BulletSpawned;
            UseableGun.onProjectileSpawned -= ProjectileSpawned;
            Patches.InternalPatches.OnLandmineExplode -= OnLandmineExploded;
            Patches.BarricadeSpawnedHandler -= OnBarricadePlaced;
            Patches.BarricadeDestroyedHandler -= OnBarricadeDestroyed;
            if (!InitialLoadEventSubscription)
            {
                Level.onLevelLoaded -= OnLevelLoaded;
                R.Plugins.OnPluginsLoaded -= OnPluginsLoaded;
            }
        }

        private void OnBarricadeDestroyed(BarricadeData data, uint instanceID)
        {
            if (Data.OwnerComponents != null)
            {
                int c = Data.OwnerComponents.FindIndex(x => x.transform.position == data.point);
                if (c != -1)
                {
                    Destroy(Data.OwnerComponents[c]);
                    Data.OwnerComponents.RemoveAt(c);
                }
            }
        }
        private void OnBarricadePlaced(BarricadeRegion region, BarricadeData data, ref Transform location)
        {
            F.Log("Placed barricade: " + data.barricade.asset.itemName + ", " + location.position.ToString());
            BarricadeOwnerDataComponent c = location.gameObject.AddComponent<BarricadeOwnerDataComponent>();
            c.SetData(data, region, location);
            Data.OwnerComponents.Add(c);
        }
        private void OnLandmineExploded(InteractableTrap trap, Collider collider, BarricadeOwnerDataComponent owner, ref bool allow)
        {
            if (owner == null) return;
            if(F.TryGetPlaytimeComponent(owner.owner.player, out PlaytimeComponent c))
                c.LastLandmineExploded = new LandmineDataForPostAccess(trap, owner);
            F.Log(owner.owner.playerID.playerName + "'s landmine exploded");
        }
        private void ThrowableSpawned(UseableThrowable useable, GameObject throwable)
        {
            F.Log(useable == null ? "null" : useable.player.name + " - " + useable.equippedThrowableAsset.itemName);
            ThrowableOwnerDataComponent t = throwable.AddComponent<ThrowableOwnerDataComponent>();
            PlaytimeComponent c = F.GetPlaytimeComponent(useable.player, out bool success);
            t.Set(useable, throwable, c);
            if (success)
                c.thrown.Add(t);
        }
        private void ProjectileSpawned(UseableGun gun, GameObject projectile)
        {
            PlaytimeComponent c = F.GetPlaytimeComponent(gun.player, out bool success); 
            if (success)
            {
                c.lastProjected = gun.equippedGunAsset.id;
            }
        }
        private void BulletSpawned(UseableGun gun, BulletInfo bullet)
        {
            PlaytimeComponent c = F.GetPlaytimeComponent(gun.player, out bool success);
            if (success)
            {
                c.lastShot = gun.equippedGunAsset.id;
            }
        }
        private void ReloadCommand_onTranslationsReloaded(object sender, EventArgs e)
        {
            foreach(SteamPlayer player in Provider.clients)
                UpdateLangs(player);
        }
        private void OnBarricadeTryPlaced(Barricade barricade, ItemBarricadeAsset asset, Transform hit, ref Vector3 point, ref float angle_x, 
            ref float angle_y, ref float angle_z, ref ulong owner, ref ulong group, ref bool shouldAllow)
        {
            if (hit != null && hit.transform.CompareTag("Vehicle"))
            {
                if (!Configuration.Instance.AdminLoggerSettings.AllowedBarricadesOnVehicles.Contains(asset.id))
                {
                    UnturnedPlayer player = UnturnedPlayer.FromCSteamID(new Steamworks.CSteamID(owner));
                    if (player != null && player.OffDuty())
                    {
                        shouldAllow = false;
                        player.SendChat("no_placement_on_vehicle", GetColor("defaulterror"), asset.itemName, asset.itemName.An());
                    }
                }
            }
        }
        private void OnPluginsLoaded()
        {
            StartAllCoroutines();
            F.Log("Subscribing to events...", ConsoleColor.Magenta);
            SubscribeToEvents();
        }
        private void UpdateLangs(SteamPlayer player)
        {
            foreach (BarricadeRegion region in BarricadeManager.regions)
            {
                List<BarricadeDrop> signs = new List<BarricadeDrop>();
                foreach (BarricadeDrop drop in region.drops)
                {
                    if (drop.model.TryGetComponent(out InteractableSign sign))
                    {
                        if (sign.text.StartsWith("sign_"))
                        {
                            if (BarricadeManager.tryGetInfo(drop.model, out byte x, out byte y, out ushort plant, out ushort index, out BarricadeRegion _))
                                F.InvokeSignUpdateFor(player, x, y, plant, index, region, false);
                        }
                    }
                }
            }
            
        }
        private void LangCommand_OnPlayerChangedLanguage(object sender, Commands.PlayerChangedLanguageEventArgs e) => UpdateLangs(e.player.Player.channel.owner);

        private void OnPrePlayerConnect(ValidateAuthTicketResponse_t callback, ref bool isValid, ref string explanation)
        {
            SteamPending player = Provider.pending.FirstOrDefault(x => x.playerID.steamID.m_SteamID == callback.m_SteamID.m_SteamID);
            if (player == default(SteamPending)) return;
            F.Log(player.playerID.playerName);
            if (Data.OriginalNames.ContainsKey(player.playerID.steamID.m_SteamID))
                Data.OriginalNames[player.playerID.steamID.m_SteamID] = new FPlayerName(player.playerID);
            else
                Data.OriginalNames.Add(player.playerID.steamID.m_SteamID, new FPlayerName(player.playerID));
            const string prefix = "[TEAM] ";
            if (!player.playerID.characterName.StartsWith(prefix))
                player.playerID.characterName = prefix + player.playerID.characterName;
            if (!player.playerID.nickName.StartsWith(prefix))
                player.playerID.nickName = prefix + player.playerID.nickName;
            // remove any "staff" from player's names.
            player.playerID.characterName = player.playerID.characterName.ReplaceCaseInsensitive("staff");
            player.playerID.nickName = player.playerID.nickName.ReplaceCaseInsensitive("staff");
        }

        private void OnLevelLoaded(int level)
        {
            F.Log("Sending assets...", ConsoleColor.Magenta);
            if (Configuration.Instance.SendAssetsOnStartup)
                Data.WebInterface.SendAssetUpdate();
            Data.TeamManager = new TeamManager();
            Data.ExtraZones = JSONMethods.LoadExtraZones(Config.FlagSettings.CurrentGamePreset);
            Data.FlagManager.Load();

            //Start new game
            Data.GameStats = gameObject.AddComponent<WarStatsTracker>();
        }

        private void StartListening()
        {
            Data.ListenServer = new AsyncListenServer();
            Data.ListenServer.ListenerResultHeard += ReceivedResponeFromListenServer;
            Data.ListenServer.StartListening();
        }

        private void OnPostPlayerConnected(UnturnedPlayer player)
        {
            F.Broadcast("player_connected", GetColor("join_message_background"), player.Player.channel.owner.playerID.playerName, GetColorHex("join_message_name"));
            Data.WebInterface?.SendPlayerJoinedAsync(player.Player.channel.owner);
            FPlayerName names;
            if (Data.OriginalNames.ContainsKey(player.Player.channel.owner.playerID.steamID.m_SteamID))
                names = Data.OriginalNames[player.Player.channel.owner.playerID.steamID.m_SteamID];
            else names = new FPlayerName(player);
            Data.DatabaseManager?.UpdateUsernameAsync(player.Player.channel.owner.playerID.steamID.m_SteamID, names);
            Data.GameStats.AddPlayer(player.Player);
            if(Data.PlaytimeComponents.ContainsKey(player.Player.channel.owner.playerID.steamID.m_SteamID))
            {
                DestroyImmediate(Data.PlaytimeComponents[player.Player.channel.owner.playerID.steamID.m_SteamID]);
                Data.PlaytimeComponents.Remove(player.Player.channel.owner.playerID.steamID.m_SteamID);
            }
            player.Player.transform.gameObject.AddComponent<PlaytimeComponent>().StartTracking(player.Player);
            if (F.TryGetPlaytimeComponent(player.Player, out PlaytimeComponent c))
                Data.PlaytimeComponents.Add(player.Player.channel.owner.playerID.steamID.m_SteamID, c);
        }
        private void OnPlayerDisconnected(UnturnedPlayer player)
        {
            if (Data.OriginalNames.ContainsKey(player.Player.channel.owner.playerID.steamID.m_SteamID))
                Data.OriginalNames.Remove(player.Player.channel.owner.playerID.steamID.m_SteamID);
            F.Broadcast("player_disconnected", GetColor("leave_message_background"), player.Player.channel.owner.playerID.playerName, GetColorHex("leave_message_name"));
            Data.WebInterface?.SendPlayerLeftAsync(player.Player.channel.owner);
            IEnumerable<BarricadeOwnerDataComponent> ownedTraps = Data.OwnerComponents.Where(x => x != null && x.owner?.playerID.steamID.m_SteamID == player.CSteamID.m_SteamID && x.barricade?.asset?.type == EItemType.TRAP);
            foreach(BarricadeOwnerDataComponent comp in ownedTraps.ToList())
            {
                if (comp == null) continue;
                if(BarricadeManager.tryGetInfo(comp.barricadeTransform, out byte x, out byte y, out ushort plant, out ushort index, out BarricadeRegion region))
                {
                    BarricadeManager.destroyBarricade(region, x, y, plant, index);
                    F.Log($"Removed {player.DisplayName}'s {comp.barricade.asset.itemName} at {x}, {y}", ConsoleColor.Green);
                }
                Destroy(comp);
                Data.OwnerComponents.Remove(comp);
            }
            if (F.TryGetPlaytimeComponent(player.Player, out PlaytimeComponent c))
            {
                Destroy(c);
                Data.PlaytimeComponents.Remove(player.CSteamID.m_SteamID);
            }
        }

        protected override void Unload()
        {
            UCWarfareUnloading?.Invoke(this, EventArgs.Empty);

            IAsyncResult CloseSQLAsyncResult = Data.DatabaseManager.CloseAsync(AsyncDatabaseCallbacks.ClosedOnUnload);
            F.Log("Unloading " + Name, ConsoleColor.Magenta);
            Data.WebInterface?.Dispose();
            Data.FlagManager?.Dispose();
            Data.DatabaseManager?.Dispose();
            Data.ReviveManager?.Dispose();
            Data.FOBManager?.Dispose();
            F.Log("Stopping Coroutines...", ConsoleColor.Magenta);
            StopAllCoroutines();
            F.Log("Unsubscribing from events...", ConsoleColor.Magenta);
            UnsubscribeFromEvents();
            CommandWindow.shouldLogDeaths = true;
            try
            {
                CloseSQLAsyncResult.AsyncWaitHandle.WaitOne();
            }
            catch (ObjectDisposedException) { }
        }
        public static Color GetColor(string key)
        {
            if (Data.Colors == null) return Color.white;
            if (Data.Colors.ContainsKey(key)) return Data.Colors[key];
            else if (Data.Colors.ContainsKey("default")) return Data.Colors["default"];
            else return Color.white;
        }
        public static string GetColorHex(string key)
        {
            if (Data.ColorsHex == null) return "ffffff";
            if (Data.ColorsHex.ContainsKey(key)) return Data.ColorsHex[key];
            else if (Data.ColorsHex.ContainsKey("default")) return Data.ColorsHex["default"];
            else return "ffffff";
        }
    }
}
