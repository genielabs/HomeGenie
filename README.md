<p align="center">
  <img src="https://genielabs.github.io/HomeGenie/images/homegenie_logo.svg" alt="HomeGenie Logo" width="150"/>
</p>
<h1 align="center">HomeGenie</h1>
<p align="center">
  <strong>The Programmable Automation Intelligence</strong>
  <br />
  <a href="https://homegenie.it"><strong>www.homegenie.it</strong></a>
</p>

<p align="center">
  <a href="https://github.com/genielabs/HomeGenie/actions/workflows/msbuild.yml"><img src="https://github.com/genielabs/HomeGenie/actions/workflows/msbuild.yml/badge.svg" alt="Build Status"></a>
  <a href="https://github.com/genielabs/HomeGenie/releases/latest"><img src="https://img.shields.io/github/v/release/genielabs/HomeGenie" alt="Latest Release"></a>
  <a href="https://github.com/genielabs/HomeGenie/blob/master/LICENSE"><img src="https://img.shields.io/github/license/genielabs/HomeGenie.svg" alt="License"></a>
</p>

HomeGenie is a versatile, open-source home automation platform written in .NET. Designed for tinkerers, developers, and DIY enthusiasts, it provides a powerful, server-centric solution that runs on a wide range of hardware, from a Raspberry Pi to a dedicated home server.

With its robust scripting engine and extensive hardware support, HomeGenie offers the freedom to create complex automation scenarios and integrate a vast array of devices, all while ensuring your data and logic remain securely within your own network.

![HomeGenie Dashboard](https://genielabs.github.io/HomeGenie/images/homegenie_dashboard_02.jpg)

## ✨ Key Features

-   **Wide Protocol Support:** Integrated drivers for X10, Z-Wave, ZigBee, GPIO, SPI, I2C, and more.
-   **100% Offline & Private:** Works completely offline without relying on any external cloud service. Your data, your rules, your home.
-   **Powerful Scripting Engine:** Automate anything with a fluent API programmable in **C#**, **JavaScript**, and **Python**.
-   **Advanced Scheduler:** Supports extended cron expressions with variables, conditions, and logical operators for time-based automations.
-   **Customizable Dashboard & Widgets:** A modern, responsive UI with a powerful built-in editor to create and customize your own widgets using HTML, JavaScript, and CSS.
-   **Visual Programming:** Create complex scenarios in an intuitive way with the Visual Programming workspace—no coding skills required.
-   **Voice Control:** Integrated support for voice and text-message-based commands.
-   **Extensible:** Features groups, configuration backup, a package repository, and much more.

##  Documentation

For detailed guides, API references, and tutorials, visit the official documentation website:
**https://genielabs.github.io/HomeGenie**

## 💾 Installation

You can find the latest release assets on the [**GitHub Releases**](https://github.com/genielabs/HomeGenie/releases) page.

Download the `.zip` archive corresponding to your operating system and architecture:

| Platform              | Archive Name                       |
|-----------------------|------------------------------------|
| Windows (x64)         | `homegenie_*_win-x64.zip`          |
| macOS (x64)           | `homegenie_*_osx-x64.zip`          |
| Linux (x64)           | `homegenie_*_linux-x64.zip`        |
| Raspberry Pi (32-bit) | `homegenie_*_linux-arm.zip`        |
| Raspberry Pi (64-bit) | `homegenie_*_linux-arm64.zip`      |
| .NET Framework 4.7.2  | `homegenie_*_net472.zip`           |

After downloading, unzip the archive. A new `homegenie` folder will be created.

### Running from a Terminal

Navigate to the `homegenie` directory and execute the `HomeGenie` command:

```shell
cd homegenie
./HomeGenie
```
To stop the application, press `CTRL + C`.

### Running as a System Service

HomeGenie can be run directly from a terminal for easy setup and testing,
or installed as a system service for continuous, unattended operation.

#### Linux (systemd)

1.  (Recommended) Create a dedicated user for the service and move the application files:
    ```shell
    # Create a new system user named 'homegenie' with its home directory
    sudo useradd -r -m -s /bin/false homegenie
    
    # Copy the application files into the new home directory
    # Assumes you are in the directory where you extracted the zip
    sudo cp -ar ./homegenie/* /home/homegenie/
    
    # Ensure the ownership of all files is correct
    sudo chown -R homegenie:homegenie /home/homegenie
    ```

2.  Create a service definition file at `/etc/systemd/system/homegenie.service`:
    ```ini
    [Unit]
    Description=HomeGenie Automation Server
    After=network.target
    
    [Service]
    Type=notify
    User=homegenie
    Group=homegenie
    WorkingDirectory=/home/homegenie/
    ExecStart=/home/homegenie/HomeGenie
    Restart=on-failure
    RestartSec=15s
    
    [Install]
    WantedBy=multi-user.target
    ```

3.  Reload the systemd daemon, then start and enable the service:
    ```shell
    sudo systemctl daemon-reload
    sudo systemctl start homegenie.service
    sudo systemctl enable homegenie.service
    ```
    You can check the service status with `sudo systemctl status homegenie.service`.

#### Windows

HomeGenie can be installed as a Windows Service using the built-in commands. Open a Command Prompt **as an Administrator**, navigate to the `homegenie` directory, and run:
```shell
HomeGenie.exe --service install
HomeGenie.exe --service start
```
For more options, see the [official .NET documentation](https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service).

### Accessing the UI

Once running, the HomeGenie user interface can be accessed from any web browser at: `http://<server_ip>:<port>/`

-   `<server_ip>` is the IP address of the machine running HomeGenie.
-   The default `<port>` is **80**, or the first available port starting from **8080**.

You can find the exact address and port used by checking the `serviceaddress.txt` file in the `homegenie` application folder.

## 🛠️ Post-Installation (Linux)

To grant HomeGenie access to hardware like serial ports, USB devices, and GPIO, you may need to run additional commands.

#### Audio & Voice Synthesis

```shell
# For audio playback
sudo apt install alsa-utils lame
# For embedded text-to-speech
sudo apt install libttspico-utils
```

#### Serial Port & GPIO Access

Add the `homegenie` user to the `dialout` and `gpio` groups:
```shell
sudo usermod -a -G dialout homegenie
sudo usermod -a -G gpio homegenie
```

#### X10 (CM15/CM19) USB Controller

1.  Install the required library:
    ```shell
    sudo apt install libusb-1.0-0
    ```
2.  Create a udev rule to grant access. Create the file `/etc/udev/rules.d/98-homegenie.rules` with the following content:
    ```
    # CM15 and CM19 X10 USB controllers
    SUBSYSTEM=="usb", ATTRS{idVendor}=="0bc7", ATTRS{idProduct}=="0001", MODE="0660", GROUP="dialout"
    SUBSYSTEM=="usb", ATTRS{idVendor}=="0bc7", ATTRS{idProduct}=="0002", MODE="0660", GROUP="dialout"
    ```
3.  Reload the udev rules and reconnect the device:
    ```shell
    sudo udevadm control --reload-rules && sudo udevadm trigger
    ```

## 💻 Development

### Contributing

Contributions are welcome! Please read the [**CONTRIBUTING.md**](https://github.com/genielabs/HomeGenie/blob/master/CONTRIBUTING.md) file for guidelines.

### Repository Structure

The main solution file is `HomeGenie.sln` located in the repository root.
-   `src/HomeGenie`: The main application project, multi-targeted for `.NET Framework 4.7.2` and modern `.NET (6.0+)`.
-   `src/HomeGenie.Tests`: Unit and integration tests.
-   `src/SupportLibraries`: Utility libraries used by HomeGenie.
-   `assets/`: Contains build assets, UI source code, and deployment scripts.

### Related Projects

- https://github.com/genielabs/mig-service-dotnet
- https://github.com/genielabs/homegenie-mini
- https://play.google.com/store/apps/details?id=com.glabs.homegenieplus
- https://github.com/zuixjs/zuix

---

### Disclaimer

This software is provided "as is", without warranty of any kind. See the [LICENSE](LICENSE) file for more details.
