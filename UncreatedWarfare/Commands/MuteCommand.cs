using SDG.Unturned;
using System;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class MuteCommand : Command
{
    private const string SYNTAX = "/mute <voice|text|both> <name or steam64> <permanent | duration in minutes> <reason...>";
    private const string HELP = "Mute players in either voice chat or text chat.";

    public MuteCommand() : base("mute", EAdminType.MEMBER) { }

    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertHelpCheck(0, SYNTAX + HELP);
        ctx.AssertHelpCheck(1, SYNTAX + HELP);

        if (!ctx.HasArgs(4))
            throw ctx.Reply("mute_syntax");

        EMuteType type = ctx.MatchParameter(0, "voice")
            ? EMuteType.VOICE_CHAT
            : (ctx.MatchParameter(0, "text")
                ? EMuteType.TEXT_CHAT
                : (ctx.MatchParameter(0, "both")
                    ? EMuteType.BOTH 
                    : throw ctx.Reply("mute_syntax")));

        if (!ctx.TryGet(2, out int duration) || duration < -1 || duration == 0)
        {
            if (ctx.MatchParameter(2, "perm", "permanent"))
                duration = -1;
            else
                throw ctx.Reply("mute_cant_read_duration");
        }

        if (!ctx.TryGet(1, out ulong targetId, out _))
            throw ctx.Reply("mute_no_player_found");

        OffenseManager.MutePlayer(targetId, ctx.CallerID, type, duration, ctx.GetRange(3)!);
        ctx.Defer();
    }
}
[Translatable("Mute Severity")]
public enum EMuteType : byte
{
    NONE = 0,
    [Translatable("Voice Chat")]
    VOICE_CHAT = 1,
    [Translatable("Text Chat")]
    TEXT_CHAT = 2,
    [Translatable("Voice and Text Chat")]
    BOTH = 3
}