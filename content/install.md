## Download HomeGenie Server

<div align="center" class="content-margin">Current release is <strong>v1.1-525</strong></div>

<div self="size-small center" layout="rows top-spread">

<a layout="row center-left" href="https://github.com/genielabs/HomeGenie/releases/download/v1.1-beta.525/homegenie-beta_1.1.r525_all.deb" title="Download HomeGenie for Debian Linux" style="height:120px;margin:8px" class="mdl-shadow--8dp mdl-button mdl-js-button mdl-js-ripple-effect">
<img src="images/logos/luxicon.png" alt="" style="margin-right:10px" align="left" width="82" align="left">
<div layout="column center-spread">
<h3>Linux .deb</h3>
<span>Ubuntu / Debian derivatives</span>
</div>
</a>

<a layout="row center-left" href="https://github.com/genielabs/HomeGenie/releases/download/v1.1-beta.525/homegenie_1_1_beta_r525.tgz" title="Download HomeGenie platform agnostic archive" style="height:120px;margin:8px" class="mdl-shadow--8dp mdl-button mdl-js-button mdl-js-ripple-effect">
<img src="images/logos/macicon.png" alt="" style="margin-right:10px" align="left" width="82" align="left">
<div layout="column center-spread">
<h3>Platform agnostic .tgz</h3>
<span>OSX / Other Mono/.NET compatibles</span>
</div>
</a>

<a layout="row center-left" href="https://github.com/genielabs/HomeGenie/releases/download/v1.1-beta.525/HomeGenie_1_1_beta_r525.exe" title="Download HomeGenie for Windows" style="height:120px;margin:8px" class="mdl-shadow--8dp mdl-button mdl-js-button mdl-js-ripple-effect">
<img src="images/logos/winicon.png" alt="" style="margin-right:10px" width="82" align="left">
<div layout="column center-spread">
<h3>Windows Installer</h3>
<span>Vista / Win7 / Win8</span>
</div>
</a>

</div>

<br clear="all"/>


## Install instructions


### Windows

Download *HomeGenie Windows Installer* and run it. Once installation process is completed, *HomeGenie* UI will be opened.
The UI can be also opened by right clicking on the tray icon.


### Ubuntu, Raspbian and others Debian derivatives

Download _.deb package_ file and install it by double clicking on it, or by using gdebi command line tool:

```bash
wget https://github.com/genielabs/HomeGenie/releases/download/v1.1-beta.525/homegenie-beta_1.1.r525_all.deb
sudo apt-get update
sudo apt-get install gdebi-core
sudo gdebi homegenie-beta_1.1.r525_all.deb
```

HomeGenie will be installed in the _/usr/local/bin/homegenie_ foder.
Once installed, *HomeGenie* UI can be opened by entering the following URL in your web browser:
`http://<linux_box_address>/`
(where `<linux_box_address>` is the name or ip of the host where homegenie is installed).

#### Optional packages

```bash
# SSL client support
sudo apt-get install ca-certificates-mono
# Embedded speech synthesys engine
sudo apt-get install libttspico-utils
# Arduino™ programming from *HG* program editor
sudo apt-get install arduino-mk empty-expect
```

**Note** *HomeGenie requires mono runtime version 3.2 or later.*


### Mac OS X and other UNIX systems

HomeGenie can also be installed on other systems (eg. Mac OS X and other UNIX based systems) by following the procedure described below. 

#### Installing prerequisites

 Enter the following command from terminal (`apt-get` is shown here, eventually replace it with whatever package manager
 is suitable for your operating system):

```bash
sudo apt-get install mono-runtime libmono-corlib2.0-cil libmono-system-web4.0-cil libmono-system-numerics4.0-cil libmono-system-serviceprocess4.0-cil libmono-system-data4.0-cil libmono-system-core4.0-cil libmono-system-servicemodel4.0a-cil libmono-windowsbase4.0-cil libmono-system-runtime-serialization-formatters-soap4.0-cil libmono-system-runtime-serialization4.0-cil libmono-system-xml-linq4.0-cil mono-dmcs
```

#### Optional dependencies

In order to activate some features, optional dependencies may be required to install.

```bash
# Audio playback utilities
sudo apt-get install alsa-utils lame
# Embedded speech syntesys engine
sudo apt-get install libttspico-utils
# SSL client support
sudo apt-get install ca-certificates-mono
# LIRC Infrared inteface
sudo apt-get install lirc liblircclient-dev
# Video4Linux camera
sudo apt-get install libv4l-0
# X10 CM15 Home Automation interface
sudo apt-get install libusb-1.0-0 libusb-1.0-0-dev
# Arduino™ programming from *HG* program editor
sudo apt-get install arduino-mk empty-expect
```

#### Downloading and uncompressing tgz archive file

Enter the following command terminal

```bash
wget https://github.com/genielabs/HomeGenie/releases/download/v1.1-beta.525/homegenie_1_1_beta_r525.tgz
tar xzvf homegenie_1_1_beta_r525.tgz
```

#### Running HomeGenie

After uncompressing, the *homegenie* folder will be created. Enter the following commands from terminal to start HomeGenie:

```bash
cd homegenie
./startup.sh
```

You can now start using HomeGenie opening the following URL in your web browser:
`http://<server_address>/` (where `<server_address>` is the name or ip of the host where HomeGenie was installed).
