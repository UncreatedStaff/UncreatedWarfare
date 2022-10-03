using HarmonyLib;
using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Uncreated.Warfare.Components;
internal class DebugComponent : MonoBehaviour
{
    private float _startRt;
    private float _lastDt;
    private float _lastFixed;
    private float frmRt;
    private float avgFrameRate;
    private uint updates;
    private float _maxUpdateSpeed;
    private float _maxFixedUpdateSpeed;
    private int ttlBytesPlayers;
    private int ttlBytesPending;
    private int ttlBytesOther;
    private readonly Queue<UCPlayer> PingUpdates = new Queue<UCPlayer>(48);
    private readonly List<UCPlayer> Lagging = new List<UCPlayer>(48);
    private readonly List<ulong> Lagged = new List<ulong>(96);
    private void Start()
    {
        Reset();
        try
        {
            Patches.Patcher.Patch(typeof(SteamPlayer).GetMethod(nameof(SteamPlayer.lag), BindingFlags.Instance | BindingFlags.Public),
                postfix: new HarmonyMethod(typeof(DebugComponent).GetMethod(nameof(LagPostfix), BindingFlags.Static | BindingFlags.NonPublic)));
        }
        catch (Exception ex)
        {
            L.LogError("Failed to patch SteamPlayer.lag, ping spike monitoring won't be active.");
            L.LogError(ex);
        }
        try
        {
            Patches.Patcher.Patch(typeof(Provider).Assembly.GetType("SDG.Unturned.NetMessages", true, false).GetMethod("ReceiveMessageFromClient", BindingFlags.Static | BindingFlags.Public),
                postfix: new HarmonyMethod(typeof(DebugComponent).GetMethod("ReceiveClientMessagePostfix", BindingFlags.Static | BindingFlags.NonPublic)));
        }
        catch (Exception ex)
        {
            L.LogError("Failed to patch NetMessages.ReceiveMessageFromClient, network monitoring won't be active.");
            L.LogError(ex);
        }
    }
    public void Reset()
    {
        Lagging.RemoveAll(x => !x.IsOnline);
        if (updates > 0)
            Dump();
        updates = 0;
        _startRt = Time.realtimeSinceStartup;
        _lastFixed = _startRt;
        frmRt = 1f / Application.targetFrameRate;
        _maxUpdateSpeed = frmRt * 1.75f;
        _maxFixedUpdateSpeed = Time.fixedDeltaTime * 1.75f;
        ttlBytesPlayers = 0;
        ttlBytesOther = 0;
        ttlBytesPending = 0;
        Lagged.Clear();
    }
    private void Update()
    {
        avgFrameRate = (avgFrameRate * updates + Time.deltaTime) / ++updates;
        _lastDt = Time.deltaTime;
        if (_lastDt > _maxUpdateSpeed && Level.isLoaded)
            L.LogWarning("Update took " + _lastDt.ToString("F6", Data.Locale) + " seconds, higher than the max: " + _maxUpdateSpeed.ToString("F3", Data.Locale) + "!!", ConsoleColor.Yellow);

    }
    private void FixedUpdate()
    {
        float t = Time.realtimeSinceStartup;
        if (t - _lastFixed > _maxFixedUpdateSpeed && Level.isLoaded)
            L.LogWarning("FixedUpdate took " + (t - _lastFixed).ToString("F6", Data.Locale) + " seconds, higher than the max: " + _maxFixedUpdateSpeed.ToString("F3", Data.Locale) + "!!", ConsoleColor.Yellow);
        _lastFixed = t;
    }
    private void LateUpdate()
    {
        while (PingUpdates.Count > 0)
        {
            UCPlayer pl = PingUpdates.Dequeue();
            if (!pl.IsOnline) continue;
            if (pl.Player.TryGetPlayerData(out UCPlayerData data))
                AnalyzePing(pl, data);
        }
    }
    private void AnalyzePing(UCPlayer pl, UCPlayerData data)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float latest = pl.SteamPlayer.ping * 1000f;
        data.AddPing(latest);
        float[] pings = data.PingBuffer;
        int size = Math.Min(UCPlayerData.PING_BUFFER_SIZE, data.PingBufferIndex + 1);
        float total = 0f;
        for (int i = 0; i < size; ++i)
            total += pings[i];
        float mean = total / size;
        total = 0f;
        float d;
        for (int i = 0; i < size; ++i)
        {
            d = pings[i] - mean;
            total += d < 0 ? -d : d;//d * d;
        }
        float avgDifference = total / size;//Mathf.Sqrt(total);
        float dif;

        float lastPing;
        if (size < 2)
            lastPing = latest;
        else
        {
            int ind = data.PingBufferIndex % UCPlayerData.PING_BUFFER_SIZE;
            if (ind == 0)
                ind = UCPlayerData.PING_BUFFER_SIZE - 1;
            lastPing = data.PingBuffer[ind - 1];
        }
        lastPing -= latest;
        if (data.LastAvgPingDifference == 0)
            dif = avgDifference;
        else
            dif = data.LastAvgPingDifference - avgDifference;
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
        for (int i = Lagging.Count - 1; i >= 0; --i)
        {
            UCPlayer pl = Lagging[i];
            if (pl.Steam64 == player.Steam64 || !pl.IsOnline)
                Lagging.RemoveAtFast(i);
        }
        L.LogWarning("Lag settled for " + player.CharacterName + ".", ConsoleColor.Yellow);
    }
    private void StartLag(UCPlayer player)
    {
        for (int i = Lagging.Count - 1; i >= 0; --i)
        {
            UCPlayer pl = Lagging[i];
            if (!pl.IsOnline)
                Lagging.RemoveAtFast(i);
            else if (pl.Steam64 == player.Steam64)
                goto inList;
        }
        Lagging.Add(player);
        for (int i = 0; i < Lagged.Count; ++i)
        {
            if (Lagged[i] == player.Steam64)
                goto inList;
        }

        Lagged.Add(player.Steam64);
    inList:
        L.LogWarning("Lag detected from " + player.CharacterName + ".", ConsoleColor.Yellow);
        if (Provider.clients.Count >= 3 ? (float)Lagging.Count / Provider.clients.Count > 0.6f : Lagging.Count >= 1)
        {
            L.LogWarning("A lot of players are lagging, lag spike detected.", ConsoleColor.Yellow);
            Dump();
        }
    }
    public void Dump()
    {
        float t = Time.realtimeSinceStartup;
        float ttlSeconds = t - _startRt;
        L.Log("Debug output for the last " + ttlSeconds.ToString("F3", Data.Locale) + " seconds.");
        using IDisposable indent = L.IndentLog(2);
        L.Log("Updates: " + updates.ToString(Data.Locale));
        L.Log("Average framerate: " + (1f / avgFrameRate).ToString(Data.Locale) + " FPS (target: " + Application.targetFrameRate + " FPS)");
        if (ttlBytesPlayers > 0)
            L.Log($"Network usage verified players:     {ttlBytesPlayers.ToString(Data.Locale)} bytes ({(ttlBytesPlayers / ttlSeconds).ToString("F2", Data.Locale)} B/s)");
        if (ttlBytesPending > 0)
            L.Log($"Network usage pending players:      {ttlBytesPending.ToString(Data.Locale)} bytes ({(ttlBytesPending / ttlSeconds).ToString("F2", Data.Locale)} B/s)");
        if (ttlBytesOther > 0)
            L.Log($"Network usage non-players:          {ttlBytesOther.ToString(Data.Locale)} bytes ({(ttlBytesOther / ttlSeconds).ToString("F2", Data.Locale)} B/s)");
        if (Lagged.Count > 0)
            L.Log(Lagged.Count + " Players lagged, currently " + Lagging.Count + " lagging.");
    }
    private void OnPingUpdated(UCPlayer player)
    {
        PingUpdates.Enqueue(player);
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
                ttlBytesPending += size;
            else
                ttlBytesPlayers += size;
        }
        else ttlBytesOther += size;
    }
}
