using StardewModdingAPI;
using StardewValley;
using System.Reflection;
using HarmonyLib;

namespace SV_InventorySystem.Framework.Reflection;

/// <summary>
/// Harmony patches for Farmer CurrentItem and ActiveItem properties to support multiple inventories
/// </summary>
public class FarmerPatches
{
    private static IMonitor? Monitor;
    private static IMultiInventoryManager? InventoryManager;

    public static void Initialize(IMonitor monitor, IMultiInventoryManager inventoryManager)
    {
        Monitor = monitor;
        InventoryManager = inventoryManager;
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
            var itemStowedField = AccessTools.Field(typeof(Farmer), "_itemStowed");
            if (itemStowedField == null)
            {
                Monitor?.Log("Could not find _itemStowed field, falling back to original", LogLevel.Warn);
                return true;
            }
            
            var stowedValue = itemStowedField.GetValue(__instance);
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
            var itemStowedField = AccessTools.Field(typeof(Farmer), "_itemStowed");
            if (itemStowedField == null)
            {
                Monitor?.Log("Could not find _itemStowed field, falling back to original", LogLevel.Warn);
                return true;
            }
            
            var stowedValue = itemStowedField.GetValue(__instance);
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
                InventoryManager.AddItemAtIndex(__instance, value, __instance.CurrentToolIndex);
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
