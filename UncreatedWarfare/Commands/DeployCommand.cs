using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;

namespace Uncreated.Warfare.Commands;

[Command("deploy", "dep", "warp", "warps", "tpa", "go", "goto", "fob", "deployfob", "df", "dp")]
[HelpMetadata(nameof(GetHelpMetadata))]
public class DeployCommand : IExecutableCommand
{
    private const string Syntax = "/deploy main -OR- /deploy <fob name>";
    private const string Help = "Deploy to a point of interest such as a main base, FOB, VCP, or cache.";

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = Help,
            Parameters =
            [
                new CommandParameter("Location", typeof(IDeployable), "Main"),
                new CommandParameter("Cancel")
                {
                    Aliases = [ "stop" ]
                }
            ]
        };
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        Context.AssertArgs(1, Syntax + " - " + Help);

        if (Context.MatchParameter(0, "cancel", "stop") && Context.Player.UnturnedPlayer.TryGetPlayerData(out UCPlayerData comp) && comp.CurrentTeleportRequest != null)
        {
            comp.CancelDeployment();
            throw Context.Reply(T.DeployCancelled);
        }

        if (Data.Is(out IRevives r) && r.ReviveManager.IsInjured(Context.CallerId.m_SteamID))
            throw Context.Reply(T.DeployInjured);

        string input = Context.GetRange(0)!;

        UCPlayerData? c = Context.Player.UnturnedPlayer.GetPlayerData(out _);
        if (c is null) throw Context.SendUnknownError();

        ulong team = Context.Player.GetTeam();
        if (team is not 1 and not 2)
            throw Context.Reply(T.NotOnCaptureTeam);

        bool inMain = Context.Player.UnturnedPlayer.IsInMain();
        bool inLobby = !inMain && TeamManager.LobbyZone.IsInside(Context.Player.Position);
        bool shouldCancelOnMove = !inMain;
        bool shouldCancelOnDamage = !inMain;

        if (CooldownManager.HasCooldown(Context.Player, CooldownType.Deploy, out Cooldown cooldown))
            throw Context.Reply(T.DeployCooldown, cooldown);

        IFOB? deployFromFob = null;

        if (!(inMain || inLobby))
        {
            if (CooldownManager.HasCooldown(Context.Player, CooldownType.Combat, out Cooldown combatlog))
                throw Context.Reply(T.DeployInCombat, combatlog);

            if (!(Context.Player.IsOnFOB(out deployFromFob) && deployFromFob is not FOB { Bleeding: true } && deployFromFob.CheckDeployable(Context.Player, null)))
                throw Context.Reply(Data.Is<Insurgency>() ? T.DeployNotNearFOBInsurgency : T.DeployNotNearFOB);
        }

        IDeployable? destination = null;
        if (!FOBManager.Loaded || !FOBManager.TryFindFOB(input, team, out destination))
        {
            if (input.Equals("lobby", StringComparison.InvariantCultureIgnoreCase))
                throw Context.Reply(T.DeployLobbyRemoved);

            if (input.Equals("main", StringComparison.InvariantCultureIgnoreCase) ||
                input.Equals("base", StringComparison.InvariantCultureIgnoreCase) ||
                input.Equals("home", StringComparison.InvariantCultureIgnoreCase) ||
                input.Equals("mainbase", StringComparison.InvariantCultureIgnoreCase) ||
                input.Equals("main base", StringComparison.InvariantCultureIgnoreCase) ||
                input.Equals("homebase", StringComparison.InvariantCultureIgnoreCase) ||
                input.Equals("home base", StringComparison.InvariantCultureIgnoreCase))
            {
                destination = TeamManager.GetMain(team);
            }
        }

        if (destination == null)
            throw Context.Reply(T.DeployableNotFound, input);

        if (destination.Equals(deployFromFob))
            throw Context.Reply(T.DeployableAlreadyOnFOB);

        Deployment.DeployTo(Context.Player, deployFromFob, destination, Context, shouldCancelOnMove, shouldCancelOnDamage, startCooldown: true);
        Context.Defer();
        return default;
    }
}