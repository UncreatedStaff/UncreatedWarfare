using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Kits.Cosmetics;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Patches;

[Ignore]
internal sealed class ClothingReplicationPatches : IHarmonyPatch
{
    private static readonly MethodInfo?[] AskWearMethods = new MethodInfo?[ClothingItem.Count];
    private static readonly Delegate?[] AskWearPatches = new Delegate?[ClothingItem.Count];

    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        PatchAskWear(logger, patcher);
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        for (int i = 0; i < ClothingItem.Count; ++i)
        {
            if (AskWearPatches[i] != null)
            {
                patcher.Unpatch(AskWearMethods[i], AskWearPatches[i]!.Method);
            }
        }
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
                patcher.Patch(AskWearMethods[i], transpiler: new HarmonyMethod(AskWearPatches[i] = CreateAskWearTranspiler(item)));
            }
        }

        if (mask == 0)
        {
            logger.LogError("Unable to patch any askWear for cosmetics.");
            return;
        }


    }

    private static Func<IEnumerable<CodeInstruction>, ILGenerator, MethodBase, IEnumerable<CodeInstruction>> CreateAskWearTranspiler(
        ClothingItem item
    )
    {
        return (instructions, generator, method) =>
        {
            TranspileContext ctx = new TranspileContext(method, generator, instructions);

            string fieldName = "SendWear" + EnumUtility.GetNameSafe(item.Type);

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
            args[2] = typeof(List<ITranspileContextLogger>);
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
                    WarfareModule.Singleton.GlobalLogger.LogWarning($"Unable to find InvokeAndLoopback call after loading {fieldName}.");
                    continue;
                }

                int endIndex = ctx.CaretIndex;
                ctx.CaretIndex = startIndex;
                ctx.EmitBelow(emit =>
                {
                    emit.LoadConstantInt32((int)item.Type);
                });

                ctx.CaretIndex = endIndex;
                ctx.Replace(emit =>
                {
                    emit.Invoke(OnAskWearClothingMethod);
                });
                patched = true;
            }

            if (!patched)
            {
                WarfareModule.Singleton.GlobalLogger.LogWarning($"Unable to find reference to RPC field: \"{fieldName}\" in {method.Name}.");
            }

            return ctx;
        };
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

        if (!WarfareModule.Singleton.IsLayoutActive())
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


    }
}