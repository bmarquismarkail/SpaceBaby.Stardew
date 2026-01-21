using StardewModdingAPI;
using SV_InventorySystem.Framework.Reflection;
using StardewValley;

namespace SV_InventorySystem;

public class ModEntry : Mod
{
    private MultiInventoryManager? _inventoryManager;
    private PatchManager? _patchManager;
    private SmapiReflectionHelper? _smapiHelper;
    private ModConfig? _config;

    public override void Entry(IModHelper helper)
    {
        // Load configuration
        _config = this.Helper.ReadConfig<ModConfig>();

        // Initialize the multi-inventory manager
        _inventoryManager = new MultiInventoryManager(this.Monitor);

        // Initialize SMAPI reflection helper (primary approach)
        _smapiHelper = new SmapiReflectionHelper(this.Helper.Reflection, this.Monitor);

        // Use SMAPI reflection as the primary approach (better compatibility)
        if (!_config.UseHarmonyPatches)
        {
            this.Monitor.Log("Using SMAPI reflection approach for multi-inventory system (recommended)", LogLevel.Info);
            InitializeSmapiApproach();
        }
        else
        {
            this.Monitor.Log("Using Harmony patches for multi-inventory system (advanced)", LogLevel.Info);
            InitializeHarmonyApproach();
        }

        // Set up event handlers
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
    }

    private void InitializeHarmonyApproach()
    {
        try
        {
            // Initialize the patch manager
            _patchManager = new PatchManager(this.Monitor, this.ModManifest.UniqueID, _inventoryManager!);

            // Validate patches before applying
            if (!_patchManager.ValidatePatches())
            {
                this.Monitor.Log("Patch validation failed. Falling back to SMAPI approach.", LogLevel.Warn);
                _config!.UseHarmonyPatches = false;
                this.Helper.WriteConfig(_config);
                InitializeSmapiApproach();
                return;
            }

            // Apply the reflection patches
            _patchManager.ApplyPatches();

            // Log patch information for debugging
            this.Monitor.Log(_patchManager.GetPatchInfo(), LogLevel.Debug);
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Failed to initialize Harmony patches: {ex.Message}. Falling back to SMAPI approach.", LogLevel.Error);
            _config!.UseHarmonyPatches = false;
            this.Helper.WriteConfig(_config);
            InitializeSmapiApproach();
        }
    }

    private void InitializeSmapiApproach()
    {
        // Validate SMAPI reflection works properly
        if (_smapiHelper!.ValidateReflection(Game1.player))
        {
            this.Monitor.Log("SMAPI reflection helper initialized and validated successfully.", LogLevel.Info);
            this.Monitor.Log("Use SmapiHelper.GetCurrentItem/GetActiveItem/SetActiveItem methods for multi-inventory access.", LogLevel.Info);
        }
        else
        {
            this.Monitor.Log("SMAPI reflection validation failed. Some features may not work correctly.", LogLevel.Warn);
        }
    }

    private void OnGameLaunched(object? sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
    {
        this.Monitor.Log($"Multi-inventory system initialized using {(_config?.UseHarmonyPatches == true ? "Harmony patches" : "SMAPI reflection")}", LogLevel.Info);
    }

    private void OnSaveLoaded(object? sender, StardewModdingAPI.Events.SaveLoadedEventArgs e)
    {
        // Example: Add an additional inventory for the player
        if (_inventoryManager != null && Game1.player != null)
        {
            _inventoryManager.AddInventory(Game1.player, 36); // Add a second inventory with 36 slots
            this.Monitor.Log($"Added additional inventory. Player now has {_inventoryManager.GetInventoryCount(Game1.player)} inventories", LogLevel.Info);

            // Test SMAPI reflection validation if not using Harmony
            if (!(_config?.UseHarmonyPatches ?? true) && _smapiHelper != null)
            {
                if (_smapiHelper.ValidateReflection(Game1.player))
                {
                    this.Monitor.Log("SMAPI reflection approach validated successfully", LogLevel.Info);
                    
                    // Example of manual usage
                    var currentItem = _smapiHelper.GetCurrentItem(Game1.player, _inventoryManager);
                    this.Monitor.Log($"Current item via SMAPI helper: {currentItem?.DisplayName ?? "null"}", LogLevel.Debug);
                }
            }
        }
    }

    /// <summary>
    /// Public API for other mods to access the multi-inventory manager
    /// </summary>
    public IMultiInventoryManager? GetMultiInventoryManager() => _inventoryManager;

    /// <summary>
    /// Public API for other mods to access the SMAPI reflection helper
    /// </summary>
    public SmapiReflectionHelper? GetSmapiHelper() => _smapiHelper;

    /// <summary>
    /// Returns whether the mod is currently using SMAPI reflection (true) or Harmony patches (false)
    /// </summary>
    public bool IsUsingSmapiReflection() => !(_config?.UseHarmonyPatches ?? false);

    /// <summary>
    /// Public API for other mods to access the SMAPI reflection helper
    /// </summary>
    public SmapiReflectionHelper? GetSmapiReflectionHelper() => _smapiHelper;

    /// <summary>
    /// Called when the mod is being disposed
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _patchManager?.RemovePatches();
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Configuration class for the mod
/// </summary>
public class ModConfig
{
    /// <summary>
    /// Whether to use Harmony patches (true) or SMAPI reflection approach (false)
    /// SMAPI reflection is the default and recommended approach for better compatibility
    /// Harmony patches provide automatic interception but may have compatibility issues
    /// </summary>
    public bool UseHarmonyPatches { get; set; } = false;

    /// <summary>
    /// Number of additional inventories to create by default
    /// </summary>
    public int DefaultAdditionalInventories { get; set; } = 1;

    /// <summary>
    /// Size of each additional inventory
    /// </summary>
    public int AdditionalInventorySize { get; set; } = 36;
}
