using System;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Objects;
using Uncreated.Warfare.Events.Models.Zones;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Kits.UI;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Tweaks;

internal sealed class KitArmoryTweaks :
    IAsyncEventListener<QuestObjectInteracted>,
    IAsyncEventListener<PlayerExitedZone>
{
    private readonly AssetConfiguration _assetConfig;
    private readonly ZoneStore _zoneStore;
    private readonly ChatService _chatService;
    private readonly KitSelectionUI _kitUi;
    private readonly KitsCommandTranslations _translations;

    public KitArmoryTweaks(
        AssetConfiguration assetConfig,
        ZoneStore zoneStore,
        ChatService chatService,
        TranslationInjection<KitsCommandTranslations> translations,
        KitSelectionUI kitUi
    )
    {
        _assetConfig = assetConfig;
        _zoneStore = zoneStore;
        _chatService = chatService;
        _kitUi = kitUi;
        _translations = translations.Value;
    }

    private async UniTask HandleArmoryInteracted(WarfarePlayer player, CancellationToken token = default)
    {
        Team team = player.Team;
        bool isInWarRoom = _zoneStore.IsInWarRoom(player, team.Faction);
        bool isInMainBase = isInWarRoom || _zoneStore.IsInMainBase(player, team.Faction);

        if (!isInWarRoom && !isInMainBase)
        {
            _chatService.Send(player, _translations.NotInMainOrWarRoom);
            return;
        }

        await _kitUi.OpenAsync(player, token);
    }

    UniTask IAsyncEventListener<QuestObjectInteracted>.HandleEventAsync(QuestObjectInteracted e, IServiceProvider serviceProvider, CancellationToken token)
    {
        if (!_assetConfig.GetAssetLink<ObjectAsset>("Objects:KitArmory").MatchAsset(e.Interactable.objectAsset))
        {
            return UniTask.CompletedTask;
        }

        return HandleArmoryInteracted(e.Player, token);
    }

    async UniTask IAsyncEventListener<PlayerExitedZone>.HandleEventAsync(PlayerExitedZone e, IServiceProvider serviceProvider, CancellationToken token)
    {
        if (e.Zone.Type is not ZoneType.MainBase and not ZoneType.WarRoom)
            return;
        
        if (_zoneStore.IsInMainBase(e.Player))
            return;

        await _kitUi.CloseAsync(e.Player, token: token);
    }
}