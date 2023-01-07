# net472

dotnet publish --configuration Release --framework net472
rm -rf ./src/HomeGenie/bin/Release/net472/net472
rm -rf ./src/HomeGenie/bin/Release/net472/publish
cp ./src/HomeGenie/release_info.xml ./src/HomeGenie/bin/Release/net472/
sed -i "s/%runtime%/net472/" ./src/HomeGenie/bin/Release/net472/release_info.xml
ls -la ./src/HomeGenie/bin/Release/net472/

# net6.0

runtimes=( linux-arm linux-arm64 linux-x64 osx-x64 win-x64 )
for runtime in "${runtimes[@]}"
do
	echo "Building for '${runtime}' runtime."
  dotnet publish --configuration Release --framework net6.0 --runtime ${runtime} --self-contained
  rm -rf ./src/HomeGenie/bin/Release/net6.0/${runtime}/publish
  cp ./src/HomeGenie/release_info.xml ./src/HomeGenie/bin/Release/net6.0/${runtime}/
  sed -i "s/%runtime%/${runtime}/" ./src/HomeGenie/bin/Release/net6.0/${runtime}/release_info.xml
  ls -la ./src/HomeGenie/bin/Release/net6.0/${runtime}
done
