using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Teams;
using UnityEngine;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare.Components;

public class Cache : IFOB, IObjective, IDeployable
{
    private CacheComponent? _component;
    private string _name;
    private readonly GridLocation _gc;
    private readonly string _cl;
    public int Number;
    public bool IsDiscovered;
    public GridLocation GridLocation => _gc;
    public string ClosestLocation => _cl;
    public ulong Team => _component == null ? 0 : _component.Team;
    public List<UCPlayer> NearbyDefenders { get; private set; }
    public List<UCPlayer> NearbyAttackers { get; private set; }

    public Vector3 Position { get; private set; }
    public bool IsDestroyed { get; private set; }
    public string Name { get => _name; set => _name = value; }
    float IDeployable.Yaw => _component == null ? 0 : _component.transform.rotation.eulerAngles.y;

    public Cache(BarricadeDrop drop)
    {
        IsDiscovered = false;
        _component = drop.model.gameObject.AddComponent<CacheComponent>().Initialize(drop, this);

        Position = _component.transform.position;
        Vector3 pos = Position;
        _gc = new GridLocation(in pos);
        _cl = F.GetClosestLocationName(pos);

        if (Data.Is(out IFlagRotation fg))
        {
            Flag flag = fg.LoadedFlags.Find(f => f.Name.Equals(_cl, StringComparison.OrdinalIgnoreCase));
            if (flag != null)
                _cl = flag.ShortName;
        }
    }

    public string UIColor
    {
        get
        {
            L.Log("CACHE: getting UI color...");

            if (NearbyAttackers.Count != 0)
                return UCWarfare.GetColorHex("enemy_nearby_fob_color");
            else if (!IsDiscovered)
                return UCWarfare.GetColorHex("insurgency_cache_undiscovered_color");
            else
                return UCWarfare.GetColorHex("insurgency_cache_discovered_color");
        }
    }

    
    public void SpawnAttackIcon()
    {
        _component?.SpawnAttackIcon();
    }
    
    string ITranslationArgument.Translate(string language, string? format, UCPlayer? target, CultureInfo? culture,
        ref TranslationFlags flags)
    {
        if (format is not null)
        {
            if (format.Equals(FOB.COLORED_NAME_FORMAT, StringComparison.Ordinal))
                return Localization.Colorize(UIColor, Name, flags);
            else if (format.Equals(FOB.CLOSEST_LOCATION_FORMAT, StringComparison.Ordinal))
                return ClosestLocation;
            else if (format.Equals(FOB.GRID_LOCATION_FORMAT, StringComparison.Ordinal))
                return GridLocation.ToString();
        }
        return Name;
    }
    bool IDeployable.CheckDeployable(UCPlayer player, CommandInteraction? ctx)
    {
        if (NearbyAttackers.Count != 0)
        {
            if (ctx is not null)
                throw ctx.Reply(T.DeployEnemiesNearby, this);
            return false;
        }
        if (_component == null)
        {
            if (ctx is not null)
                throw ctx.Reply(T.DeployDestroyed, this);
            return false;
        }

        return true;
    }
    bool IDeployable.CheckDeployableTick(UCPlayer player, bool chat)
    {
        if (NearbyAttackers.Count != 0)
        {
            if (chat)
                player.SendChat(T.DeployEnemiesNearbyTick, this);
            return false;
        }
        if (_component == null)
        {
            if (chat)
                player.SendChat(T.DeployDestroyed);
            return false;
        }

        return true;
    }
    void IDeployable.OnDeploy(UCPlayer player, bool chat)
    {
        ActionLog.Add(ActionLogType.DeployToLocation, "CACHE " + Name + " TEAM " + TeamManager.TranslateName(Team, 0), player);
        if (chat)
            player.SendChat(T.DeploySuccess, this);
    }

    float IDeployable.GetDelay() => FOBManager.Config.DeployFOBDelay;
    internal void OnDefenderEntered(UCPlayer player)
    {

    }
    internal void OnDefenderLeft(UCPlayer player)
    {

    }
    internal void OnAttackerEntered(UCPlayer player)
    {
        FOBManager.UpdateFOBListForTeam(Team, this);
    }
    internal void OnAttackerLeft(UCPlayer player)
    {
        FOBManager.UpdateFOBListForTeam(Team, this);
    }

    public override string ToString()
    {
        return $"{Name} - T{Team} - Discovered: {IsDiscovered} - Destroyed: {IsDestroyed}";
    }

    public class CacheComponent : MonoBehaviour
    {
        private Cache _cache;
        private float _radius;
        private float _sqrRadius;
        private BarricadeDrop _structure;
        public Cache Cache => _cache;
        public ulong Team => _structure.GetServersideData().group;
        public float Radius
        {
            get => _radius;
            private set
            {
                _radius = value;
                _sqrRadius = value * value;
            }
        }

        public CacheComponent Initialize(BarricadeDrop drop, Cache cache)
        {
            _cache = cache;
            _structure = drop;
            Radius = 40;
            _cache.NearbyDefenders = new List<UCPlayer>();
            _cache.NearbyAttackers = new List<UCPlayer>();

            EventDispatcher.PlayerLeaving += OnPlayerDisconnect;

            return this;
        }
        public void SpawnAttackIcon()
        {
            if (Data.Is(out Insurgency ins) && Gamemode.Config.EffectMarkerCacheAttack.ValidReference(out Guid effect))
            {
                IconManager.AttachIcon(effect, _structure.model, ins.AttackingTeam, 30F);
            }
        }
        private void OnDestroy()
        {
            EventDispatcher.PlayerLeaving -= OnPlayerDisconnect;
        }
        public void Destroy()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            foreach (UCPlayer player in _cache.NearbyDefenders)
                _cache.OnDefenderLeft(player);
            foreach (UCPlayer player in _cache.NearbyAttackers)
                _cache.OnAttackerLeft(player);

            _cache.NearbyAttackers.Clear();
            _cache.NearbyDefenders.Clear();

            _cache.IsDestroyed = true;
            _cache._component = null;

            Destroy(gameObject);
        }
        private void OnPlayerDisconnect(PlayerEvent e)
        {
            _cache.NearbyDefenders.RemoveAll(x => !x.IsOnline || x.Steam64 == e.Steam64);
        }

        private float lastTick = 0;
        private const float TICK_SPEED = 0.25f;
        private void Update()
        {
            float t = Time.realtimeSinceStartup;
            if (t - lastTick > TICK_SPEED)
            {
                lastTick = t;
                Vector3 pos = transform.position;
                for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                {
                    UCPlayer pl = PlayerManager.OnlinePlayers[i];
                    ulong team = pl.GetTeam();
                    Vector3 pos2 = pl.Position;
                    if (pos2 == Vector3.zero) continue;
                    if (team == Team)
                    {
                        if ((pos2 - pos).sqrMagnitude < _sqrRadius)
                        {
                            if (!_cache.NearbyDefenders.HasPlayer(pl))
                            {
                                _cache.NearbyDefenders.Add(pl);
                                _cache.OnDefenderEntered(pl);
                            }
                        }
                        else if (_cache.NearbyDefenders.Remove(pl))
                            _cache.OnDefenderLeft(pl);
                    }
                    else if (team is > 0 and < 3)
                    {
                        if ((pos2 - pos).sqrMagnitude < _sqrRadius)
                        {
                            if (!_cache.NearbyAttackers.HasPlayer(pl))
                            {
                                _cache.NearbyAttackers.Add(pl);
                                _cache.OnDefenderEntered(pl);
                            }
                        }
                        else if (_cache.NearbyAttackers.Remove(pl))
                            _cache.OnDefenderLeft(pl);
                    }
                }
            }
        }
    }
}
