using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Buildables;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Containers;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.Buildables;
public class BuildableContainer : MonoBehaviour, IComponentContainer<IBuildableComponent>, IManualOnDestroy
{
    private readonly List<IBuildableComponent> _components = new List<IBuildableComponent>();

#nullable disable
    public IBuildable Buildable { get; private set; }
    public DateTime CreateTime { get; private set; }
    public FrameHandle SignEditFrame { get; private set; }
    public CSteamID SignEditor
    {
        get;
        set
        {
            SignEditFrame = value.GetEAccountType() == EAccountType.k_EAccountTypeIndividual ? FrameHandle.Claim() : default;
            field = value;
        }
    }
#nullable restore

    internal void Init(IBuildable buildable)
    {
        Buildable = buildable;
        CreateTime = DateTime.UtcNow;
    }

    public static BuildableContainer Get(IBuildable buildable)
    {
        GameThread.AssertCurrent();

        if (buildable.IsDead)
            throw new InvalidOperationException("Buildable is dead.");

        if (!buildable.Model.TryGetComponent(out BuildableContainer container))
            container = buildable.Model.gameObject.AddComponent<BuildableContainer>();

        return container;
    }

    public static BuildableContainer Get(BarricadeDrop barricade)
    {
        GameThread.AssertCurrent();

        if (barricade.GetServersideData().barricade.isDead)
            throw new InvalidOperationException("Barricade is dead.");

        if (!barricade.model.TryGetComponent(out BuildableContainer container))
            container = barricade.model.gameObject.AddComponent<BuildableContainer>();

        return container;
    }

    public static BuildableContainer Get(StructureDrop structure)
    {
        GameThread.AssertCurrent();

        if (structure.GetServersideData().structure.isDead)
            throw new InvalidOperationException("Structure is dead.");

        if (!structure.model.TryGetComponent(out BuildableContainer container))
            container = structure.model.gameObject.AddComponent<BuildableContainer>();

        return container;
    }

    public void AddComponent(IBuildableComponent newComponent)
    {
        GameThread.AssertCurrent();

        lock (_components)
        {
            if (_components.Exists(c => c.GetType() == newComponent.GetType()))
            {
                throw new InvalidOperationException($"Container already has a component of type {Accessor.ExceptionFormatter.Format(newComponent.GetType())}. Multiple instances of the same component type are not supported.");
            }

            _components.Add(newComponent);
        }
    }
    public T Component<T>() where T : class, IBuildableComponent
    {
        return ComponentOrNull<T>() ?? throw new ArgumentException($"Component of type {Accessor.ExceptionFormatter.Format(typeof(T))} not found.");
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
            foreach (IBuildableComponent? component in _components)
                component.Dispose();
        }

        Destroy(this);
    }
}

/// <summary>
/// Components implementing this will receive information about the most recent salvager.
/// </summary>
public interface ISalvageInfo
{
    bool IsSalvaged { set; get; }
    CSteamID Salvager { set; get; }
}

/// <summary>
/// Components implementing this will receive a callback allowing it to handle a salvage request.
/// </summary>
public interface ISalvageListener : ISalvageInfo
{
    void OnSalvageRequested(SalvageRequested e);
}

/// <summary>
/// Components implementing this will receive information about the most recent destroy event.
/// </summary>
public interface IDestroyInfo
{
    IBaseBuildableDestroyedEvent? DestroyInfo { get; set; }
}