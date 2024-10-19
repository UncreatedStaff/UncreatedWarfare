using DanielWillett.SpeedBytes;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.ValueFormatters;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Uncreated.Warfare.Players.Permissions;

/// <summary>
/// Represents a relative permission with wildcards and additive/subtractive metadata.
/// </summary>
/// <remarks>Stolen from DevkitServer.</remarks>
[JsonConverter(typeof(PermissionBranchJsonConverter))]
public readonly struct PermissionBranch : IEquatable<PermissionBranch>, IEquatable<PermissionLeaf>, ITranslationArgument
{
    /// <summary>
    /// Additive superuser branch. Unlocks all permissions.
    /// </summary>
    public static readonly PermissionBranch Superuser = new PermissionBranch(PermissionMode.Additive);

    /// <summary>
    /// Removes a superuser permission branch which would've unlocked all permissions. Doesn't necessarily remove all permissions.
    /// </summary>
    public static readonly PermissionBranch SuperuserSubtractive = new PermissionBranch(PermissionMode.Subtractive);

    private readonly byte _flags;

    /// <summary>
    /// The path relative to the branch's domain. The domain is the permission prefix of the permission owner.
    /// </summary>
    /// <remarks>Example: <c>request.*</c>.</remarks>
    public string Path { get; }

    /// <summary>
    /// Permission depth level (number of periods plus one) where the wildcard is at, or zero for absolute permissions.
    /// </summary>
    /// <remarks>Example: <c>warfare::request.*</c> would be 2.</remarks>
    public int WildcardLevel { get; }

    /// <summary>
    /// How this branch affects existing permissions. Additive gives the permission, subtractive removes it.
    /// </summary>
    public PermissionMode Mode => (PermissionMode)(_flags >> 3 & 1);

    /// <summary>
    /// If this branch doesn't contain a wildcard.
    /// </summary>
    public bool IsAbsolute => WildcardLevel <= 0;

    /// <summary>
    /// Is this permission relating to the base game?
    /// </summary>
    public bool Unturned => (_flags & 0b001) != 0;

    /// <summary>
    /// Is this permission relating to Uncreated Warfare?
    /// </summary>
    public bool Warfare => (_flags & 0b010) != 0;

    /// <summary>
    /// Is this the superuser permission (unlocks all permissions).
    /// </summary>
    public bool IsSuperuser => (_flags & 0b100) != 0;

    /// <summary>
    /// Is this permission valid (has a path and a source definition)?
    /// </summary>
    public bool Valid => (_flags & 0b111) is not 0 and not 0b111 and not 0b110 and not 0b101 and not 0b011 && !string.IsNullOrWhiteSpace(Path);

    /// <summary>
    /// Parse a permission branch with a prefix.
    /// </summary>
    /// <exception cref="FormatException">Parse failure.</exception>
    public PermissionBranch(string path)
    {
        this = Parse(path);
    }
    internal PermissionBranch(PermissionMode mode)
    {
        _flags = (byte)((int)mode << 3 | 0b100);
        Path = "*";
        WildcardLevel = 1;
    }
    internal PermissionBranch(PermissionMode mode, string path, bool unturned, bool warfare)
    {
        _flags = (byte)((int)mode << 3 | (unturned ? 0b001 : 0) | (warfare ? 0b010 : 0));
        WildcardLevel = GetWildcardLevel(path);
        Path = path;
    }

    /// <summary>
    /// Get the wildcard level in a path. Example: <c>warfare::request.*</c> would return 2. If there's no wildcard zero is returned.
    /// </summary>
    /// <remarks>Can optionally have a prefix.</remarks>
    public static int GetWildcardLevel(string path)
    {
        if (path.Length == 1 && path[0] == '*')
            return 1;

        int prefixSeparator = -1;

        do
        {
            prefixSeparator = path.IndexOf(':', prefixSeparator + 1);
        }
        while (prefixSeparator > 0 && prefixSeparator < path.Length - 1 && path[prefixSeparator + 1] != ':');

        int ct = 1;
        bool foundWildcard = false;
        for (int i = prefixSeparator > 0 ? prefixSeparator + 2 : 0; i < path.Length; ++i)
        {
            char c = path[i];
            if (c == '.') ++ct;
            else if (c == '*')
            {
                foundWildcard = true;
                break;
            }
        }

        return foundWildcard ? ct : 0;
    }

    /// <summary>
    /// Get the permission prefix.
    /// </summary>
    /// <remarks>Looks like this when placed in a full permission branch: <c>warfare::request.*</c>.</remarks>
    public string GetPrefix()
    {
        return Unturned ? PermissionLeaf.UnturnedModulePrefix : Warfare ? PermissionLeaf.WarfareModulePrefix : PermissionLeaf.InvalidPrefix;
    }

    public override string ToString()
    {
        string str = !IsSuperuser ? GetPrefix() + "::" + Path : "*";

        if (Mode == PermissionMode.Subtractive)
            str = "-" + str;

        return str;
    }
    public string Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        if ((parameters.Options & TranslationOptions.NoRichText) != 0)
            return ToString();

        if (IsSuperuser)
            return "<color=#fa3219>*</color>";

        string prefix = GetPrefix();
        Color32 prefixColor = Unturned ? new Color32(99, 123, 99, 255) : Warfare ? new Color32(156, 182, 164, 255) : new Color32(221, 221, 221, 255);

        bool hasStar = Path.EndsWith('*');
        string pathEnd = hasStar ? Path[..^1] : Path;
        string pathStar = hasStar ? "<color=#737373>*</color>" : string.Empty;
        string subPrefix = Mode == PermissionMode.Subtractive ? "<color=#ff704d>-</color>" : string.Empty;

        return subPrefix + formatter.Colorize(prefix, prefixColor, parameters.Options) + "<color=#737373>::</color>" + formatter.Colorize(pathEnd, new Color32(211, 222, 214, 255), parameters.Options) + pathStar;
    }
    public bool Equals(PermissionLeaf leaf)
    {
        if (WildcardLevel > 0 || IsSuperuser)
            return false;

        return Unturned == leaf.Unturned
               && Warfare == leaf.Warfare
               && string.Equals(Path, leaf.Path, StringComparison.InvariantCultureIgnoreCase);
    }
    public bool Equals(PermissionBranch branch)
    {
        return WildcardLevel == branch.WildcardLevel
               && _flags == branch._flags
               && string.Equals(Path, branch.Path, StringComparison.InvariantCultureIgnoreCase);
    }
    public bool EqualsWithoutMode(PermissionBranch branch)
    {
        return WildcardLevel == branch.WildcardLevel
               && (_flags & ~(1 << 3)) == (branch._flags & ~(1 << 3))
               && string.Equals(Path, branch.Path, StringComparison.InvariantCultureIgnoreCase);
    }
    public override int GetHashCode() => ~HashCode.Combine(_flags, Path);
    public override bool Equals(object? obj)
    {
        if (obj is PermissionBranch branch)
            return Equals(branch);

        if (obj is PermissionLeaf leaf)
            return Equals(leaf);

        return false;
    }

    /// <summary>
    /// Parse a permission branch with a prefix.
    /// </summary>
    /// <exception cref="FormatException">Parse failure.</exception>
    public static PermissionBranch Parse(string path)
    {
        if (!TryParse(path, out PermissionBranch leaf))
            throw new FormatException(leaf.Path != null ? "Unable to find prefix domain for permission branch." : "Unable to parse permission branch.");

        return leaf;
    }

    private static bool IsDash(char character) => character is '-' or '‐' or '‑' or '‒' or '–' or '—' or '―' or '⸺' or '⸻' or '﹘';
    private static bool IsPlus(char character) => character is '+' or '＋';

    /// <summary>
    /// Parse a permission branch with a prefix.
    /// </summary>
    /// <returns><see langword="true"/> after a successful parse, otherwise <see langword="false"/>.</returns>
    public static bool TryParse(string path, out PermissionBranch permissionBranch)
    {
        permissionBranch = default;
        if (string.IsNullOrEmpty(path))
            return false;

        char firstChar = path[0];
        bool hasFirstChar = IsPlus(firstChar) || IsDash(firstChar);
        PermissionMode mode = hasFirstChar && !IsPlus(firstChar) ? PermissionMode.Subtractive : PermissionMode.Additive;

        if (!hasFirstChar && mode == PermissionMode.Additive && path is ['*'])
        {
            permissionBranch = new PermissionBranch(PermissionMode.Additive);
            return true;
        }
        if (hasFirstChar && path is [_, '*'])
        {
            permissionBranch = new PermissionBranch(mode);
            return true;
        }

        int startIndex = hasFirstChar || path.Length > 1 && firstChar == '\\' && (IsPlus(path[1]) || IsDash(path[1])) ? 0 : -1;
        int prefixSeparator = startIndex;

        do
        {
            prefixSeparator = path.IndexOf(':', prefixSeparator + 1);
            if (prefixSeparator <= 0 || prefixSeparator >= path.Length - 2)
                return false;
        }
        while (path[prefixSeparator + 1] != ':');

        int laterIndex = path.IndexOf(':', prefixSeparator + 2);
        if (laterIndex != -1 && laterIndex < path.Length - 1 && path[laterIndex + 1] == ':')
            return false;

        ReadOnlySpan<char> prefix = path.AsSpan(startIndex + 1, prefixSeparator - startIndex - 1);

        prefixSeparator += 2;

        int wildcardIndex = path.IndexOf('*', prefixSeparator) + 1;
        if (wildcardIndex <= 0)
            wildcardIndex = path.Length;

        string value = path[prefixSeparator..wildcardIndex];

        if (string.IsNullOrWhiteSpace(value) || prefix.IsWhiteSpace())
            return false;

        if (prefix.Equals(PermissionLeaf.UnturnedModulePrefix, StringComparison.InvariantCultureIgnoreCase))
        {
            permissionBranch = new PermissionBranch(mode, value, unturned: true, warfare: false);
            return true;
        }

        if (prefix.Equals(PermissionLeaf.WarfareModulePrefix, StringComparison.InvariantCultureIgnoreCase))
        {
            permissionBranch = new PermissionBranch(mode, value, unturned: false, warfare: true);
            return true;
        }

        permissionBranch = new PermissionBranch(mode, value, unturned: false, warfare: false);
        return false;
    }

    public static void Write(ByteWriter writer, PermissionBranch branch)
    {
        byte flags = (byte)(branch._flags | 1 << 7 | (branch.Path.Length > byte.MaxValue ? 1 << 6 : 0));

        writer.Write(flags);

        // superuser
        if ((flags & 0b100) != 0)
            return;

        if ((flags & 1 << 6) != 0)
            writer.Write(branch.Path);
        else
            writer.WriteShort(branch.Path);
    }

    public static PermissionBranch Read(ByteReader reader)
    {
        byte flags = reader.ReadUInt8();

        PermissionMode mode = (PermissionMode)(flags >> 3 & 1);

        // superuser
        if ((flags & 0b100) != 0)
            return new PermissionBranch(mode);

        string path = (flags & 1 << 6) != 0 ? reader.ReadString() : reader.ReadShortString();
        return new PermissionBranch(mode, path, unturned: (flags & 0b001) != 0, warfare: (flags & 0b010) != 0);
    }

    /// <summary>
    /// If <paramref name="leaf"/> would be included in this branch.
    /// </summary>
    /// <remarks>Does not check <see cref="Mode"/>.</remarks>
    public bool Contains(PermissionLeaf leaf)
    {
        if (IsSuperuser)
            return true;

        if (leaf.Unturned != Unturned || leaf.Warfare != Warfare)
            return false;

        if (WildcardLevel == 0)
            return Path.Equals(leaf.Path, StringComparison.InvariantCultureIgnoreCase);

        ReadOnlySpan<char> leafPath = leaf.Path.AsSpan();
        ReadOnlySpan<char> branchPathWithoutStar = Path.AsSpan(0, Path.Length - 1);

        // make sure "a::b.c.*" contains "a::b.c"
        if (leaf.Level == WildcardLevel - 1)
        {
            return leafPath.Equals(branchPathWithoutStar[..^1], StringComparison.InvariantCultureIgnoreCase);
        }

        return leaf.Level >= WildcardLevel && leafPath.StartsWith(branchPathWithoutStar, StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// If <paramref name="branch"/> (and any leaves it contains) would be included in this branch.
    /// </summary>
    /// <remarks>Does not check <see cref="Mode"/>.</remarks>
    public bool Contains(PermissionBranch branch)
    {
        if (IsSuperuser)
            return true;

        if (branch.Unturned != Unturned || branch.Warfare != Warfare)
            return false;

        if (WildcardLevel == 0)
            return branch.WildcardLevel == 0 && Path.Equals(branch.Path, StringComparison.InvariantCultureIgnoreCase);

        if (branch.WildcardLevel == 0)
            return branch.Path.AsSpan().StartsWith(Path.AsSpan(0, Path.Length - 1), StringComparison.InvariantCultureIgnoreCase);

        return branch.WildcardLevel >= WildcardLevel &&
               branch.Path.AsSpan(0, branch.Path.Length - 1)
                          .StartsWith(Path.AsSpan(0, Path.Length - 1), StringComparison.InvariantCultureIgnoreCase);
    }

    public static explicit operator PermissionLeaf(PermissionBranch branch)
    {
        if ((branch._flags & 0b100) != 0)
            throw new InvalidCastException("Can not represent superuser branch as a leaf.");

        if (branch.WildcardLevel > 0)
            throw new InvalidCastException("Can not represent branches with wildcards as a leaf.");

        return new PermissionLeaf(branch.Path, unturned: (branch._flags & 0b001) != 0, warfare: (branch._flags & 0b010) != 0);
    }
    public static bool operator ==(PermissionBranch left, PermissionBranch right) => left.Equals(right);
    public static bool operator !=(PermissionBranch left, PermissionBranch right) => !left.Equals(right);
}

public sealed class PermissionBranchJsonConverter : JsonConverter<PermissionBranch>
{
    public override PermissionBranch Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return default;
            case JsonTokenType.String:
                string? str = reader.GetString();
                if (string.IsNullOrEmpty(str) || str.Equals("null", StringComparison.Ordinal))
                    return default;
                if (PermissionBranch.TryParse(str, out PermissionBranch leaf))
                    return leaf;

                throw new JsonException("Invalid string value for permission branch: \"" + str + "\".");
            default:
                throw new JsonException("Unexpected token " + reader.TokenType + " while reading permission branch.");
        }
    }

    public override void Write(Utf8JsonWriter writer, PermissionBranch value, JsonSerializerOptions options)
    {
        if (value.Path == null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value.ToString());
    }
}

public sealed class PermissionBranchYamlConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return type == typeof(PermissionBranch);
    }
    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        string value = parser.Consume<Scalar>().Value;

        return value.Equals("null", StringComparison.OrdinalIgnoreCase)
            ? default
            : PermissionBranch.Parse(value);
    }
    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        emitter.Emit(new Scalar(value == null ? "null" : ((PermissionBranch)value).ToString()));
    }
}