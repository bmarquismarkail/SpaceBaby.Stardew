namespace SpaceBaby.PartOfTheCommunity.Framework
{
    /// <summary>Represents a character definition for JSON serialization.</summary>
    public class CharacterData
    {
        /// <summary>The character's name.</summary>
        public string Name { get; set; }

        /// <summary>Whether the character is male.</summary>
        public bool IsMale { get; set; }

        /// <summary>The character type.</summary>
        public CharacterType Type { get; set; } = CharacterType.Villager;
    }
}