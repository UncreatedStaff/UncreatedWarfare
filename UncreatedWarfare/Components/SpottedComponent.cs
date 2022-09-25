#define ENABLE_SPOTTED_BUFF
using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Players;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Vehicles;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Traits.Buffs;
using UnityEngine;

namespace Uncreated.Warfare.Components;

public class SpottedComponent : MonoBehaviour
{
    public Guid EffectGUID { get; private set; }
    public ESpotted Type { get; private set; }
    public ushort EffectID { get; private set; }
    // CAN BE OFFLINE
    public UCPlayer? CurrentSpotter { get; private set; }
    public ulong SpottingTeam => team;
    public ulong OwnerTeam { get => ownerTeam; set => ownerTeam = value; }
    public bool IsActive { get => _coroutine != null; }
    private float _frequency;
    private float _defaultTimer;
    private Coroutine? _coroutine;
    public static readonly Guid LaserDesignatorGUID = new Guid("3879d9014aca4a17b3ed749cf7a9283e");
    public float toBeUnspottedNonUAV = 0f;
    public float endTime = 0f;
    public bool UAVMode = false;
    public UCPlayer? LastNonUAVSpotter = null;
    private ulong team;
    private ulong ownerTeam;
    public Vector3 UAVLastKnown { get; internal set; }

    public static readonly HashSet<SpottedComponent> ActiveMarkers = new HashSet<SpottedComponent>();
    public static readonly List<SpottedComponent> AllMarkers = new List<SpottedComponent>(128);
#if ENABLE_SPOTTED_BUFF
    private static bool statInit = false;
#endif
    public void Initialize(ESpotted type, ulong ownerTeam)
    {
        this.ownerTeam = ownerTeam;
#if ENABLE_SPOTTED_BUFF
        if (!statInit)
        {
            EventDispatcher.OnEnterVehicle += OnEnterVehicle;
            EventDispatcher.OnExitVehicle += OnExitVehicle;
            statInit = true;
        }
#endif
        Type = type;
        CurrentSpotter = null;

        switch (type)
        {
            case ESpotted.INFANTRY:
                EffectGUID = new Guid("70f75e38a90e481190ba147f25bd6e24");
                _defaultTimer = 12;
                _frequency = 0.5f;
                break;
            case ESpotted.LIGHT_VEHICLE:
                EffectGUID = new Guid("34fea0ab821141bd935b001ee82a7049");
                _defaultTimer = 20;
                _frequency = 0.5f;
                break;
            case ESpotted.ARMOR:
                EffectGUID = new Guid("1a25daa6f506441282cd30be48d27883");
                _defaultTimer = 30;
                _frequency = 0.5f;
                break;
            case ESpotted.AIRCRAFT:
                EffectGUID = new Guid("0e90e68eff624456b76fee28a4875d14");
                _defaultTimer = 20;
                _frequency = 0.5f;
                break;
            case ESpotted.EMPLACEMENT:
                EffectGUID = new Guid("f7816f7d06e1475f8e68ed894c282a74");
                _defaultTimer = 90;
                _frequency = 0.5f;
                break;
            case ESpotted.FOB:
                EffectGUID = new Guid("de142d979e12442fb9d44baf8f520751");
                _defaultTimer = 90;
                _frequency = 0.5f;
                break;
        }

        if (Assets.find(EffectGUID) is EffectAsset effect)
        {
            EffectID = effect.id;
        }
        else
            L.LogWarning("SpottedComponent could not initialize: Effect asset not found: " + EffectGUID);

        AllMarkers.Add(this);
    }
#if ENABLE_SPOTTED_BUFF
    private static void OnExitVehicle(ExitVehicle e)
    {
        if (e.Vehicle.TryGetComponent(out SpottedComponent comp))
        {
            if (comp.IsActive)
            {
                if (e.Player.Player.TryGetComponent(out SpottedComponent pcomp) && pcomp.IsActive)
                    StartOrUpdateBuff(e.Player, false);
                else
                    RemoveBuff(e.Player);
            }
        }
    }

    private static void OnEnterVehicle(EnterVehicle e)
    {
        if (e.Vehicle.TryGetComponent(out SpottedComponent comp))
        {
            if (comp.IsActive)
                StartOrUpdateBuff(e.Player, true);
        }
    }
#endif
    public static void MarkTarget(Transform transform, UCPlayer spotter, bool isUav = false)
    {
        if (transform.gameObject.TryGetComponent(out SpottedComponent spotted))
        {
            if (transform.TryGetComponent(out InteractableVehicle vehicle) && vehicle.lockedGroup.m_SteamID != spotter.GetTeam())
            {
                if (vehicle.transform.TryGetComponent(out VehicleComponent vc))
                    spotted.TryAnnounce(spotter, Localization.TranslateEnum(vc.Data.Type, L.DEFAULT));
                else
                    spotted.TryAnnounce(spotter, vehicle.asset.vehicleName);

                L.LogDebug("Spotting vehicle " + vehicle.asset.vehicleName);
                spotted.Activate(spotter, isUav);
            }
            else if (transform.TryGetComponent(out Player player) && player.GetTeam() != spotter.GetTeam() && !Ghost.IsHidden(UCPlayer.FromPlayer(player)!))
            {
                spotted.TryAnnounce(spotter, T.SpottedTargetPlayer.Translate(L.DEFAULT));
                L.LogDebug("Spotting player " + player.name);

                spotted.Activate(spotter, isUav);
            }
            else if (transform.TryGetComponent(out Cache cache) && cache.Team != spotter.GetTeam())
            {
                spotted.TryAnnounce(spotter, T.SpottedTargetCache.Translate(L.DEFAULT));
                L.LogDebug("Spotting cache " + cache.Name);

                spotted.Activate(spotter, isUav);
            }
            else
            {
                BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(transform);
                if (drop != null && drop.GetServersideData().group != spotter.GetTeam())
                {
                    spotted.TryAnnounce(spotter, T.SpottedTargetFOB.Translate(L.DEFAULT));
                    L.LogDebug("Spotting barricade " + drop.asset.itemName);
                    spotted.Activate(spotter, isUav);
                }
            }
        }
    }

    public void OnTargetKilled(int assistXP)
    {
        if (CurrentSpotter != null && CurrentSpotter.IsOnline)
        {
            Points.AwardXP(CurrentSpotter, assistXP, T.XPToastSpotterAssist);
        }
    }

    private void OnDestroy()
    {
        Deactivate();
        AllMarkers.Remove(this);
    }
    public void Activate(UCPlayer spotter, bool isUav) => Activate(spotter, _defaultTimer, isUav);
    public void Activate(UCPlayer spotter, float seconds, bool isUav)
    {
        endTime = Time.realtimeSinceStartup + seconds;
        if (!isUav)
            toBeUnspottedNonUAV = endTime;
        UAVMode = isUav;
        if (_coroutine != null)
            StopCoroutine(_coroutine);

        CurrentSpotter = spotter;
        team = spotter.GetTeam();

        _coroutine = StartCoroutine(MarkerLoop());
        
        if (!isUav)
            spotter.ActivateMarker(this);
#if ENABLE_SPOTTED_BUFF
        if (Type is ESpotted.INFANTRY or ESpotted.LIGHT_VEHICLE or ESpotted.ARMOR or ESpotted.AIRCRAFT or ESpotted.EMPLACEMENT)
        {
            if (Type == ESpotted.INFANTRY)
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
                endTime = toBeUnspottedNonUAV;
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
        if (Type is ESpotted.INFANTRY or ESpotted.LIGHT_VEHICLE or ESpotted.ARMOR or ESpotted.AIRCRAFT or ESpotted.EMPLACEMENT)
        {
            if (Type == ESpotted.INFANTRY)
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
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer player = PlayerManager.OnlinePlayers[i];
            Vector3 pos = UAVMode ? UAVLastKnown : transform.position;
            if (player.GetTeam() == team && (player.Position - pos).sqrMagnitude < Math.Pow(650, 2))
                EffectManager.sendEffect(EffectID, player.Connection, pos);
        }
    }
    private void TryAnnounce(UCPlayer spotter, string targetName)
    {
        if (IsActive)
            return;

        ToastMessage.QueueMessage(spotter, new ToastMessage(T.SpottedToast.Translate(spotter), EToastMessageSeverity.MINI), true);

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

        while (UAVMode || Time.realtimeSinceStartup < endTime)
        {
            SendMarkers();

            yield return new WaitForSeconds(_frequency);
        }

        Deactivate();
    }
    public override string ToString()
    {
        return $"Spotter ({GetInstanceID()}) for {Type}: {(IsActive ? "Spotted" : "Not Spotted")}, CurrentSpotter: {(CurrentSpotter == null ? "null" : CurrentSpotter.Name.PlayerName)}. Under UAV: {(UAVMode ? "Yes" : "No")}, Spotting team: {SpottingTeam}, Owner Team: {OwnerTeam}";
    }
    public enum ESpotted
    {
        INFANTRY,
        LIGHT_VEHICLE,
        ARMOR,
        AIRCRAFT,
        EMPLACEMENT,
        FOB
    }

    private sealed class SpottedBuff : IBuff
    {
        public readonly UCPlayer Player;
        public bool IsVehicle = false;
        bool IBuff.IsBlinking => true;
        bool IBuff.Reserved => true;
        string IBuff.Icon => IsVehicle ? Gamemode.Config.UIIconVehicleSpotted : Gamemode.Config.UIIconSpotted;
        UCPlayer IBuff.Player => Player;

        public SpottedBuff(UCPlayer player)
        {
            Player = player;
        }
    }
}
