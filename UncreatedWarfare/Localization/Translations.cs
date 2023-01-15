using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Uncreated.Framework;
using Uncreated.Players;
using Uncreated.Warfare.Quests;
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
    private TranslationValue _defaultData;
    private TranslationValue[]? _data;
    private TranslationDataAttribute? _attr;
    private bool _init;
    public string Key;
    public int Id;
    public static event Action? OnReload;
    public TranslationFlags Flags => _flags;
    protected string InvalidValue => "Translation Error - " + Key;
    internal TranslationDataAttribute? AttributeData { get => _attr; set => _attr = value; }
    public Translation(string @default, TranslationFlags flags) : this(@default)
    {
        _flags = flags;
    }
    public Translation(string @default)
    {
        string def2 = @default;
        Color clr = ProcessValue(ref def2, out string inner, Flags);
        _defaultData = new TranslationValue(L.Default, @default, inner, def2, clr);
    }
    public void RefreshColors()
    {
        _defaultData = new TranslationValue(in _defaultData, Flags);
        if (_data is not null)
        {
            for (int i = 0; i < _data.Length; ++i)
                _data[i] = new TranslationValue(in _data[i], Flags);
        }
    }
    internal void Init()
    {
        if (_init) return;
        _init = true;
        VerifyOriginal(null, _defaultData.Original);
    }
    private void VerifyOriginal(string? lang, string def)
    {
        if ((_flags & TranslationFlags.SuppressWarnings) == TranslationFlags.SuppressWarnings) return;
        if ((_flags & TranslationFlags.TMProSign) == TranslationFlags.TMProSign && def.IndexOf("<size", StringComparison.OrdinalIgnoreCase) != -1)
            L.LogWarning("[" + (lang == null ? "DEFAULT" : lang.ToUpper()) + "] " + Key + " has a size tag, which shouldn't be on signs.", method: "TRANSLATIONS");
        int ct = this.GetType().GenericTypeArguments.Length;
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
            L.LogError("[" + (lang == null ? "DEFAULT" : lang.ToUpper()) + "] " + Key + " has " + (max - ct == 1 ? ("an extra paremeter: " + max) : $"{max - ct} extra parameters: Should have: {ct + 1}, has: {max + 1}"), method: "TRANSLATIONS");
    }
    public void AddTranslation(string language, string value)
    {
        VerifyOriginal(language, value);
        if (_data is null || _data.Length == 0)
            _data = new TranslationValue[] { new TranslationValue(language, value, Flags) };
        else
        {
            for (int i = 0; i < _data.Length; ++i)
            {
                ref TranslationValue v = ref _data[i];
                if (v.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
                {
                    v = new TranslationValue(language, value, Flags);
                    return;
                }
            }

            TranslationValue[] old = _data;
            _data = new TranslationValue[old.Length + 1];
            if (language.Equals(L.Default, StringComparison.OrdinalIgnoreCase))
            {
                Array.Copy(old, 0, _data, 1, old.Length);
                _data[0] = new TranslationValue(L.Default, value, Flags);
            }
            else
            {
                Array.Copy(old, _data, old.Length);
                _data[_data.Length - 1] = new TranslationValue(language, value, Flags);
            }
        }
    }
    public void RemoveTranslation(string language)
    {
        if (_data is null || _data.Length == 0) return;
        int index = -1;
        for (int i = 0; i < _data.Length; ++i)
        {
            ref TranslationValue v = ref _data[i];
            if (v.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
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
    protected ref TranslationValue this[string language]
    {
        get
        {
            if (_data is not null)
            {
                for (int i = 0; i < _data.Length; ++i)
                {
                    ref TranslationValue v = ref _data[i];
                    if (v.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
                    {
                        return ref v;
                    }
                }
            }
            return ref _defaultData;
        }
    }
    public static string ToString(object value, string language, string? format, UCPlayer? target, TranslationFlags flags)
    {
        if (value is null)
            return ToString<object>(value!, language, format, target, flags);
        return (string)typeof(ToStringHelperClass<>).MakeGenericType(value.GetType())
            .GetMethod("ToString", BindingFlags.Static | BindingFlags.Public)!.Invoke(null, new object?[] { value, language, format,
                target, LanguageAliasSet.GetCultureInfo(language), flags });
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToString<T>(T value, string language, string? format, UCPlayer? target, TranslationFlags flags)
        => ToStringHelperClass<T>.ToString(value, language, format, target, LanguageAliasSet.GetCultureInfo(language), flags);

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
        public static string ToString(T value, string language, string? format, UCPlayer? target, IFormatProvider locale, TranslationFlags flags)
        {
            if (value is null)
                return Null(flags);

            if (value is string str)
                return CheckCase(Pluralize(str, flags), format);

            str = Type switch
            {
                1  => ToStringFunc1!(value, format!, locale),
                2  => ToStringFunc2!(value, format!),
                3  => ToStringFunc3!(value, locale),
                4  => Pluralize((value as ITranslationArgument)!.Translate(language, format, target, ref flags), flags),
                5  => CheckCase(Pluralize((value as UnityEngine.Object)!.name, flags), format),
                6  => value is Color clr ? clr.Hex() : value.ToString(),
                7  => value is CSteamID id ? id.m_SteamID.ToString(format, locale) : value.ToString(),
                8  => PlayerToString((value as PlayerCaller)!.player, flags, format),
                9  => PlayerToString((value as Player)!, flags, format),
                10 => PlayerToString((value as SteamPlayer)!.player, flags, format),
                11 => PlayerToString((value as SteamPlayerID)!, flags, format),
                12 => CheckCase(Pluralize(value is Type t ? (t.IsEnum ? Localization.TranslateEnumName(t, language) : (t.IsArray ? (t.GetElementType()!.Name + " Array") : TypeToString(t))) : value.ToString(), flags), format),
                13 => CheckCase(Pluralize(Localization.TranslateEnum(value, language), flags), format),
                14 => CheckCase(AssetToString((value as Asset)!, format, flags), format),
                15 => ToStringFunc4!(value),
                16 => CheckCase((value as BarricadeData)?.barricade.asset.itemName ?? value.ToString(), format),
                17 => CheckCase((value as StructureData)?.structure.asset.itemName ?? value.ToString(), format),
                18 => value is Guid guid ? guid.ToString(format ?? "N", locale) : value.ToString(),
                19 => value is char chr ? CheckCase(new string(chr, 1), format) : value.ToString(),
                50 => CheckTime(value, format, out string? val, language, locale) ? val! : value.ToString(),
                51 => CheckTime(value, format, out string? val, language, locale) ? val! : ToStringFunc1!(value, format!, locale),
                52 => CheckTime(value, format, out string? val, language, locale) ? val! : ToStringFunc2!(value, format!),
                53 => CheckTime(value, format, out string? val, language, locale) ? val! : ToStringFunc3!(value, locale),
                _  => Default(value, format, language, locale, target, flags),
            };

            return str;
        }

        private static string Default(T value, string? format, string lang, IFormatProvider locale, UCPlayer? target, TranslationFlags flags)
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
        private static bool CheckTime(T value, string? format, out string? val, string lang, IFormatProvider locale)
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
                                val = sec.GetTimeFromSeconds(lang);
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
                        string sep = locale is CultureInfo info ? info.DateTimeFormat.TimeSeparator : ":";
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
                                        val = (sec / 60).ToString("00", locale) + sep + (sec % 60).ToString("00", locale);
                                    else
                                    {
                                        int hrs = sec / 3600;
                                        int mins = sec - hrs * 3600;
                                        val = hrs.ToString("00", locale) + sep + (mins / 60).ToString("00", locale) + sep + (mins % 60).ToString("00", locale);
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
        private static string AssetToString(Asset asset, string? format, TranslationFlags flags)
        {
            if (asset is ItemAsset a)
                return ItemAssetToString(a, format, flags);
            else if (asset is VehicleAsset b)
                return VehicleAssetToString(b, format, flags);
            else if (asset is QuestAsset c)
                return QuestAssetToString(c, format, flags);

            return asset.FriendlyName;
        }
        private static string QuestAssetToString(QuestAsset asset, string? format, TranslationFlags flags)
        {
            if (format is not null)
            {
                if (format.Equals(BaseQuestData.COLOR_QUEST_ASSET_FORMAT, StringComparison.Ordinal))
                    return asset.questName;
            }
            return (flags & TranslationFlags.NoRichText) == TranslationFlags.NoRichText ? F.RemoveColorTag(asset.questName) : asset.questName;
        }
        private static string VehicleAssetToString(VehicleAsset asset, string? format, TranslationFlags flags)
        {
            if (format is not null)
            {
                if (format.Equals(Warfare.T.FormatRarityColor, StringComparison.Ordinal))
                    return Localization.Colorize(ItemTool.getRarityColorUI(asset.rarity).Hex(), Pluralize(asset.vehicleName, flags), flags);
            }
            return asset.vehicleName;
        }
        private static string ItemAssetToString(ItemAsset asset, string? format, TranslationFlags flags)
        {
            if (format is not null)
            {
                if (format.Equals(Warfare.T.FormatRarityColor, StringComparison.Ordinal))
                    return Localization.Colorize(ItemTool.getRarityColorUI(asset.rarity).Hex(), Pluralize(asset.itemName, flags), flags);
            }
            return asset.itemName;
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
                    return Localization.Colorize(Teams.TeamManager.GetTeamHexColor(player.GetTeam()), names.CharacterName, flags);
                if (format.Equals(UCPlayer.NICK_NAME_FORMAT, StringComparison.Ordinal))
                    return names.NickName;
                if (format.Equals(UCPlayer.COLOR_NICK_NAME_FORMAT, StringComparison.Ordinal))
                    return Localization.Colorize(Teams.TeamManager.GetTeamHexColor(player.GetTeam()), names.NickName, flags);
                if (format.Equals(UCPlayer.PLAYER_NAME_FORMAT, StringComparison.Ordinal))
                    return names.PlayerName;
                if (format.Equals(UCPlayer.COLOR_PLAYER_NAME_FORMAT, StringComparison.Ordinal))
                    return Localization.Colorize(Teams.TeamManager.GetTeamHexColor(player.GetTeam()), names.PlayerName, flags);
                if (format.Equals(UCPlayer.STEAM_64_FORMAT, StringComparison.Ordinal))
                    return player.channel.owner.playerID.steamID.m_SteamID.ToString(Data.AdminLocale);
                if (format.Equals(UCPlayer.COLOR_STEAM_64_FORMAT, StringComparison.Ordinal))
                    return Localization.Colorize(Teams.TeamManager.GetTeamHexColor(player.GetTeam()), player.channel.owner.playerID.steamID.m_SteamID.ToString(Data.AdminLocale), flags);
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
    protected struct TranslationValue
    {
        public static TranslationValue Nil = new TranslationValue();
        public readonly string Language;
        public readonly string Original;
        public readonly string ProcessedInner;
        public readonly string Processed;
        public readonly Color Color;
        public readonly bool RichText;
        private string? _console;
        public string Console => RichText ? (_console ??= Util.RemoveRichText(ProcessedInner)) : Original;
        public ConsoleColor ConsoleColor => Util.GetClosestConsoleColor(Color);
        public bool IsNil => Original is null;
        public TranslationValue(string language, string original, TranslationFlags flags)
        {
            RichText = (flags & TranslationFlags.NoRichText) == 0;
            Original = original;
            Processed = original;
            Color = ProcessValue(ref Processed, out ProcessedInner, flags);
            Language = language;
            _console = null;
        }
        public TranslationValue(string language, string original, string processedInner, string processed, Color color)
        {
            RichText = original.Contains(">");
            Language = language;
            Original = original;
            ProcessedInner = processedInner;
            Processed = processed;
            Color = color;
            _console = null;
        }
        public TranslationValue(in TranslationValue value, TranslationFlags flags) : this(value.Language, value.Original, flags) { }
    }
    public static string Pluralize(string word, TranslationFlags flags)
    {
        if ((flags & TranslationFlags.NoPlural) == TranslationFlags.NoPlural || word.Length < 3 || (flags & TranslationFlags.Plural) == 0)
            return word;
        string[] words = word.Split(' ');
        bool hOthWrds = words.Length > 1;
        string otherWords = string.Empty;
        string str = (hOthWrds ? words[words.Length - 1] : word).ToLowerInvariant();
        if (str.Length < 2)
            return word;
        if (hOthWrds)
            otherWords = string.Join(" ", words, 0, words.Length - 1);
        bool isPCaps = char.IsUpper(word[0]);

        if (str.Equals("child", StringComparison.OrdinalIgnoreCase))
            return word + "ren";
        if (str.Equals("goose", StringComparison.OrdinalIgnoreCase))
            return otherWords + (isPCaps ? "Geese" : "geese");
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

        if (str.EndsWith("man", StringComparison.OrdinalIgnoreCase))
            return str.Substring(0, str.Length - 2) + (char.IsUpper(str[str.Length - 2]) ? "E" : "e") + str[str.Length - 1];

        char last = str[str.Length - 1];
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
    public static unsafe void ReplaceColors(ref string message)
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
    public static Color ProcessValue(ref string message, out string innerText, TranslationFlags flags)
    {
        ReplaceColors(ref message);
        Color color;

        if ((flags & TranslationFlags.NoRichText) == 0 && (flags & TranslationFlags.ReplaceTMProRichText) == TranslationFlags.ReplaceTMProRichText)
            ReplaceTMProRichText(ref message, flags);

        if ((flags & TranslationFlags.NoColorOptimization) == TranslationFlags.NoColorOptimization)
            goto noColor;

        // <#ffffff>
        if (message.Length > 2 && message.StartsWith("<#", StringComparison.OrdinalIgnoreCase) && message[2] != '{')
        {
            int endtag = message.IndexOf('>', 2);
            if (endtag == -1 || endtag is not 5 and not 6 and not 8 and not 10)
                goto noColor;
            string clr = message.Substring(2, endtag - 2);
            if (endtag == message.Length - 1)
                innerText = string.Empty;
            else if (message.EndsWith("</color>", StringComparison.OrdinalIgnoreCase))
                innerText = message.Substring(endtag + 1, message.Length - endtag - 1 - 8);
            else
                innerText = message.Substring(endtag + 1, message.Length - endtag - 1);
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
                innerText = string.Empty;
            else if (message.EndsWith("</color>", StringComparison.OrdinalIgnoreCase))
                innerText = message.Substring(endtag + 1, message.Length - endtag - 1 - 8);
            else
                innerText = message.Substring(endtag + 1, message.Length - endtag - 1);
            color = clr.Hex();
            goto next;
        }

        noColor:
        color = UCWarfare.GetColor("default");
        innerText = message;
        next:
        return color;
    }
    protected TranslationFlags GetFlags(ulong targetTeam) => targetTeam switch
    {
        3 => Flags | TranslationFlags.Team3,
        2 => Flags | TranslationFlags.Team2,
        1 => Flags | TranslationFlags.Team1,
        _ => Flags
    };
    internal static string Null(TranslationFlags flags) =>
        ((flags & TranslationFlags.NoRichText) == TranslationFlags.NoRichText)
            ? NullNoColor
            : (((flags & TranslationFlags.TranslateWithUnityRichText) == TranslationFlags.TranslateWithUnityRichText)
                ? NullColorUnity
                : NullColorTMPro);
    public string Translate(string? language)
    {
        if (language is null)
            return _defaultData.Processed;
        if (language.Length == 0 || language.Equals("0", StringComparison.Ordinal))
            language = L.Default;

        ref TranslationValue data = ref this[language];
        return data.Processed;
    }
    public string Translate(IPlayer? player) => player is null ? Translate(L.Default) : Translate(Localization.GetLang(player.Steam64));
    public string Translate(ulong player) => Translate(Localization.GetLang(player));
    public string Translate(ulong player, out Color color) => Translate(Localization.GetLang(player), out color);
    public string Translate(string? language, out Color color)
    {
        if (language is null)
        {
            color = _defaultData.Color;
            return _defaultData.ProcessedInner;
        }
        if (language.Length == 0 || language.Equals("0", StringComparison.Ordinal))
            language = L.Default;

        ref TranslationValue data = ref this[language];
        color = data.Color;
        return data.ProcessedInner;
    }
    private string BaseUnsafeTranslate(Type t, string val, string language, Type[] gens, object[] formatting, UCPlayer? target, ulong targetTeam)
    {
        if (gens.Length > formatting.Length)
            throw new ArgumentException("Insufficient amount of formatting arguments supplied.", nameof(formatting));
        for (int i = 0; i < gens.Length; ++i)
        {
            object v = formatting[i];
            if (v is not null && !gens[i].IsInstanceOfType(v))
            {
                if (gens[i].IsAssignableFrom(typeof(string)))
                {
                    formatting[i] = typeof(ToStringHelperClass<>).MakeGenericType(v.GetType())
                        .GetMethod("ToString", BindingFlags.Static | BindingFlags.Public)!
                        .Invoke(null, new object[] { language, (t.GetField("_arg" + i.ToString(Data.AdminLocale) + "Fmt",
                            BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(this) as string)!, target!, Localization.GetLocale(language), this.Flags });
                    continue;
                }
                throw new ArgumentException("Formatting argument at index " + i + " is not a type compatable with it's generic type!", nameof(formatting) + "[" + i + "]");
            }
        }
        object[] newCallArr = new object[gens.Length + 5];
        Array.Copy(formatting, 0, newCallArr, 2, gens.Length);
        newCallArr[0] = val;
        newCallArr[1] = language;
        int ind = gens.Length + 2;
        newCallArr[ind] = target!;
        newCallArr[ind + 1] = targetTeam;
        newCallArr[ind + 2] = this.Flags;
        return (string)this.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(x => x.Name.Equals("Translate", StringComparison.Ordinal) && x.GetParameters().Length == 5 + gens.Length)!.Invoke(this, newCallArr);
    }
    /// <exception cref="ArgumentException">Either not enough formatting arguments were supplied or </exception>
    internal string TranslateUnsafe(string language, object[] formatting, UCPlayer? target = null, ulong targetTeam = 0)
    {
        Type t = this.GetType();
        Type[] gens = t.GenericTypeArguments;
        string val = this.Translate(language);
        if (gens.Length == 0 || formatting is null || formatting.Length == 0)
            return val;
        return BaseUnsafeTranslate(t, val, language, gens, formatting, target, targetTeam);
    }
    internal string TranslateUnsafe(string language, out Color color, object[] formatting, UCPlayer? target = null, ulong targetTeam = 0)
    {
        Type t = this.GetType();
        Type[] gens = t.GenericTypeArguments;
        string val = this.Translate(language, out color);
        if (gens.Length == 0 || formatting is null || formatting.Length == 0)
            return val;
        return BaseUnsafeTranslate(t, val, language, gens, formatting, target, targetTeam);
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
        foreach (FieldInfo field in typeof(UCWarfare).Assembly.GetTypes().SelectMany(x => x.GetFields(BindingFlags.Public | BindingFlags.Static)).Where(x => (x.IsLiteral || x.IsInitOnly) && x.FieldType == typeof(string)))
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
    internal static void ReadTranslations()
    {
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
            if (lang.Equals(L.Default, StringComparison.OrdinalIgnoreCase))
            {
                lang = L.Default;
                defRead = true;
            }
            else
            {
                foreach (LanguageAliasSet set in Data.LanguageAliases)
                {
                    if (set.key.Equals(lang, StringComparison.OrdinalIgnoreCase))
                    {
                        lang = set.key;
                        goto foundLanguage;
                    }
                }

                L.LogWarning("Unknown language: " + lang + ", skipping directory.");
                continue;
            }

            foundLanguage:
            using (FileStream str = new FileStream(info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (StreamReader reader = new StreamReader(str))
            {
                string? multiline = null;
                while (true)
                {
                    string line = reader.ReadLine()!;
                    if (line is null) break;
                    if (line.Length == 0 || line[0] is '#' or ' ' or '!')
                        continue;
                    if (multiline is not null)
                    {
                        line = multiline + line;
                        multiline = null;
                    }

                    int j2 = line.Length - 1;
                    for (; j2 >= 0; --j2)
                    {
                        if (line[j2] != '\\') break;
                    }

                    if (line.Length - j2 % 2 == 0)
                    {
                        multiline = line.Substring(0, line.Length - 1);
                        continue;
                    }
                    int ind2 = line.IndexOf(' ');
                    if (ind2 < 1)
                    {
                        ind2 = line.IndexOf('=');
                        if (ind2 < 1)
                        {
                            ind2 = line.IndexOf(':');
                            if (ind2 < 1)
                                continue;
                        }
                    }
                    int ind1 = ind2;
                    while (line.Length - 1 > ind2 && line[ind2 + 1] is '=' or ' ' or ':')
                        ++ind2;
                    if (line.Length - 1 > ind2 && line[ind2 + 1] == '\\' && line[ind2 + 2] == ' ')
                        ++ind2;
                    string key = line.Substring(0, ind1);
                    for (j2 = key.Length - 1; j2 >= 0; --j2)
                    {
                        if (key[j2] is not ' ' and not ':' and not '=')
                            break;
                    }

                    if (j2 != key.Length - 1)
                        key = key.Substring(0, j2 + 1);
                    for (int j = 0; j < T.Translations.Length; ++j)
                    {
                        Translation t = T.Translations[j];
                        if (t.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            string value = line.Substring(ind2 + 1, line.Length - ind2 - 1).Replace(@"\\", @"\").Replace(@"\n", "\n").Replace(@"\r", "\r").Replace(@"\t", "\t");
                            ++amt;
                            t.AddTranslation(langFolder.Name, value);
                            if (!T.AllLanguages.Contains(langFolder.Name, StringComparer.Ordinal))
                                T.AllLanguages.Add(langFolder.Name);
                            goto n;
                        }
                    }
                    L.LogWarning("[TRANSLATIONS] Unknown translation key: " + key + " in " + lang + " translation file.");
                n:;
                }
            }

            WriteLanguage(lang);
            L.Log("Loaded " + amt + " translations for " + lang + ".", ConsoleColor.Magenta);
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
                    ref TranslationValue v = ref t._data[i];
                    if (v.Language.Equals(L.Default, StringComparison.Ordinal))
                        goto c;
                }
            }
            ++amt;
            t.AddTranslation(L.Default, t._defaultData.Original);
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
        foreach (Translation t in T.Translations.OrderBy(x => x._attr?.Section ?? "~", StringComparer.OrdinalIgnoreCase))
        {
            WriteTranslation(writer, t, t._defaultData.Original, ref lastSection);
        }

        writer.Flush();
    }
    private static void WriteLanguage(string language)
    {
        DirectoryInfo dir = new DirectoryInfo(Path.Combine(Data.Paths.LangStorage, language));
        if (!dir.Exists)
            dir.Create();
        FileInfo info = new FileInfo(Path.Combine(dir.FullName, LocalFileName));
        using FileStream str = new FileStream(info.FullName, FileMode.Create, FileAccess.Write, FileShare.Read);
        using StreamWriter writer = new StreamWriter(str);
        string? lastSection = null;
        foreach (Translation t in T.Translations.OrderBy(x => x._attr?.Section ?? "~", StringComparer.OrdinalIgnoreCase))
        {
            if (t._data is null) continue;
            string? val = null;
            for (int i = 0; i < t._data.Length; ++i)
            {
                ref TranslationValue v = ref t._data[i];
                if (v.Language.Equals(language, StringComparison.Ordinal))
                    val = v.Original;
            }
            if (val is not null)
                WriteTranslation(writer, t, val, ref lastSection);
        }

        writer.Flush();
    }
    private static void WriteTranslation(StreamWriter writer, Translation t, string val, ref string? lastSection)
    {
        if (string.IsNullOrEmpty(val)) return;
        string? sect = t._attr?.Section;
        if (sect != lastSection)
        {
            lastSection = sect;
            if (writer.BaseStream.Position > 0)
                writer.WriteLine();
            if (sect is not null)
                writer.WriteLine("! " + sect + " !");
        }
        writer.WriteLine();
        if (t._attr is not null)
        {
            if (t._attr.Description is not null)
                writer.WriteLine("# Description: " + t._attr.Description.RemoveMany(true, '\r', '\n'));
        }

        Type tt = t.GetType();
        Type[] gen = tt.GetGenericArguments();
        if (gen.Length > 0)
            writer.WriteLine("# Formatting Arguments:");
        for (int i = 0; i < gen.Length; ++i)
        {
            Type type = gen[i];
            string vi = i.ToString(Data.AdminLocale);
            string fmt = "#  " + "{" + vi + "} - [" + ToString(type, L.Default, null, null, TranslationFlags.NoColorOptimization) + "]";

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

            if (t._attr is not null && t._attr.FormattingDescriptions is not null && i < t._attr.FormattingDescriptions.Length && !string.IsNullOrEmpty(t._attr.FormattingDescriptions[i]))
                fmt += " " + t._attr.FormattingDescriptions[i].RemoveMany(true, '\r', '\n');

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
    string Translate(string language, string? format, UCPlayer? target, ref TranslationFlags flags);
}
public sealed class Translation<T> : Translation
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
    public string Translate(string value, string language, T arg, UCPlayer? target, ulong targetTeam, TranslationFlags flags)
    {
        flags |= GetFlags(targetTeam);
        try
        {
            return string.Format(value, ToString(arg, language, _arg0Fmt, target, CheckPlurality(_arg0PluralExp, flags)));
        }
        catch (FormatException ex)
        {
            if ((Flags & TranslationFlags.ThrowMissingException) == TranslationFlags.ThrowMissingException)
            {
                throw new ArgumentException("[TRANSLATIONS] Error while formatting " + Key, ex);
            }

            L.LogError("[TRANSLATIONS] Error while formatting " + Key);
            L.LogError(ex);
            return InvalidValue;
        }
    }

    private TranslationFlags CheckPlurality(short expectation, TranslationFlags flags)
    {
        return expectation == -1 ? flags : flags | TranslationFlags.Plural;
    }
    public string Translate(string language, T arg, UCPlayer? target = null, ulong team = 0) =>
        Translate(Translate(language), language, arg, target, team != 0 ? team : (target == null ? 0 : target.GetTeam()), Flags);
    public string Translate(string language, T arg, out Color color, UCPlayer? target = null, ulong team = 0) =>
        Translate(Translate(language, out color), language, arg, target, team != 0 ? team : (target == null ? 0 : target.GetTeam()), Flags);
    public string Translate(UCPlayer player, T arg)
    {
        string lang = player is null ? L.Default : Localization.GetLang(player.Steam64);
        return Translate(Translate(lang), lang, arg, player, player is null ? 0 : player.GetTeam(), Flags);
    }
    public string Translate(ulong player, T arg)
    {
        UCPlayer? pl = UCPlayer.FromID(player);
        string lang = Localization.GetLang(player);
        return Translate(Translate(lang), lang, arg, pl, pl is null ? 0 : pl.GetTeam(), Flags);
    }
}
public sealed class Translation<T1, T2> : Translation
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
    public string Translate(string value, string language, T1 arg1, T2 arg2, UCPlayer? target, ulong targetTeam, TranslationFlags flags)
    {
        flags |= GetFlags(targetTeam);
        try
        {
            return string.Format(value, ToString(arg1, language, _arg0Fmt, target, CheckPlurality(_arg0PluralExp, flags, arg1, arg2)),
                ToString(arg2, language, _arg1Fmt, target, CheckPlurality(_arg1PluralExp, flags, arg1, arg2)));
        }
        catch (FormatException ex)
        {
            if ((Flags & TranslationFlags.ThrowMissingException) == TranslationFlags.ThrowMissingException)
            {
                throw new ArgumentException("[TRANSLATIONS] Error while formatting " + Key, ex);
            }

            L.LogError("[TRANSLATIONS] Error while formatting " + Key);
            L.LogError(ex);
            return InvalidValue;
        }
    }
    private TranslationFlags CheckPlurality(short expectation, TranslationFlags flags, in T1 arg1, in T2 arg2)
    {
        if (expectation == -1)
            return flags;
        flags |= expectation switch
        {
            0 => IsOne(arg1) ? TranslationFlags.NoPlural : TranslationFlags.Plural,
            1 => IsOne(arg2) ? TranslationFlags.NoPlural : TranslationFlags.Plural,
            _ => TranslationFlags.Plural
        };
        return flags;
    }
    public string Translate(string language, T1 arg1, T2 arg2, UCPlayer? target = null, ulong team = 0) =>
        Translate(Translate(language), language, arg1, arg2, target, team != 0 ? team : (target == null ? 0 : target.GetTeam()), Flags);
    public string Translate(string language, T1 arg1, T2 arg2, out Color color, UCPlayer? target = null, ulong team = 0) =>
        Translate(Translate(language, out color), language, arg1, arg2, target, team != 0 ? team : (target == null ? 0 : target.GetTeam()), Flags);
    public string Translate(UCPlayer player, T1 arg1, T2 arg2)
    {
        string lang = player is null ? L.Default : Localization.GetLang(player.Steam64);
        return Translate(Translate(lang), lang, arg1, arg2, player, player is null ? 0 : player.GetTeam(), Flags);
    }
    public string Translate(ulong player, T1 arg1, T2 arg2)
    {
        UCPlayer? pl = UCPlayer.FromID(player);
        string lang = Localization.GetLang(player);
        return Translate(Translate(lang), lang, arg1, arg2, pl, pl is null ? 0 : pl.GetTeam(), Flags);
    }
}
public sealed class Translation<T1, T2, T3> : Translation
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
    public string Translate(string value, string language, T1 arg1, T2 arg2, T3 arg3, UCPlayer? target, ulong targetTeam, TranslationFlags flags)
    {
        flags |= GetFlags(targetTeam);
        try
        {
            return string.Format(value, ToString(arg1, language, _arg0Fmt, target, CheckPlurality(_arg0PluralExp, flags, arg1, arg2, arg3)),
                ToString(arg2, language, _arg1Fmt, target, CheckPlurality(_arg1PluralExp, flags, arg1, arg2, arg3)),
                ToString(arg3, language, _arg2Fmt, target, CheckPlurality(_arg2PluralExp, flags, arg1, arg2, arg3)));
        }
        catch (FormatException ex)
        {
            if ((Flags & TranslationFlags.ThrowMissingException) == TranslationFlags.ThrowMissingException)
            {
                throw new ArgumentException("[TRANSLATIONS] Error while formatting " + Key, ex);
            }

            L.LogError("[TRANSLATIONS] Error while formatting " + Key);
            L.LogError(ex);
            return InvalidValue;
        }
    }
    private TranslationFlags CheckPlurality(short expectation, TranslationFlags flags, in T1 arg1, in T2 arg2, in T3 arg3)
    {
        if (expectation == -1)
            return flags;
        flags |= TranslationFlags.Plural;
        return expectation switch
        {
            0 => IsOne(arg1) ? flags | TranslationFlags.NoPlural : flags,
            1 => IsOne(arg2) ? flags | TranslationFlags.NoPlural : flags,
            2 => IsOne(arg3) ? flags | TranslationFlags.NoPlural : flags,
            _ => flags
        };
    }
    public string Translate(string language, T1 arg1, T2 arg2, T3 arg3, UCPlayer? target = null, ulong team = 0) =>
        Translate(Translate(language), language, arg1, arg2, arg3, target, team != 0 ? team : (target == null ? 0 : target.GetTeam()), Flags);
    public string Translate(string language, T1 arg1, T2 arg2, T3 arg3, out Color color, UCPlayer? target = null, ulong team = 0) =>
        Translate(Translate(language, out color), language, arg1, arg2, arg3, target, team != 0 ? team : (target == null ? 0 : target.GetTeam()), Flags);
    public string Translate(IPlayer player, T1 arg1, T2 arg2, T3 arg3)
    {
        string lang = player is null ? L.Default : Localization.GetLang(player.Steam64);
        return Translate(Translate(lang), lang, arg1, arg2, arg3, player as UCPlayer, player is null ? 0 : player.GetTeam(), Flags);
    }
    public string Translate(ulong player, T1 arg1, T2 arg2, T3 arg3)
    {
        UCPlayer? pl = UCPlayer.FromID(player);
        string lang = Localization.GetLang(player);
        return Translate(Translate(lang), lang, arg1, arg2, arg3, pl, pl is null ? 0 : pl.GetTeam(), Flags);
    }
}
public sealed class Translation<T1, T2, T3, T4> : Translation
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
    public string Translate(string value, string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, UCPlayer? target, ulong targetTeam, TranslationFlags flags)
    {
        flags |= GetFlags(targetTeam);
        try
        {
            return string.Format(value, ToString(arg1, language, _arg0Fmt, target, CheckPlurality(_arg0PluralExp, flags, arg1, arg2, arg3, arg4)),
                ToString(arg2, language, _arg1Fmt, target, CheckPlurality(_arg1PluralExp, flags, arg1, arg2, arg3, arg4)),
                ToString(arg3, language, _arg2Fmt, target, CheckPlurality(_arg2PluralExp, flags, arg1, arg2, arg3, arg4)),
                ToString(arg4, language, _arg3Fmt, target, CheckPlurality(_arg3PluralExp, flags, arg1, arg2, arg3, arg4)));
        }
        catch (FormatException ex)
        {
            if ((Flags & TranslationFlags.ThrowMissingException) == TranslationFlags.ThrowMissingException)
            {
                throw new ArgumentException("[TRANSLATIONS] Error while formatting " + Key, ex);
            }

            L.LogError("[TRANSLATIONS] Error while formatting " + Key);
            L.LogError(ex);
            return InvalidValue;
        }
    }
    private TranslationFlags CheckPlurality(short expectation, TranslationFlags flags, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4)
    {
        if (expectation == -1)
            return flags;
        flags |= TranslationFlags.Plural;
        return expectation switch
        {
            0 => IsOne(arg1) ? flags | TranslationFlags.NoPlural : flags,
            1 => IsOne(arg2) ? flags | TranslationFlags.NoPlural : flags,
            2 => IsOne(arg3) ? flags | TranslationFlags.NoPlural : flags,
            3 => IsOne(arg4) ? flags | TranslationFlags.NoPlural : flags,
            _ => flags
        };
    }
    public string Translate(string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, UCPlayer? target = null, ulong team = 0) =>
        Translate(Translate(language), language, arg1, arg2, arg3, arg4, target, team != 0 ? team : (target == null ? 0 : target.GetTeam()), Flags);
    public string Translate(string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, out Color color, UCPlayer? target = null, ulong team = 0) =>
        Translate(Translate(language, out color), language, arg1, arg2, arg3, arg4, target, team != 0 ? team : (target == null ? 0 : target.GetTeam()), Flags);
    public string Translate(IPlayer player, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        string lang = player is null ? L.Default : Localization.GetLang(player.Steam64);
        return Translate(Translate(lang), lang, arg1, arg2, arg3, arg4, player as UCPlayer, player is null ? 0 : player.GetTeam(), Flags);
    }
    public string Translate(ulong player, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        UCPlayer? pl = UCPlayer.FromID(player);
        string lang = Localization.GetLang(player);
        return Translate(Translate(lang), lang, arg1, arg2, arg3, arg4, pl, pl is null ? 0 : pl.GetTeam(), Flags);
    }
}
public sealed class Translation<T1, T2, T3, T4, T5> : Translation
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
    public string Translate(string value, string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, UCPlayer? target, ulong targetTeam, TranslationFlags flags)
    {
        flags |= GetFlags(targetTeam);
        try
        {
            return string.Format(value, ToString(arg1, language, _arg0Fmt, target, CheckPlurality(_arg0PluralExp, flags, arg1, arg2, arg3, arg4, arg5)),
                ToString(arg2, language, _arg1Fmt, target, CheckPlurality(_arg1PluralExp, flags, arg1, arg2, arg3, arg4, arg5)),
                ToString(arg3, language, _arg2Fmt, target, CheckPlurality(_arg2PluralExp, flags, arg1, arg2, arg3, arg4, arg5)),
                ToString(arg4, language, _arg3Fmt, target, CheckPlurality(_arg3PluralExp, flags, arg1, arg2, arg3, arg4, arg5)),
                ToString(arg5, language, _arg4Fmt, target, CheckPlurality(_arg4PluralExp, flags, arg1, arg2, arg3, arg4, arg5)));
        }
        catch (FormatException ex)
        {
            if ((Flags & TranslationFlags.ThrowMissingException) == TranslationFlags.ThrowMissingException)
            {
                throw new ArgumentException("[TRANSLATIONS] Error while formatting " + Key, ex);
            }

            L.LogError("[TRANSLATIONS] Error while formatting " + Key);
            L.LogError(ex);
            return InvalidValue;
        }
    }
    private TranslationFlags CheckPlurality(short expectation, TranslationFlags flags, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5)
    {
        if (expectation == -1)
            return flags;
        flags |= TranslationFlags.Plural;
        return expectation switch
        {
            0 => IsOne(arg1) ? flags | TranslationFlags.NoPlural : flags,
            1 => IsOne(arg2) ? flags | TranslationFlags.NoPlural : flags,
            2 => IsOne(arg3) ? flags | TranslationFlags.NoPlural : flags,
            3 => IsOne(arg4) ? flags | TranslationFlags.NoPlural : flags,
            4 => IsOne(arg5) ? flags | TranslationFlags.NoPlural : flags,
            _ => flags
        };
    }
    public string Translate(string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, UCPlayer? target = null, ulong team = 0) =>
        Translate(Translate(language), language, arg1, arg2, arg3, arg4, arg5, target, team != 0 ? team : (target == null ? 0 : target.GetTeam()), Flags);
    public string Translate(string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, out Color color, UCPlayer? target = null, ulong team = 0) =>
        Translate(Translate(language, out color), language, arg1, arg2, arg3, arg4, arg5, target, team != 0 ? team : (target == null ? 0 : target.GetTeam()), Flags);
    public string Translate(IPlayer player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        string lang = player is null ? L.Default : Localization.GetLang(player.Steam64);
        return Translate(Translate(lang), lang, arg1, arg2, arg3, arg4, arg5, player as UCPlayer, player is null ? 0 : player.GetTeam(), Flags);
    }
    public string Translate(ulong player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        UCPlayer? pl = UCPlayer.FromID(player);
        string lang = Localization.GetLang(player);
        return Translate(Translate(lang), lang, arg1, arg2, arg3, arg4, arg5, pl, pl is null ? 0 : pl.GetTeam(), Flags);
    }
}
public sealed class Translation<T1, T2, T3, T4, T5, T6> : Translation
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
    public string Translate(string value, string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, UCPlayer? target, ulong targetTeam, TranslationFlags flags)
    {
        flags |= GetFlags(targetTeam);
        try
        {
            return string.Format(value, ToString(arg1, language, _arg0Fmt, target, CheckPlurality(_arg0PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6)),
                ToString(arg2, language, _arg1Fmt, target, CheckPlurality(_arg1PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6)),
                ToString(arg3, language, _arg2Fmt, target, CheckPlurality(_arg2PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6)),
                ToString(arg4, language, _arg3Fmt, target, CheckPlurality(_arg3PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6)),
                ToString(arg5, language, _arg4Fmt, target, CheckPlurality(_arg4PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6)),
                ToString(arg6, language, _arg5Fmt, target, CheckPlurality(_arg5PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6)));
        }
        catch (FormatException ex)
        {
            if ((Flags & TranslationFlags.ThrowMissingException) == TranslationFlags.ThrowMissingException)
            {
                throw new ArgumentException("[TRANSLATIONS] Error while formatting " + Key, ex);
            }

            L.LogError("[TRANSLATIONS] Error while formatting " + Key);
            L.LogError(ex);
            return InvalidValue;
        }
    }
    private TranslationFlags CheckPlurality(short expectation, TranslationFlags flags, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6)
    {
        if (expectation == -1)
            return flags;
        flags |= TranslationFlags.Plural;
        return expectation switch
        {
            0 => IsOne(arg1) ? flags | TranslationFlags.NoPlural : flags,
            1 => IsOne(arg2) ? flags | TranslationFlags.NoPlural : flags,
            2 => IsOne(arg3) ? flags | TranslationFlags.NoPlural : flags,
            3 => IsOne(arg4) ? flags | TranslationFlags.NoPlural : flags,
            4 => IsOne(arg5) ? flags | TranslationFlags.NoPlural : flags,
            5 => IsOne(arg6) ? flags | TranslationFlags.NoPlural : flags,
            _ => flags
        };
    }
    public string Translate(string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, UCPlayer? target = null, ulong team = 0) =>
        Translate(Translate(language), language, arg1, arg2, arg3, arg4, arg5, arg6, target, team != 0 ? team : (target == null ? 0 : target.GetTeam()), Flags);
    public string Translate(string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, out Color color, UCPlayer? target = null, ulong team = 0) =>
        Translate(Translate(language, out color), language, arg1, arg2, arg3, arg4, arg5, arg6, target, team != 0 ? team : (target == null ? 0 : target.GetTeam()), Flags);
    public string Translate(IPlayer player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        string lang = player is null ? L.Default : Localization.GetLang(player.Steam64);
        return Translate(Translate(lang), lang, arg1, arg2, arg3, arg4, arg5, arg6, player as UCPlayer, player is null ? 0 : player.GetTeam(), Flags);
    }
    public string Translate(ulong player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        UCPlayer? pl = UCPlayer.FromID(player);
        string lang = Localization.GetLang(player);
        return Translate(Translate(lang), lang, arg1, arg2, arg3, arg4, arg5, arg6, pl, pl is null ? 0 : pl.GetTeam(), Flags);
    }
}
public sealed class Translation<T1, T2, T3, T4, T5, T6, T7> : Translation
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
    public string Translate(string value, string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, UCPlayer? target, ulong targetTeam, TranslationFlags flags)
    {
        flags |= GetFlags(targetTeam);
        try
        {
            return string.Format(value, ToString(arg1, language, _arg0Fmt, target, CheckPlurality(_arg0PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7)),
                ToString(arg2, language, _arg1Fmt, target, CheckPlurality(_arg1PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7)),
                ToString(arg3, language, _arg2Fmt, target, CheckPlurality(_arg2PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7)),
                ToString(arg4, language, _arg3Fmt, target, CheckPlurality(_arg3PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7)),
                ToString(arg5, language, _arg4Fmt, target, CheckPlurality(_arg4PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7)),
                ToString(arg6, language, _arg5Fmt, target, CheckPlurality(_arg5PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7)),
                ToString(arg7, language, _arg6Fmt, target, CheckPlurality(_arg6PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7)));
        }
        catch (FormatException ex)
        {
            if ((Flags & TranslationFlags.ThrowMissingException) == TranslationFlags.ThrowMissingException)
            {
                throw new ArgumentException("[TRANSLATIONS] Error while formatting " + Key, ex);
            }

            L.LogError("[TRANSLATIONS] Error while formatting " + Key);
            L.LogError(ex);
            return InvalidValue;
        }
    }
    private TranslationFlags CheckPlurality(short expectation, TranslationFlags flags, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7)
    {
        if (expectation == -1)
            return flags;
        flags |= TranslationFlags.Plural;
        return expectation switch
        {
            0 => IsOne(arg1) ? flags | TranslationFlags.NoPlural : flags,
            1 => IsOne(arg2) ? flags | TranslationFlags.NoPlural : flags,
            2 => IsOne(arg3) ? flags | TranslationFlags.NoPlural : flags,
            3 => IsOne(arg4) ? flags | TranslationFlags.NoPlural : flags,
            4 => IsOne(arg5) ? flags | TranslationFlags.NoPlural : flags,
            5 => IsOne(arg6) ? flags | TranslationFlags.NoPlural : flags,
            6 => IsOne(arg7) ? flags | TranslationFlags.NoPlural : flags,
            _ => flags
        };
    }
    public string Translate(string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, UCPlayer? target = null, ulong team = 0) =>
        Translate(Translate(language), language, arg1, arg2, arg3, arg4, arg5, arg6, arg7, target, team != 0 ? team : (target == null ? 0 : target.GetTeam()), Flags);
    public string Translate(string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, out Color color, UCPlayer? target = null, ulong team = 0) =>
        Translate(Translate(language, out color), language, arg1, arg2, arg3, arg4, arg5, arg6, arg7, target, team != 0 ? team : (target == null ? 0 : target.GetTeam()), Flags);
    public string Translate(IPlayer player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        string lang = player is null ? L.Default : Localization.GetLang(player.Steam64);
        return Translate(Translate(lang), lang, arg1, arg2, arg3, arg4, arg5, arg6, arg7, player as UCPlayer, player is null ? 0 : player.GetTeam(), Flags);
    }
    public string Translate(ulong player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        UCPlayer? pl = UCPlayer.FromID(player);
        string lang = Localization.GetLang(player);
        return Translate(Translate(lang), lang, arg1, arg2, arg3, arg4, arg5, arg6, arg7, pl, pl is null ? 0 : pl.GetTeam(), Flags);
    }
}
public sealed class Translation<T1, T2, T3, T4, T5, T6, T7, T8> : Translation
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
    public string Translate(string value, string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, UCPlayer? target, ulong targetTeam, TranslationFlags flags)
    {
        flags |= GetFlags(targetTeam);
        try
        {
            return string.Format(value, ToString(arg1, language, _arg0Fmt, target, CheckPlurality(_arg0PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8)),
                ToString(arg2, language, _arg1Fmt, target, CheckPlurality(_arg1PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8)),
                ToString(arg3, language, _arg2Fmt, target, CheckPlurality(_arg2PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8)),
                ToString(arg4, language, _arg3Fmt, target, CheckPlurality(_arg3PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8)),
                ToString(arg5, language, _arg4Fmt, target, CheckPlurality(_arg4PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8)),
                ToString(arg6, language, _arg5Fmt, target, CheckPlurality(_arg5PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8)),
                ToString(arg7, language, _arg6Fmt, target, CheckPlurality(_arg6PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8)),
                ToString(arg8, language, _arg7Fmt, target, CheckPlurality(_arg7PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8)));
        }
        catch (FormatException ex)
        {
            if ((Flags & TranslationFlags.ThrowMissingException) == TranslationFlags.ThrowMissingException)
            {
                throw new ArgumentException("[TRANSLATIONS] Error while formatting " + Key, ex);
            }

            L.LogError("[TRANSLATIONS] Error while formatting " + Key);
            L.LogError(ex);
            return InvalidValue;
        }
    }
    private TranslationFlags CheckPlurality(short expectation, TranslationFlags flags, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7, in T8 arg8)
    {
        if (expectation == -1)
            return flags;
        flags |= TranslationFlags.Plural;
        return expectation switch
        {
            0 => IsOne(arg1) ? flags | TranslationFlags.NoPlural : flags,
            1 => IsOne(arg2) ? flags | TranslationFlags.NoPlural : flags,
            2 => IsOne(arg3) ? flags | TranslationFlags.NoPlural : flags,
            3 => IsOne(arg4) ? flags | TranslationFlags.NoPlural : flags,
            4 => IsOne(arg5) ? flags | TranslationFlags.NoPlural : flags,
            5 => IsOne(arg6) ? flags | TranslationFlags.NoPlural : flags,
            6 => IsOne(arg7) ? flags | TranslationFlags.NoPlural : flags,
            7 => IsOne(arg8) ? flags | TranslationFlags.NoPlural : flags,
            _ => flags
        };
    }
    public string Translate(string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, UCPlayer? target = null, ulong team = 0) =>
        Translate(Translate(language), language, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, target, team != 0 ? team : (target == null ? 0 : target.GetTeam()), Flags);
    public string Translate(string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, out Color color, UCPlayer? target = null, ulong team = 0) =>
        Translate(Translate(language, out color), language, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, target, team != 0 ? team : (target == null ? 0 : target.GetTeam()), Flags);
    public string Translate(IPlayer player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        string lang = player is null ? L.Default : Localization.GetLang(player.Steam64);
        return Translate(Translate(lang), lang, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, player as UCPlayer, player is null ? 0 : player.GetTeam(), Flags);
    }
    public string Translate(ulong player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        UCPlayer? pl = UCPlayer.FromID(player);
        string lang = Localization.GetLang(player);
        return Translate(Translate(lang), lang, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, pl, pl is null ? 0 : pl.GetTeam(), Flags);
    }
}
public sealed class Translation<T1, T2, T3, T4, T5, T6, T7, T8, T9> : Translation
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
    public string Translate(string value, string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, UCPlayer? target, ulong targetTeam, TranslationFlags flags)
    {
        flags |= GetFlags(targetTeam);
        try
        {
            return string.Format(value, ToString(arg1, language, _arg0Fmt, target, CheckPlurality(_arg0PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9)),
                ToString(arg2, language, _arg1Fmt, target, CheckPlurality(_arg1PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9)),
                ToString(arg3, language, _arg2Fmt, target, CheckPlurality(_arg2PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9)),
                ToString(arg4, language, _arg3Fmt, target, CheckPlurality(_arg3PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9)),
                ToString(arg5, language, _arg4Fmt, target, CheckPlurality(_arg4PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9)),
                ToString(arg6, language, _arg5Fmt, target, CheckPlurality(_arg5PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9)),
                ToString(arg7, language, _arg6Fmt, target, CheckPlurality(_arg6PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9)),
                ToString(arg8, language, _arg7Fmt, target, CheckPlurality(_arg7PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9)),
                ToString(arg9, language, _arg8Fmt, target, CheckPlurality(_arg8PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9)));
        }
        catch (FormatException ex)
        {
            if ((Flags & TranslationFlags.ThrowMissingException) == TranslationFlags.ThrowMissingException)
            {
                throw new ArgumentException("[TRANSLATIONS] Error while formatting " + Key, ex);
            }
            L.LogError("[TRANSLATIONS] Error while formatting " + Key);
            L.LogError(ex);
            return InvalidValue;
        }
    }
    private TranslationFlags CheckPlurality(short expectation, TranslationFlags flags, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7, in T8 arg8, in T9 arg9)
    {
        if (expectation == -1)
            return flags;
        flags |= TranslationFlags.Plural;
        return expectation switch
        {
            0 => IsOne(arg1) ? flags | TranslationFlags.NoPlural : flags,
            1 => IsOne(arg2) ? flags | TranslationFlags.NoPlural : flags,
            2 => IsOne(arg3) ? flags | TranslationFlags.NoPlural : flags,
            3 => IsOne(arg4) ? flags | TranslationFlags.NoPlural : flags,
            4 => IsOne(arg5) ? flags | TranslationFlags.NoPlural : flags,
            5 => IsOne(arg6) ? flags | TranslationFlags.NoPlural : flags,
            6 => IsOne(arg7) ? flags | TranslationFlags.NoPlural : flags,
            7 => IsOne(arg8) ? flags | TranslationFlags.NoPlural : flags,
            8 => IsOne(arg9) ? flags | TranslationFlags.NoPlural : flags,
            _ => flags
        };
    }
    public string Translate(string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, UCPlayer? target = null, ulong team = 0) =>
        Translate(Translate(language), language, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, target, team != 0 ? team : (target == null ? 0 : target.GetTeam()), Flags);
    public string Translate(string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, out Color color, UCPlayer? target = null, ulong team = 0) =>
        Translate(Translate(language, out color), language, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, target, team != 0 ? team : (target == null ? 0 : target.GetTeam()), Flags);
    public string Translate(IPlayer player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        string lang = player is null ? L.Default : Localization.GetLang(player.Steam64);
        return Translate(Translate(lang), lang, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, player as UCPlayer, player is null ? 0 : player.GetTeam(), Flags);
    }
    public string Translate(ulong player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        UCPlayer? pl = UCPlayer.FromID(player);
        string lang = Localization.GetLang(player);
        return Translate(Translate(lang), lang, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, pl, pl is null ? 0 : pl.GetTeam(), Flags);
    }
}
public sealed class Translation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : Translation
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
    public string Translate(string value, string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, UCPlayer? target, ulong targetTeam, TranslationFlags flags)
    {
        flags |= GetFlags(targetTeam);
        try
        {
            return string.Format(value, ToString(arg1, language, _arg0Fmt, target, CheckPlurality(_arg0PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10)),
                ToString(arg2, language, _arg1Fmt, target, CheckPlurality(_arg1PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10)),
                ToString(arg3, language, _arg2Fmt, target, CheckPlurality(_arg2PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10)),
                ToString(arg4, language, _arg3Fmt, target, CheckPlurality(_arg3PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10)),
                ToString(arg5, language, _arg4Fmt, target, CheckPlurality(_arg4PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10)),
                ToString(arg6, language, _arg5Fmt, target, CheckPlurality(_arg5PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10)),
                ToString(arg7, language, _arg6Fmt, target, CheckPlurality(_arg6PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10)),
                ToString(arg8, language, _arg7Fmt, target, CheckPlurality(_arg7PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10)),
                ToString(arg9, language, _arg8Fmt, target, CheckPlurality(_arg8PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10)),
                ToString(arg10, language, _arg9Fmt, target, CheckPlurality(_arg9PluralExp, flags, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10)));
        }
        catch (FormatException ex)
        {
            if ((Flags & TranslationFlags.ThrowMissingException) == TranslationFlags.ThrowMissingException)
            {
                throw new ArgumentException("[TRANSLATIONS] Error while formatting " + Key, ex);
            }

            L.LogError("[TRANSLATIONS] Error while formatting " + Key);
            L.LogError(ex);
            return InvalidValue;
        }
    }
    private TranslationFlags CheckPlurality(short expectation, TranslationFlags flags, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7, in T8 arg8, in T9 arg9, in T10 arg10)
    {
        if (expectation == -1)
            return flags;
        flags |= TranslationFlags.Plural;
        return expectation switch
        {
            0 => arg1 is IComparable c && c.CompareTo(1) == 0 ? flags | TranslationFlags.NoPlural : flags,
            1 => arg2 is IComparable c && c.CompareTo(1) == 0 ? flags | TranslationFlags.NoPlural : flags,
            2 => arg3 is IComparable c && c.CompareTo(1) == 0 ? flags | TranslationFlags.NoPlural : flags,
            3 => arg4 is IComparable c && c.CompareTo(1) == 0 ? flags | TranslationFlags.NoPlural : flags,
            4 => arg5 is IComparable c && c.CompareTo(1) == 0 ? flags | TranslationFlags.NoPlural : flags,
            5 => arg6 is IComparable c && c.CompareTo(1) == 0 ? flags | TranslationFlags.NoPlural : flags,
            6 => arg7 is IComparable c && c.CompareTo(1) == 0 ? flags | TranslationFlags.NoPlural : flags,
            7 => arg8 is IComparable c && c.CompareTo(1) == 0 ? flags | TranslationFlags.NoPlural : flags,
            8 => arg9 is IComparable c && c.CompareTo(1) == 0 ? flags | TranslationFlags.NoPlural : flags,
            9 => arg10 is IComparable c && c.CompareTo(1) == 0 ? flags | TranslationFlags.NoPlural : flags,
            _ => flags
        };
    }
    public string Translate(string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, UCPlayer? target = null, ulong team = 0) =>
        Translate(Translate(language), language, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, target, team != 0 ? team : (target == null ? 0 : target.GetTeam()), Flags);
    public string Translate(string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, out Color color, UCPlayer? target = null, ulong team = 0) =>
        Translate(Translate(language, out color), language, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, target, team != 0 ? team : (target == null ? 0 : target.GetTeam()), Flags);
    public string Translate(IPlayer player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
    {
        string lang = player is null ? L.Default : Localization.GetLang(player.Steam64);
        return Translate(Translate(lang), lang, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, player as UCPlayer, player is null ? 0 : player.GetTeam(), Flags);
    }
    public string Translate(ulong player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
    {
        UCPlayer? pl = UCPlayer.FromID(player);
        string lang = Localization.GetLang(player);
        return Translate(Translate(lang), lang, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, pl, pl is null ? 0 : pl.GetTeam(), Flags);
    }
}
[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class TranslationDataAttribute : Attribute
{
    private string? _signId;
    private string? _description;
    private string? _section;
    private string[]? _formatArgs;
    private bool _announcerTranslation;
    public TranslationDataAttribute()
    {

    }
    public TranslationDataAttribute(string section)
    {
        _section = section;
    }
    public TranslationDataAttribute(string section, string description, params string[] parameters)
    {
        _section = section;
        _description = description;
        for (int i = 0; i < parameters.Length; ++i)
        {
            parameters[i] ??= string.Empty;
        }

        _formatArgs = parameters;
    }
    public string? SignId { get => _signId; set => _signId = value; }
    public string? Description { get => _description; set => _description = value; }
    public string? Section { get => _section; set => _section = value; }
    public string[]? FormattingDescriptions { get => _formatArgs; set => _formatArgs = value; }
    public bool IsAnnounced { get => _announcerTranslation; set => _announcerTranslation = value; }
}

[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
public sealed class FormatDisplayAttribute : Attribute
{
    private readonly string _displayName;
    private readonly Type? _forType;
    private readonly bool _typeSupplied;
    public FormatDisplayAttribute(string displayName)
    {
        _displayName = displayName;
    }
    public FormatDisplayAttribute(Type? forType, string displayName)
    {
        _displayName = displayName;
        _typeSupplied = true;
        _forType = forType;
    }

    public string DisplayName => _displayName;
    public bool TypeSupplied => _typeSupplied;
    public Type? TargetType => _forType;
}