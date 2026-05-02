using System;
using System.Collections.Generic;
using System.Text;

namespace chess_engine_v2
{
    public class Evaluation
    {
        // Material values
        private const int PawnValue = 100;
        private const int KnightValue = 320;
        private const int BishopValue = 330;
        private const int RookValue = 500;
        private const int QueenValue = 900;

        // Piece-Square Tables (from white's perspective, rank 0 = rank 1, rank 7 = rank 8)
        // Bonuses encourage pieces to occupy good squares

        private static readonly int[] PawnTable = {
             0,  0,  0,  0,  0,  0,  0,  0,
            50, 50, 50, 50, 50, 50, 50, 50,
            10, 10, 20, 30, 30, 20, 10, 10,
             5,  5, 10, 25, 25, 10,  5,  5,
             0,  0,  0, 20, 20,  0,  0,  0,
             5, -5,-10,  0,  0,-10, -5,  5,
             5, 10, 10,-20,-20, 10, 10,  5,
             0,  0,  0,  0,  0,  0,  0,  0
        };

        private static readonly int[] KnightTable = {
            -50,-40,-30,-30,-30,-30,-40,-50,
            -40,-20,  0,  0,  0,  0,-20,-40,
            -30,  0, 10, 15, 15, 10,  0,-30,
            -30,  5, 15, 20, 20, 15,  5,-30,
            -30,  0, 15, 20, 20, 15,  0,-30,
            -30,  5, 10, 15, 15, 10,  5,-30,
            -40,-20,  0,  5,  5,  0,-20,-40,
            -50,-40,-30,-30,-30,-30,-40,-50
        };

        private static readonly int[] BishopTable = {
            -20,-10,-10,-10,-10,-10,-10,-20,
            -10,  0,  0,  0,  0,  0,  0,-10,
            -10,  0,  5, 10, 10,  5,  0,-10,
            -10,  5,  5, 10, 10,  5,  5,-10,
            -10,  0, 10, 10, 10, 10,  0,-10,
            -10, 10, 10, 10, 10, 10, 10,-10,
            -10,  5,  0,  0,  0,  0,  5,-10,
            -20,-10,-10,-10,-10,-10,-10,-20
        };

        private static readonly int[] RookTable = {
             0,  0,  0,  0,  0,  0,  0,  0,
             5, 10, 10, 10, 10, 10, 10,  5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
             0,  0,  0,  5,  5,  0,  0,  0
        };

        private static readonly int[] QueenTable = {
            -20,-10,-10, -5, -5,-10,-10,-20,
            -10,  0,  0,  0,  0,  0,  0,-10,
            -10,  0,  5,  5,  5,  5,  0,-10,
             -5,  0,  5,  5,  5,  5,  0, -5,
              0,  0,  5,  5,  5,  5,  0, -5,
            -10,  5,  5,  5,  5,  5,  0,-10,
            -10,  0,  5,  0,  0,  0,  0,-10,
            -20,-10,-10, -5, -5,-10,-10,-20
        };

        private static readonly int[] KingMiddleGameTable = {
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -20,-30,-30,-40,-40,-30,-30,-20,
            -10,-20,-20,-20,-20,-20,-20,-10,
             20, 20,  0,  0,  0,  0, 20, 20,
             20, 30, 10,  0,  0, 10, 30, 20
        };

        private static readonly int[] KingEndGameTable = {
            -50,-40,-30,-20,-20,-30,-40,-50,
            -30,-20,-10,  0,  0,-10,-20,-30,
            -30,-10, 20, 30, 30, 20,-10,-30,
            -30,-10, 30, 40, 40, 30,-10,-30,
            -30,-10, 30, 40, 40, 30,-10,-30,
            -30,-10, 20, 30, 30, 20,-10,-30,
            -30,-30,  0,  0,  0,  0,-30,-30,
            -50,-30,-30,-30,-30,-30,-30,-50
        };

        public int Evaluate(Board board)
        {
            int score = 0;

            // Material and positional evaluation
            score += EvaluatePieces(board, true) - EvaluatePieces(board, false);

            // Pawn structure bonuses
            score += EvaluatePawnStructure(board, true) - EvaluatePawnStructure(board, false);

            // Bishop pair bonus
            if (BitOperations.PopCount(board.WBishops) >= 2)
                score += 30;
            if (BitOperations.PopCount(board.BBishops) >= 2)
                score -= 30;

            // Mobility bonus (simplified - count of pieces developed)
            score += EvaluateMobility(board);

            // Return from side to move perspective
            return board.sideToMove == 0 ? score : -score;
        }

        private int EvaluatePieces(Board board, bool isWhite)
        {
            int score = 0;

            // Determine if we're in endgame (few pieces on board)
            int totalPieces = BitOperations.PopCount(board.AllPieces);
            bool isEndgame = totalPieces <= 16;

            ulong pawns = isWhite ? board.WPawns : board.BPawns;
            ulong knights = isWhite ? board.WKnights : board.BKnights;
            ulong bishops = isWhite ? board.WBishops : board.BBishops;
            ulong rooks = isWhite ? board.WRooks : board.BRooks;
            ulong queens = isWhite ? board.WQueens : board.BQueens;
            ulong kings = isWhite ? board.WKings : board.BKings;

            // Pawns
            ulong pawnsCopy = pawns;
            while (pawnsCopy != 0)
            {
                int square = BitOperations.TrailingZeroCount(pawnsCopy);
                int adjustedSquare = isWhite ? square : (63 - square); // Flip for black
                score += PawnValue + PawnTable[adjustedSquare];
                pawnsCopy &= pawnsCopy - 1; // Clear the least significant bit
            }

            // Knights
            ulong knightsCopy = knights;
            while (knightsCopy != 0)
            {
                int square = BitOperations.TrailingZeroCount(knightsCopy);
                int adjustedSquare = isWhite ? square : (63 - square);
                score += KnightValue + KnightTable[adjustedSquare];
                knightsCopy &= knightsCopy - 1;
            }

            // Bishops
            ulong bishopsCopy = bishops;
            while (bishopsCopy != 0)
            {
                int square = BitOperations.TrailingZeroCount(bishopsCopy);
                int adjustedSquare = isWhite ? square : (63 - square);
                score += BishopValue + BishopTable[adjustedSquare];
                bishopsCopy &= bishopsCopy - 1;
            }

            // Rooks
            ulong rooksCopy = rooks;
            while (rooksCopy != 0)
            {
                int square = BitOperations.TrailingZeroCount(rooksCopy);
                int adjustedSquare = isWhite ? square : (63 - square);
                score += RookValue + RookTable[adjustedSquare];
                rooksCopy &= rooksCopy - 1;
            }

            // Queens
            ulong queensCopy = queens;
            while (queensCopy != 0)
            {
                int square = BitOperations.TrailingZeroCount(queensCopy);
                int adjustedSquare = isWhite ? square : (63 - square);
                score += QueenValue + QueenTable[adjustedSquare];
                queensCopy &= queensCopy - 1;
            }

            // King (use different table for endgame)
            ulong kingsCopy = kings;
            while (kingsCopy != 0)
            {
                int square = BitOperations.TrailingZeroCount(kingsCopy);
                int adjustedSquare = isWhite ? square : (63 - square);
                int[] kingTable = isEndgame ? KingEndGameTable : KingMiddleGameTable;
                score += kingTable[adjustedSquare];
                kingsCopy &= kingsCopy - 1;
            }

            return score;
        }

        private int EvaluatePawnStructure(Board board, bool isWhite)
        {
            int score = 0;
            ulong pawns = isWhite ? board.WPawns : board.BPawns;

            for (int file = 0; file < 8; file++)
            {
                ulong filemask = 0x0101010101010101UL << file;
                ulong pawnsOnFile = pawns & filemask;
                int pawnCount = BitOperations.PopCount(pawnsOnFile);

                // Penalty for doubled pawns
                if (pawnCount > 1)
                {
                    score -= 20 * (pawnCount - 1);
                }

                // Bonus for passed pawns (simplified - no enemy pawns on file or adjacent files)
                if (pawnCount == 1)
                {
                    ulong adjacentFiles = 0UL;
                    if (file > 0) adjacentFiles |= 0x0101010101010101UL << (file - 1);
                    if (file < 7) adjacentFiles |= 0x0101010101010101UL << (file + 1);

                    ulong enemyPawns = isWhite ? board.BPawns : board.WPawns;
                    ulong blockingPawns = enemyPawns & (filemask | adjacentFiles);

                    if (blockingPawns == 0)
                    {
                        score += 30; // Passed pawn bonus
                    }
                }
            }

            return score;
        }

        private int EvaluateMobility(Board board)
        {
            int score = 0;

            // Bonus for developed knights (not on starting squares)
            ulong whiteKnightsStart = 0x0000000000000042UL;
            ulong blackKnightsStart = 0x4200000000000000UL;

            int whiteKnightsDeveloped = BitOperations.PopCount(board.WKnights & ~whiteKnightsStart);
            int blackKnightsDeveloped = BitOperations.PopCount(board.BKnights & ~blackKnightsStart);

            score += whiteKnightsDeveloped * 10;
            score -= blackKnightsDeveloped * 10;

            // Bonus for developed bishops
            ulong whiteBishopsStart = 0x0000000000000024UL;
            ulong blackBishopsStart = 0x2400000000000000UL;

            int whiteBishopsDeveloped = BitOperations.PopCount(board.WBishops & ~whiteBishopsStart);
            int blackBishopsDeveloped = BitOperations.PopCount(board.BBishops & ~blackBishopsStart);

            score += whiteBishopsDeveloped * 10;
            score -= blackBishopsDeveloped * 10;

            // Bonus for controlling center squares (e4, d4, e5, d5)
            ulong centerSquares = 0x0000001818000000UL;
            ulong whitePieces = board.WPieces;
            ulong blackPieces = board.BPieces;

            int whiteCenterControl = BitOperations.PopCount(whitePieces & centerSquares);
            int blackCenterControl = BitOperations.PopCount(blackPieces & centerSquares);

            score += whiteCenterControl * 5;
            score -= blackCenterControl * 5;

            return score;
        }
    }
}
