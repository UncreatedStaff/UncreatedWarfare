using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Commands
{
    public class MuteCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "mute";
        public string Help => "Mute players in either voice chat or text chat.";
        public string Syntax => "/mute <voice|text|both> <name or steam64> <permanent | duration in minutes> <reason...>";
        public List<string> Aliases => new List<string>(0);
        public List<string> Permissions => new List<string>(1) { "uc.mute" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UCPlayer admin = UCPlayer.FromIRocketPlayer(caller);

            if (command.Length < 4)
                goto Syntax;
            EMuteType type;
            switch (command[0].ToLower())
            {
                case "voice":
                    type = EMuteType.VOICE_CHAT;
                    break;
                case "text":
                    type = EMuteType.TEXT_CHAT;
                    break;
                case "both":
                    type = EMuteType.BOTH;
                    break;
                default:
                    goto Syntax;
            }

            if (!int.TryParse(command[2], System.Globalization.NumberStyles.Any, Data.Locale, out int duration) || duration < -1 || duration == 0)
                if (command[2].ToLower().StartsWith("perm"))
                    duration = -1;
                else
                    goto CantReadDuration;

            UCPlayer player;
            if (ulong.TryParse(command[1], System.Globalization.NumberStyles.Any, Data.Locale, out ulong target) && OffenseManager.IsValidSteam64ID(target))
            {
                player = UCPlayer.FromID(target);
            }
            else
            {
                player = UCPlayer.FromName(command[1], true);
                if (player == null) goto NoPlayerFound;
                target = player.Steam64;
            }

            string reason = string.Join(" ", command, 3, command.Length - 3);

            OffenseManager.MutePlayer(player, target, admin, type, duration, reason).ConfigureAwait(false);

            return;

        Syntax:
            if (admin == null)
                L.Log(Translation.Translate("mute_syntax", 0, out _), ConsoleColor.Yellow);
            else
                admin.SendChat("mute_syntax");
            return;
        NoPlayerFound:
            if (admin == null)
                L.Log(Translation.Translate("mute_no_player_found", 0, out _), ConsoleColor.Yellow);
            else
                admin.SendChat("mute_no_player_found");
            return;
        CantReadDuration:
            if (admin == null)
                L.Log(Translation.Translate("mute_cant_read_duration", 0, out _), ConsoleColor.Yellow);
            else
                admin.SendChat("mute_cant_read_duration");
            return;
        }
    }
    public enum EMuteType : byte
    {
        NONE = 0,
        VOICE_CHAT = 1,
        TEXT_CHAT = 2,
        BOTH = 3
    }
}