using SDG.Unturned;
using System;
using System.Collections.Generic;
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

public class Cache : MonoBehaviour, IFOB, IObjective, IDeployable
{
    public int Number;
    private string _name;
    private GridLocation _gc;
    private string _cl;
    public bool IsDiscovered;
    public GridLocation GridLocation => _gc;
    public string ClosestLocation => _cl;
    public ulong Team => Structure.GetServersideData().group;
    public Vector3 Position => Structure.model.position;
    public bool IsDestroyed => !isActiveAndEnabled || Structure == null || Structure.GetServersideData().barricade.isDead;
    public string Name { get => _name; set => _name = value; }
    float IDeployable.Yaw => Structure == null || Structure.model == null ? 0 : Structure.model.rotation.eulerAngles.y;
    public float Radius
    {
        get => _radius;
        private set
        {
            _radius = value;
            _sqrRadius = value * value;
        }
    }

    private float _radius;
    private float _sqrRadius;

    public BarricadeDrop Structure { get; private set; }
    public string UIColor
    {
        get
        {
            if (NearbyAttackers.Count != 0)
                return UCWarfare.GetColorHex("enemy_nearby_fob_color");
            else if (!IsDiscovered)
                return UCWarfare.GetColorHex("insurgency_cache_undiscovered_color");
            else
                return UCWarfare.GetColorHex("insurgency_cache_discovered_color");
        }
    }

    public List<UCPlayer> NearbyDefenders { get; private set; }
    public List<UCPlayer> NearbyAttackers { get; private set; }

    private void Awake()
    {
        Structure = BarricadeManager.FindBarricadeByRootTransform(transform);
        NearbyDefenders = new List<UCPlayer>();
        NearbyAttackers = new List<UCPlayer>();

        Radius = 40;

        IsDiscovered = false;
        Vector3 pos = Position;
        _gc = new GridLocation(in pos);
        _cl = F.GetClosestLocation(pos);

        if (Data.Is(out IFlagRotation fg))
        {
            Flag flag = fg.LoadedFlags.Find(f => f.Name.Equals(_cl, StringComparison.OrdinalIgnoreCase));
            if (flag != null)
                _cl = flag.ShortName;
        }

        EventDispatcher.OnPlayerLeaving += OnPlayerDisconnect;
    }
    private void OnDestroy()
    {
        EventDispatcher.OnPlayerLeaving -= OnPlayerDisconnect;
    }
    private void OnPlayerDisconnect(PlayerEvent e)
    {
        NearbyDefenders.RemoveAll(x => !x.IsOnline || x.Steam64 == e.Steam64);
    }

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
    public void SpawnAttackIcon()
    {
        if (Data.Is(out Insurgency ins) && Gamemode.Config.EffectMarkerCacheAttack.ValidReference(out Guid effect))
        {
            IconManager.AttachIcon(effect, Structure.model, ins.AttackingTeam, 2.25F);
        }
    }

    private float lastTick = 0;
    private const float TICK_SPEED = 0.25f;
    private void Update()
    {
        float t = Time.realtimeSinceStartup;
        if (t - lastTick > TICK_SPEED && !IsDestroyed)
        {
            lastTick = t;
            Vector3 pos = Position;
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
                        if (!NearbyDefenders.HasPlayer(pl))
                        {
                            NearbyDefenders.Add(pl);
                            OnDefenderEntered(pl);
                        }
                    }
                    else if (NearbyDefenders.RemoveFast(pl))
                        OnDefenderLeft(pl);
                }
                else if (team is > 0 and < 3)
                {
                    if ((pos2 - pos).sqrMagnitude < _sqrRadius)
                    {
                        if (!NearbyAttackers.HasPlayer(pl))
                        {
                            NearbyAttackers.Add(pl);
                            OnDefenderEntered(pl);
                        }
                    }
                    else if (NearbyAttackers.RemoveFast(pl))
                        OnDefenderLeft(pl);
                }
            }
        }
    }
    public void Destroy()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        foreach (UCPlayer player in NearbyDefenders)
            OnDefenderLeft(player);
        foreach (UCPlayer player in NearbyAttackers)
            OnAttackerLeft(player);

        NearbyAttackers.Clear();
        NearbyDefenders.Clear();

        Destroy(gameObject, 2);
    }
    string ITranslationArgument.Translate(string language, string? format, UCPlayer? target, ref TranslationFlags flags)
    {
        if (format is not null)
        {
            if (format.Equals(FOB.COLORED_NAME_FORMAT, StringComparison.Ordinal))
                return Localization.Colorize(TeamManager.GetTeamHexColor(Team), Name, flags);
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
        if (Structure == null || Structure.GetServersideData().barricade.isDead)
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
        if (Structure == null || Structure.GetServersideData().barricade.isDead)
        {
            if (chat)
                player.SendChat(T.DeployDestroyed);
            return false;
        }

        return true;
    }
    void IDeployable.OnDeploy(UCPlayer player, bool chat)
    {
        if (chat)
            player.SendChat(T.DeploySuccess, this);
    }
}
