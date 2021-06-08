﻿using System;
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
        public const int DATA_VERSION = 1;
        public ulong steam_id;
        public Usernames usernames;
        public Sessions sessions;
        public Addresses addresses;
        public GlobalizationData globalization_data;
        public string language;
        public DiscordInfo discord_account;
        public WarfareStats warfare_stats;

        [JsonIgnore]
        public bool isOnline;
        [JsonIgnore]
        public SteamPlayer player;
        public static string FileName(ulong steam_id) => Data.StatsDirectory + steam_id.ToString() + ".json";
        [JsonConstructor]
        public UncreatedPlayer(ulong steam_id, Usernames usernames, Sessions sessions, Addresses addresses, GlobalizationData globalization_data, string language, DiscordInfo discord_account, WarfareStats warfare_stats)
        {
            if (steam_id == default) throw new ArgumentException("SteamID was not a valid Steam64 ID", "steam_id");
            this.steam_id = steam_id;
            this.player = PlayerTool.getSteamPlayer(this.steam_id);
            this.isOnline = player != default;
            this.usernames = usernames ?? (isOnline ? new Usernames(F.GetPlayerOriginalNames(player)) : default);
            this.sessions = sessions ?? new Sessions(new Dictionary<long, int>());
            this.addresses = addresses ?? new Addresses();
            this.globalization_data = globalization_data ?? new GlobalizationData();
            this.language = language ?? JSONMethods.DefaultLanguage;
            this.discord_account = discord_account ?? new DiscordInfo();
            this.warfare_stats = warfare_stats ?? new WarfareStats();
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
            this.player = player;
            this.isOnline = true;
            this.usernames = new Usernames(F.GetPlayerOriginalNames(this.player));
            this.sessions = new Sessions(new Dictionary<long, int>());
            this.addresses = new Addresses();
            if (player.getIPv4Address(out uint ip))
                this.addresses.LogIn(Parser.getIPFromUInt32(ip));
            this.globalization_data = new GlobalizationData();
            this.language = Data.Languages.ContainsKey(this.steam_id) ? Data.Languages[this.steam_id] : JSONMethods.DefaultLanguage;
            this.discord_account = new DiscordInfo();
            this.warfare_stats = new WarfareStats();
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
            this.player = PlayerTool.getSteamPlayer(this.steam_id);
            this.isOnline = player != default;
            this.usernames = usernames ?? (isOnline ? new Usernames(F.GetPlayerOriginalNames(player)) : default);
            this.sessions = new Sessions(new Dictionary<long, int>());
            this.addresses = new Addresses();
            if (isOnline && this.player.getIPv4Address(out uint ip))
                this.addresses.LogIn(Parser.getIPFromUInt32(ip));
            this.globalization_data = new GlobalizationData();
            this.language = Data.Languages.ContainsKey(this.steam_id) ? Data.Languages[this.steam_id] : JSONMethods.DefaultLanguage;
            this.discord_account = new DiscordInfo();
            this.warfare_stats = new WarfareStats();
            this.warfare_stats.OnNeedsSave += SaveEscalator;
            this.discord_account.OnNeedsSave += SaveEscalator;
            this.globalization_data.OnNeedsSave += SaveEscalator;
            this.addresses.OnNeedsSave += SaveEscalator;
            this.sessions.OnNeedsSave += SaveEscalator;
            Save();
        }
        public static UncreatedPlayer Load(ulong id)
        {
            string path = FileName(id);
            if (id == default) throw new ArgumentException("SteamID was not a valid Steam64 ID", "steam_id");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                UncreatedPlayer player = JsonConvert.DeserializeObject<UncreatedPlayer>(json);
                if (player != default) return player;
            }
            UncreatedPlayer newplayer = new UncreatedPlayer(id);
            newplayer.SavePath(path);
            return newplayer;
        }
        public override void Save() => SavePath(FileName(steam_id));
        private void SavePath(string path)
        {
            F.Log("Saving " + usernames.player_name, ConsoleColor.DarkCyan);
            using (TextWriter writer = File.CreateText(path))
            {
                JsonSerializer serializer = new JsonSerializer() { Formatting = Formatting.Indented };
                serializer.Serialize(writer, this);
                writer.Close();
                writer.Dispose();
            }
        }
        protected override void SaveEscalator(object sender, EventArgs e) => Save();
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
        public Dictionary<long, int> sessions;
        /// <summary>
        /// Adds a session to <see cref="sessions"/> and auto-saves.
        /// </summary>
        public void AddSession(DateTime start, int duration_seconds)
        {
            sessions.Add(start.Ticks, duration_seconds);
            Save();
        }
        [JsonConstructor]
        public Sessions(Dictionary<long, int> sessions)
        {
            this.sessions = sessions;
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
        public void LogIn(string ip)
        {
            if (ip_list.ContainsKey(ip)) ip_list[ip] = DateTime.Now.Ticks;
            else ip_list.Add(ip, DateTime.Now.Ticks);
            Save();
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
        internal event EventHandler OnNeedsSave;
        /// <summary> Saves all of the player's stats </summary>
        private void InvokeSave() => OnNeedsSave?.Invoke(this, EventArgs.Empty);
        /// <summary>Escalates the save event all the way to <see cref="UncreatedPlayer"/>.</summary>
        protected virtual void SaveEscalator(object sender, EventArgs e) => InvokeSave();
        /// <summary> Saves all of the player's stats </summary>
        public virtual void Save() => InvokeSave();
    }
    public struct FPlayerName
    {
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
    }
}