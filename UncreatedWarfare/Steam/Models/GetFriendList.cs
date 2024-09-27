using System.Collections.Generic;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Configuration.JsonConverters;

namespace Uncreated.Warfare.Steam.Models;

#nullable disable
public class PlayerFriendsListResponse
{
    [JsonPropertyName("friendslist")]
    public PlayerFriendsList FriendsList { get; set; }
}

public class PlayerFriendsList
{
    [JsonPropertyName("friends")]
    public List<PlayerFriend> Friends { get; set; }
}

public class PlayerFriend
{
    [JsonPropertyName("steamid")]
    [JsonConverter(typeof(UInt64StringConverter))]
    public ulong Steam64 { get; set; }

    [JsonPropertyName("relationship")]
    public string Relationship { get; set; }

    [JsonPropertyName("friend_since")]
    public ulong FriendsSince { get; set; }
}
#nullable restore