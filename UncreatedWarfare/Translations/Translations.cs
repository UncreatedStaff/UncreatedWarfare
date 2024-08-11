using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare;

public class TranslationOld
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
    public TranslationOld(string @default, TranslationFlags flags) : this(@default)
    {
        _flags = flags;
    }
    public TranslationOld(string @default)
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
            L.LogWarning("[" + (lang == null ? "DEFAULT" : lang.Code.ToUpper()) + "] " + Key + " has a size tag, which shouldn't be on signs.", method: "TRANSLATIONS");
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
            L.LogError("[" + (lang == null ? "DEFAULT" : lang.Code.ToUpper()) + "] " + Key + " has " + (max - ct == 1 ? ("an extra paremeter: " + max) : $"{max - ct} extra parameters: Should have: {ct + 1}, has: {max + 1}"), method: "TRANSLATIONS");
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
        foreach (FieldInfo field in Accessor.GetTypesSafe().SelectMany(x => x.GetFields(BindingFlags.Public | BindingFlags.Static)).Where(x => (x.IsLiteral || x.IsInitOnly) && x.FieldType == typeof(string)))
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

    public static async Task ExportLanguage(LanguageInfo? language, bool missingOnly, bool excludeNonPrioritized, CancellationToken token = default)
    {
        language ??= Localization.GetDefaultLanguage();
        await ReloadCommand.ReloadTranslations(token).ConfigureAwait(false);
        await UniTask.SwitchToMainThread(token);

        DirectoryInfo dir = new DirectoryInfo(Path.Combine(Data.Paths.LangStorage, "Export"));
        if (dir.Exists)
            dir.Delete(true);
        dir.Create();
        FileInfo file = new FileInfo(Path.Combine(dir.FullName, LocalFileName));
        WriteLanguage(language, file.FullName, true, missingOnly, excludeNonPrioritized);
        Deaths.DeathMessageResolver.Write(Path.Combine(dir.FullName, "deaths.json"), language, true);
        Localization.WriteEnums(language, Path.Combine(dir.FullName, "Enums"), true, true);

        await KitEx.WriteKitLocalization(language, Path.Combine(dir.FullName, "kits.properties"), true, token);
        await UniTask.SwitchToMainThread(token);

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

    protected static void CheckPluralFormatting(ref short val, ref string? fmt)
    {
        if (!string.IsNullOrEmpty(fmt))
        {
            int ind1 = fmt.IndexOf(T.FormatPlural, StringComparison.Ordinal);
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
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
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
