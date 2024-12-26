using Microsoft.Extensions.DependencyInjection;
using SDG.NetTransport;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Uncreated.Warfare.Configuration;
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
    private static readonly ClientInstanceMethod<byte[]>? SendUpdateState
        = ReflectionUtility.FindRpc<BarricadeDrop, ClientInstanceMethod<byte[]>>("SendUpdateState");

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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateBarricades()
    {
        GameThread.AssertCurrent();

        return new BarricadeIterator((byte)(Regions.WORLD_SIZE / 2), (byte)(Regions.WORLD_SIZE / 2), true, true);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around <paramref name="center"/>, then planted barricades (barricades on a vehicle).
    /// </summary>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateBarricades(byte x, byte y)
    {
        GameThread.AssertCurrent();

        return new BarricadeIterator(x, y, true, true);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around <paramref name="region"/>, then planted barricades (barricades on a vehicle).
    /// </summary>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateBarricades(RegionCoord region, byte maxRegionDistance)
    {
        GameThread.AssertCurrent();

        return new BarricadeIterator(region.x, region.y, true, true, maxRegionDistance);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around the center of the level.
    /// </summary>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateNonPlantedBarricades()
    {
        GameThread.AssertCurrent();

        return new BarricadeIterator((byte)(Regions.WORLD_SIZE / 2), (byte)(Regions.WORLD_SIZE / 2), true, false);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around <paramref name="center"/>.
    /// </summary>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateNonPlantedBarricades(byte x, byte y)
    {
        GameThread.AssertCurrent();

        return new BarricadeIterator(x, y, true, false);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around <paramref name="region"/>.
    /// </summary>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateNonPlantedBarricades(RegionCoord region, byte maxRegionDistance)
    {
        GameThread.AssertCurrent();

        return new BarricadeIterator(region.x, region.y, true, false, maxRegionDistance);
    }

    /// <summary>
    /// Enumerate through planted barricades (barricades on a vehicle).
    /// </summary>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumeratePlantedBarricades()
    {
        GameThread.AssertCurrent();

        return new BarricadeIterator((byte)(Regions.WORLD_SIZE / 2), (byte)(Regions.WORLD_SIZE / 2), false, true);
    }

    /// <summary>
    /// Find a barricade by it's instance ID.
    /// </summary>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo FindBarricade(uint instanceId)
    {
        return FindBarricade(instanceId, (byte)(Regions.WORLD_SIZE / 2), (byte)(Regions.WORLD_SIZE / 2));
    }

    /// <summary>
    /// Find a barricade by it's instance ID, with help from a position to prevent having to search every region.
    /// </summary>
    /// <remarks>All regions will be searched if it's not found near the expected position.</remarks>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// Set the state of any barricade and properly update it for the client.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    /// <exception cref="InvalidBarricadeStateException"/>
    public static void SetState(BarricadeDrop barricade, byte[] state)
    {
        GameThread.AssertCurrent();

        VerifyState(state, barricade.asset);

        switch (barricade.interactable)
        {
            case InteractableStorage storage:
                // storages have different states on clientside

                // close storage user
                if (storage.opener != null)
                {
                    if (storage.opener.inventory.isStoring)
                        storage.opener.inventory.closeStorageAndNotifyClient();
                    storage.opener = null;
                    storage.isOpen = false;
                }

                BarricadeManager.updateState(barricade.model, state, state.Length);
                state = barricade.GetServersideData().barricade.state;
                storage.updateState(barricade.asset, state);

                if (SendUpdateState == null || !BarricadeManager.tryGetRegion(barricade.model, out byte x, out byte y, out ushort plant, out _))
                    break;

                byte[] clientState = new byte[GetClientsideStorageStateLength(storage)];
                WriteClientsideStorageState(clientState, storage, MemoryMarshal.Read<CSteamID>(state), MemoryMarshal.Read<CSteamID>(state[8..]));
                SendUpdateState.Invoke(barricade.GetNetId(), ENetReliability.Reliable, BarricadeManager.GatherRemoteClientConnections(x, y, plant), clientState);
                break;

            case InteractableSign:
                // invokes the sign updated event
                byte count = state[16];
                string text = count == 0 ? string.Empty : Encoding.UTF8.GetString(state, 17, count);
                SetServersideSignText(barricade, text);
                return;

            default:
                // update other barricades normally
                BarricadeManager.updateReplicatedState(barricade.model, state, state.Length);
                break;
        }
    }

    private static Encoder? _utf8Encoder;

    /// <summary>
    /// Sets the sign text without replicating to clients.
    /// </summary>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    /// <exception cref="ArgumentException"><paramref name="barricade"/> is not a sign.</exception>
    public static void SetServersideSignText(BarricadeDrop barricade, ReadOnlySpan<char> text)
    {
        GameThread.AssertCurrent();
        
        if (barricade.interactable is not InteractableSign sign)
            throw new ArgumentException("Barricade must be a sign.", nameof(barricade));

        int byteCt = Encoding.UTF8.GetByteCount(text);
        if (byteCt + 17 > byte.MaxValue)
        {
            WarfareModule.Singleton.GlobalLogger.LogWarning(text.Concat(" is too long to go on a sign! (SetServersideSignText)"));
            byteCt = byte.MaxValue - 17;
        }

        byte[] newState = new byte[byteCt + 17];

        Unsafe.WriteUnaligned(ref newState[0], sign.owner.m_SteamID);
        Unsafe.WriteUnaligned(ref newState[8], sign.group.m_SteamID);

        int length = 17;
        if (byteCt != 0)
        {
            _utf8Encoder ??= Encoding.UTF8.GetEncoder();
            _utf8Encoder.Convert(text, newState.AsSpan(17), true, out _, out int bytesUsed, out _);
            newState[16] = (byte)bytesUsed;
            length = 17 + bytesUsed;
        }

        if (length != newState.Length)
        {
            byte[] old = newState;
            newState = new byte[length];
            Buffer.BlockCopy(old, 0, newState, 0, length);
        }

        BarricadeManager.updateState(barricade.model, newState, length);
        sign.updateState(barricade.asset, newState);
    }

    /// <summary>
    /// Throw an exception if the state isn't valid for this barricade type.
    /// </summary>
    /// <remarks>Note that states that are too long will not throw exceptions, only states that are too short or are structually invalid somehow.</remarks>
    /// <exception cref="InvalidBarricadeStateException"/>
    public static void VerifyState(ReadOnlySpan<byte> state, ItemBarricadeAsset barricade)
    {
        switch (barricade.build)
        {
            case EBuild.DOOR:
            case EBuild.GATE:
            case EBuild.SHUTTER:
            case EBuild.HATCH:
            case EBuild.STEREO:
                if (state.Length < 17)
                    throw new InvalidBarricadeStateException(barricade.build, 17);
                break;

            case EBuild.BED:
                if (state.Length < 8)
                    throw new InvalidBarricadeStateException(barricade.build, 8);
                break;

            case EBuild.STORAGE:
            case EBuild.STORAGE_WALL:
            case EBuild.SENTRY:
            case EBuild.SENTRY_FREEFORM:
                if (state.Length < 17)
                    throw new InvalidBarricadeStateException(barricade.build, 17);
                int itemCt = state[16];
                int startIndex = 17;
                for (int i = 0; i < itemCt; ++i)
                {
                    int minimumRemainingLength = startIndex + (i - itemCt) * 8;
                    if (state.Length < minimumRemainingLength)
                        throw new InvalidBarricadeStateException(barricade.build, minimumRemainingLength);

                    byte stateLen = state[startIndex + 7];
                    startIndex += 8 + stateLen;
                }

                if (state.Length < startIndex)
                    throw new InvalidBarricadeStateException(barricade.build, startIndex);

                if (barricade is not ItemStorageAsset { isDisplay: true })
                    break;

                startIndex += 4;
                if (state.Length < startIndex + 3)
                    throw new InvalidBarricadeStateException(barricade.build, startIndex);

                byte tagsStrLen = state[startIndex];
                if (tagsStrLen != 0)
                {
                    startIndex += 1 + tagsStrLen;
                }
                if (state.Length < startIndex + 2)
                    throw new InvalidBarricadeStateException(barricade.build, startIndex);

                byte propsStrLen = state[startIndex];
                if (propsStrLen != 0)
                {
                    startIndex += 1 + propsStrLen;
                }

                if (state.Length < startIndex + 1)
                    throw new InvalidBarricadeStateException(barricade.build, startIndex);

                if (27 + tagsStrLen + propsStrLen > byte.MaxValue)
                    throw new InvalidBarricadeStateException(barricade.build, $"Display tags and dynamic properties are too long: {tagsStrLen} and {propsStrLen} UTF-8 bytes respectively. Together they must be below {byte.MaxValue - 27} bytes.");
                break;

            case EBuild.TORCH:
            case EBuild.CAMPFIRE:
            case EBuild.OVEN:
            case EBuild.SPOT:
            case EBuild.CAGE:
            case EBuild.SAFEZONE:
            case EBuild.OXYGENATOR:
            case EBuild.BARREL_RAIN:
                if (state.Length < 1)
                    throw new InvalidBarricadeStateException(barricade.build, 1);
                break;

            case EBuild.GENERATOR:
                if (state.Length < 3)
                    throw new InvalidBarricadeStateException(barricade.build, 3);
                break;

            case EBuild.SIGN:
            case EBuild.SIGN_WALL:
            case EBuild.NOTE:
                if (state.Length < 17)
                    throw new InvalidBarricadeStateException(barricade.build, 17);

                int strLen = state[16];
                if (state.Length < 17 + strLen)
                    throw new InvalidBarricadeStateException(barricade.build, 17 + strLen);
                if (strLen > byte.MaxValue - 17)
                    throw new InvalidBarricadeStateException(barricade.build, $"Sign text too long: {strLen} UTF-8 bytes. Must be below {byte.MaxValue - 17} bytes.");
                break;

            case EBuild.OIL:
            case EBuild.TANK:
                if (state.Length < 2)
                    throw new InvalidBarricadeStateException(barricade.build, 2);
                break;

            case EBuild.LIBRARY:
                if (state.Length < 20)
                    throw new InvalidBarricadeStateException(barricade.build, 20);
                break;

            case EBuild.MANNEQUIN:
                if (state.Length < 73)
                    throw new InvalidBarricadeStateException(barricade.build, 17);

                int index = 65;
                for (int i = 0; i < 7; ++i)
                {
                    int stateLen = state[index];
                    index += stateLen + 1;
                    if (state.Length < index)
                        throw new InvalidBarricadeStateException(barricade.build, index);
                }
                if (state.Length < index + 1)
                    throw new InvalidBarricadeStateException(barricade.build, index + 1);
                break;
        }
    }

    /// <summary>
    /// Write the group and owner to a span of bytes to prepare it for a new barricade.
    /// </summary>
    /// <exception cref="InvalidBarricadeStateException"/>
    public static void WriteOwnerAndGroup(Span<byte> state, BarricadeDrop drop, ulong owner, ulong group)
    {
        EBuild build = drop.asset.build;

        // write owner and group to interactables where needed
        switch (build)
        {
            case EBuild.DOOR:
            case EBuild.GATE:
            case EBuild.SHUTTER:
            case EBuild.HATCH:
            case EBuild.LIBRARY:
            case EBuild.SIGN:
            case EBuild.SIGN_WALL:
            case EBuild.NOTE:
            case EBuild.STORAGE:
            case EBuild.STORAGE_WALL:
                if (state.Length < 16)
                    throw new InvalidBarricadeStateException(build, 8);

                MemoryMarshal.Write(state, ref owner);
                MemoryMarshal.Write(state[8..], ref group);
                break;
        }
    }

    /// <summary>
    /// Get the exact length in bytes of the client-side state for a storage.
    /// </summary>
    public static int GetClientsideStorageStateLength(InteractableStorage storage)
    {
        if (!storage.isDisplay)
        {
            return 16;
        }

        int length = 11;
        if (storage.displayItem != null)
            length += storage.displayItem.state.Length;
        Encoding encoding = Encoding.UTF8;
        if (!string.IsNullOrEmpty(storage.displayTags))
            length += encoding.GetByteCount(storage.displayTags);
        if (!string.IsNullOrEmpty(storage.displayDynamicProps))
            length += encoding.GetByteCount(storage.displayDynamicProps);

        return 16 + length;
    }

    /// <summary>
    /// Write the bytes of the client-side state for a storage.
    /// </summary>
    /// <remarks>Use <see cref="GetClientsideStorageStateLength"/> to pre-calculate the length.</remarks>
    /// <exception cref="ArgumentException">Output is not long enough for the full state.</exception>
    /// <exception cref="OverflowException">Displayed item's state or display metadata is longer than 255 elements.</exception>
    public static int WriteClientsideStorageState(Span<byte> output, InteractableStorage storage, CSteamID owner, CSteamID group)
    {
        if (output.Length < (!storage.isDisplay ? 16 : 27))
            throw new ArgumentException("Output not long enough for state.", nameof(output));

        MemoryMarshal.Write(output, ref owner);
        MemoryMarshal.Write(output[8..], ref group);

        if (!storage.isDisplay)
            return 16;

        int index = 16;

        ushort tempRef2;
        if (storage.displayItem != null)
        {
            tempRef2 = storage.displayItem.id;
            MemoryMarshal.Write(output[index..], ref tempRef2);
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

        tempRef2 = storage.displaySkin;
        MemoryMarshal.Write(output[index..], ref tempRef2);
        index += sizeof(ushort);

        tempRef2 = storage.displayMythic;
        MemoryMarshal.Write(output[index..], ref tempRef2);
        index += sizeof(ushort);

        if (!string.IsNullOrEmpty(storage.displayTags))
        {
            byte byteCt = checked( (byte)Encoding.UTF8.GetBytes(storage.displayTags, output[(index + 1)..]) );
            output[index] = byteCt;
            index += byteCt + 1;
        }
        else
        {
            ++index;
            output[index] = 0;
        }

        if (!string.IsNullOrEmpty(storage.displayDynamicProps))
        {
            byte byteCt = checked( (byte)Encoding.UTF8.GetBytes(storage.displayDynamicProps, output[(index + 1)..]) );
            output[index] = byteCt;
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
    public static bool ReplicateBarricadeState(BarricadeDrop drop, IServiceProvider serviceProvider, byte[]? stateToReplicate = null)
    {
        return ReplicateBarricadeState(drop, serviceProvider.GetRequiredService<IPlayerService>(), serviceProvider.GetService<SignInstancer>(), stateToReplicate);
    }

    /// <summary>
    /// Sends the given <paramref name="stateToReplicate"/>, which defaults to the barricade's state if <see langword="null"/> to all relevant clients.
    /// </summary>
    /// <returns><see langword="true"/> if the barricade state was replicated, otherwise <see langword="false"/> (due to reflection failure or out of bounds barricade).</returns>
    public static bool ReplicateBarricadeState(BarricadeDrop drop, IPlayerService playerService, SignInstancer? signs, byte[]? stateToReplicate = null)
    {
        if (SendUpdateState == null || !BarricadeManager.tryGetRegion(drop.model, out byte x, out byte y, out ushort plant, out _))
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
                SendUpdateState.Invoke(id, ENetReliability.Reliable, BarricadeManager.GatherRemoteClientConnections(x, y, plant), state);
                break;

            // special case to handle sign translations being replicated to the right players
            case InteractableSign:
                if (signs == null || !signs.IsInstanced(drop))
                {
                    // don't translate the sign
                    SendUpdateState.Invoke(id, ENetReliability.Reliable, BarricadeManager.GatherRemoteClientConnections(x, y, plant), stateToReplicate);
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
                    SendUpdateState.Invoke(id, ENetReliability.Reliable, player.Connection, state);
                }

                break;

            default:
                SendUpdateState.Invoke(id, ENetReliability.Reliable, BarricadeManager.GatherRemoteClientConnections(x, y, plant), stateToReplicate);
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

                if (SendUpdateState == null || !BarricadeManager.tryGetRegion(drop.model, out byte x, out byte y, out ushort plant, out _))
                    return false;

                state = new byte[GetClientsideStorageStateLength(storage)];
                WriteClientsideStorageState(state, storage, Unsafe.As<ulong, CSteamID>(ref o), Unsafe.As<ulong, CSteamID>(ref g));
                SendUpdateState.Invoke(drop.GetNetId(), ENetReliability.Reliable, BarricadeManager.GatherRemoteClientConnections(x, y, plant), state);
                return true;

            // special case to handle sign translations being replicated to the right players
            case InteractableSign:
                if (oldSt.Length < sizeof(ulong) * 2 + 1)
                    oldSt = new byte[sizeof(ulong) * 2 + 1];
                BitConverter.TryWriteBytes(oldSt, o);
                BitConverter.TryWriteBytes(oldSt.AsSpan(sizeof(ulong)), o);
                if (signs == null
                    || !signs.IsInstanced(drop)
                    || SendUpdateState == null
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
                    SendUpdateState.Invoke(id, ENetReliability.Reliable, player.Connection, state);
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static bool IsBarricadeInRange(Vector3 position, float radius, Predicate<BarricadeDrop> barricadeSelector, bool horizontalDistanceOnly = false)
    {
        return GetClosestBarricadeWhere(position, radius, barricadeSelector, horizontalDistanceOnly).Drop != null;
    }

    /// <summary>
    /// Check for a nearby barricade to <paramref name="position"/> within the given <paramref name="radius"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static bool IsBarricadeInRange(Vector3 position, float radius, ulong group, Predicate<BarricadeDrop> barricadeSelector, bool horizontalDistanceOnly = false)
    {
        return GetClosestBarricadeWhere(position, radius, group, barricadeSelector, horizontalDistanceOnly).Drop != null;
    }

    /// <summary>
    /// Check for a nearby barricade to <paramref name="position"/> within the given <paramref name="radius"/> and <paramref name="group"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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

    /// <summary>
    /// Prevent this barricade from dropping it's items if its about to be destroyed.
    /// </summary>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    public static void PreventItemDrops(BarricadeDrop drop)
    {
        GameThread.AssertCurrent();

        switch (drop.interactable)
        {
            case InteractableStorage storage:
                storage.despawnWhenDestroyed = true;
                break;

            case InteractableMannequin mannequin:
                mannequin.clearClothes();
                break;
        }
    }
}

public class InvalidBarricadeStateException : FormatException
{
    public InvalidBarricadeStateException(EBuild type, string message) 
        : base($"Invalid state on {type} barricade. {message}") { }
    public InvalidBarricadeStateException(EBuild type, int expectedBytes) 
        : this(type, expectedBytes == 1
            ? "Expected at least 1 byte."
            : $"Expected at least {expectedBytes} bytes.") { }
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