# To build in .NET

```
dotnet clean

dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true

dotnet publish FolderCompare.csproj -r win-x64 -c Release
```
