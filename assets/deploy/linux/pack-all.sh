cd ./src/HomeGenie/bin/Release/
mkdir artifacts

# net10.0

runtimes=( linux-arm linux-arm64 linux-x64 linux-x64-cuda12 linux-x64-vulkan osx-x64 win-x64 win-x64-cuda12 win-x64-vulkan )
cd out

for runtime in "${runtimes[@]}"
do
  echo "Creating release bundle for '${runtime}' runtime."

  mv ${runtime} homegenie

  # Generate the startup script based on the runtime, handling the Restart logic (Exit Code 1)
  case $runtime in
    win-*)
      # Windows
      SCRIPT_NAME="start.bat"
      cat << 'EOF' > $SCRIPT_NAME
@echo off
title HomeGenie Server
cd /d "%~dp0homegenie"

:run
echo ==========================================
echo   Starting HomeGenie...
echo ==========================================
HomeGenie.exe --start-browser

:: Check if the exit code is 1 (Restart requested)
if %ERRORLEVEL% equ 1 (
    echo.
    echo [System] Restart requested...
    echo.
    goto run
)

:: If exited with 0 or any other code, terminate
if %ERRORLEVEL% neq 0 (
    echo.
    echo [System] HomeGenie stopped with error code %ERRORLEVEL%
    pause
)
EOF
      ;;

    osx-*|linux-*)
      # Mac / Linux
      if [[ $runtime == osx-* ]]; then
          SCRIPT_NAME="start.command"
      else
          SCRIPT_NAME="start.sh"
      fi

      cat << 'EOF' > $SCRIPT_NAME
#!/bin/bash
# Get the absolute path of the script
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
cd "$DIR/homegenie"

# Ensure the executable has correct permissions
chmod +x ./HomeGenie

while true; do
    echo "=========================================="
    echo "  Starting HomeGenie..."
    echo "=========================================="

    ./HomeGenie --start-browser
    EXIT_CODE=$?

    # If the exit code is 1, continue the loop (restart)
    if [ $EXIT_CODE -eq 1 ]; then
        echo ""
        echo "[System] Restart requested. Re-launching..."
        echo ""
        sleep 1
    else
        # Exit the loop for any other code
        echo "[System] HomeGenie stopped (Exit Code: $EXIT_CODE)"
        exit $EXIT_CODE
    fi
done
EOF
      chmod +x $SCRIPT_NAME
      ;;
  esac

  zip homegenie_${VERSION_NUMBER}_${runtime}.zip -r homegenie $SCRIPT_NAME
  mv homegenie_${VERSION_NUMBER}_${runtime}.zip ../artifacts/
  rm -rf homegenie $SCRIPT_NAME

done
