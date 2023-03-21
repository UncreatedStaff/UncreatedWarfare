using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Events.Barricades;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Levels;
using UnityEngine;

namespace Uncreated.Warfare.Components;

internal class RepairableComponent : MonoBehaviour
{
    public BarricadeDrop Structure { get; private set; }

    private Dictionary<ulong, float> PlayerHits;

    public void Awake()
    {
        Structure = BarricadeManager.FindBarricadeByRootTransform(transform);
        PlayerHits = new Dictionary<ulong, float>();
    }

    public void Repair(UCPlayer builder)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Structure.GetServersideData().barricade.health >= Structure.asset.health)
            return;

        float amount = 30;

        if (builder.KitClass == Class.CombatEngineer)
            amount *= 2;

        BarricadeManager.repair(transform, amount, 1, builder.CSteamID);
        if (Gamemode.Config.EffectDig.ValidReference(out EffectAsset effect))
            F.TriggerEffectReliable(effect, EffectManager.MEDIUM, builder.Position);

        if (PlayerHits.ContainsKey(builder.Steam64))
            PlayerHits[builder.Steam64] += amount;
        else
            PlayerHits.Add(builder.Steam64, amount);

        /*
        if (Structure.GetServersideData().barricade.health >= Structure.asset.health)
        {
            // todo give XP?
        }*/
    }
    public void Destroy(BarricadeDestroyed e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        BuildableData buildable = FOBManager.Config.Buildables.Find(b => b.BuildableBarricade.MatchGuid(Structure.asset.GUID) && b.Type != BuildableType.Emplacement);

        if (buildable != null && buildable.Foundation.ValidReference(out ItemBarricadeAsset asset))
        {
            string structureName = asset.itemName;
            // todo translation
            string message = structureName + " DESTROYED";

            UCPlayer? player = e.Instigator;
            if (player != null)
            {
                ulong bteam = Structure.GetServersideData().group.GetTeam();
                bool teamkilled = bteam != 0 && bteam == player.GetTeam();

                if (buildable.Type == BuildableType.Bunker)
                {
                    if (teamkilled)
                    {
                        // TODO: find out why random barricade teamkills are still happening, if they are at all
                        //Points.AwardXP(player, XPReward.FriendlyBunkerDestroyed);
                    }
                    else
                    {
                        Points.AwardXP(player, XPReward.BunkerDestroyed);
                        Points.TryAwardDriverAssist(player.Player, XPReward.BunkerDestroyed, quota: 5);
                    }
                }
                else if (buildable.Type == BuildableType.Fortification)
                {
                    int amount = Mathf.RoundToInt(buildable.RequiredHits * 0.1f);


                    if (teamkilled)
                    {
                        // TODO: find out why random barricade teamkills are still happening, if they are at all
                        //Points.AwardXP(player, XPReward.FriendlyBuildableDestroyed, "FRIENDLY " + message, -amount);
                    }
                    else
                    {
                        Points.AwardXP(player, XPReward.FortificationDestroyed, message, amount);
                        Points.TryAwardDriverAssist(player.Player, XPReward.FortificationDestroyed, quota: 0.1f);
                    }
                }
                else
                {
                    int amount = Mathf.RoundToInt(buildable.RequiredHits * 0.75f);
                    if (teamkilled)
                    {
                        Points.AwardXP(player, XPReward.FriendlyBuildableDestroyed, "FRIENDLY " + message, -amount);
                    }
                    else
                    {
                        Points.AwardXP(player, XPReward.BuildableDestroyed, message, amount);
                        Points.TryAwardDriverAssist(player.Player, XPReward.BuildableDestroyed, quota: amount * 0.02f);
                    }
                }
            }
        }

        Destroy(this);
    }
}