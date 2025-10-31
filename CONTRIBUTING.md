# Contributing to HomeGenie

First off, thank you for considering contributing to HomeGenie! We welcome any help, from reporting a bug to submitting a new feature. Every contribution is valuable and helps make HomeGenie better for everyone.

This document provides a set of guidelines to help you contribute to the project in a way that is efficient and effective for everyone involved.

## Code of Conduct

This project and everyone participating in it is governed by our [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code. Please report unacceptable behavior.

## How Can I Contribute?

There are many ways to contribute to HomeGenie, and not all of them involve writing code.

### 🐛 Reporting Bugs

If you think you've found a bug, please ensure it hasn't already been reported by searching the existing [Issues](https://github.com/genielabs/HomeGenie/issues).

If you can't find an existing issue, please [**open a new one**](https://github.com/genielabs/HomeGenie/issues/new/choose). When reporting a bug, please include:
-   A clear and descriptive title.
-   A detailed description of the problem, including what you expected to happen.
-   Steps to reproduce the bug.
-   Information about your environment (e.g., OS, HomeGenie version, relevant hardware).
-   Screenshots or log file excerpts if applicable.

### ✨ Suggesting Enhancements or New Features

We love new ideas! If you have a suggestion for a new feature or an enhancement to an existing one, please start by [**opening an "enhancement" issue**](https://github.com/genielabs/HomeGenie/issues/new?labels=enhancement). This allows us to discuss the idea before any code is written.

Please provide as much detail as possible, including the problem you're trying to solve and why you think the feature would be valuable.

### 📝 Contributing to the Documentation

Good documentation is crucial. If you find a typo, want to improve a guide, or write a new one, you can contribute to our documentation repository:
-   [**homegenie.it Website Repository**](https://github.com/genielabs/homegenie.it)

### 💻 Submitting Code Changes (Pull Requests)

Ready to submit a code contribution? That's fantastic!

1.  **Fork the repository** and create your branch from `master`.
2.  **Make your changes.** Please ensure your code adheres to the project's coding style.
3.  **Submit a Pull Request (PR)** to the `master` branch.
4.  In your PR description, clearly explain the problem you are solving and the changes you have made. Reference any relevant issues (e.g., `Fixes #123`).

**A note on cosmetic changes:** Pull requests that only fix whitespace, reformat code, or make other purely cosmetic changes will generally not be accepted unless they are part of a larger, functional change.

## 🎨 Coding Style and Conventions

HomeGenie follows the official **Microsoft .NET Coding Conventions** and **Naming Guidelines**.
-   [C# Coding Conventions](https://docs.microsoft.com/dotnet/csharp/programming-guide/inside-a-program/coding-conventions)
-   [Naming Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/capitalization-conventions)

We use an `.editorconfig` file in the repository to help enforce these styles automatically. Please ensure your editor is configured to use it.

## 🚀 Releasing a New Version (for Maintainers)

To create a new release, a new tag must be pushed to the repository using the Semantic Versioning format:

`v<major>.<minor>.<patch>[-<prerelease>.<build>]`

Examples:
-   Stable Release: `v2.0.0`
-   Pre-release: `v2.0.0-rc.1`

When a new tag is pushed, the CI/CD workflow will automatically build the project, run tests, package the assets for all target platforms, and publish a new release on GitHub.

---

Thank you for your interest in making HomeGenie better! ❤️
