using SDG.Unturned;
using UnityEngine;

namespace Uncreated.Warfare.Events.Barricades;
public class PlaceBarricadeRequested : BreakableEvent
{
    private readonly UCPlayer? _originalPlacer;
    private readonly Barricade _barricade;
    private readonly ItemBarricadeAsset _asset;
    private readonly Transform? _hit;
    private readonly InteractableVehicle? _vehicle;
    private Vector3 _position;
    private Vector3 _rotation;
    private ulong _owner;
    private ulong _group;
    public UCPlayer? OriginalPlacer => _originalPlacer;
    public Barricade Barricade => _barricade;
    public ItemBarricadeAsset Asset => _asset;
    public Transform? HitTarget => _hit;
    public InteractableVehicle? TargetVehicle => _vehicle;
    public bool IsOnVehicle => _vehicle != null;
    public Vector3 Position
    {
        get => _position;
        set => _position = value;
    }
    public Vector3 Rotation
    {
        get => _rotation;
        set => _rotation = value;
    }
    public ulong Owner
    {
        get => _owner;
        set => _owner = value;
    }
    public ulong GroupOwner
    {
        get => _group;
        set => _group = value;
    }
    public PlaceBarricadeRequested(UCPlayer? placer, InteractableVehicle? vehicle, Barricade barricade, ItemBarricadeAsset asset, Transform? hit, Vector3 point, Vector3 rotation, ulong ownerId, ulong group, bool shouldAllow)
    {
        _originalPlacer = placer;
        _vehicle = vehicle;
        _barricade = barricade;
        _asset = asset;
        _hit = hit;
        _position = point;
        _rotation = rotation;
        _owner = ownerId;
        _group = group;
        if (!shouldAllow) Break();
    }
}
