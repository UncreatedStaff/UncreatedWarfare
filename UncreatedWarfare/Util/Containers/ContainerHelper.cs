using System;

namespace Uncreated.Warfare.Util.Containers;

public static class ContainerHelper
{
    private static readonly List<IComponentContainer> TempContainers = new List<IComponentContainer>(16);
    private static readonly List<MonoBehaviour> TempComponents = new List<MonoBehaviour>(16);

    /// <summary>
    /// Find a component on an object or in any containers on the object.
    /// </summary>
    public static T? FindComponent<T>(Transform transform) where T : class
    {
        GameThread.AssertCurrent();

        transform.GetComponents(TempComponents);
        try
        {
            foreach (MonoBehaviour child in TempComponents) // TryGetComponent doesn't work with interfaces
            {
                if (child is T t)
                    return t;
            }
        }
        finally
        {
            TempComponents.Clear();
        }


        try
        {
            transform.GetComponentsInChildren(TempContainers);

            for (int i = 0; i < TempContainers.Count; ++i)
            {
                if (TempContainers[i] is T c)
                    return c;

                if (TempContainers[i] is not IComponentContainer<T> container)
                    continue;
                
                T? comp = container.ComponentOrNull<T>();
                if (comp != null)
                    return comp;
            }

            Type type = typeof(T);
            for (int i = 0; i < TempContainers.Count; ++i)
            {
                T? comp = (T?)TempContainers[i].ComponentOrNull(type);
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
