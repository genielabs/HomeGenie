#!/bin/sh
command -v doxygen >/dev/null 2>&1 || { echo >&2 "Doxygen command is not installed. Type the command below to install it:\n    sudo apt-get install doxygen\nAborting."; exit 1; }
cd Doxy
# clean up old generated files
rm -rf homegenie_api
# generate API docs
doxygen
# create a bundle out of it
tar czvf homegenie_api.tgz homegenie_api

