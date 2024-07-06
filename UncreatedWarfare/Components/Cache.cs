using JetBrains.Annotations;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Layouts.Insurgency;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Models.Stats.Records;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Util;
using UnityEngine;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare.Components;

public class Cache : IRadiusFOB, IObjective, IPlayerDisconnectListener, IDisposable
{
    private bool _disposed;
    private CacheComponent? _component;
    private readonly GridLocation _gc;
    private readonly string _cl;
    public int Number;
    public bool IsDiscovered;
    public IBuildableDestroyedEvent? DestroyInfo { get; set; }
    public GridLocation GridLocation => _gc;
    public string ClosestLocation => _cl;
    public ulong Team { get; }
    public List<UCPlayer> NearbyDefenders { get; private set; }
    public List<UCPlayer> NearbyAttackers { get; private set; }
    UCPlayer? IFOB.Instigator { get; set; }
    public Vector3 Position { get; }
    public Vector3 SpawnPosition { get; }
    public bool IsDestroyed { get; private set; }
    public string Name { get; }
    float IDeployable.Yaw => _component == null ? 0 : _component.transform.rotation.eulerAngles.y;
    public IBuildable Drop { get; private set; }
    public CacheLocation Location { get; }
    public float Radius => 40;
    public bool ContainsBuildable(IBuildable buildable) => Drop != null && Drop.Equals(buildable);
    public bool ContainsVehicle(InteractableVehicle vehicle) => false;
    public IFOBItem? FindFOBItem(IBuildable buildable) => null;
    public IFOBItem? FindFOBItem(InteractableVehicle vehicle) => null;
    public FobRecordTracker<WarfareDbContext>? Record { get; }
    public Cache(IBuildable drop, ulong team, CacheLocation location, string name, int number)
    {
        IsDiscovered = false;
        _component = drop.Model.gameObject.AddComponent<CacheComponent>().Initialize(drop, this);
        Drop = drop;
        Name = name;
        Number = number;
        Vector3 pos = _component.transform.position;
        SpawnPosition = pos + new Vector3(0, 1f, 0);
        Position = pos;
        _gc = new GridLocation(in pos);
        Team = team;
        _cl = F.GetClosestLocationName(pos, true, true);
        Location = location;

        if (Data.Is(out IFlagRotation? fg))
        {
            Flag flag = fg.LoadedFlags.Find(f => f.Name.Equals(_cl, StringComparison.OrdinalIgnoreCase));
            if (flag != null)
                _cl = flag.ShortName;
        }

        FobRecord fobRecord = new FobRecord
        {
            Steam64 = null,
            FobAngle = Drop.Rotation.eulerAngles,
            FobPosition = Drop.Position,
            FobName = Name,
            FobType = FobType.Cache,
            FobNumber = Number,
            NearestLocation = ClosestLocation,
            Timestamp = DateTimeOffset.UtcNow,
            Team = (byte)Team
        };

        Record = new FobRecordTracker<WarfareDbContext>(fobRecord);
        UCWarfare.RunTask(Record.Create, ctx: "Create FOB record.");
    }

    public string UIColor
    {
        get
        {
            if (NearbyAttackers.Count != 0)
                return UCWarfare.GetColorHex("enemy_nearby_fob_color");
            if (!IsDiscovered)
                return UCWarfare.GetColorHex("insurgency_cache_undiscovered_color");
            
            return UCWarfare.GetColorHex("insurgency_cache_discovered_color");
        }
    }

    
    public void SpawnAttackIcon()
    {
        _component?.SpawnAttackIcon();
    }
    
    string ITranslationArgument.Translate(LanguageInfo language, string? format, UCPlayer? target, CultureInfo? culture,
        ref TranslationFlags flags)
    {
        if (format is not null)
        {
            if (format.Equals(FOB.FormatNameColored, StringComparison.Ordinal))
                return Localization.Colorize(UIColor, Name, flags);
            else if (format.Equals(FOB.FormatLocationName, StringComparison.Ordinal))
                return ClosestLocation;
            else if (format.Equals(FOB.FormatGridLocation, StringComparison.Ordinal))
                return GridLocation.ToString();
        }
        return Name;
    }
    bool IDeployable.CheckDeployable(UCPlayer player, CommandContext? ctx)
    {
        ulong team = player.GetTeam();
        if (team == 0 || team != Team)
        {
            if (ctx is not null)
                throw ctx.Reply(T.DeployableNotFound, Name);
            return false;
        }
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
        ulong team = player.GetTeam();
        if (team == 0 || team != Team)
        {
            if (chat)
                player.SendChat(T.DeployCancelled);
            return false;
        }
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
        ActionLog.Add(ActionLogType.DeployToLocation, "CACHE " + Name + " TEAM " + TeamManager.TranslateName(Team), player);
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
        if (Data.Singletons.TryGetSingleton(out FOBManager fobManager))
            fobManager.UpdateFOBInList(Team, this);
    }
    internal void OnAttackerLeft(UCPlayer player)
    {
        if (Data.Singletons.TryGetSingleton(out FOBManager fobManager))
            fobManager.UpdateFOBInList(Team, this);
    }

    public override string ToString()
    {
        return $"{Name} - T{Team} - Discovered: {IsDiscovered} - Destroyed: {IsDestroyed}";
    }
    public void Dump(UCPlayer? target)
    {
        L.Log($"[FOBS] [{Name}] === Special Fob Dump ===");
        L.Log($" Grid Location: {GridLocation}, Closest Location: {ClosestLocation}.");
        L.Log($" Color: {UIColor}.");
        L.Log($" Discovered: {IsDiscovered}, Destroyed: {IsDestroyed}.");
        L.Log($" Team: {Team}.");
    }

    void IPlayerDisconnectListener.OnPlayerDisconnecting(UCPlayer player)
    {
        if (NearbyAttackers.Remove(player))
            OnAttackerLeft(player);
        if (NearbyDefenders.Remove(player))
            OnDefenderLeft(player);
    }
    public bool IsPlayerOn(UCPlayer player) => NearbyDefenders.Contains(player) || NearbyAttackers.Contains(player);
    public void Dispose()
    {
        if (_disposed) return;
        if (_component != null)
            _component.Destroy();
        if (Data.Is(out Insurgency? ins))
            ins.OnCacheDestroyed(this, ((IFOB)this).Instigator);
        _disposed = true; // MUST be set to true before deleting the barricade, or this method will be called multiple times recursively
        if (Drop != null)
        {
            Drop.Destroy();
            Drop = null!;
        }
        Record?.Dispose();
    }
    public class CacheComponent : MonoBehaviour, IManualOnDestroy
    {
        private Cache _cache;
        private BarricadeDrop _structure;
        public Cache Cache => _cache;
        public ulong Team => _structure.GetServersideData().group.GetTeam();

        public CacheComponent Initialize(BarricadeDrop drop, Cache cache)
        {
            _cache = cache;
            _structure = drop;
            _cache.NearbyDefenders = new List<UCPlayer>();
            _cache.NearbyAttackers = new List<UCPlayer>();

            return this;
        }
        public void SpawnAttackIcon()
        {
            if (Data.Is(out IAttackDefense? ins) && Gamemode.Config.EffectMarkerCacheAttack.TryGetGuid(out Guid effect))
            {
                IconManager.AttachIcon(effect, _structure.model, ins.AttackingTeam, 30F);
            }
        }
        public void Destroy()
        {
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

        private float _lastTick;
        private const float TickSpeed = 0.25f;

        [UsedImplicitly]
        private void Update()
        {
            float t = Time.realtimeSinceStartup;
            if (t - _lastTick > TickSpeed)
            {
                float rad = Cache.Radius;
                rad *= rad;
                float vehicleRad = Cache.Radius / 4f;
                vehicleRad *= vehicleRad;
                _lastTick = t;
                Vector3 pos = transform.position;
                for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                {
                    UCPlayer pl = PlayerManager.OnlinePlayers[i];
                    ulong team = pl.GetTeam();
                    Vector3 pos2 = pl.Position;
                    if (pos2 == Vector3.zero) continue;
                    if (team == Team)
                    {
                        if (!pl.Player.life.isDead && (pos2 - pos).sqrMagnitude < rad)
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
                        bool isProxying = !pl.Player.life.isDead && (pos2 - pos).sqrMagnitude < rad;

                        if (isProxying && pl.IsInVehicle)
                            isProxying = (pos2 - pos).sqrMagnitude < vehicleRad;

                        if (isProxying)
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

        public void ManualOnDestroy()
        {
            _cache.Drop = null!;
            _cache.Dispose();
        }
    }
}
