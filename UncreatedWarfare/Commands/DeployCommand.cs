using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Revives;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Commands;

[Command("deploy", "dep", "warp", "warps", "tpa", "go", "goto", "fob", "deployfob", "df", "dp")]
[MetadataFile(nameof(GetHelpMetadata))]
public class DeployCommand : IExecutableCommand
{
    private const string Syntax = "/deploy main -OR- /deploy <fob name>";
    private const string Help = "Deploy to a point of interest such as a main base, FOB, VCP, or cache.";

    private readonly ReviveManagerOld? _reviveManager;
    private readonly ZoneStore _globalZoneStore;
    private readonly DeploymentTranslations _translations;
    private readonly DeploymentService _deploymentService;

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    public DeployCommand(IServiceProvider serviceProvider)
    {
        _reviveManager = serviceProvider.GetService<ReviveManagerOld>(); // optional
        _globalZoneStore = serviceProvider.GetRequiredService<ZoneStore>();
        _deploymentService = serviceProvider.GetRequiredService<DeploymentService>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<DeploymentTranslations>>().Value;
    }

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

        if (Context.MatchParameter(0, "cancel", "stop"))
        {
            DeploymentComponent comp = Context.Player.Component<DeploymentComponent>();

            if (comp.CurrentDeployment == null)
                throw Context.Reply(_translations.DeployCancelNotDeploying);

            comp.CancelDeployment(false);
            throw Context.Reply(_translations.DeployCancelled);
        }

        if (_reviveManager != null && _reviveManager.IsInjured(Context.CallerId.m_SteamID))
            throw Context.Reply(_translations.DeployInjured);

        string input = Context.GetRange(0)!;

        IDeployable? destination = null;

        DeploySettings deploySettings = default;

        if (!FOBManager.Loaded || !FOBManager.TryFindFOB(input, Context.Player.Team, out destination))
        {
            if (input.Equals("lobby", StringComparison.InvariantCultureIgnoreCase))
                throw Context.Reply(_translations.DeployLobbyRemoved);

            if (input.Equals("main", StringComparison.InvariantCultureIgnoreCase) ||
                input.Equals("base", StringComparison.InvariantCultureIgnoreCase) ||
                input.Equals("home", StringComparison.InvariantCultureIgnoreCase) ||
                input.Equals("mainbase", StringComparison.InvariantCultureIgnoreCase) ||
                input.Equals("main base", StringComparison.InvariantCultureIgnoreCase) ||
                input.Equals("homebase", StringComparison.InvariantCultureIgnoreCase) ||
                input.Equals("home base", StringComparison.InvariantCultureIgnoreCase))
            {
                destination = _globalZoneStore.SearchZone(ZoneType.MainBase, Context.Player.Team.Faction);
            }
        }

        if (destination == null)
            throw Context.Reply(_translations.DeployableNotFound, input);

        if (_globalZoneStore.IsInMainBase(Context.Player))
        {
            deploySettings.AllowMovement = true;
            deploySettings.AllowNearbyEnemies = true;
        }

        _deploymentService.TryStartDeployment(Context.Player, destination, deploySettings);
        Context.Defer();
        return default;
    }
}