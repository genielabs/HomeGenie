#!/bin/sh

if [-f/usr/bin/mono]
then
MONO=/usr/bin/mono
else
MONO=/usr/local/bin/mono
fi
cd $1 && sudo $MONO HomeGenie.exe > /dev/null 2>&1
