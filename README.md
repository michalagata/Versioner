# Artifact versioning tool, based on git commit history and static rules.

## Performs versiong of:
- AssemblyInfo.cs
- CSPROJ file (SDK)
- NPM (package.json)
- Docker
- Custom NuSPEC (standalone)

### Usage

**.net core**
```
dotnet Versioner.Cli.dll
```
or

**Windows**
```
Versioner.Cli.exe
```

### Switches

#### Global

**-d** : Sets default settings. May be overriden with **-c** parameter.

**-c=** [string] : Sets path to custom configuration file (global versioning rules).

**-w** [string] : Sets working directory, in which Versioner shall be executed. **IMPORTANT**: must be a git repo!

**-s** :  Sets the file evidence mode. Versioner result will be saved in version.txt file (useful for external tools, like SQL deployment or custom CI)

**-l=** [string] : Sets logging level. Possible codes are: **V**erbose, **D**ebug, **I**information, **W**arning, **E**rror, **F**atal

**-g** :  Enforces Versioner to calculate and add ProjectGuid within CSPROJ files. It may be required for external tools integration, alike SonarQube. **WARNING**: -g parameter is combined with reqiured -f parameter, which sets the path to already calculated project guids (look below)

**-f=** [string] : Sets file path, containing already calculated project guids. If a project receives calculated project guid, it should remain the same for further use and should not be changed.

**-u=** [string] : Sets path to custom project settings file. It is required when indirect commit changes should perform versioning on outside components (ex. files under certain path has not been changed, but other element was and should impact on versioning procedure). Example file [^1]

**-e** : Version NuSPEC files within working directory structure **even** if there are other project type files available (ex. CSPROJ). Without the parameter set, if there are any other than NuSPEC project files available - no custom NuSPEC versioning shall be achieved.

**-v=** [int] : Semantic Versioning Format: may be 1 or 2. V1 consists of four versioning positions (ex. A.B.C.D), V2 consists of three (A.B.C). *Default is V1*.

#### Monorepo Mode

In Monorepo mode, single pointed is the versioning base, while the rest of version-qualified components shall gain the exact same version as the base file.

It contains three functionalities:

**-m** : Monorepo mode, if set blank (no additional parameters) - shall search working directory for SLN file and based on VS2017+ standard) - point first project file (CSPROJ), which should be base for monorepo versioning.

##### Additional p[arameters follow

**-a** : alters SLN search criterias, not only running through the working directory (top level), but also subdirectories. First found shall be parsed.

**-p=** [string] : Points directly to CSPROJ file, which shall be used as base for versioning.

**-x=** [string] : Set prerelease version, adding defined suffix to calculated SemVer.


### Versioning override file

It is possible to override Minor + Major versioning base. Just commit into repo file **ProjectOverride.json** (root directory), with body as in example [^2]


### Environment variable output (may be used on CI level - ex. TeamCity)

Versioner outputs 2 environment variables, which can be used for further versioning needs:

- env.BuildLabel → typo + Commit Hash
- env.DockerBuildLabel → Dock SemVer (X.Y.Z)
- env.BuildNuspecVersion → NuSpec SemVer (wither X.Y.Z.W or X.Y.Z)


### Example command line usage
```
dotnet Versioner.Cli.dll -w="C:\TEMP\PROJECT1REPO" -d -s -l I -g -f="C:\TEMP\ProjectGuids.json" -u="C:\TEMP\CustomProjectSettings.json"
```

### Example configuration files

[^1]: CustomProjectSettingsExample.json
```
{
    ProjectSettings: [
    {
    "ProjectName": "TestProject",
    "Directories": [
        "Directory1",
        "Directory2/SubDirectory1"
        ]
        },
    {
    "ProjectName": "TestProject2",
    "Directories": [
        "TestProject2/TestProject2.Common",
        "TestProject2/TestProject2.Contracts",
        "TestProject2/TestProject2.DAL",
        "TestProject2/TestProject2.Client"
    ]
    }
    ]
}
```

[^2]: ProjectOverride.json
```
{
"Major": 1,
"Minor": 4
}
```

or

```
{
"Major": 1,
"Minor": 4,
"Patch": 0,
"Hotfix": 0
}
```
