using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("deploy", "dep", "warp", "warps", "tpa", "go", "goto", "fob", "deployfob", "df", "dp"), MetadataFile]
internal sealed class DeployCommand : IExecutableCommand
{
    private readonly DeploymentTranslations _translations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public DeployCommand(TranslationInjection<DeploymentTranslations> translations)
    {
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        DeploymentComponent comp = Context.Player.Component<DeploymentComponent>();

        if (Context.MatchParameter(0, "lobby"))
        {
            throw Context.Reply(_translations.DeployLobbyRemoved);
        }

        if (Context.MatchParameter(0, "cancel", "stop", "c"))
        {
            if (comp.CurrentDeployment == null)
                throw Context.Reply(_translations.DeployCancelNotDeploying);

            comp.CancelDeployment(false);
            throw Context.Reply(_translations.DeployCancelled);
        }

        throw Context.SendHelp();
    }
}