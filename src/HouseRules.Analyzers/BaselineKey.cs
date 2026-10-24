using System.Security.Cryptography;
using System.Text;

namespace HouseRules.Analyzers;

/// <summary>
/// One entry of <c>houserules.baseline</c>: <c>path|rule id|SHA-256 of the trimmed source line</c>, with the path
/// relative to the baseline's folder. The line text, not its number, identifies a finding, so editing code above it
/// does not invalidate the entry. Compiled into both the suppressor and the MSBuild task that writes the file.
/// </summary>
internal static class BaselineKey
{
    public const string FileName = "houserules.baseline";

    public static string Of(string relativePath, string id, string lineText)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(lineText.Trim()));
        var hex = new StringBuilder(hash.Length * 2);
        foreach (var value in hash)
        {
            _ = hex.Append(value.ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
        }

        return $"{relativePath}|{id}|{hex}";
    }

    public static string RelativePath(string baselineDirectory, string filePath)
    {
        var root = Path.GetFullPath(baselineDirectory).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
        var full = Path.GetFullPath(filePath);
        var relative = full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? full.Substring(root.Length) : full;
        return relative.Replace('\\', '/');
    }
}
