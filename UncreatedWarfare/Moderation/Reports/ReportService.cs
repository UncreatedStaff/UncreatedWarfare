using DanielWillett.ModularRpcs.Annotations;
using SDG.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.Moderation.Reports;

[RpcClass(DefaultTypeName = "Uncreated.Web.Bot.Services.DiscordReportService, uncreated-web")]
public class ReportService : IDisposable, IEventListener<PlayerLeft>, IEventListener<PlayerChatSent>
{
    private static readonly TimeSpan RequestableDataKeepDuration = TimeSpan.FromHours(24d);
    private static readonly TimeSpan PlayerDataKeepDuration = TimeSpan.FromHours(2d);

    private const int ChatMessageBufferSize = 32;
    private const int ShotBufferSize = 512;

    private readonly SemaphoreSlim _reportLock = new SemaphoreSlim(1, 1);
    private readonly IPlayerService _playerService;
    private readonly DatabaseInterface _moderationSql;
    private readonly ILoopTicker _loopTicker;
    private readonly PlayerDictionary<PlayerData> _playerData = new PlayerDictionary<PlayerData>(128);

    // data requestable for the next 24 hours if needed but not saved by default
    private readonly List<RequestableData> _requestableData = new List<RequestableData>();
    private readonly ILogger<ReportService> _logger;

    public ReportService(IPlayerService playerService, ILoopTickerFactory loopTickerFactory, DatabaseInterface moderationSql, ILogger<ReportService> logger)
    {
        _playerService = playerService;
        _moderationSql = moderationSql;
        _logger = logger;
        _loopTicker = loopTickerFactory.CreateTicker(TimeSpan.FromMinutes(15d), false, true, CheckExpiredRequestableData);

        UseableGun.onBulletSpawned += OnBulletSpawned;
        UseableGun.onProjectileSpawned += OnProjectileSpawned;
        UseableGun.onBulletHit += OnBulletHit;
    }

    private void OnProjectileSpawned(UseableGun sender, GameObject projectile)
    {
        
    }

    private void OnBulletHit(UseableGun gun, BulletInfo bullet, InputInfo hit, ref bool shouldallow)
    {
        
    }

    private void OnBulletSpawned(UseableGun gun, BulletInfo bullet)
    {
        
    }

    void IDisposable.Dispose()
    {
        _loopTicker.Dispose();
    }

    public async Task<Report> StartReport(CSteamID target, IModerationActor? reporter, ReportType reportType, CancellationToken token = default)
    {
        if (reportType is not ReportType.Custom and not ReportType.Griefing and not ReportType.ChatAbuse and not ReportType.VoiceChatAbuse)
            throw new ArgumentOutOfRangeException(nameof(reportType));

        WarfarePlayer? onlinePlayer = _playerService.GetOnlinePlayerOrNullThreadSafe(target);

        DateTimeOffset startTime = DateTimeOffset.UtcNow;

        await UniTask.SwitchToMainThread(token);

        Type clrType = reportType switch
        {
            ReportType.Griefing => typeof(GriefingReport),
            ReportType.ChatAbuse => typeof(ChatAbuseReport),
            ReportType.VoiceChatAbuse => typeof(VoiceChatAbuseReport),
            _ => typeof(Report)
        };

        Report report = (Report)Activator.CreateInstance(clrType);

        _playerData.TryGetValue(target, out PlayerData? playerData);

        report.Player = target.m_SteamID;
        report.Type = reportType;
        report.StartedTimestamp = startTime;

        // spy screenshot
        if (onlinePlayer is { IsOnline: true })
        {
            byte[] spyResult = await SpyTask.RequestScreenshot(onlinePlayer.SteamPlayer, TimeSpan.FromSeconds(5d));
            await UniTask.SwitchToMainThread(token);

            if (spyResult is { Length: > 0 })
            {
                if (report.ShouldScreenshot)
                    report.ScreenshotJpgData = spyResult;
                else
                    AddRequestableScreenshotData(report, spyResult);
            }
        }
        if (reporter != null)
            report.Actors = [ new RelatedActor(RelatedActor.RoleReporter, false, reporter) ];

        await FillReportDetails(report, playerData, token);

        await _reportLock.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await UniTask.SwitchToMainThread(token);


        }
        finally
        {
            _reportLock.Release();
        }

        return report;
    }

    [RpcReceive]
    public async Task<byte[]?> RequestScreenshot(uint reportId, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        for (int i = _requestableData.Count - 1; i >= 0; --i)
        {
            RequestableData requestableData = _requestableData[i];
            if (requestableData is not ScreenshotData scData || requestableData.Report.Id != reportId)
            {
                continue;
            }

            requestableData.Report.ScreenshotJpgData = scData.Jpg;

            try
            {
                await _moderationSql.AddOrUpdate(requestableData.Report, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to update report {0} with requested screenshot data.", reportId);
            }

            return scData.Jpg;
        }

        return null;
    }

    private void CheckExpiredRequestableData(ILoopTicker ticker, TimeSpan timesincestart, TimeSpan deltatime)
    {
        int maxIndex = -1;
        DateTime now = DateTime.UtcNow;
        for (int i = 0; i < _requestableData.Count; ++i)
        {
            RequestableData data = _requestableData[i];
            if (now - data.Saved <= RequestableDataKeepDuration)
                break;

            maxIndex = i;
        }

        _requestableData.RemoveRange(0, maxIndex + 1);
    }

    private void AddRequestableScreenshotData(Report report, byte[] spyResult)
    {
        _requestableData.Add(new ScreenshotData { Jpg = spyResult, Report = report, Saved = DateTime.UtcNow });
    }

    private async UniTask FillReportDetails(Report report, PlayerData? playerData, CancellationToken token)
    {
        switch (report)
        {
            case ChatAbuseReport chatAbuseReport:
                // chat records
                if (playerData is { ChatMessages.Count: > 0 })
                {
                    AbusiveChatRecord[] records = new AbusiveChatRecord[playerData.ChatMessages.Count];
                    int ind = -1;
                    foreach (ChatRecord cr in playerData.ChatMessages)
                    {
                        records[++ind] = new AbusiveChatRecord(cr.Count == 1 ? cr.Message : $"[x{cr.Count}] {cr.Message}", cr.Timestamp);
                    }

                    chatAbuseReport.Messages = records;
                }

                break;

            case VoiceChatAbuseReport vcReport:
                // voice chat record

                AudioRecordPlayerComponent? recordComp = playerData?.OnlinePlayer?.ComponentOrNull<AudioRecordPlayerComponent>();
                if (recordComp != null)
                {
                    using MemoryStream stream = new MemoryStream(262144);
                    AudioRecordManager.AudioConvertResult status = await recordComp.TryConvert(stream, true, token);
                    if (status == AudioRecordManager.AudioConvertResult.Success)
                    {
                        vcReport.PreviousVoiceData = stream.ToArray();
                    }
                }

                break;

            case CheatingReport cheating:
                
                // shots
                if (playerData is { Shots.Count: > 0 })
                {
                    cheating.Shots = playerData.Shots.ToArray();
                }

                break;
        }
    }

    private PlayerData GetOrAddPlayerData(WarfarePlayer player)
    {
        GameThread.AssertCurrent();

        if (!_playerData.TryGetValue(player, out PlayerData? data))
            _playerData.Add(player, data = new PlayerData { PlayerId = player.Steam64, OnlinePlayer = player });

        return data;
    }

    void IEventListener<PlayerChatSent>.HandleEvent(PlayerChatSent e, IServiceProvider serviceProvider)
    {
        PlayerData data = GetOrAddPlayerData(e.Player);

        data.AddChatMessage(e.OriginalText);
    }

    void IEventListener<PlayerLeft>.HandleEvent(PlayerLeft e, IServiceProvider serviceProvider)
    {
        DateTime now = DateTime.UtcNow;
        if (_playerData.TryGetValue(e.Player, out PlayerData? playerData))
        {
            playerData.LeftTime = now;
        }

        List<PlayerData>? toRemove = null;
        foreach (PlayerData data in _playerData.Values)
        {
            if (now - data.LeftTime <= PlayerDataKeepDuration)
                continue;

            (toRemove ??= ListPool<PlayerData>.claim()).Add(data);
        }

        if (toRemove != null)
        {
            foreach (PlayerData data in toRemove)
            {
                _playerData.Remove(data.PlayerId);
            }
        }

        if (toRemove != null)
            ListPool<PlayerData>.release(toRemove);
    }

    private class ScreenshotData : RequestableData
    {
        public required byte[] Jpg;
    }

    private class RequestableData
    {
        public required DateTime Saved;
        public required Report Report;
    }

    private class PlayerData
    {
        public required CSteamID PlayerId;
        public required WarfarePlayer OnlinePlayer;
        public DateTime LeftTime;

        public readonly RingBuffer<ChatRecord> ChatMessages = new RingBuffer<ChatRecord>(ChatMessageBufferSize);

        public readonly RingBuffer<ShotRecord> Shots = new RingBuffer<ShotRecord>(ShotBufferSize);

        public void AddChatMessage(string message)
        {
            if (ChatMessages.Count > 0)
            {
                ChatRecord lastChatMessage = ChatMessages[^1];
                if (lastChatMessage.Message.Equals(message, StringComparison.Ordinal))
                {
                    ++lastChatMessage.Count;
                    lastChatMessage.Timestamp = DateTime.UtcNow;
                    ChatMessages[^1] = lastChatMessage;
                    return;
                }
            }

            ChatMessages.Add(new ChatRecord { Count = 1, Message = message, Timestamp = DateTime.UtcNow });
        }

        public void AddShot(ShotRecord shot)
        {
            Shots.Add(shot);
        }
    }

    private struct ChatRecord
    {
        public required string Message;
        public required int Count;
        public required DateTime Timestamp;
    }
}