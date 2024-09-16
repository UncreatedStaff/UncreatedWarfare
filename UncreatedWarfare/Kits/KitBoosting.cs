using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.Saves;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Kits;
public class KitBoosting(KitManager manager, IServiceProvider serviceProvider)
{
    private readonly ILogger<KitBoosting> _logger = serviceProvider.GetRequiredService<ILogger<KitBoosting>>();
    private readonly IPlayerService _playerService = serviceProvider.GetRequiredService<IPlayerService>();
    public KitManager Manager { get; } = manager;

    public bool IsNitroBoostingQuick(ulong player)
    {
        GameThread.AssertCurrent();

        WarfarePlayer? onlinePlayer = _playerService.GetOnlinePlayerOrNull(player);
        if (onlinePlayer != null)
            return onlinePlayer.Save.WasNitroBoosting;

        BinaryPlayerSave save = new BinaryPlayerSave(new CSteamID(player), _logger);
        save.Load();
        return save.WasNitroBoosting;
    }

    /// <exception cref="TimeoutException"/>
    public async Task<bool?> IsNitroBoosting(ulong player, CancellationToken token = default)
    {
        return null;
        //bool?[]? state = await IsNitroBoosting([ player ], token).ConfigureAwait(false);
        //return state == null || state.Length < 1 ? null : state[0];
    }
    /// <exception cref="TimeoutException"/>
    public async Task<bool?[]?> IsNitroBoosting(ulong[] players, CancellationToken token = default)
    {
        return new bool?[players.Length];
        //if (!UCWarfare.CanUseNetCall)
        //    return null;
        //bool?[] rtn = new bool?[players.Length];

        //RequestResponse response = await KitEx.NetCalls.RequestIsNitroBoosting
        //    .Request(KitEx.NetCalls.RespondIsNitroBoosting, UCWarfare.I.NetClient!, players, 8192);
        //if (!response.TryGetParameter(0, out byte[] state))
        //    throw new TimeoutException("Timed out while checking nitro status.");

        //int len = Math.Min(state.Length, rtn.Length);
        //await UniTask.SwitchToMainThread(token);
        //for (int i = 0; i < len; ++i)
        //{
        //    byte b = state[i];
        //    rtn[i] = b switch { 0 => false, 1 => true, _ => null };
        //    if (b is not 0 and not 1)
        //        continue;

        //    if (!PlayerSave.TryReadSaveFile(players[i], out PlayerSave save))
        //    {
        //        if (b != 1)
        //            continue;

        //        save = new PlayerSave(players[i]);
        //    }
        //    else if (save.WasNitroBoosting == (b == 1))
        //        continue;

        //    save.WasNitroBoosting = b == 1;
        //    PlayerSave.WriteToSaveFile(save);
        //}

        //return rtn;
    }
    //internal void OnNitroBoostingUpdated(ulong player, byte state)
    //{
    //    GameThread.AssertCurrent();
    //    if (state is 0 or 1)
    //    {
    //        if (!PlayerSave.TryReadSaveFile(player, out PlayerSave save))
    //        {
    //            if (state != 1)
    //                return;
    //            save = new PlayerSave(player);
    //        }
    //        else if (save.WasNitroBoosting == (state == 1))
    //            return;
    //        save.WasNitroBoosting = state == 1;
    //        PlayerSave.WriteToSaveFile(save);
    //        if (UCPlayer.FromID(player) is { } pl)
    //        {
    //            pl.SendChat(state == 1 ? T.StartedNitroBoosting : T.StoppedNitroBoosting);
    //            Manager.Cache.OnNitroUpdated(pl, state);
    //        }
    //    }

    //    string stateStr = state switch { 0 => "Not Boosting", 1 => "Boosting", _ => "Unknown" };
    //    ActionLog.Add(ActionLogType.NitroBoostStateUpdated, "State: \"" + stateStr + "\".", player);
    //    L.Log("Player {" + player + "} nitro boost status updated: \"" + stateStr + "\".", ConsoleColor.Magenta);
    //}
    //async Task ITCPConnectedListener.OnConnected(CancellationToken token)
    //{
    //    if (PlayerManager.OnlinePlayers.Count < 1)
    //        return;

    //    int v = Interlocked.Increment(ref _v) - 1;

    //    await UniTask.SwitchToMainThread(token);
    //    CheckLoaded();

    //    ulong[] players = new ulong[PlayerManager.OnlinePlayers.Count];

    //    for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
    //        players[i] = PlayerManager.OnlinePlayers[i].Steam64;

    //    RequestResponse response = await KitEx.NetCalls.RequestIsNitroBoosting.Request(KitEx.NetCalls.RespondIsNitroBoosting,
    //        UCWarfare.I.NetClient!, players, 8192);

    //    CheckLoaded();
    //    if (response.Responded && response.TryGetParameter(0, out byte[] bytes))
    //    {
    //        await UniTask.SwitchToMainThread(token);
    //        int len = Math.Min(bytes.Length, players.Length);
    //        for (int i = 0; i < len; ++i)
    //            OnNitroBoostingUpdated(players[i], bytes[i]);
    //    }

    //    void CheckLoaded()
    //    {
    //        if (v != _v || !Manager.IsLoaded || !UCWarfare.CanUseNetCall)
    //            throw new OperationCanceledException();
    //    }
    //}
}
