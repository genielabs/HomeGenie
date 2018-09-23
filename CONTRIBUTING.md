# Contributing

## How to contribute to HomeGenie

#### **Did you find a bug?**

* **Ensure the bug was not already reported** by searching on GitHub under [Issues](https://github.com/genielabs/HomeGenie/issues).

* If you're unable to find an open issue addressing the problem, [open a new one](https://github.com/genielabs/HomeGenie/issues/new).
Be sure to include a **title and clear description**, as much relevant information as possible, and a **code sample**
or an **executable test case** demonstrating the expected behavior that is not occurring.

#### **Did you write a patch that fixes a bug?**

* Open a new GitHub pull request with the patch.

* Ensure the PR description clearly describes the problem and solution.
Include the relevant issue number if applicable.

#### **Did you fix whitespace, format code, or make a purely cosmetic patch?**

Changes that are cosmetic in nature and do not add anything substantial to the stability, functionality,
or testability of HomeGenie will generally not be accepted unless discussed via the [issue tracker](https://github.com/genielabs/HomeGenie/issues).

#### **Do you intend to add a new feature or change an existing one?**

File a new *[enhancement issue](https://github.com/genielabs/HomeGenie/issues/new?labels=enhancement)*.

#### **Do you have questions about the source code?**

File a new *[question issue](https://github.com/genielabs/HomeGenie/issues/new?labels=question)*.

#### **Do you want to contribute to the HomeGenie documentation?**

The documentation site source code is available from the **[gh-pages](https://github.com/genielabs/HomeGenie/tree/gh-pages)** branch, you can
contribute following same rules used for the main repo.

#### **Coding styles and conventions**

This project follows *Microsoft .Net* [coding conventions](https://docs.microsoft.com/dotnet/csharp/programming-guide/inside-a-program/coding-conventions) and [naming guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/capitalization-conventions).

#### **Getting started with HomeGenie source code**

Clone [**HomeGenie repository**](https://github.com/genielabs/HomeGenie).

##### Building and debugging

You can use one of the following IDEs:

- **MonoDevelop / Xamarin Studio**
- **Microsoft Visual Studio**
- **JetBrains Rider**

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

##### Releasing a new version

To release a new version push a new tag using the format:

`v<major>.<minor>-{stable|rc|beta|alpha}.<build>`

example: `v1.1-stable.527`

When a new tag is submitted the CI system will build the project, run tests and package assets (.deb, .tgz, .exe distribution files). Assets will be also uploaded to the new release on GitHub repository.


#### Join HomeGenie team!

HomeGenie is a volunteer effort. We encourage you to pitch in and join the team!

Thanks! :heart:

