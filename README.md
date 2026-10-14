# Altium — PCB Manufacturing Order

A WPF desktop application that simulates creating a PCB (Printed Circuit Board) manufacturing order.
It will provide a single window with three tabs: **Preferences**, **Quote**, and **Order**.

> **Current status: Phase 1 — Infrastructure only. Application functionality is not implemented yet.**

## Technology Stack

| Component            | Technology                                  |
|----------------------|---------------------------------------------|
| Language             | C#                                          |
| Runtime              | .NET 8 (`net8.0-windows`)                   |
| Desktop UI           | WPF                                         |
| MVVM                 | CommunityToolkit.Mvvm                       |
| Dependency Injection | Microsoft.Extensions.DependencyInjection    |
| Testing              | xUnit v3                                    |

## Prerequisites

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later, **and** the .NET 8 Desktop Runtime
- Optional: Visual Studio 2022 (17.8+) with the *.NET desktop development* workload

## Build

```shell
dotnet restore
dotnet build -c Debug
dotnet build -c Release
```

## Run

```shell
dotnet run --project src/Altium.PcbManufacturing
```

Or open `Altium.sln` in Visual Studio and start `Altium.PcbManufacturing`.

## Test

```shell
dotnet test
```

`global.json` opts `dotnet test` into Microsoft.Testing.Platform, which the .NET 10 SDK requires
for xUnit v3. Older SDKs and Visual Studio Test Explorer use the VSTest adapter instead.

## CI/CD

GitHub Actions workflows live in `.github/workflows/`.

### Continuous integration — `ci.yml`

- Runs on every push to `main` and on pull requests targeting `main`.
- Windows runner: restore → Release build → all xUnit tests.
- The run fails if compilation or any test fails. It never deploys.

### Release — `release.yml`

Manual only: **Actions → Release → Run workflow**, choose the branch, and enter a version such as `v1.0.0`
(`v1.1.0-rc.1` creates a pre-release).

1. Validates the version format, that the source ref is a branch, and that no tag or release with that version exists.
2. Restores, builds and tests in Release; stops on any failure.
3. Publishes a self-contained, untrimmed `win-x64` build stamped with the version.
4. Creates the tag on the exact commit that was built, and a GitHub Release with
   `Altium.PcbManufacturing-<version>-win-x64.zip` attached.

The ZIP needs no .NET installation: extract it and run `Altium.PcbManufacturing.exe`.

To reproduce the release package locally:

```shell
dotnet publish src/Altium.PcbManufacturing -c Release -r win-x64 --self-contained true -p:PublishTrimmed=false -o publish
```

## Solution Structure

```text
Altium/
├── Altium.sln
├── global.json                         # dotnet test runner selection
├── .github/workflows/                  # CI and release pipelines
├── src/
│   └── Altium.PcbManufacturing/        # WPF application
│       ├── App.xaml(.cs)               # Startup and shutdown; builds the DI container
│       ├── ServiceRegistration.cs  # Composition root (service registrations)
│       ├── MainWindow.xaml(.cs)
│       ├── Models/
│       ├── ViewModels/
│       ├── Views/
│       ├── Services/
│       └── Resources/
│           ├── Styles/
│           ├── Templates/
│           └── Themes/
└── tests/
    └── Altium.PcbManufacturing.Tests/  # xUnit tests
```
