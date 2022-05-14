using Rocket.API;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Interfaces;
using UnityEngine;

namespace Uncreated.Warfare.Commands
{
    public class ZoneCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "zone";
        public string Help => "Zone utility commands.";
        public string Syntax => "/zone <visualize|go|list>";
        private readonly List<string> _aliases = new List<string>(0);
        public List<string> Aliases => _aliases;
        private readonly List<string> _permissions = new List<string>(1) { "uc.zone" };
		public List<string> Permissions => _permissions;
        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (caller is not UnturnedPlayer ucplayer) return;
            UCPlayer? player = UCPlayer.FromUnturnedPlayer(ucplayer);
            if (player == null) return;
            if (command.Length == 0)
            {
                player.SendChat("zone_syntax");
                return;
            }
            string operation = command[0];
            string perm = "uc.zone." + operation.ToLower();
            if (perm == "uc.zone.goto") perm = "uc.zone.go";
            if (!player.HasPermission(perm))
            {
                player.SendChat("missing_permission", perm);
            }
            if (operation.Equals("visualize", StringComparison.OrdinalIgnoreCase))
            {
                Visualize(command, player);
            }
            else if (operation.Equals("go", StringComparison.OrdinalIgnoreCase) || operation.Equals("goto", StringComparison.OrdinalIgnoreCase))
            {
                Go(command, player);
            }
            else if (operation.Equals("list", StringComparison.OrdinalIgnoreCase))
            {
                List(command, player);
            }
            else if (operation.Equals("edit", StringComparison.OrdinalIgnoreCase))
            {
                if (player.Player.TryGetComponent(out ZonePlayerComponent comp))
                    comp.EditCommand(command);
                else
                    player.SendChat("zone_syntax");
            }
            else if (operation.Equals("create", StringComparison.OrdinalIgnoreCase))
            {
                if (player.Player.TryGetComponent(out ZonePlayerComponent comp))
                    comp.CreateCommand(command);
                else
                    player.SendChat("zone_syntax");
            }
            else if (operation.Equals("util", StringComparison.OrdinalIgnoreCase))
            {
                if (player.Player.TryGetComponent(out ZonePlayerComponent comp))
                    comp.UtilCommand(command);
                else
                    player.SendChat("zone_syntax");
            }
            else
            {
                player.SendChat("zone_syntax");
                return;
            }
        }
        private void Visualize(string[] command, UCPlayer player)
        {
            Zone? zone;
            if (command.Length == 1)
            {
                Vector3 plpos = player.Position;
                if (player.Player == null) return; // player got kicked
                zone = GetZone(plpos);
            }
            else
            {
                string name = string.Join(" ", command, 1, command.Length - 1);
                zone = GetZone(name);
            }
            if (zone == null)
            {
                player.SendChat("zone_visualize_no_results");
                return;
            }
            Vector2[] points = zone.GetParticleSpawnPoints(out Vector2[] corners, out Vector2 center);
            CSteamID channel = player.Player.channel.owner.playerID.steamID;
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
            player.Player.StartCoroutine(ClearPoints(player));
            player.SendChat("zone_visualize_success", (points.Length + corners.Length + 1).ToString(Data.Locale), zone.Name);
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
        private void List(string[] command, UCPlayer player)
        {
            for (int i = 0; i < Data.ZoneProvider.Zones.Count; i++)
            {
                L.Log(Data.ZoneProvider.Zones[i].ToString(), ConsoleColor.DarkGray);
            }
        }
        private void Go(string[] command, UCPlayer player)
        {
            Zone? zone;
            if (command.Length == 1)
            {
                Vector3 plpos = player.Position;
                if (player.Player == null) return; // player got kicked
                zone = GetZone(plpos);
            }
            else
            {
                string name = string.Join(" ", command, 1, command.Length - 1);
                zone = GetZone(name);
            }
            if (zone == null)
            {
                player.SendChat("zone_go_no_results");
                return;
            }
            if (Physics.Raycast(new Ray(new Vector3(zone.Center.x, Level.HEIGHT, zone.Center.y), Vector3.down), out RaycastHit hit, Level.HEIGHT, RayMasks.BLOCK_COLLISION))
            {
                player.Player.teleportToLocationUnsafe(hit.point, 0);
                player.SendChat("zone_go_success", zone.Name);
                ActionLog.Add(EActionLogType.TELEPORT, zone.Name.ToUpper(), player.Steam64);
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
}