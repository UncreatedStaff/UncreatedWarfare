using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Officers;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.XP;
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
            var buildable = FOBManager.config.Data.Buildables.Find(b => b.structureID == Structure.asset.GUID && b.type != EbuildableType.EMPLACEMENT);

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
                    if (buildable.type == EbuildableType.FOB_BUNKER)
                    {
                        if (teamkilled) amount = XPManager.config.Data.FOBTeamkilledXP;
                        else amount = XPManager.config.Data.FOBKilledXP;
                    }
                    if (buildable.type == EbuildableType.FORTIFICATION)
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
                        XPManager.AddXP(player.Player, amount, message.ToUpper());
                }
            }

            Destroy(gameObject);
        }
    }
    
}
