using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
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
    private static int _totalDefaultTranslations;
    private static readonly Dictionary<TranslationSection, int> TotalSectionedDefaultTranslations = new Dictionary<TranslationSection, int>(6);
    internal static void IncrementSection(TranslationSection section, int amt)
    {
        if (amt <= 0)
            return;
        if (TotalSectionedDefaultTranslations.TryGetValue(section, out int value))
            TotalSectionedDefaultTranslations[section] = value + amt;
        else TotalSectionedDefaultTranslations.Add(section, amt);
        if (_totalDefaultTranslations != 0)
            _totalDefaultTranslations += amt;
    }
    internal static void ClearSection(TranslationSection section)
    {
        TotalSectionedDefaultTranslations.Remove(section);
        _totalDefaultTranslations = 0;
        if (Data.LanguageDataStore == null) return;
        foreach (LanguageInfo language in Data.LanguageDataStore.Languages)
            language.ClearSection(section);
    }

    internal static int TotalDefaultTranslations
    {
        get
        {
            if (_totalDefaultTranslations == 0)
            {
                foreach (int val in TotalSectionedDefaultTranslations.Values)
                    _totalDefaultTranslations += val;
            }
            return _totalDefaultTranslations;
        }
        set => _totalDefaultTranslations = value;
    }

    private const string EnumNamePlaceholder = "%NAME%";

    private static readonly string EnumTranslationFileName = "Enums" + Path.DirectorySeparatorChar;
    private static readonly Dictionary<Type, Dictionary<string, Dictionary<string, string>>> EnumTranslations = new Dictionary<Type, Dictionary<string, Dictionary<string, string>>>(16);
    private static readonly Dictionary<Type, Dictionary<string, List<TranslatableAttribute>>> EnumTranslationAttributes = new Dictionary<Type, Dictionary<string, List<TranslatableAttribute>>>(16);
    private static CultureInfo[]? _allCultures;

    public const string UnityRichTextColorBaseStart = "<color=#";
    public const string RichTextColorEnd = ">";
    public const string TMProRichTextColorBase = "<#";
    public const string RichTextColorClosingTag = "</color>";

    public static CultureInfo[] AllCultures => _allCultures ??= CultureInfo.GetCultures(CultureTypes.AllCultures);

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
    public static string Translate<T>(this T translatable, LanguageInfo language, ulong team, string? fmt = null, bool imgui = false) where T : ITranslationArgument
    {
        return Translate(translatable, language, GetCultureInfo(language), team, fmt, imgui);
    }
    public static string Translate<T>(this T translatable, LanguageInfo language, CultureInfo? culture, ulong team, string? fmt = null, bool imgui = false) where T : ITranslationArgument
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
        return translatable.Translate(language, fmt, null, culture ?? GetCultureInfo(language), ref flags);
    }
    public static string Translate<T>(this T translatable, UCPlayer player, string? fmt = null) where T : ITranslationArgument
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
        return translatable.Translate(player.Locale.LanguageInfo, fmt, player, player.Locale.CultureInfo, ref flags);
    }
    public static string Translate<T>(this T translatable, CommandInteraction ctx, string? fmt = null) where T : ITranslationArgument
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
        return translatable.Translate(ctx.LanguageInfo, fmt, ctx.Caller, ctx.CultureInfo, ref flags);
    }
    public static string Translate<T0>(this Translation<T0> translation, UCPlayer? player, T0 arg0)
    {
        return translation.Translate(player?.Locale.LanguageInfo, player?.Locale.CultureInfo, arg0, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T0, T1>(this Translation<T0, T1> translation, UCPlayer? player, T0 arg0, T1 arg1)
    {
        return translation.Translate(player?.Locale.LanguageInfo, player?.Locale.CultureInfo, arg0, arg1, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T0, T1, T2>(this Translation<T0, T1, T2> translation, UCPlayer? player, T0 arg0, T1 arg1, T2 arg2)
    {
        return translation.Translate(player?.Locale.LanguageInfo, player?.Locale.CultureInfo, arg0, arg1, arg2, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T0, T1, T2, T3>(this Translation<T0, T1, T2, T3> translation, UCPlayer? player, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        return translation.Translate(player?.Locale.LanguageInfo, player?.Locale.CultureInfo, arg0, arg1, arg2, arg3, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T0, T1, T2, T3, T4>(this Translation<T0, T1, T2, T3, T4> translation, UCPlayer? player, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        return translation.Translate(player?.Locale.LanguageInfo, player?.Locale.CultureInfo, arg0, arg1, arg2, arg3, arg4, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T0, T1, T2, T3, T4, T5>(this Translation<T0, T1, T2, T3, T4, T5> translation, UCPlayer? player, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        return translation.Translate(player?.Locale.LanguageInfo, player?.Locale.CultureInfo, arg0, arg1, arg2, arg3, arg4, arg5, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T0, T1, T2, T3, T4, T5, T6>(this Translation<T0, T1, T2, T3, T4, T5, T6> translation, UCPlayer? player, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        return translation.Translate(player?.Locale.LanguageInfo, player?.Locale.CultureInfo, arg0, arg1, arg2, arg3, arg4, arg5, arg6, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, UCPlayer? player, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        return translation.Translate(player?.Locale.LanguageInfo, player?.Locale.CultureInfo, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, UCPlayer? player, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        return translation.Translate(player?.Locale.LanguageInfo, player?.Locale.CultureInfo, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, UCPlayer? player, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        return translation.Translate(player?.Locale.LanguageInfo, player?.Locale.CultureInfo, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, player, player is null ? 0 : player.GetTeam());
    }
    public static string TranslateUnsafe(Translation translation, UCPlayer? player, object?[] formatting, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
    {
        return translation.TranslateUnsafe(player?.Locale.LanguageInfo, player?.Locale.CultureInfo, formatting, player, player is null ? 0 : player.GetTeam(), canUseIMGUI, flags);
    }
    public static string GetTimeFromMinutes(int seconds) => GetTimeFromSeconds(seconds * 60, GetDefaultLanguage(), Data.LocalLocale);
    public static string GetTimeFromSeconds(int seconds) => GetTimeFromSeconds(seconds, GetDefaultLanguage(), Data.LocalLocale);
    public static string GetTimeFromMinutes(int minutes, in LanguageSet set) => GetTimeFromSeconds(minutes * 60, in set);
    public static string GetTimeFromSeconds(int seconds, in LanguageSet set) => GetTimeFromSeconds(seconds, set.Language, set.CultureInfo);
    public static string GetTimeFromMinutes(int minutes, UCPlayer? player) => GetTimeFromSeconds(minutes * 60, player);
    public static string GetTimeFromSeconds(int seconds, UCPlayer? player) => GetTimeFromSeconds(seconds, player?.Locale.LanguageInfo, player?.Locale.CultureInfo);
    public static string GetTimeFromMinutes(int minutes, LanguageInfo? language, CultureInfo? culture) => GetTimeFromSeconds(minutes * 60, language, culture);
    public static string GetTimeFromSeconds(int seconds, LanguageInfo? language, CultureInfo? culture)
    {
        language ??= GetDefaultLanguage();
        culture ??= GetCultureInfo(language);
        if (seconds < 0)
            return T.TimePermanent.Translate(language, culture);
        if (seconds == 0)
            seconds = 1;
        if (seconds < 60) // < 1 minute
            return seconds.ToString(culture) + ' ' + (seconds == 1 ? T.TimeSecondSingle : T.TimeSecondPlural).Translate(language, culture);
        int val;
        int overflow;
        if (seconds < 3600) // < 1 hour
        {
            val = F.DivideRemainder(seconds, 60, out overflow);
            return $"{val} {(val == 1 ? T.TimeMinuteSingle : T.TimeMinutePlural).Translate(language, culture)}" +
                   $"{(overflow == 0 ? string.Empty : $" {(T.TimeAnd).Translate(language, culture)} {overflow} {(overflow == 1 ? T.TimeSecondSingle : T.TimeSecondPlural).Translate(language, culture)}")}";
        }
        if (seconds < 86400) // < 1 day 
        {
            val = F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out overflow);
            return $"{val} {(val == 1 ? T.TimeHourSingle : T.TimeHourPlural).Translate(language, culture)}" +
                   $"{(overflow == 0 ? string.Empty : $" {(T.TimeAnd).Translate(language, culture)} {overflow} {(overflow == 1 ? T.TimeMinuteSingle : T.TimeMinutePlural).Translate(language, culture)}")}";
        }
        if (seconds < 2565000) // < 1 month (29.6875 days) (365.25/12)
        {
            val = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out overflow);
            return $"{val} {(val == 1 ? T.TimeDaySingle : T.TimeDayPlural).Translate(language, culture)}" +
                   $"{(overflow == 0 ? string.Empty : $" {(T.TimeAnd).Translate(language, culture)} {overflow} {(overflow == 1 ? T.TimeHourSingle : T.TimeHourPlural).Translate(language, culture)}")}";
        }
        if (seconds < 31536000) // < 1 year
        {
            val = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out _), 30.416m, out overflow);
            return $"{val} {(val == 1 ? T.TimeMonthSingle : T.TimeMonthPlural).Translate(language, culture)}" +
                   $"{(overflow == 0 ? string.Empty : $" {(T.TimeAnd).Translate(language, culture)} {overflow} {(overflow == 1 ? T.TimeDaySingle : T.TimeDayPlural).Translate(language, culture)}")}";
        }
        // > 1 year

        val = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out _), 30.416m, out _), 12, out overflow);
        return $"{val} {(val == 1 ? T.TimeYearSingle : T.TimeYearPlural).Translate(language, culture)}" +
               $"{(overflow == 0 ? string.Empty : $" {(T.TimeAnd).Translate(language, culture)} {overflow} {(overflow == 1 ? T.TimeMonthSingle : T.TimeMonthPlural).Translate(language, culture)}")}";
    }
    public static string TranslateLoadoutSign(byte loadoutId, UCPlayer player)
    {
        UCPlayer.TryApplyViewLens(ref player);
        ulong team = player.GetTeam();
        if (loadoutId <= 0)
        {
            return "<#ff0000>INVALID LOADOUT</color>";
        }
        SqlItem<Kit>? proxy = KitManager.GetLoadoutQuick(player, loadoutId);
        Kit? kit = proxy?.Item;
        if (kit != null)
        {
            string name;
            bool keepline = false;
            if (!player.OnDuty())
            {
                name = kit.GetDisplayName(player.Locale.LanguageInfo, false);
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
                name = kit.Id + '\n' + "(" + (char)(loadoutId + 47) + ") " + kit.GetDisplayName(player.Locale.LanguageInfo, false);
                keepline = true;
            }
            name = "<b>" + name.ToUpper()
                .ColorizeTMPro(
                    UCWarfare.GetColorHex(KitManager.IsFavoritedQuick(proxy!.LastPrimaryKey, player) ? "kit_public_header_fav" : "kit_public_header")
                    , true) + "</b>";
            string cost = "<sub>" + T.LoadoutName.Translate(player, KitEx.GetLoadoutLetter(KitEx.ParseStandardLoadoutId(kit.Id))) + "</sub>";
            if (!keepline) cost = "\n" + cost;

            string playercount;
            if (kit.NeedsUpgrade)
            {
                playercount = T.KitLoadoutUpgrade.Translate(player);
            }
            else if (kit.NeedsSetup)
            {
                playercount = T.KitLoadoutSetup.Translate(player);
            }
            else if (kit.RequiresNitro)
            {
                if (KitManager.IsNitroBoostingQuick(player.Steam64))
                    playercount = T.KitNitroBoostOwned.Translate(player);
                else
                    playercount = T.KitNitroBoostNotOwned.Translate(player);
            }
            else if (kit.TeamLimit >= 1f || kit.TeamLimit <= 0f)
            {
                playercount = T.KitUnlimited.Translate(player);
            }
            else if (kit.IsClassLimited(out int total, out int allowed, team, true))
            {
                playercount = T.KitPlayerCount.Translate(player, total, allowed).ColorizeTMPro(UCWarfare.GetColorHex("kit_player_counts_unavailable"), true);
            }
            else
            {
                playercount = T.KitPlayerCount.Translate(player, total, allowed).ColorizeTMPro(UCWarfare.GetColorHex("kit_player_counts_available"), true);
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

        return "<b>" + T.LoadoutName.Translate(player, "#" + loadoutId) + "</b>\n\n\n\n" +
               T.KitPremiumCost.Translate(player, Data.PurchasingDataStore is { LoadoutProduct.DefaultPrice.UnitAmountDecimal: { } price } ? decimal.Round(decimal.Divide(price, 100m), 2) : UCWarfare.Config.LoadoutCost)
                   .ColorizeTMPro(UCWarfare.GetColorHex("kit_level_dollars"), true);
    }
    public static string TranslateKitSign(Kit kit, UCPlayer player)
    {
        KitManager? manager = KitManager.GetSingletonQuick();
        bool keepline = false;
        UCPlayer.TryApplyViewLens(ref player);
        ulong team = player.GetTeam();
        string name;
        if (!player.OnDuty())
        {
            name = kit.GetDisplayName(player.Locale.LanguageInfo, false);
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
            name = kit.Id + "\n" + kit.GetDisplayName(player.Locale.LanguageInfo, false);
            keepline = true;
        }
        name = "<b>" + name
            .ToUpper()
            .ColorizeTMPro(UCWarfare.GetColorHex(
                kit.SquadLevel == SquadLevel.Commander
                    ? "kit_public_commander_header"
                    : (KitManager.IsFavoritedQuick(kit.PrimaryKey, player)
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
                cost = c.Steam64 != player.Steam64 ? T.KitCommanderTaken.Translate(player) : T.KitCommanderTakenByViewer.Translate(player);
                goto n;
            }
        }
        if (kit.RequiresNitro)
        {
            if (KitManager.IsNitroBoostingQuick(player.Steam64))
                cost = T.KitNitroBoostOwned.Translate(player);
            else
                cost = T.KitNitroBoostNotOwned.Translate(player);
            goto n;
        }
        if (kit.Type is KitType.Elite or KitType.Special)
        {
            if (manager != null && KitManager.HasAccessQuick(kit, player))
                cost = T.KitPremiumOwned.Translate(player);
            else if (kit.Type == KitType.Special)
                cost = T.KitExclusive.Translate(player);
            else if (kit.EliteKitInfo?.Product?.DefaultPrice is { UnitAmountDecimal: { } price })
                cost = T.KitPremiumCost.Translate(player, decimal.Round(decimal.Divide(price, 100m), 2));
            else
                cost = kit.PremiumCost <= 0m ? T.KitFree.Translate(player) : T.KitPremiumCost.Translate(player, kit.PremiumCost);
            goto n;
        }
        if (kit.UnlockRequirements != null && kit.UnlockRequirements.Length != 0)
        {
            for (int i = 0; i < kit.UnlockRequirements.Length; i++)
            {
                UnlockRequirement req = kit.UnlockRequirements[i];
                if (req.CanAccess(player)) continue;
                cost = req.GetSignText(player);
                goto n;
            }
        }
        if (kit.CreditCost > 0)
        {
            cost = KitManager.HasAccessQuick(kit, player) ? T.KitPremiumOwned.Translate(player) : T.KitCreditCost.Translate(player, kit.CreditCost);
        }
        else cost = T.KitFree.Translate(player);
        n:
        if (!keepline) cost = "\n" + cost;
        if (kit.TeamLimit >= 1f || kit.TeamLimit <= 0f)
        {
            playercount = T.KitUnlimited.Translate(player);
        }
        else if (kit.IsLimited(out int total, out int allowed, team, true))
        {
            playercount = T.KitPlayerCount.Translate(player, total, allowed).ColorizeTMPro(UCWarfare.GetColorHex("kit_player_counts_unavailable"), true);
        }
        else
        {
            playercount = T.KitPlayerCount.Translate(player, total, allowed).ColorizeTMPro(UCWarfare.GetColorHex("kit_player_counts_available"), true);
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
    public static string TranslateVBS(Vehicles.VehicleSpawn spawn, VehicleData data, LanguageInfo language, CultureInfo culture, FactionInfo? team)
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
            (data.TicketCost > 0 ? T.VBSTickets.Translate(language, culture, data.TicketCost, null, teamNum) : " ") + "\n" +
            unlock +
            "{0}";

        finalformat = finalformat.Colorize("ffffff") + "\n";
        if (comp.State is <= VehicleBayState.Unknown or VehicleBayState.NotInitialized)
            return finalformat;

        if (comp.State == VehicleBayState.Dead) // vehicle is dead
        {
            float rem = data.RespawnTime - comp.DeadTime;
            return finalformat + T.VBSStateDead.Translate(language, culture, Mathf.FloorToInt(rem / 60f), Mathf.FloorToInt(rem) % 60, null, teamNum);
        }
        if (comp.State == VehicleBayState.InUse)
        {
            return finalformat + T.VBSStateActive.Translate(language, culture, comp.CurrentLocation);
        }
        if (comp.State == VehicleBayState.Idle)
        {
            float rem = data.RespawnTime - comp.IdleTime;
            return finalformat + T.VBSStateIdle.Translate(language, culture, Mathf.FloorToInt(rem / 60f), Mathf.FloorToInt(rem) % 60, null, teamNum);
        }
        if (data.IsDelayed(out Delay delay))
        {
            string? del = GetDelaySignText(in delay, language, culture, teamNum);
            if (del != null)
                return finalformat + del;
        }
        return finalformat + T.VBSStateReady.Translate(language, culture);
    }

    public static string TranslateEnum<TEnum>(TEnum value) => TranslateEnum(value, GetDefaultLanguage());
    public static string TranslateEnum<TEnum>(TEnum value, LanguageInfo? language)
    {
        language ??= GetDefaultLanguage();
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        if (UCWarfare.IsLoaded && EnumTranslations.TryGetValue(typeof(TEnum), out Dictionary<string, Dictionary<string, string>> t))
        {
            if (!t.TryGetValue(language.LanguageCode, out Dictionary<string, string>? v) &&
                (language.IsDefault ||
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
    public static string TranslateEnumName(Type type) => TranslateEnumName(type, GetDefaultLanguage());
    public static string TranslateEnumName(Type type, LanguageInfo? language)
    {
        language ??= GetDefaultLanguage();
        if (UCWarfare.IsLoaded && EnumTranslations.TryGetValue(type, out Dictionary<string, Dictionary<string, string>> t))
        {
            if (!t.TryGetValue(language.LanguageCode, out Dictionary<string, string>? v) &&
                (language.IsDefault ||
                 !t.TryGetValue(L.Default, out v)))
                v = t.Values.FirstOrDefault();
            if (v == null || !v.TryGetValue(EnumNamePlaceholder, out string v2))
                return EnumNamePlaceholder.ToProperCase();
            return v2;
        }
        return EnumNameToStringDynamic(type);
    }
    public static string TranslateEnumName<TEnum>() where TEnum : struct, Enum => TranslateEnumName(typeof(TEnum), GetDefaultLanguage());
    public static string TranslateEnumName<TEnum>(LanguageInfo? language) where TEnum : struct, Enum => TranslateEnumName(typeof(TEnum), language);
    public static void ReadEnumTranslations(List<KeyValuePair<Type, string?>> extEnumTypes)
    {
        ClearSection(TranslationSection.Enums);
        int publicTranslations = 0;
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
        foreach (KeyValuePair<Type, TranslatableAttribute> enumType in Util.GetTypesSafe(Assembly.GetExecutingAssembly())
                     .Where(x => x.IsEnum)
                     .SelectMany(x => Attribute.GetCustomAttributes(x, typeof(TranslatableAttribute)).OfType<TranslatableAttribute>().Select(y => new KeyValuePair<Type, TranslatableAttribute>(x, y)))
                     .Where(t => t.Value != null)
                     .Concat(extEnumTypes
                         .Where(x => x.Key.IsEnum)
                         .Select(x => new KeyValuePair<Type, TranslatableAttribute>(x.Key, new TranslatableAttribute(x.Value)))))
        {
            if (EnumTranslations.ContainsKey(enumType.Key)) continue;
            Dictionary<string, Dictionary<string, string>> k = new Dictionary<string, Dictionary<string, string>>();
            bool isPriorityType = true;
            if (!EnumTranslationAttributes.TryGetValue(enumType.Key, out Dictionary<string, List<TranslatableAttribute>>? a))
            {
                a = new Dictionary<string, List<TranslatableAttribute>>(8) { { EnumNamePlaceholder, new List<TranslatableAttribute>(2) } };
                EnumTranslationAttributes.Add(enumType.Key, a);
            }
            else if (a.TryGetValue(EnumNamePlaceholder, out List<TranslatableAttribute> nameList))
            {
                isPriorityType = nameList.All(x => x.IsPrioritizedTranslation);
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
                if (Attribute.IsDefined(fields[i], typeof(JsonIgnoreAttribute)) || fields[i].IsIgnored())
                    continue;
                string name = fields[i].GetValue(null).ToString();
                Attribute[] attrs = Attribute.GetCustomAttributes(fields[i]);
                if (attrs.Length == 0) continue;
                if (!a.TryGetValue(name, out List<TranslatableAttribute> list))
                {
                    list = new List<TranslatableAttribute>(2);
                    a.Add(name, list);
                }

                bool isPriority = isPriorityType;
                for (int j = 0; j < attrs.Length; ++j)
                {
                    if (attrs[j] is not TranslatableAttribute t)
                        continue;

                    isPriority &= t.IsPrioritizedTranslation;
                    if (list.Exists(x => x.Language.Equals(t.Language, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    if (t.Language.IsDefault())
                        list.Insert(0, t);
                    else
                        list.Add(t);
                }

                if (isPriority)
                    ++publicTranslations;
            }
            if (isPriorityType)
                ++publicTranslations; // name
            if (!File.Exists(fn))
            {
                Dictionary<string, string> k2 = new Dictionary<string, string>(fields.Length + 1);
                WriteEnums(GetDefaultLanguage(), fields, enumType.Key, enumType.Value, fn, k2, defaultLangs);
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
                LanguageInfo? language = Data.LanguageDataStore.GetInfoCached(dir.Name);
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
                                    {
                                        k2.Add(key, value);
                                        if (isPriorityType && language is { IsDefault: false } &&
                                            EnumTranslationAttributes.TryGetValue(enumType.Key, out Dictionary<string, List<TranslatableAttribute>> fieldAttributeTable) &&
                                            fieldAttributeTable.TryGetValue(key, out List<TranslatableAttribute> attributes)
                                            && attributes.All(x => x.IsPrioritizedTranslation)
                                            )
                                        {
                                            language.IncrementSection(TranslationSection.Enums, 1);
                                        }
                                    }
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
                    if (lang.Equals(L.Default, StringComparison.Ordinal) || Data.LanguageDataStore.GetInfoCached(lang) is not { } languageInfo)
                        continue;

                    string p = Path.Combine(Data.Paths.LangStorage, lang, EnumTranslationFileName) + Path.DirectorySeparatorChar;
                    if (!Directory.Exists(p))
                        Directory.CreateDirectory(p);
                    p = Path.Combine(p, v.Key.FullName + ".json");
                    FieldInfo[] fields = v.Key.GetFields(BindingFlags.Public | BindingFlags.Static);
                    if (!File.Exists(p))
                    {
                        Dictionary<string, string> k2 = new Dictionary<string, string>(fields.Length + 1);
                        WriteEnums(languageInfo, fields, v.Key, null, p, k2, null);
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

        IncrementSection(TranslationSection.Enums, publicTranslations);
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
    public static void WriteEnums(LanguageInfo language, string folder, bool writeMissing, bool excludeNonPriorities)
    {
        F.CheckDir(folder, out _);
        foreach (KeyValuePair<Type, Dictionary<string, Dictionary<string, string>>> enumData in EnumTranslations)
        {
            if ((!enumData.Value.TryGetValue(language.LanguageCode, out Dictionary<string, string> values) && (language.IsDefault || !enumData.Value.TryGetValue(L.Default, out values))) || values == null)
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
            string fn = Path.Combine(folder, TranslateEnumName(type, GetDefaultLanguage()).RemoveMany(false, Data.Paths.BadFileNameCharacters));
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
            if (!language.IsDefault && writeMissing && enumData.Value.TryGetValue(L.Default, out Dictionary<string, string> defaultValues))
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
    private static void WriteEnums(LanguageInfo language, FieldInfo[] fields, Type type, TranslatableAttribute? attr1, string fn, Dictionary<string, string> k2, List<KeyValuePair<Type, List<string>>>? otherlangs)
    {
        using FileStream stream = new FileStream(fn, FileMode.Create, FileAccess.Write, FileShare.Read);
        Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
        writer.WriteStartObject();
        string name;
        if (attr1 != null && attr1.Default != null && attr1.Language.Equals(language.LanguageCode, StringComparison.Ordinal))
        {
            name = attr1.Default;
            writer.WritePropertyName(EnumNamePlaceholder);
            writer.WriteStringValue(name);
            k2.Add(EnumNamePlaceholder, name);
        }
        else if (Attribute.GetCustomAttributes(type, typeof(TranslatableAttribute))
                     .OfType<TranslatableAttribute>()
                     .FirstOrDefault(x => x.Language.Equals(language.LanguageCode, StringComparison.Ordinal)) is { Default: { } } attr2)
        {
            name = attr2.Default;
            writer.WritePropertyName(EnumNamePlaceholder);
            writer.WriteStringValue(name);
            k2.Add(EnumNamePlaceholder, name);
        }
        else if (language.IsDefault)
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
                k1 = language.IsDefault ? EnumToStringDynamic(k0) : null;
            else
            {
                for (int j = 0; j < tas.Length; ++j)
                {
                    TranslatableAttribute t = tas[j];
                    if (((t.Language is null && language.IsDefault) || (t.Language is not null && t.Language.Equals(language.LanguageCode, StringComparison.Ordinal)) && (language.IsDefault || t.Default != null)))
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
                if (language.IsDefault)
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
    public static LanguageInfo GetDefaultLanguage() => Data.LanguageDataStore?.GetInfoCached(L.Default) ?? Data.FallbackLanguageInfo;
    public static async ValueTask<LanguageInfo> GetLanguage(ulong player, CancellationToken token = default)
    {
        if (UCWarfare.IsLoaded && UCPlayer.FromID(player) is { Locale: { } locale })
            return locale.LanguageInfo;

        PlayerLanguagePreferences prefs = await Data.LanguageDataStore.GetLanguagePreferences(player, token).ConfigureAwait(false);
        if (prefs == null)
            return Data.LanguageDataStore.GetInfoCached(L.Default) ?? Data.FallbackLanguageInfo;

        return Data.LanguageDataStore.GetInfoCached(prefs.Language) ?? Data.LanguageDataStore.GetInfoCached(L.Default) ?? Data.FallbackLanguageInfo;
    }
    public static async ValueTask<CultureInfo> GetCulture(ulong player, CancellationToken token = default)
    {
        if (UCWarfare.IsLoaded && UCPlayer.FromID(player) is { Locale: { } locale })
            return locale.CultureInfo;

        PlayerLanguagePreferences prefs = await Data.LanguageDataStore.GetLanguagePreferences(player, token).ConfigureAwait(false);
        if (prefs == null)
            return Data.LocalLocale;

        return prefs.CultureCode != null && TryGetCultureInfo(prefs.CultureCode, out CultureInfo prefCulture) ? prefCulture : Data.LocalLocale;
    }
    public static CultureInfo GetCultureInfo(LanguageInfo? language)
    {
        if (language == null)
            return Data.LocalLocale;

        if (language.DefaultCultureCode != null)
        {
            if (TryGetCultureInfo(language.DefaultCultureCode, out CultureInfo culture))
                return culture;
        }
        else if (language.AvailableCultureCodes.Length > 0)
        {
            string code = language.AvailableCultureCodes.FirstOrDefault(x =>
                              x.Length == 5 && char.ToUpperInvariant(x[0]) == x[3] &&
                              char.ToUpperInvariant(x[1]) == x[4]) ??
                          language.AvailableCultureCodes[0];

            if (TryGetCultureInfo(code, out CultureInfo culture))
                return culture;
        }

        return Data.LocalLocale;
    }
    public static bool TryGetCultureInfo(string code, out CultureInfo cultureInfo)
    {
        if (code.Equals("invariant", StringComparison.InvariantCultureIgnoreCase))
        {
            cultureInfo = CultureInfo.InvariantCulture;
            return true;
        }
        try
        {
            cultureInfo = CultureInfo.GetCultureInfo(code);
            return true;
        }
        catch (CultureNotFoundException)
        {
            cultureInfo = null!;
            return false;
        }
    }
    public static string? GetDelaySignText(in Delay delay, LanguageInfo language, CultureInfo culture, ulong team)
    {
        if (delay.Type == DelayType.OutOfStaging)
        {
            return T.VBSDelayStaging.Translate(language, culture);
        }
        else if (delay.Type == DelayType.Time)
        {
            float timeLeft = delay.Value - Data.Gamemode.SecondsSinceStart;
            return T.VBSDelayTime.Translate(language, culture, Mathf.FloorToInt(timeLeft / 60f), Mathf.FloorToInt(timeLeft % 60), null, team);
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
                        return T.VBSDelayLoseFlag.Translate(language, culture, invasion.Rotation[ind], null, team);
                    else
                        return T.VBSDelayCaptureFlag.Translate(language, culture, invasion.Rotation[ind], null, team);
                }
                else if (team == invasion.DefendingTeam)
                    return T.VBSDelayLoseFlagMultiple.Translate(language, culture, ct2, null, team);
                else
                    return T.VBSDelayCaptureFlagMultiple.Translate(language, culture, ct2, null, team);
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
                    return T.VBSDelayCaptureFlag.Translate(language, culture, flags.Rotation[ind], null, team);
                else
                    return T.VBSDelayCaptureFlagMultiple.Translate(language, culture, ct2, null, team);
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
                    return T.VBSDelayCaptureFlag.Translate(language, culture, flags.Rotation[ind]);
                
                return T.VBSDelayCaptureFlagMultiple.Translate(language, culture, ct2);
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
                            return T.VBSDelayAttackCache.Translate(language, culture, ins.Caches[ind].Cache, null, team);
                        
                        return T.VBSDelayAttackCacheUnknown.Translate(language, culture);
                    }

                    if (ins.Caches[ind].IsActive)
                        return T.VBSDelayDefendCache.Translate(language, culture, ins.Caches[ind].Cache, null, team);
                    
                    return T.VBSDelayDefendCacheUnknown.Translate(language, culture);
                }

                if (team == ins.AttackingTeam)
                    return T.VBSDelayAttackCacheMultiple.Translate(language, culture, ct2, null, team);
                    
                return T.VBSDelayDefendCacheMultiple.Translate(language, culture, ct2, null, team);
            }
        }
        else if (delay.Type == DelayType.Teammates)
        {
            return T.VBSDelayTeammates.Translate(language, Mathf.FloorToInt(delay.Value), null, team);
        }
        return null;
    }
    public static void SendDelayRequestText(in Delay delay, UCPlayer player, ulong team, DelayTarget target)
    {
        InitDelayResponses();
        DelayResponses res = target switch
        {
            DelayTarget.Trait => _traitDelayResponses!,
            _ => _vehicleDelayResponses!
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
            player.SendChat(res.TimeDelay, GetTimeFromSeconds(Mathf.RoundToInt(timeLeft), player));
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
        else if (delay.Type == DelayType.Teammates)
        {
            player.SendChat(res.TeammatesDelay, Mathf.FloorToInt(delay.Value));
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

    private static DelayResponses? _vehicleDelayResponses;
    private static DelayResponses? _traitDelayResponses;
    private static void InitDelayResponses()
    {
        _vehicleDelayResponses = new DelayResponses(DelayTarget.VehicleBay);
        _traitDelayResponses = new DelayResponses(DelayTarget.Trait);
    }
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
        public readonly Translation<int> TeammatesDelay;
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
                TeammatesDelay = T.RequestTraitTeammatesDelay;
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
                TeammatesDelay = T.RequestVehicleTeammatesDelay;
            }
        }
    }
}

public struct LanguageSet : IEnumerator<UCPlayer>
{
    public readonly LanguageInfo Language;
    public readonly CultureInfo CultureInfo;
    public readonly bool IsDefault;
    public ulong Team = 0;
    public List<UCPlayer> Players { get; }
    public readonly bool IMGUI;
    private int _index;
    /// <summary>Use <see cref="MoveNext"/> to enumerate through the players and <seealso cref="Reset"/> to reset it.</summary>
    public UCPlayer Next;
    UCPlayer IEnumerator<UCPlayer>.Current => Next;
    object IEnumerator.Current => Next;
    public LanguageSet(UCPlayer player)
    {
        Language = player.Locale.LanguageInfo;
        CultureInfo = player.Locale.CultureInfo;
        IsDefault = player.Locale.IsDefaultLanguage && player.Locale.IsDefaultCulture;
        Players = new List<UCPlayer>(1) { player };
        IMGUI = player.Save.IMGUI;
        _index = -1;
        Next = null!;
        Team = player.GetTeam();
    }
    public LanguageSet(LanguageInfo lang, CultureInfo cultureInfo)
    {
        Language = lang ?? throw new ArgumentNullException(nameof(lang));
        CultureInfo = cultureInfo ?? throw new ArgumentNullException(nameof(cultureInfo));
        IsDefault = lang.IsDefault && cultureInfo.Name.Equals(Data.LocalLocale.Name, StringComparison.Ordinal);
        Players = new List<UCPlayer>(IsDefault ? Provider.clients.Count : 1);
        _index = -1;
        Next = null!;
        IMGUI = false;
    }
    public LanguageSet(LanguageInfo lang, CultureInfo cultureInfo, UCPlayer first)
    {
        Language = lang ?? throw new ArgumentNullException(nameof(lang));
        CultureInfo = cultureInfo ?? throw new ArgumentNullException(nameof(cultureInfo));
        IsDefault = lang.IsDefault && cultureInfo.Name.Equals(Data.LocalLocale.Name, StringComparison.Ordinal);
        Players = new List<UCPlayer>(IsDefault ? Provider.clients.Count : 1) { first };
        _index = -1;
        Next = null!;
        IMGUI = first.Save.IMGUI;
    }

    public bool Equals(in LanguageSet other) => other.IMGUI == IMGUI &&
                                                other.Language.LanguageCode.Equals(other.Language.LanguageCode, StringComparison.OrdinalIgnoreCase) &&
                                                other.CultureInfo.Name.Equals(CultureInfo.Name, StringComparison.Ordinal);

    public bool MatchesPlayer(UCPlayer player) => player.Save.IMGUI == IMGUI &&
                                                  player.Locale.LanguageInfo.LanguageCode.Equals(Language.LanguageCode, StringComparison.OrdinalIgnoreCase) &&
                                                  player.Locale.CultureInfo.Name.Equals(CultureInfo.Name);

    public bool MatchesPlayer(UCPlayer player, ulong team) => team == Team &&
                                                              player.Save.IMGUI == IMGUI &&
                                                              player.Locale.LanguageInfo.LanguageCode.Equals(Language.LanguageCode, StringComparison.OrdinalIgnoreCase) &&
                                                              player.Locale.CultureInfo.Name.Equals(CultureInfo.Name);
    public void Add(UCPlayer pl) => Players.Add(pl);
    /// <summary>Use <see cref="MoveNext"/> to enumerate through the players and <seealso cref="Reset"/> to reset it.</summary>
    public bool MoveNext()
    {
        if (_index < Players.Count - 1)
        {
            Next = Players[++_index];
            return true;
        }

        return false;
    }
    /// <summary>Use <see cref="MoveNext"/> to enumerate through the players and <seealso cref="Reset"/> to reset it.</summary>
    public void Reset()
    {
        Next = null!;
        _index = -1;
    }
    void IDisposable.Dispose() { }
    public override string ToString()
    {
        return _index.ToString(Data.AdminLocale) + "   " + string.Join(", ", Players.Select(x => x == null ? "null" : x.CharacterName)) + "   Current: " + (Next == null ? "null" : Next.CharacterName);
    }
    public struct LanguageSetEnumerator : IEnumerable<LanguageSet>, IEnumerator<LanguageSet>
    {
        public readonly LanguageSet[] Sets;
        private int _index;
        private LanguageSetEnumerator(LanguageSet[] sets, int index)
        {
            _index = index;
            Sets = sets;
        }
        public LanguageSetEnumerator(LanguageSet[] sets)
        {
            Sets = sets;
            _index = -1;
            for (int i = 1; i < sets.Length; ++i)
            {
                if (sets[i].IsDefault)
                {
                    if (i != 0)
                        (sets[i], sets[0]) = (sets[0], sets[i]);
                    
                    break;
                }
            }
        }
        public LanguageSetEnumerator GetEnumerator() => new LanguageSetEnumerator(Sets, -1);
        public LanguageSet Current => Sets[_index];
        public bool MoveNext() => ++_index < Sets.Length;
        public void Reset() => _index = -1;
        IEnumerator<LanguageSet> IEnumerable<LanguageSet>.GetEnumerator() => new LanguageSetEnumerator(Sets, -1);
        IEnumerator IEnumerable.GetEnumerator() => new LanguageSetEnumerator(Sets, -1);
        void IDisposable.Dispose() { }
        LanguageSet IEnumerator<LanguageSet>.Current => Sets[_index];
        object IEnumerator.Current => Sets[_index];
    }

    [ThreadStatic]
    private static LanguageSet[]? _languages;
    private static void CheckLang()
    {
        if (_languages == null || _languages.Length < PlayerManager.OnlinePlayers.Count)
            _languages = new LanguageSet[Math.Max(PlayerManager.OnlinePlayers.Count, Provider.maxPlayers)];
    }
    public static LanguageSetEnumerator All()
    {
        CheckLang();
        int len = -1;
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            bool found = false;
            for (int i2 = 0; i2 <= len; i2++)
            {
                ref LanguageSet set = ref _languages![i2];
                if (set.MatchesPlayer(pl))
                {
                    set.Add(pl);
                    found = true;
                    break;
                }
            }
            if (!found)
                _languages![++len] = new LanguageSet(pl.Locale.LanguageInfo, pl.Locale.CultureInfo, pl);
        }
        LanguageSetEnumerator rtn = new LanguageSetEnumerator(F.CloneStructArray(_languages!, length: len + 1));
        Array.Clear(_languages!, 0, len + 1);
        return rtn;
    }
    public static LanguageSetEnumerator InRegions(byte x, byte y, byte regionDistance)
    {
        CheckLang();
        int len = -1;
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            if (!Regions.checkArea(x, y, pl.Player.movement.region_x, pl.Player.movement.region_y, regionDistance)) continue;
            bool found = false;
            for (int i2 = 0; i2 <= len; i2++)
            {
                ref LanguageSet set = ref _languages![i2];
                if (set.MatchesPlayer(pl))
                {
                    set.Add(pl);
                    found = true;
                    break;
                }
            }
            if (!found)
                _languages![++len] = new LanguageSet(pl.Locale.LanguageInfo, pl.Locale.CultureInfo, pl);
        }
        LanguageSetEnumerator rtn = new LanguageSetEnumerator(F.CloneStructArray(_languages!, length: len + 1));
        Array.Clear(_languages!, 0, len + 1);
        return rtn;
    }
    public static LanguageSetEnumerator InRegions(byte x, byte y, ushort plant, byte regionDistance)
    {
        CheckLang();
        if (plant != ushort.MaxValue)
            return All();
        int len = -1;
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            if (!Regions.checkArea(x, y, pl.Player.movement.region_x, pl.Player.movement.region_y, regionDistance)) continue;
            bool found = false;
            for (int i2 = 0; i2 <= len; i2++)
            {
                ref LanguageSet set = ref _languages![i2];
                if (set.MatchesPlayer(pl))
                {
                    set.Add(pl);
                    found = true;
                    break;
                }
            }
            if (!found)
                _languages![++len] = new LanguageSet(pl.Locale.LanguageInfo, pl.Locale.CultureInfo, pl);
        }
        LanguageSetEnumerator rtn = new LanguageSetEnumerator(F.CloneStructArray(_languages!, length: len + 1));
        Array.Clear(_languages!, 0, len + 1);
        return rtn;
    }
    public static LanguageSetEnumerator InRegionsByTeam(byte x, byte y, byte regionDistance)
    {
        CheckLang();
        int len = -1;
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            if (!Regions.checkArea(x, y, pl.Player.movement.region_x, pl.Player.movement.region_y, regionDistance)) continue;
            ulong team = pl.GetTeam();
            bool found = false;
            for (int i2 = 0; i2 <= len; i2++)
            {
                ref LanguageSet set = ref _languages![i2];
                if (set.MatchesPlayer(pl, team))
                {
                    set.Add(pl);
                    found = true;
                    break;
                }
            }
            if (!found)
                _languages![++len] = new LanguageSet(pl.Locale.LanguageInfo, pl.Locale.CultureInfo, pl) { Team = team };
        }
        LanguageSetEnumerator rtn = new LanguageSetEnumerator(F.CloneStructArray(_languages!, length: len + 1));
        Array.Clear(_languages!, 0, len + 1);
        return rtn;
    }
    public static LanguageSetEnumerator All(IEnumerable<SteamPlayer> players) => All(players.GetEnumerator());
    public static LanguageSetEnumerator All(IEnumerator<SteamPlayer> players)
    {
        CheckLang();
        int len = -1;
        while (players.MoveNext())
        {
            UCPlayer? pl = UCPlayer.FromSteamPlayer(players.Current!);
            if (pl == null) continue;
            bool found = false;
            for (int i2 = 0; i2 <= len; i2++)
            {
                ref LanguageSet set = ref _languages![i2];
                if (set.MatchesPlayer(pl))
                {
                    set.Add(pl);
                    found = true;
                    break;
                }
            }
            if (!found)
                _languages![++len] = new LanguageSet(pl.Locale.LanguageInfo, pl.Locale.CultureInfo, pl);
        }
        players.Dispose();
        LanguageSetEnumerator rtn = new LanguageSetEnumerator(F.CloneStructArray(_languages!, length: len + 1));
        Array.Clear(_languages!, 0, len + 1);
        return rtn;
    }
    public static LanguageSetEnumerator AllBut(params ulong[] exclude)
    {
        CheckLang();
        int len = -1;
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            for (int j = 0; j < exclude.Length; j++)
                if (pl.Steam64 == exclude[j]) goto next;
            bool found = false;
            for (int i2 = 0; i2 <= len; i2++)
            {
                ref LanguageSet set = ref _languages![i2];
                if (set.MatchesPlayer(pl))
                {
                    set.Add(pl);
                    found = true;
                    break;
                }
            }
            if (!found)
                _languages![++len] = new LanguageSet(pl.Locale.LanguageInfo, pl.Locale.CultureInfo, pl);
            next:;
        }
        LanguageSetEnumerator rtn = new LanguageSetEnumerator(F.CloneStructArray(_languages!, length: len + 1));
        Array.Clear(_languages!, 0, len + 1);
        return rtn;
    }
    public static LanguageSetEnumerator AllBut(ulong exclude)
    {
        CheckLang();
        int len = -1;
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            if (pl.Steam64 == exclude) continue;
            bool found = false;
            for (int i2 = 0; i2 <= len; i2++)
            {
                ref LanguageSet set = ref _languages![i2];
                if (set.MatchesPlayer(pl))
                {
                    set.Add(pl);
                    found = true;
                    break;
                }
            }
            if (!found)
                _languages![++len] = new LanguageSet(pl.Locale.LanguageInfo, pl.Locale.CultureInfo, pl);
        }
        LanguageSetEnumerator rtn = new LanguageSetEnumerator(F.CloneStructArray(_languages!, length: len + 1));
        Array.Clear(_languages!, 0, len + 1);
        return rtn;
    }
    public static LanguageSetEnumerator All(IEnumerable<Player> players) => All(players.GetEnumerator());
    public static LanguageSetEnumerator All(IEnumerator<Player> players)
    {
        CheckLang();
        int len = -1;
        while (players.MoveNext())
        {
            UCPlayer? pl = UCPlayer.FromPlayer(players.Current!);
            if (pl == null) continue;
            bool found = false;
            for (int i2 = 0; i2 <= len; i2++)
            {
                ref LanguageSet set = ref _languages![i2];
                if (set.MatchesPlayer(pl))
                {
                    set.Add(pl);
                    found = true;
                    break;
                }
            }
            if (!found)
                _languages![++len] = new LanguageSet(pl.Locale.LanguageInfo, pl.Locale.CultureInfo, pl);
        }
        players.Dispose();
        LanguageSetEnumerator rtn = new LanguageSetEnumerator(F.CloneStructArray(_languages!, length: len + 1));
        Array.Clear(_languages!, 0, len + 1);
        return rtn;
    }
    public static LanguageSetEnumerator OfPermission(EAdminType type, PermissionComparison comparison = PermissionComparison.AtLeast)
    {
        CheckLang();
        int len = -1;
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            if (!pl.PermissionLevel.IsOfPermission(type, comparison)) continue;
            bool found = false;
            for (int i2 = 0; i2 <= len; i2++)
            {
                ref LanguageSet set = ref _languages![i2];
                if (set.MatchesPlayer(pl))
                {
                    set.Add(pl);
                    found = true;
                    break;
                }
            }
            if (!found)
                _languages![++len] = new LanguageSet(pl.Locale.LanguageInfo, pl.Locale.CultureInfo, pl);
        }
        LanguageSetEnumerator rtn = new LanguageSetEnumerator(F.CloneStructArray(_languages!, length: len + 1));
        Array.Clear(_languages!, 0, len + 1);
        return rtn;
    }
    public static LanguageSetEnumerator All(IEnumerable<UCPlayer> players) => All(players.GetEnumerator());
    public static LanguageSetEnumerator All(IEnumerator<UCPlayer> players)
    {
        CheckLang();
        int len = -1;
        while (players.MoveNext())
        {
            UCPlayer? pl = players.Current;
            if (pl == null) continue;
            bool found = false;
            for (int i2 = 0; i2 <= len; i2++)
            {
                ref LanguageSet set = ref _languages![i2];
                if (set.MatchesPlayer(pl))
                {
                    set.Add(pl);
                    found = true;
                    break;
                }
            }
            if (!found)
                _languages![++len] = new LanguageSet(pl.Locale.LanguageInfo, pl.Locale.CultureInfo, pl);
        }
        players.Dispose();
        LanguageSetEnumerator rtn = new LanguageSetEnumerator(F.CloneStructArray(_languages!, length: len + 1));
        Array.Clear(_languages!, 0, len + 1);
        return rtn;
    }
    public static LanguageSetEnumerator OnTeam(ulong team)
    {
        CheckLang();
        int len = -1;
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            if (pl.GetTeam() != team) continue;
            bool found = false;
            for (int i2 = 0; i2 <= len; i2++)
            {
                ref LanguageSet set = ref _languages![i2];
                if (set.MatchesPlayer(pl, team))
                {
                    set.Add(pl);
                    found = true;
                    break;
                }
            }
            if (!found)
                _languages![++len] = new LanguageSet(pl.Locale.LanguageInfo, pl.Locale.CultureInfo, pl) { Team = team };
        }
        LanguageSetEnumerator rtn = new LanguageSetEnumerator(F.CloneStructArray(_languages!, length: len + 1));
        Array.Clear(_languages!, 0, len + 1);
        return rtn;
    }
    public static LanguageSetEnumerator InSquad(Squad squad)
    {
        CheckLang();
        int len = -1;
        for (int i = 0; i < squad.Members.Count; i++)
        {
            UCPlayer pl = squad.Members[i];
            bool found = false;
            for (int i2 = 0; i2 <= len; i2++)
            {
                ref LanguageSet set = ref _languages![i2];
                if (set.MatchesPlayer(pl))
                {
                    set.Add(pl);
                    found = true;
                    break;
                }
            }
            if (!found)
                _languages![++len] = new LanguageSet(pl.Locale.LanguageInfo, pl.Locale.CultureInfo, pl) { Team = squad.Team };
        }
        LanguageSetEnumerator rtn = new LanguageSetEnumerator(F.CloneStructArray(_languages!, length: len + 1));
        Array.Clear(_languages!, 0, len + 1);
        return rtn;
    }
    public static LanguageSetEnumerator Where(Predicate<UCPlayer> selector)
    {
        CheckLang();
        int len = -1;
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            if (!selector(pl)) continue;
            bool found = false;
            for (int i2 = 0; i2 <= len; i2++)
            {
                ref LanguageSet set = ref _languages![i2];
                if (set.MatchesPlayer(pl))
                {
                    set.Add(pl);
                    found = true;
                    break;
                }
            }
            if (!found)
                _languages![++len] = new LanguageSet(pl.Locale.LanguageInfo, pl.Locale.CultureInfo, pl);
        }
        LanguageSetEnumerator rtn = new LanguageSetEnumerator(F.CloneStructArray(_languages!, length: len + 1));
        Array.Clear(_languages!, 0, len + 1);
        return rtn;
    }
    public static LanguageSetEnumerator Where(Predicate<UCPlayer> selector, ulong team)
    {
        CheckLang();
        int len = -1;
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            if (pl.GetTeam() != team || !selector(pl)) continue;
            bool found = false;
            for (int i2 = 0; i2 <= len; i2++)
            {
                ref LanguageSet set = ref _languages![i2];
                if (set.MatchesPlayer(pl))
                {
                    set.Add(pl);
                    found = true;
                    break;
                }
            }
            if (!found)
                _languages![++len] = new LanguageSet(pl.Locale.LanguageInfo, pl.Locale.CultureInfo, pl) { Team = team };
        }
        LanguageSetEnumerator rtn = new LanguageSetEnumerator(F.CloneStructArray(_languages!, length: len + 1));
        Array.Clear(_languages!, 0, len + 1);
        return rtn;
    }
}

public enum TranslationSection
{
    Primary,
    Kits,
    Traits,
    Enums,
    Factions,
    Deaths
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
