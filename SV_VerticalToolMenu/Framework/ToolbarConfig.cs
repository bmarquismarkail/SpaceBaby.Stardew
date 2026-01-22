namespace VerticalToolbar.Framework
{
    public class ToolbarConfig
    {
        public bool UseMultiInventorySystem { get; set; } = true;
        public int SlotsPerInventory { get; set; } = 5;
        public bool AllowInventorySwitching { get; set; } = true;
        public int MaxInventories { get; set; } = 4;
        public bool ShowInventoryIndicator { get; set; } = true;
    }
}