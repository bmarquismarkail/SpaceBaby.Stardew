using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System.Collections.Generic;
using System.Linq;

namespace SpaceBaby.BuildersList
{
    internal class ModEntry : Mod
    {
        ScavengerMenu scavengermenu;
        private bool isReady, isHidden;
        ModConfig config;


        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnToTitle;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            isReady = true;
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!isReady || !Context.IsWorldReady || scavengermenu == null) return;
            // GameMenu tab changes don't raise MenuChanged. Resolve the visible page now.
            CraftingPage activeCraftingPage = Game1.activeClickableMenu as CraftingPage;
            if (Game1.activeClickableMenu is GameMenu gameMenu)
                activeCraftingPage = gameMenu.GetCurrentPage() as CraftingPage;
            if (!isHidden && activeCraftingPage != null)
            {
                if (e.Button.Equals(SButton.MouseLeft) && Game1.oldKBState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl))
                {
                    List<Dictionary<ClickableTextureComponent, CraftingRecipe>> recipes = this.Helper.Reflection.GetField<List<Dictionary<ClickableTextureComponent, CraftingRecipe>>>(activeCraftingPage, "pagesOfCraftingRecipes").GetValue();

                    int currentCraftingPagePage = this.Helper.Reflection.GetField<int>(activeCraftingPage, "currentCraftingPage").GetValue();
                    if (currentCraftingPagePage < 0 || currentCraftingPagePage >= recipes.Count)
                        return;
                    foreach (ClickableTextureComponent button in recipes[currentCraftingPagePage].Keys)
                    {
                        if (button.containsPoint((int)e.Cursor.ScreenPixels.X, (int)e.Cursor.ScreenPixels.Y))
                        {
                            scavengermenu.ScavengerRecipe = recipes[currentCraftingPagePage][button];
                            scavengermenu.recipeListNeedsUpdate = true;
                            this.config.currentRecipe = scavengermenu.ScavengerRecipe.name;
                            this.config.isCooking = scavengermenu.ScavengerRecipe.isCookingRecipe;
                            this.Helper.WriteConfig<ModConfig>(this.config);
                            this.Helper.Input.Suppress(e.Button);
                            return;
                        }
                    }
                }
            }
            if(e.Button.Equals(SButton.E) && Game1.oldKBState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl))
            {
                isHidden = !isHidden;
                if (!isHidden)
                {
                    Game1.onScreenMenus.Add(scavengermenu);
                }
                else
                {
                    Game1.onScreenMenus.Remove(scavengermenu); 
                }
                this.Helper.Input.Suppress(e.Button);
            }
        }

        private void OnReturnToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            isReady = false;
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            this.config = this.Helper.ReadConfig<ModConfig>();
            scavengermenu = new ScavengerMenu(null, this.Helper.Reflection, this.config.BottomOffset);
            isHidden = !this.config.isActive;
            if (this.config.currentRecipe != null)
            { 
                scavengermenu.ScavengerRecipe = new CraftingRecipe(this.config.currentRecipe, this.config.isCooking);
                scavengermenu.recipeListNeedsUpdate = true;
            }

            if (!isHidden) Game1.onScreenMenus.Add(scavengermenu);
            isReady = true;
        }

    }
}
