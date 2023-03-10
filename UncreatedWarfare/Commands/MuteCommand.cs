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

    public MuteCommand() : base("mute", EAdminType.MODERATOR)
    {
        Structure = new CommandStructure
        {
            Description = "Mute players in either voice chat or text chat.",
            Parameters = new CommandParameter[]
            {
                new CommandParameter("Type", "Voice", "Text", "Both")
                {
                    ChainDisplayCount = 4,
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Player", typeof(IPlayer))
                        {
                            Parameters = new CommandParameter[]
                            {
                                new CommandParameter("Duration", typeof(TimeSpan), "Permanent")
                                {
                                    Parameters = new CommandParameter[]
                                    {
                                        new CommandParameter("Reason", typeof(string))
                                        {
                                            IsRemainder = true
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertHelpCheck(0, SYNTAX + HELP);
        ctx.AssertHelpCheck(1, SYNTAX + HELP);

        if (!ctx.HasArgs(4))
            throw ctx.SendCorrectUsage(SYNTAX);

        EMuteType type = ctx.MatchParameter(0, "voice")
            ? EMuteType.VOICE_CHAT
            : (ctx.MatchParameter(0, "text")
                ? EMuteType.TEXT_CHAT
                : (ctx.MatchParameter(0, "both")
                    ? EMuteType.BOTH
                    : throw ctx.SendCorrectUsage(SYNTAX)));

        int duration = Util.ParseTime(ctx.Get(2)!);

        if (duration < -1 || duration == 0)
            throw ctx.Reply(T.InvalidTime);

        if (!ctx.TryGet(1, out ulong targetId, out _))
            throw ctx.Reply(T.PlayerNotFound);
        Task.Run(async () =>
        {
            try
            {
                await OffenseManager.MutePlayerAsync(targetId, ctx.CallerID, type, duration, ctx.GetRange(3)!, DateTimeOffset.UtcNow);
            }
            catch (Exception ex)
            {
                L.LogError("Error muting " + targetId + ".");
                L.LogError(ex);
            }
        });
        ctx.Defer();
    }
}
[Translatable("Mute Severity")]
[Flags]
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