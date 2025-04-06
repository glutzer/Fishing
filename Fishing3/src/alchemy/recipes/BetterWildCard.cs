using System.Text.RegularExpressions;
using Vintagestory.API.Common;

namespace Fishing3;

public static class BetterWildCard
{
    /// <summary>
    /// Convert a pattern like *-pattern-* to regex.
    /// </summary>
    public static string ConvertToWildCard(string pattern)
    {
        return Regex.Escape(pattern).Replace(@"\*", @"(.*)");
    }

    /// <summary>
    /// Does a stack match a regex?
    /// </summary>
    public static bool Matches(ItemStack stack, string regex)
    {
        string code = stack.Collectible.Code;
        return Regex.IsMatch(code, regex);
    }
}