using System;
using DanielWillett.ReflectionTools;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events;
internal class SynchronizationBucket : IDisposable
{
    public string? Tag { get; }
    public Type? TypeOwner { get; }
    public WarfarePlayer? Player { get; }
    public SemaphoreSlim Semaphore { get; }
    public bool IsGlobal { get; }
    public SynchronizationBucket(string? tag, bool locked, WarfarePlayer? player = null)
    {
        Tag = tag;
        Player = player;
        Semaphore = new SemaphoreSlim(!locked ? 1 : 0, 1);
        IsGlobal = player == null;
    }
    public SynchronizationBucket(Type? owner, bool locked, WarfarePlayer? player = null)
    {
        TypeOwner = owner;
        Player = player;
        Semaphore = new SemaphoreSlim(!locked ? 1 : 0, 1);
        IsGlobal = player == null;
    }

    public void Dispose()
    {
        Semaphore.Dispose();
    }

    public override string ToString()
    {
        if (TypeOwner != null)
        {
            if (Player != null)
                return $"Sync {Accessor.Formatter.Format(TypeOwner)} for {Player}";

            return $"Sync {Accessor.Formatter.Format(TypeOwner)} for all players";
        }

        if (Player != null)
            return $"Sync tag \"{Tag}\" for {Player}";

        return $"Sync tag \"{Tag}\" for all players";
    }
}
