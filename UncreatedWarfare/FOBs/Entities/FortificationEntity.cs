using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.FOBs.Entities;

public class FortificationEntity : BuildableFobEntity<ShovelableInfo>
{
    public FortificationEntity(ShovelableInfo info, Team team, IBuildable buildable, IServiceProvider serviceProvider)
        : base(info, buildable, team, serviceProvider)
    { }
}