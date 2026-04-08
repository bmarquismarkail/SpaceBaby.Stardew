using System.Collections.Generic;

namespace SpaceBaby.PartOfTheCommunity.Framework
{
    /// <summary>API that other mods can use to register characters and relationships.</summary>
    public interface IPartOfTheCommunityApi
    {
        /// <summary>Try to register a character that can have relationships with others.</summary>
        /// <param name="name">The character's name.</param>
        /// <param name="isMale">Whether the character is male.</param>
        /// <param name="type">The character type (defaults to Villager).</param>
        /// <returns>Returns whether the character was newly registered or an existing character's unlock condition was updated.</returns>
        bool TryRegisterCharacter(string name, bool isMale, CharacterType type = CharacterType.Villager);

        /// <summary>Register a character that can have relationships with others.</summary>
        /// <param name="name">The character's name.</param>
        /// <param name="isMale">Whether the character is male.</param>
        /// <param name="type">The character type (defaults to Villager).</param>
        void RegisterCharacter(string name, bool isMale, CharacterType type = CharacterType.Villager);

        /// <summary>Try to register a character with a Stardew 1.6 game-state query that can gate PotC friendship bonuses for that character.</summary>
        /// <param name="name">The character's name.</param>
        /// <param name="isMale">Whether the character is male.</param>
        /// <param name="type">The character type.</param>
        /// <param name="unlockCondition">A Stardew 1.6 game-state query string that must match before PotC can award this character friendship points, or <c>null</c>/<c>empty</c> to leave it always available.</param>
        /// <returns>Returns whether the character was newly registered or an existing character's unlock condition was updated.</returns>
        bool TryRegisterCharacter(string name, bool isMale, CharacterType type, string unlockCondition = null);

        /// <summary>Register a character with a Stardew 1.6 game-state query that can gate PotC friendship bonuses for that character.</summary>
        /// <param name="name">The character's name.</param>
        /// <param name="isMale">Whether the character is male.</param>
        /// <param name="type">The character type.</param>
        /// <param name="unlockCondition">A Stardew 1.6 game-state query string that must match before PotC can award this character friendship points, or <c>null</c>/<c>empty</c> to leave it always available.</param>
        void RegisterCharacter(string name, bool isMale, CharacterType type, string unlockCondition = null);

        /// <summary>Try to add a relationship between two characters.</summary>
        /// <param name="characterA">The first character's name.</param>
        /// <param name="relationshipA">Character A's relationship to character B.</param>
        /// <param name="characterB">The second character's name.</param>
        /// <param name="relationshipB">Character B's relationship to character A.</param>
        /// <returns>Returns whether a new relationship was added.</returns>
        bool TryAddRelationship(string characterA, Relationship relationshipA, string characterB, Relationship relationshipB);

        /// <summary>Add a relationship between two characters.</summary>
        /// <param name="characterA">The first character's name.</param>
        /// <param name="relationshipA">Character A's relationship to character B.</param>
        /// <param name="characterB">The second character's name.</param>
        /// <param name="relationshipB">Character B's relationship to character A.</param>
        void AddRelationship(string characterA, Relationship relationshipA, string characterB, Relationship relationshipB);

        /// <summary>Try to add a relationship between two characters with a Stardew 1.6 game-state query that can gate when it awards PotC friendship bonuses.</summary>
        /// <param name="characterA">The first character's name.</param>
        /// <param name="relationshipA">Character A's relationship to character B.</param>
        /// <param name="characterB">The second character's name.</param>
        /// <param name="relationshipB">Character B's relationship to character A.</param>
        /// <param name="unlockCondition">A Stardew 1.6 game-state query string that must match before this relationship can award PotC friendship bonuses, or <c>null</c>/<c>empty</c> to leave it always available.</param>
        /// <returns>Returns whether a new relationship was added.</returns>
        bool TryAddRelationship(string characterA, Relationship relationshipA, string characterB, Relationship relationshipB, string unlockCondition = null);

        /// <summary>Add a relationship between two characters with a Stardew 1.6 game-state query that can gate when it awards PotC friendship bonuses.</summary>
        /// <param name="characterA">The first character's name.</param>
        /// <param name="relationshipA">Character A's relationship to character B.</param>
        /// <param name="characterB">The second character's name.</param>
        /// <param name="relationshipB">Character B's relationship to character A.</param>
        /// <param name="unlockCondition">A Stardew 1.6 game-state query string that must match before this relationship can award PotC friendship bonuses, or <c>null</c>/<c>empty</c> to leave it always available.</param>
        void AddRelationship(string characterA, Relationship relationshipA, string characterB, Relationship relationshipB, string unlockCondition = null);

        /// <summary>Try to add a friendship between two characters (bidirectional).</summary>
        /// <param name="characterA">The first character's name.</param>
        /// <param name="characterB">The second character's name.</param>
        /// <returns>Returns whether a new friendship was added.</returns>
        bool TryAddFriendship(string characterA, string characterB);

        /// <summary>Add a friendship between two characters (bidirectional).</summary>
        /// <param name="characterA">The first character's name.</param>
        /// <param name="characterB">The second character's name.</param>
        void AddFriendship(string characterA, string characterB);

        /// <summary>Try to add a friendship between two characters (bidirectional) with a Stardew 1.6 game-state query that can gate when it awards PotC friendship bonuses.</summary>
        /// <param name="characterA">The first character's name.</param>
        /// <param name="characterB">The second character's name.</param>
        /// <param name="unlockCondition">A Stardew 1.6 game-state query string that must match before this friendship can award PotC friendship bonuses, or <c>null</c>/<c>empty</c> to leave it always available.</param>
        /// <returns>Returns whether a new friendship was added.</returns>
        bool TryAddFriendship(string characterA, string characterB, string unlockCondition);

        /// <summary>Add a friendship between two characters (bidirectional) with a Stardew 1.6 game-state query that can gate when it awards PotC friendship bonuses.</summary>
        /// <param name="characterA">The first character's name.</param>
        /// <param name="characterB">The second character's name.</param>
        /// <param name="unlockCondition">A Stardew 1.6 game-state query string that must match before this friendship can award PotC friendship bonuses, or <c>null</c>/<c>empty</c> to leave it always available.</param>
        void AddFriendship(string characterA, string characterB, string unlockCondition);

        /// <summary>Get all registered characters.</summary>
        /// <returns>A dictionary of character names to character info.</returns>
        IReadOnlyDictionary<string, CharacterInfo> GetAllCharacters();

        /// <summary>Check if a character is registered.</summary>
        /// <param name="name">The character's name.</param>
        /// <returns>True if the character is registered.</returns>
        bool IsCharacterRegistered(string name);
    }
}