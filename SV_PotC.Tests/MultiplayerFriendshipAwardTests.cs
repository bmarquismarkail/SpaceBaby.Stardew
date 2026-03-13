using SpaceBaby.PartOfTheCommunity.Framework;

namespace SV_PotC.Tests;

internal sealed class MultiplayerFriendshipAwardTests
{
    public void RunAll()
    {
        Run(nameof(ShopBonus_DoesNotAwardFarmhands_WhenOnlyLocalHostHasShopMenuOpen), ShopBonus_DoesNotAwardFarmhands_WhenOnlyLocalHostHasShopMenuOpen);
        Run(nameof(FestivalBonus_DoesNotAwardAllFarmers_FromHostCurrentLocationOnly), FestivalBonus_DoesNotAwardAllFarmers_FromHostCurrentLocationOnly);
        Run(nameof(InitialShippingBonus_DoesNotReplayFullShippedHistory_ForLateOrAffectedFarmhands), InitialShippingBonus_DoesNotReplayFullShippedHistory_ForLateOrAffectedFarmhands);
        Run(nameof(UniqueShippingBonus_DoesNotAwardMoreThanOnce_PerObservedShippingDelta), UniqueShippingBonus_DoesNotAwardMoreThanOnce_PerObservedShippingDelta);
        Run(nameof(DailyQuestBonus_IsBoundToTheFarmerWhoCompletedTheQuest), DailyQuestBonus_IsBoundToTheFarmerWhoCompletedTheQuest);
    }

    private static void Run(string name, Action test)
    {
        try
        {
            test();
            Console.WriteLine($"PASS {name}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL {name}: {ex.Message}");
            Environment.ExitCode = 1;
        }
    }

    private void ShopBonus_DoesNotAwardFarmhands_WhenOnlyLocalHostHasShopMenuOpen()
    {
        Dictionary<string, string> shops = new()
        {
            ["SeedShop"] = "Pierre"
        };
        FarmerSession hostSession = new();
        FarmerSession farmhandSession = new();

        bool hostAwarded = MultiplayerRewardLogic.TryClaimShopBonus(
            localPlayerId: 100,
            evaluatedFarmerId: 100,
            currentLocationName: "SeedShop",
            hasOpenShopMenu: true,
            hasHeldItem: true,
            shops: shops,
            session: hostSession,
            out string? hostOwner
        );

        bool farmhandAwarded = MultiplayerRewardLogic.TryClaimShopBonus(
            localPlayerId: 100,
            evaluatedFarmerId: 200,
            currentLocationName: "SeedShop",
            hasOpenShopMenu: true,
            hasHeldItem: true,
            shops: shops,
            session: farmhandSession,
            out _
        );

        Assert.True(hostAwarded, "The acting local player should receive the shop bonus.");
        Assert.Equal("Pierre", hostOwner, "The shop owner should be resolved for the acting player.");
        Assert.False(farmhandAwarded, "A host-local shop menu must not award unrelated farmhands.");
    }

    private void FestivalBonus_DoesNotAwardAllFarmers_FromHostCurrentLocationOnly()
    {
        FarmerSession hostSession = new();
        FarmerSession farmhandSession = new();

        bool hostAwarded = MultiplayerRewardLogic.TryClaimFestivalBonus(
            localPlayerId: 100,
            evaluatedFarmerId: 100,
            isLocalPlayerAtFestival: true,
            session: hostSession
        );

        bool farmhandAwarded = MultiplayerRewardLogic.TryClaimFestivalBonus(
            localPlayerId: 100,
            evaluatedFarmerId: 200,
            isLocalPlayerAtFestival: true,
            session: farmhandSession
        );

        Assert.True(hostAwarded, "The local player should receive the festival bonus once.");
        Assert.False(farmhandAwarded, "Host festival state must not fan out to other farmers.");
    }

    private void InitialShippingBonus_DoesNotReplayFullShippedHistory_ForLateOrAffectedFarmhands()
    {
        PlayerData playerData = new();

        int firstAward = MultiplayerRewardLogic.ClaimInitialShippingBonus(playerData, currentUniqueItemsShipped: 5, perItemBonus: 2);
        int secondAward = MultiplayerRewardLogic.ClaimInitialShippingBonus(playerData, currentUniqueItemsShipped: 5, perItemBonus: 2);

        Assert.Equal(10, firstAward, "The initial shipping bonus should reflect current shipped history once.");
        Assert.Equal(0, secondAward, "The initial shipping bonus must not replay on later loads.");
    }

    private void UniqueShippingBonus_DoesNotAwardMoreThanOnce_PerObservedShippingDelta()
    {
        PlayerData playerData = new()
        {
            LastKnownUniqueItemsShipped = 3
        };

        int firstAward = MultiplayerRewardLogic.ClaimShippingDeltaBonus(playerData, currentUniqueItemsShipped: 5, perItemBonus: 2);
        int secondAward = MultiplayerRewardLogic.ClaimShippingDeltaBonus(playerData, currentUniqueItemsShipped: 5, perItemBonus: 2);

        Assert.Equal(4, firstAward, "Two newly shipped items should produce a single delta-based bonus.");
        Assert.Equal(0, secondAward, "Saving again without a new shipped item must not replay the bonus.");
    }

    private void DailyQuestBonus_IsBoundToTheFarmerWhoCompletedTheQuest()
    {
        FarmerSession hostSession = new()
        {
            HasTrackedDailyQuest = true,
            DaysSinceDailyQuest = 0
        };
        FarmerSession farmhandSession = new();

        int hostAward = MultiplayerRewardLogic.ClaimDailyQuestBonus(hostSession, currentDayKey: 1, baseBonus: 4);
        int hostDuplicateAward = MultiplayerRewardLogic.ClaimDailyQuestBonus(hostSession, currentDayKey: 1, baseBonus: 4);
        int farmhandAward = MultiplayerRewardLogic.ClaimDailyQuestBonus(farmhandSession, currentDayKey: 1, baseBonus: 4);

        Assert.Equal(4, hostAward, "The farmer who completed the quest should get the day-zero quest bonus.");
        Assert.Equal(0, hostDuplicateAward, "The same save day must not re-award the quest bonus.");
        Assert.Equal(0, farmhandAward, "Farmhands without tracked quest completion must not receive the quest bonus.");
    }
}

internal static class Assert
{
    public static void True(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    public static void False(bool condition, string message)
    {
        if (condition)
            throw new InvalidOperationException(message);
    }

    public static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"{message} Expected: {expected}. Actual: {actual}.");
    }
}
