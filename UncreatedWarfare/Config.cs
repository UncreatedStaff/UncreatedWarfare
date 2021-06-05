using Newtonsoft.Json;
using Rocket.API;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Uncreated.Warfare
{
    public class Config : IRocketPluginConfiguration
    {
        [XmlElement("Modules")]
        public Modules Modules;
        [XmlElement("Flags")]
        public FlagSettings FlagSettings;
        [XmlElement("AdminLogger")]
        public AdminLoggerSettings AdminLoggerSettings;
        [XmlElement("PlayerStats")]
        public PlayerStatsSettings PlayerStatsSettings;
        [XmlElement("Teams")]
        public TeamSettings TeamSettings;
        [XmlElement("DiscordIntegration")]
        public DiscordSettings DiscordSettings;
        [XmlElement("FOBs")]
        public FOBSettings FobSettings;
        [XmlElement("DeathMessages")]
        public DeathMesssagesSettings DeathMessages;
        [XmlElement("Patches")]
        public PatchToggles Patches;
        [XmlElement("MySQL")]
        public MySqlData SQL;
        public bool Debug;
        public bool SendAssetsOnStartup;
        public float DelayAfterConnectionToSendTranslations;
        public float MaxMapHeight;
        public ushort EndScreenUI;
        public bool UseColoredConsoleModule;
        public bool AllowCosmetics;
        public void LoadDefaults()
        {
            this.Modules = new Modules();
            this.FlagSettings = new FlagSettings();
            this.AdminLoggerSettings = new AdminLoggerSettings();
            this.PlayerStatsSettings = new PlayerStatsSettings();
            this.TeamSettings = new TeamSettings();
            this.FobSettings = new FOBSettings();
            this.DeathMessages = new DeathMesssagesSettings();
            this.Patches = new PatchToggles();
            this.SQL = new MySqlData { Database = "unturned", Host = "127.0.0.1", Password = "password", Port = 3306, Username = "admin", CharSet = "utf8mb4" };
            this.Debug = true;
            this.SendAssetsOnStartup = false;
            this.DelayAfterConnectionToSendTranslations = 0.5f;
            this.MaxMapHeight = 150;
            this.EndScreenUI = 10000;
            this.UseColoredConsoleModule = true;
            this.AllowCosmetics = false;
        }
    }
    public class Modules
    {
        public bool PlayerList;
        public bool Kits;
        public bool Rename;
        public bool VehicleSpawning;
        public bool FOBs;
        public bool Roles;
        public bool UI;
        public bool AdminLogging;
        public bool MainCampPrevention;
        public bool Flags;
        public bool Revives;
        public Modules()
        {
            this.PlayerList = true;
            this.Kits = true;
            this.Rename = true;
            this.VehicleSpawning = true;
            this.FOBs = true;
            this.Roles = true;
            this.UI = true;
            this.AdminLogging = true;
            this.MainCampPrevention = true;
            this.Flags = true;
            this.Revives = true;
        }

    }
    public class FlagSettings
    {
        public string NeutralColor;
        public float PlayerCheckSpeedSeconds;

        public bool UseUI;
        public bool UseChat;
        /// <summary>Alternative is level system.</summary>
        public bool UseAutomaticLevelSensing;
        public ushort UIID;
        public ushort FlagUIIdFirst;
        public string charactersForUI;
        public bool EnablePlayerCount;
        public bool ShowPointsOnUI;
        public bool AllowPlayersToCaptureInVehicle;
        public bool HideUnknownFlags;
        public int RequiredPlayerDifferenceToCapture;
        public FlagSettings()
        {
            this.NeutralColor = "ffffff";
            this.PlayerCheckSpeedSeconds = 0.25f;
            this.UseAutomaticLevelSensing = true;
            this.UseUI = true;
            this.UseChat = false;
            this.UIID = 10001;
            this.FlagUIIdFirst = 32351;
            this.charactersForUI = "456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            this.EnablePlayerCount = true;
            this.ShowPointsOnUI = true;
            this.HideUnknownFlags = true;
            this.AllowPlayersToCaptureInVehicle = false;
            this.RequiredPlayerDifferenceToCapture = 2;
        }
    }
    public class PatchToggles
    {
        public bool SendRegion;
        public bool ServerSetSignTextInternal;
        public bool dropBarricadeIntoRegionInternal;
        public bool destroyBarricade;
        public bool ReceiveVisualToggleRequest;
        public bool sendHealthChanged;
        public bool ServerSetVisualToggleState;
        /// <summary>
        /// On PlayerLife
        /// </summary>
        public bool simulatePlayerLife;
        /// <summary>
        /// On landmine explosion.
        /// </summary>
        public bool UseableTrapOnTriggerEnter;
        /// <summary>
        /// On bumper hit car add carid to pt component.
        /// </summary>
        public bool BumperOnTriggerEnter;
        /// <summary>
        /// On VehicleTool.damage(..) to add the last player that damaged the vehicle.
        /// </summary>
        public bool damageVehicleTool;
        /// <summary>
        /// On InteractableVehicle.explode() to add explosion to player's compoenent.
        /// </summary>
        public bool explodeInteractableVehicle;
        public bool outputToConsole;

        public PatchToggles()
        {
            this.SendRegion = true;
            this.ServerSetSignTextInternal = true;
            this.dropBarricadeIntoRegionInternal = true;
            this.destroyBarricade = true;
            this.ReceiveVisualToggleRequest = true;
            this.sendHealthChanged = true;
            this.simulatePlayerLife = true;
            this.ServerSetVisualToggleState = true;
            this.UseableTrapOnTriggerEnter = true;
            this.BumperOnTriggerEnter = true;
            this.damageVehicleTool = true;
            this.outputToConsole = true;
        }

    }
    public class AdminLoggerSettings
    {
        public string AdminOffDutyGroup;
        public string AdminOnDutyGroup;
        public string InternOffDutyGroup;
        public string InternOnDutyGroup;
        public bool LogTKs;
        public bool LogBans;
        public bool LogKicks;
        public bool LogUnBans;
        public bool LogWarning;
        public bool LogMainCamping;
        public bool LogBattleyeKick;
        public bool LogLastJoinedTime;
        public List<ushort> AllowedBarricadesOnVehicles;
        public AdminLoggerSettings()
        {
            this.InternOnDutyGroup = "intern";
            this.InternOffDutyGroup = "intern-od";
            this.AdminOnDutyGroup = "admin";
            this.AdminOffDutyGroup = "admin-od";
            this.LogTKs = true;
            this.LogBans = true;
            this.LogKicks = true;
            this.LogUnBans = true;
            this.LogWarning = true;
            this.LogMainCamping = false; // should be off if AntiMainCampingModule is on
            this.LogBattleyeKick = true;
            this.LogLastJoinedTime = true;
            this.AllowedBarricadesOnVehicles = new List<ushort>();
        }
    }
    public class PlayerStatsSettings
    {
        public bool EnablePlayerList;
        public string NJS_ServerURL;
        public string ServerName;
        public float StatUpdateFrequency;
        public bool EnableListenServer;

        public PlayerStatsSettings()
        {
            this.EnablePlayerList = true;
            this.EnableListenServer = true;
            this.NJS_ServerURL = "http://localhost:8080/";
            this.ServerName = "warfare";
            this.StatUpdateFrequency = 30.0f;
        }
    }
    public class DeathMesssagesSettings
    {
        public bool ColorizeNames;
        public DeathMesssagesSettings()
        {
            ColorizeNames = true;
        }
    }
    public class TeamSettings 
    {
        public bool BalanceTeams;
        public float AllowedDifferencePercent;
        public TeamSettings()
        {
            this.BalanceTeams = true;
            this.AllowedDifferencePercent = 0.15F;
        }
    }
    public class DiscordSettings
    {
        public bool SendPlayerList;
        public DiscordSettings()
        {
            this.SendPlayerList = true;
        }
    }
    public class FOBSettings
    {
        public ushort FOBID;
        public ushort FOBID_Unbuilt;
        public FOBSettings()
        {
            this.FOBID_Unbuilt = 38310;
            this.FOBID = 38311;
        }
    }
    public struct MySqlData
    {
        public string Host;
        public string Database;
        public string Password;
        public string Username;
        public ushort Port;
        public string CharSet;
        [JsonIgnore]
        public string ConnectionString { get => $"server={Host};port={Port};database={Database};uid={Username};password={Password};charset={CharSet};"; }
    }
}