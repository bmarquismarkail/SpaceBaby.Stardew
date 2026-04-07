using System.Reflection;
using SpaceBaby.PartOfTheCommunity.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Framework.Logging;

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
        Run(nameof(LegacyFlagMap_ConvertsToPlayerDataForCurrentPlayer), LegacyFlagMap_ConvertsToPlayerDataForCurrentPlayer);
        Run(nameof(RelationshipEnum_ContainsDocumentedApiMembers), RelationshipEnum_ContainsDocumentedApiMembers);
        Run(nameof(CharacterInfo_TracksRelationshipsThroughPublicApiModel), CharacterInfo_TracksRelationshipsThroughPublicApiModel);
        Run(nameof(CharacterInfo_DoesNotDuplicateIdenticalRelationships), CharacterInfo_DoesNotDuplicateIdenticalRelationships);
        Run(nameof(FlatCharacterPack_AutoMirrorsRelationshipsAndFriendships), FlatCharacterPack_AutoMirrorsRelationshipsAndFriendships);
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

    private void LegacyFlagMap_ConvertsToPlayerDataForCurrentPlayer()
    {
        Dictionary<string, bool> legacyFlags = new()
        {
            [nameof(PlayerData.HasGottenInitialUjimaBonus)] = true,
            [nameof(PlayerData.HasGottenInitialKuumbaBonus)] = false
        };

        bool migrated = PlayerDataMigration.TryConvertLegacyFlagMap(legacyFlags, playerId: 42, out IDictionary<long, PlayerData>? migratedData);

        Assert.True(migrated, "Legacy property-name save data should be recognized as supported migration input.");
        Assert.Equal(1, migratedData!.Count, "The migrated data should contain one player record.");
        Assert.True(migratedData.ContainsKey(42), "The current player should receive the migrated record.");
        Assert.True(migratedData[42].HasGottenInitialUjimaBonus, "Known legacy Ujima state should be preserved.");
        Assert.False(migratedData[42].HasGottenInitialKuumbaBonus, "Known legacy Kuumba state should be preserved.");
    }

    private void RelationshipEnum_ContainsDocumentedApiMembers()
    {
        Assert.True(Enum.IsDefined(typeof(Relationship), nameof(Relationship.StepDaughter)), "The API should expose the documented StepDaughter relationship.");
        Assert.True(Enum.IsDefined(typeof(Relationship), nameof(Relationship.Godmother)), "The API should expose the documented Godmother relationship.");
        Assert.True(Enum.IsDefined(typeof(Relationship), nameof(Relationship.Godson)), "The API should expose the documented Godson relationship.");
    }

    private void CharacterInfo_TracksRelationshipsThroughPublicApiModel()
    {
        CharacterInfo robin = new("Robin", isMale: false);
        CharacterInfo sebastian = new("Sebastian", isMale: true);

        robin.AddRelationship(Relationship.StepMother, sebastian);

        Assert.Equal(1, robin.Relationships.Count, "A registered API character should expose its relationship data.");
        Assert.Equal(Relationship.StepMother, robin.Relationships[0].Relationship, "The stored relationship should match the one added through the API model.");
        Assert.Equal("Sebastian", robin.Relationships[0].Character.Name, "The relationship target should preserve the other character's public name.");
    }

    private void CharacterInfo_DoesNotDuplicateIdenticalRelationships()
    {
        CharacterInfo robin = new("Robin", isMale: false);
        CharacterInfo sebastian = new("Sebastian", isMale: true);

        robin.AddRelationship(Relationship.StepMother, sebastian);
        robin.AddRelationship(Relationship.StepMother, sebastian);

        Assert.Equal(1, robin.Relationships.Count, "Adding the same relationship twice should not create duplicate entries.");
    }

    private void FlatCharacterPack_AutoMirrorsRelationshipsAndFriendships()
    {
        Type managerType = typeof(PlayerData).Assembly.GetType("SpaceBaby.PartOfTheCommunity.Framework.CharacterManager")
            ?? throw new InvalidOperationException("Could not locate CharacterManager for the flat-pack regression test.");

        object manager = Activator.CreateInstance(
            managerType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object?[] { null!, new TestMonitor() },
            culture: null
        ) ?? throw new InvalidOperationException("Could not create CharacterManager for the flat-pack regression test.");

        MethodInfo loadFlatCharacterPack = managerType.GetMethod("LoadFlatCharacterPack", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate LoadFlatCharacterPack.");

        CharacterPackFlat pack = new()
        {
            Characters = new Dictionary<string, CharacterEntry>(StringComparer.OrdinalIgnoreCase)
            {
                ["robin"] = new CharacterEntry
                {
                    DisplayName = "Robin",
                    Gender = "F",
                    Relationships = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["sebastian"] = "son"
                    },
                    Friends = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["maru"] = true
                    }
                },
                ["sebastian"] = new CharacterEntry
                {
                    DisplayName = "Sebastian",
                    Gender = "M"
                },
                ["maru"] = new CharacterEntry
                {
                    DisplayName = "Maru",
                    Gender = "F"
                }
            }
        };

        loadFlatCharacterPack.Invoke(manager, new object[] { pack, "test-pack.json" });

        var characters = ((IPartOfTheCommunityApi)manager).GetAllCharacters();

        Assert.True(characters["Robin"].Relationships.Any(p => p.Character.Name == "Sebastian" && p.Relationship == Relationship.Son), "Flat-pack relationships should preserve the declared relationship on the source character.");
        Assert.True(characters["Sebastian"].Relationships.Any(p => p.Character.Name == "Robin" && p.Relationship == Relationship.Mother), "Flat-pack relationships should add the inferred inverse relationship on the target character.");
        Assert.True(characters["Robin"].Relationships.Any(p => p.Character.Name == "Maru" && p.Relationship == Relationship.Friend), "Flat-pack friends should preserve the declared friendship on the source character.");
        Assert.True(characters["Maru"].Relationships.Any(p => p.Character.Name == "Robin" && p.Relationship == Relationship.Friend), "Flat-pack friends should also add the inverse friendship on the target character.");
    }

    private sealed class TestMonitor : IMonitor
    {
        public bool IsVerbose => false;

        public void Log(string message, LogLevel level = LogLevel.Trace)
        {
        }

        public void LogOnce(string message, LogLevel level = LogLevel.Trace)
        {
        }

        public void VerboseLog(string message)
        {
        }

        public void VerboseLog(ref VerboseLogStringHandler message)
        {
        }
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
