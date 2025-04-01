namespace Uncreated.Warfare.Translations.Util;
public struct ArgumentSpan
{
    public int Argument;
    public int StartIndex;
    public int Length;
    public bool Inverted;
    public ArgumentSpan(int argument, int startIndex, int length, bool inverted)
    {
        Argument = argument;
        StartIndex = startIndex;
        Length = length;
        Inverted = inverted;
    }

    public override string ToString()
    {
        return $"{{Arg: {Argument} @{StartIndex}[l = {Length}]. {(Inverted ? "inverted" : "not inverted")}}}";
    }
}