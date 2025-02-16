using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Models.Users;
using Uncreated.Warfare.Networking;

namespace Uncreated.Warfare.Moderation;

[Table(DatabaseInterface.TableIPAddresses), Index(nameof(PackedIP))]
public class PlayerIPAddress
{
    private uint _packedIP;
    private IPAddress? _ip;

    [Key]
    [JsonPropertyName("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column(DatabaseInterface.ColumnIPAddressesPrimaryKey)]
    public uint Id { get; set; }

    [JsonPropertyName("login_count")]
    [Column(DatabaseInterface.ColumnIPAddressesLoginCount)]
    public int LoginCount { get; set; }

    [Required]
    [Column(DatabaseInterface.ColumnIPAddressesSteam64)]
    [JsonPropertyName("steam64")]
    [ForeignKey(nameof(PlayerData))]
    public ulong Steam64 { get; set; }
    public WarfareUserData PlayerData { get; set; }

    [JsonPropertyName("first_login")]
    [DefaultValue(null)]
    [Column(DatabaseInterface.ColumnIPAddressesFirstLogin)]
    public DateTimeOffset? FirstLogin { get; set; }

    [JsonPropertyName("last_login")]
    [Column(DatabaseInterface.ColumnIPAddressesLastLogin)]
    public DateTimeOffset LastLogin { get; set; }

    [JsonPropertyName("is_remote_play")]
    [NotMapped]
    public bool? RemotePlay { get; set; }

    [JsonPropertyName("packed_ip")]
    [Column(DatabaseInterface.ColumnIPAddressesPackedIP)]
    public uint PackedIP
    {
        get => _packedIP;
        set
        {
            if (_packedIP == value)
                return;
            _packedIP = value;
            _ip = value == 0u ? null : IPv4Range.Unpack(value);
            RemotePlay = null;
        }
    }
    
    [JsonPropertyName("ip")]
    [DontAddPackedColumn]
    [Column(DatabaseInterface.ColumnIPAddressesUnpackedIP)]
    public IPAddress? IPAddress
    {
        get => _ip;
        set
        {
            if (Equals(_ip, value))
                return;
            _ip = value;
            _packedIP = value == null ? 0u : IPv4Range.Pack(value);
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