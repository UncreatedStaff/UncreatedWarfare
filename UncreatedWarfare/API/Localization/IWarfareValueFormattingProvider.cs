namespace Uncreated.Warfare.API.Localization;
public interface IWarfareValueFormattingProvider
{
    string FormatToString<TValue>(TValue value);
}