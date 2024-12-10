using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("rally"), MetadataFile]
internal sealed class RallyCommand : IExecutableCommand
{
    /// <inheritdoc />
    public required CommandContext Context { get; init; }
    
    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
#if false
        Context.AssertGamemode<ISquads>();

        Context.AssertRanByPlayer();

        Context.AssertHelpCheck(0, Syntax + " - " + Help);

        if (!UCWarfare.Config.EnableSquads)
        {
            throw Context.Reply(T.SquadsDisabled);
        }

        if (Context.Player.Squad is null)
            throw Context.Reply(T.RallyNotSquadleader);

        RallyPoint? rallypoint = Context.Player.Squad.RallyPoint;

        if (rallypoint == null || !rallypoint.IsActive)
            throw Context.Reply(T.RallyNotActiveSL);

        if (Context.MatchParameter(0, "cancel", "c", "abort", "deny"))
        {
            if (rallypoint.IsDeploying)
            {
                if (Context.Player.IsSquadLeader())
                {
                    rallypoint.AwaitingPlayers.Clear();
                    rallypoint.ShowUIForPlayer(Context.Player);
                    Context.Reply(T.RallyCancel);
                }
                else
                {
                    rallypoint.AwaitingPlayers.RemoveAll(p => p.Steam64 == Context.CallerId.m_SteamID);
                    rallypoint.ShowUIForPlayer(Context.Player);
                    Context.Reply(T.RallyCancel);
                }
            }
            else throw Context.Reply(T.RallyNoDeny);
        }
        else if (Context.HasArgsExact(0))
        {
            if (Context.Player.Squad is null || !Context.Player.IsSquadLeader())
                throw Context.Reply(T.RallyNotSquadleader);

            if (!rallypoint.IsDeploying)
            {
                if (CooldownManager.HasCooldown(Context.Player, CooldownType.Rally, out Cooldown cooldown))
                    throw Context.Reply(T.RallyCooldown, cooldown);

                rallypoint.StartDeployment();
                CooldownManager.StartCooldown(Context.Player, CooldownType.Rally, SquadManager.Config.RallyCooldown);
                Context.Defer();
            }
            else throw Context.Reply(T.RallyAlreadyDeploying);
        }
        else throw Context.SendCorrectUsage(Syntax);
#endif
        return UniTask.CompletedTask;
    }
}
