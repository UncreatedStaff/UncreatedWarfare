using System;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.Saves;

namespace Uncreated.Warfare.Interaction;

/// <summary>
/// Handles whether or not a player's image is replaced with a nerd emoji in chat.
/// </summary>
public class NerdService : IEventListener<PlayerChatRequested>
{
    private readonly IPlayerService _playerService;
    private readonly ILogger<NerdService> _logger;

    public NerdService(IPlayerService playerService, ILogger<NerdService> logger)
    {
        _playerService = playerService;
        _logger = logger;
    }

    /// <summary>
    /// Gets whether or not a player's image is replaced with a nerd emoji in chat.
    /// </summary>
    public async UniTask<bool> GetNerdnessAsync(CSteamID player, CancellationToken token = default)
    {
        WarfarePlayer? pl = _playerService.GetOnlinePlayerOrNullThreadSafe(player);

        if (pl != null)
            return pl.Save.IsNerd;

        await UniTask.SwitchToMainThread(token);

        BinaryPlayerSave save = new BinaryPlayerSave(player, _logger);
        save.Load();
        return save.IsNerd;
    }

    /// <summary>
    /// Sets whether or not a player's image is replaced with a nerd emoji in chat.
    /// </summary>
    public async UniTask<bool> SetNerdnessAsync(CSteamID player, CSteamID instigator, bool isNerd, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        WarfarePlayer? pl = _playerService.GetOnlinePlayerOrNull(player);

        if (pl != null)
        {
            if (pl.Save.IsNerd == isNerd)
                return false;

            pl.Save.IsNerd = isNerd;
            pl.Save.Save();
        }
        else
        {
            BinaryPlayerSave save = new BinaryPlayerSave(player, _logger);
            save.Load();

            if (save.IsNerd == isNerd)
                return false;

            save.IsNerd = isNerd;
            save.Save();
        }

        _logger.LogInformation(isNerd ? "{0} is now a nerd." : "{0} is no longer a nerd.", player);
        return true;
    }

    [EventListener(Priority = -1)]
    void IEventListener<PlayerChatRequested>.HandleEvent(PlayerChatRequested e, IServiceProvider serviceProvider)
    {
        if (e.Player.Save.IsNerd)
            e.IconUrlOverride = "https://i1.sndcdn.com/artworks-Q61q2IpGG3x0QvIQ-FRIyHw-t500x500.jpg";
    }
}