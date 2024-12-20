using DanielWillett.ReflectionTools;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("eventtest"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DebugEventTestCommand : IExecutableCommand
{
    // uncomment service registrations for TestEventService1-7 in WarfareModule for full tests

    private readonly EventDispatcher _eventDispatcher;
    private readonly IServiceProvider _serviceProvider;

    public required CommandContext Context { get; init; }

    public DebugEventTestCommand(EventDispatcher eventDispatcher, IServiceProvider serviceProvider)
    {
        _eventDispatcher = eventDispatcher;
        _serviceProvider = serviceProvider;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        TestEventService1 ev1 = _serviceProvider.GetRequiredService<TestEventService1>();
        TestEventService2 ev2 = _serviceProvider.GetRequiredService<TestEventService2>();
        TestEventService3 ev3 = _serviceProvider.GetRequiredService<TestEventService3>();
        TestEventService4 ev4 = _serviceProvider.GetRequiredService<TestEventService4>();
        TestEventService5 ev5 = _serviceProvider.GetRequiredService<TestEventService5>();
        TestEventService6 ev6 = _serviceProvider.GetRequiredService<TestEventService6>();
        TestEventService7 ev7 = _serviceProvider.GetRequiredService<TestEventService7>();

        Context.Logger.LogInformation("hashes: 1: {0}, 2: {1}, 3: {2}, 4: {3}, 5: {4}, 6: {5}, 7: {6}",
            ev1.GetHashCode(), ev2.GetHashCode(), ev3.GetHashCode(), ev4.GetHashCode(), ev5.GetHashCode(),
            ev6.GetHashCode(), ev7.GetHashCode());

        // Context.AssertRanByPlayer();

        EventModelParent3 args = new EventModelParent3();
        //{
        //    Player = Context.Player
        //};

        Stopwatch sw = Stopwatch.StartNew();

        int frameCt = Time.frameCount;

        bool cancelled = await _eventDispatcher.DispatchEventAsync(args, token);
        sw.Stop();
        Context.ReplyString($"Done. Cancelled: {cancelled}. Ms: {sw.GetElapsedMilliseconds():F6}ms Fc: {frameCt}");
    }
}

public class TestEventService1 : IEventListener<EventModelParent1>, IEventListener<EventModelParent2>////, IEventListener<object>
{
    private readonly ILogger<TestEventService1> _logger;

    public TestEventService1(ILogger<TestEventService1> logger)
    {
        _logger = logger;
    }

    void IEventListener<EventModelParent1>.HandleEvent(EventModelParent1 e, IServiceProvider serviceProvider)
    {
        _logger.LogDebug("Event1 triggered: {0}", e.GetType());
    }

    void IEventListener<EventModelParent2>.HandleEvent(EventModelParent2 e, IServiceProvider serviceProvider)
    {
        _logger.LogDebug("Event2 triggered: {0}", e.GetType());
    }

    //void IEventListener<object>.HandleEvent(object e, IServiceProvider serviceProvider)
    //{
    //    _logger.LogDebug("Object triggered: {0}", e.GetType());
    //}
}

public class TestEventService2 : IEventListener<EventModelParent1>, IEventListener<EventModelParent2>////, IEventListener<object>
{
    private readonly ILogger<TestEventService2> _logger;

    public TestEventService2(ILogger<TestEventService2> logger)
    {
        _logger = logger;
    }

    void IEventListener<EventModelParent1>.HandleEvent(EventModelParent1 e, IServiceProvider serviceProvider)
    {
        _logger.LogDebug("Event1 triggered: {0}", e.GetType());
    }

    [EventListener(Priority = 1)]
    void IEventListener<EventModelParent2>.HandleEvent(EventModelParent2 e, IServiceProvider serviceProvider)
    {
        _logger.LogDebug("Event2 triggered: {0}", e.GetType());
    }

    //void IEventListener<object>.HandleEvent(object e, IServiceProvider serviceProvider)
    //{
    //    _logger.LogDebug("Object triggered: {0}", e.GetType());
    //}
}


public class TestEventService3 : IEventListener<EventModelParent1>, IEventListener<EventModelParent2>//, IEventListener<object>
{
    private readonly ILogger<TestEventService3> _logger;

    public TestEventService3(ILogger<TestEventService3> logger)
    {
        _logger = logger;
    }

    void IEventListener<EventModelParent1>.HandleEvent(EventModelParent1 e, IServiceProvider serviceProvider)
    {
        _logger.LogDebug("Event1 triggered: {0}", e.GetType());
    }

    [EventListener(Priority = -1)]
    void IEventListener<EventModelParent2>.HandleEvent(EventModelParent2 e, IServiceProvider serviceProvider)
    {
        _logger.LogDebug("Event2 triggered: {0}", e.GetType());
    }

    //void IEventListener<object>.HandleEvent(object e, IServiceProvider serviceProvider)
    //{
    //    _logger.LogDebug("Object triggered: {0}", e.GetType());
    //}
}


public class TestEventService4 : IEventListener<EventModelParent1>, IEventListener<EventModelParent2>//, IEventListener<object>
{
    private readonly ILogger<TestEventService4> _logger;

    public TestEventService4(ILogger<TestEventService4> logger)
    {
        _logger = logger;
    }

    void IEventListener<EventModelParent1>.HandleEvent(EventModelParent1 e, IServiceProvider serviceProvider)
    {
        _logger.LogDebug("Event1 triggered: {0}", e.GetType());
    }

    void IEventListener<EventModelParent2>.HandleEvent(EventModelParent2 e, IServiceProvider serviceProvider)
    {
        _logger.LogDebug("Event2 triggered: {0}", e.GetType());
    }

    //[EventListener(Priority = 1)]
    //void IEventListener<object>.HandleEvent(object e, IServiceProvider serviceProvider)
    //{
    //    _logger.LogDebug("Object triggered: {0}", e.GetType());
    //}
}


public class TestEventService5 : IEventListener<EventModelParent1>, IEventListener<EventModelParent2>//, IEventListener<object>
{
    private readonly ILogger<TestEventService5> _logger;

    public TestEventService5(ILogger<TestEventService5> logger)
    {
        _logger = logger;
    }

    void IEventListener<EventModelParent1>.HandleEvent(EventModelParent1 e, IServiceProvider serviceProvider)
    {
        _logger.LogDebug("Event1 triggered: {0}", e.GetType());
    }

    void IEventListener<EventModelParent2>.HandleEvent(EventModelParent2 e, IServiceProvider serviceProvider)
    {
        _logger.LogDebug("Event2 triggered: {0}", e.GetType());
    }

    //[EventListener(Priority = -1)]
    //void IEventListener<object>.HandleEvent(object e, IServiceProvider serviceProvider)
    //{
    //    _logger.LogDebug("Object triggered: {0}", e.GetType());
    //}
}


public class TestEventService6 : IEventListener<EventModelParent1>, IAsyncEventListener<EventModelParent2>, IEventListener<EventModelParent2>//, IEventListener<object>
{
    private readonly ILogger<TestEventService6> _logger;

    public TestEventService6(ILogger<TestEventService6> logger)
    {
        _logger = logger;
    }

    [EventListener(Priority = 1, RequireNextFrame = true)]
    void IEventListener<EventModelParent1>.HandleEvent(EventModelParent1 e, IServiceProvider serviceProvider)
    {
        _logger.LogDebug("Event1 triggered: {0} {1}", e.GetType(), Time.frameCount);
    }

    void IEventListener<EventModelParent2>.HandleEvent(EventModelParent2 e, IServiceProvider serviceProvider)
    {
        _logger.LogDebug("Event2sync triggered: {0}", e.GetType());
    }

    UniTask IAsyncEventListener<EventModelParent2>.HandleEventAsync(EventModelParent2 e, IServiceProvider serviceProvider, CancellationToken token)
    {
        _logger.LogDebug("Event2async triggered: {0}", e.GetType());
        return UniTask.CompletedTask;
    }

    //[EventListener(MustRunInstantly = true)]
    //void IEventListener<object>.HandleEvent(object e, IServiceProvider serviceProvider)
    //{
    //    _logger.LogDebug("Object triggered: {0}", e.GetType());
    //}
}


public class TestEventService7 : IEventListener<EventModelParent1>, IEventListener<EventModelParent2>//, IEventListener<object>
{
    private readonly ILogger<TestEventService7> _logger;

    public TestEventService7(ILogger<TestEventService7> logger)
    {
        _logger = logger;
    }

    [EventListener(Priority = -1)]
    void IEventListener<EventModelParent1>.HandleEvent(EventModelParent1 e, IServiceProvider serviceProvider)
    {
        _logger.LogDebug("Event1 triggered: {0}", e.GetType());
    }

    void IEventListener<EventModelParent2>.HandleEvent(EventModelParent2 e, IServiceProvider serviceProvider)
    {
        _logger.LogDebug("Event2 triggered: {0}", e.GetType());
    }

    //void IEventListener<object>.HandleEvent(object e, IServiceProvider serviceProvider)
    //{
    //    _logger.LogDebug("Object triggered: {0}", e.GetType());
    //}
}

public abstract class EventModelParent1;
public abstract class EventModelParent2 : EventModelParent1;
public class EventModelParent3 : EventModelParent2;