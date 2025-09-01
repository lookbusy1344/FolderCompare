# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

FolderCompare is a CLI tool that compares files in two directory trees using different comparison methods (name, name+size, or hash). The project has dual implementations in C# (.NET 9) and Rust, designed for performance comparison between the two languages.

## Architecture

### Dual Implementation Structure
- **C#**: Located in `/CSharp/` - Uses .NET 9 with NativeAOT compilation for performance
- **Rust**: Located in `/Rust/` - Standard Rust implementation with optimized release profile
- **Testing**: Shared PowerShell tests in `/Testing/` that work with both implementations

### Core Components
- **FileData**: Core data structure representing files with different comparison markers
- **Comparison Types**: Name-only, Name+Size, Hash-based (SHA256)
- **Parallel Processing**: Both implementations support multi-threaded folder scanning

### Key Design Pattern
The Rust implementation uses "unit structs" as type markers to implement different comparison strategies, making `FileData<MarkerA>` incompatible with `FileData<MarkerB>`. The C# version uses `IEqualityComparer<FileData>` for pluggable comparisons.

## Development Commands

### C# Commands
```bash
# Build and run
cd CSharp
dotnet build
dotnet run -- -a <foldera> -b <folderb> -c <comparison>

# Release build with NativeAOT
dotnet publish FolderCompare.csproj -r win-x64 -c Release

# Quick publish (uses Publish.cmd)
./Publish.cmd

# Check vulnerabilities
./CheckVul.cmd
```

### Rust Commands
```bash
# Build and run
cd Rust
cargo build
cargo run -- -a <foldera> -b <folderb> -c <comparison>

# Optimized release build
cargo build -r

# Format code (ALWAYS run after making changes)
cargo fmt
```

### Testing
Tests are implemented in PowerShell and compare output against expected results:

```bash
cd Testing
# Test C# implementation
./TestCSharp.ps1
# Test Rust implementation  
./TestRust.ps1
```

Both test scripts expect the respective binaries (`foldercompare.exe` for C#, `folder_compare.exe` for Rust) to be in PATH or current directory.

## Code Style Guidelines
- Target senior engineers with concise, modern coding practices
- Favor functional programming style where appropriate
- Keep code changes minimal and focused
- Brief commit messages with single sentence summaries
- Use most modern language idioms for both C# and Rust