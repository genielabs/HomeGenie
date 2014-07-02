#!/bin/sh

_cwd="$PWD"

echo "Enter HomeGenie revision (eg. r387):"
read hg_revision_number

echo "Enter target folder (eg. /home/myuser):"
read hg_target_folder

if [ -d "${hg_target_folder}" ]
then

	hg_target_folder="${hg_target_folder}/homegenie-beta_1.00.${hg_revision_number}_all"

	mkdir -p "$hg_target_folder/usr/local/bin/homegenie"

	echo "\n- Copying files to '$hg_target_folder'..."

	cp -r ../../HomeGenie/bin/Debug/* "$hg_target_folder/usr/local/bin/homegenie/"
	cp -r ./LinuxFiles/v4l "$hg_target_folder/usr/local/bin/homegenie/"
	cp -r ./LinuxFiles/homegenie_stats.db "$hg_target_folder/usr/local/bin/homegenie/"
	cp -r ./LinuxFiles/System.Data.SQLite.dll "$hg_target_folder/usr/local/bin/homegenie/"
	rm -rf "$hg_target_folder/usr/local/bin/homegenie/x64"
	rm -rf "$hg_target_folder/usr/local/bin/homegenie/x86"
	rm -rf "$hg_target_folder/usr/local/bin/homegenie/log"

	echo "\n- Generating md5sums in DEBIAN folder..."
	cd $hg_target_folder
	find "./usr/local/bin/homegenie/" -type f ! -regex '.*.hg.*' ! -regex '.*?debian-binary.*' ! -regex '.*?DEBIAN.*' -printf "\"usr/local/bin/homegenie/%P\" " | xargs md5sum > "$_cwd/LinuxFiles/DEBIAN/md5sums"
	hg_installed_size=`du -s ./usr | cut -f1`
	echo "  installed size: $hg_installed_size"
	cd "$_cwd"

	echo "- Copying updated DEBIAN folder..."
	cp -r ./LinuxFiles/DEBIAN "$hg_target_folder/"
	cp -r ./LinuxFiles/DEBIAN "$hg_target_folder/usr/local/bin/homegenie/"
	sed -i s/-rxyz/-$hg_revision_number/g "$hg_target_folder/DEBIAN/control"
	sed -i s/-rxyz/-$hg_revision_number/g "$hg_target_folder/usr/local/bin/homegenie/DEBIAN/control"
	sed -i s/-sxyz/$hg_installed_size/g "$hg_target_folder/DEBIAN/control"
	sed -i s/-sxyz/$hg_installed_size/g "$hg_target_folder/usr/local/bin/homegenie/DEBIAN/control"

	echo "- Fixing permissions..."

	chmod -R 755 "$hg_target_folder/DEBIAN"
	chmod +x "$hg_target_folder/usr/local/bin/homegenie/startup.sh"

	echo "\n- Building deb file...\n"

	dpkg-deb --build "$hg_target_folder"

	echo "\n... done!\n"

	while true; do
		read -p "Remove temporary folder '$hg_target_folder'? " yn
		case $yn in
	    	[Yy]* ) rm -rf "$hg_target_folder"; break;;
	    	[Nn]* ) exit;;
	    	* ) echo "Please answer yes or no.";;
		esac
	done
    
else

    echo "Error: Directory '$hg_target_folder' does not exists."

fi