#!/usr/bin/env bash

# This script runs after the initial clone of the repo, but before the build process starts
# It will inject the VS App Center secrets necessary for collecting app telemetry
# It assumes the machine running has the AndroidAppCenterId and IOSAppCenterId environment vars set


# Install JQ for modifying the template.appsettings.json file. 
sudo apt-get -y install jq

# Pipe template into the appsettings file and replace values with proper secrets
cat template.appsettings.json | jq \
 --arg androidAppCenterId  "$AndroidAppCenterId" \
 --arg iosAppCenterId "$IOSAppCenterId" \
 '.IOSAppCenterId=$iosAppCenterId| .AndroidAppCenterId=$androidAppCenterId' > appsettings.json