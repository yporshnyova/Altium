# Altium — PCB Manufacturing Order

A WPF desktop application that simulates creating a PCB (Printed Circuit Board) manufacturing order.
It will provide a single window with three tabs: **Preferences**, **Quote**, and **Order**.

> **Current status: Phase 4.1 — sequential Preferences → Quote → Order workflow, Preferences with board quantity, quantity-aware price estimate, the manufacturing Quote with top and side board previews, English/Russian localization and light/dark themes are implemented. Order is a placeholder.**

## Assumptions

The assignment leaves these open; they are documented choices pending review:

- **Postcode format**: Polish `NN-NNN` (for example `55-220`), ASCII digits only, no surrounding whitespace
  (`Models/PolishPostcode.cs`). The typed value is kept in the shared preferences even while invalid.
- **Pricing**: a simplified linear model, not a manufacturer quotation (`Services/PcbPricingService.cs`). The catalog
  `basePriceUsd` ($149.00) is the price of one board. `EstimatedUnitPrice = BaseUnitPrice × material priceMultiplier`,
  rounded to cents (midpoint away from zero); `MaterialSurchargePerUnit = EstimatedUnitPrice − BaseUnitPrice`;
  `EstimatedTotalPrice = EstimatedUnitPrice × Quantity`. There are no bulk discounts, setup fees, shipping or taxes.
- **Quantity**: number of boards, default 10, entered on Preferences. Any positive whole number written with ASCII
  digits is accepted (leading zeros allowed); there is no business upper limit, only the technical `int` range
  (`Models/BoardQuantity.cs`). Invalid text stays in the field with an error and is not stored; the summary total shows
  a dash until it is corrected.
- **Production time**: the selected material's catalog `productionDays`, shown as the estimated production time; it
  does not depend on the quantity.
- **Project name**: defaults to `PCB-001`; required (not blank) and at most 50 characters, with no character restrictions.
- **Notes**: optional, at most 300 characters; a line break typed in the field counts as two characters (CR LF).
- **Postcode input**: the field accepts only digits and inserts the hyphen itself (`Views/Behaviors/PostcodeMask.cs`);
  pasted text must be 5 digits or an `NN-NNN` fragment, otherwise it is rejected unchanged.
- **Save & Continue**: validates every field and switches to the Quote tab; it keeps the preferences for the current
  session only (nothing is written to disk). On failure it stays on Preferences and focuses the first invalid field.
- **Reset to Defaults**: restores `PCB-001`, an empty postcode and notes, and the first catalog entry of each option;
  it asks for confirmation only when something differs from those defaults.
- **Workflow**: Preferences → Quote → Order are sequential steps (`ViewModels/WorkflowNavigationState.cs`). Quote and
  Order start locked; a valid Save & Continue completes Preferences and unlocks Quote. Unlocked steps stay unlocked
  when going back, and nothing is cleared. A step is available only while every earlier step is valid: if Preferences
  becomes invalid, the unlocked Quote tab is disabled at once and re-enabled as soon as the input is corrected (no new
  Save & Continue needed). Order stays locked until the Quote step can be completed.
- **Board specification**: the Quote shows a fixed sample board (`boardSpecification` in `Data/pcb-catalog.json`):
  100 × 80 mm, 2 copper layers, 1 oz copper, HASL lead-free finish, white silkscreen. These values are placeholders,
  not taken from the assignment; they are read-only and do not affect the price. The quantity and the price are the
  ones from Preferences.
- **Parameter table**: Quote lists its read-only parameters in groups (Dimensions, Layers, Finish, Board preferences)
  from `QuoteViewModel.ParameterGroups`, rendered by nested `ItemsControl`s with data templates. The structure is a
  fixed read-only list; each row reads its value from the specification or the shared preferences.
- **Board preview**: a top view (`Views/PcbPreviewView.xaml`) whose layers are frozen WPF geometries generated from the
  board size by `Views/Graphics/PcbArtworkBuilder.cs` (rounded outline, mounting holes, traces, a generated QFP
  footprint, header, passives and vias; the layout is decorative). A `Viewbox` scales it uniformly and centres it, and
  its height follows the panel width up to a cap. The outline is filled with the selected solder mask colour. A schematic side view
  (`Views/PcbSideView.xaml`): mask / core / mask, with the profile height proportional to the selected thickness
  (the thickest catalog option fills the reserved band) and the exact thickness beside it. Core colours are an
  illustrative convention in `Views/Converters/SubstrateBrushConverter.cs` (FR-4 amber, FR-4 High Tg deeper amber,
  Rogers off-white, unknown materials neutral grey), not the physical appearance or a real layer stack-up.
- **Continue to Order**: completes the Quote step and opens Order only while Preferences is valid; Back to Preferences
  keeps all data and progress.

## Localization and Themes

- **Languages**: English (default) and Russian, switched at runtime from the header. Strings live in
  `Localization/Strings.resx` and `Strings.ru.resx`; `Strings.Designer.cs` is the strongly typed accessor
  (regenerated by Visual Studio from the `.resx`; a unit test keeps it in sync). XAML uses `{loc:Text Key}`,
  `{loc:Format Key, Path=…}` and `{loc:CatalogName …}` (`Views/Markup`), which follow language changes.
  Only the UI language changes: prices are always formatted as US dollars and the postcode rule is unaffected.
- **Themes**: light and dark palettes in `Resources/Themes/Light.xaml` and `Dark.xaml` (the same semantic keys), used
  through `DynamicResource` and swapped at runtime by `Services/ThemeService.cs`.
- **Icons**: Material icons from `MahApps.Metro.IconPacks.Material` (only this pack and its `Core` dependency).
- **Saved UI preferences**: language and theme only, in `%LOCALAPPDATA%\Altium\PcbManufacturing\ui-settings.json`;
  a missing or invalid file falls back to English and the dark theme (the default). Manufacturing preferences are
  not saved.

## Technology Stack

| Component            | Technology                                  |
|----------------------|---------------------------------------------|
| Language             | C#                                          |
| Runtime              | .NET 8 (`net8.0-windows`)                   |
| Desktop UI           | WPF                                         |
| MVVM                 | CommunityToolkit.Mvvm                       |
| Dependency Injection | Microsoft.Extensions.DependencyInjection    |
| Testing              | xUnit v3                                    |
| Icons                | MahApps.Metro.IconPacks.Material 6.2.1      |

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
│       ├── Localization/               # Strings.resx (EN), Strings.ru.resx (RU), Localizer
│       ├── Models/
│       ├── Settings/                   # Saved UI preferences (language, theme)
│       ├── ViewModels/
│       ├── Views/                      # Views, dialogs, behaviors, localization markup extensions
│       ├── Services/
│       └── Resources/
│           ├── Styles/                 # Shared styles and control templates
│           ├── Templates/              # View-ViewModel mappings
│           └── Themes/                 # Light and dark palettes
└── tests/
    └── Altium.PcbManufacturing.Tests/  # xUnit tests
```
