# nMQTT, an MQTT v3 Implementation

Welcome to the nMQTT .Net MQTT Library. You can find a [quickstart here](./getting-started.md) 

## Build Server
There is a [TeamCity build server](http://teamcity.bareknucklecode.com:8111/) which provides CI 
builds. The build currently runs on Ubuntu/Mono(v2) both because it's simpler and cheaper to maintain.
This also has the side benefit of finding any filename case bugs which can cause issues with
builds under mono on linux.

## Getting Source
The source for nMQTT is on [github](https://github.com/markallanson/nmqtt).

If you don't have Git on your machine, visit [git](http://git-scm.com) and grab a copy for your platform, 
or simply install the github client for [Windows](http://windows.github.com) or 
[Mac](http://mac.github.com).

You can get a local copy by issuing the following command from your terminal.

`git clone git://github.com/markallanson/nmqtt.git nmqtt`

This command will clone a copy of the source to a new directory, "nmqtt" under your current working 
directory. 

## Build Source
### On Windows

Install the .Net SDK and run msbuild against your chosen sln file.
or
Run Visual Studio, load your chosen sln file.

### On Mac OS X or Linux

1. Install the latest [Xamarin Studio](http://xamarin.com/studio)
2. Load your chosen sln file into Xamarin Studio and build.


## Running Unit Tests

The unit tests for nMqtt are written using the [xUnit framework](http://www.codeplex.com/xunit). 

The xUnit framework test runners run on both the .Net Framework and Mono platform (To run on mono 
prefix the xunit runner executables with `mono`, then issue your command line as normal (ie. the
path to the nMQTTTests.dll assembly)

## Pull Requests

Please issue pull requests if you have additions or changes or bug fixes you would like to see
in the main branch.
