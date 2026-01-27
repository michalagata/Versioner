# Versioner.Cli

Tool for managing versions in .NET, TypeScript, and other projects.

## Installation

```bash
dotnet tool install -g Versioner.Cli
```

## Usage

### Basic Usage

```bash
versioner [options]
```

### Options

#### Required Options

- `-p, --project <path>` - Path to project file (.csproj, .props, .nuspec, package.json)
- `-v, --version <version>` - New version to set (format: x.y.z.w)

#### Optional Options

- `-d, --description <description>` - Project description
- `-s, --suffix <suffix>` - Version suffix (e.g., for pre-release versions)
- `-c, --config <path>` - Path to configuration file
- `-o, --output <path>` - Path to output file
- `-q, --quiet` - Quiet mode (no messages displayed)
- `-h, --help` - Display help

### Usage Examples

1. Setting version in .NET project:
```bash
versioner -p MyProject.csproj -v 1.2.3.4
```

2. Setting version with pre-release suffix:
```bash
versioner -p MyProject.csproj -v 1.2.3.4 -s beta
```

3. Setting version with description:
```bash
versioner -p MyProject.csproj -v 1.2.3.4 -d "New version with fixes"
```

4. Using configuration file:
```bash
versioner -p MyProject.csproj -v 1.2.3.4 -c config.json
```

### Configuration File Format

```json
{
  "AssemblyInfoVersionSet": true,
  "AssemblyVersionSet": true,
  "AssemblyFileVersionSet": true,
  "HashAsDescription": false,
  "AssemblyInfoFile": "Properties/AssemblyInfo.cs"
}
```

### Supported File Types

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

### CI/CD Integration

The tool can be integrated with CI/CD systems such as TeamCity, Azure DevOps, or GitHub Actions.

Example for TeamCity:
```bash
versioner -p MyProject.csproj -v %build.number% -s %build.branch%
```

### Error Handling

The tool returns the following exit codes:
- 0 - Success
- 1 - General error
- 2 - Configuration error
- 3 - File access error
- 4 - Invalid version

### Limitations

1. Versions must be in format x.y.z.w, where:
   - x, y, z, w are integers
   - x, y, z, w >= 0
   - x, y, z, w <= 65535

2. Version suffix:
   - Can only contain letters, numbers, dots, and hyphens
   - Maximum length: 20 characters

### Troubleshooting

1. Problem: Cannot find project file
   - Check if path is correct
   - Ensure you have read permissions for the file

2. Problem: Invalid version
   - Check version format (x.y.z.w)
   - Ensure all numbers are in range 0-65535

3. Problem: File access error
   - Check file permissions
   - Ensure file is not being used by other processes

### Support

For issues or questions:
1. Check documentation
2. Open an issue on GitHub
3. Contact support team

### License

MIT License
