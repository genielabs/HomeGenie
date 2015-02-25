# HomeGenie

**Project info and documentation**:
http://homegenie.it

![ScreenShot](https://github.com/genielabs/HomeGenie/raw/screenshots/screenshots/homegenie_eden_01.jpg "HomeGenie Dashboard 1")


![ScreenShot](https://github.com/genielabs/HomeGenie/raw/screenshots/screenshots/homegenie_01.jpg "HomeGenie Dashboard 2")

## Supported IDEs

- **MonoDevelop / Xamarin Studio**
- **Microsoft Visual Studio**


## Building, debugging and packaging HomeGenie

**Linux**
- Open the *HomeGenie_Linux/HomeGenie_Linux.sln* solution file
- Prepare base files by building the *BaseFiles/Linux* project
- Build/Debug the main *HomeGenie* project
- To bundle a debian setup package, build the *Packger* project (even if this appear to be disabled, it will lauch a script in a terminal window)

**Windows**
- Open the *HomeGenie_Windows/HomeGenie_VS10.sln* solution file
- Prepare base files by building the *BaseFiles/Windows* project
- Build/Run/Debug the main *HomeGenie* project
- To bundle a setup package, open and run the InnoSetup file located in the *HomeGenie_Windows/Packager* folder.

**Mac**
- Open the *HomeGenie_Mac/HomeGenie_Mac.sln* solution file
- Build/Debug the main *HomeGenie* project
- no setup packaging currently supported for Mac

To debug mono remotely on RPi using Xamarin Studio on Mac follow these steps:

1. In *startup_debug.sh* replace the IP on lines 13 and 16 from *10.0.1.10* to the actual IP address of your Raspberry Pi.
2. Start HG from the console using *startup_debug.sh*

    ```bash
    $ cd /usr/local/bin/homegenie
    $ ./startup_debug.sh
    ```

3. Start Xamarin Studio from Terminal with one environment variable defined:

    ```bash
    $ export MONODEVELOP_SDB_TEST=1
    $ cd /Applications
    $ open Xamarin\ Studio.app/
    ```

4. Open the project. To start debugging connect to mono debugger that runs on RPi using *Run* > *Run With* > *Custom Command Mono Soft Debugger* menu option. It will open a prompt window where you should enter the IP address of RPi in the *IP* field and 10000 in the *Port* field.
5. Click *Connect* and debug will start.


## Precompiled packages and install instructions

**Windows, Mac, Linux and Raspberry Pi**:
http://homegenie.it/download.php

## Related projects

- http://github.com/genielabs/HomeGenie-Android
- http://github.com/genielabs/HomeGenie-WindowsPhone

===============

### License Information

[READ LICENSE FILE](LICENSE)

### Disclaimer

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
