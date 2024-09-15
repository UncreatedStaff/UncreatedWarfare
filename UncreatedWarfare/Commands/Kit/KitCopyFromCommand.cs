using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;

namespace Uncreated.Warfare.Commands;

[Command("copyfrom", "copy", "cf"), SubCommandOf(typeof(KitCommand))]
internal class KitCopyFromCommand : IExecutableCommand
{
    private readonly KitCommandTranslations _translations;
    private readonly KitManager _kitManager;
    private readonly IKitsDbContext _dbContext;
    private readonly LanguageService _languageService;
    public CommandContext Context { get; set; }

    public KitCopyFromCommand(IServiceProvider serviceProvider)
    {
        _kitManager = serviceProvider.GetRequiredService<KitManager>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<KitCommandTranslations>>().Value;
        _languageService = serviceProvider.GetRequiredService<LanguageService>();
        _dbContext = serviceProvider.GetRequiredService<IKitsDbContext>();

        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        if (!Context.TryGet(0, out string? existingKitId) || !Context.TryGet(1, out string? newKitId))
        {
            throw Context.SendHelp();
        }
        
        Kit? sourceKit = await _kitManager.FindKit(existingKitId, token, true);
        if (sourceKit == null)
        {
            throw Context.Reply(_translations.KitNotFound, existingKitId);
        }

        
        Kit? existingKit = await _kitManager.FindKit(newKitId, token, true);
        if (existingKit != null)
        {
            throw Context.Reply(_translations.KitNameTaken, newKitId);
        }

        Kit kit;

        bool isAdded = false;
        if (LoadoutIdHelper.Parse(newKitId, out CSteamID player) > 0)
        {
            if (sourceKit.Type != KitType.Loadout)
            {
                throw Context.Reply(_translations.KitCopyNonLoadoutToLoadout, sourceKit, newKitId);
            }

            kit = await _kitManager.Loadouts.CreateLoadout(_dbContext, Context.CallerId, player, sourceKit.Class, sourceKit.GetDisplayName(_languageService), token);
            newKitId = kit.InternalName;
            isAdded = true;
        }
        else
        {
            kit = new Kit(newKitId.ToLowerInvariant().Replace(' ', '_'), sourceKit)
            {
                Season = UCWarfare.Season,
                Disabled = true, // temporarily lock the kit until all the properties can be copied over
                Creator = Context.CallerId.m_SteamID
            };
        }

        if (!isAdded)
        {
            await _dbContext.AddAsync(kit, token).ConfigureAwait(false);
            await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);

            kit.Disabled = false;
        }
        else
        {
            kit.CopyFrom(sourceKit, true, true);
            kit.Disabled = true;
        }

        kit.ReapplyPrimaryKey();

        foreach (KitSkillset skillset in kit.Skillsets)
            _dbContext.Add(skillset);

        foreach (KitFilteredFaction faction in kit.FactionFilter)
            _dbContext.Add(faction);

        foreach (KitFilteredMap map in kit.MapFilter)
            _dbContext.Add(map);

        foreach (KitItemModel item in kit.ItemModels)
            _dbContext.Add(item);

        foreach (KitTranslation translation in kit.Translations)
            _dbContext.Add(translation);

        foreach (KitUnlockRequirement unlockRequirement in kit.UnlockRequirementsModels)
            _dbContext.Add(unlockRequirement);

        _dbContext.Update(kit);
        await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);

        if (isAdded)
        {
            try
            {
                await _kitManager.Loadouts.UnlockLoadout(Context.CallerId, newKitId, token).ConfigureAwait(false);
            }
            catch (InvalidOperationException) { }
            catch (KitNotFoundException) { }
        }

        Context.LogAction(ActionLogType.CreateKit, newKitId + " COPIED FROM " + sourceKit.InternalName);

        await UniTask.SwitchToMainThread(token);
        _kitManager.Signs.UpdateSigns(sourceKit);
        Context.Reply(_translations.KitCopied, sourceKit, kit);
    }
}