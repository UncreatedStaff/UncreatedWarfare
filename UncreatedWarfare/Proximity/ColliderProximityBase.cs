using SDG.Framework.Utilities;
using SDG.Unturned;
using System;
using UnityEngine;

namespace Uncreated.Warfare.Proximity;
public abstract class ColliderProximityBase : MonoBehaviour, IProximity, IDisposable
{
    private Collider? _collider;
    private bool _initialized;
    private IProximity _proximity;
    public IProximity Proximity => _proximity;

    public void Initialize(IProximity proximity, Action<Collider>? colliderSettings = null)
    {
        ThreadUtil.assertIsGameThread();

        if (_initialized)
            throw new InvalidOperationException("Already initialized.");

        _initialized = true;

        _proximity = proximity;
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

                _collider = boxCollider;
                break;

            case ISphereProximity sphere:
                BoundingSphere sphereInfo = sphere.Sphere;

                transform.position = sphereInfo.position;

                SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.radius = sphereInfo.radius;
                sphereCollider.center = Vector3.zero;

                _collider = sphereCollider;
                break;

            case IPolygonProximity polygon:
                Mesh mesh = polygon.CreateMesh(triCount: -1, originOverride: null, out Vector3 origin);

                transform.position = origin;

                MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
                meshCollider.convex = false;

                _collider = meshCollider;
                break;

            default:
                throw new ArgumentException("Unsupported proximity type.", "proximity");
        }
    }

    public object Clone()
    {
        throw new NotImplementedException();
    }

    public bool TestPoint(Vector3 position)
    {
        throw new NotImplementedException();
    }

    public bool TestPoint(Vector2 position)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        if (isActiveAndEnabled)
            Destroy(this);

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
    float IShapeVolume.internalVolume => _proximity.internalVolume;
    float IShapeVolume.surfaceArea => _proximity.surfaceArea;
    bool IShapeVolume.containsPoint(Vector3 point) => _proximity.containsPoint(point);
}