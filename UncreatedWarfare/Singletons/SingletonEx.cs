namespace Uncreated.Warfare.Singletons;

public static class SingletonEx
{
    /// <exception cref="SingletonUnloadedException"/>
    public static void AssertLoaded<T>() where T : class, IUncreatedSingleton
    {
        if (!Data.Singletons.IsLoaded<T>())
            throw new SingletonUnloadedException(typeof(T));
    }
    /// <exception cref="SingletonUnloadedException"/>
    public static void AssertLoaded<T>(bool check) where T : class, IUncreatedSingleton
    {
        if (!check)
            throw new SingletonUnloadedException(typeof(T));
    }
    /// <exception cref="SingletonUnloadedException"/>
    public static void AssertLoaded<T>(this T? singleton) where T : BaseSingleton
    {
        if (singleton is null)
            throw new SingletonUnloadedException(typeof(T));
        singleton.AssertLoadedIntl();
    }
    /// <exception cref="SingletonUnloadedException"/>
    public static void AssertLoaded<T, TData>(this T? singleton) where T : ListSingleton<TData> where TData : class, new()
    {
        if (singleton is null)
            throw new SingletonUnloadedException(typeof(T));
        singleton.AssertLoadedIntl();
    }
    public static bool IsLoaded<T, TData>(this T? singleton) where T : ListSingleton<TData> where TData : class, new()
    {
        if (singleton is null)
            return false;
        return singleton.IsLoaded;
    }
    public static bool IsLoaded<T>(this T? singleton) where T : BaseSingleton
    {
        if (singleton is null)
            return false;
        return singleton.IsLoaded;
    }
    public static bool IsLoaded2<T>(this T? singleton) where T : BaseSingletonComponent
    {
        if (singleton is null)
            return false;
        return singleton.IsLoaded;
    }
    /// <exception cref="SingletonUnloadedException"/>
    public static T AssertAndGet<T>() where T : class, IUncreatedSingleton
    {
        T? singleton = Data.Singletons.GetSingleton<T>();
        if (singleton is null)
            throw new SingletonUnloadedException(typeof(T));
        return singleton;
    }
}