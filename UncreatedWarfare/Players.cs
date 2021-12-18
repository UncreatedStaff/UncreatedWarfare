using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using Uncreated.Networking.Encoding;
using Uncreated.Warfare;

namespace Uncreated.Players
{
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
    }
    public struct ToastMessage
    {
        public readonly EToastMessageSeverity Severity;
        private readonly long time;
        public readonly string Message;
        /// <summary>NULLABLE</summary>
        public readonly string SecondaryMessage;
        public const float FULL_TOAST_TIME = 12f;
        public const float MINI_TOAST_TIME = 4f;
        public const float BIG_TOAST_TIME = 5.5f;
        public static bool operator ==(ToastMessage left, ToastMessage right) => left.time == right.time && left.Message == right.Message;
        public static bool operator !=(ToastMessage left, ToastMessage right) => left.time != right.time || left.Message != right.Message;
        public override int GetHashCode() => time.GetHashCode() / 2 + Message.GetHashCode() / 2;
        public override bool Equals(object obj) => obj is ToastMessage msg && msg.time == time && msg.Message == Message;
        public ToastMessage(string message, EToastMessageSeverity severity)
        {
            this.time = DateTime.Now.Ticks;
            this.Message = message;
            this.SecondaryMessage = null;
            this.Severity = severity;
        }
        public ToastMessage(string message, string secondmessage, EToastMessageSeverity severity = EToastMessageSeverity.INFO)
        {
            this.time = DateTime.Now.Ticks;
            this.Message = message;
            this.SecondaryMessage = secondmessage;
            this.Severity = severity;
        }
        public static void QueueMessage(UnturnedPlayer player, string message, EToastMessageSeverity severity = EToastMessageSeverity.INFO) => QueueMessage(player.Player, new ToastMessage(message, severity));
        public static void QueueMessage(UnturnedPlayer player, string message, string second_message, EToastMessageSeverity severity = EToastMessageSeverity.INFO) => QueueMessage(player.Player, new ToastMessage(message, second_message, severity));
        public static void QueueMessage(UnturnedPlayer player, ToastMessage message) => QueueMessage(player.Player, message);
        public static void QueueMessage(UCPlayer player, string message, EToastMessageSeverity severity = EToastMessageSeverity.INFO) => QueueMessage(player.Player, new ToastMessage(message, severity));
        public static void QueueMessage(UCPlayer player, string message, string second_message, EToastMessageSeverity severity = EToastMessageSeverity.INFO) => QueueMessage(player.Player, new ToastMessage(message, second_message, severity));
        public static void QueueMessage(UCPlayer player, ToastMessage message) => QueueMessage(player.Player, message);
        public static void QueueMessage(SteamPlayer player, string message, EToastMessageSeverity severity = EToastMessageSeverity.INFO) => QueueMessage(player.player, new ToastMessage(message, severity));
        public static void QueueMessage(SteamPlayer player, string message, string second_message, EToastMessageSeverity severity = EToastMessageSeverity.INFO) => QueueMessage(player.player, new ToastMessage(message, second_message, severity));
        public static void QueueMessage(SteamPlayer player, ToastMessage message) => QueueMessage(player.player, message);
        public static void QueueMessage(Player player, string message, EToastMessageSeverity severity = EToastMessageSeverity.INFO) => QueueMessage(player, new ToastMessage(message, severity));
        public static void QueueMessage(Player player, string message, string second_message, EToastMessageSeverity severity = EToastMessageSeverity.INFO) => QueueMessage(player, new ToastMessage(message, second_message, severity));
        public static void QueueMessage(Player player, ToastMessage message)
        {
            if (F.TryGetPlaytimeComponent(player, out Warfare.Components.PlaytimeComponent c))
                c.QueueMessage(message, false);
        }
        public static void SendMessage(Player player, ToastMessage message)
        {
            if (F.TryGetPlaytimeComponent(player, out Warfare.Components.PlaytimeComponent c))
                c.QueueMessage(message, true);
        }
        public static void QueueMessagePriority(UnturnedPlayer player, string message, EToastMessageSeverity severity = EToastMessageSeverity.INFO) => QueueMessagePriority(player.Player, new ToastMessage(message, severity));
        public static void QueueMessagePriority(UnturnedPlayer player, string message, string second_message, EToastMessageSeverity severity = EToastMessageSeverity.INFO) => QueueMessagePriority(player.Player, new ToastMessage(message, second_message, severity));
        public static void QueueMessagePriority(UnturnedPlayer player, ToastMessage message) => QueueMessagePriority(player.Player, message);
        public static void QueueMessagePriority(UCPlayer player, string message, EToastMessageSeverity severity = EToastMessageSeverity.INFO) => QueueMessagePriority(player.Player, new ToastMessage(message, severity));
        public static void QueueMessagePriority(UCPlayer player, string message, string second_message, EToastMessageSeverity severity = EToastMessageSeverity.INFO) => QueueMessagePriority(player.Player, new ToastMessage(message, second_message, severity));
        public static void QueueMessagePriority(UCPlayer player, ToastMessage message) => QueueMessagePriority(player.Player, message);
        public static void QueueMessagePriority(SteamPlayer player, string message, EToastMessageSeverity severity = EToastMessageSeverity.INFO) => QueueMessagePriority(player.player, new ToastMessage(message, severity));
        public static void QueueMessagePriority(SteamPlayer player, string message, string second_message, EToastMessageSeverity severity = EToastMessageSeverity.INFO) => QueueMessagePriority(player.player, new ToastMessage(message, second_message, severity));
        public static void QueueMessagePriority(SteamPlayer player, ToastMessage message) => QueueMessagePriority(player.player, message);
        public static void QueueMessagePriority(Player player, string message, EToastMessageSeverity severity = EToastMessageSeverity.INFO) => QueueMessagePriority(player, new ToastMessage(message, severity));
        public static void QueueMessagePriority(Player player, string message, string second_message, EToastMessageSeverity severity = EToastMessageSeverity.INFO) => QueueMessagePriority(player, new ToastMessage(message, second_message, severity));
        public static void QueueMessagePriority(Player player, ToastMessage message)
        {
            if (F.TryGetPlaytimeComponent(player, out Warfare.Components.PlaytimeComponent c))
                c.QueueMessage(message, true);
        }
    }
    public enum EToastMessageSeverity : byte
    {
        INFO = 0,
        WARNING = 1,
        SEVERE = 2,
        MINIXP = 3,
        MINIOFFICERPTS = 4,
        BIG = 5
    }
}