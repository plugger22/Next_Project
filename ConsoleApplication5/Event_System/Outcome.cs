﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Next_Game.Event_System
{
    public enum OutcomeType { None, Delay, Conflict, GameState, GameVar, Known, EventTimer, EventStatus, EventChain, Resource, Condition, Freedom, Item, Passage, VoyageTime, Adrift, DeathTimer, Rescued,
    Follower, Promise, RelPlyr, Favour, Introduction, SafeHouse, Disguise, Rumour, Travel, HorseHealth, HorseStatus, Observe};

    /// <summary>
    /// Option outcome, event system
    /// </summary>
    class Outcome
    {
        private static int outcomeIndex = 1; //autoassigned ID's. Main focus is the Outcome Class
        public int OutcomeID { get; }
        public int EventID { get; } //could be EventFID (follower) or EventPID (player)
        public int Data { get; set; } //optional multipurpose type for use with resolve
        public int Amount { get; set; }
        public EventCalc Calc { get; set; }
        public OutcomeType Type { get; set; }
        

        public Outcome(int eventID)
        {
            OutcomeID = outcomeIndex++;
            if (eventID > 0) { EventID = eventID; }
            else { Game.SetError(new Error(67, "Invalid Input (eventID) in constructor")); }
        }
        
    }


    // --- Follower subclass ---

    /// <summary>
    /// Gives a delay to an Active Actor
    /// </summary>
    class OutDelay : Outcome
    {
        public int Delay { get; set; } //number of turns for the delay

        public OutDelay(int delay, int eventID) : base (eventID)
        {
            
            if (delay > 0)
            { this.Delay = delay; }
            else { Game.SetError(new Error(67, "Invalid Input (Delay) in OutDelay")); }
            Type = OutcomeType.Delay;
        }

        /// <summary>
        /// data1 is ActorID -> message & update actor delay
        /// </summary>
        /// <param name="data1"></param>
        /// <param name="data2"></param>
        public void Resolve(int data1, int data2 = 0)
        {
            Active actor = Game.world.GetActiveActor(data1);
            Event eventObject = Game.director.GetFollowerEvent(EventID);
            if (actor != null)
            {
                if (eventObject != null)
                {
                    actor.Delay += Delay;
                    actor.DelayReason = eventObject.Name;
                    //int refID = Game.world.ConvertLocToRef(actor.LocID);
                    //message
                    string messageText = string.Format("{0} has been {1} (\"{2}\") for {3} {4}", actor.Name, eventObject.Type == ArcType.Location ? "indisposed" : "delayed",
                        actor.DelayReason, Delay, Delay == 1 ? "Day" : "Day's");
                    Message message = new Message(messageText, MessageType.Move);
                    Game.world.SetMessage(message);
                    Game.world.SetCurrentRecord(new Record(messageText, actor.ActID, actor.LocID, CurrentActorEvent.Event));
                }
                else { Game.SetError(new Error(67, "Event not found using EventID in OutDelay.cs")); }
            }
            else { Game.SetError(new Error(67, "Active Actor not found")); }
        }
    }


    //--- Player subclasses ---

    /// <summary>
    /// Do nothing outcome
    /// </summary>
    class OutNone : Outcome
    {
        public string Description { get; set; } //optional text description that, if present is given as a Player record

        public OutNone(int eventID, string description = "", int data = 0) : base (eventID)
        {
            this.Description = description; //optional
            this.Data = data; //locID (optional)
            //set all to default (they aren't used but do show up on debug messages)
            Amount = 0;
            Calc = EventCalc.None;
            Type = OutcomeType.None;
        }
    }

    /// <summary>
    /// Player outcome -> Initiate a conflict
    /// </summary>
    class OutConflict : Outcome
    {
        public bool Challenger { get; set; } //is the Player the Challenger?
        public ConflictType Conflict_Type { get; set; }
        public ConflictCombat Combat_Type { get; set; }
        public ConflictSocial Social_Type { get; set; }
        public ConflictStealth Stealth_Type { get; set; }
        public ConflictSubType SubType { get; set; } //descriptive purposes only
        public Challenge challenge; //used for adding data that overides the default challenge settings

        public OutConflict(int eventID, int opponentID, ConflictType type, bool challenger = true) : base (eventID)
        {
            this.Data = opponentID;
            Conflict_Type = type;
            this.Challenger = challenger;
            Type = OutcomeType.Conflict;
            challenge = new Challenge(ConflictType.Special, ConflictCombat.None, ConflictSocial.None, ConflictStealth.None); //special mode Challenge (purely for data overides)
        }
    }


    /// <summary>
    /// Player outcome -> changes a GameState variable, eg. Justice
    /// </summary>
    class OutGameState : Outcome
    {
        public OutGameState(int eventID, int type, int amount, EventCalc apply = EventCalc.None) : base(eventID)
        {
            this.Data = type;
            this.Amount = amount;
            Calc = apply;
            Type = OutcomeType.GameState;
        }
    }


    /// <summary>
    /// Player outcome -> NPC character changes their relationship level with Player
    /// </summary>
    class OutRelPlyr : Outcome
    {
        public string Tag { get; set; } //short (max 4 words) tag
        public string Description { get; set; } //longer description

        public OutRelPlyr(int eventID, int amount, EventCalc apply, string description, string tag) : base(eventID)
        {
            this.Amount = amount;
            Calc = apply;
            this.Description = description;
            this.Tag = tag;
            Type = OutcomeType.RelPlyr;
        }
    }

    /// <summary>
    /// Change Known Status (Known/Unknown). 
    /// </summary>
    class OutKnown : Outcome
    {
        // Data +ve then UnKnown, Data -ve then known

        public OutKnown(int eventID, int known) : base(eventID)
        {
            Data = known;
            Type = OutcomeType.Known;
        }
    }

    /// <summary>
    /// Change SafeHouse status (in / out)
    /// </summary>
    class OutSafeHouse : Outcome
    {
        // Data is +ve then entering safehouse, -ve then has left safehouse

        public OutSafeHouse(int eventID, int status) : base(eventID)
        {
            Data = status;
            Type = OutcomeType.SafeHouse;
        }
    }

    /// <summary>
    /// Give a disguise to the Player (from an Advisor) (actorID provided by option.ActorID and all details handled automatically)
    /// </summary>
    class OutDisguise : Outcome
    {
        public OutDisguise(int eventID) : base(eventID)
        { Type = OutcomeType.Disguise; }

    }

    /// <summary>
    /// Player asks around for information at a location (CreateAutoLocEvent) and receives rumours -> Only used by CreateAutoLocEvent (fileimport.cs currently can't deal with it)
    /// </summary>
    class OutRumour : Outcome
    {
        public OutRumour(int eventID) : base(eventID)
        { Type = OutcomeType.Rumour; }
    }

    /// <summary>
    /// Player Observes his current location and gains information on a number of things
    /// </summary>
    class OutObserve : Outcome
    {
        public OutObserve(int eventID) : base(eventID)
        { Type = OutcomeType.Observe; }
    }

    /// <summary>
    /// Change a GameVar by an amount
    /// </summary>
    class OutGameVar : Outcome
    {
        public GameVar GameVar { get; set; }

        public OutGameVar(int eventID, int index, int amount, EventCalc apply) : base(eventID)
        {
            Data = index;
            GameVar = Game.variable.GetGameVar(index);
            this.Amount = amount;
            Calc = apply;
            Type = OutcomeType.GameVar;
        }
    }

    /// <summary>
    /// if Data > 0, player is free'd (ActorStatus.AtLocation), if Data < 0, player is Captured (ActorStatus.Captured) NOTE: Only applies to Player
    /// </summary>
    class OutFreedom : Outcome
    {
        public OutFreedom(int eventID, int free) : base(eventID)
        {
            Data = free;
            Type = OutcomeType.Freedom;
        }
    }

    /// <summary>
    /// Gain or lose an item
    /// </summary>
    class OutItem : Outcome
    {
        //Calc -> Add to gain, Subtract to lose
        //Data -> select from: +ve Active items only, -ve Passive items only, '0' all items

        public OutItem(int eventID, int itemType, EventCalc calc) : base(eventID)
        {
            Data = itemType;
            this.Calc = calc;
            Type = OutcomeType.Item;
        }
    }

    /// <summary>
    /// NPC gives you a favour which can be cashed in at a later date 
    /// </summary>
    class OutFavour : Outcome
    {
        //Data is the strength of the favour (1 to 5)

        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="eventID"></param>
        /// <param name="strength">between 1 and 5 (highest)</param>
        public OutFavour(int eventID, int strength) : base(eventID)
        {
            if (strength > 0 && strength < 6)
            { Data = strength; }
            else { Game.SetError(new Error(238, "Invalid strength input (\"{strength}\") must be between 1 & 5 -> assigned default value 1")); Data = 1; }
            Type = OutcomeType.Favour;
        }
    }

    /// <summary>
    /// Player uses an introduction to gain access to a House court audience
    /// </summary>
    class OutIntroduction : Outcome
    {
        //Data is the RefID of the Major house where the introduction applies

        public OutIntroduction(int eventID, int refID) : base(eventID)
        {
            Data = refID;
            Type = OutcomeType.Introduction;
        }
    }

    /// <summary>
    /// Player makes a promise
    /// </summary>
    class OutPromise : Outcome
    {
        //Data -> strength
        public PossPromiseType PromiseType { get; set; }

        public OutPromise(int eventID, PossPromiseType type, int strength) : base(eventID)
        {
            if (strength > 0 && strength < 6) { Data = strength; } else { Game.SetError(new Error(229, $"Invalid Strength input \"{strength}\" -> assigned default value 3")); Data = 3; }
            PromiseType = type;
            Type = OutcomeType.Promise;
        }
    }


    /// <summary>
    /// Player outcome -> change an Event's Timer
    /// </summary>
    class OutEventTimer : Outcome
    {
        public EventTimer Timer { get; set; }

        public OutEventTimer(int eventID, int targetEventID, int amount, EventCalc apply, EventTimer timer) : base(eventID)
        {
            Data = targetEventID;
            this.Amount = amount;
            this.Calc = apply;
            this.Timer = timer;
            Type = OutcomeType.EventTimer;
        }
    }

    /// <summary>
    /// Player outcome -> change an Event's Status
    /// </summary>
    class OutEventStatus : Outcome
    {
        public EventStatus NewStatus { get; set; }
        //

        public OutEventStatus(int eventID, int targetEventID, EventStatus status) : base(eventID)
        {
            NewStatus = status;
            Data = targetEventID;
            Type = OutcomeType.EventStatus;
        }
    }


    /// <summary>
    /// Chain Event outcome ->  immediate trigger of specified Player event
    /// </summary>
    class OutEventChain : Outcome
    {
        public EventFilter Filter { get; set; }
       
        public OutEventChain(int eventID, EventFilter filter) : base(eventID)
        {
            Data = 0;
            this.Filter = filter;
            Type = OutcomeType.EventChain;
        }
    }

    /// <summary>
    /// Change resource level of an actor
    /// </summary>
    class OutResource : Outcome
    {
        public bool PlayerRes { get; set; } //if true adjust Player resource level, otherwise option actorID

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="eventID"></param>
        /// <param name="playerRes">If true then it's the Player affected, otherwise opponent</param>
        /// <param name="amount">Remember resources remain within a 1 to 5 range</param>
        /// <param name="apply">Allowable options are Add / Subtract / Equals</param>
        public OutResource(int eventID, bool playerRes, int amount, EventCalc apply) : base(eventID)
        {
            this.Amount = amount;
            this.Calc = apply;
            this.PlayerRes = playerRes;
            Type = OutcomeType.Resource;
        }
    }

    /// <summary>
    /// Player commences a sea voyage (outcome only available through auto events)
    /// </summary>
    class OutPassage : Outcome
    {
        //Data is Estimated voyage time (turns)
        public int DestinationID { get; set; }
        public bool VoyageSafe { get; set; } //flag if true, a safe ship, if false, a risky one

        public OutPassage(int eventID, int locID, int voyageTime, bool safePassage = true) : base(eventID)
        {
            DestinationID = locID;
            Data = voyageTime;
            VoyageSafe = safePassage;
            Type = OutcomeType.Passage;
        }

    }

    /// <summary>
    /// VoyageTime increased or decreased
    /// </summary>
    class OutVoyageTime : Outcome
    {
        public OutVoyageTime(int eventID, int amount, EventCalc apply) : base(eventID)
        {
            this.Amount = amount;
            this.Calc = apply;
            Type = OutcomeType.VoyageTime;
        }
    }

    /// <summary>
    /// Player Cast Adrift at sea. Death timer kicks in.
    /// </summary>
    class OutAdrift : Outcome
    {
        public int DeathTimer { get; set; } //what value to assign the death timer
        public bool ShipSunk { get; set; } //true if ship that Player was on sinks

        public OutAdrift(int eventID, bool shipSunk, int timer = 10) : base(eventID)
        {
            DeathTimer = timer;
            ShipSunk = shipSunk;
            Type = OutcomeType.Adrift;
        }
    }

    /// <summary>
    /// Player rescued from drifting around the ocean by a passing Merchant vessel
    /// </summary>
    class OutRescued : Outcome
    {
        public bool Safe { get; set; } //is the rescuing merchant a safe, or unsafe, vessel

        public OutRescued(int eventID, bool shipSafe) : base(eventID)
        {
            Safe = shipSafe;
            Type = OutcomeType.Rescued;
        }
    }

    /// <summary>
    /// A new Follower is recruited from an Inn
    /// </summary>
    class OutFollower : Outcome
    {

        public OutFollower(int eventID) : base(eventID)
        {
            Type = OutcomeType.Follower;
        }
    }


    /// <summary>
    /// Player's death timer (applies in Adrift and Dungeon situations) goes up or down (Add/Subtract)
    /// </summary>
    class OutDeathTimer : Outcome
    {
        public OutDeathTimer(int eventID, int amount, EventCalc apply) : base(eventID)
        {
            this.Amount = amount;
            this.Calc = apply;
            Type = OutcomeType.DeathTimer;
        }
    }

    /*/// <summary>
    /// Change Player's travel mode (foot / mounted) -> NOTE: Made redundant by OutHorseStatus
    /// </summary>
    class OutTravel : Outcome
    {
        public TravelMode Mode;

        public OutTravel(int eventID, TravelMode newMode) : base(eventID)
        {
            Mode = newMode;
            Type = OutcomeType.Travel;
        }
    }*/

    /// <summary>
    /// Change Player's horse health level (range 0 to 5)
    /// </summary>
    class OutHorseHealth : Outcome
    {
        //Amount and Calc for change in Stamina

        public OutHorseHealth(int eventID, int amount, EventCalc calc) : base(eventID)
        {
            this.Amount = amount;
            this.Calc = calc;
            Type = OutcomeType.HorseHealth;
        }
    }

    /// <summary>
    /// Change Player's horse status
    /// </summary>
    class OutHorseStatus : Outcome
    {
        public HorseStatus Status { get; set; }
        public HorseGone Gone { get; set; }

        public OutHorseStatus(int eventID, HorseStatus newStatus, HorseGone goneStatus = HorseGone.None) : base(eventID)
        {
            Status = newStatus;
            Gone = goneStatus;
            Type = OutcomeType.HorseStatus;
        }

    }

    /// <summary>
    /// Applies a condition to an actor
    /// </summary>
    class OutCondition : Outcome
    {
        public bool PlayerCondition { get; set; } //if true condition applies to Player, otherwise opponent
        public Condition NewCondition;

        public OutCondition(int eventID, bool playerCondition, Condition condition) : base(eventID)
        {
            this.PlayerCondition = playerCondition;
            if (condition != null)
            {
                NewCondition = new Condition(condition.Skill, condition.Effect, condition.Text, condition.Timer);
                /*NewCondition.Text = condition.Text;
                NewCondition.Skill = condition.Skill;
                NewCondition.Effect = condition.Effect;
                NewCondition.Timer = condition.Timer;*/
            }
            else { Game.SetError(new Error(130, "Invalid Condition input (null)")); }
            Type = OutcomeType.Condition;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="outcome"></param>
        public OutCondition(OutCondition condition) : base(condition.EventID)
        {
            PlayerCondition = condition.PlayerCondition;
            NewCondition = new Condition(condition.NewCondition.Skill, condition.NewCondition.Effect, condition.NewCondition.Text, condition.NewCondition.Timer);
            /*NewCondition.Text = condition.NewCondition.Text;
            NewCondition.Skill = condition.NewCondition.Skill;
            NewCondition.Effect = condition.NewCondition.Effect;
            NewCondition.Timer = condition.NewCondition.Timer;*/
        }
    }

}
