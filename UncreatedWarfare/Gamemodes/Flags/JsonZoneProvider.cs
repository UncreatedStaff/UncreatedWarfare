using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Uncreated.Json;
using Uncreated.Warfare.Maps;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags;

public class JsonZoneProvider
{
    private readonly List<Zone> _zones;
    private readonly FileInfo _file;
    public IReadOnlyList<Zone> Zones { get; }

    public JsonZoneProvider(FileInfo file, IEnumerable<Zone> zones)
    {
        _file = file;
        _zones = new List<Zone>(zones);
        Zones = _zones.AsReadOnly();
    }
    public JsonZoneProvider(FileInfo file)
    {
        _file = file;
        _zones = new List<Zone>();
        Zones = _zones.AsReadOnly();
    }
    public void Reload()
    {
        _zones.Clear();
        if (_file.Exists)
        {
            FileStream? rs = null;
            List<Exception>? exceptions = null;
            try
            {
                using (rs = new FileStream(_file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    long len = rs.Length;
                    if (len > int.MaxValue)
                    {
                        L.LogError("File " + _file.FullName + " is too large.");
                        return;
                    }
                    byte[] buffer = new byte[len];
                    rs.Read(buffer, 0, (int)len);
                    List<ZoneModel> zones = new List<ZoneModel>(_zones.Count);
                    Utf8JsonReader reader = new Utf8JsonReader(buffer.AsSpan(), JsonEx.readerOptions);
                    if (reader.Read() && reader.TokenType == JsonTokenType.StartArray)
                    {
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        {
                            if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                ZoneModel zone;
                                // handles any parse exceptions in order to keep one zone from breaking the rest.
                                try
                                {
                                    ReadJsonZone(ref reader, out zone);
                                }
                                catch (Exception ex)
                                {
                                    if (exceptions == null)
                                        exceptions = new List<Exception>(1) { ex };
                                    else
                                        exceptions.Add(ex);
                                    continue;
                                }
                                if (zone.IsValid)
                                    zones.Add(zone);
                            }
                        }
                    }
                    _zones.Capacity = zones.Count;
                    for (int i = 0; i < zones.Count; ++i)
                    {
                        _zones.Add(zones[i].GetZone());
                    }
                    rs.Close();
                    rs.Dispose();
                }
                return;
            }
            catch (Exception ex)
            {
                if (exceptions == null)
                    exceptions = new List<Exception>(1) { ex };
                else
                    exceptions.Insert(0, ex);
                if (rs != null)
                {
                    rs.Close();
                    rs.Dispose();
                }
            }
            if (exceptions != null)
            {
                L.LogError("Failed to deserialize zone data because of the following exceptions: ");
                for (int i = 0; i < exceptions.Count; ++i)
                {
                    L.LogError(exceptions[i]);
                }
                throw exceptions[0];
            }
        }
        else
        {
            Save();
        }
    }
    public void Save()
    {
        try
        {
            if (!_file.Exists)
            {
                if (!_file.Directory!.Exists)
                    _file.Directory.Create();

                _zones.Clear();
                _zones.AddRange(JSONMethods.DefaultZones.Select(x => x.GetZone()));
                _file.Create()?.Close();
            }

            using FileStream rs = new FileStream(_file.FullName, FileMode.Truncate, FileAccess.Write, FileShare.None);
            Utf8JsonWriter writer = new Utf8JsonWriter(rs, JsonEx.writerOptions);
            writer.WriteStartArray();
            for (int i = 0; i < _zones.Count; i++)
            {
                ZoneModel mdl = _zones[i].Data;
                WriteJsonZone(writer, in mdl);
            }

            writer.WriteEndArray();
            writer.Dispose();
            rs.Close();
            rs.Dispose();
        }
        catch (Exception ex)
        {
            L.LogError("Failed to serialize zone");
            L.LogError(ex);
        }
    }

    internal static void ReadJsonZone(ref Utf8JsonReader reader, out ZoneModel mdl)
    {
        mdl = new ZoneModel() { Map = -1 };
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string? prop = reader.GetString();
                if (reader.Read() && prop != null)
                {
                    if (prop.Equals("error"))
                        throw new ZoneReadException("The zone being read was corrupted on write.") { Data = mdl };
                    else if (prop.Equals("id", StringComparison.Ordinal))
                        reader.TryGetInt32(out mdl.Id);
                    else if (prop.Equals("name", StringComparison.Ordinal))
                        mdl.Name = reader.GetString() ?? string.Empty;
                    else if (prop.Equals("short-name", StringComparison.Ordinal) || prop.Equals("short_name", StringComparison.Ordinal))
                    {
                        mdl.ShortName = reader.GetString();
                        if (mdl.ShortName!.Equals(mdl.Name, StringComparison.Ordinal))
                            mdl.ShortName = null;
                    }
                    else if (prop.Equals("x", StringComparison.Ordinal))
                        reader.TryGetSingle(out mdl.ZoneData.X);
                    else if (prop.Equals("z", StringComparison.Ordinal) || prop.Equals("y", StringComparison.Ordinal))
                        reader.TryGetSingle(out mdl.ZoneData.Z);
                    else if (prop.Equals("spawn-x", StringComparison.Ordinal))
                        reader.TryGetSingle(out mdl.SpawnX);
                    else if (prop.Equals("spawn-z", StringComparison.Ordinal) || prop.Equals("spawn-y", StringComparison.Ordinal))
                        reader.TryGetSingle(out mdl.SpawnZ);
                    else if (prop.Equals("map", StringComparison.Ordinal) && !reader.TryGetInt32(out mdl.Map))
                        mdl.Map = -1;
                    else if (prop.Equals("use-map-coordinates", StringComparison.Ordinal) || prop.Equals("use_map_size_multiplier", StringComparison.Ordinal))
                        mdl.UseMapCoordinates = reader.TokenType == JsonTokenType.True;
                    else if (prop.Equals("min-height", StringComparison.Ordinal) || prop.Equals("minHeight", StringComparison.Ordinal))
                    {
                        reader.TryGetSingle(out mdl.MinimumHeight);
                        if (mdl.MinimumHeight < 0 && prop.Equals("minHeight", StringComparison.Ordinal)) // backwards compatability
                            mdl.MinimumHeight = float.NaN;
                    }
                    else if (prop.Equals("max-height", StringComparison.Ordinal) || prop.Equals("maxHeight", StringComparison.Ordinal))
                    {
                        reader.TryGetSingle(out mdl.MaximumHeight);
                        if (mdl.MaximumHeight < 0 && prop.Equals("maxHeight", StringComparison.Ordinal)) // backwards compatability
                            mdl.MaximumHeight = float.NaN;
                    }
                    else if (prop.Equals("use-case", StringComparison.Ordinal))
                    {
                        if (reader.TokenType == JsonTokenType.String)
                        {
                            string str = reader.GetString()!;
                            if (!Enum.TryParse(str, true, out mdl.UseCase))
                            {
                                if (str.Equals("OTHER", StringComparison.Ordinal))
                                    mdl.UseCase = ZoneUseCase.Other;
                                else if (str.Equals("FLAG", StringComparison.Ordinal))
                                    mdl.UseCase = ZoneUseCase.Flag;
                                else if (str.Equals("T1_MAIN", StringComparison.Ordinal))
                                    mdl.UseCase = ZoneUseCase.Team1Main;
                                else if (str.Equals("T2_MAIN", StringComparison.Ordinal))
                                    mdl.UseCase = ZoneUseCase.Team2Main;
                                else if (str.Equals("T1_AMC", StringComparison.Ordinal))
                                    mdl.UseCase = ZoneUseCase.Team1MainCampZone;
                                else if (str.Equals("T2_AMC", StringComparison.Ordinal))
                                    mdl.UseCase = ZoneUseCase.Team2MainCampZone;
                                else if (str.Equals("LOBBY", StringComparison.Ordinal))
                                    mdl.UseCase = ZoneUseCase.Lobby;
                                else throw new JsonException("Invalid use case: " + str + ".");
                            }
                        }
                        else if (reader.TokenType == JsonTokenType.Number)
                        {
                            if (reader.TryGetByte(out byte n))
                                mdl.UseCase = (ZoneUseCase)n;
                        }
                    }
                    else if (prop.Equals("adjacencies", StringComparison.Ordinal))
                    {
                        if (reader.TokenType == JsonTokenType.StartArray)
                        {
                            List<AdjacentFlagData> tlist = new List<AdjacentFlagData>(16);
                            while (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
                            {
                                AdjacentFlagData afd = new AdjacentFlagData();
                                afd.ReadJson(ref reader);
                                tlist.Add(afd);
                            }
                            mdl.Adjacencies = tlist.ToArray();
                        }
                    }
                    else if (prop.Equals("grid-objects", StringComparison.Ordinal))
                    {
                        if (reader.TokenType == JsonTokenType.StartArray)
                        {
                            List<GridObject> tlist = new List<GridObject>(32);
                            while (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
                            {
                                GridObject afd = new GridObject();
                                afd.ReadJson(ref reader);
                                tlist.Add(afd);
                            }
                            mdl.GridObjects = tlist.ToArray();
                        }
                    }
                    else if (prop.Equals("zone", StringComparison.Ordinal)) // legacy FlagData converter
                    {
                        ushort lvlSize = Level.size;
                        mdl.SpawnX -= lvlSize / 2;
                        mdl.SpawnZ = -(mdl.SpawnZ - lvlSize / 2);
                        if (!float.IsNaN(mdl.ZoneData.X))
                            mdl.ZoneData.X -= lvlSize / 2;
                        else
                            mdl.ZoneData.X = mdl.SpawnX;
                        
                        if (!float.IsNaN(mdl.ZoneData.Z))
                            mdl.ZoneData.Z = -(mdl.SpawnZ - lvlSize / 2);
                        else
                            mdl.ZoneData.Z = mdl.SpawnZ;
                        if (reader.TokenType == JsonTokenType.StartObject)
                        {
                            while (reader.Read())
                            {
                                if (reader.TokenType == JsonTokenType.EndObject) break;
                                if (reader.TokenType == JsonTokenType.PropertyName)
                                {
                                    string? prop2 = reader.GetString();
                                    if (reader.Read() && prop2 != null)
                                    {
                                        if (prop2.Equals("type", StringComparison.Ordinal))
                                        {
                                            string? type = reader.GetString();
                                            if (type != null && Enum.TryParse(type, true, out ZoneType t) && (t == ZoneType.Polygon || t == ZoneType.Circle || t == ZoneType.Rectangle))
                                            {
                                                mdl.ZoneType = t;
                                            }
                                        }
                                        else if (prop2.Equals("data", StringComparison.Ordinal))
                                        {
                                            string? data = reader.GetString();
                                            if (data != null)
                                            {
                                                switch (mdl.ZoneType)
                                                {
                                                    case ZoneType.Circle:
                                                        if (float.TryParse(data, System.Globalization.NumberStyles.Any, Data.AdminLocale, out float rad))
                                                        {
                                                            mdl.ZoneData.Radius = rad;
                                                        }
                                                        break;
                                                    case ZoneType.Rectangle:
                                                        string[] nums = data.Split(',');
                                                        if (nums.Length == 2)
                                                        {
                                                            if (float.TryParse(nums[0], System.Globalization.NumberStyles.Any, Data.AdminLocale, out float sizeX) &&
                                                                float.TryParse(nums[1], System.Globalization.NumberStyles.Any, Data.AdminLocale, out float sizeZ))
                                                            {
                                                                mdl.ZoneData.SizeX = sizeX;
                                                                mdl.ZoneData.SizeZ = sizeZ;
                                                            }
                                                        }
                                                        break;
                                                    case ZoneType.Polygon:
                                                        nums = data.Split(',');
                                                        if (nums.Length % 2 == 0 && nums.Length >= 6)
                                                        {
                                                            int pts = nums.Length / 2;
                                                            mdl.ZoneData.Points = new Vector2[pts];
                                                            for (int i = 0; i < pts; ++i)
                                                            {
                                                                int t = i * 2;
                                                                if (float.TryParse(nums[t], System.Globalization.NumberStyles.Any, Data.AdminLocale, out float posx) &&
                                                                    float.TryParse(nums[t + 1], System.Globalization.NumberStyles.Any, Data.AdminLocale, out float posz))
                                                                {
                                                                    mdl.ZoneData.Points[i] = new Vector2(posx, posz);
                                                                }
                                                            }
                                                        }
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < ZoneModel.ValidProperties.Length; ++i)
                        {
                            ref ZoneModel.PropertyData data = ref ZoneModel.ValidProperties[i];
                            if (data.Name.Equals(prop, StringComparison.Ordinal))
                            {
                                if (mdl.ZoneType == ZoneType.Invalid || mdl.ZoneType == data.ZoneType)
                                {
                                    mdl.ZoneType = data.ZoneType;
                                    if (reader.TokenType == JsonTokenType.Number && reader.TryGetSingle(out float f))
                                    {
                                        ((ZoneModel.PropertyData.ModData<float>)data.Modifier)(ref mdl.ZoneData, f);
                                    }
                                    else if (reader.TokenType == JsonTokenType.StartArray)
                                    {
                                        List<Vector2> v2 = new List<Vector2>(16);
                                        Vector2 current = default;
                                        while (reader.Read())
                                        {
                                            if (reader.TokenType == JsonTokenType.EndObject)
                                            {
                                                v2.Add(current);
                                                current = default;
                                            }
                                            else if (reader.TokenType == JsonTokenType.PropertyName)
                                            {
                                                string? prop2 = reader.GetString();
                                                if (reader.Read() && prop2 != null)
                                                {
                                                    if (prop2.Equals("x", StringComparison.Ordinal))
                                                    {
                                                        reader.TryGetSingle(out current.x);
                                                    }
                                                    else if (prop2.Equals("z", StringComparison.Ordinal) || prop2.Equals("y", StringComparison.Ordinal))
                                                    {
                                                        reader.TryGetSingle(out current.y);
                                                    }
                                                }
                                            }
                                            else if (reader.TokenType == JsonTokenType.EndArray) break;
                                        }
                                        ((ZoneModel.PropertyData.ModData<Vector2[]>)data.Modifier)(ref mdl.ZoneData, v2.ToArray());
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        if (mdl.Map == -1)
            mdl.Map = MapScheduler.Current;
        if (mdl.ZoneType == ZoneType.Polygon)
        {
            if (!float.IsNaN(mdl.ZoneData.X))
                mdl.SpawnX = mdl.ZoneData.X;
            else if (!float.IsNaN(mdl.SpawnX))
                mdl.ZoneData.X = mdl.SpawnX;
            if (!float.IsNaN(mdl.ZoneData.Z))
                mdl.SpawnZ = mdl.ZoneData.Z;
            else if (!float.IsNaN(mdl.SpawnZ))
                mdl.ZoneData.Z = mdl.SpawnZ;
        }
        else
        {
            if (!float.IsNaN(mdl.ZoneData.X) && float.IsNaN(mdl.SpawnX))
                mdl.SpawnX = mdl.ZoneData.X;
            if (!float.IsNaN(mdl.ZoneData.Z) && float.IsNaN(mdl.SpawnZ))
                mdl.SpawnZ = mdl.ZoneData.Z;
        }
        
        mdl.ValidateRead();
    }
    internal static void WriteJsonZone(Utf8JsonWriter writer, in ZoneModel mdl)
    {
        writer.WriteStartObject();
        if (!mdl.IsValid || mdl.ZoneType == ZoneType.Invalid)
        {
            writer.WriteBoolean("error", true);
            writer.WriteEndObject();
            return;
        }

        writer.WriteNumber("id", mdl.Id);
        writer.WriteString("name", mdl.Name);
        if (mdl.ShortName != null)
            writer.WriteString("short-name", mdl.ShortName);
        writer.WriteNumber("x", float.IsNaN(mdl.ZoneData.X) ? mdl.SpawnX : mdl.ZoneData.X);
        writer.WriteNumber("z", float.IsNaN(mdl.ZoneData.Z) ? mdl.SpawnZ : mdl.ZoneData.Z);
        if (!float.IsNaN(mdl.SpawnX))
            writer.WriteNumber("spawn-x", mdl.SpawnX);
        if (!float.IsNaN(mdl.SpawnZ))
            writer.WriteNumber("spawn-z", mdl.SpawnZ);
        if (mdl.UseMapCoordinates)
            writer.WriteBoolean("use-map-coordinates", mdl.UseMapCoordinates);
        if (!float.IsNaN(mdl.MinimumHeight))
            writer.WriteNumber("min-height", mdl.MinimumHeight);
        if (!float.IsNaN(mdl.MaximumHeight))
            writer.WriteNumber("max-height", mdl.MaximumHeight);
        if (mdl.Map > 0 && mdl.Map < MapScheduler.MapCount)
            writer.WriteNumber("map", mdl.Map);
        if (mdl.UseCase != ZoneUseCase.Other && mdl.UseCase <= ZoneUseCase.Lobby)
            writer.WriteString("use-case", mdl.UseCase.ToString().ToLower());
        if (mdl.Adjacencies is { Length: > 0 })
        {
            writer.WritePropertyName("adjacencies");
            writer.WriteStartArray();
            for (int i = 0; i < mdl.Adjacencies.Length; i++)
            {
                writer.WriteStartObject();
                mdl.Adjacencies[i].WriteJson(writer);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
        if (mdl.GridObjects is { Length: > 0 })
        {
            writer.WritePropertyName("grid-objects");
            writer.WriteStartArray();
            for (int i = 0; i < mdl.GridObjects.Length; i++)
            {
                writer.WriteStartObject();
                mdl.GridObjects[i].WriteJson(writer);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
        switch (mdl.ZoneType)
        {
            case ZoneType.Rectangle:
                writer.WriteNumber("size-x", mdl.ZoneData.SizeX);
                writer.WriteNumber("size-z", mdl.ZoneData.SizeZ);
                break;
            case ZoneType.Circle:
                writer.WriteNumber("radius", mdl.ZoneData.Radius);
                break;
            case ZoneType.Polygon:
                writer.WritePropertyName("points");
                writer.WriteStartArray();
                for (int i = 0; i < mdl.ZoneData.Points.Length; ++i)
                {
                    Vector2 v = mdl.ZoneData.Points[i];
                    writer.WriteStartObject();
                    writer.WriteNumber("x", v.x);
                    writer.WriteNumber("z", v.y);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                break;
        }
        writer.WriteEndObject();
    }

    public void Dispose() { }
}
