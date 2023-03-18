using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Uncreated.Networking;
using Uncreated.Networking.Async;
using Uncreated.SQL;
using Uncreated.Warfare.Singletons;

namespace Uncreated.Warfare.Sync;
public static class ListSync
{
    private static readonly Dictionary<Type, int> SyncIds = new Dictionary<Type, int>(12);
    private static readonly List<KeyValuePair<ushort, PrimaryKey>> SendQueue = new List<KeyValuePair<ushort, PrimaryKey>>(16);
    private static bool _reflected;
    private static void GetAllSyncTypes()
    {
        Type[] types = Assembly.GetExecutingAssembly().GetTypes();
        L.Log("[LIST SYNC] Searching for sync types:");
        using IDisposable indent = L.IndentLog(1);
        for (int i = 0; i < types.Length; ++i)
        {
            Type type = types[i];
            if (SyncIds.ContainsKey(type))
                continue;
            if (Attribute.GetCustomAttribute(type, typeof(SyncAttribute)) is SyncAttribute attr &&
                attr.SyncId > 0 && attr.SyncId <= ushort.MaxValue && attr.SyncMode != SyncMode.NoSync)
            {
                L.Log("[LIST SYNC] Found syncable type: " + type.Name + ".", ConsoleColor.Magenta);
                SyncIds.Add(type, attr.SyncId);
            }
        }
        _reflected = true;
    }
    public static void OnItemUpdated<TList, TItem>(PrimaryKey key) where TList : ListSqlConfig<TItem> where TItem : class, IListItem, new()
    {
        if (SyncIds.TryGetValue(typeof(T), out int syncId))
        {
            if (!(syncId <= 0 || syncId > ushort.MaxValue))
            {
                OnItemUpdated(key, (ushort)syncId);
                return;
            }
        }
        else if (!_reflected)
        {
            Type type = typeof(TList);
            if (Attribute.GetCustomAttribute(type, typeof(SyncAttribute)) is SyncAttribute attr &&
                attr.SyncId > 0 && attr.SyncId <= ushort.MaxValue && attr.SyncMode != SyncMode.NoSync)
            {
                SyncIds.Add(type, attr.SyncId);
                OnItemUpdated(key, (ushort)attr.SyncId);
                return;
            }
            else SyncIds.Add(type, -1);
        }

        throw new ArgumentException(nameof(TList), typeof(TList).Name + " does not have the SyncAttribute applied to it with a unique ID.");
    }
    public static void OnItemUpdated(PrimaryKey key, ushort syncId)
    {
        lock (SendQueue)
            SendQueue.Add(new KeyValuePair<ushort, PrimaryKey>(syncId, key));
        if (UCWarfare.CanUseNetCall)
        {
            Task.Run(async () =>
            {
                if (UCWarfare.CanUseNetCall)
                {
                    RequestResponse resp = await NetCalls.MulticastListItemUpdated.RequestAck(UCWarfare.I.NetClient!, syncId, key, 10000);
                    if (resp.Responded && (!resp.ErrorCode.HasValue || resp.ErrorCode.Value == (int)StandardErrorCode.Success))
                    {
                        lock (SendQueue)
                        {
                            int ind = SendQueue.FindLastIndex(x => x.Key == syncId && x.Value.Key == key.Key);
                            SendQueue.RemoveAt(ind);
                        }
                    }
                }
            });
        }
    }

    public static void OnConnected(IConnection connection)
    {
        List<KeyValuePair<ushort, PrimaryKey>> q;
        lock (SendQueue)
        {
            if (SendQueue.Count == 0)
                return;
            q = new List<KeyValuePair<ushort, PrimaryKey>>(SendQueue.Count);
            for (int i = 0; i < SendQueue.Count; ++i)
            {
                ushort a = SendQueue[i].Key;
                PrimaryKey b = SendQueue[i].Value;
                for (int j = 0; j < q.Count; ++j)
                {
                    if (a == q[j].Key && b == q[j].Value)
                        goto skip;
                }

                q.Add(new KeyValuePair<ushort, PrimaryKey>(a, b));
                skip:;
            }
        }

        q.Sort((a, b) => a.Key.CompareTo(b.Key));
        Task.Run(async () =>
        {
            ushort lastGrp = 0;
            List<int> pks = new List<int>(16);
            for (int i = 0; i < q.Count; i++)
            {
                if (lastGrp != q[i].Key)
                {
                    if (pks.Count > 0)
                    {
                        if (!connection.IsActive)
                            return;
                        NetTask task;
                        if (pks.Count == 1)
                            task = NetCalls.MulticastListItemUpdated.RequestAck(connection, lastGrp, pks[0], 10000);
                        else
                            task = NetCalls.MulticastListItemsUpdated.RequestAck(connection, lastGrp, pks.ToArray(), 10000 + 2000 * pks.Count);
                        RequestResponse resp = await task;
                        if (resp.Responded && (!resp.ErrorCode.HasValue || resp.ErrorCode.Value == (int)StandardErrorCode.Success))
                        {
                            for (int j = 0; j < pks.Count; j++)
                            {
                                int pk = pks[j];
                                lock (SendQueue)
                                {
                                    int ind = SendQueue.FindLastIndex(x => x.Key == lastGrp && x.Value.Key == pk);
                                    SendQueue.RemoveAt(ind);
                                }
                            }
                        }
                        pks.Clear();
                    }
                    lock (SendQueue)
                        lastGrp = SendQueue[i].Key;
                }

                lock (SendQueue)
                    pks.Add(SendQueue[i].Value.Key);
            }
        });
    }
    private static async Task<IListConfig?> UpdateGetType(ushort syncId)
    {
        await UCWarfare.ToUpdate();
        if (!_reflected) GetAllSyncTypes();
        Type? t = null;
        foreach (KeyValuePair<Type, int> v in SyncIds)
        {
            if (v.Value == syncId)
            {
                t = v.Key;
                break;
            }
        }

        if (t == null)
        {
            L.LogWarning("[LIST SYNC] Unknown sync type: " + syncId + "!");
            return null;
        }

        IUncreatedSingleton? singleton = Data.Singletons.GetSingleton(t);
        if (singleton == null || !singleton.IsLoaded)
        {
            L.LogDebug("[LIST SYNC] Voided update for unloaded singleton: " + t.Name + ".");
            return null;
        }
        if (singleton is not IListConfig config)
        {
            L.LogWarning("[LIST SYNC] Sync type: " + t.Name + " (" + syncId + ") is not an IListConfig!");
            return null;
        }
        return config;
    }
    private static async Task UpdateItem(PrimaryKey key, ushort syncId)
    {
        IListConfig? config = await UpdateGetType(syncId);
        if (config == null)
            return;
        await config.RefreshItem(key).ConfigureAwait(false);
        L.LogDebug("[LIST SYNC] Updated key " + key + " in list " + config.GetType().Name + ".");
    }
    private static async Task UpdateItems(PrimaryKey[] keys, ushort syncId)
    {
        IListConfig? config = await UpdateGetType(syncId);
        if (config == null)
            return;
        await config.RefreshItems(keys).ConfigureAwait(false);
        L.LogDebug("[LIST SYNC] Updated " + keys.Length + " key(s) in list " + config.GetType().Name + ":");
        L.LogDebug("[LIST SYNC] " + string.Join(", ", keys));
    }
    public static class NetCalls
    {
        public static readonly NetCall<ushort, int> MulticastListItemUpdated = new NetCall<ushort, int>(ReceiveListItemUpdated);
        public static readonly NetCall<ushort, int[]> MulticastListItemsUpdated = new NetCall<ushort, int[]>(ReceiveListItemsUpdated);

        [NetCall(ENetCall.FROM_SERVER, 3000)]
        private static Task ReceiveListItemUpdated(MessageContext ctx, ushort syncId, int pkraw)
        {
            PrimaryKey pk = pkraw;
            return UpdateItem(pk, syncId);
        }

        [NetCall(ENetCall.FROM_SERVER, 3001)]
        private static Task ReceiveListItemsUpdated(MessageContext ctx, ushort syncId, int[] pksraw)
        {
            PrimaryKey[] pks = new PrimaryKey[pksraw.Length];
            for (int i = 0; i < pksraw.Length; ++i)
                pks[i] = pksraw[i];
            return UpdateItems(pks, syncId);
        }
    }
}
