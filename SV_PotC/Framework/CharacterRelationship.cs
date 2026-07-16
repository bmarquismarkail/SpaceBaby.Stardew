using System;
using StardewValley;

namespace SpaceBaby.PartOfTheCommunity.Framework
{
    /// <summary>Tracked data for a character relationship exposed through the API.</summary>
    public sealed class CharacterRelationship
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The target character.</summary>
        public CharacterInfo Character { get; }

        /// <summary>The target character's relationship to the original character (like 'Mother').</summary>
        public Relationship Relationship { get; }

        /// <summary>An optional Stardew 1.6 game-state query that must match before this relationship can award PotC friendship points.</summary>
        public string UnlockCondition { get; }

        /// <summary>Whether this is a friend (non-family) relationship.</summary>
        public bool IsFriend => this.Relationship == Relationship.Friend;

        /// <summary>Whether this is a family relationship.</summary>
        public bool IsFamily => !this.IsFriend && this.Relationship != Relationship.WarTorn;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="relationship">The target character's relationship to the original character (like 'Mother').</param>
        /// <param name="character">The target character.</param>
        internal CharacterRelationship(Relationship relationship, CharacterInfo character, string unlockCondition = null)
        {
            ArgumentNullException.ThrowIfNull(character);

            this.Relationship = relationship;
            this.Character = character;
            this.UnlockCondition = unlockCondition?.Trim() ?? string.Empty;
        }

        /// <summary>Get whether this relationship is currently unlocked for PotC bonuses in the current game context.</summary>
        /// <returns>Returns whether the relationship is currently unlocked.</returns>
        internal bool IsUnlocked()
        {
            return this.IsUnlockedFor(player: null, location: null);
        }

        /// <summary>Get whether this relationship is currently unlocked for PotC bonuses.</summary>
        /// <param name="player">The player for whom the bonus would be awarded.</param>
        /// <param name="location">The location context for the check.</param>
        /// <returns>Returns whether the relationship is currently unlocked.</returns>
        internal bool IsUnlockedFor(Farmer player = null, GameLocation location = null)
        {
            return this.Character.IsUnlockedFor(player, location)
                && UnlockConditionHelper.IsUnlocked(this.UnlockCondition, player, location);
        }
    }
}
