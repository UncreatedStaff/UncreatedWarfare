using SDG.Unturned;
using System;
using System.Text.Json.Serialization;
using Uncreated.Framework;
using Uncreated.SQL;
using UnityEngine;

namespace Uncreated.Warfare.Structures;
public sealed class SavedStructure : IListItem, ITranslationArgument
{
    public PrimaryKey PrimaryKey { get; set; }
    [JsonPropertyName("guid")]
    public Guid ItemGuid;
    [JsonPropertyName("instance_id")]
    public uint InstanceID;
    [JsonPropertyName("position")]
    public Vector3 Position;
    [JsonPropertyName("rotation")]
    public Vector3 Rotation;
    [JsonSettable]
    [JsonPropertyName("owner")]
    public ulong Owner;
    [JsonSettable]
    [JsonPropertyName("group")]
    public ulong Group;
    [JsonIgnore]
    public byte[] Metadata = Array.Empty<byte>();
    [JsonPropertyName("state")]
    public string StateString
    {
        get => Metadata is null ? string.Empty : Convert.ToBase64String(Metadata);
        set => Metadata = value is null ? Array.Empty<byte>() : Convert.FromBase64String(value);
    }

    public override string ToString()
    {
        return $"{ItemGuid:N} ({Assets.find(ItemGuid)?.FriendlyName ?? "null"}): Instance ID: {InstanceID}; Position: {Position:F0}; Rotation: {Rotation:F0}; Owner: {Owner}; Group: {Group}; State: \"{StateString}\".";
    }
    string ITranslationArgument.Translate(string language, string? format, UCPlayer? target, ref TranslationFlags flags)
    {
        return Assets.find(ItemGuid)?.FriendlyName ?? ItemGuid.ToString("N");
    }
}
