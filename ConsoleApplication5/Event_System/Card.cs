﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Next_Game.Event_System
{

    public enum CardCategory { None, Conflict, Item, Secret, Companion, Disguise }
    public enum CardConflict { None, Skill, Trait, Situation, Companion, Item, Supporter }
    public enum CardType { None, Good, Bad, Neutral}
    

    /// <summary>
    /// Base class
    /// </summary>
    class Card
    {
        public CardType Type { get; set; } //General type
        public CardCategory Category { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        

        public Card()
        { }

        public Card(CardType type, string title, string description)
        {
            this.Type = type;
            this.Title = title;
            this.Description = description;
        }
    }

    /// <summary>
    /// Conflict Cards
    /// </summary>
    class Card_Conflict : Card
    {
        public int Effect { get; set; } //eg. 1X, 2X, etc.
        public string Special { get; set; }
        public CardConflict Conflict_Type { get; set; }
        public CardType TypeAttack { get; set; } //how a situation card is treated if defender chooses an Attack strategy (default None)
        public CardType TypeBalanced { get; set; } //how a situation card is treated if defender chooses a balanced strategy (default None)
        public CardType TypeDefend { get; set; } //how a situation card is treated if defender chooses a defend strategy (default None)

        public Card_Conflict()
        { }

        /// <summary>
        /// Default constructor
        /// </summary>
        public Card_Conflict(CardConflict conflictType, CardType type, string title, string description, int effect = 1) : base (type, title, description)
        {
            Category = CardCategory.Conflict;
            Conflict_Type = conflictType;
            this.Effect = effect;
            //default values of None for Types (indicates normal use, no restrictions)
            TypeAttack = CardType.None;
            TypeBalanced = CardType.None;
            TypeDefend = CardType.None;
        }

    }


}