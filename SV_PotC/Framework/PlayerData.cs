namespace SpaceBaby.PartOfTheCommunity.Framework
{
    public class PlayerData
    {
        public bool HasGottenInitialUjimaBonus { get; set; }
        public bool HasGottenInitialKuumbaBonus { get; set; }
        public uint? LastKnownQuestCount { get; set; }
        public int LastKnownUniqueItemsShipped { get; set; } = 0;
        public int? LastDailyQuestDay { get; set; }
        public int? LastDailyQuestRewardDay { get; set; }
        public uint? LastKnownBillboardQuestCount { get; set; }
        public int? KnownChildCount { get; set; }
        public int? LastWeddingRewardDay { get; set; }
    }
}
