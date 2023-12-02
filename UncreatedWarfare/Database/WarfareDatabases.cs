using System;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Database.Abstractions;

namespace Uncreated.Warfare.Database;
public static class WarfareDatabases
{
    /*
     * The idea here is so we can use other contexts outside this assembly.
     */
#nullable disable
    public static UCSemaphore Semaphore { get; set; }
    public static IFactionDbContext Factions { get; set; }
    public static ILanguageDbContext Languages { get; set; }
    public static IUserDataDbContext UserData { get; set; }
    public static IKitsDbContext Kits { get; set; }
    public static IStatsDbContext Stats { get; set; }
    public static IGameDataDbContext GameData { get; set; }
    public static ISeasonsDbContext Seasons { get; set; }
#nullable restore
    public static void LoadFromWarfareDbContext(WarfareDbContext context)
    {
        Factions = context;
        Languages = context;
        UserData = context;
        Kits = context;
        Stats = context;
        GameData = context;
        Seasons = context;
#if DEBUG
        Semaphore.WaitCallback    += () => L.LogDebug( "Semaphore waiting       in DbContext.");
        Semaphore.ReleaseCallback += n  => L.LogDebug($"Semaphore released x{n} in DbContext.");
#endif
    }

    public static Task WaitAsync(CancellationToken token = default) => Semaphore.WaitAsync(token);
    public static Task WaitAsync(TimeSpan timeout, CancellationToken token = default) => Semaphore.WaitAsync(timeout, token);
    public static Task WaitAsync(int millisecondsTimeout, CancellationToken token = default) => Semaphore.WaitAsync(millisecondsTimeout, token);

    public static void Wait(CancellationToken token = default) => Semaphore.Wait(token);
    public static void Wait(TimeSpan timeout, CancellationToken token = default) => Semaphore.Wait(timeout, token);
    public static void Wait(int millisecondsTimeout, CancellationToken token = default) => Semaphore.Wait(millisecondsTimeout, token);

    public static void Release(int amt = 1) => Semaphore.Release(amt);
}