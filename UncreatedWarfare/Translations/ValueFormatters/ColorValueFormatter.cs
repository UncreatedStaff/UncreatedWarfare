using System;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Translations.ValueFormatters;

/// <summary>
/// Handles formatting colors to hex or rgb(..) strings.
/// </summary>
public class ColorValueFormatter : IValueFormatter<Color>, IValueFormatter<Color32>
{
    public static readonly SpecialFormat FormatHexAuto     = new SpecialFormat("HTML Color String (rrggbb[aa])",  "hx",  useForToString: false);
    public static readonly SpecialFormat FormatHex8        = new SpecialFormat("HTML Color String (rrggbbaa)",    "h8",  useForToString: false);
    public static readonly SpecialFormat FormatHex6        = new SpecialFormat("HTML Color String (rrggbb)",      "h6",  useForToString: false);
    public static readonly SpecialFormat FormatHex4        = new SpecialFormat("HTML Color String (rgba)",        "h4",  useForToString: false);
    public static readonly SpecialFormat FormatHex3        = new SpecialFormat("HTML Color String (rgb)",         "h3",  useForToString: false);
    public static readonly SpecialFormat FormatHex2        = new SpecialFormat("HTML Color String (ra)",          "h2",  useForToString: false);
    public static readonly SpecialFormat FormatHex1        = new SpecialFormat("HTML Color String (r)",           "h1",  useForToString: false);
    public static readonly SpecialFormat FormatHashHexAuto = new SpecialFormat("HTML Color String (#rrggbb[aa])", "#hx", useForToString: false);
    public static readonly SpecialFormat FormatHashHex8    = new SpecialFormat("HTML Color String (#rrggbbaa)",   "#h8", useForToString: false);
    public static readonly SpecialFormat FormatHashHex6    = new SpecialFormat("HTML Color String (#rrggbb)",     "#h6", useForToString: false);
    public static readonly SpecialFormat FormatHashHex4    = new SpecialFormat("HTML Color String (#rgba)",       "#h4", useForToString: false);
    public static readonly SpecialFormat FormatHashHex3    = new SpecialFormat("HTML Color String (#rgb)",        "#h3", useForToString: false);
    public static readonly SpecialFormat FormatHashHex2    = new SpecialFormat("HTML Color String (#ra)",         "#h2", useForToString: false);
    public static readonly SpecialFormat FormatHashHex1    = new SpecialFormat("HTML Color String (#r)",          "#h1", useForToString: false);
    public static readonly SpecialFormat FormatColorizedHexAuto     = new SpecialFormat("Colored HTML Color String (rrggbb[aa])",  "chx",  useForToString: false);
    public static readonly SpecialFormat FormatColorizedHex8        = new SpecialFormat("Colored HTML Color String (rrggbbaa)",    "ch8",  useForToString: false);
    public static readonly SpecialFormat FormatColorizedHex6        = new SpecialFormat("Colored HTML Color String (rrggbb)",      "ch6",  useForToString: false);
    public static readonly SpecialFormat FormatColorizedHex4        = new SpecialFormat("Colored HTML Color String (rgba)",        "ch4",  useForToString: false);
    public static readonly SpecialFormat FormatColorizedHex3        = new SpecialFormat("Colored HTML Color String (rgb)",         "ch3",  useForToString: false);
    public static readonly SpecialFormat FormatColorizedHex2        = new SpecialFormat("Colored HTML Color String (ra)",          "ch2",  useForToString: false);
    public static readonly SpecialFormat FormatColorizedHex1        = new SpecialFormat("Colored HTML Color String (r)",           "ch1",  useForToString: false);
    public static readonly SpecialFormat FormatColorizedHashHexAuto = new SpecialFormat("Colored HTML Color String (#rrggbb[aa])", "c#hx", useForToString: false);
    public static readonly SpecialFormat FormatColorizedHashHex8    = new SpecialFormat("Colored HTML Color String (#rrggbbaa)",   "c#h8", useForToString: false);
    public static readonly SpecialFormat FormatColorizedHashHex6    = new SpecialFormat("Colored HTML Color String (#rrggbb)",     "c#h6", useForToString: false);
    public static readonly SpecialFormat FormatColorizedHashHex4    = new SpecialFormat("Colored HTML Color String (#rgba)",       "c#h4", useForToString: false);
    public static readonly SpecialFormat FormatColorizedHashHex3    = new SpecialFormat("Colored HTML Color String (#rgb)",        "c#h3", useForToString: false);
    public static readonly SpecialFormat FormatColorizedHashHex2    = new SpecialFormat("Colored HTML Color String (#ra)",         "c#h2", useForToString: false);
    public static readonly SpecialFormat FormatColorizedHashHex1    = new SpecialFormat("Colored HTML Color String (#r)",          "c#h1", useForToString: false);

    public static readonly SpecialFormat FormatRGB255      = new SpecialFormat("RGB255 (rgb(r, g, b))", "rgb255", useForToString: false);
    public static readonly SpecialFormat FormatRGB1        = new SpecialFormat("RGB1   (rgb(r, g, b))", "rgb1",   useForToString: false);
    
    public static readonly SpecialFormat FormatColorizedRGB255      = new SpecialFormat("RGB255 (rgb(r, g, b))", "rgb255", useForToString: false);
    public static readonly SpecialFormat FormatColorizedRGB1        = new SpecialFormat("RGB1   (rgb(r, g, b))", "rgb1",   useForToString: false);

    public string Format(ITranslationValueFormatter formatter, Color value, in ValueFormatParameters parameters)
    {
        return Format(formatter, (Color32)value, in parameters);
    }

    public string Format(ITranslationValueFormatter formatter, Color32 value, in ValueFormatParameters parameters)
    {
        if (string.IsNullOrEmpty(parameters.Format.Format))
        {
            return HexStringHelper.FormatHexColor(value);
        }

        ReadOnlySpan<char> fmt = parameters.Format.Format;
        if (fmt[0] == '#')
        {
            return FormatHashHex(value, in parameters);
        }
        if (fmt[0] == 'h')
        {
            return FormatHex(value, in parameters);
        }
        if (fmt[0] == 'r')
        {
            return FormatRgb(value, in parameters);
        }
        
        if (fmt[0] == 'c' && fmt.Length > 1)
        {
            if (fmt[1] == '#')
            {
                return FormatColorizedHashHex(formatter, value, in parameters);
            }
            if (fmt[1] == 'h')
            {
                return FormatColorizedHex(formatter, value, in parameters);
            }
            if (fmt[1] == 'r')
            {
                return FormatColorizedRgb(formatter, value, in parameters);
            }
        }

        return HexStringHelper.FormatHexColor(value);
    }

    private static string FormatHexColorWithHash(Color32 color, int size = -1)
    {
        if (size == -1)
            size = color.a == 255 ? 6 : 8;

        return string.Create(size + 1, color, (span, color) =>
        {
            span[0] = '#';
            HexStringHelper.FormatHexColor(color, span[1..]);
        });
    }
    private static void FormatHexColorWithHash(Span<char> span, Color32 color, int size = -1)
    {
        if (size == -1)
            size = color.a == 255 ? 6 : 8;

        span[0] = '#';
        HexStringHelper.FormatHexColor(color, span.Slice(1, size));
    }
    private static void FormatHexColor(Span<char> span, Color32 color, int size = -1)
    {
        if (size == -1)
            size = color.a == 255 ? 6 : 8;

        HexStringHelper.FormatHexColor(color, span[..size]);
    }

    public string Format(ITranslationValueFormatter formatter, object value, in ValueFormatParameters parameters)
    {
        return value switch
        {
            Color32 c32 => Format(formatter, c32, in parameters),
            Color c => Format(formatter, (Color32)c, in parameters),
            _ => value.ToString()
        };
    }

    private string FormatColorizedRgb(ITranslationValueFormatter formatter, Color32 value, in ValueFormatParameters parameters)
    {
        if (FormatColorizedRGB255.Match(in parameters))
        {
            return formatter.Colorize($"rgb({value.r.ToString(parameters.Culture)}, {value.g.ToString(parameters.Culture)}, {value.b.ToString(parameters.Culture)})", value, parameters.Options);
        }
        if (FormatColorizedRGB1.Match(in parameters))
        {
            return formatter.Colorize($"rgb({((float)value.r / 255).ToString("F2", parameters.Culture)}, {((float)value.g / 255).ToString("F2", parameters.Culture)}, {((float)value.b / 255).ToString("F2", parameters.Culture)})", value, parameters.Options);
        }

        return HexStringHelper.FormatHexColor(value);
    }
    private string FormatRgb(Color32 value, in ValueFormatParameters parameters)
    {
        if (FormatRGB255.Match(in parameters))
        {
            return $"rgb({value.r.ToString(parameters.Culture)}, {value.g.ToString(parameters.Culture)}, {value.b.ToString(parameters.Culture)})";
        }
        if (FormatRGB1.Match(in parameters))
        {
            return $"rgb({((float)value.r / 255).ToString("F2", parameters.Culture)}, {((float)value.g / 255).ToString("F2", parameters.Culture)}, {((float)value.b / 255).ToString("F2", parameters.Culture)})";
        }

        return HexStringHelper.FormatHexColor(value);
    }
    private string FormatColorizedHex(ITranslationValueFormatter formatter, Color32 value, in ValueFormatParameters parameters)
    {
        if (FormatColorizedHexAuto.Match(in parameters))
        {
            Span<char> span = stackalloc char[8];
            FormatHexColor(span, value);
            return formatter.Colorize(span, value, parameters.Options);
        }
        if (FormatColorizedHex8.Match(in parameters))
        {
            Span<char> span = stackalloc char[8];
            FormatHexColor(span, value, 8);
            return formatter.Colorize(span, value, parameters.Options);
        }
        if (FormatColorizedHex6.Match(in parameters))
        {
            Span<char> span = stackalloc char[6];
            FormatHexColor(span, value, 6);
            return formatter.Colorize(span, value, parameters.Options);
        }
        if (FormatColorizedHex4.Match(in parameters))
        {
            Span<char> span = stackalloc char[4];
            FormatHexColor(span, value, 4);
            return formatter.Colorize(span, value, parameters.Options);
        }
        if (FormatColorizedHex3.Match(in parameters))
        {
            Span<char> span = stackalloc char[3];
            FormatHexColor(span, value, 3);
            return formatter.Colorize(span, value, parameters.Options);
        }
        if (FormatColorizedHex2.Match(in parameters))
        {
            Span<char> span = stackalloc char[2];
            FormatHexColor(span, value, 2);
            return formatter.Colorize(span, value, parameters.Options);
        }
        if (FormatColorizedHex1.Match(in parameters))
        {
            Span<char> span = stackalloc char[1];
            FormatHexColor(span, value, 1);
            return formatter.Colorize(span, value, parameters.Options);
        }

        return HexStringHelper.FormatHexColor(value);
    }
    private string FormatHex(Color32 value, in ValueFormatParameters parameters)
    {
        if (FormatHexAuto.Match(in parameters))
        {
            return HexStringHelper.FormatHexColor(value);
        }
        if (FormatHex8.Match(in parameters))
        {
            return HexStringHelper.FormatHexColor(value, 8);
        }
        if (FormatHex6.Match(in parameters))
        {
            return HexStringHelper.FormatHexColor(value, 6);
        }
        if (FormatHex4.Match(in parameters))
        {
            return HexStringHelper.FormatHexColor(value, 4);
        }
        if (FormatHex3.Match(in parameters))
        {
            return HexStringHelper.FormatHexColor(value, 3);
        }
        if (FormatHex2.Match(in parameters))
        {
            return HexStringHelper.FormatHexColor(value, 2);
        }
        if (FormatHex1.Match(in parameters))
        {
            return HexStringHelper.FormatHexColor(value, 1);
        }

        return HexStringHelper.FormatHexColor(value);
    }

    private string FormatColorizedHashHex(ITranslationValueFormatter formatter, Color32 value, in ValueFormatParameters parameters)
    {
        if (FormatColorizedHashHexAuto.Match(in parameters))
        {
            Span<char> span = stackalloc char[9];
            FormatHexColorWithHash(span, value);
            return formatter.Colorize(span, value, parameters.Options);
        }
        if (FormatColorizedHashHex8.Match(in parameters))
        {
            Span<char> span = stackalloc char[9];
            FormatHexColorWithHash(span, value, 8);
            return formatter.Colorize(span, value, parameters.Options);
        }
        if (FormatColorizedHashHex6.Match(in parameters))
        {
            Span<char> span = stackalloc char[7];
            FormatHexColorWithHash(span, value, 6);
            return formatter.Colorize(span, value, parameters.Options);
        }
        if (FormatColorizedHashHex4.Match(in parameters))
        {
            Span<char> span = stackalloc char[5];
            FormatHexColorWithHash(span, value, 4);
            return formatter.Colorize(span, value, parameters.Options);
        }
        if (FormatColorizedHashHex3.Match(in parameters))
        {
            Span<char> span = stackalloc char[4];
            FormatHexColorWithHash(span, value, 3);
            return formatter.Colorize(span, value, parameters.Options);
        }
        if (FormatColorizedHashHex2.Match(in parameters))
        {
            Span<char> span = stackalloc char[3];
            FormatHexColorWithHash(span, value, 2);
            return formatter.Colorize(span, value, parameters.Options);
        }
        if (FormatColorizedHashHex1.Match(in parameters))
        {
            Span<char> span = stackalloc char[2];
            FormatHexColorWithHash(span, value, 1);
            return formatter.Colorize(span, value, parameters.Options);
        }

        return HexStringHelper.FormatHexColor(value);
    }

    private string FormatHashHex(Color32 value, in ValueFormatParameters parameters)
    {
        if (FormatHashHexAuto.Match(in parameters))
        {
            return FormatHexColorWithHash(value);
        }
        if (FormatHashHex8.Match(in parameters))
        {
            return FormatHexColorWithHash(value, 8);
        }
        if (FormatHashHex6.Match(in parameters))
        {
            return FormatHexColorWithHash(value, 6);
        }
        if (FormatHashHex4.Match(in parameters))
        {
            return FormatHexColorWithHash(value, 4);
        }
        if (FormatHashHex3.Match(in parameters))
        {
            return FormatHexColorWithHash(value, 3);
        }
        if (FormatHashHex2.Match(in parameters))
        {
            return FormatHexColorWithHash(value, 2);
        }
        if (FormatHashHex1.Match(in parameters))
        {
            return FormatHexColorWithHash(value, 1);
        }

        return HexStringHelper.FormatHexColor(value);
    }
}
