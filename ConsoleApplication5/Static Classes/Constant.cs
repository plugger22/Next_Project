﻿using System;


namespace Next_Game
{
    public enum Global
    {
        None, //keep as first
        INHERIT_TRAIT,
        CHILDBIRTH_DEATH,
        CHILDBIRTH_INFERTILE,
        PREGNANT,
        TOUCHED,
        MEN_AT_ARMS,
        BATTLE_EVENTS,
        ADVISOR_REFUSAL,
        SIBLING_ESCAPE,
        MOVE_SPEED,
        CHILDBIRTH_COMPLICATIONS,
        CROW_NUMBER,
        CROW_BONUS,
        MAP_LOCATIONS_MIN,
        MAP_LOCATIONS_MAX,
        MAP_FREQUENCY,
        MAP_SPACING,
        MAP_SIZE,
        GAME_YEAR,
        GAME_REVOLT,
        MAP_DIVISOR,
        CONNECTOR_MIN,
        CONNECTOR_MAX,
        TERRAIN_SMALL,
        SEA_LARGE,
        MOUNTAIN_LARGE,
        FOREST_LARGE,
        NUM_FOLLOWERS,
        GAME_START,
        Count //keep as last
    }

    public class Constant
    {
        private readonly Global[] arrayOfGlobals; //holds enums which provide indexes for arrayOfConstants
        private readonly int[] arrayOfConstants; //data which that's used in code, index is a Global enum

        public Constant()
            {
            arrayOfGlobals = new Global[(int)Global.Count];
            arrayOfConstants = new int[(int)Global.Count];
            //set up array of Globals
            arrayOfGlobals[1] = Global.INHERIT_TRAIT;
            arrayOfGlobals[2] = Global.CHILDBIRTH_DEATH;
            arrayOfGlobals[3] = Global.CHILDBIRTH_INFERTILE;
            arrayOfGlobals[4] = Global.PREGNANT;
            arrayOfGlobals[5] = Global.TOUCHED;
            arrayOfGlobals[6] = Global.MEN_AT_ARMS;
            arrayOfGlobals[7] = Global.BATTLE_EVENTS;
            arrayOfGlobals[8] = Global.ADVISOR_REFUSAL;
            arrayOfGlobals[9] = Global.SIBLING_ESCAPE;
            arrayOfGlobals[10] = Global.MOVE_SPEED;
            arrayOfGlobals[11] = Global.CHILDBIRTH_COMPLICATIONS;
            arrayOfGlobals[12] = Global.CROW_NUMBER;
            arrayOfGlobals[13] = Global.CROW_BONUS;
            arrayOfGlobals[14] = Global.MAP_LOCATIONS_MIN;
            arrayOfGlobals[15] = Global.MAP_LOCATIONS_MAX;
            arrayOfGlobals[16] = Global.MAP_FREQUENCY;
            arrayOfGlobals[17] = Global.MAP_SPACING;
            arrayOfGlobals[18] = Global.MAP_SIZE;
            arrayOfGlobals[19] = Global.GAME_YEAR;
            arrayOfGlobals[20] = Global.GAME_REVOLT;
            arrayOfGlobals[21] = Global.MAP_DIVISOR;
            arrayOfGlobals[22] = Global.CONNECTOR_MIN;
            arrayOfGlobals[23] = Global.CONNECTOR_MAX;
            arrayOfGlobals[24] = Global.TERRAIN_SMALL;
            arrayOfGlobals[25] = Global.SEA_LARGE;
            arrayOfGlobals[26] = Global.MOUNTAIN_LARGE;
            arrayOfGlobals[27] = Global.FOREST_LARGE;
            arrayOfGlobals[28] = Global.NUM_FOLLOWERS;
            arrayOfGlobals[29] = Global.GAME_START;
            }

        /// <summary>
        /// returns constant value
        /// </summary>
        /// <param name="globalVar"></param>
        /// <returns></returns>
        public int GetValue( Global globalVar)
        { return arrayOfConstants[(int)globalVar]; }

        /// <summary>
        /// returns a Global enum from the arrayOfGlobals
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Global GetGlobal( int index)
        {
            if (index < arrayOfGlobals.Length && index > 0)
            { return arrayOfGlobals[index]; }
            //return default value if an incorrect index
            return Global.None;
        }

        /// <summary>
        /// initialise data in arrayOfConstants
        /// </summary>
        /// <param name="index"></param>
        /// <param name="data"></param>
        public void SetData( Global index, int data)
        {
            if ((int)index < arrayOfConstants.Length)
            {
                arrayOfConstants[(int)index] = data;
                Console.WriteLine("{0} -> {1}", index, data);
            }
            else
            { Game.SetError(new Error(9, string.Format("{0} out of range, data {1}", index, data))); }
        }

        /// <summary>
        /// loops Global enums and checks all have valid data that has been imported from 'Constants.txt' (invalid if data = 0)
        /// </summary>
        public void ErrorCheck()
        {
            for(int index = 1; index < (int)Global.Count; index++)
            {
                if (arrayOfConstants[index] == 0)
                { Game.SetError(new Error(46, string.Format("{0} is missing data. Check Constants.txt is correct", (Global)index))); }
            }
        }
    }
}
