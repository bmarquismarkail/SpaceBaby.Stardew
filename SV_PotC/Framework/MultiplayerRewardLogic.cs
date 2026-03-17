using System;
using System.Collections.Generic;

namespace SpaceBaby.PartOfTheCommunity.Framework
{
    /// <summary>Contains pure decision logic for multiplayer friendship rewards.</summary>
    public static class MultiplayerRewardLogic
    {
        public static bool TryClaimShopBonus(long localPlayerId, long evaluatedFarmerId, string currentLocationName, bool hasOpenShopMenu, bool hasHeldItem, IReadOnlyDictionary<string, string> shops, FarmerSession session, out string shopOwnerName)
        {
            shopOwnerName = null;

            if (localPlayerId != evaluatedFarmerId || !hasOpenShopMenu || !hasHeldItem || string.IsNullOrWhiteSpace(currentLocationName))
                return false;

            if (!shops.TryGetValue(currentLocationName, out shopOwnerName))
                return false;

            string shopKey = $"shop_{evaluatedFarmerId}_{shopOwnerName}";
            if (session.NearbyTalksSeen.Contains(shopKey))
                return false;

            session.NearbyTalksSeen.Add(shopKey);
            session.HasShopped = true;
            return true;
        }

        public static bool TryClaimFestivalBonus(long localPlayerId, long evaluatedFarmerId, bool isLocalPlayerAtFestival, FarmerSession session)
        {
            if (localPlayerId != evaluatedFarmerId || !isLocalPlayerAtFestival || session.HasEnteredFestival)
                return false;

            session.HasEnteredFestival = true;
            return true;
        }

        public static int ClaimInitialShippingBonus(PlayerData playerData, int currentUniqueItemsShipped, int perItemBonus)
        {
            if (playerData.HasGottenInitialKuumbaBonus)
                return 0;

            playerData.HasGottenInitialKuumbaBonus = true;
            playerData.LastKnownUniqueItemsShipped = currentUniqueItemsShipped;
            return currentUniqueItemsShipped * perItemBonus;
        }

        public static int ClaimShippingDeltaBonus(PlayerData playerData, int currentUniqueItemsShipped, int perItemBonus)
        {
            if (currentUniqueItemsShipped <= playerData.LastKnownUniqueItemsShipped)
                return 0;

            int delta = currentUniqueItemsShipped - playerData.LastKnownUniqueItemsShipped;
            playerData.LastKnownUniqueItemsShipped = currentUniqueItemsShipped;
            return delta * perItemBonus;
        }

        public static int ClaimDailyQuestBonus(FarmerSession session, int currentDayKey, int baseBonus)
        {
            if (!session.HasTrackedDailyQuest || session.DaysSinceDailyQuest >= 3)
                return 0;

            if (session.LastDailyQuestBonusDayKey == currentDayKey)
                return 0;

            session.LastDailyQuestBonusDayKey = currentDayKey;
            return baseBonus / (int)Math.Pow(2, session.DaysSinceDailyQuest);
        }
    }
}
