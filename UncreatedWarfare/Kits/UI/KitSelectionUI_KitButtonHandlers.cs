using System;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Presets;
using Uncreated.Warfare.Interaction.Requests;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

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
                SendKitInfo(_listResults[0], player, favoritedKit, player.Component<KitPlayerComponent>(), data, false, index: 0);

                for (int i = 1; i < _listResults.Length; ++i)
                {
                    ref KitCacheInformation info = ref data.GetCachedState(i);
                    if (info.Kit == null)
                        break;

                    info = default;
                    _listResults[i].Hide(player.Connection);
                }

                _ = UpdateSearchAsync(player, data);
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
                    await UpdateKitAsync(favKit, player, player.DisconnectToken);
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
                if (!await _kitFavoriteService.RemoveFavorite(player.Steam64, favKit.Key))
                {
                    return;
                }

                await UniTask.SwitchToMainThread();
                UpdateFavoriteList(player, data, await GetFavoriteKits(player, player.DisconnectToken), false);
                await UpdateKitAsync(favKit, player, player.DisconnectToken);
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
        if (!TryGetTargetKit(x => x.FavoriteButton, button, out Class @class, out int kitIndex, out KitInfo? kitInfo))
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
                if (!await _kitFavoriteService.AddFavorite(player.Steam64, kit.Key))
                {
                    return;
                }

                await UniTask.SwitchToMainThread();
                UpdateFavoriteList(player, data, await GetFavoriteKits(player, player.DisconnectToken), false);
                SendKitInfo(kitInfo, player, kit, player.Component<KitPlayerComponent>(), data, false, kitIndex, @class);
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
        if (!TryGetTargetKit(x => x.UnfavoriteButton, button, out Class @class, out int kitIndex, out KitInfo? kitInfo))
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
                if (!await _kitFavoriteService.RemoveFavorite(player.Steam64, kit.Key))
                {
                    return;
                }

                await UniTask.SwitchToMainThread();
                UpdateFavoriteList(player, data, await GetFavoriteKits(player, player.DisconnectToken), false);
                SendKitInfo(kitInfo, player, kit, player.Component<KitPlayerComponent>(), data, false, kitIndex, @class);
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
                requested = await _kitRequestService.RequestAsync(player, kit, new RequestCommandResultHandler(_chatService, _requestTranslations), player.DisconnectToken);
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

        // todo: Interlocked.Increment(ref data.Operations);

        ref KitCacheInformation cache = ref data.GetCachedState(@class, kitIndex);

        Kit? kit = cache.Kit;
        if (kit == null)
            return;

        //Task.Run(async () =>
        //{
        //    try
        //    {
        //        if (!await _kitFavoriteService.AddFavorite(player.Steam64, kit.Key))
        //        {
        //            return;
        //        }
        //
        //        await UniTask.SwitchToMainThread();
        //        UpdateFavoriteList(player, data, await GetFavoriteKits(player, player.DisconnectToken), false);
        //    }
        //    catch (Exception ex)
        //    {
        //        GetLogger().LogError(ex, $"Error adding favorite to ({@class}, {kitIndex}: {kit.Id}) for {player}.");
        //    }
        //    finally
        //    {
        //        Interlocked.Decrement(ref data.Operations);
        //    }
        //});
    }

    private bool TryGetTargetKit(Func<KitInfo, UnturnedButton> selector, UnturnedButton button, out Class @class, out int kitIndex, [NotNullWhen(true)] out KitInfo? kitInfo)
    {
        ReadOnlySpan<char> name = button.Name.Span;
        if (name.Length >= 12 && name[4] == 'P') // Kit_Panel_#_Kit_#
        {
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
            if (name.Length > startIndex && panelIndex < _panels.Length && char.IsDigit(name[startIndex]))
            {
                int index = name[startIndex] - '0';
                PanelKitInfo[] info = _panels[panelIndex].Kits;
                if (index < info.Length && (object)selector(info[index]) == button)
                {
                    kitInfo = info[index];
                    @class = GetPanelClass(panelIndex);
                    kitIndex = index;
                    return true;
                }
            }
        }
        else if (name.Length >= 11 && name[4] == 'L') // Kit_List_#
        {
            int listIndex;
            if (char.IsDigit(name[11]))
            {
                listIndex = (name[9] - '0') * 10 + (name[10] - '0');
            }
            else
            {
                listIndex = name[9] - '0';
            }

            if (listIndex < _listResults.Length && (object)selector(_listResults[listIndex]) == button)
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
