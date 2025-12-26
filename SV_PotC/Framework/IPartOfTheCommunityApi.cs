using System.Collections.Generic;

namespace SpaceBaby.PartOfTheCommunity.Framework
{
    /// <summary>API that other mods can use to register characters and relationships.</summary>
    public interface IPartOfTheCommunityApi
    {
        /// <summary>Register a character that can have relationships with others.</summary>
        /// <param name="name">The character's name.</param>
        /// <param name="isMale">Whether the character is male.</param>
        /// <param name="type">The character type (defaults to Villager).</param>
        void RegisterCharacter(string name, bool isMale, CharacterType type = CharacterType.Villager);

        /// <summary>Add a relationship between two characters.</summary>
        /// <param name="characterA">The first character's name.</param>
        /// <param name="relationshipA">Character A's relationship to character B.</param>
        /// <param name="characterB">The second character's name.</param>
        /// <param name="relationshipB">Character B's relationship to character A.</param>
        void AddRelationship(string characterA, Relationship relationshipA, string characterB, Relationship relationshipB);

        /// <summary>Add a friendship between two characters (bidirectional).</summary>
        /// <param name="characterA">The first character's name.</param>
        /// <param name="characterB">The second character's name.</param>
        void AddFriendship(string characterA, string characterB);

        /// <summary>Get all registered characters.</summary>
        /// <returns>A dictionary of character names to character info.</returns>
        IReadOnlyDictionary<string, CharacterInfo> GetAllCharacters();

        /// <summary>Check if a character is registered.</summary>
        /// <param name="name">The character's name.</param>
        /// <returns>True if the character is registered.</returns>
        bool IsCharacterRegistered(string name);
    }
}