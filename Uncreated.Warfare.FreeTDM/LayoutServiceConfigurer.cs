using Autofac;
using Uncreated.Warfare.FreeTeamDeathmatch.Tweaks;
using Uncreated.Warfare.Plugins;

namespace Uncreated.Warfare.FreeTeamDeathmatch;

public class LayoutServiceConfigurer : ILayoutServiceConfigurer
{
    public void ConfigureServices(ContainerBuilder bldr)
    {
        bldr.RegisterType<FtdmNoInjuresTweak>()
            .AsImplementedInterfaces();
    }
}