namespace HouseRules.Generate;

public sealed record Catalogue(
    IReadOnlyList<PropertyEntry> Properties,
    IReadOnlyList<CategoryEntry> Categories,
    IReadOnlyList<BanEntry> Banned,
    IReadOnlyList<RuleEntry> Rules,
    IReadOnlyList<TestEntry> Tests);

public sealed record PropertyEntry(
    string Name,
    string Value,
    string Level,
    string Since,
    string Description,
    string? Condition);

public sealed record CategoryEntry(string Name, string Mode, string Level, string Since);

public sealed record BanEntry(string Category, string Switch, string Level, string Since, string Description);

public sealed record RuleEntry(
    string Id,
    string Source,
    string Severity,
    string Level,
    string Since,
    bool Fixable,
    string Description,
    IReadOnlyDictionary<string, string>? Options,
    string? AdditionalFile,
    string? Docs);

public sealed record TestEntry(string Key, string Value, string Description);
