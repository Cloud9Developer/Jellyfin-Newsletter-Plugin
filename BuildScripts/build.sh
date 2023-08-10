#!/bin/bash
if ! docker images -a | grep mcr.microsoft.com/dotnet/sdk; then
    echo "Pulling dotnet:6.0 image"
    docker pull mcr.microsoft.com/dotnet/sdk:6.0
else
    echo "Image already exists!"
fi

docker run --name dotnet -it -v .:/Development mcr.microsoft.com/dotnet/sdk:6.0 /Development/BuildScripts/dotnetbuild.sh
docker rm dotnet