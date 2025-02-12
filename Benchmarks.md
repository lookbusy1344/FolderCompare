# RUNNING BENCHMARKS

Benchmarks are calculated by comparing the Rust `debug` and `release` folders. They typically contain about 1000 files and 400 folders after building.

Benchmarks are measured using Hyperfine.

Both C# and Rust binaries should be in your path. Then cd to: `...\FolderCompare\Rust\target`


## COMPARE BY NAME - MULTI THREAD

```
hyperfine.exe "FolderCompare.exe -a debug -b release -c name"		71ms, 70ms	x1.4
hyperfine.exe "Folder_Compare.exe -a debug -b release -c name"		49ms, 50ms
```

## COMPARE BY NAME - SINGLE THREAD

```
hyperfine.exe "FolderCompare.exe -a debug -b release -c name -o"	65ms, 65ms	x1.03
hyperfine.exe "Folder_Compare.exe -a debug -b release -c name -o"	61ms, 65ms
```

## COMPARE BY HASH - MULTI THREAD

```
hyperfine.exe "FolderCompare.exe -a debug -b release -c hash"		1390ms, 1410ms	x1.12
hyperfine.exe "Folder_Compare.exe -a debug -b release -c hash"		1253ms, 1246ms
```

## COMPARE BY HASH - SINGLE THREAD

```
hyperfine.exe "FolderCompare.exe -a debug -b release -c hash -o"	1994ms, 1974ms	x1.1
hyperfine.exe "Folder_Compare.exe -a debug -b release -c hash -o"	1808ms, 1801ms
```
