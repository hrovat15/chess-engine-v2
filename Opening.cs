using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace chess_engine_v2
{
    internal class Opening
    {
        private static string _openingsFilePath = "openings_uci.txt";

        /// <summary>
        /// Set the path to the openings file (call this from Unity before using the engine)
        /// </summary>
        public static void SetOpeningsPath(string filePath)
        {
            _openingsFilePath = filePath;
        }

        public Move GetOpeningMove(string history, Board board)
        {
            if (!File.Exists(_openingsFilePath))
            {
                return new Move(0, 0, 0); // No openings file available
            }

            var openings = File.ReadAllLines(_openingsFilePath);

            int openingLineIndex = getOpeningLine(history, openings);

            // Check if no matching opening was found
            if (openingLineIndex == -1)
            {
                return new Move(0, 0, 0); // Return invalid move
            }

            string openingLine = openings[openingLineIndex];

            // Calculate the starting position for the next move
            int moveStartPos = history.Length;

            // If history is not empty, skip the space after it
            if (!string.IsNullOrEmpty(history))
            {
                moveStartPos++; // Skip the space
            }

            // Check if there's actually a next move available
            if (moveStartPos + 3 >= openingLine.Length)
            {
                return new Move(0, 0, 0); // No more moves in this opening line
            }

            // Extract the next move (4 characters: e.g., "g1f3")
            int fromFile = openingLine[moveStartPos] - 'a';
            int fromRank = openingLine[moveStartPos + 1] - '1';
            int toFile = openingLine[moveStartPos + 2] - 'a';
            int toRank = openingLine[moveStartPos + 3] - '1';

            ushort flags = 0;

            // Check for capture
            if((1UL << (toRank * 8 + toFile) & board.AllPieces) != 0)
            {
                flags = 4;
            }

            //check for castling
            if ((fromRank * 8 + fromFile == 60 && toRank * 8 + toFile == 62) ||
            (fromRank * 8 + fromFile == 4 && (toRank * 8) + toFile == 6))
            {
                flags = 2;
            }
            if ((fromRank * 8 + fromFile == 60 && toRank * 8 + toFile == 58) ||
                (fromRank * 8 + fromFile == 4 && toRank * 8 + toFile == 2))
            {
                flags = 3;
            }

            Move move = new Move(from: fromRank * 8 + fromFile, to: toRank * 8 + toFile, flags);

            return move;
        }

        private int getOpeningLine(string history, string[] openings)
        {
            
            for (int i = 0; i < openings.Length; i++)
            {
                if (openings[i].StartsWith(history))
                {
                    return i; // Return the index of the matching opening line
                }
            }
            return -1;
        }
    }
}
