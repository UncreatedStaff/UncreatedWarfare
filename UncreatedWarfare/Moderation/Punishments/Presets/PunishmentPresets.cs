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
                new PunishmentPreset(PresetType.Griefing, ModerationEntryType.Warning, "Griefing | L1"),
                new PunishmentPreset(PresetType.Griefing, ModerationEntryType.Ban, "Griefing | L2")
                {
                    PrimaryDuration = TimeSpan.FromHours(3d)
                },
                new PunishmentPreset(PresetType.Griefing, ModerationEntryType.Ban, "Griefing | L3")
                {
                    PrimaryDuration = TimeSpan.FromHours(6d)
                },
                new PunishmentPreset(PresetType.Griefing, ModerationEntryType.Ban, "Griefing | L4")
                {
                    PrimaryDuration = TimeSpan.FromDays(1d)
                },
                new PunishmentPreset(PresetType.Griefing, ModerationEntryType.Ban, "Griefing | L5")
                {
                    PrimaryDuration = TimeSpan.FromDays(3d)
                },
                new PunishmentPreset(PresetType.Griefing, ModerationEntryType.Ban, "Griefing | L6")
                {
                    PrimaryDuration = TimeSpan.FromDays(7d)
                }
            }
        },
        {
            PresetType.Toxicity, new PunishmentPreset[]
            {
                new PunishmentPreset(PresetType.Toxicity, ModerationEntryType.Warning, "Toxicity | L1"),
                new PunishmentPreset(PresetType.Toxicity, ModerationEntryType.Mute, "Toxicity | L2")
                {
                    PrimaryDuration = TimeSpan.FromHours(1d)
                },
                new PunishmentPreset(PresetType.Toxicity, ModerationEntryType.Mute, "Toxicity | L3")
                {
                    PrimaryDuration = TimeSpan.FromHours(6d)
                },
                new PunishmentPreset(PresetType.Toxicity, ModerationEntryType.Mute, "Toxicity | L4")
                {
                    PrimaryDuration = TimeSpan.FromDays(1d)
                },
                new PunishmentPreset(PresetType.Toxicity, ModerationEntryType.Mute, "Toxicity | L5")
                {
                    PrimaryDuration = TimeSpan.FromDays(3d)
                }
            }
        },
        {
            PresetType.Soloing, new PunishmentPreset[]
            {
                new PunishmentPreset(PresetType.Soloing, ModerationEntryType.Warning, "Soloing | L1"),
                new PunishmentPreset(PresetType.Soloing, ModerationEntryType.Kick, "Soloing | L2"),
                new PunishmentPreset(PresetType.Soloing, ModerationEntryType.AssetBan, "Soloing | L3")
                {
                    PrimaryDuration = TimeSpan.FromHours(3d)
                },
                new PunishmentPreset(PresetType.Soloing, ModerationEntryType.AssetBan, "Soloing | L4")
                {
                    PrimaryDuration = TimeSpan.FromHours(6d)
                },
                new PunishmentPreset(PresetType.Soloing, ModerationEntryType.AssetBan, "Soloing | L5")
                {
                    PrimaryDuration = TimeSpan.FromDays(1d)
                },
                new PunishmentPreset(PresetType.Soloing, ModerationEntryType.AssetBan, "Soloing | L6")
                {
                    PrimaryDuration = TimeSpan.FromDays(3d)
                }
            }
        },
        {
            PresetType.AssetWaste, new PunishmentPreset[]
            {
                new PunishmentPreset(PresetType.AssetWaste, ModerationEntryType.Warning, "Asset Waste | L1"),
                new PunishmentPreset(PresetType.AssetWaste, ModerationEntryType.Ban, "Asset Waste | L2")
                {
                    PrimaryDuration = TimeSpan.FromMinutes(30d)
                },
                new PunishmentPreset(PresetType.AssetWaste, ModerationEntryType.Ban, "Asset Waste | L3")
                {
                    PrimaryDuration = TimeSpan.FromHours(6d)
                },
                new PunishmentPreset(PresetType.AssetWaste, ModerationEntryType.Ban, "Asset Waste | L4")
                {
                    PrimaryDuration = TimeSpan.FromHours(12d)
                },
                new PunishmentPreset(PresetType.AssetWaste, ModerationEntryType.Ban, "Asset Waste | L5")
                {
                    PrimaryDuration = TimeSpan.FromDays(1d)
                },
                new PunishmentPreset(PresetType.AssetWaste, ModerationEntryType.Ban, "Asset Waste | L6")
                {
                    PrimaryDuration = TimeSpan.FromDays(3d)
                }
            }
        },
        {
            PresetType.IntentionalTeamkilling, new PunishmentPreset[]
            {
                new PunishmentPreset(PresetType.IntentionalTeamkilling, ModerationEntryType.Warning, "Intentional Teamkilling | L1"),
                new PunishmentPreset(PresetType.IntentionalTeamkilling, ModerationEntryType.Ban, "Intentional Teamkilling | L2")
                {
                    PrimaryDuration = TimeSpan.FromHours(3d)
                },
                new PunishmentPreset(PresetType.IntentionalTeamkilling, ModerationEntryType.Ban, "Intentional Teamkilling | L3")
                {
                    PrimaryDuration = TimeSpan.FromHours(6d)
                },
                new PunishmentPreset(PresetType.IntentionalTeamkilling, ModerationEntryType.Ban, "Intentional Teamkilling | L4")
                {
                    PrimaryDuration = TimeSpan.FromDays(1d)
                },
                new PunishmentPreset(PresetType.IntentionalTeamkilling, ModerationEntryType.Ban, "Intentional Teamkilling | L5")
                {
                    PrimaryDuration = TimeSpan.FromDays(3d)
                },
                new PunishmentPreset(PresetType.IntentionalTeamkilling, ModerationEntryType.Ban, "Intentional Teamkilling | L6")
                {
                    PrimaryDuration = TimeSpan.FromDays(7d)
                }
            }
        },
        {
            PresetType.TargetedHarassment, new PunishmentPreset[]
            {
                new PunishmentPreset(PresetType.TargetedHarassment, ModerationEntryType.Mute, "Targeted Harassment | L1")
                {
                    PrimaryDuration = TimeSpan.FromHours(12d)
                },
                new PunishmentPreset(PresetType.TargetedHarassment, ModerationEntryType.Mute, "Targeted Harassment | L2")
                {
                    PrimaryDuration = TimeSpan.FromDays(2d)
                },
                new PunishmentPreset(PresetType.TargetedHarassment, ModerationEntryType.Mute, "Targeted Harassment | L3")
                {
                    PrimaryDuration = TimeSpan.FromDays(7d),
                    SecondaryModerationType = ModerationEntryType.Ban,
                    SecondaryDuration = TimeSpan.FromDays(1d)
                },
                new PunishmentPreset(PresetType.TargetedHarassment, ModerationEntryType.Mute, "Targeted Harassment | L4")
                {
                    PrimaryDuration = Timeout.InfiniteTimeSpan,
                    SecondaryModerationType = ModerationEntryType.Ban,
                    SecondaryDuration = TimeSpan.FromDays(3d)
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
                    SecondaryDuration = TimeSpan.FromDays(3d)
                },
                new PunishmentPreset(PresetType.Discrimination, ModerationEntryType.Mute, "Discrimination | L2")
                {
                    PrimaryDuration = TimeSpan.FromDays(365.25f / 12f),
                    SecondaryModerationType = ModerationEntryType.Ban,
                    SecondaryDuration = TimeSpan.FromDays(7d)
                },
                new PunishmentPreset(PresetType.Discrimination, ModerationEntryType.Mute, "Discrimination | L3")
                {
                    PrimaryDuration = Timeout.InfiniteTimeSpan,
                    SecondaryModerationType = ModerationEntryType.Ban,
                    SecondaryDuration = TimeSpan.FromDays(14d)
                },
                new PunishmentPreset(PresetType.Discrimination, ModerationEntryType.Ban, "Discrimination | L4")
                {
                    PrimaryDuration = Timeout.InfiniteTimeSpan
                }
            }
        },
        {
            PresetType.Cheating, new PunishmentPreset[]
            {
                new PunishmentPreset(PresetType.Discrimination, ModerationEntryType.Ban, "Cheating | L1")
                {
                    PrimaryDuration = Timeout.InfiniteTimeSpan
                }
            }
        },
        {
            PresetType.DisruptiveBehavior, new PunishmentPreset[]
            {
                new PunishmentPreset(PresetType.DisruptiveBehavior, ModerationEntryType.Mute, "Disruptive Behavior | L1")
                {
                    PrimaryDuration = TimeSpan.FromHours(3d)
                },
                new PunishmentPreset(PresetType.DisruptiveBehavior, ModerationEntryType.Mute, "Disruptive Behavior | L2")
                {
                    PrimaryDuration = TimeSpan.FromHours(6d)
                },
                new PunishmentPreset(PresetType.DisruptiveBehavior, ModerationEntryType.Mute, "Disruptive Behavior | L3")
                {
                    PrimaryDuration = TimeSpan.FromDays(1d)
                },
                new PunishmentPreset(PresetType.DisruptiveBehavior, ModerationEntryType.Mute, "Disruptive Behavior | L4")
                {
                    PrimaryDuration = TimeSpan.FromDays(3d)
                },
                new PunishmentPreset(PresetType.DisruptiveBehavior, ModerationEntryType.Mute, "Disruptive Behavior | L5")
                {
                    PrimaryDuration = TimeSpan.FromDays(7d)
                }
            }
        },
        {
            PresetType.InappropriateProfile, new PunishmentPreset[]
            {
                new PunishmentPreset(PresetType.InappropriateProfile, ModerationEntryType.Warning, "Inappropriate Profile | L1"),
                new PunishmentPreset(PresetType.InappropriateProfile, ModerationEntryType.Kick, "Inappropriate Profile | L2"),
                new PunishmentPreset(PresetType.InappropriateProfile, ModerationEntryType.Ban, "Inappropriate Profile | L3")
                {
                    PrimaryDuration = TimeSpan.FromDays(3d)
                },
                new PunishmentPreset(PresetType.InappropriateProfile, ModerationEntryType.Ban, "Inappropriate Profile | L4")
                {
                    PrimaryDuration = TimeSpan.FromDays(7d)
                }
            }
        },
        {
            PresetType.BypassingPunishment, new PunishmentPreset[]
            {
                new PunishmentPreset(PresetType.BypassingPunishment, ModerationEntryType.Ban, "Bypassing Punishment | L1")
                {
                    PrimaryDuration = Timeout.InfiniteTimeSpan
                }
            }
        }
    };
}

public sealed class PunishmentPreset
{
    [JsonPropertyName("preset_type")]
    public PresetType PresetType { get; }

    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DefaultMessage { get; }

    [JsonPropertyName("primary_moderation_type")]
    public ModerationEntryType PrimaryModerationType { get; }

    [JsonPropertyName("primary_duration")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public TimeSpan? PrimaryDuration { get; set; }

    [JsonPropertyName("secondary_moderation_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public ModerationEntryType? SecondaryModerationType { get; set; }

    [JsonPropertyName("secondary_duration")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public TimeSpan? SecondaryDuration { get; set; }

    public PunishmentPreset(PresetType presetType, ModerationEntryType modType, string? defaultMessage)
    {
        PresetType = presetType;
        PrimaryModerationType = modType;
        DefaultMessage = defaultMessage;
    }

    public PunishmentPreset() { }
}