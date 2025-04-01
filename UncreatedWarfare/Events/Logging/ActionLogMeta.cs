using DanielWillett.SpeedBytes;
using System;
using System.IO;
using Uncreated.Warfare.Logging;

namespace Uncreated.Warfare.Events.Logging;

public sealed class ActionLogMeta
{
    private const byte DataVersion = 2;

    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public ulong[]? Players { get; set; }
    public ulong[]? DataReferencedPlayers { get; set; }
    public ActionLogType?[]? Types { get; set; }
    public int RowCount { get; set; } = -1;

    public static ActionLogMeta FromFile(string file)
    {
        using FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 256, FileOptions.SequentialScan);
        ByteReader reader = new ByteReader();
        reader.LoadNew(fs);
        return Read(reader);
    }

    public void WriteToFile(string file)
    {
        using FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.Write, 256, FileOptions.SequentialScan);
        ByteWriter writer = new ByteWriter { Stream = fs };
        Write(writer);
        writer.Flush();
    }

    public static ActionLogMeta Read(ByteReader reader)
    {
        byte version = reader.ReadUInt8();
        ActionLogMeta meta = new ActionLogMeta();
        if (version == 0)
        {
            meta.Players = Array.Empty<ulong>();
            meta.DataReferencedPlayers = Array.Empty<ulong>();
            meta.Types = Array.Empty<ActionLogType?>();
            return meta;
        }

        if (version != 1)
        {
            meta.Start = reader.ReadDateTime().ToUniversalTime();
            meta.End = reader.ReadDateTime().ToUniversalTime();
            meta.RowCount = reader.ReadInt32();
        }
        else
        {
            meta.Start = reader.ReadDateTimeOffset().UtcDateTime;
            meta.End = reader.ReadDateTimeOffset().UtcDateTime;
            meta.RowCount = -1;
        }

        meta.Players = reader.ReadUInt64Array();

        if (version == 1)
        {
            byte[] oldTypes = reader.ReadUInt8Array();
            ActionLogType?[] types = new ActionLogType?[oldTypes.Length];
            for (int i = 0; i < oldTypes.Length; ++i)
            {
                ActionLogTypeOld oldType = (ActionLogTypeOld)oldTypes[i];
                types[i] = ActionLogTypes.FromLegacyType(oldType);
            }
            meta.Types = types;
        }
        else
        {
            ushort[] ids = reader.ReadUInt16Array();
            ActionLogType?[] types = new ActionLogType?[ids.Length];
            for (int i = 0; i < ids.Length; ++i)
            {
                types[i] = ActionLogTypes.FromId(ids[i]);
            }

            meta.Types = types;
        }

        meta.DataReferencedPlayers = reader.ReadUInt64Array();
        return meta;
    }

    public void Write(ByteWriter writer)
    {
        writer.Write(DataVersion);
        writer.Write(Start);
        writer.Write(End);
        writer.Write(RowCount);
        
        writer.Write(Players ?? Array.Empty<ulong>());

        if (Types != null)
        {
            ushort ct = 0;
            for (int i = 0; i < Types.Length && ct < ushort.MaxValue; ++i)
            {
                if (Types[i] != null)
                    ++ct;
            }

            writer.Write(ct);
            for (int i = 0; i < Types.Length && ct < ushort.MaxValue; ++i)
            {
                ActionLogType? type = Types[i];
                if (type != null)
                    writer.Write(type.Id);
            }
        }
        else
        {
            writer.Write((ushort)0);
        }

        writer.Write(DataReferencedPlayers ?? Array.Empty<ulong>());
    }
}