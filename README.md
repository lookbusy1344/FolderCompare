# FolderCompare
## A toy project CLI to compare files in 2 directory trees, by name, or hash.

Two implementations are provided, `/CSharp` implemented in .NET 7 C#, and `/Rust` in Rust 1.70 (June 2023). Both have almost identical behaviour, but the Rust one is probably faster and the CSharp one has a couple of extra features. Use `-h` for information.

## Building

Build the C# version use `dotnet build FolderCompare.csproj` (Or Visual Studio 2022, which is free)

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
    -h, --help                   Help
```

Hashing uses SHA256 and is obviously much slower than just comparing on name and/or size.
