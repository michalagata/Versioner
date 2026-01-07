# Versioner

Versioner is a CLI tool that automatically versions artifacts in Git repositories. It supports monorepo and standard workflows, multiple artifact types, CI/CD integration, and optional webhook notifications. This README consolidates all user-facing documentation in a single file.

---

## Quick Start

```bash
# Run compiled tool
dotnet Versioner.Cli.dll -- -w="/path/to/repo" -d -s

# Global dotnet tool (after publishing)
versioner -w="/path/to/repo" -d -s
```

- `-w` / `--workingfolder` — working directory (must be a Git repo)
- `-d` / `--usedefaults`   — use default settings
- `-s` / `--storeversionfile` — write `version.txt` in repo root
- `-l` / `--loglevel`      — V, D, I, W, E, F (default I)

---

## Supported Artifacts

- **.NET**: `.csproj`, `AssemblyInfo.cs`, `.nuspec`
  - **Note**: `.props` files are only handled in monorepo mode for `Directory.Build.props` (following .NET conventions). In standard mode, `.props` files are completely ignored.
- **NPM**: `package.json`
- **Docker**: `Dockerfile`, `docker-compose.yml`, `compose.yml`
- **Python**: `pyproject.toml`, `setup.py`, `setup.cfg`, `__version__.py`
- **Go**: `go.mod`, `version.go`
- **Rust**: `Cargo.toml`
- **Java/Maven**: `pom.xml`
- **Helm**: `Chart.yaml`
- **YAML**: Generic YAML configs

---

## Operation Modes

### Monorepo Mode (`-m, --ismonorepo`)
- One global version for all artifacts.
- Version is calculated from repository-root Git history and optional `ProjectOverride.json` in repo root.
- **Directory.Build.props handling**:
  - If `Directory.Build.props` exists in repository root, it is modified with version properties (following .NET conventions).
  - If `Directory.Build.props` does not exist, version properties are written directly to `.csproj` files.
- Only `Directory.Build.props` is handled in monorepo mode (other `.props` files are ignored).
- No need to point to a specific `.csproj`.
```bash
dotnet Versioner.Cli.dll -- -w="/path/to/repo" -m -d -s
```

### Standard Mode (default)
- Each artifact gets its own version from its Git history and directory context.
- **Only versions `.csproj` files** (`.props` files are completely ignored and never modified).
- Optional `ProjectOverride.json` in the artifact directory overrides only that artifact.
```bash
dotnet Versioner.Cli.dll -- -w="/path/to/repo" -d -s
```

---

## CLI Options (canonical)

| Short | Long | Description |
|-------|------|-------------|
| `-w`  | `--workingfolder` | Required working directory (Git repo) |
| `-d`  | `--usedefaults`   | Use default settings |
| `-s`  | `--storeversionfile` | Write `version.txt` in repo root |
| `-l`  | `--loglevel`      | V \| D \| I \| W \| E \| F |
| `-m`  | `--ismonorepo`    | Monorepo mode (single global version) |
| `-a`  | `--allslnlocations` | Search SLN recursively (monorepo helper) |
| `-p`  | `--projectfile`   | Exact `.csproj` for legacy monorepo flows |
| `-g`  | `--setprojectguid`| Add project GUIDs (requires `-f`) |
| `-f`  | `--projectsguidconfig` | Path to project GUID config |
| `-u`  | `--customprojectconfig` | Custom project settings (Standard mode only) |
| `-x`  | `--prereleasesuffix` | Pre-release suffix |
| `-z`  | `--definedpatch`  | Override patch component |
| `--versionitems` |  | Comma list: `dotnet,props,nuget,npm,docker,python,go,rust,java,yaml,helm` |
| `-h`  | `--webhookurl`    | Optional webhook URL |
| `-t`  | `--webhooktoken`  | Optional webhook HMAC secret |

Removed/legacy: `-c` (configuration file), `-e` (nuspec enforcement), `-j` (global enforcement), `-n` (versiononlyprops) — use `--versionitems` instead.

---

## Versioning Flows

### Monorepo
1. Verify Git repo and locate root.
2. Discover artifacts (honors `.versionerignore`; filter with `--versionitems`). Note: `.props` files are excluded from discovery in monorepo mode.
3. Calculate global version from root Git log and date; apply `ProjectOverride.json` (root) if present.
4. Check for `Directory.Build.props` in repository root:
   - **If exists**: Modify `Directory.Build.props` with version properties (following .NET conventions).
   - **If not exists**: Write version properties directly to `.csproj` files.
5. Apply the same version to other artifact types (NPM, Docker, etc.).
6. Write `version.txt` in repo root when `-s` is set.

### Standard
1. Discover artifacts in the working folder (honors `.versionerignore`; filter with `--versionitems`).
2. For each `.csproj` file:
   - Use artifact-specific Git history and directory context.
   - Apply local `ProjectOverride.json` (if present) for that artifact.
   - Apply `CustomProjectSettings` (`-u`) to map external directories that should impact this artifact.
3. Skip `.props` files completely (only `.csproj` files are versioned in standard mode; `.props` files are never modified).
4. Produce distinct versions per artifact.
5. Write `version.txt` in repo root when `-s` is set.

### Version Sources & Overrides
- **ProjectOverride.json**: overrides version components (Major/Minor/Patch/Hotfix) but not formats.
  - Monorepo: place in repo root to affect all artifacts.
  - Standard: place in artifact directory to affect that artifact only.
  - **Examples**: See `ExampleFiles/ProjectOverride.json` (basic), `ProjectOverrideMajor.json` (Major only), `ProjectOverrideMajorMinor.json` (Major/Minor), `ProjectOverrideMajorMinorChange.json` (Major/Minor/Patch), `ProjectOverrideMajorMinorChangeFix.json` (all components).
- **CustomProjectSettings (`-u`)**: Standard mode only; declares directories that influence a project's version.
  - **Example**: See `ExampleFiles/CustomProjectSettings.json`.
- **ProjectsGuid.json (`-f`)**: Configuration file for managing project GUIDs (SonarQube integration).
  - **Example**: See `ExampleFiles/ProjectsGuid.json`.
- **versionItems**: select which artifact types to process; if omitted, all supported types are processed (including `.nuspec`).

---

## Automatic Version Property Injection (.NET)

- Injects missing properties into `.csproj` files: `Version`, `AssemblyVersion`, `FileVersion`, `AssemblyInformationalVersion`.
- Default seed `1.0.0.0` is replaced with calculated values.
- Prefers the first unconditional `<PropertyGroup>`; creates one if needed; avoids duplicates.
- In monorepo mode, version properties are written to `Directory.Build.props` (if exists) or directly to `.csproj` files (if `Directory.Build.props` doesn't exist).
- **Example**: See `ExampleFiles/CsProjectFiles/TestFile_Major_3.1.0.1.csproj` for an example of a versioned project file.

## Project GUID Management

- Project GUIDs (SonarQube) are only added to `.csproj` files, never to `.props` or other file types.
- Use `-g` / `--setprojectguid` with `-f` / `--projectsguidconfig` to enable GUID management.

---

## Versioning Strategy Plugins (Architecture Overview)

- Strategy interface: `IVersioningStrategy`.
- Factory: `VersioningStrategyFactory` selects built-in strategies (Semantic default, Calendar) and custom ones.
- Strategies can validate requirements, declare format support, and be extended without core code changes.

---

## Universal Versioning Script

Use from any repository to run Versioner:
```bash
./scripts/universal-version-artifacts.sh -w=/path/to/project -s -l=I
```
Features:
- Auto-detects `Versioner.Cli.dll`
- Works on Linux/macOS/Windows
- Supports `-w`, `-l`, `-s`, `-d`, `-v` (versioner dir), `-c` (cleanup backups)
- Cleans `.bak` files and prints Git/version info

---

## CI/CD Integration (summary)

Run Versioner in pipelines (example):
```bash
dotnet Versioner.Cli.dll -- \
  --workingfolder="$WORKSPACE" \
  --ismonorepo \
  --storeversionfile \
  --loglevel=I
```
- Use `--versionitems` to narrow types.
- Use `-s` to emit `version.txt` for downstream steps.
- Ensure full Git history (`fetch-depth: 0`) for correct versioning.

---

## Usage Examples

- Standard mode, store version file:
  ```bash
  dotnet Versioner.Cli.dll -- -w="/repo" -d -s
  ```
- Monorepo, selective types:
  ```bash
  dotnet Versioner.Cli.dll -- -w="/repo" -m -d -s --versionitems="dotnet,npm,docker"
  ```
- With CustomProjectSettings (Standard mode):
  ```bash
  dotnet Versioner.Cli.dll -- -w="/repo" -d -s -u="/repo/CustomProjectSettings.json"
  ```
  See `ExampleFiles/CustomProjectSettings.json` for configuration format.
- With ProjectOverride.json (override version components):
  ```bash
  # Monorepo: place ProjectOverride.json in repo root
  dotnet Versioner.Cli.dll -- -w="/repo" -m -d -s
  
  # Standard: place ProjectOverride.json in artifact directory
  dotnet Versioner.Cli.dll -- -w="/repo" -d -s
  ```
  See `ExampleFiles/ProjectOverride*.json` for various override examples.
- With Project GUIDs (SonarQube):
  ```bash
  dotnet Versioner.Cli.dll -- -w="/repo" -d -s -g -f="/repo/ProjectsGuid.json"
  ```
  See `ExampleFiles/ProjectsGuid.json` for configuration format.
- With webhook notification:
  ```bash
  dotnet Versioner.Cli.dll -- -w="/repo" -d -s -h="https://example.com/webhook" -t="secret-token"
  ```
- Example project files:
  - See `ExampleFiles/CsProjectFiles/` for example `.csproj` files demonstrating versioning.

---

## Requirements
- .NET 10.0 SDK or newer
- Git installed
- Working folder must be a Git repository

---

## Best Practices
- Use absolute paths for `-w`.
- Always run versioning before build/publish steps.
- Keep `.versionerignore` to exclude irrelevant paths.
- Commit versioned files after running Versioner.
- Use `-s` to produce `version.txt` for external consumers.
- Prefer `--versionitems` to limit scope when needed.

---

## Troubleshooting
- **"Versioner.Cli.dll not found"**: ensure the compiled DLL is available in the current directory or provide full path: `dotnet /path/to/Versioner.Cli.dll`.
- **“Working directory does not exist”**: verify `-w` path and that it is a Git repo.
- **Invalid log level**: use one of `V,D,I,W,E,F`.
- **No versions written**: check Git history is present; ensure files aren’t excluded by `.versionerignore`.
- **CI failures**: ensure full Git history is available (`fetch-depth: 0`), and `dotnet` SDK 10+ is installed.

---

## Example Files

The `ExampleFiles/` directory contains practical examples for common use cases:

- **ProjectOverride*.json**: Various examples of version override configurations:
  - `ProjectOverride.json` - Basic override with Major and Minor
  - `ProjectOverrideMajor.json` - Override only Major component
  - `ProjectOverrideMajorMinor.json` - Override Major and Minor components
  - `ProjectOverrideMajorMinorChange.json` - Override Major, Minor, and Patch components
  - `ProjectOverrideMajorMinorChangeFix.json` - Override all components (Major, Minor, Patch, Hotfix)
- **CustomProjectSettings.json**: Example configuration for mapping external directories to projects (Standard mode only).
- **ProjectsGuid.json**: Example configuration for managing project GUIDs (SonarQube integration).
- **CsProjectFiles/**: Example `.csproj` files demonstrating versioning results.

## Additional Documentation
The `docs/` directory now contains only platform/build addenda (e.g., build guide, Docker setup, scripts notes, expansion plans, changelog). All core user documentation lives here.

---

## License
LGPL-3.0-or-later
