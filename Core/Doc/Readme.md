# Versioner

Tool for managing versions in .NET, TypeScript, and other projects.

## Running

The basic command is:
```bash
dotnet Versioner.dll [options]
```

## Configuration Options

### Required Options

- `-WorkingFolder, --workingfolder <path>` 
  - Specifies the directory where Versioner will be executed
  - Must be a repository directory
  - Example: `--workingfolder="D:\Projects\MyRepo"`

### Optional Options

- `-UseDefaults, --usedefaults [0/1]`
  - Uses default settings
  - Should be provided as the first switch
  - If provided, the only required parameter is WorkingFolder
  - Example: `--usedefaults`

- `-ConfigurationUseUrl, --configurationuseurl <url>`
  - Specifies URI to configuration
  - Can be:
    - Relative path
    - Absolute path
    - UNC path
    - URL
  - IMPORTANT: If provided, the UseConfigServer option will be automatically set to false
  - Example: `--configurationuseurl="https://config-server.example.com/config"`

- `-LogLevel, --loglevel <level>`
  - Specifies logging level
  - Available values:
    - V (Verbose) - most information
    - D (Debug) - debugging information
    - I (Info) - standard information
    - W (Warning) - warnings
    - E (Error) - errors
    - F (Fatal) - critical errors
  - Example: `--loglevel=I`

- `-StoreVersionFile, --storeversionfile`
  - Saves version file in project directory
  - Example: `--storeversionfile`

- `-UseConfigServer, --useconfigserver`
  - Uses configuration server
  - Example: `--useconfigserver`

## Usage Examples

1. Typical usage in TeamCity projects:
```bash
dotnet Versioner.dll --workingfolder="path_to_cloned_repo" --usedefaults
```

2. Usage with custom configuration:
```bash
dotnet Versioner.dll --workingfolder="path_to_cloned_repo" --configurationfile="dummy-config.json" --storeversionfile
```

3. Offline mode with default settings:
```bash
dotnet Versioner.dll -UseDefaults --workingfolder="path_to_cloned_repo"
```

4. Display help:
```bash
dotnet Versioner.dll --help
```

## Configuration File Format

```json
{
  "AssemblyInfoVersionSet": true,
  "AssemblyVersionSet": true,
  "AssemblyFileVersionSet": true,
  "HashAsDescription": false,
  "AssemblyInfoFile": "Properties/AssemblyInfo.cs",
  "UseConfigServer": false,
  "ConfigurationUrl": "https://some.url.example",
  "LogLevel": "Info"
}
```

## Supported File Types

1. .NET Projects (.csproj)
   - Sets version in project file
   - Updates AssemblyInfo.cs (if exists)
   - Supports SDK-style and legacy projects

2. .props Files
   - Sets version in .props file
   - Updates all related projects

3. .nuspec Files
   - Sets version in .nuspec file
   - Supports pre-release versions

4. package.json Files
   - Sets version in package.json
   - Supports pre-release versions

## CI/CD Integration

### TeamCity

```bash
dotnet Versioner.dll --workingfolder="%teamcity.build.workingDir%" --usedefaults
```

### Azure DevOps

```yaml
- task: DotNetCoreCLI@2
  inputs:
    command: 'run'
    arguments: 'Versioner.dll --workingfolder="$(Build.SourcesDirectory)" --usedefaults'
```

### GitHub Actions

```yaml
- name: Version Project
  run: dotnet Versioner.dll --workingfolder="${{ github.workspace }}" --usedefaults
```

## Error Handling

The tool returns the following exit codes:
- 0 - Success
- 1 - General error
- 2 - Configuration error
- 3 - File access error
- 4 - Invalid version

## Troubleshooting

1. Problem: Cannot find working directory
   - Check if path is correct
   - Ensure you have permissions to the directory
   - Check if directory is a repository directory

2. Problem: Configuration error
   - Check configuration file format
   - Ensure all required fields are set
   - Check configuration server availability (if used)

3. Problem: File access error
   - Check file permissions
   - Ensure files are not being used by other processes
   - Check if paths are correct

## Support

For issues or questions:
1. Check documentation
2. Open an issue on GitHub
3. Contact support team

## License

MIT License
