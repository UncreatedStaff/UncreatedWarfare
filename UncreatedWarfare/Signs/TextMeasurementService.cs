using System;
using System.Collections.Generic;
using TMPro;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Signs;

public class TextMeasurementService : IDisposable, ILayoutHostedService
{
    private readonly TextMeshPro _textMeshPro;
    private readonly Dictionary<Guid, SignMetrics> _metrics = new Dictionary<Guid, SignMetrics>(8);
    private readonly Dictionary<CacheKey, TMP_LineInfo[]> _cache = new Dictionary<CacheKey, TMP_LineInfo[]>(64);

    public TextMeasurementService()
    {
        GameObject tempTextObject = new GameObject("TempText");
        Object.DontDestroyOnLoad(tempTextObject);

        _textMeshPro = tempTextObject.AddComponent<TextMeshPro>();
        TextMeshProUtils.FixupFont(_textMeshPro);

        // vehicle bay sign
        _metrics.Add(new Guid("5251c3bcaabb4a15b9e631e19c037ec1"), new SignMetrics
        {
            SignSettings = tmp =>
            {
                tmp.rectTransform.sizeDelta = new Vector2(1.95f, 0.75f);
                tmp.isRightToLeftText = false;
                tmp.fontSize = 3.35f;
                tmp.fontWeight = FontWeight.Regular;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 1f;
                tmp.fontSizeMax = 72f;
                tmp.fontStyle = FontStyles.Normal;
                tmp.alignment = TextAlignmentOptions.Converted;
                tmp.horizontalAlignment = HorizontalAlignmentOptions.Center;
                tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
                tmp.enableWordWrapping = true;
                tmp.wordWrappingRatios = 0.4f;
                tmp.overflowMode = TextOverflowModes.Truncate;
                tmp.enableKerning = true;
                tmp.extraPadding = false;
                tmp.richText = true;
                tmp.parseCtrlCharacters = true;
                tmp.horizontalMapping = TextureMappingOptions.Character;
                tmp.verticalMapping = TextureMappingOptions.Character;
                tmp.vertexBufferAutoSizeReduction = true;
                tmp.useMaxVisibleDescender = true;
                tmp.margin = new Vector4(0.09875983f, 1.458147f, 0.1029023f, -1.819759f);
            }
        });

        // kit sign
        _metrics.Add(new Guid("275dd81d60ae443e91f0655b8b7aa920"), new SignMetrics
        {
            SignSettings = tmp =>
            {
                tmp.rectTransform.sizeDelta = new Vector2(1.2f, 0.7580871f);
                tmp.isRightToLeftText = false;
                tmp.fontSize = 36f;
                tmp.fontWeight = FontWeight.Regular;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 1f;
                tmp.fontSizeMax = 72f;
                tmp.fontStyle = FontStyles.Normal;
                tmp.alignment = TextAlignmentOptions.Converted;
                tmp.horizontalAlignment = HorizontalAlignmentOptions.Center;
                tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
                tmp.enableWordWrapping = true;
                tmp.wordWrappingRatios = 0.4f;
                tmp.overflowMode = TextOverflowModes.Truncate;
                tmp.enableKerning = true;
                tmp.extraPadding = false;
                tmp.richText = true;
                tmp.parseCtrlCharacters = true;
                tmp.horizontalMapping = TextureMappingOptions.Character;
                tmp.verticalMapping = TextureMappingOptions.Character;
                tmp.vertexBufferAutoSizeReduction = true;
                tmp.useMaxVisibleDescender = true;
                tmp.margin = new Vector4(0, 0, 0, -0.7580871f);
            }
        });
    }

    public SignMetrics GetSignMetrics(Guid signId)
    {
        _metrics.TryGetValue(signId, out SignMetrics metrics);
        return metrics;
    }

    public TMP_LineInfo[]? MeasureText(string text, float preferredFontSize, SignMetrics metrics)
    {
        GameThread.AssertCurrent();


        if (metrics.SignSettings == null)
            return null;

        CacheKey key = new CacheKey(metrics.Sign, text);
        if (_cache.TryGetValue(key, out TMP_LineInfo[] lines))
        {
            return lines;
        }

        metrics.SignSettings(_textMeshPro);
        _textMeshPro.enableAutoSizing = false;
        _textMeshPro.fontSize = preferredFontSize;

        TMP_TextInfo info = _textMeshPro.GetTextInfo(text);

        lines = new TMP_LineInfo[info.lineCount];
        Array.Copy(info.lineInfo, lines, info.lineCount);

        _cache.Add(key, lines);
        return lines;
    }

    public int SplitLines(string text, float preferredFontSize, SignMetrics metrics, Span<Range> outRanges)
    {
        TMP_LineInfo[]? info = MeasureText(text, preferredFontSize, metrics);
        if (info == null)
        {
            if (outRanges.Length == 0)
                return 0;
            outRanges[0] = Range.All;
            return 1;
        }

        int lineCt = Math.Min(outRanges.Length, info.Length);
        if (lineCt == 0)
            return 0;

        if (info.Length <= 1)
        {
            outRanges[0] = Range.All;
            return 1;
        }

        for (int i = 0; i < lineCt; ++i)
        {
            ref TMP_LineInfo line = ref info[i];
            outRanges[i] = new Range(new Index(line.firstVisibleCharacterIndex), new Index(line.lastVisibleCharacterIndex + 1));
        }

        return lineCt;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Object.Destroy(_textMeshPro.gameObject);
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        _cache.Clear();
        return UniTask.CompletedTask;
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    private readonly struct CacheKey : IComparable<CacheKey>, IEquatable<CacheKey>
    {
        public readonly Guid Sign;
        public readonly string Text;
        public CacheKey(Guid sign, string text)
        {
            Sign = sign;
            Text = text;
        }

        public int CompareTo(CacheKey other)
        {
            int cmp = Sign.CompareTo(other.Sign);
            if (cmp != 0)
                return cmp;

            cmp = string.Compare(Text, other.Text, StringComparison.Ordinal);
            return cmp;
        }

        public bool Equals(CacheKey other)
        {
            return Sign == other.Sign && string.Equals(Text, other.Text, StringComparison.Ordinal);
        }
    }
}

public struct SignMetrics
{
    public Guid Sign;
    public Action<TextMeshPro> SignSettings;
}