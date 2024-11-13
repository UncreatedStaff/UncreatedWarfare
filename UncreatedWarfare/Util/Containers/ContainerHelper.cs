using System;
using System.Collections.Generic;

namespace Uncreated.Warfare.Util.Containers;

public static class ContainerHelper
{
    private static readonly List<IComponentContainer> TempContainers = new List<IComponentContainer>(16);

    /// <summary>
    /// Find a component on an object or in any containers on the object.
    /// </summary>
    public static T? FindComponent<T>(Transform transform) where T : class
    {
        GameThread.AssertCurrent();

        if (transform.TryGetComponent(out T? comp))
            return comp;

        try
        {
            transform.GetComponentsInChildren(TempContainers);

            for (int i = 0; i < TempContainers.Count; ++i)
            {
                if (TempContainers[i] is not IComponentContainer<T> container)
                    continue;
                
                comp = container.ComponentOrNull<T>();
                if (comp != null)
                    return comp;
            }

            Type type = typeof(T);
            for (int i = 0; i < TempContainers.Count; ++i)
            {
                comp = (T?)TempContainers[i].ComponentOrNull(type);
                if (comp != null)
                    return comp;
            }
        }
        finally
        {
            TempContainers.Clear();
        }

        return null;
    }
}
