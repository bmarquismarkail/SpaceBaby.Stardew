using System.Collections.Generic;
using StardewValley;

namespace VerticalToolbar
{
    public interface IVerticalToolbarAPI
    {
        /// <summary>Get the current vertical toolbar instance</summary>
        Framework.VerticalToolBar GetToolbar();
        
        /// <summary>Switch to a specific inventory</summary>
        void SwitchInventory(int inventoryIndex);
        
        /// <summary>Get current active inventory index</summary>
        int GetActiveInventoryIndex();
    }
}