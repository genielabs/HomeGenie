#!/bin/sh

homegenie_version="${1:-$(echo $TRAVIS_TAG | cut -d"v" -f 2)}"

if [ -z "$homegenie_version" ]
then
    echo "Skipping: no version string passed and no env '\$TRAVIS_TAG' is set."
    exit 1
fi
set -x -e

script_path="$( cd "$(dirname "$0")" ; pwd -P )"
source_folder="$( cd "${script_path}/../../HomeGenie/bin/Debug" ; pwd -P )"
target_folder="${script_path}/Output"

echo "Packaging HomeGenie $homegenie_version"
echo "Source: $source_folder"
echo "Destination: $target_folder"

_cwd="$PWD"
mkdir -p $target_folder

if [ -d "${target_folder}" ]
then

	base_folder=$target_folder
	target_folder="${target_folder}/homegenie_${homegenie_version}_all"

	mkdir -p "$target_folder/usr/local/bin/homegenie"
	mkdir -p "$target_folder/etc/lirc/"

	echo "\n- Copying files to '$target_folder'..."

	cp ./lirc_options.conf "$target_folder/etc/lirc/"
	cp -r $source_folder/* "$target_folder/usr/local/bin/homegenie/"
	rm -rf "$target_folder/usr/local/bin/homegenie/log"

	echo "\n- Generating md5sums in DEBIAN folder..."
	cd "$target_folder"
	find "./usr/local/bin/homegenie/" -type f ! -regex '.*.hg.*' ! -regex '.*?debian-binary.*' ! -regex '.*?DEBIAN.*' -printf "\"usr/local/bin/homegenie/%P\" " | xargs md5sum > "$script_path/DEBIAN/md5sums"
	hg_installed_size=`du -s ./usr | cut -f1`
	echo "  installed size: $hg_installed_size"
	cd "$script_path"

	echo "- Copying updated DEBIAN folder..."
	cp -r ./DEBIAN "$target_folder/"
	cp -r ./DEBIAN "$target_folder/usr/local/bin/homegenie/"
	sed -i s/%version%/$homegenie_version/g "$target_folder/DEBIAN/control"
	sed -i s/%version%/$homegenie_version/g "$target_folder/usr/local/bin/homegenie/DEBIAN/control"
	sed -i s/%installed_size%/$hg_installed_size/g "$target_folder/DEBIAN/control"
	sed -i s/%installed_size%/$hg_installed_size/g "$target_folder/usr/local/bin/homegenie/DEBIAN/control"

	echo "- Fixing permissions..."

	chmod -R 755 "$target_folder/DEBIAN"
#	chmod +x "$target_folder/usr/local/bin/homegenie/startup.sh"

	echo "\n- Building deb file...\n"

	dpkg-deb --build "$target_folder"

	echo "\n... done!\n"

    cd "$target_folder/usr/local/bin/"
    tar -czvf "${base_folder}/homegenie_${homegenie_version}.tgz" homegenie
    rm -rf "$target_folder"; break;
	cd "$_cwd"

    ls -la "${base_folder}"
    
else

    echo "Error: Directory '$target_folder' does not exists."
    exit 1

fi

