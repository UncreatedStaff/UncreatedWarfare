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

#nullable disable
    private ChatService _chatService;
    public bool IsActive { get; set; }
    public WarfarePlayer Player { get; private set; }
#nullable restore

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
        _chatService.Send(Player, translations.TeleportSelfLocationSuccess, $"({castPt.x.ToString("0.##", Player.Locale.CultureInfo)}, {castPt.y.ToString("0.##", Player.Locale.CultureInfo)}, {castPt.z.ToString("0.##", Player.Locale.CultureInfo)})");
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
        bool isDown = false;
        if (raycast)
        {
            distance = 10f;
            raycast = Physics.Raycast(new Ray(aim.position, aim.forward), out RaycastHit hit, 1024, RayMasks.BLOCK_COLLISION & ~RayMasks.CLIP, QueryTriggerInteraction.Ignore);
            if (raycast)
                castPt = hit.point;

            // basically is looking down, teleport down through a roof or floor
            if (raycast && Math.Abs(castPt.x - aim.position.x) < 3 && Math.Abs(castPt.z - aim.position.z) < 3 && castPt.y < aim.position.y)
            {
                isDown = true;
            }
        }

        if (!raycast)
            castPt = Player.Position + aim.forward * distance;

        if (isDown)
        {
            float terrainHeight = LevelGround.getHeight(castPt);
            for (float y = castPt.y - 1f; y > terrainHeight; --y)
            {
                if (!PlayerStance.hasStandingHeightClearanceAtPosition(castPt with { y = y }))
                    continue;

                castPt.y = y;
                Player.UnturnedPlayer.teleportToLocationUnsafe(castPt, Player.Yaw);
                break;
            }
        }

        int c = 0;
        while (!PlayerStance.hasStandingHeightClearanceAtPosition(castPt) && ++c < 12)
            castPt += new Vector3(0, 1f, 0);

        Player.UnturnedPlayer.teleportToLocationUnsafe(castPt, Player.Yaw);
    }
}