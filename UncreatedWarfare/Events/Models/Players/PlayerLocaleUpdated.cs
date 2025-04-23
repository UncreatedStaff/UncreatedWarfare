using System;
using System.Globalization;
using Uncreated.Warfare.Models.Localization;

namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Invoked when the player changes their Language, Culture, TimeZone, or IMGUI setting.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class PlayerLocaleUpdated : PlayerEvent
{
    public bool IMGUI => Player.Save.IMGUI;
    public LanguageInfo Language => Player.Locale.LanguageInfo;
    public CultureInfo Culture => Player.Locale.CultureInfo;
    public TimeZoneInfo TimeZone => Player.Locale.TimeZone;
}