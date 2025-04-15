using System;
using StardewValley.Menus;
using StardewValley;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Threading;
using StardewModdingAPI;

namespace SpaceBaby.RadialMenu.Framework
{
  public class RadialMenu : IClickableMenu
  {
    List<ClickableComponent> buttons = new List<ClickableComponent>();
    IMonitor Monitor;
    readonly double ring_radius = 150.0f;
    Vector2 selectedPosition = Vector2.Zero;
    int previousButtonIndex = Game1.player.CurrentToolIndex;
    private int lastToolIndex = -1;
    private bool visible = false;
    private int visibleTicks = 0;
    private const int maxVisibleTicks = 60; // 1 second at 60fps

    //the offset in percentage
    // .25 = 25% = 1.5~ rads = 90 degree offset
    double offset = -.25d;
    private double animatedIndex = 0.0; // Smooth version of CurrentToolButtonIndex
    private const double spinSpeed = 0.15; // Higher = faster interpolation

        public RadialMenu(IMonitor monitor):
           base()
      {
          Monitor = monitor;
        buttons = GetCurrentItems();
    }

    List<ClickableComponent> GetCurrentItems()
    {
        List<ClickableComponent> buttonList = new List<ClickableComponent>();
        List<Item> itemList =  Game1.player.Items.ToList().GetRange(0, 12);
        for ( int index = 0; index< itemList.Count(); index++)
        {
            if (itemList[index] != null)
            {
                buttonList.Add( new ClickableComponent(new Rectangle(0, 1, 2, 3), string.Concat((object)index) ) );
            }
        }

        return buttonList;
    }

    private float getFadeAlpha()
    {
        if (!visible) return 0f;

        float alpha = visibleTicks / (float)maxVisibleTicks;
        // Ease out curve: smoother fade
        return MathHelper.Clamp(alpha * alpha, 0f, 1f);
    }
    
    public override void update(GameTime time)
    {
        base.update(time);

        int currentIndex = Game1.player.CurrentToolIndex;

        if (currentIndex != lastToolIndex)
        {
            visible = true;
            visibleTicks = maxVisibleTicks;
            lastToolIndex = currentIndex;
        }

        else if (visibleTicks > 0)
        {
            visibleTicks--;
            if (visibleTicks == 0)
                visible = false;
        }
    }

    public override void draw(SpriteBatch b)
    {
      float fade = getFadeAlpha();

      if (fade <= 0f || Game1.activeClickableMenu != null)
        return;

      buttons = GetCurrentItems();

      int FarmerX = (int)Game1.player.getLocalPosition(Game1.viewport).X;
      int FarmerY = (int)Game1.player.getLocalPosition(Game1.viewport).Y - (Game1.player.GetBoundingBox().Height * 2);

      // Find current selected tool index
      ClickableComponent CurrentToolButton = buttons.Find(x => (Game1.player.CurrentToolIndex == Convert.ToInt32(x.name)));
      int CurrentToolButtonIndex = (CurrentToolButton != null)
        ? Convert.ToInt32(CurrentToolButton.name)
        : Array.FindIndex(Game1.player.Items.ToArray(), (i => !(i is null)));

      // === Smooth animate toward CurrentToolButtonIndex ===
      double currentIndexDouble = (double)CurrentToolButtonIndex;
      double total = buttons.Count;

      // Wrap-around shortest-path interpolation
      double delta = (currentIndexDouble - animatedIndex + total) % total;

      if (delta > total / 2)
        delta -= total;

      animatedIndex = (animatedIndex + delta * spinSpeed + total) % total;

      for (int i = 0; i < buttons.Count; i++)
      {
        double angle = (i - animatedIndex) / buttons.Count;

        float vecX = (float)(FarmerX + Math.Cos((offset + angle) * 2d * Math.PI) * ring_radius);
        float vecY = (float)(FarmerY + Math.Sin((offset + angle) * 2d * Math.PI) * ring_radius);

        Vector2 position = new Vector2(vecX, vecY);

        int currentItemIndex = Convert.ToInt32(buttons[i].name);

        if (Game1.player.Items[currentItemIndex] != null)
        {
          bool isSelected = (Game1.player.CurrentToolIndex == currentItemIndex);
          
          Color tint = isSelected ? Color.White : Color.Gray * 0.9f;

          float scale = isSelected ? 1.0f : buttons[i].scale * 0.5f;

          float transparency = isSelected ? 1.0f : 0.75f;
          transparency *= fade;

          Game1.player.Items[currentItemIndex].drawInMenu(b, position, scale, transparency, 0.88f, StackDrawType.Draw, tint, true);

          if (Game1.player.CurrentToolIndex == currentItemIndex)
            selectedPosition = position;
        }
      }
      base.draw(b);
    }
  }
}
