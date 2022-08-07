using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Players;
using Uncreated.Warfare.Point;
using UnityEngine;

namespace Uncreated.Warfare.Components;

public class SpottedComponent : MonoBehaviour
{
    public Guid EffectGUID { get; private set; }
    public ESpotted Type { get; private set; }
    public ushort EffectID { get; private set; }
    public UCPlayer? CurrentSpotter { get; private set; }
    public bool IsActive { get => _coroutine != null; }
    private float _frequency;
    private int _defaultTimer;
    private Coroutine? _coroutine;
    public static readonly Guid LaserDesignatorGUID = new Guid("3879d9014aca4a17b3ed749cf7a9283e");

    public static readonly HashSet<SpottedComponent> ActiveMarkers = new HashSet<SpottedComponent>();


    public void Initialize(ESpotted type)
    {
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
    }
    public static void MarkTarget(Transform transform, UCPlayer spotter)
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
                spotted.Activate(spotter);
            }
            else if (transform.TryGetComponent(out Player player) && player.GetTeam() != spotter.GetTeam())
            {
                spotted.TryAnnounce(spotter, T.SpottedTargetPlayer.Translate(L.DEFAULT));
                L.LogDebug("Spotting player " + player.name);

                spotted.Activate(spotter);
            }
            else if (transform.TryGetComponent(out Cache cache) && cache.Team != spotter.GetTeam())
            {
                spotted.TryAnnounce(spotter, T.SpottedTargetCache.Translate(L.DEFAULT));
                L.LogDebug("Spotting cache " + cache.Name);

                spotted.Activate(spotter);
            }
            else
            {
                BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(transform);
                if (drop != null && drop.GetServersideData().group != spotter.GetTeam())
                {
                    spotted.TryAnnounce(spotter, T.SpottedTargetFOB.Translate(L.DEFAULT));
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
            Points.AwardXP(CurrentSpotter, assistXP, T.XPToastSpotterAssist);
        }
    }

    private void OnDestroy()
    {
        Deactivate();
    }
    public void Activate(UCPlayer spotter) => Activate(spotter, _defaultTimer);
    public void Activate(UCPlayer spotter, int seconds)
    {
        if (_coroutine != null)
            StopCoroutine(_coroutine);

        CurrentSpotter = spotter;

        _coroutine = StartCoroutine(MarkerLoop(seconds));
        
        spotter.ActivateMarker(this);

        
    }
    public void Deactivate()
    {
        if (CurrentSpotter != null)
            CurrentSpotter.DeactivateMarker(this);

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
        LIGHT_VEHICLE,
        ARMOR,
        AIRCRAFT,
        EMPLACEMENT,
        FOB
    }
}
