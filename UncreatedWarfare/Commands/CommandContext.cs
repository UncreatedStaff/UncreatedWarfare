using Rocket.API;
using Steamworks;
using System;
using System.Globalization;
using Uncreated.Framework;
using Uncreated.Warfare.Gamemodes.Interfaces;
using UnityEngine;

namespace Uncreated.Warfare.Commands;
public readonly struct CommandContext
{
    public readonly UCPlayer? Caller;
    public readonly bool IsConsole;
    public readonly string[] Parameters;
    public readonly int ArgumentCount;
    public readonly ulong CallerID;
    public readonly CSteamID CallerCSteamID;
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
        return string.Join(" ", Parameters, start, ArgumentCount - start);
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
    public void Reply(string translationKey, params string[] formatting)
    {
        if (translationKey is null) throw new ArgumentNullException(nameof(translationKey));
        if (formatting is null) formatting = Array.Empty<string>();
        if (IsConsole || Caller is null)
        {
            string message = Translation.Translate(translationKey, JSONMethods.DEFAULT_LANGUAGE, out Color color, formatting);
            ConsoleColor clr = GetClosestConsoleColor(color);
            L.Log(message, clr);
        }
        else
        {
            Caller.SendChat(translationKey, formatting);
        }
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
    public void LogAction(EActionLogType type, string data)
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
        if (IsConsole)
            SendPlayerOnlyError();
        return IsConsole;
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
    public static ConsoleColor GetClosestConsoleColor(Color color)
    {
        int i = (color.r > 0.5f || color.g > 0.5f || color.b > 0.5f) ? 8 : 0;
        if (color.r > 0.25f) i |= 4;
        if (color.g > 0.25f) i |= 2;
        if (color.b > 0.25f) i |= 1;
        return (ConsoleColor)i;
    }
}
