using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Items;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Kits.Tweaks;

/// <summary>
/// Prevents removing clothing that has storage to make item tracking much easier
/// </summary>
internal sealed class KitNoSwapStorageClothingTweak : IAsyncEventListener<SwapClothingRequested>
{
    private static readonly PermissionLeaf PermissionSwapClothing = new PermissionLeaf("warfare::features.swap_clothing");

    private readonly UserPermissionStore _userPermissionStore;
    private readonly PlayersTranslations _translations;
    private readonly ChatService _chatService;

    public KitNoSwapStorageClothingTweak(UserPermissionStore userPermissionStore, TranslationInjection<PlayersTranslations> translations, ChatService chatService)
    {
        _userPermissionStore = userPermissionStore;
        _translations = translations.Value;
        _chatService = chatService;
    }

    async UniTask IAsyncEventListener<SwapClothingRequested>.HandleEventAsync(SwapClothingRequested e, IServiceProvider serviceProvider, CancellationToken token)
    {
        if (await _userPermissionStore.HasPermissionAsync(e.Player, PermissionSwapClothing, token).ConfigureAwait(false))
            return;

        await UniTask.SwitchToMainThread(token);

        if (e.CurrentClothing == null
            || !e.Player.Component<KitPlayerComponent>().HasKit
            || e.Type is not ClothingType.Backpack and not ClothingType.Pants and not ClothingType.Shirt and not ClothingType.Vest)
        {
            return;
        }

        _chatService.Send(e.Player, _translations.NoRemovingClothing);
        e.Cancel();
    }
}