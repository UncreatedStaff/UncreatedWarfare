using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.FOBs.Construction;

namespace Uncreated.Warfare.Events.Models.Fobs.Shovelables;
/// <summary>
/// Event listener args which fires after any type of <see cref="ShovelableBuildable"/> is built up.
/// </summary>
public class ShovelableBuilt
{
    /// <summary>
    /// The <see cref="ShovelableBuildable"/> that was built up.
    /// </summary>
    public required ShovelableBuildable Shovelable { get; init; }
}
