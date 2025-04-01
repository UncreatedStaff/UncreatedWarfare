using System;
using System.Linq;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Kits;

public class KitCommandLookResolver
{
    private readonly SignInstancer _signInstancer;
    private readonly KitCommandTranslations _translations;
    private readonly IKitDataStore _kitDataStore;

    public KitCommandLookResolver(TranslationInjection<KitCommandTranslations> translations, SignInstancer signInstancer, IKitDataStore kitDataStore)
    {
        _signInstancer = signInstancer;
        _kitDataStore = kitDataStore;
        _translations = translations.Value;
    }

    public async Task<KitCommandLookResult> ResolveFromArgumentsOrLook(CommandContext ctx, int startArgument, int requiredExtraArguments, KitInclude include, CancellationToken token = default)
    {
        int totalArgs = ctx.Parameters.Count - requiredExtraArguments - startArgument;
        if (totalArgs < 0)
        {
            throw ctx.SendHelp();
        }

        BarricadeDrop? barricade;
        Kit? kit = null;

        //  kit give
        if (totalArgs == 0)
        {
            ctx.AssertRanByPlayer();

            if (!ctx.TryGetBarricadeTarget(out barricade) || barricade.interactable is not InteractableSign)
                throw ctx.Reply(_translations.KitOperationNoTarget);
            
            kit = await GetSignTarget(ctx.Player, barricade, include, token).ConfigureAwait(false);
            if (kit == null)
                throw ctx.Reply(_translations.KitOperationNoTarget);

            return new KitCommandLookResult(kit, startArgument + requiredExtraArguments, startArgument, true);
        }

        bool isSign = true;
        int argIndex = startArgument;
        if (ctx.TryGetBarricadeTarget(out barricade) && barricade.interactable is InteractableSign)
        {
            //  kit give [other... ]
            kit = await GetSignTarget(ctx.Player, barricade, include, token).ConfigureAwait(false);
        }

        if (kit == null && ctx.TryGet(startArgument, out string? kitId))
        {
            //  kit give kit-id [other... ]
            ++argIndex;
            kit = await _kitDataStore.QueryKitAsync(kitId, include, token).ConfigureAwait(false);
            if (kit == null)
                throw ctx.Reply(_translations.KitNotFound, kitId);

            isSign = false;
        }

        if (kit == null)
            throw ctx.Reply(_translations.KitOperationNoTarget);

        if (argIndex + requiredExtraArguments > ctx.Parameters.Count)
            throw ctx.SendHelp();

        // kit give [usrif1] <test> [player]
        return new KitCommandLookResult(kit, argIndex + requiredExtraArguments, argIndex, isSign);
    }

    private async Task<Kit?> GetSignTarget(WarfarePlayer player, BarricadeDrop sign, KitInclude include, CancellationToken token)
    {
        await UniTask.SwitchToMainThread();

        if (_signInstancer.GetSignProvider(sign) is not KitSignInstanceProvider kitSign)
            return null;

        if (kitSign.LoadoutNumber >= 0)
        {
            Kit? kit = player.Component<KitPlayerComponent>().Loadouts.ElementAtOrDefault(kitSign.LoadoutNumber - 1);

            // include is at most Cached
            if (kit == null || (include | KitInclude.Cached) == KitInclude.Cached)
            {
                return kit;
            }

            return await _kitDataStore.QueryKitAsync(kit.Key, include, token).ConfigureAwait(false);
        }

        return await _kitDataStore.QueryKitAsync(kitSign.KitId, include, token).ConfigureAwait(false);
    }
}

public readonly struct KitCommandLookResult
{
    public readonly Kit Kit;
    public readonly int OptionalArgumentStart;
    public readonly int RequiredArgumentStart;
    public readonly bool IsSign;

    public KitCommandLookResult(Kit kit, int optionalArgumentStart, int requiredArgumentStart, bool isSign)
    {
        Kit = kit;
        OptionalArgumentStart = optionalArgumentStart;
        RequiredArgumentStart = requiredArgumentStart;
        IsSign = isSign;
    }
}