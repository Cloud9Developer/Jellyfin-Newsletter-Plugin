#!/bin/bash
. venv/bin/activate
cd /Development
# dotnet build
# dotnet publish

if [[ "${1}" == "prod" ]]; then
    export JELLYFIN_REPO="./Jellyfin.Plugin.Newsletters"
    export JELLYFIN_REPO_URL="https://github.com/Cloud9Developer/Jellyfin-Newsletter-Plugin/releases/download"
    ./BuildScripts/jprm_build.sh
    cp ./Jellyfin.Plugin.Newsletters/manifest.json ./manifest.json
else
    # dotnet --list-sdks
    # dotnet clean
    dotnet build
    # dotnet publish
fi
exit $?