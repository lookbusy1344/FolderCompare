@echo off
echo Cleaning...
dotnet clean
echo Publishing single file...
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
cd .\bin\Release\net7.0\win-x64\publish\
pause
