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

    /// <summary>
    /// The path from the baseline's folder to the file with forward slashes, climbing with <c>..</c> for a file
    /// linked from outside the folder, so the baseline is the same on every machine. A file on another drive has no
    /// relative path and keeps its full one.
    /// </summary>
    public static string RelativePath(string baselineDirectory, string filePath)
    {
        var from = Path.GetFullPath(baselineDirectory).Replace('\\', '/').TrimEnd('/').Split('/');
        var to = Path.GetFullPath(filePath).Replace('\\', '/').Split('/');
        var common = 0;
        while (common < from.Length
            && common < to.Length - 1
            && string.Equals(from[common], to[common], StringComparison.OrdinalIgnoreCase))
        {
            common++;
        }

        return common == 0
            ? string.Join("/", to)
            : string.Join("/", Enumerable.Repeat("..", from.Length - common).Concat(to.Skip(common)));
    }
}
