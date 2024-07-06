#define ENABLE_SPOTTED_BUFF
using JetBrains.Annotations;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Traits.Buffs;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

#if ENABLE_SPOTTED_BUFF
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Vehicles;
using Uncreated.Warfare.Traits;
#endif

namespace Uncreated.Warfare.Components;

public class SpottedComponent : MonoBehaviour
{
    public EffectAsset? Effect { get; private set; }
    public Spotted? Type { get; private set; }
    public VehicleType? VehicleType { get; private set; }
    /// <summary>Player who spotted the object.</summary>
    /// <remarks>May not always be online or have a value at all.</remarks>
    public UCPlayer? CurrentSpotter { get; private set; }
    public ulong SpottingTeam => _team;
    public ulong OwnerTeam { get => _vehicle is not null ? _vehicle.lockedGroup.m_SteamID.GetTeam() : _ownerTeam; set => _ownerTeam = value; }
    public bool IsActive { get => _coroutine != null; }
    public bool IsLaserTarget { get; private set; }
    private float _frequency;
    private float _defaultTimer;
    private Coroutine? _coroutine;
    public float ToBeUnspottedNonUAV;
    public float EndTime;
    public bool UAVMode;
    public UCPlayer? LastNonUAVSpotter = null;
    private ulong _team;
    private ulong _ownerTeam;
    private InteractableVehicle? _vehicle;
    public Vector3 UAVLastKnown { get; internal set; }

    public static readonly HashSet<SpottedComponent> ActiveMarkers = new HashSet<SpottedComponent>();
    public static readonly List<SpottedComponent> AllMarkers = new List<SpottedComponent>(128);
#if ENABLE_SPOTTED_BUFF
    private static bool _statInit;
#endif
    public void Initialize(Spotted type, ulong ownerTeam)
    {
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

        IAssetLink<EffectAsset>? effect;
        switch (type)
        {
            case Spotted.Infantry:
                effect = Gamemode.Config.EffectSpottedMarkerInfantry;
                _defaultTimer = 12;
                _frequency = 0.5f;
                break;

            case Spotted.FOB:
                effect = Gamemode.Config.EffectSpottedMarkerFOB;
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
    public void Initialize(VehicleType type, InteractableVehicle vehicle)
    {
        CurrentSpotter = null;
        IsLaserTarget = VehicleData.IsGroundVehicle(type);
        _vehicle = vehicle;
        VehicleType = type;
        IAssetLink<EffectAsset>? effect;
        switch (type)
        {
            case Vehicles.VehicleType.AA:
                effect = Gamemode.Config.EffectSpottedMarkerAA;
                _defaultTimer = 240;
                _frequency = 1f;
                Type = Spotted.Emplacement;
                break;

            case Vehicles.VehicleType.APC:
                effect = Gamemode.Config.EffectSpottedMarkerAPC;
                _defaultTimer = 30;
                _frequency = 0.5f;
                Type = Spotted.Emplacement;
                break;

            case Vehicles.VehicleType.ATGM:
                effect = Gamemode.Config.EffectSpottedMarkerATGM;
                _defaultTimer = 240;
                _frequency = 1f;
                Type = Spotted.Emplacement;
                break;

            case Vehicles.VehicleType.AttackHeli:
                effect = Gamemode.Config.EffectSpottedMarkerAttackHeli;
                _defaultTimer = 15;
                _frequency = 0.5f;
                Type = Spotted.Aircraft;
                break;

            case Vehicles.VehicleType.HMG:
                effect = Gamemode.Config.EffectSpottedMarkerHMG;
                _defaultTimer = 240;
                _frequency = 1f;
                Type = Spotted.Emplacement;
                break;

            case Vehicles.VehicleType.Humvee:
                effect = Gamemode.Config.EffectSpottedMarkerHumvee;
                _defaultTimer = 30;
                _frequency = 0.5f;
                Type = Spotted.LightVehicle;
                break;

            case Vehicles.VehicleType.IFV:
                effect = Gamemode.Config.EffectSpottedMarkerIFV;
                _defaultTimer = 30;
                _frequency = 0.5f;
                Type = Spotted.Armor;
                break;

            case Vehicles.VehicleType.Jet:
                effect = Gamemode.Config.EffectSpottedMarkerJet;
                _defaultTimer = 10;
                _frequency = 0.5f;
                Type = Spotted.Aircraft;
                break;

            case Vehicles.VehicleType.MBT:
                effect = Gamemode.Config.EffectSpottedMarkerMBT;
                _defaultTimer = 30;
                _frequency = 0.5f;
                Type = Spotted.Armor;
                break;

            case Vehicles.VehicleType.Mortar:
                effect = Gamemode.Config.EffectSpottedMarkerMortar;
                _defaultTimer = 240;
                _frequency = 1f;
                Type = Spotted.Emplacement;
                break;

            case Vehicles.VehicleType.ScoutCar:
                effect = Gamemode.Config.EffectSpottedMarkerScoutCar;
                _defaultTimer = 30;
                _frequency = 0.5f;
                Type = Spotted.LightVehicle;
                break;

            case Vehicles.VehicleType.TransportAir:
                effect = Gamemode.Config.EffectSpottedMarkerTransportAir;
                _defaultTimer = 15;
                _frequency = 0.5f;
                Type = Spotted.Aircraft;
                break;

            case Vehicles.VehicleType.LogisticsGround:
                effect = Gamemode.Config.EffectSpottedMarkerLogisticsGround;
                _defaultTimer = 30;
                _frequency = 0.5f;
                Type = Spotted.LightVehicle;
                break;

            case Vehicles.VehicleType.TransportGround:
                effect = Gamemode.Config.EffectSpottedMarkerTransportGround;
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

        if (e.Player.Player.TryGetComponent(out SpottedComponent pcomp) && pcomp.IsActive)
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
    public static void MarkTarget(Transform transform, UCPlayer spotter, bool isUav = false)
    {
        if (!transform.gameObject.TryGetComponent(out SpottedComponent spotted))
            return;
        
        if (transform.TryGetComponent(out InteractableVehicle vehicle) && vehicle.lockedGroup.m_SteamID != spotter.GetTeam())
        {
            if (vehicle.transform.TryGetComponent(out VehicleComponent vc) && vc.Data?.Item != null)
                spotted.TryAnnounce(spotter, Localization.TranslateEnum(vc.Data.Item.Type, Localization.GetDefaultLanguage()));
            else
                spotted.TryAnnounce(spotter, vehicle.asset.vehicleName);

            L.LogDebug("Spotting vehicle " + vehicle.asset.vehicleName);
            spotted.Activate(spotter, isUav);
        }
        else if (transform.TryGetComponent(out Player player) && player.GetTeam() != spotter.GetTeam() && !Ghost.IsHidden(UCPlayer.FromPlayer(player)!))
        {
            spotted.TryAnnounce(spotter, T.SpottedTargetPlayer.Translate(Localization.GetDefaultLanguage()));
            L.LogDebug("Spotting player " + player.name);

            spotted.Activate(spotter, isUav);
        }
        else if (transform.TryGetComponent(out Cache cache) && cache.Team != spotter.GetTeam())
        {
            spotted.TryAnnounce(spotter, T.SpottedTargetCache.Translate(Localization.GetDefaultLanguage()));
            L.LogDebug("Spotting cache " + cache.Name);

            spotted.Activate(spotter, isUav);
        }
        else
        {
            BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(transform);
            if (drop == null || drop.GetServersideData().group == spotter.GetTeam())
                return;

            spotted.TryAnnounce(spotter, T.SpottedTargetFOB.Translate(Localization.GetDefaultLanguage()));
            L.LogDebug("Spotting barricade " + drop.asset.itemName);
            spotted.Activate(spotter, isUav);
        }
    }

    public void OnTargetKilled(int assistXP, int assistRep)
    {
        if (CurrentSpotter == null)
            return;

        Points.AwardXP(new XPParameters(CurrentSpotter.Steam64, _team, assistXP)
        {
            OverrideReputationAmount = assistRep,
            Multiplier = 1f,
            Message = PointsConfig.GetDefaultTranslation(CurrentSpotter.Locale.LanguageInfo, CurrentSpotter.Locale.CultureInfo, XPReward.KillAssist),
            Reward = XPReward.KillAssist
        });
    }

    public void Activate(UCPlayer spotter, bool isUav) => Activate(spotter, _defaultTimer, isUav);
    public void Activate(UCPlayer spotter, float seconds, bool isUav)
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
        _team = spotter.GetTeam();

        _coroutine = StartCoroutine(MarkerLoop());

        if (!isUav)
            spotter.ActivateMarker(this);
#if ENABLE_SPOTTED_BUFF
        if (Type is Spotted.Infantry or Spotted.LightVehicle or Spotted.Armor or Spotted.Aircraft or Spotted.Emplacement)
        {
            if (Type == Spotted.Infantry)
            {
                UCPlayer? target = UCPlayer.FromPlayer(GetComponent<Player>());
                if (target != null)
                    StartOrUpdateBuff(target, false);
            }
            else if (TryGetComponent(out InteractableVehicle vehicle) && vehicle.passengers.Length > 0)
            {
                for (int i = 0; i < vehicle.passengers.Length; ++i)
                {
                    UCPlayer? target = UCPlayer.FromSteamPlayer(vehicle.passengers[i].player);
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
            if (LastNonUAVSpotter != null && LastNonUAVSpotter.IsOnline && LastNonUAVSpotter.GetTeam() != OwnerTeam)
            {
                EndTime = ToBeUnspottedNonUAV;
                CurrentSpotter = LastNonUAVSpotter;
                UAVMode = false;
            }
            else Deactivate();
        }
    }
#if ENABLE_SPOTTED_BUFF
    private static void StartOrUpdateBuff(UCPlayer target, bool isVehicle)
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
    private static void RemoveBuff(UCPlayer target)
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
        if (CurrentSpotter != null && CurrentSpotter.IsOnline)
            CurrentSpotter.DeactivateMarker(this);

        if (_coroutine != null)
            StopCoroutine(_coroutine);

        _coroutine = null;
        CurrentSpotter = null;
#if ENABLE_SPOTTED_BUFF
        if (Type is Spotted.Infantry or Spotted.LightVehicle or Spotted.Armor or Spotted.Aircraft or Spotted.Emplacement)
        {
            if (Type == Spotted.Infantry)
            {
                UCPlayer? target = UCPlayer.FromPlayer(GetComponent<Player>());
                if (target != null)
                    RemoveBuff(target);
            }
            else if (TryGetComponent(out InteractableVehicle vehicle) && vehicle.passengers.Length > 0)
            {
                for (int i = 0; i < vehicle.passengers.Length; ++i)
                {
                    UCPlayer? target = UCPlayer.FromSteamPlayer(vehicle.passengers[i].player);
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
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer player = PlayerManager.OnlinePlayers[i];
            if (player.GetTeam() == _team && (player.Position - pos).sqrMagnitude < Math.Pow(650, 2))
            {
                if (Effect != null)
                    F.TriggerEffectReliable(Effect, player.Connection, pos);
            }
        }
    }
    private void TryAnnounce(UCPlayer spotter, string targetName)
    {
        if (IsActive)
            return;

        ToastMessage.QueueMessage(spotter, new ToastMessage(ToastMessageStyle.Mini, T.SpottedToast.Translate(spotter)));

        ulong t = spotter.GetTeam();
        Color t1 = Teams.TeamManager.GetTeamColor(t);
        targetName = targetName.Colorize(Teams.TeamManager.GetTeamHexColor(Teams.TeamManager.Other(t)));

        foreach (LanguageSet set in LanguageSet.OnTeam(t))
        {
            string t2 = T.SpottedMessage.Translate(set.Language, t1, targetName);
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
        if (!_statInit)
            return;
        
        EventDispatcher.EnterVehicle -= OnEnterVehicle;
        EventDispatcher.ExitVehicle -= OnExitVehicle;
        _statInit = false;
    }

    public override string ToString()
    {
        return $"Spotter ({GetInstanceID()}) for {Type}: {(IsActive ? "Spotted" : "Not Spotted")}, CurrentSpotter: {(CurrentSpotter == null ? "null" : CurrentSpotter.Name.PlayerName)}. Under UAV: {(UAVMode ? "Yes" : "No")}, Spotting team: {SpottingTeam}, Owner Team: {OwnerTeam}";
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
        public readonly UCPlayer Player;
        public bool IsVehicle;
        bool IBuff.IsBlinking => true;
        bool IBuff.Reserved => true;
        string IBuff.Icon => IsVehicle ? Gamemode.Config.UIIconVehicleSpotted : Gamemode.Config.UIIconSpotted;
        UCPlayer IBuff.Player => Player;

        public SpottedBuff(UCPlayer player)
        {
            Player = player;
        }
    }
#endif
}
