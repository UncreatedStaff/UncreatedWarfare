using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Uncreated.Framework;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Gamemodes.Flags.Invasion;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare;

public static class Localization
{
    public const string UNITY_RICH_TEXT_COLOR_BASE_START = "<color=#";
    public const string RICH_TEXT_COLOR_END = ">";
    public const string TMPRO_RICH_TEXT_COLOR_BASE = "<#";
    public const string RICH_TEXT_COLOR_CLOSE = "</color>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Colorize(string hex, string inner, TranslationFlags flags)
    {
        return (flags & TranslationFlags.SkipColorize) == TranslationFlags.SkipColorize ? inner : (((flags & TranslationFlags.TranslateWithUnityRichText) == TranslationFlags.TranslateWithUnityRichText)
            ? (UNITY_RICH_TEXT_COLOR_BASE_START + hex + RICH_TEXT_COLOR_END + inner + RICH_TEXT_COLOR_CLOSE)
            : (TMPRO_RICH_TEXT_COLOR_BASE + hex + RICH_TEXT_COLOR_END + inner + RICH_TEXT_COLOR_CLOSE));
    }
    public static string Translate(Translation translation, UCPlayer? player) =>
        Translate(translation, player is null ? 0 : player.Steam64);
    public static string Translate(Translation translation, ulong player)
    {
        if (player == 0 || !Data.Languages.TryGetValue(player, out string lang))
            lang = L.DEFAULT;
        return translation.Translate(lang);
    }
    public static string Translate(Translation translation, ulong player, out Color color)
    {
        if (player == 0 || !Data.Languages.TryGetValue(player, out string lang))
            lang = L.DEFAULT;
        return translation.Translate(lang, out color);
    }
    public static string Translate<T>(Translation<T> translation, UCPlayer? player, T arg)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.DEFAULT;
        return translation.Translate(lang, arg, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T1, T2>(Translation<T1, T2> translation, UCPlayer? player, T1 arg1, T2 arg2)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.DEFAULT;
        return translation.Translate(lang, arg1, arg2, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T1, T2, T3>(Translation<T1, T2, T3> translation, UCPlayer? player, T1 arg1, T2 arg2, T3 arg3)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.DEFAULT;
        return translation.Translate(lang, arg1, arg2, arg3, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T1, T2, T3, T4>(Translation<T1, T2, T3, T4> translation, UCPlayer? player, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.DEFAULT;
        return translation.Translate(lang, arg1, arg2, arg3, arg4, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T1, T2, T3, T4, T5>(Translation<T1, T2, T3, T4, T5> translation, UCPlayer? player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.DEFAULT;
        return translation.Translate(lang, arg1, arg2, arg3, arg4, arg5, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T1, T2, T3, T4, T5, T6>(Translation<T1, T2, T3, T4, T5, T6> translation, UCPlayer? player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.DEFAULT;
        return translation.Translate(lang, arg1, arg2, arg3, arg4, arg5, arg6, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T1, T2, T3, T4, T5, T6, T7>(Translation<T1, T2, T3, T4, T5, T6, T7> translation, UCPlayer? player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.DEFAULT;
        return translation.Translate(lang, arg1, arg2, arg3, arg4, arg5, arg6, arg7, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T1, T2, T3, T4, T5, T6, T7, T8>(Translation<T1, T2, T3, T4, T5, T6, T7, T8> translation, UCPlayer? player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.DEFAULT;
        return translation.Translate(lang, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Translation<T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, UCPlayer? player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.DEFAULT;
        return translation.Translate(lang, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Translation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> translation, UCPlayer? player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.DEFAULT;
        return translation.Translate(lang, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, player, player is null ? 0 : player.GetTeam());
    }
    public static string TranslateUnsafe(Translation translation, UCPlayer? player, object[] formatting)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.DEFAULT;
        return translation.TranslateUnsafe(lang, formatting, player, player is null ? 0 : player.GetTeam());
    }
    public static string TranslateUnsafe(Translation translation, ulong player, object[] formatting)
    {
        if (player == 0 || !Data.Languages.TryGetValue(player, out string lang))
            lang = L.DEFAULT;
        return translation.TranslateUnsafe(lang, formatting, null, 0);
    }
    public static string TranslateUnsafe(Translation translation, ulong player, out Color color, object[] formatting)
    {
        if (player == 0 || !Data.Languages.TryGetValue(player, out string lang))
            lang = L.DEFAULT;
        return translation.TranslateUnsafe(lang, out color, formatting, null, 0);
    }
    public static string GetTimeFromSeconds(this int seconds, ulong player)
    {
        if (seconds < 0)
            return T.TimePermanent.Translate(player);
        if (seconds == 0)
            seconds = 1;
        if (seconds < 60) // < 1 minute
            return seconds.ToString(Data.Locale) + ' ' + (seconds == 1 ? T.TimeSecondSingle : T.TimeSecondPlural).Translate(player);
        int val;
        int overflow;
        if (seconds < 3600) // < 1 hour
        {
            val = F.DivideRemainder(seconds, 60, out overflow);
            return $"{val} {(val == 1 ? T.TimeMinuteSingle : T.TimeMinutePlural).Translate(player)}" +
                   $"{(overflow == 0 ? string.Empty : $" {(T.TimeAnd).Translate(player)} {overflow} {(overflow == 1 ? T.TimeSecondSingle : T.TimeSecondPlural).Translate(player)}")}";
        }
        if (seconds < 86400) // < 1 day 
        {
            val = F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out overflow);
            return $"{val} {(val == 1 ? T.TimeHourSingle : T.TimeHourPlural).Translate(player)}" +
                   $"{(overflow == 0 ? string.Empty : $" {(T.TimeAnd).Translate(player)} {overflow} {(overflow == 1 ? T.TimeMinuteSingle : T.TimeMinutePlural).Translate(player)}")}";
        }
        if (seconds < 2565000) // < 1 month (29.6875 days) (365.25/12)
        {
            val = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out overflow);
            return $"{val} {(val == 1 ? T.TimeDaySingle : T.TimeDayPlural).Translate(player)}" +
                   $"{(overflow == 0 ? string.Empty : $" {(T.TimeAnd).Translate(player)} {overflow} {(overflow == 1 ? T.TimeHourSingle : T.TimeHourPlural).Translate(player)}")}";
        }
        if (seconds < 31536000) // < 1 year
        {
            val = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out _), 30.416m, out overflow);
            return $"{val} {(val == 1 ? T.TimeMonthSingle : T.TimeMonthPlural).Translate(player)}" +
                   $"{(overflow == 0 ? string.Empty : $" {(T.TimeAnd).Translate(player)} {overflow} {(overflow == 1 ? T.TimeDaySingle : T.TimeDayPlural).Translate(player)}")}";
        }
        // > 1 year

        val = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out _), 30.416m, out _), 12, out overflow);
        return $"{val} {(val == 1 ? T.TimeYearSingle : T.TimeYearPlural).Translate(player)}" +
               $"{(overflow == 0 ? string.Empty : $" {(T.TimeAnd).Translate(player)} {overflow} {(overflow == 1 ? T.TimeMonthSingle : T.TimeMonthPlural).Translate(player)}")}";
    }
    public static string GetTimeFromSeconds(this int seconds, IPlayer player)
    {
        if (seconds < 0)
            return T.TimePermanent.Translate(player);
        if (seconds == 0)
            seconds = 1;
        if (seconds < 60) // < 1 minute
            return seconds.ToString(Data.Locale) + ' ' + (seconds == 1 ? T.TimeSecondSingle : T.TimeSecondPlural).Translate(player);
        int val;
        int overflow;
        if (seconds < 3600) // < 1 hour
        {
            val = F.DivideRemainder(seconds, 60, out overflow);
            return $"{val} {(val == 1 ? T.TimeMinuteSingle : T.TimeMinutePlural).Translate(player)}" +
                   $"{(overflow == 0 ? string.Empty : $" {(T.TimeAnd).Translate(player)} {overflow} {(overflow == 1 ? T.TimeSecondSingle : T.TimeSecondPlural).Translate(player)}")}";
        }
        if (seconds < 86400) // < 1 day 
        {
            val = F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out overflow);
            return $"{val} {(val == 1 ? T.TimeHourSingle : T.TimeHourPlural).Translate(player)}" +
                   $"{(overflow == 0 ? string.Empty : $" {(T.TimeAnd).Translate(player)} {overflow} {(overflow == 1 ? T.TimeMinuteSingle : T.TimeMinutePlural).Translate(player)}")}";
        }
        if (seconds < 2565000) // < 1 month (29.6875 days) (365.25/12)
        {
            val = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out overflow);
            return $"{val} {(val == 1 ? T.TimeDaySingle : T.TimeDayPlural).Translate(player)}" +
                   $"{(overflow == 0 ? string.Empty : $" {(T.TimeAnd).Translate(player)} {overflow} {(overflow == 1 ? T.TimeHourSingle : T.TimeHourPlural).Translate(player)}")}";
        }
        if (seconds < 31536000) // < 1 year
        {
            val = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out _), 30.416m, out overflow);
            return $"{val} {(val == 1 ? T.TimeMonthSingle : T.TimeMonthPlural).Translate(player)}" +
                   $"{(overflow == 0 ? string.Empty : $" {(T.TimeAnd).Translate(player)} {overflow} {(overflow == 1 ? T.TimeDaySingle : T.TimeDayPlural).Translate(player)}")}";
        }
        // > 1 year

        val = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out _), 30.416m, out _), 12, out overflow);
        return $"{val} {(val == 1 ? T.TimeYearSingle : T.TimeYearPlural).Translate(player)}" +
               $"{(overflow == 0 ? string.Empty : $" {(T.TimeAnd).Translate(player)} {overflow} {(overflow == 1 ? T.TimeMonthSingle : T.TimeMonthPlural).Translate(player)}")}";
    }
    public static string GetTimeFromSeconds(this int seconds, string language)
    {
        if (seconds < 0)
            return T.TimePermanent.Translate(language);
        if (seconds == 0)
            seconds = 1;
        if (seconds < 60) // < 1 minute
            return seconds.ToString(Data.Locale) + ' ' + (seconds == 1 ? T.TimeSecondSingle : T.TimeSecondPlural).Translate(language);
        int val;
        int overflow;
        if (seconds < 3600) // < 1 hour
        {
            val = F.DivideRemainder(seconds, 60, out overflow);
            return $"{val} {(val == 1 ? T.TimeMinuteSingle : T.TimeMinutePlural).Translate(language)}" +
                   $"{(overflow == 0 ? string.Empty : $" {(T.TimeAnd).Translate(language)} {overflow} {(overflow == 1 ? T.TimeSecondSingle : T.TimeSecondPlural).Translate(language)}")}";
        }
        if (seconds < 86400) // < 1 day 
        {
            val = F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out overflow);
            return $"{val} {(val == 1 ? T.TimeHourSingle : T.TimeHourPlural).Translate(language)}" +
                   $"{(overflow == 0 ? string.Empty : $" {(T.TimeAnd).Translate(language)} {overflow} {(overflow == 1 ? T.TimeMinuteSingle : T.TimeMinutePlural).Translate(language)}")}";
        }
        if (seconds < 2565000) // < 1 month (29.6875 days) (365.25/12)
        {
            val = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out overflow);
            return $"{val} {(val == 1 ? T.TimeDaySingle : T.TimeDayPlural).Translate(language)}" +
                   $"{(overflow == 0 ? string.Empty : $" {(T.TimeAnd).Translate(language)} {overflow} {(overflow == 1 ? T.TimeHourSingle : T.TimeHourPlural).Translate(language)}")}";
        }
        if (seconds < 31536000) // < 1 year
        {
            val = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out _), 30.416m, out overflow);
            return $"{val} {(val == 1 ? T.TimeMonthSingle : T.TimeMonthPlural).Translate(language)}" +
                   $"{(overflow == 0 ? string.Empty : $" {(T.TimeAnd).Translate(language)} {overflow} {(overflow == 1 ? T.TimeDaySingle : T.TimeDayPlural).Translate(language)}")}";
        }
        // > 1 year

        val = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out _), 30.416m, out _), 12, out overflow);
        return $"{val} {(val == 1 ? T.TimeYearSingle : T.TimeYearPlural).Translate(language)}" +
               $"{(overflow == 0 ? string.Empty : $" {(T.TimeAnd).Translate(language)} {overflow} {(overflow == 1 ? T.TimeMonthSingle : T.TimeMonthPlural).Translate(language)}")}";
    }
    public static string GetTimeFromMinutes(this int minutes, ulong player) => GetTimeFromSeconds(minutes * 60, player);
    public static string GetTimeFromMinutes(this int minutes, IPlayer player) => GetTimeFromSeconds(minutes * 60, player);
    public static string GetTimeFromMinutes(this int minutes, string language) => GetTimeFromSeconds(minutes * 60, language);
    public static string TranslateSign(string key, string language, UCPlayer ucplayer, bool important = false)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        try
        {
            if (key == null) return string.Empty;
            if (!key.StartsWith("sign_")) return key;
            string key2 = key.Substring(5);

            if (key2.StartsWith("loadout_"))
            {
                return TranslateLoadoutSign(key2, language, ucplayer);
            }
            else if (KitManager.KitExists(key2, out Kit kit))
            {
                return TranslateKitSign(language, kit, ucplayer);
            }
            else
            {
                Translation? tr = Translation.FromSignId(key2);
                if (tr != null) return tr.Translate(language);

                return Translation.FromSignId(key)?.Translate(language) ?? key;
            }
        }
        catch (Exception ex)
        {
            L.LogError("Error translating sign: ");
            L.LogError(ex);
            return ex.GetType().Name;
        }
    }
    public static string TranslateLoadoutSign(string key, string language, UCPlayer ucplayer)
    {
        if (ucplayer != null && key.Length > 8 && ushort.TryParse(key.Substring(8), System.Globalization.NumberStyles.Any, Data.Locale, out ushort loadoutid))
        {
            ulong team = ucplayer.GetTeam();
            List<Kit> loadouts = KitManager.GetKitsWhere(k => k.IsLoadout && k.Team == team && KitManager.HasAccessFast(k, ucplayer));
            loadouts.Sort((k1, k2) => k1.Name.CompareTo(k2.Name));
            if (loadouts.Count > 0)
            {
                if (loadoutid > 0 && loadoutid <= loadouts.Count)
                {
                    Kit kit = loadouts[loadoutid - 1];

                    string name;
                    bool keepline = false;
                    if (!ucplayer.OnDuty())
                    {
                        if (!kit.SignTexts.TryGetValue(language, out name))
                            if (!kit.SignTexts.TryGetValue(L.DEFAULT, out name))
                                if (kit.SignTexts.Count > 0)
                                    name = kit.SignTexts.First().Value;
                                else
                                    name = kit.Name;
                        for (int i = 0; i < name.Length; i++)
                        {
                            char @char = name[i];
                            if (@char == '\n')
                            {
                                keepline = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        name = kit.Name;
                        if (name.Length > 18 && ulong.TryParse(name.Substring(0, 17), System.Globalization.NumberStyles.Any, Data.Locale, out ulong id) && OffenseManager.IsValidSteam64ID(id) && id == ucplayer.Steam64)
                        {
                            name = "PL #" + loadoutid.ToString(Data.Locale);
                        }
                    }
                    name = "<b>" + name.ToUpper().ColorizeTMPro(UCWarfare.GetColorHex("kit_public_header"), true) + "</b>";
                    string cost = "<sub>" + T.LoadoutName.Translate(language, loadoutid) + "</sub>";
                    if (!keepline) cost = "\n" + cost;

                    string playercount = string.Empty;

                    if (kit.TeamLimit >= 1f || kit.TeamLimit <= 0f)
                    {
                        playercount = T.KitUnlimited.Translate(language);
                    }
                    else if (kit.IsClassLimited(out int total, out int allowed, kit.Team > 0 && kit.Team < 3 ? kit.Team : team, true))
                    {
                        playercount = T.KitPlayerCount.Translate(language, total, allowed).ColorizeTMPro(UCWarfare.GetColorHex("kit_player_counts_unavailable"), true);
                    }
                    else
                    {
                        playercount = T.KitPlayerCount.Translate(language, total, allowed).ColorizeTMPro(UCWarfare.GetColorHex("kit_player_counts_available"), true);
                    }

                    string weapons = kit.Weapons ?? string.Empty;

                    if (weapons.Length > 0)
                    {
                        weapons = weapons.ToUpper().ColorizeTMPro(UCWarfare.GetColorHex("kit_weapon_list"), true);
                        return
                            name + "\n" +
                            cost + "\n" +
                            weapons + "\n" +
                            playercount;
                    }

                    return
                        name + "\n\n" +
                        cost + "\n" +
                        playercount;
                }
            }

            return
                "<b>" + T.LoadoutName.Translate(language, loadoutid) + "</b>\n\n\n\n" +
                T.KitPremiumCost.Translate(language, UCWarfare.Config.LoadoutCost)
                    .ColorizeTMPro(UCWarfare.GetColorHex("kit_level_dollars"), true);
        }
        return key;
    }
    public static string TranslateKitSign(string language, Kit kit, UCPlayer ucplayer)
    {
        bool keepline = false;
        ulong team = ucplayer.GetTeam();
        string name;
        if (!ucplayer.OnDuty() && kit.SignTexts != null)
        {
            if (!kit.SignTexts.TryGetValue(language, out name) && !kit.SignTexts.TryGetValue(L.DEFAULT, out name))
            {
                if (kit.SignTexts.Count > 0)
                    name = kit.SignTexts.First().Value;
                else
                    name = kit.Name;
            }

            for (int i = 0; i < name.Length; i++)
            {
                char @char = name[i];
                if (@char == '\n')
                {
                    keepline = true;
                    break;
                }
            }
        }
        else
        {
            name = kit.Name;
        }
        name = "<b>" + name.ToUpper().ColorizeTMPro(UCWarfare.GetColorHex(kit.SquadLevel == ESquadLevel.COMMANDER ? "kit_public_commander_header" : "kit_public_header"), true) + "</b>";
        string weapons = kit.Weapons ?? string.Empty;
        if (weapons.Length > 0)
            weapons = "<b>" + weapons.ToUpper().ColorizeTMPro(UCWarfare.GetColorHex("kit_weapon_list"), true) + "</b>";
        string cost = string.Empty;
        string playercount;
        if (kit.SquadLevel == ESquadLevel.COMMANDER && SquadManager.Loaded)
        {
            UCPlayer? c = SquadManager.Singleton.Commanders.GetCommander(team);
            if (c != null)
            {
                if (c.Steam64 != ucplayer.Steam64)
                    cost = T.KitCommanderTaken.Translate(language, c);
                else
                    cost = T.KitCommanderTakenByViewer.Translate(language);
                goto n;
            }
        }
        if (kit.IsPremium && (kit.PremiumCost > 0 || kit.PremiumCost == -1))
        {
            if (KitManager.HasAccessFast(kit, ucplayer))
                cost = T.KitPremiumOwned.Translate(language);
            else if (kit.PremiumCost == -1)
                cost = T.KitExclusive.Translate(language);
            else
                cost = T.KitPremiumCost.Translate(language, kit.PremiumCost);
        }
        else if (kit.UnlockRequirements != null && kit.UnlockRequirements.Length != 0)
        {
            for (int i = 0; i < kit.UnlockRequirements.Length; i++)
            {
                BaseUnlockRequirement req = kit.UnlockRequirements[i];
                if (req.CanAccess(ucplayer)) continue;
                cost = req.GetSignText(ucplayer);
                break;
            }
        }
        else if (kit.CreditCost > 0)
        {
            if (KitManager.HasAccessFast(kit, ucplayer))
                cost = T.KitPremiumOwned.Translate(language);
            else
                cost = T.KitCreditCost.Translate(language, kit.CreditCost);
        }
        else cost = T.KitFree.Translate(language);
    n:
        if (cost == string.Empty && kit.CreditCost > 0)
        {
            if (ucplayer != null)
            {
                if (!KitManager.HasAccessFast(kit, ucplayer))
                    cost = T.KitCreditCost.Translate(language, kit.CreditCost);
                else
                    cost = T.KitPremiumOwned.Translate(language);
            }
        }

        if (!keepline) cost = "\n" + cost;
        if (kit.TeamLimit >= 1f || kit.TeamLimit <= 0f)
        {
            playercount = T.KitUnlimited.Translate(language);
        }
        else if (kit.IsLimited(out int total, out int allowed, kit.Team > 0 && kit.Team < 3 ? kit.Team : team, true))
        {
            playercount = T.KitPlayerCount.Translate(language, total, allowed).ColorizeTMPro(UCWarfare.GetColorHex("kit_player_counts_unavailable"), true);
        }
        else
        {
            playercount = T.KitPlayerCount.Translate(language, total, allowed).ColorizeTMPro(UCWarfare.GetColorHex("kit_player_counts_available"), true);
        }
        if (weapons.Length == 0)
        {
            return
                name + "\n\n" +
                cost + "\n" +
                playercount;
        }
        return
            name + "\n" +
            cost + "\n" +
            weapons + "\n" +
            playercount;
    }
    public static string TranslateSign(string key, UCPlayer player, bool important = true)
    {
        if (player == null) return string.Empty;
        if (!Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.DEFAULT;
        return TranslateSign(key, lang, player, important);
    }

    private static readonly Guid F15 = new Guid("423d31c55cf84396914be9175ea70d0c");
    public static string TranslateVBS(Vehicles.VehicleSpawn spawn, VehicleData data, string language, ulong team)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        VehicleBayComponent comp;
        if (spawn.StructureType == Structures.EStructType.STRUCTURE)
            if (spawn.StructureDrop != null)
                comp = spawn.StructureDrop.model.gameObject.GetComponent<VehicleBayComponent>();
            else
                return spawn.VehicleGuid.ToString("N");
        else if (spawn.BarricadeDrop != null)
            comp = spawn.BarricadeDrop.model.gameObject.GetComponent<VehicleBayComponent>();
        else return spawn.VehicleGuid.ToString("N");
        if (comp == null) return spawn.VehicleGuid.ToString("N");

        string unlock = string.Empty;
        if (data.UnlockLevel > 0)
            unlock += RankData.GetRankAbbreviation(data.UnlockLevel).Colorize("f0b589");
        if (data.CreditCost > 0)
        {
            if (unlock != string.Empty)
                unlock += "    ";

            unlock += $"<color=#b8ffc1>C</color> {data.CreditCost.ToString(Data.Locale)}";
        }

        string finalformat =
            $"{(spawn.VehicleGuid == F15 ? "F15-E" : (Assets.find(spawn.VehicleGuid) is VehicleAsset asset ? asset.vehicleName : spawn.VehicleGuid.ToString("N")))}\n" +
            $"<#{UCWarfare.GetColorHex("vbs_branch")}>{TranslateEnum(data.Branch, language)}</color>\n" +
            (data.TicketCost > 0 ? T.VBSTickets.Translate(language, data.TicketCost, null, team) : " ") + "\n" +
            unlock +
            $"{{0}}";

        finalformat = finalformat.Colorize("ffffff");
        if (team is not 1 and not 2)
            return finalformat;
        finalformat += "\n";
        if (comp.State == EVehicleBayState.DEAD) // vehicle is dead
        {
            float rem = data.RespawnTime - comp.DeadTime;
            return finalformat + T.VBSStateDead.Translate(language, Mathf.FloorToInt(rem / 60f), Mathf.FloorToInt(rem) % 60, null, team);
        }
        else if (comp.State == EVehicleBayState.IN_USE)
        {
            return finalformat + T.VBSStateActive.Translate(language, comp.CurrentLocation);
        }
        else if (comp.State == EVehicleBayState.IDLE)
        {
            float rem = data.RespawnTime - comp.IdleTime;
            return finalformat + T.VBSStateIdle.Translate(language, Mathf.FloorToInt(rem / 60f), Mathf.FloorToInt(rem) % 60, null, team);
        }
        else
        {
            if (data.IsDelayed(out Delay delay))
            {
                string? del = GetDelaySignText(in delay, language, team);
                if (del != null)
                    return finalformat + del;
            }
            return finalformat + T.VBSStateReady.Translate(language);
        }
    }
    public static string TranslateEnum<TEnum>(TEnum value, string language)
    {
        if (enumTranslations.TryGetValue(typeof(TEnum), out Dictionary<string, Dictionary<string, string>> t))
        {
            if (!t.TryGetValue(language, out Dictionary<string, string> v) &&
                (L.DEFAULT.Equals(language, StringComparison.Ordinal) ||
                 !t.TryGetValue(L.DEFAULT, out v)))
                v = t.Values.FirstOrDefault();
            string strRep = value!.ToString();
            if (v == null || !v.TryGetValue(strRep, out string v2))
                return strRep.ToProperCase();
            else return v2;
        }
        else return value!.ToString().ToProperCase();
    }
    public static string TranslateEnum<TEnum>(TEnum value, ulong player)
    {
        if (player != 0 && Data.Languages.TryGetValue(player, out string language))
            return TranslateEnum(value, language);
        else return TranslateEnum(value, L.DEFAULT);
    }
    private const string ENUM_NAME_PLACEHOLDER = "%NAME%";
    public static string TranslateEnumName(Type type, string language)
    {
        if (enumTranslations.TryGetValue(type, out Dictionary<string, Dictionary<string, string>> t))
        {
            if (!t.TryGetValue(language, out Dictionary<string, string> v) &&
                (L.DEFAULT.Equals(language, StringComparison.Ordinal) ||
                 !t.TryGetValue(L.DEFAULT, out v)))
                v = t.Values.FirstOrDefault();
            if (v == null || !v.TryGetValue(ENUM_NAME_PLACEHOLDER, out string v2))
                return ENUM_NAME_PLACEHOLDER.ToProperCase();
            else return v2;
        }
        else
        {
            string name = type.Name;
            if (name.Length > 1 && name[0] == 'E' && char.IsUpper(name[1]))
                name = name.Substring(1);
            return name;
        }
    }
    public static string TranslateEnumName<TEnum>(string language) where TEnum : struct, Enum => TranslateEnumName(typeof(TEnum), language);
    public static string TranslateEnumName<TEnum>(ulong player) where TEnum : struct, Enum
    {
        if (player != 0 && Data.Languages.TryGetValue(player, out string language))
            return TranslateEnumName<TEnum>(language);
        else return TranslateEnumName<TEnum>(L.DEFAULT);
    }
    public static string TranslateEnumName(Type type, ulong player)
    {
        if (player != 0 && Data.Languages.TryGetValue(player, out string language))
            return TranslateEnumName(type, language);
        else return TranslateEnumName(type, L.DEFAULT);
    }
    private static readonly Dictionary<Type, Dictionary<string, Dictionary<string, string>>> enumTranslations = new Dictionary<Type, Dictionary<string, Dictionary<string, string>>>();
    private static readonly string ENUM_TRANSLATION_FILE_NAME = "Enums" + Path.DirectorySeparatorChar;
    public static void ReadEnumTranslations(List<KeyValuePair<Type, string?>> extEnumTypes)
    {
        enumTranslations.Clear();
        string def = Path.Combine(Data.Paths.LangStorage, L.DEFAULT) + Path.DirectorySeparatorChar;
        if (!Directory.Exists(def))
            Directory.CreateDirectory(def);
        List<KeyValuePair<Type, List<string>>> defaultLangs = new List<KeyValuePair<Type, List<string>>>(32);
        DirectoryInfo info = new DirectoryInfo(Data.Paths.LangStorage);
        if (!info.Exists) info.Create();
        DirectoryInfo[] langDirs = info.GetDirectories("*", SearchOption.TopDirectoryOnly);
        for (int i = 0; i < langDirs.Length; ++i)
        {
            if (langDirs[i].Name.Equals(L.DEFAULT, StringComparison.Ordinal))
            {
                string p = Path.Combine(langDirs[i].FullName, ENUM_TRANSLATION_FILE_NAME);
                if (!Directory.Exists(p))
                    Directory.CreateDirectory(p);
            }
        }
        foreach (KeyValuePair<Type, TranslatableAttribute> enumType in Assembly.GetExecutingAssembly()
                     .GetTypes()
                     .Where(x => x.IsEnum)
                     .SelectMany(x => Attribute.GetCustomAttributes(x, typeof(TranslatableAttribute)).OfType<TranslatableAttribute>().Select(y => new KeyValuePair<Type, TranslatableAttribute>(x, y)))
                     .Where(t => t.Value != null)
                     .Concat(extEnumTypes
                         .Where(x => x.Key.IsEnum)
                         .Select(x => new KeyValuePair<Type, TranslatableAttribute>(x.Key, new TranslatableAttribute(x.Value)))))
        {
            if (enumTranslations.ContainsKey(enumType.Key)) continue;
            Dictionary<string, Dictionary<string, string>> k = new Dictionary<string, Dictionary<string, string>>();
            enumTranslations.Add(enumType.Key, k);
            string fn = Path.Combine(def, ENUM_TRANSLATION_FILE_NAME, enumType.Key.FullName + ".json");
            FieldInfo[] fields = enumType.Key.GetFields(BindingFlags.Public | BindingFlags.Static);
            if (!File.Exists(fn))
            {
                Dictionary<string, string> k2 = new Dictionary<string, string>(fields.Length + 1);
                WriteEnums(L.DEFAULT, fields, enumType.Key, enumType.Value, fn, k2, defaultLangs);
                k.Add(L.DEFAULT, k2);
            }
            else
            {
                GetOtherLangList(defaultLangs, fields, enumType);
            }
            for (int i = 0; i < langDirs.Length; ++i)
            {
                DirectoryInfo dir = langDirs[i];
                if (k.ContainsKey(dir.Name)) continue;
                fn = Path.Combine(dir.FullName, ENUM_TRANSLATION_FILE_NAME, enumType.Key.FullName + ".json");
                if (!File.Exists(fn)) continue;
                Dictionary<string, string> k2 = new Dictionary<string, string>(fields.Length + 1);
                using (FileStream stream = new FileStream(fn, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (stream.Length > int.MaxValue)
                    {
                        L.LogWarning("Enum file \"" + fn + "\" is too big to read.");
                        continue;
                    }
                    byte[] bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, bytes.Length);
                    Utf8JsonReader reader = new Utf8JsonReader(bytes, JsonEx.readerOptions);
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.PropertyName)
                        {
                            string? key = reader.GetString();
                            if (reader.Read() && key != null)
                            {
                                string? value = reader.GetString();
                                if (value != null)
                                    k2.Add(key, value);
                            }
                        }
                    }
                }
                List<string>? list = defaultLangs.FirstOrDefault(x => x.Key == enumType.Key).Value;
                if (list is not null)
                {
                    list.Remove(dir.Name);
                    if (list.Count == 0)
                        defaultLangs.RemoveAll(x => x.Key == enumType.Key);
                }
                k.Add(dir.Name, k2);
            }
        }
        for (int i = 0; i < defaultLangs.Count; ++i)
        {
            KeyValuePair<Type, List<string>> v = defaultLangs[i];
            if (enumTranslations.TryGetValue(v.Key, out Dictionary<string, Dictionary<string, string>> dict))
            {
                for (int j = 0; j < v.Value.Count; ++j)
                {
                    string lang = v.Value[j];
                    if (lang.Equals(L.DEFAULT, StringComparison.Ordinal))
                        continue;

                    string p = Path.Combine(Data.Paths.LangStorage, lang, ENUM_TRANSLATION_FILE_NAME) + Path.DirectorySeparatorChar;
                    if (!Directory.Exists(p))
                        Directory.CreateDirectory(p);
                    p = Path.Combine(p, v.Key.FullName + ".json");
                    FieldInfo[] fields = v.Key.GetFields(BindingFlags.Public | BindingFlags.Static);
                    if (!File.Exists(p))
                    {
                        Dictionary<string, string> k2 = new Dictionary<string, string>(fields.Length + 1);
                        WriteEnums(lang, fields, v.Key, null, p, k2, null);
                        if (!dict.TryGetValue(lang, out Dictionary<string, string> l2))
                            dict.Add(lang, k2);
                        else
                        {
                            foreach (KeyValuePair<string, string> pair in k2)
                            {
                                if (!l2.ContainsKey(pair.Key))
                                    l2.Add(pair.Key, pair.Value);
                            }
                        }
                    }
                }
            }
        }
    }
    private static void GetOtherLangList(List<KeyValuePair<Type, List<string>>> otherlangs, FieldInfo[] fields, KeyValuePair<Type, TranslatableAttribute> enumType)
    {
        for (int i = 0; i < fields.Length; ++i)
        {
            TranslatableAttribute[] tas = fields[i].GetCustomAttributes(typeof(TranslatableAttribute)).OfType<TranslatableAttribute>().ToArray();

            for (int j = 0; j < tas.Length; ++j)
            {
                TranslatableAttribute t = tas[j];
                if (t.Language is null || t.Language.Equals(L.DEFAULT, StringComparison.Ordinal))
                    continue;
                for (int k = 0; k < otherlangs.Count; ++k)
                {
                    KeyValuePair<Type, List<string>> kvp2 = otherlangs[k];
                    if (kvp2.Key == enumType.Key)
                        goto added;
                }
                otherlangs.Add(new KeyValuePair<Type, List<string>>(enumType.Key, new List<string>(1) { t.Language }));
            added:;
            }
        }
    }
    private static void WriteEnums(string language, FieldInfo[] fields, Type type, TranslatableAttribute? attr1, string fn, Dictionary<string, string> k2, List<KeyValuePair<Type, List<string>>>? otherlangs)
    {
        bool isDeafult = L.DEFAULT.Equals(language, StringComparison.Ordinal);
        using (FileStream stream = new FileStream(fn, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
            writer.WriteStartObject();
            string name;
            if (attr1 != null && attr1.Default != null && attr1.Language.Equals(language, StringComparison.Ordinal))
            {
                name = attr1.Default;
                writer.WritePropertyName(ENUM_NAME_PLACEHOLDER);
                writer.WriteStringValue(name);
                k2.Add(ENUM_NAME_PLACEHOLDER, name);
            }
            else if (Attribute.GetCustomAttributes(type, typeof(TranslatableAttribute))
                         .OfType<TranslatableAttribute>()
                         .FirstOrDefault(x => x.Language.Equals(language, StringComparison.Ordinal)) is
                     TranslatableAttribute attr2 && attr2.Default != null)
            {
                name = attr2.Default;
                writer.WritePropertyName(ENUM_NAME_PLACEHOLDER);
                writer.WriteStringValue(name);
                k2.Add(ENUM_NAME_PLACEHOLDER, name);
            }
            else if (isDeafult)
            {
                name = type.Name;
                if (name.Length > 1 && name[0] == 'E' && char.IsUpper(name[1]))
                    name = name.Substring(1);
                writer.WritePropertyName(ENUM_NAME_PLACEHOLDER);
                writer.WriteStringValue(name);
                k2.Add(ENUM_NAME_PLACEHOLDER, name);
            }

            for (int i = 0; i < fields.Length; ++i)
            {
                string k0 = fields[i].GetValue(null).ToString();
                string? k1 = null;
                TranslatableAttribute[] tas = fields[i].GetCustomAttributes(typeof(TranslatableAttribute)).OfType<TranslatableAttribute>().ToArray();
                if (tas.Length == 0)
                    k1 = isDeafult ? k0.ToProperCase() : null;
                else
                {
                    for (int j = 0; j < tas.Length; ++j)
                    {
                        TranslatableAttribute t = tas[j];
                        if (((t.Language is null && isDeafult) || (t.Language is not null && t.Language.Equals(language, StringComparison.Ordinal)) && (isDeafult || t.Default != null)))
                            k1 = t.Default ?? k0.ToProperCase();
                        else if (otherlangs is not null && t.Language != null)
                        {
                            for (int k = 0; k < otherlangs.Count; ++k)
                            {
                                KeyValuePair<Type, List<string>> kvp2 = otherlangs[k];
                                if (kvp2.Key == type)
                                {
                                    if (!kvp2.Value.Contains(t.Language))
                                        kvp2.Value.Add(t.Language);
                                    goto added;
                                }
                            }
                            otherlangs.Add(new KeyValuePair<Type, List<string>>(type, new List<string>(1) { t.Language }));
                        }
                    added:;
                    }
                    if (isDeafult)
                        k1 ??= k0.ToProperCase();
                }
                if (k1 is not null)
                {
                    k2.Add(k0, k1);
                    writer.WritePropertyName(k0);
                    writer.WriteStringValue(k1);
                }
            }
            writer.WriteEndObject();
            writer.Dispose();
        }
    }
    internal static string GetLang(ulong player) => Data.Languages.TryGetValue(player, out string lang) ? lang : L.DEFAULT;
    public static string? GetDelaySignText(in Delay delay, string language, ulong team)
    {
        if (delay.type == EDelayType.OUT_OF_STAGING)
        {
            return T.VBSDelayStaging.Translate(language);
        }
        else if (delay.type == EDelayType.TIME)
        {
            float timeLeft = delay.value - Data.Gamemode.SecondsSinceStart;
            return T.VBSDelayTime.Translate(language, Mathf.FloorToInt(timeLeft / 60f), Mathf.FloorToInt(timeLeft % 60), null, team);
        }
        else if (delay.type == EDelayType.FLAG || delay.type == EDelayType.FLAG_PERCENT)
        {
            if (Data.Is(out Invasion invasion))
            {
                int ct = delay.type == EDelayType.FLAG ? Mathf.RoundToInt(delay.value) : Mathf.FloorToInt(invasion.Rotation.Count * (delay.value / 100f));
                int ct2;
                if (team == 1)
                {
                    if (invasion.AttackingTeam == 1)
                        ct2 = ct - invasion.ObjectiveT1Index;
                    else
                        ct2 = ct - (invasion.Rotation.Count - invasion.ObjectiveT2Index - 1);
                }
                else if (team == 2)
                {
                    if (invasion.AttackingTeam == 2)
                        ct2 = ct - (invasion.Rotation.Count - invasion.ObjectiveT2Index - 1);
                    else
                        ct2 = ct - invasion.ObjectiveT1Index;
                }
                else ct2 = ct;
                int ind = ct - ct2;
                if (invasion.AttackingTeam == 2) ind = invasion.Rotation.Count - ind - 1;
                if (ct2 == 1 && invasion.Rotation.Count > 0 && ind < invasion.Rotation.Count)
                {
                    if (team == invasion.DefendingTeam)
                        return T.VBSDelayLoseFlag.Translate(language, invasion.Rotation[ind], null, team);
                    else
                        return T.VBSDelayCaptureFlag.Translate(language, invasion.Rotation[ind], null, team);
                }
                else if (team == invasion.DefendingTeam)
                    return T.VBSDelayLoseFlagMultiple.Translate(language, ct2, null, team);
                else
                    return T.VBSDelayCaptureFlagMultiple.Translate(language, ct2, null, team);
            }
            else if (Data.Is(out IFlagTeamObjectiveGamemode flags))
            {
                int ct = delay.type == EDelayType.FLAG ? Mathf.RoundToInt(delay.value) : Mathf.FloorToInt(flags.Rotation.Count * (delay.value / 100f));
                int ct2;
                if (team == 1)
                    ct2 = ct - flags.ObjectiveT1Index;
                else if (team == 2)
                    ct2 = ct - (flags.Rotation.Count - flags.ObjectiveT2Index - 1);
                else ct2 = ct;
                int ind = ct - ct2;
                if (team == 2) ind = flags.Rotation.Count - ind - 1;
                if (ct2 == 1 && flags.Rotation.Count > 0 && ind < flags.Rotation.Count)
                    return T.VBSDelayCaptureFlag.Translate(language, flags.Rotation[ind], null, team);
                else
                    return T.VBSDelayCaptureFlagMultiple.Translate(language, ct2, null, team);
            }
            else if (Data.Is(out IFlagRotation rot))
            {
                int ct = delay.type == EDelayType.FLAG ? Mathf.RoundToInt(delay.value) : Mathf.FloorToInt(flags.Rotation.Count * (delay.value / 100f));
                int ct2 = 0;
                for (int i = 0; i < rot.Rotation.Count; ++i)
                {
                    if (team == 0 ? rot.Rotation[i].HasBeenCapturedT1 | rot.Rotation[i].HasBeenCapturedT2 : (team == 1 ? rot.Rotation[i].HasBeenCapturedT1 : (team == 2 ? rot.Rotation[i].HasBeenCapturedT2 : false)))
                        ++ct2;
                }
                int ind = ct - ct2;
                if (ct2 == 1 && flags.Rotation.Count > 0 && ind < flags.Rotation.Count)
                    return T.VBSDelayCaptureFlag.Translate(language, flags.Rotation[ind]);
                else
                    return T.VBSDelayCaptureFlagMultiple.Translate(language, ct2);
            }
            else if (Data.Is(out Insurgency ins))
            {
                int ct = delay.type == EDelayType.FLAG ? Mathf.RoundToInt(delay.value) : Mathf.FloorToInt(ins.Caches.Count * (delay.value / 100f));
                int ct2;
                ct2 = ct - ins.CachesDestroyed;
                int ind = ct - ct2;
                if (ct2 == 1 && ins.Caches.Count > 0 && ind < ins.Caches.Count)
                {
                    if (team == ins.AttackingTeam)
                    {
                        if (ins.Caches[ind].IsDiscovered)
                            return T.VBSDelayAttackCache.Translate(language, ins.Caches[ind].Cache, null, team);
                        else
                            return T.VBSDelayAttackCacheUnknown.Translate(language);
                    }
                    else
                        if (ins.Caches[ind].IsActive)
                        return T.VBSDelayDefendCache.Translate(language, ins.Caches[ind].Cache, null, team);
                    else
                        return T.VBSDelayDefendCacheUnknown.Translate(language);
                }
                else
                {
                    if (team == ins.AttackingTeam)
                        return T.VBSDelayAttackCacheMultiple.Translate(language, ct2, null, team);
                    else
                        return T.VBSDelayDefendCacheMultiple.Translate(language, ct2, null, team);
                }
            }
        }
        return null;
    }
    public static void SendDelayRequestText(in Delay delay, UCPlayer player, ulong team, EDelayMode mode)
    {
        DelayResponses res = mode switch
        {
            EDelayMode.TRAITS => TraitDelayResponses,
            _ => VehicleDelayResponses,
        };
        if (delay.type == EDelayType.OUT_OF_STAGING &&
            (delay.gamemode is null ||
             (Data.Is(out Insurgency ins1) && delay.gamemode == "Insurgency" && team == ins1.AttackingTeam) ||
             (Data.Is(out Invasion inv2) && delay.gamemode == "Invasion" && team == inv2.AttackingTeam))
           )
        {
            player.SendChat(res.StagingDelay);
            return;
        }
        else if (delay.type == EDelayType.TIME)
        {
            float timeLeft = delay.value - Data.Gamemode.SecondsSinceStart;
            player.SendChat(res.TimeDelay, Mathf.RoundToInt(timeLeft).GetTimeFromSeconds(player.Steam64));
        }
        else if (delay.type == EDelayType.FLAG || delay.type == EDelayType.FLAG_PERCENT)
        {
            if (Data.Is(out Invasion invasion))
            {
                int ct = delay.type == EDelayType.FLAG ? Mathf.RoundToInt(delay.value) : Mathf.FloorToInt(invasion.Rotation.Count * (delay.value / 100f));
                int ct2;
                if (team == 1)
                {
                    if (invasion.AttackingTeam == 1)
                        ct2 = ct - invasion.ObjectiveT1Index;
                    else
                        ct2 = ct - (invasion.Rotation.Count - invasion.ObjectiveT2Index - 1);
                }
                else if (team == 2)
                {
                    if (invasion.AttackingTeam == 2)
                        ct2 = ct - (invasion.Rotation.Count - invasion.ObjectiveT2Index - 1);
                    else
                        ct2 = ct - invasion.ObjectiveT1Index;
                }
                else ct2 = ct;
                int ind = ct - ct2;
                if (invasion.AttackingTeam == 2) ind = invasion.Rotation.Count - ind - 1;
                if (ct2 == 1 && invasion.Rotation.Count > 0 && ind < invasion.Rotation.Count)
                {
                    if (team == invasion.AttackingTeam)
                        player.SendChat(res.FlagDelay1, invasion.Rotation[ind]);
                    else if (team == invasion.DefendingTeam)
                        player.SendChat(res.LoseFlagDelay1, invasion.Rotation[ind]);
                    else
                        player.SendChat(res.FlagDelayMultiple, ct2);
                }
                else if (team == invasion.DefendingTeam)
                    player.SendChat(res.LoseFlagDelayMultiple, ct2);
                else
                    player.SendChat(res.FlagDelayMultiple, ct2);
            }
            else if (Data.Is(out IFlagTeamObjectiveGamemode flags))
            {
                int ct = delay.type == EDelayType.FLAG ? Mathf.RoundToInt(delay.value) : Mathf.FloorToInt(flags.Rotation.Count * (delay.value / 100f));
                int ct2;
                if (team == 1)
                    ct2 = ct - flags.ObjectiveT1Index;
                else if (team == 2)
                    ct2 = ct - (flags.Rotation.Count - flags.ObjectiveT2Index - 1);
                else ct2 = ct;
                int ind = ct - ct2;
                if (team == 2) ind = flags.Rotation.Count - ind - 1;
                if (ct2 == 1 && flags.Rotation.Count > 0 && ind < flags.Rotation.Count)
                {
                    if (team == 1 || team == 2)
                        player.SendChat(res.FlagDelay1, flags.Rotation[ind]);
                    else
                        player.SendChat(res.FlagDelayMultiple, ct2);
                }
                else
                {
                    player.SendChat(res.FlagDelayMultiple, ct2);
                }
            }
            else if (Data.Is(out IFlagRotation rot))
            {
                int ct = delay.type == EDelayType.FLAG ? Mathf.RoundToInt(delay.value) : Mathf.FloorToInt(flags.Rotation.Count * (delay.value / 100f));
                int ct2 = 0;
                for (int i = 0; i < rot.Rotation.Count; ++i)
                {
                    if (team == 0 ? rot.Rotation[i].HasBeenCapturedT1 | rot.Rotation[i].HasBeenCapturedT2 : (team == 1 ? rot.Rotation[i].HasBeenCapturedT1 : (team == 2 ? rot.Rotation[i].HasBeenCapturedT2 : false)))
                        ++ct2;
                }
                int ind = ct - ct2;
                if (ct2 == 1 && flags.Rotation.Count > 0 && ind < flags.Rotation.Count)
                {
                    if (team == 1 || team == 2)
                        player.SendChat(res.FlagDelay1, flags.Rotation[ind]);
                    else
                        player.SendChat(res.FlagDelayMultiple, ct2);
                }
                else
                    player.SendChat(res.FlagDelayMultiple, ct2);
            }
            else if (Data.Is(out Insurgency ins))
            {
                int ct = delay.type == EDelayType.FLAG ? Mathf.RoundToInt(delay.value) : Mathf.FloorToInt(ins.Caches.Count * (delay.value / 100f));
                int ct2;
                ct2 = ct - ins.CachesDestroyed;
                int ind = ct - ct2;
                if (ct2 == 1 && ins.Caches.Count > 0 && ind < ins.Caches.Count)
                {
                    if (team == ins.AttackingTeam)
                    {
                        if (ins.Caches[ind].IsDiscovered)
                            player.SendChat(res.CacheDelayAtk1, ins.Caches[ind].Cache);
                        else
                            player.SendChat(res.CacheDelayAtkUndiscovered1);
                    }
                    else if (team == ins.DefendingTeam)
                        if (ins.Caches[ind].IsActive)
                            player.SendChat(res.CacheDelayDef1, ins.Caches[ind].Cache);
                        else
                            player.SendChat(res.CacheDelayDefUndiscovered1);
                    else
                        player.SendChat(res.CacheDelayMultipleAtk, ct2);
                }
                else
                {
                    if (team == ins.AttackingTeam)
                        player.SendChat(res.CacheDelayMultipleAtk, ct2);
                    else
                        player.SendChat(res.CacheDelayMultipleDef, ct2);
                }
            }
        }
        else
        {
            player.SendChat(res.UnknownDelay, delay.ToString());
        }
    }
    public enum EDelayMode
    {
        VEHICLE_BAYS,
        TRAITS
    }

    private static readonly DelayResponses VehicleDelayResponses = new DelayResponses(EDelayMode.VEHICLE_BAYS);
    private static readonly DelayResponses TraitDelayResponses = new DelayResponses(EDelayMode.TRAITS);
    private class DelayResponses
    {
        public readonly Translation<string> UnknownDelay;
        public readonly Translation<int> CacheDelayMultipleDef;
        public readonly Translation<int> CacheDelayMultipleAtk;
        public readonly Translation CacheDelayDefUndiscovered1;
        public readonly Translation CacheDelayAtkUndiscovered1;
        public readonly Translation<Components.Cache> CacheDelayDef1;
        public readonly Translation<Components.Cache> CacheDelayAtk1;
        public readonly Translation<int> FlagDelayMultiple;
        public readonly Translation<int> LoseFlagDelayMultiple;
        public readonly Translation<Gamemodes.Flags.Flag> FlagDelay1;
        public readonly Translation<Gamemodes.Flags.Flag> LoseFlagDelay1;
        public readonly Translation<string> TimeDelay;
        public readonly Translation StagingDelay;
        public DelayResponses(EDelayMode mode)
        {
            if (mode == EDelayMode.TRAITS)
            {
                UnknownDelay = T.RequestTraitUnknownDelay;
                CacheDelayMultipleDef = T.RequestTraitCacheDelayMultipleDef;
                CacheDelayMultipleAtk = T.RequestTraitCacheDelayMultipleAtk;
                CacheDelayDefUndiscovered1 = T.RequestTraitCacheDelayDefUndiscovered1;
                CacheDelayAtkUndiscovered1 = T.RequestTraitCacheDelayAtkUndiscovered1;
                CacheDelayDef1 = T.RequestTraitCacheDelayDef1;
                CacheDelayAtk1 = T.RequestTraitCacheDelayAtk1;
                FlagDelayMultiple = T.RequestTraitFlagDelayMultiple;
                LoseFlagDelayMultiple = T.RequestTraitLoseFlagDelayMultiple;
                FlagDelay1 = T.RequestTraitFlagDelay1;
                LoseFlagDelay1 = T.RequestTraitLoseFlagDelay1;
                TimeDelay = T.RequestTraitTimeDelay;
                StagingDelay = T.RequestTraitStagingDelay;
            }
            else
            {
                UnknownDelay = T.RequestVehicleUnknownDelay;
                CacheDelayMultipleDef = T.RequestVehicleCacheDelayMultipleDef;
                CacheDelayMultipleAtk = T.RequestVehicleCacheDelayMultipleAtk;
                CacheDelayDefUndiscovered1 = T.RequestVehicleCacheDelayDefUndiscovered1;
                CacheDelayAtkUndiscovered1 = T.RequestVehicleCacheDelayAtkUndiscovered1;
                CacheDelayDef1 = T.RequestVehicleCacheDelayDef1;
                CacheDelayAtk1 = T.RequestVehicleCacheDelayAtk1;
                FlagDelayMultiple = T.RequestVehicleFlagDelayMultiple;
                LoseFlagDelayMultiple = T.RequestVehicleLoseFlagDelayMultiple;
                FlagDelay1 = T.RequestVehicleFlagDelay1;
                LoseFlagDelay1 = T.RequestVehicleLoseFlagDelay1;
                TimeDelay = T.RequestVehicleTimeDelay;
                StagingDelay = T.RequestVehicleStagingDelay;
            }
        }
    }
    [Obsolete]
    private class LanguageSetEnumerator : IEnumerable<LanguageSet>
    {
        public readonly LanguageSet[] Sets;
        public LanguageSetEnumerator(LanguageSet[] sets)
        {
            Sets = sets;
        }
        public IEnumerator<LanguageSet> GetEnumerator() => ((IEnumerable<LanguageSet>)Sets).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Sets.GetEnumerator();
    }
}
/// <summary>Disposing calls <see cref="Reset"/>.</summary>
public struct LanguageSet : IEnumerator<UCPlayer>
{
    public readonly string Language;
    public ulong Team = 0;
    public readonly List<UCPlayer> Players;
    private int index;
    /// <summary>Use <see cref="MoveNext"/> to enumerate through the players and <seealso cref="Reset"/> to reset it.</summary>
    public UCPlayer Next;
    UCPlayer IEnumerator<UCPlayer>.Current => Next;
    object IEnumerator.Current => Next;
    public LanguageSet(UCPlayer player)
    {
        if (!Data.Languages.TryGetValue(player.Steam64, out Language))
            Language = L.DEFAULT;
        Players = new List<UCPlayer>(1) { player };
        index = -1;
        Next = null!;
        Team = player.GetTeam();
    }
    public LanguageSet(string lang)
    {
        this.Language = lang;
        this.Players = new List<UCPlayer>(lang == L.DEFAULT ? Provider.clients.Count : 2);
        this.index = -1;
        this.Next = null!;
    }
    public LanguageSet(string lang, UCPlayer first)
    {
        this.Language = lang;
        this.Players = new List<UCPlayer>(lang == L.DEFAULT ? Provider.clients.Count : 2) { first };
        this.index = -1;
        this.Next = null!;
    }
    public void Add(UCPlayer pl) => this.Players.Add(pl);
    /// <summary>Use <see cref="MoveNext"/> to enumerate through the players and <seealso cref="Reset"/> to reset it.</summary>
    public bool MoveNext()
    {
        if (index < this.Players.Count - 1 && index > -2)
        {
            Next = this.Players[++index];
            return true;
        }
        else
            return false;
    }
    /// <summary>Use <see cref="MoveNext"/> to enumerate through the players and <seealso cref="Reset"/> to reset it.</summary>
    public void Reset()
    {
        Next = null!;
        index = -1;
    }
    public void Dispose() => Reset();
    public override string ToString()
    {
        return index.ToString(Data.Locale) + "   " + string.Join(", ", Players.Select(x => x == null ? "null" : x.CharacterName)) + "   Current: " + (Next == null ? "null" : Next.CharacterName);
    }
    private class LanguageSetEnumerator : IEnumerable<LanguageSet>
    {
        public readonly LanguageSet[] Sets;
        public LanguageSetEnumerator(LanguageSet[] sets)
        {
            Sets = sets;
        }
        public IEnumerator<LanguageSet> GetEnumerator() => ((IEnumerable<LanguageSet>)Sets).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Sets.GetEnumerator();
    }

    private static readonly List<LanguageSet> languages = new List<LanguageSet>(JSONMethods.DefaultLanguageAliasSets.Count);
    public static IEnumerable<LanguageSet> All()
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> InRegions(byte x, byte y, byte regionDistance)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (!Regions.checkArea(x, y, pl.Player.movement.region_x, pl.Player.movement.region_y, regionDistance)) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> InRegionsByTeam(byte x, byte y, byte regionDistance)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (!Regions.checkArea(x, y, pl.Player.movement.region_x, pl.Player.movement.region_y, regionDistance)) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                ulong team = pl.GetTeam();
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    LanguageSet l = languages[i2];
                    if (l.Team == team && l.Language.Equals(lang, StringComparison.Ordinal))
                    {
                        l.Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl) { Team = team });
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> All(IEnumerator<SteamPlayer> players)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            while (players.MoveNext())
            {
                UCPlayer? pl = UCPlayer.FromSteamPlayer(players.Current);
                if (pl == null) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
            }
            players.Dispose();
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> AllBut(params ulong[] exclude)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                for (int j = 0; j < exclude.Length; j++)
                    if (pl.Steam64 == exclude[j]) goto next;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
                next:;
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> AllBut(ulong exclude)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (pl.Steam64 == exclude) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> All(IEnumerator<Player> players)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            while (players.MoveNext())
            {
                UCPlayer? pl = UCPlayer.FromPlayer(players.Current);
                if (pl == null) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
            }
            players.Dispose();
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> OfPermission(EAdminType type, PermissionComparison comparison = PermissionComparison.AtLeast)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (!pl.PermissionLevel.IsOfPermission(type, comparison)) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> All(IEnumerator<UCPlayer> players)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            while (players.MoveNext())
            {
                UCPlayer pl = players.Current;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
            }
            players.Dispose();
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> OnTeam(ulong team)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (pl.GetTeam() != team) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl) { Team = team });
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> InSquad(Squads.Squad squad)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (pl.Squad != squad) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl) { Team = squad.Team });
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> Where(Predicate<UCPlayer> selector)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (!selector(pl)) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
}

[AttributeUsage(AttributeTargets.Enum | AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
public sealed class TranslatableAttribute : Attribute
{
    private readonly string? _default;
    private readonly string _language;

    public TranslatableAttribute()
    {
        _default = null;
        _language = L.DEFAULT;
    }
    public TranslatableAttribute(string? @default)
    {
        _default = @default;
        _language = L.DEFAULT;
    }
    public TranslatableAttribute(string language, string value)
    {
        _default = value;
        _language = language;
    }

    public string Language => _language;
    public string? Default => _default;
}
