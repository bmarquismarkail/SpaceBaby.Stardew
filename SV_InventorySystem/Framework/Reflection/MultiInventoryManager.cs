using StardewModdingAPI;
using StardewValley;
using System.Collections.Generic;

namespace SV_InventorySystem.Framework.Reflection;

/// <summary>
/// Default implementation of multi-inventory manager
/// </summary>
public class MultiInventoryManager : IMultiInventoryManager
{
    private readonly IMonitor _monitor;
    private readonly Dictionary<long, FarmerInventoryData> _farmerInventories;

    public MultiInventoryManager(IMonitor monitor)
    {
        _monitor = monitor;
        _farmerInventories = new Dictionary<long, FarmerInventoryData>();
    }

    private FarmerInventoryData GetOrCreateFarmerData(Farmer farmer)
    {
        if (!_farmerInventories.TryGetValue(farmer.UniqueMultiplayerID, out var data))
        {
            data = new FarmerInventoryData();
            _farmerInventories[farmer.UniqueMultiplayerID] = data;
        }
        return data;
    }

    public Item? GetCurrentItem(Farmer farmer)
    {
        var data = GetOrCreateFarmerData(farmer);
        var (inventoryIndex, localIndex) = TranslateGlobalIndex(farmer, farmer.CurrentToolIndex) ?? (0, farmer.CurrentToolIndex);
        
        if (inventoryIndex == 0)
        {
            // Use original inventory for index 0
            if (farmer.CurrentToolIndex >= farmer.Items.Count)
                return null;
            return farmer.Items[farmer.CurrentToolIndex];
        }
        
        var inventory = GetInventory(farmer, inventoryIndex);
        if (inventory == null || localIndex >= inventory.Count)
            return null;
            
        return inventory[localIndex];
    }

    public int GetTotalInventorySize(Farmer farmer)
    {
        int total = farmer.Items.Count; // Original inventory

        foreach (var inventory in GetOrCreateFarmerData(farmer).Inventories)
        {
            total += inventory.Count;
        }

        return total;
    }

    public bool RemoveItem(Farmer farmer, Item item)
    {
        var data = GetOrCreateFarmerData(farmer);
        
        // Try to remove from original inventory first
        for (int i = 0; i < farmer.Items.Count; i++)
        {
            if (farmer.Items[i] == item)
            {
                farmer.Items[i] = null;
                return true;
            }
        }
        
        // Try to remove from additional inventories
        for (int invIdx = 0; invIdx < data.Inventories.Count; invIdx++)
        {
            var inventory = data.Inventories[invIdx];
            for (int i = 0; i < inventory.Count; i++)
            {
                if (inventory[i] == item)
                {
                    inventory[i] = null;
                    return true;
                }
            }
        }
        
        return false;
    }

    public bool AddItemAtIndex(Farmer farmer, Item item, int index)
    {
        var (inventoryIndex, localIndex) = TranslateGlobalIndex(farmer, index) ?? (0, index);
        
        if (inventoryIndex == 0)
        {
            // Add to original inventory
            if (localIndex >= farmer.Items.Count)
                return false;
            farmer.Items[localIndex] = item;
            return true;
        }
        
        var inventory = GetInventory(farmer, inventoryIndex);
        if (inventory == null || localIndex >= inventory.Count)
            return false;
            
        inventory[localIndex] = item;
        return true;
    }

    public void OnToolIndexChanged(Farmer farmer, int newIndex)
    {
        // Handle any special logic when tool index changes
        var (inventoryIndex, localIndex) = TranslateGlobalIndex(farmer, newIndex) ?? (0, newIndex);
        var data = GetOrCreateFarmerData(farmer);
        
        // Auto-switch active inventory if needed
        if (inventoryIndex != data.ActiveInventoryIndex)
        {
            SetActiveInventoryIndex(farmer, inventoryIndex);
        }
    }

    public int GetActiveInventoryIndex(Farmer farmer)
    {
        var data = GetOrCreateFarmerData(farmer);
        return data.ActiveInventoryIndex;
    }

    public void SetActiveInventoryIndex(Farmer farmer, int inventoryIndex)
    {
        var data = GetOrCreateFarmerData(farmer);
        if (inventoryIndex >= 0 && inventoryIndex < GetInventoryCount(farmer))
        {
            data.ActiveInventoryIndex = inventoryIndex;
        }
    }

    public int GetInventoryCount(Farmer farmer)
    {
        return GetOrCreateFarmerData(farmer).Inventories.Count + 1; // include base inventory
    }

    public IList<Item?>? GetInventory(Farmer farmer, int inventoryIndex)
    {
        if (inventoryIndex == 0)
        {
            return farmer.Items;
        }

        var data = GetOrCreateFarmerData(farmer);
        int adjustedIndex = inventoryIndex - 1;

        if (adjustedIndex >= 0 && adjustedIndex < data.Inventories.Count)
        {
            return data.Inventories[adjustedIndex];
        }

        return null;
    }

    public (int inventoryIndex, int localIndex)? TranslateGlobalIndex(Farmer farmer, int globalIndex)
    {
        if (globalIndex < 0)
            return null;

        var data = GetOrCreateFarmerData(farmer);
        int currentOffset = 0;
        int totalInventories = data.Inventories.Count + 1; // include base inventory at index 0

        for (int invIdx = 0; invIdx < totalInventories; invIdx++)
        {
            var inventory = invIdx == 0 ? farmer.Items : data.Inventories[invIdx - 1];
            int inventorySize = inventory.Count;

            if (globalIndex < currentOffset + inventorySize)
            {
                return (invIdx, globalIndex - currentOffset);
            }

            currentOffset += inventorySize;
        }

        return null;
    }

    /// <summary>
    /// Adds a new inventory for the farmer
    /// </summary>
    public void AddInventory(Farmer farmer, int size = 36)
    {
        var data = GetOrCreateFarmerData(farmer);
        var newInventory = new List<Item?>(new Item?[size]);
        data.Inventories.Add(newInventory);
    }

    /// <summary>
    /// Removes an inventory for the farmer
    /// </summary>
    public bool RemoveInventory(Farmer farmer, int inventoryIndex)
    {
        if (inventoryIndex <= 0) // Cannot remove the original inventory
            return false;
            
        var data = GetOrCreateFarmerData(farmer);
        int adjustedIndex = inventoryIndex - 1;
        if (adjustedIndex >= data.Inventories.Count)
            return false;
            
        data.Inventories.RemoveAt(adjustedIndex);
        
        // Adjust active inventory index if needed
        if (data.ActiveInventoryIndex >= inventoryIndex)
        {
            data.ActiveInventoryIndex = Math.Max(0, data.ActiveInventoryIndex - 1);
        }
        
        return true;
    }

    private class FarmerInventoryData
    {
        public List<IList<Item?>> Inventories { get; } = new();
        public int ActiveInventoryIndex { get; set; } = 0;

        public FarmerInventoryData()
        {
            // Index 0 will always represent the original farmer inventory
            // Additional inventories will be added at indices 1, 2, 3, etc.
        }
    }
}