using System;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Uncreated.Warfare.Vehicles.Spawners;

/// <summary>
/// Deserialized information about a vehicle spawner.
/// </summary>
public sealed class VehicleSpawnerInfo
{
    /// <summary>
    /// Unique ID of this spawner.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// ID of the faction this spawner belongs to.
    /// </summary>
    [JsonPropertyName("faction")]
    public required string Faction { get; set; }

    /// <summary>
    /// GUID of the vehicle to spawn at this spawner.
    /// </summary>
    [JsonPropertyName("vehicle")]
    public required Guid VehicleId { get; set; }

    /// <summary>
    /// All signs referencing this spawner.
    /// </summary>
    [JsonIgnore]
    public ImmutableArray<VehicleSpawnerSignInfo> Signs { get; set; } = ImmutableArray<VehicleSpawnerSignInfo>.Empty;

    /// <summary>
    /// Steam64 ID of the player who created this spawner.
    /// </summary>
    [JsonPropertyName("creator")]
    public ulong Creator { get; set; }

    /// <summary>
    /// Position of the spawner structure.
    /// </summary>
    [JsonIgnore]
    public Vector3 Position => new Vector3(PositionX, PositionY, PositionZ);

    /// <summary>
    /// Rotation of the spawner structure.
    /// </summary>
    [JsonIgnore]
    public Quaternion Rotation => Quaternion.Euler(RotationX, RotationY, RotationZ);

    [JsonPropertyName("pos_x")]
    public float PositionX { get; set; }

    [JsonPropertyName("pos_y")]
    public float PositionY { get; set; }

    [JsonPropertyName("pos_z")]
    public float PositionZ { get; set; }

    [JsonPropertyName("rot_x")]
    public float RotationX { get; set; }

    [JsonPropertyName("rot_y")]
    public float RotationY { get; set; }

    [JsonPropertyName("rot_z")]
    public float RotationZ { get; set; }
}

/// <summary>
/// A sign linked to a vehicle spawner.
/// </summary>
public sealed class VehicleSpawnerSignInfo
{
    /// <summary>
    /// ID of the spawner this sign is for.
    /// </summary>
    [JsonPropertyName("vehicle_bay")]
    public required string VehicleSpawnerId { get; set; }

    /// <summary>
    /// Steam64 ID of the player who created this spawner sign.
    /// </summary>
    [JsonPropertyName("creator")]
    public ulong Creator { get; set; }

    /// <summary>
    /// Position of the sign barricade.
    /// </summary>
    [JsonIgnore]
    public Vector3 Position => new Vector3(PositionX, PositionY, PositionZ);

    /// <summary>
    /// Rotation of the sign barricade.
    /// </summary>
    [JsonIgnore]
    public Quaternion Rotation => Quaternion.Euler(RotationX, RotationY, RotationZ);

    [JsonPropertyName("pos_x")]
    public float PositionX { get; set; }

    [JsonPropertyName("pos_y")]
    public float PositionY { get; set; }

    [JsonPropertyName("pos_z")]
    public float PositionZ { get; set; }

    [JsonPropertyName("rot_x")]
    public float RotationX { get; set; }

    [JsonPropertyName("rot_y")]
    public float RotationY { get; set; }

    [JsonPropertyName("rot_z")]
    public float RotationZ { get; set; }
}

[JsonSerializable(typeof(VehicleSpawnerInfo), GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(VehicleSpawnerSignInfo), GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(List<VehicleSpawnerInfo>), GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(List<VehicleSpawnerSignInfo>), GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class VehicleSpawnerInfoSerializeContext : JsonSerializerContext;