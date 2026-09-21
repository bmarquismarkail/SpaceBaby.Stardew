using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using Microsoft.Xna.Framework;

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
            ObjectPatches.Initialize(_monitor, _inventoryManager);

            // Manually patch each method explicitly
            var farmerType = typeof(Farmer);

            var updateCommon = AccessTools.Method(farmerType, "updateCommon",
                new[] { typeof(GameTime), typeof(GameLocation) })
                ?? throw new MissingMethodException("Farmer.updateCommon(GameTime, GameLocation) not found.");
            _harmony.Patch(updateCommon,
                postfix: new HarmonyMethod(typeof(FarmerPatches), nameof(FarmerPatches.UpdateCommon_Postfix)));
            _monitor.Log("Patched additional-inventory tool updates", LogLevel.Debug);
            
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

            // Patch CurrentTool setter, which writes directly through Farmer.Items in vanilla.
            var currentToolSetter = AccessTools.Property(farmerType, "CurrentTool")?.GetSetMethod();
            if (currentToolSetter != null)
            {
                _harmony.Patch(
                    currentToolSetter,
                    prefix: new HarmonyMethod(typeof(FarmerPatches), nameof(FarmerPatches.CurrentTool_Setter_Prefix))
                );
                _monitor.Log("Patched CurrentTool setter", LogLevel.Debug);
            }

            // Patch the one unguarded vanilla Farmer.Items[CurrentToolIndex] interaction.
            var scarecrowAction = AccessTools.Method(
                typeof(StardewValley.Object),
                "CheckForActionOnScarecrow",
                new[] { typeof(Farmer), typeof(bool) });
            if (scarecrowAction != null)
            {
                _harmony.Patch(
                    scarecrowAction,
                    prefix: new HarmonyMethod(typeof(ObjectPatches), nameof(ObjectPatches.CheckForActionOnScarecrow_Prefix))
                );
                _monitor.Log("Patched scarecrow hat interaction", LogLevel.Debug);
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
            if (AccessTools.Method(farmerType, "updateCommon",
                new[] { typeof(GameTime), typeof(GameLocation) }) == null)
            {
                _monitor.Log("updateCommon method not found on Farmer class", LogLevel.Error);
                return false;
            }
            var activeItemProperty = farmerType.GetProperty("ActiveItem");
            var currentToolIndexProperty = farmerType.GetProperty("CurrentToolIndex");
            var currentToolProperty = farmerType.GetProperty("CurrentTool");

            if (currentItemProperty?.GetGetMethod() == null)
            {
                _monitor.Log("CurrentItem getter not found on Farmer class", LogLevel.Error);
                return false;
            }

            if (activeItemProperty?.GetGetMethod() == null || activeItemProperty.GetSetMethod() == null)
            {
                _monitor.Log("ActiveItem getter or setter not found on Farmer class", LogLevel.Error);
                return false;
            }

            if (currentToolIndexProperty?.GetSetMethod() == null)
            {
                _monitor.Log("CurrentToolIndex setter not found on Farmer class", LogLevel.Error);
                return false;
            }

            if (currentToolProperty?.GetSetMethod() == null)
            {
                _monitor.Log("CurrentTool setter not found on Farmer class", LogLevel.Error);
                return false;
            }

            var scarecrowAction = AccessTools.Method(
                typeof(StardewValley.Object),
                "CheckForActionOnScarecrow",
                new[] { typeof(Farmer), typeof(bool) });
            if (scarecrowAction == null)
            {
                _monitor.Log("CheckForActionOnScarecrow method not found on Object class", LogLevel.Error);
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
