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
        public int Ammo;
        public void Initialize(SDG.Unturned.BarricadeData data, BarricadeDrop drop)
        {
            this.data = data;
            this.drop = drop;
            ResuppliedPlayers = new Dictionary<ulong, int>();
            Ammo = FOBManager.config.data.AmmoBagMaxUses;
        }
        public void ResupplyPlayer(UCPlayer player, Kit kit, int ammoCost)
        {
            Ammo -= ammoCost;

            KitManager.ResupplyKit(player, kit, true);

            UCPlayer owner = UCPlayer.FromID(data.owner);
            if (owner != null && owner.Steam64 != player.Steam64)
            {
                Points.AwardXP(owner, Points.XPConfig.ResupplyFriendlyXP, Translation.Translate("xp_resupplied_teammate", owner));
                Points.AwardTW(owner, Points.TWConfig.ResupplyFriendlyPoints);
            }

            player.Message("ammo_success", ammoCost.ToString(), Ammo.ToString());

            if (ResuppliedPlayers.ContainsKey(player.Steam64))
                ResuppliedPlayers[player.Steam64] = player.LifeCounter;
            else
                ResuppliedPlayers.Add(player.Steam64, player.LifeCounter);

            if (Ammo <= 0 && Regions.tryGetCoordinate(drop.model.position, out byte x, out byte y))
            {
                BarricadeManager.destroyBarricade(drop, x, y, ushort.MaxValue);
            }
        }
    }
}
