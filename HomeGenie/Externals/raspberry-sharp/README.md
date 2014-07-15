Raspberry# IO
=============

See the **[Raspberry\# IO Wiki](https://github.com/raspberry-sharp/raspberry-sharp-io/wiki)** for full documentation and samples.

Introduction
------------
Raspberry# IO is a .NET/Mono IO Library for Raspberry Pi. This project is an initiative of the [Raspberry#](http://www.raspberry-sharp.org) Community.

Current release is an early public release. Some features may not have been extensively tested.
Raspberry# IO currently supports low- and high-level GPIO input/output, support for SPI and I2C peripherals.

Samples for SPI (for MCP3008 ADC or MCP4822 DAC), I2C (for MCP23017 I/O Expander), HD44780 LCD display and HC-SR04 distance sensor are provided.

Support for extended I/O (such as SDI, or PWM for motor control) is planned for future releases.

Programs using Raspberry# IO must be run with elevated privileges, for example the Test.Gpio.Chaser program included in solution:

    sudo mono Test.Gpio.Chaser.exe -loop

Features
--------

### Raspberry.IO.GeneralPurpose
Raspberry.IO.GeneralPurpose provides a convenient way to use Raspberry Pi GPIO pins, while using .NET concepts, syntax and case.
You can easily add a reference to it in your Visual Studio projects using the **[Raspberry.IO.GeneralPurpose Nuget](https://www.nuget.org/packages/Raspberry.IO.GeneralPurpose)**.

It currently support the following features:

Low-level:

+ Access to GPIO pins through in 3 flavors: basic (using files), through memory, and full (memory with support for edge detection through "pseudo-interrupt"). By default, full driver is used.
+ Addressing through **processor pin number or connector pin number**
+ Pin assignment of various Raspberry Pi revisions (as of 2013-09, **Raspberry Pi model B rev1 and rev2 as well as Raspberry Pi model A**, including rev2 P5 connector)
+ Controlled use of resources using a IDisposable component and ability to use edge detection instead of polling
+ Support sub-millisecond polling of input pins

High-level:

+ Giving custom name to pins for more readable code
+ Easy-to-use, declarative configuration of pins. Ability to revert the polarity (1/0) of pins; ability to **use an input pin as a switch button**
+ Firing of **events when pin status change** (input as well as output), using polling
+ **High-level behaviors** for output pins, including *blink*, *pattern* and *chaser*

### Raspberry.IO.SerialPeripheralInterface

+ Preliminary support for SPI through Raspberry.IO.SerialPeripheralInterface assembly
+ Includes SPI samples for MCP3008 ADC and MCP4822 DAC
+ Includes support for Linux's kernel SPI module driver spi-bcm2708 (/dev/spidev0.0)

### Raspberry.IO.InterIntegratedCircuit

+ Preliminary support for I2C through Raspberry.IO.InterIntegratedCircuit assembly
+ Includes I2C sample for MCP23017 I/O expander 
	
### Raspberry.IO.Components

+ Preliminary support for various components through Raspberry.IO.Components assembly
+ Includes samples for
    - HD44780 LCD display
    - HC-SR04 distance detector
    - Pca9685 PWM LED Controller (as used in the [Adafruit 16-Channel 12-bit PWM/Servo Driver](http://www.adafruit.com/products/815))
    - TLC59711 PWM LED Controller (as used in the [Adafruit 12-Channel 16-bit PWM LED Driver](http://www.adafruit.com/products/1455))

Parts of Raspberry# IO are inspired by [BCM2835 C Library](http://www.airspayce.com/mikem/bcm2835/) and Gordon Henderson's [WiringPi](http://wiringpi.com/).
