using System;
namespace SpaceBaby.BuildersList
{
    public class ModConfig
    {
        public bool isActive { get; set; } = true;
        public string currentRecipe { get; set; } = null;
        public bool isCooking { get; set; } = false;
        /// <summary>Extra space below the list, in UI pixels, for other HUD panels.</summary>
        public int BottomOffset { get; set; } = 96;

    }
}
