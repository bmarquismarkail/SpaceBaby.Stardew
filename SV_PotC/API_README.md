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
    /// <summary>Register a character that can have relationships with others.</summary>
    /// <param name="name">The character's name.</param>
    /// <param name="isMale">Whether the character is male.</param>
    /// <param name="type">The character type (defaults to Villager).</param>
    void RegisterCharacter(string name, bool isMale, CharacterType type = CharacterType.Villager);

    /// <summary>Add a relationship between two characters.</summary>
    /// <param name="characterA">The first character's name.</param>
    /// <param name="relationshipA">Character A's relationship to character B.</param>
    /// <param name="characterB">The second character's name.</param>
    /// <param name="relationshipB">Character B's relationship to character A.</param>
    void AddRelationship(string characterA, Relationship relationshipA, string characterB, Relationship relationshipB);

    /// <summary>Add a friendship between two characters (bidirectional).</summary>
    /// <param name="characterA">The first character's name.</param>
    /// <param name="characterB">The second character's name.</param>
    void AddFriendship(string characterA, string characterB);

    /// <summary>Get all registered characters.</summary>
    /// <returns>A dictionary of character names to character info.</returns>
    IReadOnlyDictionary<string, CharacterInfo> GetAllCharacters();

    /// <summary>Check if a character is registered.</summary>
    /// <param name="name">The character's name.</param>
    /// <returns>True if the character is registered.</returns>
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
        potcApi.RegisterCharacter("MyCustomNPC", isMale: true);
        potcApi.RegisterCharacter("AnotherNPC", isMale: false);

        // Add relationships
        potcApi.AddRelationship("MyCustomNPC", Relationship.Brother, "AnotherNPC", Relationship.Sister);
        
        // Add friendships
        potcApi.AddFriendship("MyCustomNPC", "Sam"); // Befriend an existing character
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
- **relationships**: Object where keys are other character keys or names and values are relationship types (lowercase). In the flat JSON format, these entries are loaded **exactly as written for that character only**; they are **not** automatically mirrored or gender-adjusted on the referenced character.
- **friends**: Object where keys are other character keys or names and values are `true`. In the flat JSON format, setting `"other": true` adds a friend link from the current character to `other`, but does **not** automatically add the inverse friend entry on `other`.

> The flat JSON format shown above is the recommended format for new integrations. Legacy packs are still supported for backwards compatibility.
>
> **Reciprocal relationship note:** If `A` has `"relationships": { "b": "brother" }`, PotC will load that as **A -> B = brother** only. It will **not** infer `B -> A = sister` or `brother`. If you want both sides in flat JSON, define both entries explicitly (for example, `A -> B = sister` and `B -> A = brother`). The same rule applies to `friends`: define the inverse entry too if you want both characters to list each other as friends.

### Relationship Types (lowercase in JSON)

Use these lowercase strings in the JSON relationships:

- **Family**: `brother`, `sister`, `halfbrother`, `halfsister`, `son`, `daughter`, `stepson`, `stepdaughter`, `father`, `mother`, `stepfather`, `stepmother`, `grandfather`, `grandmother`, `greatgrandfather`, `greatgrandmother`, `grandson`, `granddaughter`, `greatgrandson`, `greatgranddaughter`, `husband`, `wife`, `uncle`, `aunt`, `nephew`, `niece`, `cousin`

- **Other**: `friend`, `godfather`, `godmother`, `godson`, `goddaughter`, `wartorn`

## Notes

- Characters must be registered before adding relationships.
- Character names are matched case-insensitively by the API.
- Flat-pack relationship and friend keys can reference characters from the same pack, default PotC data, or previously registered characters by key or display name.
- `AddRelationship` and `AddFriendship` are bidirectional API calls. The flat JSON `relationships` and `friends` objects are **not** auto-mirrored; define both sides explicitly if you want reciprocal data.
- The mod will log warnings for invalid operations (missing characters, duplicate registrations, unknown relationship types, etc.).
- Character registration should happen in the `GameLaunched` event to ensure proper initialization.
