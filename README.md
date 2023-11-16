# Folder Compare

[![Building Rust app](https://github.com/lookbusy1344/FolderCompare/actions/workflows/Rust%20build.yml/badge.svg)](https://github.com/lookbusy1344/FolderCompare/actions/workflows/Rust%20build.yml)
[![CodeQL](https://github.com/lookbusy1344/FolderCompare/actions/workflows/github-code-scanning/codeql/badge.svg)](https://github.com/lookbusy1344/FolderCompare/actions/workflows/github-code-scanning/codeql)

## A small CLI project to compare files in 2 directory trees, by name, or hash.

This project is implemented in two languages, Rust and C#. It was written to compare the performance of the two languages, and to explore their comparative ergonomics.

The two implementations are in `/CSharp` implemented in .NET 8 C#, and `/Rust` in Rust 1.73 (October 2023). Both have almost identical behaviour. See below for benchmarks.

Both were developed on Windows, but should work on Linux and Mac.

## Building

Build the C# version use `dotnet publish -c Release` or the supplied `Publish.cmd` (Or Visual Studio 2022, which is free)

Build the Rust version with `cargo build -r`

## Details
The program walks the first folder tree (given by `-a`) and records all filenames, sizes and optionally hashes. It does the same with the second folder tree (given by `-b`). Files are compared (using `-c`) and differences listed. Comparison can be via:

| Comparison | Description |
| -- | -- |
| --comparison Name       | Filename only (default, fast) |
| --comparison NameSize   | Filename and file size (fast) |
| --comparison Hash       | SHA2 hash, disregarding filenames (slow) |

Comparison by name only checks the filename itself, not the path. Eg `a/b/file.txt` and `d/e/file.txt` will be considered the same file.


## Usage

```
folder_compare.exe -a <folder> -b <folder> [-c <comparison>] [-r] [-f]

Eg:

folder_compare.exe -a ./target/debug -b ./target/release -c hash
```

MANDATORY PARAMETERS:
```
    -a, --foldera                First folder to compare
    -b, --folderb                Second folder to compare
```

OPTIONS:
```
    -c, --comparison [value]     Comparison to use (Name, NameSize or Hash). Default is Name
    -r, --raw                    Raw output, for piping
    -f, --first-only             Only show files in folder A missing from folder B (default is both)
    -o, --one-thread             Only use one thread (don't scan the two folders in parallel)
    -h, --help                   Help
```

Hashing uses SHA256 and is obviously much slower than just comparing on name and/or size.

## Implementation notes

Implementing pluggable comparers (name / name & size / hash) is more difficult in Rust than in C#. C# allows different implementations of `IEqualityComparer<FileData>`.

In Rust you have to use 'unit structs' to mark the different comparisons, and then implemented `Eq`, `PartialEq` and `Hash` traits on `FileData<..marker struct..>` for each comparison technique.

This does mean that `FileData<a>` isn't type compatible with `FileData<b>`, which is an ugly side effect. An implementation of `HashSet` that took lambdas for hashing and comparison would be useful here!

## Benchmarks

Benchmarks from Hyperfine, run on a wheezy old laptop. Code from ver 1.0.3 (d75783c276c6e55d). The C# version is compiled to a native binary, to improve startup speed. All times in milliseconds (lower is better). Test folders have 800-1200 file differences.

| Benchmark      | Rust single-thread   | C# single-thread  | Difference  | Rust parallel | C# parallel | Difference |
| ----------- | -----------   | -----------  | ----------- | -----------     | ----------- | --------- |          
| Comparing by name | 80 | 77 |	x0.97 (dead-heat)	                    |	    64	|    87   |	x1.28 |
| Second run		| 78 | 77 |		                                    |	    66	|    78   | |
| Comparing by hash | 3840 | 4410 |	x1.13                               |      2738 |   3106  |	x1.13 |
| Second run	    | 3785 | 4271 |                                     |		2700 |   3050  | |

Hashing is obviously more expensive than comparison by filename. The parallel code is around 30% faster than single-threaded (a maximum of 2 threads are used, and only for the folder enumeration and hashing).

The C# code performs suprisingly well (only 13% slower than Rust for the heavier workload of hashing, and 28% slower for name comparison). This was improved by switching to 'NativeAOT' compilation (building a native binary with no JIT).

## C# Publishing

`Publish.cmd` is provided to simplify publishing. NativeAOT compilation is used, to build a large but comparatively fast native binary. It contains just:

```
dotnet publish FolderCompare.csproj -r win-x64 -c Release
```

## Testing scripts

Tests are written in Powershell, so they can be used for both implementations.

```
PS > cd .\Testing
PS > .\TestCSharp.ps1
PS > .\TestRust.ps1
```
