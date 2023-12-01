using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using SDG.Unturned;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Models.GameData;
using Uncreated.Warfare.Singletons;

namespace Uncreated.Warfare.Sessions;
public class SessionManager : BaseAsyncSingleton
{
    private readonly ConcurrentDictionary<ulong, SessionRecord> _sessions = new ConcurrentDictionary<ulong, SessionRecord>();

    public override bool AwaitLoad => true;
    public override Task LoadAsync(CancellationToken token) => Task.CompletedTask;
    
    public override Task UnloadAsync(CancellationToken token)
    {
        while (!_sessions.IsEmpty)
        {
            KeyValuePair<ulong, SessionRecord> session = _sessions.FirstOrDefault();
        }

        return Task.CompletedTask;
    }
}