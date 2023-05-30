﻿using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Steamworks;
using Uncreated.Framework;
using Uncreated.Json;
using Uncreated.SQL;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes.Flags.Invasion;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare;

public static class Localization
{
    private const string EnumNamePlaceholder = "%NAME%";

    private static readonly string EnumTranslationFileName = "Enums" + Path.DirectorySeparatorChar;
    private static readonly Dictionary<Type, Dictionary<string, Dictionary<string, string>>> EnumTranslations = new Dictionary<Type, Dictionary<string, Dictionary<string, string>>>(16);
    private static readonly Dictionary<Type, Dictionary<string, List<TranslatableAttribute>>> EnumTranslationAttributes = new Dictionary<Type, Dictionary<string, List<TranslatableAttribute>>>(16);

    public const string UnityRichTextColorBaseStart = "<color=#";
    public const string RichTextColorEnd = ">";
    public const string TMProRichTextColorBase = "<#";
    public const string RichTextColorClosingTag = "</color>";
    private static LanguageAliasSet? _defaultSet;
    public static LanguageAliasSet DefaultSet => _defaultSet ??= FindLanguageSet(L.Default, true, true) ?? throw new Exception("Unknown default language alias set (" + L.Default + ").");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Colorize(string hex, string inner, TranslationFlags flags)
    {
        return (flags & TranslationFlags.SkipColorize) == TranslationFlags.SkipColorize
            ? inner
            : (
                ((flags & TranslationFlags.TranslateWithUnityRichText) == TranslationFlags.TranslateWithUnityRichText)
                ? (UnityRichTextColorBaseStart + hex + RichTextColorEnd + inner + RichTextColorClosingTag)
                : (TMProRichTextColorBase + hex + RichTextColorEnd + inner + RichTextColorClosingTag));
    }
    public static string Translate(this ITranslationArgument translatable, string language, ulong team, string? fmt = null, bool imgui = false)
    {
        TranslationFlags flags = TranslationFlags.ForChat;
        if (imgui)
            flags |= TranslationFlags.UseUnityRichText;
        switch (team)
        {
            case 1:
                flags |= TranslationFlags.Team1;
                break;
            case 2:
                flags |= TranslationFlags.Team2;
                break;
        }
        return translatable.Translate(language, fmt, null, GetLocale(language) as CultureInfo, ref flags);
    }
    public static string Translate(this ITranslationArgument translatable, UCPlayer player, string? fmt = null)
    {
        TranslationFlags flags = TranslationFlags.ForChat;
        if (player.Save.IMGUI)
            flags |= TranslationFlags.UseUnityRichText;
        switch (player.GetTeam())
        {
            case 1:
                flags |= TranslationFlags.Team1;
                break;
            case 2:
                flags |= TranslationFlags.Team2;
                break;
        }
        return translatable.Translate(player.Language, fmt, player, player.Culture, ref flags);
    }
    public static string Translate(this ITranslationArgument translatable, CommandInteraction ctx, string? fmt = null)
    {
        TranslationFlags flags = TranslationFlags.ForChat;
        if (ctx.IMGUI)
            flags |= TranslationFlags.UseUnityRichText;
        if (!ctx.IsConsole)
        {
            switch (ctx.Caller.GetTeam())
            {
                case 1:
                    flags |= TranslationFlags.Team1;
                    break;
                case 2:
                    flags |= TranslationFlags.Team2;
                    break;
            }
        }
        return translatable.Translate(ctx.Language, fmt, ctx.Caller, ctx.GetLocale() as CultureInfo, ref flags);
    }
    public static string Translate(Translation translation, UCPlayer? player) =>
        Translate(translation, player is null ? 0 : player.Steam64);
    public static string Translate(Translation translation, ulong player)
    {
        if (player == 0 || !Data.Languages.TryGetValue(player, out string lang))
            lang = L.Default;
        return translation.Translate(lang);
    }
    public static string Translate(Translation translation, ulong player, out Color color)
    {
        if (player == 0 || !Data.Languages.TryGetValue(player, out string lang))
            lang = L.Default;
        return translation.Translate(lang, out color);
    }
    public static string Translate<T>(Translation<T> translation, UCPlayer? player, T arg)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.Default;
        return translation.Translate(lang, arg, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T1, T2>(Translation<T1, T2> translation, UCPlayer? player, T1 arg1, T2 arg2)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.Default;
        return translation.Translate(lang, arg1, arg2, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T1, T2, T3>(Translation<T1, T2, T3> translation, UCPlayer? player, T1 arg1, T2 arg2, T3 arg3)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.Default;
        return translation.Translate(lang, arg1, arg2, arg3, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T1, T2, T3, T4>(Translation<T1, T2, T3, T4> translation, UCPlayer? player, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.Default;
        return translation.Translate(lang, arg1, arg2, arg3, arg4, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T1, T2, T3, T4, T5>(Translation<T1, T2, T3, T4, T5> translation, UCPlayer? player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.Default;
        return translation.Translate(lang, arg1, arg2, arg3, arg4, arg5, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T1, T2, T3, T4, T5, T6>(Translation<T1, T2, T3, T4, T5, T6> translation, UCPlayer? player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.Default;
        return translation.Translate(lang, arg1, arg2, arg3, arg4, arg5, arg6, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T1, T2, T3, T4, T5, T6, T7>(Translation<T1, T2, T3, T4, T5, T6, T7> translation, UCPlayer? player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.Default;
        return translation.Translate(lang, arg1, arg2, arg3, arg4, arg5, arg6, arg7, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T1, T2, T3, T4, T5, T6, T7, T8>(Translation<T1, T2, T3, T4, T5, T6, T7, T8> translation, UCPlayer? player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.Default;
        return translation.Translate(lang, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Translation<T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, UCPlayer? player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.Default;
        return translation.Translate(lang, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Translation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> translation, UCPlayer? player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.Default;
        return translation.Translate(lang, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, player, player is null ? 0 : player.GetTeam());
    }
    public static string TranslateUnsafe(Translation translation, UCPlayer? player, object[] formatting)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.Default;
        return translation.TranslateUnsafe(lang, formatting, player, player is null ? 0 : player.GetTeam());
    }
    public static string TranslateUnsafe(Translation translation, ulong player, object[] formatting)
    {
        if (player == 0 || !Data.Languages.TryGetValue(player, out string lang))
            lang = L.Default;
        return translation.TranslateUnsafe(lang, formatting, null, 0);
    }
    public static string TranslateUnsafe(Translation translation, ulong player, out Color color, object[] formatting)
    {
        if (player == 0 || !Data.Languages.TryGetValue(player, out string lang))
            lang = L.Default;
        return translation.TranslateUnsafe(lang, out color, formatting, null, 0);
    }
    public static string GetTimeFromSeconds(this int seconds, ulong player)
    {
        if (seconds < 0)
            return T.TimePermanent.Translate(player);
        if (seconds == 0)
            seconds = 1;
        if (seconds < 60) // < 1 minute
            return seconds.ToString(Data.LocalLocale) + ' ' + (seconds == 1 ? T.TimeSecondSingle : T.TimeSecondPlural).Translate(player);
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
            return seconds.ToString(Data.LocalLocale) + ' ' + (seconds == 1 ? T.TimeSecondSingle : T.TimeSecondPlural).Translate(player);
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
            return seconds.ToString(Data.LocalLocale) + ' ' + (seconds == 1 ? T.TimeSecondSingle : T.TimeSecondPlural).Translate(language);
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
    public static string TranslateLoadoutSign(byte loadoutId, string language, UCPlayer ucplayer)
    {
        UCPlayer.TryApplyViewLens(ref ucplayer);
        ulong team = ucplayer.GetTeam();
        if (loadoutId <= 0)
        {
            return "<#ff0000>INVALID LOADOUT</color>";
        }
        SqlItem<Kit>? proxy = KitManager.GetLoadoutQuick(ucplayer, loadoutId);
        Kit? kit = proxy?.Item;
        if (kit != null)
        {
            string name;
            bool keepline = false;
            if (!ucplayer.OnDuty())
            {
                name = kit.GetDisplayName(language, false);
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
                name = kit.Id + '\n' + "(" + (char)(loadoutId + 47) + ") " + kit.GetDisplayName(language, false);
                keepline = true;
            }
            name = "<b>" + name.ToUpper()
                .ColorizeTMPro(
                    UCWarfare.GetColorHex(KitManager.IsFavoritedQuick(proxy!.LastPrimaryKey, ucplayer) ? "kit_public_header_fav" : "kit_public_header")
                    , true) + "</b>";
            string cost = "<sub>" + T.LoadoutName.Translate(language, KitEx.GetLoadoutLetter(KitEx.ParseStandardLoadoutId(kit.Id))) + "</sub>";
            if (!keepline) cost = "\n" + cost;

            string playercount;
            if (kit.NeedsUpgrade)
            {
                playercount = T.KitLoadoutUpgrade.Translate(language);
            }
            else if (kit.NeedsSetup)
            {
                playercount = T.KitLoadoutSetup.Translate(language);
            }
            else if (kit.RequiresNitro)
            {
                if (KitManager.IsNitroBoostingQuick(ucplayer.Steam64))
                    playercount = T.KitNitroBoostOwned.Translate(language);
                else
                    playercount = T.KitNitroBoostNotOwned.Translate(language);
            }
            else if (kit.TeamLimit >= 1f || kit.TeamLimit <= 0f)
            {
                playercount = T.KitUnlimited.Translate(language);
            }
            else if (kit.IsClassLimited(out int total, out int allowed, team, true))
            {
                playercount = T.KitPlayerCount.Translate(language, total, allowed).ColorizeTMPro(UCWarfare.GetColorHex("kit_player_counts_unavailable"), true);
            }
            else
            {
                playercount = T.KitPlayerCount.Translate(language, total, allowed).ColorizeTMPro(UCWarfare.GetColorHex("kit_player_counts_available"), true);
            }

            string weapons = kit.WeaponText ?? string.Empty;

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

        return
            "<b>" + T.LoadoutName.Translate(language, "#" + loadoutId) + "</b>\n\n\n\n" +
            T.KitPremiumCost.Translate(language, UCWarfare.Config.LoadoutCost)
                .ColorizeTMPro(UCWarfare.GetColorHex("kit_level_dollars"), true);
    }
    public static string TranslateKitSign(string language, Kit kit, UCPlayer ucplayer)
    {
        KitManager? manager = KitManager.GetSingletonQuick();
        bool keepline = false;
        UCPlayer.TryApplyViewLens(ref ucplayer);
        ulong team = ucplayer.GetTeam();
        string name;
        if (!ucplayer.OnDuty())
        {
            name = kit.GetDisplayName(language, false);
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
            name = kit.Id + "\n" + kit.GetDisplayName(language, false);
            keepline = true;
        }
        name = "<b>" + name
            .ToUpper()
            .ColorizeTMPro(UCWarfare.GetColorHex(
                kit.SquadLevel == SquadLevel.Commander
                    ? "kit_public_commander_header"
                    : (KitManager.IsFavoritedQuick(kit.PrimaryKey, ucplayer)
                        ? "kit_public_header_fav"
                        : "kit_public_header")), true) + "</b>";
        string weapons = kit.WeaponText ?? string.Empty;
        if (weapons.Length > 0)
            weapons = "<b>" + weapons.ToUpper().ColorizeTMPro(UCWarfare.GetColorHex("kit_weapon_list"), true) + "</b>";
        string cost;
        string playercount;
        if (kit.SquadLevel == SquadLevel.Commander && SquadManager.Loaded)
        {
            UCPlayer? c = SquadManager.Singleton.Commanders.GetCommander(team);
            if (c != null)
            {
                cost = c.Steam64 != ucplayer.Steam64 ? T.KitCommanderTaken.Translate(language, c) : T.KitCommanderTakenByViewer.Translate(language);
                goto n;
            }
        }
        if (kit.RequiresNitro)
        {
            if (KitManager.IsNitroBoostingQuick(ucplayer.Steam64))
                cost = T.KitNitroBoostOwned.Translate(language);
            else
                cost = T.KitNitroBoostNotOwned.Translate(language);
            goto n;
        }
        if (kit.Type is KitType.Elite or KitType.Special)
        {
            if (manager != null && KitManager.HasAccessQuick(kit, ucplayer))
                cost = T.KitPremiumOwned.Translate(language);
            else if (kit.Type == KitType.Special)
                cost = T.KitExclusive.Translate(language);
            else
                cost = kit.PremiumCost <= 0m ? T.KitFree.Translate(language) : T.KitPremiumCost.Translate(language, kit.PremiumCost);
            goto n;
        }
        if (kit.UnlockRequirements != null && kit.UnlockRequirements.Length != 0)
        {
            for (int i = 0; i < kit.UnlockRequirements.Length; i++)
            {
                UnlockRequirement req = kit.UnlockRequirements[i];
                if (req.CanAccess(ucplayer)) continue;
                cost = req.GetSignText(ucplayer);
                goto n;
            }
        }
        if (kit.CreditCost > 0)
        {
            cost = KitManager.HasAccessQuick(kit, ucplayer) ? T.KitPremiumOwned.Translate(language) : T.KitCreditCost.Translate(language, kit.CreditCost);
        }
        else cost = T.KitFree.Translate(language);
        n:
        if (!keepline) cost = "\n" + cost;
        if (kit.TeamLimit >= 1f || kit.TeamLimit <= 0f)
        {
            playercount = T.KitUnlimited.Translate(language);
        }
        else if (kit.IsLimited(out int total, out int allowed, team, true))
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

    private static readonly Guid F15 = new Guid("423d31c55cf84396914be9175ea70d0c");
    public static string TranslateVBS(Vehicles.VehicleSpawn spawn, VehicleData data, string language, FactionInfo? team)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        VehicleBayComponent? comp = spawn.Structure?.Item?.Buildable?.Model?.GetComponent<VehicleBayComponent>();
        if (comp == null) return data.VehicleID.ToString("N");

        string unlock = string.Empty;
        if (data.UnlockLevel > 0)
            unlock += LevelData.GetRankAbbreviation(data.UnlockLevel).Colorize("f0b589");
        if (data.CreditCost > 0)
        {
            if (unlock != string.Empty)
                unlock += "    ";

            unlock += $"<color=#b8ffc1>C</color> {data.CreditCost.ToString(Data.LocalLocale)}";
        }

        ulong teamNum = TeamManager.GetTeamNumber(team);
        string finalformat =
            $"{(data.VehicleID == F15 ? "F15-E" : (Assets.find(data.VehicleID) is VehicleAsset asset ? asset.vehicleName : data.VehicleID.ToString("N")))}\n" +
            $"<#{UCWarfare.GetColorHex("vbs_branch")}>{TranslateEnum(data.Branch, language)}</color>\n" +
            (data.TicketCost > 0 ? T.VBSTickets.Translate(language, data.TicketCost, null, teamNum) : " ") + "\n" +
            unlock +
            "{0}";

        finalformat = finalformat.Colorize("ffffff") + "\n";
        if (comp.State is <= VehicleBayState.Unknown or VehicleBayState.NotInitialized)
            return finalformat;

        if (comp.State == VehicleBayState.Dead) // vehicle is dead
        {
            float rem = data.RespawnTime - comp.DeadTime;
            return finalformat + T.VBSStateDead.Translate(language, Mathf.FloorToInt(rem / 60f), Mathf.FloorToInt(rem) % 60, null, teamNum);
        }
        if (comp.State == VehicleBayState.InUse)
        {
            return finalformat + T.VBSStateActive.Translate(language, comp.CurrentLocation);
        }
        if (comp.State == VehicleBayState.Idle)
        {
            float rem = data.RespawnTime - comp.IdleTime;
            return finalformat + T.VBSStateIdle.Translate(language, Mathf.FloorToInt(rem / 60f), Mathf.FloorToInt(rem) % 60, null, teamNum);
        }
        if (data.IsDelayed(out Delay delay))
        {
            string? del = GetDelaySignText(in delay, language, teamNum);
            if (del != null)
                return finalformat + del;
        }
        return finalformat + T.VBSStateReady.Translate(language);
    }
    public static string TranslateEnum<TEnum>(TEnum value, string language)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        if (UCWarfare.IsLoaded && EnumTranslations.TryGetValue(typeof(TEnum), out Dictionary<string, Dictionary<string, string>> t))
        {
            if (!t.TryGetValue(language, out Dictionary<string, string>? v) &&
                (L.Default.Equals(language, StringComparison.Ordinal) ||
                 !t.TryGetValue(L.Default, out v)))
                v = t.Values.FirstOrDefault();
            string strRep = value.ToString();
            if (v == null || !v.TryGetValue(strRep, out string v2))
                return strRep.ToProperCase();
            return v2;
        }

        return EnumToStringDynamic(value);
    }
    private static string EnumNameToStringDynamic(Type type)
    {
        string name = type.Name;
        if (name.Length > 1 && name[0] == 'E' && char.IsUpper(name[1]))
            name = name.Substring(1);
        return name;
    }
    private static string EnumToStringDynamic(string name)
    {
        bool isAlreadyProperCase = false;
        for (int i = 0; i < name.Length; ++i)
        {
            if (char.IsLower(name[i]))
            {
                isAlreadyProperCase = true;
                break;
            }
        }

        if (!isAlreadyProperCase)
            name = name.ToProperCase();
        return name;
    }
    private static string EnumToStringDynamic<TEnum>(TEnum value) => EnumToStringDynamic(value!.ToString());
    public static string TranslateEnum<TEnum>(TEnum value, ulong player)
    {
        if (UCWarfare.IsLoaded && player != 0 && Data.Languages.TryGetValue(player, out string language))
            return TranslateEnum(value, language);
        return TranslateEnum(value, L.Default);
    }
    public static string TranslateEnumName(Type type, string language)
    {
        if (UCWarfare.IsLoaded && EnumTranslations.TryGetValue(type, out Dictionary<string, Dictionary<string, string>> t))
        {
            if (!t.TryGetValue(language, out Dictionary<string, string>? v) &&
                (L.Default.Equals(language, StringComparison.Ordinal) ||
                 !t.TryGetValue(L.Default, out v)))
                v = t.Values.FirstOrDefault();
            if (v == null || !v.TryGetValue(EnumNamePlaceholder, out string v2))
                return EnumNamePlaceholder.ToProperCase();
            return v2;
        }
        return EnumNameToStringDynamic(type);
    }
    public static string TranslateEnumName<TEnum>(string language) where TEnum : struct, Enum => TranslateEnumName(typeof(TEnum), language);
    public static string TranslateEnumName<TEnum>(ulong player) where TEnum : struct, Enum
    {
        if (UCWarfare.IsLoaded && player != 0 && Data.Languages.TryGetValue(player, out string language))
            return TranslateEnumName<TEnum>(language);
        return TranslateEnumName<TEnum>(L.Default);
    }
    public static string TranslateEnumName(Type type, ulong player)
    {
        if (UCWarfare.IsLoaded && player != 0 && Data.Languages.TryGetValue(player, out string language))
            return TranslateEnumName(type, language);
        return TranslateEnumName(type, L.Default);
    }
    public static void ReadEnumTranslations(List<KeyValuePair<Type, string?>> extEnumTypes)
    {
        EnumTranslations.Clear();
        string def = Path.Combine(Data.Paths.LangStorage, L.Default) + Path.DirectorySeparatorChar;
        if (!Directory.Exists(def))
            Directory.CreateDirectory(def);
        List<KeyValuePair<Type, List<string>>> defaultLangs = new List<KeyValuePair<Type, List<string>>>(32);
        DirectoryInfo info = new DirectoryInfo(Data.Paths.LangStorage);
        if (!info.Exists) info.Create();
        DirectoryInfo[] langDirs = info.GetDirectories("*", SearchOption.TopDirectoryOnly);
        for (int i = 0; i < langDirs.Length; ++i)
        {
            if (langDirs[i].Name.Equals(L.Default, StringComparison.Ordinal))
            {
                string p = Path.Combine(langDirs[i].FullName, EnumTranslationFileName);
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
            if (EnumTranslations.ContainsKey(enumType.Key)) continue;
            Dictionary<string, Dictionary<string, string>> k = new Dictionary<string, Dictionary<string, string>>();
            Dictionary<string, List<TranslatableAttribute>>? a = null;
            if (!EnumTranslationAttributes.TryGetValue(enumType.Key, out a))
            {
                a = new Dictionary<string, List<TranslatableAttribute>>(8) { { EnumNamePlaceholder, new List<TranslatableAttribute>(2) } };
                EnumTranslationAttributes.Add(enumType.Key, a);
            }
            else if (a.TryGetValue(EnumNamePlaceholder, out List<TranslatableAttribute> nameList))
            {
                if (!nameList.Exists(x => x.Language.Equals(enumType.Value.Language, StringComparison.OrdinalIgnoreCase)))
                {
                    if (enumType.Value.Language.IsDefault())
                        nameList.Insert(0, enumType.Value);
                    else
                        nameList.Add(enumType.Value);
                }
            }
            else a.Add(EnumNamePlaceholder, new List<TranslatableAttribute>(2) { enumType.Value });
            EnumTranslations.Add(enumType.Key, k);
            string fn = Path.Combine(def, EnumTranslationFileName, enumType.Key.FullName + ".json");
            FieldInfo[] fields = enumType.Key.GetFields(BindingFlags.Public | BindingFlags.Static);
            for (int i = 0; i < fields.Length; ++i)
            {
                if (Attribute.IsDefined(fields[i], typeof(JsonIgnoreAttribute)))
                    continue;
                string name = fields[i].GetValue(null).ToString();
                Attribute[] attrs = Attribute.GetCustomAttributes(fields[i]);
                if (attrs.Length == 0) continue;
                if (!a.TryGetValue(name, out List<TranslatableAttribute> list))
                {
                    list = new List<TranslatableAttribute>(2);
                    a.Add(name, list);
                }

                for (int j = 0; j < attrs.Length; ++j)
                {
                    if (attrs[j] is TranslatableAttribute t && !list.Exists(x => x.Language.Equals(t.Language, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (t.Language.IsDefault())
                            list.Insert(0, t);
                        else
                            list.Add(t);
                    }
                }
            }
            if (!File.Exists(fn))
            {
                Dictionary<string, string> k2 = new Dictionary<string, string>(fields.Length + 1);
                WriteEnums(L.Default, fields, enumType.Key, enumType.Value, fn, k2, defaultLangs);
                k.Add(L.Default, k2);
            }
            else
            {
                GetOtherLangList(defaultLangs, fields, enumType);
            }
            for (int i = 0; i < langDirs.Length; ++i)
            {
                DirectoryInfo dir = langDirs[i];
                if (k.ContainsKey(dir.Name)) continue;
                fn = Path.Combine(dir.FullName, EnumTranslationFileName, enumType.Key.FullName + ".json");
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
                    if (bytes.Length > 0)
                    {
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
            if (EnumTranslations.TryGetValue(v.Key, out Dictionary<string, Dictionary<string, string>> dict))
            {
                for (int j = 0; j < v.Value.Count; ++j)
                {
                    string lang = v.Value[j];
                    if (lang.Equals(L.Default, StringComparison.Ordinal))
                        continue;

                    string p = Path.Combine(Data.Paths.LangStorage, lang, EnumTranslationFileName) + Path.DirectorySeparatorChar;
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
                if (t.Language.Equals(L.Default, StringComparison.Ordinal))
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
    public static void WriteEnums(string language, string folder, bool writeMissing, bool excludeNonPriorities)
    {
        bool isDefault = (language ??= L.Default).IsDefault();
        F.CheckDir(folder, out _);
        foreach (KeyValuePair<Type, Dictionary<string, Dictionary<string, string>>> enumData in EnumTranslations)
        {
            if ((!enumData.Value.TryGetValue(language, out Dictionary<string, string> values) && (isDefault || !enumData.Value.TryGetValue(L.Default, out values))) || values == null)
                continue;
            string? mainDesc = null;
            if (EnumTranslationAttributes.TryGetValue(enumData.Key, out Dictionary<string, List<TranslatableAttribute>> attrs) &&
                attrs.TryGetValue(EnumNamePlaceholder, out List<TranslatableAttribute> nameAttrs))
            {
                if (excludeNonPriorities && nameAttrs.Exists(x => !x.IsPrioritizedTranslation))
                    continue;
                mainDesc = nameAttrs.Find(x => x.Language.IsDefault())?.Description ?? nameAttrs.Find(x => x.Description != null)?.Description;
            }
            Type type = enumData.Key;
            string fn = Path.Combine(folder, TranslateEnumName(type, L.Default).RemoveMany(false, Data.Paths.BadFileNameCharacters));
            string path = fn + ".json";
            int c = 1;
            while (File.Exists(path))
                path = fn + " " + (++c).ToString(CultureInfo.InvariantCulture) + ".json";

            using FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            string comment = "// " + type.AssemblyQualifiedName + Environment.NewLine + "// " + type.FullName + ".json" + Environment.NewLine;
            if (mainDesc != null)
                comment += "// " + mainDesc + Environment.NewLine;
            byte[] commentUtf8 = System.Text.Encoding.UTF8.GetBytes(comment);
            stream.Write(commentUtf8, 0, commentUtf8.Length);
            Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
            writer.WriteStartObject();
            if (!isDefault && writeMissing && enumData.Value.TryGetValue(L.Default, out Dictionary<string, string> defaultValues))
            {
                Dictionary<string, string> clone = new Dictionary<string, string>(values.Count);
                foreach (KeyValuePair<string, string> pair in values)
                    clone.Add(pair.Key, pair.Value);
                values = clone;
                foreach (KeyValuePair<string, string> pair in defaultValues)
                {
                    if (!values.ContainsKey(pair.Key))
                        values.Add(pair.Key, pair.Value);
                }
            }
            foreach (KeyValuePair<string, string> pair in values)
            {
                if (!pair.Key.Equals(EnumNamePlaceholder, StringComparison.OrdinalIgnoreCase) &&
                    EnumTranslationAttributes.TryGetValue(enumData.Key, out Dictionary<string, List<TranslatableAttribute>> attrs2) &&
                    attrs2.TryGetValue(pair.Key, out List<TranslatableAttribute> nameAttrs2))
                {
                    if (excludeNonPriorities && nameAttrs2.Exists(x => !x.IsPrioritizedTranslation))
                        continue;
                    if ((nameAttrs2.Find(x => x.Language.IsDefault())?.Description ??
                         nameAttrs2.Find(x => x.Description != null)?.Description) is { } desc)
                        writer.WriteCommentValue(desc);
                }
                writer.WritePropertyName(pair.Key);
                writer.WriteStringValue(pair.Value);
            }
            writer.WriteEndObject();
            writer.Dispose();
        }
    }
    private static void WriteEnums(string language, FieldInfo[] fields, Type type, TranslatableAttribute? attr1, string fn, Dictionary<string, string> k2, List<KeyValuePair<Type, List<string>>>? otherlangs)
    {
        bool isDefault = L.Default.Equals(language, StringComparison.Ordinal);
        using FileStream stream = new FileStream(fn, FileMode.Create, FileAccess.Write, FileShare.Read);
        Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
        writer.WriteStartObject();
        string name;
        if (attr1 != null && attr1.Default != null && attr1.Language.Equals(language, StringComparison.Ordinal))
        {
            name = attr1.Default;
            writer.WritePropertyName(EnumNamePlaceholder);
            writer.WriteStringValue(name);
            k2.Add(EnumNamePlaceholder, name);
        }
        else if (Attribute.GetCustomAttributes(type, typeof(TranslatableAttribute))
                     .OfType<TranslatableAttribute>()
                     .FirstOrDefault(x => x.Language.Equals(language, StringComparison.Ordinal)) is { Default: { } } attr2)
        {
            name = attr2.Default;
            writer.WritePropertyName(EnumNamePlaceholder);
            writer.WriteStringValue(name);
            k2.Add(EnumNamePlaceholder, name);
        }
        else if (isDefault)
        {
            name = EnumNameToStringDynamic(type);
            writer.WritePropertyName(EnumNamePlaceholder);
            writer.WriteStringValue(name);
            k2.Add(EnumNamePlaceholder, name);
        }

        for (int i = 0; i < fields.Length; ++i)
        {
            if (Attribute.IsDefined(fields[i], typeof(JsonIgnoreAttribute)))
                continue;
            string k0 = fields[i].GetValue(null).ToString();
            string? k1 = null;
            TranslatableAttribute[] tas = fields[i].GetCustomAttributes(typeof(TranslatableAttribute)).OfType<TranslatableAttribute>().ToArray();
            if (tas.Length == 0)
                k1 = isDefault ? EnumToStringDynamic(k0) : null;
            else
            {
                for (int j = 0; j < tas.Length; ++j)
                {
                    TranslatableAttribute t = tas[j];
                    if (((t.Language is null && isDefault) || (t.Language is not null && t.Language.Equals(language, StringComparison.Ordinal)) && (isDefault || t.Default != null)))
                        k1 = t.Default ?? EnumToStringDynamic(k0);
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
                if (isDefault)
                    k1 ??= EnumToStringDynamic(k0);
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
    internal static string GetLang(ulong player) => Data.Languages.TryGetValue(player, out string lang) ? lang : L.Default;
    internal static IFormatProvider GetLocale(CommandInteraction ctx) => ctx.IsConsole ? Data.AdminLocale : GetLocale(GetLang(ctx.CallerID));
    internal static IFormatProvider GetLocale(IPlayer player) => GetLocale(GetLang(player.Steam64));
    internal static IFormatProvider GetLocale(ulong player) => GetLocale(GetLang(player));
    internal static IFormatProvider GetLocale(string language)
    {
        return LanguageAliasSet.GetCultureInfo(language);
    }
    internal static LanguageAliasSet? FindLanguageSet(string language, bool keyOnly = false, bool exact = false)
    {
        if (language.Equals(L.Default, StringComparison.OrdinalIgnoreCase) && _defaultSet != null)
            return _defaultSet;
        LanguageAliasSet? set = null;
        for (int i = 0; i < Data.LanguageAliases.Count; ++i)
        {
            set = Data.LanguageAliases[i];
            if (set.key.Equals(language, StringComparison.OrdinalIgnoreCase))
                goto found;
        }
        if (!keyOnly)
        {
            for (int i = 0; i < Data.LanguageAliases.Count; ++i)
            {
                set = Data.LanguageAliases[i];
                if (set.display_name.Equals(language, StringComparison.OrdinalIgnoreCase))
                    goto found;
            }
            for (int i = 0; i < Data.LanguageAliases.Count; ++i)
            {
                set = Data.LanguageAliases[i];
                for (int j = 0; j < set.values.Length; ++j)
                {
                    if (set.values[j].Equals(language, StringComparison.OrdinalIgnoreCase))
                        goto found;
                }
            }
            if (!exact)
            {
                for (int i = 0; i < Data.LanguageAliases.Count; ++i)
                {
                    set = Data.LanguageAliases[i];
                    if (set.display_name.IndexOf(language, StringComparison.OrdinalIgnoreCase) != -1)
                        goto found;
                }
                for (int i = 0; i < Data.LanguageAliases.Count; ++i)
                {
                    set = Data.LanguageAliases[i];
                    for (int j = 0; j < set.values.Length; ++j)
                    {
                        if (set.values[j].IndexOf(language, StringComparison.OrdinalIgnoreCase) != -1)
                            goto found;
                    }
                }
            }

            set = null;
        }
        found:
        return set;
    }
    public static bool TryGetLangData(string language, out string langId, out IFormatProvider provider)
    {
        LanguageAliasSet? set = FindLanguageSet(language);
        if (set != null)
        {
            langId = set.key;
            provider = LanguageAliasSet.GetCultureInfo(langId);
            return true;
        }
        langId = language;
        provider = Data.LocalLocale;
        return false;
    }
    public static string? GetDelaySignText(in Delay delay, string language, ulong team)
    {
        if (delay.Type == DelayType.OutOfStaging)
        {
            return T.VBSDelayStaging.Translate(language);
        }
        else if (delay.Type == DelayType.Time)
        {
            float timeLeft = delay.Value - Data.Gamemode.SecondsSinceStart;
            return T.VBSDelayTime.Translate(language, Mathf.FloorToInt(timeLeft / 60f), Mathf.FloorToInt(timeLeft % 60), null, team);
        }
        else if (delay.Type == DelayType.Flag || delay.Type == DelayType.FlagPercentage)
        {
            if (Data.Is(out Invasion invasion))
            {
                int ct = delay.Type == DelayType.Flag ? Mathf.RoundToInt(delay.Value) : Mathf.FloorToInt(invasion.Rotation.Count * (delay.Value / 100f));
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
                int ct = delay.Type == DelayType.Flag ? Mathf.RoundToInt(delay.Value) : Mathf.FloorToInt(flags.Rotation.Count * (delay.Value / 100f));
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
                int ct = delay.Type == DelayType.Flag ? Mathf.RoundToInt(delay.Value) : Mathf.FloorToInt(flags.Rotation.Count * (delay.Value / 100f));
                int ct2 = 0;
                for (int i = 0; i < rot.Rotation.Count; ++i)
                {
                    if (team == 0 ? rot.Rotation[i].HasBeenCapturedT1 | rot.Rotation[i].HasBeenCapturedT2 : (team == 1 ? rot.Rotation[i].HasBeenCapturedT1 : (team == 2 && rot.Rotation[i].HasBeenCapturedT2)))
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
                int ct = delay.Type == DelayType.Flag ? Mathf.RoundToInt(delay.Value) : Mathf.FloorToInt(ins.Caches.Count * (delay.Value / 100f));
                int ct2 = ct - ins.CachesDestroyed;
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
    public static void SendDelayRequestText(in Delay delay, UCPlayer player, ulong team, DelayTarget target)
    {
        DelayResponses res = target switch
        {
            DelayTarget.Trait => TraitDelayResponses,
            _ => VehicleDelayResponses,
        };
        if (delay.Type == DelayType.OutOfStaging &&
            (delay.Gamemode is null ||
             (Data.Is(out Insurgency ins1) && delay.Gamemode == "Insurgency" && team == ins1.AttackingTeam) ||
             (Data.Is(out Invasion inv2) && delay.Gamemode == "Invasion" && team == inv2.AttackingTeam))
           )
        {
            player.SendChat(res.StagingDelay);
            return;
        }
        if (delay.Type == DelayType.Time)
        {
            float timeLeft = delay.Value - Data.Gamemode.SecondsSinceStart;
            player.SendChat(res.TimeDelay, Mathf.RoundToInt(timeLeft).GetTimeFromSeconds(player.Steam64));
        }
        else if (delay.Type is DelayType.Flag or DelayType.FlagPercentage)
        {
            if (Data.Is(out Invasion invasion))
            {
                int ct = delay.Type == DelayType.Flag ? Mathf.RoundToInt(delay.Value) : Mathf.FloorToInt(invasion.Rotation.Count * (delay.Value / 100f));
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
                int ct = delay.Type == DelayType.Flag ? Mathf.RoundToInt(delay.Value) : Mathf.FloorToInt(flags.Rotation.Count * (delay.Value / 100f));
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
                int ct = delay.Type == DelayType.Flag ? Mathf.RoundToInt(delay.Value) : Mathf.FloorToInt(flags.Rotation.Count * (delay.Value / 100f));
                int ct2 = 0;
                for (int i = 0; i < rot.Rotation.Count; ++i)
                {
                    if (team == 0 ? rot.Rotation[i].HasBeenCapturedT1 | rot.Rotation[i].HasBeenCapturedT2 : (team == 1 ? rot.Rotation[i].HasBeenCapturedT1 : (team == 2 && rot.Rotation[i].HasBeenCapturedT2)))
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
                int ct = delay.Type == DelayType.Flag ? Mathf.RoundToInt(delay.Value) : Mathf.FloorToInt(ins.Caches.Count * (delay.Value / 100f));
                int ct2 = ct - ins.CachesDestroyed;
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
                    player.SendChat(team == ins.AttackingTeam ? res.CacheDelayMultipleAtk : res.CacheDelayMultipleDef, ct2);
                }
            }
        }
        else
        {
            player.SendChat(res.UnknownDelay, delay.ToString());
        }
    }
    public enum DelayTarget
    {
        VehicleBay,
        Trait
    }

    private static readonly DelayResponses VehicleDelayResponses = new DelayResponses(DelayTarget.VehicleBay);
    private static readonly DelayResponses TraitDelayResponses = new DelayResponses(DelayTarget.Trait);
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
        public DelayResponses(DelayTarget target)
        {
            if (target == DelayTarget.Trait)
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
}
/// <summary>Disposing calls <see cref="Reset"/>.</summary>
public struct LanguageSet : IEnumerator<UCPlayer>
{
    public readonly string Language;
    public ulong Team = 0;
    public List<UCPlayer> Players { get; private set; }
    public readonly bool IMGUI;
    private int _index;
    /// <summary>Use <see cref="MoveNext"/> to enumerate through the players and <seealso cref="Reset"/> to reset it.</summary>
    public UCPlayer Next;
    UCPlayer IEnumerator<UCPlayer>.Current => Next;
    object IEnumerator.Current => Next;
    public LanguageSet(UCPlayer player)
    {
        if (!Data.Languages.TryGetValue(player.Steam64, out Language))
            Language = L.Default;
        Players = new List<UCPlayer>(1) { player };
        IMGUI = player.Save.IMGUI;
        _index = -1;
        Next = null!;
        Team = player.GetTeam();
    }
    public LanguageSet(string lang)
    {
        this.Language = lang;
        this.Players = new List<UCPlayer>(lang == L.Default ? Provider.clients.Count : 2);
        this._index = -1;
        this.Next = null!;
        IMGUI = false;
    }
    public LanguageSet(string lang, UCPlayer first)
    {
        this.Language = lang;
        this.Players = new List<UCPlayer>(lang == L.Default ? Provider.clients.Count : 2) { first };
        this._index = -1;
        this.Next = null!;
        IMGUI = first.Save.IMGUI;
    }
    public void Add(UCPlayer pl) => this.Players.Add(pl);
    /// <summary>Use <see cref="MoveNext"/> to enumerate through the players and <seealso cref="Reset"/> to reset it.</summary>
    public bool MoveNext()
    {
        if (_index < this.Players.Count - 1 && _index > -2)
        {
            Next = this.Players[++_index];
            return true;
        }
        else
            return false;
    }
    /// <summary>Use <see cref="MoveNext"/> to enumerate through the players and <seealso cref="Reset"/> to reset it.</summary>
    public void Reset()
    {
        Next = null!;
        _index = -1;
    }
    public void Dispose()
    {
        Reset();
        Players.Clear();
        Players = null!;
    }

    public override string ToString()
    {
        return _index.ToString(Data.AdminLocale) + "   " + string.Join(", ", Players.Select(x => x == null ? "null" : x.CharacterName)) + "   Current: " + (Next == null ? "null" : Next.CharacterName);
    }
    private class LanguageSetEnumerator : IEnumerable<LanguageSet>
    {
        public readonly LanguageSet[] Sets;
        public LanguageSetEnumerator(LanguageSet[] sets)
        {
            Sets = sets;
        }
        private readonly struct LanguageSetArrayEnumerator : IEnumerator<LanguageSet>
        {
            private readonly IEnumerator<LanguageSet> _arrayEnumerator;
            private readonly LanguageSetEnumerator _set;

            public LanguageSetArrayEnumerator(LanguageSetEnumerator sets)
            {
                _arrayEnumerator = (sets.Sets as IEnumerable<LanguageSet>).GetEnumerator();
                _set = sets;
            }

            void IDisposable.Dispose()
            {
                for (int i = 0; i < _set.Sets.Length; ++i)
                {
                    ref LanguageSet set = ref _set.Sets[i];
                    set.Dispose();
                }
                _arrayEnumerator.Dispose();
            }

            bool IEnumerator.MoveNext() => _arrayEnumerator.MoveNext();
            void IEnumerator.Reset() => _arrayEnumerator.Dispose();
            LanguageSet IEnumerator<LanguageSet>.Current => _arrayEnumerator.Current;
            object IEnumerator.Current => _arrayEnumerator.Current;
        }

        public IEnumerator<LanguageSet> GetEnumerator() => new LanguageSetArrayEnumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => new LanguageSetArrayEnumerator(this);
    }

    private static readonly List<LanguageSet> Languages = new List<LanguageSet>(JSONMethods.DefaultLanguageAliasSets.Count);
    public static IEnumerable<LanguageSet> All()
    {
        lock (Languages)
        {
            if (Languages.Count > 0)
                Languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.Default;
                bool found = false;
                for (int i2 = 0; i2 < Languages.Count; i2++)
                {
                    if (Languages[i2].Language.Equals(lang, StringComparison.Ordinal) && Languages[i2].IMGUI == pl.Save.IMGUI)
                    {
                        Languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    Languages.Add(new LanguageSet(lang, pl));
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(Languages.ToArray());
            Languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> InRegions(byte x, byte y, byte regionDistance)
    {
        lock (Languages)
        {
            if (Languages.Count > 0)
                Languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (!Regions.checkArea(x, y, pl.Player.movement.region_x, pl.Player.movement.region_y, regionDistance)) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.Default;
                bool found = false;
                for (int i2 = 0; i2 < Languages.Count; i2++)
                {
                    if (Languages[i2].Language.Equals(lang, StringComparison.Ordinal) && Languages[i2].IMGUI == pl.Save.IMGUI)
                    {
                        Languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    Languages.Add(new LanguageSet(lang, pl));
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(Languages.ToArray());
            Languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> InRegions(byte x, byte y, ushort plant, byte regionDistance)
    {
        if (plant != ushort.MaxValue)
            return All();
        lock (Languages)
        {
            if (Languages.Count > 0)
                Languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (!Regions.checkArea(x, y, pl.Player.movement.region_x, pl.Player.movement.region_y, regionDistance)) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.Default;
                bool found = false;
                for (int i2 = 0; i2 < Languages.Count; i2++)
                {
                    if (Languages[i2].Language.Equals(lang, StringComparison.Ordinal) && Languages[i2].IMGUI == pl.Save.IMGUI)
                    {
                        Languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    Languages.Add(new LanguageSet(lang, pl));
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(Languages.ToArray());
            Languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> InRegionsByTeam(byte x, byte y, byte regionDistance)
    {
        lock (Languages)
        {
            if (Languages.Count > 0)
                Languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (!Regions.checkArea(x, y, pl.Player.movement.region_x, pl.Player.movement.region_y, regionDistance)) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.Default;
                ulong team = pl.GetTeam();
                bool found = false;
                for (int i2 = 0; i2 < Languages.Count; i2++)
                {
                    LanguageSet l = Languages[i2];
                    if (l.Team == team && l.Language.Equals(lang, StringComparison.Ordinal) && Languages[i2].IMGUI == pl.Save.IMGUI)
                    {
                        l.Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    Languages.Add(new LanguageSet(lang, pl) { Team = team });
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(Languages.ToArray());
            Languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> All(IEnumerator<SteamPlayer> players)
    {
        lock (Languages)
        {
            if (Languages.Count > 0)
                Languages.Clear();
            while (players.MoveNext())
            {
                UCPlayer? pl = UCPlayer.FromSteamPlayer(players.Current!);
                if (pl == null) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.Default;
                bool found = false;
                for (int i2 = 0; i2 < Languages.Count; i2++)
                {
                    if (Languages[i2].Language.Equals(lang, StringComparison.Ordinal) && Languages[i2].IMGUI == pl.Save.IMGUI)
                    {
                        Languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    Languages.Add(new LanguageSet(lang, pl));
            }
            players.Dispose();
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(Languages.ToArray());
            Languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> AllBut(params ulong[] exclude)
    {
        lock (Languages)
        {
            if (Languages.Count > 0)
                Languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                for (int j = 0; j < exclude.Length; j++)
                    if (pl.Steam64 == exclude[j]) goto next;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.Default;
                bool found = false;
                for (int i2 = 0; i2 < Languages.Count; i2++)
                {
                    if (Languages[i2].Language.Equals(lang, StringComparison.Ordinal) && Languages[i2].IMGUI == pl.Save.IMGUI)
                    {
                        Languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    Languages.Add(new LanguageSet(lang, pl));
                next:;
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(Languages.ToArray());
            Languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> AllBut(ulong exclude)
    {
        lock (Languages)
        {
            if (Languages.Count > 0)
                Languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (pl.Steam64 == exclude) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.Default;
                bool found = false;
                for (int i2 = 0; i2 < Languages.Count; i2++)
                {
                    if (Languages[i2].Language.Equals(lang, StringComparison.Ordinal) && Languages[i2].IMGUI == pl.Save.IMGUI)
                    {
                        Languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    Languages.Add(new LanguageSet(lang, pl));
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(Languages.ToArray());
            Languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> All(IEnumerator<Player> players)
    {
        lock (Languages)
        {
            if (Languages.Count > 0)
                Languages.Clear();
            while (players.MoveNext())
            {
                UCPlayer? pl = UCPlayer.FromPlayer(players.Current!);
                if (pl == null) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.Default;
                bool found = false;
                for (int i2 = 0; i2 < Languages.Count; i2++)
                {
                    if (Languages[i2].Language.Equals(lang, StringComparison.Ordinal) && Languages[i2].IMGUI == pl.Save.IMGUI)
                    {
                        Languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    Languages.Add(new LanguageSet(lang, pl));
            }
            players.Dispose();
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(Languages.ToArray());
            Languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> OfPermission(EAdminType type, PermissionComparison comparison = PermissionComparison.AtLeast)
    {
        lock (Languages)
        {
            if (Languages.Count > 0)
                Languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (!pl.PermissionLevel.IsOfPermission(type, comparison)) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.Default;
                bool found = false;
                for (int i2 = 0; i2 < Languages.Count; i2++)
                {
                    if (Languages[i2].Language.Equals(lang, StringComparison.Ordinal) && Languages[i2].IMGUI == pl.Save.IMGUI)
                    {
                        Languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    Languages.Add(new LanguageSet(lang, pl));
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(Languages.ToArray());
            Languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> All(IEnumerator<UCPlayer> players)
    {
        lock (Languages)
        {
            if (Languages.Count > 0)
                Languages.Clear();
            while (players.MoveNext())
            {
                UCPlayer pl = players.Current!;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.Default;
                bool found = false;
                for (int i2 = 0; i2 < Languages.Count; i2++)
                {
                    if (Languages[i2].Language.Equals(lang, StringComparison.Ordinal) && Languages[i2].IMGUI == pl.Save.IMGUI)
                    {
                        Languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    Languages.Add(new LanguageSet(lang, pl));
            }
            players.Dispose();
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(Languages.ToArray());
            Languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> OnTeam(ulong team)
    {
        lock (Languages)
        {
            if (Languages.Count > 0)
                Languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (pl.GetTeam() != team) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.Default;
                bool found = false;
                for (int i2 = 0; i2 < Languages.Count; i2++)
                {
                    if (Languages[i2].Language.Equals(lang, StringComparison.Ordinal) && Languages[i2].IMGUI == pl.Save.IMGUI)
                    {
                        Languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    Languages.Add(new LanguageSet(lang, pl) { Team = team });
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(Languages.ToArray());
            Languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> InSquad(Squad squad)
    {
        lock (Languages)
        {
            if (Languages.Count > 0)
                Languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (pl.Squad != squad) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.Default;
                bool found = false;
                for (int i2 = 0; i2 < Languages.Count; i2++)
                {
                    if (Languages[i2].Language.Equals(lang, StringComparison.Ordinal) && Languages[i2].IMGUI == pl.Save.IMGUI)
                    {
                        Languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    Languages.Add(new LanguageSet(lang, pl) { Team = squad.Team });
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(Languages.ToArray());
            Languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> Where(Predicate<UCPlayer> selector)
    {
        lock (Languages)
        {
            if (Languages.Count > 0)
                Languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (!selector(pl)) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.Default;
                bool found = false;
                for (int i2 = 0; i2 < Languages.Count; i2++)
                {
                    if (Languages[i2].Language.Equals(lang, StringComparison.Ordinal) && Languages[i2].IMGUI == pl.Save.IMGUI)
                    {
                        Languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    Languages.Add(new LanguageSet(lang, pl));
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(Languages.ToArray());
            Languages.Clear();
            return rtn;
        }
    }
}

[AttributeUsage(AttributeTargets.Enum | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
public sealed class TranslatableAttribute : Attribute
{
    public TranslatableAttribute()
    {
        Default = null;
        Language = L.Default;
    }
    public TranslatableAttribute(string? @default)
    {
        Default = @default;
        Language = L.Default;
    }
    public TranslatableAttribute(string language, string value)
    {
        Default = value;
        Language = language;
    }
    public string Language { get; }
    public string? Default { get; }
    public string? Description { get; set; }
    public bool IsPrioritizedTranslation { get; set; } = true;
}
