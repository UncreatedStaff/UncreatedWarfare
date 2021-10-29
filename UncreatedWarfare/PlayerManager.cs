using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Uncreated.Networking;
using Uncreated.Networking.Encoding.IO;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare
{
    public class PlayerManager
    {
        public readonly static RawByteIO<List<PlayerSave>> IO = new RawByteIO<List<PlayerSave>>(PlayerSave.ReadList, PlayerSave.WriteList, directory + file, 4);
        public static List<UCPlayer> OnlinePlayers;
        public static List<UCPlayer> Team1Players;
        public static List<UCPlayer> Team2Players;
        public static List<PlayerSave> ActiveObjects;
        private static readonly string directory = Data.KitsStorage;
        public static readonly Type Type = typeof(PlayerSave);
        private static readonly FieldInfo[] fields = Type.GetFields();
        private const string file = "playersaves.dat";
        private static readonly string Path = directory + file;
        public PlayerManager()
        {
            Load();
            OnlinePlayers = new List<UCPlayer>();
            Team1Players = new List<UCPlayer>();
            Team2Players = new List<UCPlayer>();
        }
        private void Load()
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            if (File.Exists(directory + file))
            {
                if (IO.ReadFrom(Path, out List<PlayerSave> saves))
                {
                    ActiveObjects = saves;
                    F.Log($"Read {saves.Count} saves.", ConsoleColor.Magenta);
                }
                else
                {
                    ActiveObjects = new List<PlayerSave>();
                    F.LogError("Failed to read saves!!!");
                }
                bool needwrite = ActiveObjects.Count == 0;
                for (int i = 0; i < ActiveObjects.Count; i++)
                {
                    if (ActiveObjects[i].DATA_VERSION < PlayerSave.CURRENT_DATA_VERSION)
                    {
                        ActiveObjects[i].DATA_VERSION = PlayerSave.CURRENT_DATA_VERSION;
                        needwrite = true;
                    }
                    if (needwrite)
                    {
                        IO.WriteTo(ActiveObjects, Path);
                    }
                }
            } else
            {
                ActiveObjects = new List<PlayerSave>();
                IO.WriteTo(ActiveObjects, Path);
            }
        }
        protected static List<PlayerSave> GetObjectsWhere(Func<PlayerSave, bool> predicate, bool readFile = false) => ActiveObjects.Where(predicate).ToList();
        protected static PlayerSave GetObject(Func<PlayerSave, bool> predicate, bool readFile = false) => ActiveObjects.FirstOrDefault(predicate);
        protected static bool ObjectExists(Func<PlayerSave, bool> match, out PlayerSave item, bool readFile = false)
        {
            item = GetObject(match);
            return item != null;
        }
        public static bool HasSave(ulong playerID, out PlayerSave save) => ObjectExists(ks => ks.Steam64 == playerID, out save, false);
        public static bool HasSaveRead(ulong playerID, out PlayerSave save) => ObjectExists(ks => ks.Steam64 == playerID, out save, true);
        public static PlayerSave GetSave(ulong playerID) => GetObject(ks => ks.Steam64 == playerID, true);
        public static void ApplyToOnline()
        {
            for (int i = 0; i < OnlinePlayers.Count; i++)
            {
                for (int p = 0; p < ActiveObjects.Count; p++)
                {
                    if (ActiveObjects[p].Steam64 == OnlinePlayers[i].Steam64)
                    {
                        ActiveObjects[p].Team = OnlinePlayers[i].GetTeam();
                        ActiveObjects[p].KitName = OnlinePlayers[i].KitName;
                        ActiveObjects[p].SquadName = OnlinePlayers[i].Squad != null ? OnlinePlayers[i].Squad.Name : "";
                    }
                }
            }
            IO.WriteTo(ActiveObjects, Path);
        }
        public static void Write() =>
                IO.WriteTo(ActiveObjects, Path);
        public static FPlayerList[] GetPlayerList()
        {
            FPlayerList[] rtn = new FPlayerList[OnlinePlayers.Count];
            for (int i = 0; i < OnlinePlayers.Count; i++)
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
        public static void AddSave(PlayerSave save)
        {
            ActiveObjects.Add(save);
            IO.WriteTo(ActiveObjects, Path);
        }
        private static void OnPlayerConnected(UnturnedPlayer rocketplayer)
        {
            PlayerSave save;

            if (!HasSave(rocketplayer.CSteamID.m_SteamID, out var existingSave))
            {
                save = new PlayerSave(rocketplayer.CSteamID.m_SteamID);
                AddSave(save);
            }
            else
            {
                save = existingSave;
            }

            UCPlayer player = new UCPlayer(
                    rocketplayer.CSteamID,
                    save.KitName,
                    rocketplayer.Player,
                    rocketplayer.Player.channel.owner.playerID.characterName,
                    rocketplayer.Player.channel.owner.playerID.nickName,
                    save.IsOtherDonator
                );

            OnlinePlayers.Add(player);
            if (player.IsTeam1())
                Team1Players.Add(player);
            else if (player.IsTeam2())
                Team2Players.Add(player);

            SquadManager.InvokePlayerJoined(player, save.SquadName);
            F.Log("updating FOB UI :)");
            FOBManager.UpdateUI(player);
            F.Log("FOB UI update successful :)");
        }
        private static void OnPlayerDisconnected(UnturnedPlayer rocketplayer)
        {
            UCPlayer player = UCPlayer.FromUnturnedPlayer(rocketplayer);
            player.IsOnline = false;

            OnlinePlayers.RemoveAll(s => s == default || s.Steam64 == rocketplayer.CSteamID.m_SteamID);

            if (TeamManager.IsTeam1(rocketplayer))
                Team1Players.RemoveAll(s => s == default || s.Steam64 == rocketplayer.CSteamID.m_SteamID);
            else if (TeamManager.IsTeam2(rocketplayer))
                Team2Players.RemoveAll(s => s == default || s.Steam64 == rocketplayer.CSteamID.m_SteamID);

            SquadManager.InvokePlayerLeft(player);
        }
        public static List<UCPlayer> GetNearbyPlayers(float range, Vector3 point) => OnlinePlayers.Where(p => !p.Player.life.isDead && (p.Position - point).sqrMagnitude < Math.Pow(range, 2)).ToList();
        public static bool IsPlayerNearby(ulong playerID, float range, Vector3 point) => OnlinePlayers.Find(p => p.Steam64 == playerID && !p.Player.life.isDead && (p.Position - point).sqrMagnitude < Math.Pow(range, 2)) != null;

        public static void VerifyTeam(Player nelsonplayer)
        {
            if (nelsonplayer == default) return;

            UCPlayer player = OnlinePlayers.Find(p => p.Steam64 == nelsonplayer.channel.owner.playerID.steamID.m_SteamID);
            if (player == default)
            {
                F.LogError("Failed to get UCPlayer instance of " + nelsonplayer.name);
                return;
            }

            if (TeamManager.IsTeam1(nelsonplayer))
            {
                Team2Players.RemoveAll(p => p == default || p.Steam64 == nelsonplayer.channel.owner.playerID.steamID.m_SteamID);
                if (!Team1Players.Exists(p => p == default || p.Steam64 == nelsonplayer.channel.owner.playerID.steamID.m_SteamID))
                {
                    Team1Players.Add(player);
                }
            }
            else if (TeamManager.IsTeam2(nelsonplayer))
            {
                Team1Players.RemoveAll(p => p == default || p.Steam64 == nelsonplayer.channel.owner.playerID.steamID.m_SteamID);
                if (!Team2Players.Exists(p => p == default || p.Steam64 == nelsonplayer.channel.owner.playerID.steamID.m_SteamID))
                {
                    Team2Players.Add(player);
                }
            }
        }

        internal static void PickGroupAfterJoin(UCPlayer ucplayer)
        {
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
                    ucplayer.Player.teleportToLocation(F.GetBaseSpawn(ucplayer.Player), F.GetBaseAngle(team));
                }
            }
            GroupManager.save();
        }


        /// <summary>reason [ 0: success, 1: no field, 2: invalid field, 3: non-saveable property ]</summary>
        private static FieldInfo GetField(string property, out byte reason)
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
        private static object ParseInput(string input, Type type, out bool parsed)
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
                F.LogError("Can not parse non-primitive types except for strings and enums.");
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
            FieldInfo field = GetField(property, out byte reason);
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
            object parsedValue = ParseInput(value, field.FieldType, out parsed);
            if (parsed)
            {
                if (field != default)
                {
                    try
                    {
                        field.SetValue(obj, parsedValue);
                        set = true;
                        Write();
                        return obj;
                    }
                    catch (FieldAccessException ex)
                    {
                        F.LogError(ex);
                        set = false;
                        return obj;
                    }
                    catch (TargetException ex)
                    {
                        F.LogError(ex);
                        set = false;
                        return obj;
                    }
                    catch (ArgumentException ex)
                    {
                        F.LogError(ex);
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
                F.LogError("PlayerSave saver: field not found.");
                reason = 1;
                return false;
            }
            if (field.IsStatic)
            {
                F.LogError("PlayerSave saver tried to save to a static property.");
                reason = 2;
                return false;
            }
            if (field.IsInitOnly)
            {
                F.LogError("PlayerSave saver tried to save to a readonly property.");
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
                F.LogError("PlayerSave saver tried to save to a non json-savable property.");
                reason = 3;
                return false;
            }
            reason = 0;
            return true;
        }
        /// <summary>Fields must be instanced, non-readonly, and have the <see cref="JsonSettable"/> attribute to be set.</summary>
        public static bool SetProperty(Func<PlayerSave, bool> selector, string property, string value, out bool foundObject, out bool setSuccessfully, out bool parsed, out bool found, out bool allowedToChange)
        {
            if (ObjectExists(selector, out PlayerSave selected))
            {
                foundObject = true;
                SetProperty(selected, property, value, out setSuccessfully, out parsed, out found, out allowedToChange);
                return setSuccessfully;
            }
            else
            {
                foundObject = false;
                setSuccessfully = false;
                parsed = false;
                found = false;
                allowedToChange = false;
                return false;
            }
        }
        public static bool SetProperty<V>(Func<PlayerSave, bool> selector, string property, V value, out bool foundObject, out bool setSuccessfully, out bool foundproperty, out bool allowedToChange)
        {
            if (ObjectExists(selector, out PlayerSave selected))
            {
                foundObject = true;
                SetProperty(selected, property, value, out setSuccessfully, out foundproperty, out allowedToChange);
                return setSuccessfully;
            }
            else
            {
                foundObject = false;
                setSuccessfully = false;
                foundproperty = false;
                allowedToChange = false;
                return false;
            }
        }
        public static PlayerSave SetProperty<V>(PlayerSave obj, string property, V value, out bool success, out bool found, out bool allowedToChange)
        {
            FieldInfo field = GetField(property, out byte reason);
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
                        Write();
                        return obj;
                    }
                    catch (FieldAccessException ex)
                    {
                        F.LogError(ex);
                        success = false;
                        return obj;
                    }
                    catch (TargetException ex)
                    {
                        F.LogError(ex);
                        success = false;
                        return obj;
                    }
                    catch (ArgumentException ex)
                    {
                        F.LogError(ex);
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
