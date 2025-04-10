using DanielWillett.ModularRpcs.Serialization;
using System;
using System.Text.Json.Serialization;

namespace Uncreated.Warfare.Quests.Daily;

[RpcSerializable(SerializationHelper.MinimumStringSize * 2 + 16 + 2, isFixedSize: false)]
public class DailyQuestPreset : IAssetQuestPreset, IRpcSerializable
{
    /// <summary>
    /// Name of the quest template mapping to <see cref="QuestTemplate.Name"/>.
    /// </summary>
    public string? TemplateName { get; set; }

    public Guid Key { get; set; }

    public ushort Flag { get; set; }

    [JsonIgnore]
    public IQuestState State { get; private set; }

    [JsonIgnore]
    public IQuestReward[]? RewardOverrides { get; set; }

    [JsonIgnore]
    public DailyQuestDay Day { get; set; }

    [JsonIgnore]
    public string? ReadDescriptiveText { get; set; }

    public void UpdateState(IQuestState state)
    {
        State = state;
    }

    public Guid Asset => Day.Asset;

    /// <inheritdoc />
    public int GetSize(IRpcSerializer serializer)
    {
        return serializer.GetSize(TemplateName) + serializer.GetSize(State.CreateQuestDescriptiveString()) + 16 + 2;
    }

    /// <inheritdoc />
    public int Write(Span<byte> writeTo, IRpcSerializer serializer)
    {
        int index = 0;
        index += serializer.WriteObject(TemplateName, writeTo);
        index += serializer.WriteObject(State.CreateQuestDescriptiveString(), writeTo[index..]);
        index += serializer.WriteObject(Key, writeTo[index..]);
        index += serializer.WriteObject(Flag, writeTo[index..]);
        return index;
    }

    /// <inheritdoc />
    public int Read(Span<byte> readFrom, IRpcSerializer serializer)
    {
        int index = 0;

        TemplateName = serializer.ReadObject<string>(readFrom, out int bytesRead);
        index += bytesRead;

        ReadDescriptiveText = serializer.ReadObject<string>(readFrom[index..], out bytesRead);
        index += bytesRead;

        Key = serializer.ReadObject<Guid>(readFrom[index..], out bytesRead);
        index += bytesRead;

        Flag = serializer.ReadObject<ushort>(readFrom[index..], out bytesRead);
        index += bytesRead;

        return index;
    }
}