using Microsoft.Extensions.Configuration;
using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Squads;

public sealed class SquadConfiguration : BaseAlternateConfigurationFile
{
    public IReadOnlyDictionary<Class, int> KitClassesAllowedPerXTeammates = new Dictionary<Class, int>();

    public SquadConfiguration(IServiceProvider serviceProvider)
        : base(serviceProvider, "Squads.yml")
    {

    }

    protected override void HandleLoaded()
    {
        HandleChange(true);
    }

    protected override void HandleChange(bool isMapChange)
    {
        KitClassesAllowedPerXTeammates = UnderlyingConfiguration.GetSection("KitClassesAllowedPerXTeammates").Get<Dictionary<Class, int>>() ?? new Dictionary<Class, int>();
    }
}
