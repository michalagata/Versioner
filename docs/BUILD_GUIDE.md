# Versioner Build Guide

This guide explains how to build, test, and deploy the Versioner application.

## Prerequisites

- .NET 8.0 SDK or later
- Git
- Docker (optional)
- Linux/macOS/Windows

## Quick Start

### 1. Clone Repository

```bash
git clone https://github.com/anubisworks/Versioner.git
cd Versioner
```

### 2. Build Application

```bash
# Build for Linux
./scripts/build.sh

# Build for Windows
./scripts/build.sh -r win-x64

# Build with custom configuration
./scripts/build.sh -c Debug -o /tmp/versioner
```

### 3. Test Application

```bash
# Run tests
dotnet test Tests/Versioner.Tests.csproj

# Run with coverage
dotnet test Tests/Versioner.Tests.csproj --collect:"XPlat Code Coverage"
```

## Build Scripts

### build.sh

Main build script for the .NET application.

**Features:**
- Multi-platform support (Linux/Windows)
- Self-contained deployment option
- Automatic testing
- Package creation
- Environment validation

**Usage:**
```bash
./scripts/build.sh [OPTIONS]

OPTIONS:
    -c, --config CONFIG        Build configuration (Debug|Release)
    -r, --runtime RUNTIME      Target runtime (linux-x64|win-x64)
    -s, --self-contained       Create self-contained deployment
    -o, --output DIR           Output directory
    -h, --help                 Show help
```

**Examples:**
```bash
# Basic build
./scripts/build.sh

# Debug build for Windows
./scripts/build.sh -c Debug -r win-x64

# Self-contained build
./scripts/build.sh -s -o /tmp/versioner

# Custom output directory
./scripts/build.sh -o /custom/output
```

### _performBuildDocker.sh

Docker image build script.

**Features:**
- Multi-platform builds
- Registry support
- Image tagging
- Automatic cleanup
- Size optimization

**Usage:**
```bash
./scripts/_performBuildDocker.sh [OPTIONS]

OPTIONS:
    -n, --name NAME           Image name
    -t, --tag TAG             Image tag
    -r, --registry REGISTRY   Docker registry URL
    -p, --platform PLATFORM  Target platform
    --push                    Push image to registry
    -h, --help                Show help
```

**Examples:**
```bash
# Basic build
./scripts/_performBuildDocker.sh

# Build with custom name and tag
./scripts/_performBuildDocker.sh -n my-versioner -t v1.0.0

# Build and push to registry
./scripts/_performBuildDocker.sh -r registry.example.com --push

# Multi-platform build
./scripts/_performBuildDocker.sh -p linux/amd64
./scripts/_performBuildDocker.sh -p linux/arm64
```

## Build Configurations

### Debug Configuration

- **Optimization**: Disabled
- **Debug symbols**: Full
- **Define constants**: DEBUG, TRACE
- **Use case**: Development and debugging

```bash
./scripts/build.sh -c Debug
```

### Release Configuration

- **Optimization**: Enabled
- **Debug symbols**: Portable
- **Define constants**: TRACE
- **Use case**: Production deployment

```bash
./scripts/build.sh -c Release
```

## Target Runtimes

### linux-x64

- **Platform**: Linux x64
- **Self-contained**: Optional
- **Dependencies**: .NET 8.0 Runtime

```bash
./scripts/build.sh -r linux-x64
```

### win-x64

- **Platform**: Windows x64
- **Self-contained**: Optional
- **Dependencies**: .NET 8.0 Runtime

```bash
./scripts/build.sh -r win-x64
```

## Self-Contained Deployment

Self-contained deployments include the .NET runtime and don't require .NET to be installed on the target machine.

### Advantages

- **No dependencies**: .NET runtime included
- **Portable**: Single executable
- **Isolated**: No version conflicts

### Disadvantages

- **Larger size**: Includes runtime
- **Platform specific**: One build per platform
- **Update complexity**: Runtime updates require rebuild

### Usage

```bash
# Self-contained build
./scripts/build.sh -s

# Self-contained for specific platform
./scripts/build.sh -s -r win-x64
```

## Package Creation

The build script automatically creates deployment packages:

### Linux Package

- **Format**: ZIP
- **Name**: `Versioner.linux-x64.zip`
- **Contents**: Application files, dependencies

### Windows Package

- **Format**: ZIP
- **Name**: `Versioner.win-x64.zip`
- **Contents**: Application files, dependencies

### Package Contents

```
Versioner.linux-x64.zip
├── Versioner.Cli
├── Versioner.dll
├── Versioner.deps.json
├── Versioner.runtimeconfig.json
└── [dependencies...]
```

## Testing

### Unit Tests

```bash
# Run all tests
dotnet test Tests/Versioner.Tests.csproj

# Run with specific configuration
dotnet test Tests/Versioner.Tests.csproj -c Release

# Run with coverage
dotnet test Tests/Versioner.Tests.csproj --collect:"XPlat Code Coverage"
```

### Integration Tests

```bash
# Run integration tests
dotnet test Tests/Versioner.Tests.csproj --filter Category=Integration
```

### Test Coverage

```bash
# Generate coverage report
dotnet test Tests/Versioner.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults

# View coverage report
open TestResults/*/coverage.cobertura.xml
```

## Docker Build

### Basic Docker Build

```bash
# Build Docker image
./scripts/_performBuildDocker.sh

# Build with custom name
./scripts/_performBuildDocker.sh -n versioner -t v1.0.0
```

### Multi-Platform Build

```bash
# Build for multiple platforms
./scripts/_performBuildDocker.sh -p linux/amd64
./scripts/_performBuildDocker.sh -p linux/arm64

# Create manifest
docker manifest create versioner:latest \
  versioner:linux-amd64 \
  versioner:linux-arm64
docker manifest push versioner:latest
```

### Registry Deployment

```bash
# Build and push to registry
./scripts/_performBuildDocker.sh \
  -r registry.example.com \
  -n versioner \
  -t v1.0.0 \
  --push
```

## Environment Variables

### Build Configuration

- `BUILD_CONFIG` - Build configuration (Debug|Release)
- `RUNTIME` - Target runtime (linux-x64|win-x64)
- `SELF_CONTAINED` - Self-contained flag (true|false)
- `OUTPUT_DIR` - Output directory

### Docker Configuration

- `IMAGE_NAME` - Docker image name
- `IMAGE_TAG` - Docker image tag
- `REGISTRY` - Docker registry URL
- `PLATFORM` - Target platform
- `PUSH` - Push flag (true|false)

## CI/CD Integration

### GitHub Actions

```yaml
name: Build and Test

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Build
      run: ./scripts/build.sh -c Release
    
    - name: Test
      run: dotnet test Tests/Versioner.Tests.csproj
    
    - name: Build Docker
      run: ./scripts/_performBuildDocker.sh
```

### Azure DevOps

```yaml
trigger:
- main

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: DotNetCoreCLI@2
  displayName: 'Restore packages'
  inputs:
    command: 'restore'
    projects: '**/*.csproj'

- task: DotNetCoreCLI@2
  displayName: 'Build'
  inputs:
    command: 'build'
    projects: '**/*.csproj'
    arguments: '--configuration Release'

- task: DotNetCoreCLI@2
  displayName: 'Test'
  inputs:
    command: 'test'
    projects: '**/*.csproj'
    arguments: '--configuration Release --collect:"XPlat Code Coverage"'
```

## Troubleshooting

### Common Issues

1. **Build Fails**
   ```bash
   # Check .NET version
   dotnet --version
   
   # Restore packages
   dotnet restore
   
   # Clean and rebuild
   ./scripts/clean.sh
   ./scripts/build.sh
   ```

2. **Tests Fail**
   ```bash
   # Run tests with verbose output
   dotnet test Tests/Versioner.Tests.csproj --verbosity normal
   
   # Run specific test
   dotnet test Tests/Versioner.Tests.csproj --filter "TestName"
   ```

3. **Docker Build Fails**
   ```bash
   # Check Docker version
   docker --version
   
   # Clean Docker cache
   docker system prune -a
   
   # Build with verbose output
   docker build --progress=plain .
   ```

### Debug Mode

```bash
# Enable debug logging
export LOG_LEVEL=Debug
./scripts/build.sh
```

### Log Files

Build scripts create log files:
- `build.log` - Build process logs
- `docker.log` - Docker operation logs

## Performance Optimization

### Build Performance

1. **Parallel builds**: Use `-j` flag for parallel compilation
2. **Incremental builds**: Only rebuild changed files
3. **Caching**: Use build caches for dependencies
4. **Clean builds**: Regular clean builds to avoid issues

### Docker Performance

1. **Multi-stage builds**: Separate build and runtime stages
2. **Layer caching**: Optimize Dockerfile layers
3. **Base images**: Use minimal base images
4. **Build context**: Minimize build context size

## Security Considerations

### Build Security

1. **Dependency scanning**: Scan for vulnerabilities
2. **Code signing**: Sign executables
3. **Secure builds**: Use secure build environments
4. **Access control**: Limit build access

### Docker Security

1. **Non-root user**: Run containers as non-root
2. **Minimal images**: Use minimal base images
3. **Vulnerability scanning**: Scan images for vulnerabilities
4. **Registry security**: Use secure registries

## Best Practices

### Development

1. **Regular builds**: Build frequently during development
2. **Test coverage**: Maintain high test coverage
3. **Code quality**: Use static analysis tools
4. **Documentation**: Keep build documentation updated

### Production

1. **Release builds**: Use Release configuration
2. **Versioning**: Use semantic versioning
3. **Signing**: Sign production builds
4. **Monitoring**: Monitor build performance

### CI/CD

1. **Automated builds**: Automate all builds
2. **Quality gates**: Implement quality gates
3. **Deployment**: Automate deployment
4. **Rollback**: Implement rollback procedures

## Support

For build issues:

1. Check logs: `cat build.log`
2. Verify environment: `dotnet --info`
3. Clean and rebuild: `./scripts/clean.sh && ./scripts/build.sh`
4. Check GitHub issues: [Versioner Issues](https://github.com/anubisworks/Versioner/issues)
