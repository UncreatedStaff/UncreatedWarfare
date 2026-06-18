using Autofac;
using Uncreated.Warfare.FreeTeamDeathmatch.Tweaks;
using Uncreated.Warfare.Plugins;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.FreeTeamDeathmatch;

public class LayoutServiceConfigurer : ILayoutServiceConfigurer
{
    public void ConfigureServices(ContainerBuilder bldr)
    {
        bldr.RegisterType<FtdmNoInjuresTweak>()
            .AsImplementedInterfaces();

        bldr.RegisterType<DisabledElectricalGridHandler>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();
    }
}