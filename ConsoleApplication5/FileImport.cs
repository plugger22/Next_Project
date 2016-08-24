﻿using System;
using System.Collections.Generic;
using System.IO;


namespace Next_Game
{
    //holds data read in from house.txt. Used for pool of houses.
    public struct HouseStruct
    {
        public string Name { get; set; }
        public string Motto { get; set; }
        public string Banner { get; set; }
        public int Archetype { get; set; }
        public int RefID { get; set; }
        public string Capital { get; set; }
    }



    public class FileImport
    {
        string fileDirectory;


        public FileImport(string dir)
        {
            fileDirectory = dir;
        }

        /// <summary>
        /// Standard text file importer, returns an array of data
        /// </summary>
        /// <param name="name">"filename.txt"</param>
        /// <returns></returns>
        private string[] ImportFileData(string fileName)
        {
            string[] importedText = null;
            string path = fileDirectory + fileName;
            if (File.Exists(path))
            { importedText = File.ReadAllLines(path); }
            else
            { Console.WriteLine("ERROR: History.cs, FileImport failed, file name {0}", fileName); }
            return importedText;
        }

        /// <summary>
        /// handles lists of Names
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public List<string> GetNames(string fileName)
        {
            //
            // read in lists of First Male and Female names ---
            //
            string[] arrayOfCharacterNames = ImportFileData(fileName);
            List<string> listOfNames = new List<string>();
            string tempString = null;
            //read male names from array into list
            for (int i = 0; i < arrayOfCharacterNames.Length; i++)
            {
                if (arrayOfCharacterNames[i] != "" && !arrayOfCharacterNames[i].StartsWith("#"))
                {
                    //trim off leading and trailing whitespace
                    tempString = arrayOfCharacterNames[i];
                    tempString = tempString.Trim();
                    listOfNames.Add(tempString);
                }
            }
            return listOfNames;
        }


        public List<HouseStruct> GetHouses(string fileName)
        {
            string[] arrayOfHouseNames = ImportFileData(fileName);
            List<HouseStruct> listHouses = new List<HouseStruct>();
            bool newHouse = false;
            int dataCounter = 0; //number of houses
            HouseStruct houseStruct = new HouseStruct();
            string cleanToken;
            string cleanTag;
            for (int i = 0; i < arrayOfHouseNames.Length; i++)
            {
                if (arrayOfHouseNames[i] != "" && !arrayOfHouseNames[i].StartsWith("#"))
                {
                    //set up for a new house
                    if (newHouse == false)
                    {
                        newHouse = true;
                        //Console.WriteLine();
                        dataCounter++;
                        //new structure
                        houseStruct = new HouseStruct();
                    }
                    string[] tokens = arrayOfHouseNames[i].Split(':');
                    //strip out leading spaces
                    cleanTag = tokens[0].Trim();
                    cleanToken = tokens[1].Trim();
                    //Console.WriteLine("{0}: {1}", tokens[0], tokens[1]);
                    switch (cleanTag)
                    {
                        case "House":
                            houseStruct.Name = cleanToken;
                            break;
                        case "Motto":
                            houseStruct.Motto = cleanToken;
                            break;
                        case "Banner":
                            houseStruct.Banner = cleanToken;
                            break;
                        case "ArchetypeID":
                            houseStruct.Archetype = Convert.ToInt32(cleanToken);
                            break;
                        case "ReferenceID":
                            houseStruct.RefID = Convert.ToInt32(cleanToken);
                            break;
                        case "Capital": //Major Houses
                            houseStruct.Capital = cleanToken;
                            //last datapoint - save structure to list
                            if (dataCounter > 0)
                            { listHouses.Add(houseStruct); }
                            break;
                        case "Seat": //Minor Houses
                            houseStruct.Capital = cleanToken;
                            //last datapoint - save structure to list
                            if (dataCounter > 0)
                            { listHouses.Add(houseStruct); }
                            break;
                    }
                }
                else
                { newHouse = false; }
            }
            return listHouses;
        }

        /// <summary>
        /// read in an initialise Constants
        /// </summary>
        /// <param name="fileName"></param>
        public void InitialiseConstants(string fileName)
        {
            string[] arrayOfFileInput = ImportFileData(fileName); ;
            Console.WriteLine();
            Console.WriteLine("--- Constants");
            string cleanToken = null;
            string cleanTag = null;
            int index = 0;
            int value = 0;
            Global enumTag = Global.None;
            for (int i = 0; i < arrayOfFileInput.Length; i++)
            {
                if (arrayOfFileInput[i] != "" && !arrayOfFileInput[i].StartsWith("#"))
                {
                    string[] tokens = arrayOfFileInput[i].Split(':');
                    //strip out leading spaces
                    cleanTag = tokens[0].Trim();
                    cleanToken = tokens[1].Trim();
                    //convert to #'s
                    index = Convert.ToInt32(cleanTag);
                    value = Convert.ToInt32(cleanToken);
                    //get correct enum from Global array
                    enumTag = Game.constant.GetGlobal(index);
                    //initialise data in Constants array
                    Game.constant.SetData(enumTag, value);
                }
            }
        }

        internal List<Trait> GetTraits(string fileName, TraitSex sex)
        {
            int dataCounter = 0;
            string cleanTag;
            string cleanToken;
            bool newTrait = false;
            List<Trait> listOfTraits = new List<Trait>();
            string[] arrayOfTraits = ImportFileData(fileName); ;
            TraitStruct structTrait = new TraitStruct();
            //loop imported array of strings
            for (int i = 0; i < arrayOfTraits.Length; i++)
            {
                if (arrayOfTraits[i] != "" && !arrayOfTraits[i].StartsWith("#"))
                {
                    //set up for a new house
                    if (newTrait == false)
                    {
                        newTrait = true;
                        //Console.WriteLine();
                        dataCounter++;
                        //new Trait object
                        structTrait = new TraitStruct();
                        //sex
                        structTrait.Sex = sex;
                    }
                    string[] tokens = arrayOfTraits[i].Split(':');
                    //strip out leading spaces
                    cleanTag = tokens[0].Trim();
                    cleanToken = tokens[1].Trim();
                    switch (cleanTag)
                    {
                        case "Name":
                            structTrait.Name = cleanToken;
                            break;
                        case "Skill":
                            switch (cleanToken)
                            {
                                case "Combat":
                                    structTrait.Type = TraitType.Combat;
                                    break;
                                case "Wits":
                                    structTrait.Type = TraitType.Wits;
                                    break;
                                case "Charm":
                                    structTrait.Type = TraitType.Charm;
                                    break;
                                case "Treachery":
                                    structTrait.Type = TraitType.Treachery;
                                    break;
                                case "Leadership":
                                    structTrait.Type = TraitType.Leadership;
                                    break;
                            }
                            break;
                        case "Effect":
                            structTrait.Effect = Convert.ToInt32(cleanToken);
                            break;
                        case "Chance":
                            structTrait.Chance = Convert.ToInt32(cleanToken);
                            break;
                        case "Age":
                            int tempNum = Convert.ToInt32(cleanToken);
                            if (tempNum == 5)
                            { structTrait.Age = TraitAge.Five; }
                            else
                            { structTrait.Age = TraitAge.Fifteen; }
                            break;
                        case "Nicknames":
                            //get list of nicknames
                            string[] arrayOfNames = cleanToken.Split(',');
                            List<string> tempList = new List<string>();
                            //loop nickname array and add all to lists
                            string tempHandle = null;
                            for (int k = 0; k < arrayOfNames.Length; k++)
                            {
                                tempHandle = arrayOfNames[k].Trim();
                                if (String.IsNullOrEmpty(tempHandle) == false)
                                { tempList.Add(tempHandle); }
                            }
                            //pass info over to a class instance
                            Trait classTrait = new Trait(structTrait.Name, structTrait.Type, structTrait.Effect, structTrait.Sex, structTrait.Age, structTrait.Chance, tempList);
                            //last datapoint - save object to list
                            if (dataCounter > 0)
                            { listOfTraits.Add(classTrait); }
                            break;
                    }
                }
            }
            return listOfTraits;
        }

        //methods above here
    }
}