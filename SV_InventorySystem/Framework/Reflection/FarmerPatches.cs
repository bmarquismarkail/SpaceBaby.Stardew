using StardewModdingAPI;
using StardewValley;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;

namespace SV_InventorySystem.Framework.Reflection;

/// <summary>
/// Harmony patches for Farmer CurrentItem and ActiveItem properties to support multiple inventories
/// </summary>
public class FarmerPatches
{
    private static IMonitor? Monitor;
    private static IMultiInventoryManager? InventoryManager;
    private static readonly FieldInfo? ItemStowedField = AccessTools.Field(typeof(Farmer), "_itemStowed");

    public static void Initialize(IMonitor monitor, IMultiInventoryManager inventoryManager)
    {
        Monitor = monitor;
        InventoryManager = inventoryManager;
    }

    /// <summary>Update tools outside the base inventory, including inactive tools with pending events.</summary>
    public static void UpdateCommon_Postfix(Farmer __instance, GameTime time)
    {
        if (InventoryManager == null)
            return;

        // Vanilla updateCommon ticks Items and TemporaryItem for both local and remote farmers.
        // Fishing rods need these ticks to process casting, put-away, and movement-release events.
        int inventoryCount = InventoryManager.GetInventoryCount(__instance);
        if (inventoryCount <= 1)
            return;

        var updated = new HashSet<Item>(ReferenceEqualityComparer.Instance);
        foreach (var item in __instance.Items)
        {
            if (item is Tool)
                updated.Add(item);
        }
        if (__instance.TemporaryItem is Tool temporaryTool)
            updated.Add(temporaryTool);

        for (int inventoryIndex = 1; inventoryIndex < inventoryCount; inventoryIndex++)
        {
            var inventory = InventoryManager.GetInventory(__instance, inventoryIndex);
            if (inventory == null)
                continue;

            for (int slot = inventory.Count - 1; slot >= 0; slot--)
            {
                if (inventory[slot] is Tool tool && updated.Add(tool))
                    tool.tickUpdate(time, __instance);
            }
        }
    }

    /// <summary>
    /// Prefix for CurrentItem getter to intercept and return item from multi-inventory system
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Farmer), "CurrentItem", MethodType.Getter)]
    public static bool CurrentItem_Getter_Prefix(Farmer __instance, ref Item? __result)
    {
        try
        {
            if (InventoryManager == null)
            {
                Monitor?.Log("InventoryManager not initialized, using original implementation", LogLevel.Debug);
                return true; // Fall back to original implementation
            }

            // Preserve the original priority logic from decompiled code
            
            // 1. First priority: TemporaryItem
            if (__instance.TemporaryItem != null)
            {
                __result = __instance.TemporaryItem;
                return false; // Skip original method
            }

            // 2. Second priority: Check if item is stowed
            // Use safer reflection access
            if (ItemStowedField == null)
            {
                Monitor?.Log("Could not find _itemStowed field, falling back to original", LogLevel.Warn);
                return true;
            }
            
            var stowedValue = ItemStowedField.GetValue(__instance);
            bool isItemStowed = stowedValue is bool b && b;
            if (isItemStowed)
            {
                __result = null;
                return false; // Skip original method
            }

            // 3. Third priority: Get from multi-inventory system instead of Items[CurrentToolIndex]
            __result = InventoryManager.GetCurrentItem(__instance);
            return false; // Skip original method
        }
        catch (Exception ex)
        {
            Monitor?.Log($"Error in CurrentItem getter patch: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
            return true; // Fall back to original implementation
        }
    }

    /// <summary>
    /// Prefix for ActiveItem getter to intercept and return item from multi-inventory system
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Farmer), "ActiveItem", MethodType.Getter)]
    public static bool ActiveItem_Getter_Prefix(Farmer __instance, ref Item? __result)
    {
        try
        {
            if (InventoryManager == null)
            {
                Monitor?.Log("InventoryManager not initialized, using original implementation", LogLevel.Debug);
                return true; // Fall back to original implementation
            }

            // Preserve the original priority logic from decompiled code
            
            // 1. First priority: TemporaryItem
            if (__instance.TemporaryItem != null)
            {
                __result = __instance.TemporaryItem;
                return false; // Skip original method
            }

            // 2. Second priority: Check if item is stowed
            if (ItemStowedField == null)
            {
                Monitor?.Log("Could not find _itemStowed field, falling back to original", LogLevel.Warn);
                return true;
            }
            
            var stowedValue = ItemStowedField.GetValue(__instance);
            bool isItemStowed = stowedValue is bool b && b;
            if (isItemStowed)
            {
                __result = null;
                return false; // Skip original method
            }

            // 3. Third priority: Get from multi-inventory system instead of Items[CurrentToolIndex]
            var currentItem = InventoryManager.GetCurrentItem(__instance);
            
            // ActiveItem has additional bounds and null check compared to CurrentItem
            if (__instance.CurrentToolIndex < InventoryManager.GetTotalInventorySize(__instance) && currentItem != null)
            {
                __result = currentItem;
            }
            else
            {
                __result = null;
            }
            
            return false; // Skip original method
        }
        catch (Exception ex)
        {
            Monitor?.Log($"Error in ActiveItem getter patch: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
            return true; // Fall back to original implementation
        }
    }

    /// <summary>
    /// Prefix for ActiveItem setter to intercept and handle multi-inventory system
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Farmer), "ActiveItem", MethodType.Setter)]
    public static bool ActiveItem_Setter_Prefix(Farmer __instance, Item? value)
    {
        try
        {
            if (InventoryManager == null)
            {
                Monitor?.Log("InventoryManager not initialized, using original implementation", LogLevel.Debug);
                return true; // Fall back to original implementation
            }

            // Preserve the original setter logic from decompiled code
            if (__instance.netItemStowed != null)
            {
                __instance.netItemStowed.Set(newValue: false);
            }
            
            if (value == null)
            {
                // Remove current item from multi-inventory
                var currentActiveItem = InventoryManager.GetCurrentItem(__instance);
                if (currentActiveItem != null)
                {
                    InventoryManager.RemoveItem(__instance, currentActiveItem);
                }
            }
            else
            {
                // Add item to multi-inventory at current tool index
                Item? remainder = InventoryManager.AddItemAtIndexWithRemainder(__instance, value, __instance.CurrentToolIndex);
                if (remainder != null)
                {
                    for (int index = 0; index < InventoryManager.GetTotalInventorySize(__instance); index++)
                    {
                        if (index == __instance.CurrentToolIndex)
                            continue;

                        var mapping = InventoryManager.TranslateGlobalIndex(__instance, index);
                        if (mapping == null)
                            continue;

                        var inventory = InventoryManager.GetInventory(__instance, mapping.Value.inventoryIndex);
                        if (inventory == null || mapping.Value.localIndex < 0 || mapping.Value.localIndex >= inventory.Count)
                            continue;

                        Item? existing = inventory[mapping.Value.localIndex];
                        if (existing != null && !existing.canStackWith(remainder))
                            continue;

                        remainder = InventoryManager.AddItemAtIndexWithRemainder(__instance, remainder, index);
                        if (remainder == null)
                            break;
                    }

                    if (remainder != null)
                        Game1.createItemDebris(remainder, __instance.getStandingPosition(), __instance.FacingDirection);
                }
            }

            return false; // Skip original method
        }
        catch (Exception ex)
        {
            Monitor?.Log($"Error in ActiveItem setter patch: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
            return true; // Fall back to original implementation
        }
    }

    /// <summary>
    /// Prefix for the CurrentTool setter, whose vanilla implementation writes directly to
    /// <see cref="Farmer.Items"/> and would otherwise expand the base inventory for a global index.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Farmer), "CurrentTool", MethodType.Setter)]
    public static bool CurrentTool_Setter_Prefix(Farmer __instance, Tool? value)
    {
        try
        {
            if (InventoryManager == null)
                return true;

            var mapping = InventoryManager.TranslateGlobalIndex(__instance, __instance.CurrentToolIndex);
            if (mapping == null)
            {
                Monitor?.Log($"Ignoring CurrentTool assignment at invalid global index {__instance.CurrentToolIndex}.", LogLevel.Warn);
                return false;
            }

            if (mapping.Value.inventoryIndex == 0)
                return true;

            IList<Item?>? inventory = InventoryManager.GetInventory(__instance, mapping.Value.inventoryIndex);
            if (inventory == null || mapping.Value.localIndex < 0 || mapping.Value.localIndex >= inventory.Count)
                return false;

            inventory[mapping.Value.localIndex] = value;
            return false;
        }
        catch (Exception ex)
        {
            Monitor?.Log($"Error in CurrentTool setter patch: {ex}", LogLevel.Error);
            return false;
        }
    }

    /// <summary>
    /// Postfix for CurrentToolIndex setter to handle tool changes in multi-inventory system
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Farmer), "CurrentToolIndex", MethodType.Setter)]
    public static void CurrentToolIndex_Setter_Postfix(Farmer __instance, int value)
    {
        try
        {
            if (InventoryManager == null)
                return;
            
            Monitor?.Log($"CurrentToolIndex changed to {value}", LogLevel.Debug);

            // Notify inventory manager about tool index change for any additional processing
            InventoryManager.OnToolIndexChanged(__instance, value);
        }
        catch (Exception ex)
        {
            Monitor?.Log($"Error in CurrentToolIndex setter postfix: {ex}", LogLevel.Error);
        }
    }
}
