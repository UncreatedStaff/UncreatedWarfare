using System;
using System.Net;
using System.Text.Json.Serialization;
using Uncreated.SQL;

namespace Uncreated.Warfare.Moderation;
public class PlayerIPAddress : IListItem
{
    private uint _packedIP;
    private IPAddress? _ip;
    
    [JsonPropertyName("id")]
    public PrimaryKey PrimaryKey { get; set; }

    [JsonPropertyName("login_count")]
    public int LoginCount { get; set; }

    [JsonPropertyName("steam64")]
    public ulong Steam64 { get; set; }

    [JsonPropertyName("first_login")]
    public DateTimeOffset? FirstLogin { get; set; }

    [JsonPropertyName("last_login")]
    public DateTimeOffset LastLogin { get; set; }

    [JsonPropertyName("is_remote_play")]
    public bool? RemotePlay { get; set; }

    [JsonPropertyName("packed_ip")]
    public uint PackedIP
    {
        get => _packedIP;
        set
        {
            if (_packedIP == value)
                return;
            _packedIP = value;
            _ip = value == 0u ? null : OffenseManager.Unpack(value);
            RemotePlay = null;
        }
    }
    
    [JsonPropertyName("ip")]
    public IPAddress? IPAddress
    {
        get => _ip;
        set
        {
            if (Equals(_ip, value))
                return;
            _ip = value;
            _packedIP = value == null ? 0u : OffenseManager.Pack(value);
            RemotePlay = null;
        }
    }
    public override string ToString() => IPAddress?.ToString() ?? "0.0.0.0";
    public PlayerIPAddress() { }
    public PlayerIPAddress(PrimaryKey primaryKey, ulong steam64, uint packedIp, int loginCount, DateTimeOffset? firstLogin, DateTimeOffset lastLogin)
    {
        PrimaryKey = primaryKey;
        LoginCount = loginCount;
        PackedIP = packedIp;
        Steam64 = steam64;
        FirstLogin = firstLogin;
        LastLogin = lastLogin;
    }
}