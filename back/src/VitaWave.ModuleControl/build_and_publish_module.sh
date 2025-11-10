dotnet publish -r linux-arm64 -c Debug
ssh vitawave@192.168.10.2 "mkdir -p ~/vitawave"
scp Settings/Ashton4.cfg vitawave@192.168.10.2:vitawave/
scp bin/Debug/net9.0/linux-arm64/publish/* vitawave@192.168.10.2:bin/
ssh vitawave@192.168.10.2 "rm ~/VitaWave && ln -s ~/bin/VitaWave.ModuleControl ~/VitaWave && chmod +x ~/VitaWave"
