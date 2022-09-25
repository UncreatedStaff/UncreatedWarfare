using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.FOBs;
public class FOBConfig : Config<FOBConfigData>
{
    public FOBConfig() : base(Warfare.Data.Paths.FOBStorage, "config.json", "fobs")
    {
    }
}
