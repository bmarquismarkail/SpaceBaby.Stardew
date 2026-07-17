# Repository guidance for agents

## Scope

This file applies to the entire `SpaceBaby.Stardew` repository. The repository contains independent Stardew Valley SMAPI mods collected in one Visual Studio solution. Keep changes scoped to the affected mod unless a dependency or shared behavior requires a coordinated update.

## Architectural research

The `stardew-graph` MCP is available and must be used for architectural questions about Stardew Valley systems, types, call paths, dependencies, or the likely impact of a change. Start with `query_graph` for broad questions, then use `get_node`, `get_neighbors`, or `shortest_path` to confirm specific relationships. Use BFS for surrounding context and DFS for tracing a focused path.

Graph results are architectural evidence, not a replacement for the checked-out code. Anchor graph queries to concrete namespaces, types, methods, fields, or game systems; verify important conclusions against the local project source and the exact Stardew/SMAPI APIs used by the affected mod. If graph results are ambiguous or overly broad, narrow the query instead of inferring a relationship.

For pull-request work, use the `stardew-graph` PR tools when relevant to check affected communities, blast radius, overlap, and merge risk.

## Solution and projects

`SpaceBaby.Stardew.sln` contains seven `net6.0` mod projects:

- `AdjustableFarmWaterColor/AdjustableFarmWaterColor.csproj` — lets players configure the farm water color.
- `BuildersList/BuildersList.csproj` — provides scavenging/building-list tooltips and its custom menu.
- `RadialMenu/RadialMenu.csproj` — replaces the toolbar interaction with a Secret of Mana-style radial menu.
- `SV_InventorySystem/SV_InventorySystem.csproj` — provides multiple inventories and patches the farmer's active/current item behavior. It uses Harmony.
- `SV_MidiInterface/MidiInterface.csproj` — exposes MIDI device state to Stardew through the `managed-midi` package.
- `SV_PotC/PartOfTheCommunity.csproj` — implements Part of the Community friendship rewards and a modder-facing character/relationship API.
- `SV_VerticalToolMenu/VerticalToolbar.csproj` — implements the extra vertical toolbar and depends on `SV_InventorySystem` both as a project reference and as a required SMAPI dependency.

`SV_PotC.Tests/SV_PotC.Tests.csproj` is a separate `net8.0` executable test harness. It is not currently included in `SpaceBaby.Stardew.sln`; it references `SV_PotC` and requires access to `StardewModdingAPI.dll`.

`SV_PotC.Api.ConsumerSmoke/SV_PotC.Api.ConsumerSmoke.csproj` is a compile-time consumer contract check. It references PotC without copying the provider DLL and compiles the real SMAPI `ModRegistry.GetApi` acquisition pattern. The PotC test harness references it so public API regressions fail the focused test build.

Treat each mod directory as its own deployable unit. Its `manifest.json` defines the SMAPI identity, entry DLL, minimum API version, dependencies, and release-facing version. Do not assume the manifest version and project `<Version>` are already synchronized; inspect both when changing release metadata.

## Code organization and dependencies

- The usual entry point is `ModEntry.cs`; supporting types belong under the project's `Framework/` directory.
- Preserve public APIs and SMAPI unique IDs unless the requested change explicitly includes a migration.
- Keep the `SV_VerticalToolMenu` and `SV_InventorySystem` contracts aligned when changing inventory selection, active-item behavior, or toolbar integration.
- For `SV_PotC` public API or data-format changes, inspect `SV_PotC/API_README.md`, `SV_PotC/Framework/IPartOfTheCommunityApi.cs`, the bundled JSON under `SV_PotC/Data/`, and the first-class SMAPI content-pack example under `SV_PotC/docs/content-pack-example/`.
- PotC JSON integrations should ship as owned SMAPI content packs with `ContentPackFor` targeting `SpaceBaby.PartOfTheCommunity` and a root `content.json`; direct additions to PotC's installed `Data` folder are legacy-only.
- Do not edit generated `bin/` or `obj/` content.

## Build and test

Build the full solution from the repository root when the local Stardew/SMAPI references are available:

```bash
dotnet build SpaceBaby.Stardew.sln
```

For a focused change, build the affected project directly before running the full solution build:

```bash
dotnet build path/to/Project.csproj
```

Run the Part of the Community regression harness separately. Supply the SMAPI assembly through an MSBuild property or one of the supported environment variables; do not hardcode a machine-specific game path in project files:

```bash
dotnet run --project SV_PotC.Tests/SV_PotC.Tests.csproj \
  -p:StardewModdingApiPath=/path/to/StardewModdingAPI.dll
```

Alternatively, set `STARDEW_MODDING_API_DLL` to the DLL path or `STARDEW_VALLEY_PATH` to the game directory.

For a release audit, build PotC in `Release` with deployment disabled and pass the generated ZIP to the harness using `--package=/path/to/PartOfTheCommunity.zip`. This verifies required runtime data, API documentation, and example-data placement in the actual archive.

When behavior depends on live game state, menus, multiplayer, Harmony patches, MIDI hardware, or rendering, document the manual SMAPI verification performed in addition to builds and automated tests. Report any validation that could not be run because the Stardew installation, SMAPI assembly, device, or game runtime is unavailable.

## Change workflow

1. Identify the affected mod and read its `manifest.json`, project file, `ModEntry.cs`, and relevant framework types.
2. For architectural questions or cross-system changes, query `stardew-graph` before deciding on the design, then confirm the result against local code.
3. Make the smallest cohesive change and preserve unrelated behavior and user-owned worktree edits.
4. Build the affected project. Build the solution when practical, and run `SV_PotC.Tests` for changes to Part of the Community logic.
5. Check manifests, dependencies, public API documentation, and example data whenever the change affects those contracts.
