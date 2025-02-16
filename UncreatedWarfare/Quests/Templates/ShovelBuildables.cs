using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Quests.Parameters;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Quests.Templates;

public class ShovelBuildables : QuestTemplate<ShovelBuildables, ShovelBuildables.Tracker, ShovelBuildables.State>
{
#nullable disable
    public Int32ParameterTemplate Amount { get; set; }
    public AssetParameterTemplate<ItemPlaceableAsset> Base { get; set; }
    public EnumParameterTemplate<ShovelableType> Buildable { get; set; }
#nullable restore
    public ShovelBuildables(IConfiguration templateConfig, IServiceProvider serviceProvider) : base(templateConfig, serviceProvider) { }
    public class State : IQuestState<ShovelBuildables>
    {
        [JsonIgnore]
        public string Text { get; set; }
#nullable disable
        [RewardVariable("a")]
        public QuestParameterValue<int> Amount { get; set; }
#nullable restore
        public QuestParameterValue<Guid>? Base { get; set; }
        public QuestParameterValue<ShovelableType>? Buildable { get; set; }

        [JsonIgnore]
        public QuestParameterValue<int> FlagValue => Amount;
        public UniTask CreateFromConfigurationAsync(IQuestStateConfiguration configuration, ShovelBuildables template, IServiceProvider serviceProvider, CancellationToken token)
        {
            Amount = configuration.ParseInt32Value("Amount", Int32ParameterTemplate.WildcardInclusive);
            Base = configuration.ParseAssetValue<ItemPlaceableAsset>("Base");
            Buildable = configuration.ParseEnumValue<ShovelableType>("Buildable");
            FormatText(template);
            return UniTask.CompletedTask;
        }
        public async UniTask CreateFromTemplateAsync(ShovelBuildables template, CancellationToken token)
        {
            Amount = await template.Amount.CreateValue(template.ServiceProvider);

            Base = template.Base == null ? null : await template.Base.CreateValue(template.ServiceProvider);
            Buildable = template.Buildable == null ? null : await template.Buildable.CreateValue(template.ServiceProvider);
            FormatText(template);
        }

        private void FormatText(ShovelBuildables template)
        {
            ITranslationValueFormatter formatter = template.ServiceProvider.GetRequiredService<ITranslationValueFormatter>();

            object buildable;
            if (Base == null || Base.IsWildcardInclusive())
            {
                if (Buildable == null || Buildable.IsWildcardInclusive())
                {
                    buildable = string.Empty;
                }
                else
                {
                    buildable = Buildable.GetDisplayString(formatter);
                }
            }
            else
            {
                buildable = Base.GetDisplayString(formatter);
            }

            Text = string.Format(template.Text.Translate(null, template.Type.Name),
                "{0}",
                Amount.GetDisplayString(formatter),
                buildable
            );
        }

        /// <inheritdoc />
        public string CreateQuestDescriptiveString()
        {
            return Text;
        }
    }
    public class Tracker : QuestTracker //, todo INotifyBuildableBuilt //IEventListener<BuildableBuilt>
    {
        private readonly int _targetAmount;
        private readonly QuestParameterValue<Guid> _base;
        private readonly QuestParameterValue<ShovelableType> _buildable;

        private int _amount;
        public override bool IsComplete => _amount >= _targetAmount;
        public override short FlagValue => (short)_amount;
        public Tracker(WarfarePlayer player, IServiceProvider serviceProvider, ShovelBuildables quest, State state, IQuestPreset? preset)
            : base(player, serviceProvider, quest, state, preset)
        {
            _targetAmount = state.Amount.GetSingleValueOrMinimum();

            _base = state.Base ?? AssetParameterTemplate<ItemPlaceableAsset>.WildcardInclusive;
            _buildable = state.Buildable ?? EnumParameterTemplate<ShovelableType>.WildcardInclusive;
        }

        // todo [EventListener(RequiresMainThread = false)] // todo use actual event listener
        // todo void INotifyBuildableBuilt.OnBuildableBuilt(UCPlayer player, BuildableData buildable)
        // todo {
        // todo     if (player.Steam64 != Player.Steam64
        // todo         || !_type.IsMatch(buildable.Type)
        // todo         || !buildable.Foundation.TryGetAsset(out ItemAsset? asset)
        // todo         || !_base.IsMatch<ItemPlaceableAsset>(asset))
        // todo     {
        // todo         return;
        // todo     }
        // todo 
        // todo     Interlocked.Increment(ref _amount);
        // todo     InvokeUpdate();
        // todo }

        public override void WriteProgress(Utf8JsonWriter writer)
        {
            writer.WriteNumber("BuildablesBuilt", _amount);
        }

        public override void ReadProgress(ref Utf8JsonReader reader)
        {
            JsonUtility.ReadTopLevelProperties(ref reader, (ref Utf8JsonReader reader, string property, ref object? _) =>
            {
                if (property.Equals("BuildablesBuilt", StringComparison.Ordinal))
                {
                    _amount = reader.GetInt32();
                }
            });
        }
    }
}