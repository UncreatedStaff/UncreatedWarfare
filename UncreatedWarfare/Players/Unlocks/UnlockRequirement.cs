using System.Globalization;
using System.Text.Json;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Interaction.Requests;
using Uncreated.Warfare.Models.Localization;

namespace Uncreated.Warfare.Players.Unlocks;

/// <summary>
/// Generic read-only requirement to unlock something.
/// </summary>
public abstract class UnlockRequirement : ICloneable
{
    public uint PrimaryKey { get; set; }

    //private static readonly Dictionary<Type, string[]> LegacyInfo = new Dictionary<Type, string[]>()
    //{
    //    { typeof(LevelUnlockRequirement), [ "unlock_level"               ] },
    //    { typeof(RankUnlockRequirement),  [ "unlock_rank"                ] },
    //    { typeof(QuestUnlockRequirement), [ "unlock_presets", "quest_id" ] }
    //};

    /// <summary>
    /// Initialize required services.
    /// </summary>
    public virtual void Initialize(IServiceProvider serviceProvider) { }

    /// <summary>
    /// If a player passes the requirements. This check can do some caching, mainly for signs.
    /// </summary>
    public abstract bool CanAccessFast(WarfarePlayer player);

    /// <summary>
    /// Full check if a player passes the requirements.
    /// </summary>
    public virtual async UniTask<bool> CanAccessAsync(WarfarePlayer player, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        return CanAccessFast(player);
    }

    protected virtual void ReadLegacyProperty(ILogger? logger, ref Utf8JsonReader reader, string property) { }

    /// <summary>
    /// Get the text that shows on a sign when the player is missing the requirement.
    /// </summary>
    public abstract string GetSignText(WarfarePlayer? player, LanguageInfo language, CultureInfo culture);

    /// <inheritdoc />
    public abstract object Clone();

    public virtual Exception RequestFailureToMeet(CommandContext ctx, IRequestable<object> requestable)
    {
        WarfareModule.Singleton.GlobalLogger.LogWarning("Unhandled requestable requirement type: " + GetType().Name);
        return ctx.SendUnknownError();
    }
}