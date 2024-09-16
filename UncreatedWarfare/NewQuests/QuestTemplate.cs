using DanielWillett.ReflectionTools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Uncreated.Warfare.NewQuests.Parameters;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.ValueFormatters;

namespace Uncreated.Warfare.NewQuests;

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
    protected override async UniTask<IQuestPreset?> ReadPreset(IConfiguration configuration, CancellationToken token)
    {
        TemplatePreset preset = configuration.Get<TemplatePreset>();
        TState? state = await ReadState(configuration.GetSection("State"), token);

        if (state == null)
            return null;

        preset.StateIntl = state;
        return preset;
    }

    /// <summary>
    /// Read a state from configuration.
    /// </summary>
    protected virtual async UniTask<TState?> ReadState(IConfiguration configuration, CancellationToken token)
    {
        TState state = new TState();
        await state.CreateFromConfigurationAsync(configuration, ServiceProvider, token);
        return state;
    }

    public abstract class BaseState : IQuestState<TSelf>
    {
        public abstract QuestParameterValue<int> FlagValue { get; }
        public abstract UniTask CreateFromConfigurationAsync(IConfiguration configuration, IServiceProvider serviceProvider, CancellationToken token);
        public abstract UniTask CreateFromTemplateAsync(TSelf data, CancellationToken token);
        public virtual bool IsEligible(UCPlayer player) => true;
    }

    protected internal class TemplatePreset : IQuestPreset
    {
        // this is a field to keep it from being binded to
        public TState StateIntl;

        public Guid Key { get; set; }
        public ulong TeamFilter { get; set; }
        public ushort Flag { get; set; }
        public TState State => StateIntl;
        public IQuestReward[]? RewardOverrides { get; set; }
        IQuestState IQuestPreset.State => StateIntl;
    }
}

/// <summary>
/// Represents data that can be used to generate a quest.
/// </summary>
public abstract class QuestTemplate : ITranslationArgument
{
    private readonly ILogger<QuestTemplate> _logger;
    private readonly IConfiguration _config;

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
    public IReadOnlyList<RewardExpression> Rewards { get; private set; }

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

    public IServiceProvider ServiceProvider { get; }

    protected QuestTemplate(IConfiguration templateConfig, IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        
        _logger = (ILogger<QuestTemplate>)serviceProvider.GetRequiredService(typeof(ILogger<>).MakeGenericType(GetType()));
        _config = templateConfig;
    }

    /// <summary>
    /// Read this quest template from the configuration it was created with.
    /// </summary>
    public async UniTask InitializeAsync(CancellationToken token = default)
    {
        Name = _config["Name"] ?? GetType().Name;

        IConfiguration desc = _config.GetSection("Text");
        Text = desc.GetChildren().Any() ? desc.Get<TranslationList>() : new TranslationList(_config["Text"]);

        _config.GetSection("Properties").Bind(this);
        _config.Bind(this);

        IConfigurationSection singleReward = _config.GetSection("Reward");
        if (singleReward.GetChildren().Any())
        {
            RewardExpression? reward = await ReadReward(singleReward, token);
            if (reward != null)
            {
                RewardExpression[] rewards = [reward];
                Rewards = new ReadOnlyCollection<RewardExpression>(rewards);
            }
            else
            {
                Rewards = new ReadOnlyCollection<RewardExpression>(Array.Empty<RewardExpression>());
            }
        }
        else
        {
            IConfigurationSection rewards = _config.GetSection("Rewards");
            List<RewardExpression> rewardList = new List<RewardExpression>(0);

            foreach (IConfigurationSection section in rewards.GetChildren())
            {
                RewardExpression? reward = await ReadReward(section, token);
                if (reward != null)
                    rewardList.Add(reward);
            }

            Rewards = new ReadOnlyCollection<RewardExpression>(rewardList.ToArray());
        }

        // ReSharper disable VirtualMemberCallInConstructor
        IConfigurationSection singlePreset = _config.GetSection("Preset");
        if (singlePreset.GetChildren().Any())
        {
            IQuestPreset? preset = await ReadPreset(singlePreset, token);
            if (preset != null)
            {
                IQuestPreset[] presets = [preset];
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
                IQuestPreset? preset = await ReadPreset(section, token);
                if (preset != null)
                    presetList.Add(preset);
            }

            Presets = new ReadOnlyCollection<IQuestPreset>(presetList.ToArray());
        }
    }

    /// <summary>
    /// Read a preset from configuration.
    /// </summary>
    protected abstract UniTask<IQuestPreset?> ReadPreset(IConfiguration configuration, CancellationToken token);

    /// <summary>
    /// Read a reward and it's expression from configuration.
    /// </summary>
    protected virtual UniTask<RewardExpression?> ReadReward(IConfiguration configuration, CancellationToken token)
    {
        string typeStr = configuration["Type"];
        Type? type = null;
        if (!string.IsNullOrEmpty(typeStr))
        {
            type = Type.GetType(typeStr) ?? typeof(WarfareModule).Assembly.GetType(typeStr);
        }

        if (type == null)
        {
            _logger.LogError("Unknown reward type \"{0}\" in configuration for quest {1}.", typeStr, Accessor.Formatter.Format(GetType()));
            return UniTask.FromResult<RewardExpression?>(null);
        }

        if (configuration["Expression"] is not { Length: > 0 } expression)
        {
            _logger.LogError("Missing or empty expression in configuration for reward {0} in quest {1}.", Accessor.Formatter.Format(type), Accessor.Formatter.Format(GetType()));
            return UniTask.FromResult<RewardExpression?>(null);
        }

        try
        {
            RewardExpression rewardExpression = new RewardExpression(type, GetType(), expression);

            return UniTask.FromResult<RewardExpression?>(rewardExpression);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to create quest reward {0} in quest {1}.", Accessor.Formatter.Format(type), Accessor.Formatter.Format(GetType()));
            return UniTask.FromResult<RewardExpression?>(null);
        }
    }

    public string Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        // todo
        return ToString();
    }
}
