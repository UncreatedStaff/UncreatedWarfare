using Rocket.API;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Interfaces;
using UnityEngine;

namespace Uncreated.Warfare.Squads
{
    public class Command_order : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "order";
        public string Help => "Gives a squad orders to fulfill";
        public string Syntax => "/order";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string>() { "uc.order" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UCPlayer player = UCPlayer.FromIRocketPlayer(caller);
            if (!Data.Is(out ISquads ctf))
            {
                player.SendChat("command_e_gamemode");
                return;
            }


            if (command.Length == 0)
            {
                player.Message("order_usage_1");
            }
            else if (command.Length >= 1)
            {
                string actions = "<b>attack</b>, <b>defend</b>, <b>buildfob</b>, <b>move</b>";

                if (command[0].ToLower() == "actions")
                    player.Message("order_actions", actions);

                bool squadExists = SquadManager.FindSquad(command[0], player.GetTeam(), out var squad);

                if (squadExists)
                {
                    if (command.Length == 1)
                        player.Message("order_usage_3", squad.Name);
                    else
                    {
                        if (Enum.TryParse(command[1].ToUpper(), out EOrder type))
                        {
                            if (Orders.HasOrder(squad, out var order) && order.Commander != player)
                            {
                                // TODO: check if order can be overwritten
                                player.Message("order_e_alreadyhasorder", squad.Name, order.Commander.CharacterName);
                            }
                            else
                            {
                                Vector3 playerMarker = player.Player.quests.markerPosition;

                                if (player.Player.quests.isMarkerPlaced)
                                {
                                    if (Physics.Raycast(new Vector3(playerMarker.x, Level.HEIGHT, playerMarker.z), new Vector3(0f, -1, 0f), out RaycastHit hit, Level.HEIGHT, RayMasks.BLOCK_COLLISION))
                                    {
                                        Vector3 marker = hit.point;
                                        string message = "";

                                        if (type == EOrder.ATTACK || type == EOrder.DEFEND)
                                        {
                                            if (Data.Is(out FlagGamemode flags))
                                            {
                                                var flag = flags.Rotation.Find(f => f.ZoneData.IsInside(new Vector2(marker.x, marker.z)));
                                                bool useFlag = false;

                                                if (flag != null && player.Player.quests.isMarkerPlaced)
                                                {
                                                    if (type == EOrder.ATTACK && flag.IsAttackable(player.GetTeam()))
                                                    {
                                                        useFlag = true;
                                                    }
                                                    if (type == EOrder.DEFEND && flag.IsAttackable(player.GetTeam()))
                                                    {
                                                        useFlag = true;
                                                    }
                                                    
                                                }

                                            }

                                            Orders.GiveOrder(squad, player, type, marker, message);
                                        }
                                        if (type == EOrder.BUILDFOB)
                                        {
                                            if (FOB.GetNearestFOB(marker, EFOBRadius.FOB_PLACEMENT, player.GetTeam()) != null)
                                                player.Message("order_e_buildfob_fobexists");
                                            else if (FOB.GetFOBs(player.GetTeam()).Count >= FOBManager.config.Data.FobLimit)
                                                player.Message("order_e_buildfob_foblimit");
                                            else
                                            {
                                                message = $"Build FOB near the marker";
                                                Orders.GiveOrder(squad, player, type, marker, message);
                                            }
                                        }
                                        if (type == EOrder.MOVE)
                                        {
                                            Vector3 avgMemberPoint = Vector3.zero;
                                            foreach (var member in squad.Members)
                                                avgMemberPoint += member.Position;

                                            avgMemberPoint /= squad.Members.Count;
                                            float distanceToMarker = (avgMemberPoint - marker).magnitude;

                                            if (distanceToMarker >= 100)
                                            {
                                                message = $"Move squad to the marker position";
                                                Orders.GiveOrder(squad, player, type, marker, message);
                                            }
                                            else
                                                player.Message("order_e_squadtooclose", squad.Name);
                                        }
                                    }
                                    else
                                        player.Message("order_e_raycast");
                                }
                                else
                                {
                                    if (type == EOrder.ATTACK) player.Message("order_e_attack_marker");
                                    else if (type == EOrder.DEFEND) player.Message("order_e_defend_marker");
                                    else if (type == EOrder.BUILDFOB) player.Message("order_e_buildfob_marker");
                                    else if (type == EOrder.MOVE) player.Message("order_e_move_marker");
                                }
                            }
                        }
                        else
                            player.Message("order_e_actioninvalid", command[1], actions);
                    }
                }
                else
                {
                    if (command.Length == 1)
                        player.Message("order_usage_2", "squad_name");
                    else
                        player.Message("order_e_squadnoexist", command[0].ToUpper());
                }

            }
        }
    }
}