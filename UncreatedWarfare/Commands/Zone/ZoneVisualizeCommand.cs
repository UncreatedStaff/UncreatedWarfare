using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Commands;

[Command("visualize", "vis"), SubCommandOf(typeof(ZoneCommand))]
internal sealed class ZoneVisualizeCommand : IExecutableCommand
{
    private readonly ZoneStore _zoneStore;
    private readonly ZoneCommandTranslations _translations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public ZoneVisualizeCommand(ZoneStore zoneStore, TranslationInjection<ZoneCommandTranslations> translations)
    {
        _zoneStore = zoneStore;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertArgs(1);
        Context.AssertRanByPlayer();

        Zone? zone = Context.TryGetRange(0, out string? zname)
            ? _zoneStore.SearchZone(zname)
            : _zoneStore.FindInsideZone(Context.Player.Position, false);

        if (zone == null)
        {
            throw Context.Reply(_translations.ZoneNoResults);
        }

        int particleCount = Context.Player.Component<ZoneVisualizerComponent>().SpawnPoints(zone);

        Context.Reply(_translations.ZoneVisualizeSuccess, particleCount, zone);
        return UniTask.CompletedTask;
    }
}