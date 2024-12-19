using System.Runtime.CompilerServices;
using System;
using Uncreated.Warfare.Database;
using DanielWillett.ReflectionTools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// only initialize if library exists
public static class SshTunnelHelper
{
    public static void AddSshTunnelService(ContainerBuilder serviceCollection)
    {
        serviceCollection.RegisterType(GetServiceType())
            .SingleInstance();
    }

    public static UniTask OpenIfAvailableAsync(ILifetimeScope serviceProvider, CancellationToken token)
    {
        string? configuredSshKey = serviceProvider.ResolveOptional<IConfiguration>()?.GetSection("database")?["ssh_key"];

        if (string.IsNullOrEmpty(configuredSshKey))
            return UniTask.CompletedTask;

        object? service = serviceProvider.ResolveOptional(GetServiceType());
        if (service != null)
        {
            return OpenAsync(service, token);
        }

        return UniTask.CompletedTask;
    }

    private static UniTask OpenAsync(object service, CancellationToken token)
    {
        SshTunnelService sshTunnel = (SshTunnelService)service;

        return sshTunnel.StartAsync(token);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type GetServiceType() => typeof(SshTunnelService);
}