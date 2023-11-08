using System;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.API.Permissions;

namespace Uncreated.Warfare.Models.Users;

[Table("users")]
public class WarfareUserData
{
    public ulong Steam64 { get; set; }
    public string PlayerName { get; set; }
    public string CharacterName { get; set; }
    public string NickName { get; set; }
    public DateTime? FirstJoined { get; set; }
    public DateTime? LastJoined { get; set; }
    public PermissionLevel PermissionLevel { get; set; }
}
