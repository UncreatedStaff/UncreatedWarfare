using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Util.Containers;
/// <summary>
/// A container for neatly separating and storing sub components of type <typeparamref name="TComponentType"/>.
/// </summary>
public interface IComponentContainer<TComponentType>
{
    /// <summary>
    /// Get the given component type from <see cref="Components"/>.
    /// </summary>
    /// <remarks>Always returns a value or throws an exception.</remarks>
    [Pure]
    public T Component<T>() where T : TComponentType;
    /// <summary>
    /// Get the given component type from <see cref="Components"/>.
    /// </summary>
    [Pure]
    public T? ComponentOrNull<T>() where T : TComponentType;
}
