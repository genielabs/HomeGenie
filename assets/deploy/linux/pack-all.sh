# net472

cd ./src/HomeGenie/bin/Release/
mkdir artifacts
mv net472 homegenie
zip homegenie_${VERSION_NUMBER}_net472.zip -r homegenie
mv homegenie_${VERSION_NUMBER}_net472.zip ./artifacts/

# net6.0

runtimes=( linux-arm )
cd net6.0
for runtime in "${runtimes[@]}"
do
	echo "Creating release bundle for '${runtime}' runtime."
  mv ${runtime} homegenie
  zip homegenie_${VERSION_NUMBER}_${runtime}.zip -r homegenie
  mv homegenie_${VERSION_NUMBER}_${runtime}.zip ../artifacts/
  rm -rf homegenie
done
cd ..

# net9.0

runtimes=(linux-arm64 linux-x64 osx-x64 win-x64 )
cd net9.0
for runtime in "${runtimes[@]}"
do
	echo "Creating release bundle for '${runtime}' runtime."
  mv ${runtime} homegenie
  zip homegenie_${VERSION_NUMBER}_${runtime}.zip -r homegenie
  mv homegenie_${VERSION_NUMBER}_${runtime}.zip ../artifacts/
  rm -rf homegenie
done
