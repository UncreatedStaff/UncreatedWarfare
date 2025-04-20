using Cysharp.Threading.Tasks;
using DanielWillett.ReflectionTools;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Buildables;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Tests.Utility;

namespace Uncreated.Warfare.Tests;

[NonParallelizable]
internal class EventSynchronizerTests
{
#pragma warning disable NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method

    private EventSynchronizer _eventSynchronizer;

#pragma warning restore NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method

    private IServiceProvider _serviceProvider;

    [SetUp]
    public void Setup()
    {
        ServiceCollection serviceContainer = new ServiceCollection();

        serviceContainer.AddLogging(l => l.SetMinimumLevel(LogLevel.Trace).AddSystemdConsole());
        serviceContainer.AddSingleton<TestPlayerService>();
        serviceContainer.AddTransient<IPlayerService>(sp => sp.GetRequiredService<TestPlayerService>());
        serviceContainer.AddSingleton(sp =>
            new EventSynchronizer(sp.GetRequiredService<ILogger<EventSynchronizer>>())
            {
                // to allow time for debugging
                MaxTimeout = TimeSpan.FromHours(1d)
            });

        IServiceProvider serviceProvider = _serviceProvider = serviceContainer.BuildServiceProvider();

        _eventSynchronizer = serviceProvider.GetRequiredService<EventSynchronizer>();

        TestHelpers.SetupMainThread();
    }

    [TearDown]
    public void TearDown()
    {
        if (_serviceProvider is IDisposable d)
            d.Dispose();
    }

    public void SwitchToMainThread()
    {
        TestHelpers.SetupMainThread();
    }

    [Test]
    public async Task BasicLockTest()
    {
        GlobalSyncEventModel m1 = new GlobalSyncEventModel();
        GlobalSyncEventModel m2 = new GlobalSyncEventModel();

        // first entry finishes instantly
        UniTask<SynchronizationEntry> entry1Task = _eventSynchronizer.EnterEvent(m1);

        Assert.That(entry1Task.Status, Is.EqualTo(UniTaskStatus.Succeeded));

        SynchronizationEntry entry1 = await entry1Task;
        SwitchToMainThread();

        // first entry did not create a task completion source
        Assert.That(entry1, Is.Not.Null);
        Assert.That(entry1!.WaitEvent, Is.EqualTo(null));
        Assert.That(entry1.WaitCount, Is.EqualTo(0));


        // second entry waits on first entry
        UniTask<SynchronizationEntry> entry2Task = _eventSynchronizer.EnterEvent(m2);

        Assert.That(entry2Task.Status, Is.EqualTo(UniTaskStatus.Pending));

        await Task.Delay(50);
        SwitchToMainThread();

        Assert.That(entry2Task.Status, Is.EqualTo(UniTaskStatus.Pending));

        _eventSynchronizer.ExitEvent(entry1);

        Assert.That(entry2Task.Status, Is.EqualTo(UniTaskStatus.Succeeded));

        SynchronizationEntry entry2 = await entry2Task;

        Assert.That(entry2, Is.Not.Null);
        Assert.That(entry2.WaitEvent, Is.Not.Null);
        Assert.That(entry2.WaitCount, Is.EqualTo(0));

        _eventSynchronizer.ExitEvent(entry2);
    }

    [Test]
    public async Task TestGlobalBlocksPlayers()
    {
        WarfarePlayer player1 = await TestHelpers.AddPlayer(1, _serviceProvider);
        WarfarePlayer player2 = await TestHelpers.AddPlayer(2, _serviceProvider);
        SwitchToMainThread();

        PerPlayerSyncEventModel mPre = new PerPlayerSyncEventModel { Player = player2 };
        
        SynchronizationEntry entryPre = await _eventSynchronizer.EnterEvent(mPre);
        SwitchToMainThread();
        _eventSynchronizer.ExitEvent(entryPre);

        GlobalSyncEventModel m1 = new GlobalSyncEventModel();
        PerPlayerSyncEventModel m2 = new PerPlayerSyncEventModel { Player = player1 };

        SynchronizationEntry entry1 = await _eventSynchronizer.EnterEvent(m1);
        SwitchToMainThread();

        // check that player waits for global
        UniTask<SynchronizationEntry> entry2Task = _eventSynchronizer.EnterEvent(m2);

        Assert.That(entry2Task.Status, Is.EqualTo(UniTaskStatus.Pending));

        await Task.Delay(50);
        SwitchToMainThread();

        Assert.That(entry2Task.Status, Is.EqualTo(UniTaskStatus.Pending));

        _eventSynchronizer.ExitEvent(entry1!);

        Assert.That(entry2Task.Status, Is.EqualTo(UniTaskStatus.Succeeded));

        SynchronizationEntry entry2 = await entry2Task;

        Assert.That(entry2, Is.Not.Null);
        Assert.That(entry2.WaitEvent, Is.Not.Null);
        Assert.That(entry2.WaitCount, Is.EqualTo(0));

        // check that one player doens't wait on the other
        UniTask<SynchronizationEntry> entryPreTask = _eventSynchronizer.EnterEvent(mPre);
        await entryPreTask;
        SwitchToMainThread();

        Assert.That(entryPreTask.Status, Is.EqualTo(UniTaskStatus.Succeeded));

        entryPre = await entry2Task;

        Assert.That(entryPre, Is.Not.Null);
        Assert.That(entryPre.WaitEvent, Is.Not.Null);
        Assert.That(entryPre.WaitCount, Is.EqualTo(0));

        _eventSynchronizer.ExitEvent(entry2);

        _eventSynchronizer.ExitEvent(entryPre);
    }

    [Test]
    public async Task ReplicateTimeoutError()
    {
        /*
         *  This replicates an issue that was causing massive lag on release.
         *   It's caused when the first invocation causes the second one to wait and
         *   when the first one is done the second doesn't switch contexts.
         */
        WarfarePlayer player = null;
        for (uint i = 1; i <= 23; ++i)
        {
            WarfarePlayer pl = await TestHelpers.AddPlayer(i, _serviceProvider);
            PerPlayerSyncEventModel perPlayerArgs = new PerPlayerSyncEventModel { Player = pl };
            _eventSynchronizer.ExitEvent(await _eventSynchronizer.EnterEvent(perPlayerArgs));
            player ??= pl;
        }

        ReplicateTimeoutErrorModel args1 = new ReplicateTimeoutErrorModel { Player = player! };

        SynchronizationEntry entry1 = null;
        SwitchToMainThread();
        Task t1 = new Func<Task>(async () =>
        {
            entry1 = await _eventSynchronizer.EnterEvent(args1, 11916, args1.GetType().GetAttributeSafe<EventModelAttribute>());

            await Task.Delay(200);

            SwitchToMainThread();
            if (entry1 != null)
                _eventSynchronizer.ExitEvent(entry1);
        })();

        ReplicateTimeoutErrorModel args2 = new ReplicateTimeoutErrorModel { Player = player! };

        SynchronizationEntry entry2 = null;
        Task t2 = new Func<Task>(async () =>
        {
            entry2 = await _eventSynchronizer.EnterEvent(args2, 11917, args2.GetType().GetAttributeSafe<EventModelAttribute>());

            SwitchToMainThread();
            if (entry2 != null)
                _eventSynchronizer.ExitEvent(entry2);
        })();

        await t1;
        await t2;

        Task taskTimeout = new Func<Task>(async () =>
        {
            SwitchToMainThread();
            ReplicateTimeoutErrorModel args3 = new ReplicateTimeoutErrorModel { Player = player! };
            SynchronizationEntry entry3 = await _eventSynchronizer.EnterEvent(args3, 11918, args3.GetType().GetAttributeSafe<EventModelAttribute>());

            SwitchToMainThread();
            if (entry3 != null)
                _eventSynchronizer.ExitEvent(entry3);

        })();

        await taskTimeout;

        await Task.WhenAny(Task.Delay(500), taskTimeout);

        Assert.That(taskTimeout.IsCompleted, Is.True);
    }

    private UniTask<SynchronizationEntry> EnterEvent<TEventArgs>(TEventArgs args, int eventId = 0) where TEventArgs : class
    {
        lock (_eventSynchronizer)
        {
            SwitchToMainThread();
            return _eventSynchronizer.EnterEvent(args, eventId, typeof(TEventArgs).GetAttributeSafe<EventModelAttribute>());
        }
    }
    private void ExitEvent(SynchronizationEntry entry)
    {
        lock (_eventSynchronizer)
        {
            SwitchToMainThread();
            _eventSynchronizer.ExitEvent(entry);
        }
    }

    [Test]
    public async Task ReplicateTimeoutError2()
    {
        /*
         *  This replicates another issue that was causing massive lag on release.
         */
        WarfarePlayer[] players = new WarfarePlayer[55];
        for (uint i = 1; i <= 55; ++i)
        {
            WarfarePlayer pl = await TestHelpers.AddPlayer(i, _serviceProvider);
            PerPlayerSyncEventModel perPlayerArgs = new PerPlayerSyncEventModel { Player = pl };
            _eventSynchronizer.ExitEvent(await EnterEvent(perPlayerArgs));
            players[i - 1] = pl;
        }

        PlaceBarricadeRequested args1 = new PlaceBarricadeRequested();

        SynchronizationEntry entry1 = await EnterEvent(args1, 403618);

        EquipUseableRequested args2 = new EquipUseableRequested { Player = players[49] };

        UniTask<SynchronizationEntry> entry2Task = EnterEvent(args2, 403628);

        EquipUseableRequested args3 = new EquipUseableRequested { Player = players[24] };

        UniTask<SynchronizationEntry> entry3Task = EnterEvent(args3, 403632);

        EquipUseableRequested args4 = new EquipUseableRequested { Player = players[27] };

        UniTask<SynchronizationEntry> entry4Task = EnterEvent(args4, 403635);

        await Task.Delay(500);
        ExitEvent(entry1);

        SynchronizationEntry entry2 = null;
        await Task.WhenAny(new Func<Task>(async () =>
        {
            entry2 = await entry2Task;
        })(), Task.Delay(1000));

        Assert.That(entry2, Is.Not.Null);

        SynchronizationEntry entry3 = null;
        await Task.WhenAny(new Func<Task>(async () =>
        {
            entry3 = await entry3Task;
        })(), Task.Delay(1000));

        Assert.That(entry3, Is.Not.Null);

        SynchronizationEntry entry4 = null;
        await Task.WhenAny(new Func<Task>(async () =>
        {
            entry4 = await entry4Task;
        })(), Task.Delay(1000));

        Assert.That(entry4, Is.Not.Null);

        ExitEvent(entry2);
        ExitEvent(entry3);
        ExitEvent(entry4);

        Assert.That(_eventSynchronizer.IsCleared());
    }
}

[EventModel(EventSynchronizationContext.Global, SynchronizedModelTags = ["modify_world"])]
public class ReplicateTimeoutErrorModel : CancellablePlayerEvent;

[EventModel(EventSynchronizationContext.Global, SynchronizedModelTags = ["modify_inventory", "modify_world"])]
public class PlaceBarricadeRequested : CancellableEvent;

[EventModel(EventSynchronizationContext.PerPlayer, SynchronizedModelTags = ["modify_inventory", "modify_useable"])]
public class EquipUseableRequested : CancellablePlayerEvent;

[EventModel(EventSynchronizationContext.Global, SynchronizedModelTags = [ "TestGlobalBlocksExistingPlayer" ])]
public class GlobalSyncEventModel;

[EventModel(EventSynchronizationContext.PerPlayer, SynchronizedModelTags = [ "TestGlobalBlocksExistingPlayer" ])]
public class PerPlayerSyncEventModel : PlayerEvent;