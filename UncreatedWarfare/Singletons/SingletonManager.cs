using JetBrains.Annotations;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using UnityEngine;

namespace Uncreated.Warfare.Singletons;
internal delegate void SingletonDelegate(IUncreatedSingleton singleton, bool success);
internal delegate void ReloadSingletonDelegate(IReloadableSingleton singleton, bool success);
internal class SingletonManager : MonoBehaviour
{
    private readonly List<SingletonInformation> _singletons = new List<SingletonInformation>(32);

    public IUncreatedSingleton[] GetSingletons()
    {
        ThreadUtil.assertIsGameThread();
        IUncreatedSingleton[] list = new IUncreatedSingleton[_singletons.Count];
        int i = -1;
        foreach (SingletonInformation singleton in _singletons)
        {
            list[++i] = singleton.Singleton;
        }
        return list;
    }
    /// <summary>Called when a singleton is loaded.</summary>
    public event SingletonDelegate? OnSingletonLoaded;

    /// <summary>Called when a singleton is loaded.</summary>
    public event SingletonDelegate? OnSingletonUnloaded;

    /// <summary>Called when a singleton is reloaded.</summary>
    public event ReloadSingletonDelegate? OnSingletonReloaded;

#pragma warning disable IDE0051
    [UsedImplicitly]
    private void Awake()
    {
        L.Log("Singleton system loaded", ConsoleColor.Blue);
    }
#pragma warning restore IDE0051

    /// <summary>Loads a singleton (asynchronously when available), unloading any others.</summary>
    /// <typeparam name="T">Type of singleton to load.</typeparam>
    /// <param name="throwErrors"><see langword="True"/> to throw any <see cref="SingletonLoadException"/>s that may be thrown upon loading. Otherwise, <see langword="false"/>.</param>
    /// <exception cref="NotSupportedException">Thrown if the function isn't executed on the game thread.</exception>
    /// <exception cref="SingletonLoadException">Thrown if <typeparamref name="T"/> isn't successfully loaded and if <paramref name="throwErrors"/> is <see langword="true"/>.</exception>
    /// <returns>The loaded singleton.</returns>
    public async Task<T> LoadSingletonAsync<T>(bool throwErrors = true, bool @lock = true, CancellationToken token = default) where T : class, IUncreatedSingleton
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        await UCWarfare.ToUpdate(token);
        Type inputType = typeof(T);
        T singleton = null!;
        PopulateSingleton(ref singleton, throwErrors);
        SingletonInformation info = new SingletonInformation(singleton);
        if (@lock)
            await WaitOrThrow(info, token).ConfigureAwait(false);
        try
        {
            await UCWarfare.ToUpdate(token);
            for (int i = 0; i < _singletons.Count; ++i)
            {
                if (_singletons[i].SingletonType == inputType)
                {
                    SingletonInformation ucs = _singletons[i];
                    if (ucs.IsLoaded)
                    {
                        await UnloadIntlAsync(ucs, throwErrors, token).ConfigureAwait(false);
                        await UCWarfare.ToUpdate(token);
                    }
                    _singletons[i] = info;
                    goto load;
                }
            }
            _singletons.Add(info);
            load:
            await LoadIntlAsync(info, throwErrors, token).ConfigureAwait(false);
        }
        finally
        {
            if (@lock)
                info.semaphore.Release();
        }
        return (info.Singleton as T)!;
    }
    /// <summary>Loads a singleton, (asynchronously when available), unloading any others.</summary>
    /// <param name="throwErrors"><see langword="True"/> to throw any <see cref="SingletonLoadException"/>s that may be thrown upon loading. Otherwise, <see langword="false"/>.</param>
    /// <exception cref="NotSupportedException">Thrown if the function isn't executed on the game thread.</exception>
    /// <exception cref="SingletonLoadException">Thrown if <paramref name="type"/> isn't successfully loaded and if <paramref name="throwErrors"/> is <see langword="true"/>.</exception>
    /// <exception cref="ArgumentException">Type is not derived from <see cref="IUncreatedSingleton"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if <paramref name="type"/> has to be loaded asynchronously.</exception>
    /// <returns>The loaded singleton.</returns>
    public async Task<IUncreatedSingleton> LoadSingletonAsync(Type type, bool throwErrors = true, bool @lock = true, CancellationToken token = default)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        await UCWarfare.ToUpdate(token);
        IUncreatedSingleton singleton = null!;
        PopulateSingleton(ref singleton, type, throwErrors);
        SingletonInformation info = new SingletonInformation(singleton);
        if (@lock)
            await WaitOrThrow(info, token).ConfigureAwait(false);
        try
        {
            await UCWarfare.ToUpdate(token);
            for (int i = 0; i < _singletons.Count; ++i)
            {
                if (_singletons[i].SingletonType == type)
                {
                    SingletonInformation ucs = _singletons[i];
                    if (ucs.IsLoaded)
                    {
                        await UnloadIntlAsync(ucs, throwErrors, token).ConfigureAwait(false);
                        await UCWarfare.ToUpdate(token);
                    }
                    _singletons[i] = info;
                    goto load;
                }
            }
            _singletons.Add(info);
            load:
            await LoadIntlAsync(info, throwErrors, token).ConfigureAwait(false);
        }
        finally
        {
            if (@lock)
                info.semaphore.Release();
        }
        return info.Singleton;
    }
    private async Task WaitOrThrow(SingletonInformation info, CancellationToken token = default)
    {
        if (!await info.semaphore.WaitAsync(10000, token).ConfigureAwait(false))
            throw new TimeoutException("Took more than 10 seconds to unlock semaphore.");
    }
    /// <summary>Loads a singleton that was created by <see cref="PopulateSingleton{T}(ref T, bool)"/>, (asynchronously when available).</summary>
    /// <typeparam name="T">Type of singleton to load.</typeparam>
    /// <param name="singleton">The result of running <see cref="PopulateSingleton{T}(ref T, bool)"/> that will finish loading.</param>
    /// <param name="throwErrors"><see langword="True"/> to throw any <see cref="SingletonLoadException"/>s that may be thrown upon loading. Otherwise, <see langword="false"/>.</param>
    /// <exception cref="NotSupportedException">Thrown if the function isn't executed on the game thread.</exception>
    /// <exception cref="SingletonLoadException">Thrown if <paramref name="singleton"/> isn't successfully loaded and if <paramref name="throwErrors"/> is <see langword="true"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if <paramref name="singleton"/> has to be loaded asynchronously.</exception>
    /// <returns>The loaded singleton.</returns>
    public async Task<T> LoadSingletonAsync<T>(T singleton, bool throwErrors = true, bool @lock = true, CancellationToken token = default) where T : class, IUncreatedSingleton
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Type inputType = singleton.GetType();
        SingletonInformation info = new SingletonInformation(singleton);
        if (@lock)
            await WaitOrThrow(info, token).ConfigureAwait(false);
        try
        {
            await UCWarfare.ToUpdate(token);
            for (int i = 0; i < _singletons.Count; ++i)
            {
                if (_singletons[i].SingletonType == inputType)
                {
                    SingletonInformation ucs = _singletons[i];
                    if (ucs.IsLoaded)
                    {
                        await WaitOrThrow(ucs, token).ConfigureAwait(false);
                        try
                        {
                            await UnloadIntlAsync(ucs, throwErrors, token).ConfigureAwait(false);
                            await UCWarfare.ToUpdate(token);
                        }
                        finally
                        {
                            ucs.semaphore.Release();
                        }
                    }
                    _singletons[i] = info;
                    goto load;
                }
            }
            _singletons.Add(info);
            load:
            await LoadIntlAsync(info, throwErrors, token).ConfigureAwait(false);
        }
        finally
        {
            if (@lock)
                info.semaphore.Release();
        }
        return (info.Singleton as T)!;
    }
    /// <summary>Creates a reference to a singleton object without loading it. Call <see cref="LoadSingleton{T}(T, bool)"/> to finish loading it.</summary>
    /// <typeparam name="T">Type of singleton to create.</typeparam>
    /// <param name="singleton">Reference to field that will be assigned the singleton.</param>
    /// <param name="throwErrors"><see langword="True"/> to throw any <see cref="SingletonLoadException"/>s that may be thrown upon loading. Otherwise, <see langword="false"/>.</param>
    /// <exception cref="NotSupportedException">Thrown if the function isn't executed on the game thread.</exception>
    /// <exception cref="SingletonLoadException">Thrown if <paramref name="singleton"/> isn't successfully loaded and if <paramref name="throwErrors"/> is <see langword="true"/>.</exception>
    public void PopulateSingleton<T>(ref T singleton, bool throwErrors) where T : class, IUncreatedSingleton
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ThreadUtil.assertIsGameThread();
        Type inputType = singleton?.GetType() ?? typeof(T);
        if (typeof(Component).IsAssignableFrom(inputType))
        {
            try
            {
                singleton = (gameObject.AddComponent(inputType) as T)!;
            }
            catch (Exception ex)
            {
                L.LogError(inputType.Name.ToProperCase() + " singleton threw an error when adding the component, likely an error in the Awake Unity function: ");
                L.LogError(ex);
                if (throwErrors)
                    throw new SingletonLoadException(SingletonLoadType.Load, null, ex);
                singleton = null!;
            }
        }
        else
        {
            singleton = Activator.CreateInstance<T>();
        }
    }
    /// <summary>Creates a reference to a singleton object without loading it. Call <see cref="LoadSingleton{T}(T, bool)"/> to finish loading it.</summary>
    /// <param name="type">Type of singleton to create.</param>
    /// <param name="singleton">Reference to field that will be assigned the singleton.</param>
    /// <param name="throwErrors"><see langword="True"/> to throw any <see cref="SingletonLoadException"/>s that may be thrown upon loading. Otherwise, <see langword="false"/>.</param>
    /// <exception cref="NotSupportedException">Thrown if the function isn't executed on the game thread.</exception>
    /// <exception cref="SingletonLoadException">Thrown if <paramref name="singleton"/> isn't successfully loaded and if <paramref name="throwErrors"/> is <see langword="true"/>.</exception>
    public void PopulateSingleton(ref IUncreatedSingleton singleton, Type type, bool throwErrors)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!typeof(IUncreatedSingleton).IsAssignableFrom(type))
            throw new ArgumentException("Type is not derived from " + nameof(IUncreatedSingleton), nameof(type));
        ThreadUtil.assertIsGameThread();
        if (typeof(Component).IsAssignableFrom(type))
        {
            try
            {
                singleton = (gameObject.AddComponent(type) as IUncreatedSingleton)!;
            }
            catch (Exception ex)
            {
                L.LogError(type.Name.ToProperCase() + " singleton threw an error when adding the component, likely an error in the Awake Unity function: ");
                L.LogError(ex);
                if (throwErrors)
                    throw new SingletonLoadException(SingletonLoadType.Load, null, ex);
                singleton = null!;
            }
        }
        else
        {
            singleton = (Activator.CreateInstance(type) as IUncreatedSingleton)!;
        }
    }
    internal async Task LoadSingletonsInOrderAsync(List<IUncreatedSingleton> singletons, CancellationToken token = default)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        List<SingletonDependencyInformation> info = new List<SingletonDependencyInformation>(singletons.Count);
        List<SingletonDependencyInformation> loadOrder = new List<SingletonDependencyInformation>(singletons.Count);
        for (int i = 0; i < singletons.Count; ++i)
            info.Add(new SingletonDependencyInformation(singletons[i]));
        info.Sort((a, b) => a.Dependencies.Length.CompareTo(b.Dependencies.Length));
        List<Type> tree = new List<Type>(4);
        for (int i = 0; i < info.Count; ++i)
            await LoadAsync(info[i], tree, info, loadOrder, token: token).ConfigureAwait(false);
        for (int i = 0; i < info.Count; ++i)
        {
            if (!info[i].isLoaded)
            {
                await LoadSingletonAsync(info[i].Singleton, token: token).ConfigureAwait(false);
                loadOrder.Add(info[i]);
            }
        }
        // sorts the elements in singletons in the order they were loaded.
        for (int i = 0; i < loadOrder.Count; ++i)
        {
            SingletonDependencyInformation order = loadOrder[i];
            for (int j = i; j < singletons.Count; ++j)
            {
                if (ReferenceEquals(order.Singleton, singletons[j]))
                {
                    if (i != j && i < singletons.Count - 1)
                    {
                        singletons.RemoveAt(j);
                        singletons.Insert(i, order.Singleton);
                    }
                    break;
                }
            }
        }
    }
    internal async Task<bool> UnloadSingletonsInOrderAsync(List<IUncreatedSingleton> singletons, CancellationToken token = default)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        bool success = true;
        for (int i = singletons.Count - 1; i >= 0; --i)
        {
            IUncreatedSingleton sgl = singletons[i];
            success |= (await UnloadSingletonAsync(sgl, token: token).ConfigureAwait(false));
            singletons[i] = sgl;
        }
        return success;
    }
    private void CheckLoadingUnloadingStatus(SingletonInformation singleton)
    {
        if (singleton.Singleton.IsLoading)
            throw new InvalidOperationException("Singleton " + singleton.SingletonType.Name + " is already loading.");
        if (singleton.Singleton.IsUnloading)
            throw new InvalidOperationException("Singleton " + singleton.SingletonType.Name + " is already loading.");
    }
    // recursive
    private async Task LoadAsync(SingletonDependencyInformation dep, List<Type> tree, List<SingletonDependencyInformation> singletons, List<SingletonDependencyInformation> loadOrder, CancellationToken token = default)
    {
        if (dep.isLoaded) return;
        bool circ = tree.Contains(dep.SingletonType);
        if (dep.Dependencies.Length == 0)
        {
            if (!dep.isLoaded)
            {
                await LoadSingletonAsync(dep.Singleton, @lock: false, token: token).ConfigureAwait(false);
                loadOrder.Add(dep);
                dep.isLoaded = true;
            }
        }
        else
        {
            tree.Add(dep.SingletonType);
            for (int i = 0; i < dep.Dependencies.Length; ++i)
            {
                Type? dep2 = dep.Dependencies[i];
                if (dep2 is null) continue;
                for (int j = 0; j < singletons.Count; ++j)
                {
                    if (singletons[j].SingletonType == dep2)
                    {
                        if (!singletons[j].isLoaded)
                        {
                            if (circ)
                            {
                                L.LogWarning("Circular reference detected from the singleton dependency heirarchy: " + string.Join(" > ", tree.Select(x => x.Name)) + ", skipping singleton.");
                                tree.Remove(dep.SingletonType);

                                await LoadSingletonAsync(dep.Singleton, @lock: false, token: token).ConfigureAwait(false);
                                loadOrder.Add(dep);
                                dep.isLoaded = true;
                                return;
                            }
                            await LoadAsync(singletons[j], tree, singletons, loadOrder, token).ConfigureAwait(false);
                        }
                        break;
                    }
                }
            }
            tree.Remove(dep.SingletonType);

            await LoadSingletonAsync(dep.Singleton, @lock: false, token: token).ConfigureAwait(false);
            loadOrder.Add(dep);
            dep.isLoaded = true;
        }
    }
    private async Task LoadIntlAsync(SingletonInformation singleton, bool rethrow, CancellationToken token = default)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        singleton.IsLoaded = false;
        CheckLoadingUnloadingStatus(singleton);
        try
        {
            await singleton.LoadAsync(token).ConfigureAwait(false);
            singleton.IsLoaded = singleton.Singleton.IsLoaded;
            if (OnSingletonLoaded != null)
            {
                await UCWarfare.ToUpdate(token);
                OnSingletonLoaded.Invoke(singleton.Singleton, singleton.IsLoaded);
            }
        }
        catch (Exception ex)
        {
            L.LogError("Ran into an error loading: " + singleton.Name);
            L.LogError(ex);
            if (OnSingletonLoaded != null)
            {
                await UCWarfare.ToUpdate(token);
                OnSingletonLoaded.Invoke(singleton.Singleton, false);
            }
            if (rethrow)
                throw new SingletonLoadException(SingletonLoadType.Load, singleton.Singleton, ex);
        }
    }

    /// <summary>Unloads a singleton.</summary>
    /// <typeparam name="T">Type of singleton to unload.</typeparam>
    /// <param name="throwErrors"><see langword="True"/> to throw any <see cref="SingletonLoadException"/>s that may be thrown upon unloading. Otherwise, <see langword="false"/>.</param>
    /// <exception cref="NotSupportedException">Thrown if the function isn't executed on the game thread.</exception>
    /// <exception cref="SingletonLoadException">Thrown if <typeparamref name="T"/> isn't successfully unloaded and if <paramref name="throwErrors"/> is <see langword="true"/>.</exception>
    /// <returns><see langword="True"/> if the singleton was successfully unloaded and removed, otherwise <see langword="false"/>.</returns>
    public Task<bool> UnloadSingletonAsync<T>(bool throwErrors = false, bool @lock = true, CancellationToken token = default)
        where T : class, IUncreatedSingleton
        => UnloadSingletonAsync(typeof(T), throwErrors, @lock, token);

    /// <summary>Unloads a singleton.</summary>
    /// <param name="type">Type of singleton to unload.</param>
    /// <param name="throwErrors"><see langword="True"/> to throw any <see cref="SingletonLoadException"/>s that may be thrown upon unloading. Otherwise, <see langword="false"/>.</param>
    /// <exception cref="NotSupportedException">Thrown if the function isn't executed on the game thread.</exception>
    /// <exception cref="SingletonLoadException">Thrown if <typeparamref name="T"/> isn't successfully unloaded and if <paramref name="throwErrors"/> is <see langword="true"/>.</exception>
    /// <returns><see langword="True"/> if the singleton was successfully unloaded and removed, otherwise <see langword="false"/>.</returns>
    public async Task<bool> UnloadSingletonAsync(Type type, bool throwErrors = false, bool @lock = true, CancellationToken token = default)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        SingletonInformation? info = null;
        await UCWarfare.ToUpdate(token);
        for (int i = _singletons.Count - 1; i >= 0; --i)
        {
            if (_singletons[i].SingletonType == type)
            {
                info = _singletons[i];
                _singletons.RemoveAt(i);
                break;
            }
        }
        if (info is null)
            return false;
        if (@lock)
            await WaitOrThrow(info, token).ConfigureAwait(false);
        try
        {
            bool state = await UnloadIntlAsync(info, throwErrors, token).ConfigureAwait(false);
            if (OnSingletonUnloaded != null)
            {
                await UCWarfare.ToUpdate(token);
                OnSingletonUnloaded.Invoke(info.Singleton, state);
            }
            info.Singleton = null!;
            info.SingletonType = null!;
            info.Name = null!;
            return state;
        }
        finally
        {
            if (@lock)
                info.semaphore.Release();
        }
    }
    /// <summary>Unloads a singleton.</summary>
    /// <typeparam name="T">Type of singleton to unload.</typeparam>
    /// <param name="throwErrors"><see langword="True"/> to throw any <see cref="SingletonLoadException"/>s that may be thrown upon unloading. Otherwise, <see langword="false"/>.</param>
    /// <param name="field">Will set this field to null if it was properly unloaded.</param>
    /// <exception cref="NotSupportedException">Thrown if the function isn't executed on the game thread.</exception>
    /// <exception cref="SingletonLoadException">Thrown if <typeparamref name="T"/> isn't successfully unloaded and if <paramref name="throwErrors"/> is <see langword="true"/>.</exception>
    /// <returns><see langword="True"/> if the singleton was successfully unloaded and removed, otherwise <see langword="false"/>.</returns>
    public async Task<bool> UnloadSingletonAsync<T>(T field, bool throwErrors = false, bool @lock = true, CancellationToken token = default) where T : class, IUncreatedSingleton
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Type inputType = field?.GetType() ?? typeof(T);
        SingletonInformation? info = null;
        await UCWarfare.ToUpdate(token);
        for (int i = _singletons.Count - 1; i >= 0; --i)
        {
            if (_singletons[i].SingletonType == inputType)
            {
                info = _singletons[i];
                _singletons.RemoveAt(i);
                break;
            }
        }
        if (info is null)
            return false;
        if (@lock)
            await WaitOrThrow(info, token).ConfigureAwait(false);
        try
        {
            bool state = await UnloadIntlAsync(info, throwErrors, token).ConfigureAwait(false);
            if (OnSingletonUnloaded != null)
            {
                await UCWarfare.ToUpdate(token);
                OnSingletonUnloaded.Invoke(info.Singleton, state);
            }
            info.Singleton = null!;
            info.SingletonType = null!;
            info.Name = null!;
            return state;
        }
        finally
        {
            if (@lock)
                info.semaphore.Release();
        }
    }
    private async Task<bool> UnloadIntlAsync(SingletonInformation singleton, bool rethrow, CancellationToken token = default)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        singleton.IsLoaded = false;
        CheckLoadingUnloadingStatus(singleton);
        try
        {
            await singleton.UnloadAsync(token).ConfigureAwait(false);
            if (singleton.Singleton is Component comp)
            {
                await UCWarfare.ToUpdate(token);
                Destroy(comp);
            }
            return true;
        }
        catch (Exception ex)
        {
            L.LogError("Ran into an error unloading: " + singleton.Name);
            L.LogError(ex);
            if (rethrow)
                throw new SingletonLoadException(SingletonLoadType.Unload, singleton.Singleton, ex);
            return false;
        }
    }
    /// <summary>Reloads a <see cref="IReloadableSingleton"/> with the provided <paramref name="key"/>.</summary>
    /// <param name="key"><see cref="IReloadableSingleton.ReloadKey"/> of the singleton to reload.</param>
    /// <exception cref="NotSupportedException">Thrown if the function isn't executed on the game thread.</exception>
    /// <exception cref="SingletonLoadException">Thrown if the singleton represented by <paramref name="key"/> isn't successfully reloaded.</exception>
    /// <returns>The singleton if it was found, otherwise <see langword="null"/>.</returns>
    public async Task<IReloadableSingleton?> ReloadSingletonAsync(string key, CancellationToken token = default)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        await UCWarfare.ToUpdate(token);
        for (int i = 0; i < _singletons.Count; ++i)
        {
            string? k1 = _singletons[i].ReloadKey;
            if (k1 is not null && k1.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                SingletonInformation s = _singletons[i];
                await WaitOrThrow(s, token).ConfigureAwait(false);
                try
                {
                    IReloadableSingleton? reloadable = await ReloadIntlAsync(s, true, token).ConfigureAwait(false);
                    return reloadable;
                }
                finally
                {
                    s.semaphore.Release();
                }
            }
        }
        return null;
    }

    public async Task<IReloadableSingleton?> ReloadSingletonAsync(IReloadableSingleton singleton, CancellationToken token = default)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        await UCWarfare.ToUpdate(token);
        await UCWarfare.ToUpdate(token);
        for (int i = 0; i < _singletons.Count; ++i)
        {
            if (ReferenceEquals(_singletons[i].Singleton, singleton))
            {
                SingletonInformation s = _singletons[i];
                await WaitOrThrow(s, token).ConfigureAwait(false);
                try
                {
                    IReloadableSingleton? reloadable = await ReloadIntlAsync(s, true, token).ConfigureAwait(false);
                    return reloadable;
                }
                finally
                {
                    s.semaphore.Release();
                }
            }
        }
        return null;
    }
    private async Task<IReloadableSingleton?> ReloadIntlAsync(SingletonInformation singleton, bool rethrow, CancellationToken token = default)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (singleton.Singleton is IReloadableSingleton reloadable)
        {
            CheckLoadingUnloadingStatus(singleton);
            singleton.IsLoaded = false;
            try
            {
                await reloadable.ReloadAsync(token).ConfigureAwait(false);
                singleton.IsLoaded = true;
                if (OnSingletonReloaded != null)
                {
                    await UCWarfare.ToUpdate(token);
                    OnSingletonReloaded.Invoke(reloadable, true);
                }
            }
            catch (Exception ex)
            {
                L.LogError("Ran into an error reloading: " + singleton.Name);
                L.LogError(ex);
                if (OnSingletonReloaded != null)
                {
                    await UCWarfare.ToUpdate(token);
                    OnSingletonReloaded.Invoke(reloadable, false);
                }
                if (rethrow)
                    throw new SingletonLoadException(SingletonLoadType.Reload, singleton.Singleton, ex);
            }
            return reloadable;
        }
        return null;
    }
    /// <summary>Get a singleton by type.</summary>
    /// <typeparam name="T">Type of <see cref="IUncreatedSingleton"/> to get.</typeparam>
    /// <returns>Singleton of type <typeparamref name="T"/>, or <see langword="null"/> if it isn't found.</returns>
    public T? GetSingleton<T>() where T : class, IUncreatedSingleton
    {
        ThreadUtil.assertIsGameThread();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < _singletons.Count; ++i)
            if (_singletons[i].Singleton is T v)
                return v;
        return null;
    }

    public bool TryGetSingleton<T>(out T loaded) where T : class, IUncreatedSingleton
    {
        ThreadUtil.assertIsGameThread();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < _singletons.Count; ++i)
        {
            if (_singletons[i].Singleton is T v)
            {
                loaded = v;
                return v.IsLoaded;
            }
        }

        loaded = null!;
        return false;
    }
    /// <summary>Get a singleton by type.</summary>
    /// <typeparam name="T">Type of <see cref="IUncreatedSingleton"/> to get.</typeparam>
    /// <returns>Singleton of type <typeparamref name="T"/>, or <see langword="null"/> if it isn't found.</returns>
    public IUncreatedSingleton? GetSingleton(Type type)
    {
        ThreadUtil.assertIsGameThread();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < _singletons.Count; ++i)
            if (_singletons[i].SingletonType == type)
                return _singletons[i].Singleton;
        return null;
    }
    /// <summary>Check if a singleton is loaded.</summary>
    /// <typeparam name="T">Type of <see cref="IUncreatedSingleton"/> to check for.</typeparam>
    /// <returns><see langword="True"/> if <typeparamref name="T"/> singleton is loaded, otherwise <see langword="false"/>.</returns>
    public bool IsLoaded<T>() where T : class, IUncreatedSingleton
    {
        ThreadUtil.assertIsGameThread();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < _singletons.Count; ++i)
            if (_singletons[i].SingletonType is T)
                return _singletons[i].IsLoaded;
        return false;
    }
    /// <summary>
    /// Unload all currently loaded singletons.
    /// </summary>
    public async Task UnloadAllAsync(CancellationToken token = default)
    {
        await UCWarfare.ToUpdate(token);
        List<SingletonInformation> list = new List<SingletonInformation>(_singletons);
        _singletons.Clear();
        foreach (SingletonInformation info in list)
        {
            if (info.IsLoaded)
            {
                await UnloadIntlAsync(info, false, token).ConfigureAwait(false);
            }
        }
    }
    private class SingletonInformation
    {
        public readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        public readonly string? ReloadKey;
        public bool IsLoaded;
        public IUncreatedSingleton Singleton;
        public Type SingletonType;
        public string Name;
        public bool RequiresLevel;
        public SingletonInformation(IUncreatedSingleton singleton)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            Singleton = singleton;
            IsLoaded = false;
            SingletonType = singleton.GetType();
            if (Attribute.GetCustomAttributes(SingletonType, 
                    typeof(SingletonDependencyAttribute))
                .Any(x => x is SingletonDependencyAttribute attr &&
                          attr.Dependency == typeof(Level)))
            {
                RequiresLevel = true;
            }
            Name = SingletonType.Name.ToProperCase();
            if (singleton is IReloadableSingleton reloadable)
                ReloadKey = reloadable.ReloadKey;
        }
        /// <exception cref="SingletonLoadException"/>
        public async Task UnloadAsync(CancellationToken token = default)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            await UCWarfare.ToUpdate(token);
            if (Singleton.LoadAsynchrounously)
            {
                await Singleton.UnloadAsync(token).ConfigureAwait(false);
                IsLoaded = Singleton.IsLoaded;
            }
            else
            {
                await UCWarfare.ToUpdate(token);
                // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                Singleton.Unload();
            }
        }
        /// <exception cref="SingletonLoadException"/>
        public async Task LoadAsync(CancellationToken token = default)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            await UCWarfare.ToUpdate(token);
            if (RequiresLevel && !Level.isLoaded)
            {
                await UCWarfare.ToLevelLoad(token);
            }
            if (Singleton.LoadAsynchrounously)
            {
                await Singleton.LoadAsync(token).ConfigureAwait(false);
                IsLoaded = Singleton.IsLoaded;
            }
            else
            {
                // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                Singleton.Load();
            }
        }
    }

    private class SingletonDependencyInformation
    {
        public readonly Type?[] Dependencies;
        //public List<Type>? Dependents;
        public readonly Type SingletonType;
        public readonly IUncreatedSingleton Singleton;
        public bool isLoaded = false;
        public SingletonDependencyInformation(IUncreatedSingleton singleton)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            Singleton = singleton;
            SingletonType = singleton.GetType();
            Attribute[] attributes = Attribute.GetCustomAttributes(SingletonType, typeof(SingletonDependencyAttribute));
            Dependencies = new Type[attributes.Length];
            for (int i = 0; i < attributes.Length; ++i)
            {
                if (attributes[i] is SingletonDependencyAttribute attr && attr.Dependency != SingletonType)
                {
                    for (int j = 0; j < i; ++j)
                        if (Dependencies[j] == attr.Dependency)
                            goto next;
                    Dependencies[i] = attr.Dependency;
                }
                next:;
            }
        }
    }
}

/// <summary>Thrown by <see cref="SingletonManager"/> when loading or unloading a <see cref="IUncreatedSingleton"/> goes wrong.</summary>
[Serializable]
public class SingletonLoadException : Exception
{
    public readonly SingletonLoadType LoadType;
    public readonly IUncreatedSingleton? Singleton;
    public SingletonLoadException(SingletonLoadType loadType, IUncreatedSingleton? singleton, string message) : this(loadType, singleton, new Exception(message)) { }
    public SingletonLoadException(SingletonLoadType loadType, IUncreatedSingleton? singleton, Exception inner) : base(GetMessage(loadType, singleton, inner, inner.StackTrace), inner)
    {
        LoadType = loadType;
        Singleton = singleton;
    }
    protected SingletonLoadException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    private static string GetMessage(SingletonLoadType loadType, IUncreatedSingleton? singleton, Exception innerException, string innerStack)
    {
        if (innerException is not null && innerException.GetType() != typeof(SingletonLoadException))
            return "Exception while " + loadType.ToString().ToLower() + "ing " + (singleton?.GetType()?.Name?.ToProperCase() ?? "unknown") + " singleton: \n" + innerException.ToString();
        else
            return "Unknown exception while " + loadType.ToString().ToLower() + "ing " + (singleton?.GetType()?.Name?.ToProperCase() ?? "unknown") + " singleton: \n" + innerStack;
    }
    public override string ToString()
    {
        if (InnerException is not null && InnerException.GetType() != typeof(SingletonLoadException))
            return InnerException.GetType().Name + " while " + LoadType.ToString().ToLower() + "ing " + (Singleton?.GetType()?.Name?.ToProperCase() ?? "unknown") + " singleton: \n" + StackTrace;
        else
            return "Unknown exception while " + LoadType.ToString().ToLower() + "ing " + (Singleton?.GetType()?.Name?.ToProperCase() ?? "unknown") + " singleton: \n" + StackTrace;
    }
}
/// <summary>Thrown by <see cref="SingletonEx.AssertLoaded{T}()"/> and <see cref="SingletonEx.AssertLoaded{T}(bool)"/> if the <see cref="IUncreatedSingleton"/> they reference isn't loaded.</summary>
[Serializable]
public class SingletonUnloadedException : Exception
{
    private readonly Type? singletonType;
    public SingletonUnloadedException(Type singletonType) : base("Error executing code in an unloaded singleton: " +
        (singletonType?.Name ?? "Unknown singleton") + " is not currently loaded.")
    { this.singletonType = singletonType; }
    protected SingletonUnloadedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    public override string ToString()
    {
        if (singletonType is not null && Message is not null)
            return this.Message + "\n" + StackTrace;
        else
            return base.ToString();
    }
}
public enum SingletonLoadType : byte
{
    Unknown = 0,
    Load = 1,
    Unload = 2,
    Reload = 3
}