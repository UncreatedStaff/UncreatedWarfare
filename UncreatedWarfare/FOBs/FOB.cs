using Microsoft.Extensions.DependencyInjection;
using SDG.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Proximity;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Fobs;

/// <summary>
/// Base class for standard FOBs, caches, and any other FOBs that support items.
/// </summary>
public class BasePlayableFob : MonoBehaviour, IRadiusFob, IResourceFob
{
    private ILogger _logger;
    private float _sqrRadius;
    private float _radius;
    private List<WarfarePlayer> _players;
    private List<InteractableVehicle> _vehicles;
    private List<IFobItem> _items;

    /// <summary>
    /// List of all players within the radius of the FOB.
    /// </summary>
    public IReadOnlyList<WarfarePlayer> Players { get; private set; }

    /// <summary>
    /// List of all vehicles within the radius of the FOB.
    /// </summary>
    public IReadOnlyList<InteractableVehicle> Vehicles { get; private set; }

    /// <summary>
    /// List of all items owned by this FOB.
    /// </summary>
    public IReadOnlyList<IFobItem> Items { get; private set; }

    /// <inheritdoc />
    public int AmmoCount { get; set; }

    /// <inheritdoc />
    public int BuildCount { get; set; }

    /// <inheritdoc />
    public string Name { get; private set; }

    /// <inheritdoc />
    public Color32 Color { get; private set; }

    /// <inheritdoc />
    public Team Team { get; private set; }

    /// <inheritdoc />
    public Vector3 Position
    {
        get => transform.position;
        set => transform.position = value;
    }

    /// <inheritdoc />
    public float EffectiveRadius
    {
        get => _radius;
        private set
        {
            _radius = value;
            _sqrRadius = value * value;
        }
    }

    /// <summary>
    /// Invoked when a player enters the radius of the FOB.
    /// </summary>
    public event Action<WarfarePlayer>? OnPlayerEntered;

    /// <summary>
    /// Invoked when a player exits the radius of the FOB. Always invoked before the player enters the next FOB if they teleport to another.
    /// </summary>
    public event Action<WarfarePlayer>? OnPlayerExited;

    public event Action<InteractableVehicle>? OnVehicleEntered;
    public event Action<InteractableVehicle>? OnVehicleExited;

    public event Action<IFobItem>? OnItemAdded;
    public event Action<IFobItem>? OnItemRemoved;

    protected virtual void Awake()
    {
        _players = new List<WarfarePlayer>(24);
        Players = new ReadOnlyCollection<WarfarePlayer>(_players);

        _vehicles = new List<InteractableVehicle>(4);
        Vehicles = new ReadOnlyCollection<InteractableVehicle>(_vehicles);

        _items = new List<IFobItem>(32);
        Items = new ReadOnlyCollection<IFobItem>(_items);
    }

    internal virtual void Init(IServiceProvider serviceProvider, string name, BarricadeDrop radio)
    {
        Name = name;
        _logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(GetType().Name + " | " + Name);
    }

    protected virtual void Start()
    {

    }

    public UniTask DestroyAsync(CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public UniTask AddItemAsync(IFobItem fobItem, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public UniTask BuildItemAsync(IFobItem fobItem, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Vector3 SpawnPosition => Position; // todo
    public float Yaw => 0f;

    public TimeSpan GetDelay(WarfarePlayer player)
    {
        return TimeSpan.Zero;
    }
    public bool CheckDeployableTo(WarfarePlayer player, DeploymentTranslations translations, in DeploySettings settings)
    {
        return false;
    }

    public bool CheckDeployableFrom(WarfarePlayer player, DeploymentTranslations translations, in DeploySettings settings, IDeployable deployingTo)
    {
        throw new NotImplementedException();
    }

    public bool CheckDeployableToTick(WarfarePlayer player, DeploymentTranslations translations, in DeploySettings settings)
    {
        throw new NotImplementedException();
    }

    public int CompareTo(IFob other)
    {
        return -1; // todo
    }

    public bool TestPoint(in Vector3 position)
    {
        return (transform.position - position).sqrMagnitude <= _sqrRadius;
    }

    public bool TestPoint(in Vector2 position)
    {
        Vector3 pos = transform.position;
        Vector2 pos2d = new Vector2(pos.x, pos.z);
        return (pos2d - position).sqrMagnitude <= _sqrRadius;
    }

    public override string ToString()
    {
        return "{" + Name + " | " + Team + "}";
    }

    public string Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        return formatter.Colorize(Name, Color, parameters.Options);
    }

    event Action<WarfarePlayer>? IEventBasedProximity<WarfarePlayer>.OnObjectEntered
    {
        add => OnPlayerEntered += value;
        remove => OnPlayerEntered -= value;
    }

    event Action<WarfarePlayer>? IEventBasedProximity<WarfarePlayer>.OnObjectExited
    {
        add => OnPlayerExited += value;
        remove => OnPlayerExited -= value;
    }

    event Action<IFobItem>? IEventBasedProximity<IFobItem>.OnObjectEntered
    {
        add => OnItemAdded += value;
        remove => OnItemAdded -= value;
    }

    event Action<IFobItem>? IEventBasedProximity<IFobItem>.OnObjectExited
    {
        add => OnItemRemoved += value;
        remove => OnItemRemoved -= value;
    }

    event Action<InteractableVehicle>? IEventBasedProximity<InteractableVehicle>.OnObjectEntered
    {
        add => OnVehicleEntered += value;
        remove => OnVehicleEntered -= value;
    }

    event Action<InteractableVehicle>? IEventBasedProximity<InteractableVehicle>.OnObjectExited
    {
        add => OnVehicleExited += value;
        remove => OnVehicleExited -= value;
    }

    public bool Contains(InteractableVehicle obj)
    {
        for (int i = 0; i < _vehicles.Count; ++i)
        {
            if (ReferenceEquals(_vehicles[i], obj))
                return true;
        }

        return false;
    }

    public bool Contains(IFobItem obj)
    {
        for (int i = 0; i < _items.Count; ++i)
        {
            if (ReferenceEquals(_items[i], obj))
                return true;
        }

        return false;
    }

    public bool Contains(WarfarePlayer obj)
    {
        for (int i = 0; i < _players.Count; ++i)
        {
            if (ReferenceEquals(_players[i], obj))
                return true;
        }

        return false;
    }

    object ICloneable.Clone()
    {
        throw new NotSupportedException();
    }

    IReadOnlyList<WarfarePlayer> ITrackingProximity<WarfarePlayer>.ActiveObjects => Players;

    IReadOnlyList<IFobItem> ITrackingProximity<IFobItem>.ActiveObjects => Items;

    IReadOnlyList<InteractableVehicle> ITrackingProximity<InteractableVehicle>.ActiveObjects => Vehicles;

    Matrix4x4 ITransformObject.WorldToLocal => transform.worldToLocalMatrix;

    public Matrix4x4 LocalToWorld => throw new NotImplementedException();

    public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        throw new NotImplementedException();
    }

    public Matrix4x4 WorldToLocal => throw new NotImplementedException();

    Matrix4x4 ITransformObject.LocalToWorld => transform.localToWorldMatrix;
    BoundingSphere ISphereProximity.Sphere
    {
        get
        {
            BoundingSphere sphere = default;
            sphere.position = transform.position;
            sphere.radius = _radius;
            return sphere;
        }
    }
    Bounds IShapeVolume.worldBounds
    {
        get
        {
            Vector3 center = transform.position;
            float r = _radius * 2;
            Vector3 size = default;
            size.x = r;
            size.y = r;
            size.z = r;
            return new Bounds(center, size);
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
}
