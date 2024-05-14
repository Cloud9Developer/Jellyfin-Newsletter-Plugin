#!/bin/bash
TAG=${1}
if ! docker images -a | grep mcr.microsoft.com/dotnet/sdk${TAG}.0; then
    echo "Pulling dotnet:${TAG}.0 image"
    docker pull mcr.microsoft.com/dotnet/sdk:${TAG}.0
else
    echo "Image already exists!"
fi

docker run --name dotnet -it -v .:/Development mcr.microsoft.com/dotnet/sdk:${TAG}.0 /Development/BuildScripts/dotnetbuild.sh
docker rm dotnet