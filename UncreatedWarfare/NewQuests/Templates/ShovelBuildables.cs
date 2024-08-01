using Microsoft.Extensions.Configuration;
using System;
using System.Text.Json;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.NewQuests.Parameters;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.NewQuests.Templates;
public class ShovelBuildables : QuestTemplate<ShovelBuildables, ShovelBuildables.Tracker, ShovelBuildables.State>
{
    public Int32ParameterTemplate Amount { get; set; }
    public AssetParameterTemplate<ItemPlaceableAsset> Base { get; set; }
    public EnumParameterTemplate<BuildableType> Type { get; set; }
    public ShovelBuildables(IConfiguration templateConfig, IServiceProvider serviceProvider) : base(templateConfig, serviceProvider) { }
    public class State : BaseState
    {
        [RewardField("a")]
        public QuestParameterValue<int> Amount { get; set; }
        public QuestParameterValue<Guid>? Base { get; set; }
        public QuestParameterValue<BuildableType>? Type { get; set; }
        public override QuestParameterValue<int> FlagValue => Amount;
        public override UniTask CreateFromConfigurationAsync(IConfiguration configuration, IServiceProvider serviceProvider, CancellationToken token)
        {
            string? amountStr = configuration["Amount"],
                    baseStr = configuration["Base"],
                    typeStr = configuration["Type"];

            QuestParameterValue<Guid>? baseAsset = null;
            QuestParameterValue<BuildableType>? type = null;

            if (string.IsNullOrEmpty(amountStr) || !Int32ParameterTemplate.TryParseValue(amountStr, out QuestParameterValue<int>? amount))
                throw new QuestConfigurationException(typeof(ShovelBuildables), "Failed to parse integer parameter for \"Amount\".");
            
            if (!string.IsNullOrEmpty(baseStr) && !AssetParameterTemplate<ItemPlaceableAsset>.TryParseValue(baseStr, out baseAsset))
                throw new QuestConfigurationException(typeof(ShovelBuildables), "Failed to parse ItemPlaceableAsset parameter for \"Base\".");
            
            if (!string.IsNullOrEmpty(typeStr) && !EnumParameterTemplate<BuildableType>.TryParseValue(typeStr, out type))
                throw new QuestConfigurationException(typeof(ShovelBuildables), "Failed to parse BuildableType parameter for \"Type\".");

            Amount = amount;
            Base = baseAsset;
            Type = type;
            return UniTask.CompletedTask;
        }
        public override async UniTask CreateFromTemplateAsync(ShovelBuildables data, CancellationToken token)
        {
            Amount = await data.Amount.CreateValue(data.ServiceProvider);

            Base = data.Base == null ? null : await data.Base.CreateValue(data.ServiceProvider);
            Type = data.Type == null ? null : await data.Type.CreateValue(data.ServiceProvider);
        }
    }
    public class Tracker : QuestTracker, INotifyBuildableBuilt //IEventListener<BuildableBuilt>
    {
        private readonly int _targetAmount;
        private readonly QuestParameterValue<Guid> _base;
        private readonly QuestParameterValue<BuildableType> _type;

        private int _amount;
        public override bool IsComplete => _amount >= _targetAmount;
        public override short FlagValue => (short)_amount;
        public Tracker(UCPlayer player, IServiceProvider serviceProvider, ShovelBuildables quest, State state, IQuestPreset? preset)
            : base(player, serviceProvider, quest, state, preset)
        {
            _targetAmount = state.Amount.GetSingleValueOrMinimum();

            _base = state.Base ?? AssetParameterTemplate<ItemPlaceableAsset>.WildcardInclusive;
            _type = state.Type ?? EnumParameterTemplate<BuildableType>.WildcardInclusive;
        }

        [EventListener(RequiresMainThread = false)] // todo use actual event listener
        void INotifyBuildableBuilt.OnBuildableBuilt(UCPlayer player, BuildableData buildable)
        {
            if (player.Steam64 != Player.Steam64
                || !_type.IsMatch(buildable.Type)
                || !buildable.Foundation.TryGetAsset(out ItemAsset? asset)
                || !_base.IsMatch<ItemPlaceableAsset>(asset))
            {
                return;
            }

            Interlocked.Increment(ref _amount);
            InvokeUpdate();
        }

        protected override void WriteProgress(Utf8JsonWriter writer)
        {
            writer.WriteNumber("buildables_built", _amount);
        }

        protected override void ReadProgress(ref Utf8JsonReader reader)
        {
            JsonUtility.ReadTopLevelProperties(ref reader, (ref Utf8JsonReader reader, string property, ref object? _) =>
            {
                if (property.Equals("buildables_built", StringComparison.Ordinal))
                {
                    _amount = reader.GetInt32();
                }
            });
        }
    }
}