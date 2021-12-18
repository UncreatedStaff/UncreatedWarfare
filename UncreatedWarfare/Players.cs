using Rocket.Unturned.Player;
using SDG.Unturned;
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
        public static void QueueMessage(UCPlayer player, ToastMessage message) => QueueMessage(player.Player, message);
        public static void QueueMessage(SteamPlayer player, string message, ToastMessageSeverity severity = ToastMessageSeverity.INFO) => QueueMessage(player.player, new ToastMessage(message, severity));
        public static void QueueMessage(SteamPlayer player, string message, string second_message, ToastMessageSeverity severity = ToastMessageSeverity.INFO) => QueueMessage(player.player, new ToastMessage(message, second_message, severity));
        public static void QueueMessage(SteamPlayer player, ToastMessage message) => QueueMessage(player.player, message);
        public static void QueueMessage(Player player, string message, ToastMessageSeverity severity = ToastMessageSeverity.INFO) => QueueMessage(player, new ToastMessage(message, severity));
        public static void QueueMessage(Player player, string message, string second_message, ToastMessageSeverity severity = ToastMessageSeverity.INFO) => QueueMessage(player, new ToastMessage(message, second_message, severity));
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
        public static void Write(ByteWriter W, ToastMessage M)
        {
            W.Write(M.Message);
            if (M.SecondaryMessage != null)
            {
                W.Write(true);
                W.Write(M.SecondaryMessage);
            }
            else
                W.Write(false);
            W.Write(M.Severity);
            W.Write(M.delay);
        }
        public static ToastMessage Read(ByteReader R) =>
            new ToastMessage()
            {
                Message = R.ReadString(),
                SecondaryMessage = R.ReadBool() ? R.ReadString() : null,
                Severity = R.ReadEnum<ToastMessageSeverity>(),
                delay = R.ReadFloat()
            };
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
}