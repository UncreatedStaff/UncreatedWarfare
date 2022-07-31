using SDG.Unturned;
using Steamworks;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Uncreated.Warfare.Gamemodes.Flags;
using UnityEngine;

namespace Uncreated.Warfare;

public class Translation
{
    private const string NULL_CLR_1 = "<#569cd6><b>null</b></color>";
    private const string NULL_CLR_2 = "<color=#569cd6><b>null</b></color>";
    private const string NULL_NO_CLR = "null";
    private const string LOCAL_FILE_NAME = "translations.properties";
    private readonly TranslationFlags _flags;
    private TranslationValue DefaultData;
    private TranslationValue[]? Data;
    private TranslationDataAttribute? attr;
    public string Key;
    public int Id;
    public TranslationFlags Flags => _flags;
    protected string InvalidValue => "Translation Error - " + Key;
    internal TranslationDataAttribute? AttributeData { get => attr; set => attr = value; }
    public Translation(string @default, TranslationFlags flags) : this(@default)
    {
        _flags = flags;
    }
    public Translation(string @default)
    {
        string def2 = @default;
        Color clr = ProcessValue(ref def2, out string inner, Flags);
        DefaultData = new TranslationValue(JSONMethods.DEFAULT_LANGUAGE, @default, inner, def2, clr);
    }
    public void RefreshColors()
    {
        DefaultData = new TranslationValue(in DefaultData, Flags);
        if (Data is not null)
        {
            for (int i = 0; i < Data.Length; ++i)
                Data[i] = new TranslationValue(in Data[i], Flags);
        }
    }
    private void VerifyDefault(string def)
    {
        if ((_flags & TranslationFlags.SuppressWarnings) == TranslationFlags.SuppressWarnings) return;
        int ct = this.GetType().GenericTypeArguments.Length;
        int index = -1;
        int flag = 0;
        int max = 0;
        while (true)
        {
            index = def.IndexOf('{', index + 1);
            if (index == -1 || index >= def.Length - 2) break;
            char next = def[index + 1];
            if (next is >= '0' and <= '9')
            {
                char next2 = def[index + 2];
                int num;
                if (next2 is not >= '0' and <= '9')
                    num = next - 48;
                else
                    num = (next - 48) * 10 + (next2 - 48);
                flag |= num;
                if (max < num) max = num;
            }
        }

        for (int i = 0; i < ct; ++i)
        {
            if (((flag >> i) & 1) == 0)
            {
                L.LogWarning("[TRANSLATIONS] " + Key + " parameter at index " + i + " is unused.");
            }
        }
        if (ct == 0) return;
        --ct;
        if (max > ct)
            L.LogWarning("[TRANSLATIONS] " + Key + " has " + (ct - max == 1 ? ("an extra paremeter: " + max) : $"{ct - max} extra parameters: {ct + 1} to {max}"));
    }
    public void AddTranslation(string language, string value)
    {
        if (Data is null || Data.Length == 0)
            Data = new TranslationValue[1] { new TranslationValue(language, value, Flags) };
        else
        {
            for (int i = 0; i < Data.Length; ++i)
            {
                ref TranslationValue v = ref Data[i];
                if (v.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
                {
                    v = new TranslationValue(language, value, Flags);
                    return;
                }
            }

            TranslationValue[] old = Data;
            Data = new TranslationValue[old.Length + 1];
            if (language.Equals(JSONMethods.DEFAULT_LANGUAGE, StringComparison.OrdinalIgnoreCase))
            {
                Array.Copy(old, 0, Data, 1, old.Length);
                Data[0] = new TranslationValue(JSONMethods.DEFAULT_LANGUAGE, value, Flags);
            }
            else
            {
                Array.Copy(old, Data, old.Length);
                Data[Data.Length - 1] = new TranslationValue(language, value, Flags);
            }
        }
    }
    public void RemoveTranslation(string language)
    {
        if (Data is null || Data.Length == 0) return;
        int index = -1;
        for (int i = 0; i < Data.Length; ++i)
        {
            ref TranslationValue v = ref Data[i];
            if (v.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
            {
                index = i;
                break;
            }
        }
        if (index == -1) return;
        if (Data.Length == 1)
        {
            Data = Array.Empty<TranslationValue>();
            return;
        }
        TranslationValue[] old = Data;
        Data = new TranslationValue[old.Length - 1];
        if (index != 0)
            Array.Copy(old, 0, Data, 0, index);
        Array.Copy(old, index + 1, Data, index, old.Length - index - 1);
    }
    public void ClearTranslations() => Data = Array.Empty<TranslationValue>();
    protected ref TranslationValue this[string language]
    {
        get
        {
            if (Data is not null)
            {
                for (int i = 0; i < Data.Length; ++i)
                {
                    ref TranslationValue v = ref Data[i];
                    if (v.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
                    {
                        return ref v;
                    }
                }
            }
            return ref DefaultData;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToString<T>(T value, string language, string? format, UCPlayer? target, TranslationFlags flags) => ToStringHelperClass<T>.ToString(value, language, format, target, Warfare.Data.Locale, flags);

    private static readonly Type[] tarr1 = new Type[] { typeof(string), typeof(IFormatProvider) };
    private static readonly Type[] tarr2 = new Type[] { typeof(string) };
    private static readonly Type[] tarr3 = new Type[] { typeof(IFormatProvider) };
    private static class ToStringHelperClass<T>
    {
        private static readonly Func<T, string, IFormatProvider, string>? toStringFunc1;
        private static readonly Func<T, string, string>? toStringFunc2;
        private static readonly Func<T, IFormatProvider, string>? toStringFunc3;
        private static readonly Func<T, string>? toStringFunc4;
        private static readonly int type;
        public static string ToString(T value, string language, string? format, UCPlayer? target, IFormatProvider locale, TranslationFlags flags)
        {
            if (value is null)
                return Null(flags);

            if (value is string str)
                return CheckCase(str, format);

            if ((flags & TranslationFlags.Plural) == TranslationFlags.Plural && format is not null && format.EndsWith(Warfare.T.PLURAL, StringComparison.Ordinal))
                format = format.Length == Warfare.T.PLURAL.Length ? null : format.Substring(0, format.Length - Warfare.T.PLURAL.Length);
            str = type switch
            {
                1 => toStringFunc1!(value, format!, locale),
                2 => toStringFunc2!(value, format!),
                3 => toStringFunc3!(value, locale),
                4 => (value as ITranslationArgument)!.Translate(language, format, target, ref flags),
                5 => CheckCase((value as UnityEngine.Object)!.name, format),
                6 => value is Color clr ? clr.Hex() : value.ToString(),
                7 => value is CSteamID id ? id.m_SteamID.ToString(format, locale) : value.ToString(),
                8 => PlayerToString((value as PlayerCaller)!.player, flags, format),
                9 => PlayerToString((value as Player)!, flags, format),
                10 => PlayerToString((value as SteamPlayer)!.player, flags, format),
                11 => PlayerToString((value as SteamPlayerID)!, flags, format),
                12 => CheckCase(value is Type t ? (t.IsEnum ? Localization.TranslateEnumName(t, language) : (t.IsArray ? (t.GetElementType().Name + " Array") : TypeToString(t))) : value.ToString(), format),
                13 => CheckCase(Localization.TranslateEnum(value, language), format),
                14 => CheckCase((value as Asset)?.FriendlyName ?? value.ToString(), format),
                15 => toStringFunc4!(value),
                16 => CheckCase((value as BarricadeData)?.barricade?.asset?.itemName ?? value.ToString(), format),
                17 => CheckCase((value as StructureData)?.structure?.asset?.itemName ?? value.ToString(), format),
                18 => value is Guid guid ? guid.ToString(format ?? "N", locale) : value.ToString(),
                _ => value.ToString(),
            };
            if ((flags & TranslationFlags.Plural) == TranslationFlags.Plural)
                str = Pluralize(str, language, flags);

            return str;
        }
        private static string CheckCase(string str, string? format)
        {
            if (format is not null)
            {
                if (format.Equals(Warfare.T.UPPERCASE, StringComparison.Ordinal))
                    return str.ToUpperInvariant();
                else if (format.Equals(Warfare.T.LOWERCASE, StringComparison.Ordinal))
                    return str.ToLowerInvariant();
                else if (format.Equals(Warfare.T.PROPERCASE, StringComparison.Ordinal))
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
            else if (!player.isActiveAndEnabled)
                return player.channel.owner.playerID.steamID.m_SteamID.ToString(Warfare.Data.Locale);
            Players.FPlayerName names = F.GetPlayerOriginalNames(player);
            if (format is not null)
            {
                if (format.Equals(UCPlayer.CHARACTER_NAME_FORMAT, StringComparison.Ordinal))
                    return names.CharacterName;
                else if (format.Equals(UCPlayer.COLORIZED_CHARACTER_NAME_FORMAT, StringComparison.Ordinal))
                    return Localization.Colorize(Teams.TeamManager.GetTeamHexColor(player.GetTeam()), names.CharacterName, flags);
                else if (format.Equals(UCPlayer.NICK_NAME_FORMAT, StringComparison.Ordinal))
                    return names.NickName;
                else if (format.Equals(UCPlayer.COLORIZED_NICK_NAME_FORMAT, StringComparison.Ordinal))
                    return Localization.Colorize(Teams.TeamManager.GetTeamHexColor(player.GetTeam()), names.NickName, flags);
                else if (format.Equals(UCPlayer.PLAYER_NAME_FORMAT, StringComparison.Ordinal))
                    return names.PlayerName;
                else if (format.Equals(UCPlayer.COLORIZED_PLAYER_NAME_FORMAT, StringComparison.Ordinal))
                    return Localization.Colorize(Teams.TeamManager.GetTeamHexColor(player.GetTeam()), names.PlayerName, flags);
                else if (format.Equals(UCPlayer.STEAM_64_FORMAT, StringComparison.Ordinal))
                    return player.channel.owner.playerID.steamID.m_SteamID.ToString(Warfare.Data.Locale);
                else if (format.Equals(UCPlayer.COLORIZED_STEAM_64_FORMAT, StringComparison.Ordinal))
                    return Localization.Colorize(Teams.TeamManager.GetTeamHexColor(player.GetTeam()), player.channel.owner.playerID.steamID.m_SteamID.ToString(Warfare.Data.Locale), flags);
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
                if (format.Equals(UCPlayer.CHARACTER_NAME_FORMAT, StringComparison.Ordinal)   || format.Equals(UCPlayer.COLORIZED_CHARACTER_NAME_FORMAT, StringComparison.Ordinal))
                    return player.characterName;
                else if (format.Equals(UCPlayer.NICK_NAME_FORMAT, StringComparison.Ordinal)   || format.Equals(UCPlayer.COLORIZED_NICK_NAME_FORMAT, StringComparison.Ordinal))
                    return player.nickName;
                else if (format.Equals(UCPlayer.PLAYER_NAME_FORMAT, StringComparison.Ordinal) || format.Equals(UCPlayer.COLORIZED_PLAYER_NAME_FORMAT, StringComparison.Ordinal))
                    return player.playerName;
                else if (format.Equals(UCPlayer.STEAM_64_FORMAT, StringComparison.Ordinal)    || format.Equals(UCPlayer.COLORIZED_STEAM_64_FORMAT, StringComparison.Ordinal))
                    return player.steamID.m_SteamID.ToString(Warfare.Data.Locale);
            }
            return player.characterName;
        }
        static ToStringHelperClass()
        {
            DynamicMethod dm;
            ILGenerator il;
            Type t = typeof(T);
            if (typeof(string).IsAssignableFrom(t))
            {
                type = 0;
                return;
            }
            if (t.IsEnum)
            {
                type = 13;
                return;
            }
            if (typeof(ITranslationArgument).IsAssignableFrom(t))
            {
                type = 4;
                return;
            }
            if (typeof(PlayerCaller).IsAssignableFrom(t))
            {
                type = 8;
                return;
            }
            if (typeof(Player).IsAssignableFrom(t))
            {
                type = 9;
                return;
            }
            if (typeof(SteamPlayer).IsAssignableFrom(t))
            {
                type = 10;
                return;
            }
            if (typeof(SteamPlayerID).IsAssignableFrom(t))
            {
                type = 11;
                return;
            }
            if (typeof(Asset).IsAssignableFrom(t))
            {
                type = 14;
                return;
            }
            if (typeof(BarricadeData).IsAssignableFrom(t))
            {
                type = 16;
                return;
            }
            if (typeof(StructureData).IsAssignableFrom(t))
            {
                type = 17;
                return;
            }
            if (typeof(Guid).IsAssignableFrom(t))
            {
                type = 18;
                return;
            }
            PropertyInfo info1 = t
                .GetProperties(BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .FirstOrDefault(x => x.Name.IndexOf("asset", StringComparison.OrdinalIgnoreCase) != -1
                                     && typeof(Asset).IsAssignableFrom(x.PropertyType)
                                     && x.GetGetMethod(true) != null
                                     && Attribute.GetCustomAttribute(x, typeof(ObsoleteAttribute)) is null);

            if (info1 == null)
            {
                FieldInfo info2 = t
                    .GetFields(BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .FirstOrDefault(x => x.Name.IndexOf("asset", StringComparison.OrdinalIgnoreCase) != -1
                                         && typeof(Asset).IsAssignableFrom(x.FieldType)
                                         && Attribute.GetCustomAttribute(x, typeof(ObsoleteAttribute)) is null);

                if (info2 != null)
                {
                    dm = new DynamicMethod("GetAssetName",
                        MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.Final,
                        CallingConventions.Standard, typeof(string), new Type[] { t }, t,
                        true);
                    dm.DefineParameter(1, ParameterAttributes.None, "value");
                    il = dm.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, info2);
                    il.EmitCall(OpCodes.Callvirt, typeof(Asset).GetProperty(nameof(Asset.FriendlyName), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(false), null);
                    il.Emit(OpCodes.Ret);
                    toStringFunc4 = (Func<T, string>)dm.CreateDelegate(typeof(Func<T, string>));
                    type = 15;
                    return;
                }
            }
            else
            {
                dm = new DynamicMethod("GetAssetName",
                    MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.Final,
                    CallingConventions.Standard, typeof(string), new Type[] { t }, t,
                    true);
                dm.DefineParameter(1, ParameterAttributes.None, "value");
                il = dm.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.EmitCall(OpCodes.Callvirt, info1.GetGetMethod(true), null);
                il.EmitCall(OpCodes.Callvirt, typeof(Asset).GetProperty(nameof(Asset.FriendlyName), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(), null);
                il.Emit(OpCodes.Ret);
                toStringFunc4 = (Func<T, string>)dm.CreateDelegate(typeof(Func<T, string>));
                type = 15;
                return;
            }
            if (typeof(UnityEngine.Object).IsAssignableFrom(t))
            {
                type = 5;
                return;
            }
            if (t == typeof(Color))
            {
                type = 6;
                return;
            }

            if (t == typeof(CSteamID))
            {
                type = 7;
                return;
            }
            if (typeof(Type).IsAssignableFrom(t))
            {
                type = 12;
                return;
            }
            MethodInfo? info = t.GetMethod("ToString", BindingFlags.Instance | BindingFlags.Public, null, tarr1, null);
            if (info == null || Attribute.GetCustomAttribute(info, typeof(ObsoleteAttribute)) is not null)
            {
                info = t.GetMethod("ToString", BindingFlags.Instance | BindingFlags.Public, null, tarr2, null);
                if (info == null || Attribute.GetCustomAttribute(info, typeof(ObsoleteAttribute)) is not null)
                {
                    info = t.GetMethod("ToString", BindingFlags.Instance | BindingFlags.Public, null, tarr3, null);
                    if (info == null || Attribute.GetCustomAttribute(info, typeof(ObsoleteAttribute)) is not null)
                    {
                        type = 0;
                    }
                    else
                    {
                        type = 3;
                        dm = new DynamicMethod("ToStringHelper",
                            MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard,
                            typeof(string), new Type[] { t, typeof(IFormatProvider) }, typeof(ToStringHelperClass<T>),
                            true);
                        dm.DefineParameter(1, ParameterAttributes.None, "value");
                        dm.DefineParameter(2, ParameterAttributes.None, "provider");
                        il = dm.GetILGenerator();
                        if (typeof(T).IsValueType)
                            il.Emit(OpCodes.Ldarga_S, 0);
                        else
                            il.Emit(OpCodes.Ldarg_0, 0);
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Callvirt, info);
                        il.Emit(OpCodes.Ret);
                        toStringFunc3 = (Func<T, IFormatProvider, string>)dm.CreateDelegate(typeof(Func<T, IFormatProvider, string>));
                    }
                }
                else
                {
                    type = 2;
                    dm = new DynamicMethod("ToStringHelper",
                        MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(string), new Type[] { t, typeof(string) }, typeof(ToStringHelperClass<T>),
                        true);
                    dm.DefineParameter(1, ParameterAttributes.None, "value");
                    dm.DefineParameter(2, ParameterAttributes.None, "format");
                    il = dm.GetILGenerator();
                    if (typeof(T).IsValueType)
                        il.Emit(OpCodes.Ldarga_S, 0);
                    else
                        il.Emit(OpCodes.Ldarg_0, 0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Callvirt, info);
                    il.Emit(OpCodes.Ret);
                    toStringFunc2 = (Func<T, string, string>)dm.CreateDelegate(typeof(Func<T, string, string>));
                }
            }
            else
            {
                type = 1;
                dm = new DynamicMethod("ToStringHelper",
                    MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(string), new Type[] { t, typeof(string), typeof(IFormatProvider) }, typeof(ToStringHelperClass<T>),
                    true);
                dm.DefineParameter(1, ParameterAttributes.None, "value");
                dm.DefineParameter(2, ParameterAttributes.None, "format");
                dm.DefineParameter(3, ParameterAttributes.None, "provider");
                il = dm.GetILGenerator();
                if (typeof(T).IsValueType)
                    il.Emit(OpCodes.Ldarga_S, 0);
                else
                    il.Emit(OpCodes.Ldarg_0, 0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Callvirt, info);
                il.Emit(OpCodes.Ret);
                toStringFunc1 = (Func<T, string, IFormatProvider, string>)dm.CreateDelegate(typeof(Func<T, string, IFormatProvider, string>));
            }
        }
    }
    protected struct TranslationValue
    {
        public static TranslationValue Nil = new TranslationValue();
        public readonly string Language;
        public readonly string Original;
        public readonly string ProcessedInner;
        public readonly string Processed;
        public readonly Color  Color;
        public readonly bool rt;
        private string? _console;
        public string Console => rt ? (_console ??= F.RemoveRichText(ProcessedInner)) : Original;
        public ConsoleColor ConsoleColor => F.GetClosestConsoleColor(Color);
        public bool IsNil => Original is null;
        public TranslationValue(string language, string original, TranslationFlags flags)
        {
            rt = (flags & TranslationFlags.NoRichText) == 0;
            Original = original;
            Processed = original;
            Color = ProcessValue(ref Processed, out ProcessedInner, flags);
            Language = language;
            _console = null;
        }
        public TranslationValue(string language, string original, string processedInner, string processed, Color color)
        {
            Language = language;
            Original = original;
            ProcessedInner = processedInner;
            Processed = processed;
            Color = color;
            _console = null;
        }
        public TranslationValue(in TranslationValue value, TranslationFlags flags) : this (value.Language, value.Original, flags) { }
    }
    protected static TranslationFlags CheckPlurality(string? format, TranslationFlags flags)
    {
        if (format is not null && format.EndsWith(T.PLURAL, StringComparison.Ordinal))
            return flags | TranslationFlags.Plural;
        return flags;
    }
    public static string Pluralize(string word, string language, TranslationFlags flags)
    {
        if ((flags & TranslationFlags.NoPlural) == TranslationFlags.NoPlural || word.Length < 3)
            return word;
        bool isAllCaps = true;
        bool isPCaps = char.IsUpper(word[0]);
        for (int i = word.Length - 1; i >= 0; --i)
        {
            if (char.IsLower(word[i]))
            {
                isAllCaps = false;
                break;
            }
        }
        string str = word.ToLowerInvariant();
        char last = str[str.Length - 1];
        char slast = str[str.Length - 2];
        if (last is 's' or 'x' or 'z' || (last is 'h' && slast is 's' or 'c'))
            return word + (isAllCaps ? "ES" : "es");

        if (str.Equals("roof", StringComparison.OrdinalIgnoreCase) ||
            str.Equals("belief", StringComparison.OrdinalIgnoreCase) ||
            str.Equals("chef", StringComparison.OrdinalIgnoreCase) ||
            str.Equals("chief", StringComparison.OrdinalIgnoreCase)
            )
        goto s;
        if (last is 'f')
            return word.Substring(0, str.Length - 1) + (isAllCaps ? "VES" : "ves");

        if (last is 'e' && slast is 'f')
            return word.Substring(0, str.Length - 2) + (isAllCaps ? "VES" : "ves");

        if (last is 'y')
            if (!(slast is 'a' or 'e' or 'i' or 'o' or 'u'))
                return word.Substring(0, str.Length - 1) + (isAllCaps ? "IES" : "ies");
            else
                return word + (isAllCaps ? "S" : "s");

        if (str.Equals("photo", StringComparison.OrdinalIgnoreCase) ||
            str.Equals("piano", StringComparison.OrdinalIgnoreCase) ||
            str.Equals("halo", StringComparison.OrdinalIgnoreCase) ||
            str.Equals("volcano", StringComparison.OrdinalIgnoreCase)
           )
            goto s;

        if (last is 'o')
            return word + (isAllCaps ? "ES" : "es");

        if (last is 's' && slast is 'u')
            return word.Substring(0, word.Length - 2) + (isAllCaps ? "I" : "i");

        if (last is 's' && slast is 'i')
            return word.Substring(0, word.Length - 2) + (isAllCaps ? "ES" : "es");

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

        if (str.Equals("child", StringComparison.OrdinalIgnoreCase))
            return word + (isAllCaps ? "REN" : "ren");
        if (str.Equals("goose", StringComparison.OrdinalIgnoreCase))
            return isAllCaps ? "GEESE" : isPCaps ? "Geese" : "geese";
        if (str.Equals("man", StringComparison.OrdinalIgnoreCase))
            return isAllCaps ? "MEN" : isPCaps ? "Men" : "men";
        if (str.Equals("woman", StringComparison.OrdinalIgnoreCase))
            return isAllCaps ? "WOMEN" : isPCaps ? "Women" : "women";
        if (str.Equals("tooth", StringComparison.OrdinalIgnoreCase))
            return isAllCaps ? "TEETH" : isPCaps ? "Teeth" : "teeth";
        if (str.Equals("foot", StringComparison.OrdinalIgnoreCase))
            return isAllCaps ? "FEET" : isPCaps ? "Feet" : "feet";
        if (str.Equals("mouse", StringComparison.OrdinalIgnoreCase))
            return isAllCaps ? "MICE" : isPCaps ? "Mice" : "mice";
        if (str.Equals("die", StringComparison.OrdinalIgnoreCase))
            return isAllCaps ? "DICE" : isPCaps ? "Dice" : "dice";
        if (str.Equals("person", StringComparison.OrdinalIgnoreCase))
            return isAllCaps ? "PEOPLE" : isPCaps ? "People" : "people";
        if (str.Equals("axis", StringComparison.OrdinalIgnoreCase))
            return isAllCaps ? "AXES" : isPCaps ? "Axes" : "axes";

    s:
        return word + (isAllCaps ? "S" : "s");
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
            if (nindex - index is > 10)
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
        value = F.RemoveTMProRichText(inp);
    }
    public static unsafe void ReplaceColors(ref string message)
    {
        int index = -2;
        while (true)
        {
            index = message.IndexOf("c$", index + 2, StringComparison.OrdinalIgnoreCase);
            if (index == -1 || index >= message.Length - 2) break;
            char next = message[index + 1];
            if (next is '$') continue;
            int nindex = message.IndexOf('$', index);
            fixed (char* ptr = message)
            {
                string str = new string(ptr, index + 2, nindex - index + 1);
                str = UCWarfare.GetColorHex(str);
                if (index > 0 && message[index - 1] != '#')
                    str = "#" + str;
                message = new string(ptr, 0, index) + str + new string(ptr, nindex + 1, message.Length - nindex);
            }
            index = nindex;
        }
    }
    public static unsafe Color ProcessValue(ref string message, out string innerText, TranslationFlags flags)
    {
        ReplaceColors(ref message);
        Color color;

        if ((flags & TranslationFlags.NoRichText) == 0 && (flags & TranslationFlags.ReplaceTMProRichText) == TranslationFlags.ReplaceTMProRichText)
            ReplaceTMProRichText(ref message, flags);

        if ((flags & TranslationFlags.NoColor) == TranslationFlags.NoColor)
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
            color = F.Hex(clr);
            goto next;
        }
        // <color=#ffffff>
        else if (message.Length > 8 && message.StartsWith("<color=#", StringComparison.OrdinalIgnoreCase) && message[8] != '{')
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
            color = F.Hex(clr);
            goto next;
        }
        
    noColor:
        color = UCWarfare.GetColor("default");
        innerText = message;
    next: return color; // todo
    }
    protected TranslationFlags GetFlags(ulong targetTeam) => targetTeam switch
    {
        3 => Flags | TranslationFlags.Team3,
        2 => Flags | TranslationFlags.Team2,
        1 => Flags | TranslationFlags.Team1,
        _ => Flags
    };
    private static string Null(TranslationFlags flags) => 
        ((flags & TranslationFlags.NoRichText) == TranslationFlags.NoRichText)
            ? NULL_NO_CLR
            : (((flags & TranslationFlags.TranslateWithUnityRichText) == TranslationFlags.TranslateWithUnityRichText)
                ? NULL_CLR_2
                : NULL_CLR_1);
    public virtual string Translate(string? language)
    {
        if (language is null)
            return DefaultData.Processed;
        if (language.Length == 0 || language.Equals("0", StringComparison.Ordinal))
            language = JSONMethods.DEFAULT_LANGUAGE;

        ref TranslationValue data = ref this[language];
        return data.Processed;
    }
    public virtual string Translate(string? language, out Color color)
    {
        if (language is null)
        {
            color = DefaultData.Color;
            return DefaultData.Processed;
        }
        if (language.Length == 0 || language.Equals("0", StringComparison.Ordinal))
            language = JSONMethods.DEFAULT_LANGUAGE;

        ref TranslationValue data = ref this[language];
        color = data.Color;
        return data.ProcessedInner;
    }
    internal static void OnColorsReloaded()
    {
        for (int i = 0; i < T.Translations.Length; ++i)
            T.Translations[i].RefreshColors();
    }
    private static bool _first = true;
    internal static void ReadTranslations()
    {
        L.Log("Detected " + T.Translations.Length + " translations.", ConsoleColor.Magenta);
        DateTime start = DateTime.Now;
        if (!_first)
        {
            for (int i = 0; i < T.Translations.Length; ++i)
                T.Translations[i].ClearTranslations();
        }
        else _first = false;
        DirectoryInfo[] dirs = new DirectoryInfo(Warfare.Data.Paths.LangStorage).GetDirectories();
        bool defRead = false;
        int amt = 0;
        for (int i = 0; i < dirs.Length; ++i)
        {
            DirectoryInfo langFolder = dirs[i];

            FileInfo info = new FileInfo(Path.Combine(langFolder.FullName, LOCAL_FILE_NAME));
            if (!info.Exists) continue;
            string lang = langFolder.Name;
            if (lang.Equals(JSONMethods.DEFAULT_LANGUAGE, StringComparison.OrdinalIgnoreCase))
            {
                lang = JSONMethods.DEFAULT_LANGUAGE;
                defRead = true;
            }
            else
            {
                foreach (LanguageAliasSet set in Warfare.Data.LanguageAliases.Values)
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
                string line;
                string? multiline = null;
                while (true)
                {
                    line = reader.ReadLine();
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
                        if (t.Key.Equals(key, StringComparison.OrdinalIgnoreCase) || (t.attr is not null && t.attr.LegacyTranslationId is not null && t.attr.LegacyTranslationId.Equals(key, StringComparison.OrdinalIgnoreCase)))
                        {
                            string value = line.Substring(ind2 + 1, line.Length - ind2 - 1).Replace(@"\\", @"\").Replace(@"\n", "\n").Replace(@"\r", "\r").Replace(@"\t", "\t");
                            ++amt;
                            t.AddTranslation(langFolder.Name, value);
                            goto n;
                        }
                    }
                    L.LogWarning("[TRANSLATIONS] Unknown translation key: " + key + " in " + lang + " translation file.");
                    n: ;
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
            if (t.Data is not null && t.Data.Length > 0)
            {
                for (int i = 0; i < t.Data.Length; ++i)
                {
                    ref TranslationValue v = ref t.Data[i];
                    if (v.Language.Equals(JSONMethods.DEFAULT_LANGUAGE, StringComparison.Ordinal))
                        goto c;
                }
            }
            ++amt;
            t.AddTranslation(JSONMethods.DEFAULT_LANGUAGE, t.DefaultData.Original);
        c:;
        }
        if (amt > 0 && defRead)
            L.Log("Added " + amt + " missing default translations for " + JSONMethods.DEFAULT_LANGUAGE + ".", ConsoleColor.Yellow);
        L.Log("Loaded translations in " + (DateTime.Now - start).TotalMilliseconds.ToString("F1", Warfare.Data.Locale) + "ms", ConsoleColor.Magenta);
    }
    private static void WriteDefaultTranslations()
    {
        DirectoryInfo dir = new DirectoryInfo(Path.Combine(Warfare.Data.Paths.LangStorage, JSONMethods.DEFAULT_LANGUAGE));
        if (!dir.Exists)
            dir.Create();
        FileInfo info = new FileInfo(Path.Combine(dir.FullName, LOCAL_FILE_NAME));
        using (FileStream str = new FileStream(info.FullName, FileMode.Create, FileAccess.Write, FileShare.Read))
        using (StreamWriter writer = new StreamWriter(str))
        {
            string? lastSection = null;
            foreach (Translation t in T.Translations.OrderBy(x => x.attr?.Section ?? "~", StringComparer.OrdinalIgnoreCase))
            {
                WriteTranslation(writer, t, t.DefaultData.Original, ref lastSection);
            }

            writer.Flush();
        }
    }
    private static void WriteLanguage(string language)
    {
        DirectoryInfo dir = new DirectoryInfo(Path.Combine(Warfare.Data.Paths.LangStorage, language));
        if (!dir.Exists)
            dir.Create();
        FileInfo info = new FileInfo(Path.Combine(dir.FullName, LOCAL_FILE_NAME));
        using (FileStream str = new FileStream(info.FullName, FileMode.Create, FileAccess.Write, FileShare.Read))
        using (StreamWriter writer = new StreamWriter(str))
        {
            string? lastSection = null;
            foreach (Translation t in T.Translations.OrderBy(x => x.attr?.Section ?? "~", StringComparer.OrdinalIgnoreCase))
            {
                if (t.Data is null) continue;
                string? val = null;
                for (int i = 0; i < t.Data.Length; ++i)
                {
                    ref TranslationValue v = ref t.Data[i];
                    if (v.Language.Equals(language, StringComparison.Ordinal))
                        val = v.Original;
                }
                if (val is not null)
                    WriteTranslation(writer, t, val, ref lastSection);
            }

            writer.Flush();
        }
    }
    private static void WriteTranslation(StreamWriter writer, Translation t, string val, ref string? lastSection)
    {
        val = val.Replace(@"\", @"\\").Replace("\n", @"\n").Replace("\r", @"\r").Replace("\t", @"\t");
        if (string.IsNullOrEmpty(val)) return;
        string? sect = t.attr?.Section;
        if (sect != lastSection)
        {
            lastSection = sect;
            if (writer.BaseStream.Position > 0)
                writer.WriteLine();
            if (sect is not null)
                writer.WriteLine("! " + sect + " !");
        }
        writer.WriteLine();
        if (t.attr is not null)
        {
            if (t.attr.Description is not null)
                writer.WriteLine("# Description: " + t.attr.Description.RemoveMany(true, '\r', '\n'));
        }
        Type[] gen = t.GetType().GetGenericArguments();
        if (gen.Length > 0)
            writer.WriteLine("# Formatting Arguments:");
        for (int i = 0; i < gen.Length; ++i)
        {
            string fmt = "#  " + "{" + i + "} - [" + ToString(gen[i], JSONMethods.DEFAULT_LANGUAGE, null, null, TranslationFlags.NoColor) + "]";

            if (t.attr is not null && t.attr.FormattingDescriptions is not null && i < t.attr.FormattingDescriptions.Length && !string.IsNullOrEmpty(t.attr.FormattingDescriptions[i]))
                fmt += " " + t.attr.FormattingDescriptions[i].RemoveMany(true, '\r', '\n');
            writer.WriteLine(fmt);
        }

        writer.Write(t.Key);
        writer.Write(": ");
        if (val[0] == ' ')
            val = @"\" + val;
        writer.Write(val);
    }
    public static Translation? FromLegacyId(string legacyId)
    {
        for (int i = 0; i < T.Translations.Length; ++i)
        {
            Translation t = T.Translations[i];
            if (t.attr is not null && t.attr.LegacyTranslationId is not null && t.attr.LegacyTranslationId.Equals(legacyId, StringComparison.Ordinal))
                return t;
        }

        return null;
    }
}

[Flags]
public enum TranslationFlags
{
    None = 0,
    /// <summary>Tells the translator not to search for translations in other languages if not found in the current language, and instead just return the field name.
    /// <para>If the current language isn't <see cref="JSONMethods.DEFAULT_LANGUAGE"/>, a default will not be chosen.</para></summary>
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
    NoColor = 32,
    /// <summary>Tells the translator to translate the messsage for each player when broadcasted.</summary>
    PerPlayerTranslation = 64,
    /// <summary>Don't use this in a constructor, used to tell translator functions that the translation is for team 1.</summary>
    Team1 = 256,
    /// <summary>Don't use this in a constructor, used to tell translator functions that the translation is for team 2.</summary>
    Team2 = 512,
    /// <summary>Don't use this in a constructor, used to tell translator functions that the translation is for team 3.</summary>
    Team3 = 1024,
    /// <summary>Tells <see cref="ChatManager"/> to send chat messages with RichText set to false.</summary>
    NoRichText = 2048,
    /// <summary>Use for translations to be used on TMPro UI. Skips color scanning.</summary>
    TMProUI = NoColor,
    /// <summary>Use for translations to be used on non-TMPro UI. Skips color optimization and convert to &lt;color=#ffffff&gt; format.</summary>
    UnityUI = NoColor | UseUnityRichText,
    /// <summary>Use for translations to be used on non-TMPro UI. Skips color optimization and convert to &lt;color=#ffffff&gt; format, doesn't replace already existing TMPro tags.</summary>
    UnityUINoReplace = NoColor | TranslateWithUnityRichText,
    /// <summary>Tells the translator to format the term plurally, this will be automatically applied to individual arguments if the format is <see cref="T.PLURAL"/>.</summary>
    Plural,
    /// <summary>Tells the translator to not try to turn arguments plural.</summary>
    NoPlural,
    /// <summary>Don't use this in a constructor, used to tell translator functions that the translation is going to be sent in chat.</summary>
    ForChat
}

public interface ITranslationArgument
{
    string Translate(string language, string? format, UCPlayer? target, ref TranslationFlags flags);
}
public sealed class Translation<T> : Translation
{
    private readonly string? _arg0Fmt;
    public Translation(string @default) : base(@default) { }
    public Translation(string @default, TranslationFlags flags) : base(@default, flags) { }
    public Translation(string @default, TranslationFlags flags, string arg1Fmt) : base(@default, flags)
    {
        _arg0Fmt = arg1Fmt;
    }
    public Translation(string @default, string arg1Fmt) : base(@default)
    {
        _arg0Fmt = arg1Fmt;
    }
    public string Translate(string value, string language, T arg, UCPlayer? target, ulong targetTeam, TranslationFlags flags)
    {
        flags |= GetFlags(targetTeam) | Flags;
        try
        {
            return string.Format(value, ToString(arg, language, _arg0Fmt, target, CheckPlurality(_arg0Fmt, flags)));
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
}
public sealed class Translation<T1, T2> : Translation
{
    private readonly string? _arg0Fmt;
    private readonly string? _arg1Fmt;
    public Translation(string @default) : base(@default) { }
    public Translation(string @default, TranslationFlags flags) : base(@default, flags) { }
    public Translation(string @default, TranslationFlags flags, string? arg1Fmt = null, string? arg2Fmt = null) : base(@default, flags)
    {
        _arg0Fmt = arg1Fmt;
        _arg1Fmt = arg2Fmt;
    }
    public Translation(string @default, string? arg1Fmt = null, string? arg2Fmt = null) : base(@default)
    {
        _arg0Fmt = arg1Fmt;
        _arg1Fmt = arg2Fmt;
    }
    public string Translate(string value, string language, T1 arg1, T2 arg2, UCPlayer? target, ulong targetTeam, TranslationFlags flags)
    {
        flags |= GetFlags(targetTeam) | Flags;
        try
        {
            return string.Format(value, ToString(arg1, language, _arg0Fmt, target, CheckPlurality(_arg0Fmt, flags)),
                ToString(arg2, language, _arg1Fmt, target, CheckPlurality(_arg1Fmt, flags)));
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
}
public sealed class Translation<T1, T2, T3> : Translation
{
    private readonly string? _arg0Fmt;
    private readonly string? _arg1Fmt;
    private readonly string? _arg2Fmt;
    public Translation(string @default) : base(@default) { }
    public Translation(string @default, TranslationFlags flags) : base(@default, flags) { }
    public Translation(string @default, TranslationFlags flags, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null) : base(@default, flags)
    {
        _arg0Fmt = arg1Fmt;
        _arg1Fmt = arg2Fmt;
        _arg2Fmt = arg3Fmt;
    }
    public Translation(string @default, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null) : base(@default)
    {
        _arg0Fmt = arg1Fmt;
        _arg1Fmt = arg2Fmt;
        _arg2Fmt = arg3Fmt;
    }
    public string Translate(string value, string language, T1 arg1, T2 arg2, T3 arg3, UCPlayer? target, ulong targetTeam, TranslationFlags flags)
    {
        flags |= GetFlags(targetTeam) | Flags;
        try
        {
            return string.Format(value, ToString(arg1, language, _arg0Fmt, target, CheckPlurality(_arg0Fmt, flags)),
                ToString(arg2, language, _arg1Fmt, target, CheckPlurality(_arg1Fmt, flags)),
                ToString(arg3, language, _arg2Fmt, target, CheckPlurality(_arg2Fmt, flags)));
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
}
public sealed class Translation<T1, T2, T3, T4> : Translation
{
    private readonly string? _arg0Fmt;
    private readonly string? _arg1Fmt;
    private readonly string? _arg2Fmt;
    private readonly string? _arg3Fmt;
    public Translation(string @default) : base(@default) { }
    public Translation(string @default, TranslationFlags flags) : base(@default, flags) { }
    public Translation(string @default, TranslationFlags flags, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null, string? arg4Fmt = null) : base(@default, flags)
    {
        _arg0Fmt = arg1Fmt;
        _arg1Fmt = arg2Fmt;
        _arg2Fmt = arg3Fmt;
        _arg3Fmt = arg4Fmt;
    }
    public Translation(string @default, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null, string? arg4Fmt = null) : base(@default)
    {
        _arg0Fmt = arg1Fmt;
        _arg1Fmt = arg2Fmt;
        _arg2Fmt = arg3Fmt;
        _arg3Fmt = arg4Fmt;
    }
    public string Translate(string value, string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, UCPlayer? target, ulong targetTeam, TranslationFlags flags)
    {
        flags |= GetFlags(targetTeam) | Flags;
        try
        {
            return string.Format(value, ToString(arg1, language, _arg0Fmt, target, CheckPlurality(_arg0Fmt, flags)),
                ToString(arg2, language, _arg1Fmt, target, CheckPlurality(_arg1Fmt, flags)),
                ToString(arg3, language, _arg2Fmt, target, CheckPlurality(_arg2Fmt, flags)),
                ToString(arg4, language, _arg3Fmt, target, CheckPlurality(_arg3Fmt, flags)));
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
}
public sealed class Translation<T1, T2, T3, T4, T5> : Translation
{
    private readonly string? _arg0Fmt;
    private readonly string? _arg1Fmt;
    private readonly string? _arg2Fmt;
    private readonly string? _arg3Fmt;
    private readonly string? _arg4Fmt;
    public Translation(string @default) : base(@default) { }
    public Translation(string @default, TranslationFlags flags) : base(@default, flags) { }
    public Translation(string @default, TranslationFlags flags, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null, string? arg4Fmt = null, string? arg5Fmt = null) : base(@default, flags)
    {
        _arg0Fmt = arg1Fmt;
        _arg1Fmt = arg2Fmt;
        _arg2Fmt = arg3Fmt;
        _arg3Fmt = arg4Fmt;
        _arg4Fmt = arg5Fmt;
    }
    public Translation(string @default, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null, string? arg4Fmt = null, string? arg5Fmt = null) : base(@default)
    {
        _arg0Fmt = arg1Fmt;
        _arg1Fmt = arg2Fmt;
        _arg2Fmt = arg3Fmt;
        _arg3Fmt = arg4Fmt;
        _arg4Fmt = arg5Fmt;
    }
    public string Translate(string value, string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, UCPlayer? target, ulong targetTeam, TranslationFlags flags)
    {
        flags |= GetFlags(targetTeam) | Flags;
        try
        {
            return string.Format(value, ToString(arg1, language, _arg0Fmt, target, CheckPlurality(_arg0Fmt, flags)),
                ToString(arg2, language, _arg1Fmt, target, CheckPlurality(_arg1Fmt, flags)),
                ToString(arg3, language, _arg2Fmt, target, CheckPlurality(_arg2Fmt, flags)),
                ToString(arg4, language, _arg3Fmt, target, CheckPlurality(_arg3Fmt, flags)),
                ToString(arg5, language, _arg4Fmt, target, CheckPlurality(_arg4Fmt, flags)));
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
}
public sealed class Translation<T1, T2, T3, T4, T5, T6> : Translation
{
    private readonly string? _arg0Fmt;
    private readonly string? _arg1Fmt;
    private readonly string? _arg2Fmt;
    private readonly string? _arg3Fmt;
    private readonly string? _arg4Fmt;
    private readonly string? _arg5Fmt;
    public Translation(string @default) : base(@default) { }
    public Translation(string @default, TranslationFlags flags) : base(@default, flags) { }
    public Translation(string @default, TranslationFlags flags, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null, string? arg4Fmt = null, string? arg5Fmt = null, string? arg6Fmt = null) : base(@default, flags)
    {
        _arg0Fmt = arg1Fmt;
        _arg1Fmt = arg2Fmt;
        _arg2Fmt = arg3Fmt;
        _arg3Fmt = arg4Fmt;
        _arg4Fmt = arg5Fmt;
        _arg5Fmt = arg6Fmt;
    }
    public Translation(string @default, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null, string? arg4Fmt = null, string? arg5Fmt = null, string? arg6Fmt = null) : base(@default)
    {
        _arg0Fmt = arg1Fmt;
        _arg1Fmt = arg2Fmt;
        _arg2Fmt = arg3Fmt;
        _arg3Fmt = arg4Fmt;
        _arg4Fmt = arg5Fmt;
        _arg5Fmt = arg6Fmt;
    }
    public string Translate(string value, string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, UCPlayer? target, ulong targetTeam, TranslationFlags flags)
    {
        flags |= GetFlags(targetTeam) | Flags;
        try
        {
            return string.Format(value, ToString(arg1, language, _arg0Fmt, target, CheckPlurality(_arg0Fmt, flags)),
                ToString(arg2, language, _arg1Fmt, target, CheckPlurality(_arg1Fmt, flags)),
                ToString(arg3, language, _arg2Fmt, target, CheckPlurality(_arg2Fmt, flags)),
                ToString(arg4, language, _arg3Fmt, target, CheckPlurality(_arg3Fmt, flags)),
                ToString(arg5, language, _arg4Fmt, target, CheckPlurality(_arg4Fmt, flags)),
                ToString(arg6, language, _arg5Fmt, target, CheckPlurality(_arg5Fmt, flags)));
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
    public Translation(string @default) : base(@default) { }
    public Translation(string @default, TranslationFlags flags) : base(@default, flags) { }
    public Translation(string @default, TranslationFlags flags, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null, string? arg4Fmt = null, string? arg5Fmt = null, string? arg6Fmt = null, string? arg7Fmt = null) : base(@default, flags)
    {
        _arg0Fmt = arg1Fmt;
        _arg1Fmt = arg2Fmt;
        _arg2Fmt = arg3Fmt;
        _arg3Fmt = arg4Fmt;
        _arg4Fmt = arg5Fmt;
        _arg5Fmt = arg6Fmt;
        _arg6Fmt = arg7Fmt;
    }
    public Translation(string @default, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null, string? arg4Fmt = null, string? arg5Fmt = null, string? arg6Fmt = null, string? arg7Fmt = null) : base(@default)
    {
        _arg0Fmt = arg1Fmt;
        _arg1Fmt = arg2Fmt;
        _arg2Fmt = arg3Fmt;
        _arg3Fmt = arg4Fmt;
        _arg4Fmt = arg5Fmt;
        _arg5Fmt = arg6Fmt;
        _arg6Fmt = arg7Fmt;
    }
    public string Translate(string value, string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, UCPlayer? target, ulong targetTeam, TranslationFlags flags)
    {
        flags |= GetFlags(targetTeam) | Flags;
        try
        {
            return string.Format(value, ToString(arg1, language, _arg0Fmt, target, CheckPlurality(_arg0Fmt, flags)),
                ToString(arg2, language, _arg1Fmt, target, CheckPlurality(_arg1Fmt, flags)),
                ToString(arg3, language, _arg2Fmt, target, CheckPlurality(_arg2Fmt, flags)),
                ToString(arg4, language, _arg3Fmt, target, CheckPlurality(_arg3Fmt, flags)),
                ToString(arg5, language, _arg4Fmt, target, CheckPlurality(_arg4Fmt, flags)),
                ToString(arg6, language, _arg5Fmt, target, CheckPlurality(_arg5Fmt, flags)),
                ToString(arg7, language, _arg6Fmt, target, CheckPlurality(_arg6Fmt, flags)));
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
    public Translation(string @default) : base(@default) { }
    public Translation(string @default, TranslationFlags flags) : base(@default, flags) { }
    public Translation(string @default, TranslationFlags flags, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null, string? arg4Fmt = null, string? arg5Fmt = null, string? arg6Fmt = null, string? arg7Fmt = null, string? arg8Fmt = null) : base(@default, flags)
    {
        _arg0Fmt = arg1Fmt;
        _arg1Fmt = arg2Fmt;
        _arg2Fmt = arg3Fmt;
        _arg3Fmt = arg4Fmt;
        _arg4Fmt = arg5Fmt;
        _arg5Fmt = arg6Fmt;
        _arg6Fmt = arg7Fmt;
        _arg7Fmt = arg8Fmt;
    }
    public Translation(string @default, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null, string? arg4Fmt = null, string? arg5Fmt = null, string? arg6Fmt = null, string? arg7Fmt = null, string? arg8Fmt = null) : base(@default)
    {
        _arg0Fmt = arg1Fmt;
        _arg1Fmt = arg2Fmt;
        _arg2Fmt = arg3Fmt;
        _arg3Fmt = arg4Fmt;
        _arg4Fmt = arg5Fmt;
        _arg5Fmt = arg6Fmt;
        _arg6Fmt = arg7Fmt;
        _arg7Fmt = arg8Fmt;
    }
    public string Translate(string value, string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, UCPlayer? target, ulong targetTeam, TranslationFlags flags)
    {
        flags |= GetFlags(targetTeam) | Flags;
        try
        {
            return string.Format(value, ToString(arg1, language, _arg0Fmt, target, CheckPlurality(_arg0Fmt, flags)),
                ToString(arg2, language, _arg1Fmt, target, CheckPlurality(_arg1Fmt, flags)),
                ToString(arg3, language, _arg2Fmt, target, CheckPlurality(_arg2Fmt, flags)),
                ToString(arg4, language, _arg3Fmt, target, CheckPlurality(_arg3Fmt, flags)),
                ToString(arg5, language, _arg4Fmt, target, CheckPlurality(_arg4Fmt, flags)),
                ToString(arg6, language, _arg5Fmt, target, CheckPlurality(_arg5Fmt, flags)),
                ToString(arg7, language, _arg6Fmt, target, CheckPlurality(_arg6Fmt, flags)),
                ToString(arg8, language, _arg7Fmt, target, CheckPlurality(_arg7Fmt, flags)));
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
    public Translation(string @default) : base(@default) { }
    public Translation(string @default, TranslationFlags flags) : base(@default, flags) { }
    public Translation(string @default, TranslationFlags flags, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null, string? arg4Fmt = null, string? arg5Fmt = null, string? arg6Fmt = null, string? arg7Fmt = null, string? arg8Fmt = null, string? arg9Fmt = null) : base(@default, flags)
    {
        _arg0Fmt = arg1Fmt;
        _arg1Fmt = arg2Fmt;
        _arg2Fmt = arg3Fmt;
        _arg3Fmt = arg4Fmt;
        _arg4Fmt = arg5Fmt;
        _arg5Fmt = arg6Fmt;
        _arg6Fmt = arg7Fmt;
        _arg7Fmt = arg8Fmt;
        _arg8Fmt = arg9Fmt;
    }
    public Translation(string @default, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null, string? arg4Fmt = null, string? arg5Fmt = null, string? arg6Fmt = null, string? arg7Fmt = null, string? arg8Fmt = null, string? arg9Fmt = null) : base(@default)
    {
        _arg0Fmt = arg1Fmt;
        _arg1Fmt = arg2Fmt;
        _arg2Fmt = arg3Fmt;
        _arg3Fmt = arg4Fmt;
        _arg4Fmt = arg5Fmt;
        _arg5Fmt = arg6Fmt;
        _arg6Fmt = arg7Fmt;
        _arg7Fmt = arg8Fmt;
        _arg8Fmt = arg9Fmt;
    }
    public string Translate(string value, string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, UCPlayer? target, ulong targetTeam, TranslationFlags flags)
    {
        flags |= GetFlags(targetTeam) | Flags;
        try
        {
            return string.Format(value, ToString(arg1, language, _arg0Fmt, target, CheckPlurality(_arg0Fmt, flags)),
                ToString(arg2, language, _arg1Fmt, target, CheckPlurality(_arg1Fmt, flags)),
                ToString(arg3, language, _arg2Fmt, target, CheckPlurality(_arg2Fmt, flags)),
                ToString(arg4, language, _arg3Fmt, target, CheckPlurality(_arg3Fmt, flags)),
                ToString(arg5, language, _arg4Fmt, target, CheckPlurality(_arg4Fmt, flags)),
                ToString(arg6, language, _arg5Fmt, target, CheckPlurality(_arg5Fmt, flags)),
                ToString(arg7, language, _arg6Fmt, target, CheckPlurality(_arg6Fmt, flags)),
                ToString(arg8, language, _arg7Fmt, target, CheckPlurality(_arg7Fmt, flags)),
                ToString(arg9, language, _arg8Fmt, target, CheckPlurality(_arg8Fmt, flags)));
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
    public Translation(string @default) : base(@default) { }
    public Translation(string @default, TranslationFlags flags) : base(@default, flags) { }
    public Translation(string @default, TranslationFlags flags, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null, string? arg4Fmt = null, string? arg5Fmt = null, string? arg6Fmt = null, string? arg7Fmt = null, string? arg8Fmt = null, string? arg9Fmt = null, string? arg10Fmt = null) : base(@default, flags)
    {
        _arg0Fmt = arg1Fmt;
        _arg1Fmt = arg2Fmt;
        _arg2Fmt = arg3Fmt;
        _arg3Fmt = arg4Fmt;
        _arg4Fmt = arg5Fmt;
        _arg5Fmt = arg6Fmt;
        _arg6Fmt = arg7Fmt;
        _arg7Fmt = arg8Fmt;
        _arg8Fmt = arg9Fmt;
        _arg9Fmt = arg10Fmt;
    }
    public Translation(string @default, string? arg1Fmt = null, string? arg2Fmt = null, string? arg3Fmt = null, string? arg4Fmt = null, string? arg5Fmt = null, string? arg6Fmt = null, string? arg7Fmt = null, string? arg8Fmt = null, string? arg9Fmt = null, string? arg10Fmt = null) : base(@default)
    {
        _arg0Fmt = arg1Fmt;
        _arg1Fmt = arg2Fmt;
        _arg2Fmt = arg3Fmt;
        _arg3Fmt = arg4Fmt;
        _arg4Fmt = arg5Fmt;
        _arg5Fmt = arg6Fmt;
        _arg6Fmt = arg7Fmt;
        _arg7Fmt = arg8Fmt;
        _arg8Fmt = arg9Fmt;
        _arg9Fmt = arg10Fmt;
    }
    public string Translate(string value, string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, UCPlayer? target, ulong targetTeam, TranslationFlags flags)
    {
        flags |= GetFlags(targetTeam) | Flags;
        try
        {
            return string.Format(value, ToString(arg1, language, _arg0Fmt, target, CheckPlurality(_arg0Fmt, flags)),
                ToString(arg2, language, _arg1Fmt, target, CheckPlurality(_arg1Fmt, flags)),
                ToString(arg3, language, _arg2Fmt, target, CheckPlurality(_arg2Fmt, flags)),
                ToString(arg4, language, _arg3Fmt, target, CheckPlurality(_arg3Fmt, flags)),
                ToString(arg5, language, _arg4Fmt, target, CheckPlurality(_arg4Fmt, flags)),
                ToString(arg6, language, _arg5Fmt, target, CheckPlurality(_arg5Fmt, flags)),
                ToString(arg7, language, _arg6Fmt, target, CheckPlurality(_arg6Fmt, flags)),
                ToString(arg8, language, _arg7Fmt, target, CheckPlurality(_arg7Fmt, flags)),
                ToString(arg9, language, _arg8Fmt, target, CheckPlurality(_arg8Fmt, flags)),
                ToString(arg10, language, _arg9Fmt, target, CheckPlurality(_arg9Fmt, flags)));
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
}

[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
sealed class TranslationDataAttribute : Attribute
{
    private string? _legacyTranslationID;
    private string? _description;
    private string? _section;
    private string[]? _formatArgs;
    public TranslationDataAttribute()
    {

    }
    public string? LegacyTranslationId { get => _legacyTranslationID; set => _legacyTranslationID = value; }
    public string? Description { get => _description; set => _description = value; }
    public string? Section { get => _section; set => _section = value; }
    public string[]? FormattingDescriptions { get => _formatArgs; set => _formatArgs = value; }
}