using System;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.Util.Containers;

/// <summary>
/// A container for neatly separating and storing sub components of type <typeparamref name="TComponentType"/>.
/// </summary>
public interface IComponentContainer
{
    /// <summary>
    /// Get the given component type from a list of components.
    /// </summary>
    /// <exception cref="ComponentNotFoundException">Component not found.</exception>
    /// <remarks>Always returns a value or throws an exception.</remarks>
    [Pure]
    object Component(Type t);

    /// <summary>
    /// Get the given component type from a list of components.
    /// </summary>
    [Pure]
    object? ComponentOrNull(Type t);
}

/// <summary>
/// A container for neatly separating and storing sub components of type <typeparamref name="TComponentType"/>.
/// </summary>
public interface IComponentContainer<in TComponentType> : IComponentContainer where TComponentType : notnull
{
    /// <summary>
    /// Get the given component type from a list of components.
    /// </summary>
    /// <exception cref="ComponentNotFoundException">Component not found.</exception>
    /// <remarks>Always returns a value or throws an exception.</remarks>
    [Pure]
    T Component<T>() where T : class, TComponentType;

    /// <summary>
    /// Get the given component type from a list of components.
    /// </summary>
    [Pure]
    T? ComponentOrNull<T>() where T : class, TComponentType;
}

/// <summary>
/// Extensions for working with <see cref="IComponentContainer{TComponentType}"/>s.
/// </summary>
public static class ComponentContainerExtensions
{
    /// <summary>
    /// Get the given component type from a list of components.
    /// </summary>
    /// <returns><see langword="false"/> if not found, otherwise <see langword="true"/>.</returns>
    public static bool TryGetFromContainer<T>(this IComponentContainer<T> container, out T? result) where T : class
    {
        result = container.ComponentOrNull<T>();
        return result != null;
    }
}