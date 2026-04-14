# Changelog

All notable changes to Versioner will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [28.2604.104.175] - 2026-04-14

### Fixed
- **CRITICAL: `ReplaceLastUnit()` impossible guard condition**: Changed `&&` to `||` in `SemVerHelper.ReplaceLastUnit()`. The original condition `(pieces.Length > 4 && pieces.Length < 3)` could never be true, so invalid version formats (1, 2, or 5+ segments) silently returned an empty string instead of throwing. Now correctly throws `ArgumentOutOfRangeException`.
- **MonoRepo fallback uses inconsistent time source**: The ProjectOverride fallback path in `PrimalPerformer` used `DateTime.Now` for Minor/Patch calculation instead of the algorithm-consistent `artifactVersion` already computed from git history. Now extracts YYMM and DayOfYear from the calculated `artifactVersion` for consistency.
- **Missing mutual exclusivity check for `--produceimagetag` / `--produceimagetag-only`**: Added explicit validation — using both flags now returns exit code 406 with a clear error message instead of a confusing "Invalid parameter" error.
- **`--versiononlyprops` silently ignored in MonoRepo mode**: Added a warning when `--versiononlyprops` (`-n`) is used with `--ismonorepo` (`-m`), and the flag is automatically cleared. MonoRepo mode versions all artifacts uniformly via `GlobalRepoVersioningService`.

## [28.2604.103.64] - 2026-04-13

### Added
- **Version preservation logic**: Versioner now respects existing version values in project files. Major and Minor segments are preserved from existing `<Version>` tags; only Patch and Hotfix are recalculated. Wildcard segments (`*`) signal "calculate this". Works in both Classic and MonoRepo modes.
- **Classic mode: `.props` as read-only base**: If `Directory.Build.props` has a `<Version>` tag, it serves as the base version for all `.csproj` files. Major/Minor are preserved from `.props`; Patch/Hotfix are calculated per `.csproj` directory.
- **MonoRepo mode: `.csproj` fallback**: When no `Directory.Build.props` exists, Versioner scans all `.csproj` files and uses the highest version as the base.
- New `VersionPreservationAnalyzer` helper with 12 unit tests.
- **`--versiononlyprops` (`-n`) now implemented**: When enabled, versions only `.props` files and skips `.csproj`. Useful for central version management via `Directory.Build.props`.

### Changed
- `Directory.Build.props` is now a **read-only** version base in all modes (unless `--versiononlyprops` is used) — never modified by default.
- `CleanDirectoryBuildPropsInClassicMode` is now a no-op (`.props` version properties are preserved as the user's configuration source).
- **Docker files now always use 3-segment version** (`X.Y.W`) in MonoRepo mode, consistent with Classic mode and Docker/SemVer conventions. Previously MonoRepo wrote 4-segment versions to Dockerfiles.

## [28.2604.099.167] - 2026-04-09

### Changed
- **BREAKING (default Major calculation)**: Replaced the "hybrid digit-sum" Major algorithm (`20 + 2 + 6 = 28` for 2026) with a monotonic year-offset formula `major = year - 1998`.
  - 2026 → 28 (unchanged, backward compatible with current releases)
  - 2030 → 32 (was 23 — fixes regression where the old algorithm dropped by 8)
  - 2035 → 37 (was 28 — fixes collision with 2026)
  - Monotonically increasing, never repeats across years.
  - Override mechanisms (`ProjectOverride.json`, `Preserve Major` from existing file) are UNCHANGED. Only the default calculated value is affected, so projects pinning a Major via overrides see no difference.
- Renamed public API `SemVerHelper.CalculateDigitSum(int)` → `SemVerHelper.CalculateMajorFromYear(int year)`. Clean rename — no backwards-compatible alias.
- Bumped documentation `docs/VERSIONING_LOGIC.md` to version 2.2.

### Removed
- Removed obsolete historical status documents from `docs/`: `FINAL_*`, `VERIFICATION_*`, `BUG_FIX_*`, `CRITICAL_BUG_FIX_*`, `ENHANCEMENT_*`, `EXPANSION_PLAN*`, `FORMAT_SWITCHING_*`, `GITHUB_RELEASE_VERIFICATION`, `IMAGETAG_FEATURE_*`, `IMPLEMENTATION_SUMMARY_PL`, `MAIN_DEPLOYMENT_SCRIPT_VERIFICATION`, `PRERELEASE_SUFFIX_VALIDATION_FIX`, `SCRIPT_MODIFICATIONS_TEST_FILTER`, `TEST_RESULTS_FINAL`, `TEST_SUMMARY_IMAGETAG`, `verify-imagetag-feature.sh`. Kept only active product documentation: `BUILD_GUIDE.md`, `BuildAndPublish.md`, `DOCKER_SETUP.md`, `DUPLICATE_VERSION_HANDLING.md`, `SCRIPTS_README.md`, `THREE_SEGMENT_VERSION_SUPPORT.md`, `VERSIONING_LOGIC.md`.
- Removed superseded `docs/VERSIONING_LOGIC_V2.1.md` (replaced by `VERSIONING_LOGIC.md` v2.2).

### Added
- New unit tests in `SemVerHelperTests`: `CalculateMajorFromYear_IsMonotonicAndUnique_ForYears2020To2100` (monotonicity + uniqueness guard) and `CalculateMajorFromYear_ReturnsExpectedMajor` (year → major mapping).
- Added `.nuget-api-key` and `.nuget-api-key.*` to `.gitignore` to prevent accidental secret commits.

### Released
- Published `Versioner.Cli` 28.2604.99.167 to nuget.org.

## [28.2601.134] - 2026-01-27

### Added

#### MonoRepo Mode Parameter Enhancements
- **Enhanced MonoRepo mode parameter handling** with automatic `-u` parameter ignoring
- MonoRepo mode (`--ismonorepo`) now automatically clears `-u/--customprojectconfig` parameter with a yellow WARNING (instead of throwing ERROR)
- Explicit `-u` parameter detection prevents false positives (e.g., `--use` no longer matches `-u`)
- Both `=` and `:` separators are now supported universally across all command-line parameters
- Silent clearing of default values (non-intrusive UX improvement)

#### Test Coverage Improvements
- Added **42 new unit and integration tests** for parameter handling:
  - `MonoRepoParameterTests.cs` (8 test cases): MonoRepo mode validation and parameter clearing
  - `ParameterDetectionTests.cs` (34 test cases): Exact matching, false positive prevention, separator support
  - `Tests/IntegrationTests/test_monorepo_u_parameter.sh` (6 end-to-end tests): Complete workflow validation

#### Documentation Updates
- Updated README.md with new parameter format support section
- Updated troubleshooting guide reflecting WARNING behavior (previously ERROR)
- All test results verified: 736 tests passed (0 failed)

### Changed

#### Parameter Detection Logic
- Changed from `StartsWith("-u")` to exact matching: `arg == "-u" || arg.StartsWith("-u=") || arg.StartsWith("-u:")`
- Prevents false positive matches with parameters like `--use`, `--user`, `-usage`
- Same enhancement applied to `-f/--projectsguidconfig` parameter detection

#### MonoRepo Validation
- Changed from throwing ERROR to displaying yellow WARNING when `-u` is explicitly provided in MonoRepo mode
- CustomConfigurationFile is now automatically cleared (set to empty string) instead of terminating execution
- Execution continues normally after parameter clearing with informative message

## [8.0.0.0] - 2025-01-06

### Added

#### Automatic Version Property Injection
- **Auto-detection and injection of missing version properties** in `.csproj` and `.props` files
- Versioner now automatically adds `Version`, `AssemblyVersion`, `FileVersion`, and `AssemblyInformationalVersion` properties when they are missing
- Default version `1.0.0.0` is used as the initial value before calculating the final version based on Git history
- Smart property group management that finds or creates unconditional `<PropertyGroup>` elements
- Detailed logging at Debug and Information levels to track property injection

#### Global Versioning Enforcement
- **New `-j` / `--enforceglobalversioning` parameter** for enforcing version consistency across multiple file types
- When enabled, Versioner versions both `.csproj` and `.props` files simultaneously
- Automatically injects missing version properties into both file types
- Useful for projects with central version management in `Directory.Build.props` or `Directory.Packages.props`
- See [Version Property Injection Guide](docs/VERSION_PROPERTY_INJECTION.md) for detailed usage

#### Cross-Platform Compatibility
- Removed Windows-specific path handling methods (`RemoveIllegalCharactersWindowsPath`, `ToWindowsPath`)
- Introduced unified cross-platform methods: `RemoveIllegalCharactersPath()`, `NormalizePath()`, `ToUnixPath()`
- Added `PlatformDetector` helper class for OS detection and path separator retrieval
- Tested on Windows, Linux, and macOS

#### Universal Versioning Script
- **New `universal-version-artifacts.sh` script** for versioning any .NET project in any repository
- Auto-detects Versioner installation by searching standard locations
- Uses `dotnet run --project Cli/Versioner.Cli.csproj` for maximum compatibility
- Supports both `-w=/path` and `-w /path` argument formats
- Comprehensive error handling, logging, and backup cleanup
- See [Universal Versioning Guide](docs/UNIVERSAL_VERSIONING_GUIDE.md) for detailed usage

#### Central Package Management
- Implemented `Directory.Build.props` for solution-wide property management
- Implemented `Directory.Packages.props` for centralized NuGet package version management
- All package versions are now managed centrally with `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`

### Changed

#### .NET 8.0 Upgrade
- Upgraded solution from .NET 7.0 to **.NET 8.0**
- Updated all `TargetFramework` properties to `net8.0`
- Updated NuGet packages to latest .NET 8.0-compatible versions
- All projects now use C# `latest` language version with nullable reference types enabled

#### Enhanced Logging
- Added structured logging for version property injection
- Improved diagnostic messages at Information and Debug levels
- Clear indication when properties are added vs. when they already exist

#### Improved Code Quality
- Achieved **80%+ code coverage** with comprehensive unit tests
- Added unit tests for `StringExtensions`, `PlatformDetector`, and version property injection
- Fixed test compilation errors after refactoring path handling methods
- Removed explicit initialization of boolean properties to their default values (CA1805)

#### Documentation
- Created comprehensive [Version Property Injection Guide](docs/VERSION_PROPERTY_INJECTION.md)
- Updated [README.md](README.md) with new `-j` parameter and usage examples
- Updated [scripts/README.md](scripts/README.md) with universal script documentation
- Added CI/CD integration examples for GitHub Actions, GitLab CI, and Azure DevOps

### Fixed

#### Bug Fixes
- **Fixed `version.txt` creation bug**: `version.txt` is now created correctly even when `PrereleaseSuffix` is empty
- Fixed argument parsing in `universal-version-artifacts.sh` to support both `-w=/path` and `-w /path` formats
- Fixed unbound variable error in `scripts/build-linux.sh` by providing default value for `OUTPUT_DIR`
- Fixed test run crash caused by `lcov` reporter not supporting `DeterministicReport` in coverlet
- Fixed version properties not being saved when `versionPropertiesAdded` was true

#### Code Quality
- Removed all Windows-specific dependencies (no more `Win32Exception` or platform-specific path handling)
- Unified path handling across all platforms using `Path.DirectorySeparatorChar`
- Fixed compilation errors in test files after refactoring extension methods

### Removed

- Removed Windows-specific extension methods: `RemoveIllegalCharactersWindowsPath()`, `ToWindowsPath()`, `ToLinuxPath()`
- Removed explicit initialization of boolean properties to their default values (addressing CA1805 warnings)

## API Changes

### New Command-Line Parameters

```bash
-j, --enforceglobalversioning
```
Enforce global versioning mode. When enabled, Versioner will:
1. Search for all `.props` files in the project directory
2. Inject missing version properties into all `.props` files
3. Version all `.props` files with calculated versions
4. Also version the `.csproj` file (standard behavior)
5. Inject missing version properties into `.csproj` if needed

### New Interfaces

```csharp
namespace AnubisWorks.Tools.Versioner.Domain.Interfaces
{
    public interface IVersionPropertyInjector
    {
        bool EnsureVersionPropertiesExist(XDocument project, ProjectType projectType, string defaultVersion);
        bool HasVersionProperties(XDocument project, ProjectType projectType);
    }
}
```

### New Services

```csharp
namespace AnubisWorks.Tools.Versioner.Infrastructure.Services
{
    public class VersionPropertyInjector : IVersionPropertyInjector
    {
        // Implementation for automatic version property injection
    }
}
```

### Modified Signatures

```csharp
// IProjectVersionCalculator
VersionedSetModel CalculateVersion(
    string gitPath,
    string filePath,
    string workingFolder,
    ref List<string> consoleOutputs,
    string buildLabel,
    bool storeVersionFile,
    MajorMinorPatchHotfixModel patchHotfixModel,
    CustomProjectSettings customProjectSettings,
    string prereleaseSuffix,
    string definedPatch,
    bool calculateMonoMode = false,
    bool enforceGlobalVersioning = false); // New parameter
```

## Migration Guide

### Upgrading from 7.x to 8.0

#### Command-Line Changes

No breaking changes to existing command-line parameters. The new `-j` parameter is optional.

**Before:**
```bash
dotnet Versioner.Cli.dll -w=/path/to/project -d -s
```

**After (with global versioning):**
```bash
dotnet Versioner.Cli.dll -w=/path/to/project -d -s -j
```

#### Code Changes

If you're using Versioner as a library:

1. **Update ProjectEntity constructor calls** to include the new `enforceGlobalVersioning` parameter:
   ```csharp
   // Before
   var proj = new ProjectEntity(gitPath, filePath, workingFolder, ref outputs, 
       buildLabel, storeVersionFile, patchHotfixModel, customProjectSettings, 
       preReleaseSuffix, definedPatch, calculateMonoMode);
   
   // After
   var proj = new ProjectEntity(gitPath, filePath, workingFolder, ref outputs, 
       buildLabel, storeVersionFile, patchHotfixModel, customProjectSettings, 
       preReleaseSuffix, definedPatch, calculateMonoMode, 
       enforceGlobalVersioning: false); // Add new parameter
   ```

2. **Update path handling code** to use new cross-platform methods:
   ```csharp
   // Before
   var path = somePath.RemoveIllegalCharactersWindowsPath();
   var unixPath = somePath.ToLinuxPath();
   
   // After
   var path = somePath.RemoveIllegalCharactersPath();
   var normalizedPath = somePath.NormalizePath();
   var unixPath = somePath.ToUnixPath();
   ```

#### .NET Version Update

If you're building from source, ensure you have .NET 8.0 SDK installed:

```bash
dotnet --version  # Should be 8.0.x or later
```

## Security

### Dependency Updates

All NuGet packages have been updated to their latest secure versions compatible with .NET 8.0:

- Microsoft.Extensions.* → 8.0.0
- Serilog → 4.2.0
- xUnit → 2.9.2
- FluentAssertions → 6.12.0
- Moq → 4.20.70
- LibGit2Sharp → 0.29.0
- StyleCop.Analyzers → 1.2.0-beta.556
- SonarAnalyzer.CSharp → 9.58.0.78984

## Known Issues

- Existing CA code analysis warnings (not introduced by this release):
  - CA1050, CA1002, CA1034, CA1040, CA1051, CA1307, CA1708, CA2227
  - These warnings existed before version 8.0 and are tracked for future releases

## Contributors

- Michael Agata (@anubisworks)

## Links

- [Version Property Injection Guide](docs/VERSION_PROPERTY_INJECTION.md)
- [Universal Versioning Guide](docs/UNIVERSAL_VERSIONING_GUIDE.md)
- [GitHub Repository](https://github.com/anubisworks/Versioner)
- [Issue Tracker](https://github.com/anubisworks/Versioner/issues)

---

## [7.x.x.x] - Previous Releases

See git history for previous release notes.

