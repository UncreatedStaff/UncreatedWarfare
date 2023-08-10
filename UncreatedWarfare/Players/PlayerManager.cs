using Cysharp.Threading.Tasks;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Networking;
using Uncreated.Networking.Async;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Players;
using UnityEngine;
using SteamAPI = Uncreated.Warfare.Networking.SteamAPI;

namespace Uncreated.Warfare;

public static class PlayerManager
{
    
    // auto added on join and detroyed on leave
    public static readonly Type[] PlayerComponentTypes =
    {

    };

    public static readonly List<UCPlayer> OnlinePlayers;
    private static readonly Dictionary<ulong, UCPlayer> _dict;
    private static readonly Dictionary<ulong, UCSemaphore> _semaphores;
    internal static List<KeyValuePair<ulong, CancellationTokenSource>> PlayerConnectCancellationTokenSources = new List<KeyValuePair<ulong, CancellationTokenSource>>(Provider.queueSize);

    static PlayerManager()
    {
        OnlinePlayers = new List<UCPlayer>(50);
        _dict = new Dictionary<ulong, UCPlayer>(50);
        _semaphores = new Dictionary<ulong, UCSemaphore>(128);
        EventDispatcher.GroupChanged += OnGroupChagned;
        Provider.onRejectingPlayer += OnRejectingPlayer;
        EventDispatcher.PlayerPending += OnPlayerPending;
    }
    internal static List<UCSemaphore> GetAllSemaphores()
    {
        lock (_semaphores)
        {
            return _semaphores.Values.ToList();
        }
    }
    internal static void DeregisterPlayerSemaphore(ulong player)
    {
        lock (_semaphores)
        {
            if (FromID(player) == null && _semaphores.TryGetValue(player, out UCSemaphore semaphore))
            {
                _semaphores.Remove(player);
                semaphore.Dispose();
                L.LogDebug("Semaphore for [" + player + "] has been disposed of.");
            }
        }
    }
    private static void OnPlayerPending(PlayerPending e)
    {
        for (int i = PlayerConnectCancellationTokenSources.Count - 1; i >= 0; --i)
        {
            KeyValuePair<ulong, CancellationTokenSource> kvp = PlayerConnectCancellationTokenSources[i];
            if (kvp.Key == e.Steam64)
            {
                kvp.Value.Cancel();
                PlayerConnectCancellationTokenSources.RemoveAt(i);
            }
        }
        PlayerConnectCancellationTokenSources.Add(new KeyValuePair<ulong, CancellationTokenSource>(e.Steam64, new CancellationTokenSource()));
    }

    public static UCPlayer? FromID(ulong steam64)
    {
        lock (_dict)
        {
            return _dict.TryGetValue(steam64, out UCPlayer pl) ? pl : null;
        }
    }
    private static void OnRejectingPlayer(CSteamID steamid, ESteamRejection rejection, string explanation)
    {
        for (int i = PlayerConnectCancellationTokenSources.Count - 1; i >= 0; --i)
        {
            KeyValuePair<ulong, CancellationTokenSource> kvp = PlayerConnectCancellationTokenSources[i];
            if (kvp.Key == steamid.m_SteamID)
            {
                kvp.Value.Cancel();
                PlayerConnectCancellationTokenSources.RemoveAt(i);
            }
        }
    }
    public static bool HasSave(ulong playerID, out PlayerSave save) => PlayerSave.TryReadSaveFile(playerID, out save!);
    public static PlayerSave? GetSave(ulong playerID) => PlayerSave.TryReadSaveFile(playerID, out PlayerSave? save) ? save : null;
    public static void ApplyToOnline()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < OnlinePlayers.Count; i++)
        {
            ApplyTo(OnlinePlayers[i]);
        }
    }
    public static void ApplyTo(UCPlayer player)
    {
        ThreadUtil.assertIsGameThread();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        player.Save.Apply(player);
        PlayerSave.WriteToSaveFile(player.Save);
    }
    public static PlayerListEntry[] GetPlayerList()
    {
        PlayerListEntry[] rtn = new PlayerListEntry[OnlinePlayers.Count];
        for (int i = 0; i < OnlinePlayers.Count; i++)
        {
            rtn[i] = new PlayerListEntry
            {
                Duty = OnlinePlayers[i].OnDuty(),
                Steam64 = OnlinePlayers[i].Steam64,
                Name = OnlinePlayers[i].Name.CharacterName,
                Team = OnlinePlayers[i].Player.GetTeamByte()
            };
        }
        return rtn;
    }
    public static void AddSave(PlayerSave save) => PlayerSave.WriteToSaveFile(save);
    public static async UniTask TryDownloadAllPlayerSummaries(bool allowCache = true, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        List<ulong> players = new List<ulong>(OnlinePlayers.Count);
        for (int i = 0; i < OnlinePlayers.Count; ++i)
        {
            if (!allowCache || OnlinePlayers[i].CachedSteamProfile == null)
                players.Add(OnlinePlayers[i].Steam64);
        }

        PlayerSummary[] summaries = await SteamAPI.GetPlayerSummaries(players.ToArrayFast(), token);
        for (int j = 0; j < summaries.Length; ++j)
        {
            PlayerSummary summary = summaries[j];
            UCPlayer? player = FromID(summary.Steam64);
            if (player != null)
                player.CachedSteamProfile = summary;
        }
    }
    internal static UCPlayer InvokePlayerConnected(Player player, PendingAsyncData asyncData, out bool newPlayer)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong s64 = player.channel.owner.playerID.steamID.m_SteamID;
        if (!PlayerSave.TryReadSaveFile(s64, out PlayerSave? save) || save == null)
        {
            newPlayer = true;
            save = new PlayerSave(s64);
            PlayerSave.WriteToSaveFile(save);
        }
        else newPlayer = false;

        CancellationTokenSource? src = null;
        for (int i = 0; i < PlayerConnectCancellationTokenSources.Count; ++i)
        {
            KeyValuePair<ulong, CancellationTokenSource> kvp = PlayerConnectCancellationTokenSources[i];
            if (kvp.Key == s64)
            {
                PlayerConnectCancellationTokenSources.RemoveAt(i);
                src = kvp.Value;
                break;
            }
        }

        Component[] compBuffer = new Component[PlayerComponentTypes.Length];
        for (int i = 0; i < PlayerComponentTypes.Length; ++i)
        {
            compBuffer[i] = player.gameObject.AddComponent(PlayerComponentTypes[i]);
        }

        UCPlayer ucplayer;
        lock (_semaphores)
        {
            if (!_semaphores.TryGetValue(player.channel.owner.playerID.steamID.m_SteamID, out UCSemaphore semaphore))
            {
                semaphore = new UCSemaphore();
#if DEBUG
                semaphore.WaitCallback += () => L.LogDebug("Waiting for " + player.channel.owner.playerID.steamID.m_SteamID + " purchase sync."); 
                semaphore.ReleaseCallback += amt => L.LogDebug("Released " + amt + " from " + player.channel.owner.playerID.steamID.m_SteamID + " purchase sync.");
#endif
                L.LogDebug("Semaphore for [" + player + "] has been created.");
                _semaphores.Add(player.channel.owner.playerID.steamID.m_SteamID, semaphore);
            }
            else
            {
                L.LogDebug("Existing semaphore found for [" + player + "].");
            }
            ucplayer = new UCPlayer(
                player.channel.owner.playerID.steamID,
                player,
                player.channel.owner.playerID.characterName,
                player.channel.owner.playerID.nickName,
                save.IsOtherDonator,
                src ?? new CancellationTokenSource(),
                save,
                semaphore,
                asyncData
            );

            Data.OriginalPlayerNames.Remove(ucplayer.Steam64);
            
            OnlinePlayers.Add(ucplayer);
            lock (_dict)
            {
                _dict.Add(ucplayer.Steam64, ucplayer);
            }
        }

        for (int i = 0; i < compBuffer.Length; ++i)
        {
            if (compBuffer[i] is IPlayerComponent pc)
            {
                pc.Player = ucplayer;
                try
                {
                    pc.Init();
                }
                catch (Exception ex)
                {
                    L.LogError(ex, method: pc.GetType().Name.ToUpperInvariant() + ".INIT");
                }
            }
        }
        return ucplayer;
    }
    private static void OnGroupChagned(GroupChanged e)
    {
        ApplyTo(e.Player);
        NetCalls.SendTeamChanged.NetInvoke(e.Steam64, e.NewGroup.GetTeamByte());
        if (e.Player.Player.TryGetComponent(out SpottedComponent spot))
            spot.OwnerTeam = e.NewTeam;
    }
    internal static void InvokePlayerDisconnected(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        player.SetOffline();
        for (int i = 0; i < PlayerComponentTypes.Length; ++i)
        {
            if (player.Player.TryGetComponent(PlayerComponentTypes[i], out Component comp))
                UnityEngine.Object.Destroy(comp);
        }
        OnlinePlayers.RemoveAll(s => s == default || s.Steam64 == player.Steam64);
        lock (_dict)
        {
            _dict.Remove(player.Steam64);
        }
        player.Player = null!;
    }
    public static IEnumerable<UCPlayer> GetNearbyPlayers(float range, Vector3 point)
    {
        float sqrRange = range * range;
        for (int i = 0; i < OnlinePlayers.Count; i++)
        {
            UCPlayer current = OnlinePlayers[i];
            if (!current.Player.life.isDead && (current.Position - point).sqrMagnitude < sqrRange)
                yield return current;
        }
    }
    public static bool IsPlayerNearby(ulong playerID, float range, Vector3 point)
    {
        float sqrRange = range * range;
        for (int i = 0; i < OnlinePlayers.Count; i++)
        {
            UCPlayer current = OnlinePlayers[i];
            if (current.Steam64 == playerID && !current.Player.life.isDead && (current.Position - point).sqrMagnitude < sqrRange)
                return true;
        }
        return false;
    }
    public static bool IsPlayerNearby(UCPlayer player, float range, Vector3 point)
    {
        float sqrRange = range * range;
        return !player.Player.life.isDead && (player.Position - point).sqrMagnitude < sqrRange;
    }
    internal static async Task<bool?> IsUserInDiscordServer(ulong discordId)
    {
        if (!UCWarfare.CanUseNetCall)
            return null;
        RequestResponse response = await NetCalls.CheckUserInDiscordServerRequest.RequestAck(UCWarfare.I.NetClient!, discordId);
        if (response.Responded)
            return response.ErrorCode.HasValue && (StandardErrorCode)response.ErrorCode.Value == StandardErrorCode.Success;
        return null;
    }
    public static class NetCalls
    {
        public static readonly NetCall<ulong, bool> SendSetQueueSkip = new NetCall<ulong, bool>(ReceiveSetQueueSkip);
        public static readonly NetCall<ulong> GetPermissionsRequest = new NetCall<ulong>(ReceivePermissionRequest);
        public static readonly NetCall<ulong> CheckPlayerOnlineStatusRequest = new NetCall<ulong>(ReceivePlayerOnlineCheckRequest);
        /// <summary>Ack: Success = yes, other = no.</summary>
        public static readonly NetCall<ulong> CheckUserInDiscordServerRequest = new NetCall<ulong>(1022);

        public static readonly NetCall<ulong, ulong, TicketType, string> RequestOpenTicket = new NetCall<ulong, ulong, TicketType, string>(1037);

        public static readonly NetCallRaw<PlayerListEntry[]> SendPlayerList = new NetCallRaw<PlayerListEntry[]>(1000, PlayerListEntry.ReadArray, PlayerListEntry.WriteArray);
        public static readonly NetCallRaw<PlayerListEntry> SendPlayerJoined = new NetCallRaw<PlayerListEntry>(1016, PlayerListEntry.Read, PlayerListEntry.Write);
        public static readonly NetCall<ulong> SendPlayerLeft = new NetCall<ulong>(1017);
        public static readonly NetCall<ulong, bool> SendDutyChanged = new NetCall<ulong, bool>(1018);
        public static readonly NetCall<ulong, byte> SendTeamChanged = new NetCall<ulong, byte>(1019);
        public static readonly NetCall<ulong, bool> SendPlayerOnlineStatus = new NetCall<ulong, bool>(1036);
        public static readonly NetCall<ulong, EAdminType> SendPermissions = new NetCall<ulong, EAdminType>(1034);

        [NetCall(ENetCall.FROM_SERVER, 1024)]
        internal static async Task ReceiveSetQueueSkip(MessageContext context, ulong player, bool status)
        {
            await UCWarfare.ToUpdate();
            if (PlayerSave.TryReadSaveFile(player, out PlayerSave save))
            {
                save.HasQueueSkip = status;
                PlayerSave.WriteToSaveFile(save);
            }
            else if (status)
            {
                save = new PlayerSave(player) { HasQueueSkip = status };
                PlayerSave.WriteToSaveFile(save);
            }
        }
        [NetCall(ENetCall.FROM_SERVER, 1033)]
        internal static void ReceivePermissionRequest(MessageContext context, ulong target)
        {
            context.Reply(SendPermissions, target, PermissionSaver.Instance.GetPlayerPermissionLevel(target));
        }
        [NetCall(ENetCall.FROM_SERVER, 1035)]
        internal static void ReceivePlayerOnlineCheckRequest(MessageContext context, ulong target)
        {
            context.Reply(SendPlayerOnlineStatus, target, PlayerTool.getSteamPlayer(target) is not null);
        }
    }
}

internal interface IPlayerComponent
{
    public UCPlayer Player { get; set; }
    public void Init();
}
