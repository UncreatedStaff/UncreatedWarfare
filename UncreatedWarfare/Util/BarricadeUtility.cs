using Microsoft.Extensions.DependencyInjection;
using SDG.NetTransport;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Util.Region;

namespace Uncreated.Warfare.Util;

/// <summary>
/// Helper functions for barricades.
/// </summary>
public static class BarricadeUtility
{
    /// <summary>
    /// All transformations using a buildable as reference should rotate the barricade by this first.
    /// </summary>
    public static readonly Quaternion InverseDefaultBarricadeRotation = Quaternion.Euler(90f, 0f, 0f);

    /// <summary>
    /// All buildables should be rotated by this amount when placed relative to any other objects.
    /// </summary>
    public static readonly Quaternion DefaultBarricadeRotation = Quaternion.Euler(-90f, 0f, 0f);

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around the center of the level, then planted barricades (barricades on a vehicle).
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateBarricades()
    {
        GameThread.AssertCurrent();

        return new BarricadeIterator((byte)(Regions.WORLD_SIZE / 2), (byte)(Regions.WORLD_SIZE / 2), true, true);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around <paramref name="center"/>, then planted barricades (barricades on a vehicle).
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateBarricades(Vector3 center)
    {
        GameThread.AssertCurrent();

        if (!Regions.tryGetCoordinate(center, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        return new BarricadeIterator(x, y, true, true);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around <paramref name="center"/>, then planted barricades (barricades on a vehicle).
    /// </summary>
    /// <remarks>The square enumerated will have a size of <c><paramref name="maxRegionDistance"/> * 2 + 1</c> regions.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateBarricades(Vector3 center, byte maxRegionDistance)
    {
        GameThread.AssertCurrent();

        if (!Regions.tryGetCoordinate(center, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        return new BarricadeIterator(x, y, true, true, maxRegionDistance);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around region <paramref name="x"/>, <paramref name="y"/>, then planted barricades (barricades on a vehicle).
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateBarricades(byte x, byte y)
    {
        GameThread.AssertCurrent();

        return new BarricadeIterator(x, y, true, true);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around <paramref name="region"/>, then planted barricades (barricades on a vehicle).
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateBarricades(RegionCoord region)
    {
        GameThread.AssertCurrent();

        return new BarricadeIterator(region.x, region.y, true, true);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around region <paramref name="x"/>, <paramref name="y"/>, then planted barricades (barricades on a vehicle).
    /// </summary>
    /// <remarks>The square enumerated will have a size of <c><paramref name="maxRegionDistance"/> * 2 + 1</c> regions.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateBarricades(byte x, byte y, byte maxRegionDistance)
    {
        GameThread.AssertCurrent();

        return new BarricadeIterator(x, y, true, true, maxRegionDistance);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around <paramref name="region"/>, then planted barricades (barricades on a vehicle).
    /// </summary>
    /// <remarks>The square enumerated will have a size of <c><paramref name="maxRegionDistance"/> * 2 + 1</c> regions.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateBarricades(RegionCoord region, byte maxRegionDistance)
    {
        GameThread.AssertCurrent();

        return new BarricadeIterator(region.x, region.y, true, true, maxRegionDistance);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around the center of the level.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateNonPlantedBarricades()
    {
        GameThread.AssertCurrent();

        return new BarricadeIterator((byte)(Regions.WORLD_SIZE / 2), (byte)(Regions.WORLD_SIZE / 2), true, false);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around <paramref name="center"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateNonPlantedBarricades(Vector3 center)
    {
        GameThread.AssertCurrent();

        if (!Regions.tryGetCoordinate(center, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        return new BarricadeIterator(x, y, true, false);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around <paramref name="center"/>.
    /// </summary>
    /// <remarks>The square enumerated will have a size of <c><paramref name="maxRegionDistance"/> * 2 + 1</c> regions.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateNonPlantedBarricades(Vector3 center, byte maxRegionDistance)
    {
        GameThread.AssertCurrent();

        if (!Regions.tryGetCoordinate(center, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        return new BarricadeIterator(x, y, true, false, maxRegionDistance);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around region <paramref name="x"/>, <paramref name="y"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateNonPlantedBarricades(byte x, byte y)
    {
        GameThread.AssertCurrent();

        return new BarricadeIterator(x, y, true, false);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around <paramref name="region"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateNonPlantedBarricades(RegionCoord region)
    {
        GameThread.AssertCurrent();

        return new BarricadeIterator(region.x, region.y, true, false);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around region <paramref name="x"/>, <paramref name="y"/>.
    /// </summary>
    /// <remarks>The square enumerated will have a size of <c><paramref name="maxRegionDistance"/> * 2 + 1</c> regions.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateNonPlantedBarricades(byte x, byte y, byte maxRegionDistance)
    {
        GameThread.AssertCurrent();

        return new BarricadeIterator(x, y, true, false, maxRegionDistance);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around <paramref name="region"/>.
    /// </summary>
    /// <remarks>The square enumerated will have a size of <c><paramref name="maxRegionDistance"/> * 2 + 1</c> regions.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateNonPlantedBarricades(RegionCoord region, byte maxRegionDistance)
    {
        GameThread.AssertCurrent();

        return new BarricadeIterator(region.x, region.y, true, false, maxRegionDistance);
    }

    /// <summary>
    /// Enumerate through planted barricades (barricades on a vehicle).
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumeratePlantedBarricades()
    {
        GameThread.AssertCurrent();

        return new BarricadeIterator((byte)(Regions.WORLD_SIZE / 2), (byte)(Regions.WORLD_SIZE / 2), false, true);
    }

    /// <summary>
    /// Find a barricade by it's instance ID.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo FindBarricade(uint instanceId)
    {
        return FindBarricade(instanceId, (byte)(Regions.WORLD_SIZE / 2), (byte)(Regions.WORLD_SIZE / 2));
    }

    /// <summary>
    /// Find a barricade by it's instance ID, with help from a position to prevent having to search every region.
    /// </summary>
    /// <remarks>All regions will be searched if it's not found near the expected position.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo FindBarricade(uint instanceId, Vector3 expectedPosition)
    {
        return Regions.tryGetCoordinate(expectedPosition, out byte x, out byte y)
            ? FindBarricade(instanceId, x, y)
            : FindBarricade(instanceId, (byte)(Regions.WORLD_SIZE / 2), (byte)(Regions.WORLD_SIZE / 2));
    }

    /// <summary>
    /// Find a barricade by it's instance ID, with help from an expected region to prevent having to search every region.
    /// </summary>
    /// <remarks>All regions will be searched if it's not found in the expected region.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo FindBarricade(uint instanceId, byte expectedRegionX, byte expectedRegionY)
    {
        GameThread.AssertCurrent();

        SurroundingRegionsIterator iterator = RegionUtility.EnumerateRegions(expectedRegionX, expectedRegionY);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                if (drops[i].instanceID == instanceId)
                    return new BarricadeInfo(drops[i], i, coord);
            }
        }

        IReadOnlyList<VehicleBarricadeRegion> vRegions = BarricadeManager.vehicleRegions;
        int ct = Math.Min(ushort.MaxValue - 1, vRegions.Count);
        for (int r = 0; r < ct; ++r)
        {
            List<BarricadeDrop> drops = vRegions[r].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                if (drops[i].instanceID == instanceId)
                    return new BarricadeInfo(drops[i], i, (ushort)r);
            }
        }
        
        return new BarricadeInfo(null, -1, new RegionCoord(expectedRegionX, expectedRegionY));
    }

    /// <summary>
    /// Sets the sign text without replicating to clients.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    /// <exception cref="ArgumentException"><paramref name="barricade"/> is not a sign.</exception>
    public static void SetServersideSignText(BarricadeDrop barricade, string text)
    {
        GameThread.AssertCurrent();
        
        if (barricade.interactable is not InteractableSign sign)
            throw new ArgumentException("Barricade must be a sign.", nameof(barricade));

        byte[] state = barricade.GetServersideData().barricade.state;
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        if (17 + bytes.Length > byte.MaxValue)
        {
            WarfareModule.Singleton.GlobalLogger.LogWarning(text + " is too long to go on a sign! (SetServersideSignText)");
            return;
        }
        byte[] newState = new byte[17 + bytes.Length];
        Buffer.BlockCopy(state, 0, newState, 0, 16);
        newState[16] = (byte)bytes.Length;
        if (bytes.Length != 0)
            Buffer.BlockCopy(bytes, 0, newState, 17, bytes.Length);
        BarricadeManager.updateState(barricade.model, newState, newState.Length);
        sign.updateState(barricade.asset, newState);
    }
    
    /// <summary>
    /// Get the exact length in bytes of the client-side state for a storage.
    /// </summary>
    public static int GetClientsideStorageStateLength(InteractableStorage storage)
    {
        if (!storage.isDisplay)
        {
            return sizeof(ulong) * 2;
        }

        int length = 4;
        if (storage.displayItem != null)
            length += storage.displayItem.state.Length;

        length += 7;
        if (storage.displayTags != null)
            length += storage.displayTags.Length;
        if (storage.displayDynamicProps != null)
            length += storage.displayDynamicProps.Length;

        return length;
    }

    /// <summary>
    /// Write the bytes of the client-side state for a storage.
    /// </summary>
    /// <remarks>Use <see cref="GetClientsideStorageStateLength"/> to pre-calculate the length.</remarks>
    /// <exception cref="ArgumentException">Output is not long enough for the full state.</exception>
    /// <exception cref="OverflowException">Displayed item's state or display metadata is longer than 255 elements.</exception>
    public static int WriteClientsideStorageState(Span<byte> output, InteractableStorage storage, CSteamID owner, CSteamID group)
    {
        if (output.Length < sizeof(ulong) * 2)
            throw new ArgumentException("Output not long enough for state.", nameof(output));

        BitConverter.TryWriteBytes(output, owner.m_SteamID);
        BitConverter.TryWriteBytes(output[sizeof(ulong)..], group.m_SteamID);
        int index = sizeof(ulong) * 2;

        if (!storage.isDisplay)
            return index;

        if (output.Length - index < 11)
            throw new ArgumentException("Output not long enough for state.", nameof(output));

        if (storage.displayItem != null)
        {
            BitConverter.TryWriteBytes(output[index..], storage.displayItem.id);
            index += sizeof(ushort);
            output[index++] = storage.displayItem.quality;
            output[index++] = checked( (byte)(storage.displayItem.state?.Length ?? 0) );
            if (storage.displayItem.state is { Length: > 0 })
            {
                Span<byte> state = storage.displayItem.state;
                if (output.Length - index < state.Length + 7)
                    throw new ArgumentException("Output not long enough for state.", nameof(output));

                state.CopyTo(output[index..]);
                index += state.Length;
            }
        }
        else
        {
            Unsafe.WriteUnaligned(ref output[index], 0);
            index += sizeof(int);
        }

        BitConverter.TryWriteBytes(output[index..], storage.displaySkin);
        index += sizeof(ushort);
        BitConverter.TryWriteBytes(output[index..], storage.displayMythic);
        index += sizeof(ushort);

        if (!string.IsNullOrEmpty(storage.displayTags))
        {
            byte byteCt   = checked( (byte)Encoding.UTF8.GetByteCount(storage.displayTags) );
            if (output.Length - index < byteCt + 3)
                throw new ArgumentException("Output not long enough for state.", nameof(output));
            output[index] = checked( (byte)Encoding.UTF8.GetBytes(storage.displayTags, output.Slice(index + 1, byteCt)) );
            index += byteCt + 1;
        }
        else
        {
            ++index;
            output[index] = 0;
        }

        if (!string.IsNullOrEmpty(storage.displayDynamicProps))
        {
            byte byteCt   = checked( (byte)Encoding.UTF8.GetByteCount(storage.displayDynamicProps) );
            if (output.Length - index < byteCt + 2)
                throw new ArgumentException("Output not long enough for state.", nameof(output));
            output[index] = checked( (byte)Encoding.UTF8.GetBytes(storage.displayDynamicProps, output.Slice(index + 1, byteCt)) );
            index += byteCt + 1;
        }
        else
        {
            ++index;
            output[index] = 0;
        }

        output[index] = storage.rot_comp;
        return index + 1;
    }

    /// <summary>
    /// Sends the given <paramref name="stateToReplicate"/>, which defaults to the barricade's state if <see langword="null"/> to all relevant clients.
    /// </summary>
    /// <returns><see langword="true"/> if the barricade state was replicated, otherwise <see langword="false"/> (due to reflection failure or out of bounds barricade).</returns>
    public static bool ReplicateBarricadeState(BarricadeDrop drop, IServiceProvider? serviceProvider, byte[]? stateToReplicate = null)
    {
        return ReplicateBarricadeState(drop, serviceProvider.GetRequiredService<IPlayerService>(), serviceProvider.GetService<SignInstancer>(), stateToReplicate);
    }

    /// <summary>
    /// Sends the given <paramref name="stateToReplicate"/>, which defaults to the barricade's state if <see langword="null"/> to all relevant clients.
    /// </summary>
    /// <returns><see langword="true"/> if the barricade state was replicated, otherwise <see langword="false"/> (due to reflection failure or out of bounds barricade).</returns>
    public static bool ReplicateBarricadeState(BarricadeDrop drop, IPlayerService playerService, SignInstancer? signs, byte[]? stateToReplicate = null)
    {
        if (Data.SendUpdateBarricadeState == null || !BarricadeManager.tryGetRegion(drop.model, out byte x, out byte y, out ushort plant, out _))
            return false;

        BarricadeData bData = drop.GetServersideData();
        NetId id = drop.GetNetId();

        stateToReplicate ??= bData.barricade.state;

        switch (drop.interactable)
        {
            // special case to handle the client-side state difference between storages
            case InteractableStorage storage:
                byte[] state = new byte[GetClientsideStorageStateLength(storage)];
                WriteClientsideStorageState(
                    state,
                    storage,
                    new CSteamID(stateToReplicate.Length >= sizeof(ulong) * 2 ? BitConverter.ToUInt64(stateToReplicate, 0) : bData.owner),
                    new CSteamID(stateToReplicate.Length >= sizeof(ulong) * 2 ? BitConverter.ToUInt64(stateToReplicate, sizeof(ulong)) : bData.group)
                );
                Data.SendUpdateBarricadeState.Invoke(id, ENetReliability.Reliable, BarricadeManager.GatherRemoteClientConnections(x, y, plant), state);
                break;

            // special case to handle sign translations being replicated to the right players
            case InteractableSign:
                if (signs == null || !signs.IsInstanced(drop))
                {
                    // don't translate the sign
                    Data.SendUpdateBarricadeState.Invoke(id, ENetReliability.Reliable, BarricadeManager.GatherRemoteClientConnections(x, y, plant), stateToReplicate);
                    break;
                }

                state = null!;
                byte[]? utf8Buffer = null;
                foreach (WarfarePlayer player in playerService.OnlinePlayers)
                {
                    if (plant == ushort.MaxValue &&
                        !Regions.checkArea(x, y, player.UnturnedPlayer.movement.region_x, player.UnturnedPlayer.movement.region_y, BarricadeManager.BARRICADE_REGIONS))
                        continue;

                    ReadOnlySpan<char> signText = signs.GetSignText(drop, player);

                    int byteCt = Math.Min(Encoding.UTF8.GetByteCount(signText), byte.MaxValue - 17);
                    if (utf8Buffer == null || utf8Buffer.Length < byteCt)
                    {
                        utf8Buffer = new byte[byteCt];
                    }

                    byteCt = Encoding.UTF8.GetBytes(signText, utf8Buffer.AsSpan(0, byteCt));
                    if (state == null || state.Length != byteCt + 17)
                    {
                        state = new byte[byteCt + 17];
                        if (stateToReplicate.Length >= sizeof(ulong) * 2)
                        {
                            Buffer.BlockCopy(stateToReplicate, 0, state, 0, sizeof(ulong) * 2);
                        }
                        state[sizeof(ulong) * 2] = (byte)byteCt;
                    }

                    Buffer.BlockCopy(utf8Buffer, 0, state, sizeof(ulong) * 2, byteCt);
                    Data.SendUpdateBarricadeState.Invoke(id, ENetReliability.Reliable, player.Connection, state);
                }

                break;

            default:
                Data.SendUpdateBarricadeState.Invoke(id, ENetReliability.Reliable, BarricadeManager.GatherRemoteClientConnections(x, y, plant), stateToReplicate);
                break;
        }

        return true;
    }

    /// <summary>
    /// Set the owner and/or group of a barricade.
    /// </summary>
    /// <returns><see langword="true"/> if the barricade state was replicated, otherwise <see langword="false"/>.</returns>
    public static bool SetOwnerOrGroup(BarricadeDrop drop, IServiceProvider serviceProvider, CSteamID? owner = null, CSteamID? group = null)
    {
        return SetOwnerOrGroup(drop, serviceProvider.GetRequiredService<IPlayerService>(), serviceProvider.GetService<SignInstancer>(), owner, group);
    }

    /// <summary>
    /// Set the owner and/or group of a barricade.
    /// </summary>
    /// <returns><see langword="true"/> if the barricade state was replicated, otherwise <see langword="false"/>.</returns>
    public static bool SetOwnerOrGroup(BarricadeDrop drop, IPlayerService playerService, SignInstancer? signs, CSteamID? owner = null, CSteamID? group = null)
    {
        GameThread.AssertCurrent();
        if (!owner.HasValue && !group.HasValue)
            return false;
        BarricadeData bdata = drop.GetServersideData();
        ulong o = owner?.m_SteamID ?? bdata.owner;
        ulong g = group?.m_SteamID ?? bdata.group;
        BarricadeManager.changeOwnerAndGroup(drop.model, o, g);
        byte[] oldSt = bdata.barricade.state;
        byte[] state;
        switch (drop.interactable)
        {
            // special case to handle the client-side state difference between storages
            case InteractableStorage storage:
                if (oldSt.Length < sizeof(ulong) * 2)
                    oldSt = new byte[sizeof(ulong) * 2];
                BitConverter.TryWriteBytes(oldSt, o);
                BitConverter.TryWriteBytes(oldSt.AsSpan(sizeof(ulong)), o);
                BarricadeManager.updateState(drop.model, oldSt, oldSt.Length);
                drop.ReceiveUpdateState(oldSt);

                if (Data.SendUpdateBarricadeState == null || !BarricadeManager.tryGetRegion(drop.model, out byte x, out byte y, out ushort plant, out _))
                    return false;

                state = new byte[GetClientsideStorageStateLength(storage)];
                WriteClientsideStorageState(state, storage, new CSteamID(o), new CSteamID(g));
                Data.SendUpdateBarricadeState.Invoke(drop.GetNetId(), ENetReliability.Reliable, BarricadeManager.GatherRemoteClientConnections(x, y, plant), state);
                return true;

            // special case to handle sign translations being replicated to the right players
            case InteractableSign:
                if (oldSt.Length < sizeof(ulong) * 2 + 1)
                    oldSt = new byte[sizeof(ulong) * 2 + 1];
                BitConverter.TryWriteBytes(oldSt, o);
                BitConverter.TryWriteBytes(oldSt.AsSpan(sizeof(ulong)), o);
                if (signs == null
                    || !signs.IsInstanced(drop)
                    || Data.SendUpdateBarricadeState == null
                    || !BarricadeManager.tryGetRegion(drop.model, out x, out y, out plant, out _)
                )
                {
                    // don't translate the sign
                    BarricadeManager.updateReplicatedState(drop.model, oldSt, oldSt.Length);
                    return true;
                }

                BarricadeManager.updateState(drop.model, oldSt, oldSt.Length);
                drop.ReceiveUpdateState(oldSt);
                NetId id = drop.GetNetId();
                state = null!;
                byte[]? utf8Buffer = null;
                foreach (WarfarePlayer player in playerService.OnlinePlayers)
                {
                    if (plant == ushort.MaxValue && !Regions.checkArea(x, y,
                            player.UnturnedPlayer.movement.region_x, player.UnturnedPlayer.movement.region_y,
                            BarricadeManager.BARRICADE_REGIONS))
                        continue;

                    ReadOnlySpan<char> signText = signs.GetSignText(drop, player);

                    int byteCt = Math.Min(Encoding.UTF8.GetByteCount(signText), byte.MaxValue - 17);
                    if (utf8Buffer == null || utf8Buffer.Length < byteCt)
                    {
                        utf8Buffer = new byte[byteCt];
                    }

                    byteCt = Encoding.UTF8.GetBytes(signText, utf8Buffer.AsSpan(0, byteCt));
                    if (state == null || state.Length != byteCt + 17)
                    {
                        state = new byte[byteCt + 17];
                        Buffer.BlockCopy(oldSt, 0, state, 0, sizeof(ulong) * 2);
                        state[sizeof(ulong) * 2] = (byte)byteCt;
                    }

                    Buffer.BlockCopy(utf8Buffer, 0, state, 17, byteCt);
                    Data.SendUpdateBarricadeState.Invoke(id, ENetReliability.Reliable, player.Connection, state);
                }

                return true;
        }

        switch (drop.asset.build)
        {
            case EBuild.DOOR:
            case EBuild.GATE:
            case EBuild.SHUTTER:
            case EBuild.HATCH:
                state = new byte[17];
                BitConverter.TryWriteBytes(state, o);
                BitConverter.TryWriteBytes(state.AsSpan(sizeof(ulong)), o);
                state[16] = (byte)(oldSt[16] > 0 ? 1 : 0);
                break;

            case EBuild.BED:
                state = BitConverter.GetBytes(o);
                break;

            case EBuild.STORAGE:
            case EBuild.SENTRY:
            case EBuild.SENTRY_FREEFORM:
            case EBuild.SIGN:
            case EBuild.SIGN_WALL:
            case EBuild.NOTE:
            case EBuild.LIBRARY:
            case EBuild.MANNEQUIN:
                state = oldSt.Length < sizeof(ulong) * 2
                    ? new byte[sizeof(ulong) * 2]
                    : oldSt.CloneBytes();
                BitConverter.TryWriteBytes(state, o);
                BitConverter.TryWriteBytes(state.AsSpan(sizeof(ulong)), o);
                break;

            case EBuild.SPIKE:
            case EBuild.WIRE:
            case EBuild.CHARGE:
            case EBuild.BEACON:
            case EBuild.CLAIM:
                state = oldSt.Length == 0 ? oldSt : Array.Empty<byte>();
                if (drop.interactable is InteractableCharge charge)
                {
                    charge.owner = o;
                    charge.group = g;
                }
                else if (drop.interactable is InteractableClaim claim)
                {
                    claim.owner = o;
                    claim.group = g;
                }
                break;

            default:
                state = oldSt;
                break;
        }

        bool isDifferent = state.Length != oldSt.Length;
        if (!isDifferent)
        {
            for (int i = 0; i < state.Length; ++i)
            {
                if (state[i] == oldSt[i])
                    continue;

                isDifferent = true;
                break;
            }
        }

        if (isDifferent)
            BarricadeManager.updateReplicatedState(drop.model, state, state.Length);

        return isDifferent;
    }

    /// <summary>
    /// Find a barricade by it's instance ID, with help from an expected region to prevent having to search every region.
    /// </summary>
    /// <remarks>All regions will be searched if it's not found in the expected region. Only instance ID is checked on planted barricades.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo FindBarricade(uint instanceId, IAssetLink<ItemBarricadeAsset> expectedAsset, Vector3 expectedPosition)
    {
        GameThread.AssertCurrent();

        BarricadeInfo foundByPosition = default;

        SurroundingRegionsIterator iterator = RegionUtility.EnumerateRegions(expectedPosition);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];

                if (drop.instanceID == instanceId && expectedAsset.MatchAsset(drop.asset))
                    return new BarricadeInfo(drop, i, coord);

                Vector3 pos = drop.GetServersideData().point;
                if (!pos.IsNearlyEqual(expectedPosition, 0.1f) || !expectedAsset.MatchAsset(drop.asset))
                    continue;

                // if not found or the one found is farther from the expected point than this one
                if (foundByPosition.Drop == null
                    || (foundByPosition.Drop.GetServersideData().point - expectedPosition).sqrMagnitude > (pos - expectedPosition).sqrMagnitude)
                {
                    foundByPosition = new BarricadeInfo(drop, i, coord);
                }
            }
        }

        IReadOnlyList<VehicleBarricadeRegion> vRegions = BarricadeManager.vehicleRegions;
        int ct = Math.Min(ushort.MaxValue - 1, vRegions.Count);
        for (int r = 0; r < ct; ++r)
        {
            List<BarricadeDrop> drops = vRegions[r].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                if (drops[i].instanceID == instanceId)
                    return new BarricadeInfo(drops[i], i, (ushort)r);
            }
        }

        if (foundByPosition.Drop != null)
        {
            return foundByPosition;
        }

        if (!Regions.tryGetCoordinate(expectedPosition, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        return new BarricadeInfo(null, -1, new RegionCoord(x, y));
    }

    /// <summary>
    /// Check for a nearby barricade with the given <paramref name="asset"/> to <paramref name="position"/> within the given <paramref name="radius"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static bool IsBarricadeInRange(Vector3 position, float radius, IAssetLink<ItemBarricadeAsset> asset, bool horizontalDistanceOnly = false)
    {
        return GetClosestBarricadeInRange(position, radius, asset, horizontalDistanceOnly).Drop != null;
    }

    /// <summary>
    /// Check for a nearby barricade matching a predicate to <paramref name="position"/> within the given <paramref name="radius"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static bool IsBarricadeInRange(Vector3 position, float radius, Predicate<BarricadeDrop> barricadeSelector, bool horizontalDistanceOnly = false)
    {
        return GetClosestBarricadeWhere(position, radius, barricadeSelector, horizontalDistanceOnly).Drop != null;
    }

    /// <summary>
    /// Check for a nearby barricade to <paramref name="position"/> within the given <paramref name="radius"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static bool IsBarricadeInRange(Vector3 position, float radius, bool horizontalDistanceOnly = false)
    {
        return GetClosestBarricadeInRange(position, radius, horizontalDistanceOnly).Drop != null;
    }

    /// <summary>
    /// Check for a nearby barricade with the given <paramref name="asset"/> to <paramref name="position"/> within the given <paramref name="radius"/> and <paramref name="group"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static bool IsBarricadeInRange(Vector3 position, float radius, ulong group, IAssetLink<ItemBarricadeAsset> asset, bool horizontalDistanceOnly = false)
    {
        return GetClosestBarricadeInRange(position, radius, group, asset, horizontalDistanceOnly).Drop != null;
    }

    /// <summary>
    /// Check for a nearby barricade matching a predicate to <paramref name="position"/> within the given <paramref name="radius"/> and <paramref name="group"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static bool IsBarricadeInRange(Vector3 position, float radius, ulong group, Predicate<BarricadeDrop> barricadeSelector, bool horizontalDistanceOnly = false)
    {
        return GetClosestBarricadeWhere(position, radius, group, barricadeSelector, horizontalDistanceOnly).Drop != null;
    }

    /// <summary>
    /// Check for a nearby barricade to <paramref name="position"/> within the given <paramref name="radius"/> and <paramref name="group"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static bool IsBarricadeInRange(Vector3 position, float radius, ulong group, bool horizontalDistanceOnly = false)
    {
        return GetClosestBarricadeInRange(position, radius, group, horizontalDistanceOnly).Drop != null;
    }

    /// <summary>
    /// Find the closest barricade with the given <paramref name="asset"/> to <paramref name="position"/> within the given <paramref name="radius"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo GetClosestBarricadeInRange(Vector3 position, float radius, IAssetLink<ItemBarricadeAsset> asset, bool horizontalDistanceOnly = false)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        GameThread.AssertCurrent();

        float closestSqrDist = 0f;
        float sqrRadius = radius * radius;
        BarricadeInfo closest = default;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];

                float sqrDist = MathUtility.SquaredDistance(in position, in drop.GetServersideData().point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || sqrDist > sqrRadius || !asset.MatchAsset(drop.asset))
                    continue;

                closest = new BarricadeInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest barricade with the given <paramref name="asset"/> to <paramref name="position"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo GetClosestBarricade(Vector3 position, IAssetLink<ItemBarricadeAsset> asset, bool horizontalDistanceOnly = false)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        GameThread.AssertCurrent();

        float closestSqrDist = 0f;
        BarricadeInfo closest = default;
        if (!Regions.tryGetCoordinate(position, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator(x, y);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];

                float sqrDist = MathUtility.SquaredDistance(in position, in drop.GetServersideData().point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || !asset.MatchAsset(drop.asset))
                    continue;

                closest = new BarricadeInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest barricade with the given <paramref name="asset"/> to <paramref name="position"/> within the given <paramref name="radius"/> and <paramref name="group"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo GetClosestBarricadeInRange(Vector3 position, float radius, ulong group, IAssetLink<ItemBarricadeAsset> asset, bool horizontalDistanceOnly = false)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        GameThread.AssertCurrent();

        float closestSqrDist = 0f;
        float sqrRadius = radius * radius;
        BarricadeInfo closest = default;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];
                BarricadeData data = drop.GetServersideData();
                if (data.group != group)
                    continue;

                float sqrDist = MathUtility.SquaredDistance(in position, in data.point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || sqrDist > sqrRadius || !asset.MatchAsset(drop.asset))
                    continue;

                closest = new BarricadeInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest barricade with the given <paramref name="asset"/> to <paramref name="position"/> with the given <paramref name="group"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo GetClosestBarricade(Vector3 position, ulong group, IAssetLink<ItemBarricadeAsset> asset, bool horizontalDistanceOnly = false)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        GameThread.AssertCurrent();

        float closestSqrDist = 0f;
        BarricadeInfo closest = default;
        if (!Regions.tryGetCoordinate(position, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator(x, y);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];
                BarricadeData data = drop.GetServersideData();
                if (data.group != group)
                    continue;

                float sqrDist = MathUtility.SquaredDistance(in position, in data.point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || !asset.MatchAsset(drop.asset))
                    continue;

                closest = new BarricadeInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest barricade to <paramref name="position"/> within the given <paramref name="radius"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo GetClosestBarricadeInRange(Vector3 position, float radius, bool horizontalDistanceOnly = false)
    {
        GameThread.AssertCurrent();
     
        float closestSqrDist = 0f;
        float sqrRadius = radius * radius;
        BarricadeInfo closest = default;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];

                float sqrDist = MathUtility.SquaredDistance(in position, in drop.GetServersideData().point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || sqrDist > sqrRadius)
                    continue;

                closest = new BarricadeInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest barricade to <paramref name="position"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo GetClosestBarricade(Vector3 position, bool horizontalDistanceOnly = false)
    {
        GameThread.AssertCurrent();

        float closestSqrDist = 0f;
        BarricadeInfo closest = default;
        if (!Regions.tryGetCoordinate(position, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator(x, y);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];

                float sqrDist = MathUtility.SquaredDistance(in position, in drop.GetServersideData().point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist)
                    continue;

                closest = new BarricadeInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest barricade to <paramref name="position"/> within the given <paramref name="radius"/> and <paramref name="group"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo GetClosestBarricadeInRange(Vector3 position, float radius, ulong group, bool horizontalDistanceOnly = false)
    {
        GameThread.AssertCurrent();
     
        float closestSqrDist = 0f;
        float sqrRadius = radius * radius;
        BarricadeInfo closest = default;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];
                BarricadeData data = drop.GetServersideData();

                if (data.group != group)
                    continue;

                float sqrDist = MathUtility.SquaredDistance(in position, in data.point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || sqrDist > sqrRadius)
                    continue;

                closest = new BarricadeInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest barricade to <paramref name="position"/> with the given <paramref name="group"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo GetClosestBarricade(Vector3 position, ulong group, bool horizontalDistanceOnly = false)
    {
        GameThread.AssertCurrent();

        float closestSqrDist = 0f;
        BarricadeInfo closest = default;
        if (!Regions.tryGetCoordinate(position, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator(x, y);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];
                BarricadeData data = drop.GetServersideData();

                if (data.group != group)
                    continue;

                float sqrDist = MathUtility.SquaredDistance(in position, in data.point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist)
                    continue;

                closest = new BarricadeInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest barricade matching a predicate to <paramref name="position"/> within a given <paramref name="radius"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo GetClosestBarricadeWhere(Vector3 position, float radius, Predicate<BarricadeDrop> barricadeSelector, bool horizontalDistanceOnly = false)
    {
        if (barricadeSelector == null)
            throw new ArgumentNullException(nameof(barricadeSelector));

        GameThread.AssertCurrent();

        float closestSqrDist = 0f;
        float sqrRadius = radius * radius;
        BarricadeInfo closest = default;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];

                float sqrDist = MathUtility.SquaredDistance(in position, in drop.GetServersideData().point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || sqrDist > sqrRadius || !barricadeSelector(drop))
                    continue;

                closest = new BarricadeInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest barricade matching a predicate to <paramref name="position"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo GetClosestBarricadeWhere(Vector3 position, Predicate<BarricadeDrop> barricadeSelector, bool horizontalDistanceOnly = false)
    {
        if (barricadeSelector == null)
            throw new ArgumentNullException(nameof(barricadeSelector));

        GameThread.AssertCurrent();

        float closestSqrDist = 0f;
        BarricadeInfo closest = default;
        if (!Regions.tryGetCoordinate(position, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator(x, y);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];

                float sqrDist = MathUtility.SquaredDistance(in position, in drop.GetServersideData().point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || !barricadeSelector(drop))
                    continue;

                closest = new BarricadeInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest barricade matching a predicate to <paramref name="position"/> within a given <paramref name="radius"/> and <paramref name="group"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo GetClosestBarricadeWhere(Vector3 position, float radius, ulong group, Predicate<BarricadeDrop> barricadeSelector, bool horizontalDistanceOnly = false)
    {
        if (barricadeSelector == null)
            throw new ArgumentNullException(nameof(barricadeSelector));

        GameThread.AssertCurrent();

        float closestSqrDist = 0f;
        float sqrRadius = radius * radius;
        BarricadeInfo closest = default;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];
                BarricadeData data = drop.GetServersideData();

                if (data.group != group)
                    continue;

                float sqrDist = MathUtility.SquaredDistance(in position, in data.point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || sqrDist > sqrRadius || !barricadeSelector(drop))
                    continue;

                closest = new BarricadeInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest barricade matching a predicate to <paramref name="position"/> with the given <paramref name="group"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo GetClosestBarricadeWhere(Vector3 position, ulong group, Predicate<BarricadeDrop> barricadeSelector, bool horizontalDistanceOnly = false)
    {
        if (barricadeSelector == null)
            throw new ArgumentNullException(nameof(barricadeSelector));

        GameThread.AssertCurrent();

        float closestSqrDist = 0f;
        BarricadeInfo closest = default;
        if (!Regions.tryGetCoordinate(position, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator(x, y);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];
                BarricadeData data = drop.GetServersideData();

                if (data.group != group)
                    continue;

                float sqrDist = MathUtility.SquaredDistance(in position, in data.point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || !barricadeSelector(drop))
                    continue;

                closest = new BarricadeInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Count the number of barricades in the given <paramref name="radius"/> matching a predicate.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static int CountBarricadesWhere(Vector3 position, float radius, Predicate<BarricadeDrop> barricadeSelector, int max = -1, bool horizontalDistanceOnly = false)
    {
        if (barricadeSelector == null)
            throw new ArgumentNullException(nameof(barricadeSelector));

        GameThread.AssertCurrent();

        float sqrRadius = radius * radius;
        int totalBarricadesFound = 0;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];

                float sqrDist = MathUtility.SquaredDistance(in position, in drop.GetServersideData().point, horizontalDistanceOnly);

                if (sqrDist > sqrRadius || !barricadeSelector(drop))
                    continue;

                ++totalBarricadesFound;
                if (max >= 0 && totalBarricadesFound >= max)
                {
                    return totalBarricadesFound;
                }
            }
        }

        return totalBarricadesFound;
    }

    /// <summary>
    /// Count the number of barricades in the given radius matching a predicate.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static int CountBarricadesWhere(Predicate<BarricadeDrop> barricadeSelector, int max = -1)
    {
        if (barricadeSelector == null)
            throw new ArgumentNullException(nameof(barricadeSelector));

        GameThread.AssertCurrent();

        int totalBarricadesFound = 0;
        SurroundingRegionsIterator iterator = RegionUtility.EnumerateRegions();
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];
                if (!barricadeSelector(drop))
                    continue;

                ++totalBarricadesFound;
                if (max >= 0 && totalBarricadesFound >= max)
                {
                    return totalBarricadesFound;
                }
            }
        }

        IReadOnlyList<VehicleBarricadeRegion> vRegions = BarricadeManager.vehicleRegions;
        int ct = Math.Min(ushort.MaxValue - 1, vRegions.Count);
        for (int r = 0; r < ct; ++r)
        {
            List<BarricadeDrop> drops = vRegions[r].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];
                if (!barricadeSelector(drop))
                    continue;

                ++totalBarricadesFound;
                if (max >= 0 && totalBarricadesFound >= max)
                {
                    return totalBarricadesFound;
                }
            }
        }

        return totalBarricadesFound;
    }

    /// <summary>
    /// Count the number of barricades in the given <paramref name="radius"/> matching an <paramref name="asset"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static int CountBarricadesInRange(Vector3 position, float radius, IAssetLink<ItemBarricadeAsset> asset, int max = -1, bool horizontalDistanceOnly = false)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        GameThread.AssertCurrent();

        float sqrRadius = radius * radius;
        int totalBarricadesFound = 0;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];

                float sqrDist = MathUtility.SquaredDistance(in position, in drop.GetServersideData().point, horizontalDistanceOnly);

                if (sqrDist > sqrRadius || !asset.MatchAsset(drop.asset))
                    continue;

                ++totalBarricadesFound;
                if (max >= 0 && totalBarricadesFound >= max)
                {
                    return totalBarricadesFound;
                }
            }
        }

        return totalBarricadesFound;
    }

    /// <summary>
    /// Count the number of barricades in the given radius matching an <paramref name="asset"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static int CountBarricades(IAssetLink<ItemBarricadeAsset> asset, int max = -1)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        GameThread.AssertCurrent();

        int totalBarricadesFound = 0;
        SurroundingRegionsIterator iterator = RegionUtility.EnumerateRegions();
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];
                if (!asset.MatchAsset(drop.asset))
                    continue;

                ++totalBarricadesFound;
                if (max >= 0 && totalBarricadesFound >= max)
                {
                    return totalBarricadesFound;
                }
            }
        }

        IReadOnlyList<VehicleBarricadeRegion> vRegions = BarricadeManager.vehicleRegions;
        int ct = Math.Min(ushort.MaxValue - 1, vRegions.Count);
        for (int r = 0; r < ct; ++r)
        {
            List<BarricadeDrop> drops = vRegions[r].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];
                if (!asset.MatchAsset(drop.asset))
                    continue;

                ++totalBarricadesFound;
                if (max >= 0 && totalBarricadesFound >= max)
                {
                    return totalBarricadesFound;
                }
            }
        }

        return totalBarricadesFound;
    }

    /// <summary>
    /// Count the number of barricades in the given radius.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static int CountBarricadesInRange(Vector3 position, float radius, int max = -1, bool horizontalDistanceOnly = false)
    {
        GameThread.AssertCurrent();

        float sqrRadius = radius * radius;
        int totalBarricadesFound = 0;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];

                float sqrDist = MathUtility.SquaredDistance(in position, in drop.GetServersideData().point, horizontalDistanceOnly);

                if (sqrDist > sqrRadius)
                    continue;

                ++totalBarricadesFound;
                if (max >= 0 && totalBarricadesFound >= max)
                {
                    return totalBarricadesFound;
                }
            }
        }

        return totalBarricadesFound;
    }
}

/// <summary>
/// Stores return information about a barricade including it's region information.
/// </summary>
/// <remarks>Only valid for one frame, shouldn't be stored for longer than that.</remarks>
public readonly struct BarricadeInfo
{
#nullable disable
    public BarricadeDrop Drop { get; }
    public BarricadeData Data => Drop?.GetServersideData();
#nullable restore
    public bool HasValue => Drop != null;

    /// <summary>
    /// Coordinates of the region the barricade is in, if it's not on a vehicle.
    /// </summary>
    public RegionCoord Coord { get; }

    /// <summary>
    /// Index of the barricade in it's region's drop list.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// The index of the vehicle region in <see cref="BarricadeManager.vehicleRegions"/>.
    /// </summary>
    public ushort Plant { get; }
    public bool IsOnVehicle => Plant != ushort.MaxValue;

    public BarricadeInfo(BarricadeDrop? drop, int index, RegionCoord coord)
    {
        Drop = drop;
        Coord = coord;
        Index = index;
        Plant = ushort.MaxValue;
    }
    public BarricadeInfo(BarricadeDrop? drop, int index, ushort plant)
    {
        Drop = drop;
        Index = index;
        Plant = plant;
    }

    [Pure]
    public BarricadeRegion GetRegion()
    {
        if (Drop == null)
            throw new NullReferenceException("This info doesn't store a valid BarricadeDrop instance.");

        if (Plant != ushort.MaxValue)
            return BarricadeManager.vehicleRegions[Plant];

        RegionCoord regionCoord = Coord;
        return BarricadeManager.regions[regionCoord.x, regionCoord.y];
    }
}