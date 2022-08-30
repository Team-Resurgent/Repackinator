@echo off
dotnet publish -p:PublishSingleFile=true -r win-x86 -c Release --self-contained false -p:PublishTrimmed=false
pause