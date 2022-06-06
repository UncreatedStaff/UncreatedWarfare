using Rocket.API;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Uncreated.Framework;
using Uncreated.Warfare.Gamemodes.Interfaces;
using UnityEngine;

namespace Uncreated.Warfare.Commands;
public struct CommandContext
{
    public readonly UCPlayer? Caller;
    public readonly bool IsConsole;
    public readonly string[] Parameters;
    public readonly int ArgumentCount;
    public readonly ulong CallerID;
    public readonly CSteamID CallerCSteamID;

    private static readonly Regex RemoveRichTextRegex = new Regex("<(?:(?:(?=.*<\\/color>)color=#{0,1}[0123456789ABCDEF]{6})|(?:(?<=<color=#{0,1}[0123456789ABCDEF]{6}>.*)\\/color)|(?:(?=.*<\\/b>)b)|(?:(?<=<b>.*)\\/b)|(?:(?=.*<\\/i>)i)|(?:(?<=<i>.*)\\/i)|(?:(?=.*<\\/size>)size=\\d+)|(?:(?<=<size=\\d+>.*)\\/size)|(?:(?=.*<\\/material>)material=\\d+)|(?:(?<=<material=\\d+>.*)\\/material))>", RegexOptions.IgnoreCase);
    public CommandContext(IRocketPlayer caller, string[] args)
    {
        if (args is null) args = Array.Empty<string>();
        if (caller is ConsolePlayer)
        {
            Caller = null;
            IsConsole = true;
            CallerID = 0;
            CallerCSteamID = CSteamID.Nil;
        }
        else
        {
            Caller = UCPlayer.FromIRocketPlayer(caller);
            if (Caller is null)
            {
                CallerID = 0;
                CallerCSteamID = CSteamID.Nil;
                IsConsole = true;
            }
            else
            {
                IsConsole = false;
                CallerID = Caller.Steam64;
                CallerCSteamID = Caller.Player.channel.owner.playerID.steamID;
            }
        }
        Parameters = args;
        ArgumentCount = args.Length;
    }
    public bool CheckPermission(string permission) =>
        IsConsole || (Caller is not null && Caller.HasPermission(permission));
    public bool CheckPermissionOr(string permission1, string permission2) =>
        IsConsole || (Caller is not null && (Caller.HasPermission(permission1) || Caller.HasPermission(permission2)));
    public bool CheckPermissionAnd(string permission1, string permission2) =>
        IsConsole || (Caller is not null && Caller.HasPermission(permission1) && Caller.HasPermission(permission2));
    public bool CheckPermission(EAdminType permission) =>
        IsConsole || (Caller is not null && F.PermissionCheck(Caller, permission));
    public bool HasDutyPerms() =>
        IsConsole || (Caller is not null && Caller.OnDuty());
    public bool HasArg(int position)
    {
        return position > -1 && position < ArgumentCount;
    }
    public bool HasArgs(int count)
    {
        return count > -1 && count <= ArgumentCount;
    }
    public bool MatchParameter(int parameter, string value)
    {
        if (parameter < 0 || parameter >= ArgumentCount)
            return false;
        return Parameters[parameter].Equals(value, StringComparison.OrdinalIgnoreCase);
    }
    public bool MatchParameter(int parameter, string value, string alternate)
    {
        if (parameter < 0 || parameter >= ArgumentCount)
            return false;
        string v = Parameters[parameter];
        return v.Equals(value, StringComparison.OrdinalIgnoreCase) || v.Equals(alternate, StringComparison.OrdinalIgnoreCase);
    }
    public bool MatchParameter(int parameter, string value, string alternate1, string alternate2)
    {
        if (parameter < 0 || parameter >= ArgumentCount)
            return false;
        string v = Parameters[parameter];
        return v.Equals(value, StringComparison.OrdinalIgnoreCase) || v.Equals(alternate1, StringComparison.OrdinalIgnoreCase) || v.Equals(alternate2, StringComparison.OrdinalIgnoreCase);
    }
    public bool MatchParameterPartial(int parameter, string value)
    {
        if (parameter < 0 || parameter >= ArgumentCount)
            return false;
        return Parameters[parameter].IndexOf(value, StringComparison.OrdinalIgnoreCase) != -1;
    }
    public bool MatchParameterPartial(int parameter, string value, string alternate)
    {
        if (parameter < 0 || parameter >= ArgumentCount)
            return false;
        string v = Parameters[parameter];
        return v.IndexOf(value, StringComparison.OrdinalIgnoreCase) != -1 || v.IndexOf(alternate, StringComparison.OrdinalIgnoreCase) != -1;
    }
    public bool MatchParameterPartial(int parameter, string value, string alternate1, string alternate2)
    {
        if (parameter < 0 || parameter >= ArgumentCount)
            return false;
        string v = Parameters[parameter];
        return v.IndexOf(value, StringComparison.OrdinalIgnoreCase) != -1 || v.IndexOf(alternate1, StringComparison.OrdinalIgnoreCase) != -1 || v.IndexOf(alternate2, StringComparison.OrdinalIgnoreCase) != -1;
    }
    public bool TryGet(int parameter, out string value)
    {
        if (parameter < 0 || parameter >= ArgumentCount)
        {
            value = null!;
            return false;
        }
        value = Parameters[parameter];
        return true;
    }
    public bool TryGet<TEnum>(int parameter, out TEnum value) where TEnum : unmanaged, Enum
    {
        if (parameter < 0 || parameter >= ArgumentCount)
        {
            value = default;
            return false;
        }
        return Enum.TryParse(Parameters[parameter], true, out value);
    }
    public bool TryGetRange(int start, out string value, int length = -1)
    {
        value = GetRange(start, length)!;
        return value is not null;
    }
    public string? Get(int parameter)
    {
        if (parameter < 0 || parameter >= ArgumentCount)
            return null;
        return Parameters[parameter];
    }
    public string? GetRange(int start, int length = -1)
    {
        if (length == 1) return Get(start);
        if (start < 0 || start >= ArgumentCount)
            return null;
        if (start == ArgumentCount - 1)
            return Parameters[start];
        if (length == -1)
            return string.Join(" ", Parameters, start, ArgumentCount - start);
        if (length < 1) return null;
        if (start + length >= ArgumentCount)
            length = ArgumentCount - start;
        return string.Join(" ", Parameters, start, length);
    }
    public bool TryGet(int parameter, out int value)
    {
        if (parameter < 0 || parameter >= ArgumentCount)
        {
            value = 0;
            return false;
        }
        return int.TryParse(Parameters[parameter], NumberStyles.Any, Data.Locale, out value);
    }
    public bool TryGet(int parameter, out byte value)
    {
        if (parameter < 0 || parameter >= ArgumentCount)
        {
            value = 0;
            return false;
        }
        return byte.TryParse(Parameters[parameter], NumberStyles.Any, Data.Locale, out value);
    }
    public bool TryGet(int parameter, out sbyte value)
    {
        if (parameter < 0 || parameter >= ArgumentCount)
        {
            value = 0;
            return false;
        }
        return sbyte.TryParse(Parameters[parameter], NumberStyles.Any, Data.Locale, out value);
    }
    public bool TryGet(int parameter, out Guid value)
    {
        if (parameter < 0 || parameter >= ArgumentCount)
        {
            value = default;
            return false;
        }
        return Guid.TryParse(Parameters[parameter], out value);
    }
    public bool TryGet(int parameter, out uint value)
    {
        if (parameter < 0 || parameter >= ArgumentCount)
        {
            value = 0;
            return false;
        }
        return uint.TryParse(Parameters[parameter], NumberStyles.Any, Data.Locale, out value);
    }
    public bool TryGet(int parameter, out ushort value)
    {
        if (parameter < 0 || parameter >= ArgumentCount)
        {
            value = 0;
            return false;
        }
        return ushort.TryParse(Parameters[parameter], NumberStyles.Any, Data.Locale, out value);
    }
    public bool TryGet(int parameter, out ulong value)
    {
        if (parameter < 0 || parameter >= ArgumentCount)
        {
            value = 0;
            return false;
        }
        return ulong.TryParse(Parameters[parameter], NumberStyles.Any, Data.Locale, out value);
    }
    public bool TryGet(int parameter, out ulong steam64, out UCPlayer? onlinePlayer)
    {
        if (parameter < 0 || parameter >= ArgumentCount)
        {
            steam64 = 0;
            onlinePlayer = null;
            return false;
        }

        string s = Parameters[parameter];
        if (ulong.TryParse(s, NumberStyles.Any, Data.Locale, out steam64) && OffenseManager.IsValidSteam64ID(steam64))
        {
            onlinePlayer = UCPlayer.FromID(steam64);
            return true;
        }
        onlinePlayer = UCPlayer.FromName(s, true);
        if (onlinePlayer is not null)
        {
            steam64 = onlinePlayer.Steam64;
            return true;
        }
        else
            return false;
    }
    public bool TryGet(int parameter, out ulong steam64, out UCPlayer onlinePlayer, IEnumerable<UCPlayer> selection)
    {
        if (parameter < 0 || parameter >= ArgumentCount)
        {
            steam64 = 0;
            onlinePlayer = null!;
            return false;
        }

        string s = Parameters[parameter];
        if (ulong.TryParse(s, NumberStyles.Any, Data.Locale, out steam64) && OffenseManager.IsValidSteam64ID(steam64))
        {
            foreach (UCPlayer player in selection)
            {
                if (player.Steam64 == steam64)
                {
                    onlinePlayer = player;
                    return true;
                }
            }
        }
        onlinePlayer = UCPlayer.FromName(s, true, selection)!;
        if (onlinePlayer is not null)
        {
            steam64 = onlinePlayer.Steam64;
            return true;
        }
        else
            return false;
    }
    public bool TryGet(int parameter, out float value)
    {
        if (parameter < 0 || parameter >= ArgumentCount)
        {
            value = 0;
            return false;
        }
        return float.TryParse(Parameters[parameter], NumberStyles.Any, Data.Locale, out value);
    }
    public bool TryGet(int parameter, out double value)
    {
        if (parameter < 0 || parameter >= ArgumentCount)
        {
            value = 0;
            return false;
        }
        return double.TryParse(Parameters[parameter], NumberStyles.Any, Data.Locale, out value);
    }
    public bool TryGet(int parameter, out decimal value)
    {
        if (parameter < 0 || parameter >= ArgumentCount)
        {
            value = 0;
            return false;
        }
        return decimal.TryParse(Parameters[parameter], NumberStyles.Any, Data.Locale, out value);
    }
    /// <summary>Get an asset based on a <see cref="Guid"/> search, <see cref="ushort"/> search, then <see cref="Asset.FriendlyName"/> search.</summary>
    /// <typeparam name="TAsset"><see cref="Asset"/> type to find.</typeparam>
    /// <param name="length">Set to 1 to only get one parameter (default), set to -1 to get any remaining parameters.</param>
    /// <param name="multipleResultsFound"><see langword="true"/> if <paramref name="allowMultipleResults"/> is <see langword="false"/> and multiple results were found.</param>
    /// <param name="allowMultipleResults">Set to <see langword="false"/> to make the function return <see langword="false"/> if multiple results are found. <paramref name="asset"/> will still be set.</param>
    /// <returns><see langword="true"/> If a <typeparamref name="TAsset"/> is found or multiple are found and <paramref name="allowMultipleResults"/> is <see langword="true"/>.</returns>
    public bool TryGet<TAsset>(int parameter, out TAsset asset, out bool multipleResultsFound, bool remainder = false, bool allowMultipleResults = false) where TAsset : Asset
    {
        if (!TryGetRange(parameter, out string p, remainder ? -1 : 1))
        {
            multipleResultsFound = false;
            asset = null!;
            return false;
        }
        if (Guid.TryParse(p, out Guid guid))
        {
            asset = Assets.find<TAsset>(guid);
            multipleResultsFound = false;
            return asset is not null;
        }
        EAssetType type = JsonAssetReference<TAsset>.AssetTypeHelper.Type;
        if (type != EAssetType.NONE)
        {
            if (ushort.TryParse(p, out ushort value))
            {
                if (Assets.find(type, value) is TAsset asset2)
                {
                    asset = asset2;
                    multipleResultsFound = false;
                    return true;
                }
            }

            TAsset[] assets = Assets.find(type).OfType<TAsset>().OrderBy(x => x.FriendlyName.Length).ToArray();
            if (allowMultipleResults)
            {
                for (int i = 0; i < assets.Length; ++i)
                {
                    if (assets[i].FriendlyName.Equals(p, StringComparison.OrdinalIgnoreCase))
                    {
                        asset = assets[i];
                        multipleResultsFound = false;
                        return true;
                    }
                }
                for (int i = 0; i < assets.Length; ++i)
                {
                    if (assets[i].FriendlyName.IndexOf(p, StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        asset = assets[i];
                        multipleResultsFound = false;
                        return true;
                    }
                }
            }
            else
            {
                List<TAsset> results = new List<TAsset>(16);
                for (int i = 0; i < assets.Length; ++i)
                {
                    if (assets[i].FriendlyName.Equals(p, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(assets[i]);
                    }
                }
                if (results.Count == 1)
                {
                    asset = results[0];
                    multipleResultsFound = false;
                    return true;
                }
                else if (results.Count > 1)
                {
                    multipleResultsFound = true;
                    asset = results[0];
                    return false; // if multiple results match for the full name then a partial will be the same
                }
                for (int i = 0; i < assets.Length; ++i)
                {
                    if (assets[i].FriendlyName.IndexOf(p, StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        results.Add(assets[i]);
                    }
                }
                if (results.Count == 1)
                {
                    asset = results[0];
                    multipleResultsFound = false;
                    return true;
                }
                else if (results.Count > 1)
                {
                    multipleResultsFound = true;
                    asset = results[0];
                    return false;
                }
            }
        }
        multipleResultsFound = false;
        asset = null!;
        return false;
    }
    public void Reply(string translationKey, params string[] formatting)
    {
        if (translationKey is null) throw new ArgumentNullException(nameof(translationKey));
        if (formatting is null) formatting = Array.Empty<string>();
        if (IsConsole || Caller is null)
        {
            string message = Translation.Translate(translationKey, JSONMethods.DEFAULT_LANGUAGE, out Color color, formatting);
            message = RemoveRichText(message);
            ConsoleColor clr = GetClosestConsoleColor(color);
            L.Log(message, clr);
        }
        else
        {
            Caller.SendChat(translationKey, formatting);
        }
    }
    public bool TryGetTarget(out Transform transform)
    {
        if (IsConsole || Caller is null || !Caller.IsOnline)
        {
            transform = null!;
            return false;
        }
        Transform aim = Caller.Player.look.aim;
        RaycastInfo info = DamageTool.raycast(new Ray(aim.position, aim.forward), 4f, RayMasks.PLAYER_INTERACT, Caller.Player);
        transform = info.transform;
        return transform != null;
    }
    public bool TryGetTarget<T>(out T interactable) where T : Interactable
    {
        if (IsConsole || Caller is null || !Caller.IsOnline)
        {
            interactable = null!;
            return false;
        }
        Transform aim = Caller.Player.look.aim;
        RaycastInfo info = DamageTool.raycast(new Ray(aim.position, aim.forward), 4f, RayMasks.PLAYER_INTERACT, Caller.Player);
        if (info.transform == null)
        {
            interactable = null!;
            return false;
        }
        if (typeof(InteractableVehicle).IsAssignableFrom(typeof(T)))
        {
            interactable = (info.vehicle as T)!;
            return interactable != null;
        }
        else if (typeof(InteractableForage).IsAssignableFrom(typeof(T)))
        {
            if (info.transform.TryGetComponent(out InteractableForage forage))
            {
                interactable = (forage as T)!;
                return interactable != null;
            }
        }
        BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(info.transform);
        interactable = (drop?.interactable as T)!;
        return interactable != null;
    }
    public bool TryGetTarget(out BarricadeDrop drop)
    {
        if (IsConsole || Caller is null || !Caller.IsOnline)
        {
            drop = null!;
            return false;
        }
        Transform aim = Caller.Player.look.aim;
        RaycastInfo info = DamageTool.raycast(new Ray(aim.position, aim.forward), 4f, RayMasks.BARRICADE, Caller.Player);
        if (info.transform == null)
        {
            drop = null!;
            return false;
        }
        drop = BarricadeManager.FindBarricadeByRootTransform(info.transform);
        return drop != null;
    }
    public bool TryGetTarget(out StructureDrop drop)
    {
        if (IsConsole || Caller is null || !Caller.IsOnline)
        {
            drop = null!;
            return false;
        }
        Transform aim = Caller.Player.look.aim;
        RaycastInfo info = DamageTool.raycast(new Ray(aim.position, aim.forward), 4f, RayMasks.STRUCTURE, Caller.Player);
        if (info.transform == null)
        {
            drop = null!;
            return false;
        }
        drop = StructureManager.FindStructureByRootTransform(info.transform);
        return drop != null;
    }
    public bool TryGetTarget(out InteractableVehicle vehicle)
    {
        if (IsConsole || Caller is null || !Caller.IsOnline)
        {
            vehicle = null!;
            return false;
        }
        Transform aim = Caller.Player.look.aim;
        RaycastInfo info = DamageTool.raycast(new Ray(aim.position, aim.forward), 4f, RayMasks.VEHICLE, Caller.Player);
        if (info.transform == null)
        {
            vehicle = null!;
            return false;
        }
        vehicle = info.vehicle;
        return vehicle != null;
    }
    public bool TryGetTarget(out UCPlayer player)
    {
        if (IsConsole || Caller is null || !Caller.IsOnline)
        {
            player = null!;
            return false;
        }
        Transform aim = Caller.Player.look.aim;
        RaycastInfo info = DamageTool.raycast(new Ray(aim.position, aim.forward), 4f, RayMasks.PLAYER, Caller.Player);
        player = (info.player == null ? null : UCPlayer.FromPlayer(info.player))!;
        return player != null && player.IsOnline;
    }
    public bool HasPermissionOrReply(string permission, string noPermissionMessageKey = "no_permissions")
    {
        bool perm = CheckPermission(permission);
        if (!perm)
            Reply(noPermissionMessageKey);
        return perm;
    }
    public bool HasPermissionOrReply(string permission, string[] formatting, string noPermissionMessageKey = "no_permissions")
    {
        bool perm = CheckPermission(permission);
        if (!perm)
            Reply(noPermissionMessageKey, formatting);
        return perm;
    }
    public bool HasPermissionOrReplyAnd(string permission1, string permission2, string noPermissionMessageKey = "no_permissions")
    {
        bool perm = CheckPermissionOr(permission1, permission2);
        if (!perm)
            Reply(noPermissionMessageKey);
        return perm;
    }
    public bool HasPermissionOrReplyOr(string permission1, string permission2, string noPermissionMessageKey = "no_permissions")
    {
        bool perm = CheckPermissionOr(permission1, permission2);
        if (!perm)
            Reply(noPermissionMessageKey);
        return perm;
    }
    public bool HasPermissionOrReplyAnd(string permission1, string permission2, string[] formatting, string noPermissionMessageKey = "no_permissions")
    {
        bool perm = CheckPermissionAnd(permission1, permission2);
        if (!perm)
            Reply(noPermissionMessageKey, formatting);
        return perm;
    }
    public bool HasPermissionOrReplyOr(string permission1, string permission2, string[] formatting, string noPermissionMessageKey = "no_permissions")
    {
        bool perm = CheckPermissionOr(permission1, permission2);
        if (!perm)
            Reply(noPermissionMessageKey, formatting);
        return perm;
    }
    public bool HasPermissionOrReply(EAdminType permission, string noPermissionMessageKey = "no_permissions")
    {
        bool perm = CheckPermission(permission);
        if (!perm)
            Reply(noPermissionMessageKey);
        return perm;
    }
    public bool HasPermissionOrReply(EAdminType permission, string[] formatting, string noPermissionMessageKey = "no_permissions")
    {
        bool perm = CheckPermission(permission);
        if (!perm)
            Reply(noPermissionMessageKey, formatting);
        return perm;
    }
    public bool OnDutyOrReply(string noPermissionMessageKey = "no_permissions")
    {
        bool perm = IsConsole || (Caller is not null && Caller.OnDuty());
        if (!perm)
            Reply(noPermissionMessageKey);
        return perm;
    }
    public bool OnDutyOrReply(string[] formatting, string noPermissionMessageKey = "no_permissions")
    {
        bool perm = IsConsole || (Caller is not null && Caller.OnDuty());
        if (!perm)
            Reply(noPermissionMessageKey, formatting);
        return perm;
    }
    public void LogAction(EActionLogType type, string? data = null)
    {
        ActionLog.Add(type, data, CallerID);
    }
    public void SendGamemodeError()
    {
        Reply("command_e_gamemode");
    }
    public void SendPlayerOnlyError()
    {
        Reply("command_e_no_console");
    }
    public void SendConsoleOnlyError()
    {
        Reply("command_e_no_player");
    }
    public void SendUnknownError()
    {
        Reply("command_e_unknown_error");
    }
    public bool IsConsoleReply()
    {
        if (IsConsole || Caller is null)
        {
            SendPlayerOnlyError();
            return false;
        }
        return true;
    }
    public bool IsPlayerReply()
    {
        if (!IsConsole)
            SendConsoleOnlyError();
        return !IsConsole;
    }
    public bool CheckGamemodeAndSend<T>() where T : IGamemode
    {
        if (Data.Is<T>())
            return true;
        SendGamemodeError();
        return false;
    }
    public void SendNoPermission()
    {
        Reply("no_permissions");
    }
    public void SendCorrectUsage(string usage)
    {
        Reply("correct_usage", usage);
    }
    public void SendPlayerNotFound()
    {
        Reply("command_e_player_not_found");
    }
    public static ConsoleColor GetClosestConsoleColor(Color color)
    {
        int i = (color.r > 0.5f || color.g > 0.5f || color.b > 0.5f) ? 8 : 0;
        if (color.r > 0.5f) i |= 4;
        if (color.g > 0.5f) i |= 2;
        if (color.b > 0.5f) i |= 1;
        return (ConsoleColor)i;
    }
    public static string RemoveRichText(string text)
    {
        return RemoveRichTextRegex.Replace(text, string.Empty);
    }
}
