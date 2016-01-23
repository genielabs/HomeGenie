# Restoring a backup file with HG v1.1-beta.513 and up

Starting from version **1.1-beta.513**, many HomeGenie automation programs have been moved to the **Package Repository**.

If you are restoring an old backup file you will need to **install and configure** missing automation programs from the **Package Manager**.

After installing the missing programs, make a new backup so that the installed package informations will be included in the new backup file.

When restoring a new backup file, HomeGenie will automatically download and install extra packages that were included in your backup.

Internet connection is required when extra packages are present in the backup file, otherwise package download will fail.

## List of packages moved to the Package Repository

### MIG Interfaces

- W800RF32 X10 RF Receiver
- Insteon PLM
- LIRC Infrared Control
- V4L Camera

### Devices and Things

- X10 FireCracker modules
- KNX device modules
- MiLight Control
- One-Wire Devices
- Serial Port IO Test

### Drivers

- Telldus Tellstick

### Interconnections

- MQTT Broker Service

### Messaging and Social

- Alcatel One Touch Y800Z
- Pushing Box

### Remote Control

- Global Cache IR

### Security

- Ping Me at Home
- Presence Simulator

### Single Board Computer

- CubieTruck GPIO
- **ALL** Raspberry Pi programs
- Weeco 4M Board GPIO

### Weather and Environment

- Earth Tools
- Open Weather

### Z-Wave

- Fibaro RGBW
- Tag Reader
- Z-Wave.Me Floor Thermostat


