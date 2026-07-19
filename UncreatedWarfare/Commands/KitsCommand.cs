using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits.UI;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Commands;

[Command("kits", "k", "kist", "ksit"), MetadataFile]
internal sealed class KitsCommand : IExecutableCommand
{
    private readonly ZoneStore _zoneStore;
    private readonly KitSelectionUI _kitUi;
    private readonly KitsCommandTranslations _translations;

    public required CommandContext Context { get; init; }

    public KitsCommand(
        TranslationInjection<KitsCommandTranslations> translations,
        ZoneStore zoneStore,
        KitSelectionUI kitUi)
    {
        _zoneStore = zoneStore;
        _kitUi = kitUi;
        _translations = translations.Value;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        Team team = Context.Player.Team;
        bool isInWarRoom = _zoneStore.IsInWarRoom(Context.Player, team.Faction);
        bool isInMainBase = isInWarRoom || _zoneStore.IsInMainBase(Context.Player, team.Faction);

        if (!isInWarRoom && !isInMainBase)
            throw Context.Reply(_translations.NotInMainOrWarRoom);

        await _kitUi.OpenAsync(Context.Player, token);
        Context.Defer();
    }
}

public class KitsCommandTranslations : TranslationCollection
{
    public override string Name => "Commands/Kits";

    [TranslationData("Sent to a player if they try to use /kits or interact with the Kit Armory while not in main or in the war room.")]
    public readonly Translation NotInMainOrWarRoom = new Translation(
        "<#c2c2c2>You must be in the main base or war room to use this command. " +
        "You can also interact with a <#e26a5d>Large Ammo Crate</color> to restock or change your kit."
    );
}