using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Tweaks;

public class PreventLeaveGroupTweak : IAsyncEventListener<PlayerLeaveGroupRequested>
{
    public const string LeaveGroupPermissionName = "warfare::features.leavegroup";
    public static readonly PermissionLeaf LeaveGroupPermission = new(LeaveGroupPermissionName);
    public async UniTask HandleEventAsync(PlayerLeaveGroupRequested e, IServiceProvider serviceProvider,
        CancellationToken token = default)
    {
        UserPermissionStore? permissionStore = serviceProvider.GetService<UserPermissionStore>();
        if (permissionStore == null)
            return;
        
        if (await permissionStore.HasPermissionAsync(e.Player, LeaveGroupPermission, token))
            return;
        
        ChatService chatService = serviceProvider.GetRequiredService<ChatService>();
        PlayersTranslations translations = serviceProvider.GetRequiredService<TranslationInjection<PlayersTranslations>>().Value;
        
        chatService.Send(e.Player, translations.NoLeavingGroup);
        e.Cancel();
    }
}