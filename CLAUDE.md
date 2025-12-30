# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Development Commands

- `dotnet restore` - restore NuGet packages
- `dotnet build` - compile the solution
- `dotnet watch run` - run with hot reload (serves at https://localhost:5001)
- `dotnet publish -c Release` - produce deployable artifacts
- `dotnet format` - format code before committing

## Architecture Overview

This is an ASP.NET Core Razor Pages application targeting .NET 10, with route-based localization (English and German).

### Localization System

Routes are prefixed with culture codes (`/en/...`, `/de/...`). Root path `/` redirects to `/en`.

- `Localization/` - Custom JSON-based string localizer (`JsonStringLocalizerFactory`, `JsonStringLocalizer`)
- `ResourcesJson/Pages/` - Translation files follow page structure (e.g., `Index.json`, `Index.de.json`)
- Culture is extracted from route via `RouteDataRequestCultureProvider`

### Data and Services

- `Data/RAL/all-colors.json` - RAL color dataset (Classic, Design Plus, Effect categories)
- `Services/RalColorLoader.cs` - Loads and caches RAL colors, registered as singleton
- `Services/ColorClassifier.cs` - Color classification utilities (similar colors, root color detection)
- `Helpers/ColorMath.cs` - Consolidated color utilities (conversions, Delta E, color temperature, LRV, contrast)
- `Models/RalColor.cs` - RAL color model with category, brightness, localized names

### Pages Structure

- `Pages/` - Razor Pages with `.cshtml` views and `.cshtml.cs` PageModels
- `Pages/ral-colors/` - RAL color listing and details pages
- `Pages/Shared/` - Shared layouts and partials

### Key Patterns

- Services are registered via DI in `Program.cs`
- PageModels expose bound properties; keep Razor markup slim
- Localized content uses `IStringLocalizer` injection

## Dependencies

- `Colourful` - Color space conversion library
