using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.TeamDeathmatch;

public class TDMGameStatsTracker : MonoBehaviour
{
    public TimeSpan Duration { get => TimeSpan.FromSeconds(durationCounter); }
    public Dictionary<ulong, TDMPlayerStats> playerstats;
    private float durationCounter = 0;
    public int casualtiesT1;
    public int casualtiesT2;
    public float averageArmySizeT1;
    public float averageArmySizeT2;
    public int fobsPlacedT1;
    public int fobsPlacedT2;
    public int fobsDestroyedT1;
    public int fobsDestroyedT2;
    public int teamkills;
    internal int gamepercentagecounter;
    public Coroutine update;
    public LongestShot LongestShot = LongestShot.Nil;
    public void Update()
    {
        durationCounter += Time.deltaTime;
    }
    public void AddPlayer(UCPlayer player)
    {
        if (!playerstats.TryGetValue(player.Steam64, out TDMPlayerStats s))
        {
            s = new TDMPlayerStats(player);
            playerstats.Add(player.Steam64, s);
            if (player.Player.TryGetPlayerData(out Components.UCPlayerData c))
                c.stats = s;
        }
        else
        {
            s.player = player;
            if (player.Player.TryGetPlayerData(out Components.UCPlayerData c))
                c.stats = s;
        }
        L.Log(player.CharacterName + " added to playerstats, " + playerstats.Count + " trackers");
    }
    public void Start() => Reset(true);
    public void Reset(bool start = false)
    {
        if (!start)
            StopCounting();
        if (playerstats == null)
            playerstats = new Dictionary<ulong, TDMPlayerStats>();
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            if (playerstats.TryGetValue(pl.Steam64, out TDMPlayerStats p))
            {
                p.player = pl;
                p.Reset();
            }
            else
            {
                TDMPlayerStats s = new TDMPlayerStats(pl);
                playerstats.Add(pl.Steam64, s);
                if (pl.Player.TryGetPlayerData(out Components.UCPlayerData pt))
                    pt.stats = s;
            }
        }
        foreach (KeyValuePair<ulong, TDMPlayerStats> p in playerstats.ToList())
        {
            SteamPlayer player = PlayerTool.getSteamPlayer(p.Key);
            if (player == null) playerstats.Remove(p.Key);
        }
        durationCounter = 0;
        casualtiesT1 = 0;
        casualtiesT2 = 0;
        averageArmySizeT1 = 0;
        averageArmySizeT2 = 0;
        gamepercentagecounter = 0;
        fobsPlacedT1 = 0;
        fobsPlacedT2 = 0;
        fobsDestroyedT1 = 0;
        fobsDestroyedT2 = 0;
        teamkills = 0;
        update = StartCoroutine(CompileAverages());
        LongestShot = LongestShot.Nil;
    }
    public void StopCounting()
    {
        if (update == null) return;
        StopCoroutine(update);
    }
    private void CompileArmyAverageT1(int newcount)
    {
        float oldArmySize = averageArmySizeT1 * gamepercentagecounter;
        averageArmySizeT1 = gamepercentagecounter == 0 ? (oldArmySize + newcount) : ((oldArmySize + newcount) / gamepercentagecounter);
    }
    private void CompileArmyAverageT2(int newcount)
    {
        float oldArmySize = averageArmySizeT2 * gamepercentagecounter;
        averageArmySizeT2 = gamepercentagecounter == 0 ? (oldArmySize + newcount) : ((oldArmySize + newcount) / gamepercentagecounter);
    }
    private IEnumerator<WaitForSeconds> CompileAverages()
    {
        while (true)
        {
            // checks for how many players are outside of main
            DateTime dt = DateTime.Now;
            CompileArmyAverageT1(Provider.clients.Count(x => x.GetTeam() == 1 && x.player.transform != null && !TeamManager.Team1Main.IsInside(x.player.transform.position)));
            CompileArmyAverageT2(Provider.clients.Count(x => x.GetTeam() == 2 && x.player.transform != null && !TeamManager.Team2Main.IsInside(x.player.transform.position)));
            //foreach (IStats s in playerstats.Values)
            //    s.CheckGame();
            gamepercentagecounter++;
            yield return new WaitForSeconds(10f);
        }
    }
    public bool TryGetPlayer(ulong id, out TDMPlayerStats stats)
    {
        if (!(playerstats == null) && playerstats.ContainsKey(id))
        {
            stats = playerstats[id];
            return true;
        }
        else
        {
            stats = null!;
            return false;
        }
    }
    public const string NO_PLAYER_NAME_PLACEHOLDER = "---";
    public const string NO_PLAYER_VALUE_PLACEHOLDER = "--";
    public void GetTopStats(int count, out List<TDMPlayerStats> statsT1, out List<TDMPlayerStats> statsT2)
    {
        List<TDMPlayerStats> stats = playerstats.Values.ToList();

        stats.RemoveAll(p =>
        {
            if (p is null) return true;
            if (p.Player is null || !p.Player.IsOnline)
            {
                p.Player = UCPlayer.FromID(p.id)!;
                return p.Player is null || !p.Player.IsOnline;
            }
            else return false;
        });

        TDMPlayerStats totalT1 = new TDMPlayerStats();
        TDMPlayerStats totalT2 = new TDMPlayerStats();
        for (int i = 0; i < playerstats.Values.Count; i++)
        {
            TDMPlayerStats stat = playerstats.Values.ElementAt(i);

            if (stat.id.GetTeamFromPlayerSteam64ID() == 1)
            {
                totalT1.kills += stat.kills;
                totalT1.deaths += stat.deaths;
                totalT1.xpgained += stat.xpgained;
                totalT1.creditsgained += stat.creditsgained;
                totalT1.captures += stat.captures;
                totalT1.damagedone += stat.damagedone;
            }
            else if (stat.id.GetTeamFromPlayerSteam64ID() == 2)
            {
                totalT2.kills += stat.kills;
                totalT2.deaths += stat.deaths;
                totalT2.xpgained += stat.xpgained;
                totalT2.creditsgained += stat.creditsgained;
                totalT2.captures += stat.captures;
                totalT2.damagedone += stat.damagedone;
            }
        }

        stats.Sort((TDMPlayerStats a, TDMPlayerStats b) => b.xpgained.CompareTo(a.xpgained));

        statsT1 = stats.Where(p => p.player.GetTeam() == 1).ToList();
        statsT2 = stats.Where(p => p.player.GetTeam() == 2).ToList();
        statsT1.Take(count);
        statsT2.Take(count);
        statsT1.Insert(0, totalT1);
        statsT2.Insert(0, totalT2);
    }
    public List<KeyValuePair<Player, char>> GetTopSquad(out string squadname, out ulong squadteam, ulong winner)
    {
        List<Squad> squads = SquadManager.Squads.Where(x => x.Team == winner).ToList();
        if (squads.Count == 0)
        {
            squadname = NO_PLAYER_NAME_PLACEHOLDER;
            squadteam = 0;
            return new List<KeyValuePair<Player, char>>();
        }
        squads.Sort((a, b) =>
        {
            int totalxpgaina = 0;
            for (int i = 0; i < a.Members.Count; i++)
            {
                if (a.Members[i].Player.TryGetPlayerData(out Components.UCPlayerData c) && c.stats is IExperienceStats xp)
                    totalxpgaina += xp.XPGained;
            }
            int totalxpgainb = 0;
            for (int i = 0; i < b.Members.Count; i++)
            {
                if (b.Members[i].Player.TryGetPlayerData(out Components.UCPlayerData c) && c.stats is IExperienceStats xp)
                    totalxpgainb += xp.XPGained;
            }
            if (totalxpgaina == totalxpgainb)
            {
                int totalopgaina = 0;
                for (int i = 0; i < a.Members.Count; i++)
                {
                    if (a.Members[i].Player.TryGetPlayerData(out Components.UCPlayerData c) && c.stats is IExperienceStats xp)
                        totalopgaina += xp.Credits;
                }
                int totalopgainb = 0;
                for (int i = 0; i < b.Members.Count; i++)
                {
                    if (b.Members[i].Player.TryGetPlayerData(out Components.UCPlayerData c) && c.stats is IExperienceStats xp)
                        totalopgainb += xp.Credits;
                }
                if (totalxpgaina == totalxpgainb)
                {
                    int totalkillsa = 0;
                    for (int i = 0; i < a.Members.Count; i++)
                    {
                        if (a.Members[i].Player.TryGetPlayerData(out Components.UCPlayerData c) && c.stats is IPVPModeStats pvp)
                            totalkillsa += pvp.Kills;
                    }
                    int totalkillsb = 0;
                    for (int i = 0; i < b.Members.Count; i++)
                    {
                        if (b.Members[i].Player.TryGetPlayerData(out Components.UCPlayerData c) && c.stats is IPVPModeStats pvp)
                            totalkillsb += pvp.Kills;
                    }
                    return totalkillsa.CompareTo(totalkillsb);
                }
                return totalopgaina.CompareTo(totalopgainb);
            }
            return totalxpgaina.CompareTo(totalxpgainb);
        });
        Squad topsquad = squads[0];
        squadname = topsquad.Name;
        squadteam = topsquad.Team;
        List<UCPlayer> players = topsquad.Members.ToList();
        players.Sort((a, b) =>
        {
            if (topsquad.Leader.Steam64 == a.Steam64) return 1;
            else
            {
                int axp = 0, bxp = 0;
                if (a.Player.TryGetPlayerData(out Components.UCPlayerData ca) && ca.stats is IExperienceStats xp)
                    axp = xp.XPGained;
                if (b.Player.TryGetPlayerData(out ca) && ca.stats is IExperienceStats xp2)
                    bxp = xp2.XPGained;
                return axp.CompareTo(bxp);
            }
        });
        List<KeyValuePair<Player, char>> rtn = new List<KeyValuePair<Player, char>>(players.Count > 6 ? 6 : players.Count);
        for (int i = 0; i < (players.Count > 6 ? 6 : players.Count); i++)
        {
            rtn.Add(new KeyValuePair<Player, char>(players[i].Player, players[i].Icon));
        }
        return rtn;
    }
}
public class TDMPlayerStats : IStats, ITeamPVPModeStats, IRevivesStats, IExperienceStats
{
    public UCPlayer player;
    public readonly ulong id;
    public UCPlayer Player { get => player; set => player = value; }
    public ulong Steam64 => id;
    public int kills;
    public int deaths;
    public float KDR { get => deaths == 0 ? kills : (float)kills / deaths; }
    public int xpgained;
    public int creditsgained;
    public TimeSpan TimeDeployed { get => TimeSpan.FromSeconds(timeDeployedCounter); }
    public int Teamkills => teamkills;
    public int Kills => kills;
    public int Deaths => deaths;
    public float DamageDone => damagedone;
    public int Revives => revives;
    public int XPGained => xpgained;
    public int Credits => creditsgained;
    private float timeDeployedCounter;
    public int captures;
    public int teamkills;
    public int fobsdestroyed;
    public int fobsplaced;
    public float damagedone;
    public int onlineCount1;
    public int onlineCount2;
    public int revives;
    public TDMPlayerStats(UCPlayer player)
    {
        this.player = player;
        this.id = player.Steam64;
        Reset();
    }
    public TDMPlayerStats()
    {
        Reset();
    }
    public void Reset()
    {
        this.kills = 0;
        this.deaths = 0;
        this.timeDeployedCounter = 0;
        this.captures = 0;
        this.teamkills = 0;
        this.fobsdestroyed = 0;
        this.fobsplaced = 0;
        this.damagedone = 0;
        this.xpgained = 0;
        this.creditsgained = 0;
        this.onlineCount1 = 0;
        this.onlineCount2 = 0;
        this.revives = 0;
    }
    public void AddKill()
    {
        kills++;
    }
    public void AddDeath() => deaths++;
    public void AddTeamkill() => teamkills++;
    public void AddXP(int amount) => xpgained += amount;
    public void AddCredits(int amount) => creditsgained += amount;
    public void AddToTimeDeployed(float amount) => timeDeployedCounter += amount;
    public void AddDamage(float amount) => damagedone += amount;
    public void AddRevive() => revives++;
    public void CheckGame()
    {
        if (player != null && player.IsOnline)
        {
            byte team = player.Player.GetTeamByte();
            if (team == 1)
                onlineCount1++;
            else if (team == 2)
                onlineCount2++;
        }
    }
    public void Update(float dt)
    {

    }
    /*
    public override string ToString()
        =>
        $"Player: {id} ({(player == null ? "offline" : player.channel.owner.playerID.playerName)})\n" +
        $"Kills: {kills}\nDeaths: {deaths}\nTime Deployed: {TimeDeployed:g}\n" +
        $"Captures: {captures}\nTeamkills: {teamkills}\nFobs Destroyed: {fobsdestroyed}\n" +
        $"Fobs Placed: {fobsplaced}\nDamage Done: {damagedone}\nXP Gained: {xpgained}\nOfficer Pts Gained: {officerpointsgained}\n" +
        $"OnlineTimeT1:{(float)onlineCount1 / ((TeamDeathmatch)Data.Gamemode).GameStats.coroutinect * 100}%." +
        $"OnlineTimeT2:{(float)onlineCount2 / ((TeamDeathmatch)Data.Gamemode).GameStats.coroutinect * 100}%.";*/
}
