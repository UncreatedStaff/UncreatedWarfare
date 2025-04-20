using SDG.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Proximity;

public class ColliderProximity : MonoBehaviour, ITrackingProximity<WarfarePlayer>, IDisposable, IFormattable, INearestPointProximity
{
    private readonly List<WarfarePlayer> _players = new List<WarfarePlayer>(8);
    private Collider? _collider;
    private bool _initialized;
    private IProximity _proximity;
    private bool _leaveGameObjectAlive;
    private bool _disposed;
    private IPlayerService _playerService;
    private Func<WarfarePlayer, bool>? _validationCheck;
    public IProximity Proximity => _proximity;

    public event Action<WarfarePlayer>? OnObjectEntered;
    public event Action<WarfarePlayer>? OnObjectExited;

    public IReadOnlyList<WarfarePlayer> ActiveObjects { get; }

    public ColliderProximity()
    {
        ActiveObjects = new ReadOnlyCollection<WarfarePlayer>(_players);
    }

    public void Initialize(IProximity proximity,
        IPlayerService playerService,
        bool leaveGameObjectAlive,
        Action<Collider>? colliderSettings = null,
        Func<WarfarePlayer, bool>? validationCheck = null
    )
    {
        GameThread.AssertCurrent();

        if (_initialized)
            throw new InvalidOperationException("Already initialized.");

        _initialized = true;
        _leaveGameObjectAlive = leaveGameObjectAlive;
        _playerService = playerService;

        _proximity = proximity;
        _validationCheck = validationCheck;
        SetupCollider();

        colliderSettings?.Invoke(_collider!);
    }

    private void SetupCollider()
    {
        switch (_proximity)
        {
            case IAABBProximity aabb:
                Bounds aabbInfo = aabb.Dimensions;
                
                transform.position = aabbInfo.center;
                
                BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
                boxCollider.center = Vector3.zero;
                boxCollider.size = aabbInfo.size;
                boxCollider.isTrigger = true;

                _collider = boxCollider;
                break;

            case IAACylinderProximity cylinder:
                transform.position = cylinder.Center;

                CapsuleCollider capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
                capsuleCollider.center = Vector3.zero;
                capsuleCollider.radius = cylinder.Radius;
                capsuleCollider.height = cylinder.Height;
                capsuleCollider.isTrigger = true;

                break;

            case ISphereProximity sphere:
                BoundingSphere sphereInfo = sphere.Sphere;

                transform.position = sphereInfo.position;

                SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.radius = sphereInfo.radius;
                sphereCollider.center = Vector3.zero;
                sphereCollider.isTrigger = true;

                _collider = sphereCollider;
                break;

            case IPolygonProximity polygon:
                Bounds b = polygon.worldBounds;
                transform.position = b.center;
                
                boxCollider = gameObject.AddComponent<BoxCollider>();
                boxCollider.center = Vector3.zero;
                boxCollider.size = b.size;
                boxCollider.isTrigger = true;

                _collider = boxCollider;
                break;

            default:
                throw new ArgumentException("Unsupported proximity type.", "proximity");
        }
    }

    [UsedImplicitly]
    private void FixedUpdate()
    {
        for (int i = _players.Count - 1; i >= 0; --i)
        {
            WarfarePlayer player = _players[i];
            if (player.IsOnline && _proximity.TestPoint(player.Position) && (_validationCheck == null || _validationCheck(player)))
                continue;

            RemoveObject(i);
        }
    }

    [UsedImplicitly]
    private void OnTriggerStay(Collider collider)
    {
        WarfarePlayer? player = _playerService.GetOnlinePlayerOrNull(DamageTool.getPlayer(collider.transform));
        if (player != null)
        {
            OnPlayerStay(player);
            return;
        }

        InteractableVehicle? vehicle = collider.GetComponentInParent<InteractableVehicle>();
        if (vehicle is null)
            return;

        foreach (Passenger passenger in vehicle.passengers)
        {
            WarfarePlayer? pl = _playerService.GetOnlinePlayerOrNull(passenger.player?.player);
            if (pl != null)
                OnPlayerStay(pl);
        }
        return;
    }

    private void OnPlayerStay(WarfarePlayer player)
    {
        Vector3 position = player.Position;
        for (int i = 0; i < _players.Count; ++i)
        {
            if (!player.Equals(_players[i]))
                continue;

            if (!_proximity.TestPoint(in position) || _validationCheck != null && !_validationCheck(player))
            {
                RemoveObject(i);
            }

            return;
        }

        if (!_proximity.TestPoint(in position) || _validationCheck != null && !_validationCheck(player))
            return;

        AddObject(player);
    }

    private void RemoveObject(int index)
    {
        WarfarePlayer player = _players[index];
        _players.RemoveAt(index);
        OnObjectExited?.Invoke(player);
    }

    private void AddObject(WarfarePlayer value)
    {
        _players.Add(value);
        OnObjectEntered?.Invoke(value);
    }

    [UsedImplicitly]
    private void OnTriggerExit(Collider collider)
    {
        WarfarePlayer? player = _playerService.GetOnlinePlayerOrNull(DamageTool.getPlayer(collider.transform));
        if (player == null)
            return;

        for (int i = 0; i < _players.Count; ++i)
        {
            if (player.Equals(_players[i]))
                continue;

            RemoveObject(i);
            break;
        }
    }

    public bool Contains(WarfarePlayer obj)
    {
        if (obj == null)
            return false;

        for (int i = 0; i < _players.Count; ++i)
        {
            if (obj.Equals(_players[i]))
                return true;
        }

        return false;
    }

    public bool TestPoint(in Vector3 position)
    {
        return _proximity.TestPoint(in position);
    }

    public bool TestPoint(in Vector2 position)
    {
        return _proximity.TestPoint(in position);
    }

    public void Dispose()
    {
        if (GameThread.IsCurrent)
        {
            DisposeIntl();
        }
        else
        {
            if (_disposed)
                return;

            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                DisposeIntl();
            });
        }
    }

    private void DisposeIntl()
    {
        if (_disposed)
            return;

        if (_proximity is IDisposable disp)
            disp.Dispose();

        _disposed = true;
        
        if (_leaveGameObjectAlive)
        {
            if (this != null)
                Destroy(this);
        }
        else if (gameObject != null)
        {
            Destroy(gameObject);
        }

        if (_collider == null)
            return;

        if (_collider is MeshCollider meshCollider)
        {
            Mesh mesh = meshCollider.sharedMesh;
            meshCollider.sharedMesh = null;
            Destroy(mesh);
        }

        Destroy(_collider);
        _collider = null!;
    }
    
    private void OnDestroy()
    {
        DisposeIntl();
    }

    Bounds IShapeVolume.worldBounds => _proximity.worldBounds;
    float IProximity.Area => _proximity.Area;
    float IShapeVolume.internalVolume => _proximity.internalVolume;
    float IShapeVolume.surfaceArea => _proximity.surfaceArea;
    bool IShapeVolume.containsPoint(Vector3 point) => _proximity.containsPoint(point);

    /// <inheritdoc />
    public Vector3 GetNearestPointOnBorder(in Vector3 fromLocation)
    {
        if (_proximity is not INearestPointProximity p)
            throw new NotSupportedException("Expected INearestPointProximity.");

        return p.GetNearestPointOnBorder(in fromLocation);
    }

    object ICloneable.Clone() => throw new NotSupportedException();

    /// <inheritdoc />
    public override string ToString()
    {
        return _proximity.ToString();
    }

    /// <inheritdoc />
    public string ToString(string format, IFormatProvider formatProvider)
    {
        if (_proximity is IFormattable f)
            return f.ToString(format, formatProvider);

        return _proximity.ToString();
    }
}