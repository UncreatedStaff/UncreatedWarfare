using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Commands;

[Command("deploy", "dep", "warp", "warps", "tpa", "go", "goto", "fob", "deployfob", "df", "dp"), MetadataFile]
internal sealed class DeployCommand : IExecutableCommand
{
    private readonly ZoneStore _globalZoneStore;
    private readonly DeploymentTranslations _translations;
    private readonly DeploymentService _deploymentService;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public DeployCommand(IServiceProvider serviceProvider)
    {
        _globalZoneStore = serviceProvider.GetRequiredService<ZoneStore>();
        _deploymentService = serviceProvider.GetRequiredService<DeploymentService>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<DeploymentTranslations>>().Value;
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        Context.AssertArgs(1);

        if (Context.MatchParameter(0, "cancel", "stop"))
        {
            DeploymentComponent comp = Context.Player.Component<DeploymentComponent>();

            if (comp.CurrentDeployment == null)
                throw Context.Reply(_translations.DeployCancelNotDeploying);

            comp.CancelDeployment(false);
            throw Context.Reply(_translations.DeployCancelled);
        }

        if (Context.Player.IsInjured())
        {
            throw Context.Reply(_translations.DeployInjured);
        }

        string input = Context.GetRange(0)!;

        IDeployable? destination = null;

        DeploySettings deploySettings = default;

        // todo if (false /*!FOBManager.Loaded || !FOBManager.TryFindFOB(input, Context.Player.Team, out destination) */)
        // todo {
        // todo     if (input.Equals("lobby", StringComparison.InvariantCultureIgnoreCase))
        // todo         throw Context.Reply(_translations.DeployLobbyRemoved);
        // todo 
        // todo     if (input.Equals("main", StringComparison.InvariantCultureIgnoreCase) ||
        // todo         input.Equals("base", StringComparison.InvariantCultureIgnoreCase) ||
        // todo         input.Equals("home", StringComparison.InvariantCultureIgnoreCase) ||
        // todo         input.Equals("mainbase", StringComparison.InvariantCultureIgnoreCase) ||
        // todo         input.Equals("main base", StringComparison.InvariantCultureIgnoreCase) ||
        // todo         input.Equals("homebase", StringComparison.InvariantCultureIgnoreCase) ||
        // todo         input.Equals("home base", StringComparison.InvariantCultureIgnoreCase))
        // todo     {
        // todo         destination = _globalZoneStore.SearchZone(ZoneType.MainBase, Context.Player.Team.Faction);
        // todo     }
        // todo }

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