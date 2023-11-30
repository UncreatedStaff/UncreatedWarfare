using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Uncreated.Networking;
using Uncreated.SQL;

namespace Uncreated.Warfare.Configuration;

public class SystemConfig : Config<SystemConfigData>
{
    public SystemConfig() : base(Warfare.Data.Paths.BaseDirectory, "sys_config.json", "sysconfig") { }

    protected override void OnReload()
    {
        if (Warfare.Data.DatabaseManager != null)
            Warfare.Data.DatabaseManager.DebugLogging = Data.Debug;
        if (Warfare.Data.RemoteSQL != null)
            Warfare.Data.RemoteSQL.DebugLogging = Data.Debug;

        Logging.SemaphoreLogging = Data.SemaphoreDebug;
    }
}

public class SystemConfigData : JSONConfigData
{
    [JsonPropertyName("moderation")]
    public ModerationConfig ModerationSettings { get; set; }
    [JsonPropertyName("tcpServer")]
    public TCPConfig TCPSettings { get; set; }
    [JsonPropertyName("mysql")]
    public MySqlData SQL { get; set; }
    [JsonPropertyName("mysqlRemote")]
    public MySqlData? RemoteSQL { get; set; }
    [JsonPropertyName("sqlConnectionString")]
    public string? SqlConnectionString { get; set; }
    [JsonPropertyName("debugMode")]
    public bool Debug { get; set; }
    [JsonPropertyName("semaphoreDebugLogging")]
    public bool SemaphoreDebug { get; set; }
    [JsonPropertyName("region")]
    public string Region { get; set; }
    [JsonPropertyName("regionKey")]
    public byte RegionKey { get; set; }
    [JsonPropertyName("localCurrency")]
    public string Currency { get; set; }
    [JsonPropertyName("allowCosmetics")]
    public bool AllowCosmetics { get; set; }
    [JsonPropertyName("modifySkills")]
    public bool ModifySkillLevels { get; set; }
    [JsonPropertyName("allowBatteryTheft")]
    public bool AllowBatteryStealing { get; set; }
    [JsonPropertyName("discordInviteCode")]
    public string DiscordInviteCode { get; set; }
    [JsonPropertyName("injuredTimeSeconds")]
    public float InjuredLifeTimeSeconds { get; set; }
    [JsonPropertyName("warnForFriendlyMortar")]
    public bool EnableMortarWarning { get; set; }
    [JsonPropertyName("mortarWarningRadius")]
    public float MortarWarningDistance { get; set; }
    [JsonPropertyName("statCoroutineInterval")]
    public int StatsInterval { get; set; }
    [JsonPropertyName("afkCheckInterval")]
    public float AfkCheckInterval { get; set; }
    [JsonPropertyName("amcDamageMultiplier")]
    public float AMCDamageMultiplier { get; set; }
    [JsonPropertyName("overrideKitRequirements")]
    public bool OverrideKitRequirements { get; set; }
    [JsonPropertyName("injuredPlayerDamageMultiplier")]
    public float InjuredDamageMultiplier { get; set; }
    [JsonPropertyName("maxTimeInStorage")]
    public float MaxTimeInStorages { get; set; }
    [JsonPropertyName("clearItemsOnRestock")]
    public bool ClearItemsOnAmmoBoxUse { get; set; }
    [JsonPropertyName("relayMicsAfterGame")]
    public bool RelayMicsDuringEndScreen { get; set; }
    [JsonPropertyName("enableSquads")]
    public bool EnableSquads { get; set; }
    [JsonPropertyName("enableSync")]
    public bool EnableSync { get; set; }
    [JsonPropertyName("enableActionMenu")]
    public bool EnableActionMenu { get; set; }
    [JsonPropertyName("loadoutPremiumCost")]
    public decimal LoadoutCost { get; set; }
    [JsonPropertyName("vehicleAbandonmentDistance")]
    public float MaxVehicleAbandonmentDistance { get; set; }
    [JsonPropertyName("vehicleDismountMaxHeight")]
    public float MaxVehicleHeightToLeave { get; set; }
    [JsonPropertyName("rotation")]
    public string GamemodeRotation { get; set; }
    [JsonPropertyName("disableNameFilter")]
    public bool DisableNameFilter { get; set; }
    [JsonPropertyName("nameFilterAlnumLength")]
    public int MinAlphanumericStringLength { get; set; }
    [JsonPropertyName("enableReporter")]
    public bool EnableReporter { get; set; }
    [JsonPropertyName("blockLandmineFriendlyFire")]
    public bool BlockLandmineFriendlyFire { get; set; }
    [JsonPropertyName("disableDailyQuests")]
    public bool DisableDailyQuests { get; set; }
    [JsonPropertyName("playerJoinLeaveMessages")]
    public bool EnablePlayerJoinLeaveMessages { get; set; }
    [JsonPropertyName("playerJoinLeaveTeamMessages")]
    public bool EnablePlayerJoinLeaveTeamMessages { get; set; }
    [JsonPropertyName("timeBetweenAnnouncements")]
    public float SecondsBetweenAnnouncements { get; set; }
    [JsonPropertyName("sendActionLogs")]
    public bool SendActionLogs { get; set; }
    [JsonPropertyName("disableMissingAssetKick")]
    public bool DisableMissingAssetKick { get; set; }
    [JsonPropertyName("nerds")]
    public List<ulong> Nerds { get; set; }
    [JsonPropertyName("disableDailyRestart")]
    public bool DisableDailyRestart { get; set; }
    [JsonPropertyName("disableAprilFools")]
    public bool DisableAprilFools { get; set; }
    [JsonPropertyName("disableKitMenu")]
    public bool DisableKitMenu { get; set; }
    [JsonPropertyName("steam_api_key")]
    public string? SteamAPIKey { get; set; }
    [JsonPropertyName("stripe_api_key")]
    public string? StripeAPIKey { get; set; }
    [JsonPropertyName("website_domain")]
    public string? WebsiteDomain
    {
        get => WebsiteUri?.OriginalString;
        set => WebsiteUri = value == null ? null : new Uri(value);
    }

    [JsonIgnore]
    public Uri? WebsiteUri { get; set; }

    public override void SetDefaults()
    {
        ModerationSettings = new ModerationConfig();
        TCPSettings = new TCPConfig();
        SQL = new MySqlData { Database = "unturned", Host = "127.0.0.1", Password = "password", Port = 3306, Username = "root", CharSet = "utf8mb4" };
        SqlConnectionString = SQL.GetConnectionString("UCWarfare", true, false);
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
        Nerds = new List<ulong> { 76561198312948915ul };
        DisableDailyRestart = false;
        DisableAprilFools = false;
        DisableKitMenu = false;
        SteamAPIKey = null;
        StripeAPIKey = null;
        WebsiteDomain = null;
    }
    public class ModerationConfig
    {
        [JsonPropertyName("adminOffDuty")]
        public string AdminOffDutyGroup { get; set; }
        [JsonPropertyName("adminOnDuty")]
        public string AdminOnDutyGroup { get; set; }
        [JsonPropertyName("internOffDuty")]
        public string InternOffDutyGroup { get; set; }
        [JsonPropertyName("internOnDuty")]
        public string InternOnDutyGroup { get; set; }
        [JsonPropertyName("helper")]
        public string HelperGroup { get; set; }
        [JsonPropertyName("vehiclePlaceWhitelist")]
        public List<ushort> AllowedBarricadesOnVehicles { get; set; }
        [JsonPropertyName("shutdownMessageInterval")]
        public float TimeBetweenShutdownMessages { get; set; }
        [JsonPropertyName("battleyeMessageBlacklist")]
        public string[] BattleyeExclusions { get; set; }
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
        public bool EnableTCPServer { get; set; }
        public string TCPServerIP { get; set; }
        public ushort TCPServerPort { get; set; }
        public string TCPServerIdentity { get; set; }

        public TCPConfig()
        {
            EnableTCPServer = true;
            TCPServerIP = "127.0.0.1";
            TCPServerPort = 31902;
            TCPServerIdentity = "ucwarfare";
        }
    }
}