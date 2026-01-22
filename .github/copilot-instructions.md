<!-- SpaceBaby.Stardew: Copilot instructions for AI coding agents -->
# Copilot instructions — SpaceBaby.Stardew

Purpose: Give actionable, repo-specific guidance so an AI coding agent can be immediately productive.

Quick context
- This repo is a collection of Stardew/SMAPI mods grouped as subprojects (e.g. `AdjustableFarmWaterColor/`, `BuildersList/`, `SV_InventorySystem/`, `SV_VerticalToolMenu/`).
- Each mod typically exposes a `ModEntry.cs` (entry point) and a `manifest.json` for SMAPI.
- Build outputs used for testing/deployment are under `*/bin/Debug/Framework`.

Recommended workflows
- Build solution: `dotnet build SpaceBaby.Stardew.sln -c Release` or build an individual project with `dotnet build <path-to-csproj> -c Debug`.
- To test in-game: copy the contents of the mod's `bin/Debug/Framework` folder into SMAPI's `Mods/<ModName>` directory, or configure your IDE to place outputs where SMAPI loads them.
- Debugging: attach the debugger to the running SMAPI/ Stardew Valley process after loading the built mod; breakpoints in `ModEntry.cs` are reliable entry locations.

Architecture & conventions (how the repo is organized)
- Each folder is a separate mod project. Look for `ModEntry.cs` and `manifest.json` to understand mod boundaries.
- Shared patterns: a `Framework/` subfolder often contains `ModConfig.cs` and helper utilities used by the mod's runtime code.
- `obj/` and `bin/` are standard MSBuild outputs — do not edit generated files.

Project-specific patterns to follow
- Preserve the `manifest.json` GUID and metadata when modifying a mod; SMAPI uses that metadata to identify the mod.
- Configuration classes live in `Framework/ModConfig.cs` in several mods; follow the existing property names and serialization patterns when extending config.
- Many mods use reflection or internal `Manager` helpers (example: `SV_MidiInterface/Manager/`) — prefer following existing helper APIs instead of introducing new static entrypoints.

Integration points & examples
- Entry points: `AdjustableFarmWaterColor/ModEntry.cs`, `BuildersList/ModEntry.cs`, `SV_InventorySystem/ModEntry.cs`.
- Packaging: compiled assemblies and dependency files in `*/bin/Debug/Framework` are what get deployed to SMAPI.
- Cross-project references: inspect csproj files (e.g. `SV_VerticalToolMenu/VerticalToolbar.csproj`) to understand project references and target frameworks.

When editing code
- Keep public APIs stable across mods; if you change a serialized config or manifest GUID, update all relevant references and mention the change in the PR.
- Run a local build and sanity-check by placing the built `Framework` output into a SMAPI `Mods` folder before opening a PR.

Files to consult for examples
- `AdjustableFarmWaterColor/ModEntry.cs` — small, clear ModEntry example
- `BuildersList/ModEntry.cs` and `BuildersList/Framework/ModConfig.cs` — shows config + entrypoint pattern
- `SV_MidiInterface/Manager/` — demonstrates internal managers and stateful services
- `SV_VerticalToolMenu/` — branch-specific work exists on `vertical-toolbar-development` (watch for branch-specific changes)

Do not assume
- Do not assume a single global startup — each folder is typically an independent mod loaded by SMAPI.
- Do not change generated `obj/` or `bin/` contents in commits.

If unsure, quick checks
- Is there a `manifest.json` in the folder? If yes, treat folder as a standalone SMAPI mod.
- Does the folder have `Framework/ModConfig.cs`? Follow its serialization properties and naming.

If you edit these instructions or add automations, mention the exact files changed and provide a short manual test (build + copy into SMAPI Mods folder).

— End of file
