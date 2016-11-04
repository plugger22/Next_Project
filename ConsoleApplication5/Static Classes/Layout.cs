﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RLNET;

namespace Next_Game
{
    /// <summary>
    /// handles specific multi console screen layouts for the conflict system
    /// </summary>
    public class Layout
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int Offset_x { get; set; } //offset from right hand side of screen (cells)
        public int Offset_y { get; set; } //offset from top and bottom of screen (cells)
        private RLColor backColor;
        private int[,] arrayOfCells_Cards; //cell array for box and text
        private RLColor[,] arrayOfForeColors_Cards; //foreground color for cell contents
        private RLColor[,] arrayOfBackColors_Cards; //background color for cell's

        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="fillColor"></param>
        /// <param name="borderColor"></param>
        public Layout(int width, int height, int offset_x, int offset_y, RLColor fillColor, RLColor borderColor)
        {
            this.Width = width;
            this.Height = height;
            this.backColor = fillColor;
            this.Offset_x = offset_x;
            this.Offset_y = offset_y;
            //error check dimensions to see that they'll fit into the multi-console (130 x 100)
            if (Width > 130) { Width = 130; Game.SetError(new Error(80, string.Format("Invalid Width input \"{0}\", changed to 130", width))); }
            if (Height > 100) { Height = 100; Game.SetError(new Error(80, string.Format("Invalid Height input \"{0}\", changed to 130", height))); }
            //initialise border and colors
            arrayOfCells_Cards = new int[Width - Offset_x, Height - Offset_y * 2];
            arrayOfForeColors_Cards = new RLColor[Width - Offset_x, Height - Offset_y * 2];
            arrayOfBackColors_Cards = new RLColor[Width - Offset_x, Height - Offset_y * 2];

            //debug data set
            for (int height_index = 0; height_index < Height - Offset_y * 2; height_index++)
            {
                for (int width_index = 0; width_index < Width - Offset_x; width_index++)
                {
                    arrayOfBackColors_Cards[width_index, height_index] = fillColor;
                    arrayOfForeColors_Cards[width_index, height_index] = RLColor.White;
                    arrayOfCells_Cards[width_index, height_index] = 255;
                }
            }

        }

        /// <summary>
        /// Initialise Cards layout
        /// </summary>
        public void InitialiseCards()
        {
            int left_align = 8; //left side of status boxes (y_coord)
            int right_align = 120;
            int card_width = 40;
            int card_height = 41;
            int status_box_width = 33;
            int status_box_height = 11;
            int top_align = 22; //top of card (y_coord) & top y_coord for upper status boxes
            int middle_align = top_align + card_height / 2 - status_box_height / 2; // top y_coord for middle status boxes
            int bottom_align = top_align + card_height; //bottom y_coord for lower status boees
            int bottom_space = 6; //space between bottom of card and top of text boxes below
            int message_box_height = 12; //upper text box
            int instruction_box_height = 9; //lower text box
            int horizontal_align = right_align - status_box_width; //status boxes, left side of status boxes for those on the right hand side
            int vertical_align = bottom_align - status_box_height; //status boxes
            int vertical_pos = top_align + card_height + bottom_space; //used for message boxes down the bottom
            int text_box_width = horizontal_align + status_box_width - left_align; //two boxes under the card display
            int score_width = horizontal_align - left_align;
            int score_height = 12;
            int score_left_align = left_align + status_box_width / 2;
            int score_vertical_align = 6;
            int bar_offset_x = 4; //score internal bar
            int bar_offset_y = 3;
            int bar_width = score_width - (bar_offset_x * 2);
            int bar_middle = score_left_align + bar_offset_x + (bar_width / 2); //x_coord of mid point
            int bar_segment = Convert.ToInt32((float)bar_width / 8); //scoring marker segments on bar (4 either side of the zero mid point)
            int bar_top = score_vertical_align + bar_offset_y; //y_coord of bar
            int bar_height = score_height - (bar_offset_y * 2);
            //Card
            DrawBox(44, top_align, card_width, card_height, RLColor.Yellow, RLColor.LightGray);
            //Score track
            DrawBox(score_left_align, score_vertical_align, score_width, score_height, RLColor.Yellow, RLColor.LightGray);
            //...bar
            DrawBox(score_left_align + bar_offset_x, bar_top, bar_width, bar_height, RLColor.Gray, RLColor.Gray);
            //...coloured bars
            DrawBox(bar_middle, bar_top, bar_segment * 2, bar_height, RLColor.Gray, RLColor.Green);
            //...bar number markings (top)
            DrawText("0", bar_middle, score_vertical_align + bar_offset_y - 1, RLColor.Black, arrayOfCells_Cards, arrayOfForeColors_Cards);
            DrawText("+5", bar_middle + bar_segment - 1, score_vertical_align + bar_offset_y - 1, RLColor.Black, arrayOfCells_Cards, arrayOfForeColors_Cards);
            DrawText("+10", bar_middle + bar_segment * 2 - 2, score_vertical_align + bar_offset_y - 1, RLColor.Black, arrayOfCells_Cards, arrayOfForeColors_Cards);
            DrawText("+15", bar_middle + bar_segment * 3 - 2, score_vertical_align + bar_offset_y - 1, RLColor.Black, arrayOfCells_Cards, arrayOfForeColors_Cards);
            DrawText("-5", bar_middle - bar_segment - 1, score_vertical_align + bar_offset_y - 1, RLColor.Black, arrayOfCells_Cards, arrayOfForeColors_Cards);
            DrawText("-10", bar_middle - bar_segment * 2 - 2, score_vertical_align + bar_offset_y - 1, RLColor.Black, arrayOfCells_Cards, arrayOfForeColors_Cards);
            DrawText("-15", bar_middle - bar_segment * 3 - 2, score_vertical_align + bar_offset_y - 1, RLColor.Black, arrayOfCells_Cards, arrayOfForeColors_Cards);
            //...current score
            DrawText("+10", bar_middle - 1, bar_top + bar_height, RLColor.Blue, arrayOfCells_Cards, arrayOfForeColors_Cards);
            //upper text box - messages
            DrawBox(left_align, vertical_pos, text_box_width, message_box_height, RLColor.Yellow, RLColor.LightGray);
            //lower text box - instructions
            vertical_pos += message_box_height + bottom_space / 2;
            DrawBox(left_align, vertical_pos, text_box_width, instruction_box_height, RLColor.Yellow, RLColor.LightGray);
            DrawCenteredText("[F1] to Play a Card", left_align, vertical_pos + 2, text_box_width, RLColor.Blue, arrayOfCells_Cards, arrayOfForeColors_Cards);
            DrawCenteredText("[SPACE] or [ENTER] to Ignore a Card", left_align, vertical_pos + 4, text_box_width, RLColor.Black, arrayOfCells_Cards, arrayOfForeColors_Cards);
            DrawCenteredText("[ESC] to Auto Resolve", left_align, vertical_pos + 6, text_box_width, RLColor.Black, arrayOfCells_Cards, arrayOfForeColors_Cards);
            //Remaining Influence (top left in relation to card display)
            DrawBox(left_align, top_align, status_box_width, status_box_height, RLColor.Yellow, RLColor.LightGray);
            DrawCenteredText("Remaining", left_align, top_align + 2, status_box_width, RLColor.Black, arrayOfCells_Cards, arrayOfForeColors_Cards);
            DrawCenteredText("Influence", left_align, top_align + 4, status_box_width, RLColor.Black, arrayOfCells_Cards, arrayOfForeColors_Cards);
            DrawCenteredText("0", left_align, top_align + 7, status_box_width, RLColor.Blue, arrayOfCells_Cards, arrayOfForeColors_Cards);
            //Card Pool (middle left)
            DrawBox(left_align, middle_align, status_box_width, status_box_height, RLColor.Yellow, RLColor.LightGray);
            DrawText("Good cards", left_align + 7, middle_align + 2, RLColor.Blue, arrayOfCells_Cards, arrayOfForeColors_Cards);
            DrawText("Neutral cards", left_align + 7, middle_align + 4, RLColor.Black, arrayOfCells_Cards, arrayOfForeColors_Cards);
            DrawText("Bad cards", left_align + 7, middle_align + 6, RLColor.Red, arrayOfCells_Cards, arrayOfForeColors_Cards);
            DrawText("Total cards", left_align + 7, middle_align + 8, RLColor.Black, arrayOfCells_Cards, arrayOfForeColors_Cards);
            DrawText("12", left_align + 24, middle_align + 2, RLColor.Blue, arrayOfCells_Cards, arrayOfForeColors_Cards);
            DrawText("4", left_align + 24, middle_align + 4, RLColor.Black, arrayOfCells_Cards, arrayOfForeColors_Cards);
            DrawText("8", left_align + 24, middle_align + 6, RLColor.Red, arrayOfCells_Cards, arrayOfForeColors_Cards);
            DrawText("24", left_align + 24, middle_align + 8, RLColor.Black, arrayOfCells_Cards, arrayOfForeColors_Cards);
            //Remaining Cards (bottom left)
            DrawBox(left_align, vertical_align, status_box_width, status_box_height, RLColor.Yellow, RLColor.LightGray);
            DrawCenteredText("Remaining", left_align, vertical_align + 2, status_box_width, RLColor.Black, arrayOfCells_Cards, arrayOfForeColors_Cards);
            DrawCenteredText("Cards", left_align, vertical_align + 4, status_box_width, RLColor.Black, arrayOfCells_Cards, arrayOfForeColors_Cards);
            DrawCenteredText("0", left_align, vertical_align + 7, status_box_width, RLColor.Red, arrayOfCells_Cards, arrayOfForeColors_Cards);
            //Situation (top right)
            DrawBox(horizontal_align, top_align, status_box_width, status_box_height, RLColor.Yellow, RLColor.LightGray);
            DrawCenteredText("Situation", horizontal_align, top_align + 2, status_box_width,  RLColor.Black, arrayOfCells_Cards, arrayOfForeColors_Cards);
            DrawCenteredText("Defendable Hill (2 cards)", horizontal_align, top_align + 4, status_box_width, RLColor.Blue, arrayOfCells_Cards, arrayOfForeColors_Cards);
            DrawCenteredText("Muddy Ground (1 card)", horizontal_align, top_align + 6, status_box_width, RLColor.Black, arrayOfCells_Cards, arrayOfForeColors_Cards);
            DrawCenteredText("Army Size (3 cards)", horizontal_align, top_align + 8, status_box_width, RLColor.Red, arrayOfCells_Cards, arrayOfForeColors_Cards);
            //Secrets box (middle right)
            DrawBox(horizontal_align, middle_align, status_box_width, status_box_height, RLColor.Yellow, RLColor.LightGray);
            DrawCenteredText("Secrets", horizontal_align, middle_align + 2, status_box_width, RLColor.Black, arrayOfCells_Cards, arrayOfForeColors_Cards);
            DrawCenteredText("No Relevant Secrets Found", horizontal_align, middle_align + 4, status_box_width, RLColor.Black, arrayOfCells_Cards, arrayOfForeColors_Cards);
            //Strategy Info (bottom right)
            DrawBox(horizontal_align, vertical_align, status_box_width, status_box_height, RLColor.Yellow, RLColor.LightGray);
            DrawCenteredText("Your Strategy", horizontal_align, vertical_align + 2, status_box_width,  RLColor.Black, arrayOfCells_Cards, arrayOfForeColors_Cards);
            DrawCenteredText("All Out Assault 8/2", horizontal_align, vertical_align + 4, status_box_width,  RLColor.Blue, arrayOfCells_Cards, arrayOfForeColors_Cards);
            DrawCenteredText("Opponent's Strategy", horizontal_align, vertical_align + 6, status_box_width,  RLColor.Black, arrayOfCells_Cards, arrayOfForeColors_Cards);
            DrawCenteredText("Hold the Ground 4/0", horizontal_align, vertical_align + 8, status_box_width,  RLColor.Red, arrayOfCells_Cards, arrayOfForeColors_Cards);
        }

        /// <summary>
        /// Draw Cards Layout
        /// </summary>
        /// <param name="multiConsole"></param>
        public void DrawCards(RLConsole multiConsole)
        { Draw(multiConsole, arrayOfCells_Cards, arrayOfForeColors_Cards, arrayOfBackColors_Cards); }

        /// <summary>
        /// Draw box to multiConsole
        /// </summary>
        /// <param name="multiConsole"></param>
        private void Draw(RLConsole multiConsole, int[,] arrayOfCells, RLColor[,] arrayOfForeColors, RLColor[,] arrayOfBackColors )
        {
            multiConsole.Clear();
            for (int height_index = Offset_y; height_index < Height - Offset_y * 2; height_index++)
            {
                for (int width_index = 0; width_index < Width - Offset_x; width_index++)
                { multiConsole.Set(width_index, height_index, arrayOfForeColors[width_index, height_index], arrayOfBackColors[width_index, height_index], 
                    arrayOfCells[width_index, height_index]); }
            }

        }

        /// <summary>
        /// Draw a bordered, filled box of any size. Used by all layout classes to initialise
        /// </summary>
        /// <param name="coord_X"></param>
        /// <param name="coord_Y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="borderColor"></param>
        /// <param name="fillColor"></param>
        internal void DrawBox(int coord_X, int coord_Y, int width, int height, RLColor borderColor, RLColor fillColor)
        {
            //error check input, exit on bad data
            if (coord_X < 0 || coord_X + width > Width - Offset_x)
            { Game.SetError(new Error(81, string.Format("Invalid coord_X input \"{0}\"", coord_X))); return; }
            if (coord_Y < 0 || coord_Y + height > Height - Offset_y * 2)
            { Game.SetError(new Error(81, string.Format("Invalid coord_Y input \"{0}\"", coord_Y))); return; }
            //populate array - corners
            arrayOfCells_Cards[coord_X, coord_Y] = 218; arrayOfForeColors_Cards[coord_X, coord_Y] = borderColor; //top left
            arrayOfCells_Cards[coord_X, coord_Y + height - 1] = 192; arrayOfForeColors_Cards[coord_X, coord_Y + height - 1] = borderColor; //bottom left
            arrayOfCells_Cards[coord_X + width - 1, coord_Y] = 191; arrayOfForeColors_Cards[coord_X + width - 1, coord_Y] = borderColor; //top right
            arrayOfCells_Cards[coord_X + width - 1, coord_Y + height - 1] = 217; arrayOfForeColors_Cards[coord_X + width - 1, coord_Y + height - 1] = borderColor; //bottom right
            //Top & bottom rows
            for (int i = coord_X + 1; i < coord_X + width - 1; i++)
            {
                arrayOfCells_Cards[i, coord_Y] = 196;
                arrayOfCells_Cards[i, coord_Y + height - 1] = 196;
                arrayOfForeColors_Cards[i, coord_Y] = borderColor;
                arrayOfForeColors_Cards[i, coord_Y + height - 1] = borderColor;
            }
            //left and right sides
            for (int i = coord_Y + 1; i < coord_Y + height - 1; i++)
            {
                arrayOfCells_Cards[coord_X, i] = 179;
                arrayOfCells_Cards[coord_X + width - 1, i] = 179;
                arrayOfForeColors_Cards[coord_X, i] = borderColor;
                arrayOfForeColors_Cards[coord_X + width - 1, i] = borderColor;
            }
            //fill backcolor
            for (int width_index = coord_X + 1; width_index < coord_X + width - 1; width_index++)
            {
                for (int height_index = coord_Y + 1; height_index < coord_Y + height - 1; height_index++)
                { arrayOfBackColors_Cards[width_index, height_index] = fillColor; }
            }
        }

        /// <summary>
        /// Draws text on a layout (labels)
        /// </summary>
        /// <param name="text"></param>
        /// <param name="coord_X"></param>
        /// <param name="coord_Y"></param>
        /// <param name="foreColor"></param>
        internal void DrawText(string text, int coord_X, int coord_Y, RLColor foreColor, int [,] arrayOfCells, RLColor [,] arrayOfForeColors)
        {
            for (int i = 0; i < text.Length; i++)
            {
                arrayOfCells[coord_X + i, coord_Y] = text[i];
                arrayOfForeColors[coord_X + i, coord_Y] = foreColor;
            }
        }

        /// <summary>
        /// Draws text centered between two points (x1, y1 & width -> x2, y1)
        /// </summary>
        /// <param name="text"></param>
        /// <param name="coord_X_Left"></param>
        /// <param name="coord_X_Right"></param>
        /// <param name="coord_Y"></param>
        /// <param name="foreColor"></param>
        /// <param name="arrayOfCells"></param>
        /// <param name="arrayOfForeColors"></param>
        internal void DrawCenteredText(string text, int coord_X, int coord_Y, int width, RLColor foreColor, int[,] arrayOfCells, RLColor[,] arrayOfForeColors)
        {
            int length = text.Length;
            //error check
            if (length >= width)
            { Game.SetError(new Error(82, string.Format("String input \"{0}\" is to wide to fit in the box (max {1})", text, width))); return; }
            //work out start position
            int start = (width - length) / 2;
            //place text
            for (int i = 0; i < text.Length; i++)
            {
                arrayOfCells[coord_X + i + start, coord_Y] = text[i];
                arrayOfForeColors[coord_X + i + start, coord_Y] = foreColor;
            }
        }

    }
}
