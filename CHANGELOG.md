# Changelog

All notable changes to HouseRules.Analyzers are listed here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and versions follow [Semantic Versioning](https://semver.org/).

**Release policy:** anything that can add a build error at any level (a new rule, a higher severity, a dependency
update that reports more) ships only in a major version and is listed here. Minor and patch versions never add
errors.

## [1.0.0] - unreleased

### Added

- Three levels, chosen with `<HouseRulesLevel>`: `Baseline`, `Recommended` (the default) and `Strict`. Each level is
  a superset of the one below; [docs/levels.md](docs/levels.md) lists every setting and rule per level.
- HR0001, blank line after a multi-line statement, with a code fix that `dotnet format` applies.
- Strict build settings as overridable defaults: warnings as errors, code style enforced in the build, pinned
  `AnalysisLevel` 8.0, deterministic builds; documentation files at `Recommended`; nullable reference types, strict
  compiler mode, every warning wave and checked arithmetic in Debug at `Strict`.
- Roslynator.Formatting.Analyzers 5.0.1 wrapping rules configured to 120 columns.
- Banned APIs in six categories, each with its own switch: `HouseRulesBanWeakCrypto`, `HouseRulesBanHttpClient`
  (`Baseline`), `HouseRulesBanWallClock`, `HouseRulesBanRandom`, `HouseRulesBanConsole`, `HouseRulesBanEnvironment`
  (`Recommended`), through Microsoft.CodeAnalysis.BannedApiAnalyzers 3.3.4.
- The ratchet: `dotnet build -p:HouseRulesBaseline=init` records a project's existing findings in
  `houserules.baseline`, the build tolerates exactly those, and `dotnet build -p:HouseRulesBaseline=prune` drops the
  fixed ones. The file never grows.
- Naming and code-metric relaxations for test projects.
- Embedded PDBs with Source Link, so a debugger can step into the analyzers, code fixes and baseline task.
