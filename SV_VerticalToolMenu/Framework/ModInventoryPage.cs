using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VerticalToolbar.Framework
{
    internal class ModInventoryPage : StardewValley.Menus.InventoryPage
    {
        private readonly VerticalToolBar verticalToolBar;

        public ModInventoryPage(int x, int y, int width, int height)
            : base(x, y, width, height) => verticalToolBar = new VerticalToolBar(
                Orientation.LeftOfToolbar, 5, true)
            {
                xPositionOnScreen = this.xPositionOnScreen - IClickableMenu.spaceToClearSideBorder - IClickableMenu.borderWidth * 2,
                yPositionOnScreen = this.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder - IClickableMenu.borderWidth / 2 + 4
            };

        public override void performHoverAction(int x, int y)
        {
            verticalToolBar.performHoverAction(x, y);
            base.performHoverAction(x, y);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            // Handle clicks on vertical toolbar buttons
            if (HandleVerticalToolbarClick(x, y))
                return;
                
            // Handle organize button click
            if (HandleOrganizeButtonClick(x, y))
                return;

            // Fall back to base implementation
            base.receiveLeftClick(x, y, playSound);
        }
        
        private bool HandleVerticalToolbarClick(int x, int y)
        {
            Item heldItem = Game1.player.CursorSlotItem;
            
            foreach (ClickableComponent button in verticalToolBar.buttons)
            {
                if (!button.containsPoint(x, y))
                    continue;
                    
                int slotIndex = Convert.ToInt32(button.name);
                Item inventoryItem = Game1.player.Items[slotIndex];
                
                // Handle case: Adding or stacking items
                if (heldItem != null)
                {
                    if (HandleItemPlacement(heldItem, slotIndex, inventoryItem))
                        return true;
                }
                // Handle case: Picking up an item
                else if (inventoryItem != null)
                {
                    Game1.player.CursorSlotItem = inventoryItem;
                    Utility.removeItemFromInventory(slotIndex, Game1.player.Items);
                    return true;
                }
            }
            
            return false;
        }
        
        private static bool HandleItemPlacement(Item heldItem, int slotIndex, Item inventoryItem)
        {
            // Adding to empty slot or stacking
            if (inventoryItem == null || inventoryItem.canStackWith(heldItem))
            {
                if (Game1.player.CurrentToolIndex == slotIndex)
                    heldItem.actionWhenBeingHeld(Game1.player);
                    
                Utility.addItemToInventory(heldItem, slotIndex, Game1.player.Items);
                Game1.player.CursorSlotItem = null;
                Game1.playSound("stoneStep");
                return true;
            }
            // Swapping items
            else if (inventoryItem != null)
            {
                Item swapItem = Game1.player.CursorSlotItem;
                Game1.player.CursorSlotItem = inventoryItem;
                Utility.addItemToInventory(swapItem, slotIndex, Game1.player.Items);
                return true;
            }
            
            return false;
        }
        
        private bool HandleOrganizeButtonClick(int x, int y)
        {
            if (!this.organizeButton.containsPoint(x, y))
                return false;
                
            List<Item> items = Game1.player.Items.ToList();
            items.Sort(0, Game1.player.MaxItems, null);
            items.Reverse(0, Game1.player.MaxItems);
            Game1.player.setInventory(items);
            Game1.playSound("Ship");
            return true;
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            if (verticalToolBar.isWithinBounds(x, y))
            {
                Item heldItem = Game1.player.CursorSlotItem;
                Game1.player.CursorSlotItem = verticalToolBar.RightClick(x, y, heldItem, playSound);
                return;
            }
            base.receiveRightClick(x, y, playSound);
        }

        public override void draw(Microsoft.Xna.Framework.Graphics.SpriteBatch b)
        {
            for (int index = 0; index < 5; ++index)
                verticalToolBar.buttons[index].bounds = new Rectangle(
                            verticalToolBar.xPositionOnScreen + IClickableMenu.spaceToClearSideBorder,
                            verticalToolBar.yPositionOnScreen + IClickableMenu.spaceToClearSideBorder + (index * Game1.tileSize),
                            Game1.tileSize,
                            Game1.tileSize);
            verticalToolBar.draw(b);
            base.draw(b);
            verticalToolBar.drawToolTip(b);
        }
    }
}
