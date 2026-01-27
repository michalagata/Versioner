# Versioner - Automatic Git-Based Version Management Tool

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**Versioner** is a powerful, Git-history-based versioning tool that automatically manages versions across multiple project types (.NET, Docker, npm, Python, Go, Rust, Java, Helm, and more). It eliminates manual version management by calculating versions from your repository's commit history and timestamps.

---

## Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Installation](#installation)
- [Version Format](#version-format)
- [Versioning Logic - How It Works](#versioning-logic---how-it-works)
- [Operating Modes](#operating-modes)
  - [Classic Mode (Default)](#classic-mode-default)
  - [MonoRepo Mode](#monorepo-mode)
- [Command-Line Parameters - Complete Reference](#command-line-parameters---complete-reference)
- [Quick Start Examples](#quick-start-examples)
- [Version Override System](#version-override-system)
- [Supported Project Types](#supported-project-types)
- [Configuration Files](#configuration-files)
- [Troubleshooting](#troubleshooting)

---

## Overview

Versioner automatically generates semantic versions based on:
- **Git commit history** - Build number from commit count in directory or repository
- **Current date/time** - Minor (YYMM) and Patch (day-of-year) from system timestamp
- **Calculated major version** - From year digit-sum or preserved from existing files
- **Override files** - Manual control via `ProjectOverride.json` when needed

### Why Versioner?

- ✅ **Zero manual version management** - Versions calculated automatically from Git history
- ✅ **Deterministic versioning** - Same commit history = same version, always
- ✅ **Multi-project support** - .NET, Docker, npm, Python, Go, Rust, Java, Helm
- ✅ **Flexible modes** - Per-artifact versions (Classic) or unified versions (MonoRepo)
- ✅ **Override support** - Manual control via `ProjectOverride.json` when needed

---

## Key Features

### Automatic Version Generation
- **Date-based versioning**: Minor from year/month (YYMM format), Patch from day-of-year (DDD format)
- **Commit-based build numbers**: Automatically count commits for build (W) component
- **Timestamp-driven**: Versions reflect when code was written, not manually set
- **No manual intervention**: Just commit code, versions update automatically

### Two Operating Modes
1. **Classic Mode** (default): Each artifact gets its own independent version
   - Version calculated from artifact's directory commit history
   - Perfect for multi-project repositories where components evolve independently
   - Each component has its own version lifecycle

2. **MonoRepo Mode** (`-m`): All artifacts share one global version
   - Version calculated from repository root commit history
   - Ideal for unified release cycles where all components release together
   - "Preserve Major" logic maintains consistency across artifacts

### Multi-Platform Support
- **.NET**: `.csproj`, `.props`, `.nuspec`, `AssemblyInfo.cs`
- **Docker**: `Dockerfile`, `docker-compose.yml`
- **JavaScript/TypeScript**: `package.json`
- **Python**: `setup.py`, `pyproject.toml`, `__init__.py`
- **Go**: `go.mod`, version constants
- **Rust**: `Cargo.toml`
- **Java**: `pom.xml`, `build.gradle`
- **Kubernetes/Helm**: `Chart.yaml`, YAML manifests

---

## Installation

### As .NET Global Tool (Recommended)

```bash
# Install
dotnet tool install -g Versioner.Cli

# Update existing installation
dotnet tool update -g Versioner.Cli

# Verify installation
versioner --help
```

### From Source

```bash
git clone https://github.com/anubisworks/Versioner.git
cd Versioner
dotnet build --configuration Release
dotnet pack --configuration Release
dotnet tool install --global --add-source ./DEPLOYMENT/packages Versioner.Cli
```

---

## Version Format

Versioner supports two version formats depending on artifact type:

### 4-Component Format: `X.Y.Z.W` (.NET, NuGet, Java)

```
28.2601.023.114
│  │    │   └─── W (Build): Commit count (114 commits)
│  │    └─────── Z (Patch): Day of year (023 = January 23)
│  └──────────── Y (Minor): Year-Month as YYMM (2601 = January 2026)
└──────────────── X (Major): Year digit-sum (2+0+2+6 = 10) or preserved
```

**Component Calculation:**
- **X (Major)**: Digit-sum from current year (2026 → 2+0+2+6 = 10) OR preserved from existing version
- **Y (Minor)**: YYMM format - 4 digits representing year and month (January 2026 → `2601`)
- **Z (Patch)**: Day of year in DDD format (001-366, zero-padded) - January 23 → `023`
- **W (Build)**: Commit count in directory (Classic mode) or repository root (MonoRepo mode)

### 3-Component Format: `X.Y.W` (Docker, npm, Python, Go, Rust)

```
28.2601.114
│  │    └─────── W (Build): Commit count (114 commits)
│  └──────────── Y (Minor): Year-Month as YYMM (2601 = January 2026)
└──────────────── X (Major): Year digit-sum or preserved
```

**Note**: The Z (Patch/Day) component is **omitted** in 3-component format for compatibility with semantic versioning used by npm, Docker, etc.

**Why different formats?**
- **.NET/Java** use 4-component versions (X.Y.Z.W) natively
- **Docker/npm/Python** use semantic versioning 3-component (X.Y.W) standard
- Versioner automatically adapts format based on artifact type

---

## Versioning Logic - How It Works

### Core Calculation Algorithm

Versioner calculates each version component based on a **priority system**. Understanding this priority is key to controlling how versions are generated.

### Classic Mode - Version Calculation

In Classic Mode, each artifact is versioned **independently** based on its directory's Git history.

#### X (Major) Component Priority:

1. **HIGHEST**: Existing `<Version>` tag value in the file being versioned
   ```xml
   <!-- If this exists, Major=5 is used -->
   <Version>5.1.19.30</Version>
   ```

2. **LOCAL OVERRIDE**: Local `ProjectOverride.json` in artifact directory
   ```json
   // Cli/ProjectOverride.json
   { "Major": 2, "Minor": 0, "Patch": 0, "Hotfix": 0 }
   ```

3. **GLOBAL OVERRIDE**: Global `ProjectOverride.json` in repository root
   ```json
   // /ProjectOverride.json
   { "Major": 1, "Minor": 0, "Patch": 0, "Hotfix": 0 }
   ```

4. **CALCULATED**: Digit-sum from current year
   ```
   2026 → 2 + 0 + 2 + 6 = 10
   ```

#### Y (Minor) Component Priority:

1. **HIGHEST**: Existing `<Version>` tag value in file
2. **LOCAL OVERRIDE**: Local `ProjectOverride.json`
3. **GLOBAL OVERRIDE**: Global `ProjectOverride.json`
4. **CALCULATED**: YYMM format from current date
   ```
   January 2026 → 26 (year) + 01 (month) = 2601
   ```

#### Z (Patch) Component Priority:

1. **HIGHEST**: Existing `<Version>` tag value in file
2. **LOCAL OVERRIDE**: Local `ProjectOverride.json`
3. **GLOBAL OVERRIDE**: Global `ProjectOverride.json`
4. **CALCULATED**: Day of year in DDD format
   ```
   January 23 → Day 23 of 365 → 023 (zero-padded)
   March 15 → Day 74 of 365 → 074
   ```

#### W (Build) Component Priority:

1. **ABSOLUTE HIGHEST**: CLI `--definedpatch` parameter
   ```bash
   versioner -w . -z 999
   # Forces W=999, ignoring all other sources
   ```

2. **LOCAL OVERRIDE**: Local `ProjectOverride.json` `Hotfix` field
3. **GLOBAL OVERRIDE**: Global `ProjectOverride.json` `Hotfix` field
4. **CALCULATED**: Commit count in artifact's directory
   ```bash
   # For Cli/Versioner.Cli.csproj:
   git log --oneline -- Cli/ | wc -l
   # → 31 commits in Cli/ directory
   ```

**⚠️ IMPORTANT**: 
- CLI `--definedpatch` sets the **W (Build)** component, NOT Z (Patch)
- This is the **highest priority** override - it ignores all other sources for W component

### MonoRepo Mode - Version Calculation

In MonoRepo Mode, all artifacts share **one global version** calculated from repository root.

#### Key Differences from Classic Mode:

1. **"Preserve Major" Logic** applies:
   - Looks for existing Major version in `.props` files first
   - Falls back to first `.csproj` file if no `.props` has version
   - Preserves that Major for all artifacts

2. **Only global `ProjectOverride.json` applies**:
   - Local overrides in artifact directories are ignored
   - Repository root `/ProjectOverride.json` is the only override file used

3. **Commit count from repository root**:
   ```bash
   # For ALL artifacts in MonoRepo mode:
   git log --oneline | wc -l
   # → 114 commits in entire repository
   ```

#### "Preserve Major" Logic (MonoRepo Only):

**Search Order:**
1. **PRIORITY 1**: Check `Directory.Build.props` for `<Version>` tag
   ```xml
   <!-- If exists, Major=5 is preserved for ALL artifacts -->
   <Project>
     <PropertyGroup>
       <Version>5.0.0</Version>
     </PropertyGroup>
   </Project>
   ```

2. **PRIORITY 2**: If no .props version, check first `.csproj` file
   ```xml
   <!-- Cli/Versioner.Cli.csproj -->
   <Project>
     <PropertyGroup>
       <Version>1.1.19.30</Version>  <!-- Major=1 preserved -->
     </PropertyGroup>
   </Project>
   ```

3. **OVERRIDE**: `ProjectOverride.json` can override preserved Major
   ```json
   // /ProjectOverride.json
   { "Major": 2, "Minor": 0, "Patch": 0, "Hotfix": 0 }
   // Forces Major=2 for all artifacts, overriding preserved value
   ```

**Why "Preserve Major"?**
- In MonoRepo mode, you typically want version consistency
- If you've already established a major version (e.g., v5.x.x), preserve it
- Prevents major version from recalculating each year

### Commit Counting Logic

#### Classic Mode:
```bash
# Each artifact counts commits in its directory only
git log --oneline -- Cli/ | wc -l      # → 31 commits
git log --oneline -- Core/ | wc -l     # → 59 commits
git log --oneline -- docker/ | wc -l   # → 12 commits

# Results:
# Cli artifact  → X.Y.Z.31
# Core artifact → X.Y.Z.59
# Docker image  → X.Y.12 (3-component format)
```

#### MonoRepo Mode:
```bash
# All artifacts count commits in repository root
git log --oneline | wc -l   # → 114 commits (entire repository)

# Results:
# ALL artifacts → X.Y.Z.114 (or X.Y.114 for 3-component)
```

### version.txt File Behavior

#### Classic Mode:
- **Multiple `version.txt` files**: One in each artifact directory + one in repository root
- **Content**: Each file contains that artifact's calculated version
```
/version.txt                → 28.2601.023.114  (main/solution)
Cli/version.txt             → 1.2601.023.31    (Cli artifact)
Core/version.txt            → 28.2601.023.59   (Core artifact)
docker/version.txt          → 28.2601.12       (Docker image)
```

#### MonoRepo Mode:
- **Single `version.txt` file**: Only in repository root
- **Content**: Global version applied to all artifacts
```
/version.txt  → 28.2601.023.114  (all artifacts use this)
```

**⚠️ IMPORTANT**: `version.txt` is **OUTPUT ONLY** - it's never read as input for version calculation.

### Calculation Examples

#### Example 1: Classic Mode, No Overrides

**Setup:**
- Date: January 23, 2026
- Repository structure:
  ```
  /MyRepo/
    Cli/Versioner.Cli.csproj  (no <Version> tag)
    Core/Versioner.csproj     (no <Version> tag)
  ```
- Commits: Cli/ has 31, Core/ has 59

**Calculation:**
```
X (Major):  2026 → 2+0+2+6 = 10 (digit-sum)
Y (Minor):  January 2026 → 2601 (YYMM)
Z (Patch):  January 23 → 023 (day-of-year)
W (Build):  git log count in directory

Result:
  Cli/Versioner.Cli.csproj  → 10.2601.023.31
  Core/Versioner.csproj     → 10.2601.023.59
```

#### Example 2: Classic Mode, With Local Override

**Setup:**
- Date: January 23, 2026
- `Cli/ProjectOverride.json`: `{ "Major": 1, "Minor": 0, "Patch": 0, "Hotfix": 0 }`
- Commits: Cli/ has 31, Core/ has 59

**Calculation:**
```
Cli artifact:
  X (Major):  1 (from local ProjectOverride.json)
  Y (Minor):  2601 (calculated - override has 0)
  Z (Patch):  023 (calculated - override has 0)
  W (Build):  31 (calculated - override has 0)
  Result: 1.2601.023.31

Core artifact:
  X (Major):  10 (calculated - no override)
  Y (Minor):  2601 (calculated)
  Z (Patch):  023 (calculated)
  W (Build):  59 (calculated)
  Result: 10.2601.023.59
```

#### Example 3: MonoRepo Mode, "Preserve Major"

**Setup:**
- Date: January 23, 2026
- `Directory.Build.props` has `<Version>5.0.0</Version>`
- Repository commits: 114

**Calculation:**
```
X (Major):  5 (preserved from Directory.Build.props)
Y (Minor):  2601 (calculated)
Z (Patch):  023 (calculated)
W (Build):  114 (repository root commit count)

Result: ALL artifacts → 5.2601.023.114
```

#### Example 4: CLI Override

**Setup:**
- Any mode, any date
- Command: `versioner -w . -z 999`

**Calculation:**
```
X, Y, Z: Calculated normally
W (Build): 999 (FORCED by --definedpatch, ignoring all other sources)

Result: X.Y.Z.999
```

---

## Operating Modes

### Classic Mode (Default)

**Characteristics:**
- Each artifact versioned **independently**
- Commit count from **artifact's directory**
- Supports **local** and **global** `ProjectOverride.json`
- Different artifacts can have different versions

**Use when:**
- Components have independent release cycles
- You want commit counts to reflect per-component activity
- Each artifact needs its own versioning strategy
- Repository contains truly independent projects

**Command:**
```bash
versioner -w /path/to/repo -s -l I
```

**Example output:**
```
Cli/Versioner.Cli.csproj  → 1.2601.023.31
Core/Versioner.csproj     → 10.2601.023.59
package.json              → 10.2601.114
Dockerfile                → 10.2601.12
```

### MonoRepo Mode

**Characteristics:**
- All artifacts share **one global version**
- Commit count from **repository root**
- **"Preserve Major"** logic applies
- Only **global** `ProjectOverride.json` used
- Version consistency across all artifacts

**Use when:**
- All components release together as unified product
- Version consistency across artifacts is critical
- Simplified release management is desired
- Single version number represents entire system state

**Command:**
```bash
versioner -w /path/to/repo -m -s -l I
```

**Example output:**
```
All artifacts → 5.2601.023.114
```

---

## Command-Line Parameters - Complete Reference

### Core Parameters (Required and Recommended)

#### `-w, --workingfolder <path>` (REQUIRED)

**Description**: Full path to Git repository root directory.

**Purpose**: Specifies where Versioner should operate. Must be a valid Git repository.

**Usage:**
```bash
# Absolute path
versioner -w /Users/user/projects/MyRepo -s

# Relative path
versioner -w . -s

# Windows path
versioner -w "C:\Projects\MyRepo" -s

# Alternative syntax (colon separator)
versioner -w:/path/to/repo -s
versioner --workingfolder:/path/to/repo -s
```

**Requirements:**
- Path must exist
- Path must be a Git repository root (contains `.git` directory)
- User must have read/write permissions

**Common errors:**
```bash
ERROR: Working folder is not a git repository
→ Solution: Run 'git init' or provide correct repository path

ERROR: WorkingFolder path does not exist
→ Solution: Check path spelling and existence
```

---

#### `-s, --storeversionfile` (RECOMMENDED)

**Description**: Write `version.txt` file(s) to disk containing calculated versions.

**Purpose**: Enables version tracking and is **required for GitHub releases**.

**Usage:**
```bash
# Enable version file storage
versioner -w . -s

# Without this flag, versions are calculated but not saved to disk
versioner -w .
```

**Behavior:**
- **Classic Mode**: Creates `version.txt` in each artifact directory + repository root
- **MonoRepo Mode**: Creates single `version.txt` in repository root only

**When to use:**
- ✅ **Always in production/CI/CD**: Required for release automation
- ✅ **GitHub releases**: Needed to create tags from version.txt
- ✅ **Version tracking**: Provides audit trail of versions
- ❌ **Development testing**: Can be omitted during local testing

**Example version.txt content:**
```
28.2601.023.114
```

---

#### `-l, --loglevel <level>` (Recommended)

**Description**: Set logging verbosity level.

**Purpose**: Control how much information Versioner outputs during execution.

**Available levels:**
- `V` - **Verbose**: Maximum information (every operation logged)
- `D` - **Debug**: Debugging information (useful for troubleshooting)
- `I` - **Info**: Standard information (default for production)
- `W` - **Warning**: Warnings only
- `E` - **Error**: Errors only
- `F` - **Fatal**: Critical errors only

**Usage:**
```bash
# Info level (recommended for production)
versioner -w . -s -l I

# Verbose (troubleshooting)
versioner -w . -s -l V

# Errors only (silent mode)
versioner -w . -s -l E
```

**Default**: `V` (Verbose) if not specified

**When to use each level:**
- **V (Verbose)**: Troubleshooting issues, understanding what Versioner does
- **D (Debug)**: Development, debugging Versioner itself
- **I (Info)**: Production builds (recommended)
- **W (Warning)**: When you only care about potential problems
- **E (Error)**: Silent mode, only show failures
- **F (Fatal)**: Absolute minimum output

---

### Mode Selection

#### `-m, --ismonorepo`

**Description**: Enable MonoRepo mode (global version for all artifacts).

**Purpose**: Switch from Classic mode (independent versions) to MonoRepo mode (unified version).

**Usage:**
```bash
# Enable MonoRepo mode
versioner -w . -m -s -l I

# Classic mode (default, no flag needed)
versioner -w . -s -l I
```

**Effects when enabled:**
1. All artifacts get **same version number**
2. Commit count from **repository root** (not per-directory)
3. **"Preserve Major"** logic activates
4. Only **global** `ProjectOverride.json` applies
5. Single `version.txt` in repository root

**Incompatible with:**
- Local `ProjectOverride.json` files (ignored in MonoRepo mode)
- `-u` / `--customprojectconfig` when explicitly provided by user

**Example comparison:**

**Classic Mode:**
```bash
versioner -w . -s -l I
# Cli  → 1.2601.023.31
# Core → 10.2601.023.59
```

**MonoRepo Mode:**
```bash
versioner -w . -m -s -l I
# All → 5.2601.023.114
```

---

### Version Override Parameters

#### `-z, --definedpatch <number>`

**Description**: Override W (Build) component with specific number.

**Purpose**: **HIGHEST PRIORITY** override for build number, ignoring all other sources.

**Usage:**
```bash
# Set build number to 999
versioner -w . -s -z 999

# Use CI/CD build number
versioner -w . -s -z $BUILD_NUMBER

# TeamCity example
versioner -w . -s -z %build.counter%
```

**Priority:**
```
CLI --definedpatch (HIGHEST)
    ↓ (overrides)
ProjectOverride.json Hotfix field
    ↓ (overrides)
Calculated commit count
```

**⚠️ IMPORTANT**: 
- Sets **W (Build)** component, NOT Z (Patch) - name is historical
- Overrides **everything**: ProjectOverride.json, commit count, existing values
- Use when you need absolute control over build number

**Common use cases:**
- CI/CD integration: Use build server's build number
- Manual release: Set specific build number for release
- Reset build counter: Start from specific number

---

#### `-x, --prereleasesuffix <suffix>`

**Description**: Append suffix to version for pre-release versions.

**Purpose**: Create alpha, beta, or release candidate versions.

**Usage:**
```bash
# Alpha release
versioner -w . -s -x alpha
# Result: 28.2601.023.114-alpha

# Beta with number
versioner -w . -s -x beta.2
# Result: 28.2601.023.114-beta.2

# Release candidate
versioner -w . -s -x rc.1
# Result: 28.2601.023.114-rc.1

# Custom suffix
versioner -w . -s -x preview
# Result: 28.2601.023.114-preview
```

**Valid characters:** Letters, numbers, dots (`.`), hyphens (`-`)

**Common suffixes:**
- `alpha` - Early development
- `beta` - Feature complete, testing phase
- `rc.1`, `rc.2` - Release candidates
- `preview` - Preview releases
- `dev` - Development builds

**SemVer compliance:** Follows semantic versioning pre-release syntax

---

### MonoRepo-Specific Parameters

#### `-p, --projectfile <path>`

**Description**: **MonoRepo only** - Specify .csproj file for base version calculation.

**Purpose**: Enable "Preserve Major" logic using specific project file.

**Usage:**
```bash
# Use specific project as version source
versioner -w . -m -p Cli/Versioner.Cli.csproj -s

# MonoRepo mode required
versioner -w . -m -p Core/Versioner.csproj -s
```

**Behavior:**
- Extracts Major version from specified `.csproj` file
- Preserves that Major for all artifacts
- Only works in MonoRepo mode (`-m` required)

**Incompatible with:**
- `-a` / `--allslnlocations` (mutually exclusive)
- Classic mode (has no effect without `-m`)

**Example:**
```xml
<!-- Cli/Versioner.Cli.csproj -->
<Project>
  <PropertyGroup>
    <Version>1.1.19.30</Version>  <!-- Major=1 preserved -->
  </PropertyGroup>
</Project>
```

```bash
versioner -w . -m -p Cli/Versioner.Cli.csproj -s
# All artifacts → 1.2601.023.114 (Major=1 from Cli project)
```

---

#### `-a, --allslnlocations`

**Description**: **MonoRepo only** - Search for .sln files recursively and select highest version.

**Purpose**: Automatically find and use project with highest existing Major version.

**Usage:**
```bash
# Enable recursive .sln search
versioner -w . -m -a -s

# MonoRepo mode required
versioner -w . -m --allslnlocations -s
```

**Behavior:**
1. Searches repository recursively for `.sln` files
2. Finds all `.csproj` files referenced in solutions
3. Extracts Major version from each project
4. Selects **highest** Major version found
5. Preserves that Major for all artifacts

**Incompatible with:**
- `-p` / `--projectfile` (mutually exclusive)
- Classic mode (has no effect without `-m`)

**Example:**
```
Repository structure:
  Cli/Versioner.Cli.csproj     → Version 1.1.19.30 (Major=1)
  Core/Versioner.csproj        → Version 5.0.0 (Major=5)
  Legacy/Old.csproj            → Version 2.3.4 (Major=2)
```

```bash
versioner -w . -m -a -s
# Selects Major=5 (highest), all artifacts → 5.2601.023.114
```

---

### Configuration File Parameters

#### `-u, --customprojectconfig <path>`

**Description**: Path to custom project settings configuration file.

**Purpose**: Define which directories contribute to commit count for each project.

**Default value**: `customprojectconfig.json` (in current directory)

**Usage:**
```bash
# Use custom config file
versioner -w . -s -u /path/to/config.json

# Use default location
versioner -w . -s
```

**File format:**
```json
{
  "ProjectSettings": [
    {
      "ProjectName": "Versioner.Cli",
      "Directories": [
        "Cli",
        "Cli/Helper",
        "Shared/Common"
      ]
    },
    {
      "ProjectName": "Versioner.Core",
      "Directories": [
        "Core",
        "Core/Services",
        "Shared/Common"
      ]
    }
  ]
}
```

**Purpose of configuration:**
- Control which directories count toward commit history
- Share code between projects (e.g., "Shared/Common" affects both)
- Fine-tune commit counting for complex repository structures

**⚠️ Incompatibility with MonoRepo:**
When explicitly provided by user AND using MonoRepo mode, error occurs:
```bash
versioner -w . -m -u config.json
# ERROR: IsMonoRepo parameter cannot be used with CustomConfigurationFile parameter!
```

**Solution**: Don't explicitly provide `-u` in MonoRepo mode (default value is okay).

---

#### `-f, --projectsguidconfig <path>`

**Description**: Path to ProjectGuids.json configuration file.

**Purpose**: Maintain stable project GUIDs across builds (required for SonarQube).

**Default value**: `projectguids.json` (in current directory)

**Usage:**
```bash
# Enable GUID management with custom file
versioner -w . -s -g -f /path/to/projectguids.json

# Use default location
versioner -w . -s -g
```

**File format (auto-managed):**
```json
{
  "Entities": [
    {
      "ProjectHash": "abc123...",
      "ProjectGuid": "{12345678-1234-1234-1234-123456789012}"
    }
  ],
  "StateChanged": false
}
```

**How it works:**
1. Versioner calculates hash of each project file path
2. Looks up corresponding GUID in `projectguids.json`
3. If not found, generates new GUID and saves it
4. Injects GUID into `.csproj` file
5. Updates `projectguids.json` if changed

**Why needed:**
- **SonarQube** requires stable project GUIDs for analysis consistency
- Without this, GUIDs change on every build
- SonarQube treats changed GUID as new project

**⚠️ Must use with `-g` flag:**
```bash
# Correct
versioner -w . -s -g -f projectguids.json

# Incorrect (file ignored)
versioner -w . -s -f projectguids.json  # Missing -g flag
```

---

#### `-g, --setprojectguid`

**Description**: Enable project GUID management in .csproj files.

**Purpose**: Inject and maintain stable GUIDs (required for SonarQube analysis).

**Usage:**
```bash
# Enable GUID management
versioner -w . -s -g

# With custom GUID config file
versioner -w . -s -g -f /path/to/projectguids.json
```

**What it does:**
1. Scans all `.csproj` files
2. Checks if `<ProjectGuid>` exists
3. If missing, generates new GUID or loads from `projectguids.json`
4. Injects `<ProjectGuid>` into `.csproj` file
5. Saves GUID mapping to `projectguids.json`

**Example modification:**
```xml
<!-- Before -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <Version>1.0.0</Version>
  </PropertyGroup>
</Project>

<!-- After (with -g flag) -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <ProjectGuid>{12345678-1234-1234-1234-123456789012}</ProjectGuid>
    <Version>1.2601.023.31</Version>
  </PropertyGroup>
</Project>
```

**When to use:**
- ✅ **SonarQube analysis**: Required for consistent project tracking
- ✅ **Multi-developer teams**: Ensures everyone uses same GUIDs
- ❌ **Simple projects**: Not needed if not using SonarQube

---

### Advanced Parameters

#### `--versionitems <types>`

**Description**: Comma-separated list of artifact types to version.

**Purpose**: Selectively version only specific artifact types, ignoring others.

**Supported types:**
- `dotnet` - .csproj, .props files
- `nuget` - .nuspec files
- `npm` - package.json files
- `docker` - Dockerfile, docker-compose.yml
- `python` - setup.py, pyproject.toml, __init__.py
- `go` - go.mod files
- `rust` - Cargo.toml files
- `java` - pom.xml, build.gradle files
- `helm` - Chart.yaml files
- `yaml` - YAML manifest files
- `props` - .props files (when used separately from dotnet)

**Usage:**
```bash
# Version only .NET projects
versioner -w . -s --versionitems="dotnet"

# Version .NET and Docker
versioner -w . -s --versionitems="dotnet,docker"

# Version npm and Docker
versioner -w . -s --versionitems="npm,docker"

# Multiple types
versioner -w . -s --versionitems="dotnet,nuget,npm,docker,python,go"
```

**Default behavior (no flag):** Versions ALL artifact types found

**Use cases:**
- **Faster builds**: Skip unnecessary artifacts
- **Selective releases**: Version only what you're releasing
- **Testing**: Test versioning of specific artifact types
- **Mixed repositories**: Control which artifacts get versioned

**Example:**
```bash
# Repository contains: .csproj, package.json, Dockerfile, Chart.yaml
# But only version .NET and Docker:
versioner -w . -s --versionitems="dotnet,docker"

# Result: .csproj and Dockerfile versioned, package.json and Chart.yaml untouched
```

---

#### `--errors-as-warnings`

**Description**: Treat file validation errors as warnings and continue execution.

**Purpose**: Allow Versioner to continue even if some files are missing or invalid.

**Usage:**
```bash
# Enable error tolerance
versioner -w . -s --errors-as-warnings

# Without flag (default: strict mode)
versioner -w . -s
```

**Behavior difference:**

**Strict mode (default):**
```bash
versioner -w . -s -u nonexistent.json
# ERROR: CustomConfigurationFile (-u) does not exist: nonexistent.json
# Application will exit. Use --errors-as-warnings to treat this as a warning and continue.
# Exit code: 406
```

**Tolerant mode:**
```bash
versioner -w . -s -u nonexistent.json --errors-as-warnings
# WARNING: CustomConfigurationFile (-u) does not exist: nonexistent.json. Ignoring this parameter and continuing.
# Continues with versioning...
# Exit code: 0
```

**When to use:**
- ✅ **Optional config files**: When config files are optional
- ✅ **Development**: Testing without all files present
- ✅ **Gradual migration**: Migrating configuration incrementally
- ❌ **Production**: Should fail fast on errors

**Affected parameters:**
- `-u` / `--customprojectconfig`
- `-f` / `--projectsguidconfig`

---

#### `--configuration-repository <url>`

**Description**: External Git repository URL for centralized configuration files.

**Purpose**: Load `ProjectGuids.json` and `ProjectCustomSettings.json` from external repository.

**Usage:**
```bash
# SSH URL
versioner -w . -m -s \
  --configuration-repository ssh://git@repo.com:7999/config.git

# HTTPS URL
versioner -w . -m -s \
  --configuration-repository https://github.com/org/config.git
```

**How it works:**
1. Clones configuration repository to temporary directory
2. Loads `ProjectGuids.json` from cloned repo
3. Loads `ProjectCustomSettings.json` from cloned repo
4. Uses these configurations for versioning
5. Commits any changes back to configuration repository
6. Cleans up temporary directory

**Additional parameters:**
- `--configuration-branch <branch>` - Branch to use (default: `master`)
- `--default-commit-message <msg>` - Commit message for changes

**Example:**
```bash
versioner -w . -m -s \
  --configuration-repository ssh://git@repo.com:7999/config.git \
  --configuration-branch production \
  --default-commit-message "Updated from build"
```

**Use cases:**
- **Multi-team environments**: Centralized GUID management
- **Shared configurations**: Team-wide versioning policies
- **Audit trail**: Git history of configuration changes
- **Consistency**: Ensure everyone uses same configurations

**⚠️ Effect on -u and -f:**
When `--configuration-repository` is used, `-u` and `-f` parameters are **automatically overridden** with paths from cloned repository. No error occurs even if `-u` or `-f` are explicitly provided.

---

#### `--configuration-branch <branch>`

**Description**: Git branch to use in configuration repository.

**Purpose**: Select which branch to load configurations from.

**Default**: `master`

**Usage:**
```bash
# Use production branch
versioner -w . -m -s \
  --configuration-repository ssh://git@repo.com/config.git \
  --configuration-branch production

# Use development branch
versioner -w . -m -s \
  --configuration-repository ssh://git@repo.com/config.git \
  --configuration-branch develop
```

**⚠️ Requires:** `--configuration-repository` must be specified

---

#### `--default-commit-message <message>`

**Description**: Commit message for configuration repository changes.

**Purpose**: Customize commit message when Versioner updates configuration files.

**Default**: `Auto Commit {timestamp}`

**Usage:**
```bash
versioner -w . -m -s \
  --configuration-repository ssh://git@repo.com/config.git \
  --default-commit-message "Updated by CI/CD build"
```

**⚠️ Requires:** `--configuration-repository` must be specified

---

#### `--webhookurl <url>`

**Description**: Optional webhook URL for notifications.

**Purpose**: Send versioning notifications to external systems (e.g., Slack, Teams, custom endpoints).

**Usage:**
```bash
# Send notifications to webhook
versioner -w . -s --webhookurl https://hooks.example.com/versioning

# With HMAC authentication
versioner -w . -s \
  --webhookurl https://hooks.example.com/versioning \
  --webhooktoken "secret-hmac-key"
```

**Payload example:**
```json
{
  "event": "version_updated",
  "version": "28.2601.023.114",
  "repository": "/path/to/repo",
  "mode": "monorepo",
  "artifacts": 12,
  "timestamp": "2026-01-27T10:30:00Z"
}
```

**Security:** Use `--webhooktoken` for HMAC-SHA256 signature verification

**Behavior:**
- **Success**: Notification sent, Versioner continues normally
- **Failure**: Warning logged, Versioner continues normally (not blocking)

---

#### `--webhooktoken <token>`

**Description**: HMAC secret for webhook signature verification.

**Purpose**: Secure webhook calls with HMAC-SHA256 signature.

**Usage:**
```bash
versioner -w . -s \
  --webhookurl https://hooks.example.com/versioning \
  --webhooktoken "my-secret-hmac-key"
```

**How it works:**
1. Versioner generates HMAC-SHA256 signature of payload
2. Adds signature to `X-Hub-Signature-256` header
3. Webhook receiver verifies signature using same secret
4. Only processes requests with valid signature

**⚠️ Requires:** `--webhookurl` must be specified

---

### Parameter Format Support

Versioner supports multiple parameter formats for flexibility:

```bash
# Equal sign (=)
--workingfolder=/path/to/repo
-w=/path/to/repo

# Colon (:)
--workingfolder:/path/to/repo
-w:/path/to/repo

# Space (short parameters only)
-w /path/to/repo
-l I

# All formats are equivalent and automatically normalized
```

---

## Quick Start Examples

### Example 1: Basic Classic Mode

```bash
cd /path/to/repo
versioner -w . -s -l I
```

**What happens:**
- Each artifact versioned independently
- Versions calculated from directory commit history
- `version.txt` written to each artifact directory
- Info-level logging

**Output:**
```
[INFO] Classic Mode - Independent artifact versioning
[INFO] Cli/Versioner.Cli.csproj → 10.2601.023.31
[INFO] Core/Versioner.csproj → 10.2601.023.59
[INFO] package.json → 10.2601.114
```

---

### Example 2: MonoRepo Mode

```bash
versioner -w . -m -s -l I
```

**What happens:**
- All artifacts get same version
- Version calculated from repository root
- Single `version.txt` in root
- "Preserve Major" logic applies

**Output:**
```
[INFO] MonoRepo Mode - Global repository versioning
[INFO] All artifacts → 5.2601.023.114
```

---

### Example 3: Pre-release Version

```bash
versioner -w . -m -s -x beta -l I
```

**Result:**
```
All artifacts → 28.2601.023.114-beta
```

---

### Example 4: Custom Build Number

```bash
versioner -w . -m -s -z 1234 -l I
```

**Result:**
```
All artifacts → 28.2601.023.1234
```

---

### Example 5: Version Only .NET Projects

```bash
versioner -w . -s --versionitems="dotnet" -l I
```

**What happens:**
- Only `.csproj` and `.props` files versioned
- Skips `package.json`, `Dockerfile`, etc.

---

### Example 6: With SonarQube GUID Management

```bash
versioner -w . -s -g -f projectguids.json -l I
```

**What happens:**
- Versions all artifacts
- Maintains stable GUIDs in `.csproj` files
- Updates `projectguids.json` with GUID mappings
- Required for consistent SonarQube analysis

---

### Example 7: Tolerant Mode (Continue on Errors)

```bash
versioner -w . -s -u config.json --errors-as-warnings -l I
```

**What happens:**
- If `config.json` doesn't exist: WARNING instead of ERROR
- Continues with versioning using defaults
- Useful for optional configuration files

---

## Version Override System

### ProjectOverride.json Format

```json
{
  "Major": 1,      // Override X component (0 = don't override)
  "Minor": 0,      // Override Y component (0 = don't override)
  "Patch": 0,      // Override Z component (0 = don't override)
  "Hotfix": 0      // Override W component (0 = don't override)
}
```

**Rules:**
- Value `0` → **Don't override** (use calculated value)
- Value `!= 0` → **Override** with this value
- Wildcard `"*"` → Same as `0` (don't override)

### Override Locations

#### Classic Mode:
1. **Local override**: `<artifact-directory>/ProjectOverride.json`
   - Highest priority for that artifact
   - Used exclusively if exists (global ignored)
2. **Global override**: `/ProjectOverride.json` (repository root)
   - Fallback if no local override
   - Applies to all artifacts without local override

#### MonoRepo Mode:
- **Only global**: `/ProjectOverride.json` (repository root)
- Local overrides **ignored**

### Common Scenarios

#### Scenario 1: Fix Major Version

```json
{
  "Major": 5,
  "Minor": 0,
  "Patch": 0,
  "Hotfix": 0
}
```
**Result**: `5.2601.023.114` (Major=5, rest calculated)

---

#### Scenario 2: Fix Major and Minor

```json
{
  "Major": 2,
  "Minor": 1,
  "Patch": 0,
  "Hotfix": 0
}
```
**Result**: `2.1.023.114` (Major=2, Minor=1, rest calculated)

---

#### Scenario 3: Different Majors per Artifact (Classic Mode)

```bash
# Create local overrides
echo '{"Major": 1, "Minor": 0, "Patch": 0, "Hotfix": 0}' > Cli/ProjectOverride.json
echo '{"Major": 2, "Minor": 0, "Patch": 0, "Hotfix": 0}' > Core/ProjectOverride.json

versioner -w . -s -l I
```

**Result:**
```
Cli/Versioner.Cli.csproj  → 1.2601.023.31
Core/Versioner.csproj     → 2.2601.023.59
```

---

## Supported Project Types

### .NET Projects

**Files**: `.csproj`, `.props`, `.nuspec`, `AssemblyInfo.cs`

**Version Properties Updated:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <Version>28.2601.023.114</Version>
    <AssemblyVersion>28.2601.023.114</AssemblyVersion>
    <FileVersion>28.2601.023.114</FileVersion>
    <InformationalVersion>28.2601.023.114+abc123</InformationalVersion>
  </PropertyGroup>
</Project>
```

**NuSpec:**
```xml
<package>
  <metadata>
    <version>28.2601.023.114</version>
  </metadata>
</package>
```

---

### Docker

**Files**: `Dockerfile`, `docker-compose.yml`

**Format**: 3-component (`X.Y.W`)

**Image tags:**
```
my-image:28.2601.114
my-image:28.2601.114-alpha
my-image:latest
```

---

### npm / JavaScript

**Files**: `package.json`

**Format**: 3-component (`X.Y.W`)

```json
{
  "name": "my-package",
  "version": "28.2601.114"
}
```

---

### Python

**Files**: `setup.py`, `pyproject.toml`, `__init__.py`

**Format**: 3-component (`X.Y.W`)

```python
# __init__.py
__version__ = "28.2601.114"

# setup.py
setup(
    name="my-package",
    version="28.2601.114",
)
```

---

### Go

**Files**: `go.mod`, version constants in `.go`

```go
// version.go
const Version = "28.2601.114"
```

---

### Rust

**Files**: `Cargo.toml`

```toml
[package]
name = "my-crate"
version = "28.2601.114"
```

---

### Java

**Files**: `pom.xml`, `build.gradle`

**Format**: 4-component (`X.Y.Z.W`)

```xml
<!-- pom.xml -->
<project>
  <version>28.2601.023.114</version>
</project>
```

---

### Kubernetes / Helm

**Files**: `Chart.yaml`, YAML manifests

**Format**: 3-component (`X.Y.W`)

```yaml
# Chart.yaml
apiVersion: v2
name: my-chart
version: 28.2601.114
appVersion: "28.2601.114"
```

---

## Configuration Files

### CustomProjectSettings.json

**Purpose**: Define which directories contribute to commit count for each project.

**Location**: Repository root or path specified via `-u`

**Format:**
```json
{
  "ProjectSettings": [
    {
      "ProjectName": "Versioner.Cli",
      "Directories": [
        "Cli",
        "Cli/Helper",
        "Shared/Common"
      ]
    },
    {
      "ProjectName": "Versioner.Core",
      "Directories": [
        "Core",
        "Core/Services",
        "Shared/Common"
      ]
    }
  ]
}
```

**Use case:**
- Share code between projects (e.g., "Shared/Common")
- Both projects count commits in shared directory
- Fine-tune commit counting for complex structures

---

### ProjectGuids.json

**Purpose**: Maintain stable project GUIDs (auto-managed by Versioner).

**Location**: Repository root or path specified via `-f`

**Format (auto-generated):**
```json
{
  "Entities": [
    {
      "ProjectHash": "abc123def456...",
      "ProjectGuid": "{12345678-1234-1234-1234-123456789012}"
    }
  ],
  "StateChanged": false
}
```

**⚠️ Do not edit manually** - Versioner manages this file automatically when `-g` flag is used.

---

## CI/CD Integration Examples

Versioner integrates seamlessly with all major CI/CD platforms. Below are production-ready examples for TeamCity and Azure DevOps.

### TeamCity

**Build Configuration:**

```xml
<?xml version="1.0" encoding="UTF-8"?>
<build-type>
  <name>Versioner Build</name>
  <description>Build, version, test, and publish Versioner artifacts</description>
  
  <parameters>
    <param name="system.BuildCounter" value="%build.counter%" />
  </parameters>
  
  <build-runners>
    <!-- Step 1: Install Versioner CLI tool -->
    <runner id="install_versioner" name="Install Versioner" type="simpleRunner">
      <parameters>
        <param name="script.content"><![CDATA[
#!/bin/bash
set -e
dotnet tool install --global Versioner.Cli --version 28.2601.* || dotnet tool update --global Versioner.Cli
        ]]></param>
        <param name="teamcity.step.mode" value="default"/>
        <param name="use.custom.script" value="true"/>
      </parameters>
    </runner>
    
    <!-- Step 2: Version Artifacts (MonoRepo mode) -->
    <runner id="version_artifacts" name="Version Artifacts" type="simpleRunner">
      <parameters>
        <param name="script.content"><![CDATA[
#!/bin/bash
set -e

# MonoRepo mode with TeamCity build counter
versioner -w . -m -s -l I -z %system.BuildCounter%

# Verify version.txt was created
if [[ ! -f "version.txt" ]]; then
  echo "ERROR: version.txt not created!"
  exit 1
fi

# Read and publish version
VERSION=$(cat version.txt)
echo "##teamcity[setParameter name='env.VERSION' value='$VERSION']"
echo "##teamcity[buildNumber '$VERSION']"
echo "Version: $VERSION"
        ]]></param>
        <param name="teamcity.step.mode" value="default"/>
        <param name="use.custom.script" value="true"/>
      </parameters>
    </runner>
    
    <!-- Step 3: Restore Dependencies -->
    <runner id="restore" name="Restore Dependencies" type="dotnet">
      <parameters>
        <param name="dotnet-command" value="restore"/>
        <param name="dotnet-paths" value="**/*.csproj"/>
      </parameters>
    </runner>
    
    <!-- Step 4: Build Solution -->
    <runner id="build" name="Build Solution" type="dotnet">
      <parameters>
        <param name="dotnet-command" value="build"/>
        <param name="dotnet-args" value="--configuration Release --no-restore"/>
        <param name="dotnet-paths" value="*.sln"/>
      </parameters>
    </runner>
    
    <!-- Step 5: Run Tests -->
    <runner id="test" name="Run Tests" type="dotnet">
      <parameters>
        <param name="dotnet-command" value="test"/>
        <param name="dotnet-args" value="--configuration Release --no-build --logger:trx"/>
        <param name="dotnet-paths" value="**/*Tests.csproj"/>
      </parameters>
    </runner>
    
    <!-- Step 6: Pack NuGet Package -->
    <runner id="pack" name="Pack NuGet" type="dotnet">
      <parameters>
        <param name="dotnet-command" value="pack"/>
        <param name="dotnet-args" value="--configuration Release --no-build --output ./artifacts"/>
        <param name="dotnet-paths" value="Core/Versioner.csproj"/>
      </parameters>
    </runner>
    
    <!-- Step 7: Publish Artifacts -->
    <runner id="publish_artifacts" name="Publish Artifacts" type="simpleRunner">
      <parameters>
        <param name="script.content"><![CDATA[
#!/bin/bash
set -e

# Publish artifacts to TeamCity
echo "##teamcity[publishArtifacts 'version.txt']"
echo "##teamcity[publishArtifacts 'artifacts/*.nupkg']"
echo "##teamcity[publishArtifacts 'README.md']"
        ]]></param>
        <param name="teamcity.step.mode" value="default"/>
        <param name="use.custom.script" value="true"/>
      </parameters>
    </runner>
  </build-runners>
  
  <artifact-dependencies>
    <artifact-dependency id="version_file">
      <source-buildTypeId>Versioner_Build</source-buildTypeId>
      <revisionRule name="lastSuccessful" revision="latest.lastSuccessful"/>
      <artifact sourcePath="version.txt" targetPath="."/>
      <artifact sourcePath="artifacts/*.nupkg" targetPath="./artifacts"/>
      <artifact sourcePath="README.md" targetPath="."/>
    </artifact-dependency>
  </artifact-dependencies>
  
  <vcs-settings>
    <vcs-entry-ref root-id="Versioner_GitRoot"/>
  </vcs-settings>
  
  <triggers>
    <build-trigger id="vcsTrigger" type="vcsTrigger">
      <parameters>
        <param name="enableQueueOptimization" value="true"/>
        <param name="quietPeriodMode" value="DO_NOT_USE"/>
      </parameters>
    </build-trigger>
  </triggers>
</build-type>
```

**Classic Mode Example (Independent Artifact Versions):**

```bash
#!/bin/bash
set -e

# Classic mode - each artifact gets its own version
versioner -w . -s -l I

# Versioner creates multiple version.txt files:
# - /version.txt (solution level)
# - Cli/version.txt (CLI artifact)
# - Core/version.txt (Core library)

# Read versions
SOLUTION_VERSION=$(cat version.txt)
CLI_VERSION=$(cat Cli/version.txt)
CORE_VERSION=$(cat Core/version.txt)

# Publish as TeamCity parameters
echo "##teamcity[setParameter name='env.SOLUTION_VERSION' value='$SOLUTION_VERSION']"
echo "##teamcity[setParameter name='env.CLI_VERSION' value='$CLI_VERSION']"
echo "##teamcity[setParameter name='env.CORE_VERSION' value='$CORE_VERSION']"

# Use in build
echo "##teamcity[buildNumber '$SOLUTION_VERSION']"
```

---

### Azure DevOps

**azure-pipelines.yml:**

```yaml
trigger:
  branches:
    include:
      - main
      - develop
  paths:
    include:
      - src/**
      - Core/**
      - Cli/**

variables:
  buildConfiguration: 'Release'
  dotnetVersion: '8.0.x'
  # Azure DevOps provides $(Build.BuildNumber) automatically

pool:
  vmImage: 'ubuntu-latest'

stages:
  - stage: Version
    displayName: 'Version Artifacts'
    jobs:
      - job: VersionJob
        displayName: 'Calculate Version'
        steps:
          - checkout: self
            fetchDepth: 0  # Full Git history for commit counting
          
          - task: UseDotNet@2
            displayName: 'Install .NET SDK'
            inputs:
              packageType: 'sdk'
              version: '$(dotnetVersion)'
          
          - script: |
              dotnet tool install --global Versioner.Cli --version 28.2601.* || dotnet tool update --global Versioner.Cli
            displayName: 'Install Versioner CLI'
          
          - script: |
              # MonoRepo mode with Azure DevOps build number
              versioner -w $(Build.SourcesDirectory) -m -s -l I -z $(Build.BuildNumber)
              
              # Verify version.txt was created
              if [[ ! -f "version.txt" ]]; then
                echo "##vso[task.logissue type=error]version.txt not created!"
                exit 1
              fi
              
              # Read version
              VERSION=$(cat version.txt)
              echo "Calculated version: $VERSION"
              
              # Publish version as pipeline variable
              echo "##vso[task.setvariable variable=VERSION;isOutput=true]$VERSION"
              
              # Update build number
              echo "##vso[build.updatebuildnumber]$VERSION"
            displayName: 'Version Artifacts'
            name: versionStep
          
          - publish: $(Build.SourcesDirectory)/version.txt
            artifact: version
            displayName: 'Publish version.txt'

  - stage: Build
    displayName: 'Build All Platforms'
    dependsOn: Version
    variables:
      VERSION: $[ stageDependencies.Version.VersionJob.outputs['versionStep.VERSION'] ]
    jobs:
      - job: BuildLinux
        displayName: 'Build Linux x64'
        pool:
          vmImage: 'ubuntu-latest'
        steps:
          - checkout: self
            fetchDepth: 0
          
          - download: current
            artifact: version
          
          - task: UseDotNet@2
            displayName: 'Install .NET SDK'
            inputs:
              packageType: 'sdk'
              version: '$(dotnetVersion)'
          
          - script: |
              dotnet restore
            displayName: 'Restore Dependencies'
          
          - script: |
              dotnet build --configuration $(buildConfiguration) --no-restore
            displayName: 'Build Solution'
          
          - script: |
              dotnet test --configuration $(buildConfiguration) --no-build --logger trx --results-directory $(Build.ArtifactStagingDirectory)/TestResults
            displayName: 'Run Tests'
          
          - script: |
              dotnet publish Cli/Versioner.Cli.csproj \
                --configuration $(buildConfiguration) \
                --runtime linux-x64 \
                --self-contained true \
                --output $(Build.ArtifactStagingDirectory)/linux-x64 \
                -p:PublishSingleFile=true \
                -p:IncludeNativeLibrariesForSelfExtract=true
            displayName: 'Publish Linux x64'
          
          - publish: $(Build.ArtifactStagingDirectory)/linux-x64
            artifact: Versioner-linux-x64
            displayName: 'Publish Linux Artifacts'
          
          - task: PublishTestResults@2
            displayName: 'Publish Test Results'
            inputs:
              testResultsFormat: 'VSTest'
              testResultsFiles: '**/*.trx'
              searchFolder: '$(Build.ArtifactStagingDirectory)/TestResults'

      - job: BuildMacOS
        displayName: 'Build macOS x64'
        pool:
          vmImage: 'macos-latest'
        steps:
          - checkout: self
            fetchDepth: 0
          
          - download: current
            artifact: version
          
          - task: UseDotNet@2
            displayName: 'Install .NET SDK'
            inputs:
              packageType: 'sdk'
              version: '$(dotnetVersion)'
          
          - script: |
              dotnet restore
            displayName: 'Restore Dependencies'
          
          - script: |
              dotnet build --configuration $(buildConfiguration) --no-restore
            displayName: 'Build Solution'
          
          - script: |
              dotnet publish Cli/Versioner.Cli.csproj \
                --configuration $(buildConfiguration) \
                --runtime osx-x64 \
                --self-contained true \
                --output $(Build.ArtifactStagingDirectory)/osx-x64 \
                -p:PublishSingleFile=true \
                -p:IncludeNativeLibrariesForSelfExtract=true
            displayName: 'Publish macOS x64'
          
          - publish: $(Build.ArtifactStagingDirectory)/osx-x64
            artifact: Versioner-osx-x64
            displayName: 'Publish macOS Artifacts'

      - job: BuildWindows
        displayName: 'Build Windows x64'
        pool:
          vmImage: 'windows-latest'
        steps:
          - checkout: self
            fetchDepth: 0
          
          - download: current
            artifact: version
          
          - task: UseDotNet@2
            displayName: 'Install .NET SDK'
            inputs:
              packageType: 'sdk'
              version: '$(dotnetVersion)'
          
          - script: |
              dotnet restore
            displayName: 'Restore Dependencies'
          
          - script: |
              dotnet build --configuration Release --no-restore
            displayName: 'Build Solution'
          
          - script: |
              dotnet publish Cli/Versioner.Cli.csproj ^
                --configuration Release ^
                --runtime win-x64 ^
                --self-contained true ^
                --output $(Build.ArtifactStagingDirectory)/win-x64 ^
                -p:PublishSingleFile=true ^
                -p:IncludeNativeLibrariesForSelfExtract=true
            displayName: 'Publish Windows x64'
          
          - publish: $(Build.ArtifactStagingDirectory)/win-x64
            artifact: Versioner-win-x64
            displayName: 'Publish Windows Artifacts'

  - stage: Package
    displayName: 'Package NuGet'
    dependsOn: Build
    condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/main'))
    variables:
      VERSION: $[ stageDependencies.Version.VersionJob.outputs['versionStep.VERSION'] ]
    jobs:
      - job: PackageJob
        displayName: 'Create NuGet Package'
        steps:
          - checkout: self
            fetchDepth: 0
          
          - download: current
            artifact: version
          
          - task: UseDotNet@2
            displayName: 'Install .NET SDK'
            inputs:
              packageType: 'sdk'
              version: '$(dotnetVersion)'
          
          - script: |
              dotnet pack Core/Versioner.csproj \
                --configuration $(buildConfiguration) \
                --output $(Build.ArtifactStagingDirectory) \
                -p:PackageVersion=$(VERSION)
            displayName: 'Pack NuGet Package'
          
          - publish: $(Build.ArtifactStagingDirectory)
            artifact: NuGetPackages
            displayName: 'Publish NuGet Packages'

  - stage: Release
    displayName: 'Create GitHub Release'
    dependsOn: 
      - Version
      - Build
      - Package
    condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/main'))
    variables:
      VERSION: $[ stageDependencies.Version.VersionJob.outputs['versionStep.VERSION'] ]
    jobs:
      - job: GitHubRelease
        displayName: 'Publish to GitHub'
        steps:
          - checkout: self
            fetchDepth: 0
          
          - download: current
            artifact: Versioner-linux-x64
          - download: current
            artifact: Versioner-osx-x64
          - download: current
            artifact: Versioner-win-x64
          - download: current
            artifact: NuGetPackages
          - download: current
            artifact: version
          
          - script: |
              # Copy version.txt and README.md to artifacts
              cp $(Pipeline.Workspace)/version/version.txt $(Build.ArtifactStagingDirectory)/
              cp README.md $(Build.ArtifactStagingDirectory)/
              
              # Create platform archives
              cd $(Pipeline.Workspace)/Versioner-linux-x64
              tar -czf $(Build.ArtifactStagingDirectory)/Versioner-linux-x64.tar.gz *
              
              cd $(Pipeline.Workspace)/Versioner-osx-x64
              tar -czf $(Build.ArtifactStagingDirectory)/Versioner-osx-x64.tar.gz *
              
              cd $(Pipeline.Workspace)/Versioner-win-x64
              zip -r $(Build.ArtifactStagingDirectory)/Versioner-win-x64.zip *
            displayName: 'Prepare Release Artifacts'
          
          - task: GitHubRelease@1
            displayName: 'Create GitHub Release'
            inputs:
              gitHubConnection: 'GitHub-Connection'  # Configure in Azure DevOps
              repositoryName: '$(Build.Repository.Name)'
              action: 'create'
              target: '$(Build.SourceVersion)'
              tagSource: 'userSpecifiedTag'
              tag: 'R-$(VERSION)'
              title: 'Release $(VERSION)'
              releaseNotesSource: 'inline'
              releaseNotesInline: |
                ## Versioner Release $(VERSION)
                
                Automated release from Azure DevOps Pipeline.
                
                ### Artifacts:
                - Linux x64: `Versioner-linux-x64.tar.gz`
                - macOS x64: `Versioner-osx-x64.tar.gz`
                - Windows x64: `Versioner-win-x64.zip`
                - NuGet Package: `Versioner.$(VERSION).nupkg`
                
                ### Changes:
                See CHANGELOG.md for details.
              assets: |
                $(Build.ArtifactStagingDirectory)/*.tar.gz
                $(Build.ArtifactStagingDirectory)/*.zip
                $(Build.ArtifactStagingDirectory)/*.nupkg
                $(Build.ArtifactStagingDirectory)/version.txt
                $(Build.ArtifactStagingDirectory)/README.md
              isDraft: false
              isPreRelease: false
              addChangeLog: true
```

**Classic Mode for Azure DevOps:**

```yaml
# For Classic Mode (independent versions per artifact):
- script: |
    # Classic mode - no -m flag
    versioner -w $(Build.SourcesDirectory) -s -l I
    
    # Read individual versions
    SOLUTION_VERSION=$(cat version.txt)
    CLI_VERSION=$(cat Cli/version.txt)
    CORE_VERSION=$(cat Core/version.txt)
    
    # Publish as pipeline variables
    echo "##vso[task.setvariable variable=SOLUTION_VERSION;isOutput=true]$SOLUTION_VERSION"
    echo "##vso[task.setvariable variable=CLI_VERSION;isOutput=true]$CLI_VERSION"
    echo "##vso[task.setvariable variable=CORE_VERSION;isOutput=true]$CORE_VERSION"
    
    echo "Solution: $SOLUTION_VERSION"
    echo "CLI: $CLI_VERSION"
    echo "Core: $CORE_VERSION"
  displayName: 'Version Artifacts (Classic Mode)'
  name: versionStep
```

### Key Integration Points

**Both platforms support:**
- ✅ MonoRepo mode with unified versioning
- ✅ Classic mode with per-artifact versions
- ✅ Build counter integration (`-z` parameter)
- ✅ Multi-platform builds (Linux, macOS, Windows)
- ✅ Artifact publishing (version.txt, README.md, binaries)
- ✅ Test execution with results publishing
- ✅ GitHub release creation
- ✅ NuGet package publishing

**Best practices:**
1. Always use full Git history (`fetchDepth: 0` / deep clone)
2. Publish `version.txt` as artifact for downstream jobs
3. Include README.md in release artifacts
4. Verify version calculation before build
5. Use build counter for reproducible versions

---

## Troubleshooting

### Issue 1: "Working folder is not a git repository"

**Cause**: Specified path is not a Git repository root.

**Solution:**
```bash
# Initialize Git if new repository
git init

# Or provide correct repository path
versioner -w /correct/path/to/repo -s
```

---

### Issue 2: MonoRepo error with -u parameter

**Symptom:**
```
ERROR: IsMonoRepo parameter cannot be used with CustomConfigurationFile parameter!
```

**Cause**: Parameter `-u` explicitly provided with MonoRepo mode.

**Solution:**
```bash
# Don't explicitly provide -u in MonoRepo mode
versioner -w . -m -s

# Default value is used automatically
```

---

### Issue 3: Build number too high

**Cause**: Counting all repository commits instead of directory commits.

**Solutions:**
- **Classic mode**: Check if working correctly (should count per-directory)
- **MonoRepo mode**: Expected behavior (counts all commits)
- **Override**: Use `-z` to set specific number:
  ```bash
  versioner -w . -s -z 100
  ```

---

### Issue 4: File does not exist error

**Symptom:**
```
ERROR: CustomConfigurationFile (-u) does not exist: /path/to/file
```

**Solutions:**
```bash
# Option 1: Use --errors-as-warnings
versioner -w . -s --errors-as-warnings

# Option 2: Provide correct path
versioner -w . -s -u /correct/path

# Option 3: Use defaults (don't specify -u)
versioner -w . -s
```

---

### Debug Mode

**Enable verbose logging:**
```bash
versioner -w . -s -l V
```

**Output includes:**
- File discovery details
- Git command execution
- Version calculation steps
- Override application
- File write operations

---

## Error Codes

| Code | Meaning | Action |
|------|---------|--------|
| 0 | Success | No action needed |
| 1 | General error | Check logs for details |
| 406 | Parameter validation failed | Check command-line parameters |
| 500 | Internal error | Report bug with verbose logs |
| 501 | Invalid VersionItems parameter | Check `--versionitems` value |

---

## Support & Contributing

### Getting Help

1. **Documentation**: This README and files in `/docs`
2. **Issues**: [GitHub Issues](https://github.com/anubisworks/Versioner/issues)
3. **Build guide**: See `docs/BUILD_GUIDE.md`

### Reporting Bugs

**Include:**
- Versioner version (`versioner --help`)
- Operating system
- .NET version
- Command used
- Full error output with `-l V`

---

## License

MIT License - Copyright (c) 2026 AnubisWorks

See LICENSE file for details.

---

## Changelog

See `CHANGELOG.md` for version history and release notes.
