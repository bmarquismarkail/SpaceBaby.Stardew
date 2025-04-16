using System;
using StardewValley.Menus;
using StardewValley;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

using StardewModdingAPI;

namespace SpaceBaby.RadialMenu.Framework
{
    public class RadialMenu : IClickableMenu
    {
        readonly double ring_radius = 150.0f;
        private int lastToolIndex = -1;
        private bool visible = false;
        private int unselectedItemTicks = 0;
        private int selectedItemTicks = 0;                  // Countdown just for selected item

        readonly FadeHelper SelectedItemFade = new(60, 120);
        readonly FadeHelper UnselectedItemFade = new(60, 60);

        //the offset in percentage
        // .25 = 25% = 1.5~ rads = 90 degree offset
        readonly double offset = -.25d;
        private double animatedIndex = 0.0; // Smooth version of CurrentToolButtonIndex
        private const double spinSpeed = 0.15; // Higher = faster interpolation

        public RadialMenu(IMonitor monitor) :
           base()
        {}

        static List<ClickableComponent> GetCurrentItems()
        {
            List<ClickableComponent> buttonList = new();
            List<Item> itemList = Game1.player.Items.ToList().GetRange(0, 12);
            for (int index = 0; index < itemList.Count; index++)
            {
                if (itemList[index] != null)
                {
                    buttonList.Add(new ClickableComponent(new Rectangle(0, 1, 2, 3), string.Concat((object)index)));
                }
            }

            return buttonList;
        }

        private float GetUnselectedFadeAlpha()
        {
            if (!visible) return 0f;

            return UnselectedItemFade.GetAlpha(unselectedItemTicks);
        }

        private float GetSelectedItemAlpha()
        {
            if (!visible) return 0f;

            return SelectedItemFade.GetAlpha(selectedItemTicks);
        }

        public override void update(GameTime time)
        {
            base.update(time);

            int currentIndex = Game1.player.CurrentToolIndex;

            if (currentIndex != lastToolIndex)
            {
                visible = true;
                unselectedItemTicks = UnselectedItemFade.VisibilityTime;
                selectedItemTicks = SelectedItemFade.VisibilityTime;
                lastToolIndex = currentIndex;
            }
            else
            {
                if (unselectedItemTicks > 0)
                    unselectedItemTicks--;

                if (selectedItemTicks > 0)
                    selectedItemTicks--;

                // Only mark invisible when both have expired
                if (unselectedItemTicks == 0 && selectedItemTicks == 0)
                    visible = false;
            }
        }
        private static int GetCurrentToolIndex(List<ClickableComponent> buttons)
        {
            ClickableComponent button = buttons.Find(x => Game1.player.CurrentToolIndex == Convert.ToInt32(x.name));
            return (button != null)
                ? Convert.ToInt32(button.name)
                : Array.FindIndex(Game1.player.Items.ToArray(), i => i is not null);
        }
        private void UpdateAnimatedIndex(double targetIndex, int total)
        {
            double delta = (targetIndex - animatedIndex + total) % total;
            if (delta > total / 2)
                delta -= total;

            animatedIndex = (animatedIndex + delta * spinSpeed + total) % total;
        }
        private Vector2 CalculateItemPosition(int index, int count, Vector2 center)
        {
            double angle = (index - animatedIndex) / count;
            float x = center.X + (float)Math.Cos((offset + angle) * 2d * Math.PI) * (float)ring_radius;
            float y = center.Y + (float)Math.Sin((offset + angle) * 2d * Math.PI) * (float)ring_radius;
            return new Vector2(x, y);
        }
        private void DrawItem(SpriteBatch b, Item item, Vector2 position, bool isSelected)
        {
            float alpha = isSelected ? GetSelectedItemAlpha() : GetUnselectedFadeAlpha();
            if (alpha <= 0f) return;

            float scale = isSelected ? 1.0f : 0.5f;
            float transparency = isSelected ? 1.0f : 0.75f;
            transparency *= alpha;

            Color tint = isSelected ? Color.White : Color.Gray * 0.9f;

            item.drawInMenu(b, position, scale, transparency, 0.88f, StackDrawType.Draw, tint, true);
        }
        public override void draw(SpriteBatch b)
        {
            if (Game1.activeClickableMenu != null)
                return;

            List<ClickableComponent> buttons = GetCurrentItems();
            if (buttons.Count == 0)
                return;

            int toolIndex = GetCurrentToolIndex(buttons);
            UpdateAnimatedIndex(toolIndex, buttons.Count);

            Vector2 center = Game1.player.getLocalPosition(Game1.viewport);
            center.Y -= Game1.player.GetBoundingBox().Height * 2;

            foreach (var button in buttons)
            {
                int index = Convert.ToInt32(button.name);
                Item item = Game1.player.Items[index];

                if (item == null) continue;

                Vector2 position = CalculateItemPosition(index, buttons.Count, center);
                bool isSelected = (index == Game1.player.CurrentToolIndex);
                DrawItem(b, item, position, isSelected);
            }

            base.draw(b);
        }
    }
}
