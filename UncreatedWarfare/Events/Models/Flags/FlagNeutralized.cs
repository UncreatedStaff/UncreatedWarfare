using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Layouts.Phases.Flags;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Flags;
/// <summary>
/// Event listener args which fires after a <see cref="FlagObjective"/> is neutralized.
/// "Neutralized" means one team successfully reduced the former owner team's contest points to zero, 
/// causing <see cref="FlagObjective.Owner"/> to become neutral (<see cref="Team.NoTeam"/>).
/// </summary>
internal class FlagNeutralized
{
    /// <summary>
    /// The flag that was neutralized.
    /// </summary>
    public required FlagObjective Flag { get; init; }
    /// <summary>
    /// The team that neutralized the flag by means of leading the flag contest. This team is not the new <see cref="FlagObjective.Owner"/> of the flag, 
    /// but rather simply the team that caused it's owner to change.
    /// </summary>
    public required Team Neutralizer { get; init; }
}
