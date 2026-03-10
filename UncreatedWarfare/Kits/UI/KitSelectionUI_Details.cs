using System;
using System.Collections.Immutable;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Presets;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Kits.UI;

partial class KitSelectionUI
{
    private const string HeatmapSkull = "ᔁ";
    private const string HeatmapTorso = "ᔃ";
    private const string HeatmapRightArm = "ᔄ";
    private const string HeatmapLeftArm = "ᔅ";
    private const string HeatmapRightLeg = "ᔆ";
    private const string HeatmapLeftLeg = "ᔇ";

    private static readonly KnownStatType[] DefaultStats =
    [
        KnownStatType.KDR, KnownStatType.Playtime, KnownStatType.Teamkills, KnownStatType.DamageDealt, KnownStatType.Suicides, KnownStatType.MeleeKills
    ];

    private static readonly KnownStatType[][] Stats =
    [
        // None
        DefaultStats,

        // Unarmed
        DefaultStats,

        // Squadleader
        [ KnownStatType.KDR, KnownStatType.Playtime, KnownStatType.FOBsBuilt, KnownStatType.Teamkills, KnownStatType.DamageDealt, KnownStatType.MeleeKills ],

        // Rifleman
        DefaultStats,
        
        // Medic
        [ KnownStatType.KDR, KnownStatType.Playtime, KnownStatType.Revives, KnownStatType.HealthAided, KnownStatType.Teamkills, KnownStatType.DamageDealt, KnownStatType.MeleeKills ],
        
        // Breacher
        [ KnownStatType.KDR, KnownStatType.Playtime, KnownStatType.FOBsDestroyed, KnownStatType.Teamkills, KnownStatType.DamageDealt, KnownStatType.Suicides, KnownStatType.MeleeKills ],
        
        // AutomaticRifleman
        [ KnownStatType.KDR, KnownStatType.Playtime, KnownStatType.VehiclesDestroyed, KnownStatType.Teamkills, KnownStatType.DamageDealt, KnownStatType.MeleeKills ],
        
        // Grenadier
        [ KnownStatType.KDR, KnownStatType.Playtime, KnownStatType.VehiclesDestroyed, KnownStatType.Teamkills, KnownStatType.DamageDealt, KnownStatType.MeleeKills ],
        
        // MachineGunner
        [ KnownStatType.KDR, KnownStatType.Playtime, KnownStatType.VehiclesDestroyed, KnownStatType.Teamkills, KnownStatType.DamageDealt, KnownStatType.MeleeKills ],
        
        // LAT
        [ KnownStatType.KDR, KnownStatType.Playtime, KnownStatType.VehiclesDestroyed, KnownStatType.Teamkills, KnownStatType.DamageDealt, KnownStatType.Suicides, KnownStatType.MeleeKills ],
        
        // HAT
        [ KnownStatType.KDR, KnownStatType.Playtime, KnownStatType.VehiclesDestroyed, KnownStatType.Teamkills, KnownStatType.DamageDealt, KnownStatType.Suicides, KnownStatType.MeleeKills ],
        
        // Marksman
        [ KnownStatType.KDR, KnownStatType.Playtime, KnownStatType.AverageKillDistance, KnownStatType.HighestKillDistance, KnownStatType.Teamkills, KnownStatType.DamageDealt, KnownStatType.MeleeKills ],
        
        // Sniper
        [ KnownStatType.KDR, KnownStatType.Playtime, KnownStatType.AverageKillDistance, KnownStatType.HighestKillDistance, KnownStatType.Teamkills, KnownStatType.DamageDealt, KnownStatType.MeleeKills ],
        
        // APRifleman
        [ KnownStatType.KDR, KnownStatType.Playtime, KnownStatType.VehiclesDestroyed, KnownStatType.Teamkills, KnownStatType.DamageDealt, KnownStatType.Suicides, KnownStatType.MeleeKills ],
        
        // CombatEngineer
        [ KnownStatType.KDR, KnownStatType.Playtime, KnownStatType.VehiclesDestroyed, KnownStatType.FOBsDestroyed, KnownStatType.Teamkills, KnownStatType.DamageDealt, KnownStatType.MeleeKills ],
        
        // Crewman
        [ KnownStatType.KDR, KnownStatType.Playtime, KnownStatType.VehiclesDestroyed, KnownStatType.KillsWithVehicle, KnownStatType.FOBsDestroyed, KnownStatType.Teamkills, KnownStatType.DamageDealt, KnownStatType.MeleeKills ],
        
        // Pilot
        [ KnownStatType.KDR, KnownStatType.Playtime, KnownStatType.VehiclesDestroyed, KnownStatType.KillsWithVehicle, KnownStatType.FOBsDestroyed, KnownStatType.Teamkills, KnownStatType.DamageDealt, KnownStatType.MeleeKills ],
        
        // SpecOps
        DefaultStats
    ];

    private static readonly Func<KitSelectionUITranslations, Translation>[] StatTranslations =
    [
        // KDR
        t => t.StatisticKDR,
        // Playtime
        t => t.StatisticPlaytime,
        // Teamkills
        t => t.StatisticTeamkills,
        // DamageDealt
        t => t.StatisticDamageDealt,
        // VehiclesDestroyed
        t => t.StatisticVehiclesDestroyed,
        // FOBsDestroyed
        t => t.StatisticFOBsDestroyed,
        // FOBsBuilt
        t => t.StatisticFOBsBuilt,
        // Revives
        t => t.StatisticRevives,
        // MeleeKills
        t => t.StatisticMeleeKills,
        // AverageKillDistance
        t => t.StatisticAverageKillDistance,
        // HighestKillDistance
        t => t.StatisticHighestKillDistance,
        // Suicides
        t => t.StatisticSuicides,
        // HealthAided
        t => t.StatisticHealthAided,
        // KillsWithVehicle
        t => t.StatisticKillsWithVehicle
    ];

    private void HandleClickKitBackground(UnturnedButton button, Player unturnedPlayer)
    {
        if (!TryGetTargetKit(x => x.Root, button, out Class @class, out int index, out _))
        {
            return;
        }

        WarfarePlayer player = _playerService.GetOnlinePlayer(unturnedPlayer);

        KitSelectionUIData data = GetOrAddData(player);

        Kit? cachedKit = data.GetCachedState(@class, index).Kit;
        if (cachedKit == null)
        {
            return;
        }

        UniTask.Create((@this: this, player, cachedKit), static async args =>
        {
            try
            {
                await args.@this.SendKitDetailsAsync(args.player, args.cachedKit);
            }
            catch (Exception ex)
            {
                args.@this.GetLogger().LogError(
                    ex,
                    $"Error sending kit detail for kit {args.cachedKit.Id} ({args.cachedKit.Key})"
                );
            }
        });
    }

    public async UniTask SendKitDetailsAsync(WarfarePlayer player, Kit kit, CancellationToken token = default)
    {
        Task<IReadOnlyDictionary<ELimb, double>> getHeatmapInfo = _kitStatisticService.GetHeatmapDataForKit(player.Steam64, kit.Key, token: token);

        try
        {
            _ = kit.Items;
            _ = kit.Faction;
            _ = kit.Translations;
        }
        catch (NotIncludedException)
        {
            Kit? refreshed = await _kitDataStore.QueryKitAsync(kit.Key, KitInclude.UI, token);
            if (refreshed == null)
                throw;

            kit = refreshed;
        }

        await UniTask.SwitchToMainThread(token);

        ITransportConnection c = player.Connection;

        KitSelectionUIData data = GetOrAddData(player);
        if (!data.HasUI)
        {
            await OpenAsync(player, token);
            await UniTask.SwitchToMainThread(token);
        }

        int version = Interlocked.Increment(ref data.SelectedKitVersion);

        data.SelectedKit = kit;

        if (!data.HasDetailPanel)
        {
            data.HasDetailPanel = true;
            _detailPlaceholder.Hide(c);
            _detailPanelRoot.Show(c);
        }

        _detailPanelName.SetText(c, kit.GetDisplayName(player.Locale.LanguageInfo, true));
        _detailPanelClass.SetText(c, kit.Class.GetIconString());
        _detailPanelFlag.SetText(c, kit.Faction.Sprite);
        _detailPanelId.SetText(c, kit.Id);

        KnownStatType[] stats = Stats[(int)kit.Class];
        int statIndex = 0;
        for (; statIndex < stats.Length; ++statIndex)
        {
            KitDetailStatistic statUi = _detailPanelStatistics[statIndex];
            statUi.Name.SetText(c, StatTranslations[(int)stats[statIndex]](_translations).Translate(player));
            statUi.Value.SetText(c, "...");

            if (statIndex >= data.ActiveStatCount)
                statUi.Show(c);
        }

        for (; statIndex < data.ActiveStatCount; ++statIndex)
        {
            _detailPanelStatistics[statIndex].Hide(c);
        }

        data.ActiveStatCount = stats.Length;

        // start collecting kit stats
        Task<string[]> kitStats = CollectKitStats(player, kit, stats, token);

        ImmutableArray<ItemDescriptor> itemDescriptors = kit.GetItemDescriptors(
            data.Team ?? Team.NoTeam,
            _kitItemResolver,
            _iconProvider,
            _weaponTextService
        );

        int itemIndex = 0;
        int itemCt = Math.Min(itemDescriptors.Length, _detailIncludeLabels.Length);
        for (; itemIndex < itemCt; ++itemIndex)
        {
            ItemDescriptor desc = itemDescriptors[itemIndex];

            CountIncludeLabel lbl = _detailIncludeLabels[itemIndex];

            lbl.Name.SetText(c, desc.ItemName);
            if (desc.Amount > 1)
            {
                lbl.Count.SetText(c, desc.Amount.ToString(player.Locale.CultureInfo));
                lbl.Count.Show(c);
            }
            else
            {
                lbl.Count.Hide(c);
            }

            lbl.Icon.SetText(c, desc.Icon);
            lbl.Show(c);
            ImmutableArray<ItemDescriptorAttachment> attachments = desc.Attachments;

            if (itemIndex >= 3)
                continue;

            IncludeLabel[] attachmentLabels = itemIndex switch
            {
                0 => _detailPrimaryAttachments,
                1 => _detailSecondaryAttachments,
                _ => _detailTertiaryAttachments,
            };

            int attachmentCt = attachments.IsDefaultOrEmpty ? 0 : attachments.Length;
            int mask = 0;
            for (int j = 0; j < attachmentCt; ++j)
            {
                ItemDescriptorAttachment attachment = attachments[j];
                IncludeLabel attachmentLabel = attachmentLabels[_attachmentMap[(int)attachment.AttachmentType]];

                mask |= 1 << ((int)attachment.AttachmentType / 2);

                attachmentLabel.Icon.SetText(c, attachment.Icon ?? GetAttachmentIcon(j));
                attachmentLabel.Name.SetText(c, attachment.ItemName);
                attachmentLabel.Show(c);
            }

            for (int j = 0; j < 5; ++j)
            {
                if ((mask & (1 << ((int)_inverseAttachmentMap[j] / 2))) != 0)
                    continue;

                attachmentLabels[j].Hide(c);
            }
        }

        for (; itemIndex < data.ActiveItemDescriptorCount; ++itemIndex)
        {
            _detailIncludeLabels[itemIndex].Hide(c);
        }

        data.ActiveItemDescriptorCount = itemCt;

        bool didClear = false;
        if (!getHeatmapInfo.IsCompleted)
        {
            _detailResetHeatmapLogic.Show(c);
            didClear = true;
        }

        IReadOnlyDictionary<ELimb, double> heatmapInfo = await getHeatmapInfo.ConfigureAwait(false);

        await UniTask.SwitchToMainThread(token);

        // new kit selected
        if (data.SelectedKitVersion != version || !data.HasUI)
            return;

        // the player doesn't have colliders for a lot of these limbs which is why they're combined

        double armRight = heatmapInfo.GetValueOrDefault(ELimb.RIGHT_ARM) + heatmapInfo.GetValueOrDefault(ELimb.RIGHT_HAND);
        double armLeft = heatmapInfo.GetValueOrDefault(ELimb.LEFT_ARM) + heatmapInfo.GetValueOrDefault(ELimb.LEFT_HAND);

        double legRight = heatmapInfo.GetValueOrDefault(ELimb.RIGHT_LEG) + heatmapInfo.GetValueOrDefault(ELimb.RIGHT_FOOT);
        double legLeft = heatmapInfo.GetValueOrDefault(ELimb.LEFT_LEG) + heatmapInfo.GetValueOrDefault(ELimb.LEFT_FOOT);

        double torso = heatmapInfo.GetValueOrDefault(ELimb.SPINE)
                       + heatmapInfo.GetValueOrDefault(ELimb.LEFT_FRONT)
                       + heatmapInfo.GetValueOrDefault(ELimb.RIGHT_FRONT)
                       + heatmapInfo.GetValueOrDefault(ELimb.LEFT_BACK)
                       + heatmapInfo.GetValueOrDefault(ELimb.RIGHT_BACK);

        double skull = heatmapInfo.GetValueOrDefault(ELimb.SKULL);

        if (armLeft != 0 || armRight != 0 || legLeft != 0 || legRight != 0 || torso != 0 || skull == 0)
        {
            _detailHeatmap.SkullLabel.SetText(c, skull.ToString("P0", player.Locale.CultureInfo));
            _detailHeatmap.TorsoLabel.SetText(c, torso.ToString("P0", player.Locale.CultureInfo));
            _detailHeatmap.RightArmLabel.SetText(c, armRight.ToString("P0", player.Locale.CultureInfo));
            _detailHeatmap.LeftArmLabel.SetText(c, armLeft.ToString("P0", player.Locale.CultureInfo));
            _detailHeatmap.RightLegLabel.SetText(c, legRight.ToString("P0", player.Locale.CultureInfo));
            _detailHeatmap.LeftLegLabel.SetText(c, legLeft.ToString("P0", player.Locale.CultureInfo));

            SetHeatmapColor(c, skull, _detailHeatmap.Skull, HeatmapSkull);
            SetHeatmapColor(c, torso, _detailHeatmap.Torso, HeatmapTorso);
            SetHeatmapColor(c, armRight, _detailHeatmap.RightArm, HeatmapRightArm);
            SetHeatmapColor(c, armLeft, _detailHeatmap.LeftArm, HeatmapLeftArm);
            SetHeatmapColor(c, legRight, _detailHeatmap.RightLeg, HeatmapRightLeg);
            SetHeatmapColor(c, legLeft, _detailHeatmap.LeftLeg, HeatmapLeftLeg);
        }
        else if (!didClear)
        {
            _detailResetHeatmapLogic.Show(c);
        }


        string[] statValues = await kitStats.ConfigureAwait(false);

        await UniTask.SwitchToMainThread(token);

        // new kit selected
        if (data.SelectedKitVersion != version || !data.HasUI)
            return;

        for (int i = 0; i < statValues.Length; ++i)
        {
            _detailPanelStatistics[i].Value.SetText(c, statValues[i]);
        }
    }

    private async Task<string[]> CollectKitStats(WarfarePlayer player, Kit kit, KnownStatType[] stats, CancellationToken token)
    {
        IReadOnlyList<double> array = await _kitStatisticService.BulkQueryStats(stats, player.Steam64, kit.Key, token: token);
        string[] outputArray = new string[stats.Length];
        for (int i = 0; i < stats.Length; ++i)
        {
            switch (stats[i])
            {
                // the array should look like [ ..., KDR, ..., Kills, Deaths ] when KDR is in there
                case KnownStatType.KDR when array.Count == stats.Length + 2:
                    outputArray[i] = $"{array[^2].ToString("N0", player.Locale.CultureInfo)}<#444> / </color>{array[^1].ToString("N0", player.Locale.CultureInfo)}<#444> = </color>{array[i].ToString("F2", player.Locale.CultureInfo)}";
                    break;

                case KnownStatType.Playtime:
                    TimeSpan playtime = TimeSpan.FromSeconds(array[i]);
                    outputArray[i] = FormattingUtility.ToTimeString(playtime, figures: 2, space: true);
                    break;

                case KnownStatType.HighestKillDistance:
                case KnownStatType.AverageKillDistance:
                    outputArray[i] = array[i].ToString("F1", player.Locale.CultureInfo);
                    break;

                case KnownStatType.HealthAided:
                case KnownStatType.DamageDealt:
                    if (Math.Abs(Math.Round(array[i]) - array[i]) < 0.05)
                        outputArray[i] = array[i].ToString("N0", player.Locale.CultureInfo);
                    else
                        outputArray[i] = array[i].ToString("N1", player.Locale.CultureInfo);
                    break;

                default:
                    outputArray[i] = array[i].ToString("N0", player.Locale.CultureInfo);
                    break;
            }
        }

        return outputArray;
    }

    private static void SetHeatmapColor(ITransportConnection c, double p, UnturnedLabel label, string character)
    {
        if (p == 0)
        {
            label.SetText(c, character);
        }
        else
        {
            string coloredValue = TranslationFormattingUtility.Colorize(character, InterpolateHeatmapColor(p), imgui: true);
            label.SetText(c, coloredValue);
        }
    }

    private static Color32 InterpolateHeatmapColor(double p)
    {
        const byte minBrightness = 102;
        const byte maxBrightness = 255;
        byte gb = (byte)Math.Round(minBrightness + (maxBrightness - minBrightness) * (1d - p));
        return new Color32(255, gb, gb, 255);
    }
}
