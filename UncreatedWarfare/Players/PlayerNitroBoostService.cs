using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.Saves;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Players;

/// <summary>
/// Allows checking if players are Server Boosting in Discord.
/// </summary>
[GenerateRpcSource]
public partial class PlayerNitroBoostService : IEventListener<PlayerJoined>
{
    private readonly ILogger<PlayerNitroBoostService> _logger;
    private readonly IPlayerService? _playerService;
    private readonly IUserDataService? _userDataService;
    private readonly KitSignService? _kitSigns;
    private readonly WarfareModule? _module;
    private readonly ChatService? _chatService;
    private readonly PlayersTranslations? _translations;

    /// <summary>
    /// Invoked on the server when a player's status is updated, whether or not they're online.
    /// </summary>
    public event Action<WarfarePlayer?, CSteamID, bool>? OnNitroBoostStatusUpdated;

    public PlayerNitroBoostService(IServiceProvider serviceProvider, ILogger<PlayerNitroBoostService> logger)
    {
        _logger = logger;
        if (!WarfareModule.IsActive)
            return;
        
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
        _userDataService = serviceProvider.GetRequiredService<IUserDataService>();
        _kitSigns = serviceProvider.GetRequiredService<KitSignService>();
        _module = serviceProvider.GetRequiredService<WarfareModule>();
        _chatService = serviceProvider.GetRequiredService<ChatService>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<PlayersTranslations>>()?.Value;
    }

    /// <summary>
    /// Check if a player is nitro boosting, returning the last known value without contacting the bot.
    /// </summary>
    /// <exception cref="NotSupportedException">Not ran on warfare.</exception>
    public bool? IsBoostingQuick(CSteamID steam64)
    {
        GameThread.AssertCurrent();

        if (!WarfareModule.IsActive)
            throw new NotSupportedException("Expected only on Warfare.");

        if (_playerService!.GetOnlinePlayerOrNullThreadSafe(steam64) is { } pl)
            return pl.Save.WasNitroBoosting;

        BinaryPlayerSave save = new BinaryPlayerSave(steam64, _logger);
        save.Load();
        return save is { WasReadFromFile: true, WasNitroBoosting: true };
    }

    /// <summary>
    /// Check if a player is nitro boosting. Defaults to the last known value if the bot can't be contacted.
    /// </summary>
    /// <param name="forceRecheck">Check again with the bot if the player's already online.</param>
    /// <exception cref="NotSupportedException">Not ran on warfare.</exception>
    public async ValueTask<bool> IsBoosting(CSteamID steam64, bool forceRecheck = false, CancellationToken token = default)
    {
        if (!WarfareModule.IsActive)
            throw new NotSupportedException("Expected only on Warfare.");

        if (!forceRecheck)
        {
            if (_playerService!.GetOnlinePlayerOrNullThreadSafe(steam64) is { } pl)
                return pl.Save.WasNitroBoosting;
        }

        try
        {
            ulong discordId = await _userDataService!.GetDiscordIdAsync(steam64.m_SteamID, token).ConfigureAwait(false);
            if (discordId == 0)
            {
                ReceiveNitroBoostStatusUpdate(steam64, false);
                return false;
            }

            bool? isNitroBoosting = await SendCheckNitroBoostStatus(discordId);
            if (isNitroBoosting.HasValue)
                ReceiveNitroBoostStatusUpdate(steam64, isNitroBoosting.Value);

            return isNitroBoosting.GetValueOrDefault();
        }
        catch (RpcNoConnectionsException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for nitro boosting.");
        }

        await UniTask.SwitchToMainThread(token);
        BinaryPlayerSave save = new BinaryPlayerSave(steam64, _logger);

        save.Load();
        return save.WasNitroBoosting;
    }

    /// <summary>
    /// Sends an update to the server about the player's nitro boost status.
    /// </summary>
    /// <remarks>This should be used from the discord bot, not the plugin.</remarks>
    [RpcSend(nameof(ReceiveNitroBoostStatusUpdate)), RpcFireAndForget]
    public partial void SendNitroBoostStatusUpdate(CSteamID steam64, bool isNitroBoosting);

    /// <summary>
    /// Requests the discord bot to check if a user is nitro boosting.
    /// </summary>
    [RpcSend(nameof(CheckNitroBoostStatusRpc)), RpcTimeout(3 * Timeouts.Seconds)]
    protected partial RpcTask<bool?> SendCheckNitroBoostStatus(ulong discordId);

    [RpcReceive]
    private Task<bool?> CheckNitroBoostStatusRpc(ulong discordId)
    {
        return CheckNitroBoostStatus(discordId);
    }

    protected virtual Task<bool?> CheckNitroBoostStatus(ulong discordId)
    {
        return Task.FromResult<bool?>(null);
    }

    [RpcReceive]
    protected void ReceiveNitroBoostStatusUpdate(CSteamID steam64, bool isNitroBoosting)
    {
        if (!WarfareModule.IsActive)
            return;

        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();

            if (_playerService!.GetOnlinePlayerOrNull(steam64) is { } pl)
            {
                bool isUpdate = pl.Save.WasNitroBoosting != isNitroBoosting;
                _logger.LogDebug($"Nitro boost status updated for {steam64}: {(pl.Save.WasNitroBoosting ? "Boosting" : "Not Boosting")} -> {(isNitroBoosting ? "Boosting" : "Not Boosting")}.");
                pl.Save.WasNitroBoosting = isNitroBoosting;

                pl.Save.Save();

                if (!isUpdate)
                    return;

                if (_translations != null)
                {
                    _chatService?.Send(pl, isNitroBoosting ? _translations.StartedNitroBoosting : _translations.StoppedNitroBoosting);
                }

                _kitSigns?.UpdateSigns(pl);
                if (!isNitroBoosting && _module != null && _module.IsLayoutActive() && pl.Component<KitPlayerComponent>().CachedKit is { RequiresServerBoost: true })
                {
                    await _module.ScopedProvider.Resolve<KitRequestService>().GiveAvailableFreeKitAsync(pl).ConfigureAwait(false);
                }

                try
                {
                    OnNitroBoostStatusUpdated?.Invoke(pl, steam64, isNitroBoosting);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error invoking OnNitroBoostStatusUpdated.");
                }
                return;
            }
            
            BinaryPlayerSave save = new BinaryPlayerSave(steam64, _logger);
            
            save.Load();
            _logger.LogDebug($"Nitro boost status updated for {steam64}: {(save.WasNitroBoosting ? "Boosting" : "Not Boosting")} -> {(isNitroBoosting ? "Boosting" : "Not Boosting")}.");
            if (save.WasNitroBoosting == isNitroBoosting)
                return;

            save.WasNitroBoosting = isNitroBoosting;
            save.Save();

            try
            {
                OnNitroBoostStatusUpdated?.Invoke(null, steam64, isNitroBoosting);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking OnNitroBoostStatusUpdated.");
            }
        });
    }

    void IEventListener<PlayerJoined>.HandleEvent(PlayerJoined e, IServiceProvider serviceProvider)
    {
        Task.Run(async () =>
        {
            try
            {
                ulong discordId = await _userDataService!.GetDiscordIdAsync(e.Steam64.m_SteamID, e.Player.DisconnectToken).ConfigureAwait(false);
                if (discordId == 0)
                {
                    ReceiveNitroBoostStatusUpdate(e.Steam64, false);
                    return;
                }

                bool? isNitroBoosting = await SendCheckNitroBoostStatus(discordId).IgnoreNoConnections();
                _logger.LogDebug($"Received nitro boost status: {isNitroBoosting}.");
                if (isNitroBoosting.HasValue)
                    ReceiveNitroBoostStatusUpdate(e.Steam64, isNitroBoosting.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for nitro boost on join.");
            }
        });
    }
}
