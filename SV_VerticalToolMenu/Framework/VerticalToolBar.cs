using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Inventories;
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

    internal class VerticalToolBar : IClickableMenu
    {
        public List<ClickableComponent> buttons = new List<ClickableComponent>();
        public Orientation orientation;
        private float transparency = 1f;
        public Rectangle toolbarTextSource = new Rectangle(0, 256, 60, 60);
        public int numToolsInToolbar = 0;
        private Item hoverItem;
        public bool forceDraw = false;
        public Inventory Inventory { get; } = new Inventory();

        public VerticalToolBar(Orientation o, int numButtons = 5, bool forceDraw = false)
            : base()
        {

            orientation = o;
            this.forceDraw = forceDraw;
            getDimensions();

            for (int index = 0; index < numButtons; index++)
            {
                Inventory.Add(null);
                this.buttons.Add(
                    new ClickableComponent(
                        new Rectangle(
                            this.xPositionOnScreen + IClickableMenu.spaceToClearSideBorder,
                            this.yPositionOnScreen + IClickableMenu.spaceToClearSideBorder + ((index + 1) * Game1.tileSize),
                            Game1.tileSize,
                            Game1.tileSize),
                        string.Concat(index)));
            }
        }

        public static Toolbar getToolbar()
        {
            return Game1.onScreenMenus.OfType<Toolbar>().FirstOrDefault();
        }

        public void getDimensions()
        {
            Rectangle dimensionRectangle;
            int NUM_BUTTONS = Inventory.Count;
            dimensionRectangle.Width = Game1.tileSize * 3 / 2;
            dimensionRectangle.Height = Game1.tileSize* NUM_BUTTONS +(Game1.tileSize / 2);

            switch (orientation)
            {
                case Orientation.LeftOfToolbar:
                    dimensionRectangle.X = (Game1.viewport.Width / 2 - 384 - 64) - (getInitialWidth() / 2);
                    dimensionRectangle.Y = Game1.viewport.Height - getInitialHeight(NUM_BUTTONS); 
                    break;
                case Orientation.RightOfToolbar:
                    dimensionRectangle.X = (Game1.viewport.Width / 2 - 384 - 64) + getToolbar().width - (getInitialWidth() / 2);
                    dimensionRectangle.Y = Game1.viewport.Height - getInitialHeight(NUM_BUTTONS);
                    break;
                case Orientation.BottomLeft:
                    dimensionRectangle.X = IClickableMenu.spaceToClearSideBorder;
                    dimensionRectangle.Y = Game1.viewport.Height - getInitialHeight(NUM_BUTTONS);
                    break;
                case Orientation.BottomRight:
                    dimensionRectangle.X = Game1.viewport.Width - (getInitialWidth() /2) -  IClickableMenu.spaceToClearSideBorder - getInitialWidth() - (Game1.showingHealth? 64 : 0);
                    dimensionRectangle.Y = Game1.viewport.Height - getInitialHeight(NUM_BUTTONS);
                    break;
                default:
                    throw new NotSupportedException("Error: Orientation Not Supported");
            }
            this.xPositionOnScreen = dimensionRectangle.X;
            this.yPositionOnScreen = dimensionRectangle.Y;
            this.width = dimensionRectangle.Width;
            this.height = dimensionRectangle.Height;    

        }
        public void setButtons()
        {
            for (int index = 0; index < Inventory.Count; ++index)
            {
                this.buttons.Add(
                    new ClickableComponent(
                        new Rectangle(
                            this.xPositionOnScreen + IClickableMenu.spaceToClearSideBorder,
                            this.yPositionOnScreen + IClickableMenu.spaceToClearSideBorder + (index * Game1.tileSize),
                            Game1.tileSize,
                            Game1.tileSize),
                        string.Concat(index)));
            }
        }
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (Game1.player.UsingTool)
                return;
            
            foreach (ClickableComponent button in this.buttons)
            {
                Game1.player.CurrentToolIndex = Convert.ToInt32(button.name);
                if (Game1.player.ActiveObject != null)
                {
                    Game1.player.showCarrying();
                    Game1.playSound("pickUpItem");
                }
                else
                {
                    break;
                }
            }
        }

        public Item RightClick(int x, int y, Item toAddTo, bool playSound = true)
        {
            foreach (ClickableComponent button in this.buttons)
            {
                int int32 = Convert.ToInt32(button.name);
                int x1 = x;
                int y1 = y;
                if (button.containsPoint(x1, y1) && Inventory[int32] != null)
                {
                    if (Inventory[int32] is Tool && (toAddTo == null || toAddTo is SObject) && (Inventory[int32] as Tool).canThisBeAttached((SObject)toAddTo))
                        return (Inventory[int32] as Tool).attach((SObject)toAddTo);
                    if (toAddTo == null)
                    {
                        if (Inventory[int32].maximumStackSize() != -1)
                        {
                            Item one = Inventory[int32].getOne();
                            if (Inventory[int32].Stack > 1)
                            {
                                if (Game1.isOneOfTheseKeysDown(Game1.oldKBState, new[] { new InputButton(Keys.LeftShift) }))
                                {
                                    one.Stack = (int)Math.Ceiling(Inventory[int32].Stack / 2.0);
                                    Inventory[int32].Stack = Inventory[int32].Stack / 2;
                                    goto label_15;
                                }
                            }
                            if (Inventory[int32].Stack == 1)
                                Inventory[int32] = null;
                            else
                                --Inventory[int32].Stack;
                            label_15:
                            if (Inventory[int32] != null && Inventory[int32].Stack <= 0)
                                Inventory[int32] = null;
                            if (playSound)
                                Game1.playSound("dwop");
                            return one;
                        }
                    }
                    else if (Inventory[int32].canStackWith(toAddTo) && toAddTo.Stack < toAddTo.maximumStackSize())
                    {
                        if (Game1.isOneOfTheseKeysDown(Game1.oldKBState, new[] { new InputButton(Keys.LeftShift) }))
                        {
                            toAddTo.Stack += (int)Math.Ceiling(Inventory[int32].Stack / 2.0);
                            Inventory[int32].Stack = Inventory[int32].Stack / 2;
                        }
                        else
                        {
                            ++toAddTo.Stack;
                            --Inventory[int32].Stack;
                        }
                        if (playSound)
                            Game1.playSound("dwop");
                        if (Inventory[int32].Stack <= 0)
                        {
                            Inventory[int32] = null;
                        }
                        return toAddTo;
                    }
                }
            }
            return toAddTo;
        }

        private static bool IsToolAttachment(int itemIndex, Item toAddTo)
        {
            return Game1.player.Items[itemIndex] is Tool && 
                  (toAddTo == null || toAddTo is SObject) && 
                  (Game1.player.Items[itemIndex] as Tool).canThisBeAttached((SObject)toAddTo);
        }

        private static Item AttachToTool(int itemIndex, Item toAddTo)
        {
            return (Game1.player.Items[itemIndex] as Tool).attach((SObject)toAddTo);
        }

        private static Item HandleTakingItem(int itemIndex, bool playSound)
        {
            if (Game1.player.Items[itemIndex].maximumStackSize() == -1)
                return null;

            // Stop holding action if needed
            if (itemIndex == Game1.player.CurrentToolIndex && 
                Game1.player.Items[itemIndex] != null && 
                Game1.player.Items[itemIndex].Stack == 1)
            {
                Game1.player.Items[itemIndex].actionWhenStopBeingHeld(Game1.player);
            }

            Item result = Game1.player.Items[itemIndex].getOne();
            
            // Handle shift+click for splitting stacks
            if (ShouldSplitStack(itemIndex))
            {
                SplitStackInHalf(itemIndex, result);
            }
            else
            {
                // Regular item taking (one at a time)
                RemoveOneFromStack(itemIndex);
            }

            // Clean up empty stacks
            CleanupEmptyStack(itemIndex);
            
            if (playSound)
                Game1.playSound("dwop");
                
            return result;
        }

        private static bool ShouldSplitStack(int itemIndex)
        {
            return Game1.player.Items[itemIndex].Stack > 1 && 
                   Game1.isOneOfTheseKeysDown(Game1.oldKBState, new[] { new InputButton(Keys.LeftShift) });
        }

        private static void SplitStackInHalf(int itemIndex, Item result)
        {
            result.Stack = (int)Math.Ceiling(Game1.player.Items[itemIndex].Stack / 2.0);
            Game1.player.Items[itemIndex].Stack = Game1.player.Items[itemIndex].Stack / 2;
        }

        private static void RemoveOneFromStack(int itemIndex)
        {
            if (Game1.player.Items[itemIndex].Stack == 1)
                Game1.player.Items[itemIndex] = null;
            else
                --Game1.player.Items[itemIndex].Stack;
        }

        private static bool CanStackWithExistingItem(int itemIndex, Item toAddTo)
        {
            return Game1.player.Items[itemIndex].canStackWith(toAddTo) && 
                   toAddTo.Stack < toAddTo.maximumStackSize();
        }

        private static Item HandleStackingItems(int itemIndex, Item toAddTo, bool playSound)
        {
            if (Game1.isOneOfTheseKeysDown(Game1.oldKBState, new[] { new InputButton(Keys.LeftShift) }))
            {
                // Shift-click to split stack
                toAddTo.Stack += (int)Math.Ceiling(Game1.player.Items[itemIndex].Stack / 2.0);
                Game1.player.Items[itemIndex].Stack = Game1.player.Items[itemIndex].Stack / 2;
            }
            else
            {
                // Regular click to move one item
                ++toAddTo.Stack;
                --Game1.player.Items[itemIndex].Stack;
            }
            
            if (playSound)
                Game1.playSound("dwop");
                
            CleanupEmptyStack(itemIndex);
            return toAddTo;
        }

        private static void CleanupEmptyStack(int itemIndex)
        {
            if (Game1.player.Items[itemIndex] != null && Game1.player.Items[itemIndex].Stack <= 0)
            {
                if (itemIndex == Game1.player.CurrentToolIndex)
                    Game1.player.Items[itemIndex].actionWhenStopBeingHeld(Game1.player);
                    
                Game1.player.Items[itemIndex] = null;
            }
        }

        public override void performHoverAction(int x, int y)
        {
            this.hoverItem = null;
            
            var hoverButton = this.buttons.FirstOrDefault(button => button.containsPoint(x, y));
            if (hoverButton != null)
            {
                int int32 = Convert.ToInt32(hoverButton.name);
                if (int32 < Game1.player.Items.Count && Game1.player.Items[int32] != null)
                {
                    int int32 = Convert.ToInt32(button.name);
                    if (int32 < Inventory.Count && Inventory[int32] != null)
                    {
                        button.scale = Math.Min(button.scale + 0.05f, 1.1f);
                        this.hoverTitle = Inventory[int32].Name;
                        this.hoverItem = Inventory[int32];
                    }
                }
            }
            
            foreach (var button in this.buttons.Where(button => !button.containsPoint(x, y)))
            {
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

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            getDimensions();
            int NUM_BUTTONS = Inventory.Count;
            for (int index = 0; index < NUM_BUTTONS; ++index)
                buttons[index].bounds = new Rectangle(
                            this.xPositionOnScreen + IClickableMenu.spaceToClearSideBorder,
                            this.yPositionOnScreen + IClickableMenu.spaceToClearSideBorder + (index * Game1.tileSize),
                            Game1.tileSize,
                            Game1.tileSize);
        }

        public override bool isWithinBounds(int x, int y)
        {
            return new Rectangle(
                this.buttons[0].bounds.X,
                this.buttons[0].bounds.Y,
                Game1.tileSize,
                this.buttons[^1].bounds.Y - this.buttons[0].bounds.Y + Game1.tileSize
            ).Contains(x, y);
        }

        public override void draw(SpriteBatch b)
        {
            //Checks if the player is on any other menu before drawing the tooltip
            if (Game1.activeClickableMenu != null && !forceDraw)
                return;
                
            if (!forceDraw)
            {
                int NUM_BUTTONS = Inventory.Count;
                int positionOnScreen1 = this.yPositionOnScreen;
                if (Game1.options.pinToolbarToggle )
                {
                    this.yPositionOnScreen = Game1.viewport.Height - getInitialHeight(NUM_BUTTONS);
                    this.transparency = Math.Min(1f, this.transparency + 0.075f);
                    if (Game1.GlobalToLocal(Game1.viewport, new Vector2(Game1.player.GetBoundingBox().Center.X, Game1.player.GetBoundingBox().Center.Y)).Y > (double)(Game1.viewport.Height - Game1.tileSize * 3))
                        this.transparency = Math.Max(0.33f, this.transparency - 0.15f);
                }

                else if ( !(orientation == Orientation.BottomLeft || orientation == Orientation.BottomRight) )
                    this.yPositionOnScreen = (double)Game1.GlobalToLocal(Game1.viewport, new Vector2(Game1.player.GetBoundingBox().Center.X, Game1.player.GetBoundingBox().Center.Y)).Y > (double)(Game1.viewport.Height / 2 + Game1.tileSize) ? Game1.tileSize / 8 : Game1.viewport.Height - getInitialHeight(NUM_BUTTONS) - Game1.tileSize / 8;
                if (orientation == Orientation.BottomRight && Game1.showingHealth)
                {
                    this.transparency = Math.Max(0.33f, this.transparency - 0.15f);
                }
            }
            else if (!(orientation == Orientation.BottomLeft || orientation == Orientation.BottomRight))
            {
                Vector2 playerPosition = Game1.GlobalToLocal(Game1.viewport, new Vector2(Game1.player.GetBoundingBox().Center.X, Game1.player.GetBoundingBox().Center.Y));
                bool playerInLowerHalf = playerPosition.Y > (Game1.viewport.Height / 2 + Game1.tileSize);
                
                this.yPositionOnScreen = playerInLowerHalf 
                    ? Game1.tileSize / 8 
                    : Game1.viewport.Height - getInitialHeight() - Game1.tileSize / 8;
            }
        }
        
        private void UpdateXPosition()
        {
            if (orientation == Orientation.BottomRight && Game1.showingHealth)
            {
                int newXPos = Game1.viewport.Width - (getInitialWidth() / 2) - IClickableMenu.spaceToClearSideBorder - getInitialWidth() - 64;
                xPositionOnScreen = newXPos;
                
                foreach (ClickableComponent button in this.buttons)
                {
                    button.bounds.X = newXPos + IClickableMenu.spaceToClearSideBorder;
                }
            }
        }
        
        private void UpdateButtonYPositions()
        {
            for (int index = 0; index < NUM_BUTTONS; ++index)
            {
                this.buttons[index].bounds.Y = this.yPositionOnScreen + IClickableMenu.spaceToClearSideBorder + (index * Game1.tileSize);
            }
        }
        
        private void DrawBackgroundTexture(SpriteBatch b)
        {
            IClickableMenu.drawTextureBox(
                b, 
                Game1.menuTexture, 
                this.toolbarTextSource, 
                this.xPositionOnScreen, 
                this.yPositionOnScreen, 
                this.width,
                this.height, 
                Color.White * this.transparency, 
                1f, 
                false);
        }
        
        private void DrawToolbarItems(SpriteBatch b)
        {
            int toolBarIndex = 0;
            int numButtonsForDraw = Inventory.Count;
            
            for (int index = 0; index < numButtonsForDraw; ++index)
            {
                this.buttons[index].scale = Math.Max(1f, this.buttons[index].scale - 0.025f);
                Vector2 location = new Vector2(
                    this.buttons[index].bounds.X,
                    this.buttons[index].bounds.Y);
                bool selected = Game1.player.CurrentItem != null && Game1.player.CurrentItem == Inventory[index];
                b.Draw(Game1.menuTexture, location, new Rectangle?(Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, selected ? 56 : 10)), Color.White * transparency);
                // Need to customize it for toolset //string text = index == 9 ? "0" : (index == 10 ? "-" : (index == 11 ? "=" : string.Concat((object)(index + 1))));
                //b.DrawString(Game1.tinyFont, text, position + new Vector2(4f, -8f), Color.DimGray * this.transparency);
                if (Inventory.Count <= index || Inventory.ElementAt<Item>(index) == null)
                {
                    continue;
                }
                Inventory[index].drawInMenu(b, location, selected ? 0.9f : this.buttons.ElementAt<ClickableComponent>(index).scale * 0.8f, this.transparency, 0.88f);
                toolBarIndex++;
            }
            
            if (toolBarIndex != numToolsInToolbar)
                numToolsInToolbar = toolBarIndex;
        }
        
        private void DrawButtonBackground(SpriteBatch b, int index, Vector2 location)
        {
            Rectangle sourceRect = Game1.getSourceRectForStandardTileSheet(
                Game1.menuTexture, 
                Game1.player.CurrentToolIndex == (index + baseMaxItems) ? 56 : 10);
                
            b.Draw(
                Game1.menuTexture, 
                location, 
                new Rectangle?(sourceRect), 
                Color.White * transparency);
        }
        
        private void DrawItemInSlot(SpriteBatch b, int index, Vector2 location)
        {
            float scale = Game1.player.CurrentToolIndex == (index + baseMaxItems) 
                ? 0.9f 
                : this.buttons[index].scale * 0.8f;
                
            Game1.player.Items[(index + baseMaxItems)].drawInMenu(
                b, 
                location, 
                scale, 
                this.transparency, 
                0.88f);
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
            return (Game1.tileSize * 3 / 2) ;
        }

        public static int getInitialHeight(int numButtons)
        {
            return ((Game1.tileSize * numButtons) + (Game1.tileSize / 2));
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            base.receiveRightClick(x, y, playSound);
        }
    }
}
