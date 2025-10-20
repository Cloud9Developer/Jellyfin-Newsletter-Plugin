#!/bin/bash
dir="RELEASES"
if [ ! -d "${dir}" ]; then
    mkdir ${dir}
fi

read -p "VERSION: " ver

zip -j ${dir}/newsletters_${ver}.zip \
    Jellyfin.Plugin.Newsletters/bin/Release/net9.0/Jellyfin.Plugin.Newsletters.dll \
    Jellyfin.Plugin.Newsletters/bin/Release/net9.0/publish/SQLitePCL.pretty.dll

echo '---'

echo "CHECKSUM: `md5sum ${dir}/newsletters_${ver}.zip`"