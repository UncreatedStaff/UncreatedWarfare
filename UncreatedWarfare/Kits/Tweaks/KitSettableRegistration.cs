using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Kits.Tweaks;

internal class KitSettableRegistration : IHostedService
{
    private static bool _registered;

    UniTask IHostedService.StartAsync(CancellationToken token)
    {
        if (_registered)
            return UniTask.CompletedTask;

        // register custom kit properties

        // kit set faction
        SettableUtil<KitModel>.AddCustomHandler<string>((kit, faction, serviceProvider) =>
        {
            if (faction == null || faction.Equals("null", StringComparison.InvariantCultureIgnoreCase)
                                || faction.Equals("none", StringComparison.InvariantCultureIgnoreCase)
                                || faction.Equals("blank", StringComparison.InvariantCultureIgnoreCase)
                                || faction.Equals("nil", StringComparison.InvariantCultureIgnoreCase))
            {
                kit.Faction = null;
                kit.FactionId = null;
                return SetPropertyResult.Success;
            }

            IFactionDataStore factions = serviceProvider.GetRequiredService<IFactionDataStore>();

            FactionInfo? factionFound = factions.FindFaction(faction, false);

            if (factionFound == null)
                return SetPropertyResult.ParseFailure;

            kit.FactionId = factionFound.PrimaryKey;
            return SetPropertyResult.Success;
        }, "faction", "team", "group");

        _registered = true;

        return UniTask.CompletedTask;
    }

    UniTask IHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }
}