using System.Diagnostics.CodeAnalysis;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Interaction.Requests;

/// <summary>
/// Handles a request for anything implementing <see cref="IRequestable"/>.
/// </summary>
public interface IRequestHandler<in TRequestable, out TRequestObject> where TRequestable : IRequestable<TRequestObject>
{
    /// <summary>
    /// Handles a request for anything implementing <see cref="IRequestable"/>.
    /// </summary>
    /// <returns><see langword="true"/> if the request was granted, otherwise <see langword="false"/>.</returns>
    [UsedImplicitly] // RequestCommand
    Task<bool> RequestAsync(WarfarePlayer player, [NotNullWhen(true)] TRequestable? requestable, IRequestResultHandler resultHandler, CancellationToken token = default);
}