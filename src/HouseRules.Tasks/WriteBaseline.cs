using HouseRules.Analyzers;

using Microsoft.Build.Framework;

namespace HouseRules.Tasks;

/// <summary>Creates (<c>init</c>) or shrinks (<c>prune</c>) <c>houserules.baseline</c> from the compiler's error log.</summary>
public sealed class WriteBaseline : Microsoft.Build.Utilities.Task
{
    private static readonly Lazy<HashSet<string>> SuppressibleIds = new(LoadSuppressibleIds);

    [Required]
    public string Mode { get; set; } = "";

    [Required]
    public string ErrorLog { get; set; } = "";

    [Required]
    public string BaselineFile { get; set; } = "";

    public override bool Execute()
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(BaselineFile))!;
        var findings = Baseline.ReadErrorLog(File.ReadAllText(ErrorLog));
        if (Mode == "init")
        {
            var unrecordable = Baseline.Unrecordable(findings, SuppressibleIds.Value.Contains);
            if (unrecordable.Count > 0)
            {
                Log.LogError(
                    $"HouseRules: findings of {string.Join(", ", unrecordable)} cannot be recorded in "
                        + $"{BaselineKey.FileName}, so no baseline was written. Fix them, or set their severity in "
                        + "your .editorconfig, then run: dotnet build -p:HouseRulesBaseline=init");

                return false;
            }
        }

        var entries = Mode == "init"
            ? Baseline.Init(findings, directory, File.ReadAllLines)
            : Baseline.Prune(Baseline.Parse(File.ReadAllText(BaselineFile)), findings, directory, File.ReadAllLines);

        var total = entries.Values.Sum();
        if (total == 0)
        {
            File.Delete(BaselineFile);
            Log.LogMessage(
                MessageImportance.High,
                $"HouseRules: no findings to record; {BaselineKey.FileName} removed.");

            return true;
        }

        File.WriteAllText(BaselineFile, Baseline.Format(entries));
        Log.LogMessage(MessageImportance.High, $"HouseRules: {BaselineFile} records {total} finding(s).");
        return true;
    }

    private static HashSet<string> LoadSuppressibleIds()
    {
        using var stream = typeof(WriteBaseline).Assembly.GetManifestResourceStream(
            "HouseRules.Tasks.SuppressibleIds.txt")!;

        using var reader = new StreamReader(stream);
        return [.. reader.ReadToEnd().Split(['\n'], StringSplitOptions.RemoveEmptyEntries)];
    }
}
