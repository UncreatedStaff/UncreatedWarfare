using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Immutable;
using System.Globalization;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Kits;

public sealed class PublicKitLevelConfiguration : BaseAlternateConfigurationFile
{
    public ImmutableDictionary<PublicKitLevel, double> CreditCostBylevel { get; private set; }

    public PublicKitLevelConfiguration(IServiceProvider serviceProvider) : base(serviceProvider, "Kits/Public Kit Levels.yml", mapSpecific: false)
    {
        HandleChange(false);
    }

    [MemberNotNull(nameof(CreditCostBylevel))]
    protected override void HandleChange(bool isMapChange)
    {
        ImmutableDictionary<PublicKitLevel, double>.Builder cost = ImmutableDictionary.CreateBuilder<PublicKitLevel, double>();

        IConfigurationSection costsSection = UnderlyingConfiguration.GetSection("Costs");

        foreach (IConfigurationSection section in costsSection.GetChildren())
        {
            if (!ClassExtensions.TryParseClass(section.Key, out Class @class))
            {
                continue;
            }

            foreach (IConfigurationSection level in section.GetChildren())
            {
                if (!int.TryParse(level.Key, NumberStyles.None, CultureInfo.InvariantCulture, out int sectionId))
                    continue;

                double creditCost = level.Get<double>();
                PublicKitLevel levelInfo = new PublicKitLevel(@class, sectionId);

                cost.TryAdd(levelInfo, creditCost);
            }
        }

        CreditCostBylevel = cost.ToImmutable();
    }
}
