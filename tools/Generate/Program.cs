using HouseRules.Generate;

var repoRoot = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
var files = CatalogueGenerator.Generate(repoRoot);
foreach (var (path, content) in files)
{
    var file = Path.Combine(repoRoot, path);
    Directory.CreateDirectory(Path.GetDirectoryName(file)!);
    File.WriteAllText(file, content);
}

Console.WriteLine($"Wrote {files.Count} files from {CatalogueGenerator.CataloguePath}.");
