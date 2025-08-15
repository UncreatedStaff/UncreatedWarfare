using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Layouts.Seeding;

/// <summary>
/// Seeding layouts are a simple layout that is used to attract players to the server before switching to the main gamemode.
/// </summary>
[LayoutConfigureServicesCallback(nameof(ConfigureServices))]
public class SeedingLayout : Layout
{
    public class SeedingRules
    {
        /// <summary>
        /// Number of players that have to be left in the server to start a seeding vote.
        /// </summary>
        public int VotePlayerThreshold { get; set; }

        /// <summary>
        /// Number of players that have to join the server to start a real gamemode.
        /// </summary>
        public int StartPlayerThreshold { get; set; }
    }

    public IPlayerVoteManager SeedVoteManager { get; set; }

    public SeedingRules Rules { get; private set; }

    private static void ConfigureServices(ContainerBuilder bldr, LayoutInfo layoutInfo)
    {
        bldr.RegisterType<SeedingPlayerCountMonitor>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterType<PlayerVoteManager>()
            .AsImplementedInterfaces()
            .SingleInstance()
            .Named<IPlayerVoteManager>("Seeder");
    }

    public SeedingLayout(ILifetimeScope serviceProvider, LayoutInfo layoutInfo, List<IDisposable> disposableConfigs)
        : base(serviceProvider, layoutInfo, disposableConfigs)
    {
        Rules = GetRules(LayoutInfo.Layout);
        SeedVoteManager = serviceProvider.ResolveNamed<IPlayerVoteManager>("Seeder");
    }

    protected override UniTask ApplyLayoutConfigurationUpdateAsync(CancellationToken token = default)
    {
        Rules = GetRules(LayoutInfo.Layout);
        return base.ApplyLayoutConfigurationUpdateAsync(token);
    }

    private static SeedingRules GetRules(IConfiguration config)
    {
        return config.GetSection("SeedingRules")
                   .Get<SeedingRules>()
               ?? new SeedingRules();
    }
}