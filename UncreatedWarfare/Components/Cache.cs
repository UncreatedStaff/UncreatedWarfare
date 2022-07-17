using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using UnityEngine;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare.Components;

public class Cache : MonoBehaviour, IFOB
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

    private Coroutine loop;

    private void Awake()
    {
        Structure = BarricadeManager.FindBarricadeByRootTransform(transform);
        NearbyDefenders = new List<UCPlayer>();
        NearbyAttackers = new List<UCPlayer>();

        Radius = 40;

        IsDiscovered = false;

        _gc = new GridLocation(Position);
        _cl = (LevelNodes.nodes
            .Where(n => n.type == ENodeType.LOCATION)
            .Aggregate((n1, n2) =>
                (n1.point - Position).sqrMagnitude <= (n2.point - Position).sqrMagnitude ? n1 : n2) as LocationNode)?
            .name ?? string.Empty;

        if (Data.Is(out IFlagRotation fg))
        {
            Flag flag = fg.LoadedFlags.Find(f => f.Name.Equals(_cl, StringComparison.OrdinalIgnoreCase));
            if (flag != null)
                _cl = flag.ShortName;
        }

        loop = StartCoroutine(Tick());
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
        if (Data.Is(out Insurgency ins) && Gamemode.Config.UI.MarkerCacheAttack.ValidReference(out Guid effect))
        {
            IconManager.AttachIcon(effect, Structure.model, ins.AttackingTeam, 2.25F);
        }
    }

    private float lastTick = 0;
    private uint ticks = 0;
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
                    else if (NearbyDefenders.Remove(pl))
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
                    else if (NearbyAttackers.Remove(pl))
                        OnDefenderLeft(pl);
                }
            }
            if (ticks % (20 / TICK_SPEED) == 0)
                Tickets.TicketManager.OnCache20Seconds();

            ++ticks;
        }
    }
    private IEnumerator<WaitForSeconds> Tick()
    {
        float time = 0;
        float tickFrequency = 0.25F;

        while (true)
        {
            time += tickFrequency;

            if (IsDestroyed) yield break;

#if DEBUG
            IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            foreach (UCPlayer player in PlayerManager.OnlinePlayers)
            {
                if (player.GetTeam() == Team)
                {
                    if ((player.Position - Position).sqrMagnitude < Math.Pow(Radius, 2))
                    {
                        if (!NearbyDefenders.Contains(player))
                        {
                            NearbyDefenders.Add(player);
                            OnDefenderEntered(player);
                        }
                    }
                    else
                    {
                        if (NearbyDefenders.Remove(player))
                        {
                            OnDefenderLeft(player);
                        }
                    }
                }
                else
                {
                    if ((player.Position - Position).sqrMagnitude < Math.Pow(Radius, 2))
                    {
                        if (!NearbyAttackers.Contains(player))
                        {
                            NearbyAttackers.Add(player);
                            OnAttackerEntered(player);
                        }
                    }
                    else
                    {
                        if (NearbyAttackers.Remove(player))
                        {
                            OnAttackerLeft(player);
                        }
                    }
                }
            }

            if (time % 20 == 0)
            {
                Tickets.TicketManager.OnCache20Seconds();
            }

            if (time >= 60)
                time = 0;
#if DEBUG
            profiler.Dispose();
#endif
            yield return new WaitForSeconds(tickFrequency);
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

        StopCoroutine(loop);

        Destroy(gameObject, 2);
    }
    string ITranslationArgument.Translate(string language, string? format, UCPlayer? target, TranslationFlags flags)
    {
        if (format is not null && format.Equals(FOB.COLORED_NAME_FORMAT, StringComparison.Ordinal))
            return Localization.Colorize(TeamManager.GetTeamHexColor(Team), Name, flags);
        return Name;
    }
}
