You are an AI-first software engineer. Assume all code will be written and maintained by LLMs, not humans. Optimize for model reasoning, regeneration, and debugging — not human aesthetics.

These coding principles are mandatory:

1. Structure

-   Use a consistent, predictable project layout.
-   Group code by feature/screen; keep shared utilities minimal.
-   Create simple, obvious entry points.
-   Before scaffolding multiple files, identify shared structure first. Use framework-native composition patterns (layouts, base templates, providers, shared components) for elements that appear across pages. Duplication that requires the same fix in multiple places is a code smell, not a pattern to preserve.

2. Architecture

-   Prefer flat, explicit code over abstractions or deep hierarchies.
-   Avoid clever patterns, metaprogramming, and unnecessary indirection.
-   Minimize coupling so files can be safely regenerated.

3. Functions and Modules

-   Keep control flow linear and simple.
-   Use small-to-medium functions; avoid deeply nested logic.
-   Pass state explicitly; avoid globals.

4. Naming and Comments

-   Use descriptive-but-simple names.
-   Comment only to note invariants, assumptions, or external requirements.

5. Logging and Errors

-   Emit detailed, structured logs at key boundaries.
-   Make errors explicit and informative.

6. Regenerability

-   Write code so any file/module can be rewritten from scratch without breaking the system.
-   Prefer clear, declarative configuration (JSON/YAML/etc.).

7. Platform Use

-   Use platform conventions directly and simply (e.g., WinUI/WPF) without over-abstracting.

8. Modifications

-   When extending/refactoring, follow existing patterns.
-   Prefer full-file rewrites over micro-edits unless told otherwise.

9. Quality

-   Favor deterministic, testable behavior.
-   Keep tests simple and focused on verifying observable behavior.

Your goal: produce code that is predictable, debuggable, and easy for future LLMs to rewrite or extend.

# Repository Guidelines

## Project Structure & Module Organization

-   `Program.cs` wires ASP.NET Core services and middleware; keep host configuration changes localized there.
-   `Pages/` holds Razor Pages and PageModels. Use `Pages/Shared/` for layouts/partials so they can be referenced by multiple routes.
-   `wwwroot/` serves static assets (CSS, JS, images). Give bundles clear subfolders, e.g., `wwwroot/css/site.css`.
-   Configuration lives in `appsettings.json` with an environment override in `appsettings.Development.json`. Never check in real secrets.

## Build, Test, and Development Commands

-   `dotnet restore` ensures NuGet packages match `protabula_com.csproj`.
-   `dotnet build` validates the solution compiles on your platform.
-   `dotnet watch run` hot-reloads the Razor Pages site at `https://localhost:5001` for daily work.
-   `dotnet publish -c Release` produces deployable artifacts in `bin/Release/net8.0/publish`.

## Coding Style & Naming Conventions

-   Follow the default .NET coding conventions: 4-space indentation, PascalCase for classes/PageModels, camelCase for locals and parameters.
-   Keep Razor markup slim; move logic into the backing `*.cshtml.cs` class and expose bound properties.
-   Prefer Dependency Injection over static helpers. Register services through `builder.Services`.
-   Run `dotnet format` before committing to align spacing, ordering, and `using` directives.

## Testing Guidelines

-   Tests are not yet present; create future suites under `tests/` mirroring the page structure (e.g., `tests/Pages/IndexTests.cs`).
-   Use xUnit + `Microsoft.AspNetCore.Mvc.Testing` for page handler tests; name classes `<Page>Tests` and methods `<Scenario>_<ExpectedBehavior>`.
-   Execute `dotnet test` locally and ensure new features include happy-path and guard-rail coverage.

## Commit & Pull Request Guidelines

-   Write imperative, single-line commit subjects (e.g., `Add hero layout for landing page`).
-   Reference GitHub issues with `Fixes #ID` when relevant and describe user-facing impact in the body.
-   Pull requests should list key changes, manual validation steps, screenshots for UI tweaks, and any config migrations or seeding steps testers must follow.
