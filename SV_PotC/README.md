# Part of the Community

`Part of the Community` is a Stardew Valley mod that rewards community-minded play with friendship bonuses and now includes a small API for other mods to register custom characters and relationships.

## Modder API

If you want to integrate with PotC from another mod, see:

- `API_README.md` — API methods, relationship types, and JSON pack format

## Project notes

- Target framework: `net6.0`
- SMAPI minimum version: `4.3.0`
- External integrations should declare a manifest dependency on PotC and acquire the API during `GameLaunched`.
- The runtime API is ready before `GameLaunched`; see `API_README.md` for the safe compile-time reference configuration.
