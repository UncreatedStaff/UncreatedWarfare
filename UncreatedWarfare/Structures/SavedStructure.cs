using SDG.Unturned;
using System;
using System.Globalization;
using System.Text.Json.Serialization;
using Uncreated.Framework;
using Uncreated.SQL;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Models.Localization;
using UnityEngine;

namespace Uncreated.Warfare.Structures;
public sealed class SavedStructure : IListItem, ITranslationArgument
{
    public PrimaryKey PrimaryKey { get; set; } = PrimaryKey.NotAssigned;
    [JsonPropertyName("guid")]
    public Guid ItemGuid { get; set; }
    [JsonPropertyName("instance_id")]
    public uint InstanceID { get; set; }
    [JsonPropertyName("position")]
    public Vector3 Position { get; set; }
    [JsonPropertyName("rotation")]
    public Vector3 Rotation { get; set; }
    [CommandSettable]
    [JsonPropertyName("owner")]
    public ulong Owner { get; set; }
    [CommandSettable]
    [JsonPropertyName("group")]
    public ulong Group { get; set; }
    [JsonIgnore]
    public byte[] Metadata { get; set; } = Array.Empty<byte>();
    [JsonPropertyName("state")]
    public string StateString
    {
        get => Metadata is null ? string.Empty : Convert.ToBase64String(Metadata);
        set => Metadata = value is null ? Array.Empty<byte>() : Convert.FromBase64String(value);
    }
    [JsonIgnore]
    internal ItemJarData[]? Items { get; set; }
    [JsonIgnore]
    internal ItemDisplayData? DisplayData { get; set; }
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
