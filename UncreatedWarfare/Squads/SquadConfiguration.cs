using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Squads;

public sealed class SquadConfiguration : BaseAlternateConfigurationFile
{
    public IReadOnlyDictionary<Class, int> KitClassesAllowedPerXTeammates = new Dictionary<Class, int>();

    public SquadConfiguration()
        : base("Squads.yml")
    {
        HandleChange();
    }
    
    /// <inheritdoc />
    protected override void HandleChange()
    {
        KitClassesAllowedPerXTeammates = UnderlyingConfiguration.GetSection("KitClassesAllowedPerXTeammates").Get<Dictionary<Class, int>>() ?? new Dictionary<Class, int>();
    }
}
