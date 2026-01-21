using StardewModdingAPI;
using StardewValley;

namespace SV_InventorySystem.Framework.Reflection;

/// <summary>
/// SMAPI-style reflection helper for accessing private fields and methods
/// This provides an alternative approach using SMAPI's reflection API instead of Harmony patches
/// Following SMAPI documentation best practices with caching and proper error handling
/// </summary>
public class SmapiReflectionHelper
{
    private readonly IReflectionHelper _reflection;
    private readonly IMonitor _monitor;

    // Cached reflection accessors for performance (per SMAPI docs recommendation)
    private IReflectedField<bool>? _itemStowedField;
    private IReflectedMethod? _removeItemFromInventoryMethod;
    private IReflectedMethod? _addItemToInventoryMethod;

    public SmapiReflectionHelper(IReflectionHelper reflection, IMonitor monitor)
    {
        _reflection = reflection;
        _monitor = monitor;
    }

    /// <summary>
    /// Gets the _itemStowed field value using SMAPI reflection
    /// </summary>
    public bool GetItemStowed(Farmer farmer)
    {
        try
        {
            // Cache the reflection object for performance (per SMAPI docs)
            _itemStowedField ??= _reflection.GetField<bool>(farmer, "_itemStowed", required: false);
            
            if (_itemStowedField == null)
            {
                _monitor.Log("Could not access _itemStowed field", LogLevel.Warn);
                return false;
            }

            return _itemStowedField.GetValue();
        }
        catch (Exception ex)
        {
            _monitor.Log($"Error accessing _itemStowed field: {ex.Message}", LogLevel.Error);
            return false;
        }
    }

    /// <summary>
    /// Sets the _itemStowed field value using SMAPI reflection
    /// </summary>
    public void SetItemStowed(Farmer farmer, bool value)
    {
        try
        {
            // Cache the reflection object for performance (per SMAPI docs)
            _itemStowedField ??= _reflection.GetField<bool>(farmer, "_itemStowed", required: false);
            
            if (_itemStowedField == null)
            {
                _monitor.Log("Could not access _itemStowed field for setting", LogLevel.Warn);
                return;
            }

            _itemStowedField.SetValue(value);
        }
        catch (Exception ex)
        {
            _monitor.Log($"Error setting _itemStowed field: {ex.Message}", LogLevel.Error);
        }
    }

    /// <summary>
    /// Alternative CurrentItem implementation using SMAPI reflection
    /// This mimics the original game logic but allows for multi-inventory override
    /// Following the exact priority order from decompiled game code
    /// </summary>
    public Item? GetCurrentItem(Farmer farmer, IMultiInventoryManager? inventoryManager = null)
    {
        try
        {
            // 1. First priority: TemporaryItem (public property, no reflection needed)
            if (farmer.TemporaryItem != null)
            {
                return farmer.TemporaryItem;
            }

            // 2. Second priority: Check if item is stowed
            if (GetItemStowed(farmer))
            {
                return null;
            }

            // 3. Third priority: Get from inventory system
            if (inventoryManager != null)
            {
                return inventoryManager.GetCurrentItem(farmer);
            }

            // 4. Fallback: Original game logic (bounds check like decompiled code)
            if (farmer.CurrentToolIndex >= farmer.Items.Count)
            {
                return null;
            }

            return farmer.Items[farmer.CurrentToolIndex];
        }
        catch (Exception ex)
        {
            _monitor.Log($"Error in GetCurrentItem: {ex.Message}", LogLevel.Error);
            return null;
        }
    }

    /// <summary>
    /// Alternative ActiveItem implementation using SMAPI reflection
    /// Follows the decompiled ActiveItem getter logic exactly
    /// </summary>
    public Item? GetActiveItem(Farmer farmer, IMultiInventoryManager? inventoryManager = null)
    {
        try
        {
            // 1. First priority: TemporaryItem (public property, no reflection needed)
            if (farmer.TemporaryItem != null)
            {
                return farmer.TemporaryItem;
            }

            // 2. Second priority: Check if item is stowed
            if (GetItemStowed(farmer))
            {
                return null;
            }

            // 3. Third priority: Get from multi-inventory system with exact ActiveItem bounds checking
            if (inventoryManager != null)
            {
                var currentItem = inventoryManager.GetCurrentItem(farmer);
                
                // ActiveItem has additional bounds and null check (from decompiled code)
                if (farmer.CurrentToolIndex < inventoryManager.GetTotalInventorySize(farmer) && currentItem != null)
                {
                    return currentItem;
                }
                
                return null;
            }

            // 4. Fallback: Original game logic with ActiveItem's specific bounds check
            if (farmer.CurrentToolIndex < farmer.Items.Count && farmer.Items[farmer.CurrentToolIndex] != null)
            {
                return farmer.Items[farmer.CurrentToolIndex];
            }

            return null;
        }
        catch (Exception ex)
        {
            _monitor.Log($"Error in GetActiveItem: {ex.Message}", LogLevel.Error);
            return null;
        }
    }

    /// <summary>
    /// Alternative ActiveItem setter implementation using SMAPI reflection
    /// Follows the exact decompiled ActiveItem setter logic
    /// </summary>
    public void SetActiveItem(Farmer farmer, Item? value, IMultiInventoryManager? inventoryManager = null)
    {
        try
        {
            // Preserve original setter logic: always set netItemStowed to false
            if (farmer.netItemStowed != null)
            {
                farmer.netItemStowed.Set(newValue: false);
            }

            if (value == null)
            {
                if (inventoryManager != null)
                {
                    // Remove current item from multi-inventory
                    var currentActiveItem = inventoryManager.GetCurrentItem(farmer);
                    if (currentActiveItem != null)
                    {
                        inventoryManager.RemoveItem(farmer, currentActiveItem);
                    }
                }
                else
                {
                    // Fallback: Use SMAPI reflection to call removeItemFromInventory
                    _removeItemFromInventoryMethod ??= _reflection.GetMethod(farmer, "removeItemFromInventory", required: false);
                    
                    if (_removeItemFromInventoryMethod != null)
                    {
                        var currentItem = GetActiveItem(farmer);
                        if (currentItem != null)
                        {
                            _removeItemFromInventoryMethod.Invoke(currentItem);
                        }
                    }
                    else
                    {
                        _monitor.Log("Could not access removeItemFromInventory method", LogLevel.Warn);
                    }
                }
            }
            else
            {
                if (inventoryManager != null)
                {
                    // Add item to multi-inventory at current tool index
                    inventoryManager.AddItemAtIndex(farmer, value, farmer.CurrentToolIndex);
                }
                else
                {
                    // Fallback: Use SMAPI reflection to call addItemToInventory
                    _addItemToInventoryMethod ??= _reflection.GetMethod(farmer, "addItemToInventory", required: false);
                    
                    if (_addItemToInventoryMethod != null)
                    {
                        _addItemToInventoryMethod.Invoke(value, farmer.CurrentToolIndex);
                    }
                    else
                    {
                        _monitor.Log("Could not access addItemToInventory method", LogLevel.Warn);
                        // Final fallback: direct assignment if within bounds
                        if (farmer.CurrentToolIndex < farmer.Items.Count)
                        {
                            farmer.Items[farmer.CurrentToolIndex] = value;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _monitor.Log($"Error in SetActiveItem: {ex.Message}", LogLevel.Error);
        }
    }

    /// <summary>
    /// Validates that all required fields and methods can be accessed
    /// Uses the SMAPI reflection pattern with required=false for graceful validation
    /// </summary>
    public bool ValidateReflection(Farmer farmer)
    {
        try
        {
            // Test _itemStowed field access with required=false for graceful handling
            var testItemStowedField = _reflection.GetField<bool>(farmer, "_itemStowed", required: false);
            if (testItemStowedField == null)
            {
                _monitor.Log("_itemStowed field not accessible", LogLevel.Error);
                return false;
            }

            // Test accessing the value to ensure it works
            testItemStowedField.GetValue();

            // Test method access with required=false
            var testRemoveMethod = _reflection.GetMethod(farmer, "removeItemFromInventory", required: false);
            var testAddMethod = _reflection.GetMethod(farmer, "addItemToInventory", required: false);
            
            if (testRemoveMethod == null)
            {
                _monitor.Log("removeItemFromInventory method not accessible", LogLevel.Warn);
            }
            
            if (testAddMethod == null)
            {
                _monitor.Log("addItemToInventory method not accessible", LogLevel.Warn);
            }

            _monitor.Log("SMAPI reflection validation successful", LogLevel.Debug);
            return true;
        }
        catch (Exception ex)
        {
            _monitor.Log($"SMAPI reflection validation failed: {ex.Message}", LogLevel.Error);
            return false;
        }
    }

    /// <summary>
    /// Clears cached reflection objects (useful for hot-reload scenarios)
    /// </summary>
    public void ClearCache()
    {
        _itemStowedField = null;
        _removeItemFromInventoryMethod = null;
        _addItemToInventoryMethod = null;
        _monitor.Log("Reflection cache cleared", LogLevel.Debug);
    }
}