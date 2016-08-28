﻿using System;
using System.Collections.Generic;
using System.Linq;


namespace Next_Game
{
    //
    // Base class ---
    //
    class House
    {
        public string Name { get; set; }
        public string Motto { get; set; }
        public string Banner { get; set; }
        public string LocName { get; set; }
        public int HouseID { get; set; } = 0; //unique to Great House (allocated by Network.cs)
        public int LocID { get; set; } //unique to location
        public int RefID { get; set; } //unique to house (great or minor)
        public int ArchetypeID { get; set; }
        public int Branch { get; set; }
        private List<int> listOfFirstNames; //contains ID #'s (listOfMaleFirstNames index) of all first names used by males within the house (eg. 'Eddard Stark II')


        public House()
        { listOfFirstNames = new List<int>(); }

        /// <summary>
        /// adds ID to list of names and returns # of like names in list
        /// </summary>
        /// <param name="nameID"></param>
        /// <returns></returns>
        public int AddName(int nameID)
        {
            int numOfLikeNames = 1;
            listOfFirstNames.Add(nameID);
            numOfLikeNames = listOfFirstNames.Count(i => i.Equals(nameID));
            return numOfLikeNames;
        }

    }

    //
    // Great house ---
    //
    class MajorHouse : House
    {
        private List<int> listLordLocations;
        private List<int> listHousesToCapital; //unique houses (HID), ignoring special locations
        private List<int> listHousesToConnector; //unique houses (HID), ignoring special locations
        

        public MajorHouse()
        {
            listLordLocations = new List<int>();
            listHousesToCapital = new List<int>();
            listHousesToConnector = new List<int>();
            
        }

        public int GetNumBannerLords()
        { return listLordLocations.Count; }

        /// <summary>
        /// add a location to list of house controlled locations
        /// </summary>
        /// <param name="locID"></param>
        public void AddLordLocations(int locID)
        { listLordLocations.Add(locID); }

        /// <summary>
        /// add a house ID to list of unique houses to capital
        /// </summary>
        /// <param name="houseID"></param>
        public void AddHousesToCapital(int houseID)
        {
            //check houseID isn't already in list (only unique HouseID's are stored)
            if (houseID > 0)
            {
                if (listHousesToCapital.Contains(houseID) == false)
                { listHousesToCapital.Add(houseID); }
            }
        }

        

        /// <summary>
        /// returns list of Lords (subsidary bannerlord locations)
        /// </summary>
        /// <returns></returns>
        public List<int> GetLords()
        { return listLordLocations; }

    }

    //
    //Bannerlords ---
    //
    class MinorHouse : House
    {
        public MinorHouse()
        { }

    }
}