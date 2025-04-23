using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Commands;

[Command("remove", "delete"), SubCommandOf(typeof(StructureCommand))]
internal sealed class StructureRemoveCommand : IExecutableCommand
{
    private readonly BuildableAttributesDataStore _attributeStore;
    private readonly ZoneStore _zoneStore;
    private readonly StructureTranslations _translations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public StructureRemoveCommand(BuildableAttributesDataStore attributeStore, ZoneStore zoneStore, TranslationInjection<StructureTranslations> translations)
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
            if (!_attributeStore.UpdateAttributes(structure.instanceID, true).Remove(MainBaseBuildables.PermanentAttribute))
            {
                if (_attributeStore.HasAttribute(structure.instanceID, true, MainBaseBuildables.TransientAttribute)
                    || !_zoneStore.IsInsideZone(structure.GetServersideData().point, ZoneType.MainBase, null)
                    || !_zoneStore.IsInsideZone(structure.GetServersideData().point, ZoneType.WarRoom, null))
                {
                    throw Context.Reply(_translations.StructureAlreadyUnsaved, structure.asset);
                }

                _attributeStore
                    .UpdateAttributes(structure.instanceID, true)
                    .Add(MainBaseBuildables.TransientAttribute, null);
            }

            // todo: Context.LogAction(ActionLogType.UnsaveStructure, $"{structure.asset.itemName} / {structure.asset.id} / {structure.asset.GUID:N} " +
            //                                                  $"at {structure.GetServersideData().point} ({structure.instanceID})");
            Context.Reply(_translations.StructureUnsaved, structure.asset);
        }
        else if (Context.TryGetBarricadeTarget(out BarricadeDrop? barricade))
        {
            if (!_attributeStore.UpdateAttributes(barricade.instanceID, false).Remove(MainBaseBuildables.PermanentAttribute))
            {
                if (_attributeStore.HasAttribute(barricade.instanceID, false, MainBaseBuildables.TransientAttribute)
                    || !_zoneStore.IsInsideZone(barricade.GetServersideData().point, ZoneType.MainBase, null)
                    || !_zoneStore.IsInsideZone(barricade.GetServersideData().point, ZoneType.WarRoom, null))
                {
                    throw Context.Reply(_translations.StructureAlreadyUnsaved, barricade.asset);
                }

                _attributeStore
                    .UpdateAttributes(barricade.instanceID, false)
                    .Add(MainBaseBuildables.TransientAttribute, null);
            }

            // todo: Context.LogAction(ActionLogType.UnsaveStructure, $"{structure.asset.itemName} / {structure.asset.id} / {structure.asset.GUID:N} " +
            //                                                  $"at {structure.GetServersideData().point} ({structure.instanceID})");
            Context.Reply(_translations.StructureUnsaved, barricade.asset);
        }
        else throw Context.Reply(_translations.StructureNoTarget);

        return UniTask.CompletedTask;
    }
}