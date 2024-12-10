using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

[Command("create", "c", "override"), SubCommandOf(typeof(KitCommand))]
internal sealed class KitCreateCommand : IExecutableCommand
{
    private readonly KitCommandTranslations _translations;
    private readonly KitManager _kitManager;
    private readonly IFactionDataStore _factionStorage;
    private readonly CommandDispatcher _commandDispatcher;
    private readonly IKitsDbContext _dbContext;
    private readonly AssetRedirectService _assetRedirectService;

    public required CommandContext Context { get; init; }

    public KitCreateCommand(IServiceProvider serviceProvider)
    {
        _kitManager = serviceProvider.GetRequiredService<KitManager>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<KitCommandTranslations>>().Value;
        _factionStorage = serviceProvider.GetRequiredService<IFactionDataStore>();
        _commandDispatcher = serviceProvider.GetRequiredService<CommandDispatcher>();
        _dbContext = serviceProvider.GetRequiredService<IKitsDbContext>();
        _assetRedirectService = serviceProvider.GetRequiredService<AssetRedirectService>();

        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (!Context.TryGet(0, out string? kitId))
        {
            throw Context.SendHelp();
        }

        Kit? existingKit = await _kitManager.FindKit(kitId, token, set: dbContext => KitManager.RequestableSet(dbContext, false));
        if (existingKit != null)
        {
            // overwrite kit
            await UniTask.SwitchToMainThread(token);
            Context.Reply(_translations.KitConfirmOverride, existingKit, existingKit);

            // wait for /confirm
            CommandWaitResult confirmResult = await _commandDispatcher.WaitForCommand(typeof(ConfirmCommand), Context.Caller, token: token);
            if (!confirmResult.IsSuccessfullyExecuted)
            {
                if (confirmResult.IsDisconnected)
                    return;

                throw Context.Reply(_translations.KitCancelOverride);
            }

            IKitItem[] oldItems = existingKit.Items;
            IKitItem[] items = ItemUtility.ItemsFromInventory(Context.Player, assetRedirectService: _assetRedirectService);
            existingKit.SetItemArray(items, _dbContext);
            existingKit.WeaponText = _kitManager.GetWeaponText(existingKit);
            existingKit.UpdateLastEdited(Context.CallerId);
            Context.LogAction(ActionLogType.EditKit, "OVERRIDE ITEMS " + existingKit.InternalName + ".");
            _dbContext.Update(existingKit);
            await _dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

            ILogger logger = Context.Logger;
            _ = Task.Run(async () =>
            {
                try
                {
                    await _kitManager.OnItemsChangedLayoutHandler(oldItems, existingKit, token);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error invoking OnItemsChangedLayoutHandler.");
                }
            }, CancellationToken.None);
            
            _kitManager.Signs.UpdateSigns(existingKit);
            Context.Reply(_translations.KitOverwrote, existingKit);
            return;
        }

        if (!Context.TryGet(1, out Class @class))
        {
            if (Context.HasArgs(2))
                throw Context.Reply(_translations.ClassNotFound, Context.Get(1)!);

            throw Context.SendHelp();
        }

        KitType type = KitType.Public;
        if (Context.HasArgs(3) && !Context.MatchParameter(2, "default", "def", "-") && !Context.TryGet(2, out type))
        {
            throw Context.Reply(_translations.TypeNotFound, Context.Get(2)!);
        }

        string? factionString = null;
        if (Context.HasArgs(4) && !Context.MatchParameter(3, "default", "def", "-") && !Context.TryGet(3, out factionString))
        {
            throw Context.Reply(_translations.TypeNotFound, Context.Get(2)!);
        }

        FactionInfo? faction = null;
        if (!string.IsNullOrWhiteSpace(factionString))
        {
            faction = _factionStorage.FindFaction(factionString);

            if (faction == null)
            {
                throw Context.Reply(_translations.FactionNotFound, factionString);
            }
        }

        if (@class == Class.None)
        {
            @class = Class.Unarmed;
        }

        Branch branch = KitDefaults.GetDefaultBranch(@class);

        Kit kit = new Kit(kitId, @class, branch, type, SquadLevel.Member, faction);

        await _dbContext.AddAsync(kit, token).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);

        await UniTask.SwitchToMainThread(token);

        IKitItem[] newItems = ItemUtility.ItemsFromInventory(Context.Player, assetRedirectService: _assetRedirectService);
        kit.SetItemArray(newItems, _dbContext);

        kit.Creator = kit.LastEditor = Context.CallerId.m_SteamID;
        kit.WeaponText = _kitManager.GetWeaponText(kit);
        _dbContext.Update(kit);
        await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
        Context.LogAction(ActionLogType.CreateKit, kitId);

        await UniTask.SwitchToMainThread(token);
        _kitManager.Signs.UpdateSigns(kit);
        Context.Reply(_translations.KitCreated, kit);
    }
}
