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

You can also create character packs using JSON files. Place them in the `Data` folder of the Part of the Community mod:

```json
{
  "Name": "My Character Pack",
  "Author": "Your Name",
  "Version": "1.0.0",
  "Description": "Custom characters and relationships",
  "Characters": [
    { "Name": "MyCharacter", "IsMale": true, "Type": "Villager" }
  ],
  "Relationships": [
    { "CharacterA": "MyCharacter", "RelationshipA": "Brother", "CharacterB": "Sam", "RelationshipB": "Brother" }
  ],
  "Friendships": [
    { "CharacterA": "MyCharacter", "CharacterB": "Sebastian" }
  ]
}
```

## Notes

- Characters must be registered before adding relationships
- All relationships are bidirectional (adding A->B also adds B->A)
- The mod will log warnings for invalid operations (missing characters, etc.)
- Character registration should happen in the `GameLaunched` event to ensure proper initialization