using SDG.Unturned;
using System.Collections.Generic;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;
using UnityEngine;

namespace Uncreated.Warfare.FOBs
{
    public class AmmoBagComponent : MonoBehaviour
    {
        public SDG.Unturned.BarricadeData data;
        public BarricadeDrop drop;
        public Dictionary<ulong, int> ResuppliedPlayers;
        public int uses;
        public void Initialize(SDG.Unturned.BarricadeData data, BarricadeDrop drop)
        {
            this.data = data;
            this.drop = drop;
            ResuppliedPlayers = new Dictionary<ulong, int>();
            uses = 0;
        }
        public void ResupplyPlayer(UCPlayer player, Kit kit)
        {
            KitManager.ResupplyKit(player, kit, true);

            uses++;

            UCPlayer owner = UCPlayer.FromID(data.owner);
            if (owner != null && owner.Steam64 != player.Steam64)
                Points.AwardTW(owner, Points.TWConfig.ResupplyFriendlyPoints, Translation.Translate("xp_resupplied_teammate", owner));

            if (uses >= FOBManager.config.data.AmmoBagMaxUses && Regions.tryGetCoordinate(drop.model.position, out byte x, out byte y))
            {
                player.Message("ammo_success_bag_finished");
                BarricadeManager.destroyBarricade(drop, x, y, ushort.MaxValue);
            }
            else
            {
                player.Message("ammo_success_bag", (FOBManager.config.data.AmmoBagMaxUses - uses).ToString());

                if (ResuppliedPlayers.ContainsKey(player.Steam64))
                    ResuppliedPlayers[player.Steam64] = player.LifeCounter;
                else
                    ResuppliedPlayers.Add(player.Steam64, player.LifeCounter);
            }
        }
    }
}
