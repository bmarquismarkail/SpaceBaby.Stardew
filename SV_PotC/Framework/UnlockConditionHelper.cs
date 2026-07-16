using System;
using StardewModdingAPI;
using StardewValley;

namespace SpaceBaby.PartOfTheCommunity.Framework
{
    /// <summary>Evaluates optional game-state conditions that gate PotC friendship bonuses.</summary>
    internal static class UnlockConditionHelper
    {
        /// <summary>The logger used for unlock-condition diagnostics.</summary>
        public static IMonitor Monitor { get; set; }
        /// <summary>Check whether a condition matches for the given player/location context.</summary>
        /// <param name="unlockCondition">The Stardew 1.6 game-state query to evaluate.</param>
        /// <param name="player">The player for whom the condition should be evaluated.</param>
        /// <param name="location">The location context for the query.</param>
        /// <returns>Returns whether the condition is currently met.</returns>
        public static bool IsUnlocked(string unlockCondition, Farmer player = null, GameLocation location = null)
        {
            unlockCondition = unlockCondition?.Trim();
            if (string.IsNullOrWhiteSpace(unlockCondition))
                return true;

            if (GameStateQuery.IsImmutablyTrue(unlockCondition))
                return true;

            if (GameStateQuery.IsImmutablyFalse(unlockCondition))
                return false;

            Farmer resolvedPlayer = player ?? Game1.player;
            GameLocation resolvedLocation = location ?? resolvedPlayer?.currentLocation ?? Game1.currentLocation;
            if (resolvedPlayer == null)
                return false;

            try
            {
                return GameStateQuery.CheckConditions(
                    unlockCondition,
                    resolvedLocation,
                    resolvedPlayer,
                    targetItem: null,
                    inputItem: null,
                    random: null,
                    ignoreQueryKeys: null
                );
            }
            catch (Exception ex)
            {
                Monitor?.Log($"Failed to parse unlock condition '{unlockCondition}': {ex}", LogLevel.Trace);
                return false;
            }
        }
    }
}
