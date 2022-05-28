using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Singletons;
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class SingletonDependencyAttribute : Attribute
{
    readonly Type dependency;
    public SingletonDependencyAttribute(Type dependency)
    {
        this.dependency = dependency;
    }
    public Type Dependency => dependency;
}
