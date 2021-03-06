﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Next_Game.Event_System
{

    public enum ResultType { None, GameState, GameVar, RelPlyr, RelOther, Condition, Resource, Item, Secret, Favour, Introduction, Army, Event, Known, Freedom, Count} 
    //relationship change with actor (RelPlyr -> actors rel. with Plyr, RelOther -> actors rel with another actor), condition affect on actor, event triggered
    //use GameStates for main game state adjustements (eg. relative fame) and GameVars for everything else.
    //NOTE: Conflict.cs ResolveResults uses the above for a switch statement that needs to be tweaked if the above ResultType enum is changed or added to.

    /// <summary>
    /// Conflict Results (Events have outcomes). Single class, access different results via enum ResultType
    /// </summary>
    class Result
    {
        public string Description { get; set; }
        public string Tag { get; set; } //used for relationship short descriptor tags. Ignore if not required.
        public int ResultID { get; set; } //user specified
        public ResultType Type { get; set; }
        public int Data { get; set; } //multipurpose
        public EventCalc Calc { get; set; }
        public int Amount { get; set; }
        public int Test { get; set; } = 0; //optional -> if > 0 then a 1d100 <= Test must occur for result to happen, otherwise ignored.
        public GameState GameState { get; set; } //optional -> GameState
        public GameVar GameVar { get; set; } //optional -> GameVar
        public bool ConPlayer { get; set; } //optional -> Conditions
        public string ConText { get; set; } //optional -> Conditions
        public SkillType ConSkill { get; set; } //optional -> Conditions
        public int ConEffect { get; set; } //optional -> Conditions
        public int ConTimer { get; set; } //optional -> Conditions
        

        public Result()
        { }

        /// <summary>
        /// default constructor -> resultID must be > 0, description must be a valid string and ResultType can't be 'None'
        /// </summary>
        /// <param name="description"></param>
        /// <param name="type"></param>
        /// <param name="data"></param>
        /// <param name="calc"></param>
        /// <param name="amount"></param>
        public Result(int resultID, string description, ResultType type, int data, EventCalc calc, int amount)
        {
            if (resultID > 0)
            {
                this.ResultID = resultID;
                if (String.IsNullOrEmpty(description) == false)
                {
                    this.Description = description;
                    if (type > ResultType.None)
                    {
                        this.Type = type;
                        this.Data = data;
                        this.Calc = calc;
                        this.Amount = amount;
                        if (type == ResultType.GameVar)
                        { GameVar = Game.variable.GetGameVar(data); }
                    }
                    else { Game.SetError(new Error(114, "Invalid ResultType input (\"None\")")); }
                }
                else { Game.SetError(new Error(114, "Invalid description input (null or empty)")); }
            }
            else { Game.SetError(new Error(114, "Invalid resultID input (Zero or less)")); }
        }
    }
}
