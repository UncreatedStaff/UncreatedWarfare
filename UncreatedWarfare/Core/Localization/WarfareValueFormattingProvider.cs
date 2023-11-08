using Uncreated.Warfare.API.Localization;

namespace Uncreated.Warfare.Core.Localization;
internal class WarfareValueFormattingProvider : IWarfareValueFormattingProvider
{
    public string FormatToString<TValue>(TValue value)
    {
        return value?.ToString() ?? "null";
    }
}
