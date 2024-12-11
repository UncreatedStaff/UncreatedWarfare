using DanielWillett.ModularRpcs.Exceptions;
using Uncreated.Warfare.Discord;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Translations;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;

namespace Uncreated.Warfare.Commands;

[Command("upgrade"), SubCommandOf(typeof(RequestCommand))]
internal sealed class RequestUpgradeCommand : IExecutableCommand
{
    private readonly KitManager _kitManager;
    private readonly SignInstancer _signInstancer;
    private readonly LanguageService _languageService;
    private readonly IUserDataService _userData;
    private readonly DiscordUserService _discordUserService;
    private readonly RequestTranslations _requestTranslations;
    private readonly KitCommandTranslations _kitTranslations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public RequestUpgradeCommand(
        KitManager kitManager,
        TranslationInjection<RequestTranslations> requestTranslations,
        TranslationInjection<KitCommandTranslations> kitTranslations,
        SignInstancer signInstancer,
        LanguageService languageService,
        IUserDataService userData,
        DiscordUserService discordUserService)
    {
        _kitManager = kitManager;
        _signInstancer = signInstancer;
        _languageService = languageService;
        _userData = userData;
        _discordUserService = discordUserService;
        _requestTranslations = requestTranslations.Value;
        _kitTranslations = kitTranslations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (!Context.TryGetBarricadeTarget(out BarricadeDrop? barricade) || barricade.interactable is not InteractableSign)
        {
            throw Context.Reply(_requestTranslations.RequestNoTarget);
        }

        ISignInstanceProvider? provider = _signInstancer.GetSignProvider(barricade);
        if (provider is not KitSignInstanceProvider { LoadoutNumber: > 0 } kitSign)
        {
            throw Context.Reply(_requestTranslations.RequestNoTarget);
        }

        ulong discordId = await _userData.GetDiscordIdAsync(Context.CallerId.m_SteamID, token).ConfigureAwait(false);
        if (discordId == 0ul)
        {
            Context.Reply(_requestTranslations.DiscordNotLinked1);
            Context.Reply(_requestTranslations.DiscordNotLinked2, Context.Player);
            return;
        }

        int loadoutIndex = kitSign.LoadoutNumber;

        bool inDiscordServer;
        try
        {
            inDiscordServer = await _discordUserService.IsMemberOfGuild(discordId);
        }
        catch (RpcInvocationException ex)
        {
            Context.Logger.LogError(ex.InnerException, "Error checking for member of guild.");
            throw Context.SendUnknownError();
        }
        catch (RpcException)
        {
            throw Context.Reply(_requestTranslations.RequestUpgradeNotConnected);
        }

        if (!inDiscordServer)
            throw Context.Reply(_requestTranslations.RequestUpgradeNotInDiscordServer);

        Kit? kit = await _kitManager.Loadouts.GetLoadout(Context.CallerId, loadoutIndex, token).ConfigureAwait(false);
        
        if (kit == null)
        {
            throw Context.Reply(_requestTranslations.RequestNoTarget);
        }

        await UniTask.SwitchToMainThread(token);

        int loadoutLetter = LoadoutIdHelper.ParseNumber(kit.InternalName);

        if (!kit.NeedsUpgrade || loadoutLetter <= 0)
        {
            throw Context.Reply(_kitTranslations.DoesNotNeedUpgrade, kit);
        }

        KitLoadouts.OpenUpgradeTicketResult result;

        try
        {
            result = await _kitManager.Loadouts.TryOpenUpgradeTicket(
                discordId,
                Context.CallerId.m_SteamID,
                loadoutLetter,
                kit.Class,
                kit.GetDisplayName(_languageService)
            );
        }
        catch (RpcInvocationException ex)
        {
            Context.Logger.LogError(ex, "Error starting upgrade ticket.");
            throw Context.SendUnknownError();
        }
        catch (RpcException)
        {
            throw Context.Reply(_requestTranslations.RequestUpgradeNotConnected);
        }

        switch (result)
        {
            case KitLoadouts.OpenUpgradeTicketResult.AlreadyOpen:
                throw Context.Reply(_requestTranslations.RequestUpgradeAlreadyOpen, kit);

            case KitLoadouts.OpenUpgradeTicketResult.TooManyTickets:
                throw Context.Reply(_requestTranslations.RequestUpgradeTooManyTicketsOpen);
        }

        Context.Reply(_requestTranslations.TicketOpened, kit);
    }
}