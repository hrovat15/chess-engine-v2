using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace chess_engine_v2
{
    public class Evaluation
    {
        public int Evaluate(Board board)
        {
            // Simple material evaluation (positive = white is better)
            const int PawnValue = 100;
            const int KnightValue = 320;
            const int BishopValue = 330;
            const int RookValue = 500;
            const int QueenValue = 900;

            int wP = BitOperations.PopCount(board.WPawns);
            int wN = BitOperations.PopCount(board.WKnights);
            int wB = BitOperations.PopCount(board.WBishops);
            int wR = BitOperations.PopCount(board.WRooks);
            int wQ = BitOperations.PopCount(board.WQueens);

            int bP = BitOperations.PopCount(board.BPawns);
            int bN = BitOperations.PopCount(board.BKnights);
            int bB = BitOperations.PopCount(board.BBishops);
            int bR = BitOperations.PopCount(board.BRooks);
            int bQ = BitOperations.PopCount(board.BQueens);

            int whiteMaterial = wP * PawnValue + wN * KnightValue + wB * BishopValue + wR * RookValue + wQ * QueenValue;
            int blackMaterial = bP * PawnValue + bN * KnightValue + bB * BishopValue + bR * RookValue + bQ * QueenValue;

            int materialScore = whiteMaterial - blackMaterial;

            if (board.sideToMove == 1)
                materialScore = -materialScore;

            return materialScore;
        }
    }
}
