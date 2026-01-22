using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using SV_InventorySystem.Framework.Reflection;
using SObject = StardewValley.Object;

namespace VerticalToolbar.Framework
{
    public enum Orientation
    {
        LeftOfToolbar,
        RightOfToolbar,
        BottomLeft,
        BottomRight
    }

    public class VerticalToolBar : IClickableMenu
    {
        public List<ClickableComponent> buttons = new List<ClickableComponent>();
        public static int NUM_BUTTONS = 5;
        public Orientation orientation;
        private string hoverTitle = "";
        private float transparency = 1f;
        public Rectangle toolbarTextSource = new Rectangle(0, 256, 60, 60);
        public int numToolsInToolbar = 0;
        private Item hoverItem;
        public bool forceDraw = false;
        private int baseMaxItems = Game1.player.MaxItems;
        private IMultiInventoryManager? _inventoryManager;
        private ToolbarConfig toolbarConfig = new ToolbarConfig();

        public VerticalToolBar(Orientation o, int numButtons = 5, IMultiInventoryManager inventoryManager = null, bool forceDraw = false)
            : base()
        {
            _inventoryManager = inventoryManager;
            orientation = o;
            NUM_BUTTONS = numButtons;
            this.forceDraw = forceDraw;
            getDimensions();
            // For compatibility with Bigger Backpack when not using a multi-inventory manager
            if (_inventoryManager == null)
            {
                int newInventory = baseMaxItems + VerticalToolBar.NUM_BUTTONS;
                for (int count = Game1.player.Items.Count; count < newInventory; count++)
                {
                    Game1.player.Items.Add(null);
                }
            }

            for (int index = 0; index < NUM_BUTTONS; ++index)
            {
                this.buttons.Add(
                    new ClickableComponent(
                        new Rectangle(
                            this.xPositionOnScreen + IClickableMenu.spaceToClearSideBorder,
                            this.yPositionOnScreen + IClickableMenu.spaceToClearSideBorder + (index * Game1.tileSize),
                            Game1.tileSize,
                            Game1.tileSize),
                        string.Concat(index + baseMaxItems)));
            }
        }

        public void DrawInventoryIndicator(SpriteBatch b, int inventoryIndex)
        {
            if (!toolbarConfig.ShowInventoryIndicator) return;

            string text = $"Inv {inventoryIndex + 1}";
            Vector2 textSize = Game1.smallFont.MeasureString(text);
            Vector2 position = new Vector2(
                this.xPositionOnScreen + (this.width - textSize.X) / 2,
                this.yPositionOnScreen - 35
            );

            // Draw shadow
            b.DrawString(Game1.smallFont, text, position + new Vector2(2, 2), Color.Black * this.transparency);
            // Draw text
            b.DrawString(Game1.smallFont, text, position, Color.Yellow * this.transparency);
        }

        public static Toolbar getToolbar()
        {
            return Game1.onScreenMenus.OfType<Toolbar>().FirstOrDefault();
        }

        public void getDimensions()
        {
            Rectangle dimensionRectangle;
            dimensionRectangle.Width = Game1.tileSize * 3 / 2;
            dimensionRectangle.Height = Game1.tileSize * NUM_BUTTONS + (Game1.tileSize / 2);

            switch (orientation)
            {
                case Orientation.LeftOfToolbar:
                    dimensionRectangle.X = (Game1.viewport.Width / 2 - 384 - 64) - (getInitialWidth() / 2);
                    dimensionRectangle.Y = Game1.viewport.Height - getInitialHeight();
                    break;
                case Orientation.RightOfToolbar:
                    dimensionRectangle.X = (Game1.viewport.Width / 2 - 384 - 64) + getToolbar().width - (getInitialWidth() / 2);
                    dimensionRectangle.Y = Game1.viewport.Height - getInitialHeight();
                    break;
                case Orientation.BottomLeft:
                    dimensionRectangle.X = IClickableMenu.spaceToClearSideBorder;
                    dimensionRectangle.Y = Game1.viewport.Height - getInitialHeight();
                    break;
                case Orientation.BottomRight:
                    dimensionRectangle.X = Game1.viewport.Width - (getInitialWidth() / 2) - IClickableMenu.spaceToClearSideBorder - getInitialWidth() - (Game1.showingHealth ? 64 : 0);
                    dimensionRectangle.Y = Game1.viewport.Height - getInitialHeight();
                    break;
                default:
                    throw new NotSupportedException("Error: Orientation Not Supported");
            }
            this.xPositionOnScreen = dimensionRectangle.X;
            this.yPositionOnScreen = dimensionRectangle.Y;
            this.width = dimensionRectangle.Width;
            this.height = dimensionRectangle.Height;

        }
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (Game1.player.UsingTool)
                return;
            foreach (ClickableComponent button in this.buttons)
            {
                if (!button.containsPoint(x, y))
                    continue;

                int slotIndex = Convert.ToInt32(button.name);
                Item item = GetItemAtSlot(slotIndex);

                if (item == null)
                    break;

                // Simply set the tool index and let Inventory System handle CurrentItem/ActiveItem
                Game1.player.CurrentToolIndex = slotIndex;
                _inventoryManager?.OnToolIndexChanged(Game1.player, slotIndex);

                if (playSound)
                {
                    if (item is Tool)
                        Game1.playSound("pickUpItem");
                    else
                    {
                        Game1.player.showCarrying();
                        Game1.playSound("pickUpItem");
                    }
                }

                break;
            }
        }



        public Item RightClick(int x, int y, Item toAddTo, bool playSound = true)
        {
            foreach (ClickableComponent button in this.buttons)
            {
                int slotIndex = Convert.ToInt32(button.name);
                if (!button.containsPoint(x, y))
                    continue;

                if (!TryResolveSlot(slotIndex, out var inventory, out var localIndex))
                    continue;

                Item slotItem = inventory[localIndex];
                if (slotItem == null)
                    continue;

                if (slotItem is Tool tool && (toAddTo == null || toAddTo is SObject) && tool.canThisBeAttached((SObject)toAddTo))
                    return tool.attach((SObject)toAddTo);

                if (toAddTo == null)
                {
                    if (slotItem.maximumStackSize() != -1)
                    {
                        if (slotIndex == Game1.player.CurrentToolIndex && slotItem.Stack == 1)
                            slotItem.actionWhenStopBeingHeld(Game1.player);

                        Item one = slotItem.getOne();
                        if (slotItem.Stack > 1 && Game1.isOneOfTheseKeysDown(Game1.oldKBState, new[] { new InputButton(Keys.LeftShift) }))
                        {
                            one.Stack = (int)Math.Ceiling(slotItem.Stack / 2.0);
                            slotItem.Stack = slotItem.Stack / 2;
                        }
                        else
                        {
                            if (slotItem.Stack == 1)
                                inventory[localIndex] = null;
                            else
                                --slotItem.Stack;
                        }

                        if (inventory[localIndex] != null && inventory[localIndex]!.Stack <= 0)
                            inventory[localIndex] = null;
                        if (playSound)
                            Game1.playSound("dwop");
                        return one;
                    }
                }
                else if (slotItem.canStackWith(toAddTo) && toAddTo.Stack < toAddTo.maximumStackSize())
                {
                    if (Game1.isOneOfTheseKeysDown(Game1.oldKBState, new[] { new InputButton(Keys.LeftShift) }))
                    {
                        toAddTo.Stack += (int)Math.Ceiling(slotItem.Stack / 2.0);
                        slotItem.Stack = slotItem.Stack / 2;
                    }
                    else
                    {
                        ++toAddTo.Stack;
                        --slotItem.Stack;
                    }
                    if (playSound)
                        Game1.playSound("dwop");
                    if (slotItem.Stack <= 0)
                    {
                        if (slotIndex == Game1.player.CurrentToolIndex)
                            slotItem.actionWhenStopBeingHeld(Game1.player);
                        inventory[localIndex] = null;
                    }
                    return toAddTo;
                }
            }
            return toAddTo;
        }

        public override void performHoverAction(int x, int y)
        {
            this.hoverItem = null;
            foreach (ClickableComponent button in this.buttons)
            {
                if (button.containsPoint(x, y))
                {
                    int int32 = Convert.ToInt32(button.name);
                    Item item = GetItemAtSlot(int32);
                    if (item != null)
                    {
                        button.scale = Math.Min(button.scale + 0.05f, 1.1f);
                        this.hoverTitle = item.Name;
                        this.hoverItem = item;
                    }
                }
                else
                    button.scale = Math.Max(button.scale - 0.025f, 1f);
            }
        }

        public void shifted(bool right)
        {
            if (right)
            {
                for (int index = 0; index < this.buttons.Count; ++index)
                    this.buttons[index].scale = (float)(1.0 + index * 0.0299999993294477);
            }
            else
            {
                for (int index = this.buttons.Count - 1; index >= 0; --index)
                    this.buttons[index].scale = (float)(1.0 + (11 - index) * 0.0299999993294477);
            }
        }

        public override void update(GameTime time)
        {
            if (baseMaxItems != Game1.player.MaxItems)
            {
                var newInventory = Game1.player.MaxItems;

                if (_inventoryManager == null)
                {
                    if (Game1.player.Items.Count() < (newInventory + NUM_BUTTONS))
                    {
                        for (int i = Game1.player.Items.Count(); i < (newInventory + NUM_BUTTONS); i++)
                            Game1.player.Items.Add(null);
                    }

                    for (int i = 0; i < NUM_BUTTONS; i++)
                    {
                        this.buttons[i].name = string.Concat(i + newInventory);
                        Game1.player.Items[newInventory + i] = Game1.player.Items[baseMaxItems + i];
                        Game1.player.Items[baseMaxItems + i] = null;
                    }

                    if (Game1.player.CurrentToolIndex > (baseMaxItems - 1))
                        Game1.player.CurrentToolIndex += (newInventory - baseMaxItems);
                }
                else
                {
                    for (int i = 0; i < NUM_BUTTONS; i++)
                    {
                        this.buttons[i].name = string.Concat(i + newInventory);
                    }
                }

                baseMaxItems = newInventory;
            }
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            getDimensions();
            for (int index = 0; index < NUM_BUTTONS; ++index)
                buttons[index].bounds = new Rectangle(
                            //TODO: Use more reliable coordinates
                            this.xPositionOnScreen + IClickableMenu.spaceToClearSideBorder,
                            this.yPositionOnScreen + IClickableMenu.spaceToClearSideBorder + (index * Game1.tileSize),
                            Game1.tileSize,
                            Game1.tileSize);
        }

        public override bool isWithinBounds(int x, int y)
        {
            return new Rectangle(
                this.buttons.First().bounds.X,
                this.buttons.First().bounds.Y,
                Game1.tileSize,
                this.buttons.Last().bounds.Y - this.buttons.First().bounds.Y + Game1.tileSize
            ).Contains(x, y);
        }

        public override void draw(SpriteBatch b)
        {
            //Checks if the player is on any other menu before drawing the tooltip
            if (Game1.activeClickableMenu != null && !forceDraw)
                return;
            //Checks and draws the buttons
            if (!forceDraw)
            {
                int positionOnScreen1 = this.yPositionOnScreen;
                if (Game1.options.pinToolbarToggle)
                {
                    this.yPositionOnScreen = Game1.viewport.Height - getInitialHeight();
                    this.transparency = Math.Min(1f, this.transparency + 0.075f);
                    if (Game1.GlobalToLocal(Game1.viewport, new Vector2(Game1.player.GetBoundingBox().Center.X, Game1.player.GetBoundingBox().Center.Y)).Y > (double)(Game1.viewport.Height - Game1.tileSize * 3))
                        this.transparency = Math.Max(0.33f, this.transparency - 0.15f);
                }

                else if (!(orientation == Orientation.BottomLeft || orientation == Orientation.BottomRight))
                    this.yPositionOnScreen = (double)Game1.GlobalToLocal(Game1.viewport, new Vector2(Game1.player.GetBoundingBox().Center.X, Game1.player.GetBoundingBox().Center.Y)).Y > (double)(Game1.viewport.Height / 2 + Game1.tileSize) ? Game1.tileSize / 8 : Game1.viewport.Height - getInitialHeight() - Game1.tileSize / 8;
                if (orientation == Orientation.BottomRight && Game1.showingHealth)
                {
                    int newXPos = Game1.viewport.Width - (getInitialWidth() / 2) - IClickableMenu.spaceToClearSideBorder - getInitialWidth() - 64;
                    xPositionOnScreen = newXPos;
                    foreach (ClickableComponent button in this.buttons)
                    {
                        button.bounds.X = newXPos + IClickableMenu.spaceToClearSideBorder;
                    }

                }
                int positionOnScreen2 = this.yPositionOnScreen;
                if (positionOnScreen1 != positionOnScreen2)
                {
                    for (int index = 0; index < NUM_BUTTONS; ++index)
                        this.buttons[index].bounds.Y = this.yPositionOnScreen + IClickableMenu.spaceToClearSideBorder + (index * Game1.tileSize);
                }
            }
            //Draws the background texture. 
            IClickableMenu.drawTextureBox(b, Game1.menuTexture, this.toolbarTextSource, this.xPositionOnScreen, this.yPositionOnScreen, this.width,
                this.height, Color.White * this.transparency, 1f, false);
            int toolBarIndex = 0;
            for (int index = 0; index < NUM_BUTTONS; ++index)
            {
                this.buttons[index].scale = Math.Max(1f, this.buttons[index].scale - 0.025f);
                Vector2 location = new Vector2(
                    //TODO: Use more reliable coordinates
                    this.buttons[index].bounds.X,
                    this.buttons[index].bounds.Y);

                int slotId = Convert.ToInt32(this.buttons[index].name);
                bool isActiveSlot = Game1.player.CurrentToolIndex == slotId;
                b.Draw(Game1.menuTexture, location, new Rectangle?(Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, isActiveSlot ? 56 : 10)), Color.White * transparency);

                var item = GetItemAtSlot(slotId);
                if (item == null)
                {
                    continue;
                }

                item.drawInMenu(b, location, isActiveSlot ? 0.9f : this.buttons.ElementAt<ClickableComponent>(index).scale * 0.8f, this.transparency, 0.88f);
                toolBarIndex++;
            }
            if (toolBarIndex != numToolsInToolbar)
                numToolsInToolbar = toolBarIndex;

            //draw the tooltip if it's feasible, else allow another method to explicitly draw it

            // Show current inventory index overlay (e.g., "Inv 1", "Inv 2")
            if (_inventoryManager != null)
            {
                int activeInvIndex = _inventoryManager.GetActiveInventoryIndex(Game1.player);
                DrawInventoryIndicator(b, activeInvIndex);
            }

            if (Game1.activeClickableMenu == null)
            {
                drawToolTip(b);
            }
        }

        public void drawToolTip(SpriteBatch b)
        {
            //If an item is hovered, shows its tooltip.
            if (this.hoverItem == null)
                return;
            IClickableMenu.drawToolTip(b, this.hoverItem.getDescription(), this.hoverItem.Name, this.hoverItem);
            this.hoverItem = null;
        }

        public static int getInitialWidth()
        {
            return (Game1.tileSize * 3 / 2);
        }

        public static int getInitialHeight()
        {
            return ((Game1.tileSize * NUM_BUTTONS) + (Game1.tileSize / 2));
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            foreach (ClickableComponent button in this.buttons)
            {
                int int32 = Convert.ToInt32(button.name);
                int x1 = x;
                int y1 = y;
                Item item = GetItemAtSlot(int32);
                if (button.containsPoint(x1, y1) && item != null)
                {
                    if (item is Tool && Game1.player.ActiveObject != null && (item as Tool).canThisBeAttached(Game1.player.ActiveObject))
                    {
                        (item as Tool).attach(Game1.player.ActiveObject);
                        Game1.player.ActiveObject = null;
                        if (playSound)
                            Game1.playSound("dwop");
                        break;
                    }
                }
            }
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

        public Item? GetItemAtSlot(int slotIndex)
        {
            if (TryResolveSlot(slotIndex, out var inventory, out var localIndex))
            {
                return inventory[localIndex];
            }

            return null;
        }
    }
}
