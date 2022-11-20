using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Sync;
public class SqlConfigTable
{

}

public class SqlConfigValue<TData>
{
    public TData? Value;

}

public class SqlConfigValueList<TData>
{
    public List<TData> Values;
}