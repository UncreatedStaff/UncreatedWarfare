using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("unlock", "unl", "ul"), SubCommandOf(typeof(KitCommand))]
internal class KitUnlockCommand : IExecutableCommand
{
    private readonly IServiceProvider _serviceProvider;
    private readonly KitCommandTranslations _translations;
    private readonly KitManager _kitManager;
    public CommandContext Context { get; set; }

    public KitUnlockCommand(IServiceProvider serviceProvider)
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

        if (kit.NeedsSetup)
        {
            kit = await _kitManager.Loadouts.UnlockLoadout(Context.CallerId, kitName, token).ConfigureAwait(false);
            throw Context.Reply(_translations.KitUnlocked, kit);
        }

        if (!kit.Disabled)
        {
            throw Context.Reply(_translations.DoesNotNeedUnlock, kit);
        }

        // scoped
        await using IKitsDbContext dbContext = _serviceProvider.GetRequiredService<IKitsDbContext>();
        dbContext.ChangeTracker.AutoDetectChangesEnabled = false;

        kit.Disabled = false;
        kit.UpdateLastEdited(Context.CallerId);
        dbContext.Update(kit);
        await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
        throw Context.Reply(_translations.KitUnlocked, kit);
    }
}