#nullable enable
using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System.Collections.Generic;
using System.Linq;
using VerticalToolbar.Framework;
using SV_InventorySystem.Framework.Reflection;

namespace VerticalToolbar
{
    public class ModEntry : Mod, IVerticalToolbarAPI
    {
        /// <summary>The mod configuration.</summary>
        private ModConfig Config = null!;
        private VerticalToolBar verticalToolbar = null!;
        VerticalToolbar.Framework.Orientation Orientation;
        private bool isInitiated, modOverride;
        private int currentToolIndex;
        private int? pendingWheelSelection;
        private int scrolling;
        private int triggerPolling = 300;
        private int released = 0;
        private int baseMaxItems;
        private IMultiInventoryManager? _inventoryManager;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();

            helper.Events.GameLoop.GameLaunched += (s, e) => OnGameLaunched();
            helper.Events.GameLoop.SaveLoaded += onSaveLoaded;
            helper.Events.GameLoop.UpdateTicked += onUpdateTicked;
            helper.Events.Input.MouseWheelScrolled += onMouseWheelScrolled;
            helper.Events.Input.ButtonPressed += onButtonPressed;
            helper.Events.Input.ButtonReleased += onButtonReleased;
            helper.Events.Display.MenuChanged += onMenuChanged;
            helper.Events.GameLoop.ReturnedToTitle += onReturnToTitle;

            isInitiated = false;
            modOverride = false;
            Orientation = Config.Controls.Orientation;
        }
        private void SwitchToNextInventory()
        {
            if (_inventoryManager == null) return;
            int currentInvIdx = verticalToolbar.GetDisplayedInventoryIndex();
            int maxInventories = _inventoryManager.GetInventoryCount(Game1.player);
            if (maxInventories <= 1)
                return;
            int nextIdx = currentInvIdx >= maxInventories - 1 ? 1 : Math.Max(1, currentInvIdx + 1);
            SwitchInventory(nextIdx);
            Monitor.Log($"Switched to inventory {nextIdx}", LogLevel.Debug);
        }

        private void SwitchToPreviousInventory()
        {
            if (_inventoryManager == null) return;
            int currentInvIdx = verticalToolbar.GetDisplayedInventoryIndex();
            int maxInventories = _inventoryManager.GetInventoryCount(Game1.player);
            if (maxInventories <= 1)
                return;
            int prevIdx = currentInvIdx <= 1 ? maxInventories - 1 : currentInvIdx - 1;
            SwitchInventory(prevIdx);
            Monitor.Log($"Switched to inventory {prevIdx}", LogLevel.Debug);
        }
        private void OnGameLaunched()
        {
            try
            {
                var inventoryModId = "SpaceBaby.SV_InventorySystem";
                var inventoryMod = Helper.ModRegistry.GetApi<IMultiInventoryManager>(inventoryModId);

                if (inventoryMod != null)
                {
                    _inventoryManager = inventoryMod;
                    this.Monitor.Log("SV_InventorySystem detected. Multi-inventory features enabled.", LogLevel.Info);
                }
                else
                {
                    this.Monitor.Log("SV_InventorySystem didn't expose its API. Vertical Toolbar will remain disabled for this session.", LogLevel.Error);
                }
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"Error while trying to integrate with SV_InventorySystem: {ex}", LogLevel.Error);
            }
        }

        private void onReturnToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            pendingWheelSelection = null;
            if (verticalToolbar != null)
                Game1.onScreenMenus.Remove(verticalToolbar);
            isInitiated = false;
        }

        /// <summary>Raised after the game state is updated (???60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void onUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!isInitiated)
                return;

            if (pendingWheelSelection is int selectedIndex)
            {
                Game1.player.CurrentToolIndex = selectedIndex;
                currentToolIndex = selectedIndex;
                pendingWheelSelection = null;
            }

            // check input modifier
            var input = this.Helper.Input;
            modOverride = false;

            if (!Game1.player.UsingTool && input.IsDown(Config.Controls.HoldToActivateSlotKeys))
            {
                int selectedSlot = -1;
                if (input.IsDown(Config.Controls.ChooseSlot1))
                    selectedSlot = Convert.ToInt32(verticalToolbar.buttons[0].name);
                else if (input.IsDown(Config.Controls.ChooseSlot2))
                    selectedSlot = Convert.ToInt32(verticalToolbar.buttons[1].name);
                else if (input.IsDown(Config.Controls.ChooseSlot3))
                    selectedSlot = Convert.ToInt32(verticalToolbar.buttons[2].name);
                else if (input.IsDown(Config.Controls.ChooseSlot4))
                    selectedSlot = Convert.ToInt32(verticalToolbar.buttons[3].name);
                else if (input.IsDown(Config.Controls.ChooseSlot5))
                    selectedSlot = Convert.ToInt32(verticalToolbar.buttons[4].name);

                if (selectedSlot >= 0)
                {
                    currentToolIndex = selectedSlot;
                    modOverride = true;
                }
            }

            // check current tool
            if (Game1.player.CurrentToolIndex != currentToolIndex)
            {
                if (modOverride || (triggerPolling < 300))
                {
                    Game1.player.CurrentToolIndex = currentToolIndex;
                    modOverride = false;
                }
            }

            // check polling
            if (scrolling != 0)
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
                    checkHoveredItem(scrolling);
                }
            }
            else if (released < 300)
            {
                Game1.player.CurrentToolIndex = currentToolIndex;
                int polling = this.released;
                int elapsedGameTime = Game1.currentGameTime.ElapsedGameTime.Milliseconds;
                this.released = polling + elapsedGameTime;
                if (released > 300 && !modOverride)
                {
                    Game1.player.CurrentToolIndex = currentToolIndex;
                    released = 300;
                }
            }
        }

        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void onButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!isInitiated)
                return;

            if (e.Button == Config.Controls.SwitchInventoryNext)
            {
                SwitchToNextInventory();
                this.Helper.Input.Suppress(e.Button);
                return;
            }

            if (e.Button == Config.Controls.SwitchInventoryPrev)
            {
                SwitchToPreviousInventory();
                this.Helper.Input.Suppress(e.Button);
                return;
            }

            // Handle left mouse click on vertical toolbar
            if (e.Button == SButton.MouseLeft && Game1.activeClickableMenu == null)
            {
                var mousePos = this.Helper.Input.GetCursorPosition();
                int x = (int)mousePos.GetScaledScreenPixels().X;
                int y = (int)mousePos.GetScaledScreenPixels().Y;
                
                if (verticalToolbar.isWithinBounds(x, y))
                {
                    verticalToolbar.receiveLeftClick(x, y, true);
                    this.Helper.Input.Suppress(SButton.MouseLeft);
                    return;
                }
            }

            // set scrolling
            if (e.Button == this.Config.Controls.ScrollLeft || e.Button == this.Config.Controls.ScrollRight)
            {
                this.Helper.Input.Suppress(e.Button);
                Game1.player.CurrentToolIndex = currentToolIndex;
                int num = e.Button == this.Config.Controls.ScrollLeft ? -1 : 1;
                checkHoveredItem(num);
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
        private void onButtonReleased(object? sender, ButtonReleasedEventArgs e)
        {
            if (!isInitiated)
                return;

            if (e.Button == this.Config.Controls.ScrollLeft || e.Button == this.Config.Controls.ScrollRight)
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
        private void onMenuChanged(object? sender, MenuChangedEventArgs e)
        {
            if (isInitiated && e.NewMenu is GameMenu menu && menu.currentTab == GameMenu.inventoryTab)
            {
                if (menu.pages.Count > GameMenu.inventoryTab && menu.pages[GameMenu.inventoryTab] is not ModInventoryPage)
                    menu.pages[GameMenu.inventoryTab] = new ModInventoryPage(menu.xPositionOnScreen, menu.yPositionOnScreen, menu.width, menu.height, _inventoryManager, Config.Controls.ShowInventoryIndicator);
            }
        }

        private void checkHoveredItem(int num)
        {
            if (Game1.player.UsingTool || Game1.dialogueUp || Game1.eventUp || (Game1.player.CurrentTool is not StardewValley.Tools.Pickaxe && !Game1.player.CanMove))
                return;
            if (Game1.options.invertScrollDirection)
                num *= -1;

            List<int> selectableSlots = Enumerable.Range(0, Math.Min(12, Game1.player.Items.Count))
                .Concat(verticalToolbar.GetSlotIndices())
                .Where(index => index >= 0 && verticalToolbar.GetItemAtSlot(index) != null)
                .Distinct()
                .OrderBy(index => index)
                .ToList();

            if (selectableSlots.Count == 0)
                return;

            if (num > 0)
                currentToolIndex = selectableSlots.FirstOrDefault(index => index > currentToolIndex, selectableSlots[0]);
            else
                currentToolIndex = selectableSlots.LastOrDefault(index => index < currentToolIndex, selectableSlots[^1]);

            modOverride = true;
        }

        /// <summary>Raised after the player scrolls the mouse wheel.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void onMouseWheelScrolled(object? sender, MouseWheelScrolledEventArgs e)
        {
            if (!isInitiated || Game1.activeClickableMenu != null || e.Delta == 0)
                return;

            currentToolIndex = pendingWheelSelection ?? Game1.player.CurrentToolIndex;
            modOverride = false;
            verticalToolbar.RefreshInventorySlots();
            checkHoveredItem(e.Delta > 0 ? 1 : -1);
            if (modOverride)
                pendingWheelSelection = currentToolIndex;
        }

        /// <summary>Raised after the player loads a save slot and the world is initialised.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void onSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            if (_inventoryManager == null)
            {
                this.Monitor.Log("Vertical Toolbar wasn't initialized because SV_InventorySystem is unavailable.", LogLevel.Error);
                return;
            }

            baseMaxItems = Game1.player.MaxItems;
            verticalToolbar = new VerticalToolBar(this.Orientation, VerticalToolBar.DefaultButtonCount, _inventoryManager, false, Config.Controls.ShowInventoryIndicator);
            Game1.onScreenMenus.Add(verticalToolbar);

            currentToolIndex = Game1.player.CurrentToolIndex;
            isInitiated = true;
        }

        private void ModShiftToolbar(bool right)
        {
            // This is simply shiftToolbar, but modified to not use NetCode, and taking to account the vertical toolbar
            if (Game1.player.Items == null || Game1.player.Items.Count < 12 || (Game1.player.UsingTool || Game1.dialogueUp) || (Game1.player.CurrentTool is not StardewValley.Tools.Pickaxe && !Game1.player.CanMove || (Game1.player.Items.CountItemStacks() == 0 || Game1.eventUp)) || Game1.farmEvent != null)
                return;

            if (_inventoryManager != null)
            {
                Game1.player.shiftToolbar(right);
                currentToolIndex = Game1.player.CurrentToolIndex;
                return;
            }

            Game1.playSound("shwip");
            if (Game1.player.CurrentItem != null)
                Game1.player.CurrentItem.actionWhenStopBeingHeld(Game1.player);
            if (right)
            {
                List<Item> range = Game1.player.Items.ToList().GetRange(12, baseMaxItems - 12);
                range.AddRange(Game1.player.Items.ToList().GetRange(0, 12));
                range.AddRange(Game1.player.Items.ToList().GetRange(baseMaxItems, VerticalToolBar.DefaultButtonCount));
                Game1.player.setInventory(range);
            }
            else
            {
                List<Item> range = Game1.player.Items.ToList().GetRange(baseMaxItems - 12, 12);
                for (int index = 0; index < baseMaxItems - 12; ++index)
                    range.Add(Game1.player.Items[index]);
                range.AddRange(Game1.player.Items.ToList().GetRange(baseMaxItems, VerticalToolBar.DefaultButtonCount));
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

        public override object GetApi()
        {
            return this;
        }

        public VerticalToolBar GetToolbar()
        {
            if (!isInitiated)
                throw new InvalidOperationException("The vertical toolbar is only available while a save is loaded.");

            return verticalToolbar;
        }

        public void SwitchInventory(int inventoryIndex)
        {
            if (!isInitiated || _inventoryManager == null)
                return;

            _inventoryManager.SetActiveInventoryIndex(Game1.player, inventoryIndex);
            verticalToolbar.SetDisplayedInventoryIndex(inventoryIndex);
            verticalToolbar.RefreshInventorySlots();
            currentToolIndex = Game1.player.CurrentToolIndex;
        }

        public int GetActiveInventoryIndex()
        {
            return _inventoryManager == null || !Context.IsWorldReady
                ? 0
                : _inventoryManager.GetActiveInventoryIndex(Game1.player);
        }
    }
}
