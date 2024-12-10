using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("lock"), SubCommandOf(typeof(KitCommand))]
internal sealed class KitLockCommand : IExecutableCommand
{
    private readonly IServiceProvider _serviceProvider;
    private readonly KitCommandTranslations _translations;
    private readonly KitManager _kitManager;

    public required CommandContext Context { get; init; }

    public KitLockCommand(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _kitManager = serviceProvider.GetRequiredService<KitManager>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<KitCommandTranslations>>().Value;
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

        if (kit is { NeedsSetup: false, Type: KitType.Loadout })
        {
            kit = await _kitManager.Loadouts.LockLoadout(Context.CallerId, kitName, token).ConfigureAwait(false);
            throw Context.Reply(_translations.KitLocked, kit);
        }

        if (kit.Disabled)
        {
            await UniTask.SwitchToMainThread(token);
            throw Context.Reply(_translations.DoesNotNeedLock, kit);
        }

        // scoped
        await using IKitsDbContext dbContext = _serviceProvider.GetRequiredService<IKitsDbContext>();
        dbContext.ChangeTracker.AutoDetectChangesEnabled = false;

        kit.Disabled = false;
        kit.UpdateLastEdited(Context.CallerId);
        dbContext.Update(kit);
        await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
        throw Context.Reply(_translations.KitLocked, kit);
    }
}