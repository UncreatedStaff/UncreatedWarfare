using System;
using DanielWillett.ReflectionTools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Uncreated.Warfare.Players.Costs;

public abstract class UnlockCost : ICloneable
{
    /// <summary>
    /// Try to subtract the cost from the player if they can afford it.
    /// If this returns <see langword="false"/>, all already applied costs need to be undone.
    /// </summary>
    public abstract UniTask<bool> TryApply(WarfarePlayer player, ulong team, CancellationToken token = default);

    /// <summary>
    /// Check if this cost can be met by the player.
    /// </summary>
    public abstract UniTask<bool> CanApply(WarfarePlayer player, ulong team, CancellationToken token = default);

    /// <summary>
    /// Undo subtracting the cost from the player.
    /// </summary>
    public abstract UniTask Undo(WarfarePlayer player, ulong team, CancellationToken token = default);

    /// <inheritdoc />
    public abstract object Clone();

    /// <summary>
    /// Read an <see cref="UnlockCost"/> from a <see cref="IConfiguration"/> section.
    /// </summary>
    public UnlockCost? ReadFromConfiguration(IConfiguration section, IServiceProvider serviceProvider, ILogger? logger)
    {
        string? typeStr = section["Type"];
        if (string.IsNullOrEmpty(typeStr))
        {
            logger?.LogError("Missing 'Type' property on UnlockCost.");
            return null;
        }

        Type? unlockType = Type.GetType(typeStr, false, false) ?? typeof(WarfareModule).Assembly.GetType(typeStr, false, false);
        if (unlockType == null || unlockType.IsAbstract || !unlockType.IsSubclassOf(typeof(UnlockCost)))
        {
            logger?.LogError("Unknown 'Type', {0}, on UnlockCost.", typeStr);
            return null;
        }

        try
        {
            UnlockCost cost = (UnlockCost)ActivatorUtilities.CreateInstance(serviceProvider, unlockType);
            return cost;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Unable to create UnlockCost of type {0}.", unlockType);
            return null;
        }
    }
}