using System.Text.Json;

namespace Uncreated.Warfare.NewQuests.Parameters;
public static class QuestParameters
{
    public static void Write<T>(this QuestParameterValue<T> value, Utf8JsonWriter writer)
    {
        
    }
}

public enum ParameterValueType
{
    /// <summary>
    /// Entered as a constant value with no extra symbols.
    /// <code>3</code>
    /// </summary>
    Constant,

    /// <summary>
    /// Entered with parenthesis.
    /// <code>$(12:16)</code>
    /// </summary>
    Range,

    /// <summary>
    /// Entered with square brackets.
    /// <code>#[usrif1,usrif2,usrif3]</code>
    /// </summary>
    List,

    /// <summary>
    /// Represented by an asterisk.
    /// <code>$*</code>
    /// </summary>
    Wildcard
}

public enum ParameterSelectionType
{
    /// <summary>
    /// Represented by a <c>$</c>. Selects one item from the dynamic value.
    /// </summary>
    Selective,

    /// <summary>
    /// Represented by a <c>#</c>. Selects any item that matches the dynamic value.
    /// </summary>
    Inclusive
}