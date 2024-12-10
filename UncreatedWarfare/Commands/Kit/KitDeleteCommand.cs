using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("delete", "d", "remove"), SubCommandOf(typeof(KitCommand))]
internal sealed class KitDeleteCommand : IExecutableCommand
{
    private readonly KitCommandTranslations _translations;
    private readonly KitManager _kitManager;
    private readonly CommandDispatcher _commandDispatcher;
    private readonly IKitsDbContext _dbContext;

    public required CommandContext Context { get; init; }

    public KitDeleteCommand(IServiceProvider serviceProvider)
    {
        _kitManager = serviceProvider.GetRequiredService<KitManager>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<KitCommandTranslations>>().Value;
        _commandDispatcher = serviceProvider.GetRequiredService<CommandDispatcher>();
        _dbContext = serviceProvider.GetRequiredService<IKitsDbContext>();

        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        if (!Context.TryGetRange(0, out string? kitName))
        {
            throw Context.SendHelp();
        }

        Kit? kit = await _kitManager.FindKit(kitName, token, true);
        if (kit == null)
        {
            throw Context.Reply(_translations.KitNotFound, kitName);
        }

        kitName = kit.InternalName;
        await UniTask.SwitchToMainThread(token);

        Context.Reply(_translations.KitConfirmDelete, kit, kit);

        CommandWaitResult confirmResult = await _commandDispatcher.WaitForCommand(typeof(ConfirmCommand), Context.Caller, token: token);
        if (!confirmResult.IsSuccessfullyExecuted)
        {
            if (confirmResult.IsDisconnected)
                return;

            throw Context.Reply(_translations.KitCancelOverride);
        }

        kit.UpdateLastEdited(Context.CallerId);

        uint pk = kit.PrimaryKey;

        _dbContext.Remove(kit);
        await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);

        _kitManager.Cache.RemoveKit(pk, kitName);

        Context.LogAction(ActionLogType.DeleteKit, kitName);

        Context.Reply(_translations.KitDeleted, kit);

        _kitManager.Signs.UpdateSigns(kitName);
    }
}