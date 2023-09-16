# RUNNING BENCHMARKS

Benchmarks are calculated by comparing the Rust `debug` and `release` folders. They typically contain about 1000 files and 400 folders after building.

Benchmarks are measured using Hyperfine.

Both C# and Rust binaries should be in your path. Then cd to: `...\FolderCompare\Rust\target`


## COMPARE BY NAME - MULTI THREAD

```
hyperfine.exe "FolderCompare.exe -a debug -b release -c name"		87ms, 78ms	x1.28
hyperfine.exe "Folder_Compare.exe -a debug -b release -c name"		64ms, 66ms
```

## COMPARE BY NAME - SINGLE THREAD

```
hyperfine.exe "FolderCompare.exe -a debug -b release -c name -o"	77ms, 77ms	x0.97
hyperfine.exe "Folder_Compare.exe -a debug -b release -c name -o"	80ms, 78ms
```

## COMPARE BY HASH - MULTI THREAD

```
hyperfine.exe "FolderCompare.exe -a debug -b release -c hash"		3106ms, 3050ms	x1.13
hyperfine.exe "Folder_Compare.exe -a debug -b release -c hash"		2738ms, 2700ms
```

## COMPARE BY HASH - SINGLE THREAD

```
hyperfine.exe "FolderCompare.exe -a debug -b release -c hash -o"	4410ms, 4271ms	x1.13
hyperfine.exe "Folder_Compare.exe -a debug -b release -c hash -o"	3840ms, 3785ms
```
