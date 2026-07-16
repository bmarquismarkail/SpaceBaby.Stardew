using SpaceBaby.PartOfTheCommunity.Framework;
using StardewModdingAPI;

namespace SV_PotC.Api.ConsumerSmoke;

/// <summary>Compile-time smoke consumer for the public PotC API contract.</summary>
public static class PotcApiConsumer
{
    /// <summary>Acquire PotC through the same SMAPI registry call used by a real consumer mod.</summary>
    public static IPartOfTheCommunityApi? GetApi(IModHelper helper)
    {
        ArgumentNullException.ThrowIfNull(helper);
        return helper.ModRegistry.GetApi<IPartOfTheCommunityApi>("SpaceBaby.PartOfTheCommunity");
    }

    /// <summary>Register two characters and a reciprocal relationship using only public API members.</summary>
    public static IReadOnlyDictionary<string, CharacterInfo> RegisterPair(IPartOfTheCommunityApi api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.TryRegisterCharacter("ConsumerSmokeA", isMale: true);
        api.TryRegisterCharacter("ConsumerSmokeB", isMale: false);
        api.TryAddRelationship(
            "ConsumerSmokeA",
            Relationship.Brother,
            "ConsumerSmokeB",
            Relationship.Sister
        );

        return api.GetAllCharacters();
    }
}
