using System.Text.RegularExpressions;

namespace Sample;

public static class InfoFinding
{
    public static bool HasRun(string text, bool fallback)
    {
        var found = new Regex("a+").IsMatch(text)
            || fallback;
        return found;
    }
}
