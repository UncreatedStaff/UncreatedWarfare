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
/// Represents an absolute permission with no wildcards or additive/subtractive metadata.
/// </summary>
/// <remarks>Stolen from DevkitServer.</remarks>
[JsonConverter(typeof(PermissionLeafJsonConverter))]
public readonly struct PermissionLeaf : IEquatable<PermissionLeaf>, IEquatable<PermissionBranch>, ITranslationArgument
{
    /// <summary>
    /// A permission that can never be met.
    /// </summary>
    public static readonly PermissionLeaf Nil = new PermissionLeaf();

    /// <summary>
    /// Prefix for Core permissions.
    /// </summary>
    public const string UnturnedModulePrefix = "unturned";

    /// <summary>
    /// Prefix for Warfare module permissions.
    /// </summary>
    public const string WarfareModulePrefix = "warfare";

    /// <summary>
    /// Prefix for invalid permissions.
    /// </summary>
    public const string InvalidPrefix = "unknown";

    private readonly byte _flags;

    /// <summary>
    /// The path relative to the leaf's domain. The domain is the permission prefix of the permission owner.
    /// </summary>
    /// <remarks>Example: <c>request.vehicle</c>.</remarks>
    public string Path { get; }

    /// <summary>
    /// Permission depth level (number of periods plus one).
    /// </summary>
    public int Level { get; }

    /// <summary>
    /// Is this permission relating to the base game?
    /// </summary>
    public bool Unturned => (_flags & 0b001) != 0;

    /// <summary>
    /// Is this permission relating to Uncreated Warfare?
    /// </summary>
    public bool Warfare => (_flags & 0b010) != 0;

    /// <summary>
    /// Is this permission valid (has a path and a source definition)?
    /// </summary>
    public bool Valid => (_flags & 0b011) is not 0 and not 0b011 && !string.IsNullOrWhiteSpace(Path);

    /// <summary>
    /// Parse a permission leaf with or without a prefix.
    /// </summary>
    /// <remarks>Permission leafs without prefixes will be invalid.</remarks>
    /// <exception cref="FormatException">Parse failure.</exception>
    public PermissionLeaf(string path)
    {
        if (!TryParse(path, out this) && Path == null)
            throw new FormatException("Unable to parse permission leaf.");
    }
    internal PermissionLeaf(string path, bool unturned, bool warfare)
    {
        _flags = (byte)((unturned ? 0b001 : 0) | (warfare ? 0b010 : 0));
        Path = path;
        Level = CountLevels(path);
    }


    /// <summary>
    /// Count the number of levels in a path. Example: <c>warfare::request.vehicle</c> would return 2.
    /// </summary>
    /// <remarks>Can optionally have a prefix.</remarks>
    public static int CountLevels(string path)
    {
        int prefixSeparator = -1;

        do
        {
            prefixSeparator = path.IndexOf(':', prefixSeparator + 1);
        }
        while (prefixSeparator > 0 && prefixSeparator < path.Length - 1 && path[prefixSeparator + 1] != ':');

        int ct = 1;
        for (int i = prefixSeparator > 0 ? prefixSeparator + 2 : 0; i < path.Length; ++i)
        {
            if (path[i] == '.') ++ct;
        }

        return ct;
    }

    /// <summary>
    /// Get the permission prefix.
    /// </summary>
    /// <remarks>Looks like this when placed in a full permission leaf: <c>warfare::request.vehicle</c>.</remarks>
    public string GetPrefix()
    {
        return Unturned ? UnturnedModulePrefix : Warfare ? WarfareModulePrefix : InvalidPrefix;
    }

    public override string ToString() => GetPrefix() + "::" + Path;

    public string Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        if ((parameters.Options & TranslationOptions.NoRichText) != 0)
            return ToString();

        string prefix = GetPrefix();
        Color32 prefixColor = Unturned ? new Color32(99, 123, 99, 255) : Warfare ? new Color32(156, 182, 164, 255) : new Color32(221, 221, 221, 255);
        return formatter.Colorize(prefix, prefixColor, parameters.Options) + "<color=#737373>::</color>" + formatter.Colorize(Path, new Color32(211, 222, 214, 255), parameters.Options);
    }

    public bool Equals(PermissionBranch branch) => branch.Equals((PermissionBranch)this);
    public bool Equals(PermissionLeaf leaf)
    {
        return _flags == leaf._flags && string.Equals(Path, leaf.Path, StringComparison.InvariantCultureIgnoreCase);
    }
    public override bool Equals(object? obj)
    {
        if (obj is PermissionLeaf leaf)
            return Equals(leaf);

        if (obj is PermissionBranch branch)
            return branch.Equals((PermissionBranch)this);

        return false;
    }

    public override int GetHashCode() => HashCode.Combine(_flags, Path);

    /// <summary>
    /// Parse a permission leaf with a prefix.
    /// </summary>
    /// <exception cref="FormatException">Parse failure.</exception>
    public static PermissionLeaf Parse(string path)
    {
        if (!TryParse(path, out PermissionLeaf leaf))
            throw new FormatException(leaf.Path != null ? "Unable to find prefix domain for permission leaf." : "Unable to parse permission leaf.");

        return leaf;
    }

    /// <summary>
    /// Parse a permission leaf with a prefix.
    /// </summary>
    /// <returns><see langword="true"/> after a successful parse, otherwise <see langword="false"/>.</returns>
    public static bool TryParse(string path, out PermissionLeaf permissionLeaf)
    {
        permissionLeaf = default;

        if (string.IsNullOrEmpty(path))
            return false;

        int prefixSeparator = -1;

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

        ReadOnlySpan<char> prefix = path.AsSpan(0, prefixSeparator);

        string value = path[(prefixSeparator + 2)..];
        if (string.IsNullOrWhiteSpace(value) || prefix.IsWhiteSpace())
            return false;

        int wildcardIndex = value.IndexOf('*');

        if (prefix.Equals(UnturnedModulePrefix, StringComparison.InvariantCultureIgnoreCase))
        {
            permissionLeaf = new PermissionLeaf(value, unturned: true, warfare: false);
            return wildcardIndex < 0;
        }

        if (prefix.Equals(WarfareModulePrefix, StringComparison.InvariantCultureIgnoreCase))
        {
            permissionLeaf = new PermissionLeaf(value, unturned: false, warfare: true);
            return wildcardIndex < 0;
        }

        permissionLeaf = new PermissionLeaf(value, unturned: false, warfare: false);
        return false;
    }

    public static void Write(ByteWriter writer, PermissionLeaf leaf)
    {
        byte flags = (byte)(leaf._flags | 1 << 7 | (leaf.Path.Length > byte.MaxValue ? 1 << 6 : 0));

        writer.Write(flags);

        if ((flags & 1 << 6) != 0)
            writer.Write(leaf.Path);
        else
            writer.WriteShort(leaf.Path);
    }

    public static PermissionLeaf Read(ByteReader reader)
    {
        byte flags = reader.ReadUInt8();

        string path = (flags & 1 << 6) != 0 ? reader.ReadString() : reader.ReadShortString();

        return new PermissionLeaf(path, unturned: (flags & 0b001) != 0, warfare: (flags & 0b010) != 0);
    }

    public static implicit operator PermissionBranch(PermissionLeaf leaf)
    {
        return new PermissionBranch(PermissionMode.Additive, leaf.Path, (leaf._flags & 0b001) != 0, (leaf._flags & 0b010) != 0);
    }
    public static bool operator ==(PermissionLeaf left, PermissionLeaf right) => left.Equals(right);
    public static bool operator !=(PermissionLeaf left, PermissionLeaf right) => !left.Equals(right);
}

public sealed class PermissionLeafJsonConverter : JsonConverter<PermissionLeaf>
{
    public override PermissionLeaf Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return default;
            case JsonTokenType.String:
                string? str = reader.GetString();
                if (string.IsNullOrEmpty(str) || str.Equals("null", StringComparison.Ordinal))
                    return default;
                if (PermissionLeaf.TryParse(str, out PermissionLeaf leaf))
                    return leaf;

                throw new JsonException("Invalid string value for permission leaf: \"" + str + "\".");
            default:
                throw new JsonException("Unexpected token " + reader.TokenType + " while reading permission leaf.");
        }
    }

    public override void Write(Utf8JsonWriter writer, PermissionLeaf value, JsonSerializerOptions options)
    {
        if (value.Path == null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value.ToString());
    }
}

public sealed class PermissionLeafYamlConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return type == typeof(PermissionLeaf);
    }
    public object ReadYaml(IParser parser, Type type)
    {
        string value = parser.Consume<Scalar>().Value;

        return value.Equals("null", StringComparison.OrdinalIgnoreCase)
            ? default
            : PermissionLeaf.Parse(value);
    }
    public void WriteYaml(IEmitter emitter, object? value, Type type)
    {
        emitter.Emit(new Scalar(value == null ? "null" : ((PermissionLeaf)value).ToString()));
    }
}