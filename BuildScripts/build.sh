#!/bin/bash
TAG=${1}

#### V1 ####

# if ! docker images -a | grep mcr.microsoft.com/dotnet/sdk${TAG}.0; then
#     echo "Pulling dotnet:${TAG}.0 image"
#     docker pull mcr.microsoft.com/dotnet/sdk:${TAG}.0
# else
#     echo "Image already exists!"
# fi

# docker run --name dotnet -it -v .:/Development mcr.microsoft.com/dotnet/sdk:${TAG}.0 /Development/BuildScripts/dotnetbuild.sh
# docker rm dotnet

#### V2 ####
IMAGE="pypi_dotnet"
echo "Using image: '${IMAGE}:${TAG}.0'"
if ! docker images -a | grep "${IMAGE}" | grep "${TAG}.0"; then
    echo "Generating '${IMAGE}:${TAG}.0' image"
    docker build -t ${IMAGE}:${TAG}.0 --build-arg IMAGE_TAG=8 ./BuildScripts/
else
    echo "Image already exists!"
fi

docker run --name dotnet -it -v .:/Development ${IMAGE}:${TAG}.0 /Development/BuildScripts/dotnetbuild.sh "${2}"
docker rm dotnet

# docker build -t pypi_dotnet:8.0 --build-arg IMAGE_TAG=8 .