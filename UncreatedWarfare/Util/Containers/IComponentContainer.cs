using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.Util.Containers;

/// <summary>
/// A container for neatly separating and storing sub components of type <typeparamref name="TComponentType"/>.
/// </summary>
public interface IComponentContainer<in TComponentType> where TComponentType : notnull
{
    /// <summary>
    /// Get the given component type from a list of components.
    /// </summary>
    /// <exception cref="ComponentNotFoundException">Component not found.</exception>
    /// <remarks>Always returns a value or throws an exception.</remarks>
    [Pure]
    public T Component<T>() where T : TComponentType;

    /// <summary>
    /// Get the given component type from a list of components.
    /// </summary>
    [Pure]
    public T? ComponentOrNull<T>() where T : TComponentType;
}