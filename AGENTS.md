# Repository Guidelines

## Project Structure & Module Organization
- `Program.cs` wires ASP.NET Core services and middleware; keep host configuration changes localized there.
- `Pages/` holds Razor Pages and PageModels. Use `Pages/Shared/` for layouts/partials so they can be referenced by multiple routes.
- `wwwroot/` serves static assets (CSS, JS, images). Give bundles clear subfolders, e.g., `wwwroot/css/site.css`.
- Configuration lives in `appsettings.json` with an environment override in `appsettings.Development.json`. Never check in real secrets.

## Build, Test, and Development Commands
- `dotnet restore` ensures NuGet packages match `protabula_com.csproj`.
- `dotnet build` validates the solution compiles on your platform.
- `dotnet watch run` hot-reloads the Razor Pages site at `https://localhost:5001` for daily work.
- `dotnet publish -c Release` produces deployable artifacts in `bin/Release/net8.0/publish`.

## Coding Style & Naming Conventions
- Follow the default .NET coding conventions: 4-space indentation, PascalCase for classes/PageModels, camelCase for locals and parameters.
- Keep Razor markup slim; move logic into the backing `*.cshtml.cs` class and expose bound properties.
- Prefer Dependency Injection over static helpers. Register services through `builder.Services`.
- Run `dotnet format` before committing to align spacing, ordering, and `using` directives.

## Testing Guidelines
- Tests are not yet present; create future suites under `tests/` mirroring the page structure (e.g., `tests/Pages/IndexTests.cs`).
- Use xUnit + `Microsoft.AspNetCore.Mvc.Testing` for page handler tests; name classes `<Page>Tests` and methods `<Scenario>_<ExpectedBehavior>`.
- Execute `dotnet test` locally and ensure new features include happy-path and guard-rail coverage.

## Commit & Pull Request Guidelines
- Write imperative, single-line commit subjects (e.g., `Add hero layout for landing page`).
- Reference GitHub issues with `Fixes #ID` when relevant and describe user-facing impact in the body.
- Pull requests should list key changes, manual validation steps, screenshots for UI tweaks, and any config migrations or seeding steps testers must follow.
