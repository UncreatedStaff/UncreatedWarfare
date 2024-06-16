// #define DEBUG_LOGGING
using HarmonyLib;
using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Uncreated.Warfare.Events;
using UnityEngine;

namespace Uncreated.Warfare.Components;
internal class DebugComponent : MonoBehaviour
{
    public uint Updates;
    private float _startRt;
    private float _lastDt;
    private float _lastFixed;
    private float _frmRt;
    private float _avgFrameRate;
    private float _maxUpdateSpeed;
    private float _maxFixedUpdateSpeed;
    private int _ttlBytesPlayers;
    private int _ttlBytesPending;
    private int _ttlBytesOther;
    private readonly Queue<UCPlayer> _pingUpdates = new Queue<UCPlayer>(48);
    private readonly List<UCPlayer> _lagging = new List<UCPlayer>(48);
    private readonly List<ulong> _lagged = new List<ulong>(96);
    private static bool _hasPatched;
    [UsedImplicitly]
    private void Start()
    {
        Reset();
        EventDispatcher.PlayerLeft += OnDisconnect;
        if (!_hasPatched)
        {
            _hasPatched = true;
            try
            {
                Harmony.Patches.Patcher.Patch(typeof(SteamPlayer).GetMethod(nameof(SteamPlayer.lag), BindingFlags.Instance | BindingFlags.Public),
                    postfix: new HarmonyMethod(typeof(DebugComponent).GetMethod(nameof(LagPostfix), BindingFlags.Static | BindingFlags.NonPublic)));
            }
            catch (Exception ex)
            {
                L.LogError("Failed to patch SteamPlayer.lag, ping spike monitoring won't be active.");
                L.LogError(ex);
            }
            try
            {
                Harmony.Patches.Patcher.Patch(typeof(Provider).Assembly.GetType("SDG.Unturned.NetMessages", true, false).GetMethod("ReceiveMessageFromClient", BindingFlags.Static | BindingFlags.Public),
                    postfix: new HarmonyMethod(typeof(DebugComponent).GetMethod(nameof(ReceiveClientMessagePostfix), BindingFlags.Static | BindingFlags.NonPublic)));
            }
            catch (Exception ex)
            {
                L.LogError("Failed to patch NetMessages.ReceiveMessageFromClient, network monitoring won't be active.");
                L.LogError(ex);
            }
        }
    }
    [UsedImplicitly]
    private void OnDestroy()
    {
        EventDispatcher.PlayerLeft -= OnDisconnect;
    }
    public void OnDisconnect(PlayerEvent e)
    {
        ulong id = e.Steam64;
        for (int i = 0; i < _lagging.Count; i++)
        {
            if (_lagging[i].Steam64 == id)
            {
                _lagging.RemoveAtFast(i);
                break;
            }
        }
    }
    public void Reset()
    {
        _lagging.RemoveAll(x => !x.IsOnline);
        if (Updates > 0)
            Dump();
        Updates = 0;
        _startRt = Time.realtimeSinceStartup;
        _lastFixed = _startRt;
        _frmRt = 1f / Application.targetFrameRate;
        _maxUpdateSpeed = _frmRt * 1.75f;
        _maxFixedUpdateSpeed = Time.fixedDeltaTime * 1.75f;
        _ttlBytesPlayers = 0;
        _ttlBytesOther = 0;
        _ttlBytesPending = 0;
        _lagged.Clear();
    }
    [UsedImplicitly]
    private void Update()
    {
        _avgFrameRate = (_avgFrameRate * Updates + Time.deltaTime) / ++Updates;
        _lastDt = Time.deltaTime;
#if DEBUG && DEBUG_LOGGING
        if (_lastDt > _maxUpdateSpeed && Level.isLoaded && UCWarfare.I is not null && UCWarfare.I.FullyLoaded)
            L.LogWarning("Update took " + _lastDt.ToString("F6", Data.AdminLocale) + " seconds, higher than the max: " + _maxUpdateSpeed.ToString("F3", Data.AdminLocale) + "!!", ConsoleColor.Yellow);
#endif
    }
    [UsedImplicitly]
    private void FixedUpdate()
    {
        float t = Time.realtimeSinceStartup;
#if DEBUG && DEBUG_LOGGING
        if (t - _lastFixed > _maxFixedUpdateSpeed && Level.isLoaded && UCWarfare.I is not null && UCWarfare.I.FullyLoaded)
            L.LogWarning("FixedUpdate took " + (t - _lastFixed).ToString("F6", Data.AdminLocale) + " seconds, higher than the max: " + _maxFixedUpdateSpeed.ToString("F3", Data.AdminLocale) + "!!", ConsoleColor.Yellow);
#endif
        _lastFixed = t;
    }
    [UsedImplicitly]
    private void LateUpdate()
    {
        while (_pingUpdates.Count > 0)
        {
            UCPlayer pl = _pingUpdates.Dequeue();
            if (!pl.IsOnline) continue;
            if (pl.Player.TryGetPlayerData(out UCPlayerData data))
                AnalyzePing(pl, data);
        }
    }
    private void AnalyzePing(UCPlayer pl, UCPlayerData data)
    {
        float latest = pl.SteamPlayer.ping * 1000f;
        data.AddPing(latest);
        float[] pings = data.PingBuffer;
        int size = Math.Min(UCPlayerData.PingBufferSize, data.PingBufferIndex + 1);
        float total = 0f;
        for (int i = 0; i < size; ++i)
            total += pings[i];
        float mean = total / size;
        total = 0f;
        for (int i = 0; i < size; ++i)
        {
            float d = pings[i] - mean;
            total += d < 0 ? -d : d;//d * d;
        }
        float avgDifference = total / size;//Mathf.Sqrt(total);
        float lastPing;
        if (size < 2)
            lastPing = latest;
        else
        {
            int ind = data.PingBufferIndex % UCPlayerData.PingBufferSize;
            if (ind == 0)
                ind = UCPlayerData.PingBufferSize - 1;
            lastPing = data.PingBuffer[ind - 1];
        }
        lastPing -= latest;
        data.LastAvgPingDifference = avgDifference;
        if (size > 16) // sufficient data
        {
            if (lastPing > 9.999f && lastPing < 10.001f) lastPing = 10.05f;
            float t = 5f * Mathf.Sqrt(latest + 5f);
            //L.LogDebug("Ping added: " + latest.ToString("F6", Data.Locale) + ", Average difference of " + pl.CharacterName + "'s ping: " + avgDifference.ToString("F3", Data.Locale) + " / " + t.ToString("F3", Data.Locale));
            if (Mathf.Abs(lastPing) < t)
                return;
            if (lastPing > 0)
                RecoverFromLag(pl);
            else
                StartLag(pl);
        }
    }
    private void RecoverFromLag(UCPlayer player)
    {
        for (int i = _lagging.Count - 1; i >= 0; --i)
        {
            UCPlayer pl = _lagging[i];
            if (pl.Steam64 == player.Steam64 || !pl.IsOnline)
                _lagging.RemoveAtFast(i);
        }
        L.LogWarning("Lag settled for " + player.CharacterName + ".", ConsoleColor.Yellow);
    }
    private void StartLag(UCPlayer player)
    {
        for (int i = _lagging.Count - 1; i >= 0; --i)
        {
            UCPlayer pl = _lagging[i];
            if (!pl.IsOnline)
                _lagging.RemoveAtFast(i);
            else if (pl.Steam64 == player.Steam64)
                goto inList;
        }
        _lagging.Add(player);
        for (int i = 0; i < _lagged.Count; ++i)
        {
            if (_lagged[i] == player.Steam64)
                goto inList;
        }

        _lagged.Add(player.Steam64);
    inList:
        L.LogWarning("Lag detected from " + player.CharacterName + ".", ConsoleColor.Yellow);
        if (Provider.clients.Count >= 3 ? (float)_lagging.Count / Provider.clients.Count > 0.6f : _lagging.Count >= 1)
        {
            L.LogWarning("A lot of players are lagging, lag spike detected.", ConsoleColor.Yellow);
            Dump();
        }
    }
    public void Dump()
    {
        float t = Time.realtimeSinceStartup;
        float ttlSeconds = t - _startRt;
        L.Log("Debug output for the last " + ttlSeconds.ToString("F3", Data.AdminLocale) + " seconds.");
        using IDisposable indent = L.IndentLog(2);
        L.Log("Updates: " + Updates.ToString(Data.AdminLocale));
        L.Log("Average framerate: " + (1f / _avgFrameRate).ToString(Data.AdminLocale) + " FPS (target: " + Application.targetFrameRate + " FPS)");
        if (_ttlBytesPlayers > 0)
            L.Log($"Network usage verified players:     {_ttlBytesPlayers.ToString(Data.AdminLocale)} bytes ({(_ttlBytesPlayers / ttlSeconds).ToString("F2", Data.AdminLocale)} B/s)");
        if (_ttlBytesPending > 0)
            L.Log($"Network usage pending players:      {_ttlBytesPending.ToString(Data.AdminLocale)} bytes ({(_ttlBytesPending / ttlSeconds).ToString("F2", Data.AdminLocale)} B/s)");
        if (_ttlBytesOther > 0)
            L.Log($"Network usage non-players:          {_ttlBytesOther.ToString(Data.AdminLocale)} bytes ({(_ttlBytesOther / ttlSeconds).ToString("F2", Data.AdminLocale)} B/s)");
        if (_lagged.Count > 0)
            L.Log(_lagged.Count + " Players lagged, currently " + _lagging.Count + " lagging.");
    }
    private void OnPingUpdated(UCPlayer player)
    {
        _pingUpdates.Enqueue(player);
    }
    private static void LagPostfix(float value, SteamPlayer __instance)
    {
        if (UCWarfare.I.Debugger != null)
        {
            UCPlayer? pl = UCPlayer.FromSteamPlayer(__instance);
            if (pl is not null)
                UCWarfare.I.Debugger.OnPingUpdated(pl);
        }
    }
    private static void ReceiveClientMessagePostfix(ITransportConnection transportConnection, byte[] packet, int offset, int size)
    {
        if (UCWarfare.I != null && UCWarfare.I.Debugger != null)
        {
            UCWarfare.I.Debugger.OnMessageReceived(transportConnection, packet, offset, size);
        }
    }
    private void OnMessageReceived(ITransportConnection transportConnection, byte[] packet, int offset, int size)
    {
        SteamPlayerID? owner = null;
        bool pending = false;
        for (int i = 0; i < Provider.clients.Count; ++i)
        {
            if (transportConnection == Provider.clients[i].transportConnection)
            {
                owner = Provider.clients[i].playerID;
            }
        }
        if (owner is null)
        {
            for (int i = 0; i < Provider.pending.Count; ++i)
            {
                if (transportConnection == Provider.pending[i].transportConnection)
                {
                    pending = true;
                    owner = Provider.pending[i].playerID;
                }
            }
        }
        if (owner is not null)
        {
            if (pending)
                _ttlBytesPending += size;
            else
                _ttlBytesPlayers += size;
        }
        else _ttlBytesOther += size;
    }
}
