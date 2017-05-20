﻿using RLNET;
using System;
using System.Collections.Generic;
using System.Linq;
using Next_Game.Cartographic;
using Next_Game.Event_System;
using System.Text;
using System.Threading.Tasks;

namespace Next_Game
{
    public enum StoryAI { None, Benevolent, Balanced, Evil, Tricky }
    public enum GameState { None, Justice, Legend_Usurper, Legend_King, Honour_Usurper, Honour_King, Count } //arrayOfGameStates primary index
    public enum DataState { Good, Bad, Change, Count } //arrayOfGameStates secondary index (change indicates item changed since last redraw, +ve # is good, -ve is bad)
    public enum ConflictType { None, Combat, Social, Stealth, Special } //broad category (special is solely for overriding default challenge data -> used by autocreate events only)
    public enum ConflictCombat { None, Personal, Tournament, Battle, Hunting } //sub category -> a copy should be in ConflictSubType
    public enum ConflictSocial { None, Blackmail, Seduce, Befriend } //sub category -> a copy should be in ConflictSubType
    public enum ConflictStealth { None, Infiltrate, Evade, Escape } //sub category -> a copy should be in ConflictSubType
    public enum ConflictSubType { None, Personal, Tournament, Battle, Hunting, Blackmail, Seduce, Befriend, Infiltrate, Evade, Escape, Special } //combined list of all subtypes (add to as needed)
    public enum ConflictSpecial { None, Fortified_Position, Mountain_Country, Forest_Country, Castle_Walls } //special situations
    public enum ConflictResult { None, MinorWin, Win, MajorWin, MinorLoss, Loss, MajorLoss, Count } //result of challenge
    public enum ConflictState { None, Relative_Army_Size, Relative_Fame, Relative_Honour, Relative_Justice, Known_Status } //game specific states that are used for situations
    public enum ResourceLevel { None, Meagre, Moderate, Substantial, Wealthy, Excessive }


    /// <summary>
    /// used to store all triggered events for the current turn
    /// </summary>
    public class EventPackage
    {
        public Active Person { get; set; }
        public Event EventObject { get; set; }
        public bool Done { get; set; }
        public int OpponentID { get; set; } //optional
        public int LocationID { get; set; } //optional
        public int HouseID { get; set; } //optional
        public int RefID { get; set; } //optional
    }

    /// <summary>
    /// Director that manages the game world according to a Story AI personality
    /// </summary>
    public class Director
    {
        static Random rnd;
        Story story;
        //State state;
        public int EventAutoID { get; set; } = 2000; //used to provide a unique Player Event ID for auto created events
        public int NumAutoReactEvents { get; set; } = 0; //number of active autoReact events currently out there as Player events
        private int[,] arrayOfGameStates; //tracks s (enum GameStates), all are index 0 -> good, index 1 -> bad
        //events
        List<int> listOfActiveGeoClusters; //clusters that have a road through them (GeoID's)
        List<int> listGenFollEventsForest; //generic events for followers
        List<int> listGenFollEventsMountain;
        //List<int> listGenFollEventsSea;
        List<int> listGenFollEventsNormal;
        List<int> listGenFollEventsKing;
        List<int> listGenFollEventsConnector;
        List<int> listGenFollEventsCapital;
        List<int> listGenFollEventsMajor;
        List<int> listGenFollEventsMinor;
        List<int> listGenFollEventsInn;
        //archetype follower events
        List<int> listArcFollRoadEventsNormal;
        List<int> listArcFollRoadEventsKings;
        List<int> listArcFollRoadEventsConnector;
        List<int> listArcFollCapitalEvents;
        //Player generic events
        List<int> listGenPlyrEventsForest;
        List<int> listGenPlyrEventsMountain;
        List<int> listGenPlyrEventsSea;
        List<int> listGenPlyrEventsUnsafe;
        List<int> listGenPlyrEventsNormal;
        List<int> listGenPlyrEventsKing;
        List<int> listGenPlyrEventsConnector;
        List<int> listGenPlyrEventsCapital;
        List<int> listGenPlyrEventsMajor;
        List<int> listGenPlyrEventsMinor;
        List<int> listGenPlyrEventsInn;
        List<int> listGenPlyrEventsDungeon;
        List<int> listGenPlyrEventsAdrift;
        //archetype player events
        List<int> listArcPlyrRoadEventsNormal;
        List<int> listArcPlyrRoadEventsKings;
        List<int> listArcPlyrRoadEventsConnector;
        List<int> listArcPlyrCapitalEvents;
        //Rumours
        List<int> listRumoursCapital; //specific to capital
        List<int> listRumoursGlobal; //world wide
        List<int> listRumoursNorth; //north branch
        List<int> listRumoursEast;
        List<int> listRumoursSouth;
        List<int> listRumoursWest;
        //other
        List<Follower> listOfFollowers;
        List<EventPackage> listFollCurrentEvents; //follower
        List<EventPackage> listPlyrCurrentEvents; //player
        private Dictionary<int, EventFollower> dictFollowerEvents;
        private Dictionary<int, EventPlayer> dictPlayerEvents;
        private Dictionary<int, EventPlayer> dictAutoEvents;
        private Dictionary<int, Archetype> dictArchetypes;
        private Dictionary<int, Story> dictStories;
        private Dictionary<int, Situation> dictSituationsNormal;
        private Dictionary<int, Situation> dictSituationsGame;
        private Dictionary<int, Situation> dictSituationsSpecial;
        private Dictionary<int, Situation> dictSituationsSkill;
        private Dictionary<int, Situation> dictSituationsTouched;
        private Dictionary<int, Situation> dictSituationsSupporter;
        private Dictionary<int, Result> dictResults;
        private Dictionary<ConflictSubType, Challenge> dictChallenges;


        public Director(int seed)
        {
            rnd = new Random(seed);
            //state = new State(seed);
            arrayOfGameStates = new int[(int)GameState.Count, (int)DataState.Count];
            //follower generic events
            listOfActiveGeoClusters = new List<int>();
            listGenFollEventsForest = new List<int>();
            listGenFollEventsMountain = new List<int>();
            //listGenFollEventsSea = new List<int>();
            listGenFollEventsNormal = new List<int>(); //note that Normal road generic events also apply to all types of Roads (Royal generics -> Royal + Normal, for example)
            listGenFollEventsKing = new List<int>();
            listGenFollEventsConnector = new List<int>();
            listGenFollEventsCapital = new List<int>();
            listGenFollEventsMajor = new List<int>();
            listGenFollEventsMinor = new List<int>();
            listGenFollEventsInn = new List<int>();
            //archetype follower events
            listArcFollRoadEventsNormal = new List<int>();
            listArcFollRoadEventsKings = new List<int>();
            listArcFollRoadEventsConnector = new List<int>();
            listArcFollCapitalEvents = new List<int>();
            //Player events
            listGenPlyrEventsForest = new List<int>();
            listGenPlyrEventsMountain = new List<int>();
            listGenPlyrEventsSea = new List<int>();
            listGenPlyrEventsUnsafe = new List<int>();
            listGenPlyrEventsNormal = new List<int>();
            listGenPlyrEventsKing = new List<int>();
            listGenPlyrEventsConnector = new List<int>();
            listGenPlyrEventsCapital = new List<int>();
            listGenPlyrEventsMajor = new List<int>();
            listGenPlyrEventsMinor = new List<int>();
            listGenPlyrEventsInn = new List<int>();
            listGenPlyrEventsDungeon = new List<int>();
            listGenPlyrEventsAdrift = new List<int>();
            //archetype player events
            listArcPlyrRoadEventsNormal = new List<int>();
            listArcPlyrRoadEventsKings = new List<int>();
            listArcPlyrRoadEventsConnector = new List<int>();
            listArcPlyrCapitalEvents = new List<int>();
            //Rumours
            listRumoursCapital = new List<int>();
            listRumoursGlobal = new List<int>();
            listRumoursNorth = new List<int>();
            listRumoursEast = new List<int>();
            listRumoursSouth = new List<int>();
            listRumoursWest = new List<int>();
            //other
            listFollCurrentEvents = new List<EventPackage>(); //follower events
            listPlyrCurrentEvents = new List<EventPackage>(); //player events
            listOfFollowers = new List<Follower>();
            dictFollowerEvents = new Dictionary<int, EventFollower>();
            dictPlayerEvents = new Dictionary<int, EventPlayer>();
            dictAutoEvents = new Dictionary<int, EventPlayer>();
            dictArchetypes = new Dictionary<int, Archetype>();
            dictStories = new Dictionary<int, Story>();
            dictSituationsNormal = new Dictionary<int, Situation>(); //first two situations (def. adv. & neutral)
            dictSituationsGame = new Dictionary<int, Situation>(); //third, game specific, situation
            dictSituationsSpecial = new Dictionary<int, Situation>(); //decisions-derived special situations
            dictSituationsSkill = new Dictionary<int, Situation>(); //primary skill involved in a challenge
            dictSituationsTouched = new Dictionary<int, Situation>(); //Touched skill involved in a challenge
            dictSituationsSupporter = new Dictionary<int, Situation>(); //supporters involved in a challenge
            dictResults = new Dictionary<int, Result>(); //predefined results of a challenge outcome
            dictChallenges = new Dictionary<ConflictSubType, Challenge>(); //challenge data unique to individual challenge types
        }

        /// <summary>
        /// Initialisation
        /// </summary>
        public void InitialiseDirector()
        {
            listOfActiveGeoClusters.AddRange(Game.map.GetActiveGeoClusters()); //Run FIRST
            Game.logStart?.Write("--- Import Follower Events (Director.cs)");
            dictFollowerEvents = Game.file.GetFollowerEvents("Events_Follower.txt");
            Game.logStart?.Write("--- Import Player Events (Director.cs)");
            dictPlayerEvents = Game.file.GetPlayerEvents("Events_Player.txt");
            Game.logStart?.Write("--- Import AutoReact Events (Director.cs)");
            AddAutoEvents(Game.file.GetPlayerEvents("Events_AutoReact.txt"));
            InitialiseGenericEvents();
            Game.logStart?.Write("--- Import Archetypes (Director.cs)"); //Run AFTER importing Events
            dictArchetypes = Game.file.GetArchetypes("Archetypes.txt");
            Game.logStart?.Write("--- Import Stories (Director.cs)"); //Run AFTER importing Archetypes
            dictStories = Game.file.GetStories("Stories.txt");
            story = SetStory(1); //choose which story to use
            Game.logStart?.Write("--- Initialise Archetypes (Director.cs)");
            InitialiseArchetypes();
            Game.logStart?.Write("--- Initialise Normal Situations (Director.cs)");
            dictSituationsNormal = Game.file.GetSituations("SitNormal.txt");
            Game.logStart?.Write("--- Initialise Game Specific Situations (Director.cs)");
            dictSituationsGame = Game.file.GetSituations("SitGame.txt");
            Game.logStart?.Write("--- Initialise Special Situations (Director.cs)");
            dictSituationsSpecial = Game.file.GetSituations("SitSpecial.txt");
            Game.logStart?.Write("--- Initialise Skill Situations (Director.cs)");
            dictSituationsSkill = Game.file.GetSituations("SitSkill.txt");
            Game.logStart?.Write("--- Initialise Touched Situations (Director.cs)");
            dictSituationsTouched = Game.file.GetSituations("SitTouched.txt");
            Game.logStart?.Write("--- Initialise Supporter Situations (Director.cs)");
            dictSituationsSupporter = Game.file.GetSituations("SitSupporter.txt");
            Game.logStart?.Write("--- Initialise Results (Director.cs)");
            dictResults = Game.file.GetResults("Results.txt");
            Game.logStart?.Write("--- Initialise Challenges (Director.cs)"); //run AFTER GetResults
            dictChallenges = Game.file.GetChallenges("Challenge.txt");
            Game.logStart?.Write("--- InitialiseGameStates (Director.cs)");
            InitialiseStartRumours();
            InitialiseGameStates();
        }

        /// <summary>
        ///Initialise all game states
        /// </summary>
        private void InitialiseGameStates()
        {
            int multiplier = Game.constant.GetValue(Global.GAME_STATE);
            //Justice -> Old King popularity (charm) - New King
            int popularity = Game.lore.OldKing.GetSkill(SkillType.Charm);
            Game.director.SetGameState(GameState.Justice, DataState.Good, popularity * multiplier);
            popularity = Game.lore.NewKing.GetSkill(SkillType.Charm);
            Game.director.SetGameState(GameState.Justice, DataState.Bad, popularity * multiplier);
            //Legend_Usurper -> Combat
            int legend = Game.lore.OldHeir.GetSkill(SkillType.Combat);
            if (legend > 3)
            { Game.director.SetGameState(GameState.Legend_Usurper, DataState.Good, (legend - 3) * multiplier); }
            else if (legend < 3)
            { Game.director.SetGameState(GameState.Legend_Usurper, DataState.Bad, (3 - legend) * multiplier); }
            //Legend_New King -> Combat
            legend = Game.lore.NewKing.GetSkill(SkillType.Combat);
            if (legend > 3)
            { Game.director.SetGameState(GameState.Legend_King, DataState.Good, (legend - 3) * multiplier); }
            else if (legend < 3)
            { Game.director.SetGameState(GameState.Legend_King, DataState.Bad, (3 - legend) * multiplier); }
            //Honour_Usurper -> Treachery (good is < 3)
            int treachery = Game.lore.OldHeir.GetSkill(SkillType.Treachery);
            if (treachery > 3)
            { Game.director.SetGameState(GameState.Honour_Usurper, DataState.Bad, (treachery - 3) * multiplier); }
            else if (treachery < 3)
            { Game.director.SetGameState(GameState.Honour_Usurper, DataState.Good, (3 - treachery) * multiplier); }
            //Honour_King -> Treachery (good is < 3)
            treachery = Game.lore.NewKing.GetSkill(SkillType.Treachery);
            if (treachery > 3)
            { Game.director.SetGameState(GameState.Honour_King, DataState.Bad, (treachery - 3) * multiplier); }
            else if (treachery < 3)
            { Game.director.SetGameState(GameState.Honour_King, DataState.Good, (3 - treachery) * multiplier); }
        }

        /// <summary>
        /// adds imported auto events to Player dictionary
        /// </summary>
        /// <param name="autoDictionary"></param>
        public void AddAutoEvents(Dictionary<int, EventPlayer> autoDictionary)
        {
            if (autoDictionary.Count > 0)
            {
                foreach (var eventObject in autoDictionary)
                {
                    try
                    { dictAutoEvents.Add(eventObject.Value.EventPID, eventObject.Value); Game.logStart?.Write(string.Format("\"{0}\" successfully added to DictAutoEvents", eventObject.Value.Name)); }
                    catch (ArgumentNullException)
                    { Game.SetError(new Error(117, string.Format("Invalid eventObject (null), eventID {0} in AddAutoEvents", eventObject.Value.EventPID))); }
                    catch (ArgumentException)
                    { Game.SetError(new Error(117, string.Format("Invalid eventObject (duplicate ID), eventPID {0} in AddAutoEvents", eventObject.Value.EventPID))); }
                }
            }
            else { Game.SetError(new Error(117, "Invalid auto Dictionary input (no records)")); }
        }

        /// <summary>
        /// loop all events and place Generic eventID's in their approrpriate lists for both Follower and Player event types
        /// </summary>
        private void InitialiseGenericEvents()
        {
            int eventID;
            //Follower events
            foreach (var eventObject in dictFollowerEvents)
            {
                if (eventObject.Value.Category == EventCategory.Generic)
                {
                    eventID = eventObject.Value.EventFID;
                    switch (eventObject.Value.Type)
                    {
                        case ArcType.GeoCluster:
                            switch (eventObject.Value.GeoType)
                            {
                                case ArcGeo.Forest:
                                    listGenFollEventsForest.Add(eventID);
                                    break;
                                case ArcGeo.Mountain:
                                    listGenFollEventsMountain.Add(eventID);
                                    break;
                                /*case ArcGeo.Sea:
                                    listGenFollEventsSea.Add(eventID); -> followers don't go to sea
                                    break;*/
                                default:
                                    Game.SetError(new Error(50, string.Format("Invalid Type, ArcGeo, Follower Event, ID {0}", eventID)));
                                    break;
                            }
                            break;
                        case ArcType.Location:
                            switch (eventObject.Value.LocType)
                            {
                                case ArcLoc.Capital:
                                    listGenFollEventsCapital.Add(eventID);
                                    break;
                                case ArcLoc.Major:
                                    listGenFollEventsMajor.Add(eventID);
                                    break;
                                case ArcLoc.Minor:
                                    listGenFollEventsMinor.Add(eventID);
                                    break;
                                case ArcLoc.Inn:
                                    listGenFollEventsInn.Add(eventID);
                                    break;
                                default:
                                    Game.SetError(new Error(50, string.Format("Invalid Type, ArcLoc, Follower Event, ID {0}", eventID)));
                                    break;
                            }
                            break;
                        case ArcType.Road:
                            switch (eventObject.Value.RoadType)
                            {
                                case ArcRoad.Normal:
                                    listGenFollEventsNormal.Add(eventID);
                                    break;
                                case ArcRoad.Kings:
                                    listGenFollEventsKing.Add(eventID);
                                    break;
                                case ArcRoad.Connector:
                                    listGenFollEventsConnector.Add(eventID);
                                    break;
                                default:
                                    Game.SetError(new Error(50, string.Format("Invalid Type, ArcRoad, Follower Event, ID {0}", eventID)));
                                    break;
                            }
                            break;
                        default:
                            Game.SetError(new Error(50, string.Format("Invalid Type, Unknown, Follower Event, ID {0}", eventID)));
                            break;
                    }
                }
            }
            //Player Events
            foreach (var eventObject in dictPlayerEvents)
            {
                if (eventObject.Value.Category == EventCategory.Generic)
                {
                    //assign to the correct event list
                    EventPlayer playerEventObject = eventObject.Value;
                    AssignPlayerEvent(playerEventObject);
                }
            }
        }

        /// <summary>
        /// Assigns Player event to correct eventList (called by InitialiseGenericEvents and AutoReact events)
        /// </summary>
        /// <param name="eventObject"></param>
        public void AssignPlayerEvent(EventPlayer eventObject)
        {
            int eventID;
            //assign to the correct list
            eventID = eventObject.EventPID;
            switch (eventObject.Type)
            {
                case ArcType.GeoCluster:
                    switch (eventObject.GeoType)
                    {
                        case ArcGeo.Forest:
                            listGenPlyrEventsForest.Add(eventID);
                            break;
                        case ArcGeo.Mountain:
                            listGenPlyrEventsMountain.Add(eventID);
                            break;
                        case ArcGeo.Sea:
                            listGenPlyrEventsSea.Add(eventID);
                            break;
                        case ArcGeo.Unsafe:
                            listGenPlyrEventsUnsafe.Add(eventID);
                            break;
                        default:
                            Game.SetError(new Error(50, string.Format("Invalid Type, ArcGeo, Player Event, ID {0}", eventID)));
                            break;
                    }
                    break;
                case ArcType.Location:
                    switch (eventObject.LocType)
                    {
                        case ArcLoc.Capital:
                            listGenPlyrEventsCapital.Add(eventID);
                            break;
                        case ArcLoc.Major:
                            listGenPlyrEventsMajor.Add(eventID);
                            break;
                        case ArcLoc.Minor:
                            listGenPlyrEventsMinor.Add(eventID);
                            break;
                        case ArcLoc.Inn:
                            listGenPlyrEventsInn.Add(eventID);
                            break;
                        default:
                            Game.SetError(new Error(50, string.Format("Invalid Type, ArcLoc, Player Event, ID {0}", eventID)));
                            break;
                    }
                    break;
                case ArcType.Road:
                    switch (eventObject.RoadType)
                    {
                        case ArcRoad.Normal:
                            listGenPlyrEventsNormal.Add(eventID);
                            break;
                        case ArcRoad.Kings:
                            listGenPlyrEventsKing.Add(eventID);
                            break;
                        case ArcRoad.Connector:
                            listGenPlyrEventsConnector.Add(eventID);
                            break;
                        default:
                            Game.SetError(new Error(50, string.Format("Invalid Type, ArcRoad, Player Event, ID {0}", eventID)));
                            break;
                    }
                    break;
                case ArcType.Dungeon:
                    listGenPlyrEventsDungeon.Add(eventID);
                    break;
                case ArcType.Adrift:
                    listGenPlyrEventsAdrift.Add(eventID);
                    break;
                default:
                    Game.SetError(new Error(50, string.Format("Invalid Type, Unknown, Player Event, ID {0}", eventID)));
                    break;
            }
        }

        /// <summary>
        /// check active (Follower only) characters for random events
        /// </summary>
        public void CheckFollowerEvents(Dictionary<int, Active> dictActiveActors)
        {
            //loop all active players
            foreach (var actor in dictActiveActors)
            {
                //not delayed, gone or the Player?
                if (actor.Value is Follower && actor.Value.Status != ActorStatus.Gone && actor.Value.Delay == 0)
                {
                    if (actor.Value.Status == ActorStatus.AtLocation)
                    {
                        //Location event
                        if (rnd.Next(100) <= story.Ev_Follower_Loc)
                        { DetermineFollowerEvent(actor.Value, EventType.Location); }
                    }
                    else if (actor.Value.Status == ActorStatus.Travelling)
                    {
                        //travelling event
                        if (rnd.Next(100) <= story.Ev_Follower_Trav)
                        { DetermineFollowerEvent(actor.Value, EventType.Travelling); }
                    }
                }
            }
        }

        /// <summary>
        /// handles all Player events
        /// </summary>
        public void CheckPlayerEvents()
        {
            //Active player = Game.world.GetActiveActor(1);
            Player player = Game.world.GetPlayer();

            if (player != null && player.Status != ActorStatus.Gone && player.Delay == 0)
            {
                Game.logTurn?.Write("--- CheckPlayerEvents (Director.cs)");
                //check first if any enemy is about to capture the Player
                if (player.Capture == true && player.Status != ActorStatus.Captured)
                { CreateAutoEnemyEvent(); }
                else
                {
                    switch (player.Status)
                    {
                        case ActorStatus.AtLocation:
                            //in a safe house
                            if (player.Conceal == ActorConceal.SafeHouse)
                            { CreateAutoLocEvent(EventAutoFilter.SafeHouse); }
                            //Location event
                            else
                            {
                                if (rnd.Next(100) <= story.Ev_Player_Loc_Current)
                                {
                                    DeterminePlayerEvent(player, EventType.Location);
                                    //chance of event halved after each occurence (prevents a long string of random events and gives space for necessary system events)
                                    story.Ev_Player_Loc_Current /= 2;
                                    Game.logTurn?.Write(string.Format(" Chance of Player Location event {0} %", story.Ev_Player_Loc_Current));
                                }
                                else
                                {
                                    //intro record/msg (only one that mentions 'event', all the rest are narrative
                                    Location loc = Game.network.GetLocation(player.LocID);
                                    if (loc != null)
                                    {
                                        string tempText = $"Ursurper {player.Name} at {loc.LocName}, {Game.world.ShowLocationCoords(loc.LocationID)}, [Event] \"What to do?\"";
                                        Record recordEvent = new Record(tempText, 1, loc.LocationID, CurrentActorIncident.Event);
                                        Game.world.SetPlayerRecord(recordEvent);
                                        Game.world.SetMessage(new Message(tempText, MessageType.Event));
                                    }
                                    else { Game.SetError(new Error(71, "Invalid loc (null) in Player AutoLocEvent -> No Record created")); }
                                    CreateAutoLocEvent(EventAutoFilter.None);
                                    //reset back to base figure
                                    story.Ev_Player_Loc_Current = story.Ev_Player_Loc_Base;
                                    Game.logTurn?.Write(string.Format(" Chance of Player Location event {0} %", story.Ev_Player_Loc_Current));
                                }
                            }
                            break;
                        case ActorStatus.Travelling:
                            //travelling event
                            if (rnd.Next(100) <= story.Ev_Player_Trav_Base)
                            { DeterminePlayerEvent(player, EventType.Travelling); }
                            break;
                        case ActorStatus.AtSea:
                            //sea voyage event
                            if (rnd.Next(100) <= story.Ev_Player_Sea_Base)
                            { DeterminePlayerEvent(player, EventType.Sea); }
                            break;
                        case ActorStatus.Adrift:
                            //adrift at sea
                            if (rnd.Next(100) <= story.Ev_Player_Adrift_Base)
                            { DeterminePlayerEvent(player, EventType.Adrift); }
                            break;
                        case ActorStatus.Captured:
                            //dungeon event
                            if (rnd.Next(100) <= story.Ev_Player_Dungeon_Base)
                            { DeterminePlayerEvent(player, EventType.Dungeon); }
                            break;
                        default:
                            Game.SetError(new Error(71, $"Invalid ActorStatus \"{player.Status}\" -> not recognised"));
                            break;
                    }
                }
            }
            else
            { Game.SetError(new Error(71, "Player not found (null)")); }
        }

        /// <summary>
        /// Determine which event applies to a Follower
        /// </summary>
        /// <param name="actor"></param>
        private void DetermineFollowerEvent(Active actor, EventType type)
        {
            int geoID, terrain, road, locID, refID, houseID;
            string tempText;
            Cartographic.Position pos = actor.GetActorPosition();
            List<Event> listEventPool = new List<Event>();
            locID = actor.LocID;
            refID = 0;
            //Location event
            if (type == EventType.Location)
            {
                refID = Game.map.GetMapInfo(Cartographic.MapLayer.RefID, pos.PosX, pos.PosY);
                houseID = Game.map.GetMapInfo(Cartographic.MapLayer.HouseID, pos.PosX, pos.PosY);
                Location loc = Game.network.GetLocation(locID);
                if (loc != null)
                {
                    if (locID == 1)
                    {
                        //capital
                        listEventPool.AddRange(GetValidFollowerEvents(listGenFollEventsCapital));
                        listEventPool.AddRange(GetValidFollowerEvents(listArcFollCapitalEvents));
                    }
                    else if (refID > 0 && refID < 100)
                    {
                        //Major House
                        listEventPool.AddRange(GetValidFollowerEvents(listGenFollEventsMajor));
                        listEventPool.AddRange(GetValidFollowerEvents(loc.GetFollowerEvents()));
                        House house = Game.world.GetHouse(refID);
                        if (house != null)
                        { listEventPool.AddRange(GetValidFollowerEvents(house.GetFollowerEvents())); }
                        else { Game.SetError(new Error(52, "Invalid Major House (refID)")); }
                    }
                    else if (refID >= 100 && refID < 1000)
                    {
                        //Minor House
                        listEventPool.AddRange(GetValidFollowerEvents(listGenFollEventsMinor));
                        listEventPool.AddRange(GetValidFollowerEvents(loc.GetFollowerEvents()));
                        House house = Game.world.GetHouse(refID);
                        if (house != null)
                        { listEventPool.AddRange(GetValidFollowerEvents(house.GetFollowerEvents())); }
                        else { Game.SetError(new Error(52, "Invalid Minor House (refID)")); }
                    }
                    else if (houseID == 99)
                    {
                        //Special Location - Inn
                        listEventPool.AddRange(GetValidFollowerEvents(listGenFollEventsInn));
                        listEventPool.AddRange(GetValidFollowerEvents(loc.GetFollowerEvents()));
                        House house = Game.world.GetHouse(refID);
                        if (house != null)
                        { listEventPool.AddRange(GetValidFollowerEvents(house.GetFollowerEvents())); }
                        else { Game.SetError(new Error(52, "Invalid Inn (refID)")); }
                    }
                    else
                    { Game.SetError(new Error(52, "Invalid Location Event Type")); }
                }
                else { Game.SetError(new Error(52, "Invalid loc (null)")); }
            }
            //Travelling event
            else if (type == EventType.Travelling)
            {
                //Get map data for actor's current location
                geoID = Game.map.GetMapInfo(Cartographic.MapLayer.GeoID, pos.PosX, pos.PosY);
                terrain = Game.map.GetMapInfo(Cartographic.MapLayer.Terrain, pos.PosX, pos.PosY);
                road = Game.map.GetMapInfo(Cartographic.MapLayer.Road, pos.PosX, pos.PosY);
                GeoCluster cluster = Game.world.GetGeoCluster(geoID);
                //get terrain & road events
                if (locID == 0 && terrain == 1)
                {
                    //mountains
                    listEventPool.AddRange(GetValidFollowerEvents(listGenFollEventsMountain));
                    listEventPool.AddRange(GetValidFollowerEvents(cluster.GetFollowerEvents()));
                }
                else if (locID == 0 && terrain == 2)
                {
                    //forests
                    listEventPool.AddRange(GetValidFollowerEvents(listGenFollEventsForest));
                    listEventPool.AddRange(GetValidFollowerEvents(cluster.GetFollowerEvents()));
                }
                else if (locID == 0 && terrain == 0)
                {
                    //road event
                    if (road == 1)
                    {
                        //normal road
                        listEventPool.AddRange(GetValidFollowerEvents(listGenFollEventsNormal));
                        listEventPool.AddRange(GetValidFollowerEvents(listArcFollRoadEventsNormal));
                    }
                    else if (road == 2)
                    {
                        //king's road
                        listEventPool.AddRange(GetValidFollowerEvents(listGenFollEventsKing));
                        listEventPool.AddRange(GetValidFollowerEvents(listArcFollRoadEventsKings));
                    }
                    else if (road == 3)
                    {
                        //connector road
                        listEventPool.AddRange(GetValidFollowerEvents(listGenFollEventsConnector));
                        listEventPool.AddRange(GetValidFollowerEvents(listArcFollRoadEventsConnector));
                    }
                }
            }
            //character specific events
            if (actor.GetNumFollowerEvents() > 0)
            { listEventPool.AddRange(GetValidFollowerEvents(actor.GetFollowerEvents())); }
            //choose an event
            if (listEventPool.Count > 0)
            {
                int rndNum = rnd.Next(0, listEventPool.Count);
                Event eventTemp = listEventPool[rndNum];
                EventFollower eventChosen = eventTemp as EventFollower;
                Message message = null; tempText = "";
                if (type == EventType.Travelling)
                {
                    tempText = string.Format("{0}, Aid {1} {2}, [{3} Event] \"{4}\"", actor.Name, actor.ActID, Game.world.ShowLocationCoords(actor.LocID),
                      type, eventChosen.Name);
                    message = new Message(tempText, MessageType.Event);
                }
                else if (type == EventType.Location)
                {
                    tempText = string.Format("{0}, Aid {1} at {2}, [{3} Event] \"{4}\"", actor.Name, actor.ActID, Game.world.GetLocationName(actor.LocID),
                      type, eventChosen.Name);
                    message = new Message(tempText, MessageType.Event);
                }
                if (message != null)
                {
                    Game.world.SetMessage(message);
                    if (String.IsNullOrEmpty(tempText) == false)
                    { Game.world.SetCurrentRecord(new Record(tempText, actor.ActID, actor.LocID, CurrentActorIncident.Event)); }
                }
                else { Game.SetError(new Error(52, "Invalid Message (null)")); }
                //store in list of Current Events
                EventPackage current = new EventPackage() { Person = actor, EventObject = eventChosen, Done = false };
                listFollCurrentEvents.Add(current);
            }
        }

        /// <summary>
        /// Determine which event applies to the Player
        /// </summary>
        /// <param name="actor"></param>
        private void DeterminePlayerEvent(Active actor, EventType type)
        {
            int geoID, terrain, road, locID, refID, houseID;
            string tempText;
            houseID = 0; refID = 0;
            Cartographic.Position pos = actor.GetActorPosition();
            List<Event> listEventPool = new List<Event>();
            locID = Game.map.GetMapInfo(Cartographic.MapLayer.LocID, pos.PosX, pos.PosY);
            switch (type)
            {
                case EventType.Location:
                    refID = Game.map.GetMapInfo(Cartographic.MapLayer.RefID, pos.PosX, pos.PosY);
                    houseID = Game.map.GetMapInfo(Cartographic.MapLayer.HouseID, pos.PosX, pos.PosY);
                    Location loc = Game.network.GetLocation(locID);
                    if (loc != null)
                    {
                        if (locID == 1)
                        {
                            //capital
                            if (type == EventType.Location)
                            {
                                listEventPool.AddRange(GetValidPlayerEvents(listGenPlyrEventsCapital));
                                listEventPool.AddRange(GetValidPlayerEvents(listArcPlyrCapitalEvents));
                            }
                        }
                        else if (refID > 0 && refID < 100)
                        {
                            //Major House
                            if (type == EventType.Location)
                            {
                                listEventPool.AddRange(GetValidPlayerEvents(listGenPlyrEventsMajor, houseID));
                                listEventPool.AddRange(GetValidPlayerEvents(loc.GetPlayerEvents()));
                                House house = Game.world.GetHouse(refID);
                                if (house != null)
                                { listEventPool.AddRange(GetValidPlayerEvents(house.GetPlayerEvents())); }
                                else { Game.SetError(new Error(72, "Invalid Major House (refID)")); }
                            }
                        }
                        else if (refID >= 100 && refID < 1000)
                        {
                            //Minor House
                            listEventPool.AddRange(GetValidPlayerEvents(listGenPlyrEventsMinor, houseID));
                            listEventPool.AddRange(GetValidPlayerEvents(loc.GetPlayerEvents()));
                            House house = Game.world.GetHouse(refID);
                            if (house != null)
                            { listEventPool.AddRange(GetValidPlayerEvents(house.GetPlayerEvents())); }
                            else { Game.SetError(new Error(72, "Invalid Minor House (refID)")); }
                        }
                        else if (houseID == 99)
                        {
                            //Special Location - Inn
                            listEventPool.AddRange(GetValidPlayerEvents(listGenPlyrEventsInn, houseID));
                            listEventPool.AddRange(GetValidPlayerEvents(loc.GetPlayerEvents()));
                            House house = Game.world.GetHouse(refID);
                            if (house != null)
                            { listEventPool.AddRange(GetValidPlayerEvents(house.GetPlayerEvents())); }
                            else { Game.SetError(new Error(72, "Invalid Inn (refID)")); }
                        }
                        else
                        { Game.SetError(new Error(72, "Invalid Location Event Type")); }
                    }
                    else { Game.SetError(new Error(72, "Invalid Loc (null)")); }
                    break;
                case EventType.Travelling:
                    //Get map data for actor's current location
                    geoID = Game.map.GetMapInfo(Cartographic.MapLayer.GeoID, pos.PosX, pos.PosY);
                    terrain = Game.map.GetMapInfo(Cartographic.MapLayer.Terrain, pos.PosX, pos.PosY);
                    road = Game.map.GetMapInfo(Cartographic.MapLayer.Road, pos.PosX, pos.PosY);
                    GeoCluster cluster = Game.world.GetGeoCluster(geoID);
                    //get terrain & road events
                    if (locID == 0 && terrain == 1)
                    {
                        //mountains
                        listEventPool.AddRange(GetValidPlayerEvents(listGenPlyrEventsMountain, geoID));
                        if (cluster != null)
                        { listEventPool.AddRange(GetValidPlayerEvents(cluster.GetPlayerEvents())); }
                        else { Game.SetError(new Error(72, "Invalid cluster Mountains (null)")); }
                    }
                    else if (locID == 0 && terrain == 2)
                    {
                        //forests
                        listEventPool.AddRange(GetValidPlayerEvents(listGenPlyrEventsForest, geoID));
                        if (cluster != null)
                        { listEventPool.AddRange(GetValidPlayerEvents(cluster.GetPlayerEvents())); }
                        else { Game.SetError(new Error(72, "Invalid cluster Forests (null)")); }
                    }
                    else if (locID == 0 && terrain == 0)
                    {
                        //road event
                        if (road == 1)
                        {
                            //normal road
                            listEventPool.AddRange(GetValidPlayerEvents(listGenPlyrEventsNormal));
                            listEventPool.AddRange(GetValidPlayerEvents(listArcPlyrRoadEventsNormal));
                        }
                        else if (road == 2)
                        {
                            //king's road
                            listEventPool.AddRange(GetValidPlayerEvents(listGenPlyrEventsKing));
                            listEventPool.AddRange(GetValidPlayerEvents(listArcPlyrRoadEventsKings));
                        }
                        else if (road == 3)
                        {
                            //connector road
                            listEventPool.AddRange(GetValidPlayerEvents(listGenPlyrEventsConnector));
                            listEventPool.AddRange(GetValidPlayerEvents(listArcPlyrRoadEventsConnector));
                        }
                    }
                    break;
                case EventType.Sea:
                    //general sea events
                    listEventPool.AddRange(GetValidPlayerEvents(listGenPlyrEventsSea));
                    //Get map data for actor's current location -> Sea Archetype events
                    geoID = Game.map.GetMapInfo(Cartographic.MapLayer.GeoID, pos.PosX, pos.PosY);
                    GeoCluster seaCluster = Game.world.GetGeoCluster(geoID);
                    if (seaCluster != null)
                    { listEventPool.AddRange(GetValidPlayerEvents(seaCluster.GetPlayerEvents())); }
                    else { Game.SetError(new Error(72, "Invalid cluster Sea (null)")); }
                    //Player at sea on a risky vessel
                    Player player = Game.world.GetPlayer();
                    if (player != null)
                    { if (player.VoyageSafe == false) { listEventPool.AddRange(GetValidPlayerEvents(listGenPlyrEventsUnsafe)); } }
                    else { Game.SetError(new Error(72, "Invalid Player (null)")); }
                    break;
                case EventType.Dungeon:
                    listEventPool.AddRange(GetValidPlayerEvents(listGenPlyrEventsDungeon));
                    break;
                case EventType.Adrift:
                    listEventPool.AddRange(GetValidPlayerEvents(listGenPlyrEventsAdrift));
                    break;
                default:
                    Game.SetError(new Error(72, $"Invalid type \"{type}\" -> not recognised"));
                    break;
            }

            //character specific events
            if (actor.GetNumPlayerEvents() > 0)
            { listEventPool.AddRange(GetValidPlayerEvents(actor.GetPlayerEvents())); }
            //choose an event
            if (listEventPool.Count > 0)
            {
                int rndNum = rnd.Next(0, listEventPool.Count);
                Event eventTemp = listEventPool[rndNum];
                EventPlayer eventChosen = eventTemp as EventPlayer;
                Message message = null; tempText = "";
                switch (type)
                {
                    case EventType.Location:
                        tempText = string.Format("{0}, Aid {1} at {2}, [{3} Event] \"{4}\"", actor.Name, actor.ActID, Game.world.GetLocationName(actor.LocID),
                      type, eventChosen.Name);
                        message = new Message(tempText, MessageType.Event);
                        break;
                    case EventType.Travelling:
                        tempText = string.Format("{0}, Aid {1} {2}, [{3} Event] \"{4}\"", actor.Name, actor.ActID, Game.world.ShowLocationCoords(actor.LocID),
                          type, eventChosen.Name);
                        message = new Message(tempText, MessageType.Event);
                        break;
                    case EventType.Sea:
                        if (actor.ActID == 1)
                        {
                            Active player = actor as Active;
                            tempText = string.Format("{0}, Aid {1} at Sea onboard the S.S \"{2}\", [{3} Event] \"{4}\"", player.Name, player.ActID, player.ShipName,
                          type, eventChosen.Name);
                        }
                        else { tempText = "unknown actor at sea"; }
                        message = new Message(tempText, MessageType.Event);
                        break;
                    case EventType.Dungeon:
                        tempText = string.Format("{0}, Aid {1} incarcerated in the dungeons of {2}, [{3} Event] \"{4}\"", actor.Name, actor.ActID,
                            Game.world.GetLocationName(actor.LocID), type, eventChosen.Name);
                        message = new Message(tempText, MessageType.Event);
                        break;
                    case EventType.Adrift:
                        tempText = string.Format("{0}, Aid {1} is adrift in {2}. Survival time {3} day{4}", actor.Name, actor.ActID, actor.SeaName, actor.DeathTimer,
                            actor.DeathTimer != 1 ? "s" : "");
                        message = new Message(tempText, MessageType.Event);
                        break;
                    default:
                        Game.SetError(new Error(72, $"Invalid EventType \"{type}\""));
                        break;
                }

                if (message != null)
                {
                    Game.world.SetMessage(message);
                    if (String.IsNullOrEmpty(tempText) == false)
                    { Game.world.SetPlayerRecord(new Record(tempText, actor.ActID, actor.LocID, CurrentActorIncident.Event)); }
                }
                else { Game.SetError(new Error(72, "Invalid Message (null)")); }
                //store in list of Current Events
                EventPackage current = new EventPackage() { Person = actor, EventObject = eventChosen, Done = false };
                listPlyrCurrentEvents.Add(current);
            }

        }

        /// <summary>
        /// Create a dynamic auto player event (at player's current location) whenever they are about to be captured by an enemy
        /// </summary>
        private void CreateAutoEnemyEvent()
        {
            //get player
            Player player = Game.world.GetPlayer();
            if (player != null)
            {
                //assumes Player.Captured == true and Player.listOfEnemies.Count > 0
                List<int> listEnemies = player.GetListOfEnemies();
                //find enemy with highest threat rating
                int enemyID = 0;
                int highestThreat = 0;
                Enemy enemy = null;
                if (listEnemies.Count > 0)
                {
                    foreach (int actID in listEnemies)
                    {
                        Enemy tempEnemy = Game.world.GetEnemyActor(actID);
                        if (tempEnemy != null)
                        {
                            if (tempEnemy.Threat > highestThreat)
                            { highestThreat = tempEnemy.Threat; enemyID = tempEnemy.ActID; enemy = tempEnemy; }
                        }
                        else { Game.SetError(new Error(173, string.Format("Invalid enemy (null) for ActID \"{0}\"", actID))); }
                    }

                    if (enemy != null)
                    {
                        string eventName = "Unknown";
                        string eventText = "Unknown";
                        //type of enemy
                        if (enemy is Inquisitor)
                        {
                            eventName = "Enemy Afoot";
                            eventText = "The Dark Hooded man staring intently at you is an Inquisitor. " + enemy.Name + " calls on you to YIELD";
                        }
                        else if (enemy is Nemesis)
                        {
                            eventName = "Nemesis Catches Up";
                            eventText = "The Gods must be truly angry as your Nemesis, " + enemy.Name + " is now before you.";
                        }
                        EventPlayer eventObject = new EventPlayer(1000, eventName, EventFrequency.Low)
                        { Category = EventCategory.AutoCreate, Status = EventStatus.Active, Type = ArcType.Actor, Text = eventText };

                        //default option -> Surrender
                        OptionInteractive option_1 = new OptionInteractive("Lay down your Weapons") { ActorID = enemy.ActID };
                        option_1.ReplyGood = string.Format("{0} forcibly restrains you and leads you to the nearest dungeon. Any items you possess will be confiscated.", enemy.Name);
                        OutFreedom outcome_1 = new OutFreedom(eventObject.EventPID, -1);
                        option_1.SetGoodOutcome(outcome_1);
                        eventObject.SetOption(option_1);

                        //fight option
                        OptionInteractive option_2 = new OptionInteractive("Draw your Sword") { ActorID = enemy.ActID };
                        option_2.ReplyGood = string.Format("{0} reaches for his weapon and lunges at you", enemy.Name);
                        OutConflict outcome_2 = new OutConflict(eventObject.EventPID, enemy.ActID, ConflictType.Combat) { Combat_Type = ConflictCombat.Personal, SubType = ConflictSubType.Personal };
                        //customise conflict data -> Outcome texts and results
                        string[] overideOutcomes_2 = new string[7] {
                        "Dazed, you manage to escape, barely",
                        "You opponent is left flat footed and winded as you make your escape",
                        "You stand over your defiant enemy before departing. Your legend grows.",
                        "You have been caught, but luckily you are uninjured",
                        "You have been caught and sustain minor injuries",
                        "You have been caught and have been badly injured",
                        "Breathing hard, your opponent scowls at you."};
                        outcome_2.challenge.SetOutcomes(overideOutcomes_2);
                        outcome_2.challenge.SetResults(ConflictResult.MajorWin, new List<int> { 5 });
                        outcome_2.challenge.SetResults(ConflictResult.MinorWin, new List<int> { 28 });
                        outcome_2.challenge.SetResults(ConflictResult.MinorLoss, new List<int> { 45 });
                        outcome_2.challenge.SetResults(ConflictResult.Loss, new List<int> { 45, 28 });
                        outcome_2.challenge.SetResults(ConflictResult.MajorLoss, new List<int> { 45, 42 });
                        outcome_2.challenge.SetOveride(true);
                        option_2.SetGoodOutcome(outcome_2);
                        eventObject.SetOption(option_2);

                        //flee option -> player is the defender
                        OptionInteractive option_3 = new OptionInteractive("Run like the Wind") { ActorID = enemy.ActID };
                        option_3.ReplyGood = string.Format("{0} spits, curses and gives pursuit", enemy.Name);
                        OutConflict outcome_3 = new OutConflict(eventObject.EventPID, enemy.ActID, ConflictType.Stealth, false) { Stealth_Type = ConflictStealth.Evade, SubType = ConflictSubType.Evade };
                        //customise conflict data -> Outcome texts and results
                        string[] overideOutcomes_3 = new string[7] {
                        "It was very close but you've given them the slip. You're exhausted",
                        "You are free and clear",
                        "You are a veritable Ghost. A legend among men.",
                        "You tried and failed. You've been caught",
                        "You have been caught and are exhausted",
                        "You have been caught and have been injured",
                        "They know where you are but they can't reach you. It's an impasse."};
                        outcome_3.challenge.SetOutcomes(overideOutcomes_3);
                        outcome_3.challenge.SetResults(ConflictResult.MajorWin, new List<int> { 5 });
                        outcome_3.challenge.SetResults(ConflictResult.MinorWin, new List<int> { 46 });
                        outcome_3.challenge.SetResults(ConflictResult.MinorLoss, new List<int> { 45 });
                        outcome_3.challenge.SetResults(ConflictResult.Loss, new List<int> { 45, 46 });
                        outcome_3.challenge.SetResults(ConflictResult.MajorLoss, new List<int> { 45, 42 });
                        outcome_3.challenge.SetOveride(true);
                        option_3.SetGoodOutcome(outcome_3);
                        eventObject.SetOption(option_3);

                        //Create & Add Event Package
                        EventPackage package = new EventPackage() { Person = player, EventObject = eventObject, Done = false };
                        listPlyrCurrentEvents.Add(package);
                        //if more than the current event present the original one (autocreated) needs to be deleted
                        if (listPlyrCurrentEvents.Count > 1) { listPlyrCurrentEvents.RemoveAt(0); }
                        //add to Player dictionary (ResolveOutcome looks for it there) -> check not an instance present already
                        if (dictPlayerEvents.ContainsKey(1000)) { dictPlayerEvents.Remove(1000); }
                        dictPlayerEvents.Add(1000, eventObject);
                        //message
                        string locText = "Unknown";
                        if (player.Status == ActorStatus.AtLocation) { locText = "at " + Game.world.GetLocationName(player.LocID); }
                        else if (player.Status == ActorStatus.Travelling) { locText = "travelling to " + Game.world.GetLocationName(player.LocID); }
                        string tempText = string.Format("{0}, Aid {1} {2}, [{3} Event] \"{4}\"", player.Name, player.ActID, locText, eventObject.Type, eventObject.Name);
                        Message message = new Message(tempText, MessageType.Event);
                        Game.world.SetMessage(message);
                        Game.world.SetPlayerRecord(new Record(tempText, player.ActID, player.LocID, CurrentActorIncident.Event));
                    }
                    else { Game.SetError(new Error(173, "Invalid enemy (null) from search for the highest threat rated enemy loop")); }
                }
                else { Game.SetError(new Error(173, "Invalid listEnemies (Empty)")); }
            }
            else
            { Game.SetError(new Error(173, "Invalid Player (null)")); }
        }

        /// <summary>
        /// create a dynamic auto player location event - assumed to be at player's current location
        /// <param name="filter">Which group of people should the event focus on (from pool of people present at the location)</param>
        /// </summary>
        private void CreateAutoLocEvent(EventAutoFilter filter, int actorID = 0)
        {
            //get player
            Player player = (Player)Game.world.GetActiveActor(1);
            if (player != null)
            {
                Game.logTurn?.Write("- CreateAutoLocEvent (Director.cs)");
                List<Actor> listActors = new List<Actor>();
                List<Passive> listCourt = new List<Passive>();
                List<Passive> listAdvisors = new List<Passive>();
                List<Passive> listVisitors = new List<Passive>();
                List<Follower> listFollowers = new List<Follower>();
                List<Trigger> listTriggers = new List<Trigger>();
                int limit; //loop counter, prevents overshooting the # of available function keys
                int locID = player.LocID;
                int rndLocID, voyageDistance;
                int locType = 0; //1 - capital, 2 - MajorHouse, 3 - MinorHouse, 4 - Inn
                int talkRel = Game.constant.GetValue(Global.TALK_THRESHOLD);
                int speed = Game.constant.GetValue(Global.SEA_SPEED);
                int chance, voyageTime;
                string actorText = "unknown"; string optionText = "unknown"; string locName = "unknown";
                Location loc = Game.network.GetLocation(locID);
                if (loc != null)
                { locName = Game.world.GetLocationName(locID); }
                else { Game.SetError(new Error(73, "Invalid Loc (null)")); }
                int houseID = Game.map.GetMapInfo(MapLayer.HouseID, loc.GetPosX(), loc.GetPosY());
                int refID = Game.map.GetMapInfo(MapLayer.RefID, loc.GetPosX(), loc.GetPosY());
                House house = Game.world.GetHouse(refID);
                if (house == null && refID != 9999) { Game.SetError(new Error(118, "Invalid house (null)")); }
                string houseName = "Unknown";
                string tempText;
                if (refID > 0) { houseName = Game.world.GetHouseName(refID); }
                int testRefID; //which refID (loc) to use when checking who's present
                //what type of location?
                if (locID == 1) { locType = 1; } //capital
                else if (refID > 0 && refID < 100) { locType = 2; } //major houses
                else if (refID >= 100 && refID < 1000) { locType = 3; } //minor houses
                else if (refID >= 1000 && houseID == 99) //inns
                {
                    locType = 4;
                    //can't be locals present at an Inn, only Visitors and Followers
                    if (filter == EventAutoFilter.Court) { filter = EventAutoFilter.Visitors; Game.SetError(new Error(118, "Invalid filter (Locals when at an Inn)")); }
                }
                else { Game.SetError(new Error(118, "Invalid locType (doesn't fit any criteria)")); }
                //Get actors present at location
                List<int> actorIDList = loc.GetActorList();
                if (actorIDList.Count > 0)
                {
                    //get actual actors
                    for (int i = 0; i < actorIDList.Count; i++)
                    {
                        Actor tempActor = Game.world.GetAnyActor(actorIDList[i]);
                        if (tempActor != null)
                        {   //exclude player from list (they are always present) & you
                            if (tempActor.ActID != 1)
                            { listActors.Add(tempActor); Game.logTurn?.Write(string.Format(" [AutoEvent -> ActorList] \"{0}\", ID {1} added to list of Actors", tempActor.Name, tempActor.ActID)); }
                        }
                        else { Game.SetError(new Error(118, string.Format("Invalid tempActor ID {0} (Null)", actorIDList[i]))); }
                    }
                    //filter actors accordingly
                    for (int i = 0; i < listActors.Count; i++)
                    {
                        Actor actor = listActors[i];
                        if (actor is Passive)
                        {
                            Passive tempPassive = actor as Passive;
                            testRefID = refID;
                            if (locType == 1) { testRefID = Game.lore.RoyalRefIDNew; }
                            if (tempPassive.RefID == testRefID && !(actor is Advisor))
                            {
                                if (tempPassive.Type == ActorType.Lord || tempPassive.Age >= 15)
                                {
                                    listCourt.Add(tempPassive); Game.logTurn?.Write(string.Format(" [AutoEvent -> LocalList] \"{0}\", ID {1} added to list of Locals",
                                      tempPassive.Name, tempPassive.ActID));
                                }
                            }
                            else if (actor is Advisor)
                            {
                                listAdvisors.Add(tempPassive); Game.logTurn?.Write(string.Format(" [AutoEvent -> AdvisorList] \"{0}\", ID {1} added to list of Advisors",
                                  tempPassive.Name, tempPassive.ActID));
                            }
                            else
                            {
                                if (tempPassive.Age >= 15)
                                {
                                    listVisitors.Add(tempPassive); Game.logTurn?.Write(string.Format(" [AutoEvent -> VisitorList] \"{0}\", ID {1} added to list of Visitors",
                                      tempPassive.Name, tempPassive.ActID));
                                }
                            }
                        }
                        else if (actor is Follower)
                        {
                            Follower tempFollower = actor as Follower;
                            listFollowers.Add(tempFollower);
                            Game.logTurn?.Write(string.Format(" [AutoEvent -> FollowerList] \"{0}\", ID {1} added to list of Followers", tempFollower.Name, tempFollower.ActID));
                        }
                    }
                    //new event (auto location events always have eventPID of '1000' -> old version in Player dict is deleted before new one added)
                    EventPlayer eventObject = new EventPlayer(1000, "What to do?", EventFrequency.Low) { Category = EventCategory.AutoCreate, Status = EventStatus.Active, Type = ArcType.Location };
                    tempText = "";
                    switch (filter)
                    {
                        case EventAutoFilter.None:
                            eventObject.Text = string.Format("You are at {0}. How will you fill your day?", locName);

                            //option -> audience with local House member
                            if (listCourt.Count() > 0)
                            {
                                OptionInteractive option = null;
                                if (locType != 1)
                                {
                                    option = new OptionInteractive(string.Format("Seek an Audience with a member of House {0} ({1} present)", houseName, listCourt.Count));
                                    option.ReplyGood = string.Format("House {0} has agreed to allow the Ursurper to enter the Court", houseName);
                                }
                                else
                                {
                                    //capital
                                    option = new OptionInteractive(string.Format("Seek an Audience with a member of the Royal Household ({0} present)", listCourt.Count));
                                    option.ReplyGood = string.Format("The Royal Clerk has advised that the Ursurper has permission to enter the Court");
                                }
                                OutEventChain outcome = new OutEventChain(1000, EventAutoFilter.Court);
                                option.SetGoodOutcome(outcome);
                                listTriggers.Clear();
                                listTriggers.Add(new Trigger(TriggerCheck.Known, 0, 1, EventCalc.Equals, false));
                                listTriggers.Add(new Trigger(TriggerCheck.Introduction, 0, 1, EventCalc.Equals, false));
                                option.SetTriggers(listTriggers);
                                eventObject.SetOption(option);
                            }
                            //option -> audience with Advisor
                            if (listAdvisors.Count() > 0)
                            {
                                OptionInteractive option = null;
                                if (locType != 1)
                                {
                                    option = new OptionInteractive(string.Format("Seek an Audience with an Advisor to House {0} ({1} present)", houseName, listAdvisors.Count));
                                    option.ReplyGood = string.Format("House {0} is willing to let you talk to whoever you wish", houseName);
                                }
                                else
                                {
                                    //capital
                                    option = new OptionInteractive(string.Format("Seek an Audience with a Royal Advisor ({0} present)", listAdvisors.Count));
                                    option.ReplyGood = string.Format("The Royal Clerk has advised that the household is willing to consider the matter");
                                }
                                OutEventChain outcome = new OutEventChain(1000, EventAutoFilter.Advisors);
                                option.SetGoodOutcome(outcome);
                                listTriggers.Clear();
                                listTriggers.Add(new Trigger(TriggerCheck.Known, 0, 1, EventCalc.Equals, false));
                                listTriggers.Add(new Trigger(TriggerCheck.Introduction, 0, 1, EventCalc.Equals, false));
                                option.SetTriggers(listTriggers);
                                eventObject.SetOption(option);
                            }
                            //option -> audience with Visitor
                            if (listVisitors.Count() > 0)
                            {
                                OptionInteractive option = new OptionInteractive(string.Format("Seek an Audience with a Visitor to House {0} ({1} present)", houseName, listVisitors.Count));
                                option.ReplyGood = string.Format("House {0} is willing to let you talk to whoever you wish", houseName);
                                OutEventChain outcome = new OutEventChain(1000, EventAutoFilter.Visitors);
                                option.SetGoodOutcome(outcome);
                                eventObject.SetOption(option);
                            }
                            //option -> audience with Follower
                            if (listFollowers.Count() > 0)
                            {
                                OptionInteractive option = new OptionInteractive(string.Format("Talk to one of your Loyal Followers ({0} present)", listFollowers.Count));
                                option.ReplyGood = "A conversation may well be possible";
                                OutEventChain outcome = new OutEventChain(1000, EventAutoFilter.Followers);
                                option.SetGoodOutcome(outcome);
                                eventObject.SetOption(option);
                            }

                            if (player.Known == false)
                            {
                                if (player.IntroPresented == false)
                                {
                                    //option -> Make yourself Known
                                    OptionInteractive optionKnown = new OptionInteractive("Make yourself Known");
                                    optionKnown.ReplyGood = "You reveal your identity and gain access to the Court";
                                    OutKnown outKnown = new OutKnown(eventObject.EventPID, -1);
                                    OutEventChain outcome = new OutEventChain(1000, EventAutoFilter.None);
                                    optionKnown.SetGoodOutcome(outKnown);
                                    optionKnown.SetGoodOutcome(outcome);
                                    eventObject.SetOption(optionKnown);
                                }
                                //option -> Use an introduction
                                int numIntros = player.GetNumOfIntroductions(refID);
                                if (numIntros > 0)
                                {
                                    OptionInteractive optionIntro = new OptionInteractive($"Use an Introduction (you have {numIntros})");
                                    optionIntro.ReplyGood = $"You present your written Introduction to House \"{houseName}\"";
                                    OutIntroduction outIntro = new OutIntroduction(eventObject.EventPID, refID);
                                    OutEventChain outEvent = new OutEventChain(1000, EventAutoFilter.None);
                                    optionIntro.SetGoodOutcome(outIntro);
                                    optionIntro.SetGoodOutcome(outEvent);
                                    eventObject.SetOption(optionIntro);
                                }
                            }
                            //option -> recruit follower (only at Inns)
                            if (locType == 4)
                            {
                                //you haven't yet reached the max. number of Followers allowed?
                                int numFollowers = Game.world.GetNumFollowers();
                                int maxFollowers = Game.constant.GetValue(Global.MAX_FOLLOWERS);
                                if (numFollowers >= maxFollowers)
                                { Game.logTurn?.Write(" Trigger: Player has max. allowed number of Followers already -> Recruit option cancelled"); }
                                else
                                {
                                    //there is at least one remaining follower available to recruit
                                    if (Game.history.GetNumRemainingFollowers() > 0)
                                    {
                                        //option has a random chance of appearing
                                        if (rnd.Next(100) <= Game.constant.GetValue(Global.RECRUIT_FOLLOWERS))
                                        {
                                            //are there any followers available to recruit in the inn?
                                            House tempHouse = Game.world.GetHouse(refID);
                                            if (tempHouse != null)
                                            {
                                                if (tempHouse is InnHouse)
                                                {
                                                    InnHouse inn = tempHouse as InnHouse;
                                                    int numAvailable = inn.GetNumFollowers();
                                                    if (numAvailable > 0)
                                                    {
                                                        OptionInteractive option = new OptionInteractive(string.Format("Recruit a Follower ({0} present)", numAvailable));
                                                        option.ReplyGood = "You are always on the lookout for loyal followers";
                                                        OutEventChain outcome = new OutEventChain(1000, EventAutoFilter.Recruit);
                                                        option.SetGoodOutcome(outcome);
                                                        eventObject.SetOption(option);
                                                    }
                                                    else { Game.logStart?.Write($"[Notification] No followers available for recruiting at \"{inn.Name}\""); }
                                                }
                                                else { Game.SetError(new Error(118, "House isn't a valid Inn")); }
                                            }
                                            else { Game.SetError(new Error(118, "Invalid House (null)")); }
                                        }
                                        else { Game.logStart?.Write($"[Notification] Random roll didn't succeed for recruiting followers option at LocID {locID}"); }
                                    }
                                    else { Game.logStart?.Write($"[Notification] No followers remaining in listActiveActors -> recruitment option cancelled"); }
                                }
                            }

                            //option -> seek information (if not known)
                            if (player.Known == false)
                            {
                                OptionInteractive option = new OptionInteractive("Ask around for Information");
                                option.ReplyGood = "You make some discreet enquiries";
                                OutRumour outRumour = new OutRumour(eventObject.EventPID);
                                option.SetGoodOutcome(outRumour);
                                eventObject.SetOption(option);
                            }
                            //option -> seek passage to another port (if applicable)
                            if (loc.Port == true)
                            { 
                                OptionInteractive option = new OptionInteractive("Seek Sea Passage to another Port");
                                option.ReplyGood = "You head to the harbour and search for a suitable ship";
                                //OutNone outcome = new OutNone(eventObject.EventPID);
                                OutEventChain outcome = new OutEventChain(1000, EventAutoFilter.Docks);
                                option.SetGoodOutcome(outcome);
                                eventObject.SetOption(option);
                            }
                            //option -> Lay low (only if not known)
                            if (player.Known == true && house?.SafeHouse > 0 && player.Conceal != ActorConceal.SafeHouse)
                            {
                                OptionInteractive option = new OptionInteractive($"Lay Low ({house.SafeHouse} stars)");
                                option.ReplyGood = $"You seek refuge at a Safe House ({house.SafeHouse} stars). You are immune from discovery while ever the place of refuge retains at least one star";
                                OutKnown outKnown = new OutKnown(eventObject.EventPID, 1);
                                OutSafeHouse outSafe = new OutSafeHouse(eventObject.EventPID, 1);
                                option.SetGoodOutcome(outKnown);
                                option.SetGoodOutcome(outSafe);
                                eventObject.SetOption(option);
                            }
                            //option -> Leave
                            OptionInteractive option_L = new OptionInteractive("Leave");
                            if (player.Known == true) { option_L.ReplyGood = "You depart, head held high, shoulders back, meeting the eye of everyone you pass"; }
                            else { option_L.ReplyGood = "You quietly depart, moving quietly through the mottled shadows"; }
                            OutNone outcome_L = new OutNone(eventObject.EventPID);
                            option_L.SetGoodOutcome(outcome_L);
                            eventObject.SetOption(option_L);
                            break;
                        case EventAutoFilter.SafeHouse:
                            eventObject.Name = $"Safe House at {house.LocName} ({house.SafeHouse} stars)";
                            eventObject.Text = "What do you wish to do, Sire?";
                            //option -> Remain in the safe house (default)
                            OptionInteractive optionStay = new OptionInteractive("Continue Laying Low");
                            optionStay.ReplyGood = "You choose to remain in your refuge, safe from the prying eyes of your enemies";
                            OutNone outcomeStay = new OutNone(eventObject.EventPID);
                            optionStay.SetGoodOutcome(outcomeStay);
                            eventObject.SetOption(optionStay);
                            //option -> Leave the safe house
                            OptionInteractive optionLeave = new OptionInteractive("Leave the Safe House");
                            optionLeave.ReplyGood = "Carefully, checking the lane, you leave the security of your safe house and venture back out into the world";
                            OutSafeHouse outcomeLeave = new OutSafeHouse(eventObject.EventPID, -1);
                            optionLeave.SetGoodOutcome(outcomeLeave);
                            eventObject.SetOption(optionLeave);
                            break;
                        case EventAutoFilter.Court:
                            eventObject.Name = "Talk to members of the Court";
                            eventObject.Text = string.Format("Which members of House {0} do you wish to talk to?", houseName);
                            //options -> one for each member present
                            limit = listCourt.Count();
                            limit = Math.Min(12, limit); //max 12 options possible (F1 - F12)
                            if (limit > 0)
                            {
                                //default option
                                OptionInteractive option = new OptionInteractive("You've changed your mind");
                                option.ReplyGood = string.Format("You depart {0} without further ado", Game.world.GetHouseName(refID));
                                OutNone outcome = new OutNone(eventObject.EventPID);
                                option.SetGoodOutcome(outcome);
                                eventObject.SetOption(option);
                            }
                            for (int i = 0; i < limit; i++)
                            {
                                Passive local = listCourt[i];
                                if (local.Office > ActorOffice.None)
                                { actorText = string.Format("{0} {1}", local.Office, local.Name); }
                                else { actorText = string.Format("{0} {1}", local.Type, local.Name); }
                                optionText = string.Format("Seek an audience with {0}", actorText);
                                OptionInteractive option = new OptionInteractive(optionText) { ActorID = local.ActID };
                                option.ReplyGood = string.Format("{0} has agreed to meet with you", actorText);
                                //OutNone outcome = new OutNone(eventObject.EventPID);
                                OutEventChain outcome = new OutEventChain(1000, EventAutoFilter.Interact);
                                option.SetGoodOutcome(outcome);
                                eventObject.SetOption(option);
                            }
                            break;
                        case EventAutoFilter.Advisors:
                            eventObject.Name = "Talk to Advisors";
                            eventObject.Text = string.Format("Which Advisor do you wish to talk to?");
                            //options -> one for each member present
                            limit = listAdvisors.Count();
                            limit = Math.Min(12, limit); //max 12 options possible (F1 - F12)
                            if (limit > 0)
                            {
                                //default option
                                OptionInteractive option = new OptionInteractive("You've changed your mind");
                                option.ReplyGood = string.Format("You depart {0} without further ado", Game.world.GetHouseName(refID));
                                OutNone outcome = new OutNone(eventObject.EventPID);
                                option.SetGoodOutcome(outcome);
                                eventObject.SetOption(option);
                            }
                            for (int i = 0; i < limit; i++)
                            {
                                Passive local = listAdvisors[i];
                                actorText = string.Format("{0} {1}", local.Title, local.Name);
                                optionText = string.Format("Seek an audience with {0}", actorText);
                                OptionInteractive option = new OptionInteractive(optionText) { ActorID = local.ActID };
                                option.ReplyGood = string.Format("{0} has agreed to meet with you", actorText);
                                listTriggers.Clear();
                                listTriggers.Add(new Trigger(TriggerCheck.RelPlyr, local.GetRelPlyr(), talkRel, EventCalc.GreaterThanOrEqual));
                                option.SetTriggers(listTriggers);
                                OutEventChain outcome = new OutEventChain(1000, EventAutoFilter.Interact);
                                option.SetGoodOutcome(outcome);
                                eventObject.SetOption(option);
                            }
                            break;
                        case EventAutoFilter.Visitors:
                            eventObject.Name = "Talk to Visitors";
                            eventObject.Text = string.Format("You are at {0}. Which visitor do you wish to talk to?", locName);
                            //options -> one for each member present
                            limit = listVisitors.Count();
                            limit = Math.Min(12, limit); //max 12 options possible (F1 - F12)
                            if (limit > 0)
                            {
                                //default option
                                OptionInteractive option = new OptionInteractive("You've changed your mind");
                                option.ReplyGood = string.Format("You depart {0} without further ado", Game.world.GetHouseName(refID));
                                OutNone outcome = new OutNone(eventObject.EventPID);
                                option.SetGoodOutcome(outcome);
                                eventObject.SetOption(option);
                            }
                            for (int i = 0; i < limit; i++)
                            {
                                Passive visitor = listVisitors[i];
                                actorText = string.Format("{0} {1}", visitor.Title, visitor.Name);
                                //actorText = string.Format("{0} {1}", visitor.Type, visitor.Name);
                                optionText = string.Format("Seek an audience with {0}", actorText);
                                OptionInteractive option = new OptionInteractive(optionText) { ActorID = visitor.ActID };
                                option.ReplyGood = string.Format("{0} has agreed to meet with you", actorText);
                                listTriggers.Clear();
                                listTriggers.Add(new Trigger(TriggerCheck.RelPlyr, visitor.GetRelPlyr(), talkRel, EventCalc.GreaterThanOrEqual));
                                option.SetTriggers(listTriggers);
                                //OutNone outcome = new OutNone(eventObject.EventPID);
                                OutEventChain outcome = new OutEventChain(1000, EventAutoFilter.Interact);
                                option.SetGoodOutcome(outcome);
                                eventObject.SetOption(option);
                            }
                            break;
                        case EventAutoFilter.Followers:
                            eventObject.Name = "Talk to Followers";
                            eventObject.Text = string.Format("You are at {0}. Which follower do you wish to talk to?", locName);
                            //options -> one for each member present
                            limit = listFollowers.Count();
                            limit = Math.Min(12, limit); //max 12 options possible (F1 - F12)
                            if (limit > 0)
                            {
                                //default option
                                OptionInteractive option = new OptionInteractive("You've changed your mind");
                                option.ReplyGood = string.Format("You depart {0} without further ado", Game.world.GetHouseName(refID));
                                OutNone outcome = new OutNone(eventObject.EventPID);
                                option.SetGoodOutcome(outcome);
                                eventObject.SetOption(option);
                            }
                            for (int i = 0; i < limit; i++)
                            {
                                Follower follower = listFollowers[i];
                                actorText = string.Format("{0} {1}", follower.Type, follower.Name);
                                optionText = string.Format("Find time to talk with {0}", actorText);
                                OptionInteractive option = new OptionInteractive(optionText) { ActorID = follower.ActID };
                                option.ReplyGood = string.Format("{0} is happy to sit down for a chat", actorText);
                                //OutNone outcome = new OutNone(eventObject.EventPID);
                                OutEventChain outcome = new OutEventChain(1000, EventAutoFilter.Interact);
                                option.SetGoodOutcome(outcome);
                                eventObject.SetOption(option);
                            }
                            break;

                        case EventAutoFilter.Recruit:
                            eventObject.Name = "Recruit a Follower";
                            eventObject.Text = string.Format("You are at {0}. Which follower do you wish to Recruit?", locName);
                            //options -> one for each recruit present
                            if (house != null)
                            {
                                InnHouse inn = house as InnHouse;
                                List<int> listRecruits = inn.GetFollowers();
                                limit = listRecruits.Count();
                                limit = Math.Min(12, limit); //max 12 options possible (F1 - F12)
                                List<Active> listRemainingFollowers = Game.history.GetActiveActors();
                                if (listRemainingFollowers.Count > 0)
                                {
                                    if (limit > 0)
                                    {
                                        //default option
                                        OptionInteractive option = new OptionInteractive("You've changed your mind");
                                        option.ReplyGood = string.Format("You depart {0} without further ado", Game.world.GetHouseName(refID));
                                        OutNone outcome = new OutNone(eventObject.EventPID);
                                        option.SetGoodOutcome(outcome);
                                        eventObject.SetOption(option);
                                    }
                                    for (int i = 0; i < limit; i++)
                                    {
                                        //get follower
                                        Follower follower = null;
                                        foreach (Active actor in listRemainingFollowers)
                                        {
                                            if (actor.ActID == listRecruits[i])
                                            {
                                                follower = (Follower)actor;
                                                break;
                                            }
                                        }
                                        //follower found
                                        if (follower != null)
                                        {
                                            optionText = $"\"{follower.Name}\" offers to help";
                                            OptionInteractive option = new OptionInteractive(optionText) { ActorID = follower.ActID };
                                            option.ReplyGood = $"{follower.Name} agrees to help you win back your throne";
                                            OutFollower outcome = new OutFollower(eventObject.EventPID);
                                            option.SetGoodOutcome(outcome);
                                            eventObject.SetOption(option);
                                        }
                                    }
                                }
                                else { Game.SetError(new Error(73, "Invalid listRemainingFollowers (no records)")); }
                            }
                            break;

                        case EventAutoFilter.Docks:
                            //visit the docks and assess your options
                            eventObject.Name = "Seek Passage to another Port";
                            eventObject.Text = $"You are at {locName}'s Docks. Squawk! How do you wish to proceed?";
                            //Option -> Leave
                            OptionInteractive option_d0 = new OptionInteractive("You've changed your mind.");
                            option_d0.ReplyGood = string.Format("You leave the smelly docks of {0} behind", Game.world.GetHouseName(refID));
                            OutNone outcome_d0 = new OutNone(eventObject.EventPID);
                            option_d0.SetGoodOutcome(outcome_d0);
                            eventObject.SetOption(option_d0);
                            //Option -> Look for a ship
                            OptionInteractive option_d1 = new OptionInteractive("Look for a suitable ship (Possible success)");
                            option_d1.ReplyGood = "A ship might be found, but where to?";
                            OutEventChain outcome_d1 = new OutEventChain(eventObject.EventPID, EventAutoFilter.FindShip);
                            option_d1.SetGoodOutcome(outcome_d1);
                            eventObject.SetOption(option_d1);
                            //Option -> Bribe a Captain
                            OptionInteractive option_d2 = new OptionInteractive("Bribe a Captain to take you (Guaranteed success)");
                            option_d2.ReplyGood = "There is always a ship for those prepared to pay";
                            OutEventChain outcome_d2 = new OutEventChain(eventObject.EventPID, EventAutoFilter.BribeCaptain);
                            option_d2.SetGoodOutcome(outcome_d2);
                            eventObject.SetOption(option_d2);
                            List<Trigger> listDockTriggers = new List<Trigger>();
                            listDockTriggers.Add(new Trigger(TriggerCheck.ResourcePlyr, 0, 2, EventCalc.GreaterThanOrEqual));
                            option_d2.SetTriggers(listDockTriggers);
                            break;
                        case EventAutoFilter.FindShip:
                            //look for a suitable ship -> may find one, may not
                            eventObject.Name = "Look for a suitable ship";
                            eventObject.Text = $"You are at {locName}. Which Port do you wish to travel to?";
                            Dictionary<int, int> dictSeaDistances_0 = loc.GetSeaDistances();
                            //options -> one for each possible port
                            limit = dictSeaDistances_0.Count();
                            limit = Math.Min(12, limit); //max 12 options possible (F1 - F12)
                            if (limit > 0)
                            {
                                //default option
                                OptionInteractive option = new OptionInteractive("You've changed your mind.");
                                option.ReplyGood = string.Format("You leave the raucous docks of {0} behind", Game.world.GetHouseName(refID));
                                OutNone outcome = new OutNone(eventObject.EventPID);
                                option.SetGoodOutcome(outcome);
                                eventObject.SetOption(option);
                            }
                            foreach (var passage in dictSeaDistances_0)
                            {
                                Location locDestination = Game.network.GetLocation(passage.Key);
                                if (locDestination != null)
                                {
                                    //estimate voyage time, min 1 day
                                    voyageTime = passage.Value / speed;
                                    voyageTime = Math.Max(1, voyageTime);
                                    chance = (rnd.Next(1, 10) - 1) * 10;
                                    chance = Math.Max(10, chance);
                                    optionText = string.Format("Obtain passage to {0} {1}, a voyage of {2} day{3}. {4}% chance of success.", locDestination.LocName,
                                        Game.world.ShowLocationCoords(locDestination.LocationID), voyageTime, voyageTime != 1 ? "s" : "", chance);
                                    OptionInteractive option = new OptionInteractive(optionText) { LocID = locDestination.LocationID, Test = chance };
                                    option.ReplyGood = "A suitable ship is available. You board immediately";
                                    option.ReplyBad = $"No ship is available to take you to {locDestination.LocName} today";
                                    OutPassage outSuccess = new OutPassage(eventObject.EventPID, locDestination.LocationID, voyageTime);
                                    string failText = string.Format("You depart the {0} docks after failing to find a suitable ship bound for {1}", locName, locDestination.LocName);
                                    OutNone outFail = new OutNone(eventObject.EventPID, failText, loc.LocationID);
                                    option.SetGoodOutcome(outSuccess);
                                    option.SetBadOutcome(outFail);
                                    eventObject.SetOption(option);
                                }
                                else { Game.SetError(new Error(73, "invalid locDestination (null) -> Passage option not created")); }
                            }
                            //desperado option (must be at least 2 port options available)
                            if (limit > 1)
                            {
                                optionText = "Find the first available ship, who cares where it's going? (Guaranteed but Risky)";
                                List<int> tempList = new List<int>(dictSeaDistances_0.Keys);
                                rndLocID = tempList[rnd.Next(tempList.Count)];
                                voyageDistance = dictSeaDistances_0[rndLocID];
                                voyageTime = voyageDistance / speed;
                                voyageTime = Math.Max(1, voyageTime);
                                Location locRandom = Game.network.GetLocation(rndLocID);
                                if (locRandom != null)
                                {
                                    OptionInteractive optionRandom = new OptionInteractive(optionText) { LocID = rndLocID };
                                    optionRandom.ReplyGood = "A nearby ship is casting off it's mooring lines, about to leave. You jump onboard";
                                    OutPassage outRandom = new OutPassage(eventObject.EventPID, rndLocID, voyageTime, false);
                                    optionRandom.SetGoodOutcome(outRandom);
                                    eventObject.SetOption(optionRandom);
                                }
                                else { Game.SetError(new Error(73, "Invalid locRandom (null)")); }
                            }
                            break;
                        case EventAutoFilter.BribeCaptain:
                            //Bribe a Captain -> Guaranteed
                            eventObject.Name = "Look for a suitable ship";
                            eventObject.Text = $"You are at {locName}. Which Port do you wish to travel to?";
                            Dictionary<int, int> dictSeaDistances_1 = loc.GetSeaDistances();
                            //options -> one for each possible port
                            limit = dictSeaDistances_1.Count();
                            limit = Math.Min(12, limit); //max 12 options possible (F1 - F12)
                            if (limit > 0)
                            {
                                //default option
                                OptionInteractive option = new OptionInteractive("You've changed your mind.");
                                option.ReplyGood = string.Format("You leave the raucous docks of {0} behind", Game.world.GetHouseName(refID));
                                OutNone outcome = new OutNone(eventObject.EventPID);
                                option.SetGoodOutcome(outcome);
                                eventObject.SetOption(option);
                            }
                            int fastSpeed = speed + 1; //bribed passages are faster
                            foreach (var passage in dictSeaDistances_1)
                            {
                                Location locDestination = Game.network.GetLocation(passage.Key);
                                if (locDestination != null)
                                {
                                    //estimate voyage time, min 1 day
                                    voyageTime = passage.Value / fastSpeed;
                                    voyageTime = Math.Max(1, voyageTime);
                                    optionText = string.Format("Buy a fast passage to {0} {1}, a voyage of {2} day{3} (costs a Resource)", locDestination.LocName,
                                        Game.world.ShowLocationCoords(locDestination.LocationID), voyageTime, voyageTime != 1 ? "s" : "");
                                    OptionInteractive option = new OptionInteractive(optionText) { LocID = locDestination.LocationID };
                                    option.ReplyGood = "Money talks. The Captain pockets the gold and bids you come aboard";
                                    OutResource outResource = new OutResource(eventObject.EventPID, true, 1, EventCalc.Subtract);
                                    OutPassage outPassage = new OutPassage(eventObject.EventPID, locDestination.LocationID, voyageTime);
                                    //OutEventChain outcome = new OutEventChain(1000, EventFilter.Interact);
                                    option.SetGoodOutcome(outResource);
                                    option.SetGoodOutcome(outPassage);
                                    eventObject.SetOption(option);
                                }
                                else { Game.SetError(new Error(73, "invalid locDestination (null) -> Passage option not created")); }
                            }
                            //desperado option (must be at least 2 port options available)
                            if (limit > 1)
                            {
                                optionText = "Find the first available ship, who cares where it's going? (Guaranteed but Risky)";
                                List<int> bribeList = new List<int>(dictSeaDistances_1.Keys);
                                rndLocID = bribeList[rnd.Next(bribeList.Count)];
                                voyageDistance = dictSeaDistances_1[rndLocID];
                                voyageTime = voyageDistance / speed;
                                voyageTime = Math.Max(1, voyageTime);
                                Location locRandom = Game.network.GetLocation(rndLocID);
                                if (locRandom != null)
                                {
                                    OptionInteractive optionRandom = new OptionInteractive(optionText) { LocID = rndLocID };
                                    optionRandom.ReplyGood = "A nearby ship is casting off it's mooring lines, about to leave. You jump onboard";
                                    OutPassage outRandom = new OutPassage(eventObject.EventPID, rndLocID, voyageTime, false);
                                    optionRandom.SetGoodOutcome(outRandom);
                                    eventObject.SetOption(optionRandom);
                                }
                                else { Game.SetError(new Error(73, "Invalid locRandom (null)")); }
                            }
                            break;
                        case EventAutoFilter.Interact:
                            //inteact with the selected individual
                            if (actorID > 1 && Game.world.CheckActorPresent(actorID, locID) == true)
                            {
                                Actor person = Game.world.GetAnyActor(actorID);
                                if (person != null)
                                {
                                    /*if (person is Advisor)
                                    {
                                        Advisor advisor = person as Advisor;
                                        actorText = $"{advisor.Title} {advisor.Name}";
                                        //if (advisor.advisorRoyal > AdvisorRoyal.None) { actorText = string.Format("{0} {1}", advisor.advisorRoyal, advisor.Name); }
                                        //else { actorText = string.Format("{0} {1}", advisor.advisorNoble, advisor.Name); }
                                    }
                                    else if (person.Office > ActorOffice.None)
                                    { actorText = string.Format("{0} {1}", person.Office, person.Name); }
                                    else { actorText = string.Format("{0} {1}", person.Type, person.Name); }*/

                                    if (person is Advisor) { actorText = $"{Game.world.GetAdvisorType((Advisor)person)} {person.Name}"; }
                                    else { actorText = string.Format("{0} {1}", person.Type, person.Name); }

                                    actorText = $"{person.Title} {person.Name}";
                                    eventObject.Name = "Interact";
                                    eventObject.Text = string.Format("How would you like to interact with {0}?", actorText);
                                    tempText = string.Format("You are granted an audience with {0} {1} \"{2}\", ActID {3}, at {4}", person.Title, person.Name, person.Handle, person.ActID, loc.LocName);
                                    //default -> flip back to court or advisor options
                                    OptionInteractive option_0 = new OptionInteractive("Excuse Yourself") { ActorID = actorID };
                                    option_0.ReplyGood = $"{actorText} stares at you with narrowed eyes";
                                    if (person is Advisor) { OutEventChain outcome_0 = new OutEventChain(1000, EventAutoFilter.Advisors); option_0.SetGoodOutcome(outcome_0); }
                                    else { OutEventChain outcome_0 = new OutEventChain(1000, EventAutoFilter.Court); option_0.SetGoodOutcome(outcome_0); }
                                    eventObject.SetOption(option_0);
                                    //improve relationship (befriend)
                                    OptionInteractive option_1 = new OptionInteractive("Befriend") { ActorID = actorID };
                                    option_1.ReplyGood = string.Format("{0} looks at you expectantly", actorText);
                                    List<Trigger> listTriggers_1 = new List<Trigger>();
                                    listTriggers_1.Add(new Trigger(TriggerCheck.RelPlyr, person.GetRelPlyr(), Game.constant.GetValue(Global.IMPROVE_THRESHOLD), EventCalc.GreaterThanOrEqual));
                                    option_1.SetTriggers(listTriggers_1);
                                    OutConflict outcome_1 = new OutConflict(eventObject.EventPID, actorID, ConflictType.Social) { Social_Type = ConflictSocial.Befriend, SubType = ConflictSubType.Befriend };
                                    option_1.SetGoodOutcome(outcome_1);
                                    eventObject.SetOption(option_1);
                                    person.SetAllSkillsKnownStatus(true);
                                    house.SetFriendsAndEnemies(true);
                                    //blackmail
                                    OptionInteractive option_2 = new OptionInteractive("Blackmail") { ActorID = actorID };
                                    option_2.ReplyGood = string.Format("{0} frowns, their expression darkens", actorText);
                                    List<Trigger> listTriggers_2 = new List<Trigger>();
                                    listTriggers_2.Add(new Trigger(TriggerCheck.RelPlyr, person.GetRelPlyr(), Game.constant.GetValue(Global.BLACKMAIL_THRESHOLD), EventCalc.GreaterThanOrEqual));
                                    option_2.SetTriggers(listTriggers_2);
                                    OutConflict outcome_2 = new OutConflict(eventObject.EventPID, actorID, ConflictType.Social) { Social_Type = ConflictSocial.Blackmail, SubType = ConflictSubType.Blackmail };
                                    option_2.SetGoodOutcome(outcome_2);
                                    eventObject.SetOption(option_2);
                                    person.SetAllSkillsKnownStatus(true);
                                    house.SetFriendsAndEnemies(true);
                                    //seduce
                                    OptionInteractive option_3 = new OptionInteractive("Seduce") { ActorID = actorID };
                                    option_3.ReplyGood = string.Format("{0} flutters their eyelids at you", actorText);
                                    List<Trigger> listTriggers_3 = new List<Trigger>();
                                    listTriggers_3.Add(new Trigger(TriggerCheck.RelPlyr, person.GetRelPlyr(), Game.constant.GetValue(Global.SEDUCE_THRESHOLD), EventCalc.GreaterThanOrEqual));
                                    listTriggers_3.Add(new Trigger(TriggerCheck.Sex, 0, (int)person.Sex, EventCalc.NotEqual)); //must be opposite sex
                                    option_3.SetTriggers(listTriggers_3);
                                    OutConflict outcome_3 = new OutConflict(eventObject.EventPID, actorID, ConflictType.Social) { Social_Type = ConflictSocial.Seduce, SubType = ConflictSubType.Seduce };
                                    option_3.SetGoodOutcome(outcome_3);
                                    eventObject.SetOption(option_3);
                                    person.SetAllSkillsKnownStatus(true);
                                    house.SetFriendsAndEnemies(true);
                                    //You want Something from them
                                    OptionInteractive option_5 = new OptionInteractive("You want something") { ActorID = actorID };
                                    option_5.ReplyGood = $"{actorText} sits back and cautiously agrees to discuss your needs";
                                    OutEventChain outcome_5 = new OutEventChain(eventObject.EventPID, EventAutoFilter.YouWant);
                                    option_5.SetGoodOutcome(outcome_5);
                                    List<Trigger> listTriggers_5 = new List<Trigger>();
                                    listTriggers_5.Add(new Trigger(TriggerCheck.RelPlyr, person.GetRelPlyr(), talkRel, EventCalc.GreaterThanOrEqual));
                                    option_5.SetTriggers(listTriggers_5);
                                    eventObject.SetOption(option_5);
                                    person.SetAllSkillsKnownStatus(true);
                                    //Desire (NPC wants something from you)
                                    OptionInteractive option_6 = new OptionInteractive("They want something") { ActorID = actorID };
                                    option_6.ReplyGood = string.Format("{0} leans forward enthusiastically to discuss {1} needs with you", actorText, person.Sex == ActorSex.Male ? "his" : "her");
                                    List<Trigger> listTriggers_6 = new List<Trigger>();
                                    listTriggers_6.Add(new Trigger(TriggerCheck.Desire, 0, 0, EventCalc.None));
                                    listTriggers_6.Add(new Trigger(TriggerCheck.Promise, 0, 0, EventCalc.None));
                                    option_6.SetTriggers(listTriggers_6);
                                    OutEventChain outcome_6 = new OutEventChain(eventObject.EventPID, EventAutoFilter.TheyWant);
                                    //OutNone outcome_6 = new OutNone(eventObject.EventPID);
                                    option_6.SetGoodOutcome(outcome_6);
                                    eventObject.SetOption(option_6);
                                    person.SetAllSkillsKnownStatus(true);
                                }
                                else { Game.SetError(new Error(73, "Invalid actorID from AutoCreateEvent (null from dict)")); }
                            }
                            break;
                        case EventAutoFilter.TheyWant:
                            //Character has a desire that you can meet in return for a relationship boost
                            Actor personWant = Game.world.GetAnyActor(actorID);
                            if (personWant != null)
                            {
                                /*if (personWant is Advisor)
                                {
                                    Advisor advisor = personWant as Advisor;
                                    if (advisor.advisorRoyal > AdvisorRoyal.None) { actorText = string.Format("{0} {1}", advisor.advisorRoyal, advisor.Name); }
                                    else { actorText = string.Format("{0} {1}", advisor.advisorNoble, advisor.Name); }
                                }
                                else if (personWant.Office > ActorOffice.None)
                                { actorText = string.Format("{0} {1}", personWant.Office, personWant.Name); }
                                else { actorText = string.Format("{0} {1}", personWant.Type, personWant.Name); }*/
                                if (personWant is Advisor) { actorText = $"{Game.world.GetAdvisorType((Advisor)personWant)} {personWant.Name}"; }
                                else { actorText = string.Format("{0} {1}", personWant.Type, personWant.Name); }
                                if (personWant is Passive)
                                {
                                    //You've spoken to the character so you know their desire (if you didn't previously)
                                    Passive tempPassive = personWant as Passive;
                                    tempPassive.SetDesireKnown(true);
                                    //set up promises
                                    int strength; // strength of promise, 1 to 5
                                    int baseValue = Game.constant.GetValue(Global.PROMISES_BASE);
                                    //if too many promises have been handed out, effect is halved
                                    int numPromises = Game.variable.GetValue(GameVar.Promises_Num);
                                    int numHalved = Game.constant.GetValue(Global.PROMISES_HALVED);
                                    if (numPromises >= numHalved)
                                    { baseValue /= 2; Game.logTurn?.Write($"[Promises] {numPromises} handed out is >= {numHalved}, relationship baseValue halved to {baseValue}"); }
                                    Passive passive = personWant as Passive;
                                    eventObject.Name = "They Want Something";
                                    eventObject.Text = $"{passive.Title} {passive.Name}, ActID {passive.ActID} has a desire for {passive.DesireText}.";
                                    tempText = string.Format("You sit down and discuss what you can do for {0} {1} \"{2}\", ActID {3}, at {4}", passive.Title, passive.Name,
                                        passive.Handle, passive.ActID, loc.LocName);
                                    //default
                                    OptionInteractive option_w0 = new OptionInteractive("Sorry, you can't help") { ActorID = actorID };
                                    option_w0.ReplyGood = $"{actorText} shrugs their shoulders";
                                    OutEventChain outcome_w0 = new OutEventChain(eventObject.EventPID, EventAutoFilter.Interact);
                                    option_w0.SetGoodOutcome(outcome_w0);
                                    eventObject.SetOption(option_w0);
                                    //Give it some thought
                                    OptionInteractive option_w1 = new OptionInteractive("You promise to give it some thought") { ActorID = actorID };
                                    option_w1.ReplyGood = $"{actorText} nods in agreement";
                                    strength = 1;
                                    OutPromise outcome_w1_0 = new OutPromise(eventObject.EventPID, passive.Desire, strength);
                                    OutRelPlyr outcome_w1_1 = new OutRelPlyr(eventObject.EventPID, baseValue * strength, EventCalc.Add, $"Ursurper Promises to think about {passive.DesireText}", "Promise");
                                    OutFavour outcome_w1_2 = new OutFavour(eventObject.EventPID, strength);
                                    option_w1.SetGoodOutcome(outcome_w1_0);
                                    option_w1.SetGoodOutcome(outcome_w1_1);
                                    option_w1.SetGoodOutcome(outcome_w1_2);
                                    eventObject.SetOption(option_w1);
                                    //Promise to Take Care of it
                                    OptionInteractive option_w2 = new OptionInteractive("You promise to take care of it") { ActorID = actorID };
                                    option_w2.ReplyGood = $"{actorText} nods in agreement";
                                    strength = 3;
                                    OutPromise outcome_w2_0 = new OutPromise(eventObject.EventPID, passive.Desire, strength);
                                    OutRelPlyr outcome_w2_1 = new OutRelPlyr(eventObject.EventPID, baseValue * strength, EventCalc.Add, $"Ursurper Promises to take care off {passive.DesireText}", "Promise");
                                    OutFavour outcome_w2_2 = new OutFavour(eventObject.EventPID, strength);
                                    option_w2.SetGoodOutcome(outcome_w2_0);
                                    option_w2.SetGoodOutcome(outcome_w2_1);
                                    option_w2.SetGoodOutcome(outcome_w2_2);
                                    eventObject.SetOption(option_w2);
                                    //Swear on your Father's Grave
                                    OptionInteractive option_w3 = new OptionInteractive("You swear on your father's grave that you'll fix it") { ActorID = actorID };
                                    option_w3.ReplyGood = $"{actorText} nods in agreement";
                                    strength = 5;
                                    OutPromise outcome_w3_0 = new OutPromise(eventObject.EventPID, passive.Desire, strength);
                                    OutRelPlyr outcome_w3_1 = new OutRelPlyr(eventObject.EventPID, baseValue * strength, EventCalc.Add,
                                        $"Ursurper Swears on their Father's grave to deal with {passive.DesireText}", "Promise");
                                    OutFavour outcome_w3_2 = new OutFavour(eventObject.EventPID, strength);
                                    option_w3.SetGoodOutcome(outcome_w3_0);
                                    option_w3.SetGoodOutcome(outcome_w3_1);
                                    option_w3.SetGoodOutcome(outcome_w3_2);
                                    eventObject.SetOption(option_w3);
                                }
                            }
                            break;
                        case EventAutoFilter.YouWant:
                            //You want something from the NPC character
                            Actor personNeed = Game.world.GetAnyActor(actorID);
                            if (personNeed != null)
                            {
                                /*if (personNeed is Advisor)
                                {
                                    Advisor advisor = personNeed as Advisor;
                                    if (advisor.advisorRoyal > AdvisorRoyal.None) { actorText = string.Format("{0} {1}", advisor.advisorRoyal, advisor.Name); }
                                    else { actorText = string.Format("{0} {1}", advisor.advisorNoble, advisor.Name); }
                                }
                                else if (personNeed.Office > ActorOffice.None)
                                {
                                    actorText = string.Format("{0} {1}", personNeed.Office, personNeed.Name);
                                }
                                else { actorText = string.Format("{0} {1}", personNeed.Type, personNeed.Name); }*/
                                if (personNeed is Advisor) { actorText = $"{Game.world.GetAdvisorType((Advisor)personNeed)} {personNeed.Name}"; }
                                else { actorText = string.Format("{0} {1}", personNeed.Type, personNeed.Name); }
                                if (personNeed is Passive)
                                {
                                    eventObject.Name = "You Want Something";
                                    eventObject.Text = string.Format("How would you like to interact with {0}?", actorText);
                                    tempText = string.Format("You sit down and discuss your needs with {0} {1} \"{2}\", ActID {3}, at {4}", personNeed.Title, personNeed.Name,
                                        personNeed.Handle, personNeed.ActID, loc.LocName);
                                    //default -> flip back to advisor options
                                    OptionInteractive option_n0 = new OptionInteractive("Excuse Yourself") { ActorID = actorID };
                                    option_n0.ReplyGood = $"{actorText} stares at you with narrowed eyes";
                                    OutEventChain outcome_n0 = new OutEventChain(1000, EventAutoFilter.Advisors);
                                    option_n0.SetGoodOutcome(outcome_n0);
                                    eventObject.SetOption(option_n0);

                                    //Cash in a Favour
                                    OptionInteractive option_n1 = new OptionInteractive("Cash in a Favour") { ActorID = actorID };
                                    option_n1.ReplyGood = $"{actorText} squirms in their seat. It doesn't appear comfortable";
                                    OutNone outcome_n1 = new OutNone(eventObject.EventPID);
                                    option_n1.SetGoodOutcome(outcome_n1);
                                    eventObject.SetOption(option_n1);

                                    //Hand over a disguise (Player can't already have a disguise in his inventory)
                                    if (personNeed is Advisor)
                                    {
                                        Advisor advisor = personNeed as Advisor;
                                        if (advisor.CheckDisguises() == true)
                                        {
                                            OptionInteractive option_n2 = new OptionInteractive("Ask for a Disguise") { ActorID = actorID };
                                            option_n2.ReplyGood = $"{actorText} nods solmenly and reaches for a nearby sack";
                                            OutDisguise outcome_n2 = new OutDisguise(eventObject.EventPID);
                                            option_n2.SetGoodOutcome(outcome_n2);
                                            List<Trigger> listTriggers_n2 = new List<Trigger>();
                                            listTriggers_n2.Add(new Trigger(TriggerCheck.Disguise, 0, 0, EventCalc.None));
                                            option_n2.SetTriggers(listTriggers_n2);
                                            eventObject.SetOption(option_n2);
                                        }
                                    }

                                    //Become an informant
                                    OptionInteractive option_n3 = new OptionInteractive("Ask to keep me Informed") { ActorID = actorID };
                                    option_n3.ReplyGood = $"{actorText} agrees to keep their ear to the ground and lete you know if anything interesting occurs";
                                    OutNone outcome_n3 = new OutNone(eventObject.EventPID);
                                    option_n3.SetGoodOutcome(outcome_n3);
                                    eventObject.SetOption(option_n3);
                                    //swear allegiance
                                    OptionInteractive option_4 = new OptionInteractive("Ask for their Allegiance") { ActorID = actorID };
                                    option_4.ReplyGood = string.Format("{0} kneels at your feet", actorText);
                                    List<Trigger> listTriggers_4 = new List<Trigger>();
                                    listTriggers_4.Add(new Trigger(TriggerCheck.RelPlyr, personNeed.GetRelPlyr(), Game.constant.GetValue(Global.ALLEGIANCE_THRESHOLD), EventCalc.GreaterThanOrEqual));
                                    listTriggers_4.Add(new Trigger(TriggerCheck.ActorType, (int)personNeed.Type, (int)ActorType.Lord, EventCalc.Equals)); //must be a Lord
                                    option_4.SetTriggers(listTriggers_4);
                                    OutNone outcome_4 = new OutNone(eventObject.EventPID);
                                    option_4.SetGoodOutcome(outcome_4);
                                    eventObject.SetOption(option_4);
                                }
                            }
                            break;
                        default:
                            Game.SetError(new Error(118, string.Format("Invalid EventFilter (\"{0}\")", filter)));
                            break;
                    }
                    //Create & Add Event Package
                    EventPackage package = new EventPackage() { Person = player, EventObject = eventObject, Done = false };
                    listPlyrCurrentEvents.Add(package);
                    //if more than the current event present the original one (autocreated) needs to be deleted
                    if (listPlyrCurrentEvents.Count > 1) { listPlyrCurrentEvents.RemoveAt(0); }

                    //add to Player dictionary (ResolveOutcome looks for it there) -> check not an instance present already
                    if (dictPlayerEvents.ContainsKey(1000)) { dictPlayerEvents.Remove(1000); }
                    dictPlayerEvents.Add(1000, eventObject);
                    //message
                    //tempText = string.Format("{0}, Aid {1} at {2} {3}, [{4} Event] \"{5}\"", player.Name, player.ActID, locName, Game.world.ShowLocationCoords(player.LocID),
                    //eventObject.Type, eventObject.Name);
                    if (tempText.Length > 0)
                    {
                        Game.world.SetMessage(new Message(tempText, MessageType.Event));
                        Game.world.SetPlayerRecord(new Record(tempText, player.ActID, player.LocID, CurrentActorIncident.Event));
                    }
                }
                else { Game.SetError(new Error(118, "Invalid List of Actors (Zero present at Location")); }
            }
            else { Game.SetError(new Error(118, "Invalid Player (returns Null)")); }
        }

        /// <summary>
        /// clean up events
        /// </summary>
        public void HousekeepEvents()
        {
            //remove any dormant AutoReact Events from Player event Lists and the master dictionary. Only do so if the are above a certain # (avoid processing overhead)
            if (NumAutoReactEvents > 10)
            {
                List<int> tempList = new List<int>(); //temp list to hold eventPID of deleted events
                //check dictionary first
                Game.logTurn?.Write("--- HouseKeepEvents (Director.cs)");
                int counter = 0;
                foreach (var eventObject in dictPlayerEvents)
                {
                    //looking for Dormant, AutoReact events 
                    if (eventObject.Value.Status == EventStatus.Dormant && eventObject.Value.EventPID >= 2000)
                    {
                        tempList.Add(eventObject.Value.EventPID);
                        counter++;
                        Game.logTurn?.Write(string.Format(" \"{0}\" autoReact Event found, Status {1}", eventObject.Value.Name, eventObject.Value.Status));
                    }

                }
                //any to remove?
                if (counter > 0)
                {
                    foreach (int eventID in tempList)
                    {
                        //remove from dictionary
                        if (dictPlayerEvents.Remove(eventID) == true)
                        {
                            Game.logTurn?.Write(string.Format(" \"eventID {0}\" Player Event has been removed from the dictPlayerEvents", eventID));
                            NumAutoReactEvents--;
                        }
                    }
                    //remove from lists (asumes that each individual eventID will be present in only one list)
                    for (int i = 0; i < tempList.Count; i++)
                    {
                        if (listGenPlyrEventsCapital.Remove(tempList[i]) == true) { Game.logTurn?.Write(string.Format(" EventPID {0} removed from listGenPlyrEventsCapital", i)); continue; }
                        if (listGenPlyrEventsMajor.Remove(tempList[i]) == true) { Game.logTurn?.Write(string.Format(" EventPID {0} removed from listGenPlyrEventsMajor", i)); continue; }
                        if (listGenPlyrEventsMinor.Remove(tempList[i]) == true) { Game.logTurn?.Write(string.Format(" EventPID {0} removed from listGenPlyrEventsMinor", i)); continue; }
                        if (listGenPlyrEventsInn.Remove(tempList[i]) == true) { Game.logTurn?.Write(string.Format(" EventPID {0} removed from listGenPlyrEventsInn", i)); continue; }
                        if (listGenPlyrEventsForest.Remove(tempList[i]) == true) { Game.logTurn?.Write(string.Format(" EventPID {0} removed from listGenPlyrEventsForest", i)); continue; }
                        if (listGenPlyrEventsMountain.Remove(tempList[i]) == true) { Game.logTurn?.Write(string.Format(" EventPID {0} removed from listGenPlyrEventsMountain", i)); continue; }
                        if (listGenPlyrEventsSea.Remove(tempList[i]) == true) { Game.logTurn?.Write(string.Format(" EventPID {0} removed from listGenPlyrEventsSea", i)); continue; }
                        if (listGenPlyrEventsNormal.Remove(tempList[i]) == true) { Game.logTurn?.Write(string.Format(" EventPID {0} removed from listGenPlyrEventsNormal", i)); continue; }
                        if (listGenPlyrEventsKing.Remove(tempList[i]) == true) { Game.logTurn?.Write(string.Format(" EventPID {0} removed from listGenPlyrEventsKing", i)); continue; }
                        if (listGenPlyrEventsConnector.Remove(tempList[i]) == true) { Game.logTurn?.Write(string.Format(" EventPID {0} removed from listGenPlyrEventsConnector", i)); continue; }
                        //any hit above would have skipped this code
                        Game.SetError(new Error(126, string.Format("Warning! EventPID {0} wasn't found in any list (HousekeepEvents, tidy up AutoReact events", i)));
                    }
                    NumAutoReactEvents = Math.Max(0, NumAutoReactEvents);
                }
            }
            //Remove any existing autoLoc created player events prior to next turn (Process end of turn)
            if (dictPlayerEvents.ContainsKey(1000)) { dictPlayerEvents.Remove(1000); }
            //clear out current events
            listFollCurrentEvents.Clear();
            listPlyrCurrentEvents.Clear();
        }

        /// <summary>
        /// Extracts all valid Follower events from a list of EventID's
        /// </summary>
        /// <param name="listEventID"></param>
        /// <returns></returns>
        private List<Event> GetValidFollowerEvents(List<int> listEventID)
        {
            int frequency;
            List<Event> listEvents = new List<Event>();
            if (listEventID != null)
            {
                foreach (int eventID in listEventID)
                {
                    Event eventObject = dictFollowerEvents[eventID];
                    if (eventObject != null && eventObject.Status == EventStatus.Active)
                    {
                        frequency = (int)eventObject.Frequency;
                        //add # of events to pool equal to (int)EventFrequency
                        for (int i = 0; i < frequency; i++)
                        { listEvents.Add(eventObject); }
                    }
                }
            }
            return listEvents;
        }

        /// <summary>
        /// Extracts all valid Player events from a list of EventID's
        /// </summary>
        /// <param name="listEventID"></param>
        /// <param name="data">optional paramater that is multipurpose, eg. houseID for a location</param>
        /// <returns></returns>
        private List<Event> GetValidPlayerEvents(List<int> listEventID, int data = 0)
        {
            int frequency;
            List<Event> listEvents = new List<Event>();
            if (listEventID != null)
            {
                Game.logTurn?.Write("--- GetValidPlayerEvents (Director.cs)");
                foreach (int eventID in listEventID)
                {
                    Event eventObject = dictPlayerEvents[eventID];
                    if (eventObject != null && eventObject.Status == EventStatus.Active && eventObject.TimerCoolDown == 0)
                    {
                        bool proceed = true;
                        //is the event limited in any way?
                        if (eventObject.SubRef > 0)
                        {
                            //if data > 0 then SubRef must match the HouseID or GeoID in order for event to qualify
                            if (data > 0)
                            {
                                if (eventObject.LocType > ArcLoc.None)
                                {
                                    if (data != eventObject.SubRef) { proceed = false; Game.logTurn?.Write(string.Format("Event \"{0}\" failed HouseID check", eventObject.Name)); }
                                    else { Game.logTurn?.Write(string.Format(" Event \"{0}\" PASSED HouseID check", eventObject.Name)); }
                                }
                                else if (eventObject.GeoType > ArcGeo.None)
                                {
                                    if (data != eventObject.SubRef) { proceed = false; Game.logTurn?.Write(string.Format("Event \"{0}\" failed GeoID check", eventObject.Name)); }
                                    else { Game.logTurn?.Write(string.Format(" Event \"{0}\" PASSED HouseID check", eventObject.Name)); }
                                }
                            }
                        }
                        else { Game.logTurn?.Write(string.Format(" Event \"{0}\" has no attached Conditions", eventObject.Name)); }
                        if (proceed == true)
                        {
                            frequency = (int)eventObject.Frequency;
                            //add # of events to pool equal to (int)EventFrequency
                            for (int i = 0; i < frequency; i++)
                            { listEvents.Add(eventObject); }
                        }
                    }
                }
            }
            return listEvents;
        }

        /// <summary>
        /// returns an Event from follower dict, null if not found
        /// </summary>
        /// <param name="eventID"></param>
        /// <returns></returns>
        internal EventFollower GetFollowerEvent(int eventID)
        {
            EventFollower eventObject = null;
            if (dictFollowerEvents.TryGetValue(eventID, out eventObject))
            { return eventObject; }
            return eventObject;
        }

        /// <summary>
        /// returns an Event from Player dict, null if not found
        /// </summary>
        /// <param name="eventID"></param>
        /// <returns></returns>
        internal EventPlayer GetPlayerEvent(int eventID)
        {
            EventPlayer eventObject = null;
            if (dictPlayerEvents.TryGetValue(eventID, out eventObject))
            { return eventObject; }
            return eventObject;
        }

        /// <summary>
        /// returns an Auto Event archetype from Auto Event dict, null if not found
        /// </summary>
        /// <param name="eventID"></param>
        /// <returns></returns>
        internal EventPlayer GetAutoEvent(int eventID)
        {
            EventPlayer eventObject = null;
            if (dictAutoEvents.TryGetValue(eventID, out eventObject))
            { return eventObject; }
            return eventObject;
        }


        /// <summary>
        /// Resolve current Follower events one at a time. Returns true if event present to be processed, false otherwise.
        /// </summary>
        public bool ResolveFollowerEvents()
        {
            bool returnValue = false;
            int ability, rndNum, success;
            int crowCounter = 0;
            int traitMultiplier = Game.constant.GetValue(Global.TRAIT_MULTIPLIER);
            string effectText, status;
            List<Snippet> eventList = new List<Snippet>();
            RLColor foreColor = RLColor.Black;
            RLColor backColor = Color._background1;
            RLColor traitColor;
            Active player = Game.world.GetActiveActor(1);
            //loop all triggered events for this turn
            for (int i = 0; i < listFollCurrentEvents.Count; i++)
            {
                EventPackage package = listFollCurrentEvents[i];
                if (package.Done == false)
                {
                    Game.logTurn?.Write("--- ResolveFollowerEvents (Director.cs)");
                    EventFollower eventObject = (EventFollower)package.EventObject;
                    List<OptionAuto> listOptions = eventObject.GetOptions();
                    //assume only a single option
                    OptionAuto option = null;
                    if (listOptions != null) { option = listOptions[0]; }
                    else { Game.SetError(new Error(70, "Invalid ListOfOptions input (null)")); break; }
                    Active actor = package.Person;
                    //create event description
                    Position pos = actor.GetActorPosition();
                    switch (eventObject.Type)
                    {
                        case ArcType.GeoCluster:
                        case ArcType.Road:
                            eventList.Add(new Snippet(string.Format("{0}, Aid {1}, at Loc {2}:{3} travelling towards {4}", actor.Name, actor.ActID, pos.PosX, pos.PosY,
                                Game.world.GetLocationName(actor.LocID)), RLColor.LightGray, backColor));
                            break;
                        case ArcType.Location:
                            eventList.Add(new Snippet(string.Format("{0}, Aid {1}. at {2} (Loc {3}:{4})", actor.Name, actor.ActID, Game.world.GetLocationName(actor.LocID),
                                pos.PosX, pos.PosY), RLColor.LightGray, backColor));
                            break;
                        case ArcType.Actor:
                            if (actor.Status == ActorStatus.AtLocation) { status = Game.world.GetLocationName(actor.LocID) + " "; }
                            else { status = null; }
                            eventList.Add(new Snippet(string.Format("{0}, Aid {1}. at {2}(Loc {3}:{4})", actor.Name, actor.ActID, status,
                                pos.PosX, pos.PosY), RLColor.LightGray, backColor));
                            break;
                    }
                    eventList.Add(new Snippet(""));
                    eventList.Add(new Snippet("- o -", RLColor.Gray, backColor));
                    eventList.Add(new Snippet(""));
                    eventList.Add(new Snippet(eventObject.Text, foreColor, backColor));
                    eventList.Add(new Snippet(""));
                    eventList.Add(new Snippet("- o -", RLColor.Gray, backColor));
                    eventList.Add(new Snippet(""));
                    //resolve event and add to description (add delay to actor if needed)
                    eventList.Add(new Snippet(string.Format("A test of {0}", option.Trait), RLColor.Brown, backColor));
                    eventList.Add(new Snippet(""));
                    effectText = actor.GetTraitEffectText(option.Trait);
                    ability = actor.GetSkill(option.Trait);
                    rndNum = rnd.Next(100);
                    success = ability * traitMultiplier;
                    //trait stars
                    if (ability < 3) { traitColor = RLColor.LightRed; }
                    else if (ability == 3) { traitColor = RLColor.Gray; }
                    else { traitColor = RLColor.Green; }
                    //enables stars to be centred
                    if (ability != 3)
                    {
                        eventList.Add(new Snippet(string.Format("({0} {1})  {2} {3} {4}", ability, ability == 1 ? "Star" : "Stars",
                          Game.world.GetStars(ability), actor.arrayOfTraitNames[(int)option.Trait],
                          effectText), traitColor, backColor));
                    }
                    else
                    { eventList.Add(new Snippet(string.Format("({0} {1})  {2}", ability, ability == 1 ? "Star" : "Stars", Game.world.GetStars(ability)), traitColor, backColor)); }
                    eventList.Add(new Snippet(""));
                    eventList.Add(new Snippet(string.Format("Success on {0}% or less", success), RLColor.Brown, backColor));
                    eventList.Add(new Snippet(""));
                    if (rndNum < success)
                    {
                        //success
                        eventList.Add(new Snippet(string.Format("Roll {0}", rndNum), RLColor.Gray, backColor));
                        eventList.Add(new Snippet(""));
                        eventList.Add(new Snippet("- o -", RLColor.Gray, backColor));
                        eventList.Add(new Snippet(""));
                        eventList.Add(new Snippet(string.Format("{0} {1}", actor.Name, option.ReplyGood), RLColor.Black, backColor));
                        //outcomes
                        List<Outcome> listGoodOutcomes = option.GetGoodOutcomes();
                        //ignore if none present
                        if (listGoodOutcomes != null && listGoodOutcomes.Count > 0)
                        {
                            //Loop outcomes (can be multiple)
                            for (int k = 0; k < listGoodOutcomes.Count; k++)
                            {
                                Outcome outTemp = listGoodOutcomes[k];
                                //Type of Outcome
                                if (outTemp is OutDelay)
                                {
                                    /*
                                    SAMPLE PLACEHOLDER CODE
                                    */
                                }
                                else
                                {
                                    //fault condition
                                    Game.SetError(new Error(70, "Invalid Good Outcome Type (not covered by code)"));
                                    eventList.Add(new Snippet(""));
                                    eventList.Add(new Snippet("NO VALID GOOD OUTCOME PRESENT", RLColor.LightRed, backColor));
                                }
                            }
                        }
                    }
                    else
                    {
                        //failure
                        eventList.Add(new Snippet(string.Format("Roll {0}", rndNum), RLColor.LightRed, backColor));
                        eventList.Add(new Snippet(""));
                        eventList.Add(new Snippet("- o -", RLColor.Gray, backColor));
                        eventList.Add(new Snippet(""));
                        eventList.Add(new Snippet(string.Format("{0} {1}", actor.Name, option.ReplyBad), RLColor.Black, backColor));
                        //outcomes
                        List<Outcome> listBadOutcomes = option.GetBadOutcomes();
                        if (listBadOutcomes != null && listBadOutcomes.Count > 0)
                        {
                            //Loop outcomes (can be multiple)
                            for (int k = 0; k < listBadOutcomes.Count; k++)
                            {
                                Outcome outTemp = listBadOutcomes[k];
                                //Type of Outcome
                                if (outTemp is OutDelay)
                                {
                                    //Delay
                                    OutDelay outcome = outTemp as OutDelay;
                                    outcome.Resolve(actor.ActID);
                                    eventList.Add(new Snippet(""));
                                    eventList.Add(new Snippet(string.Format("{0} has been {1} for {2} {3}", actor.Name, eventObject.Type == ArcType.Location ? "indisposed" : "delayed",
                                        outcome.Delay, outcome.Delay == 1 ? "Day" : "Day's"), RLColor.LightRed, backColor));
                                    eventList.Add(new Snippet(""));
                                }
                                else
                                {
                                    //fault condition
                                    Game.SetError(new Error(70, "Invalid Bad Outcome Type (not covered by code)"));
                                    eventList.Add(new Snippet(""));
                                    eventList.Add(new Snippet("NO VALID BAD OUTCOME PRESENT", RLColor.LightRed, backColor));
                                }
                            }
                        }
                        else
                        {
                            //no bad outcomes present
                            Game.SetError(new Error(70, "Invalid ListOfBadOutcomes input (null)"));
                            eventList.Add(new Snippet(""));
                            eventList.Add(new Snippet("NO VALID BAD OUTCOME PRESENT", RLColor.LightRed, backColor));
                        }
                    }
                    eventList.Add(new Snippet(""));
                    eventList.Add(new Snippet("Press ENTER or ESC to continue", RLColor.LightGray, backColor));

                    //only show follower events if Player is at a location and able to receive crows
                    if (player.Status != ActorStatus.AtLocation)
                    { crowCounter++; }
                    else { Game.infoChannel.SetInfoList(eventList, ConsoleDisplay.Event); }
                    returnValue = true;
                    package.Done = true;
                    break;
                }
            }
            if (crowCounter > 0)
            {
                eventList.Clear();
                eventList.Add(new Snippet(""));
                eventList.Add(new Snippet("An incoming Crow failed to arrive", RLColor.LightRed, backColor));
                eventList.Add(new Snippet(""));
                eventList.Add(new Snippet(string.Format("You have missed a total of {0} crow{1} today", crowCounter, crowCounter != 1 ? "s" : ""), RLColor.Black, backColor));
                eventList.Add(new Snippet(""));
                eventList.Add(new Snippet("- o -", RLColor.Gray, backColor));
                eventList.Add(new Snippet(""));
                eventList.Add(new Snippet("You can only receive crows while at a Location", RLColor.Black, backColor));
                eventList.Add(new Snippet(""));
                Game.infoChannel.SetInfoList(eventList, ConsoleDisplay.Event);
            }
            return returnValue;
        }

        /// <summary>
        /// returns true if there are any Follower Events still to be resolved this turn
        /// </summary>
        /// <returns></returns>
        public bool CheckRemainingFollowerEvents()
        {
            for (int i = 0; i < listFollCurrentEvents.Count; i++)
            {
                EventPackage package = listFollCurrentEvents[i];
                if (package.Done == false)
                { return true; }
            }
            return false;
        }

        /// <summary>
        /// returns true if there are any Player Events still to be resolved this turn
        /// </summary>
        /// <returns></returns>
        public bool CheckRemainingPlayerEvents()
        {
            for (int i = 0; i < listPlyrCurrentEvents.Count; i++)
            {
                EventPackage package = listPlyrCurrentEvents[i];
                if (package.Done == false)
                { return true; }
            }
            return false;
        }

        /// <summary>
        /// Resolve current Player events one at a time. Returns true if event present to be processed, false otherwise.
        /// </summary>
        public bool ResolvePlayerEvents()
        {
            bool returnValue = false;
            string status;
            List<Snippet> eventList = new List<Snippet>();
            RLColor foreColor = RLColor.Black;
            RLColor backColor = Color._background1;
            //loop all triggered events for this turn
            for (int i = 0; i < listPlyrCurrentEvents.Count; i++)
            {
                EventPackage package = listPlyrCurrentEvents[i];
                if (package.Done == false)
                {
                    Game.logTurn?.Write("--- ResolvePlayerEvents (Director.cs)");
                    EventPlayer eventObject = (EventPlayer)package.EventObject;
                    Active actor = package.Person;
                    Game._eventID = eventObject.EventPID;
                    string locName = Game.world.GetLocationName(actor.LocID);
                    //create event description
                    Position pos = actor.GetActorPosition();
                    switch (eventObject.Type)
                    {
                        case ArcType.GeoCluster:
                            if (eventObject.GeoType == ArcGeo.Sea || eventObject.GeoType == ArcGeo.Unsafe)
                            { eventList.Add(new Snippet(string.Format("{0}, Aid {1}, at Sea, bound for {2} (Loc {3}:{4})", actor.Name, actor.ActID, locName, pos.PosX, pos.PosY),
                                    RLColor.LightGray, backColor)); }
                            else
                            { eventList.Add(new Snippet(string.Format("{0}, Aid {1}, at Loc {2}:{3} travelling towards {4}", actor.Name, actor.ActID, pos.PosX, pos.PosY,
                                locName), RLColor.LightGray, backColor)); }
                            break;
                        case ArcType.Road:
                            eventList.Add(new Snippet(string.Format("{0}, Aid {1}, at Loc {2}:{3} travelling towards {4}", actor.Name, actor.ActID, pos.PosX, pos.PosY,
                                locName), RLColor.LightGray, backColor));
                            break;
                        case ArcType.Location:
                            eventList.Add(new Snippet(string.Format("{0}, Aid {1}. at {2} (Loc {3}:{4})", actor.Name, actor.ActID, locName, pos.PosX, pos.PosY), RLColor.LightGray, backColor));
                            break;
                        case ArcType.Actor:
                            if (actor.Status == ActorStatus.AtLocation) { status = locName + " "; }
                            else { status = null; }
                            eventList.Add(new Snippet(string.Format("{0}, Aid {1}. at {2}(Loc {3}:{4})", actor.Name, actor.ActID, status,
                                pos.PosX, pos.PosY), RLColor.LightGray, backColor));
                            break;
                        case ArcType.Dungeon:
                            eventList.Add(new Snippet(string.Format("{0}, Aid {1}, incarcerated in {2}'s dungeons (Loc {3}:{4}). Survival time {5} days.", actor.Name, actor.ActID,
                                locName, pos.PosX, pos.PosY, actor.DeathTimer), RLColor.LightGray, backColor));
                            break;
                        case ArcType.Adrift:
                            eventList.Add(new Snippet(string.Format("{0}, Aid {1}, adrift in {2}. Survival time {3} days", actor.Name, actor.ActID, actor.SeaName,
                                actor.DeathTimer), RLColor.LightGray, backColor));
                            break;
                        default:
                            Game.SetError(new Error(70, $"Inknown ArcType \"{eventObject.Type}\""));
                            break;
                    }
                    eventList.Add(new Snippet(""));
                    eventList.Add(new Snippet("- o -", RLColor.Gray, backColor));
                    eventList.Add(new Snippet(""));
                    eventList.Add(new Snippet(eventObject.Text, foreColor, backColor));
                    eventList.Add(new Snippet(""));
                    eventList.Add(new Snippet("- o -", RLColor.Gray, backColor));
                    eventList.Add(new Snippet(""));
                    //display options (F1, F2, F3 ...)
                    List<OptionInteractive> listOptions = eventObject.GetOptions();
                    string optionText;
                    int ctr = 1;
                    int maxWidth = 0;
                    RLColor optionColor;
                    if (listOptions != null)
                    {
                        foreach (OptionInteractive option in listOptions)
                        {
                            //check any option triggers 
                            if (ResolveOptionTrigger(option) == true)
                            { optionColor = RLColor.Blue; option.Active = true; }
                            else
                            {
                                //invalid triggers, option shown greyed out and unusable
                                optionColor = RLColor.LightGray;
                                option.Active = false;
                            }
                            optionText = string.Format("[F{0}]  {1}", ctr++, option.Text);
                            if (optionText.Length > maxWidth) { maxWidth = optionText.Length; }
                            eventList.Add(new Snippet(string.Format("{0, -40}", optionText), optionColor, backColor));
                            eventList.Add(new Snippet(""));
                        }
                    }
                    else { Game.SetError(new Error(70, "Invalid ListOfOptions Player input (null)")); break; }

                    //repeat timer (set # of activations till event goes dormant)
                    if (eventObject.TimerRepeat > 0)
                    {
                        eventObject.TimerRepeat--;
                        Game.logTurn?.Write(string.Format(" Event \"{0}\" Timer Repeat now {1}", eventObject.Name, eventObject.TimerRepeat));
                        //if repeat timer has run down to 0, the event is no longer active
                        if (eventObject.TimerRepeat == 0)
                        {
                            eventObject.Status = EventStatus.Dormant;
                            Game.logTurn?.Write(string.Format(" Event \"{0}\" Timer Repeat has run down to Zero. Event is now {1}", eventObject.Name, eventObject.Status));
                        }
                    }
                    //reset cool down timer
                    eventObject.TimerCoolDown = eventObject.TimerCoolBase;
                    Game.logTurn?.Write(string.Format(" Event \"{0}\" Cooldown Timer has been reset to {1}", eventObject.Name, eventObject.TimerCoolBase));
                    //info at bottom
                    eventList.Add(new Snippet(""));
                    eventList.Add(new Snippet("Press ENTER or ESC to ignore this event", RLColor.LightGray, backColor));
                    //housekeeping
                    Game.infoChannel.SetInfoList(eventList, ConsoleDisplay.Event);
                    returnValue = true;
                    package.Done = true;
                    break;
                }
            }
            return returnValue;
        }


        /// <summary>
        /// Checks any triggers for the option and determines if it's active (returns true if any trigger passes, fails if any compulsory trigger fails)
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        private bool ResolveOptionTrigger(OptionInteractive option)
        {
            bool validCheck = true;
            int checkValue;
            List<Trigger> listTriggers = option.GetTriggers();
            if (listTriggers.Count > 0)
            {
                Player player = Game.world.GetPlayer();
                //check each trigger
                validCheck = false;
                foreach (Trigger trigger in listTriggers)
                {
                    checkValue = 0;
                    switch (trigger.Check)
                    {
                        case TriggerCheck.None:
                            break;
                        case TriggerCheck.Trait:
                            SkillType type;
                            try
                            { type = (SkillType)trigger.Data; }
                            catch
                            {
                                //set to combat to get the job done but generates an error
                                type = SkillType.Combat;
                                Game.SetError(new Error(76, string.Format("Invalid Trigger Data (\"{0}\"), default Combat trait used instead, for Option \"{1}\"", trigger.Data, option.Text)));
                            }
                            Game.logTurn?.Write(string.Format(" \"{0}\" {1} Trigger, if type {2} is {3} to {4}", option.Text, trigger.Check, trigger.Data, trigger.Calc, trigger.Threshold));
                            if (CheckTrigger(player.GetSkill(type), trigger.Calc, trigger.Threshold) == true) { validCheck = true; }
                            else
                            {
                                Game.logTurn?.Write("Trait trigger failed");
                                if (trigger.Compulsory == true) { Game.logTurn?.Write("[Notification] Trait trigger Compulsory fail check"); return false; }
                            }
                            break;
                        case TriggerCheck.GameVar:
                            //get % value for required gamevar
                            if (trigger.Data > 0 && trigger.Data < (int)GameState.Count)
                            {
                                checkValue = CheckGameState((GameState)trigger.Data);
                                if (CheckTrigger(checkValue, trigger.Calc, trigger.Threshold) == true)
                                { validCheck = true; }
                                else {
                                    Game.logTurn?.Write(" Trigger: GameVar value incorrect -> Trigger failed");
                                    if (trigger.Compulsory == true) { Game.logTurn?.Write("[Notification] GameVar trigger Compulsory fail check"); return false; }
                                }
                            }
                            else { Game.SetError(new Error(76, $"Invalid trigger.Data \"{trigger.Data}\" for option {option.Text} -> Trigger Ignored")); }
                            break;
                        case TriggerCheck.RelPlyr:
                            if (CheckTrigger(trigger.Data, trigger.Calc, trigger.Threshold) == true) { validCheck = true; }
                            else
                            {
                                Game.logTurn?.Write($" RelPlyr is too low -> Trigger failed");
                                if (trigger.Compulsory == true) { Game.logTurn?.Write("[Notification] RelPlyr trigger Compulsory fail check"); return false; }
                            }
                            break;
                        case TriggerCheck.Sex:
                            //Threshold = (int)ActorSex -> Male 1, Female 2 (sex of actor). Must be opposite sex (seduction
                            if (CheckTrigger((int)player.Sex, trigger.Calc, trigger.Threshold) == true) { validCheck = true; }
                            else
                            {
                                Game.logTurn?.Write(" Trigger: Same sex, seduction impossible -> Trigger failed");
                                if (trigger.Compulsory == true) { Game.logTurn?.Write("[Notification] Sex trigger Compulsory fail check"); return false; }
                            }
                            break;
                        case TriggerCheck.Disguise:
                            //Passes if Player Does NOT have a current disguise in his Inventory
                            if (player.ConcealDisguise == 0) { validCheck = true; }
                            else
                            {
                                Game.logTurn?.Write(" Trigger: Disguise, Player already has a disguise -> Trigger failed");
                                if (trigger.Compulsory == true) { Game.logTurn?.Write("[Notification] Disguise trigger Compulsory fail check"); return false; }
                            }
                            break;
                        case TriggerCheck.Promise:
                            //number of promises handed out is greater than the max. num. of promises allowed
                            if (CheckTrigger(Game.variable.GetValue(GameVar.Promises_Num), EventCalc.LessThanOrEqual, Game.constant.GetValue(Global.PROMISES_MAX)) == true) { validCheck = true; }
                            else
                            {
                                Game.logTurn?.Write("Trigger: Promise count has exceeded Max. allowed -> Trigger failed");
                                if (trigger.Compulsory == true) { Game.logTurn?.Write("[Notification] Promise trigger Compulsory fail check"); return false; }
                            }
                            break;
                        case TriggerCheck.Desire:
                            Actor actor = Game.world.GetAnyActor(option.ActorID);
                            if (actor != null && actor is Passive)
                            {
                                Passive person = actor as Passive;
                                if (person.Desire > PossPromiseType.None && person.Satisfied == false)
                                { Game.logTurn?.Write($"Trigger: {person.Name} has desire {person.DesireText} and Satisified {person.Satisfied} -> passed"); validCheck = true; }
                                else
                                {
                                    Game.logTurn?.Write("Trigger: Desire is None or Satisified is True -> Trigger failed");
                                    if (trigger.Compulsory == true) { Game.logTurn?.Write("[Notification] Desire trigger Compulsory fail check"); return false; }
                                }
                            }
                            else { Game.logTurn?.Write("Trigger: Desire, actor is Null or Not Passive -> Trigger ignored"); return false; }
                            break;
                        case TriggerCheck.ActorType:
                            //Data = ActorType, Threshold is required type. Must be equal
                            if (CheckTrigger(trigger.Data, trigger.Calc, trigger.Threshold) == true) { validCheck = true; }
                            else
                            {
                                Game.logTurn?.Write(" Trigger: Incorrect ActorType -> Trigger failed");
                                if (trigger.Compulsory == true) { Game.logTurn?.Write("[Notification] ActorType trigger Compulsory fail check"); return false; }
                            }
                            break;
                        case TriggerCheck.ResourcePlyr:
                            if (CheckTrigger(player.Resources, trigger.Calc, trigger.Threshold) == true) { validCheck = true; }
                            else
                            {
                                Game.logTurn?.Write(" Trigger: Player has wrong amount of Resources -> Trigger failed");
                                if (trigger.Compulsory == true) { Game.logTurn?.Write("[Notification] ResourcePlyr trigger Compulsory fail check"); return false; }
                            }
                            break;
                        case TriggerCheck.Known:
                            checkValue = 0;
                            if (player.Known == true) { checkValue = 1; }
                            if (CheckTrigger(checkValue, EventCalc.Equals, trigger.Threshold) == true) { validCheck = true; }
                            else
                            {
                                Game.logTurn?.Write(" Trigger: Player is wrong type of Known status -> Trigger failed");
                                if (trigger.Compulsory == true) { Game.logTurn?.Write("[Notification] Known trigger Compulsory fail check"); return false; }
                            }
                            break;
                        case TriggerCheck.Introduction:
                            checkValue = 0;
                            if (player.IntroPresented == true) { checkValue = 1; }
                            if (CheckTrigger(checkValue, EventCalc.Equals, trigger.Threshold) == true) { validCheck = true; }
                            else
                            {
                                Game.logTurn?.Write(" Trigger: Player is wrong type of Known status -> Trigger failed");
                                if (trigger.Compulsory == true) { Game.logTurn?.Write("[Notification] Introduction trigger Compulsory fail check"); return false; }
                            }
                            break;
                        default:
                            Game.SetError(new Error(76, string.Format("Invalid Trigger Check Type (\"{0}\") for Option \"{1}\" -> Trigger ignored", trigger.Check, option.Text)));
                            break;
                    }
                }
            }
            return validCheck;
        }

        /// <summary>
        /// Checks the validity of any trigger (if passes -> return true)
        /// </summary>
        /// <param name="data">any number</param>
        /// <param name="comparator"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        private bool CheckTrigger(int data, EventCalc comparator, int threshold)
        {
            bool validCheck = true;
            switch (comparator)
            {
                case EventCalc.GreaterThanOrEqual:
                    if (data < threshold) { validCheck = false; }
                    break;
                case EventCalc.LessThanOrEqual:
                    if (data > threshold) { validCheck = false; }
                    break;
                case EventCalc.Equals:
                    if (data != threshold) { validCheck = false; }
                    break;
                case EventCalc.NotEqual:
                    if (data == threshold) { validCheck = false; }
                    break;
                case EventCalc.Add:
                case EventCalc.Subtract:
                case EventCalc.Random:
                    break;
                default:
                    Game.SetError(new Error(77, string.Format("Invalid Trigger Calculation Type (\"{0}\")", comparator)));
                    validCheck = false;
                    break;
            }
            Game.logTurn?.Write(string.Format(" Trigger {0} on \"{1}\" {2} {3}", validCheck == true ? "passed" : "FAILED", data, comparator, threshold));
            return validCheck;
        }

        /// <summary>
        /// handles outcome resolution for player events, eg. player has chosen option 2 (pressed 'F2').Return '1' if true, '2' if a conflict, '0' if false.
        /// </summary>
        /// <param name="eventID"></param>
        /// <param name="optionNum"></param>
        public int ResolveOptionOutcome(int eventID, int optionNum)
        {
            Game.logTurn?.Write("--- ResolveOutcome (Director.cs)");
            int validOption = 1;
            int actorID;
            int rndNum, DMskill, DMtouched, numModified;
            int modBase = Game.constant.GetValue(Global.TEST_MULTIPLIER);
            string rndResult = "";
            string status;
            string outcomeText = "";
            string tempText = "";
            string skillTrait = "";
            string touchedTrait = "";
            List<Snippet> eventList = new List<Snippet>();
            List<Snippet> resultList = new List<Snippet>();
            RLColor foreColor = RLColor.Black;
            RLColor backColor = Color._background1;
            //Get eVent
            EventPlayer eventObject = GetPlayerEvent(eventID);
            if (eventObject != null)
            {
                //find option
                List<OptionInteractive> listOptions = eventObject.GetOptions();
                string optionReply = "unknown option Reply";
                string eventText = $"Event \"{eventObject.Name}\", ";
                if (listOptions != null)
                {
                    DMskill = 0; DMtouched = 0; rndNum = 0; numModified = 0;
                    if (listOptions.Count >= optionNum)
                    {
                        OptionInteractive option = listOptions[optionNum - 1];
                        Active player = Game.world.GetActiveActor(1);
                        int refID = Game.world.ConvertLocToRef(player.LocID);
                        //Active option?
                        if (option.Active == true)
                        {
                            List<Outcome> listOutcomes = new List<Outcome>();
                            //resolve option -> Is it a variable (chance of a good or bad outcome) option?
                            if (option.Test > 0)
                            {
                                if (String.IsNullOrEmpty(option.ReplyBad) == true)
                                { Game.SetError(new Error(73, string.Format("Invalid Test option (no ReplyBad) for \"{0}\"", option.Text))); }
                                else
                                {
                                    rndNum = rnd.Next(0, 100);
                                    //Skill DM?
                                    if (option.Skill > SkillType.None)
                                    {
                                        int skill = player.GetSkill(option.Skill);
                                        //DM modifier to rndNum roll if any skill level other than 3 (average)
                                        switch (skill)
                                        {
                                            case 1: DMskill = modBase * 2; skillTrait = player.GetTraitName(option.Skill); break;
                                            case 2: DMskill = modBase * 1; skillTrait = player.GetTraitName(option.Skill); break;
                                            case 4: DMskill = modBase * -1; skillTrait = player.GetTraitName(option.Skill); break;
                                            case 5: DMskill = modBase * -2; skillTrait = player.GetTraitName(option.Skill); break;
                                        }
                                        //touched effect (universal)
                                        if (option.Skill != SkillType.Touched)
                                        {
                                            int touched = player.GetSkill(SkillType.Touched);
                                            if (touched > 0)
                                            { DMtouched = touched * modBase / 2 * -1; touchedTrait = player.GetTraitName(SkillType.Touched); }
                                        }
                                    }
                                    //adjust roll for DM's
                                    numModified = rndNum + DMskill + DMtouched;
                                    //Pass the test?
                                    if (numModified <= option.Test)
                                    {
                                        listOutcomes = option.GetGoodOutcomes(); optionReply = option.ReplyGood;
                                        Game.logTurn?.Write(string.Format(" [Variable Option] \"{0}\" Passed test ({1} % needed, rolled {2})  Roll {3} + DMskill {4} + DMtouched {5} -> modifiedRoll {6}",
                                            option.Text, option.Test, numModified, rndNum, DMskill, DMtouched, numModified));
                                        rndResult = "Success!";
                                    }
                                    else
                                    {
                                        listOutcomes = option.GetBadOutcomes(); optionReply = option.ReplyBad;
                                        Game.logTurn?.Write(string.Format(" [Variable Option] \"{0}\" Failed test ({1} % needed, rolled {2})  Roll {3} + DMskill {4} + DMtouched {5} -> modifiedRoll {6}",
                                            option.Text, option.Test, numModified, rndNum, DMskill, DMtouched, numModified));
                                        rndResult = "Fail";
                                    }
                                }
                            }
                            else { listOutcomes = option.GetGoodOutcomes(); optionReply = option.ReplyGood; }
                            //resolve each Outcome
                            if (listOutcomes != null)
                            {
                                foreach (Outcome outcome in listOutcomes)
                                {
                                    switch (outcome.Type)
                                    {
                                        case OutcomeType.None:
                                            //display descriptive text, if present
                                            OutNone noneOutcome = outcome as OutNone;
                                            if (noneOutcome.Description.Length > 0)
                                            {
                                                Game.world.SetMessage(new Message(eventText + noneOutcome.Description, MessageType.Event));
                                                Game.world.SetPlayerRecord(new Record(eventText + noneOutcome.Description, 1, noneOutcome.Data, CurrentActorIncident.Event));
                                            }
                                            break;
                                        case OutcomeType.GameState:
                                            //Change a Game state variable, eg. Honour, Justice, etc.
                                            outcomeText = SetState(eventObject.Name, option.Text, outcome.Data, outcome.Amount, outcome.Calc);
                                            if (String.IsNullOrEmpty(outcomeText) == false)
                                            { resultList.Add(new Snippet(outcomeText, foreColor, backColor)); resultList.Add(new Snippet("")); }
                                            break;
                                        case OutcomeType.Known:
                                            //change an Active Actor's Known/Unknown status
                                            outcomeText = Game.world.SetActiveActorKnownStatus(1, outcome.Data);
                                            if (String.IsNullOrEmpty(outcomeText) == false)
                                            { resultList.Add(new Snippet(outcomeText, foreColor, backColor)); resultList.Add(new Snippet("")); }
                                            //message
                                            //tempText = string.Format("Event \"{0}\", Option \"{1}\", {2}", eventObject.Name, option.Text, outcomeText);
                                            Game.world.SetMessage(new Message(outcomeText, 1, 0, MessageType.Event));
                                            Game.world.SetPlayerRecord(new Record(outcomeText, player.ActID, player.LocID, CurrentActorIncident.Event));
                                            break;
                                        case OutcomeType.SafeHouse:
                                            //change Player's SafeHouse status
                                            outcomeText = ChangePlayerSafeHouseStatus(outcome.Data);
                                            if (String.IsNullOrEmpty(outcomeText) == false)
                                            { resultList.Add(new Snippet(outcomeText, foreColor, backColor)); resultList.Add(new Snippet("")); }
                                            //message
                                            //tempText = string.Format("Event \"{0}\", Option \"{1}\", {2}", eventObject.Name, option.Text, outcomeText);
                                            Game.world.SetMessage(new Message(outcomeText, 1, 0, MessageType.Event));
                                            Game.world.SetPlayerRecord(new Record(outcomeText, player.ActID, player.LocID, CurrentActorIncident.Event));
                                            break;
                                        case OutcomeType.Disguise:
                                            //transfer a disguise from an Advisor to the Player
                                            outcomeText = ChangePlayerDisguiseStatus(option.ActorID);
                                            if (String.IsNullOrEmpty(outcomeText) == false)
                                            { resultList.Add(new Snippet(outcomeText, foreColor, backColor)); resultList.Add(new Snippet("")); }
                                            //message
                                            Game.world.SetMessage(new Message(outcomeText, 1, 0, MessageType.Event));
                                            Game.world.SetPlayerRecord(new Record(outcomeText, player.ActID, player.LocID, CurrentActorIncident.Event));
                                            break;
                                        case OutcomeType.Rumour:
                                            //the Player gains rumours from 'asking around for information'
                                            int rumourCount = 0;
                                            for (int i = 0; i < Game.constant.GetValue(Global.PLAYER_RUMORS); i++)
                                            {
                                                outcomeText = ChangePlayerRumourStatus();
                                                if (String.IsNullOrEmpty(outcomeText) == false)
                                                {
                                                    if (i == 0)
                                                    {
                                                        resultList.Add(new Snippet($"{player.Name}, \"{player.Handle}\", has learned of the following rumours...", RLColor.Red, backColor));
                                                        resultList.Add(new Snippet(""));
                                                    }
                                                    rumourCount++;
                                                    resultList.Add(new Snippet(""));
                                                    resultList.Add(new Snippet(outcomeText, foreColor, backColor)); resultList.Add(new Snippet(""));
                                                }
                                            }
                                            //message
                                            if (rumourCount > 0)
                                            { outcomeText = string.Format("{0}, \"{1}\", has learned of {2} new rumour{3}", player.Name, player.Handle, rumourCount, rumourCount != 1 ? "s" : ""); }
                                            else { outcomeText = $"{player.Name}, \"{player.Handle}\", heard only old news and stale, out-of-date, rumours"; }
                                            Game.world.SetMessage(new Message(outcomeText, 1, 0, MessageType.Event));
                                            Game.world.SetPlayerRecord(new Record(outcomeText, player.ActID, player.LocID, CurrentActorIncident.Event));
                                            break;
                                        case OutcomeType.Freedom:
                                            //change a player's status
                                            if (outcome.Data > 0)
                                            {
                                                //only valid if Player is already captured -> must be at a location
                                                if (player.Status == ActorStatus.Captured)
                                                {
                                                    //free'd from captivity
                                                    player.Status = ActorStatus.AtLocation;
                                                    tempText = string.Format("{0} {1} has escaped from the dungeons of {2}", player.Title, player.Name, Game.world.GetLocationName(player.LocID));
                                                    Game.world.SetMessage(new Message(tempText, 1, 0, MessageType.Event));
                                                    Game.world.SetPlayerRecord(new Record(tempText, player.ActID, player.LocID, CurrentActorIncident.Event));
                                                }
                                                else { Game.SetError(new Error(73, "Player Status isn't currently 'Captured' (Outcome)")); }
                                            }
                                            //captured -> records and message handled by called Method
                                            else if (outcome.Data < 0) { Game.world.SetPlayerCaptured(option.ActorID); }
                                            else { Game.SetError(new Error(73, "Invalid Data value (zero) for OutcomeType -> Freedom")); }
                                            break;
                                        case OutcomeType.EventTimer:
                                            //Change an Event Timer
                                            OutEventTimer timerOutcome = outcome as OutEventTimer;
                                            outcomeText = ChangePlayerEventTimer(timerOutcome);
                                            if (String.IsNullOrEmpty(outcomeText) == false)
                                            { resultList.Add(new Snippet(outcomeText, foreColor, backColor)); resultList.Add(new Snippet("")); }
                                            break;
                                        case OutcomeType.EventStatus:
                                            //change Event Status
                                            OutEventStatus statusOutcome = outcome as OutEventStatus;
                                            outcomeText = ChangePlayerEventStatus(statusOutcome.Data, statusOutcome.NewStatus);
                                            if (String.IsNullOrEmpty(outcomeText) == false)
                                            { resultList.Add(new Snippet(outcomeText, foreColor, backColor)); resultList.Add(new Snippet("")); }
                                            break;
                                        case OutcomeType.RelPlyr:
                                            //change NPC's relationship with Player
                                            OutRelPlyr relPlyrOutcome = outcome as OutRelPlyr;
                                            outcomeText = ChangePlayerRelStatus(option.ActorID, relPlyrOutcome.Amount, relPlyrOutcome.Calc, relPlyrOutcome.Description, relPlyrOutcome.Tag);
                                            if (String.IsNullOrEmpty(outcomeText) == false)
                                            {
                                                resultList.Add(new Snippet(outcomeText, foreColor, backColor)); resultList.Add(new Snippet(""));
                                                Game.world.SetMessage(new Message(eventText + outcomeText, 1, player.LocID, MessageType.Event));
                                                Game.world.SetPlayerRecord(new Record(eventText + outcomeText, player.ActID, player.LocID, CurrentActorIncident.Event));
                                                Game.world.SetCurrentRecord(new Record(eventText + outcomeText, option.ActorID, player.LocID, CurrentActorIncident.Event));
                                            }
                                            break;
                                        case OutcomeType.Item:
                                            //Player gains or loses an item
                                            OutItem itemOutcome = outcome as OutItem;
                                            outcomeText = ChangePlayerItemStatus(itemOutcome.Calc, itemOutcome.Data, option.ActorID);
                                            if (String.IsNullOrEmpty(outcomeText) == false)
                                            {
                                                resultList.Add(new Snippet(outcomeText, foreColor, backColor)); resultList.Add(new Snippet(""));
                                                Game.world.SetMessage(new Message(eventText + outcomeText, 1, player.LocID, MessageType.Event));
                                                Game.world.SetPlayerRecord(new Record(eventText + outcomeText, player.ActID, player.LocID, CurrentActorIncident.Event));
                                            }
                                            break;
                                        case OutcomeType.Promise:
                                            //Player hands out a promise (IOU) to a Passive character
                                            OutPromise promiseOutcome = outcome as OutPromise;
                                            outcomeText = ChangePlayerPromiseStatus(option.ActorID, promiseOutcome.PromiseType, promiseOutcome.Data);
                                            if (String.IsNullOrEmpty(outcomeText) == false)
                                            {
                                                resultList.Add(new Snippet(outcomeText, foreColor, backColor)); resultList.Add(new Snippet(""));
                                                Game.world.SetMessage(new Message(eventText + outcomeText, 1, player.LocID, MessageType.Event));
                                                Game.world.SetPlayerRecord(new Record(eventText + outcomeText, player.ActID, player.LocID, CurrentActorIncident.Event));
                                            }
                                            break;
                                        case OutcomeType.Favour:
                                            //NPC (passive) hands out a Favour to the Player
                                            outcomeText = ChangePlayerFavourStatus(option.ActorID, outcome.Data);
                                            if (String.IsNullOrEmpty(outcomeText) == false)
                                            {
                                                resultList.Add(new Snippet(outcomeText, foreColor, backColor)); resultList.Add(new Snippet(""));
                                                Game.world.SetMessage(new Message(eventText + outcomeText, 1, player.LocID, MessageType.Event));
                                                Game.world.SetPlayerRecord(new Record(eventText + outcomeText, player.ActID, player.LocID, CurrentActorIncident.Event));
                                            }
                                            break;
                                        case OutcomeType.Introduction:
                                            //Player uses an introduction to gain access to a House court audience
                                            outcomeText = ChangePlayerIntroductionStatus(outcome.Data);
                                            if (String.IsNullOrEmpty(outcomeText) == false)
                                            {
                                                resultList.Add(new Snippet(outcomeText, foreColor, backColor)); resultList.Add(new Snippet(""));
                                                Game.world.SetMessage(new Message(eventText + outcomeText, 1, player.LocID, MessageType.Event));
                                                Game.world.SetPlayerRecord(new Record(eventText + outcomeText, player.ActID, player.LocID, CurrentActorIncident.Event));
                                            }
                                            break;
                                        case OutcomeType.GameVar:
                                            //Change a GameVar
                                            OutGameVar gamevarOutcome = outcome as OutGameVar;
                                            outcomeText = ChangeGameVarStatus(gamevarOutcome.GameVar, gamevarOutcome.Amount, gamevarOutcome.Calc);
                                            if (String.IsNullOrEmpty(outcomeText) == false)
                                            {
                                                resultList.Add(new Snippet(outcomeText, foreColor, backColor)); resultList.Add(new Snippet(""));
                                                Game.world.SetMessage(new Message(eventText + outcomeText, 1, player.LocID, MessageType.Event));
                                                Game.world.SetPlayerRecord(new Record(eventText + outcomeText, player.ActID, player.LocID, CurrentActorIncident.Event));
                                            }
                                            break;
                                        case OutcomeType.Passage:
                                            //Player goes on a sea voyage
                                            OutPassage passageOutcome = outcome as OutPassage;
                                            outcomeText = ChangePlayerVoyageStatus(passageOutcome.DestinationID, passageOutcome.Data, passageOutcome.VoyageSafe);
                                            if (String.IsNullOrEmpty(outcomeText) == false)
                                            {
                                                resultList.Add(new Snippet(outcomeText, foreColor, backColor)); resultList.Add(new Snippet(""));
                                                Game.world.SetMessage(new Message(outcomeText, 1, player.LocID, MessageType.Event));
                                                Game.world.SetPlayerRecord(new Record(outcomeText, player.ActID, player.LocID, CurrentActorIncident.Event));
                                            }
                                            break;
                                        case OutcomeType.VoyageTime:
                                            //Player's sea voyage time is increased or decreased
                                            outcomeText = ChangePlayerVoyageTime(outcome.Amount, outcome.Calc);
                                            if (String.IsNullOrEmpty(outcomeText) == false)
                                            {
                                                resultList.Add(new Snippet(outcomeText, foreColor, backColor)); resultList.Add(new Snippet(""));
                                                Game.world.SetMessage(new Message(eventText + outcomeText, 1, player.LocID, MessageType.Event));
                                                Game.world.SetPlayerRecord(new Record(eventText + outcomeText, player.ActID, player.LocID, CurrentActorIncident.Event));
                                            }
                                            break;
                                        case OutcomeType.Adrift:
                                            //Player cast adrift
                                            OutAdrift adriftOutcome = outcome as OutAdrift;
                                            outcomeText = ChangePlayerAdriftStatus(adriftOutcome.DeathTimer, adriftOutcome.ShipSunk);
                                            if (String.IsNullOrEmpty(outcomeText) == false)
                                            {
                                                resultList.Add(new Snippet(outcomeText, foreColor, backColor)); resultList.Add(new Snippet(""));
                                                Game.world.SetMessage(new Message(eventText + outcomeText, 1, player.LocID, MessageType.Event));
                                                Game.world.SetPlayerRecord(new Record(eventText + outcomeText, player.ActID, player.LocID, CurrentActorIncident.Event));
                                            }
                                            break;
                                        case OutcomeType.Rescued:
                                            //Player rescued from drifting around the ocean on a raft
                                            OutRescued rescuedOutcome = outcome as OutRescued;
                                            outcomeText = ChangePlayerRescuedStatus(rescuedOutcome.Safe);
                                            if (String.IsNullOrEmpty(outcomeText) == false)
                                            {
                                                resultList.Add(new Snippet(outcomeText, foreColor, backColor)); resultList.Add(new Snippet(""));
                                                Game.world.SetMessage(new Message(eventText + outcomeText, 1, player.LocID, MessageType.Event));
                                                Game.world.SetPlayerRecord(new Record(eventText + outcomeText, player.ActID, player.LocID, CurrentActorIncident.Event));
                                            }
                                            break;
                                        case OutcomeType.Follower:
                                            //New Follower recruited from an Inn
                                            outcomeText = ChangeFollowerStatus(option.ActorID, player.LocID);
                                            if (String.IsNullOrEmpty(outcomeText) == false)
                                            {
                                                resultList.Add(new Snippet(outcomeText, foreColor, backColor)); resultList.Add(new Snippet(""));
                                                Game.world.SetMessage(new Message(eventText + outcomeText, 1, player.LocID, MessageType.Event));
                                                Game.world.SetPlayerRecord(new Record(eventText + outcomeText, player.ActID, player.LocID, CurrentActorIncident.Event));
                                            }
                                            break;
                                        case OutcomeType.DeathTimer:
                                            //adjust the number of turns remaining on the Death Timer (Adrift and Dungeon situations only)
                                            OutDeathTimer deathOutcome = outcome as OutDeathTimer;
                                            outcomeText = ChangePlayerDeathTimer(deathOutcome.Amount, deathOutcome.Calc);
                                            if (String.IsNullOrEmpty(outcomeText) == false)
                                            {
                                                resultList.Add(new Snippet(outcomeText, foreColor, backColor)); resultList.Add(new Snippet(""));
                                                Game.world.SetMessage(new Message(eventText + outcomeText, 1, player.LocID, MessageType.Event));
                                                Game.world.SetPlayerRecord(new Record(eventText + outcomeText, player.ActID, player.LocID, CurrentActorIncident.Event));
                                            }
                                            break;
                                        case OutcomeType.Resource:
                                            //adjust the resource level of Player or an NPC actor
                                            OutResource resourceOutcome = outcome as OutResource;
                                            if (resourceOutcome.PlayerRes == false) { actorID = option.ActorID; }
                                            else { actorID = 1; }
                                            Actor personRes = Game.world.GetAnyActor(actorID);
                                            if (personRes != null)
                                            {
                                                outcomeText = personRes.ChangeResources(resourceOutcome.Amount, resourceOutcome.Calc);
                                                if (String.IsNullOrEmpty(outcomeText) == false)
                                                { resultList.Add(new Snippet(outcomeText, foreColor, backColor)); resultList.Add(new Snippet("")); }
                                            }
                                            break;
                                        case OutcomeType.Condition:
                                            //apply a condition to the Player or an NPC actor -> use copy constructor to pass by value, not reference (otherwise all timers are indentical)
                                            OutCondition conditionOutcome = new OutCondition(outcome as OutCondition);
                                            if (conditionOutcome.PlayerCondition == false) { actorID = option.ActorID; }
                                            else { actorID = 1; }
                                            Actor personCon = Game.world.GetAnyActor(actorID);
                                            if (personCon != null)
                                            {
                                                //does the character already have this condition?
                                                if (personCon.CheckConditionPresent(conditionOutcome.NewCondition.Text) == false)
                                                {
                                                    //not present -> add new condition
                                                    outcomeText = personCon.AddCondition(conditionOutcome.NewCondition);
                                                    if (String.IsNullOrEmpty(outcomeText) == false)
                                                    { resultList.Add(new Snippet(outcomeText, foreColor, backColor)); resultList.Add(new Snippet("")); }
                                                }
                                                else
                                                {
                                                    //existing identical condition already present. Reset existing condition timer to the max value.
                                                    personCon.ResetConditionTimer(conditionOutcome.NewCondition.Text, conditionOutcome.NewCondition.Timer);
                                                    outcomeText = string.Format("\"{0}\" Condition already acquired by {1}, Timer reset to {2} days", conditionOutcome.NewCondition.Text, personCon.Name,
                                                        conditionOutcome.NewCondition.Timer);
                                                    resultList.Add(new Snippet(outcomeText, foreColor, backColor)); resultList.Add(new Snippet(""));
                                                }
                                            }
                                            break;
                                        case OutcomeType.EventChain:
                                            //chain events -> used by CreateAuto Loc Events
                                            actorID = option.ActorID;
                                            OutEventChain chainOutcome = outcome as OutEventChain;
                                            //if introduction used to gain access to Court, or Advisors, make sure it can't be reused
                                            if (chainOutcome.Filter == EventAutoFilter.Court || chainOutcome.Filter == EventAutoFilter.Advisors)
                                            { player.IntroPresented = false; }
                                            CreateAutoLocEvent(chainOutcome.Filter, actorID);
                                            Game._eventID = eventObject.EventPID;
                                            break;
                                        case OutcomeType.Conflict:
                                            //seque straight into a Conflict
                                            actorID = option.ActorID;
                                            if (actorID > 0)
                                            {
                                                validOption = 2; //activates conflict in Game.cs SetSpecialModeInput()
                                                OutConflict conflictOutcome = outcome as OutConflict;
                                                Game.conflict.Conflict_Type = conflictOutcome.Conflict_Type;
                                                Game.conflict.Combat_Type = conflictOutcome.Combat_Type;
                                                Game.conflict.Social_Type = conflictOutcome.Social_Type;
                                                Game.conflict.Stealth_Type = conflictOutcome.Stealth_Type;
                                                Game.conflict.SetOpponent(actorID, conflictOutcome.Challenger);
                                                //create a new challenge in dictionary that's used to overide data in standard challenge
                                                if (conflictOutcome.challenge.GetOveride() == true)
                                                {
                                                    //is there an existing special challenge (overide) in the dictionary? delete is so
                                                    if (dictChallenges.ContainsKey(ConflictSubType.Special) == true)
                                                    { dictChallenges.Remove(ConflictSubType.Special); }
                                                    //create new Special challenge & copy data across from OutConflict.challenge
                                                    Challenge challenge = new Challenge(conflictOutcome.challenge);
                                                    //add to dictionary
                                                    try
                                                    { dictChallenges.Add(ConflictSubType.Special, challenge); }
                                                    catch (ArgumentException)
                                                    { Game.SetError(new Error(73, "Invalid challenge key when adding to dictChallenges (duplicate)")); }
                                                }
                                                //message
                                                Actor opponent = Game.world.GetAnyActor(actorID);
                                                if (opponent != null)
                                                {
                                                    tempText = string.Format("A {0} {1} Conflict initiated with {2} {3}, Aid {4}", conflictOutcome.SubType,
                                                        conflictOutcome.Conflict_Type, opponent.Title, opponent.Name, opponent.ActID);
                                                    Game.world.SetMessage(new Message(tempText, MessageType.Conflict));
                                                    Game.world.SetPlayerRecord(new Record(tempText, player.ActID, player.LocID, CurrentActorIncident.Challenge));
                                                }
                                                //which state to use?
                                                ConflictState state = ConflictState.None;
                                                switch (conflictOutcome.Conflict_Type)
                                                {
                                                    case ConflictType.Combat:
                                                        switch (conflictOutcome.Combat_Type)
                                                        {
                                                            case ConflictCombat.Battle:
                                                                state = ConflictState.Relative_Army_Size;
                                                                break;
                                                            case ConflictCombat.Personal:
                                                            case ConflictCombat.Tournament:
                                                            case ConflictCombat.Hunting:
                                                                state = ConflictState.Relative_Fame;
                                                                break;
                                                        }
                                                        break;
                                                    case ConflictType.Social:
                                                        switch (conflictOutcome.Social_Type)
                                                        {
                                                            case ConflictSocial.Befriend:
                                                                state = ConflictState.Relative_Honour;
                                                                break;
                                                            case ConflictSocial.Seduce:
                                                            case ConflictSocial.Blackmail:
                                                                state = ConflictState.Relative_Fame;
                                                                break;
                                                        }
                                                        break;
                                                    case ConflictType.Stealth:
                                                        state = ConflictState.Known_Status;
                                                        break;
                                                }
                                                if (state == ConflictState.None)
                                                { Game.SetError(new Error(73, "Invalid state (ConflictState.None) -> changed to Justice")); state = ConflictState.Relative_Justice; }
                                                Game.conflict.SetGameSituation(state);
                                            }
                                            else
                                            { Game.SetError(new Error(73, string.Format("Invalid actorID (derived from SpecialID) for OutConflict (zero or less) \"{0}\", option # {1}",
                                                eventObject.Name, optionNum))); }
                                            break;
                                    }
                                }
                            }
                            else { Game.SetError(new Error(73, "Invalid list of Outcomes")); }
                            //display message
                            Position pos = player.GetActorPosition();
                            switch (eventObject.Type)
                            {
                                case ArcType.GeoCluster:
                                case ArcType.Road:
                                    eventList.Add(new Snippet(string.Format("{0}, Aid {1}, at Loc {2}:{3} travelling towards {4}", player.Name, player.ActID, pos.PosX, pos.PosY,
                                        Game.world.GetLocationName(player.LocID)), RLColor.LightGray, backColor));
                                    break;
                                case ArcType.Location:
                                    eventList.Add(new Snippet(string.Format("{0}, Aid {1}. at {2} (Loc {3}:{4})", player.Name, player.ActID, Game.world.GetLocationName(player.LocID),
                                        pos.PosX, pos.PosY), RLColor.LightGray, backColor));
                                    break;
                                case ArcType.Actor:
                                    if (player.Status == ActorStatus.AtLocation) { status = Game.world.GetLocationName(player.LocID) + " "; }
                                    else { status = null; }
                                    eventList.Add(new Snippet(string.Format("{0}, Aid {1}. at {2}(Loc {3}:{4})", player.Name, player.ActID, status,
                                        pos.PosX, pos.PosY), RLColor.LightGray, backColor));
                                    break;
                            }
                            eventList.Add(new Snippet(""));
                            eventList.Add(new Snippet("- o -", RLColor.Gray, backColor));
                            eventList.Add(new Snippet(""));
                            eventList.Add(new Snippet(string.Format("{0}", eventObject.Name), foreColor, backColor));
                            eventList.Add(new Snippet(""));
                            eventList.Add(new Snippet(string.Format("\"{0}\"", option.Text), foreColor, backColor));
                            eventList.Add(new Snippet(""));
                            eventList.Add(new Snippet("- o -", RLColor.Gray, backColor));
                            if (option.Test > 0)
                            {
                                eventList.Add(new Snippet(""));
                                eventList.Add(new Snippet($"Success on {option.Test} % or less", RLColor.Gray, backColor));
                                eventList.Add(new Snippet(""));
                                string skillMod = ""; string touchedMod = "";
                                if (DMskill != 0)
                                { skillMod = string.Format("{0} {1}{2} ", skillTrait, DMskill > 0 ? "+" : "", DMskill); }
                                if (DMtouched > 0)
                                { touchedMod = string.Format("{0} +{1}", touchedTrait, DMtouched); }
                                string modifiers = "";
                                if (DMskill != 0 || DMtouched > 0)
                                { modifiers = $"(Roll {rndNum}, {skillMod}{touchedMod})"; }
                                if (modifiers.Length > 0)
                                { eventList.Add(new Snippet($"Modified Roll {numModified} {modifiers} -> {rndResult}", RLColor.Gray, backColor)); }
                                else { eventList.Add(new Snippet($"Roll {numModified} -> {rndResult}", RLColor.Gray, backColor)); }
                                eventList.Add(new Snippet(""));
                                eventList.Add(new Snippet("- o -", RLColor.Gray, backColor));
                            }
                            eventList.Add(new Snippet(""));
                            eventList.Add(new Snippet(string.Format("{0}", optionReply), RLColor.LightBlue, backColor));
                            eventList.Add(new Snippet(""));
                            eventList.Add(new Snippet("- o -", RLColor.Gray, backColor));
                            eventList.Add(new Snippet(""));
                            if (resultList.Count > 0)
                            {
                                eventList.AddRange(resultList);
                                eventList.Add(new Snippet("- o -", RLColor.Gray, backColor));
                                eventList.Add(new Snippet(""));
                            }
                            eventList.Add(new Snippet("Press ENTER or ESC to ignore this event", RLColor.LightGray, backColor));
                            //housekeeping
                            Game.infoChannel.SetInfoList(eventList, ConsoleDisplay.Event);
                        }
                        else
                        {
                            //invalid option (trigger/s didn't pass)
                            validOption = 0;
                            Game.logTurn?.Write(string.Format(" Inactive (greyed out) Option chosen for \"{0}\", option # {1}", eventObject.Name, optionNum));
                        }
                    }
                    else { validOption = 0; Game.SetError(new Error(73, string.Format("No valid option present for \"{0}\", option # {1}", eventObject.Name, optionNum))); }
                }
                else { Game.SetError(new Error(73, string.Format("No options present for \"{0}\"", eventObject.Name))); }
            }
            else
            {
                Game.SetError(new Error(73, string.Format("Invalid Event Input \"{0}\"", eventID)));
                validOption = 0;
            }
            return validOption;
        }

        /// <summary>
        /// returns the # of current events for the turn
        /// </summary>
        /// <returns></returns>
        public int GetNumCurrentEvents()
        { return listFollCurrentEvents.Count(); }

        /// <summary>
        /// query whether an event exists based on ID
        /// </summary>
        /// <param name="eventID"></param>
        /// <returns></returns>
        public bool CheckEvent(int eventID)
        {
            if (dictFollowerEvents.ContainsKey(eventID))
            { return true; }
            return false;
        }

        /// <summary>
        /// query whetehr a result exists based on ID
        /// </summary>
        /// <param name="resultID"></param>
        /// <returns></returns>
        public bool CheckResult(int resultID)
        {
            if (dictResults.ContainsKey(resultID))
            { return true; }
            return false;
        }

        /// <summary>
        /// query whether an archetype exists based on ID
        /// </summary>
        /// <param name="arcID"></param>
        /// <returns></returns>
        public bool CheckArchetype(int arcID)
        {
            if (dictArchetypes.ContainsKey(arcID))
            { return true; }
            return false;
        }

        /// <summary>
        /// Returns a story to be used by Director
        /// </summary>
        /// <param name="storyID"></param>
        /// <returns></returns>
        private Story SetStory(int storyID)
        {
            if (dictStories.TryGetValue(storyID, out story))
            { return story; }
            return null;
        }

        /// <summary>
        /// Using Story, set up archetypes for geo / loc / road's (doesn't apply to player)
        /// </summary>
        public void InitialiseArchetypes()
        {
            int refID, arcID;
            //GeoCluster archetypes
            Archetype arcSea = GetArchetype(story.Arc_Geo_Sea);
            Archetype arcMountain = GetArchetype(story.Arc_Geo_Mountain);
            Archetype arcForest = GetArchetype(story.Arc_Geo_Forest);
            //Initialise active GeoClusters (ones with roads through them & all sea clusters)
            foreach (int geoID in listOfActiveGeoClusters)
            {
                //get cluster
                GeoCluster cluster = Game.world.GetGeoCluster(geoID);
                if (cluster != null)
                {
                    switch (cluster.Terrain)
                    {
                        case Cluster.Sea:
                            if (arcSea != null)
                            {
                                //only applies to Large sea zones with 2 or more ports (no events needed for the others)
                                if (cluster.GetNumPorts() > 1 && cluster.Type == GeoType.Large_Sea)
                                {
                                    //% chance of applying to each instance
                                    if (rnd.Next(100) < arcSea.Chance)
                                    {
                                        //copy Archetype event ID's across to GeoCluster -> Player events only (followers don't travel by sea)
                                        cluster.SetPlayerEvents(arcSea.GetEvents());
                                        cluster.Archetype = arcSea.ArcID;
                                        //debug
                                        Game.logStart?.Write(string.Format(" {0}, geoID {1}, has been initialised with \"{2}\", arcID {3}", cluster.Name, cluster.GeoID, arcSea.Name, arcSea.ArcID));
                                    }
                                }
                            }
                            break;
                        case Cluster.Mountain:
                            if (arcMountain != null)
                            {
                                //% chance of applying to each instance
                                if (rnd.Next(100) < arcMountain.Chance)
                                {
                                    //copy Archetype event ID's across to GeoCluster
                                    cluster.SetFollowerEvents(arcMountain.GetEvents());
                                    cluster.Archetype = arcMountain.ArcID;
                                    //debug
                                    Game.logStart?.Write(string.Format(" {0}, geoID {1}, has been initialised with \"{2}\", arcID {3}", cluster.Name, cluster.GeoID, arcMountain.Name, arcMountain.ArcID));
                                }
                            }
                            break;
                        case Cluster.Forest:
                            if (arcForest != null)
                            {
                                //% chance of applying to each instance
                                if (rnd.Next(100) < arcForest.Chance)
                                {
                                    //copy Archetype event ID's across to GeoCluster
                                    cluster.SetFollowerEvents(arcForest.GetEvents());
                                    cluster.Archetype = arcForest.ArcID;
                                    //debug
                                    Game.logStart?.Write(string.Format(" {0}, geoID {1}, has been initialised with \"{2}\", arcID {3}", cluster.Name, cluster.GeoID, arcForest.Name, arcForest.ArcID));
                                }
                            }
                            break;
                    }
                }
            }

            //Road archetypes
            Archetype arcNormal = GetArchetype(story.Arc_Road_Normal);
            Archetype arcKings = GetArchetype(story.Arc_Road_Kings);
            Archetype arcConnector = GetArchetype(story.Arc_Road_Connector);
            //Initialise Roads
            if (arcNormal != null)
            {
                listArcFollRoadEventsNormal.AddRange(arcNormal.GetEvents());
                Game.logStart?.Write(string.Format("Normal roads have been initialised with \"{0}\", arcID {1}", arcNormal.Name, arcNormal.ArcID));
            }
            if (arcKings != null)
            {
                listArcFollRoadEventsKings.AddRange(arcKings.GetEvents());
                Game.logStart?.Write(string.Format("Kings roads have been initialised with \"{0}\", arcID {1}", arcKings.Name, arcKings.ArcID));
            }
            if (arcConnector != null)
            {
                listArcFollRoadEventsConnector.AddRange(arcConnector.GetEvents());
                Game.logStart?.Write(string.Format("Connector roads have been initialised with \"{0}\", arcID {1}", arcConnector.Name, arcConnector.ArcID));
            }

            //Capital archetype
            Archetype arcCapital = GetArchetype(story.Arc_Loc_Capital);
            //Initialise Capital
            if (arcCapital != null)
            {
                listArcFollCapitalEvents.AddRange(arcCapital.GetEvents());
                Game.logStart?.Write(string.Format("The Capital at KingsKeep has been initialised with \"{0}\", arcID {1}", arcCapital.Name, arcCapital.ArcID));
            }

            //Location archetypes
            Archetype arcMajor = GetArchetype(story.Arc_Loc_Major);
            Archetype arcMinor = GetArchetype(story.Arc_Loc_Minor);
            Archetype arcInn = GetArchetype(story.Arc_Loc_Inn);
            //Initialise Locations
            Dictionary<int, Location> tempLocations = Game.network.GetLocations();

            foreach (var loc in tempLocations)
            {
                refID = loc.Value.RefID;
                //location present (excludes capital)
                if (refID > 0)
                {
                    if (refID < 100)
                    {
                        //Major House
                        if (arcMajor != null)
                        {
                            //% chance of applying to each instance
                            if (rnd.Next(100) < arcMajor.Chance)
                            {
                                //copy Archetype event ID's across to GeoCluster
                                loc.Value.SetEvents(arcMajor.GetEvents());
                                loc.Value.ArcID = arcMajor.ArcID;
                                //debug
                                Game.logStart?.Write(string.Format("{0}, locID {1}, has been initialised with \"{2}\", arcID {3}", Game.world.GetLocationName(loc.Key), loc.Key, arcMajor.Name, arcMajor.ArcID));
                            }
                        }

                    }
                    else if (refID >= 100 && refID < 1000)
                    {
                        //Minor House
                        if (arcMinor != null)
                        {
                            //% chance of applying to each instance
                            if (rnd.Next(100) < arcMinor.Chance)
                            {
                                //copy Archetype event ID's across to GeoCluster
                                loc.Value.SetEvents(arcMinor.GetEvents());
                                loc.Value.ArcID = arcMinor.ArcID;
                                //debug
                                Game.logStart?.Write(string.Format("{0}, locID {1}, has been initialised with \"{2}\", arcID {3}", Game.world.GetLocationName(loc.Key), loc.Key, arcMinor.Name, arcMinor.ArcID));
                            }
                        }
                    }
                    else if (refID >= 1000)
                    {
                        //Inn
                        if (arcInn != null)
                        {
                            //% chance of applying to each instance
                            if (rnd.Next(100) < arcInn.Chance)
                            {
                                //copy Archetype event ID's across to GeoCluster
                                loc.Value.SetEvents(arcInn.GetEvents());
                                loc.Value.ArcID = arcInn.ArcID;
                                //debug
                                Game.logStart?.Write(string.Format("{0}, locID {1}, has been initialised with \"{2}\", arcID {3}", Game.world.GetLocationName(loc.Key), loc.Key, arcInn.Name, arcInn.ArcID));
                            }
                        }
                    }
                    //House specific archetypes
                    House house = Game.world.GetHouse(refID);
                    arcID = house.ArcID;
                    if (arcID > 0)
                    {
                        Archetype archetype = GetArchetype(arcID);
                        house.SetFollowerEvents(archetype.GetEvents());
                        //debug
                        Game.logStart?.Write(string.Format("House {0}, refID {1}, has been initialised with \"{2}\", arcID {3}", house.Name, house.RefID, archetype.Name, archetype.ArcID));
                    }
                }
            }
            //Player & Follower specific archetypes
            Dictionary<int, Active> tempActiveActors = Game.world.GetAllActiveActors();
            if (tempActiveActors != null)
            {
                foreach (var actor in tempActiveActors)
                {
                    arcID = actor.Value.ArcID;
                    if (arcID > 0)
                    {
                        Archetype archetype = GetArchetype(arcID);
                        actor.Value.SetEvents(archetype.GetEvents());
                        //debug
                        Game.logStart?.Write(string.Format("\"{0}\", AiD {1}, has been initialised with \"{2}\", arcID {3}", actor.Value.Name, actor.Value.ActID, archetype.Name, archetype.ArcID));
                    }
                }
            }
            else { Game.SetError(new Error(64, "Invalid Dictionary Input (null)")); }
        }

        /// <summary>
        /// Returns an Archetype from dictionary
        /// </summary>
        /// <param name="arcID"></param>
        /// <returns></returns>
        private Archetype GetArchetype(int arcID)
        {
            Archetype arc = new Archetype();
            if (dictArchetypes.TryGetValue(arcID, out arc))
            { return arc; }
            return null;
        }

        /// <summary>
        /// Returns a Result from dictionary
        /// </summary>
        /// <param name="resultID"></param>
        /// <returns></returns>
        internal Result GetResult(int resultID)
        {
            Result result = new Result();
            if (dictResults.TryGetValue(resultID, out result))
            { return result; }
            return null;
        }

        /// <summary>
        /// Returns name of Archetype based on arcID. Null if not found.
        /// </summary>
        /// <param name="arcID"></param>
        /// <returns></returns>
        public string GetArchetypeName(int arcID)
        {
            Archetype arc = new Archetype();
            if (dictArchetypes.TryGetValue(arcID, out arc))
            { return arc.Name; }
            return null;
        }


        /// <summary>
        /// Set data in arrayOfGameStates -> value is an absolute value, not a modifier. Call GetGameState & ChangeData first.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="state"></param>
        /// <param name="value"></param>
        /// <param name="setChange">True if you want color highlight on UI of change</param>
        public void SetGameState(GameState point, DataState state, int value, bool setChange = false)
        {
            if (point <= GameState.Count && state <= DataState.Count)
            {
                arrayOfGameStates[(int)point, (int)state] = value;
                //change - will show color highlight on UI
                if (setChange == true)
                {
                    if (state == DataState.Good) { arrayOfGameStates[(int)point, (int)DataState.Change] = 1; }
                    else if (state == DataState.Bad) { arrayOfGameStates[(int)point, (int)DataState.Change] = -1; }
                }
            }
            else
            { Game.SetError(new Error(75, "Invalid GameState, or DataState, Input (exceeds enum)")); }
        }

        /// <summary>
        /// Get a game state. Returns -999 if not found
        /// </summary>
        /// <param name="point"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public int GetGameState(GameState point, DataState state)
        {
            if (point <= GameState.Count && state <= DataState.Count)
            { return arrayOfGameStates[(int)point, (int)state]; }
            else
            { Game.SetError(new Error(75, "Invalid Input (exceeds enum)")); }
            return -999;
        }

        /// <summary>
        /// returns a % value for a Game state based on proportion of good vs. bad
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public int CheckGameState(GameState point)
        {
            int returnValue = 0;
            float good = arrayOfGameStates[(int)point, (int)DataState.Good];
            float bad = arrayOfGameStates[(int)point, (int)DataState.Bad];
            float difference = good - bad;
            if (difference == 0 || good + bad == 0) { returnValue = 50; }
            else
            {
                float percentage = good / (good + bad) * 100;
                percentage = Math.Min(100, percentage);
                percentage = Math.Max(0, percentage);
                returnValue = Convert.ToInt32(percentage);
            }
            return returnValue;
        }

        /// <summary>
        /// returns change state (-ve for an increase in bad, +ve for increase in good, 0 for none)
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public int CheckGameStateChange(GameState point)
        {
            int change = arrayOfGameStates[(int)point, (int)DataState.Change];
            //zero out any change, once queried
            if (change != 0)
            { arrayOfGameStates[(int)point, (int)DataState.Change] = 0; }
            return change;
        }

        /// <summary>
        /// checks event timers each turn
        /// </summary>
        internal void CheckEventTimers()
        {
            Game.logTurn?.Write("--- CheckEventTimers (Director.cs)");
            //PLAYER events
            foreach (var eventObject in dictPlayerEvents)
            {
                switch (eventObject.Value.Status)
                {
                    case EventStatus.Active:
                        //decrement DORMANT timer, if present
                        if (eventObject.Value.TimerDormant > 0)
                        {
                            eventObject.Value.TimerDormant--;
                            Game.logTurn?.Write(string.Format(" Event \"{0}\" Dormant Timer decremented to {1}", eventObject.Value.Name, eventObject.Value.TimerDormant));
                            //if dormant timer has run down to 0, the event is no longer active
                            if (eventObject.Value.TimerDormant == 0)
                            {
                                eventObject.Value.Status = EventStatus.Dormant;
                                Game.logTurn?.Write(string.Format(" Event \"{0}\" Dormant Timer has run down to Zero. Event is now {1}", eventObject.Value.Name, eventObject.Value.Status));
                            }
                        }
                        //decrement Cool down timers
                        if (eventObject.Value.TimerCoolDown > 0)
                        {
                            eventObject.Value.TimerCoolDown--;
                            Game.logTurn?.Write(string.Format(" \"{0}\" event, Cooldown Timer decremented from {1} to {2}", eventObject.Value.Name, eventObject.Value.TimerCoolDown + 1,
                                eventObject.Value.TimerCoolDown));
                        }
                        break;
                    case EventStatus.Live:
                        //decrement Live timer, if present
                        if (eventObject.Value.TimerLive > 0)
                        {
                            eventObject.Value.TimerLive--;
                            Game.logTurn?.Write(string.Format(" Event \"{0}\" Live Timer decremented to {1}", eventObject.Value.Name, eventObject.Value.TimerLive));
                            //if Lie timer has run down to 0, the event goes active
                            if (eventObject.Value.TimerLive == 0)
                            {
                                eventObject.Value.Status = EventStatus.Active;
                                Game.logTurn?.Write(string.Format(" Event \"{0}\" Live Timer has run down to Zero. Event is now {1}", eventObject.Value.Name, eventObject.Value.Status));
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Changes Player events from one status to another
        /// </summary>
        /// <param name="targetEventID"></param>
        /// <param name="newStatus"></param>
        private string ChangePlayerEventStatus(int targetEventID, EventStatus newStatus)
        {
            string resultText = "";
            //get player event
            EventPlayer eventObject = GetPlayerEvent(targetEventID);
            if (eventObject != null)
            {
                if (newStatus > EventStatus.None)
                {
                    if (eventObject.Status != newStatus)
                    {
                        Game.logTurn?.Write("--- ChangePlayerEventStatus (Director.cs)");
                        eventObject.Status = newStatus;
                        resultText = string.Format(" \"{0}\", eventPID {1}, has changed status to \"{2}\"", eventObject.Name, eventObject.EventPID, newStatus);
                        Game.logTurn?.Write(resultText);
                    }
                    else
                    { Game.SetError(new Error(78, string.Format("\"{0}\" identical to existing, eventID {1} status unchanged", newStatus, targetEventID))); }
                }
                else
                { Game.SetError(new Error(78, string.Format("Invalid newStatus {0}, eventID {1} status unchanged", newStatus, targetEventID))); }
            }
            else
            { Game.SetError(new Error(78, string.Format("Target Event ID {0} not found, event status unchanged", targetEventID))); }
            return resultText;
        }

        /// <summary>
        /// Player Gains, or Loses, an Item (from Opponent)
        /// </summary>
        /// <param name="calc"></param>
        /// <param name="data"></param>
        /// <param name="oppID"></param>
        /// <returns></returns>
        private string ChangePlayerItemStatus(EventCalc calc, int data, int oppID)
        {
            string tempText;
            string resultText = "";
            Active player = Game.world.GetActiveActor(1);
            Actor opponent = Game.world.GetAnyActor(oppID);
            if (player != null && opponent != null)
            {
                int rndIndex;
                //what type of item filter -> +ve then Active Items, -ve Passive Items, '0' for all
                PossItemType filter = PossItemType.Both;
                if (data > 0) { filter = PossItemType.Active; }
                else if (data < 0) { filter = PossItemType.Passive; }
                //Gain an item -> if your Opponent has one
                if (calc == EventCalc.Add && opponent.CheckItems(filter) == true)
                {
                    List<int> tempItems = opponent.GetItems(filter);
                    rndIndex = rnd.Next(tempItems.Count);
                    int possID = tempItems[rndIndex];
                    if (possID > 0)
                    {
                        player.AddItem(possID);
                        opponent.RemoveItem(possID);
                        Item item = Game.world.GetItem(possID);
                        resultText = string.Format("You have gained possession of \"{0}\", itemID {1}, from {2} {3} \"{4}\", ActID {5}", item.Description, item.ItemID, opponent.Title,
                            opponent.Name, opponent.Handle, opponent.ActID);
                        tempText = string.Format("{0} {1} has lost possession of \"{2}\", itemID {1}, to the Ursurper", opponent.Title, opponent.Name, item.Description, item.ItemID);
                        Game.world.SetCurrentRecord(new Record(tempText, opponent.ActID, opponent.LocID, CurrentActorIncident.Event));
                        Game.logTurn?.Write(tempText);

                    }
                }
                //Lose an item -> if you have one
                if (calc == EventCalc.Subtract && player.CheckItems(filter) == true)
                {
                    List<int> tempItems = player.GetItems(filter);
                    rndIndex = rnd.Next(tempItems.Count);
                    int possID = tempItems[rndIndex];
                    if (possID > 0)
                    {
                        opponent.AddItem(possID);
                        player.RemoveItem(possID);
                        Item item = Game.world.GetItem(possID);
                        resultText = string.Format("You have lost possession of \"{0}\", itemID {1}, to {2} {3} \"{4}\", ActID {5}", item.Description, item.ItemID, opponent.Title,
                            opponent.Name, opponent.Handle, opponent.ActID);
                        tempText = string.Format("{0} {1} has gained possession of \"{2}\", itemID {3}, from the Ursurper", opponent.Title, opponent.Name, item.Description, item.ItemID);
                        Game.world.SetCurrentRecord(new Record(tempText, opponent.ActID, opponent.LocID, CurrentActorIncident.Event));
                        Game.logTurn?.Write(tempText);
                    }
                }
            }
            else { Game.SetError(new Error(207, "Invalid Player or Opponent (null)")); }
            return resultText;
        }

        /// <summary>
        /// Player issues a Promise to a Passive actor (promises come due in Act II)
        /// </summary>
        /// <param name="actorID"></param>
        /// <param name="type"></param>
        /// <param name="strength"></param>
        /// <returns></returns>
        private string ChangePlayerPromiseStatus(int actorID, PossPromiseType type, int strength)
        {
            string resultText = "";
            Active player = Game.world.GetActiveActor(1);
            if (player != null)
            {
                //check valid NPC character who receives the promise
                if (actorID > 10)
                {
                    Passive actor = Game.world.GetPassiveActor(actorID);
                    if (actor != null)
                    {
                        string details = string.Format("The Ursurper has promised {0} {1}, ActID {2}, {3}", actor.Title, actor.Name, actor.ActID, actor.DesireText);
                        Promise promise = new Promise(type, actorID, details, strength);
                        //add to Possessions dictionary
                        if (Game.world.AddPossession(promise.PossID, promise) == true)
                        {
                            //add PossID to Player & NPC
                            player.AddPromise(promise.PossID);
                            actor.AddPromise(promise.PossID);
                            actor.Satisfied = true;
                            Game.variable.ChangeValue(GameVar.Promises_Num, 1, EventCalc.Add);
                            resultText = $"{player.Title} {player.Name} promises {actor.Title} {actor.Name}, ActID {actor.ActID}, that they will attend to their desire for {actor.DesireText}";
                        }
                        else { Game.SetError(new Error(230, "Error in AddPossession -> Promise not created")); }
                    }
                    else { Game.SetError(new Error(230, "Invalid NPC actor (null) -> Promise not created")); }
                }
                else { Game.SetError(new Error(230, $"Invalid Passive actorID \"{actorID}\" -> Promise not created")); }
            }
            else { Game.SetError(new Error(230, "Invalid Player (null)")); }
            return resultText;
        }


        /// <summary>
        /// Passive Actor issues a favour to the Player (can be cashed in at any time)
        /// </summary>
        /// <param name="actorID"></param>
        /// <param name="strength"></param>
        /// <returns></returns>
        private string ChangePlayerFavourStatus(int actorID, int strength)
        {
            string resultText = "";
            Player player = (Player)Game.world.GetActiveActor(1);
            if (player != null)
            {
                //check valid NPC character who receives the promise
                if (actorID > 10)
                {
                    Passive actor = Game.world.GetPassiveActor(actorID);
                    if (actor != null)
                    {
                        if (strength > 0 && strength < 6)
                        {
                            resultText = $"{actor.Title} {actor.Name} \"{actor.Handle}\" has agreed to provide a level {strength} Favour";
                            Favour favour = new Favour(resultText, strength, actorID);
                            //add to Possessions dictionary
                            if (Game.world.AddPossession(favour.PossID, favour) == true)
                            {
                                //add favour to Player
                                player.AddFavour(favour.PossID);
                            }
                        }
                        else { Game.SetError(new Error(239, $"Invalid strength input (\"{strength}\"), must be between 1 & 5 -> Favour not created")); }
                    }
                    else { Game.SetError(new Error(239, "Invalid NPC actor (null) -> Favour not created")); }
                }
                else { Game.SetError(new Error(239, $"Invalid Passive actorID \"{actorID}\" -> Favour not created")); }
            }
            else { Game.SetError(new Error(239, "Invalid Player (null)")); }
            return resultText;
        }

        /// <summary>
        /// Player uses an introduction to gain access to a House court audience
        /// </summary>
        /// <param name="refID"></param>
        /// <returns></returns>
        private string ChangePlayerIntroductionStatus(int refID)
        {
            string resultText = "";
            Player player = (Player)Game.world.GetActiveActor(1);
            if (player != null)
            {
                player.IntroPresented = true;
                player.DeleteIntroduction(refID);
                resultText = $"You have used your introduction to gain access to the court of House \"{Game.world.GetHouseName(refID)}\"";
            }
            else { Game.SetError(new Error(239, "Invalid Player (null)")); }
            return resultText;
        }

        /// <summary>
        /// Change an NPC actor's relationship with Player
        /// </summary>
        /// <param name=""></param>
        /// <param name=""></param>
        /// <param name=""></param>
        /// <returns></returns>
        private string ChangePlayerRelStatus(int actorID, int amount, EventCalc apply, string description, string tag)
        {
            string resultText = "";
            if (actorID > 1)
            {
                Actor person = Game.world.GetAnyActor(actorID);
                if (person != null)
                {
                    int change = Math.Abs(amount);
                    if (apply == EventCalc.Subtract) { amount *= -1; } // if it's not Subtract then default to 'Add'
                    person.AddRelEventPlyr(new Relation(description, tag, amount));
                    resultText = string.Format("{0} {1}'s relationship with you has {2} by {3}{4}", person.Title, person.Name,
                       amount > 0 ? "improved" : "worsened", amount > 0 ? "+" : "", amount);
                }
                else { Game.SetError(new Error(231, $"Invalid person (null), ActID {actorID}")); }
            }
            else { Game.SetError(new Error(231, $"Invalid actorID \"{actorID}\"")); }
            return resultText;
        }

        /// <summary>
        /// Initiate a Player's sea voyage
        /// </summary>
        /// <param name="destID"></param>
        /// <param name="voyageTime"></param>
        /// <returns></returns>
        private string ChangePlayerVoyageStatus(int destID, int voyageTime, bool safePassage)
        {
            string resultText = "";
            Active player = Game.world.GetActiveActor(1);
            if (player != null)
            {
                if (destID > 0)
                {
                    if (voyageTime > 0)
                    {
                        int currentLocID = player.LocID;

                        player.LocID = destID;
                        player.VoyageTimer = voyageTime;
                        player.Status = ActorStatus.AtSea;
                        player.VoyageSafe = safePassage;
                        player.ShipName = Game.history.GetShipName(player.VoyageSafe);
                        player.SeaName = Game.network.GetSeaName(currentLocID, destID);
                        string locNameOrigin = Game.world.GetLocationName(currentLocID);
                        string locNameDestination = Game.world.GetLocationName(destID);
                        resultText = string.Format("{0} {1} boards the S.S \"{2}\" at {3}, bound for {4}. Estimated voyage time {5} day{6}", player.Title, player.Name, player.ShipName,
                            locNameOrigin, locNameDestination, player.VoyageTimer, player.VoyageTimer != 1 ? "s" : "");
                    }
                    else { Game.SetError(new Error(217, "Invalid voyageTime (zero, or less)")); }
                }
                else { Game.SetError(new Error(217, "Invalid DestinationID (zero, or less)")); }
            }
            else { Game.SetError(new Error(217, "Invalid Player (null)")); }
            return resultText;
        }

        /// <summary>
        /// Initiates a Player's adrift situation
        /// </summary>
        /// <param name="setAdrift"></param>
        /// <param name="deathTimer"></param>
        /// <param name="shipSunk"></param>
        /// <returns></returns>
        private string ChangePlayerAdriftStatus(int deathTimer, bool shipSunk)
        {
            string resultText = "";
            Active player = Game.world.GetActiveActor(1);
            if (player != null)
            {
                if (deathTimer > 1)
                {
                    player.DeathTimer = deathTimer;
                    player.Status = ActorStatus.Adrift;
                    resultText = $"{player.Name} has been cast Adrift in {player.SeaName}. Survival time {player.DeathTimer} days";
                    if (shipSunk == true)
                    {
                        //find and remove ship from list
                        string shipName = player.ShipName;
                        if (String.IsNullOrEmpty(shipName) == false)
                        {
                            List<string> tempList = null;
                            if (player.VoyageSafe == true)
                            { tempList = Game.history.GetShipNamesSafe(); }
                            else { tempList = Game.history.GetShipNamesRisky(); }
                            if (tempList != null)
                            {
                                //loop list, find ship name and remove
                                for (int i = 0; i < tempList.Count; i++)
                                {
                                    if (shipName.Equals(tempList[i]) == true)
                                    {
                                        tempList.RemoveAt(i);
                                        Game.logTurn.Write($"[Notification] S.S \"{shipName}\" has been removed from the list of Ship Names");
                                        break;
                                    }
                                }
                            }
                            else { Game.SetError(new Error(221, "Invalid tempList (null)")); }
                        }
                        else { Game.SetError(new Error(221, "Invalid Ship Name (null or Empty)")); }
                    }
                }
                else { player.DeathTimer = 10; Game.SetError(new Error(221, "Invalid Death Timer ( must be > 1) -> given default value of 10")); }
            }
            else { Game.SetError(new Error(221, "Invalid Player (null)")); }
            return resultText;
        }


        /// <summary>
        /// change a GameVar
        /// </summary>
        /// <param name="gamevar"></param>
        /// <param name="amount"></param>
        /// <param name="apply"></param>
        /// <returns></returns>
        public string ChangeGameVarStatus(GameVar gamevar, int amount, EventCalc apply)
        {
            string resultText = "";
            int origValue = Game.variable.GetValue(gamevar);
            Game.variable.ChangeValue(gamevar, amount, apply);
            int newValue = Game.variable.GetValue(gamevar);
            resultText = $"GameVar {gamevar} has changed from {origValue} to {newValue}";
            return resultText;
        }

        /// <summary>
        /// Player rescued from drifting around the ocean by a passing merchant vessel. Could be a safe, or unsafe, ship.
        /// </summary>
        /// <param name="safeShip"></param>
        /// <returns></returns>
        private string ChangePlayerRescuedStatus(bool safeShip)
        {
            string resultText = "";
            Player player = Game.world.GetPlayer();
            if (player != null)
            {
                player.DeathTimer = 999;
                player.VoyageTimer--;
                player.VoyageTimer = Math.Max(1, player.VoyageTimer);
                player.VoyageSafe = safeShip;
                player.ShipName = Game.history.GetShipName(player.VoyageSafe);
                player.Status = ActorStatus.AtSea;
                resultText = string.Format("{0} has been rescued by the S.S \"{1}\", bound for {2}, arriving in {3} day{4}", player.Name, player.ShipName,
                    Game.world.GetLocationName(player.LocID), player.VoyageTimer, player.VoyageTimer != 1 ? "s" : "");
            }
            else { Game.SetError(new Error(222, "Invalid Player (null)")); }
            return resultText;
        }

        /// <summary>
        /// Player enters/leaves a SafeHouse (status +ve -> enter, status -ve -> depart)
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        private string ChangePlayerSafeHouseStatus(int status)
        {
            string resultText = "";
            Player player = Game.world.GetPlayer();
            int refID = Game.world.ConvertLocToRef(player.LocID);
            House house = Game.world.GetHouse(refID);
            if (house != null)
            {
                if (house.SafeHouse > 0)
                {
                    if (player != null)
                    {
                        if (status > 0)
                        {
                            //Enter a SafeHouse
                            player.Conceal = ActorConceal.SafeHouse;
                            player.ConcealLevel = house.SafeHouse;
                            player.ConcealText = "Mr Magoo's Basement";
                            resultText = $"{player.Title} {player.Name} has found refuge in the safe house at {house.LocName} ({house.SafeHouse} stars)";
                        }
                        else if (status < 0)
                        {
                            //Depart a SafeHouse
                            player.Conceal = ActorConceal.None;
                            player.ConcealLevel = 0;
                            player.ConcealText = "None";
                            resultText = $"{player.Title} {player.Name} has left the refuge of the safe house at {house.LocName} ({house.SafeHouse} stars)";
                        }
                        else { Game.SetError(new Error(250, $"Invalid status \"{status}\" (can't be zero) -> SafeHouse Status NOT changed")); }
                    }
                    else { Game.SetError(new Error(250, "Invalid Player (null)")); }
                }
                else { Game.SetError(new Error(250, "House SafeHouse has a zero, or less, value. -> SafeHouse Status NOT changed")); }
            }
            else { Game.SetError(new Error(250, "Invalid house (null)")); }
            Game.logTurn?.Write(resultText);
            return resultText;
        }


        private string ChangePlayerDisguiseStatus(int actorID)
        {
            Game.logTurn?.Write("--- ChangePlayerDisguiseStatus (Director.cs)");
            string resultText = "";
            Player player = Game.world.GetPlayer();
            if (player != null)
            {
                //Get NPC advisor
                Passive passive = Game.world.GetPassiveActor(actorID);
                if (passive != null)
                {
                    if (passive is Advisor)
                    {
                        Advisor advisor = passive as Advisor;
                        //Get next disguise in Advisor's list
                        int possID = advisor.GetNextDisguise();
                        if (possID > 0)
                        {
                            //Get Disguise
                            Possession possession = Game.world.GetPossession(possID);
                            if (possession != null)
                            {
                                if (possession is Disguise)
                                {
                                    Disguise disguise = possession as Disguise;
                                    //give to player
                                    player.ConcealDisguise = possID;
                                    disguise.WhoHas = 1;
                                    resultText = $"{player.Name} has obtained the disguise, \"{disguise.Description}\" ({disguise.Strength} stars), from {advisor.Title} {advisor.Name}";
                                    //delete disguise from advisor's list
                                    advisor.DeleteDisguise(possID);
                                    //advisor record
                                    Record record = new Record(resultText, advisor.ActID, advisor.LocID, CurrentActorIncident.Event);
                                    Game.world.SetCurrentRecord(record);
                                }
                                else { Game.SetError(new Error(254, $"Invalid possession (not a Disguise) from possID {possID}")); }
                            }
                            else { Game.SetError(new Error(254, $"Invalid possession (null) from possID {possID}")); }
                        }
                        else { Game.logTurn?.Write("[Alert] Invalid possID (zero, or less)"); }
                    }
                    else { Game.SetError(new Error(254, $"Invalid passive (NOT an advisor) from actorID {actorID}")); }
                }
                else { Game.SetError(new Error(254, $"Invalid passive actor (null) from actorID {actorID}")); }
            }
            else { Game.SetError(new Error(254, "Invalid Player (null)")); }
            Game.logTurn?.Write(resultText);
            return resultText;
        }

        /// <summary>
        /// Player gains  a single rumour from 'asking around for information'
        /// </summary>
        /// <returns></returns>
        private string ChangePlayerRumourStatus()
        {
            int rumourID;
            Game.logTurn?.Write("--- ChangePlayerRumourStatus (Director.cs)");
            string resultText = "";
            Rumour rumour = null;
            Player player = Game.world.GetPlayer();
            int refID = Game.world.ConvertLocToRef(player.LocID);
            if (player != null)
            {
                rumourID = GetRumourUnknown(refID, player.ActID);
                if (rumourID > 0)
                {
                    rumour = Game.world.GetRumour(rumourID);
                    if (rumour != null)
                    {
                        if (Game.world.AddRumourKnown(rumourID, rumour) == true)
                        {
                            Game.logTurn?.Write($"Rumour \"{rumour.Text}\", rumourID {rumour.RumourID}, added to dictRumoursKnown");
                            //deal with special cases
                            switch (rumour.Type)
                            {
                                case RumourType.Skill:
                                    if (rumour is RumourSkill)
                                    {
                                        //set relevant Passive actor skill to true (visible)
                                        RumourSkill rumourSkill = rumour as RumourSkill;
                                        Passive actor = Game.world.GetPassiveActor(rumourSkill.ActorID);
                                        if (actor != null)
                                        {
                                            if (actor.SetSkillKnownStatus(rumourSkill.Skill, true) == false)
                                            { Game.SetError(new Error(272, $"Invalid SkillType \"{rumourSkill.Skill}\" prevents change of Known status")); }
                                            else { Game.logTurn?.Write($"{actor.Title} {actor.Name}, ActID {actor.ActID}, has their {rumourSkill.Skill} changed to True"); }
                                        }
                                        else { Game.SetError(new Error(272, $"RumourSkill -> Invalid actor (null) for ActID {rumourSkill.ActorID}")); }
                                    }
                                    else { Game.SetError(new Error(272, $"Rumour Type doesn't match Rumour class (Skill) -> Skill not made Known, RumourID {rumour.RumourID}")); }
                                    break;
                                case RumourType.HouseRel:
                                    if (rumour is RumourHouseRel)
                                    {
                                        RumourHouseRel rumourHouse = rumour as RumourHouseRel;
                                        House house = Game.world.GetHouse(rumour.RefID);
                                        if (house != null)
                                        {
                                            //find Relationship
                                            List<Relation> listHouseRels = house.GetRelations();
                                            if (listHouseRels != null)
                                            {
                                                foreach (var relation in listHouseRels)
                                                {
                                                    if (relation.TrackerID == rumourHouse.TrackerID)
                                                    {
                                                        //set Relation to Known (so it's visible to the player)
                                                        relation.Known = true;
                                                        Game.logTurn?.Write($"[Notification -> HouseRel] \"{relation.Text}\" -> Known is True");
                                                        break;
                                                    }
                                                }
                                            }
                                            else { Game.SetError(new Error(272, "Invalid listHouseRels (null) -> HouseRel not made Known")); }
                                        }
                                        else { Game.SetError(new Error(272, $"Invalid house (null) for rumour.RefID {rumour.RefID} -> HouseRel not made Known")); }
                                    }
                                    else { Game.SetError(new Error(272, $"Rumour Type doesn't match Rumour class (HouseRel) -> HouseRel not made Known, RumourID {rumour.RumourID}")); }
                                    break;
                                case RumourType.Friends:
                                    if (rumour is RumourFriends)
                                    {
                                        House house = Game.world.GetHouse(rumour.RefID);
                                        if (house != null)
                                        { house.SetFriendsAndEnemies(true); }
                                        else { Game.SetError(new Error(272, $"Invalid house (null) for rumour.RefID {rumour.RefID} -> FriendsAndEnemies not made Known")); }
                                    }
                                    else { Game.SetError(new Error(272, $"Rumour Type doesn't match Rumour class (Friends) -> FriendsAndEnemies not made Known, RumourID {rumour.RumourID}")); }
                                    break;
                                case RumourType.Desire:
                                    if (rumour is RumourDesire)
                                    {
                                        RumourDesire rumourDesire = rumour as RumourDesire;
                                        Passive actor = Game.world.GetPassiveActor(rumourDesire.Data);
                                        if (actor != null)
                                        { actor.SetDesireKnown(true); }
                                        else { Game.SetError(new Error(272, $"Invalid actor (null) for rumourDesire.ActID {rumourDesire.Data} -> DesireKnown unchanged")); }
                                    }
                                    break;
                            }
                            //end switch
                        }
                        else { Game.SetError(new Error(272, $"RumourID {rumourID} failed to be added to dictRumoursKnown")); }
                    }
                    else { Game.SetError(new Error(272, $"Invalid rumour (null) for rumourID {rumourID} -> not added to dictRumoursKnown")); }
                }
                else { Game.logTurn?.Write("[Notification] RumourID zero or less -> not added to dictRumoursKnown"); }
            }
            else { Game.SetError(new Error(272, "Invalid Player (null)")); }
            if (rumour != null) { resultText = $"{rumour.Text}"; }
            Game.logTurn?.Write(resultText);
            return resultText;
        }


        /// <summary>
        /// Follower recruited from an Inn.
        /// </summary>
        /// <returns></returns>
        private string ChangeFollowerStatus(int actID, int locID)
        {
            string resultText = "";
            //get follower
            Follower follower = null;
            if (actID > 1 && actID < 10)
            {
                List<Active> listRemainingFollowers = Game.history.GetActiveActors();
                foreach (Active actor in listRemainingFollowers)
                {
                    if (actor.ActID == actID)
                    {
                        follower = (Follower)actor;
                        //crow set to 100% as at same location as Player
                        follower.CrowDistance = 0;
                        follower.CrowChance = 100;
                        follower.CrowBonus = 0;
                        follower.Activated = true; //can be given orders this turn
                        break;
                    }
                }
                //Follower found
                if (follower != null)
                {
                    //Add to Location
                    if (locID > 0)
                    {
                        Location loc = Game.network.GetLocation(locID);
                        loc.AddActor(follower.ActID);
                        follower.LocID = locID;
                        follower.LastKnownLocID = locID;
                        follower.SetActorPosition(loc.GetPosition());
                    }
                    else { Game.SetError(new Error(227, $"Invalid locID \"{locID}\" -> Follower not added to Location")); }
                    //Add to dictionaries
                    Game.world.SetActiveActor(follower);
                    //Remove from InnHouse listOfFollowers
                    int refID = Game.world.ConvertLocToRef(locID);
                    if (refID > 0)
                    {
                        InnHouse inn = (InnHouse)Game.world.GetHouse(refID);
                        if (inn != null)
                        { inn.RemoveFollower(actID); Game.logTurn?.Write($"{follower.Name} removed from \"{inn.Name}\" listOfFollowers"); }
                        else { Game.SetError(new Error(227, "Invalid inn (null)")); }
                    }
                    else { Game.SetError(new Error(227, $"Invalid refID \"{refID}\" -> Follower not removed from InnHouse listOfFollowers")); }
                    //remove from listActiveActors
                    List<Active> listActiveActors = Game.history.GetActiveActors();
                    bool removed = false;
                    for (int i = 0; i < listActiveActors.Count; i++)
                    {
                        Active actor = listActiveActors[i];
                        if (actor.ActID == actID)
                        {
                            Game.logTurn?.Write($"{actor.Name}, ActID {actor.ActID} has been removed from History.cs listOfActiveActors");
                            listActiveActors.RemoveAt(i);
                            removed = true;
                            break;
                        }
                    }
                    if (removed == false)
                    { Game.SetError(new Error(227, $"ActID {actID} wasn't found in listActiveActors -> Not removed")); }
                    //return string
                    resultText = string.Format("\"{0}\", ActID {1}, has joined your cause at \"{2}\"", follower.Name, follower.ActID, Game.world.GetLocationName(locID));
                }
                else { Game.SetError(new Error(227, "Invalid Player (null)")); }
            }
            else { Game.SetError(new Error(227, $"Invalid actID \"{actID}\" -> Follower not created")); }
            return resultText;
        }

        /// <summary>
        /// Change the voyage time for a Player's sea passage (increase or decrease)
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="calc"></param>
        /// <returns></returns>
        private string ChangePlayerVoyageTime(int amount, EventCalc calc)
        {
            string resultText = "";
            Active player = Game.world.GetActiveActor(1);
            if (player != null)
            {
                int voyageTime = player.VoyageTimer;
                switch (calc)
                {
                    case EventCalc.Add:
                        voyageTime += amount;
                        resultText = string.Format("The voyage of the S.S \"{0}\" has been increased by {1} day{2} to {3} days", player.ShipName, amount,
                            amount != 1 ? "s" : "", voyageTime);
                        break;
                    case EventCalc.Subtract:
                        voyageTime -= amount;
                        voyageTime = Math.Max(1, voyageTime);
                        resultText = string.Format("The voyage of the S.S \"{0}\" has been reduced by {1} day{2} to {3} days", player.ShipName, amount,
                            amount != 1 ? "s" : "", voyageTime);
                        break;
                    default:
                        Game.SetError(new Error(218, string.Format("Invalid EventCalc \"{0}\" -> voyage time change invalid")));
                        break;
                }
                player.VoyageTimer = voyageTime;
            }
            return resultText;
        }

        /// <summary>
        /// Change the death timer for the Player when in an Adrift or Dungeon situation (increase/decrease)
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="calc"></param>
        /// <returns></returns>
        private string ChangePlayerDeathTimer(int amount, EventCalc calc)
        {
            string resultText = "";
            Active player = Game.world.GetActiveActor(1);
            if (player != null)
            {
                int deathTime = player.DeathTimer;
                switch (calc)
                {
                    case EventCalc.Add:
                        deathTime += amount;
                        resultText = $"Survival time has been increased by {amount} to {deathTime}";
                        break;
                    case EventCalc.Subtract:
                        deathTime -= amount;
                        deathTime = Math.Max(1, deathTime);
                        resultText = $"Survival time has been decreased by {amount} to {deathTime}";
                        break;
                    default:
                        Game.SetError(new Error(218, string.Format("Invalid EventCalc \"{0}\" -> Death time change invalid")));
                        break;
                }
                player.DeathTimer = deathTime;
            }
            return resultText;
        }

        /// <summary>
        /// Change Player event Timer 
        /// </summary>
        /// <param name="outcome"></param>
        private string ChangePlayerEventTimer(OutEventTimer outcome)
        {
            string resultText = "";
            if (outcome != null)
            {
                try
                {
                    Game.logTurn?.Write("--- ChangePlayerEventTimer (Director.cs)");
                    EventPlayer eventObject = GetPlayerEvent(outcome.EventID);
                    int oldValue, newValue;
                    switch (outcome.Timer)
                    {
                        case EventTimer.Repeat:
                            oldValue = eventObject.TimerRepeat;
                            newValue = ChangeData(oldValue, outcome.Amount, outcome.Calc);
                            eventObject.TimerRepeat = newValue;
                            resultText = string.Format(" \"{0}\", EventPID {1}, {2} timer changed from {3} to {4}", eventObject.Name, eventObject.EventPID, outcome.Timer, oldValue, newValue);
                            Game.logTurn?.Write(resultText);
                            break;
                        case EventTimer.Dormant:
                            oldValue = eventObject.TimerDormant;
                            newValue = ChangeData(oldValue, outcome.Amount, outcome.Calc);
                            eventObject.TimerDormant = newValue;
                            resultText = string.Format(" \"{0}\", EventPID {1}, {2} timer changed from {3} to {4}", eventObject.Name, eventObject.EventPID, outcome.Timer, oldValue, newValue);
                            Game.logTurn?.Write(resultText);
                            break;
                        case EventTimer.Live:
                            oldValue = eventObject.TimerLive;
                            newValue = ChangeData(oldValue, outcome.Amount, outcome.Calc);
                            eventObject.TimerLive = newValue;
                            resultText = string.Format(" \"{0}\", EventPID {1}, {2} timer changed from {3} to {4}", eventObject.Name, eventObject.EventPID, outcome.Timer, oldValue, newValue);
                            Game.logTurn?.Write(resultText);
                            break;
                        case EventTimer.Cool:
                            oldValue = eventObject.TimerCoolBase;
                            newValue = ChangeData(oldValue, outcome.Amount, outcome.Calc);
                            eventObject.TimerCoolBase = newValue;
                            resultText = string.Format(" \"{0}\", EventPID {1}, {2} timer changed from {3} to {4}", eventObject.Name, eventObject.EventPID, outcome.Timer, oldValue, newValue);
                            Game.logTurn?.Write(resultText);
                            break;
                        default:
                            Game.SetError(new Error(79, string.Format("Invalid Timer \"{0}\", EventID \"{1}\"", outcome.Timer, outcome.EventID)));
                            break;
                    }
                }
                catch
                { Game.SetError(new Error(79, string.Format("Invalid EventID \"{0}\" (not found)", outcome.EventID))); }
            }
            else
            { Game.SetError(new Error(79, "Invalid Outcome argument (null)")); }
            return resultText;
        }


        /// <summary>
        /// implements actual changes
        /// </summary>
        /// <param name="currentValue"></param>
        /// <param name="amount"></param>
        /// <param name="apply"></param>
        /// <returns></returns>
        public int ChangeData(int currentValue, int amount, EventCalc apply)
        {
            int newValue = currentValue;
            switch (apply)
            {
                case EventCalc.Add:
                    newValue += amount;
                    break;
                case EventCalc.Subtract:
                    newValue -= amount;
                    break;
                case EventCalc.Random:
                    //Adds
                    int rndNum = rnd.Next(amount);
                    newValue += rndNum;
                    break;
                case EventCalc.Equals:
                    newValue = amount;
                    break;
                case EventCalc.NotEqual:
                case EventCalc.LessThanOrEqual:
                case EventCalc.GreaterThanOrEqual:
                    break;
            }
            return newValue;
        }

        /// <summary>
        /// return dictionary of Situations to conflict.GetSituations
        /// </summary>
        /// <returns></returns>
        internal Dictionary<int, Situation> GetSituationsNormal()
        { return dictSituationsNormal; }

        internal Dictionary<int, Situation> GetSituationsGame()
        { return dictSituationsGame; }

        internal Dictionary<int, Situation> GetSituationsSpecial()
        { return dictSituationsSpecial; }

        internal Dictionary<int, Situation> GetSituationsSkill()
        { return dictSituationsSkill; }

        internal Dictionary<int, Situation> GetSituationsTouched()
        { return dictSituationsTouched; }

        internal Dictionary<int, Situation> GetSituationsSupporter()
        { return dictSituationsSupporter; }

        /// <summary>
        /// return a challenge from the dictionary, null if not found
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        internal Challenge GetChallenge(ConflictSubType subType)
        {
            Challenge challenge = null;
            try
            {
                if (dictChallenges.ContainsKey(subType))
                { challenge = dictChallenges[subType]; }
                else
                { Game.SetError(new Error(112, string.Format("{0} Challenge doesn't exist in dictChallenges", subType))); }
            }
            catch (ArgumentNullException)
            { Game.SetError(new Error(112, "Invalid subType (null)")); }
            return challenge;
        }

        /// <summary>
        /// returns true if a specified conflict subType exists in dictionary
        /// </summary>
        /// <returns></returns>
        internal bool CheckChallenge(ConflictSubType type)
        {
            if (dictChallenges.ContainsKey(type) == true)
            { return true; }
            return false;
        }

        /// <summary>
        /// add an autoReact new Player Event to the main dictionary, returns true if successful
        /// </summary>
        /// <param name="eventObject"></param>
        /// <returns></returns>
        public bool AddPlayerEvent(EventPlayer eventObject)
        {
            try
            { dictPlayerEvents.Add(eventObject.EventPID, eventObject); return true; }
            catch (ArgumentNullException)
            { Game.SetError(new Error(125, "Invalid Player Event Object (null)")); }
            catch (ArgumentException)
            { Game.SetError(new Error(125, string.Format("Invalid EventPID (duplicate ID) for \"{0}\"", eventObject.Name))); }
            return false;
        }

        /// <summary>
        /// adjusts a GameState
        /// </summary>
        /// <param name="outType">GameState enum index. If positive then DataState.Good, if negative then DataState.Bad</param>
        /// <param name="amount">how much</param>
        /// <param name="apply">how to apply it</param>
        public string SetState(string eventTxt, string optionTxt, int outType, int amount, EventCalc apply)
        {
            string resultText = "";
            int amountNum = Math.Abs(amount); //must be positive 
            GameState gameState;
            bool stateChanged = false;
            DataState state = DataState.Good;
            if (outType < 0) { state = DataState.Bad; outType *= -1; }
            int newData = 0;
            int oldData = 0;
            //convert to a GameState enum
            if (outType <= (int)GameState.Count)
            {
                gameState = (GameState)outType;
                stateChanged = true;
                OptionInteractive option = new OptionInteractive();
                switch (gameState)
                {
                    case GameState.Justice:
                        oldData = GetGameState(GameState.Justice, state);
                        //apply change (positive #)
                        newData = Math.Abs(ChangeData(oldData, amountNum, apply));
                        //update 
                        SetGameState(GameState.Justice, state, newData, true);
                        break;
                    case GameState.Legend_Usurper:
                        oldData = GetGameState(GameState.Legend_Usurper, state);
                        //apply change (positive #)
                        newData = Math.Abs(ChangeData(oldData, amountNum, apply));
                        //update 
                        SetGameState(GameState.Legend_Usurper, state, newData, true);
                        break;
                    case GameState.Legend_King:
                        oldData = GetGameState(GameState.Legend_King, state);
                        //apply change (positive #)
                        newData = Math.Abs(ChangeData(oldData, amountNum, apply));
                        //update 
                        SetGameState(GameState.Legend_King, state, newData, true);
                        break;
                    case GameState.Honour_Usurper:
                        oldData = GetGameState(GameState.Honour_Usurper, state);
                        //apply change (positive #)
                        newData = Math.Abs(ChangeData(oldData, amountNum, apply));
                        //update 
                        SetGameState(GameState.Honour_Usurper, state, newData, true);
                        break;
                    case GameState.Honour_King:
                        oldData = GetGameState(GameState.Honour_King, state);
                        //apply change (positive #)
                        newData = Math.Abs(ChangeData(oldData, amountNum, apply));
                        //update 
                        SetGameState(GameState.Honour_King, state, newData, true);
                        break;
                    default:
                        Game.SetError(new Error(74, string.Format("Invalid input (enum \"{0}\") for eventPID {1}", gameState, eventTxt)));
                        stateChanged = false;
                        break;
                }
                //update Change state if required
                if (stateChanged == true)
                {
                    //message
                    resultText = string.Format("{0} \"{1}\" {2} from {3} to {4}", gameState, state, oldData > newData ? "decreased" : "increased", oldData, newData);
                    Message message = new Message(string.Format("Event \"{0}\", {1}", eventTxt, resultText), 1, 0, MessageType.Event);
                    Game.world.SetMessage(message);
                }
            }
            else { Game.SetError(new Error(74, string.Format("Invalid input (data \"{0}\") for eventPID {1}", outType, eventTxt))); }
            return resultText;
        }

        /// <summary>
        /// Add rumour to Capital (eg. all rumours with refID 9999)
        /// </summary>
        /// <param name="rumourID"></param>
        public void AddRumourToCapital(int rumourID)
        {
            if (rumourID > 0)
            { listRumoursCapital.Add(rumourID); }
            else { Game.SetError(new Error(269, "Invalid rumourID (zero, or less)")); }
        }


        /// <summary>
        /// Add's a global rumour to appropriate director list and appropriate master dictionary, returns True on success
        /// </summary>
        /// <param name="rumourID"></param>
        /// <param name="global"></param>
        internal bool AddGlobalRumour(Rumour rumour)
        {
            //add to appropriate dictionary (handled automatically)
            if (Game.world.AddRumour(rumour.RumourID, rumour) == false)
            { Game.SetError(new Error(270, $"RumourID {rumour.RumourID} failed to Add to Dictionary -> not added to Global lists")); return false; }
            else
            {
                //add to appropraite Global list
                switch (rumour.Global)
                {
                    case RumourGlobal.All:
                        listRumoursGlobal.Add(rumour.RumourID);
                        break;
                    case RumourGlobal.North:
                        listRumoursNorth.Add(rumour.RumourID);
                        break;
                    case RumourGlobal.East:
                        listRumoursEast.Add(rumour.RumourID);
                        break;
                    case RumourGlobal.South:
                        listRumoursSouth.Add(rumour.RumourID);
                        break;
                    case RumourGlobal.West:
                        listRumoursWest.Add(rumour.RumourID);
                        break;
                    default:
                        listRumoursGlobal.Add(rumour.RumourID);
                        Game.SetError(new Error(270, $"[Alert] RumourID {rumour.RumourID} has an invalid Global Type \"{rumour.Global}\" -> Added to Global list"));
                        break;
                }
                return true;
            }
        }


        /// <summary>
        /// create rumours at game start -> adds to dict and places rumourID in relevant pool
        /// </summary>
        private void InitialiseStartRumours()
        {
            Dictionary<int, Passive> dictPassiveActors = Game.world.GetAllPassiveActors();
            Dictionary<int, MajorHouse> dictMajorHouses = Game.world.GetMajorHouses();

            if (dictPassiveActors != null)
            {
                Game.logStart?.Write("--- InitialiseRumours (World.cs)");
                int skill, strength, index, branch;
                bool proceedFlag;
                int royalRefID = Game.lore.RoyalRefIDNew;
                string trait, rumourText, immersionText, startText, locName;
                string[] arrayOfRumourTexts = new string[] { "rumoured", "rumoured", "said", "known", "widely known", "suspected", "well known", "known by all", "commonly known", "whispered",
                    "whispered", "murmured among friends" };
                string[] arrayOfSecretTexts = new string[] { "has a dark secret", "is keeping something private", "knows more than they let on", "has a mysterious past",
                    "has hidden secrets carefully kept from view", "jealously guards a secret", "knows something important", "conceals a dark truth" };
                string[] arrayOfPrefixTexts = new string[] { "has an unquenched hunger for", "thirsts for", "hungers for", "lusts after", "thinks only of", "is in thrall to", "is fixated upon" };
                string[] arrayOfTitleTexts = new string[] { "is impatient for a Title", "desires a Title", "has their eyes set on a Title", "is determined to gain a Title", "would do anything for a Title" };
                //
                // Characters ---
                //
                //loop through all Passive Actors 
                for (int i = 0; i < dictPassiveActors.Count - 1; i++)
                {
                    Passive actor = dictPassiveActors.ElementAt(i).Value;
                    if (actor != null)
                    {
                        //ignore special actors
                        if (!(actor is Special))
                        {
                            House house = Game.world.GetHouse(actor.RefID);
                            if (house != null || actor.RefID == 9999)
                            {
                                switch (actor.Status)
                                {
                                    case ActorStatus.AtLocation:
                                        locName = Game.world.GetLocationName(actor.LocID);
                                        //
                                        //rumour -> Skills (all skills except touched)
                                        //
                                        for (int skillIndex = 1; skillIndex < (int)SkillType.Count; skillIndex++)
                                        {
                                            skill = actor.GetSkill((SkillType)skillIndex);
                                            if (skill != 3)
                                            {
                                                trait = actor.GetTraitName((SkillType)skillIndex);
                                                if (String.IsNullOrEmpty(trait) == false)
                                                {
                                                    //create a rumour -> make more important characters rumours more visible (higher strength)
                                                    switch (actor.Type)
                                                    {
                                                        case ActorType.Lord:
                                                            strength = 5;
                                                            break;
                                                        case ActorType.Lady:
                                                            strength = 4;
                                                            break;
                                                        case ActorType.BannerLord:
                                                            strength = 4;
                                                            break;
                                                        case ActorType.Heir:
                                                            strength = 3;
                                                            break;
                                                        case ActorType.lady:
                                                        case ActorType.lord:
                                                            strength = 1;
                                                            break;
                                                        default:
                                                            strength = 2;
                                                            break;
                                                    }
                                                    immersionText = $"{arrayOfRumourTexts[rnd.Next(arrayOfRumourTexts.Length)]} {actor.GetPrefixName((SkillType)skillIndex)}";
                                                    rumourText = $"{actor.Title} {actor.Name} \"{actor.Handle}\", ActID {actor.ActID} at {locName}, is {immersionText} {trait}";
                                                    RumourSkill rumour = new RumourSkill(rumourText, strength, actor.ActID, (SkillType)skillIndex, RumourScope.Local, rnd.Next(100) * -1) { RefID = actor.RefID };
                                                    //add to dictionary and house list
                                                    Game.world.AddRumour(rumour.RumourID, rumour);
                                                    //royal family or royal advisors go to the Capital list, all others to their house list
                                                    proceedFlag = true;
                                                    if (actor.RefID == 9999) { proceedFlag = false; }
                                                    if (actor is Noble && actor.RefID == royalRefID) { proceedFlag = false; }
                                                    if (proceedFlag == true) { house.AddRumour(rumour.RumourID); }
                                                    else { AddRumourToCapital(rumour.RumourID); }
                                                    Game.logStart?.Write($"[Skill] RID {rumour.RumourID}, \"{rumourText}\" -> dict");
                                                }
                                            }
                                        }
                                        //
                                        //Rumour -> Secrets (one rumour per character, regardless of how many secrets a character has)
                                        //
                                        if (actor.CheckSecrets() == true)
                                        {
                                            strength = 2;
                                            startText = $"It is {arrayOfRumourTexts[rnd.Next(arrayOfRumourTexts.Length)]}";
                                            immersionText = $"{arrayOfSecretTexts[rnd.Next(arrayOfSecretTexts.Length)]}";
                                            rumourText = $"{startText} that {actor.Title} {actor.Name} \"{actor.Handle}\", ActID {actor.ActID} at {locName}, {immersionText}";
                                            RumourSecret rumour = new RumourSecret(rumourText, strength, RumourScope.Local, rnd.Next(100) * -1) { RefID = actor.RefID };
                                            //add to dictionary and house list
                                            Game.world.AddRumour(rumour.RumourID, rumour);
                                            //royal family or royal advisors go to the Capital list, all others to their house list
                                            proceedFlag = true;
                                            if (actor.RefID == 9999) { proceedFlag = false; }
                                            if (actor is Noble && actor.RefID == royalRefID) { proceedFlag = false; }
                                            if (proceedFlag == true) { house.AddRumour(rumour.RumourID); }
                                            else { AddRumourToCapital(rumour.RumourID); }
                                            Game.logStart?.Write($"[Secret] RID {rumour.RumourID}, \"{rumourText}\" -> dict");
                                        }
                                        //
                                        //Rumours -> Items (one rumour per item possessed, global All)
                                        //
                                        if (actor.CheckItems() == true)
                                        {
                                            strength = 1;
                                            List<int> listOfItems = actor.GetItems();
                                            if (listOfItems != null)
                                            {
                                                int numItems = listOfItems.Count;
                                                if (numItems > 0)
                                                {
                                                    //loop items and create a global rumour for each
                                                    for(index = 0; index < numItems; index++)
                                                    {
                                                        Possession possession = Game.world.GetPossession(listOfItems[index]);
                                                        if (possession != null)
                                                        {
                                                            if (possession is Item)
                                                            {
                                                                Item item = possession as Item;
                                                                startText = $"It is {arrayOfRumourTexts[rnd.Next(arrayOfRumourTexts.Length)]}";
                                                                rumourText = $"{startText} that the {item.Prefix} {possession.Description} ({item.ItemType} Item) is to be found at {locName}";
                                                                RumourItem rumour = new RumourItem(rumourText, strength, RumourScope.Global, rnd.Next(100) * -1, RumourGlobal.All) { RefID = actor.RefID };
                                                                //add to dictionary and global list
                                                                if (AddGlobalRumour(rumour) == false)
                                                                { Game.SetError(new Error(268, $"{rumour.Text}, RumourID {rumour.RumourID}, failed to load (Item) -> Rumour Cancelled")); }
                                                                Game.logStart?.Write($"[Item] RID {rumour.RumourID}, \"{rumourText}\" -> dict");
                                                            }
                                                            else { Game.SetError(new Error(268, "Possession is not an Item")); }
                                                        }
                                                        else { Game.SetError(new Error(268, "Invalid possession (null)")); }
                                                    }
                                                }
                                                else { Game.SetError(new Error(268, "listOfItems is Empty")); }
                                            }
                                            else { Game.SetError(new Error(268, "Invalid listOfItems (null)")); }
                                        }
                                        //
                                        //Rumours -> Disguises (one rumour regardless of # of disguises, global All)
                                        //
                                        if(actor is Advisor)
                                        {
                                            Advisor advisor = actor as Advisor;
                                            if (advisor.CheckDisguises() == true)
                                            {
                                                strength = 4;
                                                startText = $"It is {arrayOfRumourTexts[rnd.Next(arrayOfRumourTexts.Length)]}";
                                                rumourText = $"{startText} that Disguises can be had from {advisor.Title} {advisor.Name}, ActID {advisor.ActID} at {locName}";
                                                RumourDisguise rumour = new RumourDisguise(rumourText, strength, RumourScope.Global, rnd.Next(100) * -1, RumourGlobal.All) { RefID = actor.RefID };
                                                //add to dictionary and global list
                                                if (AddGlobalRumour(rumour) == false)
                                                { Game.SetError(new Error(268, $"{rumour.Text}, RumourID {rumour.RumourID}, failed to load (Disguise) -> Rumour Cancelled")); }
                                                Game.logStart?.Write($"[Disguise] RID {rumour.RumourID}, \"{rumourText}\" -> dict");
                                            }
                                        }
                                        //
                                        //Rumours -> Desires (one rumour per desire -> global.Branch)
                                        //
                                        if (actor.Desire > PossPromiseType.None)
                                        {
                                            string desireText = ""; int data = 0;  branch = 0;
                                            startText = $"It is {arrayOfRumourTexts[rnd.Next(arrayOfRumourTexts.Length)]}";
                                            switch(actor.Desire)
                                            {
                                                case PossPromiseType.Land:
                                                    //Lord wants neighbouring minor house
                                                    data = actor.DesireData; //refID of land desired
                                                    desireText = $"{actor.Title} {actor.Name}, ActID {actor.ActID} at {locName}, desires the neighbouring lands belonging to the Bannerlord at {Game.world.GetHouseName(data)}";
                                                    break;
                                                case PossPromiseType.Court:
                                                    //Lord's son, or brother, to Court. Lady's son to Court
                                                    data = actor.DesireData;
                                                    Passive sibling = Game.world.GetPassiveActor(data);
                                                    if (sibling != null)
                                                    {
                                                        //get family relationship
                                                        if (actor is Noble)
                                                        {
                                                            Noble noble = actor as Noble;
                                                            string relation = noble.GetFamilyRelationship(sibling.ActID);
                                                            string nobleSex = string.Format("{0}", noble.Sex == ActorSex.Male ? "his" : "her");
                                                            desireText = $"{actor.Title} {actor.Name}, ActID {actor.ActID} at {locName}, desires that {nobleSex} {relation}, {sibling.Title} {sibling.Name} \"{sibling.Handle}\", ActID {sibling.ActID}, attains a position in the Royal Court";
                                                        }
                                                        else { Game.SetError(new Error(268, $"Actor {actor.Title} {actor.Name}, ActID {actor.ActID}, is not a Noble -> Desire Court rumour cancelled")); }
                                                    }
                                                    else { Game.SetError(new Error(268, $"Invalid sibling for (actor.DesireData) ActID {data} -> Desire rumour cancelled")); }
                                                    break;
                                                case PossPromiseType.Gold:
                                                    //actor wants gold
                                                    desireText = $"{actor.Title} {actor.Name}, ActID {actor.ActID} at {locName}, {arrayOfPrefixTexts[rnd.Next(arrayOfPrefixTexts.Length)]} Gold";
                                                    break;
                                                case PossPromiseType.Marriage:
                                                    //Lord or Lady want to marry their daughter to the Ursurper
                                                    data = actor.DesireData;
                                                    Passive daughter = Game.world.GetPassiveActor(data);
                                                    if (daughter != null)
                                                    {
                                                        //get family relationship
                                                        if (actor is Noble)
                                                        {
                                                            Noble noble = actor as Noble;
                                                            string relation = noble.GetFamilyRelationship(daughter.ActID);
                                                            string nobleSex = string.Format("{0}", noble.Sex == ActorSex.Male ? "his" : "her");
                                                            string daughterText = $"{daughter.Title} {daughter.Name} \"{daughter.Handle}\", ActID {daughter.ActID}";
                                                            desireText = $"{actor.Title} {actor.Name}, ActID {actor.ActID} at {locName}, desires that {nobleSex} {relation}, {daughterText}, is betrothed to the Ursurper";
                                                        }
                                                        else { Game.SetError(new Error(268, $"Actor {actor.Title} {actor.Name}, ActID {actor.ActID}, is not a Noble -> Desire Marriage rumour cancelled")); }
                                                    }
                                                    else { Game.SetError(new Error(268, $"Invalid sibling for (actor.DesireData) ActID {data} -> Desire rumour cancelled")); }
                                                    break;
                                                case PossPromiseType.Item:
                                                    data = actor.DesireData;
                                                    Possession possession = Game.world.GetPossession(data);
                                                    if (possession != null)
                                                    {
                                                        if (possession is Item)
                                                        {
                                                            Item item = possession as Item;
                                                            string prefixText = $"{arrayOfPrefixTexts[rnd.Next(arrayOfPrefixTexts.Length)]} the";
                                                            desireText = $"{actor.Title} { actor.Name}, ActID { actor.ActID} at {locName}, {prefixText} {item.Prefix} {item.Description}";
                                                        }
                                                        else { Game.SetError(new Error(268, $"Invalid possession (Not an Item), possID {data} -> Desire Item rumour cancelled")); }
                                                    }
                                                    else { Game.SetError(new Error(268, $"Invalid Item possession (Null), possID {data} -> Desire rumour cancelled")); }
                                                    break;
                                                case PossPromiseType.Title:
                                                    //Knight who wants to become a BannerLord
                                                    desireText = $"{actor.Title} {actor.Name}, ActID {actor.ActID} at {locName}, {arrayOfTitleTexts[rnd.Next(arrayOfTitleTexts.Length)]}";
                                                    break;
                                                case PossPromiseType.Lordship:
                                                    data = actor.DesireData; //houseID of majorHouse
                                                    desireText = $"{actor.Title} { actor.Name}, ActID { actor.ActID} at {locName}, desires to become Lord of House {Game.world.GetMajorHouseName(data)}";
                                                    break;
                                                default:
                                                    Game.SetError(new Error(268, $"Invalid actor.Desire \"{actor.Desire}\" -> Desire rumour not created"));
                                                    break;
                                            }
                                            if (desireText.Length > 0)
                                            {
                                                //create rumour
                                                if (actor.RefID != 9999) { branch = house.Branch; }
                                                else { branch = 0; } //Royal Court
                                                if (branch >= 0 && branch < 5)
                                                {
                                                    strength = 2;
                                                    rumourText = $"{startText} that {desireText}";
                                                    RumourDesire rumour = new RumourDesire(rumourText, strength, RumourScope.Global, data, rnd.Next(100) * -1, (RumourGlobal)branch) { RefID = actor.RefID };
                                                    //add to dictionary and global list
                                                    if (AddGlobalRumour(rumour) == false)
                                                    { Game.SetError(new Error(268, $"{rumour.Text}, RumourID {rumour.RumourID}, failed to load (Desire) -> Rumour Cancelled")); }
                                                    Game.logStart?.Write($"[Desire] RID {rumour.RumourID}, \"{rumourText}\" -> dict");
                                                }
                                                else { Game.SetError(new Error(268, $"Invalid House Branch \"{branch}\" -> Rumour Desire not created")); }
                                            }
                                        }
                                        break;
                                    case ActorStatus.Gone:
                                        //
                                        //rumour of past character
                                        //
                                        break;
                                }
                            }
                            else { Game.SetError(new Error(268, $"Invalid house (null) for Passive ActID {actor.ActID} and RefID {actor.RefID}")); }
                        }
                    }
                    else { Game.SetError(new Error(268, "Invalid Passive actor (null)")); }
                }
                //
                //Houses ---
                //
                int relFriends = Game.constant.GetValue(Global.FRIEND_THRESHOLD);
                int relEnemies = Game.constant.GetValue(Global.ENEMY_THRESHOLD);
                int numFriends, numEnemies, relPlyr;
                string friendPrefix, enemyPrefix;
                string[] arrayOfDescriptors = new string[] { "no", "an", "a few", "a handful of", "many" }; //used for Friends and enemies, index corresponds to #'s, eg. index 0 -> 'no' friends, 4+ -> 'many'
                //Major Houses
                foreach (var house in dictMajorHouses)
                {
                    //
                    //Rumours -> Friends and Enemies (one per Major House if any exist, Capital excluded as auto known -> Global.Branches)
                    //
                    Location loc = Game.network.GetLocation(house.Value.LocID);
                    if (loc != null)
                    {
                        numFriends = 0; numEnemies = 0; relPlyr = 0;
                        //characters at location
                        List<int> charList = loc.GetActorList();
                        charList.Sort();
                        if (charList.Count > 0)
                        {
                            foreach (int charID in charList)
                            {
                                Actor actor = Game.world.GetAnyActor(charID);
                                if (actor != null)
                                {
                                    //tally friends and enemies
                                    if (actor.ActID > 1)
                                    {
                                        relPlyr = actor.GetRelPlyr();
                                        if (relPlyr >= relFriends) { numFriends++; }
                                        else if (relPlyr <= relEnemies) { numEnemies++; }
                                    }
                                }
                            }
                        }
                        else { Game.logStart?.Write( $"[Notification] There are no characters at {loc.LocName}, LocID {loc.LocationID}"); }
                        //any friends or enemies?
                        friendPrefix = ""; enemyPrefix = "";
                        if (numFriends > 0 || numEnemies > 0)
                        {
                            branch = house.Value.Branch;
                            if (branch >= 0 && branch < 5)
                            {
                                strength = 3;
                                index = numFriends;
                                index = Math.Min(4, numFriends); //cap to size of arrayOfDescriptors
                                friendPrefix = arrayOfDescriptors[index];
                                index = numEnemies;
                                index = Math.Min(4, numEnemies); //cap to size of arrayOfDescriptors
                                enemyPrefix = arrayOfDescriptors[index];
                                startText = $"It is {arrayOfRumourTexts[rnd.Next(arrayOfRumourTexts.Length)]}";
                                rumourText = string.Format("{0} that you have {1} Friend{2} and {3} Enem{4} at {5} (House {6})", startText, friendPrefix, numFriends != 1 ? "s" : "",
                                    enemyPrefix, numEnemies != 1 ? "ies" : "y", loc.LocName, house.Value.Name);
                                RumourFriends rumour = new RumourFriends(rumourText, strength, RumourScope.Global, rnd.Next(100) * -1, (RumourGlobal)branch) { RefID = loc.RefID };
                                //add to dictionary and global list
                                if (AddGlobalRumour(rumour) == false)
                                { Game.SetError(new Error(268, $"{rumour.Text}, RumourID {rumour.RumourID}, failed to load (Friends and Enemies) -> Rumour Cancelled")); }
                                Game.logStart?.Write($"[Friends and Enemies] RID {rumour.RumourID}, \"{rumourText}\" -> dict");
                            }
                            else { Game.SetError(new Error(268, $"Invalid House Branch \"{branch}\" -> Rumour Friends not created")); }
                        }
                    }
                    else { Game.SetError(new Error(268, $"Invalid Location (null) from LocID {house.Value.LocID}")); }
                    //
                    //Rumours -> Past History between Major Houses, other Major Houses and their Minor Houses (one entry per relationship, global.Branch)
                    //
                    List<Relation> listOfHouseRelationships = house.Value.GetRelations();
                    if (listOfHouseRelationships != null)
                    {
                        if (listOfHouseRelationships.Count > 0)
                        {
                            //House who the relationship is From
                            House houseFrom = Game.world.GetHouse(house.Value.RefID);
                            if (houseFrom != null)
                            {
                                foreach (var relationship in listOfHouseRelationships)
                                {
                                    //get house who the relationship is with (relationship To..)
                                    House houseTo = Game.world.GetHouse(relationship.RefID);
                                    if (houseTo != null)
                                    {
                                        rumourText = "";
                                        //generate rumour text
                                        switch(relationship.Type)
                                        {
                                            case RelListType.HousePastGood:
                                                rumourText = $"House {houseFrom.Name} has had good past relations with House {houseTo.Name} due to {relationship.Rumour}";
                                                break;
                                            case RelListType.HousePastBad:
                                                rumourText = $"House {houseFrom.Name} has had poor past relations with House {houseTo.Name} due to {relationship.Rumour}";
                                                break;
                                            case RelListType.BannerPastGood:
                                                rumourText = $"House {houseFrom.Name} has had good past relations with their BannerLord at House {houseTo.Name} due to {relationship.Rumour}";
                                                break;
                                            case RelListType.BannerPastBad:
                                                rumourText = $"House {houseFrom.Name} has had poor past relations with their BannerLord at House {houseTo.Name} due to {relationship.Rumour}";
                                                break;
                                            default:
                                                Game.SetError(new Error(268, $"Invalid relationship.Type \"{relationship.Type}\""));
                                                break;
                                        }
                                        //is there a rumour to create?
                                        if (rumourText.Length > 0)
                                        {
                                            branch = houseFrom.Branch;
                                            if (branch >= 0 && branch < 5)
                                            {
                                                strength = 2;
                                                RumourHouseRel rumour = new RumourHouseRel(rumourText, strength, RumourScope.Global, rnd.Next(100) * -1, (RumourGlobal)branch)
                                                { RefID = houseFrom.RefID, HouseToRefID = houseTo.RefID, TrackerID = relationship.TrackerID };
                                                //add to dictionary and global list
                                                if (AddGlobalRumour(rumour) == false)
                                                { Game.SetError(new Error(268, $"{rumour.Text}, RumourID {rumour.RumourID}, failed to load (HouseRel) -> Rumour Cancelled")); }
                                                Game.logStart?.Write($"[HouseRel] RID {rumour.RumourID}, \"{rumourText}\" -> dict");
                                            }
                                            else { Game.SetError(new Error(268, $"Invalid houseFrom.Branch \"{branch}\" -> rumour cancelled")); }
                                        }
                                    }
                                    else { Game.SetError(new Error(268, $"Invalid houseTo (null), RefID {relationship.RefID} -> Rumour not created")); }
                                }
                            }
                            else { Game.SetError(new Error(268, $"Invalid houseFrom (null), RefID {house.Value.RefID} -> No rumours created ")); }
                        }
                    }
                    else { Game.SetError(new Error(268, "Invalid listOfHouseRelationships (null)")); }
                }
            }
            else { Game.SetError(new Error(268, "Invalid dictOfPassiveActors (null)")); }
        }

        /// <summary>
        /// Creates Rumours during Game -> adds to dict and places rumourID in relevant pool
        /// </summary>
        public void InitialiseDynamicRumours()
        {
            Game.logTurn?.Write("--- InitialiseDynamicRumours (Director.cs)");
            int chanceOfRumour, rndNum, strength, branch, timerExpire;
            string rumourText, waitText;
            //
            //Enemies
            //
            Dictionary<int, Enemy> dictEnemyActors = Game.world.GetEnemyActors();
            if (dictEnemyActors != null)
            {
                chanceOfRumour = Game.constant.GetValue(Global.ENEMY_RUMOURS);
                string[] arrayOfInquisitorWaitTexts = new string[] { "relaxing", "wenching", "drinking", "gambling", "doing sword drills" };
                string[] arrayOfNemesisWaitTexts = new string[] { "praying", "worshipping", "meditating", "flagelatting themself", "absolving themself" };
                foreach(var enemy in dictEnemyActors)
                {
                    rumourText = ""; branch = 0;
                    //no need to check enemy.Status != Gone as enemies can never die
                    if( enemy.Value is Inquisitor || enemy.Value is Nemesis)
                    {
                        //rumour possible only if enemy not already known
                        if (enemy.Value.Known == false)
                        {
                            rndNum = rnd.Next(100);
                            strength = 3;
                            timerExpire = Game.constant.GetValue(Global.ENEMY_RUMOUR_TIME);
                            Location loc = Game.network.GetLocation(enemy.Value.LocID);
                            if (loc != null)
                            {
                                branch = loc.GetBranch();
                                switch (enemy.Value.Goal)
                                {
                                    case ActorAIGoal.Hide:
                                        //halved chance for an enemy in hiding -> hunt mode not possible
                                        if (rndNum < (chanceOfRumour / 2))
                                        { rumourText = string.Format("{0}, {1}, ActID {2} has been spotted hiding at {3} {4}", enemy.Value.Title, enemy.Value.Name,
                                             enemy.Value.ActID, loc.LocName, Game.world.ShowLocationCoords(loc.LocationID)); }
                                        break;
                                    case ActorAIGoal.Wait:
                                        //hunt mode not possible for 'wait'
                                        if (rndNum < (chanceOfRumour ))
                                        {
                                            if (enemy.Value is Nemesis)
                                            { waitText = arrayOfNemesisWaitTexts[rnd.Next(arrayOfNemesisWaitTexts.Length)]; }
                                            else
                                            { waitText = arrayOfInquisitorWaitTexts[rnd.Next(arrayOfInquisitorWaitTexts.Length)]; }
                                            rumourText = string.Format("{0}, {1}, ActID {2} has been spotted {3} at {4} {5}", enemy.Value.Title, enemy.Value.Name,
                                                  enemy.Value.ActID, waitText, loc.LocName, Game.world.ShowLocationCoords(loc.LocationID));
                                        }
                                        break;
                                    case ActorAIGoal.Search:
                                        if (rndNum < chanceOfRumour)
                                        {
                                            if (enemy.Value.HuntMode == true)
                                            { rumourText = string.Format("{0}, {1}, ActID {2} has been spotted asking about the Usurper's whereabouts at {3} {4} with {5} sword at the ready", enemy.Value.Title, enemy.Value.Name,
                                                  enemy.Value.ActID, loc.LocName, Game.world.ShowLocationCoords(loc.LocationID), enemy.Value.Sex == ActorSex.Male ? "his" : "her"); }
                                            else
                                            { rumourText = string.Format("{0}, {1}, ActID {2} has been spotted asking about the Usurper's whereabouts at {3} {4}", enemy.Value.Title, enemy.Value.Name,
                                             enemy.Value.ActID, loc.LocName, Game.world.ShowLocationCoords(loc.LocationID)); }
                                        }
                                        break;
                                    case ActorAIGoal.Move:
                                        if (rndNum < chanceOfRumour)
                                        {
                                            if (enemy.Value.HuntMode == true)
                                            { rumourText = string.Format("{0}, {1}, ActID {2} has been seen on the road to {3} {4} with {5} sword at the ready", enemy.Value.Title, enemy.Value.Name,
                                                    enemy.Value.ActID, loc.LocName, Game.world.ShowLocationCoords(loc.LocationID), enemy.Value.Sex == ActorSex.Male ? "his" : "her"); }
                                            else
                                            { rumourText = string.Format("{0}, {1}, ActID {2} has been seen on the road to {3} {4}", enemy.Value.Title, enemy.Value.Name,
                                               enemy.Value.ActID, loc.LocName, Game.world.ShowLocationCoords(loc.LocationID)); }
                                        }
                                        break;
                                }
                                //is there a rumour to create?
                                if (rumourText.Length > 0)
                                {
                                    if (branch >= 0 && branch < 5)
                                    {
                                        strength = 3;
                                        RumourEnemy rumour = new RumourEnemy(rumourText, strength, RumourScope.Global, timerExpire, 0, (RumourGlobal)branch);
                                        //add to appropriate dictionary and appropriate global list
                                        if (AddGlobalRumour(rumour) == false)
                                        { Game.SetError(new Error(277, $"{rumour.Text}, RumourID {rumour.RumourID}, failed to load (Enemy) -> Enemy Rumour Cancelled")); }
                                        Game.logTurn?.Write($"[Enemy] RID {rumour.RumourID}, \"{rumourText}\" -> dict");
                                    }
                                    else { Game.SetError(new Error(277, $"Invalid branch \"{branch}\" -> Enemy rumour cancelled")); }
                                }
                            }
                            else { Game.SetError(new Error(277, $"Invalid Location (null) for {enemy.Value.Title} {enemy.Value.Name}, ActID {enemy.Value.ActID}, LocID {enemy.Value.LocID} -> Cancelled")); }
                        }
                    }
                }
            }
            else { Game.SetError(new Error(277, "Invalid dictEnemyActors (null) -> all Enemy rumours cancelled")); }
        }

        /// <summary>
        /// Returns a rumourID from a pool of possibles and returns the RumourID -> returns '0' if none found
        /// </summary>
        /// <param name="refID">The refID of the current location</param>
        /// <param name="actorID">The actorID of the actor learning of the rumour</param>
        /// <returns></returns>
        public int GetRumourUnknown(int refID, int actorID)
        {
            Game.logTurn?.Write("--- GetRumour (Director.cs)");
            int rumourID = 0;
            int tempRumourID, index;
            int branch = 0;
            int[] arrayOfRumours = new int[7];  //selected rumourID's -> [0] global All [1 to 4] branch N/E/S/W [5] house [6] Capital
            List<int> listRumoursReturn = new List<int>();
            List<int> listRumoursPool = new List<int>();
            List<int> listRumoursHouse = new List<int>();
            //get local rumours
            if (refID != 9999)
            {
                House house = Game.world.GetHouse(refID);
                if (house != null)
                {
                    //get branch rumour
                    branch = house.Branch;
                    //get local rumours
                    listRumoursHouse = house.GetRumours();
                    if (listRumoursHouse != null)
                    {
                        if (listRumoursHouse.Count > 0)
                        {
                            index = rnd.Next(listRumoursHouse.Count);
                            tempRumourID = listRumoursHouse[index];
                            //keep track of which rumour was selected (might have to delete it)
                            arrayOfRumours[5] = tempRumourID;
                        }
                        else { Game.logTurn?.Write("[Notification] No house rumours available -> none added to pool"); }
                    }
                    else { Game.SetError(new Error(271, "Invalid listTempRumours (null)")); }
                }
                else { Game.SetError(new Error(271, $"Invalid house (null) for refID {refID}")); }
            }
            else
            {
                //Capital
                if (listRumoursCapital.Count > 0)
                {
                    index = rnd.Next(listRumoursCapital.Count);
                    tempRumourID = listRumoursCapital[index];
                    arrayOfRumours[6] = tempRumourID;
                }
                else { Game.logTurn?.Write("[Notification] No Capital rumours available -> none added to pool"); }
            }
            //get global All rumour
            if (listRumoursGlobal.Count > 0)
            {
                index = rnd.Next(listRumoursGlobal.Count);
                tempRumourID = listRumoursGlobal[index];
                arrayOfRumours[0] = tempRumourID;
            }
            else { Game.logTurn?.Write("[Notification] No Global rumours available -> none added to pool"); }
            //get appropriate branch rumours
            if (branch > 0)
            {
                switch (branch)
                {
                    case 1:
                        //get North branch rumours
                        if (listRumoursNorth.Count > 0)
                        {
                            index = rnd.Next(listRumoursNorth.Count);
                            tempRumourID = listRumoursNorth[index];
                            arrayOfRumours[1] = tempRumourID;
                        }
                        else { Game.logTurn?.Write("[Notification] No Branch North rumours available  -> none added to pool"); }
                        break;
                    case 2:
                        //get East branch rumours
                        if (listRumoursEast.Count > 0)
                        {
                            index = rnd.Next(listRumoursEast.Count);
                            tempRumourID = listRumoursEast[index];
                            arrayOfRumours[2] = tempRumourID;
                        }
                        else { Game.logTurn?.Write("[Notification] No Branch East rumours available  -> none added to pool"); }
                        break;
                    case 3:
                        //get South branch rumours
                        if (listRumoursSouth.Count > 0)
                        {
                            index = rnd.Next(listRumoursSouth.Count);
                            tempRumourID = listRumoursSouth[index];
                            arrayOfRumours[3] = tempRumourID;
                        }
                        else { Game.logTurn?.Write("[Notification] No Branch South rumours available  -> none added to pool"); }
                        break;
                    case 4:
                        //get North branch rumours
                        if (listRumoursWest.Count > 0)
                        {
                            index = rnd.Next(listRumoursWest.Count);
                            tempRumourID = listRumoursWest[index];
                            arrayOfRumours[4] = tempRumourID;
                        }
                        else { Game.logTurn?.Write("[Notification] No Branch West rumours available  -> none added to pool"); }
                        break;
                }
            }
            else { Game.logTurn?.Write("[Notification] No Branch (overall) rumours available  -> none added to pool"); }
            //add rumours into the pool, one entry per level of strength
            for (int i = 0; i < arrayOfRumours.Length; i++)
            {
                tempRumourID = arrayOfRumours[i];
                if (tempRumourID > 0)
                {
                    //Get Rumour
                    Rumour rumour = Game.world.GetRumour(tempRumourID);
                    if (rumour != null)
                    {
                        Game.logTurn?.Write($"arrayOfRumours [{i}] -> RumourID {tempRumourID}, Strength {rumour.Strength}, Scope {rumour.Scope}, Type {rumour.Type}, Global {rumour.Global}");
                        //add one instance of the rumour to the pool for every level of strength
                        for (int k = 0; k < rumour.Strength; k++)
                        { listRumoursPool.Add(rumour.RumourID); }
                    }
                    else { Game.SetError(new Error(271, $"Invalid rumour (null) for RumourID {tempRumourID}, arrayOfRumours index {i} -> Not added to pool")); }
                }
            }
            //select the correct number of rumours from the pool
            if (listRumoursPool.Count > 0)
            {
                rumourID = listRumoursPool[rnd.Next(listRumoursPool.Count)];
                Game.logTurn?.Write($"RumourID {rumourID} selected from the Pool");
                //update rumour for the actor who learned of it and the turn it was revealed
                Rumour rumour = Game.world.GetRumour(rumourID);
                rumour.TurnRevealed = Game.gameTurn;
                if (actorID > 0 && actorID < 10)
                { rumour.WhoHeard = actorID; }
                else { Game.SetError(new Error(271, $"Invalid actorID {actorID} (not Player or a follower) -> rumour.WhoHeard not updated")); }
                Game.logTurn?.Write($"Rumour \"{rumour.Text}\" revealed to ActorID{actorID} on turn {rumour.TurnRevealed} -> added to List");

                //delete rumour from appropriate list (so it can't be used again)
                int deleteIndex = -1;
                for (int i = 0; i < arrayOfRumours.Length; i++)
                {
                    if (arrayOfRumours[i] == rumourID)
                    {
                        deleteIndex = i;
                        break;
                    }
                }
                //delete from specific list of rumours
                if (deleteIndex > -1)
                {
                    switch (deleteIndex)
                    {
                        case 0:
                            //global all
                            index = listRumoursGlobal.FindIndex(id => id == rumourID);
                            listRumoursGlobal.RemoveAt(index);
                            Game.logTurn?.Write($"RumourID {rumourID} removed from listRumoursGlobal[{index}]");
                            break;
                        case 1:
                            //north branch
                            index = listRumoursNorth.FindIndex(id => id == rumourID);
                            listRumoursNorth.RemoveAt(index);
                            Game.logTurn?.Write($"RumourID {rumourID} removed from listRumoursNorth[{index}]");
                            break;
                        case 2:
                            //east branch
                            index = listRumoursEast.FindIndex(id => id == rumourID);
                            listRumoursEast.RemoveAt(index);
                            Game.logTurn?.Write($"RumourID {rumourID} removed from listRumoursEast[{index}]");
                            break;
                        case 3:
                            //south branch
                            index = listRumoursSouth.FindIndex(id => id == rumourID);
                            listRumoursSouth.RemoveAt(index);
                            Game.logTurn?.Write($"RumourID {rumourID} removed from listRumoursSouth[{index}]");
                            break;
                        case 4:
                            //west branch
                            index = listRumoursWest.FindIndex(id => id == rumourID);
                            listRumoursWest.RemoveAt(index);
                            Game.logTurn?.Write($"RumourID {rumourID} removed from listRumoursWest[{index}]");
                            break;
                        case 5:
                            //House list
                            index = listRumoursHouse.FindIndex(id => id == rumourID);
                            listRumoursHouse.RemoveAt(index);
                            Game.logTurn?.Write($"RumourID {rumourID} removed from listRumoursHouse[{index}]");
                            break;
                        case 6:
                            //Capital
                            index = listRumoursCapital.FindIndex(id => id == rumourID);
                            listRumoursCapital.RemoveAt(index);
                            Game.logTurn?.Write($"RumourID {rumourID} removed from listRumoursCapital[{index}]");
                            break;
                    }
                }
                else { Game.SetError(new Error(271, "Selected Rumour not found in arrayOfRumours (deleteIndex -1) -> rumour cancelled")); rumourID = 0; }
            }
            else { Game.logTurn?.Write("[Notification] No rumours available (listRumourPool empty)  -> none selected"); }
            return rumourID;
        }

        /// <summary>
        /// Remove a Rumour's ID form the appropriate list
        /// </summary>
        /// <param name="rumour"></param>
        /// <returns></returns>
        internal bool RemoveRumourFromList(Rumour rumour)
        {
            if (rumour != null)
            {
                switch (rumour.Scope)
                {
                    case RumourScope.Local:
                        //remove from a house list
                        if (rumour.RefID != 9999)
                        {
                            House house = Game.world.GetHouse(rumour.RefID);
                            if (house != null)
                            {
                                if (house.RemoveRumour(rumour.RumourID) == true)
                                { Game.logTurn?.Write($"[Notification -> Removal] RID {rumour.RumourID}, \"{rumour.Text}\" removed from House list"); return true; }
                                else { Game.SetError(new Error(279, $"RID {rumour.RumourID}, \"{rumour.Text}\" FAILED removed from House list")); }
                            }
                            else { Game.SetError(new Error(279, $"Invalid house (null) for rumourID {rumour.RumourID}, refID {rumour.RefID} -> list removal cancelled")); }
                        }
                        else
                        {
                            //remove from capital list
                            if (listRumoursCapital.Remove(rumour.RumourID) == true)
                            { Game.logTurn?.Write($"[Notification -> Removal] RID {rumour.RumourID}, \"{rumour.Text}\" removed from Capital list"); return true; }
                            else { Game.SetError(new Error(279, $"RID {rumour.RumourID}, \"{rumour.Text}\" FAILED removed from Capital list")); }
                        }
                        break;
                    case RumourScope.Global:
                        switch (rumour.Global)
                        {
                            case RumourGlobal.All:
                                if (listRumoursGlobal.Remove(rumour.RumourID) == true)
                                { Game.logTurn?.Write($"[Notification -> Removal] RID {rumour.RumourID}, \"{rumour.Text}\" removed from Global All list"); return true; }
                                else
                                {
                                    if (Game.world.CheckRumourKnown(rumour.RumourID) == true)
                                    { Game.logTurn?.Write($"[Notification -> ALL] RID {rumour.RumourID}, \"{rumour.Text}\" already Known (not removed, but O.K)"); return true; }
                                    else { Game.SetError(new Error(279, $"RID {rumour.RumourID}, \"{rumour.Text}\" FAILED removed from Global All list")); }
                                }
                                break;
                            case RumourGlobal.North:
                                if (listRumoursNorth.Remove(rumour.RumourID) == true)
                                { Game.logTurn?.Write($"[Notification -> North] RID {rumour.RumourID}, \"{rumour.Text}\" removed from Global North list"); return true; }
                                else
                                {
                                    if (Game.world.CheckRumourKnown(rumour.RumourID) == true)
                                    { Game.logTurn?.Write($"[Notification -> North] RID {rumour.RumourID}, \"{rumour.Text}\" already Known (not removed, but O.K)"); return true; }
                                    else { Game.SetError(new Error(279, $"RID {rumour.RumourID}, \"{rumour.Text}\" FAILED removed from Global All list")); }
                                }
                                break;
                            case RumourGlobal.East:
                                if (listRumoursEast.Remove(rumour.RumourID) == true)
                                { Game.logTurn?.Write($"[Notification -> Removal] RID {rumour.RumourID}, \"{rumour.Text}\" removed from Global East list"); return true; }
                                else
                                {
                                    if (Game.world.CheckRumourKnown(rumour.RumourID) == true)
                                    { Game.logTurn?.Write($"[Notification -> East] RID {rumour.RumourID}, \"{rumour.Text}\" already Known (not removed, but O.K)"); return true; }
                                    else { Game.SetError(new Error(279, $"RID {rumour.RumourID}, \"{rumour.Text}\" FAILED removed from Global All list")); }
                                }
                                break;
                            case RumourGlobal.South:
                                if (listRumoursSouth.Remove(rumour.RumourID) == true)
                                { Game.logTurn?.Write($"[Notification -> Removal] RID {rumour.RumourID}, \"{rumour.Text}\" removed from Global South list"); return true; }
                                else
                                {
                                    if (Game.world.CheckRumourKnown(rumour.RumourID) == true)
                                    { Game.logTurn?.Write($"[Notification -> South] RID {rumour.RumourID}, \"{rumour.Text}\" already Known (not removed, but O.K)"); return true; }
                                    else { Game.SetError(new Error(279, $"RID {rumour.RumourID}, \"{rumour.Text}\" FAILED removed from Global All list")); }
                                }
                                break;
                            case RumourGlobal.West:
                                if (listRumoursGlobal.Remove(rumour.RumourID) == true)
                                { Game.logTurn?.Write($"[Notification -> Removal] RID {rumour.RumourID}, \"{rumour.Text}\" removed from Global West list"); return true; }
                                else
                                {
                                    if (Game.world.CheckRumourKnown(rumour.RumourID) == true)
                                    { Game.logTurn?.Write($"[Notification -> West] RID {rumour.RumourID}, \"{rumour.Text}\" already Known (not removed, but O.K)"); return true; }
                                    else { Game.SetError(new Error(279, $"RID {rumour.RumourID}, \"{rumour.Text}\" FAILED removed from Global All list")); }
                                }
                                break;
                            default:
                                Game.SetError(new Error(279, $"Invalid rumour.Global \"{rumour.Global}\" for RumourID {rumour.RumourID} -> list removal cancelled"));
                                break;
                        }
                        break;
                    default:
                        Game.SetError(new Error(279, $"Invalid rumour.Scope \"{rumour.Scope}\" for RumourID {rumour.RumourID} -> list removal cancelled"));
                        break;
                }
            }
            else { Game.SetError(new Error(279, "Invalid Rumour (null)")); }
            return false;
        }

        /// <summary>
        /// Gives the word from the street (tied into gamestates) -> rotates through options using GameVar.Street_View
        /// </summary>
        /// <returns></returns>
        public string GetStreetView()
        {
            string streetView = "";
            int streetIndex = Game.variable.GetValue(GameVar.Street_View);
            string[] arrayOfOccupationsMale = new string[] { "Noble", "Merchant", "Church", "Craftsman", "Craftsman", "Craftsman", "Peasant", "Peasant", "Peasant", "Peasant" };
            string name, group;
            string occupation = "Unknown";
            string view = "";
            int data;
            int age = rnd.Next(15, 50);
            if (rnd.Next(100) <= 50)
            {
                //male
                group = arrayOfOccupationsMale[rnd.Next(arrayOfOccupationsMale.Length)];
                name = Game.history.GetFirstName(ActorSex.Male);
            }
            else
            {
                //female
                group = "peasant";
                name = Game.history.GetFirstName(ActorSex.Female);
            }
            switch (group)
            {
                case "Craftsman":
                    string[] arrayOfCraftsman = new string[] {"Armourer", "Artist", "Baker", "Bookbinder", "Candlemaker", "Blacksmith", "Carpenter", "Dyer", "Forester",
                    "Engraver", "Brewer", "Bricklayer", "Stonemason", "Glassblower", "Jester", "Furrier", "Clothier", "Weaver", "Engineer", "Cartographer", "Potter", "Scribe", "Musician",
                    "Poet", "Troubadour", "Tumbler", "Illuminator", "Fiddler", "Barker", "Bard", "Saddler", "Chandler", "Shoemaker", "Tanner", "Locksmith", "Glover", "Butcher", "Scabbard Maker"};
                    occupation = arrayOfCraftsman[rnd.Next(arrayOfCraftsman.Length)];
                    break;
                case "Merchant":
                    string[] arrayOfMerchants = new string[] { "Innkeeper", "MoneyLender", "Trader", "Brothel Owner", "Glass Seller", "Ironmonger", "Linen Draper", "Peddler", "Mercer",
                    "Eggler", "Chapman", "Boothman", "Banker", "Apothecary", "Acater", "Oil Merchant", "Oynter", "Skinner", "Spice Merchant", "Spicer", "Stationer", "Thresher", "Taverner",
                    "Unguentary", "Waferer", "Waterseller", "Woodmonger", "Wool Stapler"};
                    occupation = arrayOfMerchants[rnd.Next(arrayOfMerchants.Length)];
                    break;
                case "Peasant":
                    string[] arrayOfPeasants = new string[] { "Farmer", "Fowler", "Crofter", "Farmer", "Cook", "Story Teller", "Fortune Teller", "Messenger", "Rat Catcher", "Astrologer",
                    "Pickpocket", "Boothaler", "Footpad", "Poacher", "Silk Snatcher", "Thimblerigger", "Hermit", "Beggar", "Beggar", "Beggar", "Buffoon", "Dwarf", "Palmer", "Tenter",
                    "Ferryman", "Sheperd", "Hawker", "Hunter", "Goatherder", "Fewterer", "Falconer", "Sheepshearer", "Reaper", "Trapper", "Molecatcher" };
                    occupation = arrayOfPeasants[rnd.Next(arrayOfPeasants.Length)];
                    break;
                case "Noble":
                    string[] arrayOfNobles = new string[] { "Bailiff", "Chancellor", "Diplomat", "Constable", "Hayward", "Jailer", "Judge", "Nobleman", "Nobleman", "Nobleman", "Pursuivant", "Sherrif" };
                    occupation = arrayOfNobles[rnd.Next(arrayOfNobles.Length)];
                    break;
                case "Church":
                    string[] arrayOfChurch = new string[] { "Pilgrim", "Ostiary", "Pardoner", "Sacristan", "Sexton", "Summoner", "Clerk", "Chanty Priest", "Cantor", "Beadle", "Almoner", "Friar", "Monk" };
                    occupation = arrayOfChurch[rnd.Next(arrayOfChurch.Length)];
                    break;
                default:
                    Game.SetError(new Error(282, $"Invalid Group \"{group}\" -> StreetView cancelled"));
                    break;
            }
            //each turn is a different option to maximise variety
            switch(streetIndex)
            {
                case 1:
                    //Justice of Cause
                    data = CheckGameState(GameState.Justice);
                    break;
                case 2:
                    //Relative Legend
                    if (rnd.Next(100) <= 50)
                    { data = CheckGameState(GameState.Legend_Usurper); }
                    else
                    { data = CheckGameState(GameState.Legend_King); }
                    view =
                    break;
                case 3:
                    //Relative Honour

                    break;
                case 4:
                    //Known / Unknown

                    break;
                default:
                    Game.SetError(new Error(282, $"Invalid Street_View \"{streetIndex} -> Street View cancelled"));
                    break;
            }
            //increment index and roll over if need be
            streetIndex++;
            if (streetIndex > 4) { streetIndex = 1; }
            //update GameVar
            Game.variable.SetValue(GameVar.Street_View, streetIndex);
            //put string together
            if (view.Length > 0)
            { streetView = $"{name}, age {age}, {occupation} \"{view}\""; }
            return streetView;
        }

        //place new methods above here
    }
}
