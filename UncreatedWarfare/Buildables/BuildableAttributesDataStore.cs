using DanielWillett.ReflectionTools;
using DanielWillett.SpeedBytes;
using DanielWillett.SpeedBytes.Unity;
using System;
using System.Collections.Generic;
using System.IO;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Buildables;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.Buildables;

/// <summary>
/// Stores a key-value-pair dictionary for each barricade and structure.
/// </summary>
[Priority(11 /* before MainBaseBuildables */)]
public class BuildableAttributesDataStore : IHostedService, ILevelHostedService, IEventListener<IBuildableDestroyedEvent>
{
    private static int _hasSaveSub;

    private readonly ILogger<BuildableAttributesDataStore> _logger;
    private readonly Dictionary<uint, BuildableAttributes> _barricadeAttributes = new Dictionary<uint, BuildableAttributes>();
    private readonly Dictionary<uint, BuildableAttributes> _structureAttributes = new Dictionary<uint, BuildableAttributes>();

    public BuildableAttributesDataStore(ILogger<BuildableAttributesDataStore> logger)
    {
        _logger = logger;
    }

    private static string GetFolderPath()
    {
        return Path.Combine(
            UnturnedPaths.RootDirectory.FullName,
            ServerSavedata.directoryName,
            Provider.serverID,
            "Level",
            Provider.map,
            "Buildable Attributes.dat"
        );
    }

    /// <summary>
    /// Check if a buildable contains an attribute.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public bool HasAttribute(IBuildable buildable, string attribute)
    {
        return UpdateAttributes(buildable).ContainsKey(attribute);
    }

    /// <summary>
    /// Check if a buildable contains an attribute.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public bool HasAttribute(uint instanceId, bool isStrucutre, string attribute)
    {
        return UpdateAttributes(instanceId, isStrucutre).ContainsKey(attribute);
    }

    /// <summary>
    /// Get the value of a buildable attribute.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public bool TryGetAttribute(IBuildable buildable, string attribute, out object? value)
    {
        return UpdateAttributes(buildable).TryGetValue(attribute, out value);
    }


    /// <summary>
    /// Get the value of a buildable attribute.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public bool TryGetAttribute(uint instanceId, bool isStrucutre, string attribute, out object? value)
    {
        return UpdateAttributes(instanceId, isStrucutre).TryGetValue(attribute, out value);
    }

    /// <summary>
    /// Start modifying the attributes of a buildable.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public IDictionary<string, object?> UpdateAttributes(IBuildable buildable)
    {
        return UpdateAttributes(buildable.InstanceId, buildable.IsStructure);
    }

    /// <summary>
    /// Start modifying the attributes of a buildable.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public IDictionary<string, object?> UpdateAttributes(uint instanceId, bool isStrucutre)
    {
        GameThread.AssertCurrent();

        Dictionary<uint, BuildableAttributes> dict = isStrucutre ? _structureAttributes : _barricadeAttributes;
        if (!dict.TryGetValue(instanceId, out BuildableAttributes? attributes))
        {
            attributes = new BuildableAttributes(instanceId, isStrucutre);
            dict.Add(instanceId, attributes);
        }

        return attributes.Attributes;
    }

    UniTask IHostedService.StartAsync(CancellationToken token)
    {
        ReadFromSave();
        if (Interlocked.Exchange(ref _hasSaveSub, 1) == 0)
        {
            // note: we don't really want to un-subscribe this since save will run after
            //       the service container disposes. its a little hacky but it works
            SaveManager.onPreSave += OnSavingWorld;
        }
        return UniTask.CompletedTask;
    }

    UniTask IHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    UniTask ILevelHostedService.LoadLevelAsync(CancellationToken token)
    {
        List<BuildableAttributes> toRemove = new List<BuildableAttributes>(0);
        foreach (BuildableAttributes attribute in _barricadeAttributes.Values)
        {
            BarricadeInfo barricade = BarricadeUtility.FindBarricade(attribute.InstanceId, attribute.KnownPosition);
            if (!barricade.HasValue)
                toRemove.Add(attribute);
            else
                attribute.KnownPosition = barricade.Data.point;
        }

        foreach (BuildableAttributes attribute in _structureAttributes.Values)
        {
            StructureInfo structure = StructureUtility.FindStructure(attribute.InstanceId, attribute.KnownPosition);
            if (!structure.HasValue)
                toRemove.Add(attribute);
            else
                attribute.KnownPosition = structure.Data.point;
        }

        foreach (BuildableAttributes attribute in toRemove)
        {
            if (attribute.IsStructure)
                _structureAttributes.Remove(attribute.InstanceId);
            else
                _barricadeAttributes.Remove(attribute.InstanceId);
        }

        if (toRemove.Count > 0)
            WriteToSave();

        return UniTask.CompletedTask;
    }

    private void OnSavingWorld()
    {
        try
        {
            WriteToSave();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving buildable attributes.");
        }
    }

    private void ReadFromSave()
    {
        GameThread.AssertCurrent();

        string path = GetFolderPath();

        _barricadeAttributes.Clear();
        _structureAttributes.Clear();

        if (!File.Exists(path))
        {
            return;
        }

        using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 512, FileOptions.SequentialScan);
        ByteReader reader = new ByteReader();
        reader.LoadNew(fs);

        byte version = reader.ReadUInt8();

        int barricadeCt = reader.ReadInt32();
        int structureCt = reader.ReadInt32();
        _barricadeAttributes.EnsureCapacity(barricadeCt);
        _structureAttributes.EnsureCapacity(structureCt);

        for (int i = 0; i < barricadeCt; ++i)
        {
            uint instId = reader.ReadUInt32();
            BuildableAttributes attributes = new BuildableAttributes(instId, false);
            attributes.Read(reader, version);
            _barricadeAttributes.Add(instId, attributes);
        }
        for (int i = 0; i < structureCt; ++i)
        {
            uint instId = reader.ReadUInt32();
            BuildableAttributes attributes = new BuildableAttributes(instId, true);
            attributes.Read(reader, version);
            _structureAttributes.Add(instId, attributes);
        }
    }

    private void WriteToSave()
    {
        string path = GetFolderPath();
        Thread.BeginCriticalRegion();
        try
        {
            using FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.SequentialScan);
            ByteWriter writer = new ByteWriter { Stream = fs };

            writer.Write((byte)0);
            writer.Write(_barricadeAttributes.Count);
            writer.Write(_structureAttributes.Count);

            foreach (BuildableAttributes attributes in _barricadeAttributes.Values)
            {
                writer.Write(attributes.InstanceId);
                attributes.Write(writer);
            }

            foreach (BuildableAttributes attributes in _structureAttributes.Values)
            {
                writer.Write(attributes.InstanceId);
                attributes.Write(writer);
            }

            writer.Flush();
            fs.Flush();
        }
        finally
        {
            Thread.EndCriticalRegion();
        }
    }

    /// <inheritdoc />
    [EventListener(Priority = int.MinValue, MustRunLast = true)]
    void IEventListener<IBuildableDestroyedEvent>.HandleEvent(IBuildableDestroyedEvent e, IServiceProvider serviceProvider)
    {
        IBuildable buildable = e.Buildable;
        if (buildable.IsStructure)
            _structureAttributes.Remove(buildable.InstanceId);
        else
            _barricadeAttributes.Remove(buildable.InstanceId);
    }
}

public class BuildableAttributes
{
    private readonly LinearDictionary<string, object?> _attributes;
    public uint InstanceId { get; }
    public bool IsStructure { get; }

    public Vector3 KnownPosition { get; set; }

    public IDictionary<string, object?> Attributes { get; }

    public BuildableAttributes(uint instanceId, bool isStructure)
    {
        InstanceId = instanceId;
        IsStructure = isStructure;

        _attributes = new LinearDictionary<string, object?>(StringComparer.Ordinal, 0);

        Attributes = _attributes;
    }


    internal void Write(ByteWriter writer)
    {
        writer.Write(KnownPosition);

        writer.Write(Attributes.Count);
        foreach (KeyValuePair<string, object?> attribute in _attributes)
        {
            writer.Write(attribute.Key);
            object? value = attribute.Value;
            if (value == null)
            {
                writer.Write((byte)TypeCode.DBNull);
                continue;
            }

            Type type = value.GetType();
            TypeCode tc = Type.GetTypeCode(type);
            switch (tc)
            {
                case TypeCode.Boolean:
                    writer.Write((bool)value);
                    break;

                case TypeCode.Char:
                    writer.Write((char)value);
                    break;

                case TypeCode.SByte:
                    writer.Write((sbyte)value);
                    break;

                case TypeCode.Byte:
                    writer.Write((byte)value);
                    break;

                case TypeCode.Int16:
                    writer.Write((short)value);
                    break;

                case TypeCode.UInt16:
                    writer.Write((ushort)value);
                    break;

                case TypeCode.Int32:
                    writer.Write((int)value);
                    break;

                case TypeCode.UInt32:
                    writer.Write((uint)value);
                    break;

                case TypeCode.Int64:
                    writer.Write((long)value);
                    break;

                case TypeCode.UInt64:
                    writer.Write((ulong)value);
                    break;

                case TypeCode.Single:
                    writer.Write((float)value);
                    break;

                case TypeCode.Double:
                    writer.Write((double)value);
                    break;

                case TypeCode.Decimal:
                    writer.Write((decimal)value);
                    break;

                case TypeCode.DateTime:
                    writer.Write((DateTime)value);
                    break;

                case (TypeCode)17:
                    writer.Write((TimeSpan)value);
                    break;

                case TypeCode.String:
                    writer.Write((string)value);
                    break;

                case (TypeCode)19:
                    writer.Write((Guid)value);
                    break;

                case (TypeCode)20:
                    writer.Write((DateTimeOffset)value);
                    break;

                default:
                    writer.Write((byte)TypeCode.DBNull);
                    break;
            }
        }
    }

    internal void Read(ByteReader reader, byte version)
    {
        _ = version;
        KnownPosition = reader.ReadVector3();

        int ct = reader.ReadInt32();

        _attributes.Clear();
        _attributes.Capacity = ct;

        for (int i = 0; i < ct; ++i)
        {
            string key = reader.ReadString();
            TypeCode tc = (TypeCode)reader.ReadUInt8();

            object? value = tc switch
            {
                TypeCode.Boolean => reader.ReadBool(),
                TypeCode.Char => reader.ReadChar(),
                TypeCode.SByte => reader.ReadInt8(),
                TypeCode.Byte => reader.ReadUInt8(),
                TypeCode.Int16 => reader.ReadInt16(),
                TypeCode.UInt16 => reader.ReadUInt16(),
                TypeCode.Int32 => reader.ReadInt32(),
                TypeCode.UInt32 => reader.ReadUInt32(),
                TypeCode.Int64 => reader.ReadInt64(),
                TypeCode.UInt64 => reader.ReadUInt64(),
                TypeCode.Single => reader.ReadFloat(),
                TypeCode.Double => reader.ReadDouble(),
                TypeCode.Decimal => reader.ReadDecimal(),
                TypeCode.DateTime => reader.ReadDateTime(),
                (TypeCode)17 => reader.ReadTimeSpan(),
                TypeCode.String => reader.ReadString(),
                (TypeCode)19 => reader.ReadGuid(),
                (TypeCode)20 => reader.ReadDateTimeOffset(),
                _ => null
            };

            _attributes[key] = value;
        }
    }
}