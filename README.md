# HomeGenie

**Project info and documentation**:
http://homegenie.it


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
