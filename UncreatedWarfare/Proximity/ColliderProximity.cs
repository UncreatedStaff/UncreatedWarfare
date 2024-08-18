using SDG.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Uncreated.Warfare.Proximity;

public class ColliderProximity : MonoBehaviour, ITrackingProximity<Collider>, IDisposable
{
    private readonly List<Collider> _colliders = new List<Collider>(8);
    private Collider? _collider;
    private bool _initialized;
    private IProximity _proximity;
    private bool _leaveGameObjectAlive;
    private bool _disposed;
    private Func<Collider, bool>? _validationCheck;
    public IProximity Proximity => _proximity;

    public event Action<Collider>? OnObjectEntered;
    public event Action<Collider>? OnObjectExited;

    public IReadOnlyList<Collider> ActiveObjects { get; }

    public ColliderProximity()
    {
        ActiveObjects = new ReadOnlyCollection<Collider>(_colliders);
    }

    public void Initialize(IProximity proximity,
        bool leaveGameObjectAlive,
        Action<Collider>? colliderSettings = null,
        Func<Collider, bool>? validationCheck = null
    )
    {
        GameThread.AssertCurrent();

        if (_initialized)
            throw new InvalidOperationException("Already initialized.");

        _initialized = true;
        _leaveGameObjectAlive = leaveGameObjectAlive;

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
                Mesh mesh = polygon.CreateMesh(triCount: -1, originOverride: null, out Vector3 origin);

                transform.position = origin;

                MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
                meshCollider.isTrigger = true;
                meshCollider.convex = true;

                _collider = meshCollider;
                break;

            default:
                throw new ArgumentException("Unsupported proximity type.", "proximity");
        }
    }

    [UsedImplicitly]
    private void OnTriggerStay(Collider collider)
    {
        Vector3 position = collider.transform.position;
        bool foundCollider = false;
        for (int i = 0; i < _colliders.Count; ++i)
        {
            if (collider != _colliders[i])
                continue;

            if (!TestPoint(position) || _validationCheck != null && !_validationCheck(collider))
            {
                _colliders.RemoveAt(i);
                OnObjectExited?.Invoke(collider);
                return;
            }

            foundCollider = true;
            break;
        }

        if (foundCollider || !TestPoint(position) || _validationCheck != null && !_validationCheck(collider))
            return;

        _colliders.Add(collider);
        OnObjectEntered?.Invoke(collider);
    }

    [UsedImplicitly]
    private void OnTriggerExit(Collider collider)
    {
        for (int i = 0; i < _colliders.Count; ++i)
        {
            if (collider != _colliders[i])
                continue;

            _colliders.RemoveAt(i);
            OnObjectExited?.Invoke(collider);
            break;
        }
    }

    public bool Contains(Collider obj)
    {
        if (obj == null)
            return false;

        for (int i = 0; i < _colliders.Count; ++i)
        {
            if (ReferenceEquals(obj, _colliders[i]))
                return true;
        }

        return false;
    }

    public bool TestPoint(Vector3 position)
    {
        return _proximity.TestPoint(position);
    }

    public bool TestPoint(Vector2 position)
    {
        return _proximity.TestPoint(position);
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

    Bounds IShapeVolume.worldBounds => _proximity.worldBounds;
    float IProximity.Area => _proximity.Area;
    float IShapeVolume.internalVolume => _proximity.internalVolume;
    float IShapeVolume.surfaceArea => _proximity.surfaceArea;
    bool IShapeVolume.containsPoint(Vector3 point) => _proximity.containsPoint(point);
    object ICloneable.Clone() => throw new NotSupportedException();
}