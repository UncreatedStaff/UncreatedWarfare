using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using UnityEngine;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare.Components
{
    public class Cache : MonoBehaviour
    {
        public BarricadeDrop Structure { get; private set; }
        public int Number;
        public string Name;
        public string GridCoordinates;
        public string ClosestLocation;
        public ulong Team { get => Structure.GetServersideData().group; }
        public Vector3 Position { get => Structure.model.position; }
        public float Radius { get; private set; }
        public bool IsDiscovered;
        public bool IsDestroyed { get => Structure == null || Structure.GetServersideData().barricade.isDead; }
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

            GridCoordinates = F.ToGridPosition(Position);
            ClosestLocation =
                (LevelNodes.nodes
                .Where(n => n.type == ENodeType.LOCATION)
                .Aggregate((n1, n2) =>
                    (n1.point - Position).sqrMagnitude <= (n2.point - Position).sqrMagnitude ? n1 : n2) as LocationNode)
                .name;

            if (Data.Is(out IFlagRotation fg))
            {
                Flag flag = fg.LoadedFlags.Find(f => f.Name == ClosestLocation);
                if (flag != null)
                {
                    if (flag.ShortName != "")
                        ClosestLocation = flag.ShortName;
                }
            }

            loop = StartCoroutine(Tick());

            // UCWarfare.GetColorHex("insurgency_cache_color");
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
            if (Data.Is(out Insurgency ins))
            {
                IconManager.AttachIcon(Gamemode.Config.UI.MarkerCacheAttack, Structure.model, ins.AttackingTeam, 2.25F);
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

                foreach (var player in PlayerManager.OnlinePlayers)
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

                if (time % 10 == 0)
                {
                    Tickets.TicketManager.OnCache10Seconds();
                }

                if (time >= 60)
                    time = 0;
                yield return new WaitForSeconds(tickFrequency);
            }
        }
        public void Destroy()
        {
            foreach (var player in NearbyDefenders)
                OnDefenderLeft(player);
            foreach (var player in NearbyAttackers)
                OnAttackerLeft(player);

            NearbyAttackers.Clear();
            NearbyDefenders.Clear();

            StopCoroutine(loop);

            Destroy(gameObject, 2);
        }
    }
}
