using StardewValley;

namespace SV_InventorySystem.Framework.Reflection;

/// <summary>
/// Interface for managing multiple inventories per farmer
/// </summary>
public interface IMultiInventoryManager
{
    /// <summary>
    /// Gets the current item for the farmer based on their current tool index and active inventory
    /// </summary>
    /// <param name="farmer">The farmer to get the current item for</param>
    /// <returns>The current item, or null if no item is selected</returns>
    Item? GetCurrentItem(Farmer farmer);

    /// <summary>
    /// Gets the total size of all inventories for the farmer
    /// </summary>
    /// <param name="farmer">The farmer to get inventory size for</param>
    /// <returns>Total number of inventory slots across all inventories</returns>
    int GetTotalInventorySize(Farmer farmer);

    /// <summary>
    /// Removes an item from the farmer's inventories
    /// </summary>
    /// <param name="farmer">The farmer to remove the item from</param>
    /// <param name="item">The item to remove</param>
    /// <returns>True if the item was removed, false otherwise</returns>
    bool RemoveItem(Farmer farmer, Item item);

    /// <summary>
    /// Adds an item to a specific index in the farmer's inventories
    /// </summary>
    /// <param name="farmer">The farmer to add the item to</param>
    /// <param name="item">The item to add</param>
    /// <param name="index">The index to add the item at</param>
    /// <returns>True if the item was added, false otherwise</returns>
    bool AddItemAtIndex(Farmer farmer, Item item, int index);

    /// <summary>
    /// Called when the farmer's CurrentToolIndex changes
    /// </summary>
    /// <param name="farmer">The farmer whose tool index changed</param>
    /// <param name="newIndex">The new tool index</param>
    void OnToolIndexChanged(Farmer farmer, int newIndex);

    /// <summary>
    /// Gets the active inventory index for the farmer
    /// </summary>
    /// <param name="farmer">The farmer to get active inventory for</param>
    /// <returns>The index of the currently active inventory</returns>
    int GetActiveInventoryIndex(Farmer farmer);

    /// <summary>
    /// Sets the active inventory index for the farmer
    /// </summary>
    /// <param name="farmer">The farmer to set active inventory for</param>
    /// <param name="inventoryIndex">The index of the inventory to make active</param>
    void SetActiveInventoryIndex(Farmer farmer, int inventoryIndex);

    /// <summary>
    /// Gets the number of inventories available for the farmer
    /// </summary>
    /// <param name="farmer">The farmer to get inventory count for</param>
    /// <returns>The number of inventories available</returns>
    int GetInventoryCount(Farmer farmer);

    /// <summary>
    /// Gets a specific inventory for the farmer
    /// </summary>
    /// <param name="farmer">The farmer to get inventory for</param>
    /// <param name="inventoryIndex">The index of the inventory to get</param>
    /// <returns>The inventory at the specified index, or null if index is invalid</returns>
    IList<Item?>? GetInventory(Farmer farmer, int inventoryIndex);

    /// <summary>
    /// Translates a global item index to a specific inventory and local index
    /// </summary>
    /// <param name="farmer">The farmer to translate index for</param>
    /// <param name="globalIndex">The global index across all inventories</param>
    /// <returns>Tuple of (inventoryIndex, localIndex) or null if invalid</returns>
    (int inventoryIndex, int localIndex)? TranslateGlobalIndex(Farmer farmer, int globalIndex);
}