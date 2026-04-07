using System.Collections.Generic;

namespace SpaceBaby.PartOfTheCommunity.Framework
{
    /// <summary>Represents a complete character pack with characters, relationships, and friendships.</summary>
    public class CharacterPack
    {
        /// <summary>The name of this character pack.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>The author of this character pack.</summary>
        public string Author { get; set; } = string.Empty;

        /// <summary>The version of this character pack.</summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>A description of this character pack.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>The characters defined in this pack.</summary>
        public List<CharacterData> Characters { get; set; } = new List<CharacterData>();

        /// <summary>The relationships defined in this pack.</summary>
        public List<RelationshipData> Relationships { get; set; } = new List<RelationshipData>();

        /// <summary>The friendships defined in this pack.</summary>
        public List<FriendshipData> Friendships { get; set; } = new List<FriendshipData>();
    }
}