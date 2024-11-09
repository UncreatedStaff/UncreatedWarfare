using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Tweaks;

/// <summary>
/// Manages the player's ability to teleport to where they're looking by right-click punching.
/// </summary>
[PlayerComponent]
public class PlayerJumpComponent : IPlayerComponent, IAsyncEventListener<PlayerPunched>
{
    public static readonly PermissionLeaf AutoJumpPermission = new PermissionLeaf("warfare::commands.teleport");
    private ChatService _chatService;
    public bool IsActive { get; set; }
    public WarfarePlayer Player { get; private set; }
    public void Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        _chatService = serviceProvider.GetRequiredService<ChatService>();

        if (!isOnJoin)
            return;

        IsActive = false;
    }

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }

    async UniTask IAsyncEventListener<PlayerPunched>.HandleEventAsync(PlayerPunched e, IServiceProvider serviceProvider, CancellationToken token)
    {
        if (!IsActive || e.PunchType != EPlayerPunch.RIGHT)
            return;

        UserPermissionStore permissions = serviceProvider.GetRequiredService<UserPermissionStore>();
        if (!await permissions.HasPermissionAsync(e.Player, AutoJumpPermission, token))
            return;

        await UniTask.SwitchToMainThread(token);
        Jump();

        Vector3 castPt = Player.Position;
        TeleportCommandTranslations translations = serviceProvider.GetRequiredService<TranslationInjection<TeleportCommandTranslations>>().Value;
        _chatService.Send(Player, translations.TeleportSelfLocationSuccess, $"({castPt.x.ToString("0.##", Data.LocalLocale)}, {castPt.y.ToString("0.##", Data.LocalLocale)}, {castPt.z.ToString("0.##", Data.LocalLocale)})");
    }

    public void Jump()
    {
        Jump(true, -1f);
    }

    public void Jump(bool raycast, float distance)
    {
        GameThread.AssertCurrent();

        if (!Player.IsOnline)
            return;

        Vector3 castPt = default;
        Transform aim = Player.UnturnedPlayer.look.aim;
        if (raycast)
        {
            distance = 10f;
            raycast = Physics.Raycast(new Ray(aim.position, aim.forward), out RaycastHit hit,
                1024, RayMasks.BLOCK_COLLISION);
            if (raycast)
                castPt = hit.point;
        }
        if (!raycast)
            castPt = Player.Position + aim.forward * distance;

        int c = 0;
        while (!PlayerStance.hasStandingHeightClearanceAtPosition(castPt) && ++c < 12)
            castPt += new Vector3(0, 1f, 0);

        Player.UnturnedPlayer.teleportToLocationUnsafe(castPt, Player.Yaw);
    }
}