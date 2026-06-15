# Copilot instructions for Ddth.Signum

Ddth.Signum is a lightweight .NET library for calculating checksums and object fingerprints,
published as a NuGet package (`Ddth.Signum`). The repository is currently a scaffold: the
`Ddth.Signum` and `Ddth.Signum.Tests` projects exist but contain no source files yet, so new
work usually means adding the first production and test code.

## Layout

- `Ddth.Signum/` – library project (`Ddth.Signum.csproj`), packed and published to NuGet.
- `Ddth.Signum.Tests/` – xUnit test project; references the library and is excluded from packing
  and from its own coverage.
- `Ddth.Signum.sln` – solution tying both projects together.
- `.dev.md` – local commands for running tests with coverage and building docs (docfx).

## Build, test, lint

- Restore/build: `dotnet build --configuration=Release`
- Run all tests: `dotnet test --configuration=Release`
- Run a single test: `dotnet test --filter "FullyQualifiedName~Namespace.ClassName.MethodName"`
  (also accepts `Name=`, `ClassName~`, or trait filters).
- Tests with coverage (see `.dev.md` for the full report flow):
  `dotnet test --configuration=Release --collect="XPlat Code Coverage" --results-directory=TestResults/`
- Formatting/analysis is governed by `.editorconfig` (rich Roslyn ruleset); run `dotnet format`
  to apply it. There is no separate lint step.

## Conventions

- Target framework is `net6.0` for both projects; CI builds and tests against .NET 6, 7, 8, 9,
  and 10, so keep code compatible with the .NET 6 minimum.
- `ImplicitUsings` and nullable reference types (`<Nullable>enable</Nullable>`) are enabled — do
  not add redundant `using` directives and honor non-null contracts.
- `.editorconfig` enforces (as errors/warnings): interfaces prefixed with `I` and PascalCased,
  accessibility modifiers always present, no `this.` qualification, `coalesce`/`null-propagation`
  preferred, System usings sorted first, 4-space indent (2 for YAML), LF line endings, final
  newline.
- The test project pulls in xUnit via a project-level `<Using Include="Xunit" />`, so test files
  do not need an explicit `using Xunit;`.

## Release process

- Releasing is automated via `.github/workflows/release.yaml` using `action-semrelease`: merging a
  PR into the `release` branch tags a version (prefix `v`), packs, pushes to NuGet, updates
  `RELEASE-NOTES.md`, and opens a follow-up PR back to `main`. Do not hand-edit version numbers;
  `PackageVersion` stays `0.0.0` and `PackageReleaseNotes` stays `${RELEASE-NOTES}` in
  `Ddth.Signum.csproj` (the release job substitutes them).
- Note: `release.yaml`'s `NUGET_PROJECT_FILE` currently points at `Ddth.Signum.Mylib/Ddth.Signum.Mylib.csproj`,
  a leftover from the template that does not match the real `Ddth.Signum/Ddth.Signum.csproj`. Fix
  this path if you touch the release workflow.
