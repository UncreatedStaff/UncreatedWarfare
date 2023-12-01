using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Models.Users;

namespace Uncreated.Warfare.Moderation;

[Table("ip_addresses")]
public class PlayerIPAddress
{
    private uint _packedIP;
    private IPAddress? _ip;

    [Key]
    [JsonPropertyName("id")]
    public uint Id { get; set; }

    [JsonPropertyName("login_count")]
    public int LoginCount { get; set; }

    [Required]
    [Column("Steam64")]
    [JsonPropertyName("steam64")]
    [ForeignKey(nameof(PlayerData))]
    public ulong Steam64 { get; set; }
    public WarfareUserData PlayerData { get; set; }

    [JsonPropertyName("first_login")]
    [DefaultValue(null)]
    public DateTimeOffset? FirstLogin { get; set; }

    [JsonPropertyName("last_login")]
    public DateTimeOffset LastLogin { get; set; }

    [JsonPropertyName("is_remote_play")]
    [NotMapped]
    public bool? RemotePlay { get; set; }

    [JsonPropertyName("packed_ip")]
    [Column("Packed")]
    [Index]
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
    [DontAddPackedColumn]
    [Column("Unpacked")]
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
    public PlayerIPAddress() { }
    public PlayerIPAddress(uint id, ulong steam64, uint packedIp, int loginCount, DateTimeOffset? firstLogin, DateTimeOffset lastLogin)
    {
        Id = id;
        LoginCount = loginCount;
        PackedIP = packedIp;
        Steam64 = steam64;
        FirstLogin = firstLogin;
        LastLogin = lastLogin;
    }
    public override string ToString() => IPAddress?.ToString() ?? "0.0.0.0";
}