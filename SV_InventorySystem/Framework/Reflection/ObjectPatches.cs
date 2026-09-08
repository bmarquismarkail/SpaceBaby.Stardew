using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace SV_InventorySystem.Framework.Reflection;

/// <summary>Compatibility patches for object interactions which bypass Farmer.CurrentItem.</summary>
internal static class ObjectPatches
{
    private static IMonitor? Monitor;
    private static IMultiInventoryManager? InventoryManager;

    public static void Initialize(IMonitor monitor, IMultiInventoryManager inventoryManager)
    {
        Monitor = monitor;
        InventoryManager = inventoryManager;
    }

    /// <summary>
    /// Handle hats selected from an additional inventory before vanilla directly indexes Farmer.Items.
    /// The base-inventory path is left entirely to the game.
    /// </summary>
    public static bool CheckForActionOnScarecrow_Prefix(
        SObject __instance,
        Farmer who,
        bool justCheckingForActivity,
        ref bool __result)
    {
        if (justCheckingForActivity
            || who.CurrentToolIndex < who.Items.Count
            || __instance.QualifiedItemId != "(BC)126"
            || who.CurrentItem is not Hat hat)
        {
            return true;
        }

        if (InventoryManager == null || !InventoryManager.RemoveItem(who, hat))
        {
            Monitor?.Log("Couldn't remove the selected extra-inventory hat during scarecrow interaction.", LogLevel.Error);
            __result = false;
            return false;
        }

        __instance.shakeTimer = 100;
        if (__instance.Quality != 0)
        {
            Game1.createItemDebris(
                ItemRegistry.Create("(H)" + (__instance.Quality - 1)),
                __instance.TileLocation * 64f,
                (who.FacingDirection + 2) % 4);
            __instance.Quality = 0;
        }

        if (__instance.preservedParentSheetIndex.Value != null)
        {
            Game1.createItemDebris(
                new Hat(__instance.preservedParentSheetIndex.Value),
                __instance.TileLocation * 64f,
                (who.FacingDirection + 2) % 4);
        }

        __instance.preservedParentSheetIndex.Value = hat.ItemId;
        __instance.Location.playSound("dirtyHit");
        __result = true;
        return false;
    }
}
