using DanielWillett.ReflectionTools;
using SDG.NetTransport;
using System;
using System.Globalization;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Presets;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Interaction.UI;
public class ImageProgressBar : ILabel
{
    private const string ApiUrl = "https://uncreated.network/api/ui/color1x1/";

    private bool _hasCheckedBasePath;

    private readonly Func<float, ImageProgressBar, string> _getLogicName;
    private string _logicBasePath;
    public UnturnedUIElement Root { get; }
    public UnturnedImage Foreground { get; }
    public UnturnedImage Background { get; }
    public UnturnedLabel Label { get; }

    /// <summary>
    /// If the label has to be set separately instead of by animation.
    /// </summary>
    public bool NeedsToSetLabel { get; set; }

    /// <summary>
    /// 100 / the number of animations.
    /// </summary>
    public int Resolution { get; set; } = 1;

    private static string DefaultGetLogicName(float progress, ImageProgressBar bar)
    {
        return (Mathf.RoundToInt(Mathf.Clamp01(progress) * 100f * bar.Resolution) / bar.Resolution).ToString("D3", CultureInfo.InvariantCulture);
    }

    public ImageProgressBar(string rootPath)
        : this(rootPath, "./Background", "./Foreground", "./Label", "./", DefaultGetLogicName) { }
    public ImageProgressBar(string rootPath, string? backgroundPath, string? foregroundPath, string? labelPath, string? logicBasePath)
        : this(rootPath, backgroundPath, foregroundPath, labelPath, logicBasePath, DefaultGetLogicName) { }
    public ImageProgressBar(string rootPath, string? backgroundPath, string? foregroundPath, string? labelPath, string? logicBasePath, Func<float, ImageProgressBar, string> logicNameAccessor)
    {
        _getLogicName = logicNameAccessor ?? throw new ArgumentNullException(nameof(logicNameAccessor));
        _logicBasePath = logicBasePath ?? "./";

        Root = new UnturnedUIElement(rootPath);
        Background = new UnturnedImage(UnturnedUIUtility.ResolveRelativePath(rootPath, backgroundPath ?? "./Background"));
        Foreground = new UnturnedImage(UnturnedUIUtility.ResolveRelativePath(rootPath, foregroundPath ?? "./Foreground"));
        Label = new UnturnedLabel(UnturnedUIUtility.ResolveRelativePath(rootPath, labelPath ?? "./Label"));
    }
    
    /// <summary>
    /// Sets the color of the progress bar using an image API that returns a 1x1 image of a given color.
    /// </summary>
    /// <remarks>Thread-safe</remarks>
    public void SetColor(ITransportConnection connection, Color32 color)
    {
        color.a = byte.MaxValue;

        string url = ApiUrl + HexStringHelper.FormatHexColor(color);
        if (GameThread.IsCurrent)
        {
            Foreground.SetImage(connection, url);
            Background.SetImage(connection, url);
        }
        else
        {
            string url2 = url;
            ITransportConnection c2 = connection;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                Foreground.SetImage(c2, url2);
                Background.SetImage(c2, url2);
            });
        }
    }

    /// <summary>
    /// Sets the progress of the bar.
    /// </summary>
    /// <param name="progress">Value from 0-1 representing how far the progress bar is from finishing.</param>
    /// <remarks>Thread-safe</remarks>
    public void SetProgress(ITransportConnection connection, float progress)
    {
        if (GameThread.IsCurrent)
        {
            SetProgressIntl(connection, progress);
        }
        else
        {
            float p2 = progress;
            ITransportConnection c2 = connection;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                SetProgressIntl(c2, p2);
            });
        }
    }

    private void SetProgressIntl(ITransportConnection connection, float progress)
    {
        if (!_hasCheckedBasePath)
        {
            // root path gets replaced by base path later
            _logicBasePath = UnturnedUIUtility.ResolveRelativePath(Root.Path, _logicBasePath);
            _hasCheckedBasePath = true;
        }

        string elementPath = UnturnedUIUtility.CombinePath(_logicBasePath, _getLogicName(progress, this));

        EffectManager.sendUIEffectVisibility(Root.Owner.Key, connection, Root.Owner.IsReliable, elementPath, true);

        if (NeedsToSetLabel)
        {
            Label.SetText(connection, Mathf.RoundToInt(progress * 100).ToString(CultureInfo.InvariantCulture) + "%");
        }
    }

    [Ignore]
    UnturnedUIElement IElement.Element => Root;
}