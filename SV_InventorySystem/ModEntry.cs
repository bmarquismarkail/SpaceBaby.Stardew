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

        // Use Harmony patches to intercept CurrentItem/ActiveItem (required for multi-inventory)
        if (_config.UseHarmonyPatches)
        {
            this.Monitor.Log("Using Harmony patches for multi-inventory system (required for tool use)", LogLevel.Info);
            InitializeHarmonyApproach();
        }
        else
        {
            this.Monitor.Log("Using SMAPI reflection approach for multi-inventory system (validation only)", LogLevel.Info);
            InitializeSmapiApproach();
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
        // Defer validation until a save is loaded; Game1.player is null during Entry
        this.Monitor.Log("Using SMAPI reflection approach for multi-inventory system (validation deferred until save load)", LogLevel.Info);
    }

    private void OnGameLaunched(object? sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
    {
        this.Monitor.Log($"Multi-inventory system initialized using {(_config?.UseHarmonyPatches == true ? "Harmony patches" : "SMAPI reflection")}", LogLevel.Info);
    }

    private void OnSaveLoaded(object? sender, StardewModdingAPI.Events.SaveLoadedEventArgs e)
    {
        // Ensure additional inventories exist for the player
        if (_inventoryManager != null && Game1.player != null)
        {
            int additionalInventories = Math.Max(0, _config?.DefaultAdditionalInventories ?? 1);
            int inventorySize = Math.Max(0, _config?.AdditionalInventorySize ?? 36);

            _inventoryManager.EnsureAdditionalInventories(Game1.player, additionalInventories, inventorySize);
            this.Monitor.Log($"Ensured additional inventories (count={additionalInventories}, size={inventorySize}). Player now has {_inventoryManager.GetInventoryCount(Game1.player)} inventories", LogLevel.Info);

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
    /// Expose the multi-inventory manager to other mods via SMAPI GetApi.
    /// </summary>
    public override object? GetApi()
    {
        return _inventoryManager;
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
    /// HARMONY PATCHES are required for CurrentItem/ActiveItem to work with multi-inventories
    /// SMAPI reflection is only for validation and helper methods
    /// </summary>
    public bool UseHarmonyPatches { get; set; } = true;

    /// <summary>
    /// Number of additional inventories to create by default
    /// </summary>
    public int DefaultAdditionalInventories { get; set; } = 1;

    /// <summary>
    /// Size of each additional inventory
    /// </summary>
    public int AdditionalInventorySize { get; set; } = 36;
}
