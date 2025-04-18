using Microsoft.Extensions.Configuration;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Loadouts;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("upgrade"), SubCommandOf(typeof(RequestCommand))]
internal sealed class RequestUpgradeCommand : IExecutableCommand
{
    private readonly LoadoutService _loadoutService;
    private readonly string _domain;
    private readonly SignInstancer _signInstancer;
    private readonly RequestTranslations _requestTranslations;
    private readonly KitCommandTranslations _kitTranslations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public RequestUpgradeCommand(
        TranslationInjection<RequestTranslations> requestTranslations,
        TranslationInjection<KitCommandTranslations> kitTranslations,
        SignInstancer signInstancer,
        LoadoutService loadoutService,
        IConfiguration systemConfig)
    {
        _signInstancer = signInstancer;
        _loadoutService = loadoutService;
        _domain = systemConfig["domain"] ?? "https://uncreated.network";
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

        int loadoutIndex = kitSign.LoadoutNumber;

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

        Context.ReplyUrl(_requestTranslations.RequestUpgradeMessage.Translate(Context.Player),
            $"{_domain}/loadouts/{Context.CallerId.m_SteamID}/{LoadoutIdHelper.GetLoadoutLetter(loadoutLetter)}");
    }
}