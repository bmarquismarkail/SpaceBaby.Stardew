using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System.Collections.Generic;
using System.Linq;
using VerticalToolbar.Framework;

namespace VerticalToolbar
{
    internal class ModEntry : Mod
    {
        /// <summary>The mod configuration.</summary>
        private ModConfig Config;
        private VerticalToolBar verticalToolbar;
        VerticalToolbar.Framework.Orientation Orientation;
        private bool isInitiated, modOverride;
        private int currentToolIndex;
        private int scrolling;
        private int triggerPolling = 300;
        private int released = 0;
        private int baseMaxItems;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();

            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.UpdateTicked += onUpdateTicked;
            helper.Events.Input.MouseWheelScrolled += OnMouseWheelScrolled;
            helper.Events.Input.ButtonPressed += onButtonPressed;
            helper.Events.Input.ButtonReleased += onButtonReleased;
            helper.Events.Display.MenuChanged += onMenuChanged;
            helper.Events.GameLoop.ReturnedToTitle += onReturnToTitle;

            isInitiated = false;
            modOverride = false;
            Orientation = Config.Controls.Orientation;
        }

        private void onReturnToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            isInitiated = false;
        }

        /// <summary>Raised after the game state is updated (???60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void onUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!isInitiated)
                return;

            HandleInputModifier();
            UpdateCurrentTool();
            HandlePolling();
        }

        private void HandleInputModifier()
        {
            modOverride = false;

            if (!Game1.player.UsingTool && this.Helper.Input.IsDown(Config.Controls.HoldToActivateSlotKeys))
            {
                for (int i = 0; i < 5; i++) // Limit to 1-5 slots
                {
                    if (this.Helper.Input.IsDown(Config.Controls.HoldToActivateSlotKeys) && this.Helper.Input.IsDown((SButton)((int)SButton.D1 + i)))
                    {
                        currentToolIndex = Convert.ToInt32(verticalToolbar.buttons[i].name);
                        modOverride = true;
                        break;
                    }
                }
            }
        }

        private void UpdateCurrentTool()
        {
            if (verticalToolbar.numToolsInToolbar > 0 && Game1.player.CurrentToolIndex != currentToolIndex && (modOverride || (triggerPolling < 300)))
            {
                Game1.player.CurrentToolIndex = currentToolIndex;
                modOverride = false;
            }
        }

        private void HandlePolling()
        {
            if (verticalToolbar.numToolsInToolbar <= 0)
                return;

            var input = this.Helper.Input;

            if (scrolling != 0)
            {
                HandleScrolling(input);
            }
            else if (released < 300)
            {
                HandleRelease();
            }
        }

        private void HandleScrolling(IInputHelper input)
        {
            if (!input.IsDown(this.Config.Controls.ScrollLeft) && !input.IsDown(this.Config.Controls.ScrollRight))
            {
                scrolling = 0;
                return;
            }

            Game1.player.CurrentToolIndex = currentToolIndex;
            int elapsedGameTime = Game1.currentGameTime.ElapsedGameTime.Milliseconds;
            this.triggerPolling -= elapsedGameTime;

            if (this.triggerPolling <= 0 && !modOverride)
            {
                Game1.player.CurrentToolIndex = currentToolIndex;
                this.triggerPolling = 100;
                CheckHoveredItem(scrolling);
            }
        }

        private void HandleRelease()
        {
            Game1.player.CurrentToolIndex = currentToolIndex;
            int elapsedGameTime = Game1.currentGameTime.ElapsedGameTime.Milliseconds;
            this.released += elapsedGameTime;

            if (released > 300 && !modOverride)
            {
                Game1.player.CurrentToolIndex = currentToolIndex;
                released = 300;
            }
        }

        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void onButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!isInitiated)
                return;

            // set scrolling
            if(verticalToolbar.numToolsInToolbar > 0 && (e.Button == this.Config.Controls.ScrollLeft || e.Button == this.Config.Controls.ScrollRight))
            {
                this.Helper.Input.Suppress(e.Button);
                Game1.player.CurrentToolIndex = currentToolIndex;
                int num = e.Button == this.Config.Controls.ScrollLeft ? -1 : 1;
                CheckHoveredItem(num);
                scrolling = num;
            }

            //set sorting
            if (e.Button == (SButton)Game1.options.toolbarSwap[0].key)
            {
                this.Helper.Input.Suppress(e.Button);
                ModShiftToolbar(this.Helper.Input.IsDown(SButton.LeftControl));
            }
        }

        /// <summary>Raised after the player releases a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void onButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            if (!isInitiated)
                return;

            if (verticalToolbar.numToolsInToolbar > 0 && (e.Button == this.Config.Controls.ScrollLeft || e.Button == this.Config.Controls.ScrollRight))
            {
                Game1.player.CurrentToolIndex = currentToolIndex;
                scrolling = 0;
                released = 0;
                triggerPolling = 300;
            }
        }

        /// <summary>Raised after a game menu is opened, closed, or replaced.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void onMenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (e.NewMenu is GameMenu menu && menu.currentTab == GameMenu.inventoryTab)
            {
                List<IClickableMenu> pages = this.Helper.Reflection.GetField<List<IClickableMenu>>(menu, "pages").GetValue();
                pages.RemoveAt(0);
                pages.Insert(0, new ModInventoryPage(menu.xPositionOnScreen, menu.yPositionOnScreen, menu.width, menu.height));
            }
        }

        private void CheckHoveredItem(int num)
        {
            int MAXcurrentToolIndex = 11;

            if (!IsValidState()) return;

            if (Game1.options.invertScrollDirection)
                num *= -1;

            AdjustToolIndex(num, MAXcurrentToolIndex);
            modOverride = true;
        }

        private static bool IsValidState()
        {
            return !Game1.player.UsingTool && !Game1.dialogueUp &&
                   ((Game1.player.CurrentTool is StardewValley.Tools.Pickaxe || Game1.player.CanMove) &&
                   (Game1.player.Items.CountItemStacks() != 0 && !Game1.eventUp));
        }

        private void AdjustToolIndex(int num, int maxIndex)
        {
            while (true)
            {
                currentToolIndex += num;

                if (num < 0)
                {
                    HandleNegativeScroll(maxIndex);
                }
                else if (num > 0)
                {
                    HandlePositiveScroll(maxIndex);
                }

                if (Game1.player.Items[currentToolIndex] != null)
                    break;
            }
        }

        private void HandleNegativeScroll(int maxIndex)
        {
            if (currentToolIndex < 0)
            {
                currentToolIndex = Convert.ToInt32(verticalToolbar.buttons[verticalToolbar.numToolsInToolbar - 1].name);
            }
            else if (currentToolIndex > maxIndex && currentToolIndex < Convert.ToInt32(verticalToolbar.buttons[0].name))
            {
                currentToolIndex = maxIndex;
            }
        }

        private void HandlePositiveScroll(int maxIndex)
        {
            if (currentToolIndex > Convert.ToInt32(verticalToolbar.buttons[verticalToolbar.numToolsInToolbar - 1].name))
            {
                currentToolIndex = 0;
            }
            else if (currentToolIndex > maxIndex && currentToolIndex < Convert.ToInt32(verticalToolbar.buttons[0].name))
            {
                currentToolIndex = Convert.ToInt32(verticalToolbar.buttons[0].name);
            }
        }

        /// <summary>Raised after the player scrolls the mouse wheel.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnMouseWheelScrolled(object sender, MouseWheelScrolledEventArgs e)
        {
            if (!isInitiated)
                return;

            if (verticalToolbar.numToolsInToolbar > 0)
                CheckHoveredItem(e.Delta > 0 ? 1 : -1);
        }

        /// <summary>Raised after the player loads a save slot and the world is initialised.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            baseMaxItems = Game1.player.MaxItems;
            verticalToolbar = new VerticalToolBar(this.Orientation);
            Game1.onScreenMenus.Add(verticalToolbar);

            currentToolIndex = Game1.player.CurrentToolIndex;
            isInitiated = true;
        }

        private void ModShiftToolbar(bool right)
        {
            // This is simply shiftToolbar, but modified to not use NetCode, and taking to account the vertical toolbar
            if (Game1.player.Items == null || Game1.player.Items.Count < 12 || (Game1.player.UsingTool || Game1.dialogueUp) || (Game1.player.CurrentTool is not StardewValley.Tools.Pickaxe && !Game1.player.CanMove || (Game1.player.Items.CountItemStacks() == 0 || Game1.eventUp)) || Game1.farmEvent != null)
                return;
            Game1.playSound("shwip");
            if (Game1.player.CurrentItem != null)
                Game1.player.CurrentItem.actionWhenStopBeingHeld(Game1.player);
            if (right)
            {
                List<Item> range = Game1.player.Items.ToList().GetRange(12,baseMaxItems - 12);
                range.AddRange(Game1.player.Items.ToList().GetRange(0, 12));
                range.AddRange(Game1.player.Items.ToList().GetRange(baseMaxItems, 5));
                Game1.player.setInventory(range);
            }
            else
            {
                List<Item> range = Game1.player.Items.ToList().GetRange(baseMaxItems - 12, 12);
                for (int index = 0; index < baseMaxItems - 12; ++index)
                    range.Add(Game1.player.Items[index]);
                range.AddRange(Game1.player.Items.ToList().GetRange(baseMaxItems, 5));
                Game1.player.setInventory(range);
            }
            Game1.player.netItemStowed.Set(false);
            if (Game1.player.CurrentItem != null)
                Game1.player.CurrentItem.actionWhenBeingHeld(Game1.player);
            for (int index = 0; index < Game1.onScreenMenus.Count; ++index)
            {
                if (Game1.onScreenMenus[index] is Toolbar toolbar)
                {
                    toolbar.shifted(right);
                    break;
                }
            }
        }
    }
}
