# Levels

<!-- Generated from catalogue/rules.json by tools/Generate. Do not edit. -->

Choose a level with `<HouseRulesLevel>` in `Directory.Build.props`; the default is `Recommended`. Each level contains everything in the levels before it, so the tables under a level list exactly what moving up to it costs. Generated from [the rules catalogue](../catalogue/rules.json).

## Baseline

Security and reliability rules, plus the layout rules `dotnet format` fixes without judgement. Turning it on costs one mechanical commit and the security fixes.

### Build settings

| Property | Value | Why |
|---|---|---|
| `TreatWarningsAsErrors` | `true` | Every reported warning fails the build. |
| `EnforceCodeStyleInBuild` | `true` | Code style (IDE) rules run in the build, not only in the editor. |
| `EnableNETAnalyzers` | `true` | The .NET SDK code analysis rules run in every project, including .NET Standard ones. |
| `AnalysisLevel` | `10.0` | Which SDK rules exist is pinned per HouseRules major, so an SDK update adds no rules. |
| `Deterministic` | `true` | Identical source gives identical binaries. |
| `ContinuousIntegrationBuild` | `true` (when `'$(CI)' == 'true'`) | CI builds embed normalised paths. |

### Code analysis categories

| Category | Mode |
|---|---|
| Security | `All` |
| Reliability | `All` |

### Rules

| ID | Source | What it enforces | Severity | Fixed by `dotnet format` |
|---|---|---|---|---|
| [HR0001](rules/HR0001.md) | HouseRules | Blank line after a multi-line statement. | warning | yes |
| [IDE0055](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0055) | .NET SDK code style | Formatting: Allman braces, indentation, spacing. | warning | yes |
| [IDE0011](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0011) | .NET SDK code style | Braces on every if, else, for, foreach, while and using body. | warning | yes |
| [IDE0161](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0161) | .NET SDK code style | File-scoped namespaces. | warning | yes |
| [IDE0065](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0065) | .NET SDK code style | Using directives outside the namespace. | warning | yes |
| [RCS0056](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS0056) | Roslynator | Lines are at most 120 characters long. | warning | yes |
| [RCS0054](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS0054) | Roslynator | Once a call chain wraps, one call per line. | warning | yes |
| [RCS0053](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS0053) | Roslynator | Argument and parameter lists: on one line, or one element per line, indented in 4-space steps unless an `.editorconfig` says otherwise. | warning | yes |
| [RCS0032](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS0032) | Roslynator | A wrapped expression body breaks after `=>`. | warning | yes |
| [RCS0052](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS0052) | Roslynator | A wrapped assignment breaks after `=`. | warning | yes |
| [RCS0027](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS0027) | Roslynator | A wrapped binary expression puts the operator first on the new line. | warning | yes |
| [RCS0028](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS0028) | Roslynator | A wrapped conditional expression puts `?` and `:` first on the new line. | warning | yes |
| [RCS0059](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS0059) | Roslynator | A wrapped null-conditional access puts `?.` first on the new line. | warning | yes |
| [RCS0055](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS0055) | Roslynator | Once a binary expression wraps, it breaks at every operator. | warning | yes |
| [CA2100](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2100) | .NET SDK code analysis | SQL built from strings is reviewed for injection. | warning | no |
| [CA3075](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca3075) | .NET SDK code analysis | No insecure DTD processing in XML. | warning | no |
| [CA5350](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca5350) | .NET SDK code analysis | No weak cryptographic algorithms. | warning | no |
| [CA5351](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca5351) | .NET SDK code analysis | No broken cryptographic algorithms. | warning | no |
| [CA5394](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca5394) | .NET SDK code analysis | No insecure randomness. | warning | no |
| [CA2022](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2022) | .NET SDK code analysis | A `Stream.Read` handles a short read: use `ReadExactly` or the count it returns. | warning | yes |
| [CA2023](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2023) | .NET SDK code analysis | Logging message templates have balanced braces. | warning | no |
| [CA2024](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2024) | .NET SDK code analysis | No `StreamReader.EndOfStream` in async methods: it blocks. | warning | no |
| [CA2025](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2025) | .NET SDK code analysis | No `IDisposable` handed to a task that may still run after it is disposed. | warning | no |
| [CA2264](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2264) | .NET SDK code analysis | No `ArgumentNullException.ThrowIfNull` on a value that cannot be null: the check never fires. | warning | yes |
| [CA2265](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2265) | .NET SDK code analysis | No comparing a `Span<T>` to `null` or `default`; test `IsEmpty`. | warning | yes |
| [CA2266](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2266) | .NET SDK code analysis | Off: the SDK 10.0.1xx analyzers lack it, so turning it on would let an SDK update add a rule. It checks file-based programs only. | none | no |
| [CA2007](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2007) | .NET SDK code analysis | Off: ConfigureAwait on every await is noise in apps without a synchronization context. | none | no |

## Recommended

Adds design, maintainability, naming and usage rules whose fixes need human judgement.

### Build settings

| Property | Value | Why |
|---|---|---|
| `GenerateDocumentationFile` | `true` | Lets IDE0005 (unused usings) run in the build; missing doc comments (CS1591) stay off. A project that sets it to false loses IDE0005 instead of failing the build. |

### Code analysis categories

| Category | Mode |
|---|---|
| Usage | `All` |
| Performance | `Recommended` |
| Maintainability | `Recommended` |
| Design | `Recommended` |
| Naming | `Recommended` |
| Globalization | `Recommended` |

### Rules

| ID | Source | What it enforces | Severity | Fixed by `dotnet format` |
|---|---|---|---|---|
| [IDE1006](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide1006) | .NET SDK code style | Naming: IPrefix interfaces, TPrefix type parameters, PascalCase types and members (not namespaces, which may start with a brand such as xUnit), _camelCase private fields, camelCase locals and parameters. No Async suffix rule: ASP.NET Core actions go without it. | warning | yes |
| [IDE0005](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0005) | .NET SDK code style | No unused using directives. | warning | yes |
| [CS1591](https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-messages/cs1591) | C# compiler | Off: documentation files are generated for IDE0005, doc comments stay optional. | none | no |
| [CA2016](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2016) | .NET SDK code analysis | Pass the CancellationToken on when the callee accepts one. | warning | yes |
| [CA1514](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1514) | .NET SDK code analysis | No redundant length argument when slicing to the end. | warning | yes |
| [CA1871](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1871) | .NET SDK code analysis | No nullable struct passed to `ArgumentNullException.ThrowIfNull`: it boxes. | warning | no |
| [CA1872](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1872) | .NET SDK code analysis | `Convert.ToHexString` instead of `BitConverter.ToString` call chains. | warning | yes |
| [CA1873](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1873) | .NET SDK code analysis | Logging arguments that are costly to compute are guarded by `IsEnabled`. | warning | no |
| [CA1874](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1874) | .NET SDK code analysis | `Regex.IsMatch` instead of `Regex.Match(...).Success`. | warning | yes |
| [CA1875](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1875) | .NET SDK code analysis | `Regex.Count` instead of `Regex.Matches(...).Count`. | warning | yes |
| [CA2262](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2262) | .NET SDK code analysis | `MaxResponseHeadersLength` is set in kilobytes, not bytes. | warning | no |
| [CA2263](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2263) | .NET SDK code analysis | The generic overload when the type is known at compile time. | warning | yes |
| [IDE0040](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0040) | .NET SDK code style | Explicit accessibility modifiers. | warning | yes |
| [IDE0044](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0044) | .NET SDK code style | Fields that never change are readonly. | warning | yes |
| [IDE0036](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0036) | .NET SDK code style | Modifiers in one order. | warning | yes |
| [IDE0003](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0003) | .NET SDK code style | No `this.` qualification. | warning | yes |
| [IDE0009](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0009) | .NET SDK code style | No `this.` qualification (the opposite preference is off). | warning | yes |
| [IDE0049](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0049) | .NET SDK code style | Language keywords over BCL type names (`string`, not `String`). | warning | yes |
| [IDE0017](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0017) | .NET SDK code style | Object initializers. | warning | yes |
| [IDE0028](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0028) | .NET SDK code style | Collection initializers. | warning | yes |
| [IDE0300](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0300) | .NET SDK code style | Collection expressions for arrays. | warning | yes |
| [IDE0305](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0305) | .NET SDK code style | Suggestion: collection expressions instead of fluent `ToList()`/`ToArray()` calls. | suggestion | yes |
| [IDE0029](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0029) | .NET SDK code style | `??` instead of a null-check ternary. | warning | yes |
| [IDE0030](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0030) | .NET SDK code style | `??` for nullable value types. | warning | yes |
| [IDE0031](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0031) | .NET SDK code style | Suggestion: `?.` instead of a null-check ternary. Roslyn also suggests it after a `join` or a second `from` in a query over `IQueryable`, where `?.` does not compile (CS8072). | suggestion | yes |
| [IDE0041](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0041) | .NET SDK code style | `is null` instead of `ReferenceEquals`. | warning | yes |
| [IDE0032](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0032) | .NET SDK code style | Auto-properties instead of a property over a private field. | warning | yes |
| [IDE0075](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0075) | .NET SDK code style | Simplified conditional expressions. | warning | yes |
| [IDE0037](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0037) | .NET SDK code style | Inferred tuple and anonymous type member names. | warning | yes |
| [IDE0054](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0054) | .NET SDK code style | Compound assignment (`x += 1`). | warning | yes |
| [IDE0074](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0074) | .NET SDK code style | Coalesce compound assignment (`x ??= y`). | warning | yes |
| [IDE0071](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0071) | .NET SDK code style | Simplified interpolation. | warning | yes |
| [IDE0019](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0019) | .NET SDK code style | Pattern matching instead of `as` plus a null check. | warning | yes |
| [IDE0020](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0020) | .NET SDK code style | Pattern matching instead of `is` plus a cast. | warning | yes |
| [IDE0083](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0083) | .NET SDK code style | `is not` patterns. | warning | yes |
| [IDE0150](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0150) | .NET SDK code style | A null check instead of a type check when the type is known. | warning | yes |
| [IDE1005](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide1005) | .NET SDK code style | `?.Invoke` for delegates. | warning | yes |
| [IDE0018](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0018) | .NET SDK code style | Inline `out` variable declarations. | warning | yes |
| [IDE0056](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0056) | .NET SDK code style | Index operator (`^1`). | warning | yes |
| [IDE0057](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0057) | .NET SDK code style | Range operator (`a..b`). | warning | yes |
| [IDE0090](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0090) | .NET SDK code style | Target-typed `new()` when the type is apparent. | warning | yes |
| [IDE0034](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0034) | .NET SDK code style | `default` instead of `default(T)`. | warning | yes |
| [IDE0063](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0063) | .NET SDK code style | Using declarations instead of using blocks. | warning | yes |
| [IDE0039](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0039) | .NET SDK code style | Local functions instead of lambdas stored in locals. | warning | yes |
| [IDE0062](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0062) | .NET SDK code style | Static local functions when nothing is captured. | warning | yes |
| [IDE0170](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0170) | .NET SDK code style | Extended property patterns. | warning | yes |
| [IDE0051](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0051) | .NET SDK code style | No unused private members; constructors are exempt, since EF Core and serializers call them through reflection. | warning | yes |
| [IDE0052](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0052) | .NET SDK code style | No private members that are written but never read. | warning | yes |
| [IDE0060](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0060) | .NET SDK code style | No unused parameters on non-public members. | warning | yes |
| [IDE0059](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0059) | .NET SDK code style | No assignments whose value is never used. | warning | yes |
| [IDE0035](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0035) | .NET SDK code style | No unreachable code. | warning | yes |
| [IDE0004](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0004) | .NET SDK code style | No unnecessary casts. | warning | yes |
| [IDE0001](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0001) | .NET SDK code style | Simplified names. | warning | yes |
| [IDE0002](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0002) | .NET SDK code style | Simplified member access. | warning | yes |
| [IDE0100](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0100) | .NET SDK code style | No redundant `== true` comparisons. | warning | yes |
| [IDE0079](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0079) | .NET SDK code style | No unnecessary suppressions. | warning | yes |
| [IDE0072](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0072) | .NET SDK code style | Switch expressions handle every enum member, so a new member fails the build. | warning | yes |

## Strict

Adds nullable reference types, every compiler warning wave, code metrics, the remaining style rules and bans that push a design: injected clocks, randomness, logging, configuration and HTTP clients.

### Build settings

| Property | Value | Why |
|---|---|---|
| `Nullable` | `enable` | Nullable reference types. |
| `Features` | `strict` | The compiler's strict mode. |
| `WarningLevel` | `9999` | Every compiler warning wave. |

### Rules

| ID | Source | What it enforces | Severity | Fixed by `dotnet format` |
|---|---|---|---|---|
| [RS0030](https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md) | BannedApiAnalyzers | Banned APIs, in switchable categories. Categories: [Strict](#banned-apis-at-strict). | warning | no |
| [IDE0130](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0130) | .NET SDK code style | Namespaces match the folder structure. | warning | yes |
| [IDE0250](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0250) | .NET SDK code style | Readonly structs when no member mutates. | warning | yes |
| [IDE0251](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0251) | .NET SDK code style | Readonly struct members when they do not mutate. | warning | yes |
| [CA1502](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1502) | .NET SDK code analysis | Cyclomatic complexity at most 25 per method, the .NET default. | warning | no |
| [CA1505](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1505) | .NET SDK code analysis | Maintainability index at least 10, the .NET default. | warning | no |
| [CA1506](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1506) | .NET SDK code analysis | Class coupling at most 40 per method and 95 per type, the .NET defaults. | warning | no |
| [CS8602](https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-messages/nullable-warnings#possible-dereference-of-null) | C# compiler | No dereference of a possibly null reference. | warning | no |
| [CS8618](https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-messages/nullable-warnings#nonnullable-reference-not-initialized) | C# compiler | Non-nullable members are initialised by every constructor. | warning | no |
| [CS1572](https://learn.microsoft.com/dotnet/csharp/misc/cs1572) | C# compiler | XML doc `param` tags name real parameters. | warning | no |
| [CS1573](https://learn.microsoft.com/dotnet/csharp/misc/cs1573) | C# compiler | XML docs that document some parameters document all of them. | warning | no |
| [IDE0010](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0010) | .NET SDK code style | Suggestion: switch statements over an enum handle every member. | suggestion | yes |
| [IDE0007](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0007) | .NET SDK code style | Suggestion: `var`. | suggestion | yes |
| [IDE0047](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0047) | .NET SDK code style | Suggestion: parentheses only where they clarify precedence. | suggestion | yes |
| [IDE0045](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0045) | .NET SDK code style | Suggestion: conditional expressions for assignments. | suggestion | yes |
| [IDE0046](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0046) | .NET SDK code style | Suggestion: conditional expressions for returns. | suggestion | yes |
| [IDE0078](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0078) | .NET SDK code style | Suggestion: pattern matching. | suggestion | yes |
| [IDE0066](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0066) | .NET SDK code style | Suggestion: switch expressions. | suggestion | yes |
| [IDE0016](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0016) | .NET SDK code style | Suggestion: throw expressions. | suggestion | yes |
| [IDE0042](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0042) | .NET SDK code style | Suggestion: deconstruct tuples into variables. | suggestion | yes |
| [IDE0230](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0230) | .NET SDK code style | Suggestion: UTF-8 string literals. | suggestion | yes |
| [IDE0180](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0180) | .NET SDK code style | Suggestion: tuple swap. | suggestion | yes |
| [IDE0290](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0290) | .NET SDK code style | Suggestion: primary constructors. | suggestion | yes |
| [IDE0200](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0200) | .NET SDK code style | Suggestion: method group conversions instead of lambdas. | suggestion | yes |
| [IDE0320](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0320) | .NET SDK code style | Suggestion: static anonymous functions. | suggestion | yes |
| [IDE0058](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0058) | .NET SDK code style | Suggestion: discard an unused expression value explicitly. | suggestion | yes |
| [IDE0022](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0022) | .NET SDK code style | Suggestion: expression bodies only for one-line members and accessors. | suggestion | yes |

### Banned APIs at Strict

| Category | Turn off with | What it bans |
|---|---|---|
| HttpClient | `<HouseRulesBanHttpClient>false</HouseRulesBanHttpClient>` | Constructing HttpClient or WebClient directly. |
| WallClock | `<HouseRulesBanWallClock>false</HouseRulesBanWallClock>` | Reading the clock directly (DateTime.Now, UtcNow, Today; DateTimeOffset.Now, UtcNow). |
| Random | `<HouseRulesBanRandom>false</HouseRulesBanRandom>` | System.Random. |
| Console | `<HouseRulesBanConsole>false</HouseRulesBanConsole>` | System.Console. |
| Environment | `<HouseRulesBanEnvironment>false</HouseRulesBanEnvironment>` | Reading environment variables directly. |

## Test projects

Test projects (`IsTestProject` or `IsTestingPlatformApplication` is `true`) also get these relaxations (naming and code metrics, never bans):

| Setting | Value | Why |
|---|---|---|
| `dotnet_diagnostic.CA1707.severity` | `none` | Test names are sentences with underscores. |
| `dotnet_diagnostic.CA1502.severity` | `none` | Code metrics judge production design; a test's size and coupling are its fixture. |
| `dotnet_diagnostic.CA1505.severity` | `none` | Code metrics judge production design; a test's size and coupling are its fixture. |
| `dotnet_diagnostic.CA1506.severity` | `none` | Code metrics judge production design; a test's size and coupling are its fixture. |
