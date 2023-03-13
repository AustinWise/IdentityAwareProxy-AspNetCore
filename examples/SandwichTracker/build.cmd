@echo off
setlocal

pushd %~dp0

dotnet publish --os linux --arch x64 /t:PublishContainer -c Release
