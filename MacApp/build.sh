#!/bin/bash

mkdir -p rosi.bundle
cp -a ../multi/* rosi.bundle
xcodebuild -project Rosi.xcodeproj -configuration Release
cd Build/Release
zip -9 -r ../../../macapp.zip Rosi.app
