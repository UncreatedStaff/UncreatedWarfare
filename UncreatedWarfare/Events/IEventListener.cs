﻿using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Uncreated.Warfare.Services;

namespace Uncreated.Warfare.Events;

/// <summary>
/// Listen for an event in a non-asynchronous context. Object must be in the <see cref="IServiceProvider"/> or a <see cref="IEventListenerProvider"/>.
/// </summary>
public interface IEventListener<in TEventArgs>
{
    void HandleEvent(TEventArgs e);
}

/// <summary>
/// Listen for an event in an asynchronous context. Object must be in the <see cref="IServiceProvider"/> or a <see cref="IEventListenerProvider"/>.
/// </summary>
public interface IAsyncEventListener<in TEventArgs>
{
    UniTask HandleEventAsync(TEventArgs e, CancellationToken token = default);
}