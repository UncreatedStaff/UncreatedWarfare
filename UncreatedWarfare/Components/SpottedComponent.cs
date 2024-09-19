//#define ENABLE_SPOTTED_BUFF
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Traits.Buffs;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Players.Management.Legacy;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;


#if ENABLE_SPOTTED_BUFF
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Traits;
#endif

namespace Uncreated.Warfare.Components;

public class SpottedComponent : MonoBehaviour
{
    private IServiceProvider _serviceProvider;
    private IPlayerService _playerService;

    public EffectAsset? Effect { get; private set; }
    public Spotted? Type { get; private set; }
    public VehicleType? VehicleType { get; private set; }

    /// <summary>
    /// Player who spotted the object.
    /// </summary>
    /// <remarks>May not always be online or have a value at all.</remarks>
    public WarfarePlayer? CurrentSpotter { get; private set; }
    public Team SpottingTeam => _team;
    public Team OwnerTeam { get => _vehicle is not null ? Team.NoTeam /* _vehicle.lockedGroup.m_SteamID  todo */ : _ownerTeam; set => _ownerTeam = value; }
    public bool IsActive { get => _coroutine != null; }
    public bool IsLaserTarget { get; private set; }
    private float _frequency;
    private float _defaultTimer;
    private Coroutine? _coroutine;
    public float ToBeUnspottedNonUAV;
    public float EndTime;
    public bool UAVMode;
    public WarfarePlayer? LastNonUAVSpotter = null;
    private Team _team;
    private Team _ownerTeam;
    private InteractableVehicle? _vehicle;
    public Vector3 UAVLastKnown { get; internal set; }

    public static readonly HashSet<SpottedComponent> ActiveMarkers = new HashSet<SpottedComponent>();
    public static readonly List<SpottedComponent> AllMarkers = new List<SpottedComponent>(128);
#if ENABLE_SPOTTED_BUFF
    private static bool _statInit;
#endif
    public void Initialize(Spotted type, Team ownerTeam, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();

        _ownerTeam = ownerTeam;
        _vehicle = null;
        VehicleType = null;

#if ENABLE_SPOTTED_BUFF
        if (!_statInit)
        {
            EventDispatcher.EnterVehicle += OnEnterVehicle;
            EventDispatcher.ExitVehicle += OnExitVehicle;
            _statInit = true;
        }
#endif

        Type = type;
        CurrentSpotter = null;
        IsLaserTarget = type == Spotted.FOB;

        AssetConfiguration assetConfig = serviceProvider.GetRequiredService<AssetConfiguration>();

        IAssetLink<EffectAsset>? effect;
        switch (type)
        {
            case Spotted.Infantry:
                effect = assetConfig.GetAssetLink<EffectAsset>("Effects:Spotted:Infantry");
                _defaultTimer = 12;
                _frequency = 0.5f;
                break;

            case Spotted.FOB:
                effect = assetConfig.GetAssetLink<EffectAsset>("Effects:Spotted:FOB");
                _defaultTimer = 240;
                _frequency = 1f;
                break;

            default:
                _vehicle = null;
                L.LogWarning("Unknown spotted type: " + type + " in SpottedComponent.");
                Destroy(this);
                return;
        }

        if (effect.TryGetAsset(out EffectAsset? asset))
        {
            Effect = asset;
        }
        else
        {
            L.LogWarning($"SpottedComponent could not initialize: Effect asset not found: {type}.");
        }

        if (!AllMarkers.Contains(this))
            AllMarkers.Add(this);

        L.LogDebug("Spotter initialized: " + ToString() + ".");
    }
    public void Initialize(VehicleType type, InteractableVehicle vehicle, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();

        CurrentSpotter = null;
        IsLaserTarget = type.IsGroundVehicle();
        _vehicle = vehicle;
        VehicleType = type;

        AssetConfiguration assetConfig = serviceProvider.GetRequiredService<AssetConfiguration>();
        IAssetLink<EffectAsset>? effect;
        switch (type)
        {
            case Vehicles.VehicleType.AA:
                effect = assetConfig.GetAssetLink<EffectAsset>("Effects:Spotted:AA");
                _defaultTimer = 240;
                _frequency = 1f;
                Type = Spotted.Emplacement;
                break;

            case Vehicles.VehicleType.APC:
                effect = assetConfig.GetAssetLink<EffectAsset>("Effects:Spotted:APC");
                _defaultTimer = 30;
                _frequency = 0.5f;
                Type = Spotted.Emplacement;
                break;

            case Vehicles.VehicleType.ATGM:
                effect = assetConfig.GetAssetLink<EffectAsset>("Effects:Spotted:ATGM");
                _defaultTimer = 240;
                _frequency = 1f;
                Type = Spotted.Emplacement;
                break;

            case Vehicles.VehicleType.AttackHeli:
                effect = assetConfig.GetAssetLink<EffectAsset>("Effects:Spotted:AttackHeli");
                _defaultTimer = 15;
                _frequency = 0.5f;
                Type = Spotted.Aircraft;
                break;

            case Vehicles.VehicleType.HMG:
                effect = assetConfig.GetAssetLink<EffectAsset>("Effects:Spotted:HMG");
                _defaultTimer = 240;
                _frequency = 1f;
                Type = Spotted.Emplacement;
                break;

            case Vehicles.VehicleType.Humvee:
                effect = assetConfig.GetAssetLink<EffectAsset>("Effects:Spotted:Humvee");
                _defaultTimer = 30;
                _frequency = 0.5f;
                Type = Spotted.LightVehicle;
                break;

            case Vehicles.VehicleType.IFV:
                effect = assetConfig.GetAssetLink<EffectAsset>("Effects:Spotted:IFV");
                _defaultTimer = 30;
                _frequency = 0.5f;
                Type = Spotted.Armor;
                break;

            case Vehicles.VehicleType.Jet:
                effect = assetConfig.GetAssetLink<EffectAsset>("Effects:Spotted:Jet");
                _defaultTimer = 10;
                _frequency = 0.5f;
                Type = Spotted.Aircraft;
                break;

            case Vehicles.VehicleType.MBT:
                effect = assetConfig.GetAssetLink<EffectAsset>("Effects:Spotted:MBT");
                _defaultTimer = 30;
                _frequency = 0.5f;
                Type = Spotted.Armor;
                break;

            case Vehicles.VehicleType.Mortar:
                effect = assetConfig.GetAssetLink<EffectAsset>("Effects:Spotted:Mortar");
                _defaultTimer = 240;
                _frequency = 1f;
                Type = Spotted.Emplacement;
                break;

            case Vehicles.VehicleType.ScoutCar:
                effect = assetConfig.GetAssetLink<EffectAsset>("Effects:Spotted:ScoutCar");
                _defaultTimer = 30;
                _frequency = 0.5f;
                Type = Spotted.LightVehicle;
                break;

            case Vehicles.VehicleType.TransportAir:
                effect = assetConfig.GetAssetLink<EffectAsset>("Effects:Spotted:TransportHeli");
                _defaultTimer = 15;
                _frequency = 0.5f;
                Type = Spotted.Aircraft;
                break;

            case Vehicles.VehicleType.LogisticsGround:
                effect = assetConfig.GetAssetLink<EffectAsset>("Effects:Spotted:Truck");
                _defaultTimer = 30;
                _frequency = 0.5f;
                Type = Spotted.LightVehicle;
                break;

            case Vehicles.VehicleType.TransportGround:
                effect = assetConfig.GetAssetLink<EffectAsset>("Effects:Spotted:Truck");
                _defaultTimer = 30;
                _frequency = 0.5f;
                Type = Spotted.LightVehicle;
                break;

            default:
                VehicleType = null;
                _vehicle = null;
                Type = null;
                L.LogWarning("Unknown vehicle type: " + type + " in SpottedComponent.");
                Destroy(this);
                return;
        }

        if (effect.TryGetAsset(out EffectAsset? asset))
        {
            Effect = asset;
        }
        else
        {
            L.LogWarning("SpottedComponent could not initialize: Effect asset not found: " + type + ".");
        }

        if (!AllMarkers.Contains(this))
            AllMarkers.Add(this);

        L.LogDebug("Spotter initialized: " + ToString() + ".");
    }
#if ENABLE_SPOTTED_BUFF
    private static void OnExitVehicle(ExitVehicle e)
    {
        if (!e.Vehicle.TryGetComponent(out SpottedComponent comp) || !comp.IsActive)
            return;

        if (e.Player.UnturnedPlayer.TryGetComponent(out SpottedComponent pcomp) && pcomp.IsActive)
            StartOrUpdateBuff(e.Player, false);
        else
            RemoveBuff(e.Player);
    }

    private static void OnEnterVehicle(EnterVehicle e)
    {
        if (!e.Vehicle.TryGetComponent(out SpottedComponent comp) || !comp.IsActive)
            return;

        StartOrUpdateBuff(e.Player, true);
    }
#endif
    public static void MarkTarget(Transform transform, WarfarePlayer spotter, IServiceProvider serviceProvider, bool isUav = false)
    {
        if (!transform.gameObject.TryGetComponent(out SpottedComponent spotted))
            return;

        ITranslationValueFormatter formatter = serviceProvider.GetRequiredService<ITranslationValueFormatter>();
        IPlayerService playerService = serviceProvider.GetRequiredService<IPlayerService>();
        WarfarePlayer warfarePlayer;

        if (transform.TryGetComponent(out InteractableVehicle vehicle) && vehicle.lockedGroup.m_SteamID != spotter.Team)
        {
            if (vehicle.transform.TryGetComponent(out VehicleComponent vc) && vc.VehicleData != null)
                spotted.TryAnnounce(spotter, formatter.FormatEnum(vc.VehicleData.Type, formatter.LanguageService.GetDefaultLanguage()));
            else
                spotted.TryAnnounce(spotter, vehicle.asset.vehicleName);

            L.LogDebug("Spotting vehicle " + vehicle.asset.vehicleName);
            spotted.Activate(spotter, isUav);
        }
        else if (transform.TryGetComponent(out Player player) && (warfarePlayer = playerService.GetOnlinePlayer(player)).Team != spotter.Team /* todo && !Ghost.IsHidden(warfarePlayer) */)
        {
            spotted.TryAnnounce(spotter, T.SpottedTargetPlayer.Translate(formatter.LanguageService.GetDefaultLanguage()));
            L.LogDebug("Spotting player " + player.name);

            spotted.Activate(spotter, isUav);
        }
        //else if (transform.TryGetComponent(out Cache cache) && cache.Team != spotter.GetTeam())
        //{
        //    spotted.TryAnnounce(spotter, T.SpottedTargetCache.Translate(formatter.LanguageService.GetDefaultLanguage()));
        //    L.LogDebug("Spotting cache " + cache.Name);
        //
        //    spotted.Activate(spotter, isUav);
        //}
        else
        {
            BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(transform);
            if (drop == null || drop.GetServersideData().group == spotter.Team)
                return;

            spotted.TryAnnounce(spotter, T.SpottedTargetFOB.Translate(formatter.LanguageService.GetDefaultLanguage()));
            L.LogDebug("Spotting barricade " + drop.asset.itemName);
            spotted.Activate(spotter, isUav);
        }
    }

    public void OnTargetKilled(int assistXP, int assistRep)
    {
        if (CurrentSpotter == null)
            return;

        // todo Points.AwardXP(new XPParameters(CurrentSpotter.Steam64, _team, assistXP)
        // {
        //     OverrideReputationAmount = assistRep,
        //     Multiplier = 1f,
        //     Message = PointsConfig.GetDefaultTranslation(CurrentSpotter.Locale.LanguageInfo, CurrentSpotter.Locale.CultureInfo, XPReward.KillAssist),
        //     Reward = XPReward.KillAssist
        // });
    }

    public void Activate(WarfarePlayer spotter, bool isUav) => Activate(spotter, _defaultTimer, isUav);
    public void Activate(WarfarePlayer spotter, float seconds, bool isUav)
    {
        if (this == null)
        {
            _coroutine = null;
            return;
        }
        EndTime = Time.realtimeSinceStartup + seconds;
        if (!isUav)
            ToBeUnspottedNonUAV = EndTime;
        else
            UAVLastKnown = transform.position;
        UAVMode = isUav;
        if (_coroutine != null)
            StopCoroutine(_coroutine);

        CurrentSpotter = spotter;
        _team = spotter.Team;

        _coroutine = StartCoroutine(MarkerLoop());

        // todo if (!isUav)
        // todo     spotter.ActivateMarker(this);
#if ENABLE_SPOTTED_BUFF
        if (Type is Spotted.Infantry or Spotted.LightVehicle or Spotted.Armor or Spotted.Aircraft or Spotted.Emplacement)
        {
            if (Type == Spotted.Infantry)
            {
                WarfarePlayer? target = _serviceProvider.GetRequiredService<IPlayerService>().GetOnlinePlayerOrNull(GetComponent<Player>());
                if (target != null)
                    StartOrUpdateBuff(target, false);
            }
            else if (TryGetComponent(out InteractableVehicle vehicle) && vehicle.passengers.Length > 0)
            {
                IPlayerService playerService = _serviceProvider.GetRequiredService<IPlayerService>();
                for (int i = 0; i < vehicle.passengers.Length; ++i)
                {
                    WarfarePlayer? target = playerService.GetOnlinePlayerOrNull(vehicle.passengers[i].player);
                    if (target != null)
                        StartOrUpdateBuff(target, true);
                }
            }
        }
#endif
        L.LogDebug("New Spotter activated: " + this);
    }
    internal void OnUAVLeft()
    {
        if (IsActive)
        {
            if (LastNonUAVSpotter != null && LastNonUAVSpotter.IsOnline && LastNonUAVSpotter.Team != OwnerTeam)
            {
                EndTime = ToBeUnspottedNonUAV;
                CurrentSpotter = LastNonUAVSpotter;
                UAVMode = false;
            }
            else Deactivate();
        }
    }
#if ENABLE_SPOTTED_BUFF
    private static void StartOrUpdateBuff(WarfarePlayer target, bool isVehicle)
    {
        if (!isVehicle)
        {
            InteractableVehicle? veh = target.Player.movement.getVehicle();
            if (veh != null && veh.TryGetComponent(out SpottedComponent c) && c.IsActive)
                isVehicle = true;
        }
        for (int i = 0; i < target.ActiveBuffs.Length; ++i)
        {
            if (target.ActiveBuffs[i] is SpottedBuff b)
            {
                if (b.IsVehicle != isVehicle)
                {
                    b.IsVehicle = isVehicle;
                    TraitManager.BuffUI.UpdateBuffTimeState(b);
                }
                return;
            }
        }

        TraitManager.BuffUI.AddBuff(target, new SpottedBuff(target) { IsVehicle = isVehicle });
    }
    private static void RemoveBuff(WarfarePlayer target)
    {
        for (int i = 0; i < target.ActiveBuffs.Length; ++i)
        {
            if (target.ActiveBuffs[i] is SpottedBuff b)
            {
                TraitManager.BuffUI.RemoveBuff(target, b);
                break;
            }
        }
    }
#endif

    public void Deactivate()
    {
        L.LogDebug("New Spotter deactivated: " + this);
        // todo if (CurrentSpotter != null && CurrentSpotter.IsOnline)
        //     CurrentSpotter.DeactivateMarker(this);

        if (_coroutine != null)
            StopCoroutine(_coroutine);

        _coroutine = null;
        CurrentSpotter = null;
#if ENABLE_SPOTTED_BUFF
        if (Type is Spotted.Infantry or Spotted.LightVehicle or Spotted.Armor or Spotted.Aircraft or Spotted.Emplacement)
        {
            if (Type == Spotted.Infantry)
            {
                WarfarePlayer? target = WarfarePlayer.FromPlayer(GetComponent<Player>());
                if (target != null)
                    RemoveBuff(target);
            }
            else if (TryGetComponent(out InteractableVehicle vehicle) && vehicle.passengers.Length > 0)
            {
                for (int i = 0; i < vehicle.passengers.Length; ++i)
                {
                    WarfarePlayer? target = WarfarePlayer.FromSteamPlayer(vehicle.passengers[i].player);
                    if (target != null)
                        RemoveBuff(target);
                }
            }
        }
#endif
        ActiveMarkers.Remove(this);
        UAVMode = false;
    }
    internal void SendMarkers()
    {
        Vector3 pos = UAVMode ? UAVLastKnown : transform.position;
        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            if (player.Team == _team && (player.Position - pos).sqrMagnitude < 650 * 650)
            {
                if (Effect != null)
                    EffectUtility.TriggerEffect(Effect, player.Connection, pos, false);
            }
        }
    }
    private void TryAnnounce(WarfarePlayer spotter, string targetName)
    {
        if (IsActive)
            return;

        ToastMessage.QueueMessage(spotter, new ToastMessage(ToastMessageStyle.Mini, T.SpottedToast.Translate(spotter)));

        Team t = spotter.Team;
        Color t1 = t.Faction.Color;
        // todo targetName = targetName.Colorize(Teams.TeamManager.GetTeamHexColor(Teams.TeamManager.Other(t)));
        
        foreach (LanguageSet set in _serviceProvider.GetRequiredService<ITranslationService>().SetOf.PlayersOnTeam(t))
        {
            string t2 = T.SpottedMessage.Translate(t1, targetName, in set);
            while (set.MoveNext())
                ChatManager.serverSendMessage(t2, Palette.AMBIENT, spotter.SteamPlayer, set.Next.SteamPlayer, EChatMode.SAY, null, true);
        }
    }
    private IEnumerator<WaitForSeconds> MarkerLoop()
    {
        ActiveMarkers.Add(this);

        while (UAVMode || Time.realtimeSinceStartup < EndTime)
        {
            SendMarkers();

            yield return new WaitForSeconds(_frequency);
        }

        Deactivate();
    }

    [UsedImplicitly]
    private void OnDestroy()
    {
        Deactivate();
        AllMarkers.Remove(this);
        // if (!_statInit)
        //     return;
        
        //EventDispatcher.EnterVehicle -= OnEnterVehicle;
        //EventDispatcher.ExitVehicle -= OnExitVehicle;
        //_statInit = false;
    }

    public override string ToString()
    {
        return $"Spotter ({GetInstanceID()}) for {Type}: {(IsActive ? "Spotted" : "Not Spotted")}, CurrentSpotter: {(CurrentSpotter == null ? "null" : CurrentSpotter.Names.PlayerName)}. Under UAV: {(UAVMode ? "Yes" : "No")}, Spotting team: {SpottingTeam}, Owner Team: {OwnerTeam}";
    }
    public enum Spotted
    {
        Infantry,
        FOB,
        LightVehicle,
        Armor,
        Aircraft,
        Emplacement,
        UAV // todo
    }

#if ENABLE_SPOTTED_BUFF
    private sealed class SpottedBuff : IBuff
    {
        public readonly WarfarePlayer Player;
        public bool IsVehicle;
        bool IBuff.IsBlinking => true;
        bool IBuff.Reserved => true;
        string IBuff.Icon => IsVehicle ? Gamemode.Config.UIIconVehicleSpotted : Gamemode.Config.UIIconSpotted;
        WarfarePlayer IBuff.Player => Player;

        public SpottedBuff(WarfarePlayer player)
        {
            Player = player;
        }
    }
#endif
}
