@echo off
echo Cleaning...
dotnet clean
echo Publishing native binary...
rem dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
dotnet publish FolderCompare.csproj -r win-x64 -c Release
pause
