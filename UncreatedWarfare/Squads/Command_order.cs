using SDG.Unturned;
using System;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Squads;
using UnityEngine;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare.Commands;
public class OrderCommand : Command
{
    private const string SYNTAX = "/order <squad> <type>";
    private const string HELP = "Gives a squad orders to fulfill relating to your current marker.";
    private const string ACTIONS = "<b>attack</b>, <b>defend</b>, <b>buildfob</b>, <b>move</b>";

    public OrderCommand() : base("order", EAdminType.MEMBER) { }

    public override void Execute(CommandInteraction ctx)
    {
#if RELEASE
        throw ctx.SendNotImplemented();
#else
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertRanByPlayer();

        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        ctx.AssertGamemode<ISquads>();

        ctx.AssertArgs(1, "order_usage_1");

        if (ctx.MatchParameter(0, "actions"))
            throw ctx.Reply("order_actions");

        string squadName = ctx.Get(0)!;

        if (ctx.Caller.Squad == null || ctx.Caller.Squad.Leader.Steam64 != ctx.Caller.Steam64)
            throw ctx.Reply("order_e_not_squadleader");

        if (SquadManager.FindSquad(squadName, ctx.Caller.GetTeam(), out Squad squad))
        {
            ctx.AssertArgs(2, "order_usage_3", squad.Name);

            if (ctx.TryGet(1, out EOrder type))
            {
                if (Orders.HasOrder(squad, out Order order) && order.Commander != ctx.Caller)
                {
                    // TODO: check if order can be overwritten
                    throw ctx.Reply("order_e_alreadyhasorder", squad.Name, order.Commander.CharacterName);
                }
                else
                {
                    Vector3 playerMarker = ctx.Caller.Player.quests.markerPosition;

                    if (!ctx.Caller.Player.quests.isMarkerPlaced)
                        goto markerError;

                    ulong team = ctx.Caller.GetTeam();
                    if (Physics.Raycast(new Vector3(playerMarker.x, Level.HEIGHT, playerMarker.z), new Vector3(0f, -1, 0f), out RaycastHit hit, Level.HEIGHT, RayMasks.BLOCK_COLLISION))
                    {
                        Vector3 marker = hit.point;
                        string message;
                        string[] formatting;
                        switch (type)
                        {
                            case EOrder.ATTACK or EOrder.DEFEND:
                                ctx.LogAction(EActionLogType.CREATED_ORDER, Localization.TranslateEnum(type, 0) + " " + marker.ToString("N2"));
                                switch (Data.Gamemode)
                                {
                                    case null:
                                        throw ctx.SendGamemodeError();
                                    case IFlagRotation rot:
                                        Vector2 mkr = new Vector2(marker.x, marker.z);
                                        Flag flag = rot.Rotation.Find(f => f.ZoneData.IsInside(mkr));
                                        if (flag is null)
                                            goto default;
                                        formatting = new string[] { flag.ShortName.Colorize(flag.TeamSpecificHexColor) };
                                        if (type is EOrder.ATTACK)
                                        {
                                            if (flag.IsAttackSite(team))
                                                message = "order_attack_objective";
                                            else
                                                message = "order_attack_flag";
                                        }
                                        else
                                        {
                                            if (flag.IsDefenseSite(team))
                                                message = "order_defend_objective";
                                            else
                                                message = "order_defend_flag";
                                        }
                                        break;
                                    case Insurgency ins:
                                        float sqrDst = float.NaN;
                                        int ind = -1;
                                        if ((type is EOrder.ATTACK && team != ins.AttackingTeam) || type is EOrder.DEFEND && team != ins.DefendingTeam)
                                            goto default;
                                        for (int i = 0; i < ins.Caches.Count; ++i)
                                        {
                                            float sq2 = (ins.Caches[i].Cache.Position - marker).sqrMagnitude;
                                            if (type is EOrder.ATTACK ? ins.Caches[i].IsDiscovered : ins.Caches[i].IsActive && (float.IsNaN(sqrDst) || sq2 < sqrDst))
                                            {
                                                ind = i;
                                                sqrDst = sq2;
                                            }
                                        }

                                        if (ind == -1 || sqrDst < 22500) // 150 m
                                            goto default;
                                        else
                                        {
                                            formatting = new string[] { ins.Caches[ind].Cache.Name };
                                            if (type is EOrder.ATTACK)
                                                message = "order_attack_cache";
                                            else
                                                message = "order_defend_cache";
                                        }
                                        break;
                                    default:
                                        formatting = new string[] { new GridLocation(marker).ToString() };
                                        if (type is EOrder.ATTACK)
                                            message = "order_attack_area";
                                        else
                                            message = "order_defend_area";
                                        break;
                                }
                                ctx.LogAction(EActionLogType.CREATED_ORDER, (type is EOrder.ATTACK ? "ATTACK" : "DEFEND") + " AT " + marker.ToString("N2"));
                                break;
                            case EOrder.BUILDFOB:
                                ctx.AssertGamemode<IFOBs>();

                                if (FOB.GetNearestFOB(marker, EFOBRadius.FOB_PLACEMENT, team) != null)
                                    throw ctx.Reply("order_e_buildfob_fobexists");
                                else if (FOB.GetFOBs(team).Count >= FOBManager.Config.FobLimit)
                                    throw ctx.Reply("order_e_buildfob_foblimit");
                                else
                                {
                                    switch (Data.Gamemode)
                                    {
                                        case null:
                                            throw ctx.SendGamemodeError();
                                        case IFlagRotation rot:
                                            Vector2 mkr = new Vector2(marker.x, marker.z);
                                            Flag flag = rot.Rotation.Find(f => f.ZoneData.IsInside(mkr));
                                            // flag is not discovered or is taken
                                            if (flag is null || !flag.Discovered(team) || flag.IsFull(Teams.TeamManager.Other(team)))
                                                goto default;
                                            formatting = new string[] { flag.ShortName.Colorize(flag.TeamSpecificHexColor) };
                                            message = "order_buildfob_flag";
                                            break;
                                        case Insurgency ins:
                                            float sqrDst = float.NaN;
                                            int ind = -1;
                                            for (int i = 0; i < ins.Caches.Count; ++i)
                                            {
                                                float sq2 = (ins.Caches[i].Cache.Position - marker).sqrMagnitude;
                                                if (team == ins.AttackingTeam ? ins.Caches[i].IsDiscovered : ins.Caches[i].IsActive && (float.IsNaN(sqrDst) || sq2 < sqrDst))
                                                {
                                                    ind = i;
                                                    sqrDst = sq2;
                                                }
                                            }

                                            if (ind == -1 || sqrDst < 22500) // 150 m
                                                goto default;
                                            else
                                            {
                                                formatting = new string[] { ins.Caches[ind].Cache.Name };
                                                message = "order_buildfob_cache";
                                            }
                                            break;
                                        default:
                                            formatting = new string[] { new GridLocation(marker).ToString() };
                                            message = "order_buildfob_area";
                                            break;
                                    }

                                    ctx.LogAction(EActionLogType.CREATED_ORDER, "BUILD A FOB AT " + marker.ToString("N2"));
                                }
                                break;
                            case EOrder.MOVE:
                                Vector3 avgMemberPoint = Vector3.zero;
                                foreach (var member in squad.Members)
                                    avgMemberPoint += member.Position;

                                avgMemberPoint /= squad.Members.Count;
                                avgMemberPoint.y = marker.y;
                                float distanceToMarker = (avgMemberPoint - marker).sqrMagnitude;

                                if (distanceToMarker >= 10000)
                                {
                                    switch (Data.Gamemode)
                                    {
                                        case null:
                                            throw ctx.SendGamemodeError();
                                        case IFlagRotation rot:
                                            Vector2 mkr = new Vector2(marker.x, marker.z);
                                            Flag flag = rot.Rotation.Find(f => f.ZoneData.IsInside(mkr));
                                            // flag is not discovered or is taken
                                            if (flag is null || !flag.Discovered(team))
                                                goto default;
                                            formatting = new string[] { flag.ShortName.Colorize(flag.TeamSpecificHexColor) };
                                            message = "order_move_flag";
                                            break;
                                        case Insurgency ins:
                                            float sqrDst = float.NaN;
                                            int ind = -1;
                                            for (int i = 0; i < ins.Caches.Count; ++i)
                                            {
                                                float sq2 = (ins.Caches[i].Cache.Position - marker).sqrMagnitude;
                                                if (team == ins.AttackingTeam ? ins.Caches[i].IsDiscovered : ins.Caches[i].IsActive && (float.IsNaN(sqrDst) || sq2 < sqrDst))
                                                {
                                                    ind = i;
                                                    sqrDst = sq2;
                                                }
                                            }

                                            if (ind == -1 || sqrDst < 22500) // 150 m
                                                goto default;
                                            else
                                            {
                                                formatting = new string[] { ins.Caches[ind].Cache.Name };
                                                message = "order_move_cache";
                                            }
                                            break;
                                        default:
                                            formatting = new string[] { new GridLocation(marker).ToString() };
                                            message = "order_move_area";
                                            break;
                                    }
                                    formatting = new string[] { new GridLocation(marker).ToString() };
                                    message = "order_move_squad";

                                    ctx.LogAction(EActionLogType.CREATED_ORDER, "MOVE TO " + marker.ToString("N2"));

                                }
                                else throw ctx.Reply("order_e_squadtooclose", squad.Name);
                                break;
                            default:
                                throw ctx.SendUnknownError();
                        }
                        Orders.GiveOrder(squad, ctx.Caller, type, marker, message, formatting);
                    }
                    else goto markerError;
                }
                return;
            markerError:
                throw ctx.Reply(type switch
                {
                    EOrder.ATTACK => Data.Is<Insurgency>() ? "order_e_attack_marker_ins" : "order_e_attack_marker",
                    EOrder.DEFEND => Data.Is<Insurgency>() ? "order_e_defend_marker_ins" : "order_e_defend_marker",
                    EOrder.BUILDFOB => "order_e_buildfob_marker",
                    EOrder.MOVE => "order_e_move_marker",
                    _ => "unknown_error"
                });
            }
            else
                ctx.Reply("order_e_actioninvalid", ctx.Get(1)!, ACTIONS);
        }
        else
        {
            if (ctx.HasArgsExact(1))
                ctx.Reply("order_usage_2", "squad_name");
            else
                ctx.Reply("order_e_squadnoexist", ctx.Get(0)!);
        }
#endif
    }
}