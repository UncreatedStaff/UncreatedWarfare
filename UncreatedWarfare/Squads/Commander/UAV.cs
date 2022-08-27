using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Traits;
using UnityEngine;

namespace Uncreated.Warfare.Squads.Commander;
public class UAV : MonoBehaviour, IBuff
{
    public const float GROUND_HEIGHT_OFFSET = 75f;
    private bool inited = false;
    private UCPlayer requester;
    private UCPlayer approver;
    private float startTime;
    private Vector3 deployPosition;
    private GridLocation deployGl;
    private bool isMarker;
    private float stDelay;
    private float aliveTime;
    private float scanSpeed;
    private readonly List<KeyValuePair<float, SpottedComponent>> scanOutput = new List<KeyValuePair<float, SpottedComponent>>(32);
    private float lastScan;
    private float lastDequeue;
    private float currentDelay = 0.05f;
    private float radius;
    private int currentTotal = 0;
    private bool isBlinking = true;
    private bool active;
    private bool buffAdded;
    private int activeIndex;
    private ulong team;
    private static bool isRequestActiveT1;
    private static bool isRequestActiveT2;
    private static UAV? team1UAV;
    private static UAV? team2UAV;
    private BarricadeDrop? drop;
    private bool didDropExist;
    private Transform? modelAnimTransform;
#if DEBUG
    private float lastPing;
#endif
    public bool IsMarker => isMarker;
    public Vector3 Position => deployPosition;
    public GridLocation GridLocation => deployGl;

    /// <summary>Possible for <see cref="Requester"/> to be offline. Check <see cref="UCPlayer.IsOnline"/> before using some members.</summary>
    public UCPlayer Requester => requester;
    /// <summary>Possible for <see cref="Approver"/> to be offline. Check <see cref="UCPlayer.IsOnline"/> before using some members.</summary>
    public UCPlayer Approver => approver;
    bool IBuff.IsBlinking => isBlinking;
    string IBuff.Icon => Gamemode.Config.UI.UAVIcon;
    UCPlayer IBuff.Player => requester;
    bool IBuff.Reserved => true;
    public static void RequestUAV(UCPlayer requester)
    {
        ulong team = requester.GetTeam();
        if ((team == 1 && isRequestActiveT1) || (team == 2 && isRequestActiveT2))
        {
            requester.SendChat(T.RequestAlreadyActive);
            return;
        }
        if (!KitManager.Loaded || !SquadManager.Loaded)
        {
            requester.SendChat(T.GamemodeError);
            return;
        }
        if (!KitManager.HasKit(requester, out Kit kit))
        {
            requester.SendChat(T.RequestUAVNoKit);
            return;
        }

        if (kit.Class != EClass.SQUADLEADER || !requester.IsSquadLeader())
            requester.SendChat(T.RequestUAVNotSquadleader);
        UCPlayer? activeCommander = SquadManager.Singleton.Commanders.GetCommander(team);
        if (activeCommander != null)
        {
            bool isMarker = requester.Player.quests.isMarkerPlaced;
            Vector3 pos = isMarker ? requester.Player.quests.markerPosition : requester.Player.transform.position;
            pos = pos with { y = Mathf.Min(Level.HEIGHT, F.GetHeight(pos, 0f) + GROUND_HEIGHT_OFFSET) };
            if (activeCommander.Steam64 != requester.Steam64)
            {
                if (team == 1) isRequestActiveT1 = true;
                else if (team == 2) isRequestActiveT2 = true;
                requester.SendChat(T.RequestUAVSent, activeCommander);
                activeCommander.SendChat(T.RequestUAVTell, requester, requester.Squad!, new GridLocation(pos));
                Tips.TryGiveTip(activeCommander, ETip.UAV_REQUEST, requester);
                UCWarfare.I.StartCoroutine(RequestUAVCoroutine(team, requester, activeCommander, isMarker, pos));
            }
            else
            {
                GiveUAV(team, requester, activeCommander, isMarker, pos);
            }
        }
        else
            requester.SendChat(T.RequestUAVNoActiveCommander);
    }
    private static IEnumerator RequestUAVCoroutine(ulong team, UCPlayer requester, UCPlayer activeCommander, bool isMarker, Vector3 pos)
    {
        CommandWaiter confirmTask = new CommandWaiter(activeCommander, typeof(ConfirmCommand), 15000);
        CommandWaiter denyTask = new CommandWaiter(activeCommander, typeof(DenyCommand), 15000);
        while (confirmTask.keepWaiting && denyTask.keepWaiting)
        {
            yield return null;
            if (!requester.IsOnline)
            {
                activeCommander.SendChat(T.RequestUAVRequesterLeft, requester);
                goto cancel;
            }
            if (!activeCommander.IsOnline)
            {
                requester.SendChat(T.RequestUAVCommanderLeft, activeCommander);
                goto cancel;
            }
            ulong rt = requester.GetTeam();
            ulong ct = activeCommander.GetTeam();
            if (rt != team)
            {
                activeCommander.SendChat(T.RequestUAVRequesterChangedTeams, requester);
                goto cancel;
            }
            if (ct != team)
            {
                requester.SendChat(T.RequestUAVCommanderChangedTeams, activeCommander);
                goto cancel;
            }
            if (!requester.IsSquadLeader() || requester.KitClass != EClass.SQUADLEADER)
            {
                activeCommander.SendChat(T.RequestUAVRequesterNotSquadLeader, requester);
                goto cancel;
            }
            if (SquadManager.Singleton.Commanders.GetCommander(ct) == activeCommander)
            {
                requester.SendChat(T.RequestUAVCommanderNoLongerCommander, activeCommander);
                goto cancel;
            }
        }
        if (!confirmTask.Responded)
        {
            requester.SendChat(T.RequestUAVDenied, activeCommander);
            goto cancel;
        }

        GiveUAV(team, requester, activeCommander, isMarker, pos);
    cancel:
        confirmTask.Cancel();
        denyTask.Cancel();
        if (team == 1)
            isRequestActiveT1 = false;
        else if (team == 2)
            isRequestActiveT2 = false;
        yield break;
    }
    public static UAV GiveUAV(ulong team, UCPlayer requester, UCPlayer approver, bool isMarker, Vector3 pos)
    {
        if (isMarker)
        {
            GridLocation loc = new GridLocation(pos);
            if (Gamemode.Config.GeneralConfig.UAVStartDelay > 0f)
            {
                if (approver.Steam64 != requester.Steam64 && approver.IsOnline)
                    approver.SendChat(T.UAVDeployedTimeMarkerCommander, loc, Gamemode.Config.GeneralConfig.UAVStartDelay, requester);
                if (requester.IsOnline)
                    requester.SendChat(T.UAVDeployedTimeMarker, loc, Gamemode.Config.GeneralConfig.UAVStartDelay);
            }
            else
            {
                if (approver.Steam64 != requester.Steam64 && approver.IsOnline)
                    approver.SendChat(T.UAVDeployedMarkerCommander, loc, requester);
                if (requester.IsOnline)
                    requester.SendChat(T.UAVDeployedMarker, loc);
            }
        }
        else
        {
            if (Gamemode.Config.GeneralConfig.UAVStartDelay > 0f)
            {
                if (approver.Steam64 != requester.Steam64 && approver.IsOnline)
                    approver.SendChat(T.UAVDeployedTimeSelfCommander, Gamemode.Config.GeneralConfig.UAVStartDelay, new GridLocation(pos), requester);
                if (requester.IsOnline)
                    requester.SendChat(T.UAVDeployedTimeSelf, Gamemode.Config.GeneralConfig.UAVStartDelay);
            }
            else
            {
                if (approver.Steam64 != requester.Steam64 && approver.IsOnline)
                    approver.SendChat(T.UAVDeployedSelfCommander, new GridLocation(pos), requester);
                if (requester.IsOnline)
                    requester.SendChat(T.UAVDeployedSelf);
            }
        }

        return SpawnUAV(team, requester, approver, pos, isMarker);
    }
    public static UAV SpawnUAV(ulong team, UCPlayer requester, UCPlayer approver, Vector3 loc, bool isMarker)
    {
        GameObject obj = new GameObject(requester.Steam64 + "'s UAV", new Type[] { typeof(UAV) });
        obj.transform.position = loc;
        UAV uav = obj.GetComponent<UAV>();
        uav.Init(team, requester, approver, loc, isMarker);
        return uav;
    }
    private void Init(ulong team, UCPlayer requester, UCPlayer approver, Vector3 loc, bool isMarker)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        this.requester = requester;
        this.approver = approver;
        startTime = Time.realtimeSinceStartup;
        this.isMarker = isMarker;
        deployPosition = loc;
        deployGl = new GridLocation(deployPosition);
        this.team = team;
        if (team == 1)
        {
            if (team1UAV != null)
                Destroy(team1UAV);
            team1UAV = this;
        }
        else if (team == 2)
        {
            if (team2UAV != null)
                Destroy(team2UAV);
            team2UAV = this;
        }

        scanSpeed = Gamemode.Config.GeneralConfig.UAVScanSpeed;
        stDelay = Gamemode.Config.GeneralConfig.UAVStartDelay;
        aliveTime = Gamemode.Config.GeneralConfig.UAVStartDelay + Gamemode.Config.GeneralConfig.UAVAliveTime;
        radius = Gamemode.Config.GeneralConfig.UAVRadius;

        this.inited = true;
    }

    [SuppressMessage(Data.SUPPRESS_CATEGORY, Data.SUPPRESS_ID)]
    private void OnDestroy()
    {
        if (!inited) return;
        if (Time.realtimeSinceStartup - startTime >= aliveTime)
        {
            if (requester.IsOnline)
                requester.SendChat(T.UAVDestroyedTimer);
        }
        if (buffAdded)
        {
            if (requester.IsOnline)
                TraitManager.BuffUI.RemoveBuff(requester, this);
        }
        if (drop != null && drop.model != null && Regions.tryGetCoordinate(drop.model.position, out byte x, out byte y))
            BarricadeManager.destroyBarricade(drop, x, y, ushort.MaxValue);
        if (team == 1)
        {
            if (team1UAV == this)
                team1UAV = null;
        }
        else if (team == 2)
        {
            if (team2UAV == this)
                team2UAV = null;
        }
        for (int j = 0; j < scanOutput.Count; ++j)
        {
            SpottedComponent c = scanOutput[j].Value;
            L.LogDebug("Spotter reset: " + c);
            c.OnUAVLeft();
        }
    }

    // speed gap parameters
    private const float BASE_START = 0.2f;
    private const float RAMP_MULTIPLIER = 0.004656f;

    [SuppressMessage(Data.SUPPRESS_CATEGORY, Data.SUPPRESS_ID)]
    private void Update()
    {
        if (!this.inited) return;
        float time = Time.realtimeSinceStartup;
        if (!buffAdded)
        {
            TraitManager.BuffUI.AddBuff(requester, this);
            buffAdded = true;
        }
        if (time - startTime < stDelay)
            return;
        else if (!active)
        {
            Activate();
        }
        if (time - startTime > aliveTime || (didDropExist && drop?.model == null))
        {
            Destroy(gameObject);
            return;
        }
        else if (!isBlinking && time - startTime > aliveTime - Buff.BLINK_LEAD_TIME)
        {
            isBlinking = true;
            TraitManager.BuffUI.UpdateBuffTimeState(this);
        }
        if (time - lastScan > scanSpeed && activeIndex <= 0)
        {
            lastScan = time;
            // diffing scan
            if (scanOutput.Count > 0)
            {
                KeyValuePair<float, SpottedComponent>[] sp2 = scanOutput.ToArray();
                scanOutput.Clear();
                Scan();
                for (int j = 0; j < sp2.Length; ++j)
                {
                    SpottedComponent c = sp2[j].Value;
                    for (int i = 0; i < scanOutput.Count; ++i)
                    {
                        if (ReferenceEquals(c, scanOutput[i].Value))
                            goto next;
                    }
                    L.LogDebug("Spotter left: " + c);
                    c.OnUAVLeft();
                    next: ;
                }
            }
            else Scan();
            currentTotal = scanOutput.Count;
            currentDelay = scanSpeed / currentTotal;
            activeIndex = scanOutput.Count;
            return;
        }
        else if (activeIndex > 0 && time - lastDequeue > currentDelay)
        {
            lastDequeue = time;
            lastScan = time;
            KeyValuePair<float, SpottedComponent> c = scanOutput[--activeIndex];
            float dist = c.Key;
            currentDelay = dist <= 1f ? BASE_START : (RAMP_MULTIPLIER / radius * dist /* dist is already squared */ + BASE_START);
            SpottedComponent spot = c.Value;

            if (spot.UAVMode && spot.CurrentSpotter != null)
            {
                if (spot.CurrentSpotter.Steam64 == requester.Steam64)
                {
                    spot.UAVLastKnown = spot.transform.position;
                    L.LogDebug("Updating spotter: " + spot);
                }
                else
                {
                    L.LogDebug("Spotter: " + spot + ": is already under the control of another UAV.");
                }
            }
            else
            {
                spot.Activate(requester, true);
                L.LogDebug("Activated spotter: " + spot);
            }
        }
#if DEBUG
        if (time - lastPing > 1.5f)
        {
            lastPing = time;
            if (modelAnimTransform != null)
            {
                EffectManager.ClearEffectByID_AllPlayers(36112);
                EffectManager.sendEffect(36112, Level.size * 2, modelAnimTransform.position);
            }
        }
#endif
    }
    private void Activate()
    {
        active = true;
        isBlinking = aliveTime < Buff.BLINK_LEAD_TIME + stDelay;
        if (!isBlinking)
            TraitManager.BuffUI.UpdateBuffTimeState(this);
#if DEBUG
        CircleZone.CalculateParticleSpawnPoints(out Vector2[] pts, radius, new Vector2(deployPosition.x, deployPosition.z));
        EffectManager.sendEffectReliable(120, Level.size, deployPosition);
        for (int i = 0; i < pts.Length; ++i)
        {
            ref Vector2 pt = ref pts[i];
            EffectManager.sendEffectReliable(120, Level.size, new Vector3(pt.x, F.GetHeight(pt, 0f), pt.y));
        }
#endif
        if (drop == null && Gamemode.Config.Barricades.UAV.ValidReference(out ItemBarricadeAsset asset))
        {
            Transform tr = BarricadeManager.dropNonPlantedBarricade(new Barricade(asset, asset.health, asset.getState()),
                deployPosition, Quaternion.Euler(new Vector3(-90f, 0f, 0f)), requester.Steam64,
                TeamManager.GetGroupID(team));
            drop = BarricadeManager.FindBarricadeByRootTransform(tr);
            if (drop != null)
            {
                didDropExist = true;
                modelAnimTransform = drop.model.Find("UAV");
            }
        }
    }
    private void Scan()
    {
#if DEBUG
        IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float rad = radius * radius;
        foreach (SpottedComponent spot in SpottedComponent.AllMarkers)
        {
            if (spot.OwnerTeam != team && spot.isActiveAndEnabled && CanUAVSpot(spot.Type))
            {
                float dist = F.SqrDistance2D(deployPosition, spot.transform.position);
                if (dist < rad)
                    scanOutput.Add(new KeyValuePair<float, SpottedComponent>(dist, spot));
            }
        }

        scanOutput.Sort((a, b) => a.Key.CompareTo(b.Key));
#if DEBUG
        profiler.Dispose();
        L.LogDebug(Time.realtimeSinceStartup.ToString("0.#") + " Scan output: ");
        using IDisposable d = L.IndentLog(1);
        for (int i = 0; i < scanOutput.Count; ++i)
        {
            L.LogDebug(Mathf.Sqrt(scanOutput[i].Key).ToString("0.#") + "m: " + scanOutput[i].Value.ToString());
        }
#endif
    }
    private static bool CanUAVSpot(SpottedComponent.ESpotted spotType)
    {
        return spotType switch { _ => true };
    }
}
