﻿using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

[Command("createloadout", "cloadout", "cl"), SubCommandOf(typeof(KitCommand))]
internal class KitCreateLoadoutCommand : IExecutableCommand
{
    private readonly KitCommandTranslations _translations;
    private readonly KitManager _kitManager;
    private readonly IKitsDbContext _dbContext;
    public CommandContext Context { get; set; }

    public KitCreateLoadoutCommand(IServiceProvider serviceProvider)
    {
        _kitManager = serviceProvider.GetRequiredService<KitManager>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<KitCommandTranslations>>().Value;
        _dbContext = serviceProvider.GetRequiredService<IKitsDbContext>();

        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (!Context.TryGet(0, out CSteamID steam64, out WarfarePlayer? onlinePlayer) || !Context.TryGet(1, out Class @class))
        {
            throw Context.SendHelp();
        }

        if (!Context.TryGetRange(2, out string? signText) || string.IsNullOrWhiteSpace(signText))
        {
            signText = null;
        }

        Kit loadout = await _kitManager.Loadouts.CreateLoadout(_dbContext, Context.CallerId, steam64, @class, signText, token).ConfigureAwait(false);
        
        loadout.SetItemArray(ItemUtility.ItemsFromInventory(Context.Player, findAssetRedirects: true), _dbContext);

        await _dbContext.SaveChangesAsync(CancellationToken.None);

        try
        {
            // items have already been added so might as well unlock it
            await _kitManager.Loadouts.UnlockLoadout(Context.CallerId, loadout.InternalName, CancellationToken.None);
        }
        catch (InvalidOperationException) { }

        if (token.IsCancellationRequested)
        {
            return;
        }

        IPlayer player = (IPlayer?)onlinePlayer ?? await F.GetPlayerOriginalNamesAsync(steam64.m_SteamID, CancellationToken.None).ConfigureAwait(false);
        Context.Reply(_translations.LoadoutCreated, @class, player, player, loadout);
    }
}