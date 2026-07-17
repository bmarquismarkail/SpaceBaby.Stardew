# Part of the Community - API Documentation

Part of the Community now provides an API that allows other mods to register custom characters and relationships. This document explains how to use the API.

## Add the dependency and compile-time reference

The API uses public contract types from `PartOfTheCommunity.dll`. Add PotC as a required dependency so SMAPI loads the provider before your mod and the contract assembly is always available:

```json
"Dependencies": [
  {
    "UniqueID": "SpaceBaby.PartOfTheCommunity",
    "MinimumVersion": "1.4.0",
    "IsRequired": true
  }
]
```

Reference the installed PotC DLL for compilation, but do **not** copy it into your own mod package. Bundling a second copy can create conflicting API type identities at runtime.

```xml
<ItemGroup>
  <Reference Include="PartOfTheCommunity">
    <HintPath>/path/to/Stardew Valley/Mods/PartOfTheCommunity/PartOfTheCommunity.dll</HintPath>
    <Private>false</Private>
  </Reference>
</ItemGroup>
```

Keep the machine-specific path in a local MSBuild property or environment-specific build file rather than committing it. PotC's release ZIP includes this document and the DLL needed for the reference.

## Getting the API

```csharp
// Get the API in your mod's GameLaunched event
private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
{
    var potcApi = this.Helper.ModRegistry.GetApi<IPartOfTheCommunityApi>("SpaceBaby.PartOfTheCommunity");
    if (potcApi != null)
    {
        // Use the API
    }
}
```

## API Interface

```csharp
public interface IPartOfTheCommunityApi
{
    bool TryRegisterCharacter(string name, bool isMale, CharacterType type = CharacterType.Villager);
    void RegisterCharacter(string name, bool isMale, CharacterType type = CharacterType.Villager);
    bool TryRegisterCharacter(string name, bool isMale, CharacterType type, string unlockCondition);
    void RegisterCharacter(string name, bool isMale, CharacterType type, string unlockCondition);

    bool TryAddRelationship(string characterA, Relationship relationshipA, string characterB, Relationship relationshipB);
    void AddRelationship(string characterA, Relationship relationshipA, string characterB, Relationship relationshipB);
    bool TryAddRelationship(string characterA, Relationship relationshipA, string characterB, Relationship relationshipB, string unlockCondition);
    void AddRelationship(string characterA, Relationship relationshipA, string characterB, Relationship relationshipB, string unlockCondition);

    bool TryAddFriendship(string characterA, string characterB);
    void AddFriendship(string characterA, string characterB);
    bool TryAddFriendship(string characterA, string characterB, string unlockCondition);
    void AddFriendship(string characterA, string characterB, string unlockCondition);

    IReadOnlyDictionary<string, CharacterInfo> GetAllCharacters();
    bool IsCharacterRegistered(string name);
}
```

## Supporting Types

The API methods above use the following public types from `SpaceBaby.PartOfTheCommunity.Framework`:

```csharp
public enum CharacterType
{
    Villager,
    Player,
    Child
}

public enum Relationship
{
    Brother,
    Sister,
    HalfBrother,
    HalfSister,
    Son,
    Daughter,
    StepSon,
    StepDaughter,
    Grandson,
    Granddaughter,
    GreatGrandson,
    GreatGranddaughter,
    Father,
    Mother,
    StepFather,
    StepMother,
    Grandfather,
    Grandmother,
    GreatGrandfather,
    GreatGrandmother,
    Husband,
    Wife,
    FatherInLaw,
    MotherInLaw,
    BrotherInLaw,
    SisterInLaw,
    SonInLaw,
    DaughterInLaw,
    Aunt,
    Uncle,
    Niece,
    Nephew,
    Godfather,
    Godmother,
    Godson,
    Goddaughter,
    Cousin,
    Friend,
    WarTorn
}

public class CharacterInfo
{
    public string Name { get; }
    public bool IsMale { get; }
    public CharacterType Type { get; }
    public string UnlockCondition { get; }
}
```

`GetAllCharacters()` returns a read-only dictionary of character names to `CharacterInfo` objects using this shape.

The returned character and relationship objects are immutable metadata views. Register characters and modify relationships only through `IPartOfTheCommunityApi`; this preserves validation, duplicate handling, and reciprocal links.

### Recommended calling pattern

- Prefer the `TryRegisterCharacter`, `TryAddRelationship`, and `TryAddFriendship` methods if your mod wants an immediate success/failure result.
- The older `RegisterCharacter`, `AddRelationship`, and `AddFriendship` methods are still available as convenience wrappers and will simply log/ignore invalid input.
- Character names are matched case-insensitively, surrounding whitespace is trimmed, and duplicate registrations or duplicate relationship entries are ignored safely.
- `Relationship.Friend` is a normal enum value. `TryAddFriendship(characterA, characterB)` is a convenience wrapper around `TryAddRelationship(characterA, Relationship.Friend, characterB, Relationship.Friend)`, so it creates the same bidirectional friend relationship and uses the same validations, duplicate checks, and logging behavior.
- In practice, `TryAddRelationship(..., Relationship.Friend, ..., Relationship.Friend)` is equivalent to `TryAddFriendship(...)`. Prefer `TryAddFriendship` when you specifically mean friendship because it is clearer to readers; use `TryAddRelationship` when you want to work with the full enum-based relationship API more generally.
- If a character, relationship, or friendship should only start granting **PotC bonus friendship** after some point in the story, use the overloads that accept an `unlockCondition` string. This string is a Stardew 1.6 **Game State Query** such as `PLAYER_HAS_MAIL Current leoMoved`, `PLAYER_HAS_SEEN_EVENT Current 6497421`, or `YEAR 2`.

## Available Relationship Types

The following relationship types are available:

### Family

- Brother/Sister
- HalfBrother/HalfSister
- Son/Daughter
- StepSon/StepDaughter
- Father/Mother
- StepFather/StepMother
- Grandfather/Grandmother
- GreatGrandfather/GreatGrandmother
- Grandson/Granddaughter
- GreatGrandson/GreatGranddaughter
- Husband/Wife
- FatherInLaw/MotherInLaw
- BrotherInLaw/SisterInLaw
- SonInLaw/DaughterInLaw
- Uncle/Aunt
- Nephew/Niece
- Cousin

### Other

- Friend
- Godfather/Godmother
- Godson/Goddaughter
- WarTorn

## Character Types

- `Villager` - Regular NPCs
- `Player` - The player character
- `Child` - Player's children

## Usage Example

```csharp
public class MyMod : Mod
{
    public override void Entry(IModHelper helper)
    {
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
    }

    private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
    {
        var potcApi = this.Helper.ModRegistry.GetApi<IPartOfTheCommunityApi>("SpaceBaby.PartOfTheCommunity");
        if (potcApi == null)
        {
            this.Monitor.Log("Part of the Community is not installed.", LogLevel.Info);
            return;
        }

        // Register custom characters
        if (!potcApi.TryRegisterCharacter("MyCustomNPC", isMale: true))
            this.Monitor.Log("MyCustomNPC was already registered or invalid.", LogLevel.Debug);

        potcApi.TryRegisterCharacter("AnotherNPC", isMale: false);

        // Add relationships
        potcApi.TryAddRelationship("MyCustomNPC", Relationship.Brother, "AnotherNPC", Relationship.Sister);

        // Add friendships
        potcApi.TryAddFriendship("MyCustomNPC", "Sam"); // Befriend an existing character

        // Gate friendship bonuses until a late-game event/mail flag.
        potcApi.TryAddFriendship("Leo", "Linus", "PLAYER_HAS_MAIL Current leoMoved");
    }
}
```

## Loading Characters from JSON

The recommended JSON integration is a standard SMAPI content pack. Create a separate mod folder containing these two files:

```text
My PotC Relationship Pack/
├── manifest.json
└── content.json
```

Point `manifest.json` at Part of the Community:

```json
{
  "Name": "My PotC Relationship Pack",
  "Author": "Your Name",
  "Version": "1.0.0",
  "Description": "Adds characters and relationships to Part of the Community.",
  "UniqueID": "YourName.MyPotCRelationshipPack",
  "ContentPackFor": {
    "UniqueID": "SpaceBaby.PartOfTheCommunity",
    "MinimumVersion": "1.4.0"
  },
  "UpdateKeys": []
}
```

Then put the character graph in the root `content.json`:

```json
{
  "characters": {
    "mycharacter": {
      "displayName": "MyCharacter",
      "gender": "M",
      "type": "Villager",
      "unlockCondition": "YEAR 2",
      "relationships": {
        "anothercharacter": "brother"
      },
      "relationshipConditions": {
        "anothercharacter": "PLAYER_HAS_MET Current AnotherCharacter"
      },
      "friends": {
        "sebastian": true
      },
      "friendConditions": {
        "sebastian": "PLAYER_HAS_SEEN_EVENT Current 123456"
      }
    },
    "anothercharacter": {
      "displayName": "AnotherCharacter", 
      "gender": "F",
      "type": "Villager",
      "relationships": {
        "mycharacter": "sister"
      },
      "friends": {
        "abigail": true
      }
    }
  }
}
```

### JSON Structure Details

- **Root object**: Contains a `characters` object.
- **Character keys**: Use lowercase names as keys (for example, `"mycharacter"`).
- **displayName**: The proper capitalized name shown in game.
- **gender**: `"M"` for male, `"F"` for female.
- **type**: Character type (`"Villager"`, `"Player"`, or `"Child"`).
- **unlockCondition** *(optional)*: A Stardew 1.6 Game State Query that must match before this character can receive PotC friendship bonuses at all.
- **relationships**: Object where keys are other character keys or display names and values are relationship types (lowercase). The value describes the current character's role relative to the referenced character. PotC adds the declared relationship on the current character **and** automatically adds the inferred reciprocal relationship on the referenced character.
- **relationshipConditions** *(optional)*: Object keyed by relationship target name. Each value is a Stardew 1.6 Game State Query that gates that specific relationship bonus link.
- **friends**: By default, this is an object where keys are other character keys or names and values are `true`. For convenience, PotC also accepts a simple array of names like `"friends": ["sam", "sebastian"]`. The original object form remains the canonical documented format and is still fully supported. In the flat JSON format, setting `"other": true` or listing `"other"` in the array adds a friend link from the current character to `other` and automatically adds the inverse friend entry on `other` too.
- **friendConditions** *(optional)*: Object keyed by friend name. Each value is a Stardew 1.6 Game State Query that gates that specific friendship bonus link.

> A complete, copyable pack is included in the PotC release under `docs/content-pack-example`. The old behavior of placing JSON directly in PotC's own `Data` folder remains supported for backwards compatibility, but separately distributed integrations should use a SMAPI content pack so installs and updates don't modify PotC's files.
>
> **Reciprocal relationship note:** PotC uses the **referenced character's gender** to infer their role back to the declaring character. For example:
>
> ```json
> "robin": {
>   "displayName": "Robin",
>   "gender": "F",
>   "relationships": {
>     "sebastian": "mother"
>   },
>   "friends": ["demetrius"]
> },
> "sebastian": {
>   "displayName": "Sebastian",
>   "gender": "M"
> }
> ```
>
> This stores **Robin -> Sebastian = mother** and infers **Sebastian -> Robin = son** because Sebastian (the referenced character) is male. For friends, either `"other": true` or the array shorthand like `"friends": ["demetrius"]` creates mutual `Friend` entries on both characters automatically. You only need to declare each relationship or friendship once; an identical explicit reciprocal is safely ignored.

PotC reads all owned content packs in two global passes: it registers every character first, then resolves relationships and friendships. This lets one content pack reference a character supplied by another pack regardless of SMAPI's pack order. Within the same pack, a link can use a character key or display name. Across packs, use the other character's registered `displayName`, since pack-local keys aren't global IDs. A missing or malformed pack is logged and skipped without preventing other packs from loading.

To add links to an existing PotC character, include an entry whose `displayName` matches that character and put the new links on it. PotC reuses the existing registration instead of replacing it; `gender`, `type`, and `unlockCondition` are primarily character-definition fields. Relationship values still describe that existing character's role relative to each target.

### Late-game lockout example

If you want Leo's PotC friendship bonuses with `jas`, `vincent`, and `linus` to stay inactive until the vanilla move-to-town progression happens, you can write:

```json
"leo": {
  "displayName": "Leo",
  "gender": "M",
  "type": "Villager",
  "relationships": {},
  "friends": {
    "jas": true,
    "linus": true,
    "vincent": true
  },
  "friendConditions": {
    "jas": "PLAYER_HAS_MAIL Current leoMoved",
    "linus": "PLAYER_HAS_MAIL Current leoMoved",
    "vincent": "PLAYER_HAS_MAIL Current leoMoved"
  }
}
```

This keeps the links in your data, but PotC won't award the indirect friendship bonus from those links until `leoMoved` is true for the evaluated player.

### Relationship Types (lowercase in JSON)

Use these lowercase strings in the JSON relationships:

- **Family**: `brother`, `sister`, `halfbrother`, `halfsister`, `son`, `daughter`, `stepson`, `stepdaughter`, `father`, `mother`, `stepfather`, `stepmother`, `grandfather`, `grandmother`, `greatgrandfather`, `greatgrandmother`, `grandson`, `granddaughter`, `greatgrandson`, `greatgranddaughter`, `husband`, `wife`, `fatherinlaw`, `motherinlaw`, `brotherinlaw`, `sisterinlaw`, `soninlaw`, `daughterinlaw`, `uncle`, `aunt`, `nephew`, `niece`, `cousin`

- **Other**: `friend`, `godfather`, `godmother`, `godson`, `goddaughter`, `wartorn`

## Notes

- Characters must be registered before adding relationships.
- Character names are matched case-insensitively by the API.
- Flat-pack relationship and friend keys can reference characters from the same pack by key or display name, and default or other-pack characters by display name.
- `TryAddRelationship` and `TryAddFriendship` are bidirectional API calls that return `false` when the input is invalid or already present. The flat JSON `relationships` and `friends` objects are also mirrored automatically using the inferred inverse relationship.
- When the player marries a character, PotC also derives spouse, in-law, and step-family relationships at runtime from that spouse's known family data.
- The convenience `AddRelationship`, `AddFriendship`, and `RegisterCharacter` methods still log invalid operations for backwards compatibility.
- Character registration should happen in the `GameLaunched` event to ensure proper initialization.
- PotC loads its baseline character graph during its own `Entry` call, before another dependency-ordered mod can receive the API. Repeated load calls do not discard registrations.

## Compatibility and versioning

- The manifest, package, and assembly use the same API version. Consumers should set `MinimumVersion` to the first PotC version containing the contract they require.
- Additive API members are intended to remain backwards compatible within the same major version. Removing or changing an existing method, enum value, or metadata property requires a major-version release.
- The API must be called from SMAPI's main thread. PotC keeps mutable Stardew objects and game-state-query evaluation behind the provider boundary; consumers receive immutable metadata.
