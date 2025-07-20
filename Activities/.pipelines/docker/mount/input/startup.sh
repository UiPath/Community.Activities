#!/bin/sh

mkdir -p /home/robotuser/.local/share/UiPath/
cp -vf /input/UiPath.settings /home/robotuser/.local/share/UiPath/
/application/startup.sh