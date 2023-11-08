using OpenMod.API.Plugins;
using OpenMod.EntityFrameworkCore.MySql.Extensions;
using Uncreated.Warfare.Database;

namespace Uncreated.Warfare;
public sealed class WarfareContainerConfigurator : IPluginContainerConfigurator
{
    void IPluginContainerConfigurator.ConfigureContainer(IPluginServiceConfigurationContext context)
    {
        /*
         * Registers all db context providers with the WarfareDatabaseContext attribute.
         */
        context.ContainerBuilder.AddMySqlDbContext<WarfareDbContext>();
    }
}
