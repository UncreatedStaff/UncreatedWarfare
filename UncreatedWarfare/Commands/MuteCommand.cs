using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Players;

namespace Uncreated.Warfare.Commands
{
    public class MuteCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "mute";
        public string Help => "Mute players in either voice chat or text chat.";
        public string Syntax => "/mute <voice|text|both> <name or steam64> <permanent | duration in minutes> <reason...>";
        private readonly List<string> _aliases = new List<string>(0);
        public List<string> Aliases => _aliases;
        private readonly List<string> _permissions = new List<string>(1) { "uc.mute" };
		public List<string> Permissions => _permissions;
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UCPlayer? admin = UCPlayer.FromIRocketPlayer(caller);
            
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

            if (!(ulong.TryParse(command[1], System.Globalization.NumberStyles.Any, Data.Locale, out ulong target) && OffenseManager.IsValidSteam64ID(target)))
            {
                UCPlayer? player = UCPlayer.FromName(command[1], true);
                if (player == null) goto NoPlayerFound;
                target = player.Steam64;
            }

            string reason = string.Join(" ", command, 3, command.Length - 3);
            MutePlayer(target, admin == null ? 0ul : admin.Steam64, type, duration, reason);
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

        public static void MutePlayer(ulong violator, ulong admin, EMuteType type, int duration, string reason)
        {
            UCPlayer? muted = UCPlayer.FromID(violator);
            UCPlayer? muter = UCPlayer.FromID(admin);
            Task.Run(async () => {
                await OffenseManager.MutePlayer(muted, violator, admin, type, duration, reason);
                FPlayerName names = await F.GetPlayerOriginalNamesAsync(violator);
                FPlayerName names2 = await F.GetPlayerOriginalNamesAsync(admin);
                await UCWarfare.ToUpdate();
                string dur = duration == -1 ? "PERMANENT" : ((uint)duration).GetTimeFromMinutes(0);
                ActionLog.Add(EActionLogType.MUTE_PLAYER, $"MUTED {violator} FOR \"{reason}\" DURATION: " + dur);

                if (muter == null)
                {
                    if (duration == -1)
                    {
                        foreach (LanguageSet set in Translation.EnumerateLanguageSets(violator, admin))
                        {
                            string e = Translation.TranslateEnum(type, set.Language);
                            while (set.MoveNext())
                            {
                                Chat.Broadcast(set, "mute_broadcast_operator_permanent", names.CharacterName, e);
                            }
                        }
                        L.Log(Translation.Translate("mute_feedback", 0, out _, names.PlayerName, violator.ToString(),
                            dur, Translation.TranslateEnum(type, 0), reason));
                    }
                    else
                    {
                        foreach (LanguageSet set in Translation.EnumerateLanguageSets(violator, admin))
                        {
                            string e = Translation.TranslateEnum(type, set.Language);
                            while (set.MoveNext())
                            {
                                Chat.Broadcast(set, "mute_broadcast_operator", names.CharacterName, e, dur);
                            }
                        }
                        L.Log(Translation.Translate("mute_feedback_permanent", 0, out _, names.PlayerName, violator.ToString(),
                            Translation.TranslateEnum(type, 0), reason));
                    }
                }
                else
                {
                    if (duration == -1)
                    {
                        foreach (LanguageSet set in Translation.EnumerateLanguageSets(violator, admin))
                        {
                            string e = Translation.TranslateEnum(type, set.Language);
                            while (set.MoveNext())
                            {
                                Chat.Broadcast(set, "mute_broadcast_permanent", names.CharacterName, names2.CharacterName, e);
                            }
                        }
                        muter.SendChat("mute_feedback_permanent", names.PlayerName, violator.ToString(), Translation.TranslateEnum(type, admin));
                    }
                    else
                    {
                        foreach (LanguageSet set in Translation.EnumerateLanguageSets(violator, admin))
                        {
                            string e = Translation.TranslateEnum(type, set.Language);
                            while (set.MoveNext())
                            {
                                Chat.Broadcast(set, "mute_broadcast", names.CharacterName, names2.CharacterName, e, dur);
                            }
                        }
                        muter.SendChat("mute_feedback", names.PlayerName, violator.ToString(), dur, Translation.TranslateEnum(type, admin));
                    }
                }
                if (muted != null)
                {
                    if (admin == 0)
                    {
                        if (duration == -1)
                            muted.SendChat("mute_dm_operator_permanent", reason, Translation.TranslateEnum(type, muted));
                        else
                            muted.SendChat("mute_dm_operator", reason, dur, Translation.TranslateEnum(type, muted));
                    }
                    else
                    {
                        if (duration == -1)
                            muted.SendChat("mute_dm_permanent", names2.CharacterName, reason, Translation.TranslateEnum(type, muted));
                        else
                            muted.SendChat("mute_dm", names2.CharacterName, reason, dur, Translation.TranslateEnum(type, muted));
                    }
                }
            }).ConfigureAwait(false);
        }
    }
    [Translatable]
    public enum EMuteType : byte
    {
        NONE = 0,
        VOICE_CHAT = 1,
        TEXT_CHAT = 2,
        BOTH = 3
    }
}