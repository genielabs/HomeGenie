## IR and RF remotes

Even if a phone or tablet can be used to remote control *HomeGenie*, a RF/IR remote
is still a quick and comfortable way for controlling devices.

HomeGenie support X10 RF remotes and IR controller/remotes supported by [LIRC](http://www.lirc.org/)
or other IR gateways that can be found in the *Package Manager*.

See the video at the end of in the [Setup](#/setup) page for instruction on
installing additional drivers though the *Package Manager*.

The video below shows how to use an infrared remote to control lights and switches.

<div class="content-margin" align="center">
    <iframe self="size-medium" height="440" src="https://www.youtube.com/embed/f_uywgXmAwk?rel=0" frameborder="0" allowfullscreen></iframe>
</div>

#### Troubleshooting LIRC in Linux, Raspberry & co.

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
