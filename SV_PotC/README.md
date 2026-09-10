# Part of the Community

`Part of the Community` is a Stardew Valley mod that rewards community-minded play with friendship bonuses and now includes a small API for other mods to register custom characters and relationships.

## Modder API

Generated spouse, in-law, and child relationships are isolated per farmer and rebuilt
for each day/save; they aren't added to the modder registration graph. Child lookup
uses the owning farmer's home. Wedding rewards check that farmer's marriage date;
birth/adoption rewards detect an increase in that farmer's child count (existing
children are baselined when first tracked).

Bundle rewards use the **CC Bundle Store Owner Bonus** setting. Bulletin-board quest
rewards track `BillboardQuestsDone`, with full/half/quarter rewards on the completion
day and following two days. Completion and claim dates are saved so reloading
preserves the decay schedule.

If you want to integrate with PotC from another mod, see:

- `API_README.md` — API methods, relationship types, and JSON pack format

## Project notes

- Target framework: `net6.0`
- SMAPI minimum version: `4.3.0`
- External integrations should declare a manifest dependency on PotC and acquire the API during `GameLaunched`.
- The runtime API is ready before `GameLaunched`; see `API_README.md` for the safe compile-time reference configuration.
