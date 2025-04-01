namespace Uncreated.Warfare.Plugins;

/// <summary>
/// Provides the ability to set up services for a layout when listed in a layout configuration.
/// </summary>
/// <remarks>These should be registered using a <see cref="IServiceConfigurer"/> and will be queried when creating a layout scope if the type is listed in the Services section.</remarks>
public interface ILayoutServiceConfigurer
{
    /// <summary>
    /// Provides the ability to set up services for a layout when listed in a layout configuration.
    /// </summary>
    void ConfigureServices(ContainerBuilder bldr);
}