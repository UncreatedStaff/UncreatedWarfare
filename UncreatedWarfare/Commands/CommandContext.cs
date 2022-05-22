using Rocket.API;
using Steamworks;
using System;
using System.Globalization;
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
    public static ConsoleColor GetClosestConsoleColor(Color color)
    {
        int i = (color.r > 0.5f || color.g > 0.5f || color.b > 0.5f) ? 8 : 0;
        if (color.r > 0.25f) i |= 4;
        if (color.g > 0.25f) i |= 2;
        if (color.b > 0.25f) i |= 1;
        return (ConsoleColor)i;
    }
}
