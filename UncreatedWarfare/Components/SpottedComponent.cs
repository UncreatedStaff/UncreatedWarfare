using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Players;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using static Uncreated.Framework.Report;

namespace Uncreated.Warfare.Components;

public class SpottedComponent : MonoBehaviour
{
    public Guid EffectGUID { get; private set; }
    public ESpotted? Type { get; private set; }
    public EVehicleType? VehicleType { get; private set; }
    public ushort EffectID { get; private set; }
    public Player? CurrentSpotter { get; private set; }
    public bool IsActive { get => _coroutine != null; }
    public bool IsLaserTarget { get; private set; }
    private float _frequency;
    private int _defaultTimer;
    private Coroutine? _coroutine;
    public static readonly Guid LaserDesignatorGUID = new Guid("3879d9014aca4a17b3ed749cf7a9283e");

    public static readonly HashSet<SpottedComponent> ActiveMarkers = new HashSet<SpottedComponent>();

    public void Initialize(ESpotted type)
    {
        Type = type;
        CurrentSpotter = null;
        IsLaserTarget = type == ESpotted.FOB;

        switch (type)
        {
            case ESpotted.INFANTRY:
                EffectGUID = new Guid("79add0f1b07c478f87207d30fe5a5f4f");
                _defaultTimer = 12;
                _frequency = 0.5f;
                break;
            case ESpotted.FOB:
                EffectGUID = new Guid("39dce42142074b46b819feba9ce83353");
                _defaultTimer = 240;
                _frequency = 1f;
                break;
        }

        if (Assets.find(EffectGUID) is EffectAsset effect)
        {
            EffectID = effect.id;
        }
        else
            L.LogWarning("SpottedComponent could not initialize: Effect asset not found: " + EffectGUID);
    }
    public void Initialize(EVehicleType type)
    {
        CurrentSpotter = null;
        IsLaserTarget = VehicleData.IsGroundVehicle(type);

        switch (type)
        {
            case EVehicleType.AA:
                EffectGUID = new Guid("0e90e68eff624456b76fee28a4875d14");
                _defaultTimer = 240;
                _frequency = 1f;
                break;
            case EVehicleType.APC:
                EffectGUID = new Guid("31d1404b7b3a465b8631308cdb48e3b2");
                _defaultTimer = 30;
                _frequency = 0.5f;
                break;
            case EVehicleType.ATGM:
                EffectGUID = new Guid("b20a7d914f92492fb1588f7baac80239");
                _defaultTimer = 240;
                _frequency = 1f;
                break;
            case EVehicleType.HELI_ATTACK:
                EffectGUID = new Guid("3f2c6776ba484f8ea443719161ec6ce5");
                _defaultTimer = 15;
                _frequency = 0.5f;
                break;
            case EVehicleType.HMG:
                EffectGUID = new Guid("2315e6ed970542499fec1b06df87ffd2");
                _defaultTimer = 240;
                _frequency = 1f;
                break;
            case EVehicleType.HUMVEE:
                EffectGUID = new Guid("99a84b82f9bd433891fdb99e80394bf3");
                _defaultTimer = 30;
                _frequency = 0.5f;
                break;
            case EVehicleType.IFV:
                EffectGUID = new Guid("f2c29856b4f64146afd9872ab528c242");
                _defaultTimer = 30;
                _frequency = 0.5f;
                break;
            case EVehicleType.JET:
                EffectGUID = new Guid("08f2cc6ed558459ea2caf3477b40df64");
                _defaultTimer = 10;
                _frequency = 0.5f;
                break;
            case EVehicleType.MBT:
                EffectGUID = new Guid("983c6510c13042bf983e81f49cffca39");
                _defaultTimer = 30;
                _frequency = 0.5f;
                break;
            case EVehicleType.MORTAR:
                EffectGUID = new Guid("c377810f849c4c7d84391b491406918b");
                _defaultTimer = 240;
                _frequency = 1f;
                break;
            case EVehicleType.SCOUT_CAR:
                EffectGUID = new Guid("b0937aff90b94a588b70bc96ece49f53");
                _defaultTimer = 30;
                _frequency = 0.5f;
                break;
            case EVehicleType.HELI_TRANSPORT:
                EffectGUID = new Guid("91b9f175b84849268d861eb0f0567788");
                _defaultTimer = 15;
                _frequency = 0.5f;
                break;
            case EVehicleType.LOGISTICS:
                EffectGUID = new Guid("fa226268e87b4ec89664eca5b22b4d3d");
                _defaultTimer = 30;
                _frequency = 0.5f;
                break;
            case EVehicleType.TRANSPORT:
                EffectGUID = new Guid("fa226268e87b4ec89664eca5b22b4d3d");
                _defaultTimer = 30;
                _frequency = 0.5f;
                break;

        }

        if (Assets.find(EffectGUID) is EffectAsset effect)
        {
            EffectID = effect.id;
        }
        else
            L.LogWarning("SpottedComponent could not initialize: Effect asset not found: " + EffectGUID);
    }
    public static void MarkTarget(Transform transform, Player spotter)
    {
        if (transform.TryGetComponent(out InteractableVehicle vehicle) && vehicle.lockedGroup.m_SteamID != spotter.GetTeam())
        {
            if (vehicle.transform.gameObject.TryGetComponent(out SpottedComponent spotted))
            {
                if (vehicle.transform.TryGetComponent(out VehicleComponent vc))
                {
                    spotted.TryAnnounce(spotter, Localization.Translate(vc.Data.Type.ToString(), JSONMethods.DEFAULT_LANGUAGE).ToUpper().Colorize("f2a172"));
                }
                L.LogDebug("Spotting vehicle " + vehicle.asset.vehicleName);
                spotted.Activate(spotter);
            }
        }
        else if (transform.TryGetComponent(out Player player) && player.GetTeam() != spotter.GetTeam())
        {
            if (player.transform.gameObject.TryGetComponent(out SpottedComponent spotted))
            {
                spotted.TryAnnounce(spotter, "contact");
                L.LogDebug("Spotting player " + player.name);

                spotted.Activate(spotter);
            }
        }
        else
        {
            var drop = BarricadeManager.FindBarricadeByRootTransform(transform);
            if (drop != null && drop.GetServersideData().group != spotter.GetTeam())
            {
                if (drop.model.gameObject.gameObject.TryGetComponent(out SpottedComponent spotted))
                {
                    spotted.TryAnnounce(spotter, "FOB".Colorize("ff7e5e"));
                    L.LogDebug("Spotting barricade " + drop.asset.itemName);
                    spotted.Activate(spotter);
                }
            }
        }
    }

    public void OnTargetKilled(int assistXP)
    {
        if (CurrentSpotter != null)
        {
            Points.AwardXP(CurrentSpotter, assistXP, Localization.Translate("xp_spotted_assist", CurrentSpotter));
        }
    }

    private void OnDestroy()
    {
        Deactivate();
    }
    public void Activate(Player spotter) => Activate(spotter, _defaultTimer);
    public void Activate(Player spotter, int seconds)
    {
        if (_coroutine != null)
            StopCoroutine(_coroutine);

        CurrentSpotter = spotter;

        _coroutine = StartCoroutine(MarkerLoop(seconds));
        
        UCPlayer.FromPlayer(spotter)!.ActivateMarker(this);

        
    }
    public void Deactivate()
    {
        if (CurrentSpotter != null)
            UCPlayer.FromPlayer(CurrentSpotter)!.DeactivateMarker(this);

        if (_coroutine != null)
            StopCoroutine(_coroutine);

        _coroutine = null;
        CurrentSpotter = null;

        ActiveMarkers.Remove(this);
    }
    private void SendMarkers()
    {
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            var player = PlayerManager.OnlinePlayers[i];

            if (player.GetTeam() == CurrentSpotter!.GetTeam() && (player.Position - transform.position).sqrMagnitude < Math.Pow(650, 2))
                EffectManager.sendEffect(EffectID, player.Connection, transform.position);
        }
    }
    private void TryAnnounce(Player spotter, string targetName)
    {
        if (IsActive)
            return;

        ToastMessage.QueueMessage(spotter, new ToastMessage(Localization.Translate("spotted", spotter), EToastMessageSeverity.MINI), true);

        foreach (var player in PlayerManager.OnlinePlayers)
        {
            if (player.GetTeam() == spotter.GetTeam())
                ChatManager.serverSendMessage($"[T] <color=#{Teams.TeamManager.GetTeamHexColor(spotter.GetTeam())}>%SPEAKER%</color>: Enemy {targetName} spotted!",
            Color.white, spotter.channel.owner, player, EChatMode.SAY, null, true);
        }
    }
    private IEnumerator<WaitForSeconds> MarkerLoop(int seconds)
    {
        ActiveMarkers.Add(this);

        int counter = 0;

        while (counter < seconds * (1 / _frequency))
        {
            SendMarkers();

            counter++;
            yield return new WaitForSeconds(_frequency);
        }

        Deactivate();
    }
    public enum ESpotted
    {
        INFANTRY,
        FOB
    }
}
