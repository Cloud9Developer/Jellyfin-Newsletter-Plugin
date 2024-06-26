#!/bin/bash
dir="RELEASES"
if [ ! -d "${dir}" ]; then
    mkdir ${dir}
fi

read -p "VERSION: " ver

zip -j ${dir}/Newsletters-v${ver}.zip \
    Jellyfin.Plugin.Newsletters/bin/Release/net8.0/Jellyfin.Plugin.Newsletters.dll \
    Jellyfin.Plugin.Newsletters/bin/Release/net8.0/publish/SQLitePCL.pretty.dll

echo '---'

echo "CHECKSUM: `md5sum ${dir}/Newsletters-v${ver}.zip`"