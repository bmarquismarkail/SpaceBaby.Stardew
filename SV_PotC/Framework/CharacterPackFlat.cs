using System;
using System.Collections.Generic;

namespace SpaceBaby.PartOfTheCommunity.Framework
{
    /// <summary>Represents the new flat character data structure for JSON serialization.</summary>
    public class CharacterPackFlat
    {
        /// <summary>The characters data with lowercase key names.</summary>
        public Dictionary<string, CharacterEntry> Characters { get; set; } = new Dictionary<string, CharacterEntry>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Represents a character entry in the flat structure.</summary>
    public class CharacterEntry
    {
        /// <summary>The display name of the character.</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>The gender of the character ("M" or "F").</summary>
        public string Gender { get; set; } = string.Empty;

        /// <summary>The character type.</summary>
        public string Type { get; set; } = "Villager";

        /// <summary>Relationships where key is the other character's lowercase name and value is this character's relationship to them.</summary>
        public Dictionary<string, string> Relationships { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Friends where key is the other character's lowercase name and value is true.</summary>
        public Dictionary<string, bool> Friends { get; set; } = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
    }
}