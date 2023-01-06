#!/bin/sh

homegenie_version="${1:-$(echo $RELEASE_VERSION | cut -d"v" -f 2)}"

if [ -z "$homegenie_version" ]
then
	echo "Skipping: no version string passed and no env '\$RELEASE_VERSION' is set."
	exit 1
fi
set -x -e

hg_user="homegenie"
script_path="$( cd "$(dirname "$0")" ; pwd -P )"
source_folder="$( cd "${script_path}/../../../src/HomeGenie/bin/Release/homegenie" ; pwd -P )"
target_folder="${script_path}/Output"
deploy_folder="home"

echo "Packaging HomeGenie $homegenie_version"
echo "Source: $source_folder"
echo "Destination: $target_folder"
echo "Deploy folder: $deploy_folder"

_cwd="$PWD"
mkdir -p $target_folder

if [ -d "${target_folder}" ]
then

	base_folder=$target_folder
	target_folder="${target_folder}/homegenie_${homegenie_version}_all"

	mkdir -p "$target_folder/$deploy_folder/homegenie"

	echo "\n- Copying files to '$target_folder'..."

	cp -r $source_folder/* "$target_folder/$deploy_folder/homegenie/"
	rm -rf "$target_folder/$deploy_folder/homegenie/log"

	echo "\n- Generating md5sums in DEBIAN folder..."
	cd "$target_folder"
	find "./$deploy_folder/homegenie/" -type f ! -regex '.*.hg.*' ! -regex '.*?debian-binary.*' ! -regex '.*?DEBIAN.*' -printf "\"$deploy_folder/homegenie/%P\" " | xargs md5sum > "$script_path/DEBIAN/md5sums"
	hg_installed_size=`du -s "./$deploy_folder/homegenie/" | cut -f1`
	echo "  installed size: $hg_installed_size"
	cd "$script_path"

	echo "- Copying updated DEBIAN folder..."
	cp -r ./DEBIAN "$target_folder/"
	cp -r ./DEBIAN "$target_folder/$deploy_folder/homegenie/"
	sed -i s/%version%/$homegenie_version/g "$target_folder/DEBIAN/control"
	sed -i s/%version%/$homegenie_version/g "$target_folder/$deploy_folder/homegenie/DEBIAN/control"
	sed -i s/%installed_size%/$hg_installed_size/g "$target_folder/DEBIAN/control"
	sed -i s/%installed_size%/$hg_installed_size/g "$target_folder/$deploy_folder/homegenie/DEBIAN/control"

	echo "- Fixing permissions..."

	chmod -R 755 "$target_folder/DEBIAN"

	echo "\n- Building deb file...\n"

	dpkg-deb --build "$target_folder"

	echo "\n... done!\n"

	cd "$target_folder/$deploy_folder/"
	tar -czvf "${base_folder}/homegenie_${homegenie_version}_update.tgz" homegenie
	rm -rf "$target_folder"; break;
	cd "$_cwd"

 	ls -la "${base_folder}"

else

	echo "Error: Directory '$target_folder' does not exists."
	exit 1

fi
