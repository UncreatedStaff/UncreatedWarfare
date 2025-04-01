using System;
using Uncreated.Warfare.Services;

namespace Uncreated.Warfare.Events.Models;

/// <summary>
/// Listen for an event in a non-asynchronous context. Object must be in the <see cref="IServiceProvider"/> or a <see cref="IEventListenerProvider"/>.
/// </summary>
public interface IEventListener<in TEventArgs> where TEventArgs : class
{
    void HandleEvent(TEventArgs e, IServiceProvider serviceProvider);
}

/// <summary>
/// Listen for an event in an asynchronous context. Object must be in the <see cref="IServiceProvider"/> or a <see cref="IEventListenerProvider"/>.
/// </summary>
public interface IAsyncEventListener<in TEventArgs> where TEventArgs : class
{
    UniTask HandleEventAsync(TEventArgs e, IServiceProvider serviceProvider, CancellationToken token = default);
}