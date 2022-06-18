using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Reflection;
using Uncreated.Framework;
using Uncreated.Networking;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare;

public static class PlayerManager
{
    public static readonly List<UCPlayer> OnlinePlayers;
    private static readonly Dictionary<ulong, UCPlayer> _dict;
    public static readonly Type Type = typeof(PlayerSave);
    private static readonly FieldInfo[] fields = Type.GetFields();

    static PlayerManager()
    {
        OnlinePlayers = new List<UCPlayer>(50);
        _dict = new Dictionary<ulong, UCPlayer>(50);
        EventDispatcher.OnGroupChanged += OnGroupChagned;
    }
    public static UCPlayer? FromID(ulong steam64) => _dict.TryGetValue(steam64, out UCPlayer pl) ? pl : null;
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
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!PlayerSave.TryReadSaveFile(player.Steam64, out PlayerSave? save) || save == null)
            save = new PlayerSave(player.Steam64);
        save.Team = player.GetTeam();
        save.KitName = player.KitName;
        save.SquadName = player.Squad?.Name ?? string.Empty;
        save.LastGame = Data.Gamemode.GameID;
        PlayerSave.WriteToSaveFile(save);
    }
    public static PlayerListEntry[] GetPlayerList()
    {
        PlayerListEntry[] rtn = new PlayerListEntry[OnlinePlayers.Count];
        for (int i = 0; i < OnlinePlayers!.Count; i++)
        {
            if (OnlinePlayers == null) continue;
            rtn[i] = new PlayerListEntry
            {
                Duty = OnlinePlayers[i].OnDuty(),
                Steam64 = OnlinePlayers[i].Steam64,
                Name = F.GetPlayerOriginalNames(OnlinePlayers[i]).CharacterName,
                Team = OnlinePlayers[i].Player.GetTeamByte()
            };
        }
        return rtn;
    }
    public static void InvokePlayerConnected(Player player) => OnPlayerConnected(player);
    public static void InvokePlayerDisconnected(UCPlayer player) => OnPlayerDisconnected(player);
    public static void AddSave(PlayerSave save) => PlayerSave.WriteToSaveFile(save);
    private static void OnPlayerConnected(Player player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!PlayerSave.TryReadSaveFile(player.channel.owner.playerID.steamID.m_SteamID, out PlayerSave? save) || save == null)
        {
            save = new PlayerSave(player.channel.owner.playerID.steamID.m_SteamID);
            PlayerSave.WriteToSaveFile(save);
        }
        UCPlayer ucplayer = new UCPlayer(
            player.channel.owner.playerID.steamID,
            save.KitName,
            player,
            player.channel.owner.playerID.characterName,
            player.channel.owner.playerID.nickName,
            save.IsOtherDonator
        );


        OnlinePlayers.Add(ucplayer);
        _dict.Add(ucplayer.Steam64, ucplayer);

        SquadManager.OnPlayerJoined(ucplayer, save.SquadName);
        FOBManager.SendFOBList(ucplayer);
    }
    private static void OnGroupChagned(GroupChanged e)
    {
        ApplyTo(e.Player);
        NetCalls.SendTeamChanged.NetInvoke(e.Steam64, F.GetTeamByte(e.NewGroup));
    }
    private static void OnPlayerDisconnected(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        player.IsOnline = false;

        OnlinePlayers.RemoveAll(s => s == default || s.Steam64 == player.Steam64);
        _dict.Remove(player.Steam64);
        SquadManager.OnPlayerDisconnected(player);
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
    internal static void PickGroupAfterJoin(UCPlayer ucplayer)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong oldGroup = ucplayer.Player.quests.groupID.m_SteamID;
        if (HasSave(ucplayer.Steam64, out PlayerSave save))
        {
            if (TeamManager.CanJoinTeam(save.Team) && ucplayer.Player.quests.groupID.m_SteamID != save.Team)
            {
                ucplayer.Player.quests.ServerAssignToGroup(new CSteamID(TeamManager.GetGroupID(save.Team)), EPlayerGroupRank.MEMBER, true);
            }
            else
            {
                ulong other = TeamManager.Other(save.Team);
                if (TeamManager.CanJoinTeam(other) && ucplayer.Player.quests.groupID.m_SteamID != other)
                {
                    ucplayer.Player.quests.ServerAssignToGroup(new CSteamID(TeamManager.GetGroupID(other)), EPlayerGroupRank.MEMBER, true);
                }
            }
        }
        if (oldGroup != ucplayer.Player.quests.groupID.m_SteamID)
        {
            ulong team = ucplayer.Player.quests.groupID.m_SteamID.GetTeam();
            if (team != oldGroup.GetTeam())
            {
                ucplayer.Player.teleportToLocation(ucplayer.Player.GetBaseSpawn(), team.GetBaseAngle());
            }
        }
        GroupManager.save();

    }
    public static ESetFieldResult SetProperty(PlayerSave obj, string property, string value)
    {
        if (obj is null) return ESetFieldResult.OBJECT_NOT_FOUND;
        if (property is null || value is null) return ESetFieldResult.FIELD_NOT_FOUND;
        FieldInfo? field = GetField(property, out ESetFieldResult reason);
        if (field is not null && reason == ESetFieldResult.SUCCESS)
        {
            if (F.TryParseAny(value, field.FieldType, out object val) && val != null && field.FieldType.IsAssignableFrom(val.GetType()))
            {
                try
                {
                    field.SetValue(obj, val);
                }
                catch (Exception ex)
                {
                    L.LogError(ex);
                    return ESetFieldResult.FIELD_NOT_SERIALIZABLE;
                }
                return ESetFieldResult.SUCCESS;
            }
            return ESetFieldResult.INVALID_INPUT;
        }
        else return reason;
    }
    public static ESetFieldResult SetProperty<TValue>(PlayerSave obj, string property, TValue value)
    {
        if (obj is null) return ESetFieldResult.OBJECT_NOT_FOUND;
        if (property is null || value is null) return ESetFieldResult.FIELD_NOT_FOUND;
        FieldInfo? field = GetField(property, out ESetFieldResult reason);
        if (field is not null && reason == ESetFieldResult.SUCCESS)
        {
            if (field.FieldType.IsAssignableFrom(value.GetType()))
            {
                try
                {
                    field.SetValue(obj, value);
                }
                catch (Exception ex)
                {
                    L.LogError(ex);
                    return ESetFieldResult.FIELD_NOT_SERIALIZABLE;
                }
                return ESetFieldResult.SUCCESS;
            }
            return ESetFieldResult.INVALID_INPUT;
        }
        else return reason;
    }
    private static FieldInfo? GetField(string property, out ESetFieldResult reason)
    {
        for (int i = 0; i < fields.Length; i++)
        {
            FieldInfo fi = fields[i];
            if (fi.Name.Equals(property, StringComparison.Ordinal))
                return ValidateField(fi, out reason) ? fi : null;
        }
        for (int i = 0; i < fields.Length; i++)
        {
            FieldInfo fi = fields[i];
            if (fi.Name.Equals(property, StringComparison.OrdinalIgnoreCase))
                return ValidateField(fi, out reason) ? fi : null;
        }
        reason = ESetFieldResult.FIELD_NOT_FOUND;
        return default;
    }
    private static bool ValidateField(FieldInfo field, out ESetFieldResult reason)
    {
        if (field == null || field.IsStatic || field.IsInitOnly)
        {
            reason = ESetFieldResult.FIELD_NOT_FOUND;
            return false;
        }
        Attribute atr = Attribute.GetCustomAttribute(field, typeof(JsonSettable));
        if (atr is not null)
        {
            reason = ESetFieldResult.SUCCESS;
            return true;
        }
        else
        {
            reason = ESetFieldResult.FIELD_PROTECTED;
            return false;
        }
    }
    public static class NetCalls
    {
        public static readonly NetCall<ulong, bool> SendSetQueueSkip = new NetCall<ulong, bool>(ReceiveSetQueueSkip);
        public static readonly NetCall<ulong> GetPermissionsRequest = new NetCall<ulong>(ReceivePermissionRequest);
        public static readonly NetCall<ulong> CheckPlayerOnlineStatusRequest = new NetCall<ulong>(ReceivePlayerOnlineCheckRequest);

        public static readonly NetCallRaw<PlayerListEntry[]> SendPlayerList = new NetCallRaw<PlayerListEntry[]>(1000, PlayerListEntry.ReadArray, PlayerListEntry.WriteArray);
        public static readonly NetCallRaw<PlayerListEntry> SendPlayerJoined = new NetCallRaw<PlayerListEntry>(1016, PlayerListEntry.Read, PlayerListEntry.Write);
        public static readonly NetCall<ulong> SendPlayerLeft = new NetCall<ulong>(1017);
        public static readonly NetCall<ulong, bool> SendDutyChanged = new NetCall<ulong, bool>(1018);
        public static readonly NetCall<ulong, byte> SendTeamChanged = new NetCall<ulong, byte>(1019);
        public static readonly NetCall<ulong, bool> SendPlayerOnlineStatus = new NetCall<ulong, bool>(1036);
        public static readonly NetCall<ulong, EAdminType> SendPermissions = new NetCall<ulong, EAdminType>(1034);

        [NetCall(ENetCall.FROM_SERVER, 1024)]
        internal static void ReceiveSetQueueSkip(MessageContext context, ulong player, bool status)
        {
            if (PlayerSave.TryReadSaveFile(player, out PlayerSave save))
            {
                save.HasQueueSkip = status;
                PlayerSave.WriteToSaveFile(save);
            }
            else if (status)
            {
                save = new PlayerSave(player, 0, string.Empty, string.Empty, status, 0, false, false);
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
