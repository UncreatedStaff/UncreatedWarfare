using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using Uncreated.SQL;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags;

/// <summary>
/// Do not depend on object equality for your plugins. Zone objects will be cycled every time the file is re-read.
/// The equality operators and function will compare names with <see cref="StringComparison.OrdinalIgnoreCase"/>. This is the most reliable way to compare <see cref="Zone"/>s.
/// </summary>
public abstract class Zone : IDeployable, IListItem
{
    public PrimaryKey PrimaryKey { get; set; }
    private static bool _isReady;
    /// <summary>
    /// For converting between image sources and coordinate sources.
    /// </summary>
    private static float _imageMultiplier;
    internal static ushort LvlSize;
    internal static ushort LvlBrdr;
    internal static bool HasCartographyVolume;
    internal static Transform? CartographyTransform;
    internal static Vector3 CartographyBoxSize;

    /// <summary>Returns a zone builder scaled to world coordinates.</summary>
    internal virtual ZoneBuilder Builder => new ZoneBuilder
    {
        ZoneType = Type,
        MinHeight = MinHeight,
        MaxHeight = MaxHeight,
        Name = Name,
        ShortName = ShortName,
        Adjacencies = Data.Adjacencies,
        Id = PrimaryKey,
        SpawnX = Spawn.x,
        SpawnZ = Spawn.y,
        UseMapCoordinates = false,
        UseCase = Data.UseCase,
        GridObjects = Data.GridObjects
    };
    internal static void OnLevelLoaded()
    {
        CartographyVolume? volume = CartographyVolumeManager.Get()?.GetMainVolume();
        if (volume != null)
        {
            HasCartographyVolume = true;
            _imageMultiplier = 1;
            CartographyTransform = volume.transform;
            LvlBrdr = 0;
            LvlSize = (ushort)Mathf.RoundToInt(Mathf.Max(volume.GetBoxSize().x, volume.GetBoxSize().z));
            CartographyBoxSize = volume.GetBoxSize();
        }
        else
        {
            LvlSize = Level.size;
            LvlBrdr = Level.border;
            _imageMultiplier = (LvlSize - LvlBrdr * 2) / (float)LvlSize;
            HasCartographyVolume = false;
            CartographyTransform = null;
            CartographyBoxSize = Vector3.one;
        }
        _isReady = true;
    }
    /// <summary>
    /// Convert a <see cref="Vector2"/> that was gotten from the Map image to world coordinates.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 FromMapCoordinates(Vector2 v2)
    {
        if (HasCartographyVolume && CartographyTransform != null)
        {
            Vector3 v3 = CartographyTransform.TransformPoint(new Vector3(v2.x, 0, v2.y));
            v3 = new Vector3(v3.x / CartographyBoxSize.x, 0, v3.z / CartographyBoxSize.z);
            return new Vector2(v3.x, v3.z);
        }
        return new Vector2((v2.x - LvlSize / 2) * _imageMultiplier, (v2.y - LvlSize / 2) * -_imageMultiplier);
    }
    /// <summary>
    /// Convert a <see cref="float"/> scalar that was gotten from map image measurements to world measurements.
    /// </summary>
    protected static float FromMapCoordinates(float scalar)
    {
        if (HasCartographyVolume && CartographyTransform != null)
        {
            Vector3 v3 = CartographyTransform.TransformVector(scalar, 0, 0);
            v3 = new Vector3(v3.x / CartographyBoxSize.x, 0, 0);
            return v3.x;
        }
        return scalar * _imageMultiplier;
    }
    /// <summary>
    /// Convert a <see cref="Vector2"/> that was gotten from world coordinates to Map image coordinates.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static Vector2 ToMapCoordinates(Vector2 v2)
    {
        if (HasCartographyVolume && CartographyTransform != null)
        {
            Vector3 v3 = CartographyTransform.InverseTransformPoint(new Vector3(v2.x, 0, v2.y));
            v3 = new Vector3(v3.x * CartographyBoxSize.x, 0, v3.z * CartographyBoxSize.z);
            return new Vector2(v3.x, v3.z);
        }
        return new Vector2(v2.x / _imageMultiplier, v2.y / _imageMultiplier);
    }
    /// <summary>
    /// Convert a <see cref="float"/> scalar that was gotten from map world measurements to map image measurements.
    /// </summary>
    protected static float ToMapCoordinates(float scalar)
    {
        if (HasCartographyVolume && CartographyTransform != null)
        {
            Vector3 v3 = CartographyTransform.InverseTransformVector(scalar, 0, 0);
            v3 = new Vector3(v3.x * CartographyBoxSize.x, 0, 0);
            L.LogDebug($"to {scalar} -> {v3.x}");
            return v3.x;
        }
        return scalar / _imageMultiplier;
    }
    internal readonly bool UseMapCoordinates;
    /// <summary>
    /// The highest Y level where the zone takes effect.
    /// </summary>
    public readonly float MaxHeight;
    /// <summary>
    /// The lowest Y level where the zone takes effect.
    /// </summary>
    public readonly float MinHeight;
    /// <summary>
    /// Zone shape definition type.
    /// </summary>
    public readonly ZoneType Type;
    /// <summary>
    /// The 2D center of the zone (x = x, y = z)
    /// </summary>
    public readonly Vector2 Center;
    private readonly Vector3 _center;
    private Vector3? _c3d;
    /// <summary>
    /// The 3D center of the zone (at terrain height).
    /// </summary>
    public Vector3 Center3D
    {
        get
        {
            if (_c3d.HasValue) return _c3d.Value;
            if (Level.isLoaded)
            {
                _c3d = new Vector3(Center.x, F.GetHeight(_center, MinHeight), Center.y);
                return _c3d.Value;
            }
            return _center;
        }
    }
    /// <summary>
    /// The Spawn point of the zone (x = x, y = z)
    /// </summary>
    public readonly Vector2 Spawn;
    private readonly Vector3 _spawn;
    private Vector3? _sp3d;
    /// <summary>
    /// The 3D Spawn point of the zone (at terrain height).
    /// </summary>
    public Vector3 Spawn3D
    {
        get
        {
            if (_sp3d.HasValue) return _sp3d.Value;
            if (Level.isLoaded)
            {
                _sp3d = new Vector3(Spawn.x, F.GetHeight(_spawn, MinHeight), Spawn.y);
                return _sp3d.Value;
            }
            return _spawn;
        }
    }
    protected bool SucessfullyParsed = false;
    internal readonly ZoneModel Data;
    protected Vector2[]? ParticleSpawnPoints;
    /// <summary>
    /// Display name for the zone.
    /// </summary>
    public readonly string Name;
    /// <summary>
    /// Shorter display name for the zone. Optional.
    /// </summary>
    public readonly string? ShortName;
    protected Vector4 Bound;
    protected float BoundArea;
    /// <summary>
    /// Square area of the bounds rectangle, good for sorting layers.
    /// </summary>
    /// <remarks>Cached</remarks>
    public float BoundsArea => BoundArea;
    /// <summary>
    /// Rectangular bounds of the zone. (x = left, y = bottom, z = right, w = top)
    /// </summary>
    /// <remarks>Cached</remarks>
    public Vector4 Bounds => Bound;
    /// <summary>
    /// Check if a 2D <paramref name="location"/> is inside the zone. Doesn't take height into account.
    /// </summary>
    public abstract bool IsInside(Vector2 location);
    /// <summary>
    /// Check if a 3D <paramref name="location"/> is inside the zone. Takes height into account.
    /// </summary>
    public abstract bool IsInside(Vector3 location);
    /// <summary>
    /// Enumerate through all players currently in the zone. Checks on move next.
    /// </summary>
    public IEnumerator<SteamPlayer> EnumerateClients()
    {
        for (int i = 0; i < Provider.clients.Count; i++)
        {
            SteamPlayer player = Provider.clients[i];
            if (IsInside(player.player.transform.position))
                yield return player;
        }
    }
    /// <inheritdoc/>
    public override string ToString() => $"{Name}: {Type.ToString().ToLower()}. ({Center})." +
        $"{(!float.IsNaN(MaxHeight) ? $" Max Height: {MaxHeight}." : string.Empty)}{(!float.IsNaN(MinHeight) ? $" Min Height: {MinHeight}." : string.Empty)}";
    /// <summary>
    /// Get the spawnpoints for the border preview.
    /// </summary>
    public abstract Vector2[] GetParticleSpawnPoints(out Vector2[] corners, out Vector2 center);
    /// <summary>
    /// Check if a 2D <paramref name="location"/> is inside the zone's rectangular bounds. Doesn't take height into account.
    /// </summary>
    public bool IsInsideBounds(Vector2 location)
    {
        return location.x >= Bounds.x && location.x <= Bounds.z && location.y >= Bounds.y && location.y <= Bounds.w;
    }
    /// <summary>
    /// Check if a 3D <paramref name="location"/> is inside the zone. Takes height into account.
    /// </summary>
    public bool IsInsideBounds(Vector3 location)
    {
        return location.x >= Bounds.x && location.x <= Bounds.z && location.z >= Bounds.y && location.z <= Bounds.w && (float.IsNaN(MinHeight) || location.y >= MinHeight) && (float.IsNaN(MaxHeight) || location.y <= MaxHeight);
    }
    private bool _drawDataGenerated;
    private DrawData _drawData;
    internal DrawData DrawingData
    {
        get
        {
            if (!_drawDataGenerated)
            {
                _drawData = GenerateDrawData();
                _drawDataGenerated = true;
            }
            return _drawData;
        }
    }

    Vector3 IDeployable.Position => Spawn3D + new Vector3(0, 1.5f, 0);

    protected abstract DrawData GenerateDrawData();
    public struct DrawData
    {
        internal Vector2 Center;
        internal float Radius;
        internal Vector2 Size;
        internal Line[]? Lines;
        internal Vector4 Bounds;
    }
    /// <summary>
    /// Zones must set <see cref="SucessfullyParsed"/> to <see langword="true"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Tried to construct a zone before the level has loaded.</exception>
    internal Zone(in ZoneModel data)
    {
        this.UseMapCoordinates = data.UseMapCoordinates;
        if (UseMapCoordinates && !_isReady)
            throw new InvalidOperationException("Tried to construct a zone before the level has loaded.");
        this.Data = data;
        this.Type = data.ZoneType;
        this.ShortName = data.ShortName;
        this.Name = data.Name;
        this.MinHeight = data.MinimumHeight;
        this.MaxHeight = data.MaximumHeight;
        this.PrimaryKey = data.Id < 0 ? PrimaryKey.NotAssigned : data.Id;
        if (data.UseMapCoordinates)
        {
            this.Center = FromMapCoordinates(new Vector2(data.ZoneData.X, data.ZoneData.Z));
            this._center = new Vector3(Center.x, 0f, Center.y);
            this.Spawn = FromMapCoordinates(new Vector2(data.SpawnX, data.SpawnZ));
            this._spawn = new Vector3(Spawn.x, 0f, Spawn.y);
        }
        else
        {
            this._center = new Vector3(data.ZoneData.X, 0f, data.ZoneData.Z);
            this.Center = new Vector2(_center.x, _center.z);
            this._spawn = new Vector3(data.SpawnX, 0f, data.SpawnZ);
            this.Spawn = new Vector2(_spawn.x, _spawn.z);
        }
    }
    /// <summary>Compares <see cref="Name"/></summary>
    public static bool operator ==(Zone? left, Zone? right)
    {
        if (left is null)
            return right is null;
        if (right is null) return false;
        return !string.IsNullOrEmpty(left.Name) && !string.IsNullOrEmpty(right.Name) && left.Name.Equals(right.Name, StringComparison.OrdinalIgnoreCase);
    }
    /// <summary>Compares <see cref="Name"/></summary>
    public static bool operator !=(Zone? left, Zone? right) => !(left == right);
    /// <summary>Compares <see cref="Name"/></summary>
    public override bool Equals(object obj)
    {
        if (this is null)
            return obj is null;
        if (obj is not Zone z) return false;
        return this == z;
    }
    /// <summary>Hashes <see cref="Name"/></summary>
    public override int GetHashCode() => Name.GetHashCode();

    bool IDeployable.CheckDeployable(UCPlayer player, CommandInteraction? ctx) => true;
    bool IDeployable.CheckDeployableTick(UCPlayer player, bool chat) => true;
    string ITranslationArgument.Translate(string language, string? format, UCPlayer? target, CultureInfo? culture,
        ref TranslationFlags flags)
    {
        if (format is not null && (format.Equals(Flag.SHORT_NAME_FORMAT, StringComparison.Ordinal) ||
                                   format.Equals(Flag.COLOR_SHORT_NAME_FORMAT, StringComparison.Ordinal) ||
                                   format.Equals(Flag.COLOR_SHORT_NAME_DISCOVER_FORMAT, StringComparison.Ordinal) ||
                                   format.Equals(Flag.SHORT_NAME_DISCOVER_FORMAT, StringComparison.Ordinal)))
            return string.IsNullOrEmpty(ShortName) ? Name : ShortName!;

        return Name;
    }
    float IDeployable.Yaw => Data.UseCase switch
    {
        ZoneUseCase.Team1Main => TeamManager.Team1SpawnAngle,
        ZoneUseCase.Team2Main => TeamManager.Team2SpawnAngle,
        ZoneUseCase.Lobby => TeamManager.LobbySpawnAngle,
        _ => 0f
    };
    void IDeployable.OnDeploy(UCPlayer player, bool chat)
    {
        if (Data.UseCase is ZoneUseCase.Team1Main or ZoneUseCase.Team2Main)
        {
            ActionLog.Add(ActionLogType.DeployToLocation, "MAIN BASE " + TeamManager.TranslateName(Data.UseCase == ZoneUseCase.Team1Main ? 1ul : 2ul, 0), player);
            if (chat)
                player.SendChat(T.DeploySuccess, this);
        }
        else if (Data.UseCase is ZoneUseCase.Lobby)
        {
            if (chat)
                player.SendChat(T.DeploySuccess, this);
            ActionLog.Add(ActionLogType.DeployToLocation, "LOBBY", player);
        }
        else
        {
            if (chat)
                player.SendChat(T.DeploySuccess, this);
            ActionLog.Add(ActionLogType.DeployToLocation, "ZONE " + Name, player);
        }
    }

    float IDeployable.GetDelay()
    {
        return Data.UseCase switch
        {
            ZoneUseCase.Team1Main or ZoneUseCase.Team2Main => FOBManager.Config.DeployMainDelay,
            _ => 0f
        };
    }
}