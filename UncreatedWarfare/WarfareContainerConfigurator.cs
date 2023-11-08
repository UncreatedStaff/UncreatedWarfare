using DanielWillett.ReflectionTools;
using Microsoft.Extensions.Logging;
using OpenMod.API.Plugins;
using OpenMod.EntityFrameworkCore.MySql.Extensions;
using System;
using System.Linq;
using Uncreated.Warfare.Automation;

namespace Uncreated.Warfare;
public sealed class WarfareContainerConfigurator : IPluginContainerConfigurator
{
    private readonly ILogger _logger;
    public WarfareContainerConfigurator(ILogger<WarfareContainerConfigurator> logger)
    {
        _logger = logger;
    }
    void IPluginContainerConfigurator.ConfigureContainer(IPluginServiceConfigurationContext context)
    {
        /*
         * Registers all db context providers with the WarfareDatabaseContext attribute.
         */
        foreach (Type type in Accessor.GetTypesSafe().Where(type => type.IsDefinedSafe<WarfareDatabaseContextAttribute>() && !type.IsIgnored()))
        {
            context.ContainerBuilder.AddMySqlDbContext(type);
            _logger.LogDebug($"Registered database context provider: {type.Name}.");
        }
    }
}
