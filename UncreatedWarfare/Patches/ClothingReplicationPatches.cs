using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Kits.Cosmetics;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Patches;

internal sealed class ClothingReplicationPatches : IHarmonyPatch
{
    private static readonly MethodInfo?[] AskWearMethods = new MethodInfo?[ClothingItem.Count];
    private static MethodInfo? _writeClothingStateMethod;
    private static MethodInfo? _sendInitialPlayerState;
    private static readonly Delegate?[] AskWearPatches = new Delegate?[ClothingItem.Count];

    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        PatchAskWear(logger, patcher);
        PatchWriteClothingState(logger, patcher);
        PatchSendInitialPlayerState(logger, patcher);
    }

    private static void PatchAskWear(ILogger logger, Harmony patcher)
    {
        int mask = 0;
        for (int i = 0; i < ClothingItem.Count; ++i)
        {
            ClothingItem item = new ClothingItem((ClothingType)i);

            string methodName = "askWear" + EnumUtility.GetNameSafe(item.Type);
            AskWearMethods[i] = typeof(PlayerClothing).GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                CallingConventions.Any,
                [item.AssetType, typeof(byte), typeof(byte[]), typeof(bool)],
                null
            );

            if (AskWearMethods[i] == null)
            {
                logger.LogError($"Unable to find {typeof(PlayerClothing)}.{methodName}.");
            }
            else
            {
                mask |= item.Flag;
                patcher.Patch(AskWearMethods[i], transpiler: new HarmonyMethod(CreateAskWearTranspiler));
            }
        }

        if (mask == 0)
        {
            logger.LogError("Unable to patch any askWear for cosmetics.");
        }
    }

    private static void PatchWriteClothingState(ILogger logger, Harmony patcher)
    {
        _writeClothingStateMethod = typeof(PlayerClothing).GetMethod("WriteClothingState", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (_writeClothingStateMethod == null)
        {
            logger.LogError($"Unable to find {typeof(PlayerClothing)}.WriteClothingState.");
            return;
        }

        patcher.Patch(_writeClothingStateMethod, transpiler: new HarmonyMethod(TranspileWriteClothingState));
    }

    private static void PatchSendInitialPlayerState(ILogger logger, Harmony patcher)
    {
        _sendInitialPlayerState = typeof(PlayerClothing).GetMethod(
            "SendInitialPlayerState",
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
            null,
            CallingConventions.Any,
            [ typeof(SteamPlayer) ],
            null
        );
        if (_sendInitialPlayerState == null)
        {
            logger.LogError($"Unable to find {typeof(PlayerClothing)}.SendInitialPlayerState.");
            return;
        }

        patcher.Patch(
            _sendInitialPlayerState,
            prefix: new HarmonyMethod(SendInitialPlayerStatePrefix),
            postfix: new HarmonyMethod(SendInitialPlayerStatePostfix)
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        for (int i = 0; i < ClothingItem.Count; ++i)
        {
            if (AskWearPatches[i] != null)
            {
                patcher.Unpatch(AskWearMethods[i]!, AskWearPatches[i]!.Method);
            }
        }

        if (_writeClothingStateMethod != null)
        {
            patcher.Unpatch(_writeClothingStateMethod, Accessor.GetMethod(TranspileWriteClothingState));
        }

        if (_sendInitialPlayerState != null)
        {
            patcher.Unpatch(_sendInitialPlayerState, Accessor.GetMethod(SendInitialPlayerStatePrefix));
            patcher.Unpatch(_sendInitialPlayerState, Accessor.GetMethod(SendInitialPlayerStatePostfix));
        }
    }

    private static IEnumerable<CodeInstruction> TranspileWriteClothingState(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
    {
        TranspileContext ctx = new TranspileContext(method, generator, instructions);

        MethodInfo?[] getAssetMethods = new MethodInfo?[ClothingItem.Count];
        FieldInfo?[] qualityFields = new FieldInfo?[ClothingItem.Count];
        FieldInfo?[] stateFields = new FieldInfo?[ClothingItem.Count];

        int expectedMask = 0;
        for (int i = 0; i < ClothingItem.Count; ++i)
        {
            expectedMask |= 1 << i;
            ClothingItem item = new ClothingItem((ClothingType)i);
            string prefix = EnumUtility.GetNameSafe(item.Type).ToLowerInvariant();

            string propertyName = prefix + "Asset";
            getAssetMethods[i] = typeof(PlayerClothing).GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetMethod;
            if (getAssetMethods[i] == null || getAssetMethods[i]!.ReturnType != item.AssetType)
            {
                ctx.LogError($"Failed to find asset property for {item.Type} clothing.");
            }

            string fieldName = prefix + "Quality";
            qualityFields[i] = typeof(PlayerClothing).GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (qualityFields[i] == null || qualityFields[i]!.FieldType != typeof(byte))
            {
                ctx.LogError($"Failed to find quality field for {item.Type} clothing.");
            }

            fieldName = prefix + "State";
            stateFields[i] = typeof(PlayerClothing).GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (stateFields[i] == null || stateFields[i]!.FieldType != typeof(byte[]))
            {
                ctx.LogError($"Failed to find state field for {item.Type} clothing.");
            }
        }

        int assetMask = 0, qualityMask = 0, stateMask = 0;
        while (ctx.MoveNext())
        {
            int assetIndex   = Array.FindIndex(getAssetMethods, ctx.Instruction.Calls);
            int qualityIndex = Array.FindIndex(qualityFields, x => ctx.Instruction.LoadsField(x));
            int stateIndex   = Array.FindIndex(stateFields, x => ctx.Instruction.LoadsField(x));

            if (assetIndex >= 0)
            {
                ctx.Replace(emit =>
                {
                    emit.LoadConstantInt32(assetIndex)
                        .Invoke(OnSendingAssetMethod);
                });
                assetMask |= 1 << assetIndex;
            }
            else if (qualityIndex >= 0)
            {
                ctx.Replace(emit =>
                {
                    emit.LoadConstantInt32(qualityIndex)
                        .Invoke(OnSendingQualityMethod);
                });
                qualityMask |= 1 << qualityIndex;
            }
            else if (stateIndex >= 0)
            {
                ctx.Replace(emit =>
                {
                    emit.LoadConstantInt32(stateIndex)
                        .Invoke(OnSendingStateMethod);
                });
                stateMask |= 1 << stateIndex;
            }
        }

        if (assetMask != expectedMask)
        {
            ctx.LogError($"Missing OnSendingAsset patch (0b{Convert.ToString(assetMask, 2)}).");
        }

        if (qualityMask != expectedMask)
        {
            ctx.LogError($"Missing OnSendingQuality patch (0b{Convert.ToString(assetMask, 2)}).");
        }

        if (stateMask != expectedMask)
        {
            ctx.LogError($"Missing OnSendingState patch (0b{Convert.ToString(assetMask, 2)}).");
        }

        return ctx;
    }

    private static PlayerClothing? _currentPlayer;
    private static ClothingType _currentType;
    private static int _opMask;

    private static ItemClothingAsset? _asset;
    private static byte _quality;
    private static byte[]? _state;
    private static SteamPlayer? _sendingToPlayer;

    private static readonly MethodInfo OnSendingAssetMethod = typeof(ClothingReplicationPatches).GetMethod(
        nameof(OnSendingAsset),
        BindingFlags.Static | BindingFlags.NonPublic
    ) ?? throw new MissingMethodException(nameof(ClothingReplicationPatches), nameof(OnSendingAsset));

    private static ItemClothingAsset? OnSendingAsset(PlayerClothing clothing, ClothingType type)
    {
#if DEBUG
        if (clothing == null)
            throw new ArgumentNullException(nameof(clothing), $"Clothing is null for OnSendingAsset({type}).");
#endif
        if (_sendingToPlayer == null || !MaybeCalculateWrittenType(clothing, type, 1))
        {
            _sendingToPlayer = null;
            ClothingItem item = new ClothingItem(clothing, type);
            return item.Asset;
        }

        ItemClothingAsset? asset = _asset;
        _asset = null;
        if (asset != null)
        {
            // this will cause an invalid IL exception if its not the right type
            ClothingItem item = new ClothingItem(type);
            if (!item.ValidAsset(asset))
                return null;
        }

        return asset;
    }

    private static readonly MethodInfo OnSendingQualityMethod = typeof(ClothingReplicationPatches).GetMethod(
        nameof(OnSendingQuality),
        BindingFlags.Static | BindingFlags.NonPublic
    ) ?? throw new MissingMethodException(nameof(ClothingReplicationPatches), nameof(OnSendingQuality));

    private static byte OnSendingQuality(PlayerClothing clothing, ClothingType type)
    {
#if DEBUG
        if (clothing == null)
            throw new ArgumentNullException(nameof(clothing), $"Clothing is null for OnSendingQuality({type}).");
#endif
        if (_sendingToPlayer == null || !MaybeCalculateWrittenType(clothing, type, 2))
        {
            _sendingToPlayer = null;
            ClothingItem item = new ClothingItem(clothing, type);
            return item.Quality;
        }

        return _quality;
    }

    private static readonly MethodInfo OnSendingStateMethod = typeof(ClothingReplicationPatches).GetMethod(
        nameof(OnSendingState),
        BindingFlags.Static | BindingFlags.NonPublic
    ) ?? throw new MissingMethodException(nameof(ClothingReplicationPatches), nameof(OnSendingState));

    private static byte[] OnSendingState(PlayerClothing clothing, ClothingType type)
    {
#if DEBUG
        if (clothing == null)
            throw new ArgumentNullException(nameof(clothing), $"Clothing is null for OnSendingState({type}).");
#endif
        if (_sendingToPlayer == null || !MaybeCalculateWrittenType(clothing, type, 4))
        {
            _sendingToPlayer = null;
            ClothingItem item = new ClothingItem(clothing, type);
            return item.State;
        }

        return _state ?? Array.Empty<byte>();
    }

    private static bool MaybeCalculateWrittenType(PlayerClothing clothing, ClothingType type, int bit)
    {
        if (_currentPlayer == clothing && _currentType == type)
        {
            _opMask |= bit;
            if (_opMask == (1 | 2 | 4))
            {
                _opMask = 0;
                _currentPlayer = null;
            }

            return true;
        }

        _currentPlayer = clothing;
        _currentType = type;
        // whichever function is called first starts figuring out what is needed
        _opMask = bit;

        if (!WarfareModule.Singleton.IsLayoutActive())
        {
            return false;
        }

        ILifetimeScope serviceProvider = WarfareModule.Singleton.ScopedProvider;
        IPlayerService playerService = serviceProvider.Resolve<IPlayerService>();

        WarfarePlayer onPlayer = playerService.GetOnlinePlayer(clothing.player);
        CosmeticInstancer? cosmeticInstancer = serviceProvider.ResolveOptional<CosmeticInstancer>();
        if (cosmeticInstancer is not { IsEnabled: true })
        {
            return false;
        }

        WarfarePlayer viewPlayer = playerService.GetOnlinePlayer(_sendingToPlayer!);
        cosmeticInstancer.Resolve(viewPlayer, onPlayer, type, out _asset, out _quality, out _state);
        return true;
    }
    
    private static void SendInitialPlayerStatePrefix(SteamPlayer client)
    {
        _sendingToPlayer = client;
    }

    private static void SendInitialPlayerStatePostfix(SteamPlayer client)
    {
        Interlocked.CompareExchange(ref _sendingToPlayer, null, client);
    }

    private static IEnumerable<CodeInstruction> CreateAskWearTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
    {
        int index = Array.IndexOf(AskWearMethods, (MethodInfo)method);
        if (index < 0)
            throw new InvalidOperationException("Failed to find askWear function in AskWearMethods");

        ClothingType type = (ClothingType)index;

        TranspileContext ctx = new TranspileContext(method, generator, instructions);

        string fieldName = "SendWear" + EnumUtility.GetNameSafe(type);

        Type fieldType = typeof(ClientInstanceMethod<Guid, byte, byte[], bool>);

        FieldInfo? sendWearField = typeof(PlayerClothing).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        if (sendWearField == null || sendWearField.FieldType != fieldType)
        {
            return ctx.Fail(new FieldDefinition(fieldName)
                .AsReadOnly()
                .DeclaredIn<PlayerClothing>(isStatic: true)
                .WithFieldType(fieldType)
            );
        }

        Type[] genTypes = fieldType.GetGenericArguments();

        Type[] args = new Type[3 + genTypes.Length];
        args[0] = typeof(NetId);
        args[1] = typeof(ENetReliability);
        args[2] = typeof(List<ITransportConnection>);
        Array.Copy(genTypes, 0, args, 3, genTypes.Length);

        MethodInfo? invokeAndLoopback = fieldType.GetMethod(
            nameof(ClientInstanceMethod<,,,>.InvokeAndLoopback),
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            CallingConventions.Any,
            args,
            null
        );

        if (invokeAndLoopback == null)
        {
            MethodDefinition mtd = new MethodDefinition(nameof(ClientInstanceMethod<,,,>.InvokeAndLoopback))
                .DeclaredIn(fieldType, isStatic: false)
                .WithParameter<NetId>("netId")
                .WithParameter<ENetReliability>("reliability")
                .WithParameter<List<ITransportConnection>>("transportConnections")
                .ReturningVoid();

            for (int i = 0; i < genTypes.Length; ++i)
                mtd.WithParameter(genTypes[i], $"arg{i + 1}");
            
            return ctx.Fail(mtd);
        }

        bool patched = false;
        while (ctx.MoveNext())
        {
            if (!ctx.Instruction.LoadsField(sendWearField))
                continue;

            int startIndex = ctx.CaretIndex;
            bool found = false;
            while (ctx.MoveNext())
            {
                if (!ctx.Instruction.Calls(invokeAndLoopback))
                    continue;

                found = true;
                break;
            }

            if (!found)
            {
                ctx.LogError($"Unable to find InvokeAndLoopback call after loading {fieldName}.");
                break;
            }

            int endIndex = ctx.CaretIndex;
            ctx.CaretIndex = startIndex;
            ctx.EmitBelow(emit =>
            {
                emit.LoadConstantInt32((int)type);
            });

            ctx.CaretIndex = endIndex + 1; // EmitBelow moves it up by 1
            ctx.Replace(emit =>
            {
                emit.Invoke(OnAskWearClothingMethod);
            });
            patched = true;
        }

        if (!patched)
        {
            ctx.LogError($"Unable to find reference to RPC field: \"{fieldName}\" in {method.Name}.");
        }

        return ctx;
    }

    private static readonly MethodInfo OnAskWearClothingMethod = typeof(ClothingReplicationPatches).GetMethod(
        nameof(OnAskWearClothing),
        BindingFlags.Static | BindingFlags.NonPublic
    ) ?? throw new MissingMethodException(nameof(ClothingReplicationPatches), nameof(OnAskWearClothing));

    private static void OnAskWearClothing(
        ClientInstanceMethod<Guid, byte, byte[], bool> rpc,
        ClothingType type,
        NetId netId,
        ENetReliability reliability,
        List<ITransportConnection> transportConnections,
        Guid guid,
        byte quality,
        byte[] state,
        bool playEffect
    )
    {
        PlayerClothing? clothing = NetIdRegistry.Get<PlayerClothing>(netId);

        if (clothing == null)
        {
            WarfareModule.Singleton.GlobalLogger.LogWarning("Unable to find PlayerClothing from OnAskWearClothing.");
            rpc.InvokeAndLoopback(netId, reliability, transportConnections, guid, quality, state, playEffect);
            return;
        }

        if (!WarfareModule.Singleton.IsLayoutActive() || !ItemUtility.HasClothingRpc(type))
        {
            rpc.InvokeAndLoopback(netId, reliability, transportConnections, guid, quality, state, playEffect);
            return;
        }

        ILifetimeScope serviceProvider = WarfareModule.Singleton.ScopedProvider;

        IPlayerService playerService = serviceProvider.Resolve<IPlayerService>();

        WarfarePlayer player = playerService.GetOnlinePlayer(clothing.player);
        CosmeticInstancer? cosmeticInstancer = serviceProvider.ResolveOptional<CosmeticInstancer>();
        if (cosmeticInstancer is not { IsEnabled: true } || !cosmeticInstancer.ShouldInstance(type, player))
        {
            rpc.InvokeAndLoopback(netId, reliability, transportConnections, guid, quality, state, playEffect);
            return;
        }

        ItemClothingAsset? clothingAsset = guid == Guid.Empty ? null : Assets.find<ItemClothingAsset>(guid);

        cosmeticInstancer.SetClothingShortcut(type, player, clothingAsset, quality, state, playEffect);
    }
}