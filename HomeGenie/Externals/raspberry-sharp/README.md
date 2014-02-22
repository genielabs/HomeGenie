Raspberry# IO
=============

See the **[Raspberry\# IO Wiki](raspberry-sharp-io/wiki)** for full documentation and samples.

Introduction
------------
Raspberry# IO is a .NET/Mono IO Library for Raspberry Pi. This project is an initiative of the [Raspberry#](http://www.raspberry-sharp.org) Community.

Current release is an early public release. Some features may not have been extensively tested.
Raspberry# IO currently supports GPIO input/output, and samples with SPI (for MCP3008 ADC or MCP4822 DAC), and HD44780 LCD display are provided.

Support for extended IO (such as support for PWM or I2C peripherals) is planned for future releases.

Programs using Raspberry# IO must be run with elevated privileges, for example the Test.Gpio.Chaser program included in solution:

    sudo mono Test.Gpio.Chaser.exe -loop

Features
--------

### Raspberry.IO.GeneralPurpose
Raspberry.IO.GeneralPurpose provides a convenient way to use Raspberry Pi GPIO pins, while using .NET concepts, syntax and case.
You can easily add a reference to it in your Visual Studio projects using the **[Raspberry.IO.GeneralPurpose Nuget](https://www.nuget.org/packages/Raspberry.IO.GeneralPurpose)**.

It currently support the following features:
+ Access to GPIO pins through memory (using [BCM2835 C Library](http://www.open.com.au/mikem/bcm2835/)) or file (native) drivers
+ Addressing through **processor pin number or connector pin number**
+ Giving custom name to pins for more readable code
+ Various Raspberry Pi revisions, for now **Raspberry B rev1 and rev2**, including rev2 P5 connector
+ Easy-of-use, declarative configuration of pins. Ability to revert the polarity (1/0) of pins; ability to **use an input pin as a switch button**
+ Firing of **events when pin status change** (input as well as output)
+ **High-level behaviors** for output pins, including *blink*, *pattern* and *chaser*
+ Controlled use of resources using a IDisposable component
+ Support sub-millisecond polling of input pins
+ Preliminary support for SPI through Raspberry.IO.SerialPeripheralInterface assembly
+ Preliminary support for I2C through Raspberry.IO.InterIntegratedCircuit assembly
+ Includes SPI samples for MCP3008 ADC and MCP4822 DAC, I2C sample for MCP23017 I/O expander, as well as samples for HD44780 LCD display and HC-SR04 distance detector
