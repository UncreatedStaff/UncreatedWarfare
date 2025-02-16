using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("delete", "d", "remove"), SubCommandOf(typeof(KitCommand))]
internal sealed class KitDeleteCommand : IExecutableCommand
{
    private readonly KitCommandTranslations _translations;
    private readonly IKitDataStore _kitSql;
    private readonly CommandDispatcher _commandDispatcher;

    public required CommandContext Context { get; init; }

    public KitDeleteCommand(IServiceProvider serviceProvider)
    {
        _kitSql = serviceProvider.GetRequiredService<IKitDataStore>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<KitCommandTranslations>>().Value;
        _commandDispatcher = serviceProvider.GetRequiredService<CommandDispatcher>();
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        if (!Context.TryGetRange(0, out string? kitName))
        {
            throw Context.SendHelp();
        }

        Kit? kit = await _kitSql.QueryKitAsync(kitName, KitInclude.Translations, token);
        if (kit == null)
        {
            throw Context.Reply(_translations.KitNotFound, kitName);
        }

        kitName = kit.Id;
        await UniTask.SwitchToMainThread(token);

        Context.Reply(_translations.KitConfirmDelete, kit, kit);

        CommandWaitResult confirmResult = await _commandDispatcher.WaitForCommand(typeof(ConfirmCommand), Context.Caller, token: token);
        if (!confirmResult.IsSuccessfullyExecuted)
            throw Context.Reply(_translations.KitCancelOverride);

        await _kitSql.DeleteKitAsync(kit.Key, token: token).ConfigureAwait(false);

        Context.LogAction(ActionLogType.DeleteKit, kitName);

        Context.Reply(_translations.KitDeleted, kit);
    }
}