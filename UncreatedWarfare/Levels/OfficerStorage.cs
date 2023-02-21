using System;
using System.IO;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Levels;

public class OfficerStorage : ListSingleton<OfficerData>
{
    internal const int OFFICER_RANK_ORDER = 9;
    private static OfficerStorage Singleton;
    public static bool Loaded => Singleton.IsLoaded<OfficerStorage, OfficerData>();
    public OfficerStorage() : base("officers", Path.Combine(Data.Paths.PointsStorage, "officers.json"))
    {

    }
    public override void Load()
    {
        Singleton = this;
    }
    public override void Unload()
    {
        Singleton = null!;
    }
    protected override string LoadDefaults() => EMPTY_LIST;
    public static bool IsOfficer(ulong playerID, ulong team, out OfficerData officer)
    {
        Singleton.AssertLoaded<OfficerStorage, OfficerData>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        officer = Singleton.GetObject(o => o.Steam64 == playerID && o.Team == team, true);
        return officer != null;
    }
    public static void ChangeOfficerRank(ulong playerID, int newOfficerTier, ulong newTeam)
    {
        Singleton.AssertLoaded<OfficerStorage, OfficerData>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        bool isNewOfficer = false;

        if (Singleton.ObjectExists(o => o.Steam64 == playerID && o.Team == newTeam, out OfficerData? officer))
        {
            if (officer.OfficerTier == newOfficerTier)
                return;

            Singleton.UpdateObjectsWhere(o => o.Steam64 == playerID, o => { o.Team = newTeam; o.OfficerTier = newOfficerTier; });
        }
        else
        {
            Singleton.AddObjectToSave(new OfficerData(playerID, newTeam, newOfficerTier));
            isNewOfficer = true;
        }

        UCPlayer? player = UCPlayer.FromID(playerID);
        if (player != null)
        {
            ref Ranks.RankData rankdata = ref Ranks.RankManager.GetRank(player, out _);
            FactionInfo f = TeamManager.GetFaction(newTeam);
            if (isNewOfficer || newOfficerTier >= officer.OfficerTier)
            {
                player.SendChat(T.OfficerPromoted, rankdata, f);
                Chat.Broadcast(LanguageSet.AllBut(player.Steam64), T.OfficerPromotedBroadcast, player, rankdata, f);
            }
            else
            {
                player.SendChat(T.OfficerDemoted, rankdata, f);
                Chat.Broadcast(LanguageSet.AllBut(player.Steam64), T.OfficerDemotedBroadcast, player, rankdata, f);
            }
        }
    }
    public static void DischargeOfficer(ulong playerID)
    {
        Singleton.AssertLoaded<OfficerStorage, OfficerData>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Singleton.RemoveWhere(o => o.Steam64 == playerID);

        UCPlayer? player = UCPlayer.FromID(playerID);
        if (player != null)
        {
            ref Ranks.RankData rankdata = ref Ranks.RankManager.GetRank(player, out _);
            player.SendChat(T.OfficerDischarged);
            Chat.Broadcast(LanguageSet.AllBut(player.Steam64), T.OfficerDischargedBroadcast, player, rankdata);
        }
    }
}
public class OfficerData
{
    public ulong Steam64;
    public ulong Team;
    public int OfficerTier;

    public OfficerData(ulong steam64, ulong team, int officerLevel)
    {
        Steam64 = steam64;
        Team = team;
        OfficerTier = officerLevel;
    }
    public OfficerData() { }
}
