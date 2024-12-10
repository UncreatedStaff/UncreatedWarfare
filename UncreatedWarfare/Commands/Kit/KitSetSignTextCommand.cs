using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

[Command("sign", "text"), SubCommandOf(typeof(KitCommand))]
internal sealed class KitSetSignTextCommand : IExecutableCommand
{
    private readonly KitCommandTranslations _translations;
    private readonly KitManager _kitManager;
    private readonly IKitsDbContext _dbContext;
    public required CommandContext Context { get; init; }

    public KitSetSignTextCommand(IServiceProvider serviceProvider)
    {
        _kitManager = serviceProvider.GetRequiredService<KitManager>();
        _dbContext = serviceProvider.GetRequiredService<IKitsDbContext>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<KitCommandTranslations>>().Value;

        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        if (!Context.TryGet(1, out string? kitId) || !Context.TryGet(2, out int level))
        {
            throw Context.SendHelp();
        }

        Kit? kit = await _kitManager.FindKit(kitId, token, true, x => x.Kits
            .Include(y => y.UnlockRequirementsModels)
            .Include(y => y.Translations)
        );

        if (kit == null)
        {
            throw Context.Reply(_translations.KitNotFound);
        }

        kitId = kit.InternalName;

        if (level == 0)
        {
            UnlockRequirement[] ulr = kit.UnlockRequirements;
            while (true)
            {
                int index = Array.FindIndex(ulr, x => x is LevelUnlockRequirement);
                if (index == -1)
                    break;

                CollectionUtility.RemoveFromArray(ref ulr, index);
            }
            kit.SetUnlockRequirementArray(ulr, _dbContext);
        }
        else
        {
            UnlockRequirement[] ulr = kit.UnlockRequirements;
            int index = Array.FindIndex(ulr, x => x is LevelUnlockRequirement);
            UnlockRequirement req = new LevelUnlockRequirement { UnlockLevel = level };
            if (index == -1)
            {
                CollectionUtility.AddToArray(ref ulr, req);
                kit.SetUnlockRequirementArray(ulr, _dbContext);
            }
            else
            {
                ((LevelUnlockRequirement)ulr[index]).UnlockLevel = level;
                kit.MarkLocalUnlockRequirementsDirty(_dbContext);
            }
        }

        kit.UpdateLastEdited(Context.CallerId);
        _dbContext.Update(kit);
        await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);

        await UniTask.SwitchToMainThread(token);

        _kitManager.Signs.UpdateSigns(kit);
        Context.Reply(_translations.KitPropertySet, "Level", kit, level.ToString(Context.Culture));
        Context.LogAction(ActionLogType.SetKitProperty, kitId + ": LEVEL >> " + level);
    }
}