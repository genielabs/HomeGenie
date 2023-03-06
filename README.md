![example workflow](https://github.com/genielabs/HomeGenie/actions/workflows/msbuild.yml/badge.svg)

# HomeGenie

**The programmable automation intelligence**

- Integrated drivers for X10, ZigBee, Z-Wave, GPIO, SPI, I2C
- Works 100% off-line without relaying on any external cloud service
- Scripting engine with fluent API programmable using `csharp`, `javascript`, `python`
- Scheduler service supporting extended cron expressions (with variables, conditions and logical operators)
- Voice and text message based control
- Localization
- Dashboards, groups, configuration backup, packages repository and much more! 
- Brand new UI that works both on desktop or mobile screens, suitable also to be used with fixed control panels
- New Visual Programming workspace to create scenarios in an intuitive way, no coding skills required
- Customizable client configurations that can be saved and recalled quickly from the UI preferences 


## Documentation

https://genielabs.github.io/HomeGenie

## Install instructions

Download the `.zip` archive corresponding to the hosting operating system:

**Windows**
- `homegenie_*_win-x64.zip`

**Mac**
- `homegenie_*_osx-x64.zip`

**Linux**
- `homegenie_*_linux-x64.zip`

**Raspbian 32bit**
- `homegenie_*_linux-arm.zip`

**Raspbian 64bit**
- `homegenie_*_linux-arm64.zip`

https://github.com/genielabs/HomeGenie/releases

Unzip the archive file and run the `./HomeGenie` command to start the service:

```shell
cd homegenie
./HomeGenie
```

HomeGenie UI is now accessible with a web browser:

`http://<server_address>:<port>/`

where `<server_address>` is the name or ip of the host where *HomeGenie* was installed and `<port>`
is the port on which is listening for web requests (default port is `8080`).

---

Depending on the hosting operating system, it might be required to run additional steps
in order to allow the service to access the **Serial port**, **USB** devices and **GPIO** hardware.

### Common additional steps

To enable **audio playack** and **voice synthesis**:
```shell
# Audio playback utilities
sudo apt-get install alsa-utils lame
# Embedded speech syntesys engine
sudo apt-get install libttspico-utils
```

To use **X10 Home Automation** hardware:
```shell
sudo apt-get install libusb-1.0-0 libusb-1.0-0-dev
```

To grant access to the **Serial port** and/or **GPIO** to the current user:
```shell
sudo gpasswd -a $USER dialout
sudo gpasswd -a $USER gpio
```

It's recommended that a dedicated user is added for running a service, but as a last resort, if you are still getting `access denied`
error while trying to access connected hardware, run `./HomeGenie` service using `sudo`:
```
sudo ./HomeGenie
```


## Development

### Contributing

Read the [CONTRIBUTING.md](https://github.com/genielabs/HomeGenie/blob/master/CONTRIBUTING.md) file
for information about contributing to this repository.

### Repository structure

The main solution file is `HomeGenie.sln` that is located in the repository root.

- `assets/build`
common (all) and OS specific static files that are copied after the build process
- `assets/build/all/app`
  This folder contains HomeGenie user interface (YOT)
- `assets/deploy`
OS specific files required for bundling and deploying the app redistributable
- `src/HomeGenie`
main application project files (**net6** and **net472**)
- `src/HomeGenie.Tests`
project implementing Unit Tests
- `src/SupportLibraries` 
support and utility libraries used by HomeGenie
- `src/WindowsService`
Windows specific solution for deploying HomeGenie as a Windows service (deprecated)


### Integrated Development Environment

[JetBrains Rider](https://www.jetbrains.com/rider/) is the official IDE employed for developing this project.

![JetBrains Logo](https://raw.githubusercontent.com/genielabs/HomeGenie/master/assets/github/jetbrains.svg)
![JetBrains Rider Logo](https://raw.githubusercontent.com/genielabs/HomeGenie/master/assets/github/rider-logo.svg)

### Related projects

- https://github.com/genielabs/homegenie-packages
- https://github.com/genielabs/mig-service-dotnet
- https://play.google.com/store/apps/details?id=com.glabs.homegenieplus
- https://github.com/genielabs/HomeGenie-Android-ClientLib
- https://github.com/genielabs/HomeGenie-Android
- https://github.com/genielabs/HomeGenie-WindowsPhone
- https://github.com/genielabs/homegenie-mini
- https://github.com/genielabs/yot

------

### Disclaimer

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

