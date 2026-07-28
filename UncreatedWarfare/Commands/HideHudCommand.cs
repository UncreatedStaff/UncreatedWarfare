using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("nohud", "hud"), MetadataFile]
internal sealed class HideHudCommand : IExecutableCommand
{
    private readonly HudManager _hudManager;
    private readonly HideHudCommandTranslations _translations;

    public required CommandContext Context { get; init; }

    public HideHudCommand(HudManager hudManager, TranslationInjection<HideHudCommandTranslations> translations)
    {
        _translations = translations.Value;
        _hudManager = hudManager;
    }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        bool isHudVisible = _hudManager.GetHudVisibilityPreference(Context.Player);
        bool isChatBlocked = _hudManager.GetBlockChatPreference(Context.Player);

        if (!isHudVisible || isChatBlocked)
        {
            _hudManager.SetHudVisibilityPreference(Context.Player, true);
            _hudManager.SetBlockChatPreference(Context.Player, false);
            throw Context.Reply(_translations.HudRestored);
        }

        if (isHudVisible)
            _hudManager.SetHudVisibilityPreference(Context.Player, false);

        if (!Context.MatchFlag('s', "silent"))
            Context.Reply(_translations.HudHidden, $"/{Context.CommandInfo.CompositeName}", $"/{Context.CommandInfo.CompositeName} -s");
        else
            Context.Defer();
        
        if (!isChatBlocked)
            _hudManager.SetBlockChatPreference(Context.Player, true);

        return UniTask.CompletedTask;
    }
}

internal sealed class HideHudCommandTranslations : TranslationCollection
{
    public override string Name => "Commands/Hide HUD";


    [TranslationData("Sent to the player when their HUD and chat are restored to default.")]
    public Translation HudRestored = new Translation("<#ffe699>Your HUD and chat have been restored.");


    [TranslationData("Sent to the player when their HUD and chat are cleared.", "Command to restore the HUD.", "Command to clear the HUD without sending this message.")]
    public Translation<string, string> HudHidden = new Translation<string, string>("<#ffe699>Your HUD has been hidden and chat paused. <#fff>{0}</color> to restore. <#fff>{1}</color> to hide this message.");
}