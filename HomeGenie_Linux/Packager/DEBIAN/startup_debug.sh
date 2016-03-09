#!/bin/sh
#
# Usage: startup_debug.sh <homegenie_folder_path> <debugger_ip_and_port>
# Example: ./startup_debug.sh /usr/local/bin/homegenie 10.0.1.10:10000
#
cd "`dirname \"$0\"`"
# !!!NOTE!!!
# "LC_NUMERIC=en_US LC_MONETARY=en_US LC_MEASUREMENT=en_US" was added as a work-around 
# for buggy mono on ARM (mono < 4.x)
# remove it as soon as official mono 4.x is available through debian-arm/raspbian repository
if [ -f /usr/bin/mono ]
then
    MONO="LC_NUMERIC=en_US LC_MONETARY=en_US LC_MEASUREMENT=en_US /usr/bin/mono"
else
    MONO="LC_NUMERIC=en_US LC_MONETARY=en_US LC_MEASUREMENT=en_US /usr/local/bin/mono"
fi

EXITCODE="1"
while [ "$EXITCODE" = "1" ]; do
    if [ -z "$1" ]
    then
        sudo $MONO --debug --debugger-agent="address=$1,transport=dt_socket,server=y" HomeGenie.exe 
    else
        cd $1
        sudo $MONO --debug --debugger-agent="address=$1,transport=dt_socket,server=y" HomeGenie.exe >/dev/null  2>&1 
    fi
    EXITCODE="$?"
    echo "Exit code: $EXITCODE"
done