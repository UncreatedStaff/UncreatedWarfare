using System;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Data;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.UI;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Layouts.UI;

[UnturnedUI(BasePath = "Box")]
public class CaptureUI : UnturnedUI
{
    public UnturnedLabel Title { get; } = new UnturnedLabel("TitleLabel");
    public ImageProgressBar CaptureProgress { get; } = new ImageProgressBar("CaptureProgress");
    public CaptureUI(AssetConfiguration assetConfig, ILoggerFactory loggerFactory)
        : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:CaptureProgress"), reliable: false) { }

    public void UpdateCaptureUI(WarfarePlayer player, in CaptureUIState state)
    {
        UpdateCaptureUI(new LanguageSet(player), in state);
    }

    public void UpdateCaptureUI(LanguageSet set, in CaptureUIState state)
    {
        GameThread.AssertCurrent();

        Color32 color = state.GetColor();
        string text = state.Translate(set);
        while (set.MoveNext())
        {
            WarfarePlayer player = set.Next;
            CaptureUIData data = GetOrAddData(player);
            if (!data.HasUI)
            {
                data.HasUI = true;
                data.LastColor = default;
                data.IsProgressLabelHidden = false;
                SendToPlayer(player.Connection);
            }

            Color32 lastColor = data.LastColor;
            if (lastColor.a == 0 || lastColor.r != color.r || lastColor.g != color.g || lastColor.b != color.b)
            {
                CaptureProgress.SetColor(player.Connection, color);
                data.LastColor = color;
            }

            if (float.IsNaN(state.Progress))
            {
                if (!data.IsProgressLabelHidden)
                {
                    CaptureProgress.Label.Hide(player);
                    data.IsProgressLabelHidden = true;
                }
            }
            else
            {
                if (data.IsProgressLabelHidden)
                {
                    CaptureProgress.Label.Show(player);
                    data.IsProgressLabelHidden = false;
                }
                CaptureProgress.SetProgress(player.Connection, state.Progress);
            }


            if (!string.Equals(data.LastLabel, text, StringComparison.Ordinal))
            {
                Title.SetText(player, text);
                data.LastLabel = text;
            }
        }
    }

    public void HideCaptureUI(LanguageSet set)
    {
        while (set.MoveNext())
            HideCaptureUI(set.Next);
    }

    public void HideCaptureUI(WarfarePlayer player)
    {
        GameThread.AssertCurrent();

        CaptureUIData data = GetOrAddData(player);
        if (!data.HasUI)
            return;

        ClearFromPlayer(player.Connection);
        data.HasUI = false;
    }

    private CaptureUIData GetOrAddData(WarfarePlayer player)
    {
        return GetOrAddData(player.Steam64, steam64 => new CaptureUIData(steam64, this));
    }

    private class CaptureUIData : IUnturnedUIData
    {
        public CSteamID Player { get; }
        public UnturnedUI Owner { get; }

        public bool HasUI { get; set; }
        public bool IsProgressLabelHidden { get; set; }
        public Color32 LastColor { get; set; }
        public string? LastLabel { get; set; }

        public CaptureUIData(CSteamID player, UnturnedUI owner)
        {
            Player = player;
            Owner = owner;
        }

        
        UnturnedUIElement? IUnturnedUIData.Element => null;
    }
}

public readonly struct CaptureUIState
{
    public readonly Translation Translation;
    public readonly Team? Team;
    public readonly Team? ProminentOtherTeam;
    public readonly Color32 OverrideColor;
    public readonly bool UseEnemyColor;
    public readonly string? Location;

    // NaN hides the percentage label
    public readonly float Progress;

    /// <summary>
    /// Use static factory methods if possible.
    /// </summary>
    public CaptureUIState(float progress, Translation translation, Team? team, Team? prominentOtherTeam, string? location, Color32 color = default, bool useEnemyColor = false)
    {
        Translation = translation;
        Team = team;
        ProminentOtherTeam = prominentOtherTeam;
        Location = location;
        Progress = progress;
        OverrideColor = color;
        UseEnemyColor = useEnemyColor;
    }

    public static CaptureUIState Capturing(CaptureUITranslations translations, float progress, Team capturingTeam, string location)
    {
        return new CaptureUIState(progress, translations.Capturing, capturingTeam, null, location);
    }
    
    public static CaptureUIState Losing(CaptureUITranslations translations, float progress, Team otherTeam, string location)
    {
        return new CaptureUIState(progress, translations.Losing, null, otherTeam, location, useEnemyColor: true);
    }
    
    public static CaptureUIState Secured(CaptureUITranslations translations, string location)
    {
        return new CaptureUIState(float.NaN, translations.Secured, null, null, location, new Color32(125, 232, 125, 255));
    }
    
    public static CaptureUIState Neutralized(CaptureUITranslations translations, string location)
    {
        return new CaptureUIState(float.NaN, translations.Neutralized, null, null, location, new Color32(255, 255, 255, 255));
    }
    
    public static CaptureUIState Lost(CaptureUITranslations translations, Team otherTeam, string location)
    {
        return new CaptureUIState(float.NaN, translations.Lost, null, otherTeam, location, useEnemyColor: true);
    }
    
    public static CaptureUIState Contesting(CaptureUITranslations translations, float progress, string location)
    {
        return new CaptureUIState(progress, translations.Contesting, null, null, location, new Color32(236, 236, 121, 255));
    }
    
    public static CaptureUIState Ineffective(CaptureUITranslations translations, string location)
    {
        return new CaptureUIState(float.NaN, translations.Ineffective, null, null, location, new Color32(255, 255, 255, 255));
    }

    public static CaptureUIState Clearing(CaptureUITranslations translations, float progress, Team capturingTeam, string location)
    {
        return new CaptureUIState(progress, translations.Clearing, capturingTeam, null, location);
    }
    
    public static CaptureUIState InVehicle(CaptureUITranslations translations, float progress, string location)
    {
        return new CaptureUIState(progress, translations.InVehicle, null, null, location, new Color32(255, 153, 153, 255));
    }
    
    public static CaptureUIState Locked(CaptureUITranslations translations, float progress, string location)
    {
        return new CaptureUIState(float.NaN, translations.Locked, null, null, location, new Color32(255, 153, 153, 255));
    }

    internal Color32 GetColor()
    {
        if (OverrideColor.a == 0)
        {
            return (UseEnemyColor ? ProminentOtherTeam?.Faction.Color : Team?.Faction.Color) ?? Color.white;
        }

        return OverrideColor with { a = byte.MaxValue };
    }

    internal string Translate(LanguageSet set)
    {
        if (Location != null && Translation is Translation<string> t)
        {
            return t.Translate(Location, in set);
        }
        
        return Translation.GetValueForLanguage(set.Language).Value;
    }
}

public class CaptureUITranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Capture UI";

    [TranslationData("Shown when your team is capturing the flag.")]
    public readonly Translation<string> Capturing = new Translation<string>("Capturing {0}");

    [TranslationData("Shown when your team is losing the flag because the other team has more players.")]
    public readonly Translation<string> Losing = new Translation<string>("Losing {0}");

    [TranslationData("Shown when your team is holding the flag after it has been captured.")]
    public readonly Translation<string> Secured = new Translation<string>("{0} Secured");

    [TranslationData("Shown when the flag has not been captured by either team.")]
    public readonly Translation<string> Neutralized = new Translation<string>("{0} Neutralized");

    [TranslationData("Shown when your team lost the flag and you dont have enough people on the flag to clear.")]
    public readonly Translation<string> Lost = new Translation<string>("{0} Lost");

    [TranslationData("Shown when your team and the other team have the same amount of people on the flag.")]
    public readonly Translation<string> Contesting = new Translation<string>("Contesting {0}");

    [TranslationData("Shown when you're on a flag but it's not the objective.")]
    public readonly Translation<string> Ineffective = new Translation<string>("{0} Lost - Ineffective force");

    [TranslationData("Shown when your team is capturing a flag still owned by the other team.")]
    public readonly Translation<string> Clearing = new Translation<string>("Clearing {0}");

    [TranslationData("Shown when you're trying to capture a flag while in a vehicle.")]
    public readonly Translation InVehicle = new Translation("In Vehicle");

    [TranslationData("Shown in Invasion when a flag has already been captured by attackers and can't be recaptured.")]
    public readonly Translation<string> Locked = new Translation<string>("{0} Locked");
}