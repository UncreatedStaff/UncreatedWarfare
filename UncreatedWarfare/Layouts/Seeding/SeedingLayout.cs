using System;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Layouts.Seeding;

/// <summary>
/// Seeding layouts are a simple layout that is used to attract players to the server before switching to the main gamemode.
/// </summary>
[LayoutConfigureServicesCallback(nameof(ConfigureServices))]
public class SeedingLayout : Layout
{
    public IPlayerVoteManager SeedVoteManager { get; set; }

    private static void ConfigureServices(ContainerBuilder bldr, LayoutInfo layoutInfo)
    {
    }

    public SeedingLayout(ILifetimeScope serviceProvider, LayoutInfo layoutInfo, List<IDisposable> disposableConfigs)
        : base(serviceProvider, layoutInfo, disposableConfigs)
    {
        SeedVoteManager = serviceProvider.ResolveNamed<IPlayerVoteManager>("Seeder");
    }

    protected override UniTask ApplyLayoutConfigurationUpdateAsync(CancellationToken token = default)
    {
        return base.ApplyLayoutConfigurationUpdateAsync(token);
    }
}