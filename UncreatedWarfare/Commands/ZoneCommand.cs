using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Interfaces;
using UnityEngine;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class ZeCommand : Command
{
    private const string SYNTAX = "/ze <existing|maxheight|minheight|finalize|cancel|addpoint|delpoint|clearpoints|setpoint|orderpoint|radius|sizex|sizez|center|name|shortname|type> [value]";
    private const string HELP = "Shortcut for /zone edit.";

    public ZeCommand() : base("ze", EAdminType.MODERATOR) { }

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
    private const string SYNTAX = "/zone <visualize|go|list|delete|create|util>";
    private const string HELP = "Shortcut for /zone edit.";

    public ZoneCommand() : base("zone", EAdminType.MEMBER) { }

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
        else if (ctx.MatchParameter(0, "list"))
        {
            ctx.AssertRanByConsole();

            ctx.Offset = 1;
            List(ctx);
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
        CSteamID channel = ctx.Caller.Player.channel.owner.playerID.steamID;
        bool hasui = ZonePlayerComponent._airdrop != null;
        foreach (Vector2 point in points)
        {   // Border
            Vector3 pos = new Vector3(point.x, 0f, point.y);
            pos.y = F.GetHeight(pos, zone.MinHeight);
            F.TriggerEffectReliable(ZonePlayerComponent._side.id, channel, pos);
            if (hasui)
                F.TriggerEffectReliable(ZonePlayerComponent._airdrop!.id, channel, pos);
        }
        foreach (Vector2 point in corners)
        {   // Corners
            Vector3 pos = new Vector3(point.x, 0f, point.y);
            pos.y = F.GetHeight(pos, zone.MinHeight);
            F.TriggerEffectReliable(ZonePlayerComponent._corner.id, channel, pos);
            if (hasui)
                F.TriggerEffectReliable(ZonePlayerComponent._airdrop!.id, channel, pos);
        }
        {   // Center
            Vector3 pos = new Vector3(center.x, 0f, center.y);
            pos.y = F.GetHeight(pos, zone.MinHeight);
            F.TriggerEffectReliable(ZonePlayerComponent._center.id, channel, pos);
            if (hasui)
                F.TriggerEffectReliable(ZonePlayerComponent._airdrop!.id, channel, pos);
        }
        ctx.Caller.Player.StartCoroutine(ClearPoints(ctx.Caller));
        ctx.Reply(T.ZoneVisualizeSuccess, points.Length + corners.Length + 1, zone);
    }
    private IEnumerator<WaitForSeconds> ClearPoints(UCPlayer player)
    {
        yield return new WaitForSeconds(60f);
        if (player == null) yield break;
        ITransportConnection channel = player.Player.channel.owner.transportConnection;
        if (ZonePlayerComponent._airdrop != null)
            EffectManager.askEffectClearByID(ZonePlayerComponent._airdrop.id, channel);
        EffectManager.askEffectClearByID(ZonePlayerComponent._side.id, channel);
        EffectManager.askEffectClearByID(ZonePlayerComponent._corner.id, channel);
        EffectManager.askEffectClearByID(ZonePlayerComponent._center.id, channel);
    }
    private void List(CommandInteraction ctx)
    {
        for (int i = 0; i < Data.ZoneProvider.Zones.Count; i++)
        {
            L.Log(Data.ZoneProvider.Zones[i].ToString(), ConsoleColor.DarkGray);
        }
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

        if (zone == null) throw ctx.Reply(T.ZoneNoResultsName);

        if (Physics.Raycast(new Ray(new Vector3(zone.Center.x, Level.HEIGHT, zone.Center.y), Vector3.down), out RaycastHit hit, Level.HEIGHT, RayMasks.BLOCK_COLLISION))
        {
            ctx.Caller.Player.teleportToLocationUnsafe(hit.point, 0);
            ctx.Reply(T.ZoneGoSuccess, zone);
            ctx.LogAction(EActionLogType.TELEPORT, zone.Name.ToUpper());
        }
    }
    internal static Zone? GetZone(string nameInput)
    {
        if (int.TryParse(nameInput, System.Globalization.NumberStyles.Any, Data.Locale, out int num))
        {
            for (int i = 0; i < Data.ZoneProvider.Zones.Count; i++)
            {
                if (Data.ZoneProvider.Zones[i].Id == num)
                    return Data.ZoneProvider.Zones[i];
            }
        }
        for (int i = 0; i < Data.ZoneProvider.Zones.Count; i++)
        {                
            if (Data.ZoneProvider.Zones[i].Name.Equals(nameInput, StringComparison.OrdinalIgnoreCase))
                return Data.ZoneProvider.Zones[i];
        }
        for (int i = 0; i < Data.ZoneProvider.Zones.Count; i++)
        {                
            if (Data.ZoneProvider.Zones[i].Name.IndexOf(nameInput, StringComparison.OrdinalIgnoreCase) != -1)
                return Data.ZoneProvider.Zones[i];
        }
        if (nameInput.Equals("lobby", StringComparison.OrdinalIgnoreCase))
            return Teams.TeamManager.LobbyZone;
        if (nameInput.Equals("t1main", StringComparison.OrdinalIgnoreCase) || nameInput.Equals("t1", StringComparison.OrdinalIgnoreCase))
            return Teams.TeamManager.Team1Main;
        if (nameInput.Equals("t2main", StringComparison.OrdinalIgnoreCase) || nameInput.Equals("t2", StringComparison.OrdinalIgnoreCase))
            return Teams.TeamManager.Team2Main;
        if (nameInput.Equals("t1amc", StringComparison.OrdinalIgnoreCase))
            return Teams.TeamManager.Team1AMC;
        if (nameInput.Equals("t2amc", StringComparison.OrdinalIgnoreCase))
            return Teams.TeamManager.Team2AMC;
        if (nameInput.Equals("obj1", StringComparison.OrdinalIgnoreCase))
        {
            if (Data.Is(out IFlagTeamObjectiveGamemode gm))
            {
                Gamemodes.Flags.Flag? fl = gm.ObjectiveTeam1;
                if (fl != null)
                    return fl.ZoneData;
            }
            else if (Data.Is(out IAttackDefense atdef) && Data.Is(out IFlagRotation rot))
            {
                ulong t = atdef.DefendingTeam;
                if (t == 1)
                {
                    Gamemodes.Flags.Flag? fl = gm.ObjectiveTeam1;
                    if (fl != null)
                        return fl.ZoneData;
                }
                else
                {
                    Gamemodes.Flags.Flag? fl = gm.ObjectiveTeam2;
                    if (fl != null)
                        return fl.ZoneData;
                }
            }
        }
        else if (nameInput.Equals("obj2", StringComparison.OrdinalIgnoreCase))
        {
            if (Data.Is(out IFlagTeamObjectiveGamemode gm))
            {
                Gamemodes.Flags.Flag? fl = gm.ObjectiveTeam2;
                if (fl != null)
                    return fl.ZoneData;
            }
            else if (Data.Is(out IAttackDefense atdef) && Data.Is(out IFlagRotation rot))
            {
                ulong t = atdef.DefendingTeam;
                if (t == 1)
                {
                    Gamemodes.Flags.Flag? fl = gm.ObjectiveTeam1;
                    if (fl != null)
                        return fl.ZoneData;
                }
                else
                {
                    Gamemodes.Flags.Flag? fl = gm.ObjectiveTeam2;
                    if (fl != null)
                        return fl.ZoneData;
                }
            }
        }
        else if (nameInput.Equals("obj", StringComparison.OrdinalIgnoreCase))
        {
            if (Data.Is(out IAttackDefense atdef) && Data.Is(out IFlagTeamObjectiveGamemode rot))
            {
                ulong t = atdef.DefendingTeam;
                if (t == 1)
                {
                    Gamemodes.Flags.Flag? fl = rot.ObjectiveTeam1;
                    if (fl != null)
                        return fl.ZoneData;
                }
                else
                {
                    Gamemodes.Flags.Flag? fl = rot.ObjectiveTeam2;
                    if (fl != null)
                        return fl.ZoneData;
                }
            }
        }
        return null;
    }
    internal static Zone? GetZone(Vector3 position)
    {
        for (int i = 0; i < Data.ZoneProvider.Zones.Count; i++)
        {                
            if (Data.ZoneProvider.Zones[i].IsInside(position))
                return Data.ZoneProvider.Zones[i];
        }
        Vector2 pos2 = new Vector2(position.x, position.z);
        for (int i = 0; i < Data.ZoneProvider.Zones.Count; i++)
        {
            if (Data.ZoneProvider.Zones[i].IsInside(pos2))
                return Data.ZoneProvider.Zones[i];
        }
        return null;
    }
}