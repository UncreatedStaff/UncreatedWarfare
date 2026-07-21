using Microsoft.Extensions.Configuration;
using System;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Costs;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Interaction.Requests;

/// <summary>
/// Handles chat output for /request.
/// </summary>
public class RequestCommandResultHandler : IRequestResultHandler
{
    private readonly ChatService _chatService;
    private readonly IConfiguration _systemConfig;
    private readonly RequestTranslations _translations;

    public bool CanUseIMGUI => true;
    
    public RequestCommandResultHandler(ChatService chatService, IConfiguration systemConfig, TranslationInjection<RequestTranslations> translations)
    {
        _chatService = chatService;
        _systemConfig = systemConfig;
        _translations = translations.Value;
    }

    public void NotFoundOrRegistered(WarfarePlayer player)
    {
        _chatService.Send(player, _translations.RequestNotRegistered);
    }

    public void MissingRequirement(WarfarePlayer player, IRequestable<object> value, string localizedRequirement)
    {
        _chatService.Send(player, _translations.RequestError, localizedRequirement);
    }

    public void Success(WarfarePlayer player, IRequestable<object> value)
    {
        _chatService.Send(player, _translations.RequestedSuccess, value);
    }

    public void MissingCreditsOwnership(WarfarePlayer player, IRequestable<object> value, double creditCost)
    {
        _chatService.Send(player, _translations.RequestNotOwnedCreditsCannotAfford, (int)player.CachedPoints.Credits, (int)creditCost);
    }

    public void MissingDonorOwnership(WarfarePlayer player, IRequestable<object> value, decimal usdCost)
    {
        _chatService.Send(player, _translations.RequestNotOwnedDonor, $"$ {usdCost.ToString("N2", player.Locale.CultureInfo)} USD");

        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();
            string domain = _systemConfig["domain"] ?? "https://uncreated.network";
            if (!player.IsOnline)
                return;

            player.UnturnedPlayer.sendBrowserRequest(
                _translations.RequestNotOwnedDonorWebRequest.Translate($"$ {usdCost.ToString("N2", player.Locale.CultureInfo)} USD"),
                domain + "/products/kits"
            );
        });
    }

    public void MissingExclusiveOwnership(WarfarePlayer player, IRequestable<object> value)
    {
        _chatService.Send(player, _translations.RequestMissingAccess);
    }

    public void MissingUnlockCost(WarfarePlayer player, IRequestable<object> value, UnlockCost unlockCost)
    {
        _chatService.Send(player, _translations.RequestError, unlockCost.ToString());
    }

    public void MissingUnlockRequirement(WarfarePlayer player, IRequestable<object> value, UnlockRequirement unlockRequirement)
    {
        _chatService.Send(player, _translations.RequestError, unlockRequirement.ToString());
    }

    public void VehicleDelayed(WarfarePlayer player, IRequestable<object> value, TimeSpan timeLeft)
    {
        _chatService.Send(player, _translations.RequestVehicleTimeDelay, timeLeft);
    }

    public void MissingSquad(WarfarePlayer player, IRequestable<object> value, ref bool openSquadMenu)
    {
        if (!openSquadMenu)
            _chatService.Send(player, _translations.RequestNotInSquad);
    }
}
