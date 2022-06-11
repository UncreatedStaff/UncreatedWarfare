using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Players;
using Uncreated.Warfare.Singletons;

namespace Uncreated.Warfare.Point;

public class OfficerStorage : ListSingleton<OfficerData>
{
    internal const int OFFICER_RANK_ORDER = 9;
    private static OfficerStorage Singleton;
    public static bool Loaded => Singleton.IsLoaded<OfficerStorage, OfficerData>();
    public OfficerStorage() : base("officers", Path.Combine(Data.PointsStorage, "officers.json"))
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
    // Are we still using this?
    public static void ChangeOfficerRank(ulong playerID, int newOfficerTier, ulong newTeam)
    {
        Singleton.AssertLoaded<OfficerStorage, OfficerData>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        bool isNewOfficer = false;

        if (Singleton.ObjectExists(o => o.Steam64 == playerID && o.Team == newTeam, out var officer))
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
            if (isNewOfficer || newOfficerTier >= officer.OfficerTier)
            {
                ref Ranks.RankData rankdata = ref Ranks.RankManager.GetRank(player, out _);
                player.Message("officer_promoted", rankdata.GetName(playerID), Translation.Translate("team_" + newTeam, player));

                FPlayerName names = F.GetPlayerOriginalNames(player);
                foreach (LanguageSet set in Translation.EnumerateLanguageSets(player.Steam64))
                {
                    string name = rankdata.GetName(set.Language);
                    string team = Translation.Translate("team_" + newTeam, set.Language);
                    while (set.MoveNext())
                        set.Next.SendChat("officer_announce_promoted", names.CharacterName, name, team);
                }
            }
            else
            {
                ref Ranks.RankData rankdata = ref Ranks.RankManager.GetRank(player, out _);
                player.Message("officer_demoted", rankdata.GetName(playerID), Translation.Translate("team_" + newTeam, player));

                FPlayerName names = F.GetPlayerOriginalNames(player);
                foreach (LanguageSet set in Translation.EnumerateLanguageSets(player.Steam64))
                {
                    string name = rankdata.GetName(set.Language);
                    string team = Translation.Translate("team_" + newTeam, set.Language);
                    while (set.MoveNext())
                        set.Next.SendChat("officer_announce_promoted", names.CharacterName, name, team);
                }
            }
            Points.UpdateXPUI(player);
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
            player.Message("officer_discharged", rankdata.GetName(playerID));
            FPlayerName names = F.GetPlayerOriginalNames(player);
            foreach (LanguageSet set in Translation.EnumerateLanguageSets(player.Steam64))
            {
                string name = rankdata.GetName(set.Language);
                while (set.MoveNext())
                    set.Next.SendChat("officer_announce_discharged", names.CharacterName, name);
            }
            Points.UpdateXPUI(player);
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
