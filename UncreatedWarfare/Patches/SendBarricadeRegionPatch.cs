using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using SDG.NetPak;
using SDG.NetTransport;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Patches;
internal sealed class SendBarricadeRegionPatch : IHarmonyPatch
{
    private static readonly ClientStaticMethod? SendMultipleBarricades = ReflectionUtility.FindRpc<BarricadeManager, ClientStaticMethod>("SendMultipleBarricades");

    private static MethodInfo? _target;
    void IHarmonyPatch.Patch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        _target = typeof(BarricadeManager).GetMethod("SendRegion", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

        if (_target != null)
        {
            if (SendMultipleBarricades == null)
            {
                logger.LogError("Failed to find SendMultipleBarricades net call. Unable to patch {0}.", _target);
                _target = null;
                return;
            }

            patcher.Patch(_target, prefix: Accessor.GetMethod(Prefix));
            logger.LogDebug("Patched {0} for overriding send region.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition("SendRegion")
                .DeclaredIn<BarricadeManager>(isStatic: false)
                .WithParameter<SteamPlayer>("client")
                .WithParameter<BarricadeRegion>("region")
                .WithParameter<byte>("x")
                .WithParameter<byte>("y")
                .WithParameter<NetId>("parentNetId")
                .WithParameter<float>("sortOrder")
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Prefix));
        logger.LogDebug("Unpatched {0} for overriding send region.", _target);
        _target = null;
    }

    // SDG.Unturned.BarricadeManager
    /// <summary>
    /// Prefix for overriding send region.
    /// </summary>
    private static bool Prefix(SteamPlayer client, BarricadeRegion region, byte x, byte y, NetId parentNetId, float sortOrder)
    {
        if (SendMultipleBarricades == null)
            return true;

        ILifetimeScope serviceProvider = WarfareModule.Singleton.ServiceProvider;

        IPlayerService playerService = serviceProvider.Resolve<IPlayerService>();
        SignInstancer signInstancer = serviceProvider.Resolve<SignInstancer>();

        WarfarePlayer player = playerService.GetOnlinePlayer(client);
        if (region.drops.Count <= 0)
        {
            SendMultipleBarricades.Invoke(ENetReliability.Reliable, client.transportConnection, writer =>
            {
                writer.WriteUInt8(x);
                writer.WriteUInt8(y);
                writer.WriteNetId(NetId.INVALID);
                writer.WriteUInt8(0);
                writer.WriteUInt16(0);
            });
            return false;
        }

        byte packet = 0;
        int index = 0;
        int count = 0;
        while (index < region.drops.Count)
        {
            int num = 0;
            while (count < region.drops.Count)
            {
                num += 44 + region.drops[count].GetServersideData().barricade.state.Length;
                count++;
                if (num > Block.BUFFER_SIZE / 2)
                    break;
            }

            byte pkt = packet;
            SendMultipleBarricades.Invoke(ENetReliability.Reliable, client.transportConnection, writer =>
            {
                writer.WriteUInt8(x);
                writer.WriteUInt8(y);
                writer.WriteNetId(parentNetId);
                writer.WriteUInt8(pkt);
                writer.WriteUInt16((ushort)(count - index));
                writer.WriteFloat(sortOrder);
                for (; index < count; ++index)
                {
                    BarricadeDrop drop = region.drops[index];
                    BarricadeData serversideData = drop.GetServersideData();
                    writer.WriteGuid(drop.asset.GUID);
                    switch (drop.interactable)
                    {
                        case InteractableStorage:
                            // storage's written state is trimmed down to avoid sending storage items to all players
                            // this is vanilla behavior
                            WriteStorage(writer, drop);
                            break;

                        case InteractableSign:
                            // sign states are sent separately for each player for translation purposes
                            WriteSign(writer, drop, player, signInstancer);
                            break;

                        default:
                            // every thing else can be normal
                            WriteState(writer, serversideData.barricade.state);
                            break;
                    }

                    writer.WriteClampedVector3(serversideData.point, fracBitCount: 11);
                    writer.WriteSpecialYawOrQuaternion(serversideData.rotation, yawBitCount: 23);

                    writer.WriteUInt8((byte)Mathf.RoundToInt(serversideData.barricade.health / (float)serversideData.barricade.asset.health * 100f));
                    writer.WriteUInt64(serversideData.owner);
                    writer.WriteUInt64(serversideData.group);
                    writer.WriteNetId(drop.GetNetId());
                }
            });
            packet++;
        }

        return false;
    }

    private static void WriteState(NetPakWriter writer, ReadOnlySpan<byte> data)
    {
        int len = Math.Min(byte.MaxValue, data.Length);
        writer.WriteUInt8((byte)len);
        writer.WriteSpan(data[..len]);
    }

    private static void WriteSign(NetPakWriter writer, BarricadeDrop drop, WarfarePlayer player, SignInstancer signInstancer)
    {
        InteractableSign sign = (InteractableSign)drop.interactable;

        scoped Span<byte> data;

        if (!signInstancer.IsInstanced(drop))
        {
            data = drop.GetServersideData().barricade.state;
        }
        else
        {
            Encoding encoding = Encoding.UTF8;
            string signText = signInstancer.GetSignText(drop, player);
            int byteCt = encoding.GetByteCount(signText);
            if (byteCt > byte.MaxValue - 17)
            {
                WarfareModule.Singleton.GlobalLogger.LogWarning(
                    "Sign translation too long: {0}. Must be <= {1} UTF-8 bytes (was {2} bytes).",
                    sign.text, byte.MaxValue - 17, byteCt
                );

                signText = sign.text;
                byteCt = Math.Min(byte.MaxValue - 17, encoding.GetByteCount(signText));
            }

            data = stackalloc byte[17 + byteCt];

            ulong tempRef64 = sign.owner.m_SteamID;
            MemoryMarshal.Write(data, ref tempRef64);

            tempRef64 = sign.group.m_SteamID;
            MemoryMarshal.Write(data[8..], ref tempRef64);

            data[16] = (byte)byteCt;
            if (byteCt != 0)
            {
                encoding.GetBytes(signText, data[17..]);
            }
        }

        WriteState(writer, data);
    }

    private static void WriteStorage(NetPakWriter writer, BarricadeDrop drop)
    {
        Encoding encoding = Encoding.UTF8;
        InteractableStorage interactable = (InteractableStorage)drop.interactable;

        scoped Span<byte> data;
        if (interactable.isDisplay)
        {
            if (interactable.displayItem != null)
            {
                int stateLen = Math.Min(byte.MaxValue, interactable.displayItem.state.Length);

                int dispTagCt = interactable.displayTags is not { Length: > 0 } ? 0 : encoding.GetByteCount(interactable.displayTags);
                int propsCt = interactable.displayDynamicProps is not { Length: > 0 } ? 0 : encoding.GetByteCount(interactable.displayDynamicProps);

                int dataSize = 27 + stateLen + dispTagCt + propsCt;

                if (dataSize > byte.MaxValue)
                {
                    dataSize -= dispTagCt + propsCt;
                    dispTagCt = 0;
                    propsCt = 0;
                }

                data = stackalloc byte[dataSize];

                int index = 16;

                // item id
                ushort tempRef = interactable.displayItem.id;
                MemoryMarshal.Write(data[index..], ref tempRef);
                index += 2;

                // quality
                data[index++] = interactable.displayItem.quality;

                // state
                data[index++] = (byte)stateLen;
                interactable.displayItem.state.AsSpan(0, stateLen).CopyTo(data[index..]);
                index += stateLen;

                // skin and mythic
                tempRef = interactable.displaySkin;
                MemoryMarshal.Write(data[index..], ref tempRef);
                index += 2;

                tempRef = interactable.displayMythic;
                MemoryMarshal.Write(data[index..], ref tempRef);
                index += 2;

                // display tags
                data[index++] = (byte)dispTagCt;
                if (dispTagCt > 0)
                {
                    encoding.GetBytes(interactable.displayTags, data.Slice(index, dispTagCt));
                }

                // dynamic display props
                data[index++] = (byte)propsCt;
                if (propsCt > 0)
                {
                    encoding.GetBytes(interactable.displayDynamicProps, data.Slice(index, propsCt));
                }

                data[index] = interactable.rot_comp;
            }
            else
            {
                data = stackalloc byte[27];
            }
        }
        else
        {
            data = stackalloc byte[16];
        }

        ulong tempRef64 = interactable.owner.m_SteamID;
        MemoryMarshal.Write(data, ref tempRef64);

        tempRef64 = interactable.group.m_SteamID;
        MemoryMarshal.Write(data[8..], ref tempRef64);

        WriteState(writer, data);
    }
}