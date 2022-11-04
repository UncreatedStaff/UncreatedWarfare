using System;

namespace Uncreated.Warfare.Singletons;
/// <summary>
/// Use <see cref="SDG.Unturned.Level"/> as as a dependency to wait until the level is loaded before loading the singleton.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class SingletonDependencyAttribute : Attribute
{
    readonly Type dependency;
    /// <summary>
    /// Use <see cref="SDG.Unturned.Level"/> as as a dependency to wait until the level is loaded before loading the singleton.
    /// </summary>
    public SingletonDependencyAttribute(Type dependency)
    {
        this.dependency = dependency;
    }
    public Type Dependency => dependency;
}
