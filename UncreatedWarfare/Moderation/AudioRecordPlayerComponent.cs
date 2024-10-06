using DanielWillett.SpeedBytes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;
using Unity.Collections;

namespace Uncreated.Warfare.Moderation;

/// <summary>
/// Highly-effecient voice data recorder that stores a bunch of different packets in one block of bytes.
/// </summary>
public class AudioRecordPlayerComponent : IPlayerComponent
{
    private static readonly ByteWriter MetaWriter = new ByteWriter(capacity: 0);
    private const ushort DataVersion = 1;
    private AudioRecordManager _audioListenService = null!;

    private byte[]? _voiceBuffer;
    private List<PacketInfo>? _packets;
    private IReadOnlyList<PacketInfo>? _packetsReadOnly;
    private int _startIndex;
    private int _byteCount;
    public WarfarePlayer Player { get; set; }
    public int PacketCount => _packets?.Count ?? 0;
    public ArraySegment<byte> RingSectionOne
    {
        get
        {
            return _voiceBuffer == null
                ? default
                : new ArraySegment<byte>(_voiceBuffer, _startIndex, _startIndex + _byteCount > _voiceBuffer.Length
                    ? _voiceBuffer.Length - _startIndex
                    : _byteCount);
        }
    }
    public ArraySegment<byte> RingSectionTwo
    {
        get
        {
            return _voiceBuffer != null && _startIndex + _byteCount > _voiceBuffer.Length
                ? new ArraySegment<byte>(_voiceBuffer, 0, _startIndex + _byteCount - _voiceBuffer.Length)
                : default;
        }
    }

    internal byte[] InternalBuffer => _voiceBuffer ?? Array.Empty<byte>();
    internal int StartIndex => _startIndex;
    internal int ByteCount => _byteCount;
    internal IReadOnlyList<PacketInfo> Packets => _packetsReadOnly ??= new ReadOnlyCollection<PacketInfo>(_packets ??= []);

    public void Reset()
    {
        _startIndex = 0;
        _byteCount = 0;
        _packets?.Clear();
    }
    public void AppendPacket(ArraySegment<byte> packet)
    {
        int newSize = _byteCount + packet.Count;

        while (_voiceBuffer == null || newSize >= _voiceBuffer.Length || PacketCount >= ushort.MaxValue)
        {
            if (_voiceBuffer == null || _packets!.Count == 0 || packet.Count > _voiceBuffer.Length)
            {
                _voiceBuffer = new byte[Math.Max(_audioListenService.VoiceBufferSize, packet.Count)];
                _packets = new List<PacketInfo>(256);
                _packetsReadOnly = null;
                _byteCount = 0;
                _startIndex = 0;
                break;
            }

            int oldestPacketStartInd = _packets[0].StartIndex;
            int nextPacketStartInd = _packets.Count == 1 ? (_startIndex + _byteCount) % _voiceBuffer.Length : _packets[1].StartIndex;
            int packetSize;
            if (nextPacketStartInd < oldestPacketStartInd)
            {
                packetSize = _voiceBuffer!.Length - oldestPacketStartInd + nextPacketStartInd;
            }
            else
            {
                packetSize = nextPacketStartInd - oldestPacketStartInd;
            }
            _byteCount -= packetSize;
            _startIndex = (_startIndex + packetSize) % _voiceBuffer.Length;
            newSize = _byteCount + packet.Count;
        }

        int startIndex = (_startIndex + _byteCount) % _voiceBuffer.Length;
        if (startIndex + packet.Count > _voiceBuffer.Length)
        {
            int sect1Ct = _voiceBuffer.Length - startIndex;
            int sect2Ct = packet.Count - sect1Ct;
            Buffer.BlockCopy(packet.Array!, packet.Offset, _voiceBuffer, startIndex, sect1Ct);
            Buffer.BlockCopy(packet.Array!, packet.Offset + sect1Ct, _voiceBuffer, 0, sect2Ct);
        }
        else
        {
            Buffer.BlockCopy(packet.Array!, packet.Offset, _voiceBuffer, startIndex, packet.Count);
        }

        _byteCount += packet.Count;
        _packets!.Add(new PacketInfo(Time.realtimeSinceStartup, startIndex));

        Dump();
    }
    public NativeArray<byte> CreateNativeArray()
    {
        GameThread.AssertCurrent();

        NativeArray<byte> array = new NativeArray<byte>(_byteCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        if (_byteCount == 0)
            return array;

        ArraySegment<byte> ringSectionOne = RingSectionOne;
        ArraySegment<byte> ringSectionTwo = RingSectionTwo;

        NativeArray<byte>.Copy(ringSectionOne.Array, ringSectionOne.Offset, array, 0, ringSectionOne.Count);
        NativeArray<byte>.Copy(ringSectionTwo.Array, ringSectionTwo.Offset, array, ringSectionOne.Count, ringSectionTwo.Count);
        return array;
    }
    public async UniTask<AudioRecordManager.AudioConvertResult> TryConvert(Stream writeTo, bool leaveOpen = true, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        byte[] data = _audioListenService.CreateMultipartPacket(this);
        if (data.Length == 0)
            return AudioRecordManager.AudioConvertResult.NoData;
        File.WriteAllBytes(@"C:\Users\danny\OneDrive\Desktop\multi-part.txt", data);

        return await _audioListenService.TryWriteWavAsync(data, writeTo, leaveOpen, token);
    }
    public void WriteMetaFile(Stream writeTo, bool includeData = false, bool leaveOpen = true)
    {
        GameThread.AssertCurrent();

        MetaWriter.Stream = writeTo;
        try
        {
            WriteMetaFile(MetaWriter, includeData);
            MetaWriter.Flush();
            if (!leaveOpen)
                writeTo.Dispose();
        }
        finally
        {
            MetaWriter.Stream = null;
        }
    }
    public void WriteMetaFile(ByteWriter writer, bool includeData = false)
    {
        GameThread.AssertCurrent();

        writer.Write(DataVersion | (includeData ? 1 << 31 : 0));
        writer.Write((byte)1); // player count
        WriteMetaFilePlayerSection(writer, includeData);
    }
    public static void WriteMetaFileForPlayers(IEnumerable<WarfarePlayer> players, Stream writeTo, bool includeData = false, bool leaveOpen = true)
    {
        GameThread.AssertCurrent();

        MetaWriter.Stream = writeTo;
        try
        {
            WriteMetaFileForPlayers(MetaWriter, players, includeData);
            MetaWriter.Flush();
            if (!leaveOpen)
                writeTo.Dispose();
        }
        finally
        {
            MetaWriter.Stream = null;
        }
    }
    public static void WriteMetaFileForPlayers(ByteWriter writer, IEnumerable<WarfarePlayer> players, bool includeData = false)
    {
        GameThread.AssertCurrent();

        writer.Write(DataVersion | (includeData ? 1 << 31 : 0));
        List<AudioRecordPlayerComponent> playerComps = players.Select(player => player.Component<AudioRecordPlayerComponent>()).ToList();
        playerComps.RemoveAll(x => x == null);

        int ct = Math.Min(byte.MaxValue, playerComps.Count);
        writer.Write((byte)ct); // player count
        for (int i = 0; i < ct; ++i)
        {
            playerComps[i].WriteMetaFilePlayerSection(writer, includeData);
        }
    }
    public void WriteMetaFilePlayerSection(ByteWriter writer, bool includeData = false)
    {
        writer.Write(Player.Steam64.m_SteamID);
        writer.Write(PacketCount);
        for (int i = 0; i < PacketCount; ++i)
        {
            PacketInfo packet = _packets![i];
            int nextPacketStartInd = _packets.Count == i - 1 ? _startIndex + _byteCount : _packets[1].StartIndex;
            int packetSize;
            if (nextPacketStartInd < packet.StartIndex)
            {
                packetSize = _voiceBuffer!.Length - packet.StartIndex + nextPacketStartInd;
            }
            else
            {
                packetSize = nextPacketStartInd - packet.StartIndex;
            }
            writer.Write(packetSize);
            writer.Write((ushort)i);
            writer.Write(DateTime.UtcNow - TimeSpan.FromSeconds(Time.realtimeSinceStartup) + TimeSpan.FromSeconds(packet.TimeRelayed));
            if (!includeData)
                continue;

            if (packet.StartIndex + packetSize > _voiceBuffer!.Length)
            {
                writer.WriteBlock(_voiceBuffer, packet.StartIndex, _voiceBuffer.Length - packet.StartIndex);
                writer.WriteBlock(_voiceBuffer, 0, nextPacketStartInd % _voiceBuffer.Length);
            }
            else
            {
                writer.WriteBlock(_voiceBuffer, packet.StartIndex, packetSize);
            }

        }
    }

    void IPlayerComponent.Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        _audioListenService = serviceProvider.GetRequiredService<AudioRecordManager>();
    }

    [Conditional("DEBUG")]
    public void Dump()
    {
        if (_voiceBuffer == null || _byteCount == 0 || _packets == null || _packets.Count == 0)
        {
            WarfareModule.Singleton.GlobalLogger.LogDebug($"Player {Player} voice buffer... no packets.");
            return;
        }

        WarfareModule.Singleton.GlobalLogger.LogDebug($"Player {Player} voice buffer @ {Time.realtimeSinceStartup}...");
        WarfareModule.Singleton.GlobalLogger.LogDebug($"  Packets: {PacketCount}, size: {_byteCount}, index: {_startIndex}.");
        for (int i = 0; i < _packets.Count; ++i)
        {
            int stInd = _packets[i].StartIndex;
            int endInd = i == _packets.Count - 1 ? (_startIndex + _byteCount) % _voiceBuffer.Length : _packets[i + 1].StartIndex;
            int packetSize = endInd < stInd ? _voiceBuffer!.Length - stInd + endInd : endInd - stInd;
            WarfareModule.Singleton.GlobalLogger.LogDebug($"    Packet {i} starts at {_packets[i].StartIndex} and ends at {endInd}, size: {packetSize}. Wraps around? {stInd > endInd}");
        }
    }
}

public readonly struct PacketInfo
{
    public readonly float TimeRelayed;
    public readonly int StartIndex;
    public PacketInfo(float timeRelayed, int startIndex)
    {
        TimeRelayed = timeRelayed;
        StartIndex = startIndex;
    }
}