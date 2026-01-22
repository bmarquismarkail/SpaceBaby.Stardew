using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Menus;
using SV_InventorySystem.Framework.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VerticalToolbar.Framework
{
    internal class ModInventoryPage : StardewValley.Menus.InventoryPage
    {
        private readonly VerticalToolBar verticalToolBar;
        private readonly IMultiInventoryManager? _inventoryManager;

        public ModInventoryPage(int x, int y, int width, int height, IMultiInventoryManager? inventoryManager = null)
            : base(x, y, width, height)
        {
            _inventoryManager = inventoryManager;
            verticalToolBar = new VerticalToolBar(
                Orientation.LeftOfToolbar,
                VerticalToolBar.NUM_BUTTONS,
                inventoryManager,
                true)
            {
                xPositionOnScreen = this.xPositionOnScreen - IClickableMenu.spaceToClearSideBorder - IClickableMenu.borderWidth * 2,
                yPositionOnScreen = this.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder - IClickableMenu.borderWidth / 2 + 4
            };
        }

        public override void performHoverAction(int x, int y)
        {
            verticalToolBar.performHoverAction(x, y);
            base.performHoverAction(x, y);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            Item heldItem = Game1.player.CursorSlotItem;
            foreach (ClickableComponent button in verticalToolBar.buttons)
            {
                if (!button.containsPoint(x, y))
                    continue;

                int slotIndex = Convert.ToInt32(button.name);
                if (!TryResolveSlot(slotIndex, out var inventory, out var localIndex))
                    continue;

                Item slotItem = inventory[localIndex];

                if (heldItem != null)
                {
                    if (slotItem == null)
                    {
                        inventory[localIndex] = heldItem;
                        Game1.player.CursorSlotItem = null;
                        Game1.playSound("stoneStep");
                        return;
                    }

                    if (slotItem.canStackWith(heldItem))
                    {
                        int maxAdd = slotItem.maximumStackSize() - slotItem.Stack;
                        if (maxAdd > 0)
                        {
                            int toMove = Math.Min(maxAdd, heldItem.Stack);
                            slotItem.Stack += toMove;
                            heldItem.Stack -= toMove;

                            if (heldItem.Stack <= 0)
                                Game1.player.CursorSlotItem = null;

                            Game1.playSound("stoneStep");
                        }
                        return;
                    }

                    // Swap items
                    Game1.player.CursorSlotItem = slotItem;
                    inventory[localIndex] = heldItem;
                    Game1.playSound("stoneStep");
                    return;
                }

                if (slotItem != null)
                {
                    Game1.player.CursorSlotItem = slotItem;
                    inventory[localIndex] = null;
                    Game1.playSound("dwop");
                    return;
                }
            }
            if (this.organizeButton.containsPoint(x, y))
            {
                List<Item> items = Game1.player.Items.ToList();
                items.Sort(0, Game1.player.MaxItems, null);
                items.Reverse(0, Game1.player.MaxItems);
                Game1.player.setInventory(items);
                Game1.playSound("Ship");
                return;
            }

            base.receiveLeftClick(x, y, true);
        }

        private bool TryResolveSlot(int globalIndex, out IList<Item?> inventory, out int localIndex)
        {
            inventory = null;
            localIndex = -1;

            if (_inventoryManager == null)
            {
                if (Game1.player.Items.Count > globalIndex)
                {
                    inventory = Game1.player.Items;
                    localIndex = globalIndex;
                    return true;
                }

                return false;
            }

            var mapping = _inventoryManager.TranslateGlobalIndex(Game1.player, globalIndex);
            if (mapping == null)
                return false;

            var targetInventory = _inventoryManager.GetInventory(Game1.player, mapping.Value.inventoryIndex);
            if (targetInventory == null)
                return false;

            if (mapping.Value.localIndex < 0 || mapping.Value.localIndex >= targetInventory.Count)
                return false;

            inventory = targetInventory;
            localIndex = mapping.Value.localIndex;
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
            for (int index = 0; index < VerticalToolBar.NUM_BUTTONS; ++index)
                verticalToolBar.buttons[index].bounds = new Rectangle(
                            //TODO: Use more reliable coordinates
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
