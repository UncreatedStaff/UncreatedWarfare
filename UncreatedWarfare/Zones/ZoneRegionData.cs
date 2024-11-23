using System;
using System.Collections.Generic;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Zones;
public class ZoneRegionData : IDisposable
{
    private readonly ZoneRegion _region;
    private readonly Dictionary<Type, object> _types;

    /// <summary>
    /// Invoked when the zone is being disposed.
    /// </summary>
    public event Action? Disposing;

    /// <summary>
    /// The cluster linked to this data.
    /// </summary>
    public ZoneRegion Cluster => _region;
    public ZoneRegionData(ZoneRegion cluster)
    {
        _region = cluster;
        _types = new Dictionary<Type, object>();
    }

    /// <summary>
    /// Get or add a component 
    /// </summary>
    public TComponent GetOrAdd<TComponent>() where TComponent : new()
    {
        lock (_types)
        {
            if (!_types.TryGetValue(typeof(TComponent), out object component))
            {
                _types.Add(typeof(TComponent), component = typeof(TComponent).IsValueType ? default! : new TComponent());
            }

            return (TComponent)component;
        }
    }
    
    /// <summary>
    /// Get or add a component made from a service provider.
    /// </summary>
    public TComponent GetOrAdd<TComponent>(IServiceProvider serviceProvider) where TComponent : class
    {
        lock (_types)
        {
            if (!_types.TryGetValue(typeof(TComponent), out object component))
            {
                _types.Add(typeof(TComponent), component = ReflectionUtility.CreateInstanceFixed(serviceProvider, typeof(TComponent), [ _region ]));
            }

            return (TComponent)component;
        }
    }

    public void Dispose()
    {
        lock (_types)
        {
            foreach (object value in _types.Values)
            {
                if (value is IDisposable disp)
                    disp.Dispose();
            }
        }

        Disposing?.Invoke();
    }
}