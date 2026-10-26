# Changelog

All notable changes to HouseRules.Analyzers are listed here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and versions follow [Semantic Versioning](https://semver.org/).

**Release policy:** anything that can add a build error at any level (a new rule, a higher severity, a dependency
update that reports more) ships only in a major version and is listed here. Minor and patch versions never add
errors.

## [Unreleased]

## [1.0.0-preview.4] - 2026-10-26

The fourth preview of 1.0.0. `Strict` reports oversized code by the .NET default limits.

### Changed

- `Strict` uses the .NET default code-metric limits: cyclomatic complexity 25 (was 15, CA1502), maintainability index
  10 (was 20, CA1505), and class coupling 40 per member and 95 per type (was 30 and 60, CA1506). The stricter limits
  reported ordinary EF Core handlers, which touch their entities, the DbContext and LINQ, and composition roots such
  as `Program.cs` and a DbContext, which no refactoring can bring under them.

## [1.0.0-preview.3] - 2026-10-26

The third preview of 1.0.0. It fixes what moving an ASP.NET Core and EF Core backend to `Recommended` showed:
the ratchet's prune, builds without an `.editorconfig`, and three rules that failed correct code.

### Changed

- The levels set `indent_style = space`, `indent_size = 4` and `tab_width = 4`, so RCS0053 measures indentation the
  same way with or without an `.editorconfig`, such as in a Docker build context; without them, Roslynator guessed
  the step from the code, and a wrapped base list passed or failed depending on the machine. An `.editorconfig`
  that sets these options still wins. A project without one may now see RCS0053 on a top-level statement whose only
  argument is a block lambda with `{` in column 0, which is a Roslynator bug (see the README's known limitations).
- IDE0031 (`?.` instead of a null-check ternary) is a suggestion instead of a warning. After a `join` or a second
  `from` in a query over `IQueryable`, Roslyn suggests `?.` where an expression tree cannot hold it, and the fix does
  not compile (CS8072).
- The PascalCase naming rule no longer covers namespaces, which may start with a brand name such as xUnit or iOS.

### Fixed

- `-p:HouseRulesBaseline=prune` no longer fails on a project without `houserules.baseline`. The property reaches
  every project of a solution and every project reference, so pruning a solution where only some projects have a
  baseline failed; such a project now builds as usual and says there is nothing to prune.
- `prune` drops an entry whose finding is now suppressed in source (a pragma or `[SuppressMessage]`) or by another
  suppressor. It kept such entries, although the build passed without them.
- A project that sets `GenerateDocumentationFile` to false no longer fails at `Recommended` with
  `EnableGenerateDocumentationFile` in every build: IDE0005 needs the documentation file, so it is off for that
  project.

### Removed

- The `WeakCrypto` ban and its `HouseRulesBanWeakCrypto` switch: CA5350 and CA5351, which `Baseline` already turns
  on, report the same uses of MD5, SHA1, DES, TripleDES and RC2, so each was reported twice. Banned APIs now start at
  `Strict`, and the banned-API analyzer does no work below it.

## [1.0.0-preview.2] - 2026-10-25

The second preview of 1.0.0. The default levels drop rules that push a design or misfire on ASP.NET Core and EF
Core code.

### Added

- A package icon, shown on nuget.org and in IDE package managers.

### Changed

- The `HttpClient`, `WallClock`, `Random`, `Console` and `Environment` bans move to `Strict`. They push a design
  (injected clocks, logging and configuration, `IHttpClientFactory`) rather than catch a bug, and Microsoft also
  endorses a long-lived static `HttpClient`. `Baseline` and `Recommended` keep only the `WeakCrypto` ban.
- IDE0305 (collection expressions instead of `ToList()`/`ToArray()`) is a suggestion instead of a warning.
- IDE0051 (unused private member) no longer reports constructors. EF Core, serializers and dependency injection
  call private constructors through reflection, so every EF Core entity's constructor was reported.

### Removed

- `CheckForOverflowUnderflow` in Debug builds at `Strict`: Debug and Release behaved differently, and code that
  relies on wrap-around, such as hash codes and checksums, threw in Debug only. A project can still set it.
- The naming rule requiring an `Async` suffix on async methods. ASP.NET Core drops the suffix from action names, so
  controller actions go without it.

## [1.0.0-preview.1] - 2026-10-24

The first preview of 1.0.0. It carries no stability promise: the release policy above applies from 1.0.0.

### Added

- Requires the .NET 10 SDK (10.0.100) or later; projects may target any framework the SDK builds, including net8.0.
  The analyzers are built against Roslyn 5.0, the compiler in .NET SDK 10.0.100.
- Three levels, chosen with `<HouseRulesLevel>`: `Baseline`, `Recommended` (the default) and `Strict`. Each level is
  a superset of the one below; [docs/levels.md](docs/levels.md) lists every setting and rule per level.
- HR0001, blank line after a multi-line statement, with a code fix that `dotnet format` applies.
- Strict build settings as overridable defaults: warnings as errors, code style enforced in the build, pinned
  `AnalysisLevel` 10.0, deterministic builds; documentation files at `Recommended`; nullable reference types, strict
  compiler mode, every warning wave and checked arithmetic in Debug at `Strict`.
- Roslynator.Formatting.Analyzers 5.0.1 wrapping rules configured to 120 columns.
- Banned APIs in six categories, each with its own switch: `HouseRulesBanWeakCrypto`, `HouseRulesBanHttpClient`
  (`Baseline`), `HouseRulesBanWallClock`, `HouseRulesBanRandom`, `HouseRulesBanConsole`, `HouseRulesBanEnvironment`
  (`Recommended`), through Microsoft.CodeAnalysis.BannedApiAnalyzers 5.6.0.
- The ratchet: `dotnet build -p:HouseRulesBaseline=init` records a project's existing findings in
  `houserules.baseline`, the build tolerates exactly those, and `dotnet build -p:HouseRulesBaseline=prune` drops the
  fixed ones. The file never grows.
- Naming and code-metric relaxations for test projects.
- Embedded PDBs with Source Link, so a debugger can step into the analyzers, code fixes and baseline task.
