### Events
Uncreated uses a interface-based event system similar to OpenMod's `IEventBus`. To create an event listener, register a service as either an `IEventListener<>` or an `IAsyncEventListener<>`.

View the built-in events [here](https://github.com/UncreatedStaff/UncreatedWarfare/tree/master/UncreatedWarfare/Events/Models).

#### Event Listeners

When an `IEventListener<>` is invoked, its done on the game thread by default. When an `IAsyncEventListener<>` is invoked, it's not guaranteed to be on the game thread by default.

You can create an event listener like this:
```cs
public class SomeCoolService :
    IEventListener<PlayerDied>,
    IAsyncEventListener<FobDestroyed>
{
    // ...

    [EventListener(MustRunInstantly = false)] // optional configuration for the event
    void IEventListener<PlayerDied>.HandleEvent(PlayerDied e, IServiceProvider serviceProvider)
    {
        // do something
    }

    [EventListener(RequireActiveLayout = true)]
    async UniTask IAsyncEventListener<FobDestroyed>.HandleEvent(FobDestroyed e, IServiceProvider serviceProvider, CancellationToken token)
    {
        // await something
    }

    // ...
}

// then make sure the service is registered
public class MyPluginsServiceConfigurer : IServiceConfigurer
{
    void IServiceConfigurer.ConfigureServices(ContainerBuilder bldr)
    {
        // ...

        bldr.RegisterType<SomeCoolService>()
            .AsSelf().AsImplementedInterfaces();
    
        // ...
    }
}
```

---

> [!WARNING]
> Registering an event handler without AsSelf can cause issues if the class has some kind of state. A warning will be logged.

```cs
private class SomeTweak : IEventListener<PlayerDied>, IEventListener<PlayerJoined>
{
    [EventListener(MustRunInstantly = true)]
    void IEventListener<PlayerDied>.HandleEvent(PlayerDied e, IServiceProvider serviceProvider) { }

    [EventListener(Priority = 10)]
    void IEventListener<PlayerJoined>.HandleEvent(PlayerJoined e, IServiceProvider serviceProvider) { }
}

// !! DONT DO THIS !!
bldr.RegisterType<SomeTweak>()
    .AsImplementedInterfaces()
    .SingleInstance();
```

This does not work because there's no class in common when looking up `IEventListener<PlayerDied>` and `IEventListener<PlayerJoined>`, so it creates different instances of the type. You can fix this by adding `.AsSelf()` before `.AsImplementedInterfaces()`. A warning will be logged if this happens.

---

The `[EventListener]` attribute allows you to configure your event listener's behavior. It's placed on the listener method.

You have the following options:

| Option              | Description                                                                                                                                                                                                              | Default Value                                                  |
| ------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------- |
| MustRunInstantly    | Indicates that an event listener needs to be invoked before switching contexts*. Async event listeners can not use this flag.                                                                                            | false                                                          |
| MustRunLast         | Indicates that an event listener must run after the event has had a chance to be cancelled. Event listeners using this flag can not cancel the event.                                                                    | false                                                          |
| Priority            | Numeric priority defining this event's execution order in relation to others. Higher values will be ran first, lower values will be ran later.                                                                           | 0                                                              |
| RequireActiveLayout | When true, skips invoking an event when theres not an active layout. This could be during startup or between gamemodes. This allows you to guarantee that the service provider you're given can request scoped services. | false                                                          |
| RequireNextFrame    | Ensures that this event listener is not invoked until the next frame after the original event was fired. Events with this flag still respect the priority system, so best to give these a very low priority.             | false                                                          |
| RequiresMainThread  | Ensures that this event listener is started on the main thread.                                                                                                                                                          | true for `IEventListener<>`, false for `IAsyncEventListener<>` |

#### Creating Custom Events

You can easily dispatch your own events by creating event models. These are just classes which store information about the event, and optionally a way to cancel them.

```cs
[EventModel(EventSynchronizationContext.Pure)]
public class VehicleSpawned
{
    /// <summary>
    /// The vehicle that was spawned.
    /// </summary>
    public required InteractableVehicle Vehicle { get; init; }
}
```

Events can implement `IActionLoggableEvent` to be logged to the server's log file.

##### Configuring the Event
The `[EventModel]` attribute provides global configuration about the event. It's placed on the event model class.

It contains properties desciribing how the event invocation should be synchronized. Synchronziation is necessary because events can take more than one frame and other events could have side effects to running events.

For example, imagine a `DropItemRequested` event takes 500ms, then determines that the player can drop the item, but during that time the player tries to move the item into storage, even through technically the item should've already been on the ground... what do we do now?

Thats why both the `DropItemRequested` and `ItemMoveRequested` both have the `modify_inventory` tag in their `SynchronizedModelTags` list, and since this only applies to the player who's inventory is being affected, we use the `EventSynchronizationContext.PerPlayer` synchronization context.

There are 4 types of synchronization:

| Synchronization     | Description                                                                                                                                                                           |
| ------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| None                | No synchronization is attempted, event listeners are ran one-at-a-time.                                                                                                               |
| PerPlayer           | Event listeners are synchronized per-player.                                                                                                                                          |
| Global              | Events are synchronized for the entire server.                                                                                                                                        |
| Pure                | Events are grouped by priority and each group is ran simultaneously (for async handlers). This should be used on events where the handlers for this event wouldn't effect each other. |

By default, events are synchronized based on their types (so all events of one type would be synchronized), but if event models specify tags using `SynchronizedModelTags`, they will be synchronized with other events containing the same tags.

##### Player Events
Events involving one player, like `ItemDropped`, should implement `IPlayerEvent`. Most of the time you can just inherit `PlayerEvent` or `CancellablePlayerEvent`.

This is important because player events dispatched to `IPlayerComponent`s only go to components for the event's player. Also, `PerPlayer` synchronization only works with `IPlayerEvent` models.

##### Cancellable Events
Most 'onXyzRequested' events need to be able to be cancelled. To do this, event models should implement `ICancellable`. Usually you can just inherit one of the following classes instead.

| Base Type              | Description                                                                                  |
| ---------------------- | -------------------------------------------------------------------------------------------- |
| CancellableEvent       | Allows the action following the event to be cancelled.                                       |
| CancellablePlayerEvent | Implementation of `IPlayerEvent` that allows the action following the event to be cancelled. |
| ConsumableEvent        | Allows the following event listeners to be cancelled.                                        |

```cs
void IEventListener<IPlaceBuildableRequestedEvent>.HandleEvent(IPlaceBuildableRequestedEvent e, IServiceProvider serviceProvider)
{
    // Prevents the buildable from being placed
    // and doesn't invoke any more listeners.
    e.Cancel();

    // Doesn't invoke any more listeners
    // but still allows the buildable to be placed.
    e.Cancel(cancelAction: false);

    // Prevents the buildable from being placed
    // but listeners continue to be invoked.
    e.CancelAction();

    // un-does the previous method (i.e. in a future listener)
    e.ResumeAction();
}
```

##### Dispatching the Event
Events are dispatched (or invoked) using the `EventDispatcher` singleton service.

```cs
// model for request to take shower
//
// consider synchronizing this globally if concurrent showers
// could have negative side effects on each other
public sealed class TakeShowerRequested : CancellableEvent
{
    public required TimeSpan Duration { get; init; }
}

// model for shower completed
// note that this is pure because event handlers won't affect each other
[EventModel(EventSynchronizationContext.Pure)]
public sealed class ShowerTaken;

public class ShowerService
{
    private readonly EventDispatcher _eventDispatcher;

    // inject EventDispatcher
    public ShowerManager(EventDispatcher eventDispatcher)
    {
        _eventDispatcher = eventDispatcher;
    }

    public async UniTask ShowerAsync(TimeSpan duration, CancellationToken token = default)
    {
        TakeShowerRequested requestArgs = new MyEventArgs
        {
            Duration = duration
        };

        bool canContine = await _eventDispatcher.DispatchEventAsync(requestArgs, token);

        if (!canContinue)
        {
            throw new OperationCancelledException("Someone doesn't want you to shower.");
        }

        TurnOnWater();
        await ScrubAsync(duration, token);
        TurnOffWater();

        ShowerTaken actionArgs = new ShowerTaken();
        await _eventDispatcher.DispatchEventAsync(actionArgs, token);
    }
}
```

##### Event Continuations
A lot of events have to be adapted to support running asynchronously. We have a function to help with this.

Example with simplified `BarricadeManager.onModifySignRequested`:
```cs
[EventModel(EventSynchronizationContext.Global, SynchronizedModelTags = [ "modify_world" ])]
public class ChangeSignTextRequested : CancellablePlayerEvent
{
    public required BarricadeDrop Barricade { get; init; }

    // notice this is not init-only
    public required string Text { get; set; }
}
```

```cs
private readonly EventDispatcher _eventDispatcher = /* inject */;

private void BarricadeManagerOnModifySignRequested(
    WarfarePlayer instigatorPlayer,
    BarricadeDrop drop,
    ref string text,
    ref bool shouldAllow)
{
    ChangeSignTextRequested args = new ChangeSignTextRequested
    {
        Barricade = drop,
        Player = instigatorPlayer,
        Text = text
    };

    EventContinuations.Dispatch(
        args,
        _eventDispatcher,
        CancellationToken.None,
        out shouldAllow,
        continuation: args =>
        {
            // this continuation will run if the event dispatcher has to await
            // and doesn't get cancelled
            // Since it'll be ran later, we have to do a few checks

            // barricade could've been destroyed since the event was started
            if (args.Barricade.GetServersideData().barricade.isDead
                || args.Barricade.interactable is not InteractableSign sign
                || sign == null /* unity destroyed check */)
            {
                return;
            }

            BarricadeManager.ServerSetSignText(sign, args.Text);
        }
    );

    // if a context-switch is necessary, shouldAllow will be set to false
    // and the event will be cancelled (for now)
    // until DispatchEventAsync is finished
    if (!shouldAllow)
        return;

    // otherwise, the event dispatcher finished without awaiting,
    // so we can return from the event and continue anyways

    // update original text parameter in case a listener changed it
    text = args.Text;
}
```

The idea of the continuation is to define what would happen if we left `shouldAllow` as `true` to run later on. This pattern works with most events, but when it doesn't, you can set `allowAsync: false` in `DispatchEventAsync` to only allow non-async event listeners.
```cs
// can't support async event handlers because any code calling damagePlayer
// may expect the player to take damage or die instantly
//  ex. hitmarkers are handled by checking which players were damaged immediately after shooting
DamagePlayerRequested args = new DamagePlayerRequested { /* ... */ };

shouldAllow = _eventDispatcher.DispatchEventAsync(args, allowAsync: false).GetAwaiter().GetResult();
```