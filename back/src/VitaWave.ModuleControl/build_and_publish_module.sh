#!/bin/bash

if [ -z "$1" ]; then
    echo "Usage: $0 <ip-address>"
    exit 1
fi

IP="$1"

echo "Using IP: $IP"

dotnet publish -r linux-arm64 -c Debug

ssh vitawave@"$IP" "mkdir -p ~/vitawave"

scp Settings/Ashton4.cfg vitawave@"$IP":vitawave/

scp bin/Debug/net9.0/linux-arm64/publish/* vitawave@"$IP":bin/

ssh vitawave@"$IP" "rm ~/VitaWave; ln -s ~/bin/VitaWave.ModuleControl ~/VitaWave ; chmod +x ~/VitaWave"
