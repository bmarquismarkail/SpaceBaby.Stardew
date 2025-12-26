using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using SpaceBaby.PartOfTheCommunity.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Events;
using StardewValley.Locations;
using StardewValley.Menus;

namespace SpaceBaby.PartOfTheCommunity
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        /*********
        ** Properties
        *********/
        /// <summary>The shopkeeper names indexed by their location name.</summary>
        private readonly IDictionary<string, string> Shops = new Dictionary<string, string>
        {
            ["SeedShop"] = "Pierre",
            ["AnimalShop"] = "Marnie",
            ["Blacksmith"] = "Clint",
            ["FishShop"] = "Willy",
            ["ScienceHouse"] = "Robin",
            ["Saloon"] = "Gus",
            ["Mine"] = "Dwarf",
            ["SandyHouse"] = "Sandy",
            ["Sewer"] = "Krobus"
        };

        /// <summary>Metadata for NPCs tracked by the mod.</summary>
        private IDictionary<string, CharacterInfo> Characters;
        
        /// <summary>The character manager for loading and managing character relationships.</summary>
        private CharacterManager CharacterManager;
        
        private int CurrentNumberOfCompletedBundles;
        private uint CurrentNumberOfCompletedDailyQuests;
        private bool IsReady;
        private ModConfig Config;
        private IDictionary<long, PlayerData> PlayerData;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            // Initialize character manager
            this.CharacterManager = new CharacterManager(helper, this.Monitor);
            
            // Read JSON file or create one if it doesn't exist
            Config = this.Helper.Data.ReadJsonFile<ModConfig>("config.json") ?? new ModConfig();
            // save (generate) config file (if needed)
            this.Helper.Data.WriteJsonFile("config.json", Config);

            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
            helper.Events.GameLoop.Saving += this.OnSaving;
            helper.Events.GameLoop.Saved += this.OnSaved;
        }

        /// <summary>Get the API that other mods can use to register characters and relationships.</summary>
        public override object GetApi()
        {
            return this.CharacterManager;
        }


        /*********
        ** Private methods
        *********/
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // Load character data
            this.CharacterManager.LoadCharacters();
            
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: this.ModManifest,
                reset: () => this.Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this.Config)
            );

            // add some config options
            {
                configMenu.AddParagraph(
                    mod: this.ModManifest,
                    text: () => "Mod by Brandon Marquis Markail Green (Space Baby), 1.6 version by Nikki864, GMCM Menu by MickeyMik (Eela11)."
                );
                configMenu.AddSectionTitle(
                    mod: this.ModManifest,
                    text: () => "Bonus Points Settings"
                );
                configMenu.AddParagraph(
                    mod: this.ModManifest,
                    text: () => "The following settings allow you to set the amount of friendship points gained for each bonus. 250 points equal 1 heart."
                );
                configMenu.AddNumberOption(
                    mod: this.ModManifest,
                    name: () => "Witness Bonus",
                    tooltip: () => "Villagers within earshot of the Farmer talking to/gifting another villager will get a slight increase in friendship (every 2^n times witnessed). (Default 2)",
                    getValue: () => this.Config.WitnessBonus,
                    setValue: value => this.Config.WitnessBonus = value
                );
                configMenu.AddNumberOption(
                    mod: this.ModManifest,
                    name: () => "Storyteller Bonus",
                    tooltip: () => "Villagers will get a slight friendship increase at the end of the day if one of their friends/family members gains a gift. (Default 4)",
                    getValue: () => this.Config.StorytellerBonus,
                    setValue: value => this.Config.StorytellerBonus = value
                );
                configMenu.AddNumberOption(
                    mod: this.ModManifest,
                    name: () => "Shop Bonus",
                    tooltip: () => "Shop Owners will increase friendship when the Farmer visits their shop. (Default 4)",
                    getValue: () => this.Config.UjamaaBonus,
                    setValue: value => this.Config.UjamaaBonus = value
                );
                configMenu.AddNumberOption(
                    mod: this.ModManifest,
                    name: () => "Festivities Bonus",
                    tooltip: () => "All villagers will increase friendship simply by joining the festivities. (Default 16)",
                    getValue: () => this.Config.UmojaBonusFestival,
                    setValue: value => this.Config.UmojaBonusFestival = value
                );
                configMenu.AddNumberOption(
                    mod: this.ModManifest,
                    name: () => "Marry Bonus",
                    tooltip: () => "Marrying a villager will give an increase to the partner's family (Default 240)",
                    getValue: () => this.Config.UmojaBonusMarry,
                    setValue: value => this.Config.UmojaBonusMarry = value
                );
                configMenu.AddNumberOption(
                    mod: this.ModManifest,
                    name: () => "In-law Family Bonus",
                    tooltip: () => "Increasing your partner's/child's friendship will give an increase to the partner's family. (Default 10)",
                    getValue: () => this.Config.UmojaBonus,
                    setValue: value => this.Config.UmojaBonus = value
                );
                configMenu.AddNumberOption(
                    mod: this.ModManifest,
                    name: () => "CC Bundle Store Owner Bonus",
                    tooltip: () => "Completing the CC Bundles will give a increase to all Store Owners. (Default 20)",
                    getValue: () => this.Config.UjimaBonusStore,
                    setValue: value => this.Config.UjimaBonusStore = value
                );
                configMenu.AddNumberOption(
                    mod: this.ModManifest,
                    name: () => "Bulletin Board Quest Bonus",
                    tooltip: () => "Completing multiple Bulletin Board Quests will give a slight increase to all villagers. (Default 2)",
                    getValue: () => this.Config.UjimaBonus,
                    setValue: value => this.Config.UjimaBonus = value
                );
                configMenu.AddNumberOption(
                    mod: this.ModManifest,
                    name: () => "Unique Shipping Bonus",
                    tooltip: () => "Shipping at least one new item will give a slight increase to all villagers. (Default 2)",
                    getValue: () => this.Config.KuumbaBonus,
                    setValue: value => this.Config.KuumbaBonus = value
                );
            }
        }

        /// <summary>Raised after the game finishes writing data to the save file (except the initial save creation).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnSaved(object sender, SavedEventArgs e)
        {
            // remove legacy file (moved into save file at this point)
            DirectoryInfo legacyDir = new DirectoryInfo(Path.Combine(this.Helper.DirectoryPath, $"{Constants.SaveFolderName}"));
            if (legacyDir.Exists)
                legacyDir.Delete(recursive: true);
        }

        /// <summary>Raised after the game begins a new day (including when the player loads a save).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            // refresh data
            this.Characters = this.GetCharacters();
            this.CurrentNumberOfCompletedBundles = ((CommunityCenter)Game1.getLocationFromName("CommunityCenter")).numberOfCompleteBundles();
            this.CurrentNumberOfCompletedDailyQuests = Game1.stats.QuestsCompleted;
            
            // Initialize PlayerData before any GetPlayerData calls to prevent NullReferenceException
            if (this.PlayerData == null)
                this.PlayerData = this.LoadPlayerData();
            
            // Reset all farmer sessions for the new day and increment daily quest counter
            // Ensure sessions exist for all current farmers first
            foreach (Farmer farmer in Game1.getAllFarmers())
            {
                PlayerSession.GetSession(farmer); // Ensures session exists
                var farmerData = this.GetPlayerData(farmer); // Ensures data exists and quest count is initialized
            }
            
            PlayerSession.ResetAllSessions();
            PlayerSession.IncrementDailyQuestCounter();
            
            // Initialize quest counts for all current farmers to prevent false quest completion detection
            foreach (Farmer farmer in Game1.getAllFarmers())
            {
                var farmerData = this.GetPlayerData(farmer);
                // Reset to current count to prevent false "new quest" detection
                farmerData.LastKnownQuestCount = farmer.stats.QuestsCompleted;
                if (farmerData.HasGottenInitialKuumbaBonus && farmerData.LastKnownUniqueItemsShipped == 0)
                    farmerData.LastKnownUniqueItemsShipped = farmer.basicShipped.Count();
            }

            if (!this.IsReady)
            {
                // init data
                this.IsReady = true;

                foreach (Farmer farmer in Game1.getAllFarmers())
                {
                    PlayerData farmerData = this.GetPlayerData(farmer);

                    // add initial community center bonus
                    if (!farmerData.HasGottenInitialUjimaBonus)
                    {
                        int bonusPoints = this.Config.UjimaBonus * this.CurrentNumberOfCompletedBundles;
                        foreach (CharacterInfo shopkeeper in this.Characters.Values.Where(p => p.IsShopOwner))
                        {
                            if (shopkeeper.TryGetNpc(out NPC npc))
                                this.AddFriendshipPoints(farmer, npc, bonusPoints);
                        }
                        this.Monitor.Log($"{farmer.Name}: Gained {bonusPoints} friendship from all store owners for completing {this.CurrentNumberOfCompletedBundles} {(this.CurrentNumberOfCompletedBundles > 1 ? "Bundles" : "Bundle")}: {farmer.Name}", LogLevel.Info);
                        farmerData.HasGottenInitialUjimaBonus = true;
                    }
                    // add initial items shipped bonus
                    if (!farmerData.HasGottenInitialKuumbaBonus)
                    {
                        int uniqueItemsShipped = farmer.basicShipped.Count();
                        int bonusPoints = MultiplayerRewardLogic.ClaimInitialShippingBonus(farmerData, uniqueItemsShipped, this.Config.KuumbaBonus);
                        if (bonusPoints > 0)
                        {
                            Utility.improveFriendshipWithEveryoneInRegion(farmer, bonusPoints, "Town");
                            this.Monitor.Log($"Gained {bonusPoints} friendship for shipping {uniqueItemsShipped} unique {(uniqueItemsShipped != 1 ? "items" : "item")}: {farmer.Name}", LogLevel.Info);
                        }
                    }
                }
            }
        }

        /// <summary>Raised after the game returns to the title screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            this.IsReady = false;
            this.PlayerData = null;
            this.Characters = null;
            this.CurrentNumberOfCompletedBundles = 0;
            this.CurrentNumberOfCompletedDailyQuests = 0;
            
            // Clear all player session data
            PlayerSession.ClearAll();
        }

        private static List<Character> AreThereCharactersWithinDistance(Vector2 tile, int tilesAway, GameLocation location)
        {
            var charactersWithinDistance = new List<Character>();

            // Guard against null location or missing character list (prevents NREs when farmer.currentLocation is null or not loaded).
            if (location == null)
                return charactersWithinDistance;

            foreach (NPC character in location.characters)
            {
                if (character != null && Vector2.Distance(character.Tile, tile) <= tilesAway)
                    charactersWithinDistance.Add(character);
            }

            return charactersWithinDistance;
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || !this.IsReady)
                return;

            foreach (Farmer farmer in Game1.getAllFarmers())
            {
                // get farmer session and data - ensure both exist
                var session = PlayerSession.GetSession(farmer);
                var farmerData = this.GetPlayerData(farmer); // This handles lazy initialization
                
                // For farmers who joined after DayStarted, ensure they get proper daily quest counter advancement
                // Check if this farmer missed the daily increment (DaysSinceDailyQuest should advance daily)
                bool isMissingDailyAdvancement = session.DaysSinceDailyQuest == 0 && !session.HasTrackedDailyQuest;
                if (isMissingDailyAdvancement)
                {
                    // This farmer likely joined after the daily reset - give them reasonable decay timing
                    // We'll start them at 1 to avoid giving infinite bonuses but not overly penalize them
                    session.DaysSinceDailyQuest = 1;
                }
                
                foreach (KeyValuePair<string, Friendship> pair in farmer.friendshipData.Pairs)
                {
                    // get friend info
                    if (pair.Key == null)
                        continue;

                    if (!this.Characters.TryGetValue(pair.Key, out CharacterInfo friend))
                        continue;

                    // get friendship
                    Friendship friendship = pair.Value;
                    if (friendship.IsDivorced())
                        continue;

                    // track gift
                    if (friendship.GiftsToday == 1)
                        session.ReceivedGift = true;

                    // track talk & apply nearby NPC bonuses
                    if (farmer.hasTalkedToFriendToday(friend.Name))
                    {
                        // Create unique key for this farmer-NPC conversation
                        string conversationKey = $"{farmer.UniqueMultiplayerID}_{friend.Name}";
                        
                        // Only run nearby NPC logic if this specific conversation hasn't been processed yet today
                        if (!session.NearbyTalksSeen.Contains($"processed_{conversationKey}"))
                        {
                            List<Character> charactersWithinDistance = ModEntry.AreThereCharactersWithinDistance(farmer.Tile, 20, farmer.currentLocation);
                            foreach (Character nearbyNpc in charactersWithinDistance)
                            {
                                // get nearby character's info
                                if (nearbyNpc == null || nearbyNpc.Name == friend.Name)
                                    continue;
                                if (!this.Characters.TryGetValue(nearbyNpc.Name, out CharacterInfo nearbyCharacter))
                                    continue;
                                if (!farmer.friendshipData.TryGetValue(nearbyNpc.Name, out Friendship nearbyFriendship))
                                    continue;

                                // ignore if divorced
                                if (nearbyFriendship.IsDivorced())
                                {
                                    nearbyNpc.doEmote(Character.angryEmote);
                                    continue;
                                }

                                // get unique key for this nearby talk
                                string nearbyTalkKey = $"{nearbyNpc.Name}_{friend.Name}";
                                
                                // add to seen talks (for tracking unique pairs)
                                session.NearbyTalksSeen.Add(nearbyTalkKey);
                                
                                // increment witness count for this NPC (this is the actual counter for bonuses)
                                if (!session.WitnessCount.ContainsKey(nearbyNpc.Name))
                                    session.WitnessCount[nearbyNpc.Name] = 0;
                                session.WitnessCount[nearbyNpc.Name]++;
                                
                                int nearbyTalkCount = session.WitnessCount[nearbyNpc.Name];

                                // add witness bonus when overhearing 2^n conversations
                                if ((nearbyTalkCount & (nearbyTalkCount - 1)) == 0)
                                {
                                    nearbyNpc.doEmote(Character.happyEmote);
                                    this.AddFriendshipPoints(farmer, nearbyNpc as NPC, this.Config.WitnessBonus);
                                    this.Monitor.Log($"{farmer.Name}: {nearbyNpc.Name} saw you talking to {friend.Name}. +{this.Config.WitnessBonus} friendship: {nearbyNpc.Name}", LogLevel.Info);
                                }
                                else // log TalksSeen counter
                                    this.Monitor.Log($"{farmer.Name}: {nearbyNpc.Name} saw you talking to {friend.Name}. {nearbyNpc.Name} has seen {nearbyTalkCount} talks", LogLevel.Info);
                            }
                            
                            // Mark this specific conversation as processed
                            session.NearbyTalksSeen.Add($"processed_{conversationKey}");
                            session.HasTalked = true; // Keep for any other general tracking needs
                        }
                    }
                }

                // check if shopping
                //TODO: Add the Bus/Pam
                if (Game1.activeClickableMenu is ShopMenu shopMenu
                    && MultiplayerRewardLogic.TryClaimShopBonus(
                        localPlayerId: Game1.player.UniqueMultiplayerID,
                        evaluatedFarmerId: farmer.UniqueMultiplayerID,
                        currentLocationName: Game1.currentLocation?.Name,
                        hasOpenShopMenu: true,
                        hasHeldItem: this.Helper.Reflection.GetField<Item>(shopMenu, "heldItem").GetValue() != null,
                        shops: (IReadOnlyDictionary<string, string>)this.Shops,
                        session: session,
                        out string shopOwnerName))
                {
                    // get shopkeeper
                    if (!this.Characters.TryGetValue(shopOwnerName, out CharacterInfo shopkeeper))
                        continue;

                    if (shopkeeper.TryGetNpc(out NPC shopkeeperNpc))
                    {
                        this.AddFriendshipPoints(farmer, shopkeeperNpc, this.Config.UjamaaBonus);
                        this.Monitor.Log($"{shopOwnerName}: Pleasure doing business with you!", LogLevel.Info);
                    }
                }

                // check if player entered a festival
                if (MultiplayerRewardLogic.TryClaimFestivalBonus(
                    localPlayerId: Game1.player.UniqueMultiplayerID,
                    evaluatedFarmerId: farmer.UniqueMultiplayerID,
                    isLocalPlayerAtFestival: Game1.currentLocation?.currentEvent?.isFestival == true,
                    session: session))
                {
                    Utility.improveFriendshipWithEveryoneInRegion(farmer, this.Config.UmojaBonusFestival, "Town");
                    foreach (KeyValuePair<string, Friendship> pair in farmer.friendshipData.Pairs)
                    {
                        if (pair.Key == null)
                            continue;

                        string name = pair.Key;
                        Friendship friendship = pair.Value;
                        if (this.Characters.TryGetValue(name, out CharacterInfo character) && character.TryGetNpc(out NPC npc) && object.ReferenceEquals(npc.currentLocation, Game1.currentLocation))
                            npc.doEmote(friendship.IsDivorced() ? Character.angryEmote : Character.happyEmote);
                    }
                    this.Monitor.Log($"The villagers are glad you came, {farmer.Name}!", LogLevel.Info);
                    session.HasEnteredFestival = true;
                }

                // check if player is getting married or having a baby
                if (!string.IsNullOrWhiteSpace(farmer.spouse) && (Game1.weddingToday || Game1.farmEvent is BirthingEvent) && !session.HasProcessedWeddingOrBirth)
                {
                    if (this.Characters.TryGetValue(farmer.spouse, out CharacterInfo spouse) && spouse != null)
                    {
                        foreach (CharacterRelationship relation in spouse.Relationships)
                        {
                            if (!relation.Character.TryGetNpc(out NPC relationNpc))
                                continue;

                            if (relation.IsFamily)
                            {
                                this.AddFriendshipPoints(farmer, relationNpc, this.Config.UmojaBonusMarry);
                                this.Monitor.Log($"{relation}: {farmer.Name} married into the family, received +{this.Config.UmojaBonusMarry} friendship", LogLevel.Info);
                            }
                            else
                            {
                                this.AddFriendshipPoints(farmer, relationNpc, this.Config.UmojaBonusMarry / 2);
                                this.Monitor.Log($"{relation}: {farmer.Name} married a friend, received +{this.Config.UmojaBonusMarry / 2} friendship", LogLevel.Info);
                            }
                        }
                    }
                    session.HasProcessedWeddingOrBirth = true;
                }

                // check if player completed daily quest - track per farmer, not globally
                uint farmerQuestCount = farmer.stats.QuestsCompleted;
                uint lastKnownCount = farmerData.LastKnownQuestCount ?? farmerQuestCount; // Use current count if null to prevent false detection
                
                if (farmerQuestCount > lastKnownCount)
                {
                    session.DaysSinceDailyQuest = 0;
                    session.HasTrackedDailyQuest = true;
                }
                
                // Always update LastKnownQuestCount to keep sessions in sync
                farmerData.LastKnownQuestCount = farmerQuestCount;
            }
        }

        /// <summary>Raised before the game begins writes data to the save file (except the initial save creation).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnSaving(object sender, SavingEventArgs e)
        {
            foreach (Farmer farmer in Game1.getAllFarmers())
            {

                // bonus for giving gifts to an NPC's friend/relative
                var session = PlayerSession.GetSession(farmer);
                foreach (CharacterInfo character in this.Characters.Values)
                {
                    if (!character.TryGetNpc(out NPC npc))
                        continue;

                    // Check if this farmer gave gifts to this character's relationships
                    int relationsGifted = character.Relationships.Count(p => 
                        farmer.friendshipData.ContainsKey(p.Character.Name) && 
                        farmer.friendshipData[p.Character.Name].GiftsToday > 0);
                    
                    if (relationsGifted > 0)
                    {
                        this.AddFriendshipPoints(farmer, npc, this.Config.StorytellerBonus * relationsGifted);
                        this.Monitor.Log($"{farmer.Name}: {character.Name}'s friendship raised {this.Config.StorytellerBonus * relationsGifted} for gifting to someone they love.", LogLevel.Info);
                    }
                }

                // extended family bonus for gifting spouse/child
                if (!string.IsNullOrWhiteSpace(farmer.spouse) && this.Characters.TryGetValue(farmer.Name, out CharacterInfo player) && this.Characters.TryGetValue(farmer.spouse, out CharacterInfo spouse))
                {
                    bool giftedFamily = false;
                    foreach (CharacterRelationship relation in player.Relationships)
                    {
                        if (relation.IsFamily && farmer.friendshipData.ContainsKey(relation.Character.Name) && farmer.friendshipData[relation.Character.Name].GiftsToday > 0)
                        {
                            giftedFamily = true;
                            break;
                        }
                    }
                    
                    if (giftedFamily)
                    {
                        foreach (CharacterRelationship relation in spouse.Relationships)
                        {
                            if (relation.Character.TryGetNpc(out NPC relationNpc) && relation.IsFamily)
                            {
                                this.AddFriendshipPoints(farmer, relationNpc, this.Config.UmojaBonus);
                                this.Monitor.Log($"{farmer.Name}: {relation}'s Friendship raised {this.Config.UmojaBonus} for loving your family.", LogLevel.Info);
                            }
                        }
                    }
                }

                // bonus for new completed bundles
                CommunityCenter communityCenter = (CommunityCenter)Game1.getLocationFromName("CommunityCenter");
                int totalBundles = communityCenter.numberOfCompleteBundles();
                if (this.CurrentNumberOfCompletedBundles < totalBundles)
                {
                    int newBundles = totalBundles - this.CurrentNumberOfCompletedBundles;
                    int bonusPoints = this.Config.UjimaBonus * newBundles;
                    foreach (CharacterInfo shopkeeper in this.Characters.Values.Where(p => p.IsShopOwner))
                    {
                        if (shopkeeper.TryGetNpc(out NPC shopkeeperNpc))
                            this.AddFriendshipPoints(farmer, shopkeeperNpc, bonusPoints);
                    }
                    this.Monitor.Log($"{farmer.Name} Gained {bonusPoints} friendship with all store owners for completing {newBundles} bundles today.", LogLevel.Info);
                }

                // bonus for completed daily quests
                int dailyQuestBonus = MultiplayerRewardLogic.ClaimDailyQuestBonus(session, GetCurrentDayKey(), this.Config.UjimaBonus);
                if (dailyQuestBonus > 0)
                {
                    Utility.improveFriendshipWithEveryoneInRegion(farmer, dailyQuestBonus, "Town");
                    this.Monitor.Log($"Gained {dailyQuestBonus} friendship with everyone for completing a daily quest.", LogLevel.Info);
                }
                else if (session.DaysSinceDailyQuest >= 3)
                {
                    session.HasTrackedDailyQuest = false;
                }

                // bonus for new shipped items
                var farmerData = this.GetPlayerData(farmer);
                int shippingBonus = MultiplayerRewardLogic.ClaimShippingDeltaBonus(farmerData, farmer.basicShipped.Count(), this.Config.KuumbaBonus);
                if (shippingBonus > 0)
                {
                    Utility.improveFriendshipWithEveryoneInRegion(farmer, shippingBonus, "Town");
                    this.Monitor.Log($"Gained {shippingBonus} friendship with everyone for shipping new items.", LogLevel.Info);
                }

                // save player data
                if (Context.IsMainPlayer && this.PlayerData != null)
                    this.Helper.Data.WriteSaveData("data", this.PlayerData);
            }
        }

        /// <summary>Get all available characters.</summary>
        private IDictionary<string, CharacterInfo> GetCharacters()
        {
            // Start with characters from the character manager (includes data file characters and API registrations)
            IDictionary<string, CharacterInfo> characters = this.CharacterManager.GetCharactersDictionary().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // mark shopkeepers
            {
                HashSet<string> shopkeeperNames = new HashSet<string>(this.Shops.Values);
                foreach (CharacterInfo character in characters.Values)
                    character.IsShopOwner = shopkeeperNames.Contains(character.Name);
            }

            // add player
            var player = new CharacterInfo(Game1.player.Name, isMale: Game1.player.IsMale, type: CharacterType.Player);
            characters[player.Name] = player;

            // add player spouse
            CharacterInfo spouse = null;
            if (Game1.player.spouse != null)
            {
                if (!characters.TryGetValue(Game1.player.spouse, out spouse))
                {
                    spouse = new CharacterInfo(Game1.player.spouse, isMale: Utility.isMale(Game1.player.spouse));
                    characters[spouse.Name] = spouse;
                }
            }

            // add unknown NPCs
            List <NPC> Villagerss = Utility.getAllVillagers();
            List<NPC> allCharacters = Utility.getAllCharacters();
            foreach (NPC npc in Utility.getAllCharacters())
            {
                if (npc.IsVillager && !characters.ContainsKey(npc.Name))
                    characters[npc.Name] = new CharacterInfo(npc.Name, npc.Gender == Gender.Male, type: CharacterType.Villager);
            }

            // add children
            foreach (Child childNpc in Game1.player.getChildren())
            {
                // add child
                CharacterInfo child = new CharacterInfo(childNpc.Name, isMale: childNpc.Gender == Gender.Male);
                characters[child.Name] = child;

                // add relationships
                this.AddRelationship(player, player.IsMale ? Relationship.Father : Relationship.Mother, child, child.IsMale ? Relationship.Son : Relationship.Daughter);
                if (spouse != null)
                {
                    foreach (CharacterRelationship parentRelation in spouse.Relationships)
                    {
                        switch (parentRelation.Relationship)
                        {
                            case Relationship.Grandfather:
                            case Relationship.Grandmother:
                                this.AddRelationship(child, child.IsMale ? Relationship.GreatGrandson : Relationship.GreatGranddaughter, parentRelation.Character, parentRelation.Character.IsMale ? Relationship.GreatGrandfather : Relationship.GreatGrandmother);
                                break;

                            case Relationship.Father:
                            case Relationship.Mother:
                            case Relationship.StepFather:
                            case Relationship.StepMother:
                                this.AddRelationship(child, child.IsMale ? Relationship.Grandson : Relationship.Granddaughter, parentRelation.Character, parentRelation.Character.IsMale ? Relationship.Grandfather : Relationship.Grandmother);
                                break;

                            case Relationship.Brother:
                            case Relationship.Sister:
                            case Relationship.HalfBrother:
                            case Relationship.HalfSister:
                                this.AddRelationship(child, child.IsMale ? Relationship.Nephew : Relationship.Niece, parentRelation.Character, parentRelation.Character.IsMale ? Relationship.Uncle : Relationship.Aunt);
                                break;

                            case Relationship.Niece:
                            case Relationship.Nephew:
                                this.AddRelationship(child, Relationship.Cousin, parentRelation.Character, Relationship.Cousin);
                                break;

                            case Relationship.Son:
                            case Relationship.Daughter:
                                this.AddRelationship(child, child.IsMale ? Relationship.Brother : Relationship.Sister, parentRelation.Character, parentRelation.Character.IsMale ? Relationship.Brother : Relationship.Sister);
                                break;
                        }
                    }
                    this.AddRelationship(spouse, spouse.IsMale ? Relationship.Father : Relationship.Mother, child, child.IsMale ? Relationship.Son : Relationship.Daughter);
                }
            }

            return characters;
        }

        /// <summary>Add a relationship between two NPCs.</summary>
        /// <param name="left">The left NPC.</param>
        /// <param name="leftType">The left relationship type (i.e. <paramref name="left"/> is the _____ of <paramref name="right"/>).</param>
        /// <param name="right">The right NPC.</param>
        /// <param name="rightType">The right relationship type (i.e. <paramref name="right"/> is the _____ of <paramref name="left"/>).</param>
        private void AddRelationship(CharacterInfo left, Relationship leftType, CharacterInfo right, Relationship rightType)
        {
            left.AddRelationship(rightType, right);
            right.AddRelationship(leftType, left);
        }

        /// <summary>Add a friend relationship between two NPCs.</summary>
        /// <param name="left">The left NPC.</param>
        /// <param name="right">The right NPC.</param>
        private void AddFriend(CharacterInfo left, CharacterInfo right)
        {
            this.AddRelationship(left, Relationship.Friend, right, Relationship.Friend);
        }

        /// <summary>Add friendship points with an NPC, if the NPC exists.</summary>
        /// <param name="farmer">The farmer instance.</param>
        /// <param name="npc">The NPC instance.</param>
        /// <param name="points">The number of points to add.</param>
        private void AddFriendshipPoints(Farmer farmer, NPC npc, int points)
        {
            if (npc != null && farmer != null) // e.g. Kent might not have arrived yet
                farmer.changeFriendship(points, npc);
        }

        private static int GetCurrentDayKey()
        {
            int seasonOffset = Game1.currentSeason switch
            {
                "spring" => 0,
                "summer" => 100,
                "fall" => 200,
                "winter" => 300,
                _ => 400
            };

            return (Game1.year * 1000) + seasonOffset + Game1.dayOfMonth;
        }

        private PlayerData GetPlayerData(Farmer farmer)
        {
            long id = farmer?.UniqueMultiplayerID ?? 0;
            if (!this.PlayerData.TryGetValue(id, out PlayerData playerData))
            {
                playerData = new PlayerData();
                this.PlayerData[id] = playerData;
            }

            // Lazy initialization for quest tracking to handle farmhands joining anytime
            // Initialize to current count to prevent false "quest completed" detection
            if (playerData.LastKnownQuestCount == null)
            {
                playerData.LastKnownQuestCount = farmer.stats.QuestsCompleted;
            }

            return playerData;
        }

        private IDictionary<long, PlayerData> LoadPlayerData()
        {
            var errors = new List<string>();

            IDictionary<long, PlayerData> data = this.TryReadPlayerDataFormat<Dictionary<long, PlayerData>>(errors);
            if (data != null)
                return data;

            var stringKeyed = this.TryReadPlayerDataFormat<Dictionary<string, PlayerData>>(errors);
            if (stringKeyed != null)
                return PlayerDataMigration.ConvertStringKeyed(stringKeyed, Game1.player.UniqueMultiplayerID);

            var legacyFlags = this.TryReadPlayerDataFormat<Dictionary<string, bool>>(errors);
            if (PlayerDataMigration.TryConvertLegacyFlagMap(legacyFlags, Game1.player.UniqueMultiplayerID, out IDictionary<long, PlayerData> migratedFlags))
                return migratedFlags;

            PlayerData legacy = this.TryReadPlayerDataFormat<PlayerData>(errors);
            if (legacy != null)
                return PlayerDataMigration.WrapLegacyPlayerData(legacy, Game1.player.UniqueMultiplayerID);

            if (errors.Count > 0)
                this.Monitor.Log($"LoadPlayerData: couldn't read known formats ({string.Join(" | ", errors)}). Falling back to empty data.", LogLevel.Warn);

            return new Dictionary<long, PlayerData>();
        }

        private T TryReadPlayerDataFormat<T>(ICollection<string> errors)
            where T : class
        {
            try
            {
                return this.Helper.Data.ReadSaveData<T>("data")
                    ?? this.Helper.Data.ReadJsonFile<T>($"{Constants.SaveFolderName}/config.json");
            }
            catch (Exception ex)
            {
                errors.Add($"{typeof(T).Name}: {ex.Message}");
                return null;
            }
        }
    }
}
