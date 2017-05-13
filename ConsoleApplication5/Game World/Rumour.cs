﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Next_Game
{
    public enum RumourType { None, Local, Global}
    public enum RumourTopic { None, Sea, Land, Road, Character}
    public enum RumourGlobal { None, North, East, South, West, All}

    /// <summary>
    /// handles all rumours, eg. 'Ask around for information'
    /// </summary>
    class Rumour
    {
        private static int rumourIndex = 1; //provides a unique ID to each rumour
        public int RumourID { get; } 
        public string Text { get; set; }
        public int Strength { get; set; } //strength of rumour -> 1 to 5 (highest)
        public bool Active { get; set; } //rumour only used if Active is true
        public int RefID { get; set; } //Optional -> location specific rumour, default '0'
        public int TurnCreated { get; set; } //Turn when rumour became active (used to show age of rumour)
        public int TimerStart { get; set; } //Turns before rumour becomes active, default '0'
        public int TimerExpire { get; set; } //Turns before rumour becomes inactive (expires), default '0'
        public RumourType Type { get; set; }
        public RumourTopic Topic { get; set; }
        public RumourGlobal Global { get; set; } //Optional -> only applies if Type is RumourType.Global


        public Rumour()
        { }

        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="text"></param>
        /// <param name="strength"></param>
        /// <param name="type"></param>
        /// <param name="topic"></param>
        /// <param name="global"></param>
        public Rumour(string text, int strength, RumourType type, RumourTopic topic, RumourGlobal global = RumourGlobal.None, bool isActive = true)
        {
            RumourID = rumourIndex++;
            Text = this.Text;
            if (strength > 0 && strength < 6) { this.Strength = strength; }
            else { Game.SetError(new Error(261, $"Invalid Rumour Strength (\"{strength}\") for \"{text}\" -> assigned default Strength of 3")); Strength = 3; }
            Type = this.Type;
            Topic = this.Topic;
            Global = this.Global;
            Active = isActive;
            TurnCreated = Game.gameTurn;
        }



        //add new methods above here
    }
}
