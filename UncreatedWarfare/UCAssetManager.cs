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
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare;

public static class UCAssetManager
{
    private static readonly char[] Ignore = { '.', ',', '&', '-', '_' };
    private static readonly char[] Splits = { ' ' };
    public static ItemAsset? FindItemAsset(string itemName, out int numberOfSimilarNames, bool additionalCheckWithoutNonAlphanumericCharacters = true)
    {
        itemName = itemName.ToLower();
        string[] insplits = itemName.Split(Splits);

        numberOfSimilarNames = 0;
        ItemAsset? asset;
        List<ItemAsset> list = ListPool<ItemAsset>.claim();
        try
        {
            Assets.find(list);
            list.RemoveAll(k => !(k is { name: { }, itemName: { } } && (
                itemName.Equals(k.id.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase) ||
                insplits.All(l => k.itemName.IndexOf(l, StringComparison.OrdinalIgnoreCase) != -1))));

            list.Sort((a, b) => a.itemName.Length.CompareTo(b.itemName.Length));

            numberOfSimilarNames = list.Count;

            asset = numberOfSimilarNames < 1 ? null : list[0];
        }
        finally
        {
            ListPool<ItemAsset>.release(list);
        }

        if (asset == null && additionalCheckWithoutNonAlphanumericCharacters)
        {
            itemName = itemName.RemoveMany(false, Ignore);

            list = ListPool<ItemAsset>.claim();
            try
            {
                Assets.find(list);
                    
                list.RemoveAll(k => !(k is { name: { }, itemName: { } } && (
                    itemName.Equals(k.id.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase) ||
                    insplits.All(l => k.itemName.RemoveMany(false, Ignore).IndexOf(l, StringComparison.OrdinalIgnoreCase) != -1))));

                list.Sort((a, b) => a.itemName.Length.CompareTo(b.itemName.Length));

                numberOfSimilarNames = list.Count;
                asset = numberOfSimilarNames < 1 ? null : list[0];
            }
            finally
            {
                ListPool<ItemAsset>.release(list);
            }
        }

        numberOfSimilarNames--;

        return asset;
    }

    /// <summary>
    /// Returns the asset category (<see cref="EAssetType"/>) of <typeparamref name="TAsset"/>. Efficiently cached.
    /// </summary>
    [Pure]
    public static EAssetType GetAssetCategory<TAsset>() where TAsset : Asset => GetAssetCategoryCache<TAsset>.Category;

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

            string assetName2 = assetName.RemoveMany(false, Ignore);
            string[] inSplits = assetName2.Split(Splits);
            for (int i = 0; i < list.Count; ++i)
            {
                TAsset item = list[i];
                if (item is { FriendlyName: { } fn } &&
                    inSplits.All(l => fn.RemoveMany(false, Ignore).IndexOf(l, StringComparison.OrdinalIgnoreCase) != -1))
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
#if FALSE
    public static class NetCalls
    {
        /// <summary>Server-side</summary>
        public static async Task<AssetInfo[]?> SearchAssets<TAsset>(IConnection connection, string name) where TAsset : Asset
        {
            RequestResponse response = await RequestFindAssetByText.Request(SendFindAssets, connection, name, typeof(TAsset));
            response.TryGetParameter(0, out AssetInfo[]? info);
            return info;
        }
        /// <summary>Server-side</summary>
        public static async Task<AssetInfo[]?> SearchAssets<TAsset>(IConnection connection, ushort id) where TAsset : Asset
        {
            RequestResponse response = await RequestFindAssetById.Request(SendFindAssets, connection, id, typeof(TAsset));
            response.TryGetParameter(0, out AssetInfo[]? info);
            return info;
        }
        /// <summary>Server-side</summary>
        public static async Task<AssetInfo?> SearchAssets(IConnection connection, Guid id)
        {
            RequestResponse response = await RequestFindAssetByGuid.Request(SendFindAssets, connection, id);
            response.TryGetParameter(0, out AssetInfo[]? info);
            return info is { Length: > 0 } ? info[0] : null;
        }

        public static readonly NetCall<ushort, Type> RequestFindAssetById = new NetCall<ushort, Type>(ReceiveRequestAssetById);
        public static readonly NetCall<string, Type> RequestFindAssetByText = new NetCall<string, Type>(ReceiveRequestAssetByText);
        public static readonly NetCall<Guid> RequestFindAssetByGuid = new NetCall<Guid>(ReceiveRequestAssetByGuid);

        public static readonly NetCallRaw<AssetInfo[]> SendFindAssets = new NetCallRaw<AssetInfo[]>(KnownNetMessage.SendFindAssets, AssetInfo.ReadMany, AssetInfo.WriteMany);

        private static MethodInfo? _idMethod;
        private static MethodInfo? _textMethod;
        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.RequestFindAssetById)]
        private static void ReceiveRequestAssetById(MessageContext context, ushort id, Type assetType)
        {
            if (!Level.isLoaded)
                return;

            _idMethod ??= typeof(NetCalls).GetMethod(nameof(ReceiveRequestAssetByIdGeneric), BindingFlags.NonPublic | BindingFlags.Static)!;
            _idMethod.MakeGenericMethod(assetType).Invoke(null, new object[] { context, id });
        }
        private static void ReceiveRequestAssetByIdGeneric<TAsset>(MessageContext ctx, ushort id) where TAsset : Asset
        {
            EAssetType type = AssetTypeHelper<TAsset>.Type;
            if (type == EAssetType.NONE)
            {
                List<AssetInfo> assets = ListPool<AssetInfo>.claim();
                for (int i = 0; i <= 10; ++i)
                {
                    if (Assets.find((EAssetType)i, id) is { } asset)
                        assets.Add(new AssetInfo(asset));
                }

                ctx.Reply(SendFindAssets, assets.ToArray());
            }
            else
            {
                Asset? asset = Assets.find(type, id);
                AssetInfo[] info = asset == null ? Array.Empty<AssetInfo>() : new AssetInfo[] { new AssetInfo(asset) };

                ctx.Reply(SendFindAssets, info);
            }
        }
        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.RequestFindAssetByText)]
        private static void ReceiveRequestAssetByText(MessageContext context, string name, Type assetType)
        {
            if (!Level.isLoaded)
                return;

            _textMethod ??= typeof(NetCalls).GetMethod(nameof(ReceiveRequestAssetByTextGeneric), BindingFlags.NonPublic | BindingFlags.Static)!;
            _textMethod.MakeGenericMethod(assetType).Invoke(null, new object[] { context, name });
        }
        private static void ReceiveRequestAssetByTextGeneric<TAsset>(MessageContext ctx, string name) where TAsset : Asset
        {
            List<TAsset> assets = TryGetAssets<TAsset>(name, pool: true);
            AssetInfo[] info = new AssetInfo[assets.Count];
            try
            {
                for (int i = 0; i < assets.Count; ++i)
                    info[i] = new AssetInfo(assets[i]);
            }
            finally
            {
                ListPool<TAsset>.release(assets);
            }

            ctx.Reply(SendFindAssets, info);
        }
        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.RequestFindAssetByGuid)]
        private static void ReceiveRequestAssetByGuid(MessageContext context, Guid guid)
        {
            if (!Level.isLoaded)
                return;
            
            context.Reply(SendFindAssets, Assets.find(guid) is { } asset ? new AssetInfo[] { new AssetInfo(asset) } : Array.Empty<AssetInfo>());
        }
    }
#endif
    public static void SyncAssetsFromOrigin(AssetOrigin origin) => SyncAssetsFromOriginMethod?.Invoke(origin);
    public static void TryLoadAsset(string filePath, AssetOrigin origin)
    {
        try
        {
            LoadFile(filePath, origin);
        }
        catch (Exception ex)
        {
            L.LogError("Error loading asset from \"" + filePath + "\".");
            L.LogError(ex);
        }
    }

    private static readonly DatParser _parser = new DatParser();
    private static void GetData(string filePath, out DatDictionary assetData, out string? assetError, out byte[] hash, out DatDictionary? translationData, out DatDictionary? fallbackTranslationData)
    {
        GameThread.AssertCurrent();

        string directoryName = Path.GetDirectoryName(filePath)!;
        using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using SHA1Stream sha1Fs = new SHA1Stream(fs);
        using StreamReader input = new StreamReader(sha1Fs);

        assetData = _parser.Parse(input);
        assetError = _parser.ErrorMessage;
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
    public static DatDictionary ReadFileWithoutHash(string path)
    {
        GameThread.AssertCurrent();

        using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using StreamReader inputReader = new StreamReader(fileStream);
        return _parser.Parse(inputReader);
    }

    private static readonly Action<string, AssetOrigin> LoadFile;
    private static readonly Action<AssetOrigin> SyncAssetsFromOriginMethod;
    static UCAssetManager()
    {
        SyncAssetsFromOriginMethod = (Action<AssetOrigin>)typeof(Assets)
            .GetMethod("AddAssetsFromOriginToCurrentMapping", BindingFlags.Static | BindingFlags.NonPublic)?
            .CreateDelegate(typeof(Action<AssetOrigin>))!;
        if (SyncAssetsFromOriginMethod == null)
        {
            L.LogError("Assets.AddAssetsFromOriginToCurrentMapping not found or arguments changed.");
            return;
        }

        MethodInfo method = typeof(UCAssetManager).GetMethod(nameof(GetData), BindingFlags.Static | BindingFlags.NonPublic)!;
        Type? assetInfo = typeof(Assets).Assembly.GetType("SDG.Unturned.AssetsWorker+AssetDefinition", false, false);
        if (assetInfo == null)
        {
            L.LogError("AssetsWorker.AssetDefinition not found.");
            return;
        }

        MethodInfo? loadFileMethod = typeof(Assets).GetMethod("LoadFile", BindingFlags.NonPublic | BindingFlags.Static);
        if (loadFileMethod == null)
        {
            L.LogError("Assets.LoadFile not found.");
            return;
        }

        FieldInfo? pathField = assetInfo.GetField("path", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        FieldInfo? hashField = assetInfo.GetField("hash", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        FieldInfo? assetDataField = assetInfo.GetField("assetData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        FieldInfo? translationDataField = assetInfo.GetField("translationData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        FieldInfo? fallbackTranslationDataField = assetInfo.GetField("fallbackTranslationData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        FieldInfo? assetErrorField = assetInfo.GetField("assetError", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        FieldInfo? originField = assetInfo.GetField("origin", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (pathField == null || hashField == null || assetDataField == null || translationDataField == null || fallbackTranslationDataField == null || assetErrorField == null || originField == null)
        {
            L.LogError("Missing field in AssetsWorker.AssetDefinition.");
            return;
        }

        DynamicMethod dm = new DynamicMethod("LoadAsset", typeof(void), new Type[] { typeof(string), typeof(AssetOrigin) }, typeof(UCAssetManager).Module, true);
        ILGenerator generator = dm.GetILGenerator();
        dm.DefineParameter(0, ParameterAttributes.None, "path");
        dm.DefineParameter(1, ParameterAttributes.None, "assetOrigin");
        generator.DeclareLocal(typeof(DatDictionary));

        generator.DeclareLocal(typeof(string));
        generator.DeclareLocal(typeof(byte[]));
        generator.DeclareLocal(typeof(DatDictionary));
        generator.DeclareLocal(typeof(DatDictionary));
        generator.DeclareLocal(assetInfo);

        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Ldloca_S, 0);
        generator.Emit(OpCodes.Ldloca_S, 1);
        generator.Emit(OpCodes.Ldloca_S, 2);
        generator.Emit(OpCodes.Ldloca_S, 3);
        generator.Emit(OpCodes.Ldloca_S, 4);
        generator.Emit(OpCodes.Call, method);
        
        generator.Emit(OpCodes.Ldloca_S, 5);
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Stfld, pathField);

        generator.Emit(OpCodes.Ldloca_S, 5);
        generator.Emit(OpCodes.Ldloc_2);
        generator.Emit(OpCodes.Stfld, hashField);

        generator.Emit(OpCodes.Ldloca_S, 5);
        generator.Emit(OpCodes.Ldloc_0);
        generator.Emit(OpCodes.Stfld, assetDataField);

        generator.Emit(OpCodes.Ldloca_S, 5);
        generator.Emit(OpCodes.Ldloc_3);
        generator.Emit(OpCodes.Stfld, translationDataField);

        generator.Emit(OpCodes.Ldloca_S, 5);
        generator.Emit(OpCodes.Ldloc_S, 4);
        generator.Emit(OpCodes.Stfld, fallbackTranslationDataField);

        generator.Emit(OpCodes.Ldloca_S, 5);
        generator.Emit(OpCodes.Ldloc_1);
        generator.Emit(OpCodes.Stfld, assetErrorField);

        generator.Emit(OpCodes.Ldloca_S, 5);
        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Stfld, originField);

        generator.Emit(OpCodes.Ldloc_S, 5);
        generator.Emit(OpCodes.Call, loadFileMethod);

        generator.Emit(OpCodes.Ret);

        LoadFile = (Action<string, AssetOrigin>)dm.CreateDelegate(typeof(Action<string, AssetOrigin>));
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