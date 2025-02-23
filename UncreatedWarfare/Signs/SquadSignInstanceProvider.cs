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
    private readonly TextMeasurementService _measurementService;
    private SignMetrics _signMetrics;

    public SquadSignInstanceProvider(ITeamManager<Team> teamManager, SquadManager squadManager, TextMeasurementService measurementService)
    {
        _teamManager = teamManager;
        _squadManager = squadManager;
        _measurementService = measurementService;
    }
    
    public bool CanBatchTranslate => true;
    public string FallbackText => $"Squad #{SquadNumber}";
    public Team Team { get; private set; }
    public int SquadNumber { get; private set; }
    public void Initialize(BarricadeDrop barricade, string extraInfo, IServiceProvider serviceProvider)
    {
        Match regexMatch = Regex.Match(extraInfo, @"team(\d+)_(\d+)");

        _signMetrics = _measurementService.GetSignMetrics(barricade.asset.GUID);

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
            .AppendColorized($"<b>{SquadNumber.ToString(culture)}", "#9effc6")
            .Append("  ")
            .AppendColorized($"{squad.Name}</b>", "#ffffff")
            .AppendLine()
            .AppendColorized($"{squad.Members.Count}/{Squad.MaxMembers}", "#8b8b8b")
            .AppendLine()
            .AppendColorized(squad.Leader.Names.CharacterName, "#8b8b8b");
        
        return StringBuilder.ToString();
    }

    private void AppendName(Squad squad, out bool hasExtraLine)
    {
        Span<Range> outRanges = stackalloc Range[2]; // max 2 lines
        int nameSplits = _measurementService.SplitLines(squad.Name, 1.3f, _signMetrics, outRanges);

        StringBuilder.Append("<#ffffff>");
        if (nameSplits > 1)
        {
            ReadOnlySpan<char> nameSpan = squad.Name.AsSpan();
            StringBuilder
                .Append(nameSpan[outRanges[0]])
                .Append('\n')
                .Append(nameSpan[outRanges[1]]);
            hasExtraLine = true;
        }
        else
        {
            StringBuilder.Append(squad.Name);
            hasExtraLine = false;
        }

        StringBuilder.Append("</color>");
    }
}