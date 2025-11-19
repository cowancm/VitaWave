#!/bin/bash

if [ -z "$1" ] || [ -z "$2" ]; then
    echo "Usage: $0 <ip-address> <debug|release>"
    exit 1
fi

IP="$1"
CONFIG="$(echo "$2" | tr '[:lower:]' '[:upper:]')"   # convert to DEBUG/RELEASE

if [ "$CONFIG" != "DEBUG" ] && [ "$CONFIG" != "RELEASE" ]; then
    echo "Second argument must be either 'debug' or 'release'"
    exit 1
fi

echo "Using IP: $IP"
echo "Build configuration: $CONFIG"

dotnet publish -r linux-arm64 -c "$CONFIG"

ssh vitawave@"$IP" "mkdir -p ~/vitawave ~/bin"

# Copy config file
scp Settings/Ashton4.cfg vitawave@"$IP":vitawave/

# Copy build output
scp "bin/$CONFIG/net9.0/linux-arm64/publish/"* vitawave@"$IP":bin/

# Update symlink
ssh vitawave@"$IP" "rm ~/VitaWave; ln -s ~/bin/VitaWave.ModuleControl ~/VitaWave ; chmod +x ~/VitaWave"
