using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UncreatedWarfare.Flags;
using SDG.NetTransport;

namespace UncreatedWarfare
{
    partial class UCWarfare
    {
        internal List<IEnumerator<WaitForSeconds>> Coroutines;
        public void StartAllCoroutines()
        {
            foreach(IEnumerator<WaitForSeconds> coroutine in Coroutines)
            {
                StartCoroutine(coroutine);
            }
        }
        internal IEnumerator<WaitForSeconds> CheckPlayers()
        {

            while(Instance.State == Rocket.API.PluginState.Loaded)
            {
                List<SteamPlayer> OnlinePlayers = Provider.clients;
                foreach(Flags.Flag flag in FlagManager.FlagRotation)
                {
                    List<SteamPlayer> Cappers = OnlinePlayers.Where(player => flag.PlayerInRange(player)).ToList();

                    List<SteamPlayer> Team1Cappers = Cappers.Where(player => player.player.quests.groupID.m_SteamID == T1.ID).ToList();
                    int Team1TotalPlayers = Team1Cappers.Count;
                    List<SteamPlayer> Team2Cappers = Cappers.Where(player => player.player.quests.groupID.m_SteamID == T2.ID).ToList();
                    int Team2TotalPlayers = Team2Cappers.Count;
                    foreach(SteamPlayer player in OnlinePlayers)
                    {
                        ITransportConnection Channel = player.player.channel.owner.transportConnection;
                        if(flag.PlayerInRange(player))
                        {
                            if(!FlagManager.OnFlag.ContainsKey(player.playerID.steamID.m_SteamID))
                            {
                                FlagManager.AddPlayerOnFlag(player.player, flag);
                                player.SendChat("entered_cap_radius", Colors["entered_cap_radius"], flag.Name, flag.Color);
                                F.UIOrChat(player.GetTeam(), F.UIOption.Blank, "", Colors["default"], Channel, player, 0, false, true); // LEFT OFF HERE COME BACK TO HERE
                                if(flag.ID == FlagManager.FlagRotation[FlagManager.ObjectiveT1].ID && player.GetTeam() == 1)
                                {

                                } else if (flag.ID == FlagManager.FlagRotation[FlagManager.ObjectiveT2].ID && player.GetTeam() == 2)
                                {

                                } else
                                {

                                }
                            }
                        }
                    }
                }
                yield return new WaitForSeconds(Config.FlagSettings.PlayerCheckSpeedSeconds);
            }
        }
    }
}
