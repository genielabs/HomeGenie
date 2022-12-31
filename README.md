![example workflow](https://github.com/genielabs/HomeGenie/actions/workflows/msbuild.yml/badge.svg)

# HomeGenie

**Embeddable, Smart Home server for Windows, Mac and Linux.**

- Integrated drivers for X10, Z-Wave and ZigBee<sup>\*</sup> <small>(<sup>\*</sup>coming soon)</small>, GPIO, SPI, I2C
- Works 100% off-line without relaying on any external cloud service
- Powerful scripting engine with fluent API supporting `csharp` and `javascript`
- Powerful scheduler supporting extended cron expressions (with variables, conditions and logical operators)
- Voice and text message based control
- Localization
- Dashboards, groups, configuration backup, packages repository and much more! 

## Documentation

https://genielabs.github.io/HomeGenie

## Precompiled packages and install instructions

**Windows, Mac, Linux**

https://genielabs.github.io/HomeGenie/download.html

## Development

### Contributing

Read the [CONTRIBUTING.md](https://github.com/genielabs/HomeGenie/blob/master/CONTRIBUTING.md) file
for information about contributing to this repository.

### Repository structure

The main solution file is `HomeGenie.sln` that is located in the repository root.

- `assets/build`
common (all) and OS specific static files that are copied after the build process
- `assets/build/all/app`
  This folder contains the new HomeGenie user interface (YOT) implemented as a Angular 11 PWA (currently in very early development stage).
- `assets/deploy`
OS specific files required for bundling and deploying the app redistributable
- `src/HomeGenie`
main application project files implemented as **net6** app
- `src/HomeGenie.Net461`
**.net 4.6.1** project files sharing the same source code base from `src/HomeGenie` 
- `src/HomeGenie.Tests`
project implementing Unit Tests
- `src/SupportLibraries` 
support and utility libraries used by HomeGenie
- `src/WindowsService`
Windows specific solution for deploying HomeGenie as a Windows service

### Building from command line

In order to build HomeGenie `msbuild` version >= 15 is required.

From the repository root folder enter the command
```
msbuild /p:Configuration=Debug HomeGenie.sln
```
This will generate both the `net6` and the `net461` version of HomeGenie app.

**net6** ->
`src/HomeGenie/bin/Degbu/net6` 

**net461** -> `src/HomeGenie.Net461/bin/Debug`


### Running

*net6*
```
cd src/HomeGenie/bin/Debug/net
./HomeGenie # (or 'dotnet HomeGenie.dll')
```

*net461*
```
cd src/HomeGenie.Net461/bin/Debug
./HomeGenie.exe # (or 'mono HomeGene.exe')
```

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

