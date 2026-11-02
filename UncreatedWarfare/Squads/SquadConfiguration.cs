using Microsoft.Extensions.Configuration;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Kits;

namespace Uncreated.Warfare.Squads;

public sealed class SquadConfiguration : BaseAlternateConfigurationFile
{
    public IReadOnlyDictionary<Class, int> KitClassesAllowedPerXTeammates = new Dictionary<Class, int>();

    /// <summary>
    /// When the player count is greater than or equal to this value, <see cref="KitModel.MinRequiredSquadMembers"/> will be at it's full effect.
    /// When the player count is less than this value, it will scale down instead.
    /// </summary>
    public int SquadLimitScaleMaximum { get; set; }

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

        SquadLimitScaleMaximum = UnderlyingConfiguration.GetValue("SquadLimitScaleMaximum", 32);
    }
}
