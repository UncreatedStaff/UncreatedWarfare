using System;
using System.Globalization;
using System.Linq;
using SDG.Unturned;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using UnityEngine;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare.FOBs;

public class SpecialFOB : IFOB, IGameTickListener
{
    private readonly string _name;
    private readonly string _cl;
    private readonly GridLocation _gc;
    private readonly Vector3 _pos;
    public string UIColor;
    public bool IsActive;
    public bool DisappearAroundEnemies;
    public string Name => _name;
    public Vector3 SpawnPosition => _pos;
    public Vector3 Position => _pos;
    float IDeployable.Yaw => 0f;
    public string ClosestLocation => _cl;
    public GridLocation GridLocation => _gc;
    UCPlayer? IFOB.Instigator { get; set; }
    public ulong Team { get; set; }
    public bool ContainsBuildable(IBuildable buildable) => false;
    public bool ContainsVehicle(InteractableVehicle vehicle) => false;

    public SpecialFOB(string name, Vector3 point, ulong team, string color, bool disappearAroundEnemies)
    {
        _name = name;
        _cl = F.GetClosestLocationName(point);

        if (Data.Is(out IFlagRotation fg))
        {
            Flag flag = fg.LoadedFlags.Find(f => f.Name.Equals(ClosestLocation, StringComparison.OrdinalIgnoreCase));
            if (flag is not null)
                _cl = flag.ShortName;
        }

        Team = team;
        _pos = point;
        _gc = new GridLocation(in point);
        UIColor = color;
        IsActive = true;
        DisappearAroundEnemies = disappearAroundEnemies;
    }

    void IGameTickListener.Tick()
    {
        if (DisappearAroundEnemies && Data.Gamemode.EveryXSeconds(5f) && Data.Is(out IFOBs fobs))
        {
            Vector3 pos = SpawnPosition;
            for (int i = 0; i < Provider.clients.Count; ++i)
            {
                SteamPlayer pl = Provider.clients[i];
                if (pl.GetTeam() != Team && (pl.player.transform.position - pos).sqrMagnitude < 70 * 70)
                {
                    L.LogDebug($"[FOBS] [{Name}] Deleting FOB because of nearby enemies.", ConsoleColor.Green);
                    fobs.FOBManager.DeleteFOB(this);
                    return;
                }
            }
        }
    }

    string ITranslationArgument.Translate(LanguageInfo language, string? format, UCPlayer? target, CultureInfo? culture,
        ref TranslationFlags flags)
    {
        if (format is not null)
        {
            if (format.Equals(FOB.FormatNameColored, StringComparison.Ordinal))
                return Localization.Colorize(UIColor ?? TeamManager.GetTeamHexColor(Team), Name, flags);
            if (format.Equals(FOB.FormatLocationName, StringComparison.Ordinal))
                return ClosestLocation;
            if (format.Equals(FOB.FormatGridLocation, StringComparison.Ordinal))
                return GridLocation.ToString();
        }
        return Name;
    }
    bool IDeployable.CheckDeployable(UCPlayer player, CommandInteraction? ctx)
    {
        ulong team = player.GetTeam();
        if (team == 0 || team != Team)
        {
            if (ctx is not null)
                throw ctx.Reply(T.DeployableNotFound, Name);
            return false;
        }
        if (IsActive)
            return true;
        if (ctx is not null)
            throw ctx.Reply(T.DeployNotSpawnable, this);
        return false;
    }
    bool IDeployable.CheckDeployableTick(UCPlayer player, bool chat)
    {
        ulong team = player.GetTeam();
        if (team == 0 || team != Team)
        {
            if (chat)
                player.SendChat(T.DeployCancelled);
            return false;
        }
        if (IsActive)
            return true;
        if (chat)
            player.SendChat(T.DeployNotSpawnableTick, this);
        return false;
    }
    void IDeployable.OnDeploy(UCPlayer player, bool chat)
    {
        ActionLog.Add(ActionLogType.DeployToLocation, "SPECIAL FOB " + Name + " TEAM " + TeamManager.TranslateName(Team), player);
        if (chat)
            player.SendChat(T.DeploySuccess, this);
    }

    float IDeployable.GetDelay() => FOBManager.Config.DeployFOBDelay;

    public void Dump(UCPlayer? target)
    {
        L.Log($"[FOBS] [{Name}] === Special Fob Dump ===");
        L.Log($" Grid Location: {GridLocation}, Closest Location: {ClosestLocation}.");
        L.Log($" Color: {UIColor}.");
        L.Log($" Active: {IsActive}, Disappear Around Enemies: {DisappearAroundEnemies}.");
        L.Log($" Team: {Team}.");
    }
}