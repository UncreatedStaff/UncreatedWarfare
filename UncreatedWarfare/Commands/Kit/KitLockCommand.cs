using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Loadouts;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("lock"), SubCommandOf(typeof(KitCommand))]
internal sealed class KitLockCommand : IExecutableCommand
{
    private readonly KitCommandTranslations _translations;
    private readonly KitCommandLookResolver _lookResolver;
    private readonly IKitDataStore _kitDataStore;
    private readonly LoadoutService _loadoutService;

    public required CommandContext Context { get; init; }

    public KitLockCommand(IServiceProvider serviceProvider)
    {
        _loadoutService = serviceProvider.GetRequiredService<LoadoutService>();
        _kitDataStore = serviceProvider.GetRequiredService<IKitDataStore>();
        _lookResolver = serviceProvider.GetRequiredService<KitCommandLookResolver>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<KitCommandTranslations>>().Value;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        KitCommandLookResult lookResult = await _lookResolver.ResolveFromArgumentsOrLook(Context, 0, 0, KitInclude.Default, token).ConfigureAwait(false);

        Kit? kit = lookResult.Kit;

        if (kit.IsLocked)
        {
            throw Context.Reply(_translations.DoesNotNeedLock, kit);
        }

        if (kit.Type == KitType.Loadout)
        {
            kit = await _loadoutService.LockLoadoutAsync(Context.CallerId, kit.Key, token: token).ConfigureAwait(false);
            if (kit == null)
                throw Context.SendUnknownError();

            throw Context.Reply(_translations.KitLocked, kit);
        }

        await _kitDataStore.UpdateKitAsync(kit.Key, KitInclude.Base, kit =>
        {
            kit.Disabled = true;
        }, Context.CallerId, token).ConfigureAwait(false);

        Context.LogAction(ActionLogType.SetKitProperty, kit.Id + ": DISABLED >> TRUE");

        throw Context.Reply(_translations.KitLocked, kit);
    }
}