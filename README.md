# HouseRules

A .NET coding standard that the build enforces, for people and AI agents alike.

AI agents now write much of the code, and they drift from a style guide they read once. A build error holds where a
document does not: the agent reads the error and fixes it. HouseRules turns a complete C# standard into build
errors whose messages say exactly what to change, with fixes that `dotnet format` applies. One package reference
brings strict build settings, the .NET SDK analyzers, Roslynator's wrapping rules, a banned-API list and HouseRules'
own rules, in three levels a codebase can adopt one at a time, with a ratchet so existing code does not block the
switch.

## Install

```bash
dotnet add package HouseRules.Analyzers
```

For a whole repository, reference it once in `Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <HouseRulesLevel>Strict</HouseRulesLevel>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="HouseRules.Analyzers" Version="1.0.0" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

## Quick start

Add the package and build:

```bash
dotnet build
```

Findings are build errors that name the change to make:

```text
Violations.cs(13,13): error HR0001: Add a blank line before this statement: the statement above spans several lines ('dotnet format' fixes this) (https://github.com/Hanif6731/HouseRules/blob/main/docs/rules/HR0001.md)
Violations.cs(26,1): error RCS0056: A line is too long (122 characters) (https://josefpihrt.github.io/docs/roslynator/analyzers/RCS0056)
```

Layout findings fix themselves:

```bash
dotnet format
```

What remains needs a decision: a weak hash, a clock read that a test cannot control, a name that breaks the
convention.

## Usage

### Choose a level

Set `HouseRulesLevel` in `Directory.Build.props` (or on the command line), not in a project file: the package
applies build settings before the project file is read, and the build stops if a project file changes the level.
A `Directory.Build.props` next to one project sets the level for that project alone.

| Level | What it adds |
|---|---|
| `Baseline` | Security and reliability rules, warnings as errors, and the layout rules `dotnet format` fixes. Costs one mechanical commit plus the security fixes. |
| `Recommended` (default) | Design, maintainability, naming and usage rules whose fixes need judgement; bans on the wall clock, `Random`, `Console` and environment reads. |
| `Strict` | Nullable reference types, every compiler warning wave, code metrics and the remaining style rules. |

Each level contains the one before it. [docs/levels.md](docs/levels.md) lists every setting and rule per level, so
the cost of the next step is known before taking it.

### Adopt a level with the ratchet

Record the current findings of each project in a `houserules.baseline` file next to it:

```bash
dotnet build -p:HouseRulesBaseline=init
```

Commit the file. The build now tolerates exactly the recorded findings and fails on every new one, so features keep
shipping while old code is cleaned up. After fixing findings, shrink the file:

```bash
dotnet build -p:HouseRulesBaseline=prune
```

A baseline never grows: `prune` only removes entries, and `init` refuses to run over an existing file. An entry is
the file, the rule and a hash of the line's text, so edits elsewhere in the file do not disturb it.

### Turn off a rule or a ban

A rule, in your `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.HR0001.severity = none
```

A ban category, in the project file or `Directory.Build.props`; for example, code that may read the clock directly:

```xml
<HouseRulesBanWallClock>false</HouseRulesBanWallClock>
```

The switches are `HouseRulesBanWeakCrypto`, `HouseRulesBanHttpClient`, `HouseRulesBanWallClock`,
`HouseRulesBanRandom`, `HouseRulesBanConsole` and `HouseRulesBanEnvironment`. Any build setting the package sets,
such as `TreatWarningsAsErrors`, keeps the value your project gives it.

### Use it with AI agents

Paste this into the repository's `AGENTS.md` or `CLAUDE.md`:

```markdown
Run `dotnet build` before you finish. On HouseRules errors, run `dotnet format`, then fix what remains by hand.
Never suppress a rule or edit `houserules.baseline` to make the build pass.
```

The build output is the whole interface; no agent-specific tooling is needed.

## Rules

| ID | Rule | Fixed by `dotnet format` |
|---|---|---|
| [HR0001](docs/rules/HR0001.md) | Blank line after a multi-line statement | yes |

Every SDK and Roslynator rule the levels configure is in [docs/levels.md](docs/levels.md).

## Requirements

- .NET 8 SDK or later, SDK-style C# projects. The analyzers are built against Roslyn 4.8, the compiler in .NET SDK
  8.0.100.
- C# only.

Restore-time settings a package cannot set are worth adding yourself: central package management, lock files,
NuGet audit, and a locked restore in CI:

```bash
dotnet restore --locked-mode
```

## Versioning

HouseRules follows [Semantic Versioning](https://semver.org/). Only a major version can add a build error, at any
level: a new rule, a higher severity, or a dependency update that reports more. Minor and patch versions never do.
Each major pins `AnalysisLevel` and the versions of Roslynator and BannedApiAnalyzers; the SDK analyzers themselves
come from your SDK, so upgrading the SDK can still change what an existing rule reports.
See [CHANGELOG.md](CHANGELOG.md).

## Contributing

Build, test and run the package smoke test:

```bash
dotnet build
dotnet test
dotnet pack src/HouseRules.Analyzers.Package -c Release -o artifacts
tests/smoke/smoke.sh artifacts
```

Levels, rule severities and ban categories live in `catalogue/rules.json`. After changing it, regenerate the level
configs and `docs/levels.md`; a test fails until you do:

```bash
dotnet run --project tools/Generate
```

To propose a rule, open an issue first. A new rule covers what no maintained analyzer does, and arrives with an
analyzer, a code fix, tests and a docs page. `main` is kept green, and the repository builds itself at `Strict`.

## Credits

- [Roslynator](https://github.com/dotnet/roslynator) provides the wrapping and line-length rules.
- [Microsoft.CodeAnalysis.BannedApiAnalyzers](https://github.com/dotnet/roslyn-analyzers) enforces the banned APIs.
- [Meziantou.DotNet.CodingStandard](https://github.com/meziantou/Meziantou.DotNet.CodingStandard) showed that a
  coding standard can ship as one package.

## License

[MIT](LICENSE)
