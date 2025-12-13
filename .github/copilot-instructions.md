<!--
Guidance to help automated coding agents (Copilot / GH coding agents) be productive
in the SpaceBaby.Stardew repository. Keep short, concrete, and codebase-specific.
-->

# SpaceBaby.Stardew — Copilot instructions

This repository is a collection of Stardew Valley SMAPI mods maintained by the same
author. Each mod is a self-contained .NET project with a `manifest.json` and an
`EntryDll`. Before changing behavior, understand the SMAPI lifecycle and where
mod state lives.

Key points
- Multi-project solution: a Visual Studio solution (`SpaceBaby.Stardew.sln`) contains
  multiple independent mod projects (folders like `SV_PotC`, `SV_VerticalToolMenu`,
  `BuildersList`, `AdjustableFarmWaterColor`, `RadialMenu`). Treat each project as
  a separate SMAPI mod — avoid cross-project runtime coupling unless clearly used.
- SMAPI integration: each mod implements `public class ModEntry : Mod` and overrides
  `Entry(IModHelper helper)`. Look for event registration patterns (e.g.
  `helper.Events.GameLoop.UpdateTicked += ...`, `Display.MenuChanged`, `Input.ButtonPressed`).
- Reflection & helper APIs: mods frequently use `this.Helper.ReadConfig<T>()` or
  `Helper.Data.ReadJsonFile(...)` to load/save config and `Helper.Reflection.GetField<T>(..., "fieldName")`
  to access private game fields (see `BuildersList/Framework/ScavengerMenu.cs` and
  `SV_PotC/ModEntry.cs`). Preserve these patterns when modifying logic.

Build & test (developer workflow)
- Projects are standard SDK-style C# projects targeting `net6.0`. Use the .NET CLI:
  - Build a single mod: `dotnet build <PathToProject>.csproj -c Release`
  - Build the whole solution: `dotnet build SpaceBaby.Stardew.sln -c Release`
- Output DLLs go to `bin/Debug` or `bin/Release` within each project — those are
  the artifacts referenced by the mod `manifest.json` (see `AdjustableFarmWaterColor/manifest.json`).
- SMAPI runtime: to test in-game, copy the built DLL and the `manifest.json` into
  a SMAPI mod folder or use your existing SMAPI mod loader workflow. The repo does
  not include automated game-run scripts.

Conventions & patterns to follow
- Config files
  - Most mods call `helper.ReadConfig<ModConfig>()` or `Helper.Data.ReadJsonFile("config.json")`.
    Keep config types in each project's `Framework` folder (e.g. `BuildersList/Framework/ModConfig.cs`).
- UI and Game menus
  - Mods manipulate game menus by subscribing to `Display.MenuChanged` and
    replacing or adding pages (example: `SV_VerticalToolMenu/ModEntry.cs` modifies `GameMenu` pages).
  - For custom on-screen UI, look in `Framework` subfolders (e.g. `ScavengerMenu.cs`)
    and follow existing drawing/size logic when adding new controls.
- Event-driven updates
  - Frequent pattern: register events in `Entry(...)`, keep light-weight logic in
    event handlers (`UpdateTicked`, `RenderedHud`, etc.). Avoid long-running/blocking work in handlers.
- Reflection usage
  - Many mods use `Helper.Reflection.GetField<T>(..., "fieldName").GetValue()` to
    read private game fields (e.g., `pagesOfCraftingRecipes`, `heldItem`, `cooking`).
  - When changing reflection field names, verify they still exist for the target
    game version; prefer safe null checks and fallbacks.

Integration points & external dependencies
- SMAPI APIs: The mods rely on the StardewModdingAPI nuget/runtime environment.
  Expect APIs like `IModHelper`, `IMonitor`, event args types, and the game model
  (`Game1`, `NPC`, `GameLocation`). Do not convert these to other frameworks.
- Third-party mods: Some projects request APIs at runtime (e.g. Generic Mod Config Menu).
  Use `Helper.ModRegistry.GetApi<T>("<modUniqueID>")` and handle null when the mod isn't installed.

Examples (copyable snippets)
- Read config in Entry:
  - AdjustableFarmWaterColor: `this.Config = this.Helper.ReadConfig<ModConfig>();`
- Use reflection to read a private field:
  - BuildersList: `this.Helper.Reflection.GetField<bool>(currentCraftingPage, "cooking").GetValue();`
- Register events in Entry:
  - `helper.Events.Display.Rendering += ChangeWater;`

Files and folders to inspect when changing behavior
- `SpaceBaby.Stardew.sln` — top-level solution
- `<mod>/manifest.json` — mod metadata and EntryDll
- `<mod>/Framework/*` — common supporting types (ModConfig, custom menus, utilities)
- `<mod>/ModEntry.cs` or `Framework/ModEntry.cs` — the SMAPI entry point and event wiring

Don'ts
- Don't assume any global game state beyond SMAPI/Game1 — each mod manages its own state.
- Don't change reflection field names without ensuring the in-game version compatibility.

If anything above is unclear or you want more detail about build/test commands or
where to run the game for debugging, tell me what platform/SMAPI version you use and
I'll extend this file with reproducer steps.
