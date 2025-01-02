using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ReflectionTools;
using DanielWillett.SpeedBytes;
using SDG.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.Moderation.Reports;

[RpcClass(DefaultTypeName = "Uncreated.Web.Bot.Services.DiscordReportService, uncreated-web")]
[Priority(-100)]
public class ReportService : IDisposable, IHostedService, IEventListener<PlayerLeft>, IEventListener<PlayerChatSent>, IEventListener<PlayerUseableEquipped>
{
    private delegate float GetBulletDamageMultiplierHandler(UseableGun gun, ref BulletInfo bullet);

    private static readonly InstanceGetter<UseableGun, List<BulletInfo>>? GetBullets =
        Accessor.GenerateInstanceGetter<UseableGun, List<BulletInfo>>("bullets");
    private static readonly GetBulletDamageMultiplierHandler? GetBulletDamageMultiplier =
        Accessor.GenerateInstanceCaller<UseableGun, GetBulletDamageMultiplierHandler>("getBulletDamageMultiplier", throwOnError: false);

    private static readonly TimeSpan RequestableDataKeepDuration = TimeSpan.FromHours(24d);
    private static readonly TimeSpan PlayerDataKeepDuration = TimeSpan.FromHours(2d);

    private const int ChatMessageBufferSize = 32;
    private const int ShotBufferSize = 512;

    private readonly IPlayerService _playerService;
    private readonly DatabaseInterface _moderationSql;
    private readonly ILoopTickerFactory _loopTickerFactory;
    private readonly PlayerDictionary<PlayerData> _playerData = new PlayerDictionary<PlayerData>(128);

    // data requestable for the next 24 hours if needed but not saved by default
    private readonly List<RequestableData> _requestableData = new List<RequestableData>();
    private readonly ILogger<ReportService> _logger;
    private ILoopTicker? _loopTicker;

    public IEnumerable<WarfarePlayer> SelectPlayers => _playerData.Values.Select(x => x.OnlinePlayer);

    public ReportService(IPlayerService playerService, ILoopTickerFactory loopTickerFactory, DatabaseInterface moderationSql, ILogger<ReportService> logger)
    {
        _loopTickerFactory = loopTickerFactory;
        _playerService = playerService;
        _moderationSql = moderationSql;
        _logger = logger;
    }

    UniTask IHostedService.StartAsync(CancellationToken token)
    {
        _loopTicker = _loopTickerFactory.CreateTicker(TimeSpan.FromMinutes(15d), false, true, CheckExpiredRequestableData);
        if (GetBullets != null)
        {
            UseableGun.onProjectileSpawned += OnProjectileSpawned;
            UseableGun.onBulletSpawned += OnBulletSpawned;
            UseableGun.onBulletHit += OnBulletHit;
        }

        return UniTask.CompletedTask;
    }

    UniTask IHostedService.StopAsync(CancellationToken token)
    {
        if (GetBullets != null)
        {
            UseableGun.onProjectileSpawned -= OnProjectileSpawned;
            UseableGun.onBulletSpawned -= OnBulletSpawned;
            UseableGun.onBulletHit -= OnBulletHit;
        }

        return UniTask.CompletedTask;
    }

    void IDisposable.Dispose()
    {
        _loopTicker?.Dispose();
    }

    private void OnProjectileSpawned(UseableGun gun, GameObject projectile)
    {
        PlayerData playerData = GetOrAddPlayerData(_playerService.GetOnlinePlayer(gun));
        CheckExpiredBullets(playerData, gun);
    }

    private void OnBulletSpawned(UseableGun gun, BulletInfo bullet)
    {
        PlayerData playerData = GetOrAddPlayerData(_playerService.GetOnlinePlayer(gun));
        CheckExpiredBullets(playerData, gun);

        PendingBullet newBullet = new PendingBullet
        {
            Bullet = bullet,
            SpawnedTime = DateTime.UtcNow
        };

        playerData.PendingBullets.Add(newBullet);
        _logger.LogInformation("Bullet spawned: {0} {1} pellets: {2}", bullet.magazineAsset, bullet.pellet, bullet.magazineAsset.pellets);
    }

    private void OnBulletHit(UseableGun gun, BulletInfo bullet, InputInfo hit, ref bool shouldallow)
    {
        PlayerData playerData = GetOrAddPlayerData(_playerService.GetOnlinePlayer(gun));

        for (int i = 0; i < playerData.PendingBullets.Count; ++i)
        {
            PendingBullet pendingBullet = playerData.PendingBullets[i];
            if (pendingBullet.Bullet != bullet)
                continue;

            playerData.PendingBullets.RemoveAt(i);
            if (!shouldallow)
            {
                _logger.LogInformation("Bullet cancelled: {0} {1}", bullet.magazineAsset, bullet.pellet);
                return;
            }

            ItemGunAsset gunAsset = gun.equippedGunAsset;
            IModerationActor? actor = hit.player != null ? Actors.GetActor(hit.player.channel.owner.playerID.steamID) : null;

            ResourceSpawnpoint? resxSpawnpoint = null;

            InteractableObjectRubble? rubbleObject = hit.type == ERaycastInfoType.OBJECT ? hit.transform.GetComponentInParent<InteractableObjectRubble>() : null;

            Asset? hitAsset = hit.type switch
            {
                ERaycastInfoType.ANIMAL => hit.animal.asset,
                ERaycastInfoType.VEHICLE => hit.vehicle.asset,
                ERaycastInfoType.ZOMBIE => hit.zombie.difficulty,
                ERaycastInfoType.BARRICADE => BarricadeManager.FindBarricadeByRootTransform(hit.transform)?.asset,
                ERaycastInfoType.STRUCTURE => StructureManager.FindStructureByRootTransform(hit.transform)?.asset,
                ERaycastInfoType.RESOURCE => ResourceManager.tryGetRegion(hit.transform, out byte x, out byte y, out ushort index) ? (resxSpawnpoint = LevelGround.trees[x, y][index]).asset : null,
                ERaycastInfoType.OBJECT => rubbleObject?.asset,
                _ => null
            };

            float distance = Vector3.Distance(bullet.origin, hit.point);

            // ballistics(), calculates true damage

            // attachment dmg multiplier and falloff
            float dmgMult = (GetBulletDamageMultiplier?.Invoke(gun, ref bullet) ?? 1f)
                            * Mathf.Lerp(1f,
                                gunAsset.damageFalloffMultiplier,
                                Mathf.InverseLerp(gunAsset.range * gunAsset.damageFalloffRange, gunAsset.range * gunAsset.damageFalloffMaxRange,
                                    distance)
                            );

            float damage = 0;
            switch (hit.type)
            {
                case ERaycastInfoType.PLAYER:
                    damage = gunAsset.playerDamageMultiplier.multiply(hit.limb) * dmgMult;
                    break;

                case ERaycastInfoType.ZOMBIE:
                    damage = gunAsset.zombieOrPlayerDamageMultiplier.multiply(hit.limb) * dmgMult * hit.zombie.getBulletResistance();
                    break;

                case ERaycastInfoType.ANIMAL:
                    damage = gunAsset.animalOrPlayerDamageMultiplier.multiply(hit.limb) * dmgMult;
                    break;

                case ERaycastInfoType.VEHICLE:

                    if (playerData.ThirdAttachments == null)
                        playerData.ThirdAttachments = gun.player.equipment.thirdModel.GetComponent<Attachments>();

                    damage = gunAsset.animalOrPlayerDamageMultiplier.multiply(hit.limb) * dmgMult * (CanDamageInvulnerable(gun, playerData.ThirdAttachments)
                        ? Provider.modeConfigData.Vehicles.Gun_Highcal_Damage_Multiplier
                        : Provider.modeConfigData.Vehicles.Gun_Lowcal_Damage_Multiplier);
                    break;

                case ERaycastInfoType.BARRICADE:

                    if (playerData.ThirdAttachments == null)
                        playerData.ThirdAttachments = gun.player.equipment.thirdModel.GetComponent<Attachments>();

                    bool invBarricade = CanDamageInvulnerable(gun, playerData.ThirdAttachments);
                    if (hitAsset is ItemBarricadeAsset { canBeDamaged: true } b && (b.isVulnerable || invBarricade))
                    {
                        damage = gunAsset.barricadeDamage * dmgMult * (invBarricade
                            ? Provider.modeConfigData.Barricades.Gun_Highcal_Damage_Multiplier
                            : Provider.modeConfigData.Barricades.Gun_Lowcal_Damage_Multiplier);
                    }

                    break;

                case ERaycastInfoType.STRUCTURE:

                    if (playerData.ThirdAttachments == null)
                        playerData.ThirdAttachments = gun.player.equipment.thirdModel.GetComponent<Attachments>();

                    bool invStructure = CanDamageInvulnerable(gun, playerData.ThirdAttachments);
                    if (hitAsset is ItemStructureAsset { canBeDamaged: true } s && (s.isVulnerable || invStructure))
                    {
                        damage = gunAsset.barricadeDamage * dmgMult * (invStructure
                            ? Provider.modeConfigData.Structures.Gun_Highcal_Damage_Multiplier
                            : Provider.modeConfigData.Structures.Gun_Lowcal_Damage_Multiplier);
                    }

                    break;

                case ERaycastInfoType.RESOURCE:
                    if (resxSpawnpoint is { isDead: false } && gunAsset.hasBladeID(resxSpawnpoint.asset.bladeID))
                    {
                        damage = gunAsset.resourceDamage * dmgMult;
                    }
                    break;

                case ERaycastInfoType.OBJECT:

                    if (playerData.ThirdAttachments == null)
                        playerData.ThirdAttachments = gun.player.equipment.thirdModel.GetComponent<Attachments>();

                    if (rubbleObject is not null && rubbleObject.IsSectionIndexValid(hit.section)
                                                 && !rubbleObject.isSectionDead(hit.section)
                                                 && gunAsset.hasBladeID(rubbleObject.asset.rubbleBladeID)
                                                 && (rubbleObject.asset.rubbleIsVulnerable || CanDamageInvulnerable(gun, playerData.ThirdAttachments)))
                    {
                        damage = gunAsset.objectDamage * dmgMult;
                    }

                    break;
            }

            _logger.LogInformation("Bullet hit: {0} {1} dmg: {2} asset: {3}", bullet.magazineAsset, bullet.pellet, damage, hitAsset);

            playerData.AddShot(new ShotRecord(gunAsset.GUID, bullet.magazineAsset.GUID,
                gunAsset.itemName,
                bullet.magazineAsset.itemName,
                hit.type,
                actor, hitAsset?.GUID, hitAsset?.FriendlyName, hit.type is ERaycastInfoType.ANIMAL or ERaycastInfoType.PLAYER or ERaycastInfoType.ZOMBIE ? hit.limb : null,
                pendingBullet.SpawnedTime, bullet.origin, Quaternion.LookRotation(bullet.ApproximatePlayerAimDirection).eulerAngles, hit.point, false, damage <= 0 ? 0 : (int)damage, distance));
            break;
        }


        CheckExpiredBullets(playerData, gun);
    }

    private static bool CanDamageInvulnerable(UseableGun gun, Attachments? thirdAttachments)
    {
        if (gun.equippedGunAsset.isInvulnerable)
            return true;

        if (thirdAttachments == null)
        {
            return false;
        }

        return thirdAttachments.barrelAsset is { CanDamageInvulernableEntities: true }
            || thirdAttachments.tacticalAsset is { CanDamageInvulernableEntities: true }
            || thirdAttachments.gripAsset is { CanDamageInvulernableEntities: true }
            || thirdAttachments.sightAsset is { CanDamageInvulernableEntities: true }
            || thirdAttachments.magazineAsset is { CanDamageInvulernableEntities: true };
    }

    private void OnBulletExpired(UseableGun gun, PlayerData playerData, in PendingBullet pendingBullet)
    {
        BulletInfo bullet = pendingBullet.Bullet;
        _logger.LogInformation("Bullet expired: {0} {1}", bullet.magazineAsset, bullet.pellet);
        playerData.AddShot(new ShotRecord(gun.equippedGunAsset.GUID, bullet.magazineAsset.GUID,
            gun.equippedGunAsset.itemName, bullet.magazineAsset.itemName, 0, null, null, null, null,
            pendingBullet.SpawnedTime, bullet.origin, Quaternion.LookRotation(bullet.ApproximatePlayerAimDirection).eulerAngles, null, false, 0, 0d));
    }

    private void CheckExpiredBullets(PlayerData playerData, UseableGun gun)
    {
        List<BulletInfo> pendingBullets = GetBullets!(gun);
        for (int i = playerData.PendingBullets.Count - 1; i >= 0; --i)
        {
            PendingBullet bullet = playerData.PendingBullets[i];
            if (pendingBullets.Contains(bullet.Bullet))
                continue;

            OnBulletExpired(gun, playerData, in bullet);
            playerData.PendingBullets.RemoveAt(i);
        }
    }

    public async Task<(Report Report, bool Sent)> StartReport(CSteamID target, IModerationActor? reporter, string? message, ReportType reportType, CancellationToken token = default)
    {
        if (reportType is not ReportType.Custom and not ReportType.Griefing and not ReportType.ChatAbuse and not ReportType.VoiceChatAbuse and not ReportType.Cheating)
            throw new ArgumentOutOfRangeException(nameof(reportType));

        WarfarePlayer? onlinePlayer = _playerService.GetOnlinePlayerOrNullThreadSafe(target);

        DateTimeOffset startTime = DateTimeOffset.UtcNow;

        await UniTask.SwitchToMainThread(token);

        Type clrType = reportType switch
        {
            ReportType.Griefing => typeof(GriefingReport),
            ReportType.ChatAbuse => typeof(ChatAbuseReport),
            ReportType.VoiceChatAbuse => typeof(VoiceChatAbuseReport),
            ReportType.Cheating => typeof(CheatingReport),
            _ => typeof(Report)
        };

        Report report = (Report)Activator.CreateInstance(clrType);

        _playerData.TryGetValue(target, out PlayerData? playerData);

        report.Message = message;
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

        await _moderationSql.AddOrUpdate(report, token).ConfigureAwait(false);
        string? messageUrl;
        try
        {
            messageUrl = await SendReport(report.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending report.");
            return (report, false);
        }

        return (report, messageUrl != null);
    }

    [RpcSend]
    public virtual RpcTask<string?> SendReport(uint reportId) => RpcTask<string?>.NotImplemented;

    [RpcReceive]
    public async Task<ArraySegment<byte>> RequestShots(uint reportId, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);
        ShotRecord[]? records = null;
        for (int i = _requestableData.Count - 1; i >= 0; --i)
        {
            RequestableData requestableData = _requestableData[i];
            if (requestableData is not ShotData shotData || requestableData.Report.Id != reportId)
            {
                continue;
            }

            records = shotData.Shots;
            break;
        }

        if (records == null)
        {
            return new ArraySegment<byte>(Array.Empty<byte>());
        }

        await UniTask.SwitchToThreadPool();

        ByteWriter writer = new ByteWriter();
        writer.Write(records.Length);
        for (int i = 0; i < records.Length; ++i)
        {
            records[i].Write(writer);
        }

        return writer.ToArraySegmentAndDontFlush();
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

    private void CheckExpiredRequestableData(ILoopTicker ticker, TimeSpan timeSinceStart, TimeSpan deltaTime)
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
    private void AddRequestableShotData(Report report, PlayerData player)
    {
        _requestableData.Add(new ShotData { Shots = player.Shots.ToArray(), Report = report, Saved = DateTime.UtcNow });
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
                else if (playerData != null)
                {
                    AddRequestableShotData(cheating, playerData);
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

    [EventListener(MustRunInstantly = true)]
    void IEventListener<PlayerUseableEquipped>.HandleEvent(PlayerUseableEquipped e, IServiceProvider serviceProvider)
    {
        PlayerData playerData = GetOrAddPlayerData(e.Player);

        if (playerData.LastGunAsset != null)
        {
            for (int i = 0; i < playerData.PendingBullets.Count; ++i)
            {
                PendingBullet pendingBullet = playerData.PendingBullets[i];
                BulletInfo bullet = pendingBullet.Bullet;
                playerData.AddShot(new ShotRecord(playerData.LastGunAsset.GUID, bullet.magazineAsset?.GUID ?? Guid.Empty,
                    playerData.LastGunAsset.itemName, bullet.magazineAsset?.itemName, 0, null, null, null, null,
                    pendingBullet.SpawnedTime, bullet.origin, Quaternion.LookRotation(bullet.ApproximatePlayerAimDirection).eulerAngles, null, false, 0, 0d));
            }
        }

        playerData.PendingBullets.Clear();

        playerData.LastGunAsset = (e.Useable as UseableGun)?.equippedGunAsset;
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

    private class ShotData : RequestableData
    {
        public required ShotRecord[] Shots;
    }

    private class RequestableData
    {
        public required DateTime Saved;
        public required Report Report;
    }

    private struct ChatRecord
    {
        public required string Message;
        public required int Count;
        public required DateTime Timestamp;
    }

    private struct PendingBullet
    {
        public required BulletInfo Bullet;
        public required DateTime SpawnedTime;
    }

    private class PlayerData
    {
        public required CSteamID PlayerId;
        public required WarfarePlayer OnlinePlayer;
        public DateTime LeftTime;
        public ItemGunAsset? LastGunAsset;
        public Attachments? ThirdAttachments;

        public readonly List<PendingBullet> PendingBullets = new List<PendingBullet>();

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
}