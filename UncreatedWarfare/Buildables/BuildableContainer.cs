using System;
using System.Collections.Generic;
using DanielWillett.ReflectionTools;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Containers;

namespace Uncreated.Warfare.Buildables;
public class BuildableContainer : MonoBehaviour, IComponentContainer<IBuildableComponent>, IManualOnDestroy
{
    private readonly List<IBuildableComponent> _components = new List<IBuildableComponent>();

#nullable disable
    public IBuildable Buildable { get; private set; }
    public DateTime CreateTime { get; private set; }
#nullable restore

    internal void Init(IBuildable buildable)
    {
        Buildable = buildable;
        CreateTime = DateTime.UtcNow;
    }

    public static BuildableContainer Get(IBuildable buildable)
    {
        if (buildable.IsDead)
            throw new InvalidOperationException("Buildable is dead.");

        if (!buildable.Model.TryGetComponent(out BuildableContainer container))
            container = buildable.Model.gameObject.AddComponent<BuildableContainer>();

        return container;
    }

    public void AddComponent(IBuildableComponent newComponent)
    {
        GameThread.AssertCurrent();

        lock (_components)
        {
            if (_components.Exists(c => c.GetType() == newComponent.GetType()))
            {
                throw new InvalidOperationException($"Container already has a component of type {typeof(T)}. Multiple instances of the same component type are not supported.");
            }

            _components.Add(newComponent);
        }
    }
    public T Component<T>() where T : class, IBuildableComponent
    {
        return ComponentOrNull<T>() ?? throw new ArgumentException($"Component of type {typeof(T)} not found.");
    }

    public T? ComponentOrNull<T>() where T : class, IBuildableComponent
    {
        IBuildableComponent? component;
        if (GameThread.IsCurrent)
        {
            // ReSharper disable once InconsistentlySynchronizedField
            component = _components.Find(x => x is T);
        }
        else
        {
            lock (_components)
                component = _components.Find(x => x is T);
        }

        return (T?)component;
    }

    public object Component(Type t)
    {
        return ComponentOrNull(t) ?? throw new ArgumentException($"Component of type {Accessor.ExceptionFormatter.Format(t)} not found.");
    }

    public object? ComponentOrNull(Type t)
    {
        IBuildableComponent? component;
        if (GameThread.IsCurrent)
        {
            // ReSharper disable once InconsistentlySynchronizedField
            component = _components.Find(t.IsInstanceOfType);
        }
        else
        {
            lock (_components)
                component = _components.Find(t.IsInstanceOfType);
        }

        return component;
    }

    void IManualOnDestroy.ManualOnDestroy()
    {
        lock (_components)
        {
            foreach (var component in _components)
                component.Dispose();
        }

        Destroy(this);
    }
}
