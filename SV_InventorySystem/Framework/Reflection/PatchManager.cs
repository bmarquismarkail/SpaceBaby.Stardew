using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace SV_InventorySystem.Framework.Reflection;

/// <summary>
/// Manages the initialization and application of Harmony patches for the multi-inventory system
/// </summary>
public class PatchManager
{
    private readonly IMonitor _monitor;
    private readonly Harmony _harmony;
    private readonly IMultiInventoryManager _inventoryManager;
    private bool _patchesApplied = false;

    public PatchManager(IMonitor monitor, string modId, IMultiInventoryManager inventoryManager)
    {
        _monitor = monitor;
        _harmony = new Harmony(modId);
        _inventoryManager = inventoryManager;
    }

    /// <summary>
    /// Applies all necessary patches for the multi-inventory system
    /// </summary>
    public void ApplyPatches()
    {
        if (_patchesApplied)
        {
            _monitor.Log("Patches already applied, skipping", LogLevel.Warn);
            return;
        }

        try
        {
            // Initialize the patches with dependencies
            FarmerPatches.Initialize(_monitor, _inventoryManager);

            // Manually patch each method explicitly
            var farmerType = typeof(Farmer);
            
            // Patch CurrentItem getter
            var currentItemGetter = AccessTools.Property(farmerType, "CurrentItem")?.GetGetMethod();
            if (currentItemGetter != null)
            {
                _harmony.Patch(
                    currentItemGetter,
                    prefix: new HarmonyMethod(typeof(FarmerPatches), nameof(FarmerPatches.CurrentItem_Getter_Prefix))
                );
                _monitor.Log("Patched CurrentItem getter", LogLevel.Debug);
            }

            // Patch ActiveItem getter
            var activeItemGetter = AccessTools.Property(farmerType, "ActiveItem")?.GetGetMethod();
            if (activeItemGetter != null)
            {
                _harmony.Patch(
                    activeItemGetter,
                    prefix: new HarmonyMethod(typeof(FarmerPatches), nameof(FarmerPatches.ActiveItem_Getter_Prefix))
                );
                _monitor.Log("Patched ActiveItem getter", LogLevel.Debug);
            }

            // Patch ActiveItem setter
            var activeItemSetter = AccessTools.Property(farmerType, "ActiveItem")?.GetSetMethod();
            if (activeItemSetter != null)
            {
                _harmony.Patch(
                    activeItemSetter,
                    prefix: new HarmonyMethod(typeof(FarmerPatches), nameof(FarmerPatches.ActiveItem_Setter_Prefix))
                );
                _monitor.Log("Patched ActiveItem setter", LogLevel.Debug);
            }

            // Patch CurrentToolIndex setter
            var currentToolIndexSetter = AccessTools.Property(farmerType, "CurrentToolIndex")?.GetSetMethod();
            if (currentToolIndexSetter != null)
            {
                _harmony.Patch(
                    currentToolIndexSetter,
                    postfix: new HarmonyMethod(typeof(FarmerPatches), nameof(FarmerPatches.CurrentToolIndex_Setter_Postfix))
                );
                _monitor.Log("Patched CurrentToolIndex setter", LogLevel.Debug);
            }

            _patchesApplied = true;
            _monitor.Log("Multi-inventory patches applied successfully", LogLevel.Info);
        }
        catch (Exception ex)
        {
            _monitor.Log($"Failed to apply patches: {ex}", LogLevel.Error);
            throw;
        }
    }

    /// <summary>
    /// Removes all patches applied by this manager
    /// </summary>
    public void RemovePatches()
    {
        if (!_patchesApplied)
        {
            _monitor.Log("No patches to remove", LogLevel.Debug);
            return;
        }

        try
        {
            _harmony.UnpatchAll(_harmony.Id);
            _patchesApplied = false;
            _monitor.Log("Multi-inventory patches removed successfully", LogLevel.Info);
        }
        catch (Exception ex)
        {
            _monitor.Log($"Failed to remove patches: {ex}", LogLevel.Error);
        }
    }

    /// <summary>
    /// Validates that the patches can be applied safely
    /// </summary>
    public bool ValidatePatches()
    {
        try
        {
            // Check if required types and methods exist
            var farmerType = typeof(Farmer);
            var currentItemProperty = farmerType.GetProperty("CurrentItem");
            var activeItemProperty = farmerType.GetProperty("ActiveItem");
            var currentToolIndexProperty = farmerType.GetProperty("CurrentToolIndex");

            if (currentItemProperty == null)
            {
                _monitor.Log("CurrentItem property not found on Farmer class", LogLevel.Error);
                return false;
            }

            if (activeItemProperty == null)
            {
                _monitor.Log("ActiveItem property not found on Farmer class", LogLevel.Error);
                return false;
            }

            if (currentToolIndexProperty == null)
            {
                _monitor.Log("CurrentToolIndex property not found on Farmer class", LogLevel.Error);
                return false;
            }

            // Check for _itemStowed field used in patches
            var itemStowedField = farmerType.GetField("_itemStowed", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (itemStowedField == null)
            {
                _monitor.Log("_itemStowed field not found on Farmer class", LogLevel.Error);
                return false;
            }

            _monitor.Log("Patch validation successful", LogLevel.Debug);
            return true;
        }
        catch (Exception ex)
        {
            _monitor.Log($"Patch validation failed: {ex}", LogLevel.Error);
            return false;
        }
    }

    /// <summary>
    /// Gets information about applied patches for debugging
    /// </summary>
    public string GetPatchInfo()
    {
        if (!_patchesApplied)
        {
            return "No patches applied";
        }

        var patches = _harmony.GetPatchedMethods().ToList();
        return $"Applied patches to {patches.Count} methods: {string.Join(", ", patches.Select(m => m.Name))}";
    }
}