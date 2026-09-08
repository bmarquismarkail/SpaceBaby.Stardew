using System;
using System.Collections.Generic;

namespace SpaceBaby.PartOfTheCommunity.Framework
{
    /// <summary>Contains pure decision logic for multiplayer friendship rewards.</summary>
    public static class MultiplayerRewardLogic
    {
        internal static int GetBundleBonus(int bundleCount, ModConfig config) => bundleCount * config.UjimaBonusStore;
        public static int ClaimPersistentQuestBonus(PlayerData data, int day, int baseBonus)
        {
            if (data.LastDailyQuestDay is not int completedDay || data.LastDailyQuestRewardDay == day)
                return 0;
            int age = day - completedDay;
            if (age < 0 || age >= 3)
                return 0;
            data.LastDailyQuestRewardDay = day;
            return baseBonus / (1 << age);
        }

        public static bool ClaimFamilyEvent(PlayerData data, int day, bool marriedToday, int childCount)
        {
            bool birth = data.KnownChildCount.HasValue && childCount > data.KnownChildCount.Value;
            data.KnownChildCount = childCount;
            bool wedding = marriedToday && data.LastWeddingRewardDay != day;
            if (wedding)
                data.LastWeddingRewardDay = day;
            return birth || wedding;
        }
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
