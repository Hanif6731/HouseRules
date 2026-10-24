using System.Reflection.PortableExecutable;
using System.Text;

using HouseRules.Analyzers;
using HouseRules.CodeFixes;
using HouseRules.Tasks;

namespace HouseRules.Tests;

public sealed class PackageTests
{
    private const string GitHubSources = "https://raw.githubusercontent.com/Hanif6731/HouseRules/";

    private static readonly Guid SourceLinkKind = new("CC110556-A091-4D38-9FEC-25AB9A351A6A");

    // The package ships these assemblies without .pdb files, so a debugger can only step into them through a PDB
    // embedded in the assembly that maps its sources to GitHub.
    [Fact]
    public void Every_shipped_assembly_embeds_a_pdb_with_source_link()
    {
        Type[] shipped = [typeof(RuleIds), typeof(BlankLineAfterMultiLineStatementCodeFix), typeof(Baseline)];
        foreach (var assembly in shipped.Select(type => type.Assembly))
        {
            using var pe = new PEReader(File.OpenRead(assembly.Location));
            var embedded = pe.ReadDebugDirectory()
                .Where(entry => entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb)
                .ToList();

            Assert.True(embedded.Count == 1, $"{assembly.GetName().Name} has no embedded PDB.");
            using var provider = pe.ReadEmbeddedPortablePdbDebugDirectoryData(embedded[0]);
            var pdb = provider.GetMetadataReader();
            var sourceLink = pdb.CustomDebugInformation
                .Select(pdb.GetCustomDebugInformation)
                .Where(information => pdb.GetGuid(information.Kind) == SourceLinkKind)
                .Select(information => Encoding.UTF8.GetString(pdb.GetBlobBytes(information.Value)))
                .SingleOrDefault()
                ?? "none";

            Assert.True(
                sourceLink.Contains(GitHubSources, StringComparison.Ordinal),
                $"{assembly.GetName().Name} has no Source Link to GitHub: {sourceLink}.");
        }
    }
}
