# Folder Compare

## A small CLI project to compare files in 2 directory trees, by name, or hash.

This project is implemented in two languages, Rust and C#. It was written to compare the performance of the two languages, and to explore their comparative ergonomics.

The two implementations are in `/CSharp` implemented in .NET 7 C#, and `/Rust` in Rust 1.71 (August 2023). Both have almost identical behaviour. See below for benchmarks.

Both were developed on Windows, but should work on Linux and Mac.

## Building

Build the C# version use `dotnet publish -c Release` or the supplied `Publish.cmd` (Or Visual Studio 2022, which is free)

Build the Rust version with `cargo build -r`

## Details
The program walks the first folder tree (given by `-a`) and records all filenames, sizes and optionally hashes. It does the same with the second folder tree (given by `-b`). Files are compared (using `-c`) and differences listed. Comparison can be via:

```
    Name        Filename only (default, fast)
    NameSize    Filename and file size (fast)
    Hash        SHA2 hash, disregarding filenames (slow)
```

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

Benchmarks from Hyperfine, run on a wheezy old laptop. Code from ver 0.9.1 (1ccbdca0). All times in milliseconds (lower is better). Test folders have 800-1200 file differences.

| Benchmark      | Rust single-thread   | C# single-thread  | Difference  | Rust parallel | C# parallel | Difference |
| ----------- | -----------   | -----------  | ----------- | -----------     | ----------- | --------- |          
| Comparing by name | 71 | 288 |	x4	|	    61	|    281   |	x4.7 |
| Second run		| 74 | 283 |		|	    60	|    284   | |
| Comparing by hash | 3044 | 3658 |	x1.2 |      2412 |   2913  |	x1.2 |
| Second run	    | 3054 | 3667 |      |		2408 |   3018  | |

Hashing is obviously more expensive than comparison by filename. The parallel code is around 25% faster than single-threaded (a maximum of 2 threads are used, and only for the folder enumeration and hashing). The C# code performs suprisingly well for the hashing tests (20% slower than Rust). For file comparison it is 400% slower, but still only takes 0.28 seconds. There is probably a significant JIT penalty for C# start-up.

## C# Publishing

```
Normal publish:

	dotnet publish -c Release

Self-contained publish:

	dotnet publish -c Release -r win-x64 --self-contained true
	dotnet publish -c Release -r linux-x64 --self-contained true
	dotnet publish -c Release -r osx-x64 --self-contained true

You can also enable Single File publish by uncommenting the following in the .csproj file:

	<PublishSingleFile>true</PublishSingleFile>
	<SelfContained>true</SelfContained>
	<RuntimeIdentifier>win-x64</RuntimeIdentifier>

Then just run the normal publish command.
```

`Publish.cmd` is provided to simplify publishing.

## Testing scripts

Tests are written in Powershell, so they can be used for both implementations.

```
PS > cd .\Testing
PS > .\TestCSharp.ps1
PS > .\TestRust.ps1
```
