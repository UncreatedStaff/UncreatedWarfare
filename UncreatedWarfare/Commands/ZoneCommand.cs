using SDG.NetTransport;
using SDG.Unturned;
using System.Collections.Generic;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Locations;
using UnityEngine;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class ZeCommand : Command
{
    private const string SYNTAX = "/ze <existing|maxheight|minheight|finalize|cancel|addpoint|delpoint|clearpoints|setpoint|orderpoint|radius|sizex|sizez|center|name|shortname|type> [value]";
    private const string HELP = "Shortcut for /zone edit.";

    public ZeCommand() : base("ze", EAdminType.MODERATOR)
    {
        Structure = new CommandStructure
        {
            Description = HELP,
            Parameters = new CommandParameter[]
            {
                new CommandParameter("Parameter", typeof(string))
                {
                    Aliases = ZonePlayerComponent.EditCommands,
                    ChainDisplayCount = 2,
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Value", typeof(object))
                        {
                            IsRemainder = true
                        }
                    }
                }
            }
        };
    }

    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        ctx.AssertRanByPlayer();

        ctx.AssertOnDuty();

        ctx.AssertArgs(1, "edit_zone_syntax");

        if (ctx.Caller.Player.TryGetComponent(out ZonePlayerComponent comp))
            comp.EditCommand(ctx);
        else
            ctx.SendUnknownError();
    }
}

public class ZoneCommand : Command
{
    private const string SYNTAX = "/zone <visualize|go|delete|create|util>";
    private const string HELP = "Manage zones.";

    public ZoneCommand() : base("zone", EAdminType.MEMBER)
    {
        Structure = new CommandStructure
        {
            Description = HELP,
            Parameters = new CommandParameter[]
            {
                new CommandParameter("Visualize")
                {
                    Description = "Spawns particles highlighting the zone border.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Zone", typeof(Zone))
                    }
                },
                new CommandParameter("Go")
                {
                    Description = "Teleport to the spawn of a zone.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Zone", typeof(Zone))
                    }
                },
                new CommandParameter("Delete")
                {
                    Description = "Deletes a zone.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Zone", typeof(Zone))
                    }
                },
                new CommandParameter("Edit")
                {
                    Description = "Modify a zone.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Parameter", typeof(string))
                        {
                            Aliases = ZonePlayerComponent.EditCommands,
                            ChainDisplayCount = 2,
                            Parameters = new CommandParameter[]
                            {
                                new CommandParameter("Value", typeof(object))
                                {
                                    IsRemainder = true
                                }
                            }
                        }
                    }
                },
                new CommandParameter("Create")
                {
                    Description = "Starts creating a zone.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Type", "Rectangle", "Polygon", "Circle")
                        {
                            Parameters = new CommandParameter[]
                            {
                                new CommandParameter("Name", typeof(string))
                            }
                        }
                    }
                },
                new CommandParameter("Util")
                {
                    Description = "Random zone utilities.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Location")
                        {
                            Description = "Responds with the player's coordinates and yaw."
                        }
                    }
                }
            }
        };
    }

    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        ctx.AssertArgs(1, "zone_syntax");

        if (ctx.MatchParameter(0, "visualize"))
        {
            ctx.AssertRanByPlayer();

            ctx.Offset = 1;
            Visualize(ctx);
            ctx.Offset = 0;
        }
        else if (ctx.MatchParameter(0, "go", "tp", "goto", "teleport"))
        {
            ctx.AssertRanByPlayer();

            ctx.AssertOnDuty();

            ctx.Offset = 1;
            Go(ctx);
            ctx.Offset = 0;
        }
        else if (ctx.MatchParameter(0, "edit", "e"))
        {
            ctx.AssertRanByPlayer();

            ctx.AssertOnDuty();

            if (ctx.Caller.Player.TryGetComponent(out ZonePlayerComponent comp))
            {
                ctx.Offset = 1;
                try
                {
                    comp.EditCommand(ctx);
                }
                finally { ctx.Offset = 0; }
            }
            else
                throw ctx.SendUnknownError();
        }
        else if (ctx.MatchParameter(0, "create", "c"))
        {
            ctx.AssertRanByPlayer();

            ctx.AssertOnDuty();

            if (ctx.Caller.Player.TryGetComponent(out ZonePlayerComponent comp))
            {
                ctx.Offset = 1;
                try
                {
                    comp.CreateCommand(ctx);
                }
                finally { ctx.Offset = 0; }
            }
            else
                throw ctx.SendUnknownError();
        }
        else if (ctx.MatchParameter(0, "delete", "remove", "d"))
        {
            ctx.AssertRanByPlayer();

            ctx.AssertOnDuty();

            if (ctx.Caller.Player.TryGetComponent(out ZonePlayerComponent comp))
            {
                ctx.Offset = 1;
                try
                {
                    comp.DeleteCommand(ctx);
                }
                finally { ctx.Offset = 0; }
            }
            else
                throw ctx.SendUnknownError();
        }
        else if (ctx.MatchParameter(0, "util", "u", "tools"))
        {
            ctx.AssertRanByPlayer();

            ctx.AssertOnDuty();

            if (ctx.Caller.Player.TryGetComponent(out ZonePlayerComponent comp))
            {
                ctx.Offset = 1;
                try
                {
                    comp.UtilCommand(ctx);
                }
                finally { ctx.Offset = 0; }
            }
            else
                throw ctx.SendUnknownError();
        }
        else throw ctx.SendCorrectUsage(SYNTAX);
    }
    private void Visualize(CommandInteraction ctx)
    {
        Zone? zone;
        if (ctx.TryGetRange(0, out string zname))
            zone = GetZone(zname);
        else
        {
            Vector3 plpos = ctx.Caller.Position;
            if (!ctx.Caller.IsOnline) return; // player got kicked
            zone = GetZone(plpos);
        }

        if (zone == null) throw ctx.Reply(T.ZoneNoResults);

        Vector2[] points = zone.GetParticleSpawnPoints(out Vector2[] corners, out Vector2 center);
        ITransportConnection channel = ctx.Caller.Player.channel.owner.transportConnection;
        bool hasui = ZonePlayerComponent.Airdrop != null;
        foreach (Vector2 point in points)
        {   // Border
            Vector3 pos = new Vector3(point.x, 0f, point.y);
            pos.y = F.GetHeight(pos, zone.MinHeight);
            F.TriggerEffectReliable(ZonePlayerComponent.Side, channel, pos);
            if (hasui)
                F.TriggerEffectReliable(ZonePlayerComponent.Airdrop!, channel, pos);
        }
        foreach (Vector2 point in corners)
        {   // Corners
            Vector3 pos = new Vector3(point.x, 0f, point.y);
            pos.y = F.GetHeight(pos, zone.MinHeight);
            F.TriggerEffectReliable(ZonePlayerComponent.Corner, channel, pos);
            if (hasui)
                F.TriggerEffectReliable(ZonePlayerComponent.Airdrop!, channel, pos);
        }
        {   // Center
            Vector3 pos = new Vector3(center.x, 0f, center.y);
            pos.y = F.GetHeight(pos, zone.MinHeight);
            F.TriggerEffectReliable(ZonePlayerComponent.Center, channel, pos);
            if (hasui)
                F.TriggerEffectReliable(ZonePlayerComponent.Airdrop!, channel, pos);
        }
        ctx.Caller.Player.StartCoroutine(ClearPoints(ctx.Caller));
        ctx.Reply(T.ZoneVisualizeSuccess, points.Length + corners.Length + 1, zone);
    }
    private IEnumerator<WaitForSeconds> ClearPoints(UCPlayer player)
    {
        yield return new WaitForSeconds(60f);
        if (player == null) yield break;
        ITransportConnection channel = player.Player.channel.owner.transportConnection;
        if (ZonePlayerComponent.Airdrop != null)
            EffectManager.askEffectClearByID(ZonePlayerComponent.Airdrop.id, channel);
        EffectManager.askEffectClearByID(ZonePlayerComponent.Side.id, channel);
        EffectManager.askEffectClearByID(ZonePlayerComponent.Corner.id, channel);
        EffectManager.askEffectClearByID(ZonePlayerComponent.Center.id, channel);
    }
    private void Go(CommandInteraction ctx)
    {
        Zone? zone;
        if (ctx.TryGetRange(0, out string zname))
            zone = GetZone(zname);
        else
        {
            Vector3 plpos = ctx.Caller.Position;
            if (!ctx.Caller.IsOnline) return; // player got kicked
            zone = GetZone(plpos);
        }

        Vector2 pos;
        GridLocation location = default;
        if (zone == null)
        {
            if (GridLocation.TryParse(zname, out location))
            {
                pos = location.Center;
            }
            else throw ctx.Reply(T.ZoneNoResultsName);
        }
        else pos = zone.Center;


        if (Physics.Raycast(new Ray(new Vector3(pos.x, Level.HEIGHT, pos.y), Vector3.down), out RaycastHit hit, Level.HEIGHT, RayMasks.BLOCK_COLLISION))
        {
            ctx.Caller.Player.teleportToLocationUnsafe(hit.point, 0);
            if (zone != null)
                ctx.Reply(T.ZoneGoSuccess, zone);
            else
                ctx.Reply(T.ZoneGoSuccessGridLocation, location);
            ctx.LogAction(ActionLogType.Teleport, zone == null ? location.ToString() : zone.Name.ToUpper());
        }
        else
        {
            ctx.SendUnknownError();
            L.LogWarning("Tried to teleport to " + (zone == null ? location.ToString() : zone.Name.ToUpper()) + " and there was no terrain to teleport to at " + pos + ".");
        }
    }

    internal static Zone? GetZone(string nameInput) => GetZone(Data.Singletons.GetSingleton<ZoneList>()!, nameInput);
    internal static Zone? GetZone(ZoneList singleton, string nameInput) => singleton?.SearchZone(nameInput)?.Item;
    internal static Zone? GetZone(Vector3 position) => GetZone(Data.Singletons.GetSingleton<ZoneList>()!, position);
    internal static Zone? GetZone(ZoneList singleton, Vector3 position)
    {
        return singleton == null ? null : 
           (singleton.FindInsizeZone(position, false) is { Item: { } z } ? z :
            singleton.FindInsizeZone(new Vector2(position.x, position.z), false)?.Item);
    }
}