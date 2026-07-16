namespace SpaceBaby.PartOfTheCommunity.Framework
{
    /// <summary>Represents a friendship definition for JSON serialization.</summary>
    public class FriendshipData
    {
        /// <summary>The first character's name.</summary>
        public string CharacterA { get; set; } = string.Empty;

        /// <summary>The second character's name.</summary>
        public string CharacterB { get; set; } = string.Empty;

        /// <summary>An optional Stardew 1.6 game-state query that gates this friendship bonus link.</summary>
        public string UnlockCondition { get; set; } = string.Empty;
    }
}