using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Plugins;

namespace Uncreated.Warfare.Moderation;

internal sealed class UcsGlobalBanConfigurer : IServiceConfigurer
{
    /// <inheritdoc />
    public void ConfigureServices(ContainerBuilder bldr)
    {
        bldr.RegisterType<UcsGlobalBanService>()
            .As<IUcsGlobalBanService>()
            .SingleInstance();
    }
}

public interface IUcsGlobalBanService
{
    /// <summary>
    /// If this player was whitelisted from the UCS system.
    /// </summary>
    Task<bool> IsWhitelisted(CSteamID steam64, CancellationToken token = default);

    /// <summary>
    /// Returns the first global ban match for a steam ID.
    /// </summary>
    Task<UcsGlobalBan?> TryGetBan(CSteamID steam64, CancellationToken token = default);
}

public class UcsGlobalBanService : IUcsGlobalBanService
{
    public UcsGlobalBanService()
    {
        
    }

    /// <inheritdoc />
    public Task<bool> IsWhitelisted(CSteamID steam64, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<UcsGlobalBan?> TryGetBan(CSteamID steam64, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}

public class UcsGlobalBanResponse
{
    public string? Status { get; set; }
    public List<UcsGlobalBan>? Content { get; set; }
}

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