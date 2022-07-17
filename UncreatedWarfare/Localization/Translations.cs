using SDG.Unturned;
using Steamworks;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Uncreated.Warfare;

public class Translation
{
    private const string NULL = "<#569cd6><b>null</b></color>";
    private readonly TranslationFlags _flags;
    private TranslationValue DefaultData;
    private TranslationValue[] Data;
    public string Key;
    public int Id;
    public TranslationFlags Flags => _flags;
    protected string InvalidValue => "Translation Error - " + Key;
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
        for (int i = 0; i < Data.Length; ++i)
            Data[i] = new TranslationValue(in Data[i], Flags);
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
            for (int i = 0; i < Data.Length; ++i)
            {
                ref TranslationValue v = ref Data[i];
                if (v.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
                {
                    return ref v;
                }
            }
            return ref TranslationValue.Nil;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static string ToStringHelper<T>(T value, string language, string? format, UCPlayer? target, TranslationFlags flags) => ToStringHelperClass<T>.ToString(value, language, format, target, Warfare.Data.Locale, flags);

    private static readonly Type[] tarr1 = new Type[] { typeof(string), typeof(IFormatProvider) };
    private static readonly Type[] tarr2 = new Type[] { typeof(string) };
    private static readonly Type[] tarr3 = new Type[] { typeof(IFormatProvider) };

    private static class ToStringHelperClass<T>
    {
        private static readonly Func<string, IFormatProvider, string>? toStringFunc1;
        private static readonly Func<string, string>? toStringFunc2;
        private static readonly Func<IFormatProvider, string>? toStringFunc3;
        private static readonly int type;

        public static string ToString(T value, string language, string? format, UCPlayer? target, IFormatProvider locale, TranslationFlags flags)
        {
            if (value is null) return NULL;
            return type switch
            {
                1 => toStringFunc1!(format!, locale),
                2 => toStringFunc2!(format!),
                3 => toStringFunc3!(locale),
                4 => (value as ITranslationArgument)!.Translate(language, format, target, flags),
                5 => (value as UnityEngine.Object)!.name,
                6 => value is Color clr ? clr.Hex() : value.ToString(),
                7 => value is CSteamID id ? id.m_SteamID.ToString(format, locale) : value.ToString(),
                8 => PlayerToString((value as PlayerCaller)!.player, flags, format),
                9 => PlayerToString((value as Player)!, flags, format),
                10 => PlayerToString((value as SteamPlayer)!.player, flags, format),
                11 => PlayerToString((value as SteamPlayerID)!, flags, format),
                12 => value is Type t ? (t.IsEnum ? Localization.TranslateEnumName(t, language) : (t.IsArray ? (t.GetElementType().Name + " Array") : TypeToString(t))) : value.ToString(),
                13 => Localization.TranslateEnum(value, language),
                _ => value.ToString(),
            };
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
                return NULL;
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
            if (player is null) return NULL;
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
            Type t = typeof(T);
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
                        toStringFunc3 =
                            (Func<IFormatProvider, string>)info.CreateDelegate(typeof(Func<IFormatProvider, string>));
                    }
                }
                else
                {

                    type = 2;
                    toStringFunc2 = (Func<string, string>)info.CreateDelegate(typeof(Func<string, string>));
                }
            }
            else
            {
                type = 1;
                toStringFunc1 =
                    (Func<string, IFormatProvider, string>)info.CreateDelegate(
                        typeof(Func<string, IFormatProvider, string>));
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
    /// <summary>Tells the translator to translate the message for each team when broadcasted.</summary>
    PerTeamTranslation = 128,
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
    UnityUINoReplace = NoColor | TranslateWithUnityRichText
}

public interface ITranslationArgument
{
    string Translate(string language, string? format, UCPlayer? target, TranslationFlags flags);
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
    public string Translate(string value, string language, T arg, UCPlayer? target, ulong targetTeam)
    {
        TranslationFlags flags = GetFlags(targetTeam);
        try
        {
            return string.Format(value, ToStringHelper(arg, language, _arg0Fmt, target, flags));
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
    public string Translate(string value, string language, T1 arg1, T2 arg2, UCPlayer? target, ulong targetTeam)
    {
        TranslationFlags flags = GetFlags(targetTeam);
        try
        {
            return string.Format(value, ToStringHelper(arg1, language, _arg0Fmt, target, flags),
                ToStringHelper(arg2, language, _arg1Fmt, target, flags));
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
    public string Translate(string value, string language, T1 arg1, T2 arg2, T3 arg3, UCPlayer? target, ulong targetTeam)
    {
        TranslationFlags flags = GetFlags(targetTeam);
        try
        {
            return string.Format(value, ToStringHelper(arg1, language, _arg0Fmt, target, flags),
                ToStringHelper(arg2, language, _arg1Fmt, target, flags),
                ToStringHelper(arg3, language, _arg2Fmt, target, flags));
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
    public string Translate(string value, string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, UCPlayer? target, ulong targetTeam)
    {
        TranslationFlags flags = GetFlags(targetTeam);
        try
        {
            return string.Format(value, ToStringHelper(arg1, language, _arg0Fmt, target, flags),
                ToStringHelper(arg2, language, _arg1Fmt, target, flags),
                ToStringHelper(arg3, language, _arg2Fmt, target, flags),
                ToStringHelper(arg4, language, _arg3Fmt, target, flags));
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
    public string Translate(string value, string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, UCPlayer? target, ulong targetTeam)
    {
        TranslationFlags flags = GetFlags(targetTeam);
        try
        {
            return string.Format(value, ToStringHelper(arg1, language, _arg0Fmt, target, flags),
                ToStringHelper(arg2, language, _arg1Fmt, target, flags),
                ToStringHelper(arg3, language, _arg2Fmt, target, flags),
                ToStringHelper(arg4, language, _arg3Fmt, target, flags),
                ToStringHelper(arg5, language, _arg4Fmt, target, flags));
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
    public string Translate(string value, string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, UCPlayer? target, ulong targetTeam)
    {
        TranslationFlags flags = GetFlags(targetTeam);
        try
        {
            return string.Format(value, ToStringHelper(arg1, language, _arg0Fmt, target, flags),
                ToStringHelper(arg2, language, _arg1Fmt, target, flags),
                ToStringHelper(arg3, language, _arg2Fmt, target, flags),
                ToStringHelper(arg4, language, _arg3Fmt, target, flags),
                ToStringHelper(arg5, language, _arg4Fmt, target, flags),
                ToStringHelper(arg6, language, _arg5Fmt, target, flags));
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
    public string Translate(string value, string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, UCPlayer? target, ulong targetTeam)
    {
        TranslationFlags flags = GetFlags(targetTeam);
        try
        {
            return string.Format(value, ToStringHelper(arg1, language, _arg0Fmt, target, flags),
                ToStringHelper(arg2, language, _arg1Fmt, target, flags),
                ToStringHelper(arg3, language, _arg2Fmt, target, flags),
                ToStringHelper(arg4, language, _arg3Fmt, target, flags),
                ToStringHelper(arg5, language, _arg4Fmt, target, flags),
                ToStringHelper(arg6, language, _arg5Fmt, target, flags),
                ToStringHelper(arg7, language, _arg6Fmt, target, flags));
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
    public string Translate(string value, string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, UCPlayer? target, ulong targetTeam)
    {
        TranslationFlags flags = GetFlags(targetTeam);
        try
        {
            return string.Format(value, ToStringHelper(arg1, language, _arg0Fmt, target, flags),
                ToStringHelper(arg2, language, _arg1Fmt, target, flags),
                ToStringHelper(arg3, language, _arg2Fmt, target, flags),
                ToStringHelper(arg4, language, _arg3Fmt, target, flags),
                ToStringHelper(arg5, language, _arg4Fmt, target, flags),
                ToStringHelper(arg6, language, _arg5Fmt, target, flags),
                ToStringHelper(arg7, language, _arg6Fmt, target, flags),
                ToStringHelper(arg8, language, _arg7Fmt, target, flags));
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
    public string Translate(string value, string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, UCPlayer? target, ulong targetTeam)
    {
        TranslationFlags flags = GetFlags(targetTeam);
        try
        {
            return string.Format(value, ToStringHelper(arg1, language, _arg0Fmt, target, flags),
                ToStringHelper(arg2, language, _arg1Fmt, target, flags),
                ToStringHelper(arg3, language, _arg2Fmt, target, flags),
                ToStringHelper(arg4, language, _arg3Fmt, target, flags),
                ToStringHelper(arg5, language, _arg4Fmt, target, flags),
                ToStringHelper(arg6, language, _arg5Fmt, target, flags),
                ToStringHelper(arg7, language, _arg6Fmt, target, flags),
                ToStringHelper(arg8, language, _arg7Fmt, target, flags),
                ToStringHelper(arg9, language, _arg8Fmt, target, flags));
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
    public string Translate(string value, string language, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, UCPlayer? target, ulong targetTeam)
    {
        TranslationFlags flags = GetFlags(targetTeam);
        try
        {
            return string.Format(value, ToStringHelper(arg1, language, _arg0Fmt, target, flags),
                ToStringHelper(arg2, language, _arg1Fmt, target, flags),
                ToStringHelper(arg3, language, _arg2Fmt, target, flags),
                ToStringHelper(arg4, language, _arg3Fmt, target, flags),
                ToStringHelper(arg5, language, _arg4Fmt, target, flags),
                ToStringHelper(arg6, language, _arg5Fmt, target, flags),
                ToStringHelper(arg7, language, _arg6Fmt, target, flags),
                ToStringHelper(arg8, language, _arg7Fmt, target, flags),
                ToStringHelper(arg9, language, _arg8Fmt, target, flags),
                ToStringHelper(arg10, language, _arg9Fmt, target, flags));
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