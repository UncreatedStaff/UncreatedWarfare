using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Plugins;
using Uncreated.Warfare.Util;
using UnityEngine.Networking;

namespace Uncreated.Warfare.Moderation.GlobalBans;

[UsedImplicitly]
internal sealed class UcsGlobalBanConfigurer : IServiceConfigurer
{
    public void ConfigureServices(ContainerBuilder bldr)
    {
        bldr.RegisterType<UcsGlobalBanService>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();
    }
}

public class UcsGlobalBanService : IGlobalBanService, IEventListener<PlayerDied>
{
    private readonly IConfiguration _systemConfig;
    private readonly ILogger<UcsGlobalBanService> _logger;
    private readonly DatabaseInterface _moderationSql;
    private readonly IPlayerService _playerService;

    public UcsGlobalBanService(IConfiguration systemConfig, ILogger<UcsGlobalBanService> logger, DatabaseInterface moderationSql, IPlayerService playerService)
    {
        _systemConfig = systemConfig;
        _logger = logger;
        _moderationSql = moderationSql;
        _playerService = playerService;
    }

    private const string QueryUrl = "/globalbans" +
                                    "?version=2" +
                                    "&steamid={0}" +
                                    "&ip={1}" +
                                    "&hwid={2}";

    private const string QueryUrlNoIP = "/globalbans" +
                                        "?version=2" +
                                        "&steamid={0}" +
                                        "&hwid={1}";

    private static readonly DateTime AutomatedBanIgnoreThreshold = new DateTime(2024, 12, 29, 19, 55, 59, DateTimeKind.Utc);

    /// <inheritdoc />
    public async Task<GlobalBan> GetGlobalBanAsync(CSteamID steam64, IPv4Range ipAddress, HWID[] hwids, CancellationToken token = default)
    {
        string? baseUrl = _systemConfig["ucs:base_url"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogWarning("UCS global bans missing base URL.");
            return default;
        }

        string[] hwidStr = new string[hwids.Length];
        for (int i = 0; i < hwidStr.Length; ++i)
            hwidStr[i] = hwids[i].ToBase64String();

        string relativeUrl;
        if (IPv4Range.IsLocalIP(ipAddress))
        {
            relativeUrl = string.Format(QueryUrlNoIP,
                steam64.m_SteamID.ToString("D17", CultureInfo.InvariantCulture),
                string.Join(';', hwidStr));
        }
        else
        {
            relativeUrl = string.Format(QueryUrl,
                steam64.m_SteamID.ToString("D17", CultureInfo.InvariantCulture),
                ipAddress.ToIPv4String(),
                string.Join(';', hwidStr));
        }

        Uri uri = new Uri(new Uri(baseUrl), relativeUrl);

        using UnityWebRequest req = UnityWebRequest.Get(uri);

        string? apiKey = _systemConfig["ucs:api_key"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            req.SetRequestHeader("Authorization", apiKey);
        }

        req.timeout = 5;

        UcsGlobalBan? ban = null;
        try
        {
            _logger.LogConditional("Sending UCS request {0}.", uri);
            await req.SendWebRequest();

            string text = req.downloadHandler.text;

            UcsGlobalBansResponse? response = (UcsGlobalBansResponse?)JsonSerializer.Deserialize(text, typeof(UcsGlobalBansResponse), UcsGlobalBanSourceGenerationContext.Default);
            ban = response?.Content?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            if (req.responseCode != 404)
            {
                _logger.LogWarning(ex, "Failed to send UCS ban request: \"{0}\".", uri);
            }
        }

        if (ban == null)
            return default;

        // don't auto-kick for auto bans since Infected kinda spammed their entire ban list
        if (ban.TimeBanned <= AutomatedBanIgnoreThreshold
            && ban.BanReason?.EndsWith("Take with grain of salt. Automated", StringComparison.Ordinal) is true)
        {
            _moderationSql.SendSuspectedCheaterMessage(steam64, ban.Id);
            return default;
        }

        ulong bannedPlayerSteam64 = 0;

        // get the first player from the steam list if it doesnt contain the checking player
        if (!string.IsNullOrEmpty(ban.SteamIds))
        {
            if (ban.SteamIds.Contains(steam64.m_SteamID.ToString("D17", CultureInfo.InvariantCulture), StringComparison.Ordinal))
            {
                bannedPlayerSteam64 = steam64.m_SteamID;
            }
            else
            {
                int firstComma = ban.SteamIds.IndexOf(',');
                ReadOnlySpan<char> firstSteamId = firstComma == -1 ? ban.SteamIds : ban.SteamIds.AsSpan().Slice(0, firstComma);

                ulong.TryParse(firstSteamId, NumberStyles.Number, CultureInfo.InvariantCulture, out bannedPlayerSteam64);
                if (new CSteamID(bannedPlayerSteam64).GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
                    bannedPlayerSteam64 = 0;
            }
        }

        string? bannedPlayerName = null;
        if (bannedPlayerSteam64 != steam64.m_SteamID && !string.IsNullOrEmpty(ban.KnownNames))
        {
            int firstComma = ban.KnownNames.IndexOf(',');
            bannedPlayerName = firstComma == -1 ? ban.KnownNames : new string(ban.KnownNames.AsSpan().Slice(0, firstComma).Trim());
        }
        
        return new GlobalBan(DateTime.SpecifyKind(ban.TimeBanned, DateTimeKind.Utc), ban.BanReason, "UCS", ban.Id, bannedPlayerSteam64, bannedPlayerName);
    }

    private const string PostUrl = "/globalbans?version=2";

    public async UniTask<UcsGlobalBan?> PostBanAsync(
        CSteamID steam64,
        IPv4Range ipAddress,
        HWID[] hwids,
        PlayerNames names,
        string reason,
        string moderator,
        DateTimeOffset timestamp,
        CancellationToken token = default
    )
    {
        token.ThrowIfCancellationRequested();

        string? baseUrl = _systemConfig["ucs:base_url"];
        string? apiKey = _systemConfig["ucs:api_key"];
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("UCS global bans missing base URL.");
            return null;
        }

        UcsPostedBan postedBan = new UcsPostedBan
        {
            SteamId = steam64.m_SteamID,
            BanReason = reason
        };

        string[] uniqueNames = names.GetUniqueNames();
        if (uniqueNames.Length == 1)
            postedBan.KnownName = uniqueNames[0];
        else if (uniqueNames.Length != 0)
            postedBan.KnownNames = uniqueNames;

        if (hwids.Length == 1)
            postedBan.HWID = hwids[0].ToBase64String();
        else if (hwids.Length != 0)
        {
            string[] hwidStrs = new string[hwids.Length];
            for (int i = 0; i < hwids.Length; ++i)
                hwidStrs[i] = hwids[i].ToBase64String();
            postedBan.HWIDs = hwidStrs;
        }

        uint pack = ipAddress.PackedIP;
        if (pack != 0 && !IPv4Range.IsLocalIP(pack) && !_moderationSql.IsRemotePlay(pack))
            postedBan.IP = ipAddress.ToIPv4String();

        postedBan.Timestamp = timestamp.UtcDateTime;
        postedBan.Moderator = moderator;

        byte[] content = JsonSerializer.SerializeToUtf8Bytes(postedBan, ConfigurationSettings.JsonCondensedSerializerSettings);

#if DEBUG
        _logger.LogInformation("Posting to UCS ban API: {0}", JsonSerializer.Serialize(postedBan, ConfigurationSettings.JsonSerializerSettings));
#endif

        Uri uri = new Uri(new Uri(baseUrl), PostUrl);

        using UnityWebRequest req = new UnityWebRequest(uri, "POST", new DownloadHandlerBuffer(), new UploadHandlerRaw(content)
        {
            contentType = "application/json; charset=utf-8"
        });

        req.SetRequestHeader("Authorization", apiKey);

        try
        {
            await req.SendWebRequest();
            
            if (req.responseCode != 200L)
            {
                _logger.LogError("Failed to UCS ban {0}, response code {1} - {2}.", steam64, req.responseCode, req.error);
            }
            else
            {
                string text = req.downloadHandler.text;

                UcsGlobalBanResponse? response = (UcsGlobalBanResponse?)JsonSerializer.Deserialize(text, typeof(UcsGlobalBanResponse), UcsGlobalBanSourceGenerationContext.Default);
                _logger.LogInformation("Posted UCS ban ID {0} for {1}.", response?.Content?.Id ?? 0, steam64);
                return response?.Content;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to UCS ban {0}.", steam64);
        }

        return null;
    }

    /// <inheritdoc />
    [EventListener(MustRunInstantly = true)]
    void IEventListener<PlayerDied>.HandleEvent(PlayerDied e, IServiceProvider serviceProvider)
    {
        if (!e.WillBan)
            return;

        CSteamID steam64 = e.Instigator;

        WarfarePlayer? onlinePlayer = _playerService.GetOnlinePlayerOrNull(steam64);

        IPv4Range ip = default;
        bool remotePlay = false;
        if (onlinePlayer != null
            && onlinePlayer.UnturnedPlayer.channel.owner.transportConnection.TryGetIPv4Address(out uint ipPacked)
            && !IPv4Range.IsLocalIP(ipPacked))
        {
            if (_moderationSql.IsRemotePlay(ipPacked))
                remotePlay = true;
            else
                ip = new IPv4Range(ipPacked);
        }

        HWID[] hwids;
        if (!remotePlay && onlinePlayer != null)
        {
            byte[][] hwidsVanilla = onlinePlayer.SteamPlayer.playerID.GetHwids().ToArrayFast();
            hwids = new HWID[hwidsVanilla.Length];

            for (int i = 0; i < hwidsVanilla.Length; ++i)
                hwids[i] = new HWID(hwidsVanilla[i]);
        }
        else
        {
            hwids = Array.Empty<HWID>();
        }

        PlayerNames names = onlinePlayer?.Names ?? default;

        UniTask.Create(async () =>
        {
            try
            {
                await PostBanAsync(steam64, ip, hwids, names, "Guaranteed cheater (ask @danielwillett for more info).", "AUTOMATED", DateTimeOffset.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to UCS ban {0}.", steam64);
            }
        });
    }
}

public class UcsGlobalBansResponse
{
    public string? Status { get; set; }
    public List<UcsGlobalBan>? Content { get; set; }
}

public class UcsGlobalBanResponse
{
    public string? Status { get; set; }
    public UcsGlobalBan? Content { get; set; }
}

#nullable disable
public class UcsPostedBan
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Id { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public ulong SteamId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string HWID { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[] HWIDs { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string IP { get; set; }

    // ReSharper disable once InconsistentNaming
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[] IPs { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[] KnownNames { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string KnownName { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string BanReason { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Moderator { get; set; }
    public string Server => "Warfare";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? Timestamp { get; set; }
}
#nullable restore

public class UcsGlobalBan
{
    [JsonPropertyName("Key")]
    public uint Id { get; set; }
    public string? SteamIds { get; set; }
    public string? BanReason { get; set; }
    public string? ServersBannedOn { get; set; }
    public string? KnownNames { get; set; }
    public string? Moderators { get; set; }
    public DateTime TimeBanned
    {
        get;
        set => field = DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
#if DEBUG
[JsonSourceGenerationOptions(WriteIndented = true, GenerationMode = JsonSourceGenerationMode.Metadata)]
#else
[JsonSourceGenerationOptions(WriteIndented = false)]
#endif
[JsonSerializable(typeof(UcsGlobalBansResponse))]
[JsonSerializable(typeof(UcsGlobalBanResponse))]
internal partial class UcsGlobalBanSourceGenerationContext : JsonSerializerContext;