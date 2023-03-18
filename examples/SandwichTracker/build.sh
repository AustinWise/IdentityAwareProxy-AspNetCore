#!/usr/bin/env bash

set -euo pipefail
set -x

if [ $# != 1 ];
then
    echo not enough args
    exit 1
fi

dotnet publish --os linux --arch x64 /t:PublishContainer -c Release -p:ContainerImageTag=$1
docker push us-central1-docker.pkg.dev/test-iap-379718/sandwich-apps/sandwichtracker:$1
