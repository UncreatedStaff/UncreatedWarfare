using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

namespace Uncreated.Warfare.Singletons;
internal delegate void SingletonDelegate(IUncreatedSingleton singleton, bool success);
internal delegate void ReloadSingletonDelegate(IReloadableSingleton singleton, bool success);
internal class SingletonManager : MonoBehaviour
{
    private readonly List<SingletonInformation> singletons = new List<SingletonInformation>(32);
    /// <summary>Called when a singleton is loaded.</summary>
    public event SingletonDelegate? OnSingletonLoaded;

    /// <summary>Called when a singleton is loaded.</summary>
    public event SingletonDelegate? OnSingletonUnloaded;

    /// <summary>Called when a singleton is reloaded.</summary>
    public event ReloadSingletonDelegate? OnSingletonReloaded;

#pragma warning disable IDE0051
    private void Awake()
    {
        L.Log("Singleton system loaded", ConsoleColor.Blue);
    }
#pragma warning restore IDE0051

    /// <summary>Loads a singleton, unloading any others.</summary>
    /// <typeparam name="T">Type of singleton to load.</typeparam>
    /// <param name="throwErrors"><see langword="True"/> to throw any <see cref="SingletonLoadException"/>s that may be thrown upon loading. Otherwise, <see langword="false"/>.</param>
    /// <exception cref="NotSupportedException">Thrown if the function isn't executed on the game thread.</exception>
    /// <exception cref="SingletonLoadException">Thrown if <paramref name="singleton"/> isn't successfully loaded and if <paramref name="throwErrors"/> is <see langword="true"/>.</exception>
    /// <returns>The loaded singleton.</returns>
    public T LoadSingleton<T>(bool throwErrors = true) where T : class, IUncreatedSingleton
    {
        ThreadUtil.assertIsGameThread();
        Type inputType = typeof(T);
        T singleton = null!;
        PopulateSingleton(ref singleton, throwErrors);
        SingletonInformation info = new SingletonInformation(singleton);
        lock (singletons)
        {
            for (int i = 0; i < singletons.Count; ++i)
            {
                if (singletons[i].SingletonType == inputType)
                {
                    SingletonInformation ucs = singletons[i];
                    if (ucs.IsLoaded)
                    {
                        UnloadIntl(ucs, throwErrors);
                    }
                    singletons[i] = info;
                    goto load;
                }
            }
        }
        singletons.Add(info);
    load:
        LoadIntl(info, throwErrors);
        return (info.Singleton as T)!;
    }
    /// <summary>Loads a singleton, unloading any others.</summary>
    /// <typeparam name="T">Type of singleton to load.</typeparam>
    /// <param name="throwErrors"><see langword="True"/> to throw any <see cref="SingletonLoadException"/>s that may be thrown upon loading. Otherwise, <see langword="false"/>.</param>
    /// <exception cref="NotSupportedException">Thrown if the function isn't executed on the game thread.</exception>
    /// <exception cref="SingletonLoadException">Thrown if <paramref name="singleton"/> isn't successfully loaded and if <paramref name="throwErrors"/> is <see langword="true"/>.</exception>
    /// <exception cref="ArgumentException">Type is not derived from <see cref="IUncreatedSingleton"/>.</exception>
    /// <returns>The loaded singleton.</returns>
    public IUncreatedSingleton LoadSingleton(Type type, bool throwErrors = true)
    {
        if (!typeof(IUncreatedSingleton).IsAssignableFrom(type))
            throw new ArgumentException("Type is not derived from " + nameof(IUncreatedSingleton), nameof(type));
        ThreadUtil.assertIsGameThread();
        IUncreatedSingleton singleton = null!;
        PopulateSingleton(ref singleton, type, throwErrors);
        SingletonInformation info = new SingletonInformation(singleton);
        lock (singletons)
        {
            for (int i = 0; i < singletons.Count; ++i)
            {
                if (singletons[i].SingletonType == type)
                {
                    SingletonInformation ucs = singletons[i];
                    if (ucs.IsLoaded)
                    {
                        UnloadIntl(ucs, throwErrors);
                    }
                    singletons[i] = info;
                    goto load;
                }
            }
        }
        singletons.Add(info);
    load:
        LoadIntl(info, throwErrors);
        return info.Singleton;
    }
    /// <summary>Loads a singleton that was created by <see cref="PopulateSingleton{T}(ref T, bool)"/>.</summary>
    /// <typeparam name="T">Type of singleton to load.</typeparam>
    /// <param name="singleton">The result of running <see cref="PopulateSingleton{T}(ref T, bool)"/> that will finish loading.</param>
    /// <param name="throwErrors"><see langword="True"/> to throw any <see cref="SingletonLoadException"/>s that may be thrown upon loading. Otherwise, <see langword="false"/>.</param>
    /// <exception cref="NotSupportedException">Thrown if the function isn't executed on the game thread.</exception>
    /// <exception cref="SingletonLoadException">Thrown if <paramref name="singleton"/> isn't successfully loaded and if <paramref name="throwErrors"/> is <see langword="true"/>.</exception>
    /// <returns>The loaded singleton.</returns>
    public T LoadSingleton<T>(T singleton, bool throwErrors = true) where T : class, IUncreatedSingleton
    {
        ThreadUtil.assertIsGameThread();
        Type inputType = singleton.GetType();
        SingletonInformation info = new SingletonInformation(singleton);
        lock (singletons)
        {
            for (int i = 0; i < singletons.Count; ++i)
            {
                if (singletons[i].SingletonType == inputType)
                {
                    SingletonInformation ucs = singletons[i];
                    if (ucs.IsLoaded)
                    {
                        UnloadIntl(ucs, throwErrors);
                    }
                    singletons[i] = info;
                    goto load;
                }
            }
        }
        singletons.Add(info);
    load:
        LoadIntl(info, throwErrors);
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
                    throw new SingletonLoadException(ESingletonLoadType.LOAD, null, ex);
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
                    throw new SingletonLoadException(ESingletonLoadType.LOAD, null, ex);
                singleton = null!;
            }
        }
        else
        {
            singleton = (Activator.CreateInstance(type) as IUncreatedSingleton)!;
        }
    }

    internal void LoadSingletonsInOrder(List<IUncreatedSingleton> singletons)
    {
        List<SingletonDependencyInformation> info = new List<SingletonDependencyInformation>(singletons.Count);
        List<SingletonDependencyInformation> loadOrder = new List<SingletonDependencyInformation>(singletons.Count);
        for (int i = 0; i < singletons.Count; ++i)
            info.Add(new SingletonDependencyInformation(singletons[i]));
        info.Sort((a, b) => a.Dependencies.Length.CompareTo(b.Dependencies.Length));
        List<Type> tree = new List<Type>(4);
        for (int i = 0; i < info.Count; ++i)
            Load(info[i], tree, info, loadOrder);
        for (int i = 0; i < info.Count; ++i)
        {
            if (!info[i].isLoaded)
            {
                LoadSingleton(info[i].Singleton);
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
    internal bool UnloadSingletonsInOrder(List<IUncreatedSingleton> singletons)
    {
        bool success = true;
        for (int i = singletons.Count - 1; i >= 0; --i)
        {
            IUncreatedSingleton sgl = singletons[i];
            success |= UnloadSingleton(ref sgl);
            singletons[i] = sgl;
        }
        return success;
#if false
        List<SingletonDependencyInformation> info = new List<SingletonDependencyInformation>(singletons.Count);
        for (int i = 0; i < singletons.Count; ++i)
            info[i] = new SingletonDependencyInformation(singletons[i]) { isLoaded = true };
        for (int i = 0; i < info.Count; ++i)
        {
            SingletonDependencyInformation i2 = info[i];
            i2.Dependents = new List<Type>(4);
            for (int j = 0; j < info.Count; ++j)
            {
                if (i == j) continue;
                SingletonDependencyInformation i3 = info[j];
                for (int t = 0; t < i3.Dependencies.Length; ++t)
                {
                    Type? d = i3.Dependencies[t];
                    if (d is null) continue;
                    if (d == i2.SingletonType)
                    {
                        for (int x = 0; x < i2.Dependents.Count; ++x)
                        {
                            if (d == i2.Dependents[x])
                                goto next;
                        }
                        i2.Dependents.Add(d);
                    next:;
                    }
                }
            }
        }
        info.Sort((a, b) => a.Dependencies.Length.CompareTo(b.Dependencies.Length));
        List<Type> tree = new List<Type>(4);
        for (int i = 0; i < info.Count; ++i)
            Unload(info[i], tree, info);
        for (int i = 0; i < info.Count; ++i)
        {
            if (info[i].isLoaded)
            {
                UnloadSingleton(ref info[i].Singleton);
            }
        }
        info.Clear();
        singletons.Clear();
#endif
    }
    // recursive
    private void Load(SingletonDependencyInformation dep, List<Type> tree, List<SingletonDependencyInformation> singletons, List<SingletonDependencyInformation> loadOrder)
    {
        if (dep.isLoaded) return;
        bool circ = tree.Contains(dep.SingletonType);
        if (dep.Dependencies.Length == 0)
        {
            if (!dep.isLoaded)
            {
                LoadSingleton(dep.Singleton);
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

                                LoadSingleton(dep.Singleton);
                                loadOrder.Add(dep);
                                dep.isLoaded = true;
                                return;
                            }
                            else
                            {
                                Load(singletons[j], tree, singletons, loadOrder);
                            }
                        }
                        break;
                    }
                }
            }
            tree.Remove(dep.SingletonType);

            LoadSingleton(dep.Singleton);
            loadOrder.Add(dep);
            dep.isLoaded = true;
        }
    }
#if false
    // recursive
    private void Unload(SingletonDependencyInformation dep, List<Type> tree, List<SingletonDependencyInformation> singletons)
    {
        if (!dep.isLoaded) return;
        bool circ = tree.Contains(dep.SingletonType);
        if (dep.Dependents!.Count == 0)
        {
            if (dep.isLoaded)
            {
                UnloadSingleton(ref dep.Singleton);
                dep.isLoaded = false;
            }
        }
        else
        {
            tree.Add(dep.SingletonType);
            for (int i = 0; i < dep.Dependents.Count; ++i)
            {
                Type dep2 = dep.Dependents[i];
                for (int j = 0; j < singletons.Count; ++j)
                {
                    if (singletons[j].GetType() == dep2)
                    {
                        if (!singletons[j].isLoaded)
                        {
                            if (circ)
                            {
                                L.LogWarning("Circular reference detected from the singleton dependency heirarchy: " + string.Join(" > ", tree.Select(x => x.Name)));
                                tree.Remove(dep.SingletonType);

                                UnloadSingleton(ref dep.Singleton);
                                dep.isLoaded = false;
                                return;
                            }
                            else
                            {
                                Unload(singletons[j], tree, singletons);
                            }
                        }
                        break;
                    }
                }
            }
            tree.Remove(dep.SingletonType);

            UnloadSingleton(ref dep.Singleton);
            dep.isLoaded = false;
        }
    }
#endif
    private void LoadIntl(SingletonInformation singleton, bool rethrow)
    {
        singleton.IsLoaded = false;
        try
        {
            singleton.Load();
            singleton.ErroredOnLoad = false;
            singleton.IsLoaded = true;
            OnSingletonLoaded?.Invoke(singleton.Singleton, true);
        }
        catch (NotSupportedException ex)
        {
            L.LogError("Tried to load a singleton without being on the game thread: " + singleton.Name);
            singleton.ErroredOnLoad = true;
            OnSingletonLoaded?.Invoke(singleton.Singleton, false);
            if (rethrow)
                throw new SingletonLoadException(ESingletonLoadType.LOAD, singleton.Singleton, ex);
        }
        catch (Exception ex)
        {
            L.LogError("Ran into an error loading: " + singleton.Name);
            L.LogError(ex);
            singleton.ErroredOnLoad = true;
            OnSingletonLoaded?.Invoke(singleton.Singleton, false);
            if (rethrow)
                throw new SingletonLoadException(ESingletonLoadType.LOAD, singleton.Singleton, ex);
        }
    }
    /// <summary>Unloads a singleton.</summary>
    /// <typeparam name="T">Type of singleton to unload.</typeparam>
    /// <param name="throwErrors"><see langword="True"/> to throw any <see cref="SingletonLoadException"/>s that may be thrown upon unloading. Otherwise, <see langword="false"/>.</param>
    /// <exception cref="NotSupportedException">Thrown if the function isn't executed on the game thread.</exception>
    /// <exception cref="SingletonLoadException">Thrown if <paramref name="singleton"/> isn't successfully unloaded and if <paramref name="throwErrors"/> is <see langword="true"/>.</exception>
    /// <returns><see langword="True"/> if the singleton was successfully unloaded and removed, otherwise <see langword="false"/>.</returns>
    public bool UnloadSingleton<T>(bool throwErrors = false) where T : class, IUncreatedSingleton
    {
        ThreadUtil.assertIsGameThread();
        Type inputType = typeof(T);
        SingletonInformation? info = null;
        lock (singletons)
        {
            for (int i = singletons.Count - 1; i >= 0; --i)
            {
                if (singletons[i].SingletonType == inputType)
                {
                    info = singletons[i];
                    singletons.RemoveAt(i);
                    break;
                }
            }
            if (info is null)
                return false;
        }
        bool state = UnloadIntl(info, throwErrors);
        OnSingletonUnloaded?.Invoke(info.Singleton, state);
        info.Singleton = null!;
        info.SingletonType = null!;
        info.Name = null!;
        return state;
    }
    /// <summary>Unloads a singleton.</summary>
    /// <typeparam name="T">Type of singleton to unload.</typeparam>
    /// <param name="throwErrors"><see langword="True"/> to throw any <see cref="SingletonLoadException"/>s that may be thrown upon unloading. Otherwise, <see langword="false"/>.</param>
    /// <param name="field">Will set this field to null if it was properly unloaded.</param>
    /// <exception cref="NotSupportedException">Thrown if the function isn't executed on the game thread.</exception>
    /// <exception cref="SingletonLoadException">Thrown if <paramref name="singleton"/> isn't successfully unloaded and if <paramref name="throwErrors"/> is <see langword="true"/>.</exception>
    /// <returns><see langword="True"/> if the singleton was successfully unloaded and removed, otherwise <see langword="false"/>.</returns>
    public bool UnloadSingleton<T>(ref T field, bool throwErrors = false) where T : class, IUncreatedSingleton
    {
        ThreadUtil.assertIsGameThread();
        Type inputType = field?.GetType() ?? typeof(T);
        SingletonInformation? info = null;
        lock (singletons)
        {
            for (int i = singletons.Count - 1; i >= 0; --i)
            {
                if (singletons[i].SingletonType == inputType)
                {
                    info = singletons[i];
                    singletons.RemoveAt(i);
                    break;
                }
            }
            if (info is null)
                return false;
        }
        bool state = UnloadIntl(info, throwErrors);
        OnSingletonUnloaded?.Invoke(info.Singleton, state);
        info.Singleton = null!;
        info.SingletonType = null!;
        info.Name = null!;
        field = null!;
        return state;
    }
    private bool UnloadIntl(SingletonInformation singleton, bool rethrow)
    {
        singleton.IsLoaded = false;
        try
        {
            singleton.Unload();
            if (singleton.Singleton is Component comp) Destroy(comp);
            singleton.ErroredOnUnload = false;
            return true;
        }
        catch (NotSupportedException ex)
        {
            L.LogError("Tried to unload a singleton without being on the game thread: " + singleton.Name);
            singleton.ErroredOnUnload = true;
            if (rethrow)
                throw new SingletonLoadException(ESingletonLoadType.UNLOAD, singleton.Singleton, ex);
            return false;
        }
        catch (Exception ex)
        {
            L.LogError("Ran into an error unloading: " + singleton.Name);
            L.LogError(ex);
            singleton.ErroredOnUnload = true;
            if (rethrow)
                throw new SingletonLoadException(ESingletonLoadType.UNLOAD, singleton.Singleton, ex);
            return false;
        }
    }
    /// <summary>Reloads a <see cref="IReloadableSingleton"/> with the provided <paramref name="key"/>.</summary>
    /// <param name="key"><see cref="IReloadableSingleton.ReloadKey"/> of the singleton to reload.</param>
    /// <exception cref="NotSupportedException">Thrown if the function isn't executed on the game thread.</exception>
    /// <exception cref="SingletonLoadException">Thrown if the singleton represented by <paramref name="key"/> isn't successfully reloaded.</exception>
    /// <returns>The singleton if it was found, otherwise <see langword="null"/>.</returns>
    public IReloadableSingleton? ReloadSingleton(string key)
    {
        ThreadUtil.assertIsGameThread();
        for (int i = 0; i < singletons.Count; ++i)
        {
            string? k1 = singletons[i].ReloadKey;
            if (k1 is not null && k1.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                IReloadableSingleton? reloadable = ReloadIntl(singletons[i], true);
                return reloadable;
            }
        }
        return null;
    }
    private IReloadableSingleton? ReloadIntl(SingletonInformation singleton, bool rethrow)
    {
        if (singleton.Singleton is IReloadableSingleton reloadable)
        {
            singleton.IsLoaded = false;
            try
            {
                reloadable.Reload();
                OnSingletonReloaded?.Invoke(reloadable, true);
                singleton.IsLoaded = true;
                singleton.ErroredOnReload = false;
            }
            catch (NotSupportedException ex)
            {
                L.LogError("Tried to reload a singleton without being on the game thread: " + singleton.Name);
                singleton.ErroredOnReload = true;
                OnSingletonReloaded?.Invoke(reloadable, false);
                if (rethrow)
                    throw new SingletonLoadException(ESingletonLoadType.RELOAD, singleton.Singleton, ex);
            }
            catch (Exception ex)
            {
                L.LogError("Ran into an error reloading: " + singleton.Name);
                L.LogError(ex);
                OnSingletonReloaded?.Invoke(reloadable, false);
                singleton.ErroredOnReload = true;
                if (rethrow)
                    throw new SingletonLoadException(ESingletonLoadType.RELOAD, singleton.Singleton, ex);
            }
            return reloadable;
        }
        return null;
    }
    /// <summary>Get a singleton by type.</summary>
    /// <typeparam name="T">Type of <see cref="IUncreatedSingleton"/> to get.</typeparam>
    /// <returns>Singleton of type <typeparamref name="T"/>, or <see langword="null"/> if it isn't found.</returns>
    public T GetSingleton<T>() where T : class, IUncreatedSingleton
    {
        Type inputType = typeof(T);
        for (int i = 0; i < singletons.Count; ++i)
            if (singletons[i].SingletonType == inputType)
                return (singletons[i].Singleton as T)!;
        return null!;
    }
    /// <summary>Check if a singleton is loaded.</summary>
    /// <typeparam name="T">Type of <see cref="IUncreatedSingleton"/> to check for.</typeparam>
    /// <returns><see langword="True"/> if <typeparamref name="T"/> singleton is loaded, otherwise <see langword="false"/>.</returns>
    public bool IsLoaded<T>() where T : class, IUncreatedSingleton
    {
        Type inputType = typeof(T);
        for (int i = 0; i < singletons.Count; ++i)
            if (singletons[i].SingletonType == inputType)
                return singletons[i].IsLoaded;
        return false;
    }
    /// <summary>
    /// Unload all currently loaded singletons.
    /// </summary>
    public void UnloadAll()
    {
        ThreadUtil.assertIsGameThread();
        lock (singletons)
        {
            foreach (SingletonInformation info in singletons)
            {
                if (info.IsLoaded)
                    UnloadIntl(info, false);
            }
            singletons.Clear();
        }
    }
    private class SingletonInformation
    {
        public bool IsLoaded;
        public IUncreatedSingleton Singleton;
        public Type SingletonType;
        public string Name;
        public bool ErroredOnLoad;
        public bool ErroredOnUnload;
        public bool ErroredOnReload;
        public string? ReloadKey;
        public SingletonInformation(IUncreatedSingleton singleton)
        {
            Singleton = singleton;
            IsLoaded = false;
            SingletonType = singleton.GetType();
            Name = SingletonType.Name.ToProperCase();
            if (singleton is IReloadableSingleton reloadable)
                ReloadKey = reloadable.ReloadKey;
        }
        /// <exception cref="SingletonLoadException"/>
        public void Unload()
        {
            ThreadUtil.assertIsGameThread();
            lock (this)
            {
                Singleton.Unload();
            }
        }
        /// <exception cref="SingletonLoadException"/>
        public void Reload()
        {
            if (Singleton is IReloadableSingleton reloadable)
            {
                ThreadUtil.assertIsGameThread();
                lock (this)
                {
                    reloadable.Reload();
                }
            }
        }
        /// <exception cref="SingletonLoadException"/>
        public void Load()
        {
            ThreadUtil.assertIsGameThread();
            lock (this)
            {
                Singleton.Load();
            }
        }
    }

    private class SingletonDependencyInformation
    {
        public Type?[] Dependencies;
        //public List<Type>? Dependents;
        public Type SingletonType;
        public IUncreatedSingleton Singleton;
        public bool isLoaded = false;
        public SingletonDependencyInformation(IUncreatedSingleton singleton)
        {
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
        public bool DependsOn(Type type)
        {
            if (type is null) return false;
            for (int i = 0; i < Dependencies.Length; ++i)
            {
                Type? t = Dependencies[i];
                if (t == type)
                    return true;
            }
            return false;
        }
    }
}

/// <summary>Thrown by <see cref="SingletonManager"/> when loading or unloading a <see cref="IUncreatedSingleton"/> goes wrong.</summary>
[Serializable]
public class SingletonLoadException : Exception
{
    public readonly ESingletonLoadType LoadType;
    public readonly IUncreatedSingleton? Singleton;
    public SingletonLoadException(ESingletonLoadType loadType, IUncreatedSingleton? singleton, Exception inner) : base(GetMessage(loadType, singleton, inner, inner.StackTrace), inner)
    {
        LoadType = loadType;
        Singleton = singleton;
    }
    protected SingletonLoadException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    private static string GetMessage(ESingletonLoadType loadType, IUncreatedSingleton? singleton, Exception innerException, string innerStack)
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
/// <summary>Thrown by <see cref="SingletonEx.AssertLoaded{T}"/> and <see cref="SingletonEx.AssertLoaded{T}(bool)"/> if the <see cref="IUncreatedSingleton"/> they reference isn't loaded.</summary>
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
public enum ESingletonLoadType : byte
{
    UNKNOWN = 0,
    LOAD = 1,
    UNLOAD = 2,
    RELOAD = 3
}