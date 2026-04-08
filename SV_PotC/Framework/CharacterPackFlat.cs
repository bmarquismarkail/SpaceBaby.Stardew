using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SpaceBaby.PartOfTheCommunity.Framework
{
    /// <summary>Represents the new flat character data structure for JSON serialization.</summary>
    public class CharacterPackFlat
    {
        /// <summary>The characters data with lowercase key names.</summary>
        public Dictionary<string, CharacterEntry> Characters { get; set; } = new Dictionary<string, CharacterEntry>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Represents a character entry in the flat structure.</summary>
    public class CharacterEntry
    {
        /// <summary>The display name of the character.</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>The gender of the character ("M" or "F").</summary>
        public string Gender { get; set; } = string.Empty;

        /// <summary>The character type.</summary>
        public string Type { get; set; } = "Villager";

        /// <summary>Relationships where key is the other character's lowercase name and value is this character's relationship to them.</summary>
        public Dictionary<string, string> Relationships { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Friends where key is the other character's lowercase name and value is true.</summary>
        [JsonConverter(typeof(FriendDictionaryJsonConverter))]
        public Dictionary<string, bool> Friends { get; set; } = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Supports both object and array syntax for the <c>friends</c> JSON field.</summary>
    internal sealed class FriendDictionaryJsonConverter : JsonConverter<Dictionary<string, bool>>
    {
        public override Dictionary<string, bool> ReadJson(JsonReader reader, Type objectType, Dictionary<string, bool> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            Dictionary<string, bool> result = existingValue ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            result.Clear();

            if (reader.TokenType == JsonToken.Null)
                return result;

            JToken token = JToken.Load(reader);
            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (JProperty property in token.Children<JProperty>())
                    {
                        bool isFriend = property.Value.Type == JTokenType.Boolean
                            ? property.Value.Value<bool>()
                            : property.Value.ToObject<bool>(serializer);

                        result[property.Name] = isFriend;
                    }
                    break;

                case JTokenType.Array:
                    foreach (JToken item in token.Children())
                    {
                        string friendName = item.Value<string>()?.Trim();
                        if (!string.IsNullOrWhiteSpace(friendName))
                            result[friendName] = true;
                    }
                    break;

                default:
                    throw new JsonSerializationException($"Unexpected token {token.Type} when parsing friends. Expected an object or an array of strings.");
            }

            return result;
        }

        public override void WriteJson(JsonWriter writer, Dictionary<string, bool> value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase));
        }
    }
}