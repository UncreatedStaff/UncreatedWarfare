using System;
using System.Collections.Generic;
using System.Text;

namespace Uncreated.Warfare.Players.Unlocks;
public interface IUnlockRequirementProvider
{
    /// <summary>
    /// Returns a list of all requirements in all elements in this list provider.
    /// </summary>
    /// <remarks>This is used for quest unlock requirements so the quests can be added when the player joins, but may have more uses later.</remarks>
    IEnumerable<UnlockRequirement> UnlockRequirements { get; }
}
