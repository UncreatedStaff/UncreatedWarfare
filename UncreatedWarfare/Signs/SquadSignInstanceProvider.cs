using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Uncreated.Warfare.Interaction.Requests;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;

namespace Uncreated.Warfare.Signs;

[SignPrefix("squad_")]
public class SquadSignInstanceProvider : ISignInstanceProvider, IRequestable<Squad>
{
    private static StringBuilder StringBuilder = new();
    private readonly ITeamManager<Team> _teamManager;
    private readonly SquadManager _squadManager;
    private readonly TextMeasurementService _measurementService;
    private readonly SquadTranslations _translations;
    private SignMetrics _signMetrics;

    public SquadSignInstanceProvider(
        ITeamManager<Team> teamManager,
        SquadManager squadManager,
        TextMeasurementService measurementService,
        TranslationInjection<SquadTranslations> translations)
    {
        Team = Team.NoTeam;
        SquadNumber = 0;

        _teamManager = teamManager;
        _squadManager = squadManager;
        _measurementService = measurementService;
        _translations = translations.Value;
    }
    
    public bool CanBatchTranslate => true;
    public string FallbackText => $"Squad #{SquadNumber}";
    public Team Team { get; private set; }
    public byte SquadNumber { get; private set; }
    public void Initialize(BarricadeDrop barricade, string extraInfo, IServiceProvider serviceProvider)
    {
        Match regexMatch = Regex.Match(extraInfo, @"team(\d+)_(\d+)");

        _signMetrics = _measurementService.GetSignMetrics(barricade.asset.GUID);

        if (!regexMatch.Success || regexMatch.Groups.Count != 3)
        {
            SquadNumber = 0;
            return;
        }
        
        string team = regexMatch.Groups[1].Value;
        string squadNumber = regexMatch.Groups[2].Value;
        
        Team = _teamManager.GetTeam(new CSteamID(ulong.Parse(team, CultureInfo.InvariantCulture)));
        SquadNumber = byte.Parse(squadNumber, CultureInfo.InvariantCulture);
    }

    public string Translate(ITranslationValueFormatter formatter, IServiceProvider serviceProvider, LanguageInfo language,
        CultureInfo culture, WarfarePlayer? player)
    {
        if (SquadNumber is < 1 or > SquadManager.MaxSquadCount)
            return "Invalid Sign";
        
        Squad? squad = _squadManager.Squads.FirstOrDefault(s => s.Team == Team && s.TeamIdentificationNumber == SquadNumber);
        if (squad == null || /* about to get disbanded */ squad.Members.Count == 0)
        {
            return _translations.EmptySquadSignTranslation.Translate(SquadNumber, language, culture, TimeZoneInfo.Utc);
        }

        StringBuilder.Clear();

        StringBuilder
            .Append("<b>")
            .AppendColorized(_translations.SquadSignHeader.Translate(SquadNumber, language, culture, TimeZoneInfo.Utc), "#9effc6")
            .Append("  ")
            .AppendColorized($"({squad.Members.Count}/{Squad.MaxMembers})", "#ffffff")
            .AppendLine();
        AppendName(squad, out bool hasExtraLine);
        StringBuilder.Append("</b>");

        // if name takes up only one line, add an extra line
        if (!hasExtraLine)
            StringBuilder.AppendLine();

        StringBuilder
            .AppendLine()
            .AppendLine()
            .AppendColorized(squad.Leader.Names.PlayerName, "#3e3e3e");
        
        return StringBuilder.ToString();
    }

    private void AppendName(Squad squad, out bool hasExtraLine)
    {
        Span<Range> outRanges = stackalloc Range[2]; // max 2 lines
        int nameSplits = _measurementService.SplitLines(squad.Name, 1.3f, _signMetrics, outRanges);

        StringBuilder.Append("<#8b8b8b>");
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