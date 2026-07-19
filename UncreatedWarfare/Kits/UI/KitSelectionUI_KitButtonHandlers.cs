using Microsoft.Extensions.Configuration;
using System;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Presets;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Interaction.Requests;
using Uncreated.Warfare.Kits.Loadouts;
using Uncreated.Warfare.Models.Users;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Squads.UI;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Kits.UI;

partial class KitSelectionUI
{
    // click the root element of a favorite kit
    private void HandleFavoriteKitClicked(UnturnedButton button, Player unturnedPlayer)
    {
        WarfarePlayer player = _playerService.GetOnlinePlayer(unturnedPlayer);
        KitSelectionUIData data = GetOrAddData(player);

        int index = Array.FindIndex(_favoriteKits, x => (object)x.Root == button);
        if (index < 0 || index >= data.FavoriteKitsCache.Length || data.FavoriteKitsCache[index] is not { } favoritedKit)
            return;

        data.ClassFilter = Class.None;
        data.NameFilter = string.Empty;


        Task.Run(async () =>
        {
            try
            {
                Kit? fullKit = await _kitDataStore.QueryKitAsync(favoritedKit.Key, KitInclude.UI);
                if (fullKit == null)
                {
                    return;
                }

                await UniTask.SwitchToMainThread();

                _listNoResult.Hide(player.Connection);
                ListKitInfo ui = _listResults[0];
                SendKitInfo(ui, player, fullKit, player.Component<KitPlayerComponent>(), data, false, index: 0);

                bool checkNitroBoostStatus = data.GetCachedState(0).LabelState == StatusState.ServerBoostRequired;

                ui.Root.Show(player);

                for (int i = 1; i < _listResults.Length; ++i)
                {
                    ref KitCacheInformation info = ref data.GetCachedState(i);
                    if (info.Kit == null)
                        break;

                    info = default;
                    _listResults[i].Hide(player.Connection);
                }

                if (!data.IsListOpen)
                {
                    SetListOpened(data, player, true);
                }

                UniTask sendDetails = SendKitDetailsAsync(player, fullKit, CancellationToken.None);
                if (checkNitroBoostStatus)
                {
                    await CheckNitroBoostStatus(player, data, player.DisconnectToken, false, true);
                }

                await sendDetails;
            }
            catch (Exception ex)
            {
                GetLogger().LogError(ex, $"Error isolating kit {favoritedKit.Id} for {player}.");
            }
        });

    }

    // click the Request button on a favorite kit
    private void HandleFavoriteKitRequestClicked(UnturnedButton button, Player unturnedPlayer)
    {
        int favIndex = Array.FindIndex(_favoriteKits, f => (object)f.RequestButton == button);

        WarfarePlayer player = _playerService.GetOnlinePlayer(unturnedPlayer);
        KitSelectionUIData data = GetOrAddData(player);
        if (data.Operations > 0 || favIndex < 0 || favIndex >= data.FavoriteKitsCache.Length || data.FavoriteKitsCache[favIndex] is not { } favKit)
            return;

        Interlocked.Increment(ref data.Operations);

        button.Hide(player);

        Task.Run(async () =>
        {
            bool requested = false;
            try
            {
                requested = await _kitRequestService.RequestAsync(player, favKit, new RequestCommandResultHandler(_chatService, _requestTranslations), player.DisconnectToken);
            }
            catch (Exception ex)
            {
                GetLogger().LogError(ex, $"Error requesting kit {favKit.Id} for {player}.");
            }
            finally
            {
                Interlocked.Decrement(ref data.Operations);
                if (!requested)
                    button.Show(player);
                else
                    await CloseAsync(player);
            }
        });
    }

    // click the Unfavorite button on a favorite kit
    private void HandleFavoriteKitUnfavoriteClicked(UnturnedButton button, Player unturnedPlayer)
    {
        int favIndex = Array.FindIndex(_favoriteKits, f => (object)f.UnfavoriteButton == button);

        WarfarePlayer player = _playerService.GetOnlinePlayer(unturnedPlayer);
        KitSelectionUIData data = GetOrAddData(player);
        if (data.Operations > 0 || favIndex < 0 || favIndex >= data.FavoriteKitsCache.Length || data.FavoriteKitsCache[favIndex] is not { } favKit)
            return;

        Interlocked.Increment(ref data.Operations);

        Task.Run(async () =>
        {
            try
            {
                // RemoveFavorite will invoke OnFavoriteUpdated to update UI
                await _kitFavoriteService.RemoveFavorite(player.Steam64, favKit.Key);
            }
            catch (Exception ex)
            {
                GetLogger().LogError(ex, $"Error removing favorite from {favKit.Id} for {player}.");
            }
            finally
            {
                Interlocked.Decrement(ref data.Operations);
            }
        });
    }

    // click the Favorite button on a full-sized kit
    private void HandleButtonFavoriteKitClicked(UnturnedButton button, Player unturnedPlayer)
    {
        if (!TryGetTargetKit(x => x.FavoriteButton, button, out Class @class, out int kitIndex, out _))
        {
            return;
        }

        WarfarePlayer player = _playerService.GetOnlinePlayer(unturnedPlayer); 
        KitSelectionUIData data = GetOrAddData(player);
        if (data.Operations > 0)
            return;

        Interlocked.Increment(ref data.Operations);

        ref KitCacheInformation cache = ref data.GetCachedState(@class, kitIndex);
        Kit? kit = cache.Kit;
        if (kit == null)
            return;

        Task.Run(async () =>
        {
            try
            {
                // AddFavorite will invoke OnFavoriteUpdated to update UI
                await _kitFavoriteService.AddFavorite(player.Steam64, kit.Key);
            }
            catch (Exception ex)
            {
                GetLogger().LogError(ex, $"Error adding favorite to ({@class}, {kitIndex}: {kit.Id}) for {player}.");
            }
            finally
            {
                Interlocked.Decrement(ref data.Operations);
            }
        });
    }

    // click the Unfavorite button on a full-sized kit
    private void HandleButtonUnfavoriteKitClicked(UnturnedButton button, Player unturnedPlayer)
    {
        if (!TryGetTargetKit(x => x.UnfavoriteButton, button, out Class @class, out int kitIndex, out _))
        {
            return;
        }

        WarfarePlayer player = _playerService.GetOnlinePlayer(unturnedPlayer);
        KitSelectionUIData data = GetOrAddData(player);
        if (data.Operations > 0)
            return;

        Interlocked.Increment(ref data.Operations);

        ref KitCacheInformation cache = ref data.GetCachedState(@class, kitIndex);

        Kit? kit = cache.Kit;
        if (kit == null)
            return;

        Task.Run(async () =>
        {
            try
            {
                // RemoveFavorite will invoke OnFavoriteUpdated to update UI
                await _kitFavoriteService.RemoveFavorite(player.Steam64, kit.Key);
            }
            catch (Exception ex)
            {
                GetLogger().LogError(ex, $"Error removing favorite from ({@class}, {kitIndex}: {kit.Id}) for {player}.");
            }
            finally
            {
                Interlocked.Decrement(ref data.Operations);
            }
        });
    }

    // click the Request button on a full-sized kit
    private void HandleButtonRequestKitClicked(UnturnedButton button, Player unturnedPlayer)
    {
        if (!TryGetTargetKit(x => x.RequestButton, button, out Class @class, out int kitIndex, out KitInfo? kitInfo))
        {
            return;
        }

        WarfarePlayer player = _playerService.GetOnlinePlayer(unturnedPlayer);
        KitSelectionUIData data = GetOrAddData(player);
        if (data.Operations > 0)
            return;

        Interlocked.Increment(ref data.Operations);

        ref KitCacheInformation cache = ref data.GetCachedState(@class, kitIndex);

        Kit? kit = cache.Kit;
        if (kit == null)
            return;

        button.Hide(player);

        Task.Run(async () =>
        {
            bool requested = false;
            try
            {
                Kit? oldKit = player.Component<KitPlayerComponent>().GetActiveEffectiveKit()?.CachedKit;
                requested = await _kitRequestService.RequestAsync(player, kit, new RequestCommandResultHandler(_chatService, _requestTranslations), player.DisconnectToken);
                if (oldKit != null)
                {
                    await UpdateKitAsync(oldKit, player, player.DisconnectToken);
                }
            }
            catch (Exception ex)
            {
                GetLogger().LogError(ex, $"Error requesting kit ({@class}, {kitIndex}: {kit.Id}) for {player}.");
            }
            finally
            {
                Interlocked.Decrement(ref data.Operations);
                if (!requested)
                    button.Show(player);
                else
                    await UpdateKitAsync(kit, player, player.DisconnectToken);
            }
        });
    }

    // click the Preview button on a full-sized kit
    private void HandleButtonPreviewKitClicked(UnturnedButton button, Player unturnedPlayer)
    {
        if (!TryGetTargetKit(x => x.PreviewButton, button, out Class @class, out int kitIndex, out KitInfo? kitInfo))
        {
            return;
        }

        WarfarePlayer player = _playerService.GetOnlinePlayer(unturnedPlayer);

        KitSelectionUIData data = GetOrAddData(player);
        if (data.Operations > 0)
            return;

        ref KitCacheInformation cache = ref data.GetCachedState(@class, kitIndex);
        if (!_zoneStore.IsInMainBase(player))
        {
            if (cache.Kit != null)
                UpdateActionButtons(cache.Kit, player, kitInfo, data, kitIndex, @class);
            return;
        }

        Interlocked.Increment(ref data.Operations);

        Kit? kit = cache.Kit;
        if (kit == null)
            return;

        Task.Run(async () =>
        {
            try
            {
                if ((KitInclude.UI & KitInclude.Giveable) != KitInclude.Giveable)
                {
                    kit = await _kitDataStore.QueryKitAsync(kit.Key, KitInclude.Giveable, player.DisconnectToken);
                    if (kit == null)
                        return;
                }

                await _kitRequestService.GiveKitAsync(player, new KitBestowData(kit)
                {
                    IsPreview = true,
                    IsLowAmmo = false,
                    Silent = true
                });

                await CloseAsync(player);

                _chatService.Send(
                    player,
                    _translations.ChatPreviewingKit,
                    kit,
                    _commandDispatcher?.FindCommand(typeof(KitBackCommand))!
                );
            }
            catch (Exception ex)
            {
                GetLogger().LogError(ex, $"Error previewing ({@class}, {kitIndex}: {kit?.Id}) for {player}.");
            }
            finally
            {
                Interlocked.Decrement(ref data.Operations);
            }
        });
    }

    // click the buy/unlock button on a full-sized kit
    private void HandleButtonUnlockKitClicked(UnturnedButton button, Player unturnedPlayer)
    {
        if (!TryGetTargetKit(x => x.UnlockButton.Button, button, out Class @class, out int kitIndex, out KitInfo? kitInfo))
        {
            return;
        }

        WarfarePlayer player = _playerService.GetOnlinePlayer(unturnedPlayer);
        KitSelectionUIData data = GetOrAddData(player);
        if (data.Operations > 0)
            return;

        Interlocked.Increment(ref data.Operations);

        ref KitCacheInformation cache = ref data.GetCachedState(@class, kitIndex);

        Kit? kit = cache.Kit;
        if (kit == null)
            return;

        PurchaseButtonState buttonState = cache.ButtonState;

        string domain = _configuration["domain"] ?? "https://uncreated.network";

        bool done = true;

        bool canRequest = CanPlayerRequestKit(data, player);

        if (!canRequest && buttonState != PurchaseButtonState.Rearm)
        {
            UpdateStatusLabels(kitInfo, false, data, @class, kitIndex, player, kit, player.Component<KitPlayerComponent>());
            UpdateActionButtons(kit, player, kitInfo, data, kitIndex, @class);
            return;
        }

        try
        {
            switch (buttonState)
            {
                case PurchaseButtonState.None:
                    done = false;

                    Task.Run(async () =>
                    {
                        bool requested = false;
                        try
                        {
                            // todo: remove ammo from data.AmmoSupply if != null and also this should show on the button
                            requested = await _kitRequestService.RequestAsync(player, kit, new RequestCommandResultHandler(_chatService, _requestTranslations), player.DisconnectToken);
                        }
                        catch (Exception ex)
                        {
                            GetLogger().LogError(ex, $"Error requesting kit {kit.Id} for {player} via button press.");
                        }
                        finally
                        {
                            Interlocked.Decrement(ref data.Operations);
                            if (!requested)
                                await UpdateKitAsync(kit, player, player.DisconnectToken);
                            else
                                await CloseAsync(player);
                        }
                    });
                    break;

                case PurchaseButtonState.PremiumCost:
                    _ = CloseAsync(player);
                    unturnedPlayer.sendBrowserRequest(_translations.PurchaseButtonCurrencyBrowserRequest.Translate(player, canUseIMGUI: true), domain + "/products/kits");
                    break;

                case PurchaseButtonState.CreditCost:
                    _ = TempCloseAsync(player);

                    // confirm purchase kit modal
                    ToastMessage message = ToastMessage.Popup(
                        _requestKitsTranslations.ModalConfirmPurchaseKitHeading.Translate(player),
                        _requestKitsTranslations.ModalConfirmPurchaseKitDescription.Translate(kit, kit.CreditCost, player),
                        _requestKitsTranslations.ModalConfirmPurchaseKitAcceptButton.Translate(player),
                        _requestKitsTranslations.ModalConfirmPurchaseKitCancelButton.Translate(player),
                        callbacks: new PopupCallbacks((player, _, in _, ref _, ref _) =>
                        {
                            Task.Run(async () =>
                            {
                                await _kitRequestService.BuyKitAsync(player, kit, player.UnturnedPlayer.look.aim.position + player.UnturnedPlayer.look.aim.forward * 0.3f, player.DisconnectToken);
                                await TempUncloseAsync(player);
                                await UpdateKitAsync(kit, player);
                            });
                        }, (player, _, in _, ref _, ref _) =>
                        {
                            _ = TempUncloseAsync(player);
                        })
                    );

                    player.SendToast(message);
                    break;

                case PurchaseButtonState.ViewLoadoutTicket:
                case PurchaseButtonState.OpenLoadoutTicket:
                    int id = LoadoutIdHelper.Parse(kit.Id);
                    if (id < 0)
                        break;

                    _ = CloseAsync(player);
                    unturnedPlayer.sendBrowserRequest(
                        _translations.PurchaseButtonViewLoadoutTicketRequest.Translate(player, canUseIMGUI: true),
                        $"{domain}/loadouts/{player.Steam64.m_SteamID}/{LoadoutIdHelper.GetLoadoutLetter(id)}?adminmode=False"
                    );
                    break;

                case PurchaseButtonState.JoinSquad:
                case PurchaseButtonState.CreateSquad:
                    if (_squadMenu == null)
                        break;

                    _ = CloseAsync(player);
                    _squadMenu.OpenUI(player, new SquadMenuUI.KitRequestState(kit, new RequestCommandResultHandler(_chatService, _requestTranslations)));
                    break;

                case PurchaseButtonState.OpenDiscordForBoosts:
                    ulong guildId = _configuration.GetValue<ulong>("discord_guild_id");
                    if (guildId != 0)
                    {
                        _ = CloseAsync(player);

                        unturnedPlayer.sendBrowserRequest(
                            _translations.PurchaseButtonNotBoostingOpenDiscordRequest.Translate(player, canUseIMGUI: true),
                            $"https://discord.com/channels/{guildId}/boosts"
                        );
                    }
                    break;

                case PurchaseButtonState.JoinDiscordGuild:
                    string url = DiscordCommand.GetDiscordJoinUrl(_configuration);
                    _ = CloseAsync(player);

                    unturnedPlayer.sendBrowserRequest(
                        _translations.PurchaseButtonNotBoostingOpenDiscordRequest.Translate(player, canUseIMGUI: true),
                        url
                    );
                    break;

                case PurchaseButtonState.BeginLinkDiscordAccount:
                    if (_acountLinkingService == null)
                        break;

                    _ = CloseAsync(player);
                    Task.Run(async () =>
                    {
                        try
                        {
                            SteamDiscordPendingLink link = await _acountLinkingService.BeginLinkFromSteamAsync(player.Steam64);
                            await UniTask.SwitchToMainThread();
                            string token = link.Token;
                            string command = $"/link token:{token}";
                            if (player.IsOnline)
                            {
                                // TODO: replace this with some kind of copy text UI
                                unturnedPlayer.sendBrowserRequest(
                                    _translations.PurchaseButtonNotBoostingLinkDiscordRequest.Translate(player, canUseIMGUI: true),
                                    domain + "/copy-text?text=" + Uri.EscapeDataString(command)
                                );
                            }
                        }
                        catch (Exception ex)
                        {
                            GetLogger().LogError(ex, "Error starting link for player.");
                        }
                    });
                    break;

                case PurchaseButtonState.Rearm:
                    IAmmoStorage? ammoStorage = data.AmmoStorage;
                    if (ammoStorage == null
                        || ammoStorage.AmmoCount == 0
                        || !(data.AmmoStorage == null ? _zoneStore.IsInMainBase(player, player.Team.Faction) : IsWithinRangeOfAmmoStorage(data, player)))
                    {
                        UpdateStatusLabels(kitInfo, false, data, @class, kitIndex, player, kit, player.Component<KitPlayerComponent>());
                        UpdateActionButtons(kit, player, kitInfo, data, kitIndex, @class);
                        break;
                    }

                    _ = CloseAsync(player);
                    Task.Run(async () =>
                    {
                        try
                        {
                            await _rearmService.RearmAsync(player, ammoStorage, player.DisconnectToken);
                        }
                        catch (OperationCanceledException) when (!player.IsOnline) { }
                        catch (Exception ex)
                        {
                            GetLogger().LogError(ex, "Error rearming player.");
                        }
                    });
                    break;
            }
        }
        catch (Exception ex)
        {
            GetLogger().LogError(ex, $"Error unlocking ({@class}, {kitIndex}: {kit.Id}) for {player}.");
        }
        finally
        {
            if (done)
            {
                Interlocked.Decrement(ref data.Operations);
            }
        }
    }

    private bool TryGetTargetKit(Func<KitInfo, UnturnedButton> selector, UnturnedButton button, out Class @class, out int kitIndex, [NotNullWhen(true)] out KitInfo? kitInfo)
    {
        ReadOnlySpan<char> name = button.Name.Span;
        if (name.Length >= 12 && name[4] == 'P')
        {
            // parse numbers from Kit_Panel_#_Kit_#
            int panelIndex;
            if (char.IsDigit(name[11]))
            {
                panelIndex = (name[10] - '0') * 10 + (name[11] - '0');
            }
            else
            {
                panelIndex = name[10] - '0';
            }

            int startIndex = panelIndex >= 10 ? 17 : 16;
            panelIndex = GetClassPanelIndex((Class)panelIndex);
            if (panelIndex >= 0 && name.Length > startIndex && panelIndex < _panels.Length && char.IsDigit(name[startIndex]))
            {
                int index = name[startIndex] - '1';
                PanelKitInfo[] info = _panels[panelIndex].Kits;
                if (index >= 0 && index < info.Length && (object)selector(info[index]) == button)
                {
                    kitInfo = info[index];
                    @class = GetPanelClass(panelIndex);
                    kitIndex = index;
                    return true;
                }
            }
        }
        else if (name.Length >= 10 && name[4] == 'L')
        {
            // parse numbers from Kit_List_#
            int listIndex;
            if (name.Length > 10 && char.IsDigit(name[10]))
            {
                listIndex = (name[9] - '0') * 10 + (name[10] - '0') - 1;
            }
            else
            {
                listIndex = name[9] - '1';
            }

            if (listIndex >= 0 && listIndex < _listResults.Length && (object)selector(_listResults[listIndex]) == button)
            {
                kitInfo = _listResults[listIndex];
                @class = Class.None;
                kitIndex = listIndex;
                return true;
            }
        }

        // Fallback if somehow the above function doesnt work.
        GetLogger().LogWarning($"Unknown kit element name: {name.ToString()}.");
        for (int i = 0; i < _panels.Length; ++i)
        {
            PanelKitInfo[] pnl = _panels[i].Kits;
            for (int j = 0; j < pnl.Length; ++j)
            {
                if ((object)selector(pnl[j]) != button)
                    continue;

                kitInfo = pnl[j];
                @class = GetPanelClass(i);
                kitIndex = j;
                return true;
            }
        }

        @class = Class.None;

        for (int i = 0; i < _listResults.Length; ++i)
        {
            if ((object)selector(_listResults[i]) == button)
            {
                kitInfo = _listResults[i];
                kitIndex = i;
                return true;
            }
        }

        kitInfo = null;
        kitIndex = -1;
        return false;
    }
}
