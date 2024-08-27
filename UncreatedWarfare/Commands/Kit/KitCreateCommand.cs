using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("create", "c", "override"), SubCommandOf(typeof(KitCommand))]
internal class KitCreateCommand : IExecutableCommand
{
    private readonly KitCommandTranslations _translations;
    private readonly KitManager _kitManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly IFactionDataStore _factionStorage;
    private readonly CommandDispatcher _commandDispatcher;
    private readonly IKitsDbContext _dbContext;
    public CommandContext Context { get; set; }

    public KitCreateCommand(IServiceProvider serviceProvider)
    {
        _kitManager = serviceProvider.GetRequiredService<KitManager>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<KitCommandTranslations>>().Value;
        _serviceProvider = serviceProvider;
        _factionStorage = serviceProvider.GetRequiredService<IFactionDataStore>();
        _commandDispatcher = serviceProvider.GetRequiredService<CommandDispatcher>();
        _dbContext = serviceProvider.GetRequiredService<IKitsDbContext>();
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (!Context.TryGet(0, out string kitId))
        {
            throw Context.SendHelp();
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
        }


    }
}
