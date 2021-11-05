using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.FOBs;
using UnityEngine;

namespace Uncreated.Warfare.Components
{
    public class FOBBaseComponent : MonoBehaviour
    {
        public BarricadeDrop drop;
        public SDG.Unturned.BarricadeData data;
        public List<UCPlayer> nearbyPlayers;

        public void Initialize(BarricadeDrop drop, SDG.Unturned.BarricadeData data)
        {
            this.drop = drop;
            this.data = data;

            nearbyPlayers = new List<UCPlayer>();

            StartCoroutine(Loop());
        }

        public void OnDestroyed()
        {
            var FOBs = FOBManager.GetFriendlyFOBs(data.group);

            foreach (var p in nearbyPlayers)
            {
                var NearbyFOBs = FOBs.Where(f => f.nearbyPlayers.Contains(p));

                if (NearbyFOBs.Count() == 0)
                {
                    EffectManager.askEffectClearByID(FOBManager.config.Data.BuildResourceUI, p.connection);
                }
                else
                {
                    FOBManager.UpdateBuildUI(p);
                }
            }
        }

        private IEnumerator<WaitForSeconds> Loop()
        {
            while (!data.barricade.isDead)
            {
                for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
                {
                    if (PlayerManager.OnlinePlayers[i].GetTeam() == data.group &&
                        !PlayerManager.OnlinePlayers[i].Player.life.isDead &&
                        PlayerManager.OnlinePlayers[i].Player.transform != null)
                    {
                        if ((PlayerManager.OnlinePlayers[i].Position - drop.model.position).sqrMagnitude < Math.Pow(30, 2))
                        {
                            if (!nearbyPlayers.Contains(PlayerManager.OnlinePlayers[i]))
                            {
                                nearbyPlayers.Add(PlayerManager.OnlinePlayers[i]);

                                IEnumerable<BarricadeDrop> TotalFOBs = UCBarricadeManager.GetAllFobs().Where(f => f.GetServersideData().group == data.group);
                                IEnumerable<BarricadeDrop> NearbyFOBs = UCBarricadeManager.GetNearbyBarricades(TotalFOBs, 30, drop.model.position, true);

                                if (NearbyFOBs.Count() == 0)
                                {
                                    EffectManager.sendUIEffect(FOBManager.config.Data.BuildResourceUI, (short)unchecked(FOBManager.config.Data.BuildResourceUI), PlayerManager.OnlinePlayers[i].Player.channel.owner.transportConnection, true);
                                }

                                FOBManager.UpdateBuildUI(PlayerManager.OnlinePlayers[i]);
                            }
                        }
                        else
                        {
                            if (nearbyPlayers.Contains(PlayerManager.OnlinePlayers[i]))
                            {
                                nearbyPlayers.Remove(PlayerManager.OnlinePlayers[i]);

                                IEnumerable<BarricadeDrop> TotalFOBs = UCBarricadeManager.GetAllFobs().Where(f => f.GetServersideData().group == data.group);
                                IEnumerable<BarricadeDrop> NearbyFOBs = UCBarricadeManager.GetNearbyBarricades(TotalFOBs, 30, drop.model.position, true);

                                if (NearbyFOBs.Count() == 0)
                                {
                                    EffectManager.askEffectClearByID(FOBManager.config.Data.BuildResourceUI, PlayerManager.OnlinePlayers[i].Player.channel.owner.transportConnection);
                                }
                            }
                        }
                    }
                }

                yield return new WaitForSeconds(1);
            }
        }
    }
}
