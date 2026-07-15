using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Patches;

/// <summary>
/// This 'patch' iterates through Provider.accept and grabs the lambda function used to write <see cref="EClientMessage.ReplicateConfig"/>.
/// It doesn't actually change the function at all.
/// </summary>
internal sealed class ReplicateConfigPatches : IHarmonyPatch
{
    private static MethodInfo? _acceptMethod;
    private static MethodInfo? _writeFunction;

    internal static NetMessages.ClientWriteHandler? Callback;

    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        MethodInfo? acceptMethod;
        try
        {
            acceptMethod = typeof(Provider).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .Where(x => x.Name == "accept")
                .Aggregate((a, b) => a.GetParameters().Length > b.GetParameters().Length ? a : b);
        }
        catch (InvalidOperationException)
        {
            acceptMethod = null;
        }

        if (acceptMethod == null)
        {
            logger.LogError("Failed to find accept method for ReplicateConfig message.");
            return;
        }

        _writeFunction = null;
        Callback = null;

        // just need to get the type from it, dont need to keep the patch
        patcher.Patch(acceptMethod, transpiler: new HarmonyMethod(Accessor.GetMethod(Transpiler)));
        patcher.Unpatch(acceptMethod, Accessor.GetMethod(Transpiler));

        if (_writeFunction == null)
        {
            logger.LogError("Failed to get write function for ReplicateConfig message.");
        }
        else if (!_writeFunction.IsStatic)
        {
            try
            {
                object instance = Activator.CreateInstance(_writeFunction.DeclaringType!);
                Callback = (NetMessages.ClientWriteHandler?)_writeFunction.CreateDelegate(typeof(NetMessages.ClientWriteHandler), instance);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Write function for ReplicateConfig is no longer static(ish), this needs updating (good luck bro).");
            }
        }
        else
        {
            Callback = (NetMessages.ClientWriteHandler?)_writeFunction.CreateDelegate(typeof(NetMessages.ClientWriteHandler), null);
        }

        if (Callback == null)
            return;

        _acceptMethod = acceptMethod;
        patcher.Patch(acceptMethod, transpiler: new HarmonyMethod(Accessor.GetMethod(AcceptTranspiler)));
        patcher.Patch(acceptMethod, finalizer: new HarmonyMethod(Accessor.GetMethod(OnDoneAcceptingPlayer)));
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        Callback = null;
        patcher.Unpatch(_acceptMethod, Accessor.GetMethod(OnDoneAcceptingPlayer));
        patcher.Unpatch(_acceptMethod, Accessor.GetMethod(AcceptTranspiler));
    }

    // apply and undo global config changes
    private static void OnStartAcceptingPlayer()
    {
        PlayerReplicatedConfigManager? manager = WarfareModule.Singleton.ServiceProvider.ResolveOptional<PlayerReplicatedConfigManager>();
        manager?.Apply(null);
    }

    private static Exception OnDoneAcceptingPlayer(Exception __exception)
    {
        PlayerReplicatedConfigManager? manager = WarfareModule.Singleton.ServiceProvider.ResolveOptional<PlayerReplicatedConfigManager>();
        manager?.Undo(null);
        return __exception;
    }

    private static IEnumerable<CodeInstruction> AcceptTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeInstruction[] { new CodeInstruction(OpCodes.Call, Accessor.GetMethod(OnStartAcceptingPlayer)) }
            .Concat(instructions);
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo? sendMsgToClient = typeof(Provider).Assembly.GetType("SDG.Unturned.NetMessages", true)?
            .GetMethod("SendMessageToClient", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        if (sendMsgToClient == null)
            throw new MissingMethodException("Failed to find NetMessages.SendMessageToClient method.");

        List<CodeInstruction> ins = new List<CodeInstruction>(instructions);

        for (int i = 0; i < ins.Count; i++)
        {
            CodeInstruction instruction = ins[i];
            if (!instruction.Calls(sendMsgToClient))
                continue;

            CodeInstruction? lastLdftn = null;
            int ldcCt = 0;
            for (int j = i - 1; j >= 0; --j)
            {
                CodeInstruction prev = ins[j];
                if (prev.opcode == OpCodes.Ldftn)
                {
                    lastLdftn = prev;
                }
                else if (prev.opcode.IsLdc(@int: true))
                {
                    ++ldcCt;

                    // skip ENetReliability
                    if (ldcCt == 1)
                        continue; 

                    int value = GetI4OpCodeValue(prev);
                    if ((EClientMessage)value != EClientMessage.ReplicateConfig)
                        lastLdftn = null;
                    
                    break;
                }
            }

            if (lastLdftn?.operand is not MethodInfo mtd)
            {
                continue;
            }

            _writeFunction = mtd;
            break;
        }

        return ins;
    }

    private static int GetI4OpCodeValue(CodeInstruction ins)
    {
        OpCode opcode = ins.opcode;
        if (opcode == OpCodes.Ldc_I4)
            return (int)ins.operand;
        if (opcode == OpCodes.Ldc_I4_S)
            return (sbyte)ins.operand;
        if (opcode == OpCodes.Ldc_I4_0)
            return 0;
        if (opcode == OpCodes.Ldc_I4_1)
            return 1;
        if (opcode == OpCodes.Ldc_I4_M1)
            return -1;
        if (opcode == OpCodes.Ldc_I4_2)
            return 2;
        if (opcode == OpCodes.Ldc_I4_3)
            return 3;
        if (opcode == OpCodes.Ldc_I4_4)
            return 4;
        if (opcode == OpCodes.Ldc_I4_5)
            return 5;
        if (opcode == OpCodes.Ldc_I4_6)
            return 6;
        if (opcode == OpCodes.Ldc_I4_7)
            return 7;
        if (opcode == OpCodes.Ldc_I4_8)
            return 8;

        throw new ArgumentException("Invalid ldc.i4 instruction.");
    }
}