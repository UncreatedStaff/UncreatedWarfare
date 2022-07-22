using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags;
/// <summary>
/// Do not depend on object equality for your plugins. Zone objects will be cycled every time the file is re-read.
/// The equality operators and function will compare names with <see cref="StringComparison.OrdinalIgnoreCase"/>. This is the most reliable way to compare <see cref="Zone"/>s.
/// </summary>
public abstract class Zone : IDeployable
{
    internal int Id = -1;
    private static bool isReady = false;
    /// <summary>
    /// For converting between image sources and coordinate sources.
    /// </summary>
    protected static float ImageMultiplier;
    internal static ushort lvlSize;
    internal static ushort lvlBrdr;
    /// <summary>Returns a zone builder scaled to world coordinates.</summary>
    internal abstract ZoneBuilder Builder { get; }
    internal static void OnLevelLoaded()
    {
        lvlSize = Level.size;
        lvlBrdr = Level.border;
        ImageMultiplier = (lvlSize - lvlBrdr * 2) / (float)lvlSize;
        isReady = true;
    }
    /// <summary>
    /// Convert 2 <see langword="float"/> that was gotten from the Map image to world coordinates.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (float x, float y) FromMapCoordinates(float x, float y)
    {
        return ((x - lvlSize / 2) * ImageMultiplier, (y - lvlSize / 2) * -ImageMultiplier);
    }
    /// <summary>
    /// Convert a <see cref="Vector2"/> that was gotten from the Map image to world coordinates.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 FromMapCoordinates(Vector2 v2)
    {
        return new Vector2((v2.x - lvlSize / 2) * ImageMultiplier, (v2.y - lvlSize / 2) * -ImageMultiplier);
    }
    /// <summary>
    /// Convert a <see cref="Vector2"/> that was gotten from world coordinates to Map image coordinates.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static Vector2 ToMapCoordinates(Vector2 v2)
    {
        return new Vector2(v2.x / ImageMultiplier, v2.y / ImageMultiplier);
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
    public readonly EZoneType Type;
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
            else return _center;
        }
    }
    protected bool SucessfullyParsed = false;
    internal readonly ZoneModel Data;
    protected Vector2[]? _particleSpawnPoints;
    /// <summary>
    /// Display name for the zone.
    /// </summary>
    public readonly string Name;
    /// <summary>
    /// Shorter display name for the zone. Optional.
    /// </summary>
    public readonly string? ShortName;
    protected Vector4 _bounds;
    protected float _boundArea;
    /// <summary>
    /// Square area of the bounds rectangle, good for sorting layers.
    /// </summary>
    /// <remarks>Cached</remarks>
    public float BoundsArea => _boundArea;
    /// <summary>
    /// Rectangular bounds of the zone. (x = left, y = bottom, z = right, w = top)
    /// </summary>
    /// <remarks>Cached</remarks>
    public Vector4 Bounds => _bounds;
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
        $"{(!float.IsNaN(MaxHeight) ? $" Max Height: {MaxHeight}." : string.Empty)}{(!float.IsNaN(MinHeight)? $" Min Height: {MinHeight}." : string.Empty)}";
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
    private Zone()
    { 
        throw new NotImplementedException();
    }
    private bool drawDataGenerated = false;
    private DrawData _drawData;
    internal DrawData DrawingData
    {
        get
        {
            if (!drawDataGenerated)
            {
                _drawData = GenerateDrawData();
                drawDataGenerated = true;
            }
            return _drawData;
        }
    }

    Vector3 IDeployable.Position => Center3D + new Vector3(0, 1.5f, 0);

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
    internal Zone(ref ZoneModel data)
    {
        this.UseMapCoordinates = data.UseMapCoordinates;
        if (UseMapCoordinates && !isReady)
            throw new InvalidOperationException("Tried to construct a zone before the level has loaded.");
        this.Data = data;
        this.Type = data.ZoneType;
        this.ShortName = data.ShortName;
        this.Name = data.Name;
        this.MinHeight = data.MinimumHeight;
        this.MaxHeight = data.MaximumHeight;
        this.Id = data.Id;
        if (data.UseMapCoordinates)
        {
            this.Center = FromMapCoordinates(new Vector2(data.X, data.Z));
            this._center = new Vector3(Center.x, 0f, Center.y);
        }
        else
        {
            this._center = new Vector3(data.X, 0f, data.Z);
            this.Center = new Vector2(_center.x, _center.z);
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
    string ITranslationArgument.Translate(string language, string? format, UCPlayer? target, ref TranslationFlags flags)
    {
        if (format is not null && (format.Equals(Flag.SHORT_NAME_FORMAT, StringComparison.Ordinal) || 
                                   format.Equals(Flag.SHORT_NAME_FORMAT_COLORED, StringComparison.Ordinal) ||
                                   format.Equals(Flag.SHORT_NAME_FORMAT_COLORED_DISCOVER, StringComparison.Ordinal) ||
                                   format.Equals(Flag.SHORT_NAME_FORMAT_DISCOVER, StringComparison.Ordinal)))
            return string.IsNullOrEmpty(ShortName) ? Name : ShortName!;

        return Name;
    }
    float IDeployable.Yaw => Data.UseCase switch
    {
        EZoneUseCase.T1_MAIN => TeamManager.Team1SpawnAngle,
        EZoneUseCase.T2_MAIN => TeamManager.Team2SpawnAngle,
        EZoneUseCase.LOBBY => TeamManager.LobbySpawnAngle,
        _ => 0f
    };
    void IDeployable.OnDeploy(UCPlayer player, bool chat)
    {
        if (Data.UseCase is EZoneUseCase.T1_MAIN or EZoneUseCase.T2_MAIN)
        {
            ActionLogger.Add(EActionLogType.DEPLOY_TO_LOCATION, "MAIN BASE " + TeamManager.TranslateName(Data.UseCase == EZoneUseCase.T1_MAIN ? 1ul : 2ul, 0), player);
            if (chat)
                player.Message("deploy_s", "f0c28d", "MAIN");
        }
        else if (Data.UseCase is EZoneUseCase.LOBBY)
        {
            if (chat)
                player.Message("deploy_s", "f0c28d", "LOBBY");
            ActionLogger.Add(EActionLogType.DEPLOY_TO_LOCATION, "LOBBY", player);
        }
        else
        {
            if (chat)
                player.Message("deploy_s", "f0c28d", Name);
            ActionLogger.Add(EActionLogType.DEPLOY_TO_LOCATION, "ZONE " + Name, player);
        }
    }
}