using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Commands;

[Command("unmute")]
[MetadataFile(nameof(GetHelpMetadata))]
public class UnmuteCommand : IExecutableCommand
{
#if false
    private const string SYNTAX = "/unmute";
    private const string HELP = "Lift a mute offense from a player.";
#endif
    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Lift a mute offense from a player",
            Parameters =
            [
                new CommandParameter("Player", typeof(IPlayer))
            ]
        };
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        throw Context.SendNotImplemented();
#if false
        Context.AssertArgs(1, SYNTAX);

        Context.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        if (Context.TryGet(0, out ulong playerId, out UCPlayer? onlinePlayer))
        {
            if (onlinePlayer is not null)
            {
                if (onlinePlayer.MuteType == MuteType.None || onlinePlayer.TimeUnmuted < DateTime.Now)
                {
                    Context.Reply(T.UnmuteNotMuted, onlinePlayer);
                    return;
                }
            }

            Task.Run(async () =>
            {
                try
                {
                    await OffenseManager.UnmutePlayerAsync(playerId, Context.CallerID, DateTimeOffset.UtcNow);
                }
                catch (Exception ex)
                {
                    L.LogError("Error unmuting " + playerId + ".");
                    L.LogError(ex);
                }
            });
            Context.Defer();
        }
        else Context.Reply(T.PlayerNotFound);
#endif
    }
}
