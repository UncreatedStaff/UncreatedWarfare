using JetBrains.Annotations;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Players;
using Uncreated.Properties;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Traits;
using UnityEngine;
using Action = System.Action;

namespace Uncreated.Warfare;

public class Translation
{
    private const string NullColorTMPro = "<#569cd6><b>null</b></color>";
    private const string NullColorUnity = "<color=#569cd6><b>null</b></color>";
    private const string NullNoColor = "null";
    private const string LocalFileName = "translations.properties";

    private static readonly Dictionary<string, List<KeyValuePair<Type, FormatDisplayAttribute>>> FormatDisplays
        = new Dictionary<string, List<KeyValuePair<Type, FormatDisplayAttribute>>>(32);

    private readonly TranslationFlags _flags;
    private readonly TranslationValue _defaultData;
    private TranslationValue[]? _data;
    private bool _init;
    public readonly int? DeclarationLineNumber;
    public string Key;
    public int Id;
    public static event Action? OnReload;
    public TranslationFlags Flags => _flags;
    protected string InvalidValue => "Translation Error - " + Key;
    internal TranslationDataAttribute? AttributeData { get; set; }
    public Translation(string @default, TranslationFlags flags) : this(@default)
    {
        _flags = flags;
    }
    public Translation(string @default)
    {
        _defaultData = new TranslationValue(Localization.GetDefaultLanguage(), @default, _flags);
        ProcessValue(_defaultData, Flags);

        // gets the declaration line number from the static constructor
        try
        {
            StackTrace trace = new StackTrace(1, true);
            StackFrame[] frames = trace.GetFrames()!;
            for (int i = 0; i < frames.Length; ++i)
            {
                StackFrame frame = frames[i];
                if (frame.GetMethod() is { } method && method.DeclaringType == typeof(T))
                {
                    DeclarationLineNumber = frame.GetFileLineNumber();
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            L.Log("Error getting line number:");
            L.LogError(ex);
            DeclarationLineNumber = null;
        }
    }
    protected readonly struct TranslationHelper
    {
        public readonly TranslationValue ValueSet;
        public readonly bool UseIMGUI;
        public readonly bool Inner;
        public readonly string PreformattedValue;
        public readonly UCPlayer? Player;
        public readonly ulong Team;
        public readonly TranslationFlags Flags;
        public readonly LanguageInfo Language;
        public readonly CultureInfo Culture;

        public ArgumentSpan[] Pluralizers
        {
            get
            {
                if (ValueSet == null) return Array.Empty<ArgumentSpan>();
                if (UseIMGUI)
                    return Inner ? ValueSet.ProcessedInnerNoTMProTagsPluralizers : ValueSet.ProcessedNoTMProTagsPluralizers;
                return Inner ? ValueSet.ProcessedInnerPluralizers : ValueSet.ProcessedPluralizers;
            }
        }
        public TranslationHelper(TranslationValue valueSet, bool useIMGUI, bool inner, string preformattedValue, LanguageInfo language, UCPlayer? player, ulong team, TranslationFlags flags, CultureInfo? culture)
        {
            ValueSet = valueSet;
            UseIMGUI = useIMGUI;
            Inner = inner;
            PreformattedValue = preformattedValue;
            Language = language;
            Player = player;
            Team = team;
            Flags = flags;
            Culture = culture ?? Data.LocalLocale;
        }
    }
    protected readonly struct ArgumentSpan
    {
        public readonly int Argument;
        public readonly int StartIndex;
        public readonly int Length;
        public readonly bool Inverted;
        public ArgumentSpan(int argument, int startIndex, int length, bool inverted)
        {
            Argument = argument;
            StartIndex = startIndex;
            Length = length;
            Inverted = inverted;
        }
        public void Pluralize(in TranslationHelper helper, ref string value, ref int offset)
        {
            int index = StartIndex + offset;
            if (index >= value.Length)
                return;
            int len = Length;
            if (len > value.Length - index)
                len = value.Length - index;
            L.LogDebug($"Argument: {Argument}, index {index} for {len} char. Inverted: {Inverted}");
            string plural = Translation.Pluralize(helper.Language, helper.Culture, value.Substring(index, len), helper.Flags | TranslationFlags.Plural);
            L.LogDebug($"Value: {plural} ({value.Substring(index, len)})");
            offset += plural.Length - len;
            value = value.Substring(0, index) + plural + value.Substring(index + len);
        }
    }

    internal void Dump()
    {
        L.Log($"Key: {Key}");
        L.Log($"Id: {Id}");
        L.Log($"Flags: {Flags}");
        L.Log("Default:");
        void DumpVal(TranslationValue val)
        {
            L.Log("Language: " + (val.Language ?? Localization.GetDefaultLanguage()));
            L.Log($"Original: \"{val.Original}\"");
            L.Log($"ProcessedInner: \"{val.ProcessedInner}\"");
            L.Log($"Processed: \"{val.Processed}\"");
            L.Log($"ProcessedNoTMProTags: \"{val.ProcessedNoTMProTags}\"");
            L.Log($"ProcessedInnerNoTMProTags: \"{val.ProcessedInnerNoTMProTags}\"");
            L.Log($"Color: \"{val.Color:F2}\"");
            L.Log($"RichText: \"{val.RichText}\"");
            L.Log($"Console: \"{val.Console}\"");
        }
        DumpVal(_defaultData);
        if (_data != null)
        {
            using IDisposable indent = L.IndentLog(1);
            for (int i = 0; i < _data.Length; ++i)
            {
                DumpVal(_data[i]);
            }
        }
    }
    public void RefreshColors()
    {
        ProcessValue(_defaultData, Flags);
        if (_data is not null)
        {
            for (int i = 0; i < _data.Length; ++i)
            {
                ProcessValue(_data[i], Flags);
            }
        }
    }
    internal void Init()
    {
        if (_init) return;
        _init = true;
        VerifyOriginal(null, _defaultData.Original);
    }
    private void VerifyOriginal(LanguageInfo? lang, string def)
    {
        if ((_flags & TranslationFlags.SuppressWarnings) == TranslationFlags.SuppressWarnings) return;
        if ((_flags & TranslationFlags.TMProSign) == TranslationFlags.TMProSign && def.IndexOf("<size", StringComparison.OrdinalIgnoreCase) != -1)
            L.LogWarning("[" + (lang == null ? "DEFAULT" : lang.LanguageCode.ToUpper()) + "] " + Key + " has a size tag, which shouldn't be on signs.", method: "TRANSLATIONS");
        int ct = GetType().GenericTypeArguments.Length;
        int index = -2;
        int flag = 0;
        int max = -1;
        while (true)
        {
            index = def.IndexOf('{', index + 2);
            if (index == -1 || index >= def.Length - 2) break;
            char next = def[index + 1];
            if (next is >= '0' and <= '9')
            {
                char next2 = def[index + 2];
                int num;
                if (next2 is < '0' or > '9')
                    num = next - 48;
                else
                    num = (next - 48) * 10 + (next2 - 48);
                flag |= (1 << num);
                if (max < num) max = num;
            }
        }

        for (int i = 0; i < ct; ++i)
        {
            if (((flag >> i) & 1) == 0 && lang == null)
            {
                L.LogWarning("[DEFAULT] " + Key + " parameter at index " + i + " is unused.", method: "TRANSLATIONS");
            }
        }
        --ct;
        if (max > ct)
            L.LogError("[" + (lang == null ? "DEFAULT" : lang.LanguageCode.ToUpper()) + "] " + Key + " has " + (max - ct == 1 ? ("an extra paremeter: " + max) : $"{max - ct} extra parameters: Should have: {ct + 1}, has: {max + 1}"), method: "TRANSLATIONS");
    }
    public void AddTranslation(LanguageInfo language, string value)
    {
        VerifyOriginal(language, value);
        if (_data is null || _data.Length == 0)
            _data = new TranslationValue[] { new TranslationValue(language, value, Flags) };
        else
        {
            for (int i = 0; i < _data.Length; ++i)
            {
                if (_data[i].Language == language)
                {
                    _data[i] = new TranslationValue(language, value, Flags);
                    return;
                }
            }

            TranslationValue[] old = _data;
            _data = new TranslationValue[old.Length + 1];
            if (language.IsDefault)
            {
                Array.Copy(old, 0, _data, 1, old.Length);
                _data[0] = new TranslationValue(Localization.GetDefaultLanguage(), value, Flags);
            }
            else
            {
                Array.Copy(old, _data, old.Length);
                _data[_data.Length - 1] = new TranslationValue(language, value, Flags);
            }
        }
    }
    public void RemoveTranslation(LanguageInfo language)
    {
        if (_data is null || _data.Length == 0) return;
        int index = -1;
        for (int i = 0; i < _data.Length; ++i)
        {
            if (_data[i].Language == language)
            {
                index = i;
                break;
            }
        }
        if (index == -1) return;
        if (_data.Length == 1)
        {
            _data = Array.Empty<TranslationValue>();
            return;
        }
        TranslationValue[] old = _data;
        _data = new TranslationValue[old.Length - 1];
        if (index != 0)
            Array.Copy(old, 0, _data, 0, index);
        Array.Copy(old, index + 1, _data, index, old.Length - index - 1);
    }
    public void ClearTranslations() => _data = Array.Empty<TranslationValue>();
    protected TranslationValue this[string language]
    {
        get
        {
            if (_data is not null)
            {
                for (int i = 0; i < _data.Length; ++i)
                {
                    if (_data[i].Language.LanguageCode.Equals(language, StringComparison.OrdinalIgnoreCase))
                    {
                        return _data[i];
                    }
                }
            }
            return _defaultData;
        }
    }
    protected TranslationValue this[LanguageInfo language]
    {
        get
        {
            if (_data is not null)
            {
                for (int i = 0; i < _data.Length; ++i)
                {
                    if (_data[i].Language == language)
                    {
                        return _data[i];
                    }
                }
            }
            return _defaultData;
        }
    }
    public static string ToString(object value, LanguageInfo language, string? format, UCPlayer? target, TranslationFlags flags)
        => ToString(value, language, Localization.GetCultureInfo(language), format, target, flags);
    public static string ToString(object value, LanguageInfo language, CultureInfo culture, string? format, UCPlayer? target, TranslationFlags flags)
    {
        if (value is null)
            return ToString<object>(value!, language, format, target, flags);
        return (string)typeof(ToStringHelperClass<>).MakeGenericType(value.GetType())
            .GetMethod("ToString", BindingFlags.Static | BindingFlags.Public)!.Invoke(null, new object?[] { value, language, format,
                target, Localization.GetCultureInfo(language), flags });
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToString<T>(T value, LanguageInfo language, string? format, UCPlayer? target, TranslationFlags flags)
        => ToStringHelperClass<T>.ToString(value, language, format, target, Localization.GetCultureInfo(language), flags);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToString<T>(T value, LanguageInfo language, CultureInfo culture, string? format, UCPlayer? target, TranslationFlags flags)
        => ToStringHelperClass<T>.ToString(value, language, format, target, culture, flags);

    private static readonly List<ArgumentSpan> WorkingPluralizers = new List<ArgumentSpan>(10);
    protected static ArgumentSpan[] GetPluralizers(ref string str) => GetArgumentModifiers(ref str, 'p');
    protected static ArgumentSpan[] GetArgumentModifiers(ref string str, char modifier)
    {
        lock (WorkingPluralizers)
        {
            try
            {
                int index = -1;
                while (index + 1 < str.Length)
                {
                    // ${p:10:test}
                    int nextSign = str.IndexOf('$', index + 1);
                    if (nextSign == -1)
                        break;
                    index = nextSign;
                    if (nextSign >= str.Length - 4 || str[nextSign + 1] != '{' || (str[nextSign + 2] != modifier && str[nextSign + 2] != char.ToUpperInvariant(modifier)) || str[nextSign + 3] != ':')
                        continue;
                    int closing = str.IndexOf('}', nextSign + 2);
                    if (closing == -1)
                        break;
                    bool inverted = str[closing - 1] == '!';
                    int firstDigit = -1;
                    int lastDigit = -1;
                    for (int j = nextSign + 4; j < str.Length; ++j)
                    {
                        if (!char.IsDigit(str[j]))
                            break;
                        if (firstDigit == -1)
                            firstDigit = j;
                        lastDigit = j;
                    }
                    if (str.Length <= lastDigit + 1 || str[lastDigit + 1] != ':' || !int.TryParse(str.SubstringRange(firstDigit, lastDigit), NumberStyles.Number, Data.AdminLocale, out int argument))
                        continue;
                    str = str.Substring(0, nextSign) + str.SubstringRange(lastDigit + 2, closing - (inverted ? 2 : 1)) + (closing < str.Length - 1 ? str.Substring(closing + 1) : string.Empty);
                    index = closing - ((inverted ? 8 : 7) + (lastDigit - firstDigit));
                    WorkingPluralizers.Add(new ArgumentSpan(argument, nextSign, closing - (lastDigit + (inverted ? 3 : 2)), inverted));
                }

                return WorkingPluralizers.Count == 0 ? Array.Empty<ArgumentSpan>() : WorkingPluralizers.ToArray();
            }
            finally
            {
                WorkingPluralizers.Clear();
            }
        }
    }

    private static readonly Type[] TypeArray1 = { typeof(string), typeof(IFormatProvider) };
    private static readonly Type[] TypeArray2 = { typeof(string) };
    private static readonly Type[] TypeArray3 = { typeof(IFormatProvider) };
    private static class ToStringHelperClass<T>
    {
        private static readonly Func<T, string, IFormatProvider, string>? ToStringFunc1;
        private static readonly Func<T, string, string>? ToStringFunc2;
        private static readonly Func<T, IFormatProvider, string>? ToStringFunc3;
        private static readonly Func<T, string>? ToStringFunc4;
        public static readonly Func<T, bool>? IsOne;
        private static readonly int Type;
        public static string ToString(T value, LanguageInfo language, string? format, UCPlayer? target, CultureInfo culture, TranslationFlags flags)
        {
            if (value is null)
                return Null(flags);

            if (value is string str)
                return CheckCase(Pluralize(language, culture, str, flags), format);

            str = Type switch
            {
                1  => ToStringFunc1!(value, format!, culture),
                2  => ToStringFunc2!(value, format!),
                3  => ToStringFunc3!(value, culture),
                4  => Pluralize(language, culture, (value as ITranslationArgument)!.Translate(language, format, target, culture, ref flags), flags),
                5  => CheckCase(Pluralize(language, culture, (value as UnityEngine.Object)!.name, flags), format),
                6  => value is Color clr ? clr.Hex() : value.ToString(),
                7  => value is CSteamID id ? id.m_SteamID.ToString(format, culture) : value.ToString(),
                8  => PlayerToString((value as PlayerCaller)!.player, flags, format),
                9  => PlayerToString((value as Player)!, flags, format),
                10 => PlayerToString((value as SteamPlayer)!.player, flags, format),
                11 => PlayerToString((value as SteamPlayerID)!, flags, format),
                12 => CheckCase(Pluralize(language, culture, value is Type t ? (t.IsEnum ? Localization.TranslateEnumName(t, language) : (t.IsArray ? (t.GetElementType()!.Name + " Array") : TypeToString(t))) : value.ToString(), flags), format),
                13 => CheckCase(Pluralize(language, culture, Localization.TranslateEnum(value, language), flags), format),
                14 => CheckCase(AssetToString(language, culture, (value as Asset)!, format, flags), format),
                15 => ToStringFunc4!(value),
                16 => CheckCase((value as BarricadeData)?.barricade.asset.itemName ?? value.ToString(), format),
                17 => CheckCase((value as StructureData)?.structure.asset.itemName ?? value.ToString(), format),
                18 => value is Guid guid ? guid.ToString(format ?? "N", culture) : value.ToString(),
                19 => value is char chr ? CheckCase(new string(chr, 1), format) : value.ToString(),
                50 => CheckTime(value, format, out string? val, language, culture) ? val! : value.ToString(),
                51 => CheckTime(value, format, out string? val, language, culture) ? val! : ToStringFunc1!(value, format!, culture),
                52 => CheckTime(value, format, out string? val, language, culture) ? val! : ToStringFunc2!(value, format!),
                53 => CheckTime(value, format, out string? val, language, culture) ? val! : ToStringFunc3!(value, culture),
                _  => Default(value, format, language, culture, target, flags),
            };

            return str;
        }

        private static string Default(T value, string? format, LanguageInfo lang, IFormatProvider locale, UCPlayer? target, TranslationFlags flags)
        {
            if (value is ICollection col)
            {
                StringBuilder builder = new StringBuilder("[", col.Count * 5);
                Type? tlast = null;
                MethodInfo? mlast = null;
                object?[]? args = null;
                foreach (object obj in col)
                {
                    if (tlast == null || !tlast.GenericTypeArguments[0].IsInstanceOfType(obj))
                    {
                        tlast = typeof(ToStringHelperClass<>).MakeGenericType(obj.GetType());
                        mlast = tlast.GetMethod(nameof(ToString),
                            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        if (mlast == null)
                            goto norm;
                    }

                    if (args is null)
                        args = new object?[] { obj, lang, format, target, locale, flags };
                    else args[0] = obj;
                    
                    builder.Append((string)mlast!.Invoke(null, args));
                }

                builder.Append(']');
                return builder.ToString();
            }
            norm:
            return value!.ToString();
        }
        private static bool CheckTime(T value, string? format, out string? val, LanguageInfo lang, CultureInfo? culture)
        {
            if (format is not null)
            {
                if (format.Equals(Warfare.T.FormatTimeLong, StringComparison.Ordinal))
                {
                    int sec = -1;
                    switch (value)
                    {
                        case float f:
                            sec = Mathf.RoundToInt(f);
                            goto default;
                        case int i:
                            sec = i;
                            goto default;
                        case uint i:
                            sec = checked((int)i);
                            goto default;
                        case TimeSpan span:
                            sec = (int)Math.Round(span.TotalSeconds);
                            goto default;
                        default:
                            if (sec != -1)
                            {
                                val = Localization.GetTimeFromSeconds(sec, lang, culture);
                                return true;
                            }
                            break;
                    }
                }
                else
                {
                    bool b1 = format.Equals(Warfare.T.FormatTimeShort_MM_SS, StringComparison.Ordinal);
                    bool b2 = !b1 && format.Equals(Warfare.T.FormatTimeShort_HH_MM_SS, StringComparison.Ordinal);
                    if (b1 || b2)
                    {
                        string sep = culture != null ? culture.DateTimeFormat.TimeSeparator : ":";
                        int sec = -1;
                        switch (value)
                        {
                            case float f:
                                sec = Mathf.RoundToInt(f);
                                goto default;
                            case int i:
                                sec = i;
                                goto default;
                            case uint i:
                                sec = checked((int)i);
                                goto default;
                            case TimeSpan span:
                                sec = (int)Math.Round(span.TotalSeconds);
                                goto default;
                            default:
                                if (sec != -1)
                                {
                                    if (b1)
                                        val = (sec / 60).ToString("00", culture) + sep + (sec % 60).ToString("00", culture);
                                    else
                                    {
                                        int hrs = sec / 3600;
                                        int mins = sec - hrs * 3600;
                                        val = hrs.ToString("00", culture) + sep + (mins / 60).ToString("00", culture) + sep + (mins % 60).ToString("00", culture);
                                    }
                                    return true;
                                }
                                break;
                        }
                    }
                }
            }

            val = null;
            return false;
        }
        private static string AssetToString(LanguageInfo language, CultureInfo culture, Asset asset, string? format, TranslationFlags flags)
        {
            if (asset is ItemAsset a)
                return ItemAssetToString(language, culture, a, format, flags);
            else if (asset is VehicleAsset b)
                return VehicleAssetToString(language, culture, b, format, flags);
            else if (asset is QuestAsset c)
                return QuestAssetToString(language, culture, c, format, flags);

            return Pluralize(language, culture, asset.FriendlyName, flags);
        }
        private static string QuestAssetToString(LanguageInfo language, CultureInfo culture, QuestAsset asset, string? format, TranslationFlags flags)
        {
            if (format is not null)
            {
                if (format.Equals(BaseQuestData.COLOR_QUEST_ASSET_FORMAT, StringComparison.Ordinal))
                    return Pluralize(language, culture, asset.questName, flags);
            }
            return Pluralize(language, culture, (flags & TranslationFlags.NoRichText) == TranslationFlags.NoRichText ? F.RemoveColorTag(asset.questName) : asset.questName, flags);
        }
        private static string VehicleAssetToString(LanguageInfo language, CultureInfo culture, VehicleAsset asset, string? format, TranslationFlags flags)
        {
            if (format is not null)
            {
                if (format.Equals(Warfare.T.FormatRarityColor, StringComparison.Ordinal))
                    return Localization.Colorize(ItemTool.getRarityColorUI(asset.rarity).Hex(), Pluralize(language, culture, asset.vehicleName, flags), flags);
            }
            return Pluralize(language, culture, asset.vehicleName, flags);
        }
        private static string ItemAssetToString(LanguageInfo language, CultureInfo culture, ItemAsset asset, string? format, TranslationFlags flags)
        {
            string name = asset.itemName;
            if (name.EndsWith(" Built", StringComparison.Ordinal))
                name = name.Substring(0, name.Length - 6);
            if (format is not null)
            {
                if (format.Equals(Warfare.T.FormatRarityColor, StringComparison.Ordinal))
                    return Localization.Colorize(ItemTool.getRarityColorUI(asset.rarity).Hex(), Pluralize(language, culture, name, flags), flags);
            }
            return Pluralize(language, culture, asset.itemName, flags);
        }
        private static string CheckCase(string str, string? format)
        {
            if (format is not null)
            {
                if (format.Equals(Warfare.T.FormatUppercase, StringComparison.Ordinal))
                    return str.ToUpperInvariant();
                else if (format.Equals(Warfare.T.FormatLowercase, StringComparison.Ordinal))
                    return str.ToLowerInvariant();
                else if (format.Equals(Warfare.T.FormatPropercase, StringComparison.Ordinal))
                    return str.ToProperCase();
            }
            return str;
        }
        private static string TypeToString(Type t)
        {
            if (t.IsPrimitive)
            {
                if (t == typeof(int) || t == typeof(long) || t == typeof(short))
                    return "Integer";
                if (t == typeof(uint))
                    return "Positive Integer";
                if (t == typeof(ulong))
                    return "Team or Steam64 ID";
                if (t == typeof(ushort))
                    return "Asset ID or Positive Integer";
                if (t == typeof(float) || t == typeof(double))
                    return "Decimal";
                if (t == typeof(char))
                    return "Single Character";
            }
            else if (t == typeof(string))
                return "Text";
            else if (t == typeof(Guid))
                return "GUID";
            else if (t == typeof(DateTime) || t == typeof(DateTimeOffset))
                return "Timestamp";
            else if (t == typeof(TimeSpan))
                return "Time Span";
            else if (t == typeof(decimal))
                return "Decimal";
            return t.Name;
        }
        private static string PlayerToString(Player player, TranslationFlags flags, string? format)
        {
            if (player is null)
                return Null(flags);
            if (!player.isActiveAndEnabled)
                return player.channel.owner.playerID.steamID.m_SteamID.ToString(Data.AdminLocale);
            UCPlayer? pl = UCPlayer.FromPlayer(player);
            PlayerNames names = pl is null ? new PlayerNames(player) : pl.Name;
            if (format is not null)
            {
                if (format.Equals(UCPlayer.CHARACTER_NAME_FORMAT, StringComparison.Ordinal))
                    return names.CharacterName;
                if (format.Equals(UCPlayer.COLOR_CHARACTER_NAME_FORMAT, StringComparison.Ordinal))
                    return Localization.Colorize(TeamManager.GetTeamHexColor(player.GetTeam()), names.CharacterName, flags);
                if (format.Equals(UCPlayer.NICK_NAME_FORMAT, StringComparison.Ordinal))
                    return names.NickName;
                if (format.Equals(UCPlayer.COLOR_NICK_NAME_FORMAT, StringComparison.Ordinal))
                    return Localization.Colorize(TeamManager.GetTeamHexColor(player.GetTeam()), names.NickName, flags);
                if (format.Equals(UCPlayer.PLAYER_NAME_FORMAT, StringComparison.Ordinal))
                    return names.PlayerName;
                if (format.Equals(UCPlayer.COLOR_PLAYER_NAME_FORMAT, StringComparison.Ordinal))
                    return Localization.Colorize(TeamManager.GetTeamHexColor(player.GetTeam()), names.PlayerName, flags);
                if (format.Equals(UCPlayer.STEAM_64_FORMAT, StringComparison.Ordinal))
                    return player.channel.owner.playerID.steamID.m_SteamID.ToString(Data.AdminLocale);
                if (format.Equals(UCPlayer.COLOR_STEAM_64_FORMAT, StringComparison.Ordinal))
                    return Localization.Colorize(TeamManager.GetTeamHexColor(player.GetTeam()), player.channel.owner.playerID.steamID.m_SteamID.ToString(Data.AdminLocale), flags);
            }
            return names.CharacterName;
        }
        private static string PlayerToString(SteamPlayerID player, TranslationFlags flags, string? format)
        {
            if (player is null) return Null(flags);
            SteamPlayer? pl = PlayerTool.getSteamPlayer(player.steamID.m_SteamID);
            if (pl is not null) return PlayerToString(pl.player, flags, format);

            if (format is not null)
            {
                if (format.Equals(UCPlayer.CHARACTER_NAME_FORMAT, StringComparison.Ordinal) || format.Equals(UCPlayer.COLOR_CHARACTER_NAME_FORMAT, StringComparison.Ordinal))
                    return player.characterName;
                if (format.Equals(UCPlayer.NICK_NAME_FORMAT, StringComparison.Ordinal) || format.Equals(UCPlayer.COLOR_NICK_NAME_FORMAT, StringComparison.Ordinal))
                    return player.nickName;
                if (format.Equals(UCPlayer.PLAYER_NAME_FORMAT, StringComparison.Ordinal) || format.Equals(UCPlayer.COLOR_PLAYER_NAME_FORMAT, StringComparison.Ordinal))
                    return player.playerName;
                if (format.Equals(UCPlayer.STEAM_64_FORMAT, StringComparison.Ordinal) || format.Equals(UCPlayer.COLOR_STEAM_64_FORMAT, StringComparison.Ordinal))
                    return player.steamID.m_SteamID.ToString(Data.AdminLocale);
            }
            return player.characterName;
        }
        static ToStringHelperClass()
        {
            Type t = typeof(T);
            DynamicMethod dm;
            ILGenerator il;
            if (t == typeof(char))
            {
                Type = 19;
                return;
            }
            if (t == typeof(decimal) || (t.IsPrimitive && t != typeof(char) && t != typeof(bool)))
            {
                if (t.IsPrimitive)
                {
                    dm = new DynamicMethod("IsOne",
                        MethodAttributes.Static | MethodAttributes.Public,
                        CallingConventions.Standard, typeof(bool), new Type[] { t }, t,
                        true);
                    dm.DefineParameter(1, ParameterAttributes.None, "value");
                    il = dm.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0);
                    if (t == typeof(float))
                        il.Emit(OpCodes.Ldc_R4, 1f);
                    else if (t == typeof(double))
                        il.Emit(OpCodes.Ldc_R8, 1d);
                    else
                        il.Emit(OpCodes.Ldc_I4_S, 1);
                    if (t == typeof(long) || t == typeof(ulong))
                        il.Emit(OpCodes.Conv_I8);
                    il.Emit(OpCodes.Ceq);
                    il.Emit(OpCodes.Ret);
                    IsOne = (Func<T, bool>)dm.CreateDelegate(typeof(Func<T, bool>));
                }
                else if (t == typeof(decimal))
                    IsOne = (v) => ((IComparable<decimal>)v!).CompareTo(1m) == 0;
            }

            if (typeof(string).IsAssignableFrom(t))
            {
                Type = 0;
                return;
            }
            if (t.IsEnum)
            {
                Type = 13;
                return;
            }
            if (typeof(ITranslationArgument).IsAssignableFrom(t))
            {
                Type = 4;
                return;
            }
            if (typeof(PlayerCaller).IsAssignableFrom(t))
            {
                Type = 8;
                return;
            }
            if (typeof(Player).IsAssignableFrom(t))
            {
                Type = 9;
                return;
            }
            if (typeof(SteamPlayer).IsAssignableFrom(t))
            {
                Type = 10;
                return;
            }
            if (typeof(SteamPlayerID).IsAssignableFrom(t))
            {
                Type = 11;
                return;
            }
            if (typeof(Asset).IsAssignableFrom(t))
            {
                Type = 14;
                return;
            }
            if (typeof(BarricadeData).IsAssignableFrom(t))
            {
                Type = 16;
                return;
            }
            if (typeof(StructureData).IsAssignableFrom(t))
            {
                Type = 17;
                return;
            }
            if (typeof(Guid).IsAssignableFrom(t))
            {
                Type = 18;
                return;
            }
            PropertyInfo? info1 = t
                .GetProperties(BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .FirstOrDefault(x => x.Name.IndexOf("asset", StringComparison.OrdinalIgnoreCase) != -1
                                     && typeof(Asset).IsAssignableFrom(x.PropertyType)
                                     && x.GetGetMethod(true) != null
                                     && Attribute.GetCustomAttribute(x, typeof(ObsoleteAttribute)) is null);

            if (info1 == null)
            {
                FieldInfo? info2 = t
                    .GetFields(BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .FirstOrDefault(x => x.Name.IndexOf("asset", StringComparison.OrdinalIgnoreCase) != -1
                                         && typeof(Asset).IsAssignableFrom(x.FieldType)
                                         && Attribute.GetCustomAttribute(x, typeof(ObsoleteAttribute)) is null);

                if (info2 != null)
                {
                    dm = new DynamicMethod("GetAssetName",
                        MethodAttributes.Static | MethodAttributes.Public,
                        CallingConventions.Standard, typeof(string), new Type[] { t }, t,
                        true);
                    dm.DefineParameter(1, ParameterAttributes.None, "value");
                    il = dm.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, info2);
                    il.EmitCall(OpCodes.Callvirt, typeof(Asset).GetProperty(nameof(Asset.FriendlyName), BindingFlags.Instance | BindingFlags.Public)!.GetGetMethod(false), null);
                    il.Emit(OpCodes.Ret);
                    ToStringFunc4 = (Func<T, string>)dm.CreateDelegate(typeof(Func<T, string>));
                    Type = 15;
                    return;
                }
            }
            else
            {
                dm = new DynamicMethod("GetAssetName",
                    MethodAttributes.Static | MethodAttributes.Public,
                    CallingConventions.Standard, typeof(string), new Type[] { t }, t,
                    true);
                dm.DefineParameter(1, ParameterAttributes.None, "value");
                il = dm.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.EmitCall(OpCodes.Callvirt, info1.GetGetMethod(true), null);
                il.EmitCall(OpCodes.Callvirt, typeof(Asset).GetProperty(nameof(Asset.FriendlyName), BindingFlags.Instance | BindingFlags.Public)!.GetGetMethod(), null);
                il.Emit(OpCodes.Ret);
                ToStringFunc4 = (Func<T, string>)dm.CreateDelegate(typeof(Func<T, string>));
                Type = 15;
                return;
            }
            if (typeof(UnityEngine.Object).IsAssignableFrom(t))
            {
                Type = 5;
                return;
            }
            if (t == typeof(Color))
            {
                Type = 6;
                return;
            }
            if (t == typeof(CSteamID))
            {
                Type = 7;
                return;
            }
            if (typeof(Type).IsAssignableFrom(t))
            {
                Type = 12;
                return;
            }
            MethodInfo? info = t.GetMethod("ToString", BindingFlags.Instance | BindingFlags.Public, null, TypeArray1, null);
            if (info == null || Attribute.GetCustomAttribute(info, typeof(ObsoleteAttribute)) is not null)
            {
                info = t.GetMethod("ToString", BindingFlags.Instance | BindingFlags.Public, null, TypeArray2, null);
                if (info == null || Attribute.GetCustomAttribute(info, typeof(ObsoleteAttribute)) is not null)
                {
                    info = t.GetMethod("ToString", BindingFlags.Instance | BindingFlags.Public, null, TypeArray3, null);
                    if (info == null || Attribute.GetCustomAttribute(info, typeof(ObsoleteAttribute)) is not null)
                    {
                        Type = TimeType(t) ? 50 : 0;
                    }
                    else
                    {
                        Type = 3;
                        if (TimeType(t))
                            Type += 50;
                        dm = new DynamicMethod("ToStringHelper",
                            MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard,
                            typeof(string), new Type[] { t, typeof(IFormatProvider) }, typeof(ToStringHelperClass<T>),
                            true);
                        dm.DefineParameter(1, ParameterAttributes.None, "value");
                        dm.DefineParameter(2, ParameterAttributes.None, "provider");
                        il = dm.GetILGenerator();
                        il.Emit(typeof(T).IsValueType ? OpCodes.Ldarga_S : OpCodes.Ldarg_0, 0);
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Callvirt, info);
                        il.Emit(OpCodes.Ret);
                        ToStringFunc3 = (Func<T, IFormatProvider, string>)dm.CreateDelegate(typeof(Func<T, IFormatProvider, string>));
                    }
                }
                else
                {
                    Type = 2;
                    if (TimeType(t))
                        Type += 50;
                    dm = new DynamicMethod("ToStringHelper",
                        MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(string), new Type[] { t, typeof(string) }, typeof(ToStringHelperClass<T>),
                        true);
                    dm.DefineParameter(1, ParameterAttributes.None, "value");
                    dm.DefineParameter(2, ParameterAttributes.None, "format");
                    il = dm.GetILGenerator();
                    il.Emit(typeof(T).IsValueType ? OpCodes.Ldarga_S : OpCodes.Ldarg_0, 0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Callvirt, info);
                    il.Emit(OpCodes.Ret);
                    ToStringFunc2 = (Func<T, string, string>)dm.CreateDelegate(typeof(Func<T, string, string>));
                }
            }
            else
            {
                Type = 1;
                if (TimeType(t))
                    Type += 50;
                dm = new DynamicMethod("ToStringHelper",
                    MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(string), new Type[] { t, typeof(string), typeof(IFormatProvider) }, typeof(ToStringHelperClass<T>),
                    true);
                dm.DefineParameter(1, ParameterAttributes.None, "value");
                dm.DefineParameter(2, ParameterAttributes.None, "format");
                dm.DefineParameter(3, ParameterAttributes.None, "provider");
                il = dm.GetILGenerator();
                il.Emit(typeof(T).IsValueType ? OpCodes.Ldarga_S : OpCodes.Ldarg_0, 0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Callvirt, info);
                il.Emit(OpCodes.Ret);
                ToStringFunc1 = (Func<T, string, IFormatProvider, string>)dm.CreateDelegate(typeof(Func<T, string, IFormatProvider, string>));
            }
        }
    }
    private static bool TimeType(Type t) => t == typeof(float) || t == typeof(int) || t == typeof(uint) || t == typeof(TimeSpan);
    protected class TranslationValue
    {
        internal ArgumentSpan[] ProcessedInnerPluralizers;
        internal ArgumentSpan[] ProcessedPluralizers;
        internal ArgumentSpan[] ProcessedNoTMProTagsPluralizers;
        internal ArgumentSpan[] ProcessedInnerNoTMProTagsPluralizers;
        internal string ProcessedInner; // has colors replaced and correct color tag type, outer color removed
        internal string Processed; // has colors replaced and correct color tag type.
        internal string ProcessedNoTMProTags; // tmpro tags are converted to unity tags
        internal string ProcessedInnerNoTMProTags; // tmpro tags are converted to unity tags
        private string? _console;
        public LanguageInfo Language { get; }
        public string Original { get; }
        public Color Color { get; internal set; }
        public bool RichText { get; }
        public string Console => RichText ? (_console ??= Util.RemoveRichText(ProcessedInner)) : Original;
        public ConsoleColor ConsoleColor => Util.GetClosestConsoleColor(Color);
        public TranslationValue(LanguageInfo language, string original, TranslationFlags flags)
        {
            Language = language;
            RichText = (flags & TranslationFlags.NoRichText) == 0;
            Original = original;
            ProcessValue(this, flags);
            ProcessedInnerPluralizers = GetPluralizers(ref ProcessedInner!);
            ProcessedPluralizers = GetPluralizers(ref Processed!);
            ProcessedNoTMProTagsPluralizers = GetPluralizers(ref ProcessedNoTMProTags!);
            ProcessedInnerNoTMProTagsPluralizers = GetPluralizers(ref ProcessedInnerNoTMProTags!);
            _console = null;
        }
        public TranslationValue(LanguageInfo language, string original, string processedInner, string processed, Color color)
        {
            RichText = original.IndexOf('>') != -1;
            Language = language;
            Original = original;
            ProcessedInner = processedInner;
            Processed = processed;
            Color = color;
            _console = null;
        }

        public string GetValue(bool inner, bool imgui)
        {
            if (imgui)
            {
                return inner ? ProcessedInnerNoTMProTags : ProcessedNoTMProTags;
            }

            return inner ? ProcessedInner : Processed;
        }

        internal void ResetConsole() => _console = null;
    }
    public static string Pluralize(LanguageInfo language, CultureInfo? culture, string word, TranslationFlags flags)
    {
        if ((flags & TranslationFlags.NoPlural) == TranslationFlags.NoPlural || word.Length < 3 || (flags & TranslationFlags.Plural) == 0)
            return word;
        //culture ??= LanguageAliasSet.GetCultureInfo(language);
        if (language.LanguageCode.Equals(LanguageAliasSet.EnglishUS, StringComparison.OrdinalIgnoreCase))
        {
            if (word.Equals("is", StringComparison.InvariantCulture))
                return "are";
            if (word.Equals("was", StringComparison.InvariantCulture))
                return "were";
            if (word.Equals("did", StringComparison.InvariantCulture))
                return "do";
            if (word.Equals("comes", StringComparison.InvariantCulture))
                return "come";
            if (word.Equals("it", StringComparison.InvariantCulture))
                return "they";
            if (word.Equals("a ", StringComparison.InvariantCulture) || word.Equals(" a", StringComparison.InvariantCulture))
                return string.Empty;
            if (word.Equals("an ", StringComparison.InvariantCulture) || word.Equals(" an", StringComparison.InvariantCulture))
                return string.Empty;
            string[] words = word.Split(' ');
            bool hOthWrds = words.Length > 1;
            string otherWords = string.Empty;
            string str = hOthWrds ? words[words.Length - 1] : word;
            if (str.Length < 2)
                return word;
            if (hOthWrds)
                otherWords = string.Join(" ", words, 0, words.Length - 1) + " ";
            bool isPCaps = char.IsUpper(str[0]);
            str = str.ToLowerInvariant();

            if (str.Equals("child", StringComparison.OrdinalIgnoreCase))
                return word + "ren";
            if (str.Equals("bunker", StringComparison.OrdinalIgnoreCase))
                return word + "s";
            if (str.Equals("goose", StringComparison.OrdinalIgnoreCase))
                return otherWords + (isPCaps ? "Geese" : "geese");
            if (str.Equals("wall", StringComparison.OrdinalIgnoreCase))
                return otherWords + (isPCaps ? "Wall" : "wall");
            if (str.Equals("tooth", StringComparison.OrdinalIgnoreCase))
                return otherWords + (isPCaps ? "Teeth" : "teeth");
            if (str.Equals("foot", StringComparison.OrdinalIgnoreCase))
                return otherWords + (isPCaps ? "Feet" : "feet");
            if (str.Equals("mouse", StringComparison.OrdinalIgnoreCase))
                return otherWords + (isPCaps ? "Mice" : "mice");
            if (str.Equals("die", StringComparison.OrdinalIgnoreCase))
                return otherWords + (isPCaps ? "Dice" : "dice");
            if (str.Equals("person", StringComparison.OrdinalIgnoreCase))
                return otherWords + (isPCaps ? "People" : "people");
            if (str.Equals("axis", StringComparison.OrdinalIgnoreCase))
                return otherWords + (isPCaps ? "Axes" : "axes");
            if (str.Equals("ammo", StringComparison.OrdinalIgnoreCase))
                return otherWords + (isPCaps ? "Ammo" : "ammo");
            if (str.Equals("radio", StringComparison.OrdinalIgnoreCase))
                return otherWords + (isPCaps ? "Radios" : "radios");
            if (str.Equals("mortar", StringComparison.OrdinalIgnoreCase))
                return otherWords + (isPCaps ? "Mortars" : "mortars");

            if (str.EndsWith("man", StringComparison.OrdinalIgnoreCase))
                return str.Substring(0, str.Length - 2) + (char.IsUpper(str[str.Length - 2]) ? "E" : "e") + str[str.Length - 1];

            char last = str[str.Length - 1];
            if (char.IsDigit(last))
                return word + "s";
            char slast = str[str.Length - 2];

            if (last is 's' or 'x' or 'z' || (last is 'h' && slast is 's' or 'c'))
                return word + "es";

            if (str.Equals("roof", StringComparison.OrdinalIgnoreCase) ||
                str.Equals("belief", StringComparison.OrdinalIgnoreCase) ||
                str.Equals("chef", StringComparison.OrdinalIgnoreCase) ||
                str.Equals("chief", StringComparison.OrdinalIgnoreCase)
                )
                goto s;
            if (last is 'f')
                return word.Substring(0, word.Length - 1) + "ves";

            if (last is 'e' && slast is 'f')
                return word.Substring(0, word.Length - 2) + "ves";

            if (last is 'y')
                if (!(slast is 'a' or 'e' or 'i' or 'o' or 'u'))
                    return word.Substring(0, word.Length - 1) + "ies";
                else goto s;

            if (str.Equals("photo", StringComparison.OrdinalIgnoreCase) ||
                str.Equals("piano", StringComparison.OrdinalIgnoreCase) ||
                str.Equals("halo", StringComparison.OrdinalIgnoreCase) ||
                str.Equals("volcano", StringComparison.OrdinalIgnoreCase)
               )
                goto s;

            if (last is 'o')
                return word + "es";

            if (last is 's' && slast is 'u')
                return word.Substring(0, word.Length - 2) + "i";

            if (last is 's' && slast is 'i')
                return word.Substring(0, word.Length - 2) + "es";

            if (str.Equals("sheep", StringComparison.OrdinalIgnoreCase) ||
                str.Equals("series", StringComparison.OrdinalIgnoreCase) ||
                str.Equals("species", StringComparison.OrdinalIgnoreCase) ||
                str.Equals("moose", StringComparison.OrdinalIgnoreCase) ||
                str.Equals("fish", StringComparison.OrdinalIgnoreCase) ||
                str.Equals("swine", StringComparison.OrdinalIgnoreCase) ||
                str.Equals("buffalo", StringComparison.OrdinalIgnoreCase) ||
                str.Equals("shrimp", StringComparison.OrdinalIgnoreCase) ||
                str.Equals("trout", StringComparison.OrdinalIgnoreCase) ||
                (str.EndsWith("craft", StringComparison.OrdinalIgnoreCase) && str.Length > 5) ||
                str.Equals("deer", StringComparison.OrdinalIgnoreCase))
                return word;

            s:
            return word + "s";
        }

        return word;
    }
    public static unsafe void ReplaceTMProRichText(ref string value, TranslationFlags flags)
    {
        if (value.Length < 6)
            return;
        string inp = value;
        int index = -8;
        int depth = 0;
        do
        {
            index = inp.IndexOf("</color>", index + 8, StringComparison.OrdinalIgnoreCase);
            if (index == -1) break;
            --depth;
        } while (index < inp.Length - 8);
        index = -2;
        while (true)
        {
            index = inp.IndexOf("<#", index + 2, StringComparison.OrdinalIgnoreCase);
            if (index == -1 || index >= inp.Length - 2) break;
            int nindex = inp.IndexOf('>', index + 2);
            if (nindex == -1) break;
            if (nindex - index > 10)
                continue;
            fixed (char* ptr = inp)
                inp = new string(ptr, 0, index) + "<color=#" + inp.Substring(index + 2, nindex - index - 2) + ">" + new string(ptr, nindex + 1, inp.Length - nindex - 1);
            ++depth;
            index += 5;
        }
        while (depth > 0)
        {
            --depth;
            inp += "</color>";
        }
        value = Util.RemoveTMProRichText(inp);
    }
    private static unsafe void ReplaceColors(ref string message)
    {
        int index = -2;
        if (message.Length < 4) return;
        while (true)
        {
            index = message.IndexOf("c$", index + 2, StringComparison.OrdinalIgnoreCase);
            if (index == -1 || index >= message.Length - 3) break;
            char next = message[index + 2];
            if (next is '$')
            {
                fixed (char* ptr = message)
                    message = new string(ptr, 0, index + 3) + new string(ptr, index + 4, message.Length - index - 5);
                --index;
                continue;
            }
            int nindex = message.IndexOf('$', index + 2);
            fixed (char* ptr = message)
            {
                string str = new string(ptr, index + 2, nindex - index - 2);
                int len = str.Length;
                str = UCWarfare.GetColorHex(str);
                if (index > 0 && message[index - 1] != '#')
                {
                    str = "#" + str;
                    ++len;
                }
                message = new string(ptr, 0, index) + str + new string(ptr, nindex + 1, message.Length - nindex - 1);
                nindex -= len + 3;
            }
            index = nindex;
        }
    }
    private static void ProcessValue(TranslationValue value, TranslationFlags flags)
    {
        string message = value.Original;
        ReplaceColors(ref message); // replace c$placeholder$s.
        Color color;
        bool noTmproRepl = false;

        // replaces tmpro rich text with unity rich text when needed
        if ((flags & TranslationFlags.NoRichText) == 0 && (flags & TranslationFlags.ReplaceTMProRichText) == TranslationFlags.ReplaceTMProRichText)
        {
            ReplaceTMProRichText(ref message, flags);
            noTmproRepl = true;
        }
        value.Processed = message;

        if ((flags & TranslationFlags.NoColorOptimization) == TranslationFlags.NoColorOptimization)
            goto noColor;

        // removes outer colors to be sent as a message color instead.
        // <#ffffff>
        if (message.Length > 2 && message.StartsWith("<#", StringComparison.OrdinalIgnoreCase) && message[2] != '{')
        {
            int endtag = message.IndexOf('>', 2);
            if (endtag == -1 || endtag is not 5 and not 6 and not 8 and not 10)
                goto noColor;
            string clr = message.Substring(2, endtag - 2);
            if (endtag == message.Length - 1)
                value.ProcessedInner = string.Empty;
            else if (message.EndsWith("</color>", StringComparison.OrdinalIgnoreCase))
                value.ProcessedInner = message.Substring(endtag + 1, message.Length - endtag - 1 - 8);
            else
                value.ProcessedInner = message.Substring(endtag + 1, message.Length - endtag - 1);
            color = clr.Hex();
            goto next;
        }
        // <color=#ffffff>
        if (message.Length > 8 && message.StartsWith("<color=#", StringComparison.OrdinalIgnoreCase) && message[8] != '{')
        {
            int endtag = message.IndexOf('>', 8);
            if (endtag == -1 || endtag is not 11 and not 12 and not 14 and not 16)
                goto noColor;
            string clr = message.Substring(8, endtag - 8);
            if (endtag == message.Length - 1)
                value.ProcessedInner = string.Empty;
            else if (message.EndsWith("</color>", StringComparison.OrdinalIgnoreCase))
                value.ProcessedInner = message.Substring(endtag + 1, message.Length - endtag - 1 - 8);
            else
                value.ProcessedInner = message.Substring(endtag + 1, message.Length - endtag - 1);
            color = clr.Hex();
            goto next;
        }

        noColor:
        color = UCWarfare.GetColor("default");
        value.ProcessedInner = message;
        next:
        value.ProcessedNoTMProTags = message;
        value.ProcessedInnerNoTMProTags = value.ProcessedInner;
        if ((flags & TranslationFlags.NoRichText) == 0 && !noTmproRepl)
        {
            ReplaceTMProRichText(ref value.ProcessedNoTMProTags, flags);
            ReplaceTMProRichText(ref value.ProcessedInnerNoTMProTags, flags);
        }

        value.Color = color;
        value.ResetConsole();
    }
    protected TranslationFlags GetFlags(ulong targetTeam, bool imgui = false)
    {
        TranslationFlags flags = targetTeam switch
        {
            3 => Flags | TranslationFlags.Team3,
            2 => Flags | TranslationFlags.Team2,
            1 => Flags | TranslationFlags.Team1,
            _ => Flags
        };
        if (imgui)
            flags |= TranslationFlags.TranslateWithUnityRichText;
        return flags;
    }
    protected string PrintFormatException(Exception ex, TranslationFlags flags)
    {
        if ((flags & TranslationFlags.ThrowMissingException) == TranslationFlags.ThrowMissingException)
        {
            throw new ArgumentException("[TRANSLATIONS] Error while formatting " + Key, ex);
        }

        L.LogError("[TRANSLATIONS] Error while formatting " + Key);
        L.LogError(ex);
        return InvalidValue;
    }
    internal static string Null(TranslationFlags flags) =>
        ((flags & TranslationFlags.NoRichText) == TranslationFlags.NoRichText)
            ? NullNoColor
            : (((flags & TranslationFlags.TranslateWithUnityRichText) == TranslationFlags.TranslateWithUnityRichText)
                ? NullColorUnity
                : NullColorTMPro);
    internal static LanguageInfo GetLanguageInfoForParameters(string? language)
    {
        if (language is null || language.Length == 0 || language.Length == 1 && language[0] is '0' or 'o' or 'O' || Data.LanguageDataStore.GetInfoCached(language) is not { } langInfo)
            return Localization.GetDefaultLanguage();
        return langInfo;
    }
    protected TranslationHelper StartTranslation(LanguageInfo? language, UCPlayer? target, ulong team, bool canUseIMGUI, bool inner, TranslationFlags flags)
    {
        language ??= target?.Locale.LanguageInfo ?? Localization.GetDefaultLanguage();
        CultureInfo culture = target?.Locale.CultureInfo ?? Localization.GetCultureInfo(language);
        bool imgui = canUseIMGUI && target is not null && target.Save.IMGUI;
        TranslationValue data = this[language];
        string rtn = data.GetValue(inner, imgui);
        AdjustForCulture(culture, ref rtn);
        return new TranslationHelper(data, imgui, inner, rtn, language, target, team, flags | GetFlags(team, imgui), culture);
    }
    protected TranslationHelper StartTranslation(UCPlayer? player, bool canUseIMGUI, bool inner)
    {
        LanguageInfo lang = player is null ? Localization.GetDefaultLanguage() : player.Locale.LanguageInfo;
        bool imgui = canUseIMGUI && player is not null && player.Save.IMGUI;
        CultureInfo culture = player?.Locale.CultureInfo ?? Data.LocalLocale;
        TranslationValue data = this[lang];
        string rtn = data.GetValue(inner, imgui);
        AdjustForCulture(culture, ref rtn);
        ulong team = player == null ? 0 : player.GetTeam();
        return new TranslationHelper(data, imgui, inner, rtn, lang, player, team, GetFlags(team, imgui), culture);
    }
    protected TranslationHelper StartTranslation(LanguageInfo? language, CultureInfo? culture, bool useIMGUI, bool inner, UCPlayer? target, ulong team, TranslationFlags flags)
    {
        language ??= target?.Locale.LanguageInfo ?? Localization.GetDefaultLanguage();
        culture ??= target?.Locale.CultureInfo ?? Localization.GetCultureInfo(language);
        TranslationValue data = this[language];
        string rtn = data.GetValue(inner, useIMGUI);
        AdjustForCulture(culture, ref rtn);
        return new TranslationHelper(data, useIMGUI, inner, rtn, language, target, team, flags | GetFlags(team, useIMGUI), culture);
    }
    public string Translate(LanguageInfo? language, bool canUseIMGUI = false)
        => Translate(language, Localization.GetCultureInfo(language), canUseIMGUI);
    public string Translate(UCPlayer? player, bool canUseIMGUI = false)
        => Translate(player?.Locale.LanguageInfo, player?.Locale.CultureInfo, canUseIMGUI && player is { Save.IMGUI: true });
    public string Translate(LanguageInfo? language, CultureInfo? culture, bool useIMGUI = false)
        => StartTranslation(language ?? Localization.GetDefaultLanguage(), culture, useIMGUI, false, null, 0, 0).PreformattedValue;
    public string Translate(UCPlayer? player, out Color color, bool canUseIMGUI = false)
        => Translate(player?.Locale.LanguageInfo, player?.Locale.CultureInfo, out color, canUseIMGUI && player is { Save.IMGUI: true });
    public string Translate(LanguageInfo? language, out Color color, bool useIMGUI = false)
        => Translate(language, Localization.GetCultureInfo(language), out color, useIMGUI);
    public string Translate(LanguageInfo? language, CultureInfo? culture, out Color color, bool useIMGUI = false)
    {
        TranslationHelper helper = StartTranslation(language ?? Localization.GetDefaultLanguage(), culture, useIMGUI, true, null, 0, 0);
        color = helper.ValueSet.Color;
        return helper.PreformattedValue;
    }
    [Conditional("FALSE")]
    [UsedImplicitly]
    private static void AdjustForCulture(CultureInfo? culture, ref string output)
    {
        // for later purposes
    }
    private string BaseUnsafeTranslate(Type t, Type[] gens, object?[] formatting, in TranslationHelper helper)
    {
        if (gens.Length > formatting.Length)
            Array.Resize(ref formatting, gens.Length);
        for (int i = 0; i < gens.Length; ++i)
        {
            object? v = formatting[i];
            if (v != null)
            {
                Type suppliedType = v.GetType();
                if (v is not null && !gens[i].IsAssignableFrom(suppliedType))
                {
                    if (gens[i] == typeof(string))
                    {
                        formatting[i] = typeof(ToStringHelperClass<>).MakeGenericType(suppliedType)
                            .GetMethod("ToString", BindingFlags.Static | BindingFlags.Public)!
                            .Invoke(null, new object[] { helper.Language, (t.GetField("_arg" + i.ToString(Data.AdminLocale) + "Fmt",
                                BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(this) as string)!, helper.Player!, helper.Culture, Flags });
                        continue;
                    }
                    throw new ArgumentException("Formatting argument at index " + i + " is not a type compatable with it's generic type!", nameof(formatting) + "[" + i + "]");
                }
            }
            else if (gens[i].IsValueType)
                throw new ArgumentException("Formatting argument at index " + i + " is null and its generic type is a value type!", nameof(formatting) + "[" + i + "]");
        }
        object[] newCallArr = new object[gens.Length + 1];
        Array.Copy(formatting, 0, newCallArr, 1, gens.Length);
        newCallArr[0] = helper;
        return (string)GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(x => x.Name.Equals("Translate", StringComparison.Ordinal) && x.GetParameters().Length == 1 + gens.Length)!.Invoke(this, newCallArr);
    }
    /// <exception cref="ArgumentException">Either not enough formatting arguments were supplied or </exception>
    internal string TranslateUnsafe(LanguageInfo? language, CultureInfo? culture, object?[] formatting, UCPlayer? target = null, ulong targetTeam = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
    {
        language ??= Localization.GetDefaultLanguage();
        culture ??= Localization.GetCultureInfo(language);
        Type t = GetType();
        Type[] gens = t.GenericTypeArguments;
        TranslationHelper helper = StartTranslation(language, culture, canUseIMGUI && target is { Save.IMGUI: true }, false, target, targetTeam, flags);
        if (gens.Length == 0 || formatting is null || formatting.Length == 0)
            return helper.PreformattedValue;
        return BaseUnsafeTranslate(t, gens, formatting, helper);
    }
    internal string TranslateUnsafe(LanguageInfo? language, CultureInfo? culture, out Color color, object?[] formatting, UCPlayer? target = null, ulong targetTeam = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
    {
        language ??= Localization.GetDefaultLanguage();
        culture ??= Localization.GetCultureInfo(language);
        Type t = GetType();
        Type[] gens = t.GenericTypeArguments;
        TranslationHelper helper = StartTranslation(language, culture, canUseIMGUI && target is { Save.IMGUI: true }, false, target, targetTeam, flags);
        color = helper.ValueSet.Color;
        if (gens.Length == 0 || formatting is null || formatting.Length == 0)
            return helper.PreformattedValue;
        return BaseUnsafeTranslate(t, gens, formatting, helper);
    }
    internal static void OnColorsReloaded()
    {
        for (int i = 0; i < T.Translations.Length; ++i)
            T.Translations[i].RefreshColors();
    }
    private static bool _first = true;
    private static void ReflectFormatDisplays()
    {
        if (FormatDisplays.Count > 0)
            FormatDisplays.Clear();
        foreach (FieldInfo field in Util.GetTypesSafe().SelectMany(x => x.GetFields(BindingFlags.Public | BindingFlags.Static)).Where(x => (x.IsLiteral || x.IsInitOnly) && x.FieldType == typeof(string)))
        {
            foreach (FormatDisplayAttribute attr in Attribute.GetCustomAttributes(field, typeof(FormatDisplayAttribute)).OfType<FormatDisplayAttribute>())
            {
                if (string.IsNullOrEmpty(attr.DisplayName)) continue;
                Type type = (attr.TypeSupplied ? (attr.TargetType ?? typeof(object)) : field.DeclaringType)!;
                if (field.GetValue(null) is not string str) continue;
                if (FormatDisplays.TryGetValue(str, out List<KeyValuePair<Type, FormatDisplayAttribute>> list))
                {
                    for (int i = 0; i < list.Count; ++i)
                    {
                        KeyValuePair<Type, FormatDisplayAttribute> kvp = list[i];
                        if (kvp.Key == type)
                        {
                            L.LogWarning("[TRANSLATIONS] Duplicate format \"" + str + "\" (" + attr.DisplayName + "/" + kvp.Value.DisplayName + ") for " + type.Name);
                            goto cont;
                        }
                    }

                    list.Add(new KeyValuePair<Type, FormatDisplayAttribute>(type, attr));
                }
                else
                {
                    FormatDisplays.Add(str, new List<KeyValuePair<Type, FormatDisplayAttribute>>(1)
                        { new KeyValuePair<Type, FormatDisplayAttribute>(type, attr) });
                }
            }
            cont:;
        }

        foreach (List<KeyValuePair<Type, FormatDisplayAttribute>> list in FormatDisplays.Values)
        {
            list.Sort((x, y) =>
            {
                if (x.Key != y.Key)
                {
                    if (y.Key.IsAssignableFrom(x.Key))
                        return 1;
                    if (x.Key.IsAssignableFrom(y.Key))
                        return -1;
                }
                return 0;
            });
        }
    }
    internal static async Task ReadTranslations(CancellationToken token = default)
    {
        await UCWarfare.ToUpdate(token);
        L.Log("Detected " + T.Translations.Length + " translations.", ConsoleColor.Magenta);
        DateTime start = DateTime.Now;
        if (!_first)
        {
            for (int i = 0; i < T.Translations.Length; ++i)
                T.Translations[i].ClearTranslations();
        }
        else
        {
            ReflectFormatDisplays();
            _first = false;
        }
        DirectoryInfo[] dirs = new DirectoryInfo(Data.Paths.LangStorage).GetDirectories();
        bool defRead = false;
        int amt = 0;
        for (int i = 0; i < dirs.Length; ++i)
        {
            DirectoryInfo langFolder = dirs[i];

            FileInfo info = new FileInfo(Path.Combine(langFolder.FullName, LocalFileName));
            if (!info.Exists) continue;
            string lang = langFolder.Name;
            bool isDefault = false;
            LanguageInfo? languageInfo;
            if (lang.IsDefault())
            {
                lang = L.Default;
                defRead = true;
                isDefault = true;
                languageInfo = Localization.GetDefaultLanguage();
            }
            else
            {
                if (!lang.Equals("Export", StringComparison.InvariantCultureIgnoreCase))
                {
                    languageInfo = Data.LanguageDataStore.GetInfoCached(lang);
                    if (languageInfo == null)
                    {
                        Data.LanguageDataStore.WriteWait();
                        try
                        {
                            foreach (LanguageInfo cachedInfo in Data.LanguageDataStore.Languages)
                            {
                                if (string.Equals(lang, cachedInfo.FallbackTranslationLanguageCode))
                                {
                                    languageInfo = cachedInfo;
                                    break;
                                }
                            }
                        }
                        finally
                        {
                            Data.LanguageDataStore.WriteRelease();
                        }
                    }

                    if (languageInfo != null)
                        goto foundLanguage;

                    L.LogWarning("Unknown language: " + lang + ", skipping directory.");
                }
                continue;
            }

            foundLanguage:
            using (PropertiesReader reader = new PropertiesReader(info.FullName))
            {
                while (reader.TryReadPair(out string key, out string value))
                {
                    for (int j = 0; j < T.Translations.Length; ++j)
                    {
                        Translation t = T.Translations[j];
                        if (t.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (t.AttributeData is not { IsPrioritizedTranslation: false })
                                ++amt;
                            t.AddTranslation(languageInfo, value);

                            if (!T.AllLanguages.Contains(langFolder.Name, StringComparer.Ordinal))
                                T.AllLanguages.Add(langFolder.Name);

                            if (!languageInfo.HasTranslationSupport)
                            {
                                languageInfo.HasTranslationSupport = true;
                                await Data.LanguageDataStore.AddOrUpdateInfo(languageInfo, token).ConfigureAwait(false);
                                await UCWarfare.ToUpdate(token);
                            }

                            goto n;
                        }
                    }
                    L.LogWarning("[TRANSLATIONS] Unknown translation key: " + key + " in " + lang + " translation file.");
                n:;
                }
            }

            WriteLanguage(languageInfo, null, writeAll: isDefault);
            L.Log("Loaded " + amt + " translations for " + lang + ".", ConsoleColor.Magenta);
            languageInfo.ClearSection(TranslationSection.Primary);
            languageInfo.IncrementSection(TranslationSection.Primary, amt);
            amt = 0;
        }

        if (!defRead)
        {
            WriteDefaultTranslations();
        }

        amt = 0;
        for (int j = 0; j < T.Translations.Length; ++j)
        {
            Translation t = T.Translations[j];
            if (t._data is not null && t._data.Length > 0)
            {
                for (int i = 0; i < t._data.Length; ++i)
                {
                    if (t._data[i].Language.IsDefault)
                        goto c;
                }
            }
            ++amt;
            t.AddTranslation(Localization.GetDefaultLanguage(), t._defaultData.Original);
        c:;
        }
        if (amt > 0 && defRead)
            L.Log("Added " + amt + " missing default translations for " + L.Default + ".", ConsoleColor.Yellow);
        L.Log("Loaded translations in " + (DateTime.Now - start).TotalMilliseconds.ToString("F1", Data.AdminLocale) + "ms", ConsoleColor.Magenta);
        OnReload?.Invoke();
    }
    private static void WriteDefaultTranslations()
    {
        DirectoryInfo dir = new DirectoryInfo(Path.Combine(Data.Paths.LangStorage, L.Default));
        if (!dir.Exists)
            dir.Create();
        FileInfo info = new FileInfo(Path.Combine(dir.FullName, LocalFileName));
        using FileStream str = new FileStream(info.FullName, FileMode.Create, FileAccess.Write, FileShare.Read);
        using StreamWriter writer = new StreamWriter(str);
        string? lastSection = null;
        foreach (Translation t in T.Translations.OrderBy(x => x.AttributeData?.Section ?? "~", StringComparer.OrdinalIgnoreCase))
        {
            WriteTranslation(writer, t, t._defaultData.Original, ref lastSection);
        }

        writer.Flush();
    }
    private static void WriteLanguage(LanguageInfo language, string? path = null, bool writeAll = false, bool missingOnly = false, bool excludeNonPrioritized = false)
    {
        if (path == null)
        {
            DirectoryInfo dir = new DirectoryInfo(Path.Combine(Data.Paths.LangStorage, language.LanguageCode));
            if (!dir.Exists)
                dir.Create();
            FileInfo info = new FileInfo(Path.Combine(dir.FullName, LocalFileName));
            path = info.FullName;
        }
        using FileStream str = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using StreamWriter writer = new StreamWriter(str, System.Text.Encoding.UTF8);
        string? lastSection = null;
        foreach (Translation t in T.Translations.OrderBy(x => x.AttributeData?.Section ?? "~", StringComparer.OrdinalIgnoreCase))
        {
            if (excludeNonPrioritized && t.AttributeData is { IsPrioritizedTranslation: false })
                continue;
            string? val = null;
            if (t._data != null)
            {
                for (int i = 0; i < t._data.Length; ++i)
                {
                    if (t._data[i].Language == language)
                        val = t._data[i].Original;
                }
            }

            if (missingOnly && val != null)
                continue;
            if (val == null && writeAll && t._defaultData != null)
            {
                val = t._defaultData.Original;
            }
            
            if (val is not null)
                WriteTranslation(writer, t, val, ref lastSection);
        }

        writer.Flush();
    }

    private static byte[]? _exportReadmeUtf8;
    private static byte[] ExportReadmeUTF8 => _exportReadmeUtf8 ??= @"# Files
## Enums (Folder)
Enums (enumerations) are a list of text values that correspond represent an option. For example, you could have an enum for direction:
```cs
enum Direction
{
    North,
    South,
    East,
    West
}
```
Some enums have translations, all of which are in the Enums folder. The **%NAME%** option is the name of the enum. In English, the name of this one would be ""Direction"". Each value also has an option.
<br>See **JSON** section below.

## translations.properties
Primary translations file. Contains the main translations used for chat messages, UI, etc.
<br><br>See **Properties** and **Rich Text** sections below.
### Extra Notation
**Arguments**

Zero based, surrounded in curly brackets (`{}`).
<br> Example (Translation<`int`, `ItemAsset`>): `Given you {0}x {1}.`
<br>  -> `Given you 4x M4A1.`


**Formatting**

***Default Formatting (T Constants)***
```
• FormatPlural              ""$plural$""  See Below
• FormatUppercase           ""upper""     Turns the argument UPPERCASE.
• FormatLowercase           ""lower""     Turns the argument lowercase.
• FormatPropercase          ""proper""    Turns the argument ProperCase.
• FormatRarityColor         ""rarity""    Colors assets to their rarity color.
• FormatTimeLong            ""tlong""     Turns time to: 3 minutes and 4 seconds, etc.
• FormatTimeShort_MM_SS     ""tshort1""   Turns time to: 03:04, etc.
• FormatTimeShort_HH_MM_SS  ""tshort2""   Turns time to: 01:03:04, etc.
   Time can be int, uint, float (all in seconds), or TimeSpan
```
Other formats are stored in the most prominant class of the interface (`UCPlayer` for `IPlayer`, `FOB` for `IDeployable`, etc.)
<br>Anything that would work in `T[N].ToString(string, IFormatProvider)` will work here.

<br>**Color substitution from color dictionary**

`c$value$` will be replaced by the color `value` from the color dictionary on startup.
<br> Example: `You need 100 more <#c$credits$>credits</color>.`

<br>**Conditional pluralization of existing terms**

`${p:arg:text}`  will replace text with the plural version of text if `{arg}` is not one.
`${p:arg:text!}` will replace text with the plural version of text if `{arg}` is one.
 Example: `There ${p:0:is} {0} ${p:0:apple}, ${p:0:it} ${p:0:is} ${p:0:a }${p:0:fruit}. ${p:0:It} ${p:0:taste!} good.`
  -> ({0} = 1) `There is 1 apple, it is a fruit. It tastes good.`
  -> ({0} = 3) `There are 3 apples, they are fruits. They taste good.`

<br>**Conditional pluralization of argument values**

Using the format: `'xxxx' + FormatPlural` will replace the value for that argument with the plural version.
<br> Example: `You cant place {0} here.` arg0Fmt: `RarityFormat + FormatPlural`
<br>  -> `You can't place <#xxxxx>FOB Radios</color> here.`
<br>
<br>Using the format: `'xxxx' + FormatPlural + '{arg}'` will replace the value for that argument with the plural version if `{arg}` is not one.
<br> Example: `There are {0} {1} already on this FOB.` arg1Fmt: `RarityFormat + FormatPlural + {0}`
<br>  -> (4, FOB Radio Asset) `There are 4 <#xxxxx>FOB Radios</color> already on this FOB.`


## factions.properties
Contains the faction translations, including names, short names, and abbreviations.
<br>See **Properties** section below.

## kits.properties
Contains the kit sign text translations.
<br>\<br> is used as a line break for sign texts, causing the name to go on two lines.
<br>See **Properties** section below.

## traits.properties
Contains the trait sign text and description translations.
<br>See **Properties** section below.

## deaths.json
Stores all possible death messages for each set of available data/flags.
<br>There is a comment at the beginning of the file explaining the flags.
<br>See **JSON** section below.

## readme.md
This file.

# Properties
Use IDE Format: **Java Properties** available.
<br>Anything starting with a **!** or a **#** is a comment, and ignored by the file reader.
```properties
# Comment
! Comment
Key: Value
```
Keys should be left as is, and values should be translated.

# JSON
Use IDE Format: **JSONC**, or **JSON** if JSONC is not available.
<br>Anything starting with a **//** or between **/\*** and **\*/** is a comment, and ignored by the file reader.
```jsonc
/*
    Multi-line
    comment
*/
{
    /* Comment */
    ""key1"": ""value1"",
    // Comment
    ""key2"": ""value2"",
}
```
Keys should be left as is, and values should be translated.<br>
For deaths, the `death-cause`, `custom-key`, `item-cause`, and `vehicle-cause` key/value pairs should not be translated.

# Rich Text
We use rich text to format our translations. The most common you'll see is \<color> tags, but there are others (\<b>, \<i>, \<sub>).
<br>These should not be removed.

Example:
```properties
# Description: Sent when a player tries to abandon a damaged vehicle.
# Formatting Arguments:
#  {0} - [InteractableVehicle]
# Default Value: <#ff8c69>Your <#cedcde>{0}</color> is damaged, repair it before returning it to the yard.
AbandonDamaged: <#ff8c69>Al tau <#cedcde>{0}</color> este deteriorat, reparal inainte sa il returnezi.
```
Notice how the default value was translated but the rich text tags were left.
<br>More info about rich text here: [TMPro Documentation](http://digitalnativestudios.com/textmeshpro/docs/rich-text/).

# Formatting Arguments
Also notice the `{n}` formatting placeholders. These are replaced by translation arguments, which are sometimes explained in the comments above.
```properties
#  {n} - [Type] (Formatting) Description
```

# Other
Please leave in-game terms such as **FOB**, **Rally**, **Build**, **Ammo**, and other item names in English."u8.ToArray();
    public static async Task ExportLanguage(LanguageInfo? language, bool missingOnly, bool excludeNonPrioritized, CancellationToken token = default)
    {
        language ??= Localization.GetDefaultLanguage();
        await ReloadCommand.ReloadTranslations(token).ConfigureAwait(false);
        await UCWarfare.ToUpdate(token);

        DirectoryInfo dir = new DirectoryInfo(Path.Combine(Data.Paths.LangStorage, "Export"));
        if (dir.Exists)
            dir.Delete(true);
        dir.Create();
        FileInfo file = new FileInfo(Path.Combine(dir.FullName, LocalFileName));
        WriteLanguage(language, file.FullName, true, missingOnly, excludeNonPrioritized);
        Deaths.Localization.Write(Path.Combine(dir.FullName, "deaths.json"), language, true);
        Localization.WriteEnums(language, Path.Combine(dir.FullName, "Enums"), true, true);
        KitEx.WriteKitLocalization(language, Path.Combine(dir.FullName, "kits.properties"), true);
        TeamManager.WriteFactionLocalization(language, Path.Combine(dir.FullName, "factions.properties"), true);
        TraitManager.WriteTraitLocalization(language, Path.Combine(dir.FullName, "traits.properties"), true);
        using FileStream str = new FileStream(Path.Combine(dir.FullName, "README.md"), FileMode.Create, FileAccess.Write, FileShare.Read);
        str.Write(ExportReadmeUTF8, 0, ExportReadmeUTF8.Length);
    }
    private static void WriteTranslation(StreamWriter writer, Translation t, string val, ref string? lastSection)
    {
        if (string.IsNullOrEmpty(val)) return;
        string? sect = t.AttributeData?.Section;
        if (sect != lastSection)
        {
            lastSection = sect;
            if (writer.BaseStream.Position > 0)
                writer.WriteLine();
            if (sect is not null)
                writer.WriteLine("! " + sect + " !");
        }
        writer.WriteLine();
        if (t.AttributeData is not null)
        {
            if (t.AttributeData.Description is not null)
                writer.WriteLine("# Description: " + t.AttributeData.Description.RemoveMany(true, '\r', '\n'));
        }

        Type tt = t.GetType();
        Type[] gen = tt.GetGenericArguments();
        if (gen.Length > 0)
            writer.WriteLine("# Formatting Arguments:");
        for (int i = 0; i < gen.Length; ++i)
        {
            Type type = gen[i];
            string vi = i.ToString(Data.AdminLocale);
            string fmt = "#  " + "{" + vi + "} - [" + ToString(type, Localization.GetDefaultLanguage(), null, null, TranslationFlags.NoColorOptimization) + "]";

            FieldInfo? info = tt.GetField("_arg" + vi + "Fmt", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo? info2 = tt.GetField("_arg" + vi + "PluralExp", BindingFlags.NonPublic | BindingFlags.Instance);
            if (info != null && info.GetValue(t) is string fmt2)
            {
                short pluralType;
                if (info2 != null)
                    pluralType = (short)info2.GetValue(t);
                else
                    pluralType = -1;
                if (FormatDisplays.TryGetValue(fmt2, out List<KeyValuePair<Type, FormatDisplayAttribute>> list))
                {
                    for (int j = 0; j < list.Count; ++j)
                    {
                        if (list[j].Key == type)
                        {
                            fmt2 = list[j].Value.DisplayName;
                            goto next;
                        }
                    }
                    for (int j = 0; j < list.Count; ++j)
                    {
                        if (list[j].Key.IsAssignableFrom(type))
                        {
                            fmt2 = list[j].Value.DisplayName;
                            goto next;
                        }
                    }
                }
                next:
                if (!string.IsNullOrEmpty(fmt2))
                    fmt += " (" + fmt2 + ")";
                if (pluralType != -1)
                {
                    if (pluralType == short.MaxValue)
                        fmt += " (Plural)";
                    else
                        fmt += " (Plural when {" + pluralType + "} is not 1)";
                }
            }

            if (t.AttributeData is not null && t.AttributeData.FormattingDescriptions is not null && i < t.AttributeData.FormattingDescriptions.Length && !string.IsNullOrEmpty(t.AttributeData.FormattingDescriptions[i]))
                fmt += " " + t.AttributeData.FormattingDescriptions[i].RemoveMany(true, '\r', '\n');

            writer.WriteLine(fmt);
        }
        if (!val.Equals(t._defaultData.Original, StringComparison.Ordinal))
        {
            writer.WriteLine("# Default Value: " + t._defaultData.Original.Replace(@"\", @"\\").Replace("\n", @"\n").Replace("\r", @"\r").Replace("\t", @"\t"));
        }
        val = val.Replace(@"\", @"\\").Replace("\n", @"\n").Replace("\r", @"\r").Replace("\t", @"\t");

        writer.Write(t.Key);
        writer.Write(": ");
        if (val[0] == ' ')
            val = @"\" + val;
        writer.WriteLine(val);
    }
    public static Translation? FromSignId(string signId)
    {
        if (signId.StartsWith("sign_", StringComparison.Ordinal))
            signId = signId.Substring(5);

        if (T.Signs.TryGetValue(signId, out Translation tr))
            return tr;

        foreach (Translation tr2 in T.Signs.Values)
        {
            if (signId.Equals(tr2.AttributeData?.SignId, StringComparison.OrdinalIgnoreCase))
                return tr2;
        }
        foreach (Translation tr2 in T.Signs.Values)
        {
            if (signId.Equals(tr2.Key, StringComparison.OrdinalIgnoreCase))
                return tr2;
        }
        return null;
    }
    protected static void CheckPluralFormatting(ref short val, ref string? fmt)
    {
        if (!string.IsNullOrEmpty(fmt))
        {
            int ind1 = fmt!.IndexOf(T.FormatPlural, StringComparison.Ordinal);
            if (ind1 != -1)
            {
                if (fmt[fmt.Length - 1] == '}')
                {
                    int ind2 = fmt.LastIndexOf('{', fmt.Length - 2);
                    if (ind2 < fmt.Length - 4 || ind2 > fmt.Length - 3)
                    {
                        val = short.MaxValue;
                        return;
                    }
                    if (int.TryParse(fmt.Substring(ind2 + 1, ind2 + 4 - fmt.Length), NumberStyles.Number, Data.AdminLocale, out int num))
                    {
                        fmt = fmt.Substring(0, ind1);
                        val = (short)num;
                        return;
                    }
                }

                fmt = fmt.Substring(0, ind1);
                val = short.MaxValue;
                return;
            }
        }

        val = -1;
    }
    protected static bool IsOne<T>(T? value) => value is not null && ToStringHelperClass<T>.IsOne is not null && ToStringHelperClass<T>.IsOne(value);
}

[Flags]
public enum TranslationFlags
{
    None = 0,

    /// <summary>Tells the translator not to search for translations in other languages if not found in the current language, and instead just return the field name.
    /// <para>If the current language isn't <see cref="L.Default"/>, a default will not be chosen.</para></summary>
    DontDefaultToOtherLanguage = 1,

    /// <summary>Tells the translator to an <see cref="ArgumentException"/> if the translation isn't found.
    /// <para>Combine with <see cref="DontDefaultToOtherLanguage"/> to make it require the active language to have the tranlation.</para></summary>
    ThrowMissingException = 2,

    /// <summary>Warnings about unused or missing parameters are not sent on load.</summary>
    SuppressWarnings = 4,

    /// <summary>Tells the translator to prioritize using base Unity rich text instead of TMPro rich text.</summary>
    TranslateWithUnityRichText = 8,

    /// <summary>Tells the translator to replace &lt;#ffffff&gt; format with &lt;color=#ffffff&gt;.</summary>
    ReplaceTMProRichText = 16,

    /// <summary>Tells the translator to target Unity rich text instead of TMPro rich text and replace &lt;#ffffff&gt; tags with &lt;color=#ffffff&gt; tags.</summary>
    UseUnityRichText = TranslateWithUnityRichText | ReplaceTMProRichText,

    /// <summary>Tells the translator not to look for color tags on this translation. Useful for UI elements mainly.</summary>
    NoColorOptimization = 32,

    /// <summary>Tells the translator to translate the messsage for each player when broadcasted.</summary>
    PerPlayerTranslation = 64,

    /// <summary>Checks for any &lt;size&gt tags.</summary>
    TMProSign = 128,

    /// <summary>Don't use this in a constructor, used to tell translator functions that the translation is for team 1.</summary>
    Team1 = 256,

    /// <summary>Don't use this in a constructor, used to tell translator functions that the translation is for team 2.</summary>
    Team2 = 512,

    /// <summary>Don't use this in a constructor, used to tell translator functions that the translation is for team 3.</summary>
    Team3 = 1024,

    /// <summary>Tells <see cref="ChatManager"/> to send chat messages with RichText set to false.</summary>
    NoRichText = 2048,

    /// <summary>Use for translations to be used on TMPro UI. Skips color scanning.</summary>
    TMProUI = NoColorOptimization,

    /// <summary>Use for translations to be used on non-TMPro UI. Skips color optimization and convert to &lt;color=#ffffff&gt; format.</summary>
    UnityUI = NoColorOptimization | UseUnityRichText,

    /// <summary>Use for translations to be used on non-TMPro UI. Skips color optimization and convert to &lt;color=#ffffff&gt; format, doesn't replace already existing TMPro tags.</summary>
    UnityUINoReplace = NoColorOptimization | TranslateWithUnityRichText,

    /// <summary>Tells the translator to format the term plurally, this will be automatically applied to individual arguments if the format is <see cref="T.FormatPlural"/>.</summary>
    Plural = 4096,

    /// <summary>Tells the translator to not try to turn arguments plural.</summary>
    NoPlural = 8192,

    /// <summary>Don't use this in a constructor, used to tell translator functions that the translation is going to be sent in chat.</summary>
    ForChat = 16384,

    /// <summary>Don't use this in a constructor, used to tell translator functions that the translation is going to be sent on a sign.</summary>
    ForSign = 32768,

    /// <summary>Tells the translator to translate the messsage for each team when broadcasted.</summary>
    PerTeamTranslation = 65536,

    /// <summary>Overrides any Colorize calls to not add color.</summary>
    SkipColorize = 131072
}

public interface ITranslationArgument
{
    string Translate(LanguageInfo language, string? format, UCPlayer? target, CultureInfo? culture, ref TranslationFlags flags);
}
public sealed class Translation<T0> : Translation
{
    private readonly string? _arg0Fmt;
    private readonly short _arg0PluralExp = -1;
    public Translation(string @default) : base(@default) { }
    public Translation(string @default, TranslationFlags flags) : base(@default, flags) { }
    public Translation(string @default, string arg0Fmt) : this(@default, default, arg0Fmt) { }
    public Translation(string @default, TranslationFlags flags, string arg0Fmt) : base(@default, flags)
    {
        _arg0Fmt = arg0Fmt;
        CheckPluralFormatting(ref _arg0PluralExp, ref _arg0Fmt);
    }

    private string Translate(in TranslationHelper data, T0 arg0)
    {
        string value = data.PreformattedValue;
        ArgumentSpan[] pluralizers = data.Pluralizers;
        for (int i = 0; i < pluralizers.Length; ++i)
        {
            ref ArgumentSpan span = ref pluralizers[i];
            int offset = 0;
            if (span.Argument == 0 && !IsOne(arg0) ^ span.Inverted)
            {
                L.LogDebug($"Pluralizing from {span.Argument}: Offset: {value}");
                span.Pluralize(in data, ref value, ref offset);
            }
            else
            {
                L.LogDebug($"Not plural: {span.Argument}.");
            }
        }
        try
        {
            return string.Format(value, ToString(arg0, data.Language, _arg0Fmt, data.Player, _arg0PluralExp == -1 ? data.Flags : CheckPlurality(_arg0PluralExp, data.Flags, in arg0)));
        }
        catch (FormatException ex)
        {
            return PrintFormatException(ex, data.Flags);
        }
    }
    public string Translate(string value, LanguageInfo? language, T0 arg0, UCPlayer? target, ulong targetTeam, TranslationFlags flags, bool useIMGUI = false)
    {
        return Translate(new TranslationHelper(null!, useIMGUI, false, value, language ?? Localization.GetDefaultLanguage(), target, targetTeam, Flags | flags | GetFlags(targetTeam, useIMGUI), target?.Locale.CultureInfo ?? Localization.GetCultureInfo(language)), arg0);
    }
    private static TranslationFlags CheckPlurality(short expectation, TranslationFlags flags, in T0 arg0)
    {
        if (expectation == -1)
            return flags;
        flags |= TranslationFlags.Plural;

        if (expectation == 0 && IsOne(arg0)) return flags | TranslationFlags.NoPlural;
        return flags;
    }
    public string Translate(LanguageInfo? language, CultureInfo? culture, T0 arg0, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
        => Translate(StartTranslation(language, culture, canUseIMGUI && target != null && target.Save.IMGUI, false, target, team, flags), arg0);
    public string Translate(LanguageInfo? language, T0 arg0, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
        => Translate(StartTranslation(language, target, team, canUseIMGUI, false, flags), arg0);
    public string Translate(LanguageInfo? language, T0 arg0, out Color color, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
    {
        TranslationHelper helper = StartTranslation(language, target, team, canUseIMGUI, true, flags);
        color = helper.ValueSet.Color;
        return Translate(helper, arg0);
    }
    public string Translate(UCPlayer? player, bool canUseIMGUI, T0 arg0)
        => Translate(StartTranslation(player, canUseIMGUI, false), arg0);
}
public sealed class Translation<T0, T1> : Translation
{
    private readonly string? _arg0Fmt;
    private readonly string? _arg1Fmt;
    private readonly short _arg0PluralExp = -1;
    private readonly short _arg1PluralExp = -1;
    public Translation(string @default) : base(@default) { }
    public Translation(string @default, TranslationFlags flags) : base(@default, flags) { }
    public Translation(string @default, string? arg0Fmt = null, string? arg1Fmt = null) : this(@default, default, arg0Fmt, arg1Fmt) { }
    public Translation(string @default, TranslationFlags flags, string? arg0Fmt = null, string? arg1Fmt = null) : base(@default, flags)
    {
        _arg0Fmt = arg0Fmt;
        _arg1Fmt = arg1Fmt;
        CheckPluralFormatting(ref _arg0PluralExp, ref _arg0Fmt);
        CheckPluralFormatting(ref _arg1PluralExp, ref _arg1Fmt);
    }
    private string Translate(in TranslationHelper data, T0 arg0, T1 arg1)
    {
        string value = data.PreformattedValue;
        ArgumentSpan[] pluralizers = data.Pluralizers;
        for (int i = 0; i < pluralizers.Length; ++i)
        {
            ref ArgumentSpan span = ref pluralizers[i];
            int offset = 0;
            if (!(span.Argument switch
                {
                    0 => IsOne(arg0),
                    1 => IsOne(arg1),
                    _ => true
                }) ^ span.Inverted)
            {
                span.Pluralize(in data, ref value, ref offset);
            }
            else
            {
                L.LogDebug($"Not plural: {span.Argument}.");
            }
        }
        try
        {
            return string.Format(value,
                ToString(arg0, data.Language, _arg0Fmt, data.Player, _arg0PluralExp == -1 ? data.Flags : CheckPlurality(_arg0PluralExp, data.Flags, arg0, arg1)),
                ToString(arg1, data.Language, _arg1Fmt, data.Player, _arg1PluralExp == -1 ? data.Flags : CheckPlurality(_arg1PluralExp, data.Flags, arg0, arg1))
            );
        }
        catch (FormatException ex)
        {
            return PrintFormatException(ex, data.Flags);
        }
    }
    public string Translate(string value, LanguageInfo? language, T0 arg0, T1 arg1, UCPlayer? target, ulong targetTeam, TranslationFlags flags, bool useIMGUI = false)
    {
        return Translate(new TranslationHelper(null!, useIMGUI, false, value, language ?? Localization.GetDefaultLanguage(), target, targetTeam, Flags | flags | GetFlags(targetTeam, useIMGUI), target?.Locale.CultureInfo ?? Localization.GetCultureInfo(language)), arg0, arg1);
    }
    private static TranslationFlags CheckPlurality(short expectation, TranslationFlags flags, in T0 arg0, in T1 arg1)
    {
        if (expectation == -1)
            return flags;
        flags |= TranslationFlags.Plural;
        return expectation switch
        {
            0 => IsOne(arg0) ? flags | TranslationFlags.NoPlural : flags,
            1 => IsOne(arg1) ? flags | TranslationFlags.NoPlural : flags,
            _ => flags
        };
    }
    public string Translate(LanguageInfo? language, CultureInfo? culture, T0 arg0, T1 arg1, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
        => Translate(StartTranslation(language, culture, canUseIMGUI && target != null && target.Save.IMGUI, false, target, team, flags), arg0, arg1);
    public string Translate(LanguageInfo? language, T0 arg0, T1 arg1, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
        => Translate(StartTranslation(language, target, team, canUseIMGUI, false, flags), arg0, arg1);
    public string Translate(LanguageInfo? language, T0 arg0, T1 arg1, out Color color, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
    {
        TranslationHelper helper = StartTranslation(language, target, team, canUseIMGUI, true, flags);
        color = helper.ValueSet.Color;
        return Translate(helper, arg0, arg1);
    }
    public string Translate(UCPlayer? player, bool canUseIMGUI, T0 arg0, T1 arg1)
        => Translate(StartTranslation(player, canUseIMGUI, false), arg0, arg1);
}
public sealed class Translation<T0, T1, T2> : Translation
{
    private readonly string? _arg0Fmt;
    private readonly string? _arg1Fmt;
    private readonly string? _arg2Fmt;
    private readonly short _arg0PluralExp = -1;
    private readonly short _arg1PluralExp = -1;
    private readonly short _arg2PluralExp = -1;
    public Translation(string @default) : base(@default) { }
    public Translation(string @default, TranslationFlags flags) : base(@default, flags) { }
    public Translation(string @default, string? arg0Fmt = null, string? arg1Fmt = null, string? arg2Fmt = null) : this(@default, default, arg0Fmt, arg1Fmt, arg2Fmt) { }
    public Translation(string @default, TranslationFlags flags, string? arg0Fmt = null, string? arg1Fmt = null, string? arg2Fmt = null) : base(@default, flags)
    {
        _arg0Fmt = arg0Fmt;
        _arg1Fmt = arg1Fmt;
        _arg2Fmt = arg2Fmt;
        CheckPluralFormatting(ref _arg0PluralExp, ref _arg0Fmt);
        CheckPluralFormatting(ref _arg1PluralExp, ref _arg1Fmt);
        CheckPluralFormatting(ref _arg2PluralExp, ref _arg2Fmt);
    }
    private string Translate(in TranslationHelper data, T0 arg0, T1 arg1, T2 arg2)
    {
        string value = data.PreformattedValue;
        ArgumentSpan[] pluralizers = data.Pluralizers;
        for (int i = 0; i < pluralizers.Length; ++i)
        {
            ref ArgumentSpan span = ref pluralizers[i];
            int offset = 0;
            if (!(span.Argument switch
                {
                    0 => IsOne(arg0),
                    1 => IsOne(arg1),
                    2 => IsOne(arg2),
                    _ => true
                }) ^ span.Inverted)
            {
                L.LogDebug($"Pluralizing from {span.Argument}: Offset: {value}");
                span.Pluralize(in data, ref value, ref offset);
            }
            else
            {
                L.LogDebug($"Not plural: {span.Argument}.");
            }
        }
        try
        {
            return string.Format(value,
                ToString(arg0, data.Language, _arg0Fmt, data.Player, _arg0PluralExp == -1 ? data.Flags : CheckPlurality(_arg0PluralExp, data.Flags, arg0, arg1, arg2)),
                ToString(arg1, data.Language, _arg1Fmt, data.Player, _arg1PluralExp == -1 ? data.Flags : CheckPlurality(_arg1PluralExp, data.Flags, arg0, arg1, arg2)),
                ToString(arg2, data.Language, _arg2Fmt, data.Player, _arg2PluralExp == -1 ? data.Flags : CheckPlurality(_arg2PluralExp, data.Flags, arg0, arg1, arg2))
            );
        }
        catch (FormatException ex)
        {
            return PrintFormatException(ex, data.Flags);
        }
    }
    public string Translate(string value, LanguageInfo? language, T0 arg0, T1 arg1, T2 arg2, UCPlayer? target, ulong targetTeam, TranslationFlags flags, bool useIMGUI = false)
    {
        return Translate(new TranslationHelper(null!, useIMGUI, false, value, language ?? Localization.GetDefaultLanguage(), target, targetTeam, Flags | flags | GetFlags(targetTeam, useIMGUI), target?.Locale.CultureInfo ?? Localization.GetCultureInfo(language)), arg0, arg1, arg2);
    }
    private static TranslationFlags CheckPlurality(short expectation, TranslationFlags flags, in T0 arg0, in T1 arg1, in T2 arg2)
    {
        if (expectation == -1)
            return flags;
        flags |= TranslationFlags.Plural;
        return expectation switch
        {
            0 => IsOne(arg0) ? flags | TranslationFlags.NoPlural : flags,
            1 => IsOne(arg1) ? flags | TranslationFlags.NoPlural : flags,
            2 => IsOne(arg2) ? flags | TranslationFlags.NoPlural : flags,
            _ => flags
        };
    }
    public string Translate(LanguageInfo? language, CultureInfo? culture, T0 arg0, T1 arg1, T2 arg2, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
        => Translate(StartTranslation(language, culture, canUseIMGUI && target != null && target.Save.IMGUI, false, target, team, flags), arg0, arg1, arg2);
    public string Translate(LanguageInfo? language, T0 arg0, T1 arg1, T2 arg2, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
        => Translate(StartTranslation(language, target, team, canUseIMGUI, false, flags), arg0, arg1, arg2);
    public string Translate(LanguageInfo? language, T0 arg0, T1 arg1, T2 arg2, out Color color, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
    {
        TranslationHelper helper = StartTranslation(language, target, team, canUseIMGUI, true, flags);
        color = helper.ValueSet.Color;
        return Translate(helper, arg0, arg1, arg2);
    }
    public string Translate(UCPlayer? player, bool canUseIMGUI, T0 arg0, T1 arg1, T2 arg2)
        => Translate(StartTranslation(player, canUseIMGUI, false), arg0, arg1, arg2);
}
public sealed class Translation<T0, T1, T2, T3> : Translation
{
    private readonly string? _arg0Fmt;
    private readonly string? _arg1Fmt;
    private readonly string? _arg2Fmt;
    private readonly string? _arg3Fmt;
    private readonly short _arg0PluralExp = -1;
    private readonly short _arg1PluralExp = -1;
    private readonly short _arg2PluralExp = -1;
    private readonly short _arg3PluralExp = -1;
    public Translation(string @default) : base(@default) { }
    public Translation(string @default, TranslationFlags flags) : base(@default, flags) { }
    public Translation(string @default, string? arg0Fmt = null, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null) : this(@default, default, arg0Fmt, arg1Fmt, arg2Fmt, arg3Fmt) { }
    public Translation(string @default, TranslationFlags flags, string? arg0Fmt = null, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null) : base(@default, flags)
    {
        _arg0Fmt = arg0Fmt;
        _arg1Fmt = arg1Fmt;
        _arg2Fmt = arg2Fmt;
        _arg3Fmt = arg3Fmt;
        CheckPluralFormatting(ref _arg0PluralExp, ref _arg0Fmt);
        CheckPluralFormatting(ref _arg1PluralExp, ref _arg1Fmt);
        CheckPluralFormatting(ref _arg2PluralExp, ref _arg2Fmt);
        CheckPluralFormatting(ref _arg3PluralExp, ref _arg3Fmt);
    }
    private string Translate(in TranslationHelper data, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        string value = data.PreformattedValue;
        ArgumentSpan[] pluralizers = data.Pluralizers;
        for (int i = 0; i < pluralizers.Length; ++i)
        {
            ref ArgumentSpan span = ref pluralizers[i];
            int offset = 0;
            if (!(span.Argument switch
                {
                    0 => IsOne(arg0),
                    1 => IsOne(arg1),
                    2 => IsOne(arg2),
                    3 => IsOne(arg3),
                    _ => true
                }) ^ span.Inverted)
            {
                L.LogDebug($"Pluralizing from {span.Argument}: Offset: {value}");
                span.Pluralize(in data, ref value, ref offset);
            }
            else
            {
                L.LogDebug($"Not plural: {span.Argument}.");
            }
        }
        try
        {
            return string.Format(value,
                ToString(arg0, data.Language, _arg0Fmt, data.Player, _arg0PluralExp == -1 ? data.Flags : CheckPlurality(_arg0PluralExp, data.Flags, arg0, arg1, arg2, arg3)),
                ToString(arg1, data.Language, _arg1Fmt, data.Player, _arg1PluralExp == -1 ? data.Flags : CheckPlurality(_arg1PluralExp, data.Flags, arg0, arg1, arg2, arg3)),
                ToString(arg2, data.Language, _arg2Fmt, data.Player, _arg2PluralExp == -1 ? data.Flags : CheckPlurality(_arg2PluralExp, data.Flags, arg0, arg1, arg2, arg3)),
                ToString(arg3, data.Language, _arg3Fmt, data.Player, _arg3PluralExp == -1 ? data.Flags : CheckPlurality(_arg3PluralExp, data.Flags, arg0, arg1, arg2, arg3))
            );
        }
        catch (FormatException ex)
        {
            return PrintFormatException(ex, data.Flags);
        }
    }
    public string Translate(string value, LanguageInfo? language, T0 arg0, T1 arg1, T2 arg2, T3 arg3, UCPlayer? target, ulong targetTeam, TranslationFlags flags, bool useIMGUI = false)
    {
        return Translate(new TranslationHelper(null!, useIMGUI, false, value, language ?? Localization.GetDefaultLanguage(), target, targetTeam, Flags | flags | GetFlags(targetTeam, useIMGUI), target?.Locale.CultureInfo ?? Localization.GetCultureInfo(language)), arg0, arg1, arg2, arg3);
    }
    private static TranslationFlags CheckPlurality(short expectation, TranslationFlags flags, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3)
    {
        if (expectation == -1)
            return flags;
        flags |= TranslationFlags.Plural;
        return expectation switch
        {
            0 => IsOne(arg0) ? flags | TranslationFlags.NoPlural : flags,
            1 => IsOne(arg1) ? flags | TranslationFlags.NoPlural : flags,
            2 => IsOne(arg2) ? flags | TranslationFlags.NoPlural : flags,
            3 => IsOne(arg3) ? flags | TranslationFlags.NoPlural : flags,
            _ => flags
        };
    }
    public string Translate(LanguageInfo? language, CultureInfo? culture, T0 arg0, T1 arg1, T2 arg2, T3 arg3, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
        => Translate(StartTranslation(language, culture, canUseIMGUI && target != null && target.Save.IMGUI, false, target, team, flags), arg0, arg1, arg2, arg3);
    public string Translate(LanguageInfo? language, T0 arg0, T1 arg1, T2 arg2, T3 arg3, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
        => Translate(StartTranslation(language, target, team, canUseIMGUI, false, flags), arg0, arg1, arg2, arg3);
    public string Translate(LanguageInfo? language, T0 arg0, T1 arg1, T2 arg2, T3 arg3, out Color color, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
    {
        TranslationHelper helper = StartTranslation(language, target, team, canUseIMGUI, true, flags);
        color = helper.ValueSet.Color;
        return Translate(helper, arg0, arg1, arg2, arg3);
    }
    public string Translate(UCPlayer? player, bool canUseIMGUI, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        => Translate(StartTranslation(player, canUseIMGUI, false), arg0, arg1, arg2, arg3);
}
public sealed class Translation<T0, T1, T2, T3, T4> : Translation
{
    private readonly string? _arg0Fmt;
    private readonly string? _arg1Fmt;
    private readonly string? _arg2Fmt;
    private readonly string? _arg3Fmt;
    private readonly string? _arg4Fmt;
    private readonly short _arg0PluralExp = -1;
    private readonly short _arg1PluralExp = -1;
    private readonly short _arg2PluralExp = -1;
    private readonly short _arg3PluralExp = -1;
    private readonly short _arg4PluralExp = -1;
    public Translation(string @default) : base(@default) { }
    public Translation(string @default, TranslationFlags flags) : base(@default, flags) { }
    public Translation(string @default, string? arg0Fmt = null, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null, string? arg4Fmt = null) : this(@default, default, arg0Fmt, arg1Fmt, arg2Fmt, arg3Fmt, arg4Fmt) { }
    public Translation(string @default, TranslationFlags flags, string? arg0Fmt = null, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null, string? arg4Fmt = null) : base(@default, flags)
    {
        _arg0Fmt = arg0Fmt;
        _arg1Fmt = arg1Fmt;
        _arg2Fmt = arg2Fmt;
        _arg3Fmt = arg3Fmt;
        _arg4Fmt = arg4Fmt;
        CheckPluralFormatting(ref _arg0PluralExp, ref _arg0Fmt);
        CheckPluralFormatting(ref _arg1PluralExp, ref _arg1Fmt);
        CheckPluralFormatting(ref _arg2PluralExp, ref _arg2Fmt);
        CheckPluralFormatting(ref _arg3PluralExp, ref _arg3Fmt);
        CheckPluralFormatting(ref _arg4PluralExp, ref _arg4Fmt);
    }
    private string Translate(in TranslationHelper data, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        string value = data.PreformattedValue;
        ArgumentSpan[] pluralizers = data.Pluralizers;
        for (int i = 0; i < pluralizers.Length; ++i)
        {
            ref ArgumentSpan span = ref pluralizers[i];
            int offset = 0;
            if (!(span.Argument switch
                {
                    0 => IsOne(arg0),
                    1 => IsOne(arg1),
                    2 => IsOne(arg2),
                    3 => IsOne(arg3),
                    4 => IsOne(arg4),
                    _ => true
                }) ^ span.Inverted)
            {
                L.LogDebug($"Pluralizing from {span.Argument}: Offset: {value}");
                span.Pluralize(in data, ref value, ref offset);
            }
            else
            {
                L.LogDebug($"Not plural: {span.Argument}.");
            }
        }
        try
        {
            return string.Format(value,
                ToString(arg0, data.Language, _arg0Fmt, data.Player, _arg0PluralExp == -1 ? data.Flags : CheckPlurality(_arg0PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4)),
                ToString(arg1, data.Language, _arg1Fmt, data.Player, _arg1PluralExp == -1 ? data.Flags : CheckPlurality(_arg1PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4)),
                ToString(arg2, data.Language, _arg2Fmt, data.Player, _arg2PluralExp == -1 ? data.Flags : CheckPlurality(_arg2PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4)),
                ToString(arg3, data.Language, _arg3Fmt, data.Player, _arg3PluralExp == -1 ? data.Flags : CheckPlurality(_arg3PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4)),
                ToString(arg4, data.Language, _arg4Fmt, data.Player, _arg4PluralExp == -1 ? data.Flags : CheckPlurality(_arg4PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4))
            );
        }
        catch (FormatException ex)
        {
            return PrintFormatException(ex, data.Flags);
        }
    }
    public string Translate(string value, LanguageInfo? language, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, UCPlayer? target, ulong targetTeam, TranslationFlags flags, bool useIMGUI = false)
    {
        return Translate(new TranslationHelper(null!, useIMGUI, false, value, language ?? Localization.GetDefaultLanguage(), target, targetTeam, Flags | flags | GetFlags(targetTeam, useIMGUI), target?.Locale.CultureInfo ?? Localization.GetCultureInfo(language)), arg0, arg1, arg2, arg3, arg4);
    }
    private static TranslationFlags CheckPlurality(short expectation, TranslationFlags flags, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4)
    {
        if (expectation == -1)
            return flags;
        flags |= TranslationFlags.Plural;
        return expectation switch
        {
            0 => IsOne(arg0) ? flags | TranslationFlags.NoPlural : flags,
            1 => IsOne(arg1) ? flags | TranslationFlags.NoPlural : flags,
            2 => IsOne(arg2) ? flags | TranslationFlags.NoPlural : flags,
            3 => IsOne(arg3) ? flags | TranslationFlags.NoPlural : flags,
            4 => IsOne(arg4) ? flags | TranslationFlags.NoPlural : flags,
            _ => flags
        };
    }
    public string Translate(LanguageInfo? language, CultureInfo? culture, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
        => Translate(StartTranslation(language, culture, canUseIMGUI && target != null && target.Save.IMGUI, false, target, team, flags), arg0, arg1, arg2, arg3, arg4);
    public string Translate(LanguageInfo? language, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
        => Translate(StartTranslation(language, target, team, canUseIMGUI, false, flags), arg0, arg1, arg2, arg3, arg4);
    public string Translate(LanguageInfo? language, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, out Color color, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
    {
        TranslationHelper helper = StartTranslation(language, target, team, canUseIMGUI, true, flags);
        color = helper.ValueSet.Color;
        return Translate(helper, arg0, arg1, arg2, arg3, arg4);
    }
    public string Translate(UCPlayer? player, bool canUseIMGUI, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        => Translate(StartTranslation(player, canUseIMGUI, false), arg0, arg1, arg2, arg3, arg4);
}
public sealed class Translation<T0, T1, T2, T3, T4, T5> : Translation
{
    private readonly string? _arg0Fmt;
    private readonly string? _arg1Fmt;
    private readonly string? _arg2Fmt;
    private readonly string? _arg3Fmt;
    private readonly string? _arg4Fmt;
    private readonly string? _arg5Fmt;
    private readonly short _arg0PluralExp = -1;
    private readonly short _arg1PluralExp = -1;
    private readonly short _arg2PluralExp = -1;
    private readonly short _arg3PluralExp = -1;
    private readonly short _arg4PluralExp = -1;
    private readonly short _arg5PluralExp = -1;
    public Translation(string @default) : base(@default) { }
    public Translation(string @default, TranslationFlags flags) : base(@default, flags) { }
    public Translation(string @default, string? arg0Fmt = null, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null, string? arg4Fmt = null, string? arg5Fmt = null) : this(@default, default, arg0Fmt, arg1Fmt, arg2Fmt, arg3Fmt, arg4Fmt, arg5Fmt) { }
    public Translation(string @default, TranslationFlags flags, string? arg0Fmt = null, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null, string? arg4Fmt = null, string? arg5Fmt = null) : base(@default, flags)
    {
        _arg0Fmt = arg0Fmt;
        _arg1Fmt = arg1Fmt;
        _arg2Fmt = arg2Fmt;
        _arg3Fmt = arg3Fmt;
        _arg4Fmt = arg4Fmt;
        _arg5Fmt = arg5Fmt;
        CheckPluralFormatting(ref _arg0PluralExp, ref _arg0Fmt);
        CheckPluralFormatting(ref _arg1PluralExp, ref _arg1Fmt);
        CheckPluralFormatting(ref _arg2PluralExp, ref _arg2Fmt);
        CheckPluralFormatting(ref _arg3PluralExp, ref _arg3Fmt);
        CheckPluralFormatting(ref _arg4PluralExp, ref _arg4Fmt);
        CheckPluralFormatting(ref _arg5PluralExp, ref _arg5Fmt);
    }
    private string Translate(in TranslationHelper data, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        string value = data.PreformattedValue;
        ArgumentSpan[] pluralizers = data.Pluralizers;
        for (int i = 0; i < pluralizers.Length; ++i)
        {
            ref ArgumentSpan span = ref pluralizers[i];
            int offset = 0;
            if (!(span.Argument switch
            {
                0 => IsOne(arg0),
                1 => IsOne(arg1),
                2 => IsOne(arg2),
                3 => IsOne(arg3),
                4 => IsOne(arg4),
                5 => IsOne(arg5),
                _ => true
            }) ^ span.Inverted)
            {
                L.LogDebug($"Pluralizing from {span.Argument}: Offset: {value}");
                span.Pluralize(in data, ref value, ref offset);
            }
            else
            {
                L.LogDebug($"Not plural: {span.Argument}.");
            }
        }
        try
        {
            return string.Format(value,
                ToString(arg0, data.Language, _arg0Fmt, data.Player, _arg0PluralExp == -1 ? data.Flags : CheckPlurality(_arg0PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5)),
                ToString(arg1, data.Language, _arg1Fmt, data.Player, _arg1PluralExp == -1 ? data.Flags : CheckPlurality(_arg1PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5)),
                ToString(arg2, data.Language, _arg2Fmt, data.Player, _arg2PluralExp == -1 ? data.Flags : CheckPlurality(_arg2PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5)),
                ToString(arg3, data.Language, _arg3Fmt, data.Player, _arg3PluralExp == -1 ? data.Flags : CheckPlurality(_arg3PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5)),
                ToString(arg4, data.Language, _arg4Fmt, data.Player, _arg4PluralExp == -1 ? data.Flags : CheckPlurality(_arg4PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5)),
                ToString(arg5, data.Language, _arg5Fmt, data.Player, _arg5PluralExp == -1 ? data.Flags : CheckPlurality(_arg5PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5))
                );
        }
        catch (FormatException ex)
        {
            return PrintFormatException(ex, data.Flags);
        }
    }
    public string Translate(string value, LanguageInfo? language, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, UCPlayer? target, ulong targetTeam, TranslationFlags flags, bool useIMGUI = false)
    {
        return Translate(new TranslationHelper(null!, useIMGUI, false, value, language ?? Localization.GetDefaultLanguage(), target, targetTeam, Flags | flags | GetFlags(targetTeam, useIMGUI), target?.Locale.CultureInfo ?? Localization.GetCultureInfo(language)), arg0, arg1, arg2, arg3, arg4, arg5);
    }
    private static TranslationFlags CheckPlurality(short expectation, TranslationFlags flags, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5)
    {
        if (expectation == -1)
            return flags;
        flags |= TranslationFlags.Plural;
        return expectation switch
        {
            0 => IsOne(arg0) ? flags | TranslationFlags.NoPlural : flags,
            1 => IsOne(arg1) ? flags | TranslationFlags.NoPlural : flags,
            2 => IsOne(arg2) ? flags | TranslationFlags.NoPlural : flags,
            3 => IsOne(arg3) ? flags | TranslationFlags.NoPlural : flags,
            4 => IsOne(arg4) ? flags | TranslationFlags.NoPlural : flags,
            5 => IsOne(arg5) ? flags | TranslationFlags.NoPlural : flags,
            _ => flags
        };
    }
    public string Translate(LanguageInfo? language, CultureInfo? culture, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
        => Translate(StartTranslation(language, culture, canUseIMGUI && target != null && target.Save.IMGUI, false, target, team, flags), arg0, arg1, arg2, arg3, arg4, arg5);
    public string Translate(LanguageInfo? language, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
        => Translate(StartTranslation(language, target, team, canUseIMGUI, false, flags), arg0, arg1, arg2, arg3, arg4, arg5);
    public string Translate(LanguageInfo? language, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, out Color color, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
    {
        TranslationHelper helper = StartTranslation(language, target, team, canUseIMGUI, true, flags);
        color = helper.ValueSet.Color;
        return Translate(helper, arg0, arg1, arg2, arg3, arg4, arg5);
    }
    public string Translate(UCPlayer? player, bool canUseIMGUI, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        => Translate(StartTranslation(player, canUseIMGUI, false), arg0, arg1, arg2, arg3, arg4, arg5);
}
public sealed class Translation<T0, T1, T2, T3, T4, T5, T6> : Translation
{
    private readonly string? _arg0Fmt;
    private readonly string? _arg1Fmt;
    private readonly string? _arg2Fmt;
    private readonly string? _arg3Fmt;
    private readonly string? _arg4Fmt;
    private readonly string? _arg5Fmt;
    private readonly string? _arg6Fmt;
    private readonly short _arg0PluralExp = -1;
    private readonly short _arg1PluralExp = -1;
    private readonly short _arg2PluralExp = -1;
    private readonly short _arg3PluralExp = -1;
    private readonly short _arg4PluralExp = -1;
    private readonly short _arg5PluralExp = -1;
    private readonly short _arg6PluralExp = -1;
    public Translation(string @default) : base(@default) { }
    public Translation(string @default, TranslationFlags flags) : base(@default, flags) { }
    public Translation(string @default, string? arg0Fmt = null, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null, string? arg4Fmt = null, string? arg5Fmt = null, string? arg6Fmt = null) : this(@default, default, arg0Fmt, arg1Fmt, arg2Fmt, arg3Fmt, arg4Fmt, arg5Fmt, arg6Fmt) { }
    public Translation(string @default, TranslationFlags flags, string? arg0Fmt = null, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null, string? arg4Fmt = null, string? arg5Fmt = null, string? arg6Fmt = null) : base(@default, flags)
    {
        _arg0Fmt = arg0Fmt;
        _arg1Fmt = arg1Fmt;
        _arg2Fmt = arg2Fmt;
        _arg3Fmt = arg3Fmt;
        _arg4Fmt = arg4Fmt;
        _arg5Fmt = arg5Fmt;
        _arg6Fmt = arg6Fmt;
        CheckPluralFormatting(ref _arg0PluralExp, ref _arg0Fmt);
        CheckPluralFormatting(ref _arg1PluralExp, ref _arg1Fmt);
        CheckPluralFormatting(ref _arg2PluralExp, ref _arg2Fmt);
        CheckPluralFormatting(ref _arg3PluralExp, ref _arg3Fmt);
        CheckPluralFormatting(ref _arg4PluralExp, ref _arg4Fmt);
        CheckPluralFormatting(ref _arg5PluralExp, ref _arg5Fmt);
        CheckPluralFormatting(ref _arg6PluralExp, ref _arg6Fmt);
    }
    private string Translate(in TranslationHelper data, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        string value = data.PreformattedValue;
        ArgumentSpan[] pluralizers = data.Pluralizers;
        for (int i = 0; i < pluralizers.Length; ++i)
        {
            ref ArgumentSpan span = ref pluralizers[i];
            int offset = 0;
            if (!(span.Argument switch
            {
                0 => IsOne(arg0),
                1 => IsOne(arg1),
                2 => IsOne(arg2),
                3 => IsOne(arg3),
                4 => IsOne(arg4),
                5 => IsOne(arg5),
                6 => IsOne(arg6),
                _ => true
            }) ^ span.Inverted)
            {
                L.LogDebug($"Pluralizing from {span.Argument}: Offset: {value}");
                span.Pluralize(in data, ref value, ref offset);
            }
            else
            {
                L.LogDebug($"Not plural: {span.Argument}.");
            }
        }
        try
        {
            return string.Format(value,
                ToString(arg0, data.Language, _arg0Fmt, data.Player, _arg0PluralExp == -1 ? data.Flags : CheckPlurality(_arg0PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6)),
                ToString(arg1, data.Language, _arg1Fmt, data.Player, _arg1PluralExp == -1 ? data.Flags : CheckPlurality(_arg1PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6)),
                ToString(arg2, data.Language, _arg2Fmt, data.Player, _arg2PluralExp == -1 ? data.Flags : CheckPlurality(_arg2PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6)),
                ToString(arg3, data.Language, _arg3Fmt, data.Player, _arg3PluralExp == -1 ? data.Flags : CheckPlurality(_arg3PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6)),
                ToString(arg4, data.Language, _arg4Fmt, data.Player, _arg4PluralExp == -1 ? data.Flags : CheckPlurality(_arg4PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6)),
                ToString(arg5, data.Language, _arg5Fmt, data.Player, _arg5PluralExp == -1 ? data.Flags : CheckPlurality(_arg5PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6)),
                ToString(arg6, data.Language, _arg6Fmt, data.Player, _arg6PluralExp == -1 ? data.Flags : CheckPlurality(_arg6PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6))
                );
        }
        catch (FormatException ex)
        {
            return PrintFormatException(ex, data.Flags);
        }
    }
    public string Translate(string value, LanguageInfo? language, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, UCPlayer? target, ulong targetTeam, TranslationFlags flags, bool useIMGUI = false)
    {
        return Translate(new TranslationHelper(null!, useIMGUI, false, value, language ?? Localization.GetDefaultLanguage(), target, targetTeam, Flags | flags | GetFlags(targetTeam, useIMGUI), target?.Locale.CultureInfo ?? Localization.GetCultureInfo(language)), arg0, arg1, arg2, arg3, arg4, arg5, arg6);
    }
    private static TranslationFlags CheckPlurality(short expectation, TranslationFlags flags, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6)
    {
        if (expectation == -1)
            return flags;
        flags |= TranslationFlags.Plural;
        return expectation switch
        {
            0 => IsOne(arg0) ? flags | TranslationFlags.NoPlural : flags,
            1 => IsOne(arg1) ? flags | TranslationFlags.NoPlural : flags,
            2 => IsOne(arg2) ? flags | TranslationFlags.NoPlural : flags,
            3 => IsOne(arg3) ? flags | TranslationFlags.NoPlural : flags,
            4 => IsOne(arg4) ? flags | TranslationFlags.NoPlural : flags,
            5 => IsOne(arg5) ? flags | TranslationFlags.NoPlural : flags,
            6 => IsOne(arg6) ? flags | TranslationFlags.NoPlural : flags,
            _ => flags
        };
    }
    public string Translate(LanguageInfo? language, CultureInfo? culture, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
        => Translate(StartTranslation(language, culture, canUseIMGUI && target != null && target.Save.IMGUI, false, target, team, flags), arg0, arg1, arg2, arg3, arg4, arg5, arg6);
    public string Translate(LanguageInfo? language, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
        => Translate(StartTranslation(language, target, team, canUseIMGUI, false, flags), arg0, arg1, arg2, arg3, arg4, arg5, arg6);
    public string Translate(LanguageInfo? language, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, out Color color, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
    {
        TranslationHelper helper = StartTranslation(language, target, team, canUseIMGUI, true, flags);
        color = helper.ValueSet.Color;
        return Translate(helper, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
    }
    public string Translate(UCPlayer? player, bool canUseIMGUI, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        => Translate(StartTranslation(player, canUseIMGUI, false), arg0, arg1, arg2, arg3, arg4, arg5, arg6);
}
public sealed class Translation<T0, T1, T2, T3, T4, T5, T6, T7> : Translation
{
    private readonly string? _arg0Fmt;
    private readonly string? _arg1Fmt;
    private readonly string? _arg2Fmt;
    private readonly string? _arg3Fmt;
    private readonly string? _arg4Fmt;
    private readonly string? _arg5Fmt;
    private readonly string? _arg6Fmt;
    private readonly string? _arg7Fmt;
    private readonly short _arg0PluralExp = -1;
    private readonly short _arg1PluralExp = -1;
    private readonly short _arg2PluralExp = -1;
    private readonly short _arg3PluralExp = -1;
    private readonly short _arg4PluralExp = -1;
    private readonly short _arg5PluralExp = -1;
    private readonly short _arg6PluralExp = -1;
    private readonly short _arg7PluralExp = -1;
    public Translation(string @default) : base(@default) { }
    public Translation(string @default, TranslationFlags flags) : base(@default, flags) { }
    public Translation(string @default, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null, string? arg4Fmt = null, string? arg5Fmt = null, string? arg6Fmt = null, string? arg7Fmt = null, string? arg8Fmt = null) : this(@default, default, arg1Fmt, arg2Fmt, arg3Fmt, arg4Fmt, arg5Fmt, arg6Fmt, arg7Fmt, arg8Fmt) { }
    public Translation(string @default, TranslationFlags flags, string? arg0Fmt = null, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null, string? arg4Fmt = null, string? arg5Fmt = null, string? arg6Fmt = null, string? arg7Fmt = null) : base(@default, flags)
    {
        _arg0Fmt = arg0Fmt;
        _arg1Fmt = arg1Fmt;
        _arg2Fmt = arg2Fmt;
        _arg3Fmt = arg3Fmt;
        _arg4Fmt = arg4Fmt;
        _arg5Fmt = arg5Fmt;
        _arg6Fmt = arg6Fmt;
        _arg7Fmt = arg7Fmt;
        CheckPluralFormatting(ref _arg0PluralExp, ref _arg0Fmt);
        CheckPluralFormatting(ref _arg1PluralExp, ref _arg1Fmt);
        CheckPluralFormatting(ref _arg2PluralExp, ref _arg2Fmt);
        CheckPluralFormatting(ref _arg3PluralExp, ref _arg3Fmt);
        CheckPluralFormatting(ref _arg4PluralExp, ref _arg4Fmt);
        CheckPluralFormatting(ref _arg5PluralExp, ref _arg5Fmt);
        CheckPluralFormatting(ref _arg6PluralExp, ref _arg6Fmt);
        CheckPluralFormatting(ref _arg7PluralExp, ref _arg7Fmt);
    }
    private string Translate(in TranslationHelper data, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        string value = data.PreformattedValue;
        ArgumentSpan[] pluralizers = data.Pluralizers;
        for (int i = 0; i < pluralizers.Length; ++i)
        {
            ref ArgumentSpan span = ref pluralizers[i];
            int offset = 0;
            if (!(span.Argument switch
            {
                0 => IsOne(arg0),
                1 => IsOne(arg1),
                2 => IsOne(arg2),
                3 => IsOne(arg3),
                4 => IsOne(arg4),
                5 => IsOne(arg5),
                6 => IsOne(arg6),
                7 => IsOne(arg7),
                _ => true
            }) ^ span.Inverted)
            {
                L.LogDebug($"Pluralizing from {span.Argument}: Offset: {value}");
                span.Pluralize(in data, ref value, ref offset);
            }
            else
            {
                L.LogDebug($"Not plural: {span.Argument}.");
            }
        }
        try
        {
            return string.Format(value,
                ToString(arg0, data.Language, _arg0Fmt, data.Player, _arg0PluralExp == -1 ? data.Flags : CheckPlurality(_arg0PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7)),
                ToString(arg1, data.Language, _arg1Fmt, data.Player, _arg1PluralExp == -1 ? data.Flags : CheckPlurality(_arg1PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7)),
                ToString(arg2, data.Language, _arg2Fmt, data.Player, _arg2PluralExp == -1 ? data.Flags : CheckPlurality(_arg2PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7)),
                ToString(arg3, data.Language, _arg3Fmt, data.Player, _arg3PluralExp == -1 ? data.Flags : CheckPlurality(_arg3PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7)),
                ToString(arg4, data.Language, _arg4Fmt, data.Player, _arg4PluralExp == -1 ? data.Flags : CheckPlurality(_arg4PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7)),
                ToString(arg5, data.Language, _arg5Fmt, data.Player, _arg5PluralExp == -1 ? data.Flags : CheckPlurality(_arg5PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7)),
                ToString(arg6, data.Language, _arg6Fmt, data.Player, _arg6PluralExp == -1 ? data.Flags : CheckPlurality(_arg6PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7)),
                ToString(arg7, data.Language, _arg7Fmt, data.Player, _arg7PluralExp == -1 ? data.Flags : CheckPlurality(_arg7PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7))
                );
        }
        catch (FormatException ex)
        {
            return PrintFormatException(ex, data.Flags);
        }
    }
    public string Translate(string value, LanguageInfo? language, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, UCPlayer? target, ulong targetTeam, TranslationFlags flags, bool useIMGUI = false)
    {
        return Translate(new TranslationHelper(null!, useIMGUI, false, value, language ?? Localization.GetDefaultLanguage(), target, targetTeam, Flags | flags | GetFlags(targetTeam, useIMGUI), target?.Locale.CultureInfo ?? Localization.GetCultureInfo(language)), arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    }
    private static TranslationFlags CheckPlurality(short expectation, TranslationFlags flags, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7)
    {
        if (expectation == -1)
            return flags;
        flags |= TranslationFlags.Plural;
        return expectation switch
        {
            0 => IsOne(arg0) ? flags | TranslationFlags.NoPlural : flags,
            1 => IsOne(arg1) ? flags | TranslationFlags.NoPlural : flags,
            2 => IsOne(arg2) ? flags | TranslationFlags.NoPlural : flags,
            3 => IsOne(arg3) ? flags | TranslationFlags.NoPlural : flags,
            4 => IsOne(arg4) ? flags | TranslationFlags.NoPlural : flags,
            5 => IsOne(arg5) ? flags | TranslationFlags.NoPlural : flags,
            6 => IsOne(arg6) ? flags | TranslationFlags.NoPlural : flags,
            7 => IsOne(arg7) ? flags | TranslationFlags.NoPlural : flags,
            _ => flags
        };
    }
    public string Translate(LanguageInfo? language, CultureInfo? culture, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
        => Translate(StartTranslation(language, culture, canUseIMGUI && target != null && target.Save.IMGUI, false, target, team, flags), arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    public string Translate(LanguageInfo? language, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
        => Translate(StartTranslation(language, target, team, canUseIMGUI, false, flags), arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    public string Translate(LanguageInfo? language, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, out Color color, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
    {
        TranslationHelper helper = StartTranslation(language, target, team, canUseIMGUI, true, flags);
        color = helper.ValueSet.Color;
        return Translate(helper, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    }
    public string Translate(UCPlayer? player, bool canUseIMGUI, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        => Translate(StartTranslation(player, canUseIMGUI, false), arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
}
public sealed class Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> : Translation
{
    private readonly string? _arg0Fmt;
    private readonly string? _arg1Fmt;
    private readonly string? _arg2Fmt;
    private readonly string? _arg3Fmt;
    private readonly string? _arg4Fmt;
    private readonly string? _arg5Fmt;
    private readonly string? _arg6Fmt;
    private readonly string? _arg7Fmt;
    private readonly string? _arg8Fmt;
    private readonly short _arg0PluralExp = -1;
    private readonly short _arg1PluralExp = -1;
    private readonly short _arg2PluralExp = -1;
    private readonly short _arg3PluralExp = -1;
    private readonly short _arg4PluralExp = -1;
    private readonly short _arg5PluralExp = -1;
    private readonly short _arg6PluralExp = -1;
    private readonly short _arg7PluralExp = -1;
    private readonly short _arg8PluralExp = -1;
    public Translation(string @default) : base(@default) { }
    public Translation(string @default, TranslationFlags flags) : base(@default, flags) { }
    public Translation(string @default, string? arg0Fmt = null, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null, string? arg4Fmt = null, string? arg5Fmt = null, string? arg6Fmt = null, string? arg7Fmt = null, string? arg8Fmt = null) : this(@default, default, arg0Fmt, arg1Fmt, arg2Fmt, arg3Fmt, arg4Fmt, arg5Fmt, arg6Fmt, arg7Fmt, arg8Fmt) { }
    public Translation(string @default, TranslationFlags flags, string? arg0Fmt = null, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null, string? arg4Fmt = null, string? arg5Fmt = null, string? arg6Fmt = null, string? arg7Fmt = null, string? arg8Fmt = null) : base(@default, flags)
    {
        _arg0Fmt = arg0Fmt;
        _arg1Fmt = arg1Fmt;
        _arg2Fmt = arg2Fmt;
        _arg3Fmt = arg3Fmt;
        _arg4Fmt = arg4Fmt;
        _arg5Fmt = arg5Fmt;
        _arg6Fmt = arg6Fmt;
        _arg7Fmt = arg7Fmt;
        _arg8Fmt = arg8Fmt;
        CheckPluralFormatting(ref _arg0PluralExp, ref _arg0Fmt);
        CheckPluralFormatting(ref _arg1PluralExp, ref _arg1Fmt);
        CheckPluralFormatting(ref _arg2PluralExp, ref _arg2Fmt);
        CheckPluralFormatting(ref _arg3PluralExp, ref _arg3Fmt);
        CheckPluralFormatting(ref _arg4PluralExp, ref _arg4Fmt);
        CheckPluralFormatting(ref _arg5PluralExp, ref _arg5Fmt);
        CheckPluralFormatting(ref _arg6PluralExp, ref _arg6Fmt);
        CheckPluralFormatting(ref _arg7PluralExp, ref _arg7Fmt);
        CheckPluralFormatting(ref _arg8PluralExp, ref _arg8Fmt);
    }
    private string Translate(in TranslationHelper data, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        string value = data.PreformattedValue;
        ArgumentSpan[] pluralizers = data.Pluralizers;
        for (int i = 0; i < pluralizers.Length; ++i)
        {
            ref ArgumentSpan span = ref pluralizers[i];
            int offset = 0;
            if (!(span.Argument switch
            {
                0 => IsOne(arg0),
                1 => IsOne(arg1),
                2 => IsOne(arg2),
                3 => IsOne(arg3),
                4 => IsOne(arg4),
                5 => IsOne(arg5),
                6 => IsOne(arg6),
                7 => IsOne(arg7),
                8 => IsOne(arg8),
                _ => true
            }) ^ span.Inverted)
            {
                L.LogDebug($"Pluralizing from {span.Argument}: Offset: {value}");
                span.Pluralize(in data, ref value, ref offset);
            }
            else
            {
                L.LogDebug($"Not plural: {span.Argument}.");
            }
        }
        try
        {
            return string.Format(value,
                ToString(arg0, data.Language, _arg0Fmt, data.Player, _arg0PluralExp == -1 ? data.Flags : CheckPlurality(_arg0PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8)),
                ToString(arg1, data.Language, _arg1Fmt, data.Player, _arg1PluralExp == -1 ? data.Flags : CheckPlurality(_arg1PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8)),
                ToString(arg2, data.Language, _arg2Fmt, data.Player, _arg2PluralExp == -1 ? data.Flags : CheckPlurality(_arg2PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8)),
                ToString(arg3, data.Language, _arg3Fmt, data.Player, _arg3PluralExp == -1 ? data.Flags : CheckPlurality(_arg3PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8)),
                ToString(arg4, data.Language, _arg4Fmt, data.Player, _arg4PluralExp == -1 ? data.Flags : CheckPlurality(_arg4PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8)),
                ToString(arg5, data.Language, _arg5Fmt, data.Player, _arg5PluralExp == -1 ? data.Flags : CheckPlurality(_arg5PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8)),
                ToString(arg6, data.Language, _arg6Fmt, data.Player, _arg6PluralExp == -1 ? data.Flags : CheckPlurality(_arg6PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8)),
                ToString(arg7, data.Language, _arg7Fmt, data.Player, _arg7PluralExp == -1 ? data.Flags : CheckPlurality(_arg7PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8)),
                ToString(arg8, data.Language, _arg8Fmt, data.Player, _arg8PluralExp == -1 ? data.Flags : CheckPlurality(_arg8PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8))
                );
        }
        catch (FormatException ex)
        {
            return PrintFormatException(ex, data.Flags);
        }
    }
    public string Translate(string value, LanguageInfo? language, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, UCPlayer? target, ulong targetTeam, TranslationFlags flags, bool useIMGUI = false)
    {
        return Translate(new TranslationHelper(null!, useIMGUI, false, value, language ?? Localization.GetDefaultLanguage(), target, targetTeam, Flags | flags | GetFlags(targetTeam, useIMGUI), target?.Locale.CultureInfo ?? Localization.GetCultureInfo(language)), arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    }
    private static TranslationFlags CheckPlurality(short expectation, TranslationFlags flags, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7, in T8 arg8)
    {
        if (expectation == -1)
            return flags;
        flags |= TranslationFlags.Plural;
        return expectation switch
        {
            0 => IsOne(arg0) ? flags | TranslationFlags.NoPlural : flags,
            1 => IsOne(arg1) ? flags | TranslationFlags.NoPlural : flags,
            2 => IsOne(arg2) ? flags | TranslationFlags.NoPlural : flags,
            3 => IsOne(arg3) ? flags | TranslationFlags.NoPlural : flags,
            4 => IsOne(arg4) ? flags | TranslationFlags.NoPlural : flags,
            5 => IsOne(arg5) ? flags | TranslationFlags.NoPlural : flags,
            6 => IsOne(arg6) ? flags | TranslationFlags.NoPlural : flags,
            7 => IsOne(arg7) ? flags | TranslationFlags.NoPlural : flags,
            8 => IsOne(arg8) ? flags | TranslationFlags.NoPlural : flags,
            _ => flags
        };
    }
    public string Translate(LanguageInfo? language, CultureInfo? culture, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
        => Translate(StartTranslation(language, culture, canUseIMGUI && target != null && target.Save.IMGUI, false, target, team, flags), arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    public string Translate(LanguageInfo? language, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
        => Translate(StartTranslation(language, target, team, canUseIMGUI, false, flags), arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    public string Translate(LanguageInfo? language, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, out Color color, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
    {
        TranslationHelper helper = StartTranslation(language, target, team, canUseIMGUI, true, flags);
        color = helper.ValueSet.Color;
        return Translate(helper, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    }
    public string Translate(UCPlayer? player, bool canUseIMGUI, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        => Translate(StartTranslation(player, canUseIMGUI, false), arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
}
public sealed class Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> : Translation
{
    private readonly string? _arg0Fmt;
    private readonly string? _arg1Fmt;
    private readonly string? _arg2Fmt;
    private readonly string? _arg3Fmt;
    private readonly string? _arg4Fmt;
    private readonly string? _arg5Fmt;
    private readonly string? _arg6Fmt;
    private readonly string? _arg7Fmt;
    private readonly string? _arg8Fmt;
    private readonly string? _arg9Fmt;
    private readonly short _arg0PluralExp = -1;
    private readonly short _arg1PluralExp = -1;
    private readonly short _arg2PluralExp = -1;
    private readonly short _arg3PluralExp = -1;
    private readonly short _arg4PluralExp = -1;
    private readonly short _arg5PluralExp = -1;
    private readonly short _arg6PluralExp = -1;
    private readonly short _arg7PluralExp = -1;
    private readonly short _arg8PluralExp = -1;
    private readonly short _arg9PluralExp = -1;
    public Translation(string @default) : base(@default) { }
    public Translation(string @default, TranslationFlags flags) : base(@default, flags) { }
    public Translation(string @default, string? arg0Fmt = null, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null, string? arg4Fmt = null, string? arg5Fmt = null, string? arg6Fmt = null, string? arg7Fmt = null, string? arg8Fmt = null, string? arg9Fmt = null) : this(@default, default, arg0Fmt, arg1Fmt, arg2Fmt, arg3Fmt, arg4Fmt, arg5Fmt, arg6Fmt, arg7Fmt, arg8Fmt, arg9Fmt) { }
    public Translation(string @default, TranslationFlags flags, string? arg0Fmt = null, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null, string? arg4Fmt = null, string? arg5Fmt = null, string? arg6Fmt = null, string? arg7Fmt = null, string? arg8Fmt = null, string? arg9Fmt = null) : base(@default, flags)
    {
        _arg0Fmt = arg0Fmt;
        _arg1Fmt = arg1Fmt;
        _arg2Fmt = arg2Fmt;
        _arg3Fmt = arg3Fmt;
        _arg4Fmt = arg4Fmt;
        _arg5Fmt = arg5Fmt;
        _arg6Fmt = arg6Fmt;
        _arg7Fmt = arg7Fmt;
        _arg8Fmt = arg8Fmt;
        _arg9Fmt = arg9Fmt;
        CheckPluralFormatting(ref _arg0PluralExp, ref _arg0Fmt);
        CheckPluralFormatting(ref _arg1PluralExp, ref _arg1Fmt);
        CheckPluralFormatting(ref _arg2PluralExp, ref _arg2Fmt);
        CheckPluralFormatting(ref _arg3PluralExp, ref _arg3Fmt);
        CheckPluralFormatting(ref _arg4PluralExp, ref _arg4Fmt);
        CheckPluralFormatting(ref _arg5PluralExp, ref _arg5Fmt);
        CheckPluralFormatting(ref _arg6PluralExp, ref _arg6Fmt);
        CheckPluralFormatting(ref _arg7PluralExp, ref _arg7Fmt);
        CheckPluralFormatting(ref _arg8PluralExp, ref _arg8Fmt);
        CheckPluralFormatting(ref _arg9PluralExp, ref _arg9Fmt);
    }

    private string Translate(in TranslationHelper data, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        string value = data.PreformattedValue;
        ArgumentSpan[] pluralizers = data.Pluralizers;
        for (int i = 0; i < pluralizers.Length; ++i)
        {
            ref ArgumentSpan span = ref pluralizers[i];
            int offset = 0;
            if (!(span.Argument switch
                {
                    0 => IsOne(arg0),
                    1 => IsOne(arg1),
                    2 => IsOne(arg2),
                    3 => IsOne(arg3),
                    4 => IsOne(arg4),
                    5 => IsOne(arg5),
                    6 => IsOne(arg6),
                    7 => IsOne(arg7),
                    8 => IsOne(arg8),
                    9 => IsOne(arg9),
                    _ => true
                }) ^ span.Inverted)
            {
                L.LogDebug($"Pluralizing from {span.Argument}: Offset: {value}");
                span.Pluralize(in data, ref value, ref offset);
            }
            else
            {
                L.LogDebug($"Not plural: {span.Argument}.");
            }
        }
        try
        {
            return string.Format(value,
                ToString(arg0, data.Language, _arg0Fmt, data.Player, _arg0PluralExp == -1 ? data.Flags : CheckPlurality(_arg0PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9)),
                ToString(arg1, data.Language, _arg1Fmt, data.Player, _arg1PluralExp == -1 ? data.Flags : CheckPlurality(_arg1PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9)),
                ToString(arg2, data.Language, _arg2Fmt, data.Player, _arg2PluralExp == -1 ? data.Flags : CheckPlurality(_arg2PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9)),
                ToString(arg3, data.Language, _arg3Fmt, data.Player, _arg3PluralExp == -1 ? data.Flags : CheckPlurality(_arg3PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9)),
                ToString(arg4, data.Language, _arg4Fmt, data.Player, _arg4PluralExp == -1 ? data.Flags : CheckPlurality(_arg4PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9)),
                ToString(arg5, data.Language, _arg5Fmt, data.Player, _arg5PluralExp == -1 ? data.Flags : CheckPlurality(_arg5PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9)),
                ToString(arg6, data.Language, _arg6Fmt, data.Player, _arg6PluralExp == -1 ? data.Flags : CheckPlurality(_arg6PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9)),
                ToString(arg7, data.Language, _arg7Fmt, data.Player, _arg7PluralExp == -1 ? data.Flags : CheckPlurality(_arg7PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9)),
                ToString(arg8, data.Language, _arg8Fmt, data.Player, _arg8PluralExp == -1 ? data.Flags : CheckPlurality(_arg8PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9)),
                ToString(arg9, data.Language, _arg9Fmt, data.Player, _arg9PluralExp == -1 ? data.Flags : CheckPlurality(_arg9PluralExp, data.Flags, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9))
                );
        }
        catch (FormatException ex)
        {
            return PrintFormatException(ex, data.Flags);
        }
    }
    public string Translate(string value, LanguageInfo? language, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, UCPlayer? target, ulong targetTeam, TranslationFlags flags, bool useIMGUI = false)
    {
        return Translate(new TranslationHelper(null!, useIMGUI, false, value, language ?? Localization.GetDefaultLanguage(), target, targetTeam, Flags | flags | GetFlags(targetTeam, useIMGUI), target?.Locale.CultureInfo ?? Localization.GetCultureInfo(language)), arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
    }
    private static TranslationFlags CheckPlurality(short expectation, TranslationFlags flags, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7, in T8 arg8, in T9 arg9)
    {
        if (expectation == -1)
            return flags;
        flags |= TranslationFlags.Plural;
        return expectation switch
        {
            0 => IsOne(arg0) ? flags | TranslationFlags.NoPlural : flags,
            1 => IsOne(arg1) ? flags | TranslationFlags.NoPlural : flags,
            2 => IsOne(arg2) ? flags | TranslationFlags.NoPlural : flags,
            3 => IsOne(arg3) ? flags | TranslationFlags.NoPlural : flags,
            4 => IsOne(arg4) ? flags | TranslationFlags.NoPlural : flags,
            5 => IsOne(arg5) ? flags | TranslationFlags.NoPlural : flags,
            6 => IsOne(arg6) ? flags | TranslationFlags.NoPlural : flags,
            7 => IsOne(arg7) ? flags | TranslationFlags.NoPlural : flags,
            8 => IsOne(arg8) ? flags | TranslationFlags.NoPlural : flags,
            9 => IsOne(arg9) ? flags | TranslationFlags.NoPlural : flags,
            _ => flags
        };
    }
    public string Translate(LanguageInfo? language, CultureInfo? culture, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
        => Translate(StartTranslation(language, culture, canUseIMGUI && target != null && target.Save.IMGUI, false, target, team, flags), arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
    public string Translate(LanguageInfo? language, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
        => Translate(StartTranslation(language, target, team, canUseIMGUI, false, flags), arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
    public string Translate(LanguageInfo? language, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, out Color color, UCPlayer? target = null, ulong team = 0, bool canUseIMGUI = false, TranslationFlags flags = TranslationFlags.None)
    {
        TranslationHelper helper = StartTranslation(language, target, team, canUseIMGUI, true, flags);
        color = helper.ValueSet.Color;
        return Translate(helper, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
    }
    public string Translate(UCPlayer? player, bool canUseIMGUI, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
        => Translate(StartTranslation(player, canUseIMGUI, false), arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
}
[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class TranslationDataAttribute : Attribute
{
    public TranslationDataAttribute() { }
    public TranslationDataAttribute(string section)
    {
        Section = section;
    }
    public TranslationDataAttribute(string section, string description, params string[] parameters)
    {
        Section = section;
        Description = description;
        for (int i = 0; i < parameters.Length; ++i)
        {
            parameters[i] ??= string.Empty;
        }

        FormattingDescriptions = parameters;
    }
    public string? SignId { get; set; }
    public string? Description { get; set; }
    public string? Section { get; set; }
    public string[]? FormattingDescriptions { get; set; }
    public bool IsAnnounced { get; set; }
    public bool IsPrioritizedTranslation { get; set; } = true;
}

[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
public sealed class FormatDisplayAttribute : Attribute
{
    public FormatDisplayAttribute(string displayName)
    {
        DisplayName = displayName;
    }
    public FormatDisplayAttribute(Type? forType, string displayName)
    {
        DisplayName = displayName;
        TypeSupplied = true;
        TargetType = forType;
    }
    public string DisplayName { get; }
    public bool TypeSupplied { get; }
    public Type? TargetType { get; }
}
