using SDG.NetTransport;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Interaction.Icons;

/// <summary>
/// Tracks information about a single icon that is kept alive.
/// </summary>
public class WorldIconInfo : ITransformObject, IDisposable
{
    private Vector3 _prevSpawnPosition;
    private bool _needsRespawn = true;
    internal float LastSpawnRealtime;
    internal float LastPositionUpdateRealtime;
    internal float FirstSpawnRealtime;

    private Vector3 _position;

    private Color32 _color = new Color32(255, 255, 255, 0);
    private Vector3 _colorForward;
    private float _colorScale;
    private bool _colorDirty;

    private readonly bool _canTrackLifetime;
    private readonly float _maximumLifetime;
    private readonly float _minimumLifetime;

    /// <summary>
    /// Optional player-only target.
    /// </summary>
    public WarfarePlayer? TargetPlayer { get; }
    
    /// <summary>
    /// Optional player predicate.
    /// </summary>
    public Func<WarfarePlayer, bool>? PlayerSelector { get; }

    /// <summary>
    /// Optional team-only target.
    /// </summary>
    public Team? TargetTeam { get; }
    
    /// <summary>
    /// The effect asset that spawns.
    /// </summary>
    public IAssetLink<EffectAsset> Effect { get; }

    /// <summary>
    /// The source of the effect's position. Interchangable with <see cref="UnityObject"/> or <see cref="Position"/>.
    /// </summary>
    public ITransformObject? TransformableObject { get; private set; }

    /// <summary>
    /// The source of the effect's position. Interchangable with <see cref="TransformableObject"/> or <see cref="Position"/>.
    /// </summary>
    public Transform? UnityObject { get; private set; }

    /// <summary>
    /// The source of the effect's position. Interchangable with <see cref="TransformableObject"/> or <see cref="UnityObject"/>.
    /// </summary>
    /// <remarks>This can be changed at any time.</remarks>
    public Vector3 EffectPosition
    {
        get => _position;
        set
        {
            _position = value;
            TransformableObject = null;
            UnityObject = null;
        }
    }

    /// <summary>
    /// The world-position offset of the effect from the original transform object.
    /// </summary>
    public Vector3 Offset { get; set; }

    /// <summary>
    /// If the effect should be sent reliably.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>.</remarks>
    public bool Reliable { get; set; }

    /// <summary>
    /// Uses a shader to convert the desired rotation and scale of the effect to it's tint color.
    /// </summary>
    /// <remarks>Only used when <see cref="Color32.a"/> is max.</remarks>
    public Color32 Color
    {
        get => _color;
        set
        {
            if (value.a == _color.a && value.r == _color.r && value.g == _color.g && value.b == _color.b)
                return;

            _color = value;
            _colorDirty = true;
        }
    }

    /// <summary>
    /// Number of seconds this effect will be alive.
    /// </summary>
    public float LifetimeSeconds { get; }

    /// <summary>
    /// Number of seconds between updates.
    /// </summary>
    /// <remarks>Defaults to 1 second. Changes after originally creating the icon will not be applied.</remarks>
    public float TickSpeed { get; set; } = 1f;

    public bool Alive { get; internal set; }

    public WorldIconInfo(Transform transform, IAssetLink<EffectAsset> effect, Team? targetTeam = null, WarfarePlayer? targetPlayer = null, Func<WarfarePlayer, bool>? playerSelector = null, float lifetimeSec = default)
        : this(effect, targetTeam, targetPlayer, playerSelector, lifetimeSec)
    {
        UnityObject = transform;
    }
    
    public WorldIconInfo(ITransformObject transform, IAssetLink<EffectAsset> effect, Team? targetTeam = null, WarfarePlayer? targetPlayer = null, Func<WarfarePlayer, bool>? playerSelector = null, float lifetimeSec = default)
        : this(effect, targetTeam, targetPlayer, playerSelector, lifetimeSec)
    {
        TransformableObject = transform;
    }
    
    public WorldIconInfo(Vector3 startPosition, IAssetLink<EffectAsset> effect, Team? targetTeam = null, WarfarePlayer? targetPlayer = null, Func<WarfarePlayer, bool>? playerSelector = null, float lifetimeSec = default)
        : this(effect, targetTeam, targetPlayer, playerSelector, lifetimeSec)
    {
        _position = startPosition;
    }

    private WorldIconInfo(IAssetLink<EffectAsset> effect, Team? targetTeam, WarfarePlayer? targetPlayer, Func<WarfarePlayer, bool>? playerSelector, float lifetimeSec)
    {
        Effect = effect;
        TargetTeam = targetTeam;
        TargetPlayer = targetPlayer;
        PlayerSelector = playerSelector;
        LifetimeSeconds = lifetimeSec == 0 ? float.MaxValue : 0;

        if (!Effect.TryGetAsset(out EffectAsset? asset) || asset.lifetime <= 0)
            return;

        _canTrackLifetime = true;
        _maximumLifetime = asset.lifetime + asset.lifetimeSpread;
        _minimumLifetime = asset.lifetime - asset.lifetimeSpread;
    }

    internal void UpdateRelevantPlayers(IPlayerService playerService, ref PooledTransportConnectionList? list, ref ITransportConnection? single, HashSet<ITransportConnection> workingHashSetCache)
    {
        if (TargetPlayer != null)
        {
            Add(TargetPlayer.Connection, ref list, ref single, workingHashSetCache);
        }
        else if (TargetTeam is not null)
        {
            foreach (WarfarePlayer player in playerService.OnlinePlayersOnTeam(TargetTeam))
            {
                Add(player.Connection, ref list, ref single, workingHashSetCache);
            }
        }
        else if (PlayerSelector != null)
        {
            foreach (WarfarePlayer player in playerService.OnlinePlayers)
            {
                if (PlayerSelector(player))
                    Add(player.Connection, ref list, ref single, workingHashSetCache);
            }
        }
        else
        {
            foreach (WarfarePlayer player in playerService.OnlinePlayers)
            {
                Add(player.Connection, ref list, ref single, workingHashSetCache);
            }
        }

        static void Add(ITransportConnection connection, ref PooledTransportConnectionList? list, ref ITransportConnection? single, HashSet<ITransportConnection> workingHashSetCache)
        {
            if (!workingHashSetCache.Add(connection))
                return;

            if (single == null)
            {
                single = connection;
            }
            else if (list == null)
            {
                list = TransportConnectionPoolHelper.Claim(8);
                list.Add(single);
                list.Add(connection);
            }
            else
            {
                list.Add(connection);
            }
        }
    }
    
    internal bool ShouldPlayerSeeIcon(WarfarePlayer player)
    {
        if (TargetPlayer != null && !TargetPlayer.Equals(player))
            return false;

        if (TargetTeam is not null && player.Team != TargetTeam)
            return false;

        if (PlayerSelector != null && !PlayerSelector(player))
            return false;

        return true;
    }

    private static void GetColoredEffectForward(Color32 c, out Vector3 forward, out float scale)
    {
        /*
         * colored effects use a special shader that renders the forward vector of the rotation (xyz) of the icon as the color (rgb)
         */

        Vector3 fwd = new Vector3(c.r, c.g, c.b);
        scale = fwd.magnitude;
        forward = fwd / scale;
    }

    /// <summary>
    /// Gets the position at which the effect will spawn at this moment.
    /// </summary>
    /// <remarks>Includes <see cref="Offset"/>.</remarks>
    internal bool TryGetSpawnPosition(out Vector3 position)
    {
        Vector3 v3;
        if (TransformableObject != null)
        {
            v3 = TransformableObject.Position + Offset;
        }
        else if (UnityObject is null)
        {
            v3 = _position;
        }
        else if (UnityObject != null)
        {
            v3 = UnityObject.position + Offset;
        }
        else
        {
            position = default;
            return false;
        }

        position = v3 + Offset;
        return true;
    }

    internal bool NeedsToBeCleared(float rt)
    {
        // can save a clearing if the effect has already been cleared by it's lifetime
        return !_needsRespawn && (!_canTrackLifetime || rt - LastSpawnRealtime <= _maximumLifetime);
    }

    internal void OnCleared()
    {
        _needsRespawn = true;
    }

    /// <summary>
    /// Spawns this effect at the expected position.
    /// </summary>
    internal void SpawnEffect(IPlayerService playerService, bool updatePosition, WarfarePlayer? forPlayer = null)
    {
        SpawnEffect(playerService, Time.realtimeSinceStartup, updatePosition, forPlayer);
    }

    /// <summary>
    /// Spawns this effect at the expected position.
    /// </summary>
    internal void SpawnEffect(IPlayerService playerService, float rt, bool updatePosition, WarfarePlayer? forPlayer = null)
    {
        GameThread.AssertCurrent();

        if (!Effect.TryGetAsset(out EffectAsset? effect))
        {
            return;
        }

        Vector3 pos;
        if (updatePosition)
        {
            if (!TryGetSpawnPosition(out pos))
            {
                return;
            }

            if (_prevSpawnPosition.IsNearlyEqual(pos) && !_needsRespawn && (!_canTrackLifetime || rt - LastSpawnRealtime < _minimumLifetime))
            {
                return;
            }

            LastPositionUpdateRealtime = rt;
        }
        else
        {
            pos = _prevSpawnPosition;
        }

        TriggerEffectParameters parameters = new TriggerEffectParameters(effect);

        if (_color.a == byte.MaxValue)
        {
            if (_colorDirty)
            {
                GetColoredEffectForward(_color, out _colorForward, out _colorScale);
                _colorDirty = false;
            }

            parameters.SetDirection(_colorForward);
            parameters.SetUniformScale(_colorScale);
        }

        _prevSpawnPosition = pos;
        _needsRespawn = false;
        
        if (forPlayer != null)
        {
            parameters.SetRelevantPlayer(forPlayer.Connection);
        }
        else if (TargetPlayer != null)
        {
            if (TargetPlayer.IsOnline && (TargetTeam is null || TargetPlayer.Team == TargetTeam) && (PlayerSelector == null || PlayerSelector(TargetPlayer)))
            {
                parameters.SetRelevantPlayer(TargetPlayer.Connection);
            }
            else
            {
                return;
            }
        }
        else if (TargetTeam is not null)
        {
            PooledTransportConnectionList list = TransportConnectionPoolHelper.Claim(Provider.clients.Count / 2);
            
            foreach (WarfarePlayer player in playerService.OnlinePlayers)
            {
                if (player.Team == TargetTeam && (PlayerSelector == null || PlayerSelector(player)))
                    list.Add(player.Connection);
            }

            if (list.Count == 0)
                return;

            parameters.SetRelevantTransportConnections(list);
        }
        else
        {
            PooledTransportConnectionList list = TransportConnectionPoolHelper.Claim(Provider.clients.Count / 2);

            if (PlayerSelector == null)
            {
                if (list.Capacity < playerService.OnlinePlayers.Count)
                    list.Capacity = playerService.OnlinePlayers.Count;
                foreach (WarfarePlayer player in playerService.OnlinePlayers)
                    list.Add(player.Connection);
            }
            else
            {
                foreach (WarfarePlayer player in playerService.OnlinePlayers)
                {
                    if (PlayerSelector(player))
                        list.Add(player.Connection);
                }
            }

            if (list.Count == 0)
                return;

            parameters.SetRelevantTransportConnections(list);
        }

        parameters.position = pos;
        parameters.reliable = Reliable;

        EffectManager.triggerEffect(parameters);
        LastSpawnRealtime = rt;
        if (FirstSpawnRealtime == 0)
            FirstSpawnRealtime = rt;
    }

    Vector3 ITransformObject.Position
    {
        get
        {
            TryGetSpawnPosition(out Vector3 position);
            return position;
        }
        set
        {
            EffectPosition = value - Offset;
        }
    }

    Quaternion ITransformObject.Rotation
    {
        get => Quaternion.identity;
        set => throw new NotSupportedException();
    }

    Vector3 ITransformObject.Scale
    {
        get => Vector3.one;
        set => throw new NotSupportedException();
    }

    void ITransformObject.SetPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        string objStr;
        if (TransformableObject != null)
        {
            objStr = TransformableObject.ToString();
        }
        else if (UnityObject is null)
        {
            objStr = "generic position";
        }
        else if (UnityObject != null)
        {
            objStr = UnityObject.name;
        }
        else
        {
            objStr = "no attachment";
        }

        string str = $"World Icon: {{{objStr}: {Effect.ToDisplayString()}, target=";
        
        if (TargetPlayer != null)
        {
            if (TargetTeam is not null)
                str += $"{TargetPlayer.Steam64.m_SteamID}@{TargetTeam.Faction.FactionId}";
            else
                str += TargetPlayer.Steam64.m_SteamID;
        }
        else if (TargetTeam != null)
        {
            str += TargetTeam.Faction.FactionId;
        }
        else
        {
            str += "*";
        }

        if (Color.a == byte.MaxValue)
        {
            str += $", color={HexStringHelper.FormatHexColor(Color)}";
        }

        return str + "}";
    }

    public void Dispose()
    {
        Alive = false;
    }
}