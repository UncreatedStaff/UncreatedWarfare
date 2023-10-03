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
                new PunishmentPreset(PresetType.Griefing, 1, ModerationEntryType.Warning, "Griefing | L1")
                {
                    Reputation = -80
                },
                new PunishmentPreset(PresetType.Griefing, 2, ModerationEntryType.Ban, "Griefing | L2")
                {
                    PrimaryDuration = TimeSpan.FromHours(3d),
                    Reputation = -120
                },
                new PunishmentPreset(PresetType.Griefing, 3, ModerationEntryType.Ban, "Griefing | L3")
                {
                    PrimaryDuration = TimeSpan.FromHours(6d),
                    Reputation = -300
                },
                new PunishmentPreset(PresetType.Griefing, 4, ModerationEntryType.Ban, "Griefing | L4")
                {
                    PrimaryDuration = TimeSpan.FromDays(1d),
                    Reputation = -400
                },
                new PunishmentPreset(PresetType.Griefing, 5, ModerationEntryType.Ban, "Griefing | L5")
                {
                    PrimaryDuration = TimeSpan.FromDays(3d),
                    Reputation = -600
                },
                new PunishmentPreset(PresetType.Griefing, 6, ModerationEntryType.Ban, "Griefing | L6")
                {
                    PrimaryDuration = TimeSpan.FromDays(7d),
                    Reputation = -800
                }
            }
        },
        {
            PresetType.Toxicity, new PunishmentPreset[]
            {
                new PunishmentPreset(PresetType.Toxicity, 1, ModerationEntryType.Warning, "Toxicity | L1")
                {
                    Reputation = -25
                },
                new PunishmentPreset(PresetType.Toxicity, 2, ModerationEntryType.Mute, "Toxicity | L2")
                {
                    PrimaryDuration = TimeSpan.FromHours(1d),
                    Reputation = -60
                },
                new PunishmentPreset(PresetType.Toxicity, 3, ModerationEntryType.Mute, "Toxicity | L3")
                {
                    PrimaryDuration = TimeSpan.FromHours(6d),
                    Reputation = -120
                },
                new PunishmentPreset(PresetType.Toxicity, 4, ModerationEntryType.Mute, "Toxicity | L4")
                {
                    PrimaryDuration = TimeSpan.FromDays(1d),
                    Reputation = -250
                },
                new PunishmentPreset(PresetType.Toxicity, 5, ModerationEntryType.Mute, "Toxicity | L5")
                {
                    PrimaryDuration = TimeSpan.FromDays(3d),
                    Reputation = -325
                }
            }
        },
        {
            PresetType.Soloing, new PunishmentPreset[]
            {
                new PunishmentPreset(PresetType.Soloing, 1, ModerationEntryType.Warning, "Soloing | L1")
                {
                    Reputation = -15
                },
                new PunishmentPreset(PresetType.Soloing, 2, ModerationEntryType.Kick, "Soloing | L2")
                {
                    Reputation = -30
                },
                new PunishmentPreset(PresetType.Soloing, 3, ModerationEntryType.AssetBan, "Soloing | L3")
                {
                    PrimaryDuration = TimeSpan.FromHours(3d),
                    Reputation = -50
                },
                new PunishmentPreset(PresetType.Soloing, 4, ModerationEntryType.AssetBan, "Soloing | L4")
                {
                    PrimaryDuration = TimeSpan.FromHours(6d),
                    Reputation = -80
                },
                new PunishmentPreset(PresetType.Soloing, 5, ModerationEntryType.AssetBan, "Soloing | L5")
                {
                    PrimaryDuration = TimeSpan.FromDays(1d),
                    Reputation = -125
                },
                new PunishmentPreset(PresetType.Soloing, 6, ModerationEntryType.AssetBan, "Soloing | L6")
                {
                    PrimaryDuration = TimeSpan.FromDays(3d),
                    Reputation = -200
                }
            }
        },
        {
            PresetType.AssetWaste, new PunishmentPreset[]
            {
                new PunishmentPreset(PresetType.AssetWaste, 1, ModerationEntryType.Warning, "Asset Waste | L1")
                {
                    Reputation = -30
                },
                new PunishmentPreset(PresetType.AssetWaste, 2, ModerationEntryType.Ban, "Asset Waste | L2")
                {
                    PrimaryDuration = TimeSpan.FromMinutes(30d),
                    Reputation = -50
                },
                new PunishmentPreset(PresetType.AssetWaste, 3, ModerationEntryType.Ban, "Asset Waste | L3")
                {
                    PrimaryDuration = TimeSpan.FromHours(6d),
                    Reputation = -120
                },
                new PunishmentPreset(PresetType.AssetWaste, 4, ModerationEntryType.Ban, "Asset Waste | L4")
                {
                    PrimaryDuration = TimeSpan.FromHours(12d),
                    Reputation = -180
                },
                new PunishmentPreset(PresetType.AssetWaste, 5, ModerationEntryType.Ban, "Asset Waste | L5")
                {
                    PrimaryDuration = TimeSpan.FromDays(1d),
                    Reputation = -250
                },
                new PunishmentPreset(PresetType.AssetWaste, 6, ModerationEntryType.Ban, "Asset Waste | L6")
                {
                    PrimaryDuration = TimeSpan.FromDays(3d),
                    Reputation = -350
                }
            }
        },
        {
            PresetType.IntentionalTeamkilling, new PunishmentPreset[]
            {
                new PunishmentPreset(PresetType.IntentionalTeamkilling, 1, ModerationEntryType.Warning, "Intentional Teamkilling | L1")
                {
                    Reputation = -50
                },
                new PunishmentPreset(PresetType.IntentionalTeamkilling, 2, ModerationEntryType.Ban, "Intentional Teamkilling | L2")
                {
                    PrimaryDuration = TimeSpan.FromHours(3d),
                    Reputation = -125
                },
                new PunishmentPreset(PresetType.IntentionalTeamkilling, 3, ModerationEntryType.Ban, "Intentional Teamkilling | L3")
                {
                    PrimaryDuration = TimeSpan.FromHours(6d),
                    Reputation = -200
                },
                new PunishmentPreset(PresetType.IntentionalTeamkilling, 4, ModerationEntryType.Ban, "Intentional Teamkilling | L4")
                {
                    PrimaryDuration = TimeSpan.FromDays(1d),
                    Reputation = -400
                },
                new PunishmentPreset(PresetType.IntentionalTeamkilling, 5, ModerationEntryType.Ban, "Intentional Teamkilling | L5")
                {
                    PrimaryDuration = TimeSpan.FromDays(3d),
                    Reputation = -500
                },
                new PunishmentPreset(PresetType.IntentionalTeamkilling, 6, ModerationEntryType.Ban, "Intentional Teamkilling | L6")
                {
                    PrimaryDuration = TimeSpan.FromDays(7d),
                    Reputation = -600
                }
            }
        },
        {
            PresetType.TargetedHarassment, new PunishmentPreset[]
            {
                new PunishmentPreset(PresetType.TargetedHarassment, 1, ModerationEntryType.Mute, "Targeted Harassment | L1")
                {
                    PrimaryDuration = TimeSpan.FromHours(12d),
                    Reputation = -250
                },
                new PunishmentPreset(PresetType.TargetedHarassment, 2, ModerationEntryType.Mute, "Targeted Harassment | L2")
                {
                    PrimaryDuration = TimeSpan.FromDays(2d),
                    Reputation = -500
                },
                new PunishmentPreset(PresetType.TargetedHarassment, 3, ModerationEntryType.Mute, "Targeted Harassment | L3")
                {
                    PrimaryDuration = TimeSpan.FromDays(7d),
                    SecondaryModerationType = ModerationEntryType.Ban,
                    SecondaryDuration = TimeSpan.FromDays(1d),
                    Reputation = -1000
                },
                new PunishmentPreset(PresetType.TargetedHarassment, 4, ModerationEntryType.Mute, "Targeted Harassment | L4")
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
                new PunishmentPreset(PresetType.Discrimination, 1, ModerationEntryType.Mute, "Discrimination | L1")
                {
                    PrimaryDuration = TimeSpan.FromDays(14d),
                    SecondaryModerationType = ModerationEntryType.Ban,
                    SecondaryDuration = TimeSpan.FromDays(3d),
                    Reputation = -250
                },
                new PunishmentPreset(PresetType.Discrimination, 2, ModerationEntryType.Mute, "Discrimination | L2")
                {
                    PrimaryDuration = TimeSpan.FromDays(29.6875d),
                    SecondaryModerationType = ModerationEntryType.Ban,
                    SecondaryDuration = TimeSpan.FromDays(7d),
                    Reputation = -500
                },
                new PunishmentPreset(PresetType.Discrimination, 3, ModerationEntryType.Mute, "Discrimination | L3")
                {
                    PrimaryDuration = Timeout.InfiniteTimeSpan,
                    SecondaryModerationType = ModerationEntryType.Ban,
                    SecondaryDuration = TimeSpan.FromDays(14d),
                    Reputation = -750
                },
                new PunishmentPreset(PresetType.Discrimination, 4, ModerationEntryType.Ban, "Discrimination | L4")
                {
                    PrimaryDuration = Timeout.InfiniteTimeSpan,
                    Reputation = -1000
                }
            }
        },
        {
            PresetType.Cheating, new PunishmentPreset[]
            {
                new PunishmentPreset(PresetType.Cheating, 1, ModerationEntryType.Ban, "Cheating | L1")
                {
                    PrimaryDuration = Timeout.InfiniteTimeSpan,
                    Reputation = -50000
                }
            }
        },
        {
            PresetType.DisruptiveBehavior, new PunishmentPreset[]
            {
                new PunishmentPreset(PresetType.DisruptiveBehavior, 1, ModerationEntryType.Mute, "Disruptive Behavior | L1")
                {
                    PrimaryDuration = TimeSpan.FromHours(3d),
                    Reputation = -50
                },
                new PunishmentPreset(PresetType.DisruptiveBehavior, 2, ModerationEntryType.Mute, "Disruptive Behavior | L2")
                {
                    PrimaryDuration = TimeSpan.FromHours(6d),
                    Reputation = -100
                },
                new PunishmentPreset(PresetType.DisruptiveBehavior, 3, ModerationEntryType.Mute, "Disruptive Behavior | L3")
                {
                    PrimaryDuration = TimeSpan.FromDays(1d),
                    Reputation = -150
                },
                new PunishmentPreset(PresetType.DisruptiveBehavior, 4, ModerationEntryType.Mute, "Disruptive Behavior | L4")
                {
                    PrimaryDuration = TimeSpan.FromDays(3d),
                    Reputation = -250
                },
                new PunishmentPreset(PresetType.DisruptiveBehavior, 5, ModerationEntryType.Mute, "Disruptive Behavior | L5")
                {
                    PrimaryDuration = TimeSpan.FromDays(7d),
                    Reputation = -400
                }
            }
        },
        {
            PresetType.InappropriateProfile, new PunishmentPreset[]
            {
                new PunishmentPreset(PresetType.InappropriateProfile, 1, ModerationEntryType.Warning, "Inappropriate Profile | L1"),
                new PunishmentPreset(PresetType.InappropriateProfile, 2, ModerationEntryType.Kick, "Inappropriate Profile | L2")
                {
                    Reputation = -30
                },
                new PunishmentPreset(PresetType.InappropriateProfile, 3, ModerationEntryType.Ban, "Inappropriate Profile | L3")
                {
                    PrimaryDuration = TimeSpan.FromDays(3d),
                    Reputation = -80
                },
                new PunishmentPreset(PresetType.InappropriateProfile, 4, ModerationEntryType.Ban, "Inappropriate Profile | L4")
                {
                    PrimaryDuration = TimeSpan.FromDays(7d),
                    Reputation = -150
                }
            }
        },
        {
            PresetType.BypassingPunishment, new PunishmentPreset[]
            {
                new PunishmentPreset(PresetType.BypassingPunishment, 1, ModerationEntryType.Ban, "Bypassing Punishment | L1")
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
    public PresetType Type { get; }

    [JsonPropertyName("preset_level")]
    public int Level { get; }

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

    [JsonPropertyName("reputation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double Reputation { get; set; }

    public PunishmentPreset(PresetType type, int level, ModerationEntryType modType, string? defaultMessage)
    {
        Type = type;
        Level = level;
        PrimaryModerationType = modType;
        DefaultMessage = defaultMessage;
    }

    public PunishmentPreset() { }
}