using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Buildables;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Tweaks;

public class AutoSaveMainStructuresTweak : IAsyncEventListener<IBuildablePlacedEvent>
{
    private readonly BuildableSaver _buildableSaver;
    private readonly ZoneStore _zoneStore;
    private readonly ChatService _chatService;
    private readonly StructureTranslations _translations;

    public AutoSaveMainStructuresTweak(BuildableSaver buildableSaver, ZoneStore zoneStore, ChatService chatService, TranslationInjection<StructureTranslations> translations)
    {
        _buildableSaver = buildableSaver;
        _zoneStore = zoneStore;
        _chatService = chatService;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask HandleEventAsync(IBuildablePlacedEvent e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        if (!e.OwnerId.IsIndividual())
        {
            return;
        }

        if (!_zoneStore.IsInsideZone(e.Buildable.Position, ZoneType.MainBase, null))
        {
            return;
        }

        await _buildableSaver.SaveBuildableAsync(e.Buildable, token);

        if (e.Owner != null)
            _chatService.Send(e.Owner, _translations.StructureSaved, e.Buildable.Asset);
    }
}