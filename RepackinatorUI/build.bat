@echo off
dotnet publish -p:PublishSingleFile=true -r win-x64 -c Release --self-contained true -p:PublishTrimmed=false -p:DebugType=None -p:DebugSymbols=false
pause