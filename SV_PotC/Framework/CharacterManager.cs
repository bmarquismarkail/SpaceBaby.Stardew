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
        /*********
        ** Fields
        *********/
        /// <summary>The mod helper for file operations.</summary>
        private readonly IModHelper Helper;

        /// <summary>The mod monitor for logging.</summary>
        private readonly IMonitor Monitor;

        /// <summary>The registered characters by name.</summary>
        private readonly Dictionary<string, CharacterInfo> Characters = new Dictionary<string, CharacterInfo>();

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

            this.IsLoaded = true;
            this.Monitor.Log($"Loaded {this.Characters.Count} characters from data files.", LogLevel.Info);
        }

        /// <summary>Register a character that can have relationships with others.</summary>
        /// <param name="name">The character's name.</param>
        /// <param name="isMale">Whether the character is male.</param>
        /// <param name="type">The character type (defaults to Villager).</param>
        public void RegisterCharacter(string name, bool isMale, CharacterType type = CharacterType.Villager)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                this.Monitor.Log("Cannot register character with null or empty name.", LogLevel.Warn);
                return;
            }

            if (this.Characters.ContainsKey(name))
            {
                this.Monitor.Log($"Character '{name}' is already registered. Skipping duplicate registration.", LogLevel.Warn);
                return;
            }

            var character = new CharacterInfo(name, isMale, type);
            this.Characters[name] = character;
            this.Monitor.Log($"Registered character '{name}' via API.", LogLevel.Trace);
        }

        /// <summary>Add a relationship between two characters.</summary>
        /// <param name="characterA">The first character's name.</param>
        /// <param name="relationshipA">Character A's relationship to character B.</param>
        /// <param name="characterB">The second character's name.</param>
        /// <param name="relationshipB">Character B's relationship to character A.</param>
        public void AddRelationship(string characterA, Relationship relationshipA, string characterB, Relationship relationshipB)
        {
            if (!this.Characters.TryGetValue(characterA, out CharacterInfo charA))
            {
                this.Monitor.Log($"Cannot add relationship: character '{characterA}' is not registered.", LogLevel.Warn);
                return;
            }

            if (!this.Characters.TryGetValue(characterB, out CharacterInfo charB))
            {
                this.Monitor.Log($"Cannot add relationship: character '{characterB}' is not registered.", LogLevel.Warn);
                return;
            }

            charA.AddRelationship(relationshipA, charB);
            charB.AddRelationship(relationshipB, charA);
            this.Monitor.Log($"Added relationship: {characterA} ({relationshipA}) <-> {characterB} ({relationshipB})", LogLevel.Trace);
        }

        /// <summary>Add a friendship between two characters (bidirectional).</summary>
        /// <param name="characterA">The first character's name.</param>
        /// <param name="characterB">The second character's name.</param>
        public void AddFriendship(string characterA, string characterB)
        {
            this.AddRelationship(characterA, Relationship.Friend, characterB, Relationship.Friend);
        }

        /// <summary>Get all registered characters.</summary>
        /// <returns>A dictionary of character names to character info.</returns>
        public IReadOnlyDictionary<string, CharacterInfo> GetAllCharacters()
        {
            return this.Characters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>Check if a character is registered.</summary>
        /// <param name="name">The character's name.</param>
        /// <returns>True if the character is registered.</returns>
        public bool IsCharacterRegistered(string name)
        {
            return this.Characters.ContainsKey(name);
        }

        /// <summary>Get the internal characters dictionary (for use by ModEntry).</summary>
        /// <returns>The characters dictionary.</returns>
        internal Dictionary<string, CharacterInfo> GetCharactersDictionary()
        {
            return this.Characters;
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
                CharacterPack pack = this.Helper.Data.ReadJsonFile<CharacterPack>(relativePath);
                if (pack == null)
                {
                    if (isRequired)
                        this.Monitor.Log($"Required character pack file '{relativePath}' not found.", LogLevel.Error);
                    else
                        this.Monitor.Log($"Character pack file '{relativePath}' not found, skipping.", LogLevel.Debug);
                    return;
                }

                this.Monitor.Log($"Loading character pack: {pack.Name} v{pack.Version} by {pack.Author}", LogLevel.Info);

                // Load characters
                foreach (var charData in pack.Characters ?? new List<CharacterData>())
                {
                    if (string.IsNullOrWhiteSpace(charData.Name))
                    {
                        this.Monitor.Log($"Skipping character with null or empty name in pack '{pack.Name}'", LogLevel.Warn);
                        continue;
                    }

                    if (!this.Characters.ContainsKey(charData.Name))
                    {
                        var character = new CharacterInfo(charData.Name, charData.IsMale, charData.Type);
                        this.Characters[charData.Name] = character;
                    }
                }

                // Load relationships
                foreach (var relData in pack.Relationships ?? new List<RelationshipData>())
                {
                    if (this.Characters.TryGetValue(relData.CharacterA, out CharacterInfo charA) &&
                        this.Characters.TryGetValue(relData.CharacterB, out CharacterInfo charB))
                    {
                        charA.AddRelationship(relData.RelationshipA, charB);
                        charB.AddRelationship(relData.RelationshipB, charA);
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
                        charA.AddRelationship(Relationship.Friend, charB);
                        charB.AddRelationship(Relationship.Friend, charA);
                    }
                    else
                    {
                        this.Monitor.Log($"Skipping friendship between '{friendData.CharacterA}' and '{friendData.CharacterB}': one or both characters not found", LogLevel.Warn);
                    }
                }

                this.Monitor.Log($"Loaded {pack.Characters?.Count ?? 0} characters, {pack.Relationships?.Count ?? 0} relationships, and {pack.Friendships?.Count ?? 0} friendships from '{relativePath}'", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"Error loading character pack '{relativePath}': {ex.Message}", LogLevel.Error);
                if (isRequired)
                    throw;
            }
        }
    }
}