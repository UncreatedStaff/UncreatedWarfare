using Microsoft.Extensions.Configuration;

namespace Uncreated.Warfare.Plugins;

/// <summary>
/// Provides the ability to set up services for a plugin. This object can not inject services except  and <see cref="IConfiguration"/>.
/// </summary>
/// <remarks>This object can inject the following services: <see cref="IConfiguration"/>, <see cref="ILogger"/>, <see cref="WarfareModule"/>, <see cref="WarfarePluginLoader"/>, <see cref="ContainerBuilder"/>, <see cref="WarfarePlugin"/>, <see cref="WarfarePluginConfiguration"/>.</remarks>
public interface IServiceConfigurer
{
    /// <summary>
    /// Provides the ability to set up services for a plugin.
    /// </summary>
    void ConfigureServices(ContainerBuilder bldr);
}