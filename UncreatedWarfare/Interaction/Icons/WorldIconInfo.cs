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
    private bool _needsRespawn = true;
    internal Vector3 LastSpawnPosition;
    internal float LastSpawnRealtime;
    internal float LastPositionUpdateRealtime;
    internal float FirstSpawnRealtime;
    private int _disposed;
    private bool _wasInvisible = true;

    internal WorldIconManager? Manager;

    // used when distance is specified to clear from players who have left the area
    private List<WarfarePlayer>? _previousPlayers;

    private Vector3 _position;

    private Color32 _color = new Color32(255, 255, 255, 0);
    private Vector3 _colorForward;
    private float _colorScale;
    private bool _colorDirty;

    private readonly bool _canTrackLifetime;
    private readonly float _maximumLifetime;
    private readonly float _minimumLifetime;
    private bool _isVisible;

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
    /// Enables debug logging for this icon.
    /// </summary>
    public bool DebugLogging { get; init; }

    /// <summary>
    /// The source of the effect's position. Interchangable with <see cref="UnityObject"/> or <see cref="Position"/>.
    /// </summary>
    /// <remarks>This can be changed at any time.</remarks>
    public ITransformObject? TransformableObject
    {
        get;
        set
        {
            field = value;
            if (value is not null)
            {
                UnityObject = null;
                _position = default;
            }
            LastPositionUpdateRealtime = 0;
        }
    }

    /// <summary>
    /// The source of the effect's position. Interchangable with <see cref="TransformableObject"/> or <see cref="Position"/>.
    /// </summary>
    /// <remarks>This can be changed at any time.</remarks>
    public Transform? UnityObject
    {
        get;
        set
        {
            field = value;
            if (value is not null)
            {
                TransformableObject = null;
                _position = default;
            }
            LastPositionUpdateRealtime = 0;
        }
    }

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
            if (TransformableObject is not null || UnityObject is not null)
                LastPositionUpdateRealtime = 0;
            TransformableObject = null;
            UnityObject = null;
        }
    }

    /// <summary>
    /// The world-position offset of the effect from the original transform object.
    /// </summary>
    public Vector3 Offset
    {
        get;
        set
        {
            field = value;
            LastPositionUpdateRealtime = 0;
        }
    }

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
    public float LifetimeSeconds { get; private set; }

    /// <summary>
    /// Number of seconds between updates.
    /// </summary>
    /// <remarks>Defaults to 1 second. Changes after originally creating the icon will not be applied.</remarks>
    public float TickSpeed { get; init; } = 1f;

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value)
                return;

            _isVisible = value;
            if (Alive)
                Manager?.UpdateIcon(Effect);
        }
    }

    public bool Alive { get; internal set; }

    /// <summary>
    /// If the rotation and scale of the target object should be taken into account. Defaults to <see langword="false"/>.
    /// </summary>
    /// <remarks>Has no effect if <see cref="EffectPosition"/> is set.</remarks>
    public bool ApplyFullTransform { get; internal set; }

    /// <summary>
    /// Radius in regions this effect should be shown. Takes the minimum of this and <see cref="RelevanceDistance"/>.
    /// </summary>
    public byte RelevanceRegions { get; set; } = byte.MaxValue;

    /// <summary>
    /// Radius in distance this effect should be shown. Takes the minimum of this and <see cref="RelevanceRegions"/>.
    /// </summary>
    public float RelevanceDistance { get; set; } = float.MaxValue;

    public bool IsDistanceLimited => RelevanceDistance <= 32768f || RelevanceRegions != byte.MaxValue;

    public WorldIconInfo(Transform transform, IAssetLink<EffectAsset> effect, Team? targetTeam = null, WarfarePlayer? targetPlayer = null, Func<WarfarePlayer, bool>? playerSelector = null, float lifetimeSec = 0)
        : this(effect, targetTeam, targetPlayer, playerSelector, lifetimeSec)
    {
        UnityObject = transform;
    }
    
    public WorldIconInfo(ITransformObject transform, IAssetLink<EffectAsset> effect, Team? targetTeam = null, WarfarePlayer? targetPlayer = null, Func<WarfarePlayer, bool>? playerSelector = null, float lifetimeSec = 0)
        : this(effect, targetTeam, targetPlayer, playerSelector, lifetimeSec)
    {
        TransformableObject = transform;
    }
    
    public WorldIconInfo(Vector3 startPosition, IAssetLink<EffectAsset> effect, Team? targetTeam = null, WarfarePlayer? targetPlayer = null, Func<WarfarePlayer, bool>? playerSelector = null, float lifetimeSec = 0)
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
        LifetimeSeconds = lifetimeSec == 0 || !float.IsFinite(lifetimeSec) ? float.MaxValue : lifetimeSec;
        _isVisible = true;

        if (!Effect.TryGetAsset(out EffectAsset? asset) || asset.lifetime <= 0)
            return;

        _canTrackLifetime = true;
        _maximumLifetime = asset.lifetime + asset.lifetimeSpread;
        _minimumLifetime = asset.lifetime - asset.lifetimeSpread;
    }

    /// <summary>
    /// Raise <see cref="LifetimeSeconds"/> such that from now the effect will despawn in a given amount of seconds.
    /// </summary>
    public void KeepAliveFor(float seconds)
    {
        LifetimeSeconds = FirstSpawnRealtime == 0 ? seconds : Time.realtimeSinceStartup - FirstSpawnRealtime + seconds;
    }

    internal void UpdateRelevantPlayers(IPlayerService playerService, ref PooledTransportConnectionList? list, ref ITransportConnection? single, in Vector3 spawnPosition, HashSet<ITransportConnection> workingHashSetCache)
    {
        bool distanceLimited = IsDistanceLimited;
        Vector3 pos = default;
        if (TargetPlayer != null)
        {
            if (distanceLimited)
                pos = TargetPlayer.Position;
            if (!distanceLimited || CheckPositionRelevant(in pos, in spawnPosition))
                Add(TargetPlayer.Connection, ref list, ref single, workingHashSetCache);
        }
        else if (TargetTeam is not null)
        {
            foreach (WarfarePlayer player in playerService.OnlinePlayersOnTeam(TargetTeam))
            {
                if (distanceLimited)
                    pos = player.Position;
                if (!distanceLimited || CheckPositionRelevant(in pos, in spawnPosition))
                    Add(player.Connection, ref list, ref single, workingHashSetCache);
            }
        }
        else if (PlayerSelector != null)
        {
            foreach (WarfarePlayer player in playerService.OnlinePlayers)
            {
                if (distanceLimited)
                    pos = player.Position;
                if ((!distanceLimited || CheckPositionRelevant(in pos, in spawnPosition)) && PlayerSelector(player))
                    Add(player.Connection, ref list, ref single, workingHashSetCache);
            }
        }
        else
        {
            foreach (WarfarePlayer player in playerService.OnlinePlayers)
            {
                if (distanceLimited)
                    pos = player.Position;
                if (!distanceLimited || CheckPositionRelevant(in pos, in spawnPosition))
                    Add(player.Connection, ref list, ref single, workingHashSetCache);
            }
        }

        if ((distanceLimited || PlayerSelector != null) && _previousPlayers != null)
        {
            foreach (WarfarePlayer player in _previousPlayers)
            {
                if (player.IsOnline)
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
    
    internal bool ShouldPlayerSeeIcon(WarfarePlayer player, in Vector3 spawnPosition)
    {
        if (TargetPlayer != null && !TargetPlayer.Equals(player))
            return false;

        if (TargetTeam is not null && player.Team != TargetTeam)
            return false;

        if (PlayerSelector != null && !PlayerSelector(player))
            return false;

        return CheckPositionRelevant(player.Position, in spawnPosition);
    }

    private bool CheckPositionRelevant(in Vector3 sendPos, in Vector3 spawnPos)
    {
        float dist = RelevanceDistance;
        int area = RelevanceRegions;
        if (dist > 32768f && area == byte.MaxValue)
            return true;

        byte regionSize = Regions.REGION_SIZE;
        if ((area + 0.5f) * regionSize > dist)
        {
            return MathUtility.SquaredDistance(in sendPos, in spawnPos, true) <= dist * dist;
        }

        if (area == byte.MaxValue)
            return true;

        int sendX = (int)Math.Floor((sendPos.x + 4096f) / regionSize);
        int sendY = (int)Math.Floor((sendPos.z + 4096f) / regionSize);
        int spawnX = (int)Math.Floor((spawnPos.x + 4096f) / regionSize);
        int spawnY = (int)Math.Floor((spawnPos.z + 4096f) / regionSize);
        return sendX >= spawnX - area && sendY >= spawnY - area &&
               sendX <= spawnX + area && sendY <= spawnY + area;
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
            try
            {
                v3 = !ApplyFullTransform ? TransformableObject.Position + Offset : TransformableObject.TransformPoint(Offset);
            }
            catch (NullReferenceException)
            {
                // TransformableObject is wrapping a unity object that was destroyed
                Alive = false;
                position = default;
                return false;
            }
        }
        else if (UnityObject is null)
        {
            v3 = _position + Offset;
        }
        else if (UnityObject != null)
        {
            v3 = !ApplyFullTransform ? UnityObject.position + Offset : UnityObject.TransformPoint(Offset);
        }
        else
        {
            Alive = false;
            position = default;
            return false;
        }

        position = v3;
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

        bool distanceLimited = IsDistanceLimited;
        bool hasMutablePlayerSelector = distanceLimited || PlayerSelector != null;

        Vector3 pos;
        if (updatePosition || LastPositionUpdateRealtime == 0 || IsVisible && _wasInvisible)
        {
            if (!TryGetSpawnPosition(out pos))
            {
                return;
            }

            if (LastSpawnPosition.IsNearlyEqual(pos) && !(_needsRespawn || hasMutablePlayerSelector) && (!_canTrackLifetime || rt - LastSpawnRealtime < _minimumLifetime))
            {
                return;
            }

            if (IsVisible)
                LastPositionUpdateRealtime = rt;
        }
        else
        {
            pos = LastSpawnPosition;
        }

        if (!IsVisible)
        {
            _wasInvisible = true;
            return;
        }

        _wasInvisible = false;

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
        else if (ApplyFullTransform)
        {
            if (TransformableObject != null)
            {
                try
                {
                    parameters.SetRotation(TransformableObject.Rotation);
                }
                catch (NullReferenceException)
                {
                    Alive = false;
                    // TransformableObject is wrapping a unity object that was destroyed
                }
            }
            else if (UnityObject is not null && UnityObject != null)
            {
                parameters.SetRotation(UnityObject.rotation);
            }
        }

        LastSpawnPosition = pos;
        _needsRespawn = false;

        if (hasMutablePlayerSelector)
        {
            if (_previousPlayers == null)
                _previousPlayers = new List<WarfarePlayer>(4);
            else
                _previousPlayers.Clear();
        }

        Vector3 sendPos = default;
        if (forPlayer != null)
        {
            if (distanceLimited)
                sendPos = forPlayer.Position;
            if (!distanceLimited || CheckPositionRelevant(in sendPos, in pos))
            {
                parameters.SetRelevantPlayer(forPlayer.Connection);
                if (hasMutablePlayerSelector)
                    _previousPlayers!.Add(forPlayer);
            }
            else
            {
                return;
            }
        }
        else if (TargetPlayer != null)
        {
            if (TargetPlayer.IsOnline)
            {
                if (distanceLimited)
                    sendPos = TargetPlayer.Position;
                if ((TargetTeam is null || TargetPlayer.Team == TargetTeam) && (!distanceLimited || CheckPositionRelevant(in sendPos, in pos)) && (PlayerSelector == null || PlayerSelector(TargetPlayer)))
                {
                    parameters.SetRelevantPlayer(TargetPlayer.Connection);
                    if (hasMutablePlayerSelector)
                        _previousPlayers!.Add(TargetPlayer);
                }
                else
                {
                    return;
                }
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
                if (distanceLimited)
                    sendPos = player.Position;

                if (player.Team == TargetTeam
                    && (!distanceLimited || CheckPositionRelevant(in sendPos, in pos))
                    && (PlayerSelector == null || PlayerSelector(player)))
                {
                    list.Add(player.Connection);
                    if (hasMutablePlayerSelector)
                        _previousPlayers!.Add(player);
                }
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
                {
                    if (distanceLimited)
                        sendPos = player.Position;
                    if (!distanceLimited || CheckPositionRelevant(in sendPos, in pos))
                    {
                        list.Add(player.Connection);
                        if (hasMutablePlayerSelector)
                            _previousPlayers!.Add(player);
                    }
                }
            }
            else
            {
                foreach (WarfarePlayer player in playerService.OnlinePlayers)
                {
                    if (distanceLimited)
                        sendPos = player.Position;
                    if ((!distanceLimited || CheckPositionRelevant(in sendPos, in pos)) && PlayerSelector(player))
                    {
                        list.Add(player.Connection);
                        if (hasMutablePlayerSelector)
                            _previousPlayers!.Add(player);
                    }
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
        set => EffectPosition = value - Offset;
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

        if (!Offset.IsNearlyZero())
        {
            str += $", offset={Offset:F2}";
        }

        return str + "}";
    }

    public void Dispose()
    {
        if (!Alive || Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        Alive = false;
        if (IsVisible)
        {
            Manager?.UpdateIcon(Effect);
            Manager = null;
        }
    }
}