using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Traits;
using UnityEngine;

namespace Uncreated.Warfare.Squads.Commander;
public class UAV : MonoBehaviour, IBuff
{
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
    private ulong team;
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

        if (SquadManager.Singleton.Commanders.ActiveCommander != null)
        {

        }
    }
    public static UAV GiveUAV(UCPlayer requester, UCPlayer approver)
    {
        bool isMarker = requester.Player.quests.isMarkerPlaced;
        Vector3 pos = isMarker ? requester.Player.quests.markerPosition : requester.Player.transform.position;
        pos = pos with { y = Mathf.Min(Level.HEIGHT, F.GetHeight(pos, 0f) + 75f) };
        if (isMarker)
        {
            if (Gamemode.Config.GeneralConfig.UAVStartDelay > 0f)
                requester.SendChat(T.UAVDeployedTimeMarker, new GridLocation(pos), Gamemode.Config.GeneralConfig.UAVStartDelay);
            else
                requester.SendChat(T.UAVDeployedMarker, new GridLocation(pos));
        }
        else
        {
            if (Gamemode.Config.GeneralConfig.UAVStartDelay > 0f)
                requester.SendChat(T.UAVDeployedTimeSelf, Gamemode.Config.GeneralConfig.UAVStartDelay);
            else
                requester.SendChat(T.UAVDeployedSelf);
        }

        return SpawnUAV(requester, approver, pos, isMarker);
    }
    public static UAV SpawnUAV(UCPlayer requester, UCPlayer approver, Vector3 loc, bool isMarker)
    {
        if (!requester.IsOnline)
            throw new ArgumentException("Requester is not online.");
        GameObject obj = new GameObject(requester.Name.CharacterName + "'s UAV", new Type[] { typeof(UAV) });
        UAV uav = obj.GetComponent<UAV>();
        uav.Init(requester, approver, loc, isMarker);
        return uav;
    }
    private void Init(UCPlayer requester, UCPlayer approver, Vector3 loc, bool isMarker)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        this.requester = requester;
        this.approver = approver;
        startTime = Time.realtimeSinceStartup;
        EventDispatcher.OnPlayerDied += OnPlayerDied;
        
        deployPosition = isMarker ? requester.Player.quests.markerPosition : requester.Player.transform.position;
        deployPosition = deployPosition with { y = Mathf.Min(Level.HEIGHT, F.GetHeight(deployPosition, 0f) + 75f) };
        deployGl = new GridLocation(deployPosition);
        team = requester.GetTeam();

        scanSpeed = Gamemode.Config.GeneralConfig.UAVScanSpeed;
        stDelay = Gamemode.Config.GeneralConfig.UAVStartDelay;
        aliveTime = Gamemode.Config.GeneralConfig.UAVStartDelay + Gamemode.Config.GeneralConfig.UAVAliveTime;
        radius = Gamemode.Config.GeneralConfig.UAVRadius;

        this.inited = true;
    }

    [SuppressMessage(Data.SUPPRESS_CATEGORY, Data.SUPPRESS_ID)]
    private void OnDisable()
    {
        if (!inited) return;
        if (Time.realtimeSinceStartup - startTime < aliveTime)
        {
            if (requester.IsOnline)
                requester.SendChat(T.UAVDestroyedTimer);
        }
        if (buffAdded)
        {
            if (requester.IsOnline)
                TraitManager.BuffUI.RemoveBuff(requester, this);
        }
    }

    // speed gap parameters
    private const float BASE_START = 0.02f;
    private const float RAMP_MULTIPLIER = 0.000656f;
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
            active = true;
            isBlinking = aliveTime < Buff.BLINK_LEAD_TIME + stDelay;
            if (!isBlinking)
                TraitManager.BuffUI.UpdateBuffTimeState(this);
#if DEBUG
            CircleZone.CalculateParticleSpawnPoints(out Vector2[] pts, radius, new Vector2(deployPosition.x, deployPosition.z));
            EffectManager.sendEffectReliable(120, Level.size, deployPosition with { y = F.GetHeight(deployPosition, 0f) });
            for (int i = 0; i < pts.Length; ++i)
            {
                ref Vector2 pt = ref pts[i];
                EffectManager.sendEffectReliable(120, Level.size, new Vector3(pt.x, F.GetHeight(pt, 0f), pt.y));
            }
#endif
        }
        if (time - startTime > aliveTime)
        {
            Destroy(gameObject);
            return;
        }
        else if (!isBlinking && time - startTime > aliveTime - Buff.BLINK_LEAD_TIME)
        {
            isBlinking = true;
            TraitManager.BuffUI.UpdateBuffTimeState(this);
        }
        if (time - lastScan > scanSpeed && scanOutput.Count == 0)
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

                    c.OnUAVLeft();
                    next: ;
                }
            }
            else Scan();
            currentTotal = scanOutput.Count;
            currentDelay = scanSpeed / currentTotal;
            return;
        }
        else if (scanOutput.Count > 0 && time - lastDequeue > currentDelay)
        {
            lastDequeue = time;
            KeyValuePair<float, SpottedComponent> c = scanOutput[scanOutput.Count - 1];
            float dist = c.Key;
            currentDelay = dist <= 1f ? BASE_START : (RAMP_MULTIPLIER / radius * dist * dist + BASE_START);
            scanOutput.RemoveAt(scanOutput.Count - 1);
            SpottedComponent spot = c.Value;

            if (spot.UAVMode && spot.CurrentSpotter != null && spot.CurrentSpotter.Steam64 == requester.Steam64)
                spot.SendMarkers();
            else
                spot.Activate(requester, true);
        }
    }


    private void OnPlayerDied(PlayerDied e)
    {
        if (e.Steam64 != requester.Steam64)
            return;

        Destroy(gameObject);
        requester.SendChat(T.UAVDestroyedDeath);
    }
    private void Scan()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float rad = radius * radius;
        foreach (SpottedComponent spot in SpottedComponent.ActiveMarkers)
        {
            if (spot.Team != team && spot.isActiveAndEnabled && CanUAVSpot(spot.Type))
            {
                float dist = F.SqrDistance2D(deployPosition, spot.transform.position);
                if (dist < rad)
                    scanOutput.Add(new KeyValuePair<float, SpottedComponent>(dist, spot));
            }
        }

        scanOutput.Sort((a, b) => a.Key.CompareTo(b.Key));
    }
    private static bool CanUAVSpot(SpottedComponent.ESpotted spotType)
    {
        return spotType switch { _ => true };
    }
}
