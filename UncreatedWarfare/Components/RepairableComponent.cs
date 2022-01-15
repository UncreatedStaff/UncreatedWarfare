using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Point;
using UnityEngine;

namespace Uncreated.Warfare.Components
{
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
            if (Structure.GetServersideData().barricade.health >= Structure.asset.health)
                return;

                float amount = 30;

            if (builder.KitClass == EClass.COMBAT_ENGINEER)
                amount *= 2;

            BarricadeManager.repair(transform, amount, 1, builder.CSteamID);

            EffectManager.sendEffect(38405, EffectManager.MEDIUM, builder.Position);

            if (PlayerHits.ContainsKey(builder.Steam64))
                PlayerHits[builder.Steam64] += amount;
            else
                PlayerHits.Add(builder.Steam64, amount);

            if (Structure.GetServersideData().barricade.health >= Structure.asset.health)
            {
                // give XP?
            }
        }
        public void Destroy()
        {
            var buildable = FOBManager.config.Data.Buildables.Find(b => b.structureID == Structure.asset.GUID && b.type != EBuildableType.EMPLACEMENT);

            if (buildable != null)
            {
                string structureName = Assets.find<ItemBarricadeAsset>(buildable.foundationID).itemName;
                string message = structureName + " DESTROYED";

                UCPlayer player = null;
                if (Structure.model.TryGetComponent(out BarricadeComponent component))
                {
                    player = UCPlayer.FromID(component.LastDamager);
                }

                if (player != null)
                {
                    bool teamkilled = Structure.GetServersideData().group == player.GetTeam();

                    int amount = 0;
                    if (buildable.type == EBuildableType.FOB_BUNKER)
                    {
                        if (teamkilled) amount = Points.XPConfig.FOBTeamkilledXP;
                        else amount = Points.XPConfig.FOBKilledXP;
                    }
                    if (buildable.type == EBuildableType.FORTIFICATION)
                    {
                        amount = (int)Math.Round(buildable.requiredHits * 0.25F);
                        if (teamkilled) amount *= -1;
                    }
                    else
                    {
                        amount = (int)Math.Round(buildable.requiredHits * 0.75F);
                        if (teamkilled) amount *= -1;
                    }

                    if (teamkilled)
                    {
                        message = "FRIENDLY " + message;
                    }

                    if (amount != 0)
                        Points.AwardXP(player, amount, message.ToUpper());
                }
            }

            Destroy(this);
        }
    }
    
}
