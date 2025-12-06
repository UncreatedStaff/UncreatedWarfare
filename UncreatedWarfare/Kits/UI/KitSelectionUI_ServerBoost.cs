using System;
using Uncreated.Framework.UI.Presets;
using Uncreated.Warfare.Moderation.Discord;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Kits.UI;

partial class KitSelectionUI
{
    private async Task CheckNitroBoostStatus(WarfarePlayer player, KitSelectionUIData data, CancellationToken token, bool publicKits, bool listKits)
    {
        if (_acountLinkingService == null)
            return;

        GuildStatusResult guildStatus = await _acountLinkingService.IsInGuild(player.Steam64, token).ConfigureAwait(false);

        await UniTask.SwitchToMainThread(token);

        UpdateNitroBoostKits(player, data, guildStatus, isNitroBoosting: false, publicKits, listKits);
    }

    private async Task UpdateNitroBoostKits(WarfarePlayer player, GuildStatusResult guildStatus, bool isNitroBoosting)
    {
        await UniTask.SwitchToMainThread();

        UpdateNitroBoostKits(player, null, guildStatus, isNitroBoosting, true, true);
    }

    private void UpdateNitroBoostKits(WarfarePlayer player, KitSelectionUIData? data, GuildStatusResult guildStatus, bool isNitroBoosting, bool publicKits, bool listKits)
    {
        GameThread.AssertCurrent();

        if (data == null)
        {
            data = GetData<KitSelectionUIData>(player.Steam64);
            if (data == null)
                return;
        }

        if (!data.HasUI)
            return;

        ITransportConnection c = player.Connection;

        if (publicKits)
        {
            for (Class @class = Class.Squadleader; @class < Class.SpecOps; ++@class)
            {
                KitPanel panel = _panels[@class - Class.Squadleader];
                for (int i = 0; i < panel.Kits.Length; ++i)
                {
                    UpdateKit(guildStatus, data, i, isNitroBoosting, @class, panel.Kits[i], player);
                }
            }
        }

        if (listKits)
        {
            for (int i = 0; i < _listResults.Length; ++i)
            {
                UpdateKit(guildStatus, data, i, isNitroBoosting, Class.None, _listResults[i], player);
            }
        }

        return;

        void UpdateKit(GuildStatusResult guildStatus, KitSelectionUIData data, int index, bool isNitroBoosting, Class @class, KitInfo ui, WarfarePlayer player)
        {
            ref KitCacheInformation cacheInfo = ref data.GetCachedState(@class, index);
            if (cacheInfo.Kit is not { RequiresServerBoost: true })
                return;

            if (isNitroBoosting)
            {
                if (cacheInfo.LabelState != StatusState.ServerBoostRequired)
                    return;

                UpdateStatusLabels(ui, c, false, data, @class, index, player, cacheInfo.Kit, player.Component<KitPlayerComponent>());
                return;
            }

            if (guildStatus == GuildStatusResult.Unknown)
            {
                guildStatus = cacheInfo.ButtonState switch
                {
                    PurchaseButtonState.OpenDiscordForBoosts => GuildStatusResult.InGuild,
                    PurchaseButtonState.JoinDiscordGuild => GuildStatusResult.NotInGuild,
                    PurchaseButtonState.BeginLinkDiscordAccount => GuildStatusResult.NotLinked,
                    _ => GuildStatusResult.Unknown
                };
            }

            switch (guildStatus)
            {
                case GuildStatusResult.InGuild:
                    ui.UnlockButton.SetText(player.Connection, _translations.PurchaseButtonNotBoostingOpenDiscord.Translate(player));
                    cacheInfo.ButtonState = PurchaseButtonState.OpenDiscordForBoosts;
                    break;
                case GuildStatusResult.NotInGuild:
                    ui.UnlockButton.SetText(player.Connection, _translations.PurchaseButtonNotBoostingJoinDiscord.Translate(player));
                    cacheInfo.ButtonState = PurchaseButtonState.JoinDiscordGuild;
                    break;
                default:
                    ui.UnlockButton.SetText(player.Connection, _translations.PurchaseButtonNotBoostingLinkDiscord.Translate(player));
                    cacheInfo.ButtonState = PurchaseButtonState.BeginLinkDiscordAccount;
                    break;
            }

            ui.UnlockSection.Show(c);
        }
    }


    private void HandleNitroBoostStatusUpdated(WarfarePlayer? player, CSteamID steam64, bool isNitroBoosting)
    {
        if (player == null)
            return;

        Task.Run(async () =>
        {
            try
            {
                await UpdateNitroBoostKits(player, isNitroBoosting ? GuildStatusResult.InGuild : GuildStatusResult.Unknown, isNitroBoosting).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                GetLogger().LogError(ex, $"Error handling nitro boost status update for {player}.");
            }
        });
    }

    private void HandleAccountLinkUpdated(CSteamID steam64, ulong discordId)
    {
        if (steam64.m_SteamID == 0 || _playerService.GetOnlinePlayerOrNullThreadSafe(steam64.m_SteamID) is not { } player)
            return;

        Task.Run(async () =>
        {
            try
            {
                await UniTask.SwitchToMainThread();

                if (GetData<KitSelectionUIData>(steam64) is not { HasUI: true })
                    return;

                GuildStatusResult status = GuildStatusResult.NotLinked;
                if (discordId != 0)
                {
                    status = _acountLinkingService != null ? await _acountLinkingService.IsInGuild(steam64, player.DisconnectToken).ConfigureAwait(false) : GuildStatusResult.Unknown;
                }

                await UpdateNitroBoostKits(player, status, player.Save.WasNitroBoosting).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                GetLogger().LogError(ex, $"Error handling nitro boost status update for {player}.");
            }
        });
    }

    private void HandleGuildStatusUpdated(CSteamID steam64, ulong discordId, GuildStatusResult status)
    {
        if (steam64.m_SteamID == 0 || _playerService.GetOnlinePlayerOrNullThreadSafe(steam64.m_SteamID) is not { } player)
            return;

        if (discordId == 0)
            status = GuildStatusResult.NotLinked;

        Task.Run(async () =>
        {
            try
            {
                await UniTask.SwitchToMainThread();

                await UpdateNitroBoostKits(player, status, player.Save.WasNitroBoosting).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                GetLogger().LogError(ex, $"Error handling nitro boost status update for {player}.");
            }
        });
    }
}
