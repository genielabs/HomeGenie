# net472

dotnet publish --configuration Release --framework net472
rm -rf ./src/HomeGenie/bin/Release/net472/net472
rm -rf ./src/HomeGenie/bin/Release/net472/publish
cp ./src/HomeGenie/release_info.xml ./src/HomeGenie/bin/Release/net472/
sed -i "s/%runtime%/net472/" ./src/HomeGenie/bin/Release/net472/release_info.xml
ls -la ./src/HomeGenie/bin/Release/net472/


# net6.0
# this is for 32bit raspbian OS that not support `GLIBC_2.34' as of 2025-03-06
# ---> ./HomeGenie: /lib/arm-linux-gnueabihf/libc.so.6: version `GLIBC_2.34' not found (required by ./HomeGenie)

runtimes=( linux-arm )
for runtime in "${runtimes[@]}"
do
	echo "Building for '${runtime}' runtime."
  dotnet publish --configuration Release --framework net6.0 --runtime ${runtime} --self-contained
  rm -rf ./src/HomeGenie/bin/Release/net6.0/${runtime}/publish
  cp ./src/HomeGenie/release_info.xml ./src/HomeGenie/bin/Release/net6.0/${runtime}/
  sed -i "s/%runtime%/${runtime}/" ./src/HomeGenie/bin/Release/net6.0/${runtime}/release_info.xml
  ls -la ./src/HomeGenie/bin/Release/net6.0/${runtime}
done


# net9.0

runtimes=( linux-arm64 linux-x64 osx-x64 win-x64 )
for runtime in "${runtimes[@]}"
do
	echo "Building for '${runtime}' runtime."
  dotnet publish --configuration Release --framework net9.0 --runtime ${runtime} --self-contained
  rm -rf ./src/HomeGenie/bin/Release/net9.0/${runtime}/publish
  cp ./src/HomeGenie/release_info.xml ./src/HomeGenie/bin/Release/net9.0/${runtime}/
  sed -i "s/%runtime%/${runtime}/" ./src/HomeGenie/bin/Release/net9.0/${runtime}/release_info.xml
  ls -la ./src/HomeGenie/bin/Release/net9.0/${runtime}
done
