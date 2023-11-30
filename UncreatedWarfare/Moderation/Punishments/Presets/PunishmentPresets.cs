using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;

namespace Uncreated.Warfare.Moderation.Punishments.Presets;
public static class PunishmentPresets
{
    public static bool TryGetPreset(PresetType type, out PunishmentPreset[] presets) => Presets.TryGetValue(type, out presets);

    public static readonly Dictionary<PresetType, PunishmentPreset[]> Presets = new Dictionary<PresetType, PunishmentPreset[]>
    {
        {
            PresetType.Griefing, new PunishmentPreset[]
            {
                new PunishmentPreset(PresetType.Griefing, ModerationEntryType.Warning, "Griefing | L1")
                {
                    Reputation = -80
                },
                new PunishmentPreset(PresetType.Griefing, ModerationEntryType.Ban, "Griefing | L2")
                {
                    PrimaryDuration = TimeSpan.FromHours(3d),
                    Reputation = -120
                },
                new PunishmentPreset(PresetType.Griefing, ModerationEntryType.Ban, "Griefing | L3")
                {
                    PrimaryDuration = TimeSpan.FromHours(6d),
                    Reputation = -300
                },
                new PunishmentPreset(PresetType.Griefing, ModerationEntryType.Ban, "Griefing | L4")
                {
                    PrimaryDuration = TimeSpan.FromDays(1d),
                    Reputation = -400
                },
                new PunishmentPreset(PresetType.Griefing, ModerationEntryType.Ban, "Griefing | L5")
                {
                    PrimaryDuration = TimeSpan.FromDays(3d),
                    Reputation = -600
                },
                new PunishmentPreset(PresetType.Griefing, ModerationEntryType.Ban, "Griefing | L6")
                {
                    PrimaryDuration = TimeSpan.FromDays(7d),
                    Reputation = -800
                }
            }
        },
        {
            PresetType.Toxicity, new PunishmentPreset[]
            {
                new PunishmentPreset(PresetType.Toxicity, ModerationEntryType.Warning, "Toxicity | L1")
                {
                    Reputation = -25
                },
                new PunishmentPreset(PresetType.Toxicity, ModerationEntryType.Mute, "Toxicity | L2")
                {
                    PrimaryDuration = TimeSpan.FromHours(1d),
                    Reputation = -60
                },
                new PunishmentPreset(PresetType.Toxicity, ModerationEntryType.Mute, "Toxicity | L3")
                {
                    PrimaryDuration = TimeSpan.FromHours(6d),
                    Reputation = -120
                },
                new PunishmentPreset(PresetType.Toxicity, ModerationEntryType.Mute, "Toxicity | L4")
                {
                    PrimaryDuration = TimeSpan.FromDays(1d),
                    Reputation = -250
                },
                new PunishmentPreset(PresetType.Toxicity, ModerationEntryType.Mute, "Toxicity | L5")
                {
                    PrimaryDuration = TimeSpan.FromDays(3d),
                    Reputation = -325
                }
            }
        },
        {
            PresetType.Soloing, new PunishmentPreset[]
            {
                new PunishmentPreset(PresetType.Soloing, ModerationEntryType.Warning, "Soloing | L1")
                {
                    Reputation = -15
                },
                new PunishmentPreset(PresetType.Soloing, ModerationEntryType.Kick, "Soloing | L2")
                {
                    Reputation = -30
                },
                new PunishmentPreset(PresetType.Soloing, ModerationEntryType.AssetBan, "Soloing | L3")
                {
                    PrimaryDuration = TimeSpan.FromHours(3d),
                    Reputation = -50
                },
                new PunishmentPreset(PresetType.Soloing, ModerationEntryType.AssetBan, "Soloing | L4")
                {
                    PrimaryDuration = TimeSpan.FromHours(6d),
                    Reputation = -80
                },
                new PunishmentPreset(PresetType.Soloing, ModerationEntryType.AssetBan, "Soloing | L5")
                {
                    PrimaryDuration = TimeSpan.FromDays(1d),
                    Reputation = -125
                },
                new PunishmentPreset(PresetType.Soloing, ModerationEntryType.AssetBan, "Soloing | L6")
                {
                    PrimaryDuration = TimeSpan.FromDays(3d),
                    Reputation = -200
                }
            }
        },
        {
            PresetType.AssetWaste, new PunishmentPreset[]
            {
                new PunishmentPreset(PresetType.AssetWaste, ModerationEntryType.Warning, "Asset Waste | L1")
                {
                    Reputation = -30
                },
                new PunishmentPreset(PresetType.AssetWaste, ModerationEntryType.Ban, "Asset Waste | L2")
                {
                    PrimaryDuration = TimeSpan.FromMinutes(30d),
                    Reputation = -50
                },
                new PunishmentPreset(PresetType.AssetWaste, ModerationEntryType.Ban, "Asset Waste | L3")
                {
                    PrimaryDuration = TimeSpan.FromHours(6d),
                    Reputation = -120
                },
                new PunishmentPreset(PresetType.AssetWaste, ModerationEntryType.Ban, "Asset Waste | L4")
                {
                    PrimaryDuration = TimeSpan.FromHours(12d),
                    Reputation = -180
                },
                new PunishmentPreset(PresetType.AssetWaste, ModerationEntryType.Ban, "Asset Waste | L5")
                {
                    PrimaryDuration = TimeSpan.FromDays(1d),
                    Reputation = -250
                },
                new PunishmentPreset(PresetType.AssetWaste, ModerationEntryType.Ban, "Asset Waste | L6")
                {
                    PrimaryDuration = TimeSpan.FromDays(3d),
                    Reputation = -350
                }
            }
        },
        {
            PresetType.IntentionalTeamkilling, new PunishmentPreset[]
            {
                new PunishmentPreset(PresetType.IntentionalTeamkilling, ModerationEntryType.Warning, "Intentional Teamkilling | L1")
                {
                    Reputation = -50
                },
                new PunishmentPreset(PresetType.IntentionalTeamkilling, ModerationEntryType.Ban, "Intentional Teamkilling | L2")
                {
                    PrimaryDuration = TimeSpan.FromHours(3d),
                    Reputation = -125
                },
                new PunishmentPreset(PresetType.IntentionalTeamkilling, ModerationEntryType.Ban, "Intentional Teamkilling | L3")
                {
                    PrimaryDuration = TimeSpan.FromHours(6d),
                    Reputation = -200
                },
                new PunishmentPreset(PresetType.IntentionalTeamkilling, ModerationEntryType.Ban, "Intentional Teamkilling | L4")
                {
                    PrimaryDuration = TimeSpan.FromDays(1d),
                    Reputation = -400
                },
                new PunishmentPreset(PresetType.IntentionalTeamkilling, ModerationEntryType.Ban, "Intentional Teamkilling | L5")
                {
                    PrimaryDuration = TimeSpan.FromDays(3d),
                    Reputation = -500
                },
                new PunishmentPreset(PresetType.IntentionalTeamkilling, ModerationEntryType.Ban, "Intentional Teamkilling | L6")
                {
                    PrimaryDuration = TimeSpan.FromDays(7d),
                    Reputation = -600
                }
            }
        },
        {
            PresetType.TargetedHarassment, new PunishmentPreset[]
            {
                new PunishmentPreset(PresetType.TargetedHarassment, ModerationEntryType.Mute, "Targeted Harassment | L1")
                {
                    PrimaryDuration = TimeSpan.FromHours(12d),
                    Reputation = -250
                },
                new PunishmentPreset(PresetType.TargetedHarassment, ModerationEntryType.Mute, "Targeted Harassment | L2")
                {
                    PrimaryDuration = TimeSpan.FromDays(2d),
                    Reputation = -500
                },
                new PunishmentPreset(PresetType.TargetedHarassment, ModerationEntryType.Mute, "Targeted Harassment | L3")
                {
                    PrimaryDuration = TimeSpan.FromDays(7d),
                    SecondaryModerationType = ModerationEntryType.Ban,
                    SecondaryDuration = TimeSpan.FromDays(1d),
                    Reputation = -1000
                },
                new PunishmentPreset(PresetType.TargetedHarassment, ModerationEntryType.Mute, "Targeted Harassment | L4")
                {
                    PrimaryDuration = Timeout.InfiniteTimeSpan,
                    SecondaryModerationType = ModerationEntryType.Ban,
                    SecondaryDuration = TimeSpan.FromDays(3d),
                    Reputation = -1250
                }
            }
        },
        {
            PresetType.Discrimination, new PunishmentPreset[]
            {
                new PunishmentPreset(PresetType.Discrimination, ModerationEntryType.Mute, "Discrimination | L1")
                {
                    PrimaryDuration = TimeSpan.FromDays(14d),
                    SecondaryModerationType = ModerationEntryType.Ban,
                    SecondaryDuration = TimeSpan.FromDays(3d),
                    Reputation = -250
                },
                new PunishmentPreset(PresetType.Discrimination, ModerationEntryType.Mute, "Discrimination | L2")
                {
                    PrimaryDuration = TimeSpan.FromDays(365.25f / 12f),
                    SecondaryModerationType = ModerationEntryType.Ban,
                    SecondaryDuration = TimeSpan.FromDays(7d),
                    Reputation = -500
                },
                new PunishmentPreset(PresetType.Discrimination, ModerationEntryType.Mute, "Discrimination | L3")
                {
                    PrimaryDuration = Timeout.InfiniteTimeSpan,
                    SecondaryModerationType = ModerationEntryType.Ban,
                    SecondaryDuration = TimeSpan.FromDays(14d),
                    Reputation = -750
                },
                new PunishmentPreset(PresetType.Discrimination, ModerationEntryType.Ban, "Discrimination | L4")
                {
                    PrimaryDuration = Timeout.InfiniteTimeSpan,
                    Reputation = -1000
                }
            }
        },
        {
            PresetType.Cheating, new PunishmentPreset[]
            {
                new PunishmentPreset(PresetType.Cheating, ModerationEntryType.Ban, "Cheating | L1")
                {
                    PrimaryDuration = Timeout.InfiniteTimeSpan,
                    Reputation = -50000
                }
            }
        },
        {
            PresetType.DisruptiveBehavior, new PunishmentPreset[]
            {
                new PunishmentPreset(PresetType.DisruptiveBehavior, ModerationEntryType.Mute, "Disruptive Behavior | L1")
                {
                    PrimaryDuration = TimeSpan.FromHours(3d),
                    Reputation = -50
                },
                new PunishmentPreset(PresetType.DisruptiveBehavior, ModerationEntryType.Mute, "Disruptive Behavior | L2")
                {
                    PrimaryDuration = TimeSpan.FromHours(6d),
                    Reputation = -100
                },
                new PunishmentPreset(PresetType.DisruptiveBehavior, ModerationEntryType.Mute, "Disruptive Behavior | L3")
                {
                    PrimaryDuration = TimeSpan.FromDays(1d),
                    Reputation = -150
                },
                new PunishmentPreset(PresetType.DisruptiveBehavior, ModerationEntryType.Mute, "Disruptive Behavior | L4")
                {
                    PrimaryDuration = TimeSpan.FromDays(3d),
                    Reputation = -250
                },
                new PunishmentPreset(PresetType.DisruptiveBehavior, ModerationEntryType.Mute, "Disruptive Behavior | L5")
                {
                    PrimaryDuration = TimeSpan.FromDays(7d),
                    Reputation = -400
                }
            }
        },
        {
            PresetType.InappropriateProfile, new PunishmentPreset[]
            {
                new PunishmentPreset(PresetType.InappropriateProfile, ModerationEntryType.Warning, "Inappropriate Profile | L1"),
                new PunishmentPreset(PresetType.InappropriateProfile, ModerationEntryType.Kick, "Inappropriate Profile | L2")
                {
                    Reputation = -30
                },
                new PunishmentPreset(PresetType.InappropriateProfile, ModerationEntryType.Ban, "Inappropriate Profile | L3")
                {
                    PrimaryDuration = TimeSpan.FromDays(3d),
                    Reputation = -80
                },
                new PunishmentPreset(PresetType.InappropriateProfile, ModerationEntryType.Ban, "Inappropriate Profile | L4")
                {
                    PrimaryDuration = TimeSpan.FromDays(7d),
                    Reputation = -150
                }
            }
        },
        {
            PresetType.BypassingPunishment, new PunishmentPreset[]
            {
                new PunishmentPreset(PresetType.BypassingPunishment, ModerationEntryType.Ban, "Bypassing Punishment | L1")
                {
                    PrimaryDuration = Timeout.InfiniteTimeSpan,
                    Reputation = -500
                }
            }
        }
    };
}

public sealed class PunishmentPreset
{
    [JsonPropertyName("preset_type")]
    public PresetType PresetType { get; set; }

    [JsonPropertyName("message")]
    public string? DefaultMessage { get; set; }

    [JsonPropertyName("primary_moderation_type")]
    public ModerationEntryType PrimaryModerationType { get; set; }

    [JsonPropertyName("primary_duration")]
    public TimeSpan? PrimaryDuration { get; set; }

    [JsonPropertyName("secondary_moderation_type")]
    public ModerationEntryType? SecondaryModerationType { get; set; }

    [JsonPropertyName("secondary_duration")]
    public TimeSpan? SecondaryDuration { get; set; }

    [JsonPropertyName("reputation")]
    public double Reputation { get; set; }

    public PunishmentPreset(PresetType presetType, ModerationEntryType modType, string? defaultMessage)
    {
        PresetType = presetType;
        PrimaryModerationType = modType;
        DefaultMessage = defaultMessage;
    }

    public PunishmentPreset() { }
}