using Newtonsoft.Json;
using Rocket.API;
using System.Xml.Serialization;

namespace UncreatedWarfare
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
        [XmlElement("Discord_Integration")]
        public DiscordSettings DiscordSettings;
        [XmlElement("FOBs")]
        public FOBSettings FobSettings;
        [XmlElement("MySQL")]
        public MySqlData SQL;
        public ulong Team1ID;
        public ulong Team2ID;
        public bool Debug;
        public bool SendAssetsOnStartup;
        public float DelayAfterConnectionToSendTranslations;
        public float MaxMapHeight;
        public void LoadDefaults()
        {
            Modules = new Modules();
            FlagSettings = new FlagSettings();
            AdminLoggerSettings = new AdminLoggerSettings();
            PlayerStatsSettings = new PlayerStatsSettings();
            TeamSettings = new TeamSettings();
            FobSettings = new FOBSettings();
            SQL = new MySqlData { Database = "unturned", Host = "127.0.0.1", Password = "password", Port = 3306, Username = "admin", CharSet = "utf8mb4" };
            Team1ID = 1;
            Team2ID = 2;
            Debug = true;
            SendAssetsOnStartup = false;
            DelayAfterConnectionToSendTranslations = 0.5f;
            MaxMapHeight = 150;
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
        public string CurrentGamePreset;
        public float PlayerCheckSpeedSeconds;

        public bool UseUI;
        public bool UseChat;
        public ushort UIID;
        public string charactersForUI;

        public int RequiredPlayerDifferenceToCapture;
        public FlagSettings()
        {
            NeutralColor = "ffffff";
            CurrentGamePreset = "goose_bay";
            PlayerCheckSpeedSeconds = 0.25f;

            UseUI = true;
            UseChat = false;
            UIID = 32366;
            charactersForUI = "456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

            RequiredPlayerDifferenceToCapture = 2;
        }
    }
    public class AdminLoggerSettings
    {
        public string AdminOffDutyGroup;
        public string AdminOnDutyGroup;
        public string InternOffDutyGroup;
        public string InternOnDutyGroup;
        public AdminLoggerSettings()
        {
            this.InternOnDutyGroup = "intern";
            this.InternOffDutyGroup = "intern-od";
            this.AdminOnDutyGroup = "admin";
            this.AdminOffDutyGroup = "admin-od";
        }
    }
    public class PlayerStatsSettings
    {
        public bool EnablePlayerList;
        public string NJS_ServerURL;
        public string ServerName;
        public float StatUpdateFrequency;
        public PlayerStatsSettings()
        {
            EnablePlayerList = true;
            NJS_ServerURL = "http://localhost:8080/";
            ServerName = "warfare";
            StatUpdateFrequency = 30.0f;
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
            SendPlayerList = true;
        }
    }
    public class FOBSettings
    {
        public ushort FOBID;
        public ushort FOBID_Unbuilt;
        public FOBSettings()
        {
            FOBID_Unbuilt = 38310;
            FOBID = 38311;
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