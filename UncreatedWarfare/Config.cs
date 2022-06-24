using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Uncreated.SQL;

namespace Uncreated.Warfare;

public class SystemConfig : Config<SystemConfigData>
{
    public SystemConfig() : base(Warfare.Data.Paths.BaseDirectory, "sys_config.json", "sysconfig")
    {
    }

    protected override void OnReload()
    {

    }
}

public class SystemConfigData : ConfigData
{
    [JsonPropertyName("moderation")]
    public ModerationConfig ModerationSettings;
    [JsonPropertyName("tcpServer")]
    public TCPConfig TCPSettings;
    [JsonPropertyName("mysql")]
    public MySqlData SQL;
    [JsonPropertyName("debugMode")]
    public bool Debug;
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
    [JsonPropertyName("loadoutPremiumCost")]
    public float LoadoutCost;
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
    [JsonPropertyName("enableQuests")]
    public bool EnableQuests;

    public override void SetDefaults()
    {
        this.ModerationSettings = new ModerationConfig();
        this.TCPSettings = new TCPConfig();
        this.SQL = new MySqlData { Database = "unturned", Host = "127.0.0.1", Password = "password", Port = 3306, Username = "root", CharSet = "utf8mb4" };
        this.Debug = true;
        this.AllowCosmetics = false;
        this.ModifySkillLevels = true;
        this.AllowBatteryStealing = false;
        this.DiscordInviteCode = "ucn"; // https://discord.gg/code
        this.InjuredLifeTimeSeconds = 90f;
        this.InjuredDamageMultiplier = 0.1f;
        this.EnableMortarWarning = true;
        this.MortarWarningDistance = 75f;
        this.StatsInterval = 1;
        this.AfkCheckInterval = 450f;
        this.AMCDamageMultiplier = 0.25f;
        this.OverrideKitRequirements = false;
        this.MaxTimeInStorages = 15f;
        this.ClearItemsOnAmmoBoxUse = true;
        this.RelayMicsDuringEndScreen = true;
        this.EnableSquads = true;
        this.LoadoutCost = 10;
        this.MaxVehicleAbandonmentDistance = 300f;
        this.MaxVehicleHeightToLeave = 50f;
        this.GamemodeRotation = "TeamCTF:2.0, Invasion:1.0, Insurgency:1.0";
        this.DisableNameFilter = false;
        this.MinAlphanumericStringLength = 5;
        this.EnableReporter = true;
        this.BlockLandmineFriendlyFire = true;
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
            this.InternOnDutyGroup = "intern";
            this.InternOffDutyGroup = "intern-od";
            this.AdminOnDutyGroup = "admin";
            this.AdminOffDutyGroup = "admin-od";
            this.AllowedBarricadesOnVehicles = new List<ushort>();
            this.TimeBetweenShutdownMessages = 60f;
            this.BattleyeExclusions = new string[]
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
            this.EnableTCPServer = true;
            this.TCPServerIP = "127.0.0.1";
            this.TCPServerPort = 31902;
            this.TCPServerIdentity = "ucwarfare";
        }
    }
}