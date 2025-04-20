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

        if (Context.MatchParameter(0, "cancel", "stop"))
        {
            DeploymentComponent comp = Context.Player.Component<DeploymentComponent>();

            if (comp.CurrentDeployment == null)
                throw Context.Reply(_translations.DeployCancelNotDeploying);

            comp.CancelDeployment(false);
            throw Context.Reply(_translations.DeployCancelled);
        }

        throw Context.SendHelp();
    }
}