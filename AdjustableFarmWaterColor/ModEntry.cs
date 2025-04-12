using System;
using SpaceBaby.AdjustableFarmWaterColor.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using xTile.Dimensions;
using StardewValley;
using StardewValley.Locations;
using System.Linq;

namespace SpaceBaby.AdjustableFarmWaterColor
{
    public class ModEntry : Mod
    {
        private ModConfig Config;
        private Farm Farm;

        public override void Entry(IModHelper helper)
        {
            Farm = Game1.getLocationFromName("Farm") as Farm;
            this.Config = this.Helper.ReadConfig<ModConfig>();
            helper.Events.Display.Rendering += ChangeWater;
        }

        private void ChangeWater(object sender, RenderingEventArgs e)
        {
            if (Farm is null) return;
            if(Farm.waterColor.Value != this.Config.waterColor)
                this.Farm.waterColor.Value = this.Config.waterColor;
        }
    }
}
