using System;
using System.Collections.Generic;
using StardewValley;

namespace SV_PotC.Framework
{
    /// <summary>Tracks transient per-farmer state that resets daily.</summary>
    public class FarmerSession
    {
        public bool HasEnteredEvent { get; set; }
        public bool HasEnteredFestival { get; set; }
        public bool HasProcessedWeddingOrBirth { get; set; }
        public bool HasTrackedDailyQuest { get; set; }
        public int DaysSinceDailyQuest { get; set; }
        public HashSet<string> NearbyTalksSeen { get; set; } = new HashSet<string>();
        public bool HasTalked { get; set; }
        public bool ReceivedGift { get; set; }
        public bool HasShopped { get; set; }

        /// <summary>Reset all daily flags while preserving persistent counters.</summary>
        public void ResetDailyFlags()
        {
            HasEnteredEvent = false;
            HasEnteredFestival = false;
            HasProcessedWeddingOrBirth = false;
            HasTrackedDailyQuest = false;
            NearbyTalksSeen.Clear();
            HasTalked = false;
            ReceivedGift = false;
            HasShopped = false;
        }
    }

    /// <summary>Manages per-farmer session state.</summary>
    public static class PlayerSession
    {
        private static readonly Dictionary<long, FarmerSession> FarmerSessions = new Dictionary<long, FarmerSession>();

        /// <summary>Get or create session data for a farmer.</summary>
        public static FarmerSession GetSession(Farmer farmer)
        {
            return GetSession(farmer.UniqueMultiplayerID);
        }

        /// <summary>Get or create session data for a farmer by ID.</summary>
        public static FarmerSession GetSession(long uniqueMultiplayerID)
        {
            if (!FarmerSessions.TryGetValue(uniqueMultiplayerID, out FarmerSession session))
            {
                session = new FarmerSession();
                FarmerSessions[uniqueMultiplayerID] = session;
            }
            return session;
        }

        /// <summary>Reset all farmer sessions for new day.</summary>
        public static void ResetAllSessions()
        {
            foreach (var session in FarmerSessions.Values)
            {
                session.ResetDailyFlags();
            }
        }

        /// <summary>Increment daily quest counter for all farmers.</summary>
        public static void IncrementDailyQuestCounter()
        {
            foreach (var session in FarmerSessions.Values)
            {
                session.DaysSinceDailyQuest++;
            }
        }

        /// <summary>Clear all session data (for returning to title).</summary>
        public static void ClearAll()
        {
            FarmerSessions.Clear();
        }
    }
}