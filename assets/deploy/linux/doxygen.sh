#!/bin/sh
set -e
command -v doxygen >/dev/null 2>&1 || { echo >&2 "Doxygen command is not installed. Type the command below to install it:\n    sudo apt-get install doxygen\nAborting."; exit 1; }
command -v python3 >/dev/null 2>&1 || { echo >&2 "Python 3 is required to generate AI API references."; exit 1; }
cd "$(dirname "$0")/Doxy"
# clean up old generated files
rm -rf homegenie_api
# generate API docs
doxygen
# Refresh the optional scripting references from the same XML as the API website.
python3 generate-ai-api-reference.py --xml-dir homegenie_api/xml --output-dir ../../../build/all/data/ai/skills/homegenie-core/references
# create a bundle out of it
tar czvf homegenie_api.tgz homegenie_api

