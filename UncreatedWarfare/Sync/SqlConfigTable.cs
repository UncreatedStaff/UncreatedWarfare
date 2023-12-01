using System.Collections.Generic;

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