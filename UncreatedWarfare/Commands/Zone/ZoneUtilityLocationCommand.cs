using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("location", "position", "loc", "pos"), SubCommandOf(typeof(ZoneUtilityCommand))]
internal sealed class ZoneUtilityLocationCommand : IExecutableCommand
{
    private readonly ZoneCommandTranslations _translations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public ZoneUtilityLocationCommand(TranslationInjection<ZoneCommandTranslations> translations)
    {
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        Vector3 p = Context.Player.Position;
        Context.Reply(_translations.ZoneUtilLocation, p.x, p.y, p.z, Context.Player.Yaw);

        return UniTask.CompletedTask;
    }
}