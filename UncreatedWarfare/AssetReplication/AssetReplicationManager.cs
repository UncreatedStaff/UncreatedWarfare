using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.SpeedBytes;
using System;
using System.IO;
using System.Security.Cryptography;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Services;

namespace Uncreated.Warfare.AssetReplication;

[GenerateRpcSource]
public partial class AssetReplicationManager : ILevelHostedService, IAsyncEventListener<HomebaseConnected>
{
    private readonly WarfareModule _module;
    private readonly ILogger<AssetReplicationManager> _logger;

    private readonly string _cacheFileLocation;


    public AssetReplicationManager(WarfareModule module, ILogger<AssetReplicationManager> logger)
    {
        _module = module;
        _logger = logger;

        _cacheFileLocation = Path.Combine(_module.HomeDirectory, "Cache", "Asset Database.bin");
    }

    public async UniTask LoadLevelAsync(CancellationToken token)
    {
        List<Asset> allAssets = new List<Asset>(8192);
        Assets.find(allAssets);

        CargoBuilder bldr = new CargoBuilder();

        string? dir = Path.GetDirectoryName(_cacheFileLocation);
        if (dir != null) Directory.CreateDirectory(dir);

        await using FileStream fs = new FileStream(_cacheFileLocation, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

        SHA256 sha256 = SHA256.Create();

        ByteWriter writer = new ByteWriter { Stream = fs };

        HashSet<Type> allTypes = new HashSet<Type>(256);

        writer.Write(0);
        writer.WriteBlock(0, 32);

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

        writer.Write(allAssets.Count);
        foreach (Asset asset in allAssets)
        {
            writer.Write(asset.GUID);

            EAssetType category = asset.assetCategory;
            writer.Write((byte)category);
            if (category != EAssetType.NONE)
                writer.Write(asset.id);

            Type assetType = asset.GetType();
            int tId = typeTreeRoot.GetTypeId(assetType);
            if (tId <= 0)
                throw new InvalidOperationException($"Type not found: {assetType}.");

            writer.Write((ushort)(tId - 1));
            writer.Write(asset.name);
            if ((object)asset.name == asset.FriendlyName)
            {
                writer.Write(false);
            }
            else
            {
                writer.Write(true);
                writer.Write(asset.FriendlyName);
            }

            asset.BuildCargoData(bldr);

            Dictionary<string, List<CargoDeclaration>> declarationGroups = bldr.declarations;

            writer.Write(checked ( (ushort)declarationGroups.Count ));
            foreach ((string name, List<CargoDeclaration> declarations) in declarationGroups)
            {
                writer.Write(name);
                writer.Write(checked ( (ushort)declarations.Count ));
                foreach (CargoDeclaration declaration in declarations)
                {
                    List<string> lines = declaration.lines;
                    writer.Write(checked( (ushort)lines.Count ));
                    foreach (string line in lines)
                    {
                        if (!line.StartsWith("| ", StringComparison.Ordinal))
                        {
                            writer.Write(string.Empty);
                            continue;
                        }
                        
                        int endIndex = line.IndexOf(" = ", 2, StringComparison.Ordinal);
                        if (endIndex < 0)
                        {
                            writer.Write(string.Empty);
                            continue;
                        }

                        ReadOnlySpan<char> key = line.AsSpan(2, endIndex - 2);
                        writer.Write(key);
                        if (key.Length == 0)
                            continue;

                        ReadOnlySpan<char> value = line.AsSpan(endIndex + 3);
                        writer.Write(value);
                    }
                }
            }

            bldr.Clear();
        }

        writer.Flush();

        fs.Seek(0L, SeekOrigin.Begin);

        byte[] hash = sha256.ComputeHash(fs);

        fs.Seek(4L, SeekOrigin.Begin);
        
        await fs.WriteAsync(hash, 0, 32, token);

        fs.Seek(0L, SeekOrigin.Begin);

        bool isRemoteUpdated = false;
        try
        {
            isRemoteUpdated = await SendAssetDatabaseHash(hash);
        }
        catch (RpcNoConnectionsException) { return; }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "Error replicating asset info.");
        }

        if (isRemoteUpdated)
        {
            return;
        }

        try
        {
            await SendAssetDatabase(fs).IgnoreNoConnections();
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "Error replicating asset info.");
        }
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

    [RpcSend("Uncreated.Web.Client.Unturned.AssetDatabase, Uncreated.Web.Client", "ReceiveAssetDatabaseHash")]
    private partial RpcTask<bool> SendAssetDatabaseHash(byte[] hash);

    [RpcSend("Uncreated.Web.Client.Unturned.AssetDatabase, Uncreated.Web.Client", "ReceiveAssetDatabase", Raw = true)]
    private partial RpcTask SendAssetDatabase(FileStream fileStream);

    public async UniTask HandleEventAsync(HomebaseConnected e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        await using FileStream fs = new FileStream(_cacheFileLocation, FileMode.Create, FileAccess.Write, FileShare.Read);

        await SendAssetDatabase(fs).IgnoreNoConnections();
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
}