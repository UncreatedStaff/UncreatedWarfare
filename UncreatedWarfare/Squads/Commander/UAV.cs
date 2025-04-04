#if DEBUG
#endif
namespace Uncreated.Warfare.Squads.Commander;
public class UAV : MonoBehaviour//, IBuff
{
#if false
    public const float GroundHeightOffset = 150f;
    private bool _inited;
    private UCPlayer _requester;
    private UCPlayer _approver;
    private float _startTime;
    private Vector3 _deployPosition;
    private GridLocation _deployGl;
    private bool _isMarker;
    private float _stDelay;
    private float _aliveTime;
    private float _scanSpeed;
    private readonly List<KeyValuePair<float, SpottedComponent>> _scanOutput = new List<KeyValuePair<float, SpottedComponent>>(32);
    private float _lastScan;
    private float _radius;
    private bool _isBlinking = true;
    private bool _active;
    private bool _buffAdded;
    private int _activeIndex;
    private ulong _team;
    private static bool _isRequestActiveT1;
    private static bool _isRequestActiveT2;
    private static UAV? _team1UAV;
    private static UAV? _team2UAV;
    private BarricadeDrop? _drop;
    private bool _didDropExist;
    private Transform? _modelAnimTransform;
    private float _reachDistSqr;
    private float _nextReachDistSqr;
#if DEBUG
    private float _lastPing;
#endif
    public bool IsMarker => _isMarker;
    public Vector3 Position => _deployPosition;
    public GridLocation GridLocation => _deployGl;

    /// <summary>Possible for <see cref="Requester"/> to be offline. Check <see cref="UCPlayer.IsOnline"/> before using some members.</summary>
    public UCPlayer Requester => _requester;
    /// <summary>Possible for <see cref="Approver"/> to be offline. Check <see cref="UCPlayer.IsOnline"/> before using some members.</summary>
    public UCPlayer Approver => _approver;
    bool IBuff.IsBlinking => _isBlinking;
    string IBuff.Icon => Gamemode.Config.UIIconUAV;
    UCPlayer IBuff.Player => _requester;
    bool IBuff.Reserved => true;
    public static async Task RequestUAV(UCPlayer requester, CancellationToken token = default)
    {
        ulong team = requester.GetTeam();
        if ((team == 1 && _isRequestActiveT1) || (team == 2 && _isRequestActiveT2) || (team == 1 && _team1UAV != null) || (team == 2 && _team1UAV != null))
        {
            requester.SendChat(T.RequestAlreadyActive);
            return;
        }
        KitManager? manager = KitManager.GetSingletonQuick();
        if (manager == null || !SquadManager.Loaded)
        {
            requester.SendChat(T.GamemodeError);
            return;
        }

        Kit? reqKit = await requester.GetActiveKit(token).ConfigureAwait(false);
        if (reqKit == null)
        {
            requester.SendChat(T.RequestUAVNoKit);
            return;
        }

        await UniTask.SwitchToMainThread(token);

        if (reqKit.Class != Class.Squadleader || !requester.IsSquadLeader())
            requester.SendChat(T.RequestUAVNotSquadleader);
        UCPlayer? activeCommander = SquadManager.Singleton.Commanders.GetCommander(team);
        if (activeCommander != null)
        {
            bool isMarker = requester.Player.quests.isMarkerPlaced;
            Vector3 pos = isMarker ? requester.Player.quests.markerPosition : requester.Player.transform.position;
            pos = pos with { y = Mathf.Min(Level.HEIGHT, F.GetHeight(pos, 0f) + GroundHeightOffset) };
            if (activeCommander.Steam64 != requester.Steam64)
            {
                if (team == 1) _isRequestActiveT1 = true;
                else if (team == 2) _isRequestActiveT2 = true;
                requester.SendChat(T.RequestUAVSent, activeCommander);
                activeCommander.SendChat(T.RequestUAVTell, requester, requester.Squad!, new GridLocation(pos));
                TipService.TryGiveTip(activeCommander, 0, T.TipUAVRequest, requester);
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
            if (!requester.IsSquadLeader() || requester.KitClass != Class.Squadleader)
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
            _isRequestActiveT1 = false;
        else if (team == 2)
            _isRequestActiveT2 = false;
    }
    public static UAV GiveUAV(ulong team, UCPlayer requester, UCPlayer approver, bool isMarker, Vector3 pos)
    {
        if (isMarker)
        {
            GridLocation loc = new GridLocation(pos);
            if (Gamemode.Config.GeneralUAVStartDelay > 0f)
            {
                if (approver.Steam64 != requester.Steam64 && approver.IsOnline)
                    approver.SendChat(T.UAVDeployedTimeMarkerCommander, loc, Mathf.CeilToInt(Gamemode.Config.GeneralUAVStartDelay), requester);
                if (requester.IsOnline)
                    requester.SendChat(T.UAVDeployedTimeMarker, loc, Mathf.CeilToInt(Gamemode.Config.GeneralUAVStartDelay));
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
            if (Gamemode.Config.GeneralUAVStartDelay > 0f)
            {
                if (approver.Steam64 != requester.Steam64 && approver.IsOnline)
                    approver.SendChat(T.UAVDeployedTimeSelfCommander, Mathf.CeilToInt(Gamemode.Config.GeneralUAVStartDelay), new GridLocation(pos), requester);
                if (requester.IsOnline)
                    requester.SendChat(T.UAVDeployedTimeSelf, Mathf.CeilToInt(Gamemode.Config.GeneralUAVStartDelay));
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
        GameObject obj = new GameObject(requester.Steam64 + "'s UAV", typeof(UAV));
        obj.transform.SetPositionAndRotation(loc, Quaternion.Euler(0f, UnityEngine.Random.value * 359.9f, 0f));
        UAV uav = obj.GetComponent<UAV>();
        uav.Init(team, requester, approver, loc, isMarker);
        return uav;
    }
    private void Init(ulong team, UCPlayer requester, UCPlayer approver, Vector3 loc, bool isMarker)
    {
        _requester = requester;
        _approver = approver;
        _startTime = Time.realtimeSinceStartup;
        _isMarker = isMarker;
        _deployPosition = loc;
        _deployGl = new GridLocation(_deployPosition);
        _team = team;
        if (team == 1)
        {
            if (_team1UAV != null)
                Destroy(_team1UAV);
            _team1UAV = this;
        }
        else if (team == 2)
        {
            if (_team2UAV != null)
                Destroy(_team2UAV);
            _team2UAV = this;
        }

        _scanSpeed = Gamemode.Config.GeneralUAVScanSpeed;
        _stDelay = Gamemode.Config.GeneralUAVStartDelay;
        _aliveTime = _stDelay + Gamemode.Config.GeneralUAVAliveTime;
        _radius = Gamemode.Config.GeneralUAVRadius;
#if DEBUG
        CircleZone.CalculateParticleSpawnPoints(out Vector2[] points, _radius, loc);
        TriggerEffectParameters p = new TriggerEffectParameters(ZonePlayerComponent.Side)
        {
            reliable = true,
            relevantDistance = Level.size
        };
        p.SetRelevantTransportConnections(Provider.GatherClientConnections());
        for (int i = 0; i < points.Length; ++i)
        {
            EffectManager.triggerEffect(p with { position = points[i] });
        }
#endif
        _inited = true;
    }

    [SuppressMessage(Data.SuppressCategory, Data.SuppressID)]
    [UsedImplicitly]
    private void OnDestroy()
    {
        if (!_inited) return;
        if (Time.realtimeSinceStartup - _startTime >= _aliveTime)
        {
            if (_requester.IsOnline)
                _requester.SendChat(T.UAVDestroyedTimer);
        }
        if (_buffAdded)
        {
            if (_requester.IsOnline)
                TraitManager.BuffUI.RemoveBuff(_requester, this);
        }
        if (_drop != null && _drop.model != null && Regions.tryGetCoordinate(_drop.model.position, out byte x, out byte y))
            BarricadeManager.destroyBarricade(_drop, x, y, ushort.MaxValue);
        if (_team == 1)
        {
            if (_team1UAV == this)
                _team1UAV = null;
        }
        else if (_team == 2)
        {
            if (_team2UAV == this)
                _team2UAV = null;
        }
        for (int j = 0; j < _scanOutput.Count; ++j)
        {
            SpottedComponent c = _scanOutput[j].Value;
            L.LogDebug("Spotter reset: " + c);
            c.OnUAVLeft();
        }
    }
    
    [SuppressMessage(Data.SuppressCategory, Data.SuppressID)]
    [UsedImplicitly]
    private void Update()
    {
        if (!_inited) return;
        float time = Time.realtimeSinceStartup;
        if (!_buffAdded)
        {
            TraitManager.BuffUI.AddBuff(_requester, this);
            _buffAdded = true;
        }
        if (time - _startTime < _stDelay)
            return;
        else if (!_active)
        {
            Activate();
        }
        if (time - _startTime > _aliveTime || (_didDropExist && _drop?.model == null))
        {
            Destroy(gameObject);
            return;
        }
        else if (!_isBlinking && time - _startTime > _aliveTime - Buff.BLINK_LEAD_TIME)
        {
            _isBlinking = true;
            TraitManager.BuffUI.UpdateBuffTimeState(this);
        }
        
        if (time - _lastScan > 4f)
        {
            _lastScan = time;
            // diffing scan
            if (_scanOutput.Count > 0)
            {
                KeyValuePair<float, SpottedComponent>[] sp2 = _scanOutput.ToArray();
                _scanOutput.Clear();
                L.LogDebug(time.ToString("0.##", CultureInfo.InvariantCulture) + " Scanning, " + sp2.Length + " existing spotters.");
                Scan();
                for (int j = 0; j < sp2.Length; ++j)
                {
                    SpottedComponent c = sp2[j].Value;
                    for (int i = 0; i < _scanOutput.Count; ++i)
                    {
                        if (ReferenceEquals(c, _scanOutput[i].Value))
                            goto next;
                    }
                    L.LogDebug(time.ToString("0.##", CultureInfo.InvariantCulture) + " spotter left: " + c + ".");
                    c.OnUAVLeft();
                next:;
                }
                L.LogDebug(time.ToString("0.##", CultureInfo.InvariantCulture) +  " spotters remaining: " + _scanOutput.Count + ".");
            }
            else
            {
                L.LogDebug(time.ToString("0.##", CultureInfo.InvariantCulture) + " Scanning, no existing spotters.");
                Scan();
            }
            _activeIndex = 0;
            _reachDistSqr = 0;
            _nextReachDistSqr = 0;
            L.LogDebug(time.ToString("0.##", CultureInfo.InvariantCulture) + $" Metrics: Reach: {_reachDistSqr:F2}, Active Index: {_activeIndex}.");
            return;
        }
        else if (_scanOutput.Count > 0 && _reachDistSqr < _radius * _radius)
        {
            _reachDistSqr = ((_reachDistSqr * Mathf.PI) + _scanSpeed * Time.deltaTime) / Mathf.PI;
            _lastScan = time - 3f;
            if (_nextReachDistSqr <= _reachDistSqr)
            {
                for (int i = _activeIndex; i < _scanOutput.Count; ++i)
                {
                    KeyValuePair<float, SpottedComponent> c = _scanOutput[i];
                    float dist = c.Key;
                    if (_reachDistSqr < dist)
                    {
                        if (i != _activeIndex)
                        {
                            _nextReachDistSqr = dist;
                            _activeIndex = i;
                            L.LogDebug(time.ToString("0.##", CultureInfo.InvariantCulture) + $" Metrics: Reach: {Mathf.Sqrt(_reachDistSqr):F2}, Active Index: {_activeIndex}.");
                        }
                        break;
                    }
                    SpottedComponent spot = c.Value;
                    L.LogDebug(time.ToString("0.##", CultureInfo.InvariantCulture) + " Dequeued " + spot + ".");

                    if (spot.UAVMode && spot.CurrentSpotter != null)
                    {
                        if (spot.CurrentSpotter.Steam64 == _requester.Steam64)
                        {
                            spot.UAVLastKnown = spot.transform.position;
                            L.LogDebug(time.ToString("0.##", CultureInfo.InvariantCulture) + " Updating spotter: " + spot);
                        }
                        else
                        {
                            L.LogDebug(time.ToString("0.##", CultureInfo.InvariantCulture) + " Spotter: " + spot +
                                       ": is already under the control of another UAV.");
                        }
                    }
                    else
                    {
                        spot.Activate(_requester, true);
                        L.LogDebug(time.ToString("0.##", CultureInfo.InvariantCulture) + " Activated spotter: " + spot);
                    }
                }
            }
        }
#if DEBUG
        if (time - _lastPing <= 1.5f)
            return;

        _lastPing = time;
        if (_modelAnimTransform == null)
            return;

        ClassConfig? config = SquadManager.Config.Classes.FirstOrDefault(x => x.Class == Class.Sniper);
        if (!(config?.MarkerEffect).TryGetAsset(out EffectAsset? asset))
            return;

        EffectManager.ClearEffectByGuid_AllPlayers(asset.GUID);
        EffectUtility.TriggerEffect(asset, Level.size * 2, _modelAnimTransform.position, false);
#endif
    }
    private void Activate()
    {
        _active = true;
        _isBlinking = _aliveTime < Buff.BLINK_LEAD_TIME + _stDelay;
        if (!_isBlinking)
            TraitManager.BuffUI.UpdateBuffTimeState(this);
#if DEBUG
        CircleZone.CalculateParticleSpawnPoints(out Vector2[] pts, _radius, new Vector2(_deployPosition.x, _deployPosition.z));
        if (ZonePlayerComponent.Airdrop != null)
        {
            EffectUtility.TriggerEffect(ZonePlayerComponent.Airdrop, Level.size, _deployPosition, false);
            for (int i = 0; i < pts.Length; ++i)
            {
                ref Vector2 pt = ref pts[i];
                EffectUtility.TriggerEffect(ZonePlayerComponent.Airdrop, Level.size, new Vector3(pt.x, F.GetHeight(pt, 0f), pt.y), false);
            }
        }
#endif
        if (_drop != null || !Gamemode.Config.BarricadeUAV.TryGetAsset(out ItemBarricadeAsset? asset))
            return;
        
        Transform tr = BarricadeManager.dropNonPlantedBarricade(
            new Barricade(asset, asset.health, asset.getState()), _deployPosition, Quaternion.Euler(new Vector3(-90f, 0f, 0f)), _requester.Steam64, TeamManager.GetGroupID(_team)
        );

        _drop = BarricadeManager.FindBarricadeByRootTransform(tr);
        if (_drop == null)
            return;

        _didDropExist = true;
        _modelAnimTransform = _drop.model.Find("UAV");
    }
    private void Scan()
    {
#if DEBUG
        float time = Time.realtimeSinceStartup;
#endif
        float rad = _radius * _radius;
        foreach (SpottedComponent spot in SpottedComponent.AllMarkers)
        {
            if (spot.OwnerTeam != _team && spot.isActiveAndEnabled && spot.Type.HasValue && CanUAVSpot(spot.Type.Value))
            {
                float dist = (spot.transform.position - _deployPosition).sqrMagnitude;
                if (dist < rad)
                    _scanOutput.Add(new KeyValuePair<float, SpottedComponent>(dist, spot));
            }
        }

        _scanOutput.Sort((a, b) => a.Key.CompareTo(b.Key));
#if DEBUG
        L.LogDebug(time.ToString("0.##", CultureInfo.InvariantCulture) + " Scan output: ");
        using IDisposable d = L.IndentLog(1);
        for (int i = 0; i < _scanOutput.Count; ++i)
        {
            L.LogDebug(Mathf.Sqrt(_scanOutput[i].Key).ToString("0.##", CultureInfo.InvariantCulture) + "m: " + _scanOutput[i].Value);
        }
#endif
    }
    private static bool CanUAVSpot(SpottedComponent.Spotted spotType)
    {
        return spotType switch { _ => true };
    }
#endif
}
