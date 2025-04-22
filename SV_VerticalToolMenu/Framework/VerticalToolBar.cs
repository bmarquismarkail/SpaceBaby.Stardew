using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
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
        public readonly int NUM_BUTTONS;
        public Orientation orientation;
        private float transparency = 1f;
        public Rectangle toolbarTextSource = new Rectangle(0, 256, 60, 60);
        public int numToolsInToolbar = 0;
        private Item hoverItem;
        public bool forceDraw = false;
        private int baseMaxItems = Game1.player.MaxItems;

        public VerticalToolBar(Orientation o, int numButtons = 5, bool forceDraw = false)
            : base()
        {

            orientation = o;
            NUM_BUTTONS = numButtons;
            this.forceDraw = forceDraw;
            getDimensions();
            // For compatibility with Bigger Backpack
            int newInventory = baseMaxItems + NUM_BUTTONS;
            for (int count = Game1.player.Items.Count; count < newInventory; count++)
            {
                Game1.player.Items.Add(null);
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

        public static Toolbar getToolbar()
        {
            return Game1.onScreenMenus.OfType<Toolbar>().FirstOrDefault();
        }

        public void getDimensions()
        {
            Rectangle dimensionRectangle;
            dimensionRectangle.Width = Game1.tileSize * 3 / 2;
            dimensionRectangle.Height = Game1.tileSize* NUM_BUTTONS +(Game1.tileSize / 2);

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
                    dimensionRectangle.X = Game1.viewport.Width - (getInitialWidth() /2) -  IClickableMenu.spaceToClearSideBorder - getInitialWidth() - (Game1.showingHealth? 64 : 0);
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
            var button = this.buttons.FirstOrDefault(btn => btn.containsPoint(x, y));
            if (button != null)
            {
                Game1.player.CurrentToolIndex = Convert.ToInt32(button.name);
                if (Game1.player.ActiveObject != null)
                {
                    Game1.player.showCarrying();
                    Game1.playSound("pickUpItem");
                }
                else
                {
                    Game1.player.showNotCarrying();
                    Game1.playSound("stoneStep");
                }
            }
        }

        public Item RightClick(int x, int y, Item toAddTo, bool playSound = true)
        {
            foreach (ClickableComponent button in this.buttons)
            {
                int itemIndex = Convert.ToInt32(button.name);
                if (button.containsPoint(x, y) && Game1.player.Items[itemIndex] != null)
                {
                    // Handle tool attachment
                    if (IsToolAttachment(itemIndex, toAddTo))
                        return AttachToTool(itemIndex, toAddTo);
                    
                    // Handle taking item when nothing is being held
                    if (toAddTo == null)
                    {
                        return HandleTakingItem(itemIndex, playSound);
                    }
                    // Handle stacking with existing item
                    else if (CanStackWithExistingItem(itemIndex, toAddTo))
                    {
                        return HandleStackingItems(itemIndex, toAddTo, playSound);
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
                    hoverButton.scale = Math.Min(hoverButton.scale + 0.05f, 1.1f);
                    this.hoverItem = Game1.player.Items[int32];
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

        public override void update(GameTime time)
        {
            if (baseMaxItems != Game1.player.MaxItems)
            {
                var newInventory = Game1.player.MaxItems;
                if (Game1.player.Items.Count < (newInventory + NUM_BUTTONS) )
                {
                    for (int i = Game1.player.Items.Count; i < (newInventory + NUM_BUTTONS); i++)
                        Game1.player.Items.Add(null);
                }
                for (int i= 0; i< NUM_BUTTONS; i++)
                {
                    this.buttons[i].name = string.Concat(i + newInventory);
                    Game1.player.Items[newInventory + i] = Game1.player.Items[baseMaxItems + i];
                    Game1.player.Items[baseMaxItems + i] = null;
                }
                if (Game1.player.CurrentToolIndex > (baseMaxItems -1) )
                    Game1.player.CurrentToolIndex += (newInventory - baseMaxItems);

                baseMaxItems = newInventory;
            }
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            getDimensions();
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
                UpdatePositions();
            }
            
            DrawBackgroundTexture(b);
            DrawToolbarItems(b);
            
            //draw the tooltip if it's feasible, else allow another method to explicitly draw it
            if(Game1.activeClickableMenu == null)
            {
                drawToolTip(b);
            }
        }
        
        private void UpdatePositions()
        {
            int positionOnScreen1 = this.yPositionOnScreen;
            
            UpdateYPosition();
            UpdateXPosition();
            
            int positionOnScreen2 = this.yPositionOnScreen;
            if (positionOnScreen1 != positionOnScreen2)
            {
                UpdateButtonYPositions();
            }
        }
        
        private void UpdateYPosition()
        {
            if (Game1.options.pinToolbarToggle)
            {
                this.yPositionOnScreen = Game1.viewport.Height - getInitialHeight();
                this.transparency = Math.Min(1f, this.transparency + 0.075f);
                
                Vector2 playerPosition = Game1.GlobalToLocal(Game1.viewport, new Vector2(Game1.player.GetBoundingBox().Center.X, Game1.player.GetBoundingBox().Center.Y));
                if (playerPosition.Y > (Game1.viewport.Height - Game1.tileSize * 3))
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
            
            for (int index = 0; index < NUM_BUTTONS; ++index)
            {
                this.buttons[index].scale = Math.Max(1f, this.buttons[index].scale - 0.025f);
                Vector2 location = new Vector2(
                    this.buttons[index].bounds.X,
                    this.buttons[index].bounds.Y);
                    
                DrawButtonBackground(b, index, location);
                
                if (Game1.player.Items.Count <= (index + baseMaxItems) || 
                    Game1.player.Items[index + baseMaxItems] == null)
                {
                    continue;
                }
                
                DrawItemInSlot(b, index, location);
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

        public static int getInitialHeight()
        {
            return ((Game1.tileSize * 5) + (Game1.tileSize / 2));
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            base.receiveRightClick(x, y, playSound);
        }
    }
}
