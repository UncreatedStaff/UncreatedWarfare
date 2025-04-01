using DanielWillett.ReflectionTools;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Logging.Formatting;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Translations.ValueFormatters;
using Uncreated.Warfare.Util;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Uncreated.Warfare.Configuration;

/// <summary>
/// Caches a reference to an asset either by short ID or GUID.
/// </summary>
/// <typeparam name="TAsset">The type of asset to reference.</typeparam>
[CannotApplyEqualityOperator]
[TypeConverter(typeof(AssetLinkTypeConverter))]
public interface IAssetLink<out TAsset> : IAssetContainer, IEquatable<IAssetLink<Asset>>, IAssetReference, ICloneable, ITranslationArgument where TAsset : Asset
{
    /// <summary>
    /// Guid of the asset, if known.
    /// </summary>
    new Guid Guid { get; }

    /// <summary>
    /// Short ID of the asset, if known.
    /// </summary>
    new ushort Id { get; }

    /// <summary>
    /// Get the actual asset from the stored info.
    /// </summary>
    TAsset? GetAsset();

    /// <summary>
    /// Converts this asset link into a <see cref="string"/> for use in logging.
    /// </summary>
    string ToDisplayString();
}

public static class AssetLink
{
    public static readonly SpecialFormat AssetLinkDescriptive = new SpecialFormat("Descriptive", "d", useForToString: false);
    public static readonly SpecialFormat AssetLinkFriendly = new SpecialFormat("Friendly", "f", useForToString: false);
    public static readonly SpecialFormat AssetLinkDescriptiveNoColor = new SpecialFormat("Descriptive (no color)", "nd", useForToString: false);
    public static readonly SpecialFormat AssetLinkFriendlyNoColor = new SpecialFormat("Friendly (no color)", "nf", useForToString: false);

    /// <summary>
    /// Returns a max 48 length string with the item name, defaulting to '00000000000000000000000000000000'.
    /// </summary>
    public static string GetDatabaseName(this IAssetLink<Asset>? assetLink)
    {
        if (assetLink != null && assetLink.TryGetAsset(out Asset? asset) && !string.IsNullOrEmpty(asset.FriendlyName))
            return asset.FriendlyName.Truncate(48);

        return "00000000000000000000000000000000";
    }

    /// <summary>
    /// Returns a max 48 length string with the item name, defaulting to '00000000000000000000000000000000'.
    /// </summary>
    public static string GetDatabaseName(this Asset? asset)
    {
        if (asset is { FriendlyName: { Length: > 0 } fn } )
            return fn.Truncate(48);

        return "00000000000000000000000000000000";
    }

    /// <summary>
    /// Returns an empty asset link.
    /// </summary>
    public static IAssetLink<TAsset> Empty<TAsset>() where TAsset : Asset
    {
        return AssetLinkImpl<TAsset>.Empty;
    }

    /// <summary>
    /// Returns an empty asset link.
    /// </summary>
    public static IAssetLink<Asset> Empty(Type assetType)
    {
        return Create(0, assetType);
    }

    /// <summary>
    /// Create an asset link from a GUID in string form.
    /// </summary>
    public static IAssetLink<TAsset> Create<TAsset>(string guid) where TAsset : Asset
    {
        return new AssetLinkImpl<TAsset>(guid);
    }

    /// <summary>
    /// Create an asset link from a GUID in string form.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="assetType"/> must derive from <see cref="Asset"/>.</exception>
    public static IAssetLink<Asset> Create(string guid, Type assetType)
    {
        if (!typeof(Asset).IsAssignableFrom(assetType))
            throw new ArgumentException("Must derive from type Asset.", nameof(assetType));

        return (IAssetLink<Asset>)Activator.CreateInstance(typeof(AssetLinkImpl<>).MakeGenericType(assetType), guid);
    }
    
    /// <summary>
    /// Create an asset link from a GUID.
    /// </summary>
    public static IAssetLink<TAsset> Create<TAsset>(Guid guid) where TAsset : Asset
    {
        return guid == Guid.Empty ? AssetLinkImpl<TAsset>.Empty : new AssetLinkImpl<TAsset>(guid);
    }

    /// <summary>
    /// Create an asset link from a GUID.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="assetType"/> must derive from <see cref="Asset"/>.</exception>
    public static IAssetLink<Asset> Create(Guid guid, Type assetType)
    {
        if (!typeof(Asset).IsAssignableFrom(assetType))
            throw new ArgumentException("Must derive from type Asset.", nameof(assetType));

        return (IAssetLink<Asset>)Activator.CreateInstance(typeof(AssetLinkImpl<>).MakeGenericType(assetType), guid);
    }
    
    /// <summary>
    /// Create an asset link from a short ID.
    /// </summary>
    public static IAssetLink<TAsset> Create<TAsset>(ushort id) where TAsset : Asset
    {
        return id == 0 ? AssetLinkImpl<TAsset>.Empty : new AssetLinkImpl<TAsset>(id);
    }

    /// <summary>
    /// Create an asset link from a short ID.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="assetType"/> must derive from <see cref="Asset"/>.</exception>
    public static IAssetLink<Asset> Create(ushort id, Type assetType)
    {
        if (!typeof(Asset).IsAssignableFrom(assetType))
            throw new ArgumentException("Must derive from type Asset.", nameof(assetType));

        return (IAssetLink<Asset>)Activator.CreateInstance(typeof(AssetLinkImpl<>).MakeGenericType(assetType), id);
    }
    
    /// <summary>
    /// Create an asset link from an asset.
    /// </summary>
    public static IAssetLink<TAsset> Create<TAsset>(TAsset? asset) where TAsset : Asset
    {
        return new AssetLinkImpl<TAsset>(asset);
    }

    /// <summary>
    /// Create an asset link from an asset.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="assetType"/> must derive from <see cref="Asset"/>.</exception>
    public static IAssetLink<Asset> Create(Asset? asset, Type assetType)
    {
        if (asset != null && !assetType.IsInstanceOfType(asset))
            throw new ArgumentException("Asset is not of the type requested.", nameof(asset));

        if (!typeof(Asset).IsAssignableFrom(assetType))
            throw new ArgumentException("Must derive from type Asset.", nameof(assetType));

        return (IAssetLink<Asset>)Activator.CreateInstance(typeof(AssetLinkImpl<>).MakeGenericType(assetType), asset);
    }

    /// <summary>
    /// Create an asset link from an asset reference.
    /// </summary>
    public static IAssetLink<TAsset> Create<TAsset>(AssetReference<TAsset> assetReference) where TAsset : Asset
    {
        return new AssetLinkImpl<TAsset>(assetReference);
    }

    /// <summary>
    /// Cast an asset link from one type to another.
    /// </summary>
    /// <exception cref="InvalidCastException"/>
    public static IAssetLink<TToAsset> Cast<TFromAsset, TToAsset>(this IAssetLink<TFromAsset> assetLink) where TFromAsset : Asset where TToAsset : Asset
    {
        if (assetLink is IAssetLink<TToAsset> to)
            return to;

        TFromAsset? asset = assetLink.GetAsset();
        if (asset is not null and not TToAsset)
            throw new InvalidCastException($"Can not cast an asset of type {Accessor.ExceptionFormatter.Format(typeof(TFromAsset))} to {Accessor.ExceptionFormatter.Format(typeof(TToAsset))}.");

        return new AssetLinkImpl<TToAsset>(assetLink);
    }

    /// <summary>
    /// Cast an asset link from one type to another.
    /// </summary>
    /// <exception cref="InvalidCastException"/>
    /// <exception cref="ArgumentException"><paramref name="toAssetType"/> must derive from <see cref="Asset"/>.</exception>
    public static IAssetLink<Asset> Cast(IAssetLink<Asset> assetLink, Type toAssetType)
    {
        if (!typeof(Asset).IsAssignableFrom(toAssetType))
            throw new ArgumentException("Must derive from type Asset.", nameof(toAssetType));

        Type fromAssetType = GetAssetType(assetLink);
        if (toAssetType.IsAssignableFrom(fromAssetType))
            return assetLink;

        Asset? asset = assetLink.GetAsset();
        if (asset != null && !toAssetType.IsInstanceOfType(asset))
            throw new InvalidCastException($"Can not cast an asset of type {Accessor.ExceptionFormatter.Format(fromAssetType)} to {Accessor.ExceptionFormatter.Format(toAssetType)}.");

        return (IAssetLink<Asset>)Activator.CreateInstance(typeof(AssetLinkImpl<>).MakeGenericType(toAssetType), assetLink);
    }

    /// <summary>
    /// Create an asset link from an asset container. May return itself if <paramref name="container"/> is <see cref="IAssetLink{TAsset}"/>.
    /// </summary>
    public static IAssetLink<TAsset> Create<TAsset>(IAssetContainer container) where TAsset : Asset
    {
        return container as IAssetLink<TAsset> ?? new AssetLinkImpl<TAsset>(container);
    }

    /// <summary>
    /// Create an asset link from an asset.
    /// </summary>
    public static IAssetLink<TAsset> Parse<TAsset>(string? value) where TAsset : Asset
    {
        if (string.IsNullOrEmpty(value))
            return new AssetLinkImpl<TAsset>(0);

        if (Guid.TryParse(value, out Guid guid))
            return new AssetLinkImpl<TAsset>(guid);

        if (ushort.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out ushort id))
            return new AssetLinkImpl<TAsset>(id);

        Asset? pathAsset = Assets.findByAbsolutePath(value);
        if (pathAsset is TAsset asset)
            return Create(asset);

        if (pathAsset != null)
            throw new FormatException($"Asset path does not match asset of type {Accessor.ExceptionFormatter.Format(typeof(TAsset))} while reading {Accessor.ExceptionFormatter.Format(typeof(IAssetLink<TAsset>))} in string notation.");

        throw new FormatException($"Invalid string value while reading {Accessor.ExceptionFormatter.Format(typeof(IAssetLink<TAsset>))} in string notation.");
    }

    /// <summary>
    /// Create an asset link from an asset.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="assetType"/> must derive from <see cref="Asset"/>.</exception>
    public static IAssetLink<Asset> Parse(string? value, Type assetType)
    {
        if (!typeof(Asset).IsAssignableFrom(assetType))
            throw new ArgumentException("Must derive from type Asset.", nameof(assetType));

        if (string.IsNullOrEmpty(value))
            return (IAssetLink<Asset>)Activator.CreateInstance(typeof(AssetLinkImpl<>).MakeGenericType(assetType), 0);

        if (Guid.TryParse(value, out Guid guid))
            return (IAssetLink<Asset>)Activator.CreateInstance(typeof(AssetLinkImpl<>).MakeGenericType(assetType), guid);

        if (ushort.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out ushort id))
            return (IAssetLink<Asset>)Activator.CreateInstance(typeof(AssetLinkImpl<>).MakeGenericType(assetType), id);

        Asset? pathAsset = Assets.findByAbsolutePath(value);
        if (pathAsset != null && assetType.IsInstanceOfType(pathAsset))
            return (IAssetLink<Asset>)Activator.CreateInstance(typeof(AssetLinkImpl<>).MakeGenericType(assetType), pathAsset);

        if (pathAsset != null)
            throw new FormatException($"Asset path does not match asset of type {Accessor.ExceptionFormatter.Format(assetType)} while reading {Accessor.ExceptionFormatter.Format(typeof(IAssetLink<>).MakeGenericType(assetType))} in string notation.");

        throw new FormatException($"Invalid string value while reading {Accessor.ExceptionFormatter.Format(typeof(IAssetLink<>).MakeGenericType(assetType))} in string notation.");
    }

    /// <summary>
    /// Create an asset link from a string.
    /// </summary>
    /// <returns><see langword="true"/> if parsing was successful or the string was null/empty (outputting an empty asset), otherwise <see langword="false"/>.</returns>s
    public static bool TryParse<TAsset>(string? value, [MaybeNullWhen(false)] out IAssetLink<TAsset> asset) where TAsset : Asset
    {
        if (string.IsNullOrEmpty(value))
        {
            asset = new AssetLinkImpl<TAsset>(0);
            return true;
        }

        if (Guid.TryParse(value, out Guid guid))
        {
            asset = new AssetLinkImpl<TAsset>(guid);
            return true;
        }

        if (ushort.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out ushort id))
        {
            asset = new AssetLinkImpl<TAsset>(id);
            return true;
        }

        Asset? pathAsset = Assets.findByAbsolutePath(value);
        if (pathAsset is TAsset typedAsset)
        {
            asset = Create(typedAsset);
            return true;
        }

        asset = null;
        return false;
    }
    
    /// <summary>
    /// Create an asset link from a string.
    /// </summary>
    /// <returns><see langword="true"/> if parsing was successful or the string was null/empty (outputting an empty asset), otherwise <see langword="false"/>.</returns>
    public static bool TryParse(string? value, Type assetType, [MaybeNullWhen(false)] out IAssetLink<Asset> asset)
    {
        if (string.IsNullOrEmpty(value))
        {
            asset = (IAssetLink<Asset>)Activator.CreateInstance(typeof(AssetLinkImpl<>).MakeGenericType(assetType), 0);
            return true;
        }

        if (Guid.TryParse(value, out Guid guid))
        {
            asset = (IAssetLink<Asset>)Activator.CreateInstance(typeof(AssetLinkImpl<>).MakeGenericType(assetType), guid);
            return true;
        }

        if (ushort.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out ushort id))
        {
            asset = (IAssetLink<Asset>)Activator.CreateInstance(typeof(AssetLinkImpl<>).MakeGenericType(assetType), id);
            return true;
        }

        Asset? pathAsset = Assets.findByAbsolutePath(value);
        if (pathAsset != null && assetType.IsInstanceOfType(pathAsset))
        {
            asset = (IAssetLink<Asset>)Activator.CreateInstance(typeof(AssetLinkImpl<>).MakeGenericType(assetType), pathAsset);
            return true;
        }

        asset = null;
        return false;
    }

    /// <summary>
    /// Resolve an asset from an asset link, or throw an error
    /// </summary>
    /// <exception cref="AssetNotFoundException"/>
    public static TAsset GetAssetOrFail<TAsset>([System.Diagnostics.CodeAnalysis.NotNull] this IAssetLink<TAsset>? link) where TAsset : Asset
    {
        return link == null ? throw new AssetNotFoundException() : link.GetAsset() ?? throw new AssetNotFoundException(link);
    }

    /// <summary>
    /// Throw an error only if this asset link is invalid. 
    /// </summary>
    /// <exception cref="AssetNotFoundException"/>
    public static void AssertValid<TAsset>([System.Diagnostics.CodeAnalysis.NotNull] this IAssetLink<TAsset>? link) where TAsset : Asset
    {
        if (link == null || !link.TryGetAsset(out _))
            throw new AssetNotFoundException();
    }

    /// <summary>
    /// Resolve an asset from an asset link, or throw an error
    /// </summary>
    /// <exception cref="AssetNotFoundException"/>
    public static TAsset GetAssetOrFail<TAsset>([System.Diagnostics.CodeAnalysis.NotNull] this IAssetLink<TAsset>? link, string propertyName) where TAsset : Asset
    {
        return link == null ? throw new AssetNotFoundException(propertyName) : link.GetAsset() ?? throw new AssetNotFoundException(link, propertyName);
    }

    /// <summary>
    /// Attempt to resolve an asset from an asset link.
    /// </summary>
    public static bool TryGetAsset<TAsset>([NotNullWhen(true)] this IAssetLink<TAsset>? link, [NotNullWhen(true)] out TAsset? asset) where TAsset : Asset
    {
        asset = link?.GetAsset();
        return asset != null;
    }

    /// <summary>
    /// Attempt to resolve the short ID from an asset link.
    /// </summary>
    public static bool TryGetId([NotNullWhen(true)] this IAssetLink<Asset>? link, out ushort id)
    {
        if (link == null)
        {
            id = 0;
            return false;
        }

        Asset? asset = link.GetAsset();

        id = link.Id;
        return id != 0 || asset is { id: 0 };
    }

    /// <summary>
    /// Attempt to resolve the GUID from an asset link.
    /// </summary>
    public static bool TryGetGuid([NotNullWhen(true)] this IAssetLink<Asset>? link, out Guid guid)
    {
        if (link == null)
        {
            guid = default;
            return false;
        }

        Asset? asset = link.GetAsset();

        guid = link.Guid;
        return guid != Guid.Empty || asset != null && asset.GUID == Guid.Empty;
    }

    /// <summary>
    /// Attempt to resolve an asset from an asset variant dictionary.
    /// </summary>
    public static bool TryGetAsset<TAsset>([NotNullWhen(true)] this AssetVariantDictionary<TAsset>? reference, string? variant, [NotNullWhen(true)] out TAsset? asset) where TAsset : Asset
    {
        IAssetLink<TAsset>? ref2 = reference?.Resolve(variant);
        if (ref2 is not null)
            return ref2.TryGetAsset(out asset);

        asset = default!;
        return false;
    }

    /// <summary>
    /// Attempt to resolve the short ID from an asset variant dictionary.
    /// </summary>
    public static bool TryGetId<TAsset>([NotNullWhen(true)] this AssetVariantDictionary<TAsset>? reference, string? variant, out ushort id) where TAsset : Asset
    {
        IAssetLink<TAsset>? ref2 = reference?.Resolve(variant);
        if (ref2 is not null)
            return ref2.TryGetId(out id);

        id = default;
        return false;
    }

    /// <summary>
    /// Attempt to resolve the GUID from an asset variant dictionary.
    /// </summary>
    public static bool TryGetGuid<TAsset>([NotNullWhen(true)] this AssetVariantDictionary<TAsset>? reference, string? variant, out Guid guid) where TAsset : Asset
    {
        IAssetLink<TAsset>? ref2 = reference?.Resolve(variant);
        if (ref2 is not null)
            return ref2.TryGetGuid(out guid);

        guid = default;
        return false;
    }

    /// <summary>
    /// Compare an asset link to an asset.
    /// </summary>
    public static bool MatchAsset<TAsset>(this IAssetLink<TAsset>? link, TAsset? asset) where TAsset : Asset
    {
        if (link == null)
        {
            return asset == null;
        }

        if (asset == null)
            return link.Guid == Guid.Empty && link.Id == 0;

        if (asset.GUID != Guid.Empty && link.Guid == asset.GUID)
            return true;
        
        return asset.id != 0 && link.Id == asset.id;
    }

    /// <summary>
    /// Compare an asset link to another asset link.
    /// </summary>
    public static bool MatchAsset(this IAssetLink<Asset>? link, IAssetLink<Asset>? asset)
    {
        if (ReferenceEquals(link, asset))
            return true;

        return !ReferenceEquals(link, null) && link.Equals(asset!);
    }

    /// <summary>
    /// Compare an asset link to another asset link.
    /// </summary>
    public static bool MatchAsset(this IAssetLink<Asset>? link, IAssetContainer? container)
    {
        if (ReferenceEquals(link, container))
            return true;

        if (container == null || link == null)
            return false;

        if (container.Guid != Guid.Empty && link.Guid != Guid.Empty)
            return link.Guid == container.Guid;

        if (container.Id != 0 && link.Id != 0)
            return link.Id == container.Id;

        return container.Id == 0 && link.Id == 0 && container.Guid == Guid.Empty && link.Guid == Guid.Empty;
    }

    /// <summary>
    /// Compare an asset link to a short ID.
    /// </summary>
    public static bool MatchId(this IAssetLink<Asset>? link, ushort id)
    {
        if (link == null)
        {
            return id == 0;
        }

        return link.TryGetId(out ushort thisId) && thisId == id;
    }

    /// <summary>
    /// Compare an asset link to a GUID.
    /// </summary>
    public static bool MatchGuid(this IAssetLink<Asset>? link, Guid guid)
    {
        if (link == null)
        {
            return guid == Guid.Empty;
        }

        return link.TryGetGuid(out Guid thisGuid) && thisGuid == guid;
    }

    /// <summary>
    /// See if a list of asset links contains an asset.
    /// </summary>
    public static bool ContainsAsset<TAsset>([NotNullWhen(true)] this IEnumerable<IAssetLink<Asset>?>? links, TAsset? asset) where TAsset : Asset
    {
        if (links == null)
        {
            return false;
        }

        foreach (IAssetLink<Asset>? link in links)
        {
            if (link.MatchAsset(asset))
                return true;
        }

        return false;
    }

    /// <summary>
    /// See if a list of asset links contains an asset link.
    /// </summary>
    public static bool ContainsAsset([NotNullWhen(true)] this IEnumerable<IAssetLink<Asset>?>? links, IAssetLink<Asset>? asset)
    {
        if (links == null)
        {
            return false;
        }

        foreach (IAssetLink<Asset>? link in links)
        {
            if (link.MatchAsset(asset))
                return true;
        }

        return false;
    }

    /// <summary>
    /// See if a list of asset links contains a short ID.
    /// </summary>
    public static bool ContainsId([NotNullWhen(true)] this IEnumerable<IAssetLink<Asset>?>? links, ushort id)
    {
        if (links == null)
        {
            return false;
        }

        foreach (IAssetLink<Asset>? link in links)
        {
            if (link.MatchId(id))
                return true;
        }

        return false;
    }

    /// <summary>
    /// See if a list of asset links contains a GUID.
    /// </summary>
    public static bool ContainsGuid([NotNullWhen(true)] this IEnumerable<IAssetLink<Asset>?>? links, Guid guid)
    {
        if (links == null)
        {
            return false;
        }

        foreach (IAssetLink<Asset>? link in links)
        {
            if (link.MatchGuid(guid))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines the value of the generic argument of <see cref="IAssetLink{TAsset}"/> given an object of any asset link type.
    /// </summary>
    public static Type GetAssetType(IAssetLink<Asset> assetLink)
    {
        Type type = assetLink.GetType();
        if (type.IsGenericType)
        {
            Type[] args = type.GetGenericArguments();
            for (int i = 0; i < args.Length; ++i)
            {
                if (typeof(Asset).IsAssignableFrom(args[i]))
                {
                    return args[i];
                }
            }
        }

        Type? intxType = Array.Find(type.GetInterfaces(), x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IAssetLink<>));
        return intxType?.GetGenericArguments()[0] ?? typeof(Asset);
    }

    /// <summary>
    /// Read an asset link from a configuration section.
    /// </summary>
    public static IAssetLink<TAsset> GetAssetLink<TAsset>(this IConfiguration configuration) where TAsset : Asset
    {
        return configuration.Get<IAssetLink<TAsset>>() ?? new AssetLinkImpl<TAsset>(0);
    }

    /// <summary>
    /// Read an asset link from a configuration at a given <paramref name="key"/> path.
    /// </summary>
    public static IAssetLink<TAsset> GetAssetLink<TAsset>(this IConfiguration configuration, string key) where TAsset : Asset
    {
        return configuration.GetValue<IAssetLink<TAsset>>(key) ?? new AssetLinkImpl<TAsset>(0);
    }

    /// <summary>
    /// Write an <see cref="IAssetLink{TAsset}"/> to a json writer.
    /// </summary>
    internal static void WriteJson(Utf8JsonWriter writer, IAssetLink<Asset>? assetLink)
    {
        if (assetLink == null)
        {
            writer.WriteNullValue();
            return;
        }

        Guid guid = assetLink.Guid;
        if (guid != Guid.Empty)
        {
            writer.WriteStringValue(guid);
            return;
        }

        ushort id = assetLink.Id;
        if (id == default)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteNumberValue(id);
    }

    /// <summary>
    /// Read an <see cref="IAssetLink{TAsset}"/> from a json reader.
    /// </summary>
    internal static IAssetLink<TAsset>? ReadJson<TAsset>(ref Utf8JsonReader reader) where TAsset : Asset
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            if (!reader.TryGetUInt16(out ushort id))
                throw new JsonException($"Short ID out of range while reading {Accessor.ExceptionFormatter.Format(typeof(IAssetLink<TAsset>))} in number notation.");

            return Create<TAsset>(id);
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            if (reader.TryGetGuid(out Guid guid))
                return Create<TAsset>(guid);

            string? val = reader.GetString();

            if (ushort.TryParse(reader.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out ushort id))
                return Create<TAsset>(id);

            Asset? pathAsset = Assets.findByAbsolutePath(val);
            if (pathAsset is TAsset asset)
                return Create(asset);

            if (pathAsset != null)
                throw new JsonException($"Asset path does not match asset of type {Accessor.ExceptionFormatter.Format(typeof(TAsset))} while reading {Accessor.ExceptionFormatter.Format(typeof(IAssetLink<TAsset>))} in string notation.");

            throw new JsonException($"Invalid string value while reading {Accessor.ExceptionFormatter.Format(typeof(IAssetLink<TAsset>))} in string notation.");
        }

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Unexpected token {reader.TokenType} while reading {Accessor.ExceptionFormatter.Format(typeof(IAssetLink<TAsset>))}.");

        if (!reader.Read() || reader.TokenType != JsonTokenType.PropertyName || !string.Equals(reader.GetString(), "GUID", StringComparison.OrdinalIgnoreCase))
            throw new JsonException($"Unexpected object layout while reading {Accessor.ExceptionFormatter.Format(typeof(IAssetLink<TAsset>))}.");

        if (reader.TokenType == JsonTokenType.Null)
            return Create<TAsset>(Guid.Empty);

        if (reader.TryGetGuid(out Guid nestedGuid))
            return Create<TAsset>(nestedGuid);

        throw new JsonException($"Unable to parse GUID while reading {Accessor.ExceptionFormatter.Format(typeof(IAssetLink<TAsset>))} in object notation.");
    }

    /// <summary>
    /// Write an <see cref="IAssetLink{TAsset}"/> to a yaml emitter.
    /// </summary>
    public static void WriteYaml(IEmitter emitter, IAssetLink<Asset>? assetLink)
    {
        if (assetLink == null)
        {
            emitter.Emit(new Scalar("null"));
            return;
        }
        
        Guid guid = assetLink.Guid;
        if (guid != Guid.Empty)
        {
            emitter.Emit(new Scalar(guid.ToString("N")));
            return;
        }

        ushort id = assetLink.Id;
        if (id == default)
        {
            emitter.Emit(new Scalar("null"));
            return;
        }

        emitter.Emit(new Scalar(id.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>
    /// Read an <see cref="IAssetLink{TAsset}"/> from a yaml parser.
    /// </summary>
    public static object? ReadYaml(IParser parser, Type type)
    {
        string value = parser.Consume<Scalar>().Value;
        Type genType = type.GetGenericArguments()[0];

        if (Guid.TryParse(value, out Guid guid))
            return Activator.CreateInstance(typeof(AssetLinkImpl<>).MakeGenericType(genType), guid);

        if (ushort.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out ushort id))
            return Activator.CreateInstance(typeof(AssetLinkImpl<>).MakeGenericType(genType), id);

        Asset? pathAsset = Assets.findByAbsolutePath(value);
        if (pathAsset != null && genType.IsInstanceOfType(pathAsset))
            return Activator.CreateInstance(typeof(AssetLinkImpl<>).MakeGenericType(genType), pathAsset);

        if (pathAsset != null)
            throw new JsonException($"Asset path does not match asset of type {Accessor.ExceptionFormatter.Format(genType)} while reading {Accessor.ExceptionFormatter.Format(type)} in string notation.");

        throw new JsonException($"Invalid string value while reading {Accessor.ExceptionFormatter.Format(type)} in string notation.");
    }

    /// <summary>
    /// Convert an asset to a display string.
    /// </summary>
    public static string ToDisplayString<TAsset>(TAsset? asset) where TAsset : Asset
    {
        return asset == null ? "0" : ToDisplayString(asset, asset.id, asset.GUID);
    }

    /// <summary>
    /// Convert an asset to a display string.
    /// </summary>
    public static string ToDisplayString<TAsset>(TAsset? asset, ushort id, Guid guid) where TAsset : Asset
    {
        if (asset == null)
        {
            return guid == Guid.Empty ? id.ToString("D", CultureInfo.InvariantCulture) : guid.ToString("N", CultureInfo.InvariantCulture);
        }

        if (guid != Guid.Empty)
        {
            if (id != 0)
                return "\"" + asset.name + "\" {" + guid.ToString("N", CultureInfo.InvariantCulture) + "} (" +
                       AssetUtility.GetAssetCategory(asset) + "/" +
                       id.ToString("D", CultureInfo.InvariantCulture) + ")";

            return "\"" + asset.name + "\" {" + guid.ToString("N", CultureInfo.InvariantCulture) + "}";
        }

        if (id != 0)
            return "\"" + asset.name + "\" (" +
                   AssetUtility.GetAssetCategory(asset) + "/" +
                   id.ToString("F0", CultureInfo.InvariantCulture) + ")";

        return "\"" + asset.name + "\"";
    }

    /// <summary>
    /// Caches a reference to an asset either by short ID or GUID.
    /// </summary>
    /// <typeparam name="TAsset">The type of asset to reference.</typeparam>
    private class AssetLinkImpl<TAsset> : IAssetLink<TAsset> where TAsset : Asset
    {
        internal static readonly IAssetLink<TAsset> Empty = new AssetLinkImpl<TAsset>(0);

        private Guid _guid;
        private ushort _id;
        private TAsset? _cachedAsset;

        public Guid Guid
        {
            get
            {
                if (_cachedAsset == null && _guid == Guid.Empty)
                    GetAsset();

                return _guid;
            }
        }

        public ushort Id
        {
            get
            {
                if (_cachedAsset == null && _id == 0)
                    GetAsset();

                return _id;
            }
        }

        public TAsset? GetAsset()
        {
            if (_cachedAsset != null)
                return _cachedAsset;

            if (_guid != Guid.Empty)
            {
                _cachedAsset = Assets.find(_guid) as TAsset;
            }
            else if (_id != default)
            {
                _cachedAsset = Assets.find(AssetUtility.GetAssetCategory<TAsset>(), _id) as TAsset;
            }

            if (_cachedAsset == null)
                return null;

            _id = _cachedAsset.id;
            _guid = _cachedAsset.GUID;
            return _cachedAsset;
        }

        Asset? IAssetContainer.Asset => GetAsset();

        public AssetLinkImpl(string guid) : this(Guid.Parse(guid)) { }
        public AssetLinkImpl(Guid guid)
        {
            _guid = guid;
        }
        public AssetLinkImpl(ushort id)
        {
            _id = id;
        }
        public AssetLinkImpl(TAsset? asset)
        {
            if (asset == null)
                return;

            _id = asset.id;
            _guid = asset.GUID;
            _cachedAsset = asset;
        }

        public AssetLinkImpl(AssetReference<TAsset> assetRef)
        {
            _guid = assetRef.GUID;
        }

        public AssetLinkImpl(IAssetContainer assetContainer)
        {
            if (assetContainer is AssetLinkImpl<TAsset> link)
            {
                _guid = link._guid;
                _id = link._id;
                _cachedAsset = link._cachedAsset;
                return;
            }

            if (assetContainer == null)
            {
                _id = 0;
                _guid = Guid.Empty;
                return;
            }

            Asset? asset = assetContainer.Asset;
            if (asset == null)
                return;

            if (asset is not TAsset typedAsset)
                throw new ArgumentException($"Container's asset is not of type {Accessor.ExceptionFormatter.Format(typeof(TAsset))}.", nameof(assetContainer));

            _id = asset.id;
            _guid = asset.GUID;
            _cachedAsset = typedAsset;
        }

        public bool Equals(IAssetLink<Asset>? other)
        {
            if (ReferenceEquals(null, other))
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return Guid.Equals(other.Guid) && Id == other.Id;
        }

        public override string ToString()
        {
            if (Guid != Guid.Empty)
                return Guid.ToString("N");

            if (Id != 0)
                return Id.ToString();

            return _cachedAsset != null
                ? Path.GetRelativePath(UnturnedPaths.RootDirectory.FullName, _cachedAsset.absoluteOriginFilePath)
                : "0";
        }

        public string ToDisplayString()
        {
            return AssetLink.ToDisplayString(GetAsset(), Id, Guid);
        }

        public string ToDisplayString(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
        {
            if ((parameters.Options & TranslationOptions.NoRichText) != 0)
            {
                return ToDisplayString();
            }

            bool rarity = AssetLinkFriendly.Match(in parameters) || AssetLinkDescriptive.Match(in parameters);

            TAsset? asset = GetAsset();
            if (asset != null)
                return AssetValueFormatter.Format(asset, formatter, in parameters);

            Guid guid = Guid;
            ushort id = Id;
            string? idStr;
            if (id != 0 || guid == Guid.Empty)
            {
                string enumStr = formatter.FormatEnum(AssetUtility.GetAssetCategory<TAsset>(), parameters.Language);
                if (rarity)
                    enumStr = formatter.Colorize(enumStr, WarfareFormattedLogValues.EnumColor, parameters.Options);

                Span<char> numSpan = stackalloc char[5];
                id.TryFormat(numSpan, out int len, "F0", parameters.Culture);
                idStr = formatter.Colorize(numSpan[..len], WarfareFormattedLogValues.NumberColor, parameters.Options);

                idStr = enumStr + "/" + idStr;
            }
            else idStr = null;

            if (guid == Guid.Empty)
                return $"({idStr!})";

            string guidStr;
            if (rarity)
            {
                Span<char> span = stackalloc char[32];
                guid.TryFormat(span, out _, "N");
                guidStr = formatter.Colorize(span, WarfareFormattedLogValues.StructColor, parameters.Options);
            }
            else
            {
                guidStr = guid.ToString("N");
            }
                
            return id != 0 ? $"{{{guidStr}}} ({idStr})" : $"{{{guidStr}}}";
        }

        public string Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
        {
            TAsset? asset = GetAsset();
            if (asset != null)
            {
                if (AssetLinkFriendlyNoColor.Match(in parameters))
                {
                    return asset.FriendlyName;
                }
                if (AssetLinkFriendly.Match(in parameters))
                {
                    return RarityColorAddon.Apply(asset.FriendlyName, asset, formatter, in parameters);
                }
                if (AssetLinkDescriptiveNoColor.Match(in parameters))
                {
                    return ToDisplayString();
                }
            }

            if (AssetLinkFriendly.Match(in parameters))
            {
                Guid guid = Guid;
                if (Guid != Guid.Empty)
                {
                    Span<char> span = stackalloc char[32];
                    guid.TryFormat(span, out _, "N");
                    return formatter.Colorize(span, WarfareFormattedLogValues.StructColor, parameters.Options);
                }

                if (Id != 0)
                {
                    Span<char> numSpan = stackalloc char[5];
                    Id.TryFormat(numSpan, out int len, "F0", parameters.Culture);
                    return formatter.Colorize(numSpan[..len], WarfareFormattedLogValues.NumberColor, parameters.Options);
                }

                return formatter.Format(null, in parameters, typeof(object));
            }

            return ToDisplayString(formatter, in parameters);
        }

        public override bool Equals(object? obj)
        {
            return !ReferenceEquals(null, obj) && (ReferenceEquals(this, obj) || Guid.Equals(((IAssetLink<TAsset>)obj).Guid) && Id == ((IAssetLink<TAsset>)obj).Id);
        }

        public override int GetHashCode()
        {
            return Guid != Guid.Empty ? Guid.GetHashCode() : Id;
        }

        public object Clone()
        {
            return new AssetLinkImpl<TAsset>(this);
        }

        Guid IAssetReference.GUID { get => Guid; set => throw new NotSupportedException(); }
        bool IAssetReference.isValid => _guid != Guid.Empty || _id != 0 || _cachedAsset != null;
    }
}

public class AssetLinkTypeConverter : TypeConverter
{
    private readonly Type _type;
    public AssetLinkTypeConverter(Type type)
    {
        _type = type;
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        return destinationType == typeof(string)
               || destinationType == typeof(ushort)
               || destinationType == typeof(Guid)
               || destinationType.IsGenericType && destinationType.GetGenericTypeDefinition() == typeof(IAssetLink<>);
    }

    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string)
               || sourceType == typeof(ushort)
               || sourceType == typeof(Guid)
               || sourceType.IsGenericType && sourceType.GetGenericTypeDefinition() == typeof(IAssetLink<>)
               || base.CanConvertFrom(context, sourceType);
    }

    public override bool IsValid(ITypeDescriptorContext context, object value)
    {
        if (value is string str)
            return AssetLink.TryParse<Asset>(str, out _);

        return value is Guid or IAssetLink<Asset> or ushort;
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object? value)
    {
        Type? assetType = _type.IsConstructedGenericType ? _type.GetGenericArguments()[0] : null;

        return value switch
        {
            IAssetLink<Asset> => value,

            string str when assetType != null => AssetLink.Parse(str, assetType),
            string str => AssetLink.Parse<Asset>(str),

            Guid guid when assetType != null => AssetLink.Create(guid, assetType),
            Guid guid => AssetLink.Create<Asset>(guid),

            ushort id when assetType != null => AssetLink.Create(id, assetType),
            ushort id => AssetLink.Create<Asset>(id),

            _ => base.ConvertFrom(context, culture, value)!
        };
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (value is not IAssetLink<Asset> asset)
            throw GetConvertToException(value, destinationType);

        if (destinationType == typeof(string))
        {
            return asset.ToString();
        }

        if (destinationType == typeof(Guid))
        {
            return asset.Guid;
        }

        if (destinationType == typeof(ushort))
        {
            return asset.Id;
        }

        if (destinationType.IsGenericType && destinationType.GetGenericTypeDefinition() == typeof(IAssetLink<>))
        {
            return AssetLink.Cast(asset, destinationType.GetGenericArguments()[0]);
        }

        throw GetConvertToException(value, destinationType);
    }
}

public class AssetLinkJsonFactory : JsonConverterFactory
{
    private static readonly ConcurrentDictionary<Type, JsonConverter> ConverterCache = new ConcurrentDictionary<Type, JsonConverter>();
    public override bool CanConvert(Type typeToConvert)
    {
        return ConverterCache.ContainsKey(typeToConvert) || typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(IAssetLink<>);
    }
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return ConverterCache.GetOrAdd(
            typeToConvert,
            typeToConvert =>
            {
                Type[] genTypes = typeToConvert.GetGenericArguments();
                if (genTypes.Length == 0)
                    return new AssetLinkJsonConverter<Asset>();

                return (JsonConverter)Activator.CreateInstance(typeof(AssetLinkJsonConverter<>).MakeGenericType());
            }
        );
    }
}

public class AssetLinkJsonConverter<TAsset> : JsonConverter<IAssetLink<TAsset>?> where TAsset : Asset
{
    public override IAssetLink<TAsset>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return AssetLink.ReadJson<TAsset>(ref reader);
    }

    public override void Write(Utf8JsonWriter writer, IAssetLink<TAsset>? value, JsonSerializerOptions options)
    {
        AssetLink.WriteJson(writer, value);
    }
}

public class AssetLinkYamlConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return Array.Exists(type.GetInterfaces(), i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAssetLink<>));
    }

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        return AssetLink.ReadYaml(parser, type);
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        AssetLink.WriteYaml(emitter, value as IAssetLink<Asset>);
    }
}