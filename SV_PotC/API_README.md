# Part of the Community - API Documentation

Part of the Community now provides an API that allows other mods to register custom characters and relationships. This document explains how to use the API.

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

    bool TryAddRelationship(string characterA, Relationship relationshipA, string characterB, Relationship relationshipB);
    void AddRelationship(string characterA, Relationship relationshipA, string characterB, Relationship relationshipB);

    bool TryAddFriendship(string characterA, string characterB);
    void AddFriendship(string characterA, string characterB);

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
}
```

`GetAllCharacters()` returns a read-only dictionary of character names to `CharacterInfo` objects using this shape.

### Recommended calling pattern

- Prefer the `TryRegisterCharacter`, `TryAddRelationship`, and `TryAddFriendship` methods if your mod wants an immediate success/failure result.
- The older `RegisterCharacter`, `AddRelationship`, and `AddFriendship` methods are still available as convenience wrappers and will simply log/ignore invalid input.
- Character names are matched case-insensitively, surrounding whitespace is trimmed, and duplicate registrations or duplicate relationship entries are ignored safely.
- `Relationship.Friend` is a normal enum value. `TryAddFriendship(characterA, characterB)` is a convenience wrapper around `TryAddRelationship(characterA, Relationship.Friend, characterB, Relationship.Friend)`, so it creates the same bidirectional friend relationship and uses the same validations, duplicate checks, and logging behavior.
- In practice, `TryAddRelationship(..., Relationship.Friend, ..., Relationship.Friend)` is equivalent to `TryAddFriendship(...)`. Prefer `TryAddFriendship` when you specifically mean friendship because it is clearer to readers; use `TryAddRelationship` when you want to work with the full enum-based relationship API more generally.

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
    }
}
```

## Loading Characters from JSON

You can create character packs using JSON files. Place them in the `Data` folder of the Part of the Community mod:

```json
{
  "characters": {
    "mycharacter": {
      "displayName": "MyCharacter",
      "gender": "M",
      "type": "Villager",
      "relationships": {
        "sam": "brother"
      },
      "friends": {
        "sebastian": true
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
- **relationships**: Object where keys are other character keys or names and values are relationship types (lowercase). In the flat JSON format, PotC adds the declared relationship on the source character **and** automatically adds the inferred inverse relationship on the referenced character.
- **friends**: By default, this is an object where keys are other character keys or names and values are `true`. For convenience, PotC also accepts a simple array of names like `"friends": ["sam", "sebastian"]`. The original object form remains the canonical documented format and is still fully supported. In the flat JSON format, setting `"other": true` or listing `"other"` in the array adds a friend link from the current character to `other` and automatically adds the inverse friend entry on `other` too.

> The flat JSON format shown above is the recommended format for new integrations. Legacy packs are still supported for backwards compatibility.
>
> **Reciprocal relationship note:** PotC uses the **source character's gender** to infer the inverse relationship. For example:
>
> ```json
> "alice": {
>   "displayName": "Alice",
>   "gender": "F",
>   "relationships": {
>     "bob": "brother"
>   },
>   "friends": ["carol"]
> }
> ```
>
> This stores **Alice -> Bob = brother** and **Bob -> Alice = sister** because Alice (the source character) is female. If Alice's gender were `"M"`, the inverse would be **Bob -> Alice = brother** instead. For friends, either `"other": true` or the array shorthand like `"friends": ["carol"]` creates mutual `Friend` entries on both characters automatically. If you also define the inverse explicitly, PotC safely ignores the duplicate.

### Relationship Types (lowercase in JSON)

Use these lowercase strings in the JSON relationships:

- **Family**: `brother`, `sister`, `halfbrother`, `halfsister`, `son`, `daughter`, `stepson`, `stepdaughter`, `father`, `mother`, `stepfather`, `stepmother`, `grandfather`, `grandmother`, `greatgrandfather`, `greatgrandmother`, `grandson`, `granddaughter`, `greatgrandson`, `greatgranddaughter`, `husband`, `wife`, `fatherinlaw`, `motherinlaw`, `brotherinlaw`, `sisterinlaw`, `soninlaw`, `daughterinlaw`, `uncle`, `aunt`, `nephew`, `niece`, `cousin`

- **Other**: `friend`, `godfather`, `godmother`, `godson`, `goddaughter`, `wartorn`

## Notes

- Characters must be registered before adding relationships.
- Character names are matched case-insensitively by the API.
- Flat-pack relationship and friend keys can reference characters from the same pack, default PotC data, or previously registered characters by key or display name.
- `TryAddRelationship` and `TryAddFriendship` are bidirectional API calls that return `false` when the input is invalid or already present. The flat JSON `relationships` and `friends` objects are also mirrored automatically using the inferred inverse relationship.
- When the player marries a character, PotC also derives spouse, in-law, and step-family relationships at runtime from that spouse's known family data.
- The convenience `AddRelationship`, `AddFriendship`, and `RegisterCharacter` methods still log invalid operations for backwards compatibility.
- Character registration should happen in the `GameLaunched` event to ensure proper initialization.
