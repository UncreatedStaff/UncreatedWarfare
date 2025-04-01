using DanielWillett.ModularRpcs.Exceptions;
using Uncreated.Warfare.Discord;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Loadouts;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("upgrade"), SubCommandOf(typeof(RequestCommand))]
internal sealed class RequestUpgradeCommand : IExecutableCommand
{
    private readonly LoadoutService _loadoutService;
    private readonly SignInstancer _signInstancer;
    private readonly IUserDataService _userData;
    private readonly DiscordUserService _discordUserService;
    private readonly RequestTranslations _requestTranslations;
    private readonly KitCommandTranslations _kitTranslations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public RequestUpgradeCommand(
        TranslationInjection<RequestTranslations> requestTranslations,
        TranslationInjection<KitCommandTranslations> kitTranslations,
        SignInstancer signInstancer,
        IUserDataService userData,
        DiscordUserService discordUserService,
        LoadoutService loadoutService)
    {
        _signInstancer = signInstancer;
        _userData = userData;
        _discordUserService = discordUserService;
        _loadoutService = loadoutService;
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
            Context.Logger.LogError(ex, "Error checking for member of guild.");
            throw Context.SendUnknownError();
        }
        catch (RpcException ex)
        {
            Context.Logger.LogWarning(ex.InnerException, "Error checking for member of guild.");
            throw Context.Reply(_requestTranslations.RequestUpgradeNotConnected);
        }

        if (!inDiscordServer)
            throw Context.Reply(_requestTranslations.RequestUpgradeNotInDiscordServer);

        Kit? kit = await _loadoutService.GetLoadoutFromNumber(Context.CallerId, loadoutIndex, KitInclude.Base, token)
            .ConfigureAwait(false);
        
        if (kit == null)
        {
            throw Context.Reply(_requestTranslations.RequestNoTarget);
        }

        await UniTask.SwitchToMainThread(token);

        int loadoutLetter = LoadoutIdHelper.ParseNumber(kit.Id);

        if (kit.Season >= WarfareModule.Season || loadoutLetter <= 0)
        {
            throw Context.Reply(_kitTranslations.DoesNotNeedUpgrade, kit);
        }

        LoadoutService.OpenUpgradeTicketResult result;

        try
        {
            result = await _loadoutService.TryOpenUpgradeTicket(
                discordId,
                Context.CallerId,
                loadoutLetter,
                kit.Class,
                kit.GetDisplayName(null, true)
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
            case LoadoutService.OpenUpgradeTicketResult.AlreadyOpen:
                throw Context.Reply(_requestTranslations.RequestUpgradeAlreadyOpen, kit);

            case LoadoutService.OpenUpgradeTicketResult.TooManyTickets:
                throw Context.Reply(_requestTranslations.RequestUpgradeTooManyTicketsOpen);
        }

        Context.Reply(_requestTranslations.TicketOpened, kit);
    }
}