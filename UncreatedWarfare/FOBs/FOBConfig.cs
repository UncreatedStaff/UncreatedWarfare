using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.FOBs;
public class FOBConfig : Config<FOBConfigData>
{
    public FOBConfig() : base(Warfare.Data.FOBStorage, "config.json")
    {
    }
}
