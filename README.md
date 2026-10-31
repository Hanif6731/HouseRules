# HouseRules

[![NuGet](https://img.shields.io/nuget/vpre/HouseRules.Analyzers)](https://www.nuget.org/packages/HouseRules.Analyzers)
[![CI](https://github.com/Hanif6731/HouseRules/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Hanif6731/HouseRules/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/github/license/Hanif6731/HouseRules)](LICENSE)

<img align="right" width="100" alt="HouseRules logo" src="https://raw.githubusercontent.com/Hanif6731/HouseRules/main/docs/logo.svg">

A .NET coding standard that the build enforces, for people and AI agents alike.

AI agents now write much of the code, and they drift from a style guide they read once. A build error holds where a
document does not: the agent reads the error and fixes it. HouseRules turns a complete C# standard into build
errors whose messages say exactly what to change, with fixes that `dotnet format` applies. One package reference
brings strict build settings, the .NET SDK analyzers, Roslynator's wrapping rules, a banned-API list and HouseRules'
own rules, in three levels a codebase can adopt one at a time, with a ratchet so existing code does not block the
switch.

The style is not a new one. Layout, naming and file structure follow the
[.NET runtime coding style](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md) and
[Microsoft's C# conventions](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions),
and long lines wrap the way [CSharpier](https://csharpier.com) wraps them: operator first, one call per line, one
argument per line. Bans that push a design, such as reading `DateTime.UtcNow` directly, start at `Strict`.

## Install

```bash
dotnet add package HouseRules.Analyzers --prerelease
```

For a whole repository, reference it once in `Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <HouseRulesLevel>Strict</HouseRulesLevel>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="HouseRules.Analyzers" Version="1.0.0-preview.4" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

1.0.0-preview.4 is a preview; 1.0.0 follows it, and the [release policy](CHANGELOG.md) applies from 1.0.0.

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

`dotnet format` fixes what it found before its first fix, so a statement it wraps gets its HR0001 blank line only on
the next run: run `dotnet format` again if the build reports HR0001 after wrapping.

What remains needs a decision: a weak hash, a name that breaks the convention, a cancellation token not passed on.

## Usage

### Choose a level

Set `HouseRulesLevel` in `Directory.Build.props` (or on the command line), not in a project file: the package
applies build settings before the project file is read, and the build stops if a project file changes the level.
To give one project its own level, put a `Directory.Build.props` next to it that imports the repository's one
first; without the import, MSBuild would skip the root file and the project would lose the package:

```xml
<Project>
  <Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />
  <PropertyGroup>
    <HouseRulesLevel>Baseline</HouseRulesLevel>
  </PropertyGroup>
</Project>
```

| Level | What it adds |
|---|---|
| `Baseline` | Security and reliability rules, warnings as errors, and the layout rules `dotnet format` fixes. Costs one mechanical commit plus the security fixes. |
| `Recommended` (default) | Design, maintainability, naming and usage rules whose fixes need judgement. |
| `Strict` | Nullable reference types, every compiler warning wave, code metrics, the remaining style rules, and bans on the wall clock, `Random`, `Console`, environment reads and `new HttpClient()`. |

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

A baseline never grows: `prune` only removes entries, and `init` refuses to run over an existing file. A finding
that is now suppressed in source, with a pragma or `[SuppressMessage]`, counts as fixed. Run on a solution, `prune`
skips the projects that have no baseline. An entry is
the file's path relative to the project folder (also for a file linked from outside it), the rule and a hash of the
line's text, so the file reads the same on every machine and edits elsewhere in the file do not disturb it; renaming
the file does.

The ratchet covers the rules HouseRules configures, compiler warnings and the SDK code analysis rules. When `init`
meets a finding it cannot record, such as another package's analyzer or a custom `[Obsolete]` id, it names the rule
and writes nothing; fix those findings or set their severity in `.editorconfig` first. Projects with several target
frameworks are not supported yet. Code generated with `#line` directives is matched on the mapped file when that
file is an additional file, as Razor files are.

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

The switches are `HouseRulesBanHttpClient`, `HouseRulesBanWallClock`, `HouseRulesBanRandom`,
`HouseRulesBanConsole` and `HouseRulesBanEnvironment`. Any build setting the package sets, such as
`TreatWarningsAsErrors`, keeps the value your project gives it. That includes `AnalysisLevel`, but a mode suffix such
as `latest-recommended` also replaces the per-category modes the levels set, so security and reliability rules
quietly drop to the recommended set; give it a plain version such as `10.0`, or leave it to the package. With
`GenerateDocumentationFile` off, IDE0005 (unused usings) is off too, because it needs the documentation file. A project
that sets its own `CodeAnalysisRuleSet` replaces the package's rule set, so `dotnet format` then sees only its
`.editorconfig` severities; its `.editorconfig` still overrides everything.

### Exempt generated code

Code a tool regenerates, such as EF Core migrations, is not worth reformatting. Mark it as generated in the
repository's `.editorconfig`; layout and style rules then skip it, while security rules still run:

```ini
[**/Migrations/*.cs]
generated_code = true
```

### Build in Docker

Exemptions, severities and indentation options in `.editorconfig` change what the build reports, so a Docker build
whose context leaves out the repository's `.editorconfig` reports differently from your machine. Copy it in with the
sources, and keep it out of `.dockerignore`:

```dockerfile
COPY .editorconfig Directory.Build.props ./
```

### Use it with AI agents

Paste this into the repository's `AGENTS.md` or `CLAUDE.md`:

```markdown
Run `dotnet build` before you finish. On HouseRules errors, run `dotnet format`, then fix what remains by hand.
Never suppress a rule or edit `houserules.baseline` to make the build pass.
```

The build output is the whole interface; no agent-specific tooling is needed.

## Known limitations

Roslynator's RCS0053 misreads two common layouts, and `dotnet format` cannot settle them: whatever it writes, RCS0053
or IDE0055 reports again.

- An anonymous object that is the only argument, or a lambda's whole body as the only argument, with `{` on the
  next line: `Serialize(new` or `.Select(x => new`. A named type (`new Row`) is fine.
  ([roslynator#1851](https://github.com/dotnet/roslynator/issues/1851))
- A block (a lambda body, object initializer or switch expression) that is the only argument in a top-level
  statement, with `{` in column 0: `builder.Services.AddCors(options =>`. It appears whenever Roslynator knows the
  indentation step, from `indent_size` (which the levels set) or from the indentation of the file's other code.
  ([roslynator#1852](https://github.com/dotnet/roslynator/issues/1852))

Both are fixed in [roslynator#1858](https://github.com/dotnet/roslynator/pull/1858), merged after 5.0.1. Until a
Roslynator release includes it, record the sites in the baseline or turn RCS0053 off for the files concerned.

## Rules

| ID | Rule | Fixed by `dotnet format` |
|---|---|---|
| [HR0001](docs/rules/HR0001.md) | Blank line after a multi-line statement | yes |

Every SDK and Roslynator rule the levels configure is in [docs/levels.md](docs/levels.md).

## Requirements

- .NET 10 SDK or later; projects may target any framework the SDK builds, including net8.0. The analyzers are built
  against Roslyn 5.0, the compiler in .NET SDK 10.0.100.
- SDK-style projects.
- C# only.

Restore-time settings a package cannot set are worth adding yourself: central package management, lock files,
NuGet audit, and a locked restore in CI:

```bash
dotnet restore --locked-mode
```

## Build cost

Measured on 2026-10-24 on a 6-project ASP.NET Core backend (~57K lines in ~495 hand-written files, plus ~100 EF Core
migration files) that already built with the SDK analyzers at `latest-recommended` and code style enforced in the
build. Each configuration was restored once, then built clean five times with `dotnet build -c Release --no-restore
--no-incremental -m -p:TreatWarningsAsErrors=false`, so every build ran to the end despite the new findings. The
configurations took turns in each round, and none of the timed builds was discarded:

| Configuration | Median | Range | Over the build without HouseRules |
|---|---|---|---|
| Without HouseRules | 7.5 s | 6.7–8.7 s | |
| `Baseline` | 8.5 s | 7.7–15.6 s | +1.0 s (+14%) |
| `Recommended` | 9.0 s | 8.1–10.8 s | +1.5 s (+20%) |
| `Strict` | 9.2 s | 8.7–9.4 s | +1.7 s (+23%) |

Machine: AMD Ryzen 9 8945HX (16 cores, 32 threads), 16 GB RAM, Windows 11, .NET SDK 10.0.402, on mains power. The
build servers were warm: one untimed build per configuration ran first, so a cold CI build adds start-up time not
shown here. Single builds varied by about a second, so compare the medians.

Most of the extra time is the SDK's own analyzers and code-style rules that the levels turn on. At `Strict`,
HouseRules' analyzer, Roslynator and the banned-API analyzer took under 1 s of the 16.5 s of analyzer time summed over
the six projects, as reported by `-p:ReportAnalyzer=true`. A codebase that does not run the SDK analyzers yet starts
from a faster build and pays more. Live analysis in an IDE is not measured.

To measure your own codebase, time the same clean build several times with and without the package reference, and add
`-p:ReportAnalyzer=true -bl` to one build to see the time per analyzer in the binary log.

## Versioning

HouseRules follows [Semantic Versioning](https://semver.org/). Only a major version can add a build error, at any
level: a new rule, a higher severity, or a dependency update that reports more. Minor and patch versions never do.
Each major pins `AnalysisLevel` and the versions of Roslynator and BannedApiAnalyzers; the SDK analyzers themselves
come from your SDK, so upgrading the SDK can still change what an existing rule reports. Raising the minimum .NET
SDK also takes a major version.
See [CHANGELOG.md](CHANGELOG.md).

## What's maintained

- The latest major version. Fixes are not backported to older majors.
- .NET SDK 10 and later; CI tests every change on SDK 10.0.1xx and the newest SDK 10, with projects that target
  net10.0 and net8.0.
- C# in SDK-style projects. VB and F# are not planned.
- All three levels and the ratchet.

HouseRules has one maintainer. Issues are usually looked at within a week, false positives first, because they break
builds; there is no support agreement beyond that.

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

To propose a rule, [open a rule proposal](https://github.com/Hanif6731/HouseRules/issues/new?template=rule-proposal.yml)
first. A new rule covers what no maintained analyzer does, and arrives with an analyzer, a code fix, tests and a docs
page. `main` is kept green, and the repository builds itself at `Strict`.

## Credits

- [Roslynator](https://github.com/dotnet/roslynator) provides the wrapping and line-length rules.
- [Microsoft.CodeAnalysis.BannedApiAnalyzers](https://github.com/dotnet/roslyn-analyzers) enforces the banned APIs.
- [Workleap.DotNet.CodingStandards](https://github.com/workleap/wl-dotnet-codingstandards) is prior art for a coding
  standard consumed as one package reference, built on the SDK's own analyzers. HouseRules adds Roslynator's
  wrapping rules and its own rules with code fixes, and ships a rollout path for existing code (levels and the
  ratchet).
- [Meziantou.NET.Sdk](https://github.com/meziantou/Meziantou.NET.Sdk) is prior art for shipping a whole coding
  standard as one reference; it is meant to be forked rather than consumed directly. HouseRules is meant to be
  consumed directly, adds errors only in major versions, and ships a rollout path (levels and the ratchet).

## Support

HouseRules is free and stays free. If it saves your team time, you can support its maintenance on
[SupportKori](https://www.supportkori.com/mdhanif).

## License

[MIT](LICENSE)
