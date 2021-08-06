using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Uncreated.SQL;
using Uncreated.Warfare;
using System.IO;
using Uncreated.Warfare.Stats;
using Data = Uncreated.Warfare.Data;

namespace Uncreated.Players
{
    public class UncreatedPlayer : PlayerObject
    {
        public static bool isSaving = false;
        public const int DATA_VERSION = 1;
        public ulong steam_id;
        public Usernames usernames;
        public Sessions sessions;
        public Addresses addresses;
        public GlobalizationData globalization_data;
        public string language;
        public DiscordInfo discord_account;
        public WarfareStats warfare_stats;
        public static string FileName(ulong steam_id) => Data.StatsDirectory + steam_id.ToString(Data.Locale) + ".json";
        [JsonConstructor]
        public UncreatedPlayer(ulong steam_id, Usernames usernames, Sessions sessions, Addresses addresses, GlobalizationData globalization_data, string language, DiscordInfo discord_account, WarfareStats warfare_stats)
        {
            if (steam_id == default) throw new ArgumentException("SteamID was not a valid Steam64 ID", "steam_id");
            this.steam_id = steam_id;
            if (usernames == null && F.TryGetPlayerOriginalNamesFromS64(steam_id, out FPlayerName un))
                this.usernames = new Usernames(un);
            else this.usernames = usernames;
            this.sessions = sessions ?? new Sessions(new List<Session>());
            this.addresses = addresses ?? new Addresses();
            this.globalization_data = globalization_data ?? new GlobalizationData();
            this.language = language ?? JSONMethods.DefaultLanguage;
            this.discord_account = discord_account ?? new DiscordInfo();
            this.warfare_stats = warfare_stats ?? new WarfareStats();
            this.warfare_stats.player = this;
            this.warfare_stats.OnNeedsSave += SaveEscalator;
            this.discord_account.OnNeedsSave += SaveEscalator;
            this.globalization_data.OnNeedsSave += SaveEscalator;
            this.addresses.OnNeedsSave += SaveEscalator;
            this.sessions.OnNeedsSave += SaveEscalator;
            if (this.usernames != default) this.usernames.OnNeedsSave += SaveEscalator;
            Save();
        }
        public UncreatedPlayer(SteamPlayer player)
        {
            if (player == default) throw new ArgumentException("Player is null.", "player");
            this.steam_id = player.playerID.steamID.m_SteamID;
            this.usernames = new Usernames(F.GetPlayerOriginalNames(player));
            this.sessions = new Sessions(new List<Session>());
            this.addresses = new Addresses();
            if (player.getIPv4Address(out uint ip))
                this.addresses.LogIn(Parser.getIPFromUInt32(ip));
            this.globalization_data = new GlobalizationData();
            this.language = Data.Languages.ContainsKey(this.steam_id) ? Data.Languages[this.steam_id] : JSONMethods.DefaultLanguage;
            this.discord_account = new DiscordInfo();
            this.warfare_stats = new WarfareStats() { player = this };
            this.addresses.OnNeedsSave += SaveEscalator;
            this.warfare_stats.OnNeedsSave += SaveEscalator;
            this.discord_account.OnNeedsSave += SaveEscalator;
            this.sessions.OnNeedsSave += SaveEscalator;
            this.globalization_data.OnNeedsSave += SaveEscalator;
            Save();
        }
        public UncreatedPlayer(ulong id)
        {
            if(id == default) throw new ArgumentException("SteamID was not a valid Steam64 ID", "id");
            this.steam_id = id;
            if (F.TryGetPlayerOriginalNamesFromS64(steam_id, out FPlayerName un))
                this.usernames = new Usernames(un);
            else
            {
                string s64 = steam_id.ToString();
                this.usernames = new Usernames(s64, s64, s64, new List<string>(), new List<string>(), new List<string>());
            }
            this.sessions = new Sessions(new List<Session>());
            this.addresses = new Addresses();
            this.globalization_data = new GlobalizationData();
            this.language = Data.Languages.ContainsKey(this.steam_id) ? Data.Languages[this.steam_id] : JSONMethods.DefaultLanguage;
            this.discord_account = new DiscordInfo();
            this.warfare_stats = new WarfareStats() { player = this };
            this.warfare_stats.OnNeedsSave += SaveEscalator;
            this.discord_account.OnNeedsSave += SaveEscalator;
            this.globalization_data.OnNeedsSave += SaveEscalator;
            this.addresses.OnNeedsSave += SaveEscalator;
            this.sessions.OnNeedsSave += SaveEscalator;
            Save();
        }
        public UncreatedPlayer() { }
        public static bool TryLoad(ulong id, out UncreatedPlayer player)
        {
            string path = FileName(id);
            if (id == default) throw new ArgumentException("SteamID was not a valid Steam64 ID", "steam_id");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                try
                {
                    player = JsonConvert.DeserializeObject<UncreatedPlayer>(json);
                    return player != default;
                }
                catch (Exception ex)
                {
                    File.WriteAllText(path.Substring(0, path.Length - 5) + "_corrupt.json", json); // resave the file somewhere else then overrwrite it
                    F.LogError($"Error in UncreatedPlayer.TryLoad with id {id}, saved a backup then rewrote the file:");
                    F.LogError(ex);
                }
            }
            player = default;
            return false;
        }
        public static UncreatedPlayer Load(ulong id)
        {
            string path = FileName(id);
            if (id == default) throw new ArgumentException("SteamID was not a valid Steam64 ID", "steam_id");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                try
                {
                    UncreatedPlayer player = JsonConvert.DeserializeObject<UncreatedPlayer>(json);
                    if (player != default) return player;
                }
                catch (Exception ex)
                {
                    File.WriteAllText(path.Substring(0, path.Length - 5) + "_corrupt.json", json); // resave the file somewhere else then overrwrite it
                    F.LogError($"Error in UncreatedPlayer.Load with id {id}, saved a backup then rewrote the file:");
                    F.LogError(ex);
                }
            }
            UncreatedPlayer newplayer = new UncreatedPlayer(id);
            newplayer.SavePathAsync(path);
            return newplayer;
        }
        public static async Task<UncreatedPlayer> LoadAsync(ulong id, bool create = true)
        {
            string path = FileName(id);
            if (id == default) throw new ArgumentException("SteamID was not a valid Steam64 ID", "steam_id");
            if (File.Exists(path))
            {
                string json;
                using (StreamReader reader = File.OpenText(path))
                {
                    json = await reader.ReadToEndAsync();
                    reader.Close();
                    reader.Dispose();
                }
                try
                {
                    UncreatedPlayer player = JsonConvert.DeserializeObject<UncreatedPlayer>(json);
                    if (player != default) return player;
                    else if (!create) return null;
                }
                catch (Exception ex)
                {
                    File.WriteAllText(path.Substring(0, path.Length - 5) + "_corrupt.json", json); // resave the file somewhere else then overrwrite it
                    F.LogError($"Error in UncreatedPlayer.Load with id {id}, saved a backup then rewrote the file:");
                    F.LogError(ex);
                    if (!create) return null;
                }
            }
            if (!create) return null;
            UncreatedPlayer newplayer = new UncreatedPlayer(id);
            newplayer.SavePathAsync(path);
            return newplayer;
        }
        public override void Save() => SavePathAsync(FileName(steam_id));
        public void SaveAsync() => SavePathAsync(FileName(steam_id));
        static readonly JsonSerializerSettings Settings = new JsonSerializerSettings { Formatting = Formatting.Indented };
        private void SavePathAsync(string path)
        {
            _ = Task.Run(async () =>
            {
                F.Log("Saving " + usernames.player_name, ConsoleColor.DarkCyan);
                while (isSaving) await Task.Delay(1);
                isSaving = true;
                try
                {
                    using (TextWriter writer = File.CreateText(path))
                    {
                        string data = JsonConvert.SerializeObject(this, Settings);
                        await writer.WriteAsync(data);
                        writer.Close();
                        writer.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    F.LogError("Error saving player " + usernames.player_name);
                    F.LogError(ex);
                }
                isSaving = false;
            }).ConfigureAwait(false);
        }
        protected override void SaveEscalator() => Save();
        public void LogIn(SteamPlayer player, string server) => LogIn(player, F.GetPlayerOriginalNames(player), server);
        public void LogIn(SteamPlayer player, FPlayerName name, string server)
        {
            if (player != default)
            {
                if (player.getIPv4Address(out uint ip))
                {
                    if (addresses == default) addresses = new Addresses();
                    addresses.LogIn(Parser.getIPFromUInt32(ip), false);
                }
                usernames.PlayerNameObject = name;
            }
            sessions.StartSession(server, false);
            SaveAsync();
        }
        public void UpdateSession(string server, bool save = true)
        {
            sessions.ModifyCurrentSession(server, false);
            if (save) SaveAsync();
        }
    }
    public abstract class StatsCollection : PlayerObject
    {
        public string name;
        [JsonIgnore]
        public string display_name;
        /// <summary> <see cref="DateTime"/> ticks. </summary>
        public long playtime;
    }
    public class Sessions : PlayerObject
    {
        public List<Session> sessions;
        /// <summary>
        /// Adds a session to <see cref="sessions"/> and auto-saves.
        /// </summary>
        public void StartSession(string server, bool save = true)
        {
            sessions.Add(new Session(DateTime.Now.Ticks, 0, server));
            if(save) Save();
        }
        public void ModifyCurrentSession(string server, bool save = true)
        {
            if (sessions.Count == 0) sessions.Add(new Session(DateTime.Now.Ticks, 0, server));
            else
            {
                Session last = sessions.Last(x => x.server == server);
                if (last == null) sessions.Add(new Session(DateTime.Now.Ticks, 0, server));
                else last.length = (int)Math.Round((DateTime.Now - new DateTime(last.start_ticks)).TotalSeconds);
            }
            if (save) Save();
        }
        [JsonConstructor]
        public Sessions(List<Session> sessions)
        {
            this.sessions = sessions ?? new List<Session>();
        }
    }
    public class Session : PlayerObject
    {
        public long start_ticks;
        public int length;
        public string server;
        [JsonConstructor]
        public Session(long start_ticks, int length, string server)
        {
            this.start_ticks = start_ticks;
            this.length = length;
            this.server = server;
        }
        
    }

    public class DiscordInfo : PlayerObject
    {
        public ulong id;
        public string username;
        public string discriminator;

        [JsonConstructor]
        public DiscordInfo(ulong id, string username, string discriminator)
        {
            this.id = id;
            this.username = username ?? string.Empty;
            this.discriminator = discriminator ?? "0000";
        }
        public DiscordInfo()
        {
            this.id = 0;
            this.username = string.Empty;
            this.discriminator = "0000";
        }
    }
    public class GlobalizationData : PlayerObject
    {
        public string status;
        public string continentCode;
        public string country;
        public string countryCode;
        public string region;
        public string regionName;
        public string timezone;

        [JsonConstructor]
        public GlobalizationData(string status, string continentCode, string country, string countryCode, string region, string regionName, string timezone)
        {
            this.status = status ?? "fail";
            this.continentCode = continentCode ?? string.Empty;
            this.country = country ?? string.Empty;
            this.countryCode = countryCode ?? string.Empty;
            this.region = region ?? string.Empty;
            this.regionName = regionName ?? string.Empty;
            this.timezone = timezone ?? string.Empty;
        }
        public GlobalizationData()
        {
            this.status = "fail";
            this.continentCode = string.Empty;
            this.country = string.Empty;
            this.countryCode = string.Empty;
            this.region = string.Empty;
            this.regionName = string.Empty;
            this.timezone = string.Empty;
        }
    }
    public class Addresses : PlayerObject
    {
        public Dictionary<string, long> ip_list;
        /// <summary>
        /// Adds an ip to <see cref="ip_list"/> and auto-saves.
        /// </summary>
        public void LogIn(string ip, bool save = true)
        {
            if (ip_list.ContainsKey(ip)) ip_list[ip] = DateTime.Now.Ticks;
            else ip_list.Add(ip, DateTime.Now.Ticks);
            if (save) Save();
        }
        [JsonConstructor]
        public Addresses(Dictionary<string, long> ip_list)
        {
            this.ip_list = ip_list ?? new Dictionary<string, long>();
        }
        public Addresses()
        {
            this.ip_list = new Dictionary<string, long>();
        }
    }
    public class Usernames : PlayerObject
    {
        public string player_name;
        public string character_name;
        public string nick_name;
        public List<string> player_name_aliases;
        public List<string> character_name_aliases;
        public List<string> nick_name_aliases;
        /// <summary>
        /// Sets the <see cref="player_name"/>, <see cref="character_name"/>, and <see cref="nick_name"/> if they don't match. The old values are then added to aliases. Auto-saves.
        /// </summary>
        [JsonIgnore]
        public FPlayerName PlayerNameObject { 
            set
            {
                if (value.Equals(default)) return;
                if(player_name != value.PlayerName)
                {
                    if (!player_name_aliases.Contains(player_name))
                        player_name_aliases.Add(player_name);
                    player_name = value.PlayerName;
                }
                if (character_name != value.CharacterName)
                {
                    if (!character_name_aliases.Contains(character_name))
                        character_name_aliases.Add(character_name);
                    character_name = value.CharacterName;
                }
                if (nick_name != value.CharacterName)
                {
                    if (!nick_name_aliases.Contains(nick_name))
                        nick_name_aliases.Add(nick_name);
                    nick_name = value.CharacterName;
                }
                Save();
            }
        }
        [JsonConstructor]
        public Usernames(string player_name, string character_name, string nick_name, List<string> player_name_aliases, List<string> character_name_aliases, List<string> nick_name_aliases)
        {
            this.player_name = player_name ?? string.Empty;
            this.character_name = character_name ?? string.Empty;
            this.nick_name = nick_name ?? string.Empty;
            this.player_name_aliases = player_name_aliases ?? new List<string>();
            this.character_name_aliases = character_name_aliases ?? new List<string>();
            this.nick_name_aliases = nick_name_aliases ?? new List<string>();
        }
        public Usernames(FPlayerName player_name)
        {
            this.player_name = player_name.PlayerName;
            this.character_name = player_name.CharacterName;
            this.nick_name = player_name.NickName;
            this.player_name_aliases = new List<string>();
            this.character_name_aliases = new List<string>();
            this.nick_name_aliases = new List<string>();
        }
    }
    public abstract class PlayerObject
    {
        /// <summary>When the main class needs to save.</summary>
        internal event System.Action OnNeedsSave;
        /// <summary> Saves all of the player's stats </summary>
        private void InvokeSave() => OnNeedsSave?.Invoke();
        /// <summary>Escalates the save event all the way to <see cref="UncreatedPlayer"/>.</summary>
        protected virtual void SaveEscalator() => InvokeSave();
        /// <summary> Saves all of the player's stats </summary>
        public virtual void Save() => InvokeSave();
    }
    public struct FPlayerName
    {
        public static readonly FPlayerName Nil = new FPlayerName() { CharacterName = string.Empty, NickName = string.Empty, PlayerName = string.Empty, Steam64 = 0 };
        public ulong Steam64;
        public string PlayerName;
        public string CharacterName;
        public string NickName;

        public FPlayerName(SteamPlayerID player)
        {
            this.PlayerName = player.playerName;
            this.CharacterName = player.characterName;
            this.NickName = player.nickName;
            this.Steam64 = player.steamID.m_SteamID;
        }
        public FPlayerName(SteamPlayer player)
        {
            this.PlayerName = player.playerID.playerName;
            this.CharacterName = player.playerID.characterName;
            this.NickName = player.playerID.nickName;
            this.Steam64 = player.playerID.steamID.m_SteamID;
        }
        public FPlayerName(UnturnedPlayer player)
        {
            this.PlayerName = player.Player.channel.owner.playerID.playerName;
            this.CharacterName = player.Player.channel.owner.playerID.characterName;
            this.NickName = player.Player.channel.owner.playerID.nickName;
            this.Steam64 = player.Player.channel.owner.playerID.steamID.m_SteamID;
        }
        public FPlayerName(Player player)
        {
            this.PlayerName = player.channel.owner.playerID.playerName;
            this.CharacterName = player.channel.owner.playerID.characterName;
            this.NickName = player.channel.owner.playerID.nickName;
            this.Steam64 = player.channel.owner.playerID.steamID.m_SteamID;
        }
        public byte[] GetBytes()
        {
            byte[] st = BitConverter.GetBytes(Steam64);
            byte[] pn = Encoding.UTF8.GetBytes(PlayerName);
            byte[] pnl = BitConverter.GetBytes((ushort)pn.Length);
            byte[] cn = Encoding.UTF8.GetBytes(CharacterName);
            byte[] cnl = BitConverter.GetBytes((ushort)cn.Length);
            byte[] nn = Encoding.UTF8.GetBytes(NickName);
            byte[] nnl = BitConverter.GetBytes((ushort)nn.Length);
            byte[] rtn = new byte[st.Length + pn.Length + cn.Length + nn.Length + pnl.Length + cnl.Length + nnl.Length];
            Array.Copy(st, rtn, st.Length);
            int i = st.Length;
            Array.Copy(pnl, 0, rtn, i, pnl.Length);
            i += pnl.Length;
            Array.Copy(pn, 0, rtn, i, pn.Length);
            i += pn.Length;
            Array.Copy(cnl, 0, rtn, i, cnl.Length);
            i += cnl.Length;
            Array.Copy(cn, 0, rtn, i, cn.Length);
            i += cn.Length;
            Array.Copy(nnl, 0, rtn, i, nnl.Length);
            i += nnl.Length;
            Array.Copy(nn, 0, rtn, i, nn.Length);
            return rtn;
        }
        public static FPlayerName FromBytes(byte[] bytes, out int length, int offset = 0)
        {
            int i = offset;
            if (!Networking.ByteMath.ReadUInt64(out ulong st, i, bytes))
                throw new ArgumentException("Failed to read Steam64 UInt64 Value from relative index 0.", nameof(bytes));
            i += sizeof(ulong);
            if (!Networking.ByteMath.ReadUInt16(out ushort pnl, i, bytes))
                throw new ArgumentException("Failed to read length of PlayerName", nameof(bytes));
            i += sizeof(ushort);
            if (!Networking.ByteMath.ReadString(out string pn, i, bytes, pnl))
                throw new ArgumentException("Failed to read PlayerName", nameof(bytes));
            i += pnl;
            if (!Networking.ByteMath.ReadUInt16(out ushort cnl, i, bytes))
                throw new ArgumentException("Failed to read length of CharacterName", nameof(bytes));
            i += sizeof(ushort);
            if (!Networking.ByteMath.ReadString(out string cn, i, bytes, cnl))
                throw new ArgumentException("Failed to read CharacterName", nameof(bytes));
            i += cnl;
            if (!Networking.ByteMath.ReadUInt16(out ushort nnl, i, bytes))
                throw new ArgumentException("Failed to read length of NickName", nameof(bytes));
            i += sizeof(ushort);
            if (!Networking.ByteMath.ReadString(out string nn, i, bytes, cnl))
                throw new ArgumentException("Failed to read NickName", nameof(bytes));
            i += nnl;
            length = i - offset;
            return new FPlayerName
            {
                Steam64 = st,
                PlayerName = pn,
                CharacterName = cn,
                NickName = nn
            };
        }
        public override string ToString() => PlayerName;
    }
    public struct ToastMessage
    {
        public ToastMessageSeverity Severity;
        public string Message;
        public string SecondaryMessage;
        public float delay;
        public const float FULL_TOAST_TIME = 12f;
        public const float MINI_TOAST_TIME = 4f;
        public const float BIG_TOAST_TIME = 5.5f;
        public ToastMessage(string message, ToastMessageSeverity severity)
        {
            this.Message = message;
            this.SecondaryMessage = null;
            this.Severity = severity;
            switch (severity)
            {
                case ToastMessageSeverity.INFO:
                case ToastMessageSeverity.WARNING:
                case ToastMessageSeverity.SEVERE:
                default:
                    this.delay = FULL_TOAST_TIME;
                    break;
                case ToastMessageSeverity.MINIXP:
                case ToastMessageSeverity.MINIOFFICERPTS:
                    this.delay = MINI_TOAST_TIME;
                    break;
                case ToastMessageSeverity.BIG:
                    this.delay = BIG_TOAST_TIME;
                    break;
            }
        }
        public ToastMessage(string message, string secondmessage, ToastMessageSeverity severity = ToastMessageSeverity.INFO)
        {
            this.Message = message;
            this.SecondaryMessage = secondmessage;
            this.Severity = severity;
            switch (severity)
            {
                case ToastMessageSeverity.INFO:
                case ToastMessageSeverity.WARNING:
                case ToastMessageSeverity.SEVERE:
                default:
                    this.delay = FULL_TOAST_TIME;
                    break;
                case ToastMessageSeverity.MINIXP:
                case ToastMessageSeverity.MINIOFFICERPTS:
                    this.delay = MINI_TOAST_TIME;
                    break;
                case ToastMessageSeverity.BIG:
                    this.delay = BIG_TOAST_TIME;
                    break;
            }
        }
        public static void QueueMessage(UnturnedPlayer player, string message, ToastMessageSeverity severity = ToastMessageSeverity.INFO) => QueueMessage(player.Player, new ToastMessage(message, severity));
        public static void QueueMessage(UnturnedPlayer player, string message, string second_message, ToastMessageSeverity severity = ToastMessageSeverity.INFO) => QueueMessage(player.Player, new ToastMessage(message, second_message, severity));
        public static void QueueMessage(UnturnedPlayer player, ToastMessage message) => QueueMessage(player.Player, message);
        public static void QueueMessage(UCPlayer player, string message, ToastMessageSeverity severity = ToastMessageSeverity.INFO) => QueueMessage(player.Player, new ToastMessage(message, severity));
        public static void QueueMessage(UCPlayer player, string message, string second_message, ToastMessageSeverity severity = ToastMessageSeverity.INFO) => QueueMessage(player.Player, new ToastMessage(message, second_message, severity));
        public static void QueueMessage(UCPlayer player, ToastMessage message) =>  QueueMessage(player.Player, message);
        public static void QueueMessage(SteamPlayer player, string message, ToastMessageSeverity severity = ToastMessageSeverity.INFO) => QueueMessage(player.player, new ToastMessage(message, severity));
        public static void QueueMessage(SteamPlayer player, string message, string second_message, ToastMessageSeverity severity = ToastMessageSeverity.INFO) => QueueMessage(player.player, new ToastMessage(message, second_message, severity));
        public static void QueueMessage(SteamPlayer player, ToastMessage message) => QueueMessage(player.player, message);
        public static void QueueMessage(Player player, string message, ToastMessageSeverity severity = ToastMessageSeverity.INFO) => QueueMessage(player, new ToastMessage(message, severity));
        public static void QueueMessage(Player player, string message, string second_message, ToastMessageSeverity severity = ToastMessageSeverity.INFO) => QueueMessage(player, new ToastMessage(message, second_message, severity));
        public static void QueueMessage(Player player, ToastMessage message)
        {
            if (F.TryGetPlaytimeComponent(player, out Warfare.Components.PlaytimeComponent c))
                c.QueueMessage(message);
        }
    }
    public enum ToastMessageSeverity : byte
    {
        INFO = 0,
        WARNING = 1,
        SEVERE = 2,
        MINIXP = 3,
        MINIOFFICERPTS = 4,
        BIG = 5
    }

    public struct BasicSQLStats
    {
        public ulong Steam64;
        public FPlayerName Usernames;
        public BasicSQLStatsTeam t1;
        public BasicSQLStatsTeam t2;
    }
    public struct BasicSQLStatsTeam
    {
        public uint Kills;
        public uint Deaths;
        public uint Teamkills;
        public uint XP;
        public uint OfficerPoints;
    }
}