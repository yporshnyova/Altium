# PCB Manufacturing

A Windows WPF application for the Altium take-home assignment. One window guides the user through **Preferences → Quote → Order**. It simulates an order; no order is sent to a backend or saved.

## Technology Stack

| Component | Technology |
|-----------|------------|
| Language | C# |
| Runtime / UI | .NET 8 (`net8.0-windows`), WPF; Windows only |
| Build SDK | .NET 10 |
| MVVM | CommunityToolkit.Mvvm 8.4.2 |
| Dependency injection | Microsoft.Extensions.DependencyInjection 10.0.13 |
| Testing | xUnit v3 / Microsoft.Testing.Platform |
| Vector icons | MahApps.Metro.IconPacks.Material 6.2.1 |

## Build and run

On Windows, install the **.NET 10 SDK** and **.NET 8 Desktop Runtime**. The application targets .NET 8; the .NET 10 SDK supports the Microsoft.Testing.Platform runner selected in `global.json`.

From the solution directory:

```shell
dotnet restore Altium.sln
dotnet build Altium.sln -c Release
dotnet run --project src/Altium.PcbManufacturing -c Release
```

Alternatively, open `Altium.sln` in a compatible Visual Studio installation with the **.NET desktop development** workload and start `Altium.PcbManufacturing`.

Run the tests:

```shell
dotnet test --solution Altium.sln -c Release
```

## Features

- **Preferences:** material, solder mask color, board thickness, project name, postcode, quantity, and notes. Validation provides visible field errors; material and quantity update the price estimate.
- **Quote:** grouped board parameters and vector top/side PCB previews that follow the selected color and thickness.
- **Order:** validates input, displays a summary confirmation dialog, and shows completion after confirmation. **New Order** resets the wizard; Back preserves entered data.

## Localization and Themes

- **Languages:** English and Russian, switched at runtime from the window header. Strings are stored in `Localization/Strings.resx` and `Strings.ru.resx`, with strongly typed access through `Strings.Designer.cs`.
- **Localized bindings:** `{loc:Text ...}`, `{loc:Format ...}`, and `{loc:CatalogName ...}` in `Views/Markup` update text when the language changes. Prices remain in US dollars; the postcode format does not change.
- **Themes:** light and dark palettes in `Resources/Themes`, applied by `ThemeService` through WPF dynamic resources.
- **Persistence:** only language and theme are saved to `%LOCALAPPDATA%\Altium\PcbManufacturing\ui-settings.json`. Order data remains in memory for the wizard session.

## Architecture

The application uses MVVM with separate `Views`, `ViewModels`, `Models`, and `Services`. Code-behind and attached behaviors handle view-only concerns such as focus and input presentation.

- **Models:** catalog entries, board specifications, and price results. Preset data is loaded from `Data/pcb-catalog.json`.
- **Services:** catalog loading, pricing, theme changes, and confirmation dialogs.
- **ViewModels:** `MainWindowViewModel` hosts `OrderWizardViewModel`, which owns navigation, completion, and the shared footer's commands and labels.
- **Wizard state:** the wizard creates its own observable `ManufacturingOrderDraft` internally and passes it to each step factory through `Create(draft)`. The draft lives in `ViewModels` and is not registered in DI. Factories supply the other step dependencies; the wizard retains each step instance across navigation.
- **Bindings and validation:** views bind directly to shared draft values where appropriate. Preferences keeps validated editing input, including invalid quantity text; only a valid number updates the draft quantity. Pricing rules remain in `PcbPricingService`.
- **Views and resources:** data templates map step view models to views. Shared styles, theme resources, converters, and localization markup extensions provide consistent presentation.

`ServiceRegistration.cs` is the composition root. The current single-window application uses one wizard for the window's lifetime. Step view models do not depend on one another.

Tests cover pricing, validation, draft notifications, factory/session ownership, wizard transitions, confirmation, reset, localization, and WPF binding behavior.

## Solution Structure

```text
Altium.sln
global.json                            # Test runner selection
src/Altium.PcbManufacturing/
├── App.xaml(.cs)                       # Application startup and resources
├── ServiceRegistration.cs             # DI composition root
├── Data/                              # Predefined PCB catalog (JSON)
├── Models/                            # Catalog, board specification, price results
├── Services/                          # Catalog loading, pricing, dialogs, themes
├── ViewModels/                        # Window, wizard, steps, and observable draft
│   └── Factories/                     # Step creation with an explicit draft
├── Views/                             # Main window and step views, each with its own *.Styles.xaml
│   ├── Behaviors/                     # Focus and input helpers
│   ├── Converters/                    # Display conversions
│   ├── Dialogs/                       # Confirmation dialog
│   ├── Graphics/                      # PCB preview geometry
│   └── Markup/                        # Localization extensions
├── Validation/                        # Input rules and catalog validation
├── Localization/                      # English/Russian resources and localizer
├── Settings/                          # Language/theme persistence
├── Formatting/                        # Currency formatting
├── Exceptions/                        # Catalog loading errors
├── Icons/                             # Application icon
└── Resources/
    ├── Styles/                        # Shared layout, typography, buttons and control templates
    ├── Templates/                     # View-model-to-view mappings and shared catalog item templates
    └── Themes/                        # Light and dark palettes
tests/Altium.PcbManufacturing.Tests/    # Unit and WPF binding tests
```

## Assignment assumptions

- Postcodes use the Polish `NN-NNN` format with ASCII digits.
- Unit price is the catalog base price multiplied by the material multiplier, rounded to cents; total is unit price × quantity. Shipping, taxes, setup fees, and bulk discounts are excluded.
- Production time comes from the selected material. Solder mask color and thickness affect the preview, not pricing.
- The catalog contains an illustrative fixed board specification; the preview is decorative rather than imported PCB artwork.
- Quantity defaults to 10 and must be a positive integer. Project names are required and limited to 50 characters; optional notes are limited to 300 characters.

## Screenshots

Screenshots of the working UI are a separate assignment deliverable and are not yet included in this repository.
