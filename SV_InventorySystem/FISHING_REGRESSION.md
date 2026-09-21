# Fishing rod in additional inventories

## Cause and fix

Verified against decompiled Stardew Valley 1.6.15.24356: `Farmer.updateCommon(GameTime, GameLocation)` calls `Tool.tickUpdate` only for `Farmer.Items` and `TemporaryItem`. Vertical Toolbar stores its tools in a separate inventory. Patching `CurrentItem` allows the rod to begin use, but does not supply its subsequent ticks. `FishingRod.tickUpdate` processes casting, put-away, and movement-release events, so missing those ticks leaves the farmer stuck using the rod.

Inventory System now patches `updateCommon` to tick tools in every additional inventory. This includes unselected tools so pending cleanup events run. Tools already covered by vanilla, and duplicate references across additional slots, are excluded. Both local and remote farmers use this game method.

## Gameplay regression checks

User-reported manual validation: steps 1–6 and 8 passed. Multiplayer (step 7) remains untested.

Use the updated Inventory System with Vertical Toolbar on a disposable test save.

1. Put a bamboo pole in the vertical toolbar. Hold use to charge, release to cast into water, and reel back in. Confirm movement, tool switching, and inventory access return.
2. Catch a fish, dismiss the catch display, and repeat a cast. Check cancellation and casting onto dry land too.
3. Repeat with a fiberglass or iridium rod, including bait/tackle and a treasure catch.
4. Switch away from the rod after fishing, then select it again. Confirm no stale animation or movement lock.
5. Repeat with the rod in the regular inventory. Confirm casting speed and behavior are unchanged.
6. Test another tool in the vertical toolbar and a rod in another registered additional inventory.
7. In multiplayer, repeat as host and farmhand; check the other player's casting animation and both players' ability to move afterward.
8. Check the SMAPI log for patch failures or exceptions.

Build validation: `dotnet build SV_VerticalToolMenu/VerticalToolbar.csproj -p:EnableModDeploy=false -p:EnableModZip=false` builds both affected mods without installing them.
