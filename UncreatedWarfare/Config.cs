using Rocket.API;
using System.Collections.Generic;
using System.Xml.Serialization;
using Uncreated.SQL;

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
        [XmlElement("DeathMessages")]
        public DeathMesssagesSettings DeathMessages;
        [XmlElement("Patches")]
        public PatchToggles Patches;
        [XmlElement("MySQL")]
        public SQL.MySqlData SQL;
        [XmlElement("VehicleBay")]
        public VehicleBaySettings VehicleBaySettings;
        public bool Debug;
        public bool SendAssetsOnStartup;
        public float DelayAfterConnectionToSendTranslations;
        public float MaxMapHeight;
        public ushort ToastIDInfo;
        public ushort ToastIDWarning;
        public ushort ToastIDSevere;
        public ushort MiniToastXP;
        public ushort MiniToastOfficerPoints;
        public ushort BigToast;
        public ushort EndScreenUI;
        public bool UseColoredConsoleModule;
        public bool AllowCosmetics;
        public bool ModifySkillLevels;
        public bool AllowBatteryStealing;
        public string DiscordInviteCode;
        public float InjuredLifeTimeSeconds;
        public byte MaxPlayerCount;
        public bool LowerSteamPlayerCount;
        public bool EnableMortarWarning;
        public ushort MortarWeapon;
        public float MortarWarningDistance;
        public int StatsInterval;
        public float AfkCheckInterval;
        public float AMCDamageMultiplier;
        public bool ReplaceEmptyNamesWithID;
        public bool OverrideKitRequirements;
        public float InjuredDamageMultiplier;
        public ushort GiveUpUI;
        public float MaxTimeInStorages;
        public ushort[] LimitedStorages;
        public bool ClearItemsOnAmmoBoxUse;
        public bool RelayMicsDuringEndScreen;
        public float TimeBetweenTp;
        public bool EnableSquads;
        public float LoadoutCost;
        public float MaxVehicleAbandonmentDistance;
        public bool UsePatchForPlayerCap;
        public bool DisableBackups;
        public float MaxVehicleHeightToLeave;
        public string GamemodeRotation;
        public void LoadDefaults()
        {
            this.Modules = new Modules();
            this.FlagSettings = new FlagSettings();
            this.AdminLoggerSettings = new AdminLoggerSettings();
            this.PlayerStatsSettings = new PlayerStatsSettings();
            this.TeamSettings = new TeamSettings();
            this.DeathMessages = new DeathMesssagesSettings();
            this.Patches = new PatchToggles();
            this.SQL = new MySqlData { Database = "unturned", Host = "127.0.0.1", Password = "password", Port = 3306, Username = "root", CharSet = "utf8mb4" };
            this.VehicleBaySettings = new VehicleBaySettings();
            this.Debug = true;
            this.SendAssetsOnStartup = false;
            this.DelayAfterConnectionToSendTranslations = 0.5f;
            this.MaxMapHeight = 150;
            this.EndScreenUI = 36007;
            this.ToastIDInfo = 36004;
            this.ToastIDWarning = 36005;
            this.ToastIDSevere = 36003;
            this.MiniToastXP = 36001;
            this.MiniToastOfficerPoints = 36002;
            this.BigToast = 36006;
            this.UseColoredConsoleModule = true;
            this.AllowCosmetics = false;
            this.ModifySkillLevels = true;
            this.AllowBatteryStealing = false;
            this.DiscordInviteCode = "KVVBu45"; // https://discord.gg/code
            this.InjuredLifeTimeSeconds = 90f;
            this.InjuredDamageMultiplier = 0.1f;
            this.MaxPlayerCount = 48;
            this.LowerSteamPlayerCount = true;
            this.EnableMortarWarning = true;
            this.MortarWeapon = 38328;
            this.MortarWarningDistance = 75f;
            this.StatsInterval = 1;
            this.AfkCheckInterval = 450f;
            this.AMCDamageMultiplier = 0.25f;
            this.ReplaceEmptyNamesWithID = true;
            this.OverrideKitRequirements = false;
            this.GiveUpUI = 36009;
            this.MaxTimeInStorages = 15f;
            this.LimitedStorages = new ushort[] { 38317, 38319, 38343, 38344 };
            this.ClearItemsOnAmmoBoxUse = true;
            this.RelayMicsDuringEndScreen = true;
            this.TimeBetweenTp = 0.1f;
            this.EnableSquads = true;
            this.LoadoutCost = 8;
            this.MaxVehicleAbandonmentDistance = 300f;
            this.UsePatchForPlayerCap = true;
            this.DisableBackups = false;
            this.MaxVehicleHeightToLeave = 50f;
            this.GamemodeRotation = "TeamCTF:2.0, Invasion:1.0, Insurgency:1.0";
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
        public bool EnablePlayerCount;
        public bool ShowPointsOnUI;
        public bool AllowPlayersToCaptureInVehicle;
        public bool HideUnknownFlags;
        public uint DiscoveryForesight;
        public int RequiredPlayerDifferenceToCapture;
        public FlagSettings()
        {
            this.NeutralColor = "ffffff";
            this.PlayerCheckSpeedSeconds = 0.25f;
            this.UseAutomaticLevelSensing = true;
            this.UseUI = true;
            this.UseChat = false;
            this.UIID = 36000;
            this.FlagUIIdFirst = 36010;
            this.EnablePlayerCount = true;
            this.ShowPointsOnUI = true;
            this.HideUnknownFlags = true;
            this.DiscoveryForesight = 2;
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
        public bool destroyStructure;
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
        public bool ReceiveStealVehicleBattery;
        public bool requestGroupExit;
        public bool ReceiveDragItem;
        public bool ReceiveGestureRequest;
        public bool project;
        public bool replicateSetMarker;
        public bool ReceiveChatRequest;
        public bool EnableQueueSkip;
        public bool closeStorage;
        public bool askStarve;
        public bool askDehydrate;

        public PatchToggles()
        {
            this.SendRegion = true;
            this.ServerSetSignTextInternal = true;
            this.dropBarricadeIntoRegionInternal = true;
            this.destroyBarricade = true;
            this.destroyStructure = true;
            this.ReceiveVisualToggleRequest = true;
            this.sendHealthChanged = true;
            this.simulatePlayerLife = true;
            this.ServerSetVisualToggleState = true;
            this.UseableTrapOnTriggerEnter = true;
            this.BumperOnTriggerEnter = true;
            this.damageVehicleTool = true;
            this.explodeInteractableVehicle = true;
            this.outputToConsole = true;
            this.ReceiveStealVehicleBattery = true;
            this.requestGroupExit = true;
            this.ReceiveDragItem = true;
            this.ReceiveGestureRequest = true;
            this.project = true;
            this.replicateSetMarker = true;
            this.ReceiveChatRequest = true;
            this.EnableQueueSkip = true;
            this.closeStorage = true;
            this.askStarve = true;
            this.askDehydrate = true;
        }

    }
    public class AdminLoggerSettings
    {
        public string AdminOffDutyGroup;
        public string AdminOnDutyGroup;
        public string InternOffDutyGroup;
        public string InternOnDutyGroup;
        public string HelperGroup;
        public bool LogTKs;
        public bool LogBans;
        public bool LogKicks;
        public bool LogUnBans;
        public bool LogWarning;
        public bool LogMainCamping;
        public bool LogBattleyeKick;
        public bool LogLastJoinedTime;
        public List<ushort> AllowedBarricadesOnVehicles;
        public uint TimeBetweenShutdownMessages;
        public string[] BattleyeExclusions;
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
            this.TimeBetweenShutdownMessages = 60;
            this.BattleyeExclusions = new string[]
            {
                "Client not responding",
                "Unofficial Modules not supported",
                "Query Timeout"
            };
        }
    }
    public class PlayerStatsSettings
    {
        public bool EnablePlayerList;
        public string NJS_ServerURL;
        public string ServerName;
        public float StatUpdateFrequency;
        public bool EnableTCPServer;
        public string TCPServerIP;
        public ushort TCPServerPort;
        public string TCPServerIdentity;

        public PlayerStatsSettings()
        {
            this.EnablePlayerList = true;
            this.EnableTCPServer = true;
            this.NJS_ServerURL = "http://localhost:8080/";
            this.ServerName = "warfare";
            this.StatUpdateFrequency = 30.0f;
            this.TCPServerIP = "127.0.0.1";
            this.TCPServerPort = 31902;
            this.TCPServerIdentity = "ucwarfare";
        }
    }
    public class DeathMesssagesSettings
    {
        public bool ColorizeNames;
        public bool PenalizeTeamkilledPlayers;
        public bool PenalizeSuicides;
        public DeathMesssagesSettings()
        {
            ColorizeNames = true;
            PenalizeTeamkilledPlayers = true;
            PenalizeSuicides = true;
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
    public class VehicleBaySettings
    {
        public ushort VehicleSpawnerID;
        public VehicleBaySettings()
        {
            VehicleSpawnerID = 20002;
        }
    }
}