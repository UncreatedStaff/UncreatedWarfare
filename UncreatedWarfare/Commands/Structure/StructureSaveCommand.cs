using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Commands;

[Command("save"), SubCommandOf(typeof(StructureCommand))]
internal sealed class StructureSaveCommand : IExecutableCommand
{
    private readonly BuildableAttributesDataStore _attributeStore;
    private readonly ZoneStore _zoneStore;
    private readonly StructureTranslations _translations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public StructureSaveCommand(BuildableAttributesDataStore attributeStore, ZoneStore zoneStore, TranslationInjection<StructureTranslations> translations)
    {
        _attributeStore = attributeStore;
        _zoneStore = zoneStore;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (Context.TryGetStructureTarget(out StructureDrop? structure))
        {
            if (_attributeStore.HasAttribute(structure.instanceID, true, MainBaseBuildables.PermanentAttribute))
            {
                throw Context.Reply(_translations.StructureAlreadySaved, structure.asset);
            }

            if (!_attributeStore.HasAttribute(structure.instanceID, true, MainBaseBuildables.TransientAttribute)
                && (_zoneStore.IsInsideZone(structure.GetServersideData().point, ZoneType.MainBase, null)
                    || _zoneStore.IsInsideZone(structure.GetServersideData().point, ZoneType.WarRoom, null)))
            {
                throw Context.Reply(_translations.StructureAlreadySaved, structure.asset);
            }


            _attributeStore
                .UpdateAttributes(structure.instanceID, true)
                .Add(MainBaseBuildables.PermanentAttribute, null);

            Context.Reply(_translations.StructureSaved, structure.asset);

            // todo: Context.LogAction(ActionLogType.SaveStructure, $"{structure.asset.itemName} / {structure.asset.id} / {structure.asset.GUID:N} " +
            //                                                $"at {structure.GetServersideData().point:0:##} ({structure.instanceID})");
        }
        else if (Context.TryGetBarricadeTarget(out BarricadeDrop? barricade))
        {
            if (_attributeStore.HasAttribute(barricade.instanceID, false, MainBaseBuildables.PermanentAttribute))
            {
                throw Context.Reply(_translations.StructureAlreadySaved, barricade.asset);
            }

            if (!_attributeStore.HasAttribute(barricade.instanceID, false, MainBaseBuildables.TransientAttribute)
                && (_zoneStore.IsInsideZone(barricade.GetServersideData().point, ZoneType.MainBase, null)
                    || _zoneStore.IsInsideZone(barricade.GetServersideData().point, ZoneType.WarRoom, null)))
            {
                throw Context.Reply(_translations.StructureAlreadySaved, barricade.asset);
            }

            _attributeStore
                .UpdateAttributes(barricade.instanceID, false)
                .Add(MainBaseBuildables.PermanentAttribute, null);

            Context.Reply(_translations.StructureSaved, barricade.asset);

            // todo: Context.LogAction(ActionLogType.SaveStructure, $"{structure.asset.itemName} / {structure.asset.id} / {structure.asset.GUID:N} " +
            //                                                $"at {structure.GetServersideData().point:0:##} ({structure.instanceID})");
        }
        else throw Context.Reply(_translations.StructureNoTarget);

        return UniTask.CompletedTask;
    }
}