using System;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Costs;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Interaction.Requests;

/// <summary>
/// Used to handle various results from a request.
/// </summary>
public interface IRequestResultHandler
{
    /// <summary>
    /// If translations for methods taking a string can use IMGUI for translations.
    /// </summary>
    bool CanUseIMGUI { get; }

    /// <summary>
    /// Called when the requestable object isn't registered or can't be found, etc.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    void NotFoundOrRegistered(WarfarePlayer player);

    /// <summary>
    /// Called when a generic requirement isn't met for this requestable object.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    void MissingRequirement(WarfarePlayer player, IRequestable<object> value, string localizedRequirement);

    /// <summary>
    /// Called when a generic requirement isn't met for this requestable object.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    void Success(WarfarePlayer player, IRequestable<object> value);

    /// <summary>
    /// Called when the player has to purchase the requestable object with /buy but hasn't yet.
    /// </summary>
    /// <param name="creditCost">The total amount of credits required to purchase the object.</param>
    void MissingCreditsOwnership(WarfarePlayer player, IRequestable<object> value, double creditCost);

    /// <summary>
    /// Called when the player has to purchase the requestable object with a donation.
    /// </summary>
    /// <param name="usdCost">The total amount of USD required to purchase the object.</param>
    void MissingDonorOwnership(WarfarePlayer player, IRequestable<object> value, decimal usdCost);
    
    /// <summary>
    /// Called when the player doesn't have access to an object.
    /// </summary>
    void MissingExclusiveOwnership(WarfarePlayer player, IRequestable<object> value);

    /// <summary>
    /// Called when the player doesn't meet an unlock cost.
    /// </summary>
    void MissingUnlockCost(WarfarePlayer player, IRequestable<object> value, UnlockCost unlockCost);

    /// <summary>
    /// Called when the player doesn't meet an unlock cost.
    /// </summary>
    void MissingUnlockRequirement(WarfarePlayer player, IRequestable<object> value, UnlockRequirement unlockRequirement);

    /// <summary>
    /// Called when the player tries to request a vehicle that's currently delayed.
    /// </summary>
    void VehicleDelayed(WarfarePlayer player, IRequestable<object> value, TimeSpan timeLeft);
}