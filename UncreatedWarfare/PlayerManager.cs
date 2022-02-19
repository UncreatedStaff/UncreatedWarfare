using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Uncreated.Networking.Encoding.IO;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare
{
    public static class PlayerManager
    {
        public static List<UCPlayer> OnlinePlayers;
        private static Dictionary<ulong, UCPlayer> _dict;
        public static readonly Type Type = typeof(PlayerSave);
        private static readonly FieldInfo[] fields = Type.GetFields();
        static PlayerManager()
        {
            OnlinePlayers = new List<UCPlayer>(50);
            _dict = new Dictionary<ulong, UCPlayer>(50);
        }
        public static UCPlayer? FromID(ulong steam64) => _dict.TryGetValue(steam64, out UCPlayer pl) ? pl : null;
        public static bool HasSave(ulong playerID, out PlayerSave? save) => PlayerSave.TryReadSaveFile(playerID, out save);
        public static PlayerSave? GetSave(ulong playerID) => PlayerSave.TryReadSaveFile(playerID, out PlayerSave? save) ? save : null;
        public static void ApplyToOnline()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            for (int i = 0; i < OnlinePlayers.Count; i++)
            {
                UCPlayer player = OnlinePlayers[i];
                if (!PlayerSave.TryReadSaveFile(player.Steam64, out PlayerSave? save) || save == null)
                    save = new PlayerSave(player.Steam64);
                save.Team = player.GetTeam();
                save.KitName = player.KitName;
                save.SquadName = player.Squad?.Name ?? string.Empty;
                save.LastGame = Data.Gamemode.GameID;
                PlayerSave.WriteToSaveFile(save);
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
        public static FPlayerList[] GetPlayerList()
        {
            FPlayerList[] rtn = new FPlayerList[OnlinePlayers.Count];
            for (int i = 0; i < OnlinePlayers!.Count; i++)
            {
                if (OnlinePlayers == null) continue;
                rtn[i] = new FPlayerList
                {
                    Duty = OnlinePlayers[i].OnDuty(),
                    Steam64 = OnlinePlayers[i].Steam64,
                    Name = F.GetPlayerOriginalNames(OnlinePlayers[i]).CharacterName,
                    Team = OnlinePlayers[i].Player.GetTeamByte()
                };
            }
            return rtn;
        }
        public static void InvokePlayerConnected(UnturnedPlayer player) => OnPlayerConnected(player);
        public static void InvokePlayerDisconnected(UnturnedPlayer player) => OnPlayerDisconnected(player);
        public static void AddSave(PlayerSave save) => PlayerSave.WriteToSaveFile(save);
        private static void OnPlayerConnected(UnturnedPlayer rocketplayer)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (!PlayerSave.TryReadSaveFile(rocketplayer.CSteamID.m_SteamID, out PlayerSave? save) || save == null)
            {
                save = new PlayerSave(rocketplayer.CSteamID.m_SteamID);
                PlayerSave.WriteToSaveFile(save);
            }
            UCPlayer player = new UCPlayer(
                    rocketplayer.CSteamID,
                    save.KitName,
                    rocketplayer.Player,
                    rocketplayer.Player.channel.owner.playerID.characterName,
                    rocketplayer.Player.channel.owner.playerID.nickName,
                    save.IsOtherDonator
                );

            Data.DatabaseManager.TryInitializeXP(player.Steam64);

            OnlinePlayers.Add(player);
            _dict.Add(player.Steam64, player);

            SquadManager.OnPlayerJoined(player, save.SquadName);
            FOBManager.SendFOBList(player);
        }
        private static void OnPlayerDisconnected(UnturnedPlayer rocketplayer)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            UCPlayer? player = UCPlayer.FromUnturnedPlayer(rocketplayer);
            if (player == null) return;
            player.IsOnline = false;

            OnlinePlayers.RemoveAll(s => s == default || s.Steam64 == rocketplayer.CSteamID.m_SteamID);
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

        /// <summary>reason [ 0: success, 1: no field, 2: invalid field, 3: non-saveable property ]</summary>
        private static FieldInfo? GetField(string property, out byte reason)
        {
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].Name == property) // case sensitive search
                {
                    if (ValidateField(fields[i], out reason))
                    {
                        return fields[i];
                    }
                }
            }
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].Name.ToLower() == property.ToLower()) // case insensitive search if case sensitive search netted no results
                {
                    if (ValidateField(fields[i], out reason))
                    {
                        return fields[i];
                    }
                }
            }
            reason = 1;
            return default;
        }
        private static object? ParseInput(string input, Type type, out bool parsed)
        {
            if (input == default || type == default)
            {
                parsed = false;
                return default;
            }
            if (type == typeof(object))
            {
                parsed = true;
                return input;
            }
            if (type == typeof(string))
            {
                parsed = true;
                return input;
            }
            if (type == typeof(bool))
            {
                string lowercase = input.ToLower();
                if (lowercase == "true")
                {
                    parsed = true;
                    return true;
                }
                else if (lowercase == "false")
                {
                    parsed = true;
                    return false;
                }
                else
                {
                    parsed = false;
                    return default;
                }
            }
            if (type == typeof(char))
            {
                if (input.Length == 1)
                {
                    parsed = true;
                    return input[0];
                }
            }
            if (type.IsEnum)
            {
                try
                {
                    object output = Enum.Parse(type, input, true);
                    if (output == default)
                    {
                        parsed = false;
                        return default;
                    }
                    parsed = true;
                    return output;
                }
                catch (ArgumentNullException)
                {
                    parsed = false;
                    return default;
                }
                catch (ArgumentException)
                {
                    parsed = false;
                    return default;
                }
            }
            if (!type.IsPrimitive)
            {
                L.LogError("Can not parse non-primitive types except for strings and enums.");
                parsed = false;
                return default;
            }

            if (type == typeof(int))
            {
                if (int.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out int result))
                {
                    parsed = true;
                    return result;
                }
            }
            else if (type == typeof(ushort))
            {
                if (ushort.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out ushort result))
                {
                    parsed = true;
                    return result;
                }
            }
            else if (type == typeof(ulong))
            {
                if (ulong.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out ulong result))
                {
                    parsed = true;
                    return result;
                }
            }
            else if (type == typeof(float))
            {
                if (float.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out float result))
                {
                    parsed = true;
                    return result;
                }
            }
            else if (type == typeof(decimal))
            {
                if (decimal.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out decimal result))
                {
                    parsed = true;
                    return result;
                }
            }
            else if (type == typeof(double))
            {
                if (double.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out double result))
                {
                    parsed = true;
                    return result;
                }
            }
            else if (type == typeof(byte))
            {
                if (byte.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out byte result))
                {
                    parsed = true;
                    return result;
                }
            }
            else if (type == typeof(sbyte))
            {
                if (sbyte.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out sbyte result))
                {
                    parsed = true;
                    return result;
                }
            }
            else if (type == typeof(short))
            {
                if (short.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out short result))
                {
                    parsed = true;
                    return result;
                }
            }
            else if (type == typeof(uint))
            {
                if (uint.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out uint result))
                {
                    parsed = true;
                    return result;
                }
            }
            else if (type == typeof(long))
            {
                if (long.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out long result))
                {
                    parsed = true;
                    return result;
                }
            }
            parsed = false;
            return default;
        }
        /// <summary>Fields must be instanced, non-readonly, and have the <see cref="JsonSettable"/> attribute to be set.</summary>
        public static PlayerSave SetProperty(PlayerSave obj, string property, string value, out bool set, out bool parsed, out bool found, out bool allowedToChange)
        {
            FieldInfo? field = GetField(property, out byte reason);
            if (reason != 0)
            {
                if (reason == 1 || reason == 2)
                {
                    set = false;
                    parsed = false;
                    found = false;
                    allowedToChange = false;
                    return obj;
                }
                else if (reason == 3)
                {
                    set = false;
                    parsed = false;
                    found = true;
                    allowedToChange = false;
                    return obj;
                }
            }
            found = true;
            allowedToChange = true;
            if (field == null)
            {
                found = false;
                allowedToChange = false;
                set = false;
                parsed = false;
                return obj;
            }
            object? parsedValue = ParseInput(value, field.FieldType, out parsed);
            if (parsed)
            {
                try
                {
                    field.SetValue(obj, parsedValue);
                    set = true;
                    PlayerSave.WriteToSaveFile(obj);
                    return obj;
                }
                catch (FieldAccessException ex)
                {
                    L.LogError(ex);
                    set = false;
                    return obj;
                }
                catch (TargetException ex)
                {
                    L.LogError(ex);
                    set = false;
                    return obj;
                }
                catch (ArgumentException ex)
                {
                    L.LogError(ex);
                    set = false;
                    return obj;
                }
            }
            else
            {
                set = false;
                return obj;
            }
        }
        /// <summary>reason [ 0: success, 1: no field, 2: invalid field, 3: non-saveable property ]</summary>
        private static bool ValidateField(FieldInfo field, out byte reason)
        {
            if (field == default)
            {
                L.LogError("PlayerSave saver: field not found.");
                reason = 1;
                return false;
            }
            if (field.IsStatic)
            {
                L.LogError("PlayerSave saver tried to save to a static property.");
                reason = 2;
                return false;
            }
            if (field.IsInitOnly)
            {
                L.LogError("PlayerSave saver tried to save to a readonly property.");
                reason = 2;
                return false;
            }
            IEnumerator<CustomAttributeData> attributes = field.CustomAttributes.GetEnumerator();
            bool settable = false;
            while (attributes.MoveNext())
            {
                if (attributes.Current.AttributeType == typeof(JsonSettable))
                {
                    settable = true;
                    break;
                }
            }
            attributes.Dispose();
            if (!settable)
            {
                L.LogError("PlayerSave saver tried to save to a non json-savable property.");
                reason = 3;
                return false;
            }
            reason = 0;
            return true;
        }
        public static PlayerSave SetProperty<V>(PlayerSave obj, string property, V value, out bool success, out bool found, out bool allowedToChange)
        {
            FieldInfo? field = GetField(property, out byte reason);
            if (reason != 0)
            {
                if (reason == 1 || reason == 2)
                {
                    found = false;
                    allowedToChange = false;
                    success = false;
                    return obj;
                }
                else if (reason == 3)
                {
                    found = true;
                    allowedToChange = false;
                    success = false;
                    return obj;
                }
            }
            found = true;
            allowedToChange = true;
            if (field != default)
            {
                if (field.FieldType.IsAssignableFrom(typeof(V)))
                {
                    try
                    {
                        field.SetValue(obj, value);
                        success = true;
                        PlayerSave.WriteToSaveFile(obj);
                        return obj;
                    }
                    catch (FieldAccessException ex)
                    {
                        L.LogError(ex);
                        success = false;
                        return obj;
                    }
                    catch (TargetException ex)
                    {
                        L.LogError(ex);
                        success = false;
                        return obj;
                    }
                    catch (ArgumentException ex)
                    {
                        L.LogError(ex);
                        success = false;
                        return obj;
                    }
                }
                else
                {
                    success = false;
                    return obj;
                }
            }
            else
            {
                success = false;
                return obj;
            }
        }
    }
}
