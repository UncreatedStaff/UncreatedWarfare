using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Signs;

[SignPrefix("squad_")]
public class SquadSignInstanceProvider : ISignInstanceProvider
{
    private static StringBuilder StringBuilder = new();
    private readonly ITeamManager<Team> _teamManager;
    private readonly SquadManager _squadManager;

    public SquadSignInstanceProvider(ITeamManager<Team> teamManager, SquadManager squadManager)
    {
        _teamManager = teamManager;
        _squadManager = squadManager;
    }
    
    public bool CanBatchTranslate => true;
    public string FallbackText => $"Squad #{SquadNumber}";
    public Team Team { get; private set; }
    public int SquadNumber { get; private set; }
    public void Initialize(BarricadeDrop barricade, string extraInfo, IServiceProvider serviceProvider)
    {
        Match regexMatch = Regex.Match(extraInfo, @"team(\d+)_(\d+)");

        if (!regexMatch.Success || regexMatch.Groups.Count != 3)
        {
            SquadNumber = -1;
            return;
        }
        
        string team = regexMatch.Groups[1].Value;
        string squadNumber = regexMatch.Groups[2].Value;
        
        Team = _teamManager.GetTeam(new CSteamID(ulong.Parse(team, CultureInfo.InvariantCulture)));
        SquadNumber = int.Parse(squadNumber, CultureInfo.InvariantCulture);
    }

    public string Translate(ITranslationValueFormatter formatter, IServiceProvider serviceProvider, LanguageInfo language,
        CultureInfo culture, WarfarePlayer? player)
    {
        if (SquadNumber is < 1 or > SquadManager.MaxSquadCount)
            return "";
        
        Squad? squad = _squadManager.Squads.FirstOrDefault(s => s.Team == Team && s.TeamIdentificationNumber == SquadNumber);
        if (squad == null)
            return "";

        StringBuilder.Clear();
        StringBuilder
            .AppendColorized($"<b>SQUAD {SquadNumber.ToString(culture)}", "#9effc6")
            .Append("  ")
            .AppendColorized($"({squad.Members.Count}/{Squad.MaxMembers})", "#ffffff")
            .AppendLine()
            .AppendColorized($"{squad.Name}</b>", "#8b8b8b")
            .AppendLine()
            .AppendLine()
            .AppendColorized(squad.Leader.Names.PlayerName, "#3e3e3e");
        
        return StringBuilder.ToString();
    }
}