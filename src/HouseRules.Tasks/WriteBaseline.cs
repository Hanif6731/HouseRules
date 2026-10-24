using HouseRules.Analyzers;

using Microsoft.Build.Framework;

namespace HouseRules.Tasks;

/// <summary>Creates (<c>init</c>) or shrinks (<c>prune</c>) <c>houserules.baseline</c> from the compiler's error log.</summary>
public sealed class WriteBaseline : Microsoft.Build.Utilities.Task
{
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
}
