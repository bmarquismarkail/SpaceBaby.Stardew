using System.Collections.Generic;

namespace SpaceBaby.PartOfTheCommunity.Framework
{
    public static class PlayerDataMigration
    {
        public static IDictionary<long, PlayerData> ConvertStringKeyed(IDictionary<string, PlayerData> stringKeyed, long fallbackPlayerId)
        {
            var converted = new Dictionary<long, PlayerData>();
            foreach (var kv in stringKeyed)
            {
                if (long.TryParse(kv.Key, out long id))
                    converted[id] = kv.Value;
                else
                    converted[fallbackPlayerId] = kv.Value;
            }

            return converted;
        }

        public static bool TryConvertLegacyFlagMap(IDictionary<string, bool> legacyFlags, long playerId, out IDictionary<long, PlayerData> migratedData)
        {
            migratedData = null;
            if (legacyFlags == null || legacyFlags.Count == 0)
                return false;

            bool hasKnownKeys = legacyFlags.ContainsKey(nameof(PlayerData.HasGottenInitialUjimaBonus))
                || legacyFlags.ContainsKey(nameof(PlayerData.HasGottenInitialKuumbaBonus));
            if (!hasKnownKeys)
                return false;

            migratedData = new Dictionary<long, PlayerData>
            {
                [playerId] = new PlayerData
                {
                    HasGottenInitialUjimaBonus = legacyFlags.TryGetValue(nameof(PlayerData.HasGottenInitialUjimaBonus), out bool ujimaBonus) && ujimaBonus,
                    HasGottenInitialKuumbaBonus = legacyFlags.TryGetValue(nameof(PlayerData.HasGottenInitialKuumbaBonus), out bool kuumbaBonus) && kuumbaBonus
                }
            };

            return true;
        }

        public static IDictionary<long, PlayerData> WrapLegacyPlayerData(PlayerData legacyData, long playerId)
        {
            return new Dictionary<long, PlayerData>
            {
                [playerId] = legacyData
            };
        }
    }
}
