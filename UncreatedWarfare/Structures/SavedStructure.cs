using SDG.Unturned;
using System;
using System.Globalization;
using System.Text.Json.Serialization;
using Uncreated.Framework;
using Uncreated.SQL;
using UnityEngine;

namespace Uncreated.Warfare.Structures;
public sealed class SavedStructure : IListItem, ITranslationArgument
{
    public PrimaryKey PrimaryKey { get; set; } = PrimaryKey.NotAssigned;
    [JsonPropertyName("guid")]
    public Guid ItemGuid;
    [JsonPropertyName("instance_id")]
    public uint InstanceID;
    [JsonPropertyName("position")]
    public Vector3 Position;
    [JsonPropertyName("rotation")]
    public Vector3 Rotation;
    [CommandSettable]
    [JsonPropertyName("owner")]
    public ulong Owner;
    [CommandSettable]
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
    [JsonIgnore]
    internal ItemJarData[]? Items = null;
    [JsonIgnore]
    internal ItemDisplayData? DisplayData = null;
    [JsonIgnore]
    public IBuildable? Buildable { get; internal set; }
    public override string ToString()
    {
        return $"#{PrimaryKey.Key:00000} | {ItemGuid:N} ({Assets.find(ItemGuid)?.FriendlyName ?? "null"}): InstID: {InstanceID}; Pos: {Position:F0}; Rot: {Rotation:F0}; Owner: {Owner}; Group: {Group}.";
    }
    string ITranslationArgument.Translate(LanguageInfo language, string? format, UCPlayer? target, CultureInfo? culture,
        ref TranslationFlags flags)
    {
        return Assets.find(ItemGuid)?.FriendlyName ?? ItemGuid.ToString("N");
    }
}
