using System.Reflection;
using System.Linq;
using Newtonsoft.Json;
using SpaceBaby.PartOfTheCommunity.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Framework.Logging;
using SV_PotC.Api.ConsumerSmoke;

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
        Run(nameof(RelationshipExtensions_IncludeMarriageDerivedInLaws), RelationshipExtensions_IncludeMarriageDerivedInLaws);
        Run(nameof(CharacterInfo_TracksRelationshipsThroughPublicApiModel), CharacterInfo_TracksRelationshipsThroughPublicApiModel);
        Run(nameof(CharacterInfo_DoesNotDuplicateIdenticalRelationships), CharacterInfo_DoesNotDuplicateIdenticalRelationships);
        Run(nameof(CharacterInfo_RespectsUnlockConditions), CharacterInfo_RespectsUnlockConditions);
        Run(nameof(ApiContract_PublicMetadataIsImmutable), ApiContract_PublicMetadataIsImmutable);
        Run(nameof(ApiContract_AssemblyVersionMatchesRelease), ApiContract_AssemblyVersionMatchesRelease);
        Run(nameof(ApiConsumer_CanRegisterAndReadThroughPublicContract), ApiConsumer_CanRegisterAndReadThroughPublicContract);
        Run(nameof(CharacterManager_DoesNotDiscardRegistrationsAfterInitialization), CharacterManager_DoesNotDiscardRegistrationsAfterInitialization);
        Run(nameof(FlatCharacterPack_AutoMirrorsRelationshipsAndFriendships), FlatCharacterPack_AutoMirrorsRelationshipsAndFriendships);
        Run(nameof(FlatCharacterPack_LoadsConditionalUnlockMetadata), FlatCharacterPack_LoadsConditionalUnlockMetadata);
        Run(nameof(CharacterPackFlat_FriendsAcceptsArrayOrObjectSyntax), CharacterPackFlat_FriendsAcceptsArrayOrObjectSyntax);
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
        Assert.True(Enum.IsDefined(typeof(Relationship), nameof(Relationship.FatherInLaw)), "The API should expose a FatherInLaw relationship for marriage-derived family links.");
        Assert.True(Enum.IsDefined(typeof(Relationship), nameof(Relationship.MotherInLaw)), "The API should expose a MotherInLaw relationship for marriage-derived family links.");
        Assert.True(Enum.IsDefined(typeof(Relationship), nameof(Relationship.BrotherInLaw)), "The API should expose a BrotherInLaw relationship for marriage-derived family links.");
        Assert.True(Enum.IsDefined(typeof(Relationship), nameof(Relationship.SisterInLaw)), "The API should expose a SisterInLaw relationship for marriage-derived family links.");
        Assert.True(Enum.IsDefined(typeof(Relationship), nameof(Relationship.SonInLaw)), "The API should expose a SonInLaw relationship for marriage-derived family links.");
        Assert.True(Enum.IsDefined(typeof(Relationship), nameof(Relationship.DaughterInLaw)), "The API should expose a DaughterInLaw relationship for marriage-derived family links.");
    }

    private void RelationshipExtensions_IncludeMarriageDerivedInLaws()
    {
        Assert.Equal(Relationship.SonInLaw, Relationship.FatherInLaw.GetInverse(sourceIsMale: true), "A male source should invert FatherInLaw into SonInLaw.");
        Assert.Equal(Relationship.DaughterInLaw, Relationship.FatherInLaw.GetInverse(sourceIsMale: false), "A female source should invert FatherInLaw into DaughterInLaw.");
        Assert.Equal(Relationship.BrotherInLaw, Relationship.BrotherInLaw.GetInverse(sourceIsMale: true), "A male source should remain a BrotherInLaw from the counterpart's perspective.");
        Assert.Equal(Relationship.SisterInLaw, Relationship.BrotherInLaw.GetInverse(sourceIsMale: false), "A female source should invert BrotherInLaw into SisterInLaw.");

        Assert.True(Relationship.Father.TryGetMarriageDerivedRelationship(playerIsMale: false, out Relationship playerToRelative, out Relationship relativeToPlayer), "A spouse's parent should produce in-law relationships when married.");
        Assert.Equal(Relationship.FatherInLaw, playerToRelative, "A spouse's father should become a father-in-law to the player.");
        Assert.Equal(Relationship.DaughterInLaw, relativeToPlayer, "The spouse's father should see a female player as a daughter-in-law.");

        Assert.True(Relationship.HalfBrother.TryGetMarriageDerivedRelationship(playerIsMale: false, out playerToRelative, out relativeToPlayer), "A spouse's sibling should produce sibling-in-law relationships when married.");
        Assert.Equal(Relationship.BrotherInLaw, playerToRelative, "A male spouse sibling should become a brother-in-law to the player.");
        Assert.Equal(Relationship.SisterInLaw, relativeToPlayer, "A male spouse sibling should see a female player as a sister-in-law.");
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

    private void CharacterInfo_RespectsUnlockConditions()
    {
        CharacterInfo leo = new("Leo", isMale: true, unlockCondition: "FALSE");
        CharacterInfo linus = new("Linus", isMale: true);

        leo.AddRelationship(Relationship.Friend, linus, unlockCondition: "FALSE");

        Assert.False(leo.IsUnlocked(), "A FALSE game-state query should block PotC friendship bonuses for the character.");
        Assert.False(leo.Relationships[0].IsUnlocked(), "A FALSE game-state query should also block the conditional relationship link.");
    }

    private void ApiContract_PublicMetadataIsImmutable()
    {
        string[] forbiddenCharacterMethods =
        {
            "AddRelationship",
            "TryAddRelationship",
            "IsUnlocked",
            "TryGetInstance",
            "TryGetNpc"
        };

        foreach (string methodName in forbiddenCharacterMethods)
        {
            MethodInfo? method = typeof(CharacterInfo).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            Assert.True(method == null, $"CharacterInfo.{methodName} must remain behind the provider boundary.");
        }

        Assert.False(
            typeof(CharacterRelationship).GetConstructors(BindingFlags.Instance | BindingFlags.Public).Any(),
            "Consumers must not be able to construct relationship entries outside the registration API."
        );

        CharacterInfo robin = new("Robin", isMale: false);
        CharacterInfo sebastian = new("Sebastian", isMale: true);
        robin.AddRelationship(Relationship.StepMother, sebastian);

        IList<CharacterRelationship> relationships = (IList<CharacterRelationship>)robin.Relationships;
        Assert.Throws<NotSupportedException>(
            () => relationships.Clear(),
            "The relationship metadata collection must reject consumer mutation."
        );
    }

    private void ApiContract_AssemblyVersionMatchesRelease()
    {
        Version? version = typeof(IPartOfTheCommunityApi).Assembly.GetName().Version;
        Assert.Equal(new Version(1, 4, 0, 0), version, "The public API assembly version should match the 1.4.0 release.");
    }

    private void ApiConsumer_CanRegisterAndReadThroughPublicContract()
    {
        var (manager, _) = CreateCharacterManager();
        IReadOnlyDictionary<string, CharacterInfo> characters = PotcApiConsumer.RegisterPair((IPartOfTheCommunityApi)manager);

        Assert.True(characters.ContainsKey("ConsumerSmokeA"), "A separately compiled consumer should be able to register a character.");
        Assert.True(
            characters["ConsumerSmokeA"].Relationships.Any(p => p.Character.Name == "ConsumerSmokeB" && p.Relationship == Relationship.Brother),
            "A separately compiled consumer should be able to create and read a reciprocal relationship."
        );
        Assert.True(
            characters["ConsumerSmokeB"].Relationships.Any(p => p.Character.Name == "ConsumerSmokeA" && p.Relationship == Relationship.Sister),
            "The provider should retain control of the inverse relationship."
        );
    }

    private void CharacterManager_DoesNotDiscardRegistrationsAfterInitialization()
    {
        var (managerObject, _) = CreateCharacterManager();
        CharacterManager manager = (CharacterManager)managerObject;
        IPartOfTheCommunityApi api = manager;

        FieldInfo isLoaded = typeof(CharacterManager).GetField("IsLoaded", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate CharacterManager.IsLoaded.");
        isLoaded.SetValue(manager, true);

        Assert.True(api.TryRegisterCharacter("LoadOrderNPC", isMale: true), "Registration should succeed once baseline initialization is complete.");
        manager.LoadCharacters();
        Assert.True(api.IsCharacterRegistered("LoadOrderNPC"), "An idempotent load call must not discard an API registration.");
    }

    private void FlatCharacterPack_AutoMirrorsRelationshipsAndFriendships()
    {
        var (manager, loadFlatCharacterPack) = CreateCharacterManager();

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

    private void FlatCharacterPack_LoadsConditionalUnlockMetadata()
    {
        var (manager, loadFlatCharacterPack) = CreateCharacterManager();

        CharacterPackFlat pack = new()
        {
            Characters = new Dictionary<string, CharacterEntry>(StringComparer.OrdinalIgnoreCase)
            {
                ["leo"] = new CharacterEntry
                {
                    DisplayName = "Leo",
                    Gender = "M",
                    UnlockCondition = "PLAYER_HAS_MAIL Current leoMoved",
                    Friends = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["linus"] = true
                    },
                    FriendConditions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["linus"] = "FALSE"
                    }
                },
                ["linus"] = new CharacterEntry
                {
                    DisplayName = "Linus",
                    Gender = "M"
                }
            }
        };

        loadFlatCharacterPack.Invoke(manager, new object[] { pack, "conditional-pack.json" });

        var characters = ((IPartOfTheCommunityApi)manager).GetAllCharacters();
        CharacterInfo leo = characters["Leo"];
        CharacterInfo linus = characters["Linus"];

        Assert.Equal("PLAYER_HAS_MAIL Current leoMoved", leo.UnlockCondition, "Flat-pack characters should preserve their unlock condition metadata.");
        Assert.True(leo.Relationships.Any(p => p.Character.Name == "Linus" && p.Relationship == Relationship.Friend && p.UnlockCondition == "FALSE"), "Flat-pack friend conditions should be preserved on Leo's own relationship entry.");
        Assert.True(linus.Relationships.Any(p => p.Character.Name == "Leo" && p.Relationship == Relationship.Friend && p.UnlockCondition == "FALSE"), "Flat-pack friend conditions should also be mirrored onto Linus's reciprocal friendship relationship metadata.");
    }

    private static (object Manager, MethodInfo LoadMethod) CreateCharacterManager()
    {
        Type managerType = typeof(PlayerData).Assembly.GetType("SpaceBaby.PartOfTheCommunity.Framework.CharacterManager")
            ?? throw new InvalidOperationException("Could not locate CharacterManager for the flat-pack regression tests.");

        object manager = Activator.CreateInstance(
            managerType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object?[] { null!, new TestMonitor() },
            culture: null
        ) ?? throw new InvalidOperationException("Could not create CharacterManager for the flat-pack regression tests.");

        MethodInfo loadFlatCharacterPack = managerType.GetMethod("LoadFlatCharacterPack", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate LoadFlatCharacterPack.");

        return (manager, loadFlatCharacterPack);
    }

    private void CharacterPackFlat_FriendsAcceptsArrayOrObjectSyntax()
    {
        const string objectJson = """
        {
          "characters": {
            "example": {
              "displayName": "Example",
              "gender": "F",
              "friends": {
                "sam": true,
                "sebastian": true
              }
            }
          }
        }
        """;

        const string arrayJson = """
        {
          "characters": {
            "example": {
              "displayName": "Example",
              "gender": "F",
              "friends": ["sam", "sebastian"]
            }
          }
        }
        """;

        CharacterPackFlat objectPack = JsonConvert.DeserializeObject<CharacterPackFlat>(objectJson)
            ?? throw new InvalidOperationException("Object-style friend JSON should deserialize.");
        CharacterPackFlat arrayPack = JsonConvert.DeserializeObject<CharacterPackFlat>(arrayJson)
            ?? throw new InvalidOperationException("Array-style friend JSON should deserialize.");

        Assert.True(objectPack.Characters["example"].Friends.ContainsKey("sam"), "The original object syntax should keep working for friend entries.");
        Assert.True(arrayPack.Characters["example"].Friends.ContainsKey("sam"), "The new array syntax should deserialize friend names into the same dictionary.");
        Assert.True(arrayPack.Characters["example"].Friends["sebastian"], "Array-style friend entries should map to a true flag internally.");
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

    public static void Throws<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }
}
