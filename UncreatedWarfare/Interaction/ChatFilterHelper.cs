using System;
using System.Text.RegularExpressions;

namespace Uncreated.Warfare.Interaction;
public static class ChatFilterHelper
{
    public static readonly Regex ChatFilter = new Regex(@"(?:[nńǹňñṅņṇṋṉn̈ɲƞᵰᶇɳȵɴｎŋǌvṼṽṿʋᶌᶌⱱⱴᴠʌｖ\|\\\/]\W{0,}[il1ÍíìĭîǐïḯĩįīỉȉȋịḭɨᵻᶖiıɪɩｉﬁIĳ\|\!]\W{0,}[gqb96ǴǵğĝǧġģḡǥɠᶃɢȝｇŋɢɢɋƣʠｑȹḂḃḅḇƀɓƃᵬᶀʙｂȸ](?!h|(?:an)|(?:[e|a|o]t)|(?:un)|(?:rab)|(?:rain)|(?:low)|(?:ue)|(?:uy))(?!n\shadi)\W{0,}[gqb96ǴǵğĝǧġģḡǥɠᶃɢȝｇŋɢɢɋƣʠｑȹḂḃḅḇƀɓƃᵬᶀʙｂȸ]{0,}\W{0,}[gqb96ǴǵğĝǧġģḡǥɠᶃɢȝｇŋɢɢɋƣʠｑȹḂḃḅḇƀɓƃᵬᶀʙｂȸ]{0,}\W{0,}[ae]{0,1}\W{0,}[r]{0,}(?:ia){0,})|(?:c\W{0,}h\W{0,}i{1,}\W{0,}n{1,}\W{0,}k{1,})|(?:[fḟƒᵮᶂꜰｆﬀﬃﬄﬁﬂ]\W{0,}[aáàâǎăãảȧạäåḁāąᶏⱥȁấầẫẩậắằẵẳặǻǡǟȃɑᴀɐɒａæᴁᴭᵆǽǣᴂ]\W{0,}[gqb96ǴǵğĝǧġģḡǥɠᶃɢȝｇŋɢɢɋƣʠｑȹḂḃḅḇƀɓƃᵬᶀʙｂȸ]{1,}\W{0,}o{0,}\W{0,}t{0,1})|(?:[kq]+\W{0,}(?:[y]\W{0,})+(?:[our]\W{0,})*[s]{1,}\W{0,}o{0,}\W{0,}t{0,1}(?!ain))|(?:[kq]+\W{0,}(?:[i1l]\W{0,}){1,}(?:y\W{0,})+(?:[o0@\*]\W{0,})*(?:[uU\*]\W{0,})*(?:[rR\*]\W{0,})*(?:[sc]\W{0,})+(?:[e]*\W{0,})+(?:[i1l]*\W{0,})*(?:[ft]*\W{0,})+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly char[] TrimChars = [ '.', '?', '\\', '/', '-', '=', '_', ',' ];
    
    /// <summary>
    /// Returns the first violation of the chat filter in a message.
    /// </summary>
    public static string? GetChatFilterViolation(string input)
    {
        Match match = ChatFilter.Match(input);
        if (!match.Success || match.Length <= 0)
            return null;

        string matchValue = match.Value.TrimEnd().TrimEnd(TrimChars);
        int len1 = matchValue.Length;
        matchValue = matchValue.TrimStart().TrimStart(TrimChars);

        int matchIndex = match.Index + (len1 - matchValue.Length);

        // whole word
        if ((matchIndex == 0 || char.IsWhiteSpace(input[matchIndex - 1]) || char.IsPunctuation(input[matchIndex - 1])) &&
            (matchIndex + matchValue.Length >= input.Length || char.IsWhiteSpace(input[matchIndex + matchValue.Length]) || IsPunctuation(input[matchIndex + matchValue.Length])))
        {
            // vibe matches the filter
            if (matchValue.Equals("vibe", StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
        }
        // .. can i be .. or .. can i go ..
        if (matchIndex - 2 >= 0 && input.Substring(matchIndex - 2, 2) is { } sub &&
            (sub.Equals("ca", StringComparison.InvariantCultureIgnoreCase) || sub.Equals("ma", StringComparison.InvariantCultureIgnoreCase)))
        {
            if ((matchIndex + matchValue.Length >= input.Length || char.IsWhiteSpace(input[matchIndex + matchValue.Length]) || IsPunctuation(input[matchIndex + matchValue.Length]))
                && matchValue.Equals("n i be", StringComparison.InvariantCultureIgnoreCase))
                return null;

            if ((matchIndex + matchValue.Length < input.Length && input[matchIndex + matchValue.Length].ToString().Equals("o", StringComparison.InvariantCultureIgnoreCase))
                && matchValue.Equals("n i g", StringComparison.InvariantCultureIgnoreCase))
                return null;
        }
        else if (matchIndex - 2 > 0 && input.Substring(matchIndex - 1, 1).Equals("o", StringComparison.InvariantCultureIgnoreCase)
                 && !(matchIndex + matchValue.Length >= input.Length || char.IsWhiteSpace(input[matchIndex + matchValue.Length + 1]) || IsPunctuation(input[matchIndex + matchValue.Length])))
        {
            // .. of a g___
            if (matchValue.Equals("f a g", StringComparison.InvariantCultureIgnoreCase))
                return null;
        }
        // .. an igla ..
        else if (matchValue.Equals("n ig", StringComparison.InvariantCultureIgnoreCase) && matchIndex > 0 &&
                 input[matchIndex - 1].ToString().Equals("a", StringComparison.InvariantCultureIgnoreCase) &&
                 matchIndex < input.Length - 2 && input.Substring(matchIndex + matchValue.Length, 2).Equals("la", StringComparison.InvariantCultureIgnoreCase))
        {
            return null;
        }

        return matchValue;
    }

    private static bool IsPunctuation(char c)
    {
        return c is '.' or '?' or '\\' or '/' or '-' or '=' or '_' or ',';
    }
}
