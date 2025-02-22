#if DEBUG
#define SPOTTER_DEBUG_LOG
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.Icons;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Squads.Spotted;

public class SpottableObjectComponent : MonoBehaviour, IManualOnDestroy
{
    private record struct SpotterTypeStats(
        SpottedType Type,
        VehicleType VehicleType,
        float DefaultTimer,
        float UpdateFrequency,
        string EffectName,
        Vector3 Offset);

    private struct SpotterInfo
    {
        public float TimeExpired;
        public bool IsTickable;
        public Team Team;
        public ISpotter Spotter;
    }

    private struct MultipleTeamIconPair
    {
        public WorldIconInfo? Icon;
        public Team Team;
        public bool AnySpotterIsTickable;
    }

    private static readonly SpotterTypeStats[] TypeStats =
    [
        // vehicles (try to keep in order of VehicleType)
        new SpotterTypeStats(SpottedType.LightVehicle, VehicleType.Humvee,          30f,  0.5f, "Effects:Spotted:Humvee",        Vector3.zero),
        new SpotterTypeStats(SpottedType.LightVehicle, VehicleType.TransportGround, 30f,  0.5f, "Effects:Spotted:Truck",         Vector3.zero),
        new SpotterTypeStats(SpottedType.LightVehicle, VehicleType.ScoutCar,        30f,  0.5f, "Effects:Spotted:ScoutCar",      Vector3.zero),
        new SpotterTypeStats(SpottedType.LightVehicle, VehicleType.LogisticsGround, 30f,  0.5f, "Effects:Spotted:Truck",         Vector3.zero),
        new SpotterTypeStats(SpottedType.Armor,        VehicleType.APC,             30f,  0.5f, "Effects:Spotted:APC",           Vector3.zero),
        new SpotterTypeStats(SpottedType.Armor,        VehicleType.IFV,             30f,  0.5f, "Effects:Spotted:IFV",           Vector3.zero),
        new SpotterTypeStats(SpottedType.Armor,        VehicleType.MBT,             30f,  0.5f, "Effects:Spotted:MBT",           Vector3.zero),
        new SpotterTypeStats(SpottedType.Aircraft,     VehicleType.TransportHeli,    15f,  0.5f, "Effects:Spotted:TransportHeli", Vector3.zero),
        new SpotterTypeStats(SpottedType.Aircraft,     VehicleType.AttackHeli,      15f,  0.5f, "Effects:Spotted:AttackHeli",    Vector3.zero),
        new SpotterTypeStats(SpottedType.Aircraft,     VehicleType.Jet,             10f,  0.5f, "Effects:Spotted:Jet",           Vector3.zero),
        new SpotterTypeStats(SpottedType.Emplacement,  VehicleType.AA,              240f, 1.0f, "Effects:Spotted:AA",            Vector3.zero),
        new SpotterTypeStats(SpottedType.Emplacement,  VehicleType.HMG,             240f, 1.0f, "Effects:Spotted:HMG",           Vector3.zero),
        new SpotterTypeStats(SpottedType.Emplacement,  VehicleType.ATGM,            240f, 1.0f, "Effects:Spotted:ATGM",          Vector3.zero),
        new SpotterTypeStats(SpottedType.Emplacement,  VehicleType.Mortar,          240f, 1.0f, "Effects:Spotted:Mortar",        Vector3.zero),

        // other
        new SpotterTypeStats(SpottedType.Infantry,     VehicleType.None,            12f,  0.5f, "Effects:Spotted:Infantry",      new Vector3(0f, 1.5f, 0f)),
        new SpotterTypeStats(SpottedType.FOB,          VehicleType.None,            240f, 1.0f, "Effects:Spotted:FOB",           Vector3.zero)
];

    // null if never spotted
    private List<SpotterInfo>? _spotters;

    private IAssetLink<EffectAsset> _asset = null!;
    private float _defaultDuration;
    private float _updateFrequency;
    private Vector3 _offset;
    private List<MultipleTeamIconPair>? _multipleTeamIcons;

    private SpottedService? _spottedService;
    private WorldIconManager? _worldIconManager;
    private WorldIconInfo? _singleTeamActiveIcon;

    // time at which the next expire check will be done.
    //  this is calculated by the lowest expiring spotter in the list, then updated after removing them
    private float _nextExpireCheck;
    private Coroutine? _expireCoroutine;

    // WarfarePlayer, InteractableVehicle, or IBuildable
    private object _owner = null!;

    /// <summary>
    /// If a player or UAV is spotting this object.
    /// </summary>
    public bool IsSpotted => _spotters is { Count: > 0 };

    /// <summary>
    /// The category of object being spotted.
    /// </summary>
    public SpottedType Type { get; private set; }

    /// <summary>
    /// The category of vehicle being spotted, or <see cref="VehicleType.None"/> if not a vehicle.
    /// </summary>
    public VehicleType VehicleType { get; private set; }

    public InteractableVehicle? Vehicle => _owner as InteractableVehicle;
    public IBuildable? Buildable => _owner as IBuildable;
    public WarfarePlayer? Player => _owner as WarfarePlayer;

    [UsedImplicitly]
    [SuppressMessage("CodeQuality", "IDE0051")]
    private void Awake()
    {
        ref SpotterTypeStats stats = ref Unsafe.NullRef<SpotterTypeStats>();

        ILifetimeScope serviceProvider = WarfareModule.Singleton.ServiceProvider;

        if (gameObject.CompareTag("Player"))
        {
            Player? player = GetComponent<Player>();
            if (player == null)
            {
                Destroy(this);
                return;
            }

            stats = ref FindTypeStats(SpottedType.Infantry);
            _owner = serviceProvider.Resolve<IPlayerService>().GetOnlinePlayer(player);
        }
        else if (gameObject.CompareTag("Vehicle"))
        {
            InteractableVehicle? vehicle = gameObject.GetComponent<InteractableVehicle>();
            if (vehicle == null
                || !vehicle.gameObject.TryGetComponent(out WarfareVehicleComponent vehicleComponent)
                || vehicleComponent.WarfareVehicle.Info.Type == VehicleType.None)
            {
                Destroy(this);
                return;
            }

            stats = ref FindTypeStats(vehicleComponent.WarfareVehicle.Info.Type);
            _owner = vehicle;
        }
        else if (gameObject.CompareTag("Barricade"))
        {
            BarricadeDrop? barricade = BarricadeManager.FindBarricadeByRootTransform(gameObject.transform);
            if (barricade == null)
            {
                Destroy(this);
                return;
            }

            // todo: is UAV check, want to be able to spot UAVs if we add them
            // SpottedType type = IsUav(barricade) ? SpottedType.UAV : SpottedType.FOB;
            stats = ref FindTypeStats(SpottedType.FOB);
            _owner = new BuildableBarricade(barricade);
        }
        else if (gameObject.CompareTag("Structure"))
        {
            StructureDrop? structure = StructureManager.FindStructureByRootTransform(gameObject.transform);
            if (structure == null)
            {
                Destroy(this);
                return;
            }

            stats = ref FindTypeStats(SpottedType.FOB);
            _owner = new BuildableStructure(structure);
        }
        else
        {
            Destroy(this);
            return;
        }

        _defaultDuration = stats.DefaultTimer;
        _updateFrequency = stats.UpdateFrequency;
        _offset = stats.Offset;

        _asset = serviceProvider.Resolve<AssetConfiguration>().GetAssetLink<EffectAsset>(stats.EffectName);

        Type = stats.Type;
        VehicleType = stats.VehicleType;

        _worldIconManager = serviceProvider.Resolve<WorldIconManager>();

        _spottedService = serviceProvider.Resolve<SpottedService>();
        _spottedService.AddSpottableObject(this);
    }

    public static SpottableObjectComponent? GetOrAddIfValid(InteractableVehicle vehicle)
    {
        SpottableObjectComponent comp = vehicle.transform.GetOrAddComponent<SpottableObjectComponent>();
        return comp._owner != null ? comp : null;
    }

    public static SpottableObjectComponent? GetOrAddIfValid(WarfarePlayer player)
    {
        SpottableObjectComponent comp = player.Transform.GetOrAddComponent<SpottableObjectComponent>();
        return comp._owner != null ? comp : null;
    }

    public static SpottableObjectComponent? GetOrAddIfValid(IBuildable buildable)
    {
        SpottableObjectComponent comp = buildable.Model.GetOrAddComponent<SpottableObjectComponent>();
        return comp._owner != null ? comp : null;
    }

    /// <summary>
    /// If this object is an active target for laser guided missiles.
    /// </summary>
    public bool IsLaserTarget(Team team)
    {
        if (_spotters is not { Count: > 0 })
            return false;

        if (_singleTeamActiveIcon is { Alive: true } && _singleTeamActiveIcon.TargetTeam == team)
            return true;

        if (_multipleTeamIcons != null)
        {
            for (int i = 0; i < _multipleTeamIcons.Count; i++)
            {
                MultipleTeamIconPair pair = _multipleTeamIcons[i];
                if (pair.Team == team)
                {
                    return pair.Icon is { Alive: true };
                }
            }
        }

        return false;
    }

    private static ref SpotterTypeStats FindTypeStats(SpottedType type)
    {
        for (int i = TypeStats.Length - 1; i >= 0; --i)
        {
            ref SpotterTypeStats stats = ref TypeStats[i];
            if (stats.Type == type)
                return ref stats;
        }

        throw new InvalidOperationException($"Invalid spotted type: {type}.");
    }

    private static ref SpotterTypeStats FindTypeStats(VehicleType vehicle)
    {
        // try by index first
        if ((int)vehicle <= TypeStats.Length)
        {
            ref SpotterTypeStats stats = ref TypeStats[(int)vehicle - 1];
            if (stats.VehicleType == vehicle)
                return ref stats;
        }

        for (int i = 0; i < TypeStats.Length; ++i)
        {
            ref SpotterTypeStats stats = ref TypeStats[i];
            if (stats.VehicleType == vehicle)
                return ref stats;
        }

        throw new InvalidOperationException($"Invalid vehicle type: {vehicle}.");
    }

    public bool TryAddSpotter(ISpotter spotter, float duration = float.NaN)
    {
        GameThread.AssertCurrent();

        if (spotter.Team is null || !spotter.Team.IsValid)
            throw new InvalidOperationException("Spotter must have a valid team.");

        if (_spotters != null && _spotters.Exists(x => ReferenceEquals(x.Spotter, spotter)))
        {
            return false;
        }

        bool isTickable = spotter.IsTrackable;
        spotter.OnDestroyed += OnSpotterDestroyed;

        SpotterInfo spotterInfo = default;

        spotterInfo.Team = spotter.Team;
        spotterInfo.Spotter = spotter;
        spotterInfo.IsTickable = isTickable;
        spotterInfo.TimeExpired = Time.realtimeSinceStartup + (float.IsFinite(duration) ? duration : _defaultDuration);

        (_spotters ??= new List<SpotterInfo>(2)).Add(spotterInfo);

        if (_spotters.Count == 1 || spotterInfo.TimeExpired < _nextExpireCheck)
        {
            if (_expireCoroutine != null)
                StopCoroutine(_expireCoroutine);
            _nextExpireCheck = spotterInfo.TimeExpired;
            _expireCoroutine = StartCoroutine(ExpireCheckCoroutine());
        }
        else
        {
            Log($"Expire check not updated: {_nextExpireCheck} (in {_nextExpireCheck - Time.realtimeSinceStartup} sec).");
        }

        UpdateIcons();
        return true;
    }

    private IEnumerator ExpireCheckCoroutine()
    {
        while (_spotters is { Count: > 0 })
        {
            Log($"Expire check at {_nextExpireCheck} (in {_nextExpireCheck - Time.realtimeSinceStartup} sec).");
            yield return new WaitForSecondsRealtime(_nextExpireCheck - Time.realtimeSinceStartup);

            if (_spotters is not { Count: > 0 })
                yield break;

            bool anyRemoved = false;
            float lowestExpireTime = float.MaxValue;

            float rt = Time.realtimeSinceStartup;

            for (int i = _spotters.Count - 1; i >= 0; --i)
            {
                SpotterInfo info = _spotters[i];
                if (rt <= info.TimeExpired)
                {
                    if (lowestExpireTime > info.TimeExpired)
                        lowestExpireTime = info.TimeExpired;
                    continue;
                }

                Log($"Spotter expired: {i} ({info.Spotter})");
                RemoveSpotterIntl(i);
                anyRemoved = true;
            }

            _nextExpireCheck = lowestExpireTime;
            if (anyRemoved)
                UpdateIcons();
        }

        _expireCoroutine = null;
    }

    public void RemoveAllSpotters()
    {
        if (_spotters == null)
            return;

        foreach (SpotterInfo spotter in _spotters)
        {
            spotter.Spotter.OnDestroyed -= OnSpotterDestroyed;
        }

        _spotters.Clear();
        UpdateIcons();
    }

    public bool RemoveSpotter(ISpotter spotter)
    {
        if (_spotters == null)
            return false;

        for (int i = 0; i < _spotters.Count; ++i)
        {
            SpotterInfo spotterInfo = _spotters[i];
            if (!ReferenceEquals(spotterInfo.Spotter, spotter))
            {
                continue;
            }

            RemoveSpotterIntl(i);
            UpdateIcons();
            return true;
        }

        return false;
    }

    private void OnSpotterDestroyed(ISpotter spotter)
    {
        RemoveSpotter(spotter);
    }

    private void RemoveSpotterIntl(int index)
    {
        SpotterInfo spotterInfo = _spotters![index];
        spotterInfo.Spotter.OnDestroyed -= OnSpotterDestroyed;
        _spotters.RemoveAt(index);
    }

    private void UpdateIcons()
    {
        if (_worldIconManager == null)
        {
            _singleTeamActiveIcon = null;
            _multipleTeamIcons?.Clear();
            return;
        }

        // no spotters
        if (_spotters is not { Count: > 0 })
        {
            if (_singleTeamActiveIcon is { Alive: true })
            {
                _worldIconManager.RemoveIcon(_singleTeamActiveIcon);
                _singleTeamActiveIcon = null;
            }

            if (_multipleTeamIcons is { Count: > 0 })
            {
                _multipleTeamIcons.ForEach(x => _worldIconManager.RemoveIcon(x.Icon));
                _multipleTeamIcons.Clear();
            }

            return;
        }

        // _multipleTeamIcons is used to keep separate icons for each team,
        //  if there's only one team active then _singleTeamActiveIcon is used instead.

        Team? allTeam = null;
        bool hasMultipleTeams = false;
        float latestExpiry = 0;
        bool anySpotterIsTickable = false;
        Log($"Spotters: {_spotters.Count} (now: {Time.realtimeSinceStartup})");
        foreach (SpotterInfo spotter in _spotters)
        {
            Log($" - spotter: {spotter.Spotter} tick: {spotter.IsTickable} team: {spotter.Team} expire: {spotter.TimeExpired}");
            if (spotter.TimeExpired > latestExpiry)
                latestExpiry = spotter.TimeExpired;

            anySpotterIsTickable |= spotter.IsTickable;

            if (hasMultipleTeams)
            {
                int index = _multipleTeamIcons!.FindIndex(x => x.Team == spotter.Team);

                if (index < 0)
                {
                    _multipleTeamIcons.Add(new MultipleTeamIconPair
                    {
                        Team = spotter.Team,
                        AnySpotterIsTickable = spotter.IsTickable
                    });
                }
                else if (spotter.IsTickable)
                {
                    MultipleTeamIconPair pair = _multipleTeamIcons[index];
                    if (!pair.AnySpotterIsTickable)
                    {
                        pair.AnySpotterIsTickable = true;
                        _multipleTeamIcons[index] = pair;
                    }
                }
            }
            else if (allTeam == null)
            {
                allTeam = spotter.Team;
            }
            else if (allTeam != spotter.Team)
            {
                hasMultipleTeams = true;
                if (_multipleTeamIcons != null)
                    _multipleTeamIcons.Clear();
                else
                    _multipleTeamIcons = new List<MultipleTeamIconPair>(2);
                _multipleTeamIcons.Add(new MultipleTeamIconPair
                { 
                    Team = allTeam,
                    AnySpotterIsTickable = anySpotterIsTickable
                });
                allTeam = null;
            }
        }
        Log($" Latest expiry: {latestExpiry}, anySpotterIsTickable: {anySpotterIsTickable}.");

        float duration = latestExpiry - Time.realtimeSinceStartup;
        if (!hasMultipleTeams)
        {
            // spotters only on one team
            Log($"Updating icon 1 team ({allTeam}).");
            CheckIcon(ref _singleTeamActiveIcon, duration, anySpotterIsTickable, allTeam!);
        }
        else
        {
            Log($"Updating icon many teams team ({_multipleTeamIcons!.Count}).");
            // spotters across multiple teams
            if (_singleTeamActiveIcon is { Alive: true })
            {
                _worldIconManager.RemoveIcon(_singleTeamActiveIcon);
            }

            _singleTeamActiveIcon = null;

            for (int i = 0; i < _multipleTeamIcons!.Count; i++)
            {
                MultipleTeamIconPair team = _multipleTeamIcons![i];
                WorldIconInfo? oldIcon = team.Icon;
                WorldIconInfo? newIcon = oldIcon;
                CheckIcon(ref newIcon, duration, team.AnySpotterIsTickable, team.Team);
                if (newIcon != oldIcon)
                    _multipleTeamIcons[i] = team with { Icon = newIcon };
            }
        }
    }

    [Conditional("SPOTTER_DEBUG_LOG")]
    private void Log(string msg)
    {
        if (_asset.TryGetAsset(out EffectAsset? asset))
            WarfareModule.Singleton.ServiceProvider.Resolve<ILoggerFactory>().CreateLogger(asset.name).LogDebug(msg);
    }

    private void CheckIcon(ref WorldIconInfo? icon, float duration, bool anySpotterIsTickable, Team team)
    {
        if (icon is not { Alive: true } || icon.TargetTeam != team)
        {
            _worldIconManager!.RemoveIcon(icon);
            icon = anySpotterIsTickable
                ? new WorldIconInfo(transform, _asset, team, lifetimeSec: duration)
                : new WorldIconInfo(transform.position, _asset, team, lifetimeSec: duration);

            icon.TickSpeed = _updateFrequency;
            icon.Offset = _offset;

            Log($" Icon created team: {team} duration: {duration}.");
            _worldIconManager.CreateIcon(icon);
            _multipleTeamIcons = null;
        }
        else
        {
            if (anySpotterIsTickable && icon.UnityObject is null)
                icon.UnityObject = transform;
            else if (!anySpotterIsTickable && icon.UnityObject is not null)
                icon.EffectPosition = transform.position;

            icon.KeepAliveFor(duration);
            Log($" Icon kept alive {duration} t: {team}.");
        }
    }

    // non-tickable spotters (like a UAV) can call this to notify of a known position update.
    public void OnPositionUpdated()
    {
        if (_singleTeamActiveIcon is { UnityObject: null })
        {
            _singleTeamActiveIcon.EffectPosition = transform.position;
        }

        if (_multipleTeamIcons == null)
            return;

        Vector3 position = transform.position;
        foreach (MultipleTeamIconPair pair in _multipleTeamIcons)
        {
            if (pair.Icon is { UnityObject: null })
                pair.Icon.EffectPosition = position;
        }
    }

    [UsedImplicitly]
    [SuppressMessage("CodeQuality", "IDE0051")]
    private void OnDestroy()
    {
        if (_expireCoroutine != null)
        {
            StopCoroutine(_expireCoroutine);
            _expireCoroutine = null;
        }

        _spottedService?.RemoveSpottableObject(this);
        RemoveAllSpotters();
    }

    void IManualOnDestroy.ManualOnDestroy()
    {
        Destroy(this);
    }
}