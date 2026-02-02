cd ./src/HomeGenie/bin/Release/
mkdir artifacts

# net10.0

runtimes=( linux-arm linux-arm64 linux-x64 osx-x64 win-x64 )
cd net10.0
for runtime in "${runtimes[@]}"
do
	echo "Creating release bundle for '${runtime}' runtime."
  mv ${runtime} homegenie
  zip homegenie_${VERSION_NUMBER}_${runtime}.zip -r homegenie
  mv homegenie_${VERSION_NUMBER}_${runtime}.zip ../artifacts/
  rm -rf homegenie
done
