using Cysharp.Threading.Tasks;
using System.Threading;
using Uncreated.Warfare.Commands.Dispatch;

namespace Uncreated.Warfare.Commands;

[Command("heal")]
[HelpMetadata(nameof(GetHelpMetadata))]
public class HealCommand : IExecutableCommand
{
    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Heal yourself or someone else to max health and revive them if they're injured.",
            Parameters =
            [
                new CommandParameter("Player", typeof(IPlayer))
                {
                    IsOptional = true,
                    IsRemainder = true
                }
            ]
        };
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertHelpCheck(0, "/heal [player] - Heal yourself or someone else to max health and revive them if they're injured.");

        Context.AssertOnDuty();

        if (Context.TryGet(0, out _, out UCPlayer? onlinePlayer) && onlinePlayer is not null)
        {
            onlinePlayer.Player.life.sendRevive();

            if (Data.Is(out IRevives rev))
                rev.ReviveManager.RevivePlayer(onlinePlayer);

            Context.Reply(T.HealPlayer, onlinePlayer);

            if (onlinePlayer.Steam64 != Context.CallerId.m_SteamID)
                onlinePlayer.SendChat(T.HealSelf);
        }
        else
        {
            Context.AssertRanByPlayer();

            Context.Player.UnturnedPlayer.life.sendRevive();

            if (Data.Is(out IRevives rev))
                rev.ReviveManager.RevivePlayer(Context.Player);

            Context.Reply(T.HealSelf);
        }
    }
}