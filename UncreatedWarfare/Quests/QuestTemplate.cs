using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Translations.ValueFormatters;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Quests;

/// <summary>
/// Defines the tracker used to keep track of quest progress and the state used to store a variation of this template.
/// </summary>
public abstract class QuestTemplate<TSelf, TTracker, TState> : QuestTemplate
    where TSelf : QuestTemplate<TSelf, TTracker, TState>
    where TTracker : QuestTracker
    where TState : class, IQuestState<TSelf>, new()
{
    protected QuestTemplate(IConfiguration templateConfig, IServiceProvider serviceProvider) : base(templateConfig, serviceProvider) { }

    /// <inheritdoc />
    public override async UniTask<IQuestPreset?> ReadPreset<TPreset>(IConfiguration configuration, CancellationToken token)
    {
        TPreset? preset = configuration.Get<TPreset>();
        TState? state = await ReadState(new QuestIConfigurationStateConfiguration(configuration.GetSection("State"), Type), token);

        if (preset == null || state == null)
            return null;

        preset.UpdateState(state);
        return preset;
    }

    /// <inheritdoc />
    public override async UniTask ReadStateToPreset(IQuestPreset preset, IQuestStateConfiguration configuration, CancellationToken token)
    {
        preset.UpdateState(await ReadState(configuration, token) ?? new TState());
    }

    /// <summary>
    /// Read a state from configuration.
    /// </summary>
    public virtual async UniTask<TState?> ReadState(IQuestStateConfiguration configuration, CancellationToken token)
    {
        TState state = new TState();
        await state.CreateFromConfigurationAsync(configuration, (TSelf)this, ServiceProvider, token);
        return state;
    }

    /// <inheritdoc />
    public override async UniTask<IQuestState> CreateState(CancellationToken token = default)
    {
        TState state = new TState();
        await state.CreateFromTemplateAsync((TSelf)this, token);
        return state;
    }

    /// <inheritdoc />
    public override QuestTracker CreateTracker(IQuestPreset preset, WarfarePlayer player)
    {
        object[] args = [ player, ServiceProvider, this, preset.State, preset ];

        QuestTracker tracker = (QuestTracker)Activator.CreateInstance(typeof(TTracker), args);

        QuestService.AddTracker(tracker);
        return tracker;
    }
}

/// <summary>
/// Represents data that can be used to generate a quest.
/// </summary>
public abstract class QuestTemplate : ITranslationArgument
{
    private readonly ILogger<QuestTemplate> _logger;
    private IConfiguration _config;

    protected QuestService QuestService;

#nullable disable

    /// <summary>
    /// The display name of this quest.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Formatted text in the quest text. {0} should always be the progressed variable.
    /// </summary>
    public TranslationList Text { get; private set; }

    /// <summary>
    /// Rewards for completing the quest.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<QuestRewardExpression> Rewards { get; private set; }

    /// <summary>
    /// Pre-defined presets for quest states.
    /// </summary>
    public IReadOnlyList<IQuestPreset> Presets { get; private set; }

    /// <summary>
    /// If this quest type can be a daily quest.
    /// </summary>
    public bool CanBeDailyQuest { get; set; }

    /// <summary>
    /// If this quest should be resetted when the game ends.
    /// </summary>
    public bool ResetOnGameEnd { get; set; }

    /// <summary>
    /// If this quest should send updates in chat.
    /// </summary>
    public bool SendChatUpdates { get; set; } = true;

#nullable restore

    [JsonIgnore]
    public IServiceProvider ServiceProvider { get; }

    [JsonIgnore]
    public Type Type { get; }
    protected QuestTemplate(IConfiguration templateConfig, IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        QuestService = serviceProvider.GetRequiredService<QuestService>();
        Type = GetType();
        _logger = (ILogger<QuestTemplate>)serviceProvider.GetRequiredService(typeof(ILogger<>).MakeGenericType(GetType()));
        _config = templateConfig;
    }

    /// <summary>
    /// Read this quest template from the configuration after it was created originally.
    /// </summary>
    public UniTask UpdateAsync(IConfiguration newConfiguration, CancellationToken token = default)
    {
        _config = newConfiguration;
        return InitializeAsync(token);
    }

    /// <summary>
    /// Read this quest template from the configuration it was created with.
    /// </summary>
    public async UniTask InitializeAsync(CancellationToken token = default)
    {
        Name = _config["Name"] ?? Type.Name;

        IConfiguration desc = _config.GetSection("Text");
        Text = desc.GetChildren().Any() ? (desc.Get<TranslationList>() ?? new TranslationList()) : new TranslationList(_config["Text"] ?? Type.Name);

        _config.GetSection("Properties").Bind(this);
        _config.Bind(this);

        IConfigurationSection singleReward = _config.GetSection("Reward");
        if (singleReward.GetChildren().Any())
        {
            QuestRewardExpression? reward = await ReadReward(singleReward, token);
            if (reward != null)
            {
                QuestRewardExpression[] rewards = [ reward ];
                Rewards = new ReadOnlyCollection<QuestRewardExpression>(rewards);
            }
            else
            {
                Rewards = new ReadOnlyCollection<QuestRewardExpression>(Array.Empty<QuestRewardExpression>());
            }
        }
        else
        {
            IConfigurationSection rewards = _config.GetSection("Rewards");
            List<QuestRewardExpression> rewardList = new List<QuestRewardExpression>(0);

            foreach (IConfigurationSection section in rewards.GetChildren())
            {
                QuestRewardExpression? reward = await ReadReward(section, token);
                if (reward != null)
                    rewardList.Add(reward);
            }

            Rewards = new ReadOnlyCollection<QuestRewardExpression>(rewardList.ToArray());
        }

        IConfigurationSection singlePreset = _config.GetSection("Preset");
        if (singlePreset.GetChildren().Any())
        {
            IQuestPreset? preset = await ReadPreset<TemplatePreset>(singlePreset, token);
            if (preset != null)
            {
                IQuestPreset[] presets = [ preset ];
                Presets = new ReadOnlyCollection<IQuestPreset>(presets);
            }
            else
            {
                Presets = new ReadOnlyCollection<IQuestPreset>(Array.Empty<IQuestPreset>());
            }
        }
        else
        {
            IConfigurationSection presets = _config.GetSection("Presets");
            List<IQuestPreset> presetList = new List<IQuestPreset>(0);

            foreach (IConfigurationSection section in presets.GetChildren())
            {
                IQuestPreset? preset = await ReadPreset<TemplatePreset>(section, token);
                if (preset != null)
                    presetList.Add(preset);
            }

            Presets = new ReadOnlyCollection<IQuestPreset>(presetList.ToArray());
        }
    }

    /// <summary>
    /// Read a preset from configuration.
    /// </summary>
    public abstract UniTask<IQuestPreset?> ReadPreset<TPreset>(IConfiguration configuration, CancellationToken token) where TPreset : IQuestPreset;

    /// <summary>
    /// Read a state to an existing preset.
    /// </summary>
    public abstract UniTask ReadStateToPreset(IQuestPreset preset, IQuestStateConfiguration configuration, CancellationToken token);

    /// <summary>
    /// Create a random state.
    /// </summary>
    public abstract UniTask<IQuestState> CreateState(CancellationToken token = default);

    /// <summary>
    /// Create a quest tracker for this template from a pre-existing preset.
    /// </summary>
    public abstract QuestTracker CreateTracker(IQuestPreset preset, WarfarePlayer player);

    /// <summary>
    /// Read a reward and it's expression from configuration.
    /// </summary>
    protected virtual UniTask<QuestRewardExpression?> ReadReward(IConfiguration configuration, CancellationToken token)
    {
        string? typeStr = configuration["Type"];
        Type? type = null;
        if (!string.IsNullOrEmpty(typeStr))
        {
            type = ContextualTypeResolver.ResolveType(typeStr, typeof(IQuestReward));
        }

        if (type == null)
        {
            _logger.LogError("Unknown reward type \"{0}\" in configuration for quest {1}.", typeStr, Type);
            return UniTask.FromResult<QuestRewardExpression?>(null);
        }

        if (configuration["Expression"] is not { Length: > 0 } expression)
        {
            _logger.LogError("Missing or empty expression in configuration for reward {0} in quest {1}.", type, Type);
            return UniTask.FromResult<QuestRewardExpression?>(null);
        }

        try
        {
            QuestRewardExpression rewardExpression = new QuestRewardExpression(type, Type, expression, _logger);

            return UniTask.FromResult<QuestRewardExpression?>(rewardExpression);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to create quest reward {0} in quest {1}.", type, Type);
            return UniTask.FromResult<QuestRewardExpression?>(null);
        }
    }

    public static readonly SpecialFormat FormatType = new SpecialFormat("Quest Type", "t");

    /// <summary>For <see cref="QuestAsset"/> formatting.</summary>
    public static readonly SpecialFormat FormatColorQuestAsset = new SpecialFormat("Quest Name", "c");

    public virtual string Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        // todo
        return ToString();
    }

    protected internal class TemplatePreset : IQuestPreset
    {
        public Guid Key { get; set; }
        public ushort Flag { get; set; }
        public IQuestState State { get; private set; }
        public IQuestReward[]? RewardOverrides { get; set; }

        public void UpdateState(IQuestState state) => State = state;
    }
}
