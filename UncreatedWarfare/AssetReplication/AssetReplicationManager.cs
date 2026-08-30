#if DEBUG
//#define NO_COMPRESS
#endif

using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.SpeedBytes;
using System;
using System.Globalization;
using System.IO;
#if !NO_COMPRESS
using System.IO.Compression;
#endif
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.AssetReplication;

[GenerateRpcSource]
public partial class AssetReplicationManager : ILevelHostedService, IAsyncEventListener<HomebaseConnected>
{
    private readonly WarfareModule _module;
    private readonly ILogger<AssetReplicationManager> _logger;

    private readonly string _cacheFileLocation;

    private bool _hasGenerated;

    private byte[] _sha256Hash;


    public AssetReplicationManager(WarfareModule module, ILogger<AssetReplicationManager> logger)
    {
        _module = module;
        _logger = logger;

        _cacheFileLocation = Path.Combine(_module.HomeDirectory, "Cache", "Asset Database.bin");
        _sha256Hash = new byte[32];
    }

    public async UniTask LoadLevelAsync(CancellationToken token)
    {
        List<Asset> allAssets = new List<Asset>(8192);
        Assets.find(allAssets);

        CargoBuilder bldr = new CargoBuilder();

        string? dir = Path.GetDirectoryName(_cacheFileLocation);
        if (dir != null) Directory.CreateDirectory(dir);

        await using FileStream fs = new FileStream(_cacheFileLocation, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

        const int headerSize = 33;

        Span<byte> headerBlock = stackalloc byte[headerSize];
#if NO_COMPRESS
        headerBlock[32] = 0;
#else
        headerBlock[32] = 1;
#endif
        fs.Write(headerBlock);

#if !NO_COMPRESS
        await using DeflateStream deflate = new DeflateStream(fs, CompressionMode.Compress, true);
#else
        Stream deflate = fs;
#endif

        SHA256 sha256 = SHA256.Create();

        ByteWriter writer = new ByteWriter { Stream = deflate };

        HashSet<Type> allTypes = new HashSet<Type>(256);

        writer.Write(0);

        foreach (Asset asset in allAssets)
        {
            Type assetType = asset.GetType();
            for (Type? t = assetType; t != typeof(Asset) && t != null; t = t.BaseType)
            {
                allTypes.Add(t);
            }
        }

        List<Type> parentTypes = new List<Type>();
        TypeTree typeTreeRoot = new TypeTree
        {
            Type = typeof(Asset)
        };
        ushort typeId = 0;
        PopulateTree(typeTreeRoot, parentTypes, allTypes, ref typeId);
        if (allTypes.Count != 0)
            throw new InvalidOperationException($"{allTypes.Count} type(s) not anchored to Asset.");

        writer.Write(typeId);

        writer.Write(checked( (ushort)typeTreeRoot.ChildTypes.Length ));
        foreach (TypeTree tree in typeTreeRoot.ChildTypes)
        {
            tree.Write(writer);
        }

        ValueCache valueCache = new ValueCache();

        writer.Write(allAssets.Count);
        foreach (Asset asset in allAssets)
        {
            valueCache.WriteValue(writer, asset.GUID);

            EAssetType category = asset.assetCategory;
            writer.Write((byte)category);
            if (category != EAssetType.NONE)
                writer.Write(asset.id);

            Type assetType = asset.GetType();
            int tId = typeTreeRoot.GetTypeId(assetType);
            if (tId <= 0)
                throw new InvalidOperationException($"Type not found: {assetType}.");

            writer.Write((ushort)(tId - 1));
            valueCache.WriteValue(writer, asset.name);
            if ((object)asset.name == asset.FriendlyName)
            {
                writer.Write((byte)0);
            }
            else if (string.Equals(asset.name, asset.FriendlyName, StringComparison.Ordinal))
            {
                writer.Write((byte)1);
            }
            else
            {
                writer.Write((byte)2);
                valueCache.WriteValue(writer, asset.FriendlyName);
            }

            valueCache.WriteKey(writer, asset.GetOriginName());

            asset.BuildCargoData(bldr);

            Dictionary<string, List<CargoDeclaration>> declarationGroups = bldr.declarations;

            writer.Write(checked ( (ushort)declarationGroups.Count ));
            foreach ((string name, List<CargoDeclaration> declarations) in declarationGroups)
            {
                ushort keyId = valueCache.GetOrAddKey(name);
                writer.Write(keyId);
                writer.Write(checked ( (ushort)declarations.Count ));
                foreach (CargoDeclaration declaration in declarations)
                {
                    List<string> lines = declaration.lines;
                    writer.Write(checked( (ushort)lines.Count ));
                    foreach (string line in lines)
                    {
                        if (!line.StartsWith("| ", StringComparison.Ordinal))
                        {
                            valueCache.WriteKey(writer, string.Empty);
                            continue;
                        }
                        
                        int endIndex = line.IndexOf(" = ", 2, StringComparison.Ordinal);
                        if (endIndex < 0)
                        {
                            valueCache.WriteKey(writer, string.Empty);
                            continue;
                        }

                        string key = line[2..endIndex];
                        valueCache.WriteKey(writer, key);
                        if (key.Length == 0)
                            continue;

                        valueCache.WriteValue(writer, line[(endIndex + 3)..]);
                    }
                }
            }

            bldr.Clear();
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug($"{valueCache.Count} unique values discovered.");
            _logger.LogDebug($" * {valueCache.Keys.Count} keys discovered.");
            _logger.LogDebug($" * {valueCache.Strings.Count} strings discovered.");
            _logger.LogDebug($" * {valueCache.Guids.Count} GUIDs discovered.");
            _logger.LogDebug($" * {valueCache.Floats.Count} floats discovered.");
            _logger.LogDebug($" * {valueCache.I32s.Count} int32s discovered.");
            _logger.LogDebug($" * {valueCache.I64s.Count} int64s discovered.");
            _logger.LogDebug($" * {valueCache.Colors.Count} colors discovered.");
#if DEBUG
            _logger.LogDebug($" * {(valueCache.TrueFound ? 1 : 0) + (valueCache.FalseFound ? 1 : 0)} bools discovered.");
            _logger.LogDebug($" * {valueCache.I8s.Count} int8s discovered.");
            _logger.LogDebug($" * {valueCache.I16s.Count} int16s discovered.");
#endif
        }

        valueCache.WriteCache(writer);

        writer.Flush();
        await deflate.FlushAsync(token);

        fs.Seek(headerSize, SeekOrigin.Begin);

        byte[] hash = sha256.ComputeHash(fs);

        fs.Seek(0L, SeekOrigin.Begin);
        
        await fs.WriteAsync(hash, 0, 32, token);

        fs.Seek(0L, SeekOrigin.Begin);

        _sha256Hash = hash;
        bool isRemoteUpdated = false;
        try
        {
            isRemoteUpdated = await SendAssetDatabaseHash(hash);
        }
        catch (RpcNoConnectionsException) { return; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replicating asset info.");
        }

        if (isRemoteUpdated)
        {
            _hasGenerated = true;
            return;
        }

        try
        {
            await SendAssetDatabase(fs).IgnoreNoConnections();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replicating asset info.");
        }

        _hasGenerated = true;
    }

    private static void WriteAssetType(Type assetType, ByteWriter writer)
    {
        byte typeFlag = 0;
        if (assetType.Assembly == typeof(Provider).Assembly)
        {
            typeFlag |= 1;
            string? fullName = assetType.FullName;
            if (fullName != null && fullName.StartsWith("SDG.Unturned.", StringComparison.Ordinal))
            {
                typeFlag |= 2;
                writer.Write(fullName.AsSpan(13));
            }
            else
            {
                writer.Write(fullName ?? assetType.Name);
            }
        }
        else
        {
            writer.Write(assetType.AssemblyQualifiedName ?? assetType.FullName ?? assetType.Name);
        }

        writer.Write(typeFlag);
    }

    [RpcTimeout(5 * Timeouts.Seconds)]
    [RpcSend("Uncreated.Web.Client.Unturned.AssetDatabase, Uncreated.Web.Client", "ReceiveAssetDatabaseHash")]
    private partial RpcTask<bool> SendAssetDatabaseHash(byte[] hash);

    [RpcTimeout(1 * Timeouts.Minutes)]
    [RpcSend("Uncreated.Web.Client.Unturned.AssetDatabase, Uncreated.Web.Client", "ReceiveAssetDatabase", Raw = true)]
    private partial RpcTask SendAssetDatabase(Stream fileStream);

    public async UniTask HandleEventAsync(HomebaseConnected e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        if (!_hasGenerated)
            return;

        bool isRemoteUpdated = false;
        try
        {
            isRemoteUpdated = await SendAssetDatabaseHash(_sha256Hash);
        }
        catch (RpcNoConnectionsException) { return; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replicating asset info.");
        }

        if (isRemoteUpdated)
        {
            _logger.LogInformation("Homebase already has the latest asset info. Skipping sending it.");
            return;
        }

        try
        {
            await using FileStream fs = new FileStream(_cacheFileLocation, FileMode.Open, FileAccess.Read, FileShare.Read);

            _logger.LogInformation("Sending updated asset info to homebase...");
            await SendAssetDatabase(fs).IgnoreNoConnections();
        }
        catch (FileNotFoundException) { }
        catch (DirectoryNotFoundException) { }
    }

    private static void PopulateTree(TypeTree tree, List<Type> parentTypes, HashSet<Type> allTypes, ref ushort typeId)
    {
        foreach (Type type in allTypes)
        {
            if (tree.Type == type.BaseType)
                parentTypes.Add(type);
        }

        TypeTree[] array = new TypeTree[parentTypes.Count];
        for (int i = 0; i < array.Length; ++i)
        {
            array[i] = new TypeTree { Type = parentTypes[i] };
            allTypes.Remove(array[i].Type);
        }

        tree.ChildTypes = array;
        tree.Id = checked ( ++typeId );

        parentTypes.Clear();
        for (int i = 0; i < array.Length; ++i)
        {
            PopulateTree(array[i], parentTypes, allTypes, ref typeId);
        }
    }

    private class TypeTree
    {
#nullable disable

        public Type Type;
        public ushort Id;
        public TypeTree[] ChildTypes;

#nullable restore

        public void Write(ByteWriter writer)
        {
            WriteAssetType(Type, writer);
            writer.Write(checked( (ushort)ChildTypes.Length) );
            foreach (TypeTree tree in ChildTypes)
            {
                tree.Write(writer);
            }
        }

        public int GetTypeId(Type type)
        {
            if (type == Type)
                return Id;

            for (int i = 0; i < ChildTypes.Length; ++i)
            {
                int id = ChildTypes[i].GetTypeId(type);
                if (id >= 0)
                    return id;
            }

            return -1;
        }
    }

    [SkipLocalsInit]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private class ValueCache
    {
        public readonly Dictionary<string, ushort> Keys = new Dictionary<string, ushort>(1024, StringComparer.Ordinal);
        public readonly Dictionary<string, uint> Strings = new Dictionary<string, uint>(16384, StringComparer.Ordinal);
        public readonly Dictionary<Guid, uint> Guids = new Dictionary<Guid, uint>(16384);
        public readonly Dictionary<float, uint> Floats = new Dictionary<float, uint>(1024);
        public readonly Dictionary<uint, uint> I32s = new Dictionary<uint, uint>(16);
        public readonly Dictionary<ulong, uint> I64s = new Dictionary<ulong, uint>(16);
        public readonly Dictionary<uint, uint> Colors = new Dictionary<uint, uint>(128);
#if DEBUG
        public readonly HashSet<byte> I8s = new HashSet<byte>(65535);
        public readonly HashSet<ushort> I16s = new HashSet<ushort>(65535);
        public bool TrueFound, FalseFound;
#endif

        private const int TypeBits = 4;
        private const uint TypeMask = 0b1111;
        // private const uint ValueMask = unchecked ( (uint)~TypeMask );
        // private const int ValueBits = 24 - TypeBits;

        private const uint TypeString = 0b0001;
        private const uint TypeGuid   = 0b0010;
        private const uint TypeFloat  = 0b0011;
        private const uint TypeI8     = 0b0100;
        private const uint TypeU8     = 0b0101;
        private const uint TypeI16    = 0b0110;
        private const uint TypeU16    = 0b0111;
        private const uint TypeI32    = 0b1000;
        private const uint TypeU32    = 0b1001;
        private const uint TypeI64    = 0b1010;
        private const uint TypeU64    = 0b1011;
        private const uint TypeColor  = 0b1100;
        private const uint TypeTrue   = 0b1101;
        private const uint TypeFalse  = 0b1110;

        private const byte KeyOffset = 1;
        private const byte StringOffset = 1;
        private const byte GuidOffset = 1;
        private const byte I32Offset = 1;
        private const byte I64Offset = 1;
        private const byte FloatOffset = 4;
        private const byte ColorOffset = 0;

        public uint Count;

        public void WriteKey(ByteWriter writer, string key)
        {
            ushort id = GetOrAddKey(key);
            writer.Write(id);
        }

        public void WriteValue(ByteWriter writer, Guid guid)
        {
            uint id = GetOrAddValue(guid);
            writer.Write(unchecked( (byte)id ));
            writer.Write(unchecked( (ushort)(id >>> 8) ));
        }

        public void WriteValue(ByteWriter writer, sbyte i8)
        {
            uint id = GetOrAddValue(unchecked( (byte)i8 ), true);
            writer.Write(unchecked( (byte)id ));
            writer.Write(unchecked( (byte)(id >>> 8) ));
        }

        public void WriteValue(ByteWriter writer, bool value)
        {
            uint id = GetOrAddValue(value);
            writer.Write(unchecked( (byte)id ));
        }

        public void WriteValue(ByteWriter writer, byte u8)
        {
            uint id = GetOrAddValue(u8, false);
            writer.Write(unchecked( (byte)id ));
            writer.Write(unchecked( (byte)(id >>> 8) ));
        }

        public void WriteValue(ByteWriter writer, short i16)
        {
            uint id = GetOrAddValue(unchecked( (ushort)i16 ), true);
            writer.Write(unchecked( (byte)id ));
            writer.Write(unchecked( (ushort)(id >>> 8) ));
        }

        public void WriteValue(ByteWriter writer, ushort u16)
        {
            uint id = GetOrAddValue(u16, false);
            writer.Write(unchecked( (byte)id ));
            writer.Write(unchecked( (ushort)(id >>> 8) ));
        }

        public void WriteValue(ByteWriter writer, int i32)
        {
            uint id = GetOrAddValue(unchecked( (uint)i32 ), true);
            writer.Write(unchecked( (byte)id ));
            writer.Write(unchecked( (ushort)(id >>> 8) ));
        }

        public void WriteValue(ByteWriter writer, uint u32)
        {
            uint id = GetOrAddValue(u32, false);
            writer.Write(unchecked( (byte)id ));
            writer.Write(unchecked( (ushort)(id >>> 8) ));
        }

        public void WriteValue(ByteWriter writer, long i64)
        {
            uint id = GetOrAddValue(unchecked( (ulong)i64 ), true);
            writer.Write(unchecked( (byte)id ));
            writer.Write(unchecked( (ushort)(id >>> 8) ));
        }

        public void WriteValue(ByteWriter writer, ulong u64)
        {
            uint id = GetOrAddValue(u64, false);
            writer.Write(unchecked( (byte)id ));
            writer.Write(unchecked( (ushort)(id >>> 8) ));
        }

        public void WriteValue(ByteWriter writer, float r32)
        {
            uint id = GetOrAddValue(r32);
            writer.Write(unchecked( (byte)id ));
            writer.Write(unchecked( (ushort)(id >>> 8) ));
        }

        public void WriteValue(ByteWriter writer, Color32 color)
        {
            uint id = GetOrAddValue(color);
            writer.Write(unchecked( (byte)id ));
            writer.Write(unchecked( (ushort)(id >>> 8) ));
        }

        public void WriteValue(ByteWriter writer, string value)
        {
            uint id = GetOrAddValue(value);

            uint type = id & TypeMask;

            writer.Write(unchecked( (byte)id ));
            switch (type)
            {
                case TypeTrue:
                case TypeFalse:
                    break;

                case TypeI8:
                case TypeU8:
                    writer.Write(unchecked( (byte)(id >>> 8) ));
                    break;

                default:
                    writer.Write(unchecked( (ushort)(id >>> 8) ));
                    break;
            }
        }

        public void WriteCache(ByteWriter writer)
        {
            writer.Write(KeyOffset);
            ushort ct = (ushort)Math.Min(ushort.MaxValue, Keys.Count);
            writer.Write(ct);
            string[] ordered = new string[Keys.Count];
#if DEBUG
            BitArray mask = new BitArray(Keys.Count);
#endif
            foreach (KeyValuePair<string, ushort> kvp in Keys)
            {
                uint index = (uint)(kvp.Value - KeyOffset);
                ordered[index] = kvp.Key;
#if DEBUG
                mask[(int)index] = true;
#endif
            }

#if DEBUG
            for (int i = 0; i < ct; ++i)
            {
                if (!mask[i])
                    throw new InvalidProgramException($"Hole in ID range (keys): {i}.");
            }
#endif

            for (int i = 0; i < ct; ++i)
            {
                writer.Write(ordered[i]);
            }

            WriteDictionary(Guids, GuidOffset, writer);
            WriteDictionary(Floats, FloatOffset, writer);
            WriteDictionary(I32s, I32Offset, writer);
            WriteDictionary(I64s, I64Offset, writer);
            WriteDictionary(Colors, ColorOffset, writer);
            WriteDictionary(Strings, StringOffset, writer);

            return;
#if !DEBUG
            static
#endif
            void WriteDictionary<T>(Dictionary<T, uint> dict, byte offset, ByteWriter writer)
            {
                Writer<T> write = ByteWriter.GetWriteMethodDelegate<T>();
                writer.Write(offset);
                uint ct = (uint)dict.Count;
                ct = Math.Min(ct, ByteEncoders.Int24MaxValue * 2);
                writer.WriteUInt24(ct);
                T[] ordered = new T[dict.Count];
#if DEBUG
                BitArray mask = new BitArray(dict.Count);
#endif
                foreach (KeyValuePair<T, uint> kvp in dict)
                {
                    uint index = kvp.Value - offset;
                    ordered[index] = kvp.Key;
#if DEBUG
                    mask[(int)index] = true;
#endif
                }

#if DEBUG
                for (int i = 0; i < mask.Length; ++i)
                {
                    if (!mask[i])
                        throw new InvalidProgramException($"Hole in ID range ({((object)dict == Colors ? "Color32" : typeof(T).Name)}): {i}.");
                }
#endif

                for (int i = 0; i < ct; ++i)
                {
                    write(writer, ordered[i]);
                }
            }
        }

        public ushort GetOrAddKey(string key)
        {
            if (key.Length == 0)
                return 0;

            if (Keys.TryGetValue(key, out ushort k))
            {
                return k;
            }

            k = checked( (ushort)(Keys.Count + KeyOffset) );
            Keys[key] = k;
            ++Count;
            return k;
        }

        public uint GetOrAddValue(string value)
        {
            if (value.Length == 0)
                return TypeString;

            if (Strings.TryGetValue(value, out uint stringId))
            {
                return (stringId << TypeBits) | TypeString;
            }

            ReadOnlySpan<char> valueTrimmed = value.AsSpan().Trim();

            if (valueTrimmed.Length > 0)
            {
                if (valueTrimmed.Length >= 32 && Guid.TryParse(valueTrimmed, out Guid guid))
                {
                    return GetOrAddValue(guid);
                }

                if (valueTrimmed.Length == 7
                    && valueTrimmed[0] == '#'
                    && HexStringHelper.TryParseColor32(valueTrimmed, CultureInfo.InvariantCulture, out Color32 c32))
                {
                    return GetOrAddValue(c32);
                }

                if (valueTrimmed.Equals("yes", StringComparison.Ordinal))
                {
                    return GetOrAddValue(true);
                }
                if (valueTrimmed.Equals("no", StringComparison.Ordinal))
                {
                    return GetOrAddValue(false);
                }

                // (####) also means negative
                if (valueTrimmed.IndexOfAny([ '.', 'e', 'E', '∞' ]) >= 0
                    || valueTrimmed.Contains("Infinity", StringComparison.OrdinalIgnoreCase)
                    || valueTrimmed.Contains("NaN", StringComparison.OrdinalIgnoreCase))
                {
                    if (float.TryParse(valueTrimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out float dec) && dec is <= 16777216 and >= -16777216)
                    {
                        return GetOrAddValue(dec);
                    }
                }
                else if ((valueTrimmed[0] == '-') ^ (valueTrimmed[^1] == '-') || (valueTrimmed[0] == '(' && valueTrimmed[^1] == ')'))
                {
                    if (sbyte.TryParse(valueTrimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out sbyte i8))
                    {
                        return GetOrAddValue(unchecked( (byte)i8 ), true);
                    }
                    if (short.TryParse(valueTrimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out short i16))
                    {
                        return GetOrAddValue(unchecked( (ushort)i16 ), true);
                    }
                    if (int.TryParse(valueTrimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out int i32))
                    {
                        return GetOrAddValue(unchecked( (uint)i32 ), true);
                    }
                    if (long.TryParse(valueTrimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out long i64))
                    {
                        return GetOrAddValue(unchecked( (ulong)i64 ), true);
                    }
                }
                else
                {
                    if (byte.TryParse(valueTrimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out byte u8))
                    {
                        return GetOrAddValue(u8, false);
                    }
                    if (ushort.TryParse(valueTrimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out ushort u16))
                    {
                        return GetOrAddValue(u16, false);
                    }
                    if (uint.TryParse(valueTrimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out uint u32))
                    {
                        return GetOrAddValue(u32, false);
                    }
                    if (ulong.TryParse(valueTrimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out ulong u64))
                    {
                        return GetOrAddValue(u64, false);
                    }
                }
            }

            stringId = (uint)(Strings.Count + StringOffset);
            Strings[value] = stringId;
            ++Count;
            return (stringId << TypeBits) | TypeString;
        }

        public uint GetOrAddValue(byte u8, bool signed)
        {
#if DEBUG
            I8s.Add(u8);
#endif
            return ((uint)u8 << TypeBits) | (signed ? TypeI8 : TypeU8);
        }

        public uint GetOrAddValue(ushort u16, bool signed)
        {
#if DEBUG
            I16s.Add(u16);
#endif
            return ((uint)u16 << TypeBits) | (signed ? TypeI16 : TypeU16);
        }

        public uint GetOrAddValue(bool value)
        {
#if DEBUG
            if (value) TrueFound = true;
            else FalseFound = true;
#endif
            return value ? TypeTrue : TypeFalse;
        }

        public uint GetOrAddValue(uint u32, bool signed)
        {
            if (u32 == 0)
            {
                return signed ? TypeI32 : TypeU32;
            }

            if (I32s.TryGetValue(u32, out uint id))
            {
                return (id << TypeBits) | (signed ? TypeI32 : TypeU32);
            }

            id = (uint)(I32s.Count + I32Offset);
            I32s[u32] = id;
            ++Count;
            return (id << TypeBits) | (signed ? TypeI32 : TypeU32);
        }

        public uint GetOrAddValue(ulong u64, bool signed)
        {
            if (u64 == 0)
                return signed ? TypeI64 : TypeU64;

            if (I64s.TryGetValue(u64, out uint id))
            {
                return (id << TypeBits) | (signed ? TypeI64 : TypeU64);
            }

            id = (uint)(I64s.Count + I64Offset);
            I64s[u64] = id;
            ++Count;
            return (id << TypeBits) | (signed ? TypeI64 : TypeU64);
        }

        public uint GetOrAddValue(Guid guid)
        {
            if (guid == Guid.Empty)
                return TypeGuid;

            if (Guids.TryGetValue(guid, out uint id))
            {
                return (id << TypeBits) | TypeGuid;
            }

            id = (uint)(Guids.Count + GuidOffset);
            Guids[guid] = id;
            ++Count;
            return (id << TypeBits) | TypeGuid;
        }

        public uint GetOrAddValue(float r32)
        {
            if (r32 == 0) return TypeFloat;
            if (!float.IsFinite(r32))
            {
                if (float.IsNaN(r32)) return (1 << TypeBits) | TypeFloat;
                if (float.IsPositiveInfinity(r32)) return (2 << TypeBits) | TypeFloat;
                if (float.IsNegativeInfinity(r32)) return (3 << TypeBits) | TypeFloat;
            }

            if (Floats.TryGetValue(r32, out uint id))
            {
                return (id << TypeBits) | TypeFloat;
            }

            id = (uint)(Floats.Count + FloatOffset);
            Floats[r32] = id;
            ++Count;
            return (id << TypeBits) | TypeFloat;
        }

        public unsafe uint GetOrAddValue(Color32 c32)
        {
            uint rgba = *(uint*)&c32;
            if (Colors.TryGetValue(rgba, out uint id))
            {
                return (id << TypeBits) | TypeColor;
            }

            id = (uint)(Colors.Count + ColorOffset);
            Colors[rgba] = id;
            ++Count;
            return (id << TypeBits) | TypeColor;
        }
    }
}