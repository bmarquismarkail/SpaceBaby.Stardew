using StardewModdingAPI;
using SV_InventorySystem.Framework.Reflection;
using StardewValley;

namespace SV_InventorySystem.Framework;

/// <summary>
/// Example integration class showing how to use the multi-inventory system
/// This demonstrates both Harmony patch approach and manual SMAPI approach
/// </summary>
public class IntegrationExample
{
    private readonly IMonitor _monitor;
    private readonly IMultiInventoryManager _inventoryManager;
    private readonly SmapiReflectionHelper? _smapiHelper;
    private readonly bool _useHarmonyPatches;

    public IntegrationExample(IMonitor monitor, IMultiInventoryManager inventoryManager, 
                             SmapiReflectionHelper? smapiHelper = null, bool useHarmonyPatches = true)
    {
        _monitor = monitor;
        _inventoryManager = inventoryManager;
        _smapiHelper = smapiHelper;
        _useHarmonyPatches = useHarmonyPatches;
    }

    /// <summary>
    /// Example: Get current item safely (works with both approaches)
    /// </summary>
    public Item? GetCurrentItemSafely(Farmer farmer)
    {
        if (_useHarmonyPatches)
        {
            // With Harmony patches, CurrentItem is automatically redirected
            return farmer.CurrentItem;
        }
        else
        {
            // With SMAPI approach, use the helper manually
            return _smapiHelper?.GetCurrentItem(farmer, _inventoryManager);
        }
    }

    /// <summary>
    /// Example: Set active item safely (works with both approaches)
    /// </summary>
    public void SetActiveItemSafely(Farmer farmer, Item? item)
    {
        if (_useHarmonyPatches)
        {
            // With Harmony patches, ActiveItem setter is automatically redirected
            farmer.ActiveItem = item;
        }
        else
        {
            // With SMAPI approach, use the helper manually
            _smapiHelper?.SetActiveItem(farmer, item, _inventoryManager);
        }
    }

    /// <summary>
    /// Example: Switch to a different inventory
    /// </summary>
    public void SwitchInventory(Farmer farmer, int inventoryIndex)
    {
        try
        {
            var oldIndex = _inventoryManager.GetActiveInventoryIndex(farmer);
            _inventoryManager.SetActiveInventoryIndex(farmer, inventoryIndex);
            
            _monitor.Log($"Switched from inventory {oldIndex} to {inventoryIndex}", LogLevel.Debug);

            // If using SMAPI approach, you might need to trigger UI updates manually
            if (!_useHarmonyPatches)
            {
                // Example: Notify game that toolbar needs refresh
                // This would be specific to your mod's integration needs
                RefreshInventoryUI(farmer);
            }
        }
        catch (Exception ex)
        {
            _monitor.Log($"Error switching inventory: {ex.Message}", LogLevel.Error);
        }
    }

    /// <summary>
    /// Example: Add item to specific inventory
    /// </summary>
    public bool AddItemToInventory(Farmer farmer, Item item, int inventoryIndex)
    {
        try
        {
            var inventory = _inventoryManager.GetInventory(farmer, inventoryIndex);
            if (inventory == null)
            {
                _monitor.Log($"Inventory {inventoryIndex} does not exist", LogLevel.Warn);
                return false;
            }

            // Find first empty slot
            for (int i = 0; i < inventory.Count; i++)
            {
                if (inventory[i] == null)
                {
                    inventory[i] = item;
                    _monitor.Log($"Added {item.DisplayName} to inventory {inventoryIndex} slot {i}", LogLevel.Debug);
                    return true;
                }
            }

            _monitor.Log($"Inventory {inventoryIndex} is full", LogLevel.Warn);
            return false;
        }
        catch (Exception ex)
        {
            _monitor.Log($"Error adding item to inventory: {ex.Message}", LogLevel.Error);
            return false;
        }
    }

    /// <summary>
    /// Example: Get all items across all inventories
    /// </summary>
    public List<Item> GetAllItems(Farmer farmer)
    {
        var allItems = new List<Item>();

        try
        {
            int inventoryCount = _inventoryManager.GetInventoryCount(farmer);
            for (int inv = 0; inv < inventoryCount; inv++)
            {
                var inventory = _inventoryManager.GetInventory(farmer, inv);
                if (inventory != null)
                {
                    foreach (var item in inventory)
                    {
                        if (item != null)
                        {
                            allItems.Add(item);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _monitor.Log($"Error getting all items: {ex.Message}", LogLevel.Error);
        }

        return allItems;
    }

    /// <summary>
    /// Example integration with vertical toolbar mod
    /// This shows how you might modify CurrentToolIndex handling
    /// </summary>
    public void HandleToolbarShift(Farmer farmer, bool shiftRight)
    {
        try
        {
            // Get current state
            int currentIndex = farmer.CurrentToolIndex;
            int currentInventory = _inventoryManager.GetActiveInventoryIndex(farmer);
            int inventoryCount = _inventoryManager.GetInventoryCount(farmer);

            // Handle current item actions (preserve original logic)
            var currentItem = GetCurrentItemSafely(farmer);
            if (currentItem != null)
            {
                currentItem.actionWhenStopBeingHeld(farmer);
            }

            // Determine new inventory/index based on shift direction
            int newInventory = currentInventory;
            int newIndex = currentIndex;

            if (shiftRight)
            {
                newIndex++;
                var currentInventorySize = _inventoryManager.GetInventory(farmer, currentInventory)?.Count ?? 0;
                if (newIndex >= currentInventorySize)
                {
                    // Wrap to next inventory
                    newInventory = (currentInventory + 1) % inventoryCount;
                    newIndex = 0;
                }
            }
            else
            {
                newIndex--;
                if (newIndex < 0)
                {
                    // Wrap to previous inventory
                    newInventory = currentInventory > 0 ? currentInventory - 1 : inventoryCount - 1;
                    var prevInventorySize = _inventoryManager.GetInventory(farmer, newInventory)?.Count ?? 0;
                    newIndex = prevInventorySize - 1;
                }
            }

            // Apply changes
            if (newInventory != currentInventory)
            {
                _inventoryManager.SetActiveInventoryIndex(farmer, newInventory);
            }

            // Update tool index (this will trigger patches if using Harmony)
            farmer.CurrentToolIndex = newIndex;

            // Handle new item actions
            var newItem = GetCurrentItemSafely(farmer);
            if (newItem != null)
            {
                newItem.actionWhenBeingHeld(farmer);
            }

            _monitor.Log($"Shifted toolbar: inventory {currentInventory} -> {newInventory}, index {currentIndex} -> {newIndex}", LogLevel.Debug);
        }
        catch (Exception ex)
        {
            _monitor.Log($"Error handling toolbar shift: {ex.Message}", LogLevel.Error);
        }
    }

    private void RefreshInventoryUI(Farmer farmer)
    {
        // This is where you would implement UI refresh logic
        // for the SMAPI approach that doesn't automatically update
        _monitor.Log("Refreshing inventory UI (placeholder)", LogLevel.Debug);
    }
}