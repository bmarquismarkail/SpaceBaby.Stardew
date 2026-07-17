using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StardewModdingAPI;

namespace SpaceBaby.PartOfTheCommunity.Framework
{
    /// <summary>Manages character loading from both JSON files and API registrations.</summary>
    internal class CharacterManager : IPartOfTheCommunityApi
    {
        private const string ContentPackFileName = "content.json";

        /*********
        ** Fields
        *********/
        /// <summary>The mod helper for file operations.</summary>
        private readonly IModHelper Helper;

        /// <summary>The mod monitor for logging.</summary>
        private readonly IMonitor Monitor;

        /// <summary>The registered characters by name.</summary>
        private readonly Dictionary<string, CharacterInfo> Characters = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Whether characters have been loaded.</summary>
        private bool IsLoaded = false;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="helper">The mod helper for file operations.</param>
        /// <param name="monitor">The mod monitor for logging.</param>
        public CharacterManager(IModHelper helper, IMonitor monitor)
        {
            this.Helper = helper;
            this.Monitor = monitor;
            UnlockConditionHelper.Monitor = monitor;
        }

        /// <summary>Load all characters from data files and initialize the system.</summary>
        public void LoadCharacters()
        {
            if (this.IsLoaded)
                return;

            this.Characters.Clear();

            // Load default characters
            this.LoadCharacterPack("Data/default_characters.json", isRequired: true);

            // Load character packs from data folder
            string dataPath = Path.Combine(this.Helper.DirectoryPath, "Data");
            if (Directory.Exists(dataPath))
            {
                foreach (string filePath in Directory.GetFiles(dataPath, "*.json"))
                {
                    if (Path.GetFileName(filePath) != "default_characters.json")
                        this.LoadCharacterPack(Path.GetRelativePath(this.Helper.DirectoryPath, filePath), isRequired: false);
                }
            }

            // Load first-class SMAPI content packs after bundled/local data. Character
            // definitions across all packs are registered before any links are resolved.
            this.LoadOwnedContentPacks(this.Helper.ContentPacks.GetOwned());

            this.IsLoaded = true;
            this.Monitor.Log($"Loaded {this.Characters.Count} characters from data files and content packs.", LogLevel.Info);
        }

        /// <summary>Try to register a character that can have relationships with others.</summary>
        /// <param name="name">The character's name.</param>
        /// <param name="isMale">Whether the character is male.</param>
        /// <param name="type">The character type (defaults to Villager).</param>
        /// <returns>Returns whether the character was newly registered or an existing character's unlock condition was updated.</returns>
        public bool TryRegisterCharacter(string name, bool isMale, CharacterType type = CharacterType.Villager)
        {
            return this.TryRegisterCharacter(name, isMale, type, unlockCondition: null);
        }

        /// <summary>Try to register a character that can have relationships with others, with a Stardew 1.6 game-state query that can gate PotC friendship bonuses for that character.</summary>
        /// <param name="name">The character's name.</param>
        /// <param name="isMale">Whether the character is male.</param>
        /// <param name="type">The character type.</param>
        /// <param name="unlockCondition">A Stardew 1.6 game-state query string that must match before PotC can award this character friendship points, or <c>null</c>/<c>empty</c> to leave it always available.</param>
        /// <returns>Returns whether the character was newly registered or an existing character's unlock condition was updated.</returns>
        public bool TryRegisterCharacter(string name, bool isMale, CharacterType type, string unlockCondition = null)
        {
            name = name?.Trim();
            unlockCondition = unlockCondition?.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                this.Monitor.Log("Cannot register character with null or empty name.", LogLevel.Warn);
                return false;
            }

            if (this.Characters.TryGetValue(name, out CharacterInfo existingCharacter))
            {
                if (string.IsNullOrWhiteSpace(existingCharacter.UnlockCondition) && !string.IsNullOrWhiteSpace(unlockCondition))
                {
                    existingCharacter.UnlockCondition = unlockCondition;
                    this.Monitor.Log($"Updated unlock condition for already-registered character '{name}'.", LogLevel.Trace);
                    return true;
                }

                this.Monitor.Log($"Character '{name}' is already registered. Skipping duplicate registration.", LogLevel.Debug);
                return false;
            }

            var character = new CharacterInfo(name, isMale, type, unlockCondition);
            this.Characters[name] = character;
            this.Monitor.Log($"Registered character '{name}' via API.", LogLevel.Trace);
            return true;
        }

        /// <summary>Register a character that can have relationships with others.</summary>
        /// <param name="name">The character's name.</param>
        /// <param name="isMale">Whether the character is male.</param>
        /// <param name="type">The character type (defaults to Villager).</param>
        public void RegisterCharacter(string name, bool isMale, CharacterType type = CharacterType.Villager)
        {
            this.TryRegisterCharacter(name, isMale, type, unlockCondition: null);
        }

        /// <summary>Register a character that can have relationships with others, with a Stardew 1.6 game-state query that can gate PotC friendship bonuses for that character.</summary>
        /// <param name="name">The character's name.</param>
        /// <param name="isMale">Whether the character is male.</param>
        /// <param name="type">The character type.</param>
        /// <param name="unlockCondition">A Stardew 1.6 game-state query string that must match before PotC can award this character friendship points, or <c>null</c>/<c>empty</c> to leave it always available.</param>
        public void RegisterCharacter(string name, bool isMale, CharacterType type, string unlockCondition = null)
        {
            this.TryRegisterCharacter(name, isMale, type, unlockCondition);
        }

        /// <summary>Try to add a relationship between two characters.</summary>
        /// <param name="characterA">The first character's name.</param>
        /// <param name="relationshipA">Character A's relationship to character B.</param>
        /// <param name="characterB">The second character's name.</param>
        /// <param name="relationshipB">Character B's relationship to character A.</param>
        /// <returns>Returns whether a new relationship was added.</returns>
        public bool TryAddRelationship(string characterA, Relationship relationshipA, string characterB, Relationship relationshipB)
        {
            return this.TryAddRelationship(characterA, relationshipA, characterB, relationshipB, unlockCondition: null);
        }

        /// <summary>Try to add a relationship between two characters with a Stardew 1.6 game-state query that can gate when it awards PotC friendship bonuses.</summary>
        /// <param name="characterA">The first character's name.</param>
        /// <param name="relationshipA">Character A's relationship to character B.</param>
        /// <param name="characterB">The second character's name.</param>
        /// <param name="relationshipB">Character B's relationship to character A.</param>
        /// <param name="unlockCondition">A Stardew 1.6 game-state query string that must match before this relationship can award PotC friendship bonuses, or <c>null</c>/<c>empty</c> to leave it always available.</param>
        /// <returns>Returns whether a new relationship was added.</returns>
        public bool TryAddRelationship(string characterA, Relationship relationshipA, string characterB, Relationship relationshipB, string unlockCondition = null)
        {
            characterA = characterA?.Trim();
            characterB = characterB?.Trim();
            unlockCondition = unlockCondition?.Trim();

            if (string.IsNullOrWhiteSpace(characterA) || string.IsNullOrWhiteSpace(characterB))
            {
                this.Monitor.Log("Cannot add relationship: character names must not be null or empty.", LogLevel.Warn);
                return false;
            }

            if (string.Equals(characterA, characterB, StringComparison.OrdinalIgnoreCase))
            {
                this.Monitor.Log($"Cannot add relationship: '{characterA}' cannot have a relationship with itself.", LogLevel.Warn);
                return false;
            }

            if (!this.Characters.TryGetValue(characterA, out CharacterInfo charA))
            {
                this.Monitor.Log($"Cannot add relationship: character '{characterA}' is not registered.", LogLevel.Warn);
                return false;
            }

            if (!this.Characters.TryGetValue(characterB, out CharacterInfo charB))
            {
                this.Monitor.Log($"Cannot add relationship: character '{characterB}' is not registered.", LogLevel.Warn);
                return false;
            }

            bool addedA = charA.TryAddRelationship(relationshipA, charB, unlockCondition);
            bool addedB = charB.TryAddRelationship(relationshipB, charA, unlockCondition);

            if (!addedA && !addedB)
            {
                this.Monitor.Log($"Relationship already exists: {characterA} ({relationshipA}) <-> {characterB} ({relationshipB})", LogLevel.Debug);
                return false;
            }

            this.Monitor.Log($"Added relationship: {characterA} ({relationshipA}) <-> {characterB} ({relationshipB})", LogLevel.Trace);
            return true;
        }

        /// <summary>Add a relationship between two characters.</summary>
        /// <param name="characterA">The first character's name.</param>
        /// <param name="relationshipA">Character A's relationship to character B.</param>
        /// <param name="characterB">The second character's name.</param>
        /// <param name="relationshipB">Character B's relationship to character A.</param>
        public void AddRelationship(string characterA, Relationship relationshipA, string characterB, Relationship relationshipB)
        {
            this.TryAddRelationship(characterA, relationshipA, characterB, relationshipB, unlockCondition: null);
        }

        /// <summary>Add a relationship between two characters with a Stardew 1.6 game-state query that can gate when it awards PotC friendship bonuses.</summary>
        /// <param name="characterA">The first character's name.</param>
        /// <param name="relationshipA">Character A's relationship to character B.</param>
        /// <param name="characterB">The second character's name.</param>
        /// <param name="relationshipB">Character B's relationship to character A.</param>
        /// <param name="unlockCondition">A Stardew 1.6 game-state query string that must match before this relationship can award PotC friendship bonuses, or <c>null</c>/<c>empty</c> to leave it always available.</param>
        public void AddRelationship(string characterA, Relationship relationshipA, string characterB, Relationship relationshipB, string unlockCondition = null)
        {
            this.TryAddRelationship(characterA, relationshipA, characterB, relationshipB, unlockCondition);
        }

        /// <summary>Try to add a friendship between two characters (bidirectional).</summary>
        /// <param name="characterA">The first character's name.</param>
        /// <param name="characterB">The second character's name.</param>
        /// <returns>Returns whether a new friendship was added.</returns>
        public bool TryAddFriendship(string characterA, string characterB)
        {
            return this.TryAddFriendship(characterA, characterB, unlockCondition: null);
        }

        /// <summary>Try to add a friendship between two characters (bidirectional).</summary>
        /// <param name="characterA">The first character's name.</param>
        /// <param name="characterB">The second character's name.</param>
        /// <param name="unlockCondition">An optional Stardew 1.6 game-state query that must match before this friendship can award PotC friendship bonuses.</param>
        /// <returns>Returns whether a new friendship was added.</returns>
        public bool TryAddFriendship(string characterA, string characterB, string unlockCondition)
        {
            return this.TryAddRelationship(characterA, Relationship.Friend, characterB, Relationship.Friend, unlockCondition);
        }

        /// <summary>Add a friendship between two characters (bidirectional).</summary>
        /// <param name="characterA">The first character's name.</param>
        /// <param name="characterB">The second character's name.</param>
        public void AddFriendship(string characterA, string characterB)
        {
            this.TryAddFriendship(characterA, characterB, unlockCondition: null);
        }

        /// <summary>Add a friendship between two characters (bidirectional), with an optional unlock condition.</summary>
        /// <param name="characterA">The first character's name.</param>
        /// <param name="characterB">The second character's name.</param>
        /// <param name="unlockCondition">An optional Stardew 1.6 game-state query that must match before this friendship can award PotC friendship bonuses.</param>
        public void AddFriendship(string characterA, string characterB, string unlockCondition)
        {
            this.TryAddFriendship(characterA, characterB, unlockCondition);
        }

        /// <summary>Get all registered characters.</summary>
        /// <returns>A dictionary of character names to character info.</returns>
        public IReadOnlyDictionary<string, CharacterInfo> GetAllCharacters()
        {
            // The dictionary is detached and CharacterInfo exposes metadata only; all graph
            // mutation stays behind the registration API so reciprocal links remain consistent.
            return new Dictionary<string, CharacterInfo>(this.Characters, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Check if a character is registered.</summary>
        /// <param name="name">The character's name.</param>
        /// <returns>True if the character is registered.</returns>
        public bool IsCharacterRegistered(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && this.Characters.ContainsKey(name.Trim());
        }

        /// <summary>Get the internal characters dictionary (for use by ModEntry).</summary>
        /// <returns>The characters dictionary.</returns>
        internal Dictionary<string, CharacterInfo> GetCharactersDictionary()
        {
            return this.Characters;
        }

        /// <summary>Load all SMAPI content packs owned by PotC.</summary>
        /// <param name="contentPacks">The owned content packs.</param>
        internal void LoadOwnedContentPacks(IEnumerable<IContentPack> contentPacks)
        {
            var loadedPacks = new List<(CharacterPackFlat Data, string Source)>();

            foreach (IContentPack contentPack in contentPacks ?? Enumerable.Empty<IContentPack>())
            {
                string source = contentPack.Manifest?.UniqueID ?? contentPack.DirectoryPath ?? "unknown content pack";

                try
                {
                    if (!contentPack.HasFile(ContentPackFileName))
                    {
                        this.Monitor.Log($"Ignoring PotC content pack '{source}' because it has no {ContentPackFileName} file.", LogLevel.Warn);
                        continue;
                    }

                    CharacterPackFlat data = contentPack.ReadJsonFile<CharacterPackFlat>(ContentPackFileName);
                    if (data?.Characters == null)
                    {
                        this.Monitor.Log($"Ignoring PotC content pack '{source}' because {ContentPackFileName} is empty or has no characters object.", LogLevel.Warn);
                        continue;
                    }

                    loadedPacks.Add((data, $"content pack '{source}'"));
                }
                catch (Exception ex)
                {
                    this.Monitor.Log($"Failed loading PotC content pack '{source}': {ex.Message}", LogLevel.Error);
                }
            }

            // Resolve in two global passes so one content pack can reference a display name
            // registered by another pack regardless of SMAPI's content-pack ordering.
            foreach ((CharacterPackFlat data, string source) in loadedPacks)
            {
                try
                {
                    this.LoadFlatCharacterDefinitions(data, source);
                }
                catch (Exception ex)
                {
                    this.Monitor.Log($"Failed registering characters from {source}: {ex.Message}", LogLevel.Error);
                }
            }

            foreach ((CharacterPackFlat data, string source) in loadedPacks)
            {
                try
                {
                    this.LoadFlatCharacterRelationships(data, source);
                    this.Monitor.Log($"Loaded {data.Characters.Count} characters from {source}.", LogLevel.Info);
                }
                catch (Exception ex)
                {
                    this.Monitor.Log($"Failed resolving relationships from {source}: {ex.Message}", LogLevel.Error);
                }
            }
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Load a character pack from a JSON file.</summary>
        /// <param name="relativePath">The relative path to the JSON file.</param>
        /// <param name="isRequired">Whether this file is required and should cause an error if missing.</param>
        private void LoadCharacterPack(string relativePath, bool isRequired)
        {
            try
            {
                // Try to load the new flat format first
                CharacterPackFlat flatPack = this.Helper.Data.ReadJsonFile<CharacterPackFlat>(relativePath);
                if (flatPack?.Characters != null)
                {
                    this.LoadFlatCharacterPack(flatPack, relativePath);
                    return;
                }

                // Fall back to the old format
                CharacterPack pack = this.Helper.Data.ReadJsonFile<CharacterPack>(relativePath);
                if (pack == null)
                {
                    if (isRequired)
                    {
                        string message = $"Required character pack file '{relativePath}' not found.";
                        this.Monitor.Log(message, LogLevel.Error);
                        throw new InvalidOperationException(message);
                    }

                    this.Monitor.Log($"Character pack file '{relativePath}' not found, skipping.", LogLevel.Debug);
                    return;
                }

                this.LoadLegacyCharacterPack(pack, relativePath);
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"Error loading character pack '{relativePath}': {ex.Message}", LogLevel.Error);
                if (isRequired)
                    throw;
            }
        }

        /// <summary>Load a character pack from the new flat format.</summary>
        /// <param name="pack">The character pack data.</param>
        /// <param name="relativePath">The file path for logging.</param>
        private void LoadFlatCharacterPack(CharacterPackFlat pack, string relativePath)
        {
            this.Monitor.Log($"Loading flat character pack from '{relativePath}'", LogLevel.Info);

            this.LoadFlatCharacterDefinitions(pack, relativePath);
            this.LoadFlatCharacterRelationships(pack, relativePath);

            this.Monitor.Log($"Loaded {pack.Characters.Count} characters from flat format in '{relativePath}'", LogLevel.Debug);
        }

        /// <summary>Register every character definition in a flat pack.</summary>
        private void LoadFlatCharacterDefinitions(CharacterPackFlat pack, string source)
        {
            if (pack?.Characters == null)
                return;

            foreach (var kvp in pack.Characters)
            {
                string characterKey = kvp.Key;
                CharacterEntry entry = kvp.Value;

                if (entry == null || string.IsNullOrWhiteSpace(entry.DisplayName))
                {
                    this.Monitor.Log($"Skipping character '{characterKey}' with null or empty display name in {source}.", LogLevel.Warn);
                    continue;
                }

                if (!this.Characters.ContainsKey(entry.DisplayName))
                {
                    bool isMale = string.Equals(entry.Gender, "M", StringComparison.OrdinalIgnoreCase);
                    CharacterType type = Enum.TryParse<CharacterType>(entry.Type, out var parsedType) ? parsedType : CharacterType.Villager;

                    var character = new CharacterInfo(entry.DisplayName, isMale, type, entry.UnlockCondition);
                    this.Characters[entry.DisplayName] = character;
                }
                else
                {
                    CharacterInfo existingCharacter = this.Characters[entry.DisplayName];
                    if (string.IsNullOrWhiteSpace(existingCharacter.UnlockCondition) && !string.IsNullOrWhiteSpace(entry.UnlockCondition))
                        existingCharacter.UnlockCondition = entry.UnlockCondition.Trim();
                }
            }
        }

        /// <summary>Resolve every relationship and friendship in a flat pack.</summary>
        private void LoadFlatCharacterRelationships(CharacterPackFlat pack, string source)
        {
            if (pack?.Characters == null)
                return;

            foreach (var kvp in pack.Characters)
            {
                string characterKey = kvp.Key;
                CharacterEntry entry = kvp.Value;

                if (entry == null || string.IsNullOrWhiteSpace(entry.DisplayName) || !this.Characters.TryGetValue(entry.DisplayName, out CharacterInfo character))
                    continue;

                // Load relationships
                foreach (var relKvp in entry.Relationships ?? new Dictionary<string, string>())
                {
                    string otherCharKey = relKvp.Key;
                    string relationshipStr = relKvp.Value;

                    if (this.TryResolveCharacter(pack, otherCharKey, out CharacterInfo otherCharacter))
                    {
                        if (Enum.TryParse<Relationship>(relationshipStr, true, out Relationship relationship))
                        {
                            string unlockCondition = null;
                            entry.RelationshipConditions?.TryGetValue(otherCharKey, out unlockCondition);
                            character.AddRelationship(relationship, otherCharacter, unlockCondition);
                            otherCharacter.AddRelationship(relationship.GetReciprocal(otherCharacter.IsMale), character, unlockCondition);
                        }
                        else
                        {
                            this.Monitor.Log($"Unknown relationship type '{relationshipStr}' for {entry.DisplayName} -> {otherCharacter.Name} in {source}.", LogLevel.Warn);
                        }
                    }
                    else
                    {
                        this.Monitor.Log($"Skipping relationship in {source}: character '{otherCharKey}' not found for {entry.DisplayName}.", LogLevel.Warn);
                    }
                }

                // Load friendships
                foreach (var friendKvp in entry.Friends ?? new Dictionary<string, bool>())
                {
                    string friendKey = friendKvp.Key;
                    bool isFriend = friendKvp.Value;

                    if (!isFriend)
                        continue;

                    if (this.TryResolveCharacter(pack, friendKey, out CharacterInfo friendCharacter))
                    {
                        string unlockCondition = null;
                        entry.FriendConditions?.TryGetValue(friendKey, out unlockCondition);
                        character.AddRelationship(Relationship.Friend, friendCharacter, unlockCondition);
                        friendCharacter.AddRelationship(Relationship.Friend, character, unlockCondition);
                    }
                    else
                    {
                        this.Monitor.Log($"Skipping friendship in {source}: character '{friendKey}' not found for {entry.DisplayName}.", LogLevel.Warn);
                    }
                }
            }
        }

        /// <summary>Try to resolve a character by pack key or display name.</summary>
        /// <param name="pack">The current flat character pack.</param>
        /// <param name="characterId">The lower-case key or display name supplied in JSON.</param>
        /// <param name="character">The resolved character, if found.</param>
        /// <returns>Returns whether the character could be resolved.</returns>
        private bool TryResolveCharacter(CharacterPackFlat pack, string characterId, out CharacterInfo character)
        {
            character = null;

            if (string.IsNullOrWhiteSpace(characterId))
                return false;

            if (pack.Characters.TryGetValue(characterId, out CharacterEntry packEntry)
                && !string.IsNullOrWhiteSpace(packEntry.DisplayName)
                && this.Characters.TryGetValue(packEntry.DisplayName, out character))
            {
                return true;
            }

            if (this.Characters.TryGetValue(characterId, out character))
                return true;

            character = this.Characters.Values.FirstOrDefault(p => string.Equals(p.Name, characterId, StringComparison.OrdinalIgnoreCase));
            return character != null;
        }

        /// <summary>Load a character pack from the legacy format.</summary>
        /// <param name="pack">The character pack data.</param>
        /// <param name="relativePath">The file path for logging.</param>
        private void LoadLegacyCharacterPack(CharacterPack pack, string relativePath)
        {
            this.Monitor.Log($"Loading legacy character pack: {pack.Name} v{pack.Version} by {pack.Author}", LogLevel.Info);

            // Load characters
            foreach (var charData in pack.Characters ?? new List<CharacterData>())
            {
                if (string.IsNullOrWhiteSpace(charData.Name))
                {
                    this.Monitor.Log($"Skipping character with null or empty name in pack '{pack.Name}'", LogLevel.Warn);
                    continue;
                }

                if (this.Characters.TryGetValue(charData.Name, out CharacterInfo existingCharacter))
                {
                    if (string.IsNullOrWhiteSpace(existingCharacter.UnlockCondition) && !string.IsNullOrWhiteSpace(charData.UnlockCondition))
                        existingCharacter.UnlockCondition = charData.UnlockCondition.Trim();
                }
                else
                {
                    var character = new CharacterInfo(charData.Name, charData.IsMale, charData.Type, charData.UnlockCondition);
                    this.Characters[charData.Name] = character;
                }
            }

            // Load relationships
            foreach (var relData in pack.Relationships ?? new List<RelationshipData>())
            {
                if (this.Characters.TryGetValue(relData.CharacterA, out CharacterInfo charA) &&
                    this.Characters.TryGetValue(relData.CharacterB, out CharacterInfo charB))
                {
                    charA.AddRelationship(relData.RelationshipA, charB, relData.UnlockCondition);
                    charB.AddRelationship(relData.RelationshipB, charA, relData.UnlockCondition);
                }
                else
                {
                    this.Monitor.Log($"Skipping relationship between '{relData.CharacterA}' and '{relData.CharacterB}': one or both characters not found", LogLevel.Warn);
                }
            }

            // Load friendships
            foreach (var friendData in pack.Friendships ?? new List<FriendshipData>())
            {
                if (this.Characters.TryGetValue(friendData.CharacterA, out CharacterInfo charA) &&
                    this.Characters.TryGetValue(friendData.CharacterB, out CharacterInfo charB))
                {
                    charA.AddRelationship(Relationship.Friend, charB, friendData.UnlockCondition);
                    charB.AddRelationship(Relationship.Friend, charA, friendData.UnlockCondition);
                }
                else
                {
                    this.Monitor.Log($"Skipping friendship between '{friendData.CharacterA}' and '{friendData.CharacterB}': one or both characters not found", LogLevel.Warn);
                }
            }

            this.Monitor.Log($"Loaded {pack.Characters?.Count ?? 0} characters, {pack.Relationships?.Count ?? 0} relationships, and {pack.Friendships?.Count ?? 0} friendships from '{relativePath}'", LogLevel.Debug);
        }
    }
}
