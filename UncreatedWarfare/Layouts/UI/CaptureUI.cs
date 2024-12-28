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
    private readonly Func<CSteamID, CaptureUIData> _getCaptureUIData;

    public UnturnedLabel Title { get; } = new UnturnedLabel("TitleLabel");
    public ImageProgressBar CaptureProgress { get; } = new ImageProgressBar("CaptureProgress");
    public CaptureUI(AssetConfiguration assetConfig, ILoggerFactory loggerFactory)
        : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:CaptureHUD"), reliable: false)
    {
        IsSendReliable = true;
        _getCaptureUIData = GetCaptureUIData;
    }

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
                data.LastLabel = null;
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
                CaptureProgress.SetProgress(player.Connection, 1);
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
        return GetOrAddData(player.Steam64, _getCaptureUIData);
    }

    private CaptureUIData GetCaptureUIData(CSteamID steam64)
    {
        return new CaptureUIData(steam64, this);
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

    public static CaptureUIState Capturing(FlagUITranslations translations, float progress, Team capturingTeam, string location)
    {
        return new CaptureUIState(progress, translations.Capturing, capturingTeam, null, location);
    }
    
    public static CaptureUIState Losing(FlagUITranslations translations, float progress, Team otherTeam, string location)
    {
        return new CaptureUIState(progress, translations.Losing, null, otherTeam, location, useEnemyColor: true);
    }
    
    public static CaptureUIState Secured(FlagUITranslations translations, string location)
    {
        return new CaptureUIState(float.NaN, translations.Secured, null, null, location, new Color32(125, 232, 125, 255));
    }
    
    public static CaptureUIState Neutralized(FlagUITranslations translations, string location)
    {
        return new CaptureUIState(float.NaN, translations.Neutralized, null, null, location, new Color32(255, 255, 255, 255));
    }
    
    public static CaptureUIState Lost(FlagUITranslations translations, Team otherTeam, string location)
    {
        return new CaptureUIState(float.NaN, translations.Lost, null, otherTeam, location, useEnemyColor: true);
    }
    
    public static CaptureUIState Contesting(FlagUITranslations translations, float progress, string location)
    {
        return new CaptureUIState(progress, translations.Contesting, null, null, location, new Color32(236, 236, 121, 255));
    }
    
    public static CaptureUIState Ineffective(FlagUITranslations translations, string location)
    {
        return new CaptureUIState(float.NaN, translations.Ineffective, null, null, location, new Color32(255, 255, 255, 255));
    }

    public static CaptureUIState Clearing(FlagUITranslations translations, float progress, Team owningTeam, string location)
    {
        return new CaptureUIState(progress, translations.Clearing, owningTeam, null, location);
    }
    
    public static CaptureUIState InVehicle(FlagUITranslations translations, float progress, string location)
    {
        return new CaptureUIState(progress, translations.InVehicle, null, null, location, new Color32(255, 153, 153, 255));
    }
    
    public static CaptureUIState Locked(FlagUITranslations translations, float progress, string location)
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

    public override string ToString()
    {
        return $"CaptureUIState: " +
               $"Translation = {Translation}, " +
               $"Team = {(Team?.ToString() ?? "null")}, " +
               $"ProminentOtherTeam = {(ProminentOtherTeam?.ToString() ?? "null")}, " +
               $"OverrideColor = #{OverrideColor.r:X2}{OverrideColor.g:X2}{OverrideColor.b:X2}{OverrideColor.a:X2}, " +
               $"UseEnemyColor = {UseEnemyColor}, " +
               $"Location = {(string.IsNullOrEmpty(Location) ? "null" : Location)}, " +
               $"Progress = {(float.IsNaN(Progress) ? "NaN" : Progress.ToString("0.##"))}";
    }
}