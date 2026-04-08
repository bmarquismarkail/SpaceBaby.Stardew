using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;

namespace SpaceBaby.PartOfTheCommunity.Framework
{
    /// <summary>Tracked data for a character registered with the Part of the Community API.</summary>
    public class CharacterInfo
    {
        /*********
        ** Fields
        *********/
        private readonly List<CharacterRelationship> relationships = new();


        /*********
        ** Accessors
        *********/
        /// <summary>The character type.</summary>
        public CharacterType Type { get; }

        /// <summary>The NPC name.</summary>
        public string Name { get; }

        /// <summary>Whether the NPC is male.</summary>
        public bool IsMale { get; }

        /// <summary>An optional Stardew 1.6 game-state query that must match before PotC can award this character friendship points.</summary>
        public string UnlockCondition { get; internal set; }

        /// <summary>Whether the NPC owns a shop.</summary>
        public bool IsShopOwner { get; internal set; }

        /// <summary>The NPC's known relationships with other characters.</summary>
        public IReadOnlyList<CharacterRelationship> Relationships => this.relationships;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="isMale">Whether the NPC is male.</param>
        /// <param name="name">The NPC name.</param>
        /// <param name="type">The character type.</param>
        public CharacterInfo(string name, bool isMale, CharacterType type = CharacterType.Villager, string unlockCondition = null)
        {
            this.Name = name;
            this.IsMale = isMale;
            this.Type = type;
            this.UnlockCondition = unlockCondition?.Trim() ?? string.Empty;
        }

        /// <summary>Try to add a relationship to another character.</summary>
        /// <param name="relationship">The target character's relationship to the original character (like 'Mother').</param>
        /// <param name="character">The target character.</param>
        /// <returns>Returns whether a new relationship entry was added.</returns>
        public bool TryAddRelationship(Relationship relationship, CharacterInfo character, string unlockCondition = null)
        {
            ArgumentNullException.ThrowIfNull(character);

            CharacterRelationship existing = this.relationships.FirstOrDefault(p => p.Relationship == relationship && string.Equals(p.Character.Name, character.Name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                if (string.IsNullOrWhiteSpace(existing.UnlockCondition) || string.Equals(existing.UnlockCondition, unlockCondition?.Trim(), StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!string.IsNullOrWhiteSpace(unlockCondition))
                    return false;

                this.relationships.Remove(existing);
            }

            this.relationships.Add(new CharacterRelationship(relationship, character, unlockCondition));
            return true;
        }

        /// <summary>Add a relationship to another character.</summary>
        /// <param name="relationship">The target character's relationship to the original character (like 'Mother').</param>
        /// <param name="character">The target character.</param>
        public void AddRelationship(Relationship relationship, CharacterInfo character, string unlockCondition = null)
        {
            this.TryAddRelationship(relationship, character, unlockCondition);
        }

        /// <summary>Get whether this character can currently receive PotC friendship bonuses for the current game context.</summary>
        /// <returns>Returns whether the character is currently unlocked.</returns>
        public bool IsUnlocked()
        {
            return this.IsUnlocked(player: null, location: null);
        }

        /// <summary>Get whether this character can currently receive PotC friendship bonuses.</summary>
        /// <param name="player">The player for whom the bonus would be awarded.</param>
        /// <param name="location">The location context for the check.</param>
        /// <returns>Returns whether the character is currently unlocked.</returns>
        internal bool IsUnlocked(Farmer player = null, GameLocation location = null)
        {
            return UnlockConditionHelper.IsUnlocked(this.UnlockCondition, player, location);
        }

        /// <summary>Get the in-game instance for this character.</summary>
        /// <param name="npc">The in-game instance for this character.</param>
        /// <returns>Returns whether the NPC was found.</returns>
        public bool TryGetInstance(out Character npc)
        {
            switch (this.Type)
            {
                case CharacterType.Villager:
                    npc = Game1.getCharacterFromName(this.Name, mustBeVillager: true);
                    return npc != null;

                case CharacterType.Player:
                    npc = this.Name == Game1.player.Name ? Game1.player : null;
                    return npc != null;

                case CharacterType.Child:
                    npc = Game1.player.getChildren().FirstOrDefault(p => p.Name == this.Name);
                    return npc != null;

                default:
                    throw new NotSupportedException($"Unknown character type {this.Type} for NPC {this.Name}.");
            }
        }

        /// <summary>Get the NPC for this character.</summary>
        /// <param name="npc">The NPC for this character.</param>
        /// <returns>Returns whether the NPC was found.</returns>
        public bool TryGetNpc(out NPC npc)
        {
            if (this.TryGetInstance(out Character character) && character is NPC instance)
            {
                npc = instance;
                return true;
            }

            npc = null;
            return false;
        }
    }
}
