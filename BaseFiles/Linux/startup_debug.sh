#!/bin/sh
if [ -f /usr/bin/mono ]
then
	MONO=/usr/bin/mono
else
	MONO=/usr/local/bin/mono
fi

EXITCODE="1"
while [ "$EXITCODE" = "1" ]; do
	if [ -z "$1" ]
	then
		sudo $MONO --debug --debugger-agent="address=10.0.1.10:10000,transport=dt_socket,server=y" HomeGenie.exe 
	else
		cd $1
		sudo $MONO --debug --debugger-agent="address=10.0.1.10:10000,transport=dt_socket,server=y" HomeGenie.exe >/dev/null  2>&1 
	fi
	EXITCODE="$?"
	echo "Exit code: $EXITCODE"
done