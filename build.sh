#!/bin/bash

set -euo pipefail
set -x

git clean -dxf

PKG_PATH=bin/pkg
PUB_DIR=bin/pub
PUB_RID=linux-x64

mkdir -p $PKG_PATH
# Commands from https://github.com/GoogleCloudPlatform/buildpacks/blob/main/cmd/dotnet/publish/main.go
dotnet restore --packages $PKG_PATH --runtime $PUB_RID
dotnet publish -nologo --verbosity minimal --configuration Release --output $PUB_DIR --no-restore --packages $PKG_PATH --self-contained false --runtime $PUB_RID
