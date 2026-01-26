using StardewModdingAPI;
using StardewValley;
using StardewValley.Inventories;
using System;
using System.Collections.Generic;

namespace SV_InventorySystem.Framework.Reflection;

/// <summary>
/// Default implementation of multi-inventory manager
/// </summary>
public class MultiInventoryManager : IMultiInventoryManager
{
    private readonly IMonitor _monitor;
    private readonly Dictionary<long, FarmerInventoryData> _farmerInventories;

    /// <summary>
    /// Prefix used for per-farmer global inventories which persist extra inventories across saves.
    /// </summary>
    private const string GlobalInventoryPrefix = "SpaceBaby.SV_InventorySystem.Inventory";

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

    private static string GetGlobalInventoryId(long farmerId, int inventoryIndex)
    {
        return $"{GlobalInventoryPrefix}.{farmerId}.{inventoryIndex}";
    }

    private static bool TryParseGlobalInventoryId(string id, long farmerId, out int inventoryIndex)
    {
        string prefix = $"{GlobalInventoryPrefix}.{farmerId}.";
        if (!id.StartsWith(prefix, StringComparison.Ordinal))
        {
            inventoryIndex = -1;
            return false;
        }

        string suffix = id.Substring(prefix.Length);
        return int.TryParse(suffix, out inventoryIndex) && inventoryIndex > 0;
    }

    private static void EnsureInventorySize(IList<Item?> inventory, int size)
    {
        while (inventory.Count < size)
            inventory.Add(null);
    }

    private static List<string> GetAdditionalInventoryIds(Farmer farmer)
    {
        var list = new List<(int index, string id)>();
        foreach (string id in farmer.team.globalInventories.Keys)
        {
            if (TryParseGlobalInventoryId(id, farmer.UniqueMultiplayerID, out int index))
                list.Add((index, id));
        }

        list.Sort((a, b) => a.index.CompareTo(b.index));
        return list.Select(p => p.id).ToList();
    }

    public Item? GetCurrentItem(Farmer farmer)
    {
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
        int total = farmer.Items.Count;
        foreach (string id in GetAdditionalInventoryIds(farmer))
            total += farmer.team.GetOrCreateGlobalInventory(id).Count;
        return total;
    }

    public bool RemoveItem(Farmer farmer, Item item)
    {
        // Try to remove from original inventory first
        for (int i = 0; i < farmer.Items.Count; i++)
        {
            if (farmer.Items[i] == item)
            {
                farmer.Items[i] = null;
                return true;
            }
        }
        
        foreach (string id in GetAdditionalInventoryIds(farmer))
        {
            Inventory inventory = farmer.team.GetOrCreateGlobalInventory(id);
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
        return GetAdditionalInventoryIds(farmer).Count + 1;
    }

    public IList<Item?>? GetInventory(Farmer farmer, int inventoryIndex)
    {
        if (inventoryIndex == 0)
        {
            return farmer.Items;
        }

        int adjustedIndex = inventoryIndex - 1;
        List<string> ids = GetAdditionalInventoryIds(farmer);
        if (adjustedIndex < 0 || adjustedIndex >= ids.Count)
            return null;

        return farmer.team.GetOrCreateGlobalInventory(ids[adjustedIndex]);
    }

    public (int inventoryIndex, int localIndex)? TranslateGlobalIndex(Farmer farmer, int globalIndex)
    {
        if (globalIndex < 0)
            return null;

        int currentOffset = 0;
        List<string> additionalInventoryIds = GetAdditionalInventoryIds(farmer);
        int totalInventories = additionalInventoryIds.Count + 1; // include base inventory at index 0

        for (int invIdx = 0; invIdx < totalInventories; invIdx++)
        {
            IList<Item?> inventory = invIdx == 0 ? farmer.Items : farmer.team.GetOrCreateGlobalInventory(additionalInventoryIds[invIdx - 1]);
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
    /// Ensures the given number of additional inventories exist and are at least the given size.
    /// Extra inventories are persisted in <see cref="FarmerTeam.globalInventories"/>.
    /// </summary>
    public void EnsureAdditionalInventories(Farmer farmer, int additionalInventories, int size = 36)
    {
        if (additionalInventories < 0)
            additionalInventories = 0;

        if (size < 0)
            size = 0;

        for (int i = 1; i <= additionalInventories; i++)
        {
            Inventory inventory = farmer.team.GetOrCreateGlobalInventory(GetGlobalInventoryId(farmer.UniqueMultiplayerID, i));
            EnsureInventorySize(inventory, size);
        }

        foreach (string id in GetAdditionalInventoryIds(farmer))
        {
            Inventory inventory = farmer.team.GetOrCreateGlobalInventory(id);
            EnsureInventorySize(inventory, size);
        }
    }

    /// <summary>
    /// Removes an inventory for the farmer
    /// </summary>
    public bool RemoveInventory(Farmer farmer, int inventoryIndex)
    {
        if (inventoryIndex <= 0) // Cannot remove the original inventory
            return false;

        int adjustedIndex = inventoryIndex - 1;
        List<string> ids = GetAdditionalInventoryIds(farmer);
        if (adjustedIndex < 0 || adjustedIndex >= ids.Count)
            return false;

        string id = ids[adjustedIndex];
        if (!farmer.team.globalInventories.ContainsKey(id))
            return false;

        farmer.team.globalInventories.Remove(id);

        // Adjust active inventory index if needed
        var data = GetOrCreateFarmerData(farmer);
        if (data.ActiveInventoryIndex >= inventoryIndex)
        {
            data.ActiveInventoryIndex = Math.Max(0, data.ActiveInventoryIndex - 1);
        }
        
        return true;
    }

    private class FarmerInventoryData
    {
        public int ActiveInventoryIndex { get; set; } = 0;

        public FarmerInventoryData()
        {
        }
    }
}
