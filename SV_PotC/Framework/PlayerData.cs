namespace SpaceBaby.PartOfTheCommunity.Framework
{
    public class PlayerData
    {
        public bool HasGottenInitialUjimaBonus { get; set; }
        public bool HasGottenInitialKuumbaBonus { get; set; }
        public uint? LastKnownQuestCount { get; set; }
        public int LastKnownUniqueItemsShipped { get; set; }
    }
}
