﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Next_Game
{
    public enum RevoltReason {None, Stupid_OldKing, Treacherous_NewKing, Incapacited_OldKing, Dead_OldKing, Internal_Dispute, External_Event}

    /// <summary>
    /// Keeps track of all key game lore (backstory)
    /// </summary>
    public class Lore
    {
        static Random rnd;
        //HouseID's
        public int RoyalHouseOld { get; set; }
        public int RoyalHouseNew { get; set; }
        public int RoyalHouseCurrent { get; set; }
        //Royal & Rebel Family and retainers
        private List<Passive> listOfOldRoyals; //at time of revolt
        private List<Passive> listOfNewRoyals; //at time of taking power
        public Noble OldKing { get; set; }
        public Noble OldQueen { get; set; }
        public Noble OldHeir { get; set; }
        public Noble NewKing { get; set; }
        public Noble NewQueen { get; set; }
        public Noble NewHeir { get; set; }
        //reasons
        public RevoltReason WhyRevolt { get; set; } = RevoltReason.None;
        //each list a series of sentences that make a paragraph about the topic
        private List<string> listOldKingRule;
        private List<string> listRevoltBackStory;
        private List<string> listUprising;
        private List<string> listRoyalFamilyFate;
        private List<string> listNewKingRule;
        private List<string> listChanges; //location's, bannerlords etc.

        public Lore(int seed)
        {
            rnd = new Random(seed);
            listOfOldRoyals = new List<Passive>();
            listOfNewRoyals = new List<Passive>();
            listOldKingRule = new List<string>();
            listRevoltBackStory = new List<string>();
            listUprising = new List<string>();
            listRoyalFamilyFate = new List<string>();
            listNewKingRule = new List<string>();
            listChanges = new List<string>();
        }

        internal void SetListOfOldRoyals(List<Passive> listRoyals)
        { listOfOldRoyals?.AddRange(listRoyals); }

        internal void SetListOfNewRoyals(List<Passive> listRebels)
        { listOfNewRoyals?.AddRange(listRebels); }


        /// <summary>
        /// generates reason and populates lore lists
        /// </summary>
        internal void CreateOldKingBackStory()
        {
            //list of possible reasons - weighted entries, one chosen at completion
            List<RevoltReason> listWhyPool = new List<RevoltReason>();

            //check how smart old king was (takes into account wife's possible influence)
            int oldKing_Wits;
            int influencer = OldKing.Influencer;
            if (influencer > 0 && Game.world.CheckActorPresent(influencer, 1) && OldKing.CheckTraitInfluenced(TraitType.Wits))
            { oldKing_Wits = OldKing.GetTrait(TraitAge.Fifteen, TraitType.Wits, true); }
            else { oldKing_Wits = OldKing.GetTrait(TraitAge.Fifteen, TraitType.Wits); }
            //dumb king (1 pool entry if wits 2 stars and 4 entries if wits 1 star)
            if (oldKing_Wits == 2) { listWhyPool.Add(RevoltReason.Stupid_OldKing); } 
            else if (oldKing_Wits == 1) { for (int i = 0; i < 4; i++) { listWhyPool.Add(RevoltReason.Stupid_OldKing); } }

            //check new king treachery
            int newKing_Treachery;
            influencer = NewKing.Influencer;
            if (influencer > 0 && Game.world.CheckActorPresent(influencer, 1) && NewKing.CheckTraitInfluenced(TraitType.Treachery))
            { newKing_Treachery = NewKing.GetTrait(TraitAge.Fifteen, TraitType.Treachery, true); }
            else { newKing_Treachery = NewKing.GetTrait(TraitAge.Fifteen, TraitType.Treachery); }
            //treacherous new king grabs power (1 pool entry if 4 starts, 4 entries if treachery 5 stars)
            if (newKing_Treachery == 4)
            { listWhyPool.Add(RevoltReason.Treacherous_NewKing); } 
            else if (newKing_Treachery == 5) { for (int i = 0; i < 4; i++) { listWhyPool.Add(RevoltReason.Treacherous_NewKing); } }

            //3 entries for old king being incapacitated
            for (int i = 0; i < 3; i++) { listWhyPool.Add(RevoltReason.Incapacited_OldKing); }
            //2 entries for old king dying
            for (int i = 0; i < 2; i++) { listWhyPool.Add(RevoltReason.Dead_OldKing); }
            //3 entries for an internal dispute
            for (int i = 0; i < 3; i++) { listWhyPool.Add(RevoltReason.Internal_Dispute); }
            //4 entries for an external event
            for (int i = 0; i < 3; i++) { listWhyPool.Add(RevoltReason.External_Event); }

            //choose a random reason from the pool
            WhyRevolt = listWhyPool[rnd.Next(0, listWhyPool.Count)];

            Console.WriteLine(Environment.NewLine + "--- Create BackStory");
            Console.WriteLine("Old King Wits {0} Aid {1}, {2}", oldKing_Wits, OldKing.ActID, OldKing.Name);
            Console.WriteLine("New King Treachery {0} Aid {1}, {2}", newKing_Treachery, NewKing.ActID, NewKing.Name);
            Console.WriteLine("WhyRevolt: {0}", WhyRevolt);
        }
    }
}