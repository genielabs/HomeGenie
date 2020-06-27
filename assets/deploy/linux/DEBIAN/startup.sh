#!/bin/sh
#
# Usage: startup_debug.sh <homegenie_folder_path>
# Example: ./startup_debug.sh /usr/local/bin/homegenie
#
cd "`dirname \"$0\"`"
ENCFIX=""
#ENCFIX="LC_NUMERIC=en_US LC_MONETARY=en_US LC_MEASUREMENT=en_US "
# !!!NOTE!!!
# "LC_NUMERIC=en_US LC_MONETARY=en_US LC_MEASUREMENT=en_US" was added as a work-around 
# for some buggy mono installations, uncomment if you're experiencing problems with decimal number parsing
if [ -f /usr/bin/mono ]
then
    MONO="$ENCFIX/usr/bin/mono"
else
    MONO="$ENCFIX/usr/local/bin/mono"
fi

EXITCODE="1"
while [ "$EXITCODE" = "1" ]; do
    if [ -z "$1" ]
    then
        sudo $MONO HomeGenie.exe 
    else
        cd $1
        sudo $MONO HomeGenie.exe >/dev/null  2>&1 
    fi
    EXITCODE="$?"
    echo "Exit code: $EXITCODE"
done
