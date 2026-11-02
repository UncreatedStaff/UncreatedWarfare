using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.SpeedBytes;
using SDG.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Util;

public static class AssetUtility
{
    private static readonly char[] Ignore = [ '.', ',', '&', '-', '_' ];
    private static readonly char[] Splits = [ ' ' ];
    
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="type"/> is not a valid value.</exception>
    public static EItemType GetItemType(this ClothingType type) => type switch
    {
        ClothingType.Shirt => EItemType.SHIRT,
        ClothingType.Pants => EItemType.PANTS,
        ClothingType.Vest => EItemType.VEST,
        ClothingType.Hat => EItemType.HAT,
        ClothingType.Mask => EItemType.MASK,
        ClothingType.Backpack => EItemType.BACKPACK,
        ClothingType.Glasses => EItemType.GLASSES,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public static TAsset? FindAsset<TAsset>(string name, out int numberOfSimilarNames, bool additionalCheckWithoutNonAlphanumericCharacters = true) where TAsset : Asset
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        name = name.ToLower();
        string[] words = name.Split(Splits);

        numberOfSimilarNames = 0;
        TAsset? asset;
        List<TAsset> list = ListPool<TAsset>.claim();
        try
        {
            Assets.find(list);
            list.RemoveAll(k => !(k is { name: not null, FriendlyName: not null } && (
                name.Equals(k.id.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase) ||
                words.All(l => k.FriendlyName.IndexOf(l, StringComparison.OrdinalIgnoreCase) != -1))));

            list.Sort((a, b) => a.FriendlyName.Length.CompareTo(b.FriendlyName.Length));

            numberOfSimilarNames = list.Count;

            asset = numberOfSimilarNames < 1 ? null : list[0];
        }
        finally
        {
            ListPool<TAsset>.release(list);
        }

        if (asset == null && additionalCheckWithoutNonAlphanumericCharacters)
        {
            name = StringUtility.RemoveMany(name, false, Ignore);

            list = ListPool<TAsset>.claim();
            try
            {
                Assets.find(list);

                list.RemoveAll(k => !(k is { name: not null, FriendlyName: not null } && (
                    name.Equals(k.id.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase) ||
                    words.All(l => StringUtility.RemoveMany(k.FriendlyName, false, Ignore).IndexOf(l, StringComparison.OrdinalIgnoreCase) != -1))));

                list.Sort((a, b) => a.FriendlyName.Length.CompareTo(b.FriendlyName.Length));

                numberOfSimilarNames = list.Count;
                asset = numberOfSimilarNames < 1 ? null : list[0];
            }
            finally
            {
                ListPool<TAsset>.release(list);
            }
        }

        numberOfSimilarNames--;

        return asset;
    }

    /// <summary>
    /// Returns the asset category (<see cref="EAssetType"/>) of <typeparamref name="TAsset"/>. Efficiently cached usually.
    /// </summary>
    [Pure]
    public static EAssetType GetAssetCategory<TAsset>(TAsset asset) where TAsset : Asset
    {
        EAssetType category = GetAssetCategoryCache<TAsset>.Category;
        if (category == EAssetType.NONE)
        {
            return GetAssetCategory(asset.GetType());
        }

        return category;
    }

    /// <summary>
    /// Returns the asset category (<see cref="EAssetType"/>) of <typeparamref name="TAsset"/>. Efficiently cached.
    /// </summary>
    [Pure]
    public static EAssetType GetAssetCategory<TAsset>() where TAsset : Asset
    {
        return GetAssetCategoryCache<TAsset>.Category;
    }

    /// <summary>
    /// Returns the asset category (<see cref="EAssetType"/>) of <paramref name="assetType"/>.
    /// </summary>
    [Pure]
    public static EAssetType GetAssetCategory(Type assetType)
    {
        if (typeof(ItemAsset).IsAssignableFrom(assetType))
        {
            return EAssetType.ITEM;
        }
        if (typeof(VehicleAsset).IsAssignableFrom(assetType))
        {
            return EAssetType.VEHICLE;
        }
        if (typeof(ObjectAsset).IsAssignableFrom(assetType))
        {
            return EAssetType.OBJECT;
        }
        if (typeof(EffectAsset).IsAssignableFrom(assetType))
        {
            return EAssetType.EFFECT;
        }
        if (typeof(AnimalAsset).IsAssignableFrom(assetType))
        {
            return EAssetType.ANIMAL;
        }
        if (typeof(SpawnAsset).IsAssignableFrom(assetType))
        {
            return EAssetType.SPAWN;
        }
        if (typeof(SkinAsset).IsAssignableFrom(assetType))
        {
            return EAssetType.SKIN;
        }
        if (typeof(MythicAsset).IsAssignableFrom(assetType))
        {
            return EAssetType.MYTHIC;
        }
        if (typeof(ResourceAsset).IsAssignableFrom(assetType))
        {
            return EAssetType.RESOURCE;
        }
        if (typeof(DialogueAsset).IsAssignableFrom(assetType) || typeof(QuestAsset).IsAssignableFrom(assetType) || typeof(VendorAsset).IsAssignableFrom(assetType))
        {
            return EAssetType.NPC;
        }

        return EAssetType.NONE;
    }
    public static bool TryGetAsset<TAsset>(string assetName, [NotNullWhen(true)] out TAsset? asset, out bool multipleResultsFound, bool allowMultipleResults = false, Predicate<TAsset>? selector = null) where TAsset : Asset
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        if (Guid.TryParse(assetName, out Guid guid))
        {
            asset = Assets.find<TAsset>(guid);

            multipleResultsFound = false;
            return asset is not null && (selector is null || selector(asset));
        }

        EAssetType type = GetAssetCategory<TAsset>();
        if (type != EAssetType.NONE)
        {
            if (ushort.TryParse(assetName, out ushort value))
            {
                if (Assets.find(type, value) is TAsset asset2)
                {
                    if (selector is not null && !selector(asset2))
                    {
                        asset = null;
                        multipleResultsFound = false;
                        return false;
                    }

                    asset = asset2;
                    multipleResultsFound = false;
                    return true;
                }
            }

            List<TAsset> list = ListPool<TAsset>.claim();
            try
            {
                Assets.find(list);

                if (selector != null)
                    list.RemoveAll(x => !selector(x));

                list.Sort((a, b) => a.FriendlyName.Length.CompareTo(b.FriendlyName.Length));

                if (allowMultipleResults)
                {
                    for (int i = 0; i < list.Count; ++i)
                    {
                        if (list[i].FriendlyName.Equals(assetName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            asset = list[i];
                            multipleResultsFound = false;
                            return true;
                        }
                    }
                    for (int i = 0; i < list.Count; ++i)
                    {
                        if (list[i].FriendlyName.IndexOf(assetName, StringComparison.InvariantCultureIgnoreCase) != -1)
                        {
                            asset = list[i];
                            multipleResultsFound = false;
                            return true;
                        }
                    }
                }
                else
                {
                    List<TAsset> results = ListPool<TAsset>.claim();
                    try
                    {
                        for (int i = 0; i < list.Count; ++i)
                        {
                            if (list[i].FriendlyName.Equals(assetName, StringComparison.InvariantCultureIgnoreCase))
                            {
                                results.Add(list[i]);
                            }
                        }
                        if (results.Count == 1)
                        {
                            asset = results[0];
                            multipleResultsFound = false;
                            return true;
                        }
                        if (results.Count > 1)
                        {
                            multipleResultsFound = true;
                            asset = results[0];
                            return false; // if multiple results match for the full name then a partial will be the same
                        }
                        for (int i = 0; i < list.Count; ++i)
                        {
                            if (list[i].FriendlyName.IndexOf(assetName, StringComparison.InvariantCultureIgnoreCase) != -1)
                            {
                                results.Add(list[i]);
                            }
                        }
                        if (results.Count == 1)
                        {
                            asset = results[0];
                            multipleResultsFound = false;
                            return true;
                        }
                        if (results.Count > 1)
                        {
                            multipleResultsFound = true;
                            asset = results[0];
                            return false;
                        }
                    }
                    finally
                    {
                        ListPool<TAsset>.release(results);
                    }
                }
            }
            finally
            {
                ListPool<TAsset>.release(list);
            }
        }
        multipleResultsFound = false;
        asset = null;
        return false;
    }
    public static List<TAsset> TryGetAssets<TAsset>(string assetName, Predicate<TAsset>? selector = null, bool pool = false) where TAsset : Asset
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        TAsset asset;
        List<TAsset> assets = pool ? ListPool<TAsset>.claim() : new List<TAsset>(4);
        if (Guid.TryParse(assetName, out Guid guid))
        {
            asset = Assets.find<TAsset>(guid);
            if (asset != null)
                assets.Add(asset);
            return assets;
        }
        EAssetType type = GetAssetCategory<TAsset>();
        if (type != EAssetType.NONE)
        {
            if (ushort.TryParse(assetName, out ushort value))
            {
                if (Assets.find(type, value) is TAsset asset2)
                {
                    if (selector is null || selector(asset2))
                        assets.Add(asset2);
                    return assets;
                }
            }
        }
        else if (ushort.TryParse(assetName, out ushort value))
        {
            for (int i = 0; i <= 10; ++i)
            {
                if (Assets.find((EAssetType)i, value) is TAsset asset2)
                {
                    if (selector is null || selector(asset2))
                        assets.Add(asset2);
                }
            }

            return assets;
        }

        List<TAsset> list = ListPool<TAsset>.claim();
        try
        {
            Assets.find(list);

            list.Sort((a, b) => a.FriendlyName.Length.CompareTo(b.FriendlyName.Length));

            for (int i = 0; i < list.Count; ++i)
            {
                TAsset item = list[i];
                if (item.FriendlyName.Equals(assetName, StringComparison.InvariantCultureIgnoreCase) && (selector == null || selector(item)))
                {
                    assets.Add(item);
                }
            }
            for (int i = 0; i < list.Count; ++i)
            {
                TAsset item = list[i];
                if (item.FriendlyName.IndexOf(assetName, StringComparison.InvariantCultureIgnoreCase) != -1 && (selector == null || selector(item)))
                {
                    assets.Add(item);
                }
            }

            string assetName2 = StringUtility.RemoveMany(assetName, false, Ignore);
            string[] inSplits = assetName2.Split(Splits);
            for (int i = 0; i < list.Count; ++i)
            {
                TAsset item = list[i];
                if (item is { FriendlyName: { } fn } &&
                    inSplits.All(l => StringUtility.RemoveMany(fn, false, Ignore).IndexOf(l, StringComparison.OrdinalIgnoreCase) != -1))
                {
                    assets.Add(item);
                }
            }
        }
        finally
        {
            ListPool<TAsset>.release(list);
        }

        return assets;
    }

    public static void SyncAssetsFromOrigin(AssetOrigin origin)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        Assets.AddAssetsFromOriginToCurrentMapping(origin);
    }

    /// <summary>
    /// Synchronously loads an asset file.
    /// </summary>
    /// <returns>A list of all errors.</returns>=
    public static string[]? LoadAsset(string filePath, AssetOrigin origin)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        string[]? errorRtn = null;
        List<string> errors = ListPool<string>.claim();
        try
        {
            AssetsWorker.AssetDefinition def = new AssetsWorker.AssetDefinition
            {
                path = filePath,
                origin = origin,
                assetErrors = errors
            };
            GetData(filePath, out def.assetData, errors, out def.hash, out def.translationData, out def.fallbackTranslationData);
            Assets.LoadFile(def);
            if (errors.Count > 0)
                errorRtn = errors.ToArray();
        }
        finally
        {
            ListPool<string>.release(errors);
        }

        return errorRtn;
    }

    private static readonly DatParser Parser = new DatParser();

    private static void GetData(string filePath, out IDatDictionary assetData, List<string>? assetErrors, out byte[] hash, out IDatDictionary? translationData, out IDatDictionary? fallbackTranslationData)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        GameThread.AssertCurrent();

        string directoryName = Path.GetDirectoryName(filePath)!;
        using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using SHA1Stream sha1Fs = new SHA1Stream(fs);
        using StreamReader input = new StreamReader(sha1Fs);

        assetData = Parser.Parse(input);
        if (Parser.ErrorMessages is { Count: > 0 } && assetErrors != null)
        {
            assetErrors.AddRange(Parser.ErrorMessages);
        }

        hash = sha1Fs.Hash;
        string localLang = Path.Combine(directoryName, Provider.language + ".dat");
        string englishLang = Path.Combine(directoryName, "English.dat");
        translationData = null;
        fallbackTranslationData = null;
        if (File.Exists(localLang))
        {
            translationData = ReadFileWithoutHash(localLang);
            if (!Provider.language.Equals("English", StringComparison.Ordinal) && File.Exists(englishLang))
                fallbackTranslationData = ReadFileWithoutHash(englishLang);
        }
        else if (File.Exists(englishLang))
            translationData = ReadFileWithoutHash(englishLang);
    }
    public static IDatDictionary ReadFileWithoutHash(string path)
    {
        GameThread.AssertCurrent();

        using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using StreamReader inputReader = new StreamReader(fileStream);
        return Parser.Parse(inputReader);
    }

    private static class GetAssetCategoryCache<TAsset> where TAsset : Asset
    {
        public static readonly EAssetType Category = GetAssetCategory(typeof(TAsset));
    }
}

public readonly struct AssetInfo
{
    private const ushort DataVersion = 0;
    public readonly EAssetType Category;
    public readonly EItemType? Type;
    public readonly Type AssetType;
    public readonly Guid Guid;
    public readonly ushort Id;
    public readonly string AssetName;
    public readonly string? EnglishName;
    public readonly string? ItemDescription;
    public AssetInfo(Asset asset)
    {
        Category = asset.assetCategory;
        AssetType = asset.GetType();
        Guid = asset.GUID;
        Id = asset.id;
        AssetName = asset.name;
        EnglishName = asset.FriendlyName;
        if (asset is ItemAsset item)
        {
            Type = item.type;
            ItemDescription = item.itemDescription;
        }
        else
        {
            Type = null;
            ItemDescription = null;
        }
    }
    public AssetInfo(EAssetType category, EItemType? type, Type assetType, Guid guid, ushort id, string assetName, string englishName, string? itemDescription)
    {
        Category = category;
        Type = type;
        AssetType = assetType;
        Guid = guid;
        Id = id;
        AssetName = assetName;
        EnglishName = englishName;
        ItemDescription = itemDescription;
    }
    public AssetInfo(ByteReader reader)
    {
        _ = reader.ReadUInt16();
        Category = (EAssetType)reader.ReadUInt8();
        Type = reader.ReadBool() ? (EItemType)reader.ReadUInt16() : null;
        AssetType = reader.ReadType()!;
        Guid = reader.ReadGuid();
        Id = reader.ReadUInt16();
        AssetName = reader.ReadString();
        EnglishName = reader.ReadNullableString();
        ItemDescription = reader.ReadNullableString();
    }
    public void Write(ByteWriter writer)
    {
        writer.Write(DataVersion);
        writer.Write((byte)Category);
        writer.Write(Type.HasValue);
        if (Type.HasValue)
            writer.Write((ushort)Type.Value);
        writer.Write(AssetType);
        writer.Write(Guid);
        writer.Write(Id);
        writer.Write(AssetName);
        writer.WriteNullable(EnglishName);
        writer.WriteNullable(ItemDescription);
    }

    public static void Write(ByteWriter writer, AssetInfo info) => info.Write(writer);
    public static AssetInfo Read(ByteReader reader) => new AssetInfo(reader);
    public static void WriteMany(ByteWriter writer, AssetInfo[] info)
    {
        writer.Write(info.Length);
        for (int i = 0; i < info.Length; ++i)
            info[i].Write(writer);
    }
    public static AssetInfo[] ReadMany(ByteReader reader)
    {
        AssetInfo[] rtn = new AssetInfo[reader.ReadInt32()];
        for (int i = 0; i < rtn.Length; ++i)
            rtn[i] = new AssetInfo(reader);
        return rtn;
    }
}