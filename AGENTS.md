# AGENTS.md

## Project
Jellyfin 10.9+ plugin (C# / .NET 8) that derives clean media titles from filenames when no metadata provider has set one. Solution `FilenameTitlePlugin.sln` contains two projects:
- `FilenameTitlePlugin/` — plugin assembly (`Jellyfin.Plugin.FilenameTitlePlugin.dll`)
- `FilenameTitlePlugin.Tests/` — xUnit tests for the cleaner

## Build & test
```bash
dotnet build                                                     # from solution root
dotnet test                                                      # runs all xUnit tests
dotnet test --filter "FullyQualifiedName~Clean_ReturnsExpectedTitle"   # one test
```
No CI, no separate lint/format config. `nuget.config` clears all sources and pins nuget.org only — add new feeds there.

## Architecture (where to make changes)
- `FilenameCleanerService.cs` — pure-function string cleaner. The hardcoded `QualityTags` array is the single source of truth for tokens to strip (resolution, source, codec, audio, HDR, release flags). Add new release tags by appending to that array.
- `Plugin.cs` — auto hook on `ILibraryManager.ItemAdded`. Plugin GUID is hardcoded at line 36 and must match `install.sh` and `release.sh` (the latter generates `manifest.json`).
- `TitleUpdaterTask.cs` — manual scheduled task "Update Titles from Filenames" (key `FilenameTitleUpdater`). Registered as `IScheduledTask` in `PluginServiceRegistrator.cs`.
- `PluginServiceRegistrator.cs` — DI registrations: `FilenameCleanerService` singleton + the scheduled task.

## Conventions
- Log lines are prefixed with `[FilenameTitlePlugin]` — preserve it when adding log calls.
- Tests use `[Theory]` + `[InlineData]` rows for happy-path cases and `[Fact]` methods for edge cases. Both styles are present in `FilenameCleanerServiceTests.cs`; follow the same shape.
- The plugin safety rule — only rewrite items whose current title still equals the bare filename (case-insensitive) — is duplicated in `Plugin.cs:61` and `TitleUpdaterTask.cs:59`. Keep them in sync if the rule changes.

## Gotchas
- `install.sh` is a developer-local install script. It hardcodes a source path under `/Download/title/...` and writes to `/var/lib/jellyfin/...`. It is **not** a generic installer; the supported path is the Jellyfin plugin repository described in `README.md`. It takes a single version argument (e.g. `./install.sh 1.1.3`), patches `FilenameTitlePlugin.csproj`'s `<Version>` to `${version}.0`, runs `dotnet build -c Release`, then deploys the DLL and a generated `meta.json` whose `version` matches the argument. When using `release.sh` instead, the csproj `<Version>` and the manifest's `version` field must be bumped together.
- `targetAbi` in `install.sh` is the Jellyfin server ABI (`10.9.0.0`), **not** the assembly version. It tracks the Jellyfin.Controller/Model package versions in `FilenameTitlePlugin.csproj` (currently `10.9.6`).
- `FilenameCleanerService.Clean` only strips the extension for known video extensions; a bare string like `"The.Dark.Knight"` is returned whole. Test cases in `FilenameCleanerServiceTests.cs` exercise this.
- The plugin GUID (`3f2a1b4c-5d6e-7f8a-9b0c-1d2e3f4a5b6c`) is hardcoded in `Plugin.cs:36` and re-stated in `install.sh`'s `meta.json` — change both together.
- Git remotes: `origin` is a fork, `upstream` is the original `adielsa/jellyfin-filename-title`. No GitHub workflows exist; PRs/CI are not automated.
