using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using Uncreated.Encoding;
using Uncreated.Warfare;

namespace Uncreated.Players
{
    public struct FPlayerName
    {
        public static readonly FPlayerName Nil = new FPlayerName() { CharacterName = string.Empty, NickName = string.Empty, PlayerName = string.Empty, Steam64 = 0 };
        public static readonly FPlayerName Console = new FPlayerName() { CharacterName = "Console", NickName = "Console", PlayerName = "Console", Steam64 = 0 };
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
        public FPlayerName(ulong player)
        {
            string ts = player.ToString();
            this.PlayerName = ts;
            this.CharacterName = ts;
            this.NickName = ts;
            this.Steam64 = player;
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
        public static void Write(ByteWriter W, FPlayerName N)
        {
            W.Write(N.Steam64);
            W.Write(N.PlayerName);
            W.Write(N.CharacterName);
            W.Write(N.NickName);
        }
        public static FPlayerName Read(ByteReader R) =>
            new FPlayerName
            {
                Steam64 = R.ReadUInt64(),
                PlayerName = R.ReadString(),
                CharacterName = R.ReadString(),
                NickName = R.ReadString()
            };
        public override string ToString() => PlayerName;
        public static bool operator ==(FPlayerName left, FPlayerName right) => left.Steam64 == right.Steam64;
        public static bool operator !=(FPlayerName left, FPlayerName right) => left.Steam64 != right.Steam64;
        public override bool Equals(object obj) => obj is FPlayerName pn && this.Steam64 == pn.Steam64;
        public override int GetHashCode() => Steam64.GetHashCode();
    }
    public struct ToastMessage
    {
        public readonly EToastMessageSeverity Severity;
        private readonly long time;
        public readonly string Message1;
        public readonly string? Message2;
        public readonly string? Message3;
        public const float FULL_TOAST_TIME = 12f;
        public const float MINI_TOAST_TIME = 4f;
        public const float BIG_TOAST_TIME = 5.5f;
        public static bool operator ==(ToastMessage left, ToastMessage right) => left.time == right.time && left.Message1 == right.Message1;
        public static bool operator !=(ToastMessage left, ToastMessage right) => left.time != right.time || left.Message1 != right.Message1;
        public override int GetHashCode() => time.GetHashCode() / 2 + Message1.GetHashCode() / 2;
        public override bool Equals(object obj) => obj is ToastMessage msg && msg.time == time && msg.Message1 == Message1;
        public ToastMessage(string message1, EToastMessageSeverity severity)
        {
            this.time = DateTime.Now.Ticks;
            this.Message1 = message1;
            this.Message2 = null;
            this.Message3 = null;
            this.Severity = severity;
        }
        public ToastMessage(string message1, string message2, EToastMessageSeverity severity = EToastMessageSeverity.INFO)
        {
            this.time = DateTime.Now.Ticks;
            this.Message1 = message1;
            this.Message2 = message2;
            this.Message3 = null;
            this.Severity = severity;
        }
        public ToastMessage(string message1, string message2, string message3, EToastMessageSeverity severity = EToastMessageSeverity.INFO)
        {
            this.time = DateTime.Now.Ticks;
            this.Message1 = message1;
            this.Message2 = message2;
            this.Message3 = message3;
            this.Severity = severity;
        }
        public static void QueueMessage(UnturnedPlayer player, ToastMessage message, bool priority = false) => QueueMessage(player.Player, message, priority);
        public static void QueueMessage(UCPlayer player, ToastMessage message, bool priority = false) => QueueMessage(player.Player, message, priority);
        public static void QueueMessage(SteamPlayer player, ToastMessage message, bool priority = false) => QueueMessage(player.player, message, priority);
        public static void QueueMessage(Player player, ToastMessage message, bool priority = false)
        {
            if (F.TryGetPlaytimeComponent(player, out Warfare.Components.PlaytimeComponent c))
                c.QueueMessage(message, priority);
        }
    }
    public enum EToastMessageSeverity : byte
    {
        INFO = 0,
        WARNING = 1,
        SEVERE = 2,
        MINI = 3,
        MEDIUM = 4,
        BIG = 5,
        PROGRESS = 6,
        TIP = 7
    }
}