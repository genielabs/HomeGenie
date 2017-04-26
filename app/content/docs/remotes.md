## RF and InfraRed Remote Control

### Controlling HomeGenie with a remote control

It's true, today many people own a mobile device such as smartphones and tablets.
But what we will really find in almost every house is the old good infrared remote control.
These are easy to use for everyone and are very cheap. So why not using one of these
for programming and controlling our home automation system?

[IMAGE ADDING IR REMOTE IN CONFIG]

As shown in the picture above, we can setup the IR remote control in HomeGenie from the
Configure->Settings section by enabling the LIRC Infrared Remotes interface and by adding
our remote model to the configuration. 

Once configured, HomeGenie will be able to receive events from the IR remote.
So, now, we can create automation scripts triggered by remote control buttons,
simply by adding a new Wizard Script and by capturing the code of the desired
IR button as shown in the example below.

<div class="content-margin" align="center">
    <iframe self="size-medium" height="440" src="https://www.youtube.com/embed/f_uywgXmAwk?rel=0" frameborder="0" allowfullscreen></iframe>
</div>

Note that the InfraRed control feature is currently available only for Linux and other
standard LIRC capable systems. It is currently not supported in Windows.

X10 RF control is indeed available for all OS.

#### LIRC configuration

To enable IR remote control edit the /etc/lirc/hardware.conf file: 

```bash
sudo nano /etc/lirc/hardware.conf 
```

Paste the following text into it:

```bash
########################################################
# /etc/lirc/hardware.conf
#
# Arguments which will be used when launching lircd
LIRCD_ARGS="--uinput"

# Don't start lircmd even if there seems to be a good config file
# START_LIRCMD=false

# Don't start irexec, even if a good config file seems to exist.
# START_IREXEC=false

# Try to load appropriate kernel modules
LOAD_MODULES=true

# Run "lircd --driver=help" for a list of supported drivers.
DRIVER="default"
# usually /dev/lirc0 is the correct setting for systems using udev
DEVICE="/dev/lirc0"
MODULES="mceusb"

# Default configuration files for your hardware if any
LIRCD_CONF=""
LIRCMD_CONF=""
########################################################
```

Change MODULES="mceusb" line with your IR transceiver module name. 

##### Raspberry Pi GPIO IR

If you are using Raspberry Pi GPIO IR hardware, change the above mentioned line
to MODULES="lirc_rpi". For more information about IR GPIO module for Raspberry Pi
see [<i class="material-icons">link</i> Raspberry Pi lirc_rpi](http://aron.ws/projects/lirc_rpi/). 

##### CubieBoard and Banana Pi built-in IR receiver

If you want to use built-in Banana Pi / CubieBoard IR receiver, see the  
following [<i class="material-icons">link</i> instructions](http://linux-sunxi.org/LIRC#Using_LIRC_with_Cubieboard2_.28A20_SoC.29). 

Restart LIRC 

```bash
sudo /etc/init.d/lirc restart 
```

Happy remote controlling! =)
