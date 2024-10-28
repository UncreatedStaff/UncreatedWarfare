using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Util.Containers;

namespace Uncreated.Warfare.Buildables;
public class BuildableContainer : MonoBehaviour, IComponentContainer<IBuildableComponent>, IManualOnDestroy
{
    public IBuildable Buildable { get; private set; }
    public DateTime CreateTime { get; private set; }
    private readonly List<IBuildableComponent> _components = new List<IBuildableComponent>();

    public void AddComponent(IBuildableComponent newComponent)
    {
        if (_components.Exists(c => c.GetType() == newComponent.GetType()))
        {
            throw new InvalidOperationException($"Container already has a component of type {typeof(T)}. Multiple instances of the same component type are not supported.");
        }

        _components.Add(newComponent);
    }
    public T Component<T>() where T : IBuildableComponent
    {
        IBuildableComponent? component = _components.FirstOrDefault(t => t is T);
        if (component == null)
            throw new ArgumentException($"Component of type {typeof(T)} not found.");
        return (T)component;
    }

    public T? ComponentOrNull<T>() where T : IBuildableComponent
    {
        IBuildableComponent? component = _components.FirstOrDefault(t => t is T);
        if (component == null)
            return default;
        return (T?)component;

    }
    public bool TryGetFromContainer<T>(out T? result) where T : IBuildableComponent
    {
        result = default;
        IBuildableComponent? component = _components.FirstOrDefault(t => t is T);
        if (component == null)
            return false;
        result = (T?)component;
        return true;

    }

    public void Init(IBuildable buildable)
    {
        Buildable = buildable;
        CreateTime = DateTime.Now;
    }

    public void ManualOnDestroy()
    {
        foreach (var component in _components)
            component.Dispose();

        Destroy(this);
    }
}
