namespace SpaceBaby.PartOfTheCommunity.Framework
{
    /// <summary>Represents a relationship definition for JSON serialization.</summary>
    public class RelationshipData
    {
        /// <summary>The first character's name.</summary>
        public string CharacterA { get; set; }

        /// <summary>Character A's relationship to character B.</summary>
        public Relationship RelationshipA { get; set; }

        /// <summary>The second character's name.</summary>
        public string CharacterB { get; set; }

        /// <summary>Character B's relationship to character A.</summary>
        public Relationship RelationshipB { get; set; }

        /// <summary>An optional Stardew 1.6 game-state query that gates this relationship bonus link.</summary>
        public string UnlockCondition { get; set; } = string.Empty;
    }
}