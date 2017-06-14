﻿using System;
using System.Collections.Generic;
using System.Linq;
using RLNET;
using System.Text;
using System.Threading.Tasks;

namespace Next_Game
{
    /// <summary>
    /// handles all display methods (separates output code from world.cs)
    /// </summary>
    public class Display
    {

        public Display()
        { }

        /// <summary>
        /// places a message in info panel detailing all relevant data for a single generation
        /// </summary>
        public void ShowGeneratorStatsRL()
        {
            List<Snippet> listStats = new List<Snippet>();
            RLColor houseColor;
            Dictionary<int, MajorHouse> dictMajorHouses = Game.world.GetMajorHouses();
            Dictionary<int, House> dictAllHouses = Game.world.GetAllHouses();
            Dictionary<int, Actor> dictAllActors = Game.world.GetAllActors();
            Dictionary<int, int> dictHousePower = Game.world.GetHousePower();
            int[] arrayTradeData = Game.world.GetTradeData();
            //calcs
            int numLocs = Game.network.GetNumLocations();
            int numGreatHouses = dictMajorHouses.Count;
            int numSpecialLocs = Game.network.GetNumSpecialLocations();
            int numBannerLords = dictAllHouses.Count - numGreatHouses - 1 - numSpecialLocs;
            int numActors = dictAllActors.Count;
            int numChildren = numActors - (numGreatHouses * 2) - numBannerLords;
            int numSecrets = Game.world.GetPossessionsCount(PossessionType.Secret);
            int numRumours = Game.world.GetRumoursNormalCount();
            int numTimedRumours = Game.world.GetRumoursTimedCount();
            //checksum
            if (numLocs != numGreatHouses + numSpecialLocs + numBannerLords)
            { Game.SetError(new Error(25, "Locations don't tally")); }
            int numErrors = Game.GetErrorCount();
            //data
            listStats.Add(new Snippet("--- Generation Statistics", RLColor.Yellow, RLColor.Black));
            listStats.Add(new Snippet(string.Format("{0} Locations", numLocs)));
            listStats.Add(new Snippet(string.Format("{0} Great Houses", numGreatHouses)));
            listStats.Add(new Snippet(string.Format("{0} BannerLords", numBannerLords)));
            listStats.Add(new Snippet(string.Format("{0} Special Locations", numSpecialLocs)));
            listStats.Add(new Snippet("1 Capital"));
            listStats.Add(new Snippet(string.Format("{0} Actors ({1} Children)", numActors, numChildren)));
            listStats.Add(new Snippet(string.Format("{0} Secrets", numSecrets)));
            listStats.Add(new Snippet(string.Format("{0} Total Rumours  ({1} Normal, {2} Timed)", numRumours + numTimedRumours, numRumours, numTimedRumours)));
            if (numErrors > 0) { listStats.Add(new Snippet(string.Format("{0} Errors", numErrors), RLColor.LightRed, RLColor.Black)); }
            //Total population and food capacity
            int food = 0; int population = 0;
            foreach (var house in dictAllHouses)
            {
                food += house.Value.FoodCapacity;
                population += house.Value.Population;
            }
            listStats.Add(new Snippet($"Total Population {population:N0}, Total Food Capacity {food:N0} Surplus/Shortfall {food - population:N0}"));
            string tradeText = string.Format("Total Net World Wealth {0}{1}", arrayTradeData[0] > 0 ? "+" : "", arrayTradeData[0]);
            listStats.Add(new Snippet(tradeText));
            string goodsText = string.Format("Goods: Iron x {0}, Timber x {1}, Gold x {2}, Wine x {3}, Oil x {4}, Wool x {5}, Furs x {6}", arrayTradeData[(int)Goods.Iron], arrayTradeData[(int)Goods.Timber],
                arrayTradeData[(int)Goods.Gold], arrayTradeData[(int)Goods.Wine], arrayTradeData[(int)Goods.Oil], arrayTradeData[(int)Goods.Wool], arrayTradeData[(int)Goods.Furs]);
            listStats.Add(new Snippet(goodsText));
            //list of all Greathouses by power
            listStats.Add(new Snippet("Great Houses", RLColor.Yellow, RLColor.Black));
            string housePower;
            foreach (var power in dictHousePower)
            {
                MajorHouse house = Game.world.GetMajorHouse(power.Key);
                housePower = string.Format("Hid {0} House {1} has {2} BannerLords  {3}, Loyal to the {4} (orig {5})", house.HouseID, house.Name, house.GetNumBannerLords(),
                    Game.world.GetLocationCoords(house.LocID), house.Loyalty_Current, house.Loyalty_AtStart);
                //highlight great houses still loyal to the old king
                if (house.Loyalty_Current == KingLoyalty.New_King) { houseColor = RLColor.White; }
                else { houseColor = Color._goodTrait; }
                listStats.Add(new Snippet(housePower, houseColor, RLColor.Black));
            }

            //if start of game also show Errors
            if (Game.gameTurn == 0)
            {
                List<Snippet> tempList = Game.ShowErrorsRL();
                if (tempList.Count > 0)
                {
                    listStats.Add(new Snippet(""));
                    listStats.Add(new Snippet(""));
                    listStats.Add(new Snippet(""));
                    listStats.Add(new Snippet("--- Errors ALL", RLColor.LightRed, RLColor.Black));
                    listStats.AddRange(tempList);
                }
            }
            //display data
            Game.infoChannel.SetInfoList(listStats, ConsoleDisplay.Multi);
        }


        public void ShowKingGroupsRL()
        {
            List<Snippet> listDisplay = new List<Snippet>();
            CapitalHouse capital = Game.world.GetCapital();
            RLColor starColor;
            int relLvl, stars;
            int spacer = 2; //number of blank lines between data groups
            if (capital != null)
            {
                //World Groups
                listDisplay.Add(new Snippet("--- KingsKeep Groups", RLColor.Yellow, RLColor.Black));
                for (int i = 1; i < (int)WorldGroup.Count; i++)
                {
                    relLvl = capital.GetGroupRelations((WorldGroup)i);
                    stars = relLvl / 20 + 1;
                    stars = Math.Min(5, stars);
                    if (stars <= 2) { starColor = RLColor.LightRed; } else { starColor = RLColor.Yellow; }
                    listDisplay.Add(new Snippet($"{(WorldGroup)i,-25}", false));
                    listDisplay.Add(new Snippet($"{GetStars(stars),-15}", starColor, RLColor.Black, false));
                    listDisplay.Add(new Snippet($"Rel Lvl {relLvl}%", RLColor.LightGray, RLColor.Black));
                }
                //spacer
                for (int i = 0; i < spacer; i++)
                { listDisplay.Add(new Snippet("")); }
                //Lords
                Dictionary<int, MajorHouse> dictMajorHouses = Game.world.GetMajorHouses();
                if (dictMajorHouses != null)
                {
                    listDisplay.Add(new Snippet("--- Major Houses", RLColor.Yellow, RLColor.Black));
                    foreach (var house in dictMajorHouses)
                    {
                        Passive lord = Game.world.GetPassiveActor(house.Value.LordID);
                        if (lord != null)
                        {
                            relLvl = 100 - lord.GetRelPlyr();
                            stars = relLvl / 20 + 1;
                            stars = Math.Min(5, stars);
                            if (stars <= 2) { starColor = RLColor.LightRed; } else { starColor = RLColor.Yellow; }
                            listDisplay.Add(new Snippet($"{"House " + house.Value.Name,-25}", false));
                            listDisplay.Add(new Snippet($"{GetStars(stars),-15}", starColor, RLColor.Black, false));
                            listDisplay.Add(new Snippet($"Lord {lord.Name}, \"{ lord.Handle }\""));
                        }
                        else { Game.SetError(new Error(308, $"Invalid Lord (null) from house.Value.LordID {house.Value.LordID}")); }
                    }
                }
                else { Game.SetError(new Error(308, "Invalid dictMajorHouses (null) -> Lord rel's not shown")); }
                //spacer
                for (int i = 0; i < spacer; i++)
                { listDisplay.Add(new Snippet("")); }
                //Lenders
                listDisplay.Add(new Snippet("--- Lenders", RLColor.Yellow, RLColor.Black));
                for (int i = 1; i < (int)Finance.Count; i++)
                {
                    relLvl = capital.GetLenderRelations((Finance)i);
                    stars = relLvl / 20 + 1;
                    stars = Math.Min(5, stars);
                    if (stars <= 2) { starColor = RLColor.LightRed; } else { starColor = RLColor.Yellow; }
                    listDisplay.Add(new Snippet($"{(Finance)i,-25}", false));
                    listDisplay.Add(new Snippet($"{GetStars(stars),-15}", starColor, RLColor.Black, false));
                    listDisplay.Add(new Snippet($"Rel Lvl {relLvl}%", RLColor.LightGray, RLColor.Black));
                }

                //display data
                Game.infoChannel.SetInfoList(listDisplay, ConsoleDisplay.Multi);
            }
            else { Game.SetError(new Error(308, "Invalid Capital (null) -> no rel's shown")); }
        }



        /// <summary>
        /// creates a string showing the number of stars for traits, secrets, etc. (1 to 5 stars)
        /// </summary>
        /// <param name="num">number of stars</param>
        /// <returns></returns>
        internal string GetStars(int num)
        {
            string stars = null;
            num = Math.Min(5, num);
            num = Math.Max(1, num);
            for (int i = 0; i < num; i++)
            { stars += "o "; }
            return stars;
        }
        //methods above here
    }
}
