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
    private bool _isOperational;

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
            this.Monitor.Log("UseHarmonyPatches is false. The reflection-only mode can't provide safe multi-inventory behavior, so the mod is disabled for this session.", LogLevel.Error);
        }

        // Set up event handlers
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
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
                this.Monitor.Log("Required Harmony patch validation failed. Multi-inventory behavior is disabled for this session.", LogLevel.Error);
                return;
            }

            // Apply the reflection patches
            _patchManager.ApplyPatches();
            _isOperational = true;

            // Log patch information for debugging
            this.Monitor.Log(_patchManager.GetPatchInfo(), LogLevel.Debug);
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Failed to initialize required Harmony patches: {ex.Message}. Multi-inventory behavior is disabled for this session.", LogLevel.Error);
        }
    }

    private void OnGameLaunched(object? sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
    {
        this.Monitor.Log(_isOperational
            ? "Multi-inventory system initialized using Harmony patches."
            : "Multi-inventory system is disabled because its required Harmony patches aren't active.",
            _isOperational ? LogLevel.Info : LogLevel.Error);
    }

    private void OnSaveLoaded(object? sender, StardewModdingAPI.Events.SaveLoadedEventArgs e)
    {
        // Ensure additional inventories exist for the player
        if (_isOperational && _inventoryManager != null && Game1.player != null)
        {
            int additionalInventories = Math.Max(0, _config?.DefaultAdditionalInventories ?? 1);
            int inventorySize = Math.Max(0, _config?.AdditionalInventorySize ?? 36);

            _inventoryManager.EnsureAdditionalInventories(Game1.player, additionalInventories, inventorySize);
            this.Monitor.Log($"Ensured additional inventories (count={additionalInventories}, size={inventorySize}). Player now has {_inventoryManager.GetInventoryCount(Game1.player)} inventories", LogLevel.Info);
        }
    }

    /// <summary>
    /// Expose the multi-inventory manager to other mods via SMAPI GetApi.
    /// </summary>
    public override object? GetApi()
    {
        return _isOperational ? _inventoryManager : null;
    }

    private void OnReturnedToTitle(object? sender, StardewModdingAPI.Events.ReturnedToTitleEventArgs e)
    {
        _inventoryManager?.ClearTransientState();
    }

    /// <summary>
    /// Public API for other mods to access the multi-inventory manager
    /// </summary>
    public IMultiInventoryManager? GetMultiInventoryManager() => _isOperational ? _inventoryManager : null;

    /// <summary>
    /// Public API for other mods to access the SMAPI reflection helper
    /// </summary>
    public SmapiReflectionHelper? GetSmapiHelper() => _smapiHelper;

    /// <summary>
    /// Compatibility shim retained for source consumers. The operational implementation always uses Harmony.
    /// </summary>
    public bool IsUsingSmapiReflection() => false;

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
    /// Legacy compatibility switch. Harmony patches are required for safe multi-inventory behavior;
    /// setting this to <c>false</c> disables the mod for that session.
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
