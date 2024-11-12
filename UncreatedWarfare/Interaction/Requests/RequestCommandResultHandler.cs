using System;
using Uncreated.Warfare.Kits.Translations;
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
    private readonly RequestTranslations _translations;
    public bool CanUseIMGUI => true;
    public RequestCommandResultHandler(ChatService chatService, TranslationInjection<RequestTranslations> translations)
    {
        _chatService = chatService;
        _translations = translations.Value;
    }

    public void NotFoundOrRegistered(WarfarePlayer player)
    {
        _chatService.Send(player, _translations.RequestNoTarget);
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
        if (creditCost > player.CachedPoints.Credits)
        {
            _chatService.Send(player, _translations.RequestNotOwnedCreditsCantAfford, (int)player.CachedPoints.Credits, (int)creditCost);
        }
        else
        {
            _chatService.Send(player, _translations.RequestNotOwnedCreditsCanAfford, (int)creditCost);
        }
    }

    public void MissingDonorOwnership(WarfarePlayer player, IRequestable<object> value, decimal usdCost)
    {
        _chatService.Send(player, _translations.RequestNotOwnedDonor, $"$ {usdCost.ToString("N2", player.Locale.CultureInfo)} USD");
    }

    public void MissingExclusiveOwnership(WarfarePlayer player, IRequestable<object> value)
    {
        _chatService.Send(player, _translations.RequestMissingAccess);
    }

    public void MissingUnlockCost(WarfarePlayer player, IRequestable<object> value, UnlockCost unlockCost)
    {
        throw new NotImplementedException();
    }

    public void MissingUnlockRequirement(WarfarePlayer player, IRequestable<object> value, UnlockRequirement unlockRequirement)
    {
        throw new NotImplementedException();
    }
}
