using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;

namespace Uncreated.Warfare.Commands;

[Command("rename", "rname", "name"), SubCommandOf(typeof(KitCommand))]
internal class KitRenameCommand : IExecutableCommand
{
    private readonly SignInstancer _signs;
    private readonly KitCommandTranslations _translations;
    private readonly KitManager _kitManager;
    private readonly LanguageService _languageService;
    private readonly IServiceProvider _serviceProvider;
    public CommandContext Context { get; set; }

    public KitRenameCommand(TranslationInjection<KitCommandTranslations> translations, SignInstancer signs, KitManager kitManager, LanguageService languageService, IServiceProvider serviceProvider)
    {
        _signs = signs;
        _kitManager = kitManager;
        _languageService = languageService;
        _translations = translations.Value;
        _serviceProvider = serviceProvider;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        string? kitId = null;

        // kit rename
        if (Context.TryGetBarricadeTarget(out BarricadeDrop? barricade)
                 && barricade.interactable is not InteractableSign
                 && _signs.GetSignProvider(barricade) is KitSignInstanceProvider signData)
        {
            kitId = signData.LoadoutNumber > 0
                ? KitEx.GetLoadoutName(Context.CallerId.m_SteamID, signData.LoadoutNumber)
                : signData.KitId;
        }

        if (kitId == null)
        {
            throw Context.Reply(_translations.KitOperationNoTarget);
        }

        Kit? kit = await _kitManager.FindKit(kitId, token, exactMatchOnly: false);
        if (kit == null)
        {
            throw Context.Reply(_translations.KitNotFound, kitId);
        }

        if (kit.Type != KitType.Loadout)
        {
            throw Context.Reply(_translations.KitRenameNotLoadout);
        }

        string? name = Context.GetRange(0);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw Context.SendHelp();
        }

        if (Data.GetChatFilterViolation(name) is { } filterViolation)
        {
            throw Context.Reply(_translations.KitRenameFilterVoilation, filterViolation);
        }

        await Context.Player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await using IKitsDbContext dbContext = _serviceProvider.GetRequiredService<WarfareDbContext>();

            kit = await _kitManager.GetKit(dbContext, kit.PrimaryKey, token);
            if (kit == null)
            {
                throw Context.Reply(_translations.KitNotFound, kitId);
            }

            if (kit.Disabled || kit.Season < UCWarfare.Season || !await _kitManager.HasAccess(dbContext, kit, Context.Player, token))
            {
                throw Context.Reply(_translations.KitRenameNoAccess, kit);
            }

            LanguageInfo defaultLanguage = _languageService.GetDefaultLanguage();

            string oldName = kit.GetDisplayName(_languageService, defaultLanguage, removeNewLine: false);
            string newName = KitEx.ReplaceNewLineSubstrings(name);

            kit.SetSignText(dbContext, Context.CallerId.m_SteamID, newName, defaultLanguage);
            if (kit.Translations.Count > 1)
            {
                for (int i = kit.Translations.Count - 1; i >= 0; i--)
                {
                    KitTranslation t = kit.Translations[i];
                    if (t.LanguageId == defaultLanguage.Key)
                        continue;

                    dbContext.Remove(t);
                    kit.Translations.RemoveAt(i);
                }
            }

            oldName = oldName.Replace("\n", "<br>");
            newName = newName.Replace("\n", "<br>");

            await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
            int ldId = KitEx.ParseStandardLoadoutId(kit.InternalName);
            string ldIdStr = ldId == -1 ? "???" : KitEx.GetLoadoutLetter(ldId).ToUpperInvariant();
            Context.LogAction(ActionLogType.SetKitProperty, kit.FactionId + ": SIGN TEXT >> \"" + newName + "\" (using /kit rename)");
            _kitManager.Signs.UpdateSigns(kit);
            Context.Reply(_translations.KitRenamed, ldIdStr, oldName, newName);
        }
        finally
        {
            Context.Player.PurchaseSync.Release();
        }
    }
}
