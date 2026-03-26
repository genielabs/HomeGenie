# net10.0

rm -rf ./src/HomeGenie/bin/Release/out
mkdir -p ./src/HomeGenie/bin/Release/out

runtimes=( linux-arm linux-arm64 linux-x64 osx-x64 win-x64 )
for runtime in "${runtimes[@]}"
do
	echo "Building for '${runtime}' runtime (CPU)."
	# CPU
  dotnet publish --configuration Release --framework net10.0 --runtime ${runtime} --self-contained -m:1
  cp ./src/HomeGenie/release_info.xml ./src/HomeGenie/bin/Release/net10.0/${runtime}/publish/
  sed -i "s/%runtime%/${runtime}/" ./src/HomeGenie/bin/Release/net10.0/${runtime}/publish/release_info.xml
  mv ./src/HomeGenie/bin/Release/net10.0/${runtime}/publish ./src/HomeGenie/bin/Release/out/${runtime}
done

runtimes=( linux-x64 win-x64 )
for runtime in "${runtimes[@]}"
do
	echo "Building for '${runtime}' runtime (GPU)."
  # GPU - CUDA 12
  dotnet publish --configuration Cuda12 --framework net10.0 --runtime ${runtime} --self-contained -m:1
  cp ./src/HomeGenie/release_info.xml ./src/HomeGenie/bin/Cuda12/net10.0/${runtime}/publish/
  sed -i "s/%runtime%/${runtime}/" ./src/HomeGenie/bin/Cuda12/net10.0/${runtime}/publish/release_info.xml
  mv ./src/HomeGenie/bin/Cuda12/net10.0/${runtime}/publish ./src/HomeGenie/bin/Release/out/${runtime}-cuda12
  # GPU - Vulkan (+ DirectX)
  dotnet publish --configuration Vulkan --framework net10.0 --runtime ${runtime} --self-contained -m:1
  cp ./src/HomeGenie/release_info.xml ./src/HomeGenie/bin/Vulkan/net10.0/${runtime}/publish/
  sed -i "s/%runtime%/${runtime}/" ./src/HomeGenie/bin/Vulkan/net10.0/${runtime}/publish/release_info.xml
  mv ./src/HomeGenie/bin/Vulkan/net10.0/${runtime}/publish ./src/HomeGenie/bin/Release/out/${runtime}-vulkan
done
