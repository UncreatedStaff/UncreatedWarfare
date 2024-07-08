using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace Uncreated.Warfare.Layouts.Objectives;

public delegate void PlayerObjectiveHandler(UCPlayer player, IObjective objective);

public interface IObjectiveProvider<out TObjective> where TObjective : IObjective
{
    /// <summary>
    /// Invoked when a player starts working towards an objective.
    /// </summary>
    event PlayerObjectiveHandler OnPlayerEnteredObjective;
    
    /// <summary>
    /// Invoked when a player is no longer working towards an objective.
    /// </summary>
    event PlayerObjectiveHandler OnPlayerExitedObjective;

    /// <summary>
    /// List of all objectives currently loaded.
    /// </summary>
    IReadOnlyList<TObjective> Objectives { get; }

    /// <summary>
    /// Invoked on phase start.
    /// </summary>
    UniTask InitializeAsync(CancellationToken token);
}