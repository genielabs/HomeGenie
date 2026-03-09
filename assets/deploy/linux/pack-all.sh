cd ./src/HomeGenie/bin/Release/
mkdir artifacts

# net10.0

runtimes=( linux-arm linux-arm64 linux-x64 osx-x64 win-x64 )
cd net10.0

for runtime in "${runtimes[@]}"
do
  echo "Creating release bundle for '${runtime}' runtime."

  mv ${runtime} homegenie

  # Generate startup script based on runtime
  case $runtime in
    win-*)
      # Windows
      SCRIPT_NAME="start.bat"
      cat << 'EOF' > $SCRIPT_NAME
@echo off
title HomeGenie Server
echo ==========================================
echo   Starting HomeGenie...
echo ==========================================
cd /d "%~dp0homegenie"
HomeGenie.exe
pause
EOF
      ;;

    osx-*)
      # Mac
      SCRIPT_NAME="start.command"
      cat << 'EOF' > $SCRIPT_NAME
#!/bin/bash
echo "=========================================="
echo "  Starting HomeGenie for Mac..."
echo "=========================================="
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
cd "$DIR/homegenie"
chmod +x ./HomeGenie
./HomeGenie
EOF
      chmod +x $SCRIPT_NAME
      ;;

    linux-*)
      # Linux
      SCRIPT_NAME="start.sh"
      cat << 'EOF' > $SCRIPT_NAME
#!/bin/bash
echo "=========================================="
echo "  Starting HomeGenie for Linux..."
echo "=========================================="
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
cd "$DIR/homegenie"
chmod +x ./HomeGenie
./HomeGenie
EOF
      chmod +x $SCRIPT_NAME
      ;;
  esac

  zip homegenie_${VERSION_NUMBER}_${runtime}.zip -r homegenie $SCRIPT_NAME
  mv homegenie_${VERSION_NUMBER}_${runtime}.zip ../artifacts/
  rm -rf homegenie $SCRIPT_NAME

done
