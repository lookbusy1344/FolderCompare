# Test folders

`Foldera` and `Folderb` can be used for testing. The trees are different (everything in Folderb is nested) They contain the following files:

```
    name match.txt                      Same name, diffent size and content (match on NAME)
    name and size match.txt             Same name & size but different content (match on NAME and NAMESIZE)
    hash match X.txt                    Different name, same size and content (match on HASH)
    no match X.txt                      Different name and content (no match)
```

## Run with

```
folder_compare.exe -a ./Testing/foldera -b ./Testing/folderb -c name -r
folder_compare.exe -a ./Testing/foldera -b ./Testing/folderb -c namesize -r
folder_compare.exe -a ./Testing/foldera -b ./Testing/folderb -c hash -r
```

## Testing scripts

Two Powershell scripts are provided, to test both versions and compare the output with the expected results.

```
PS > .\TestCSharp.ps1
PS > .\TestRust.ps1
```
