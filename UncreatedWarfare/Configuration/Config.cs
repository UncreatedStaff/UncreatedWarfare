using SDG.Unturned;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Uncreated.SQL;

namespace Uncreated.Warfare.Configuration;

public class SystemConfig : Config<SystemConfigData>
{
    public SystemConfig() : base(Warfare.Data.Paths.BaseDirectory, "sys_config.json", "sysconfig") { }
    protected override void OnReload() { }
}

public class SystemConfigData : JSONConfigData
{
    [JsonPropertyName("moderation")]
    public ModerationConfig ModerationSettings;
    [JsonPropertyName("tcpServer")]
    public TCPConfig TCPSettings;
    [JsonPropertyName("mysql")]
    public MySqlData SQL;
    [JsonPropertyName("mysqlRemote")]
    public MySqlData? RemoteSQL;
    [JsonPropertyName("debugMode")]
    public bool Debug;
    [JsonPropertyName("region")]
    public string Region;
    [JsonPropertyName("regionKey")]
    public byte RegionKey;
    [JsonPropertyName("localCurrency")]
    public string Currency;
    [JsonPropertyName("allowCosmetics")]
    public bool AllowCosmetics;
    [JsonPropertyName("modifySkills")]
    public bool ModifySkillLevels;
    [JsonPropertyName("allowBatteryTheft")]
    public bool AllowBatteryStealing;
    [JsonPropertyName("discordInviteCode")]
    public string DiscordInviteCode;
    [JsonPropertyName("injuredTimeSeconds")]
    public float InjuredLifeTimeSeconds;
    [JsonPropertyName("warnForFriendlyMortar")]
    public bool EnableMortarWarning;
    [JsonPropertyName("mortarWarningRadius")]
    public float MortarWarningDistance;
    [JsonPropertyName("statCoroutineInterval")]
    public int StatsInterval;
    [JsonPropertyName("afkCheckInterval")]
    public float AfkCheckInterval;
    [JsonPropertyName("amcDamageMultiplier")]
    public float AMCDamageMultiplier;
    [JsonPropertyName("overrideKitRequirements")]
    public bool OverrideKitRequirements;
    [JsonPropertyName("injuredPlayerDamageMultiplier")]
    public float InjuredDamageMultiplier;
    [JsonPropertyName("maxTimeInStorage")]
    public float MaxTimeInStorages;
    [JsonPropertyName("clearItemsOnRestock")]
    public bool ClearItemsOnAmmoBoxUse;
    [JsonPropertyName("relayMicsAfterGame")]
    public bool RelayMicsDuringEndScreen;
    [JsonPropertyName("enableSquads")]
    public bool EnableSquads;
    [JsonPropertyName("enableSync")]
    public bool EnableSync;
    [JsonPropertyName("enableActionMenu")]
    public bool EnableActionMenu;
    [JsonPropertyName("loadoutPremiumCost")]
    public decimal LoadoutCost;
    [JsonPropertyName("vehicleAbandonmentDistance")]
    public float MaxVehicleAbandonmentDistance;
    [JsonPropertyName("vehicleDismountMaxHeight")]
    public float MaxVehicleHeightToLeave;
    [JsonPropertyName("rotation")]
    public string GamemodeRotation;
    [JsonPropertyName("disableNameFilter")]
    public bool DisableNameFilter;
    [JsonPropertyName("nameFilterAlnumLength")]
    public int MinAlphanumericStringLength;
    [JsonPropertyName("enableReporter")]
    public bool EnableReporter;
    [JsonPropertyName("blockLandmineFriendlyFire")]
    public bool BlockLandmineFriendlyFire;
    [JsonPropertyName("disableDailyQuests")]
    public bool DisableDailyQuests;
    [JsonPropertyName("playerJoinLeaveMessages")]
    public bool EnablePlayerJoinLeaveMessages;
    [JsonPropertyName("playerJoinLeaveTeamMessages")]
    public bool EnablePlayerJoinLeaveTeamMessages;
    [JsonPropertyName("timeBetweenAnnouncements")]
    public float SecondsBetweenAnnouncements;
    [JsonPropertyName("sendActionLogs")]
    public bool SendActionLogs;
    [JsonPropertyName("disableMissingAssetKick")]
    public bool DisableMissingAssetKick;
    [JsonPropertyName("nerds")]
    public List<ulong> Nerds;

    public override void SetDefaults()
    {
        ModerationSettings = new ModerationConfig();
        TCPSettings = new TCPConfig();
        SQL = new MySqlData { Database = "unturned", Host = "127.0.0.1", Password = "password", Port = 3306, Username = "root", CharSet = "utf8mb4" };
        RemoteSQL = null;
        Debug = true;
        AllowCosmetics = false;
        ModifySkillLevels = true;
        AllowBatteryStealing = false;
        DiscordInviteCode = "ucn"; // https://discord.gg/code
        InjuredLifeTimeSeconds = 90f;
        InjuredDamageMultiplier = 0.1f;
        EnableMortarWarning = true;
        MortarWarningDistance = 75f;
        StatsInterval = 1;
        AfkCheckInterval = 450f;
        AMCDamageMultiplier = 0.25f;
        OverrideKitRequirements = false;
        MaxTimeInStorages = 15f;
        ClearItemsOnAmmoBoxUse = true;
        RelayMicsDuringEndScreen = true;
        EnableSquads = true;
        EnableActionMenu = true;
        LoadoutCost = 10m;
        MaxVehicleAbandonmentDistance = 300f;
        MaxVehicleHeightToLeave = 50f;
        GamemodeRotation = "TeamCTF:2.0, Invasion:1.0, Insurgency:1.0, Conquest:1.0";
        DisableNameFilter = false;
        MinAlphanumericStringLength = 5;
        EnableReporter = true;
        BlockLandmineFriendlyFire = true;
        DisableDailyQuests = false;
        EnablePlayerJoinLeaveMessages = false;
        EnablePlayerJoinLeaveTeamMessages = false;
        Region = "eus";
        Currency = "USD";
        RegionKey = 255;
        EnableSync = true;
        SecondsBetweenAnnouncements = 60f;
        SendActionLogs = true;
        DisableMissingAssetKick = false;
        Nerds = new List<ulong>() { 76561198312948915ul };
    }
    public class ModerationConfig
    {
        [JsonPropertyName("adminOffDuty")]
        public string AdminOffDutyGroup;
        [JsonPropertyName("adminOnDuty")]
        public string AdminOnDutyGroup;
        [JsonPropertyName("internOffDuty")]
        public string InternOffDutyGroup;
        [JsonPropertyName("internOnDuty")]
        public string InternOnDutyGroup;
        [JsonPropertyName("helper")]
        public string HelperGroup;
        [JsonPropertyName("vehiclePlaceWhitelist")]
        public List<ushort> AllowedBarricadesOnVehicles;
        [JsonPropertyName("shutdownMessageInterval")]
        public float TimeBetweenShutdownMessages;
        [JsonPropertyName("battleyeMessageBlacklist")]
        public string[] BattleyeExclusions;
        public ModerationConfig()
        {
            InternOnDutyGroup = "intern";
            InternOffDutyGroup = "intern-od";
            AdminOnDutyGroup = "admin";
            AdminOffDutyGroup = "admin-od";
            AllowedBarricadesOnVehicles = new List<ushort>();
            TimeBetweenShutdownMessages = 60f;
            BattleyeExclusions = new string[]
            {
                "Client not responding",
                "Query Timeout"
            };
        }
    }
    public class TCPConfig
    {
        public bool EnableTCPServer;
        public string TCPServerIP;
        public ushort TCPServerPort;
        public string TCPServerIdentity;

        public TCPConfig()
        {
            EnableTCPServer = true;
            TCPServerIP = "127.0.0.1";
            TCPServerPort = 31902;
            TCPServerIdentity = "ucwarfare";
        }
    }
}