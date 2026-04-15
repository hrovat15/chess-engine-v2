using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace chess_engine_v2
{
    class MovesGenerator
    {
        public Board board;

        public Move[] GenerateMoves(Board board)
        {
            this.board = board;

            // FIX: Allocate a fresh buffer each call so recursive search calls
            // (e.g. during alpha-beta) don't overwrite each other's move lists.
            Move[] moves = new Move[256];
            int moveCount = 0;

            if (board.sideToMove == 0)
                generateWhite(moves, ref moveCount);
            else
                generateBlack(moves, ref moveCount);
            // Mark end of valid moves with a default (zero) move
            if (moveCount < moves.Length) moves[moveCount] = default;
            return moves;
        }

        private void generateWhite(Move[] moves, ref int moveCount)
        {
            wPawnMoves(moves, ref moveCount);
            wLeaperMoves(moves, ref moveCount);
            wRookMoves(moves, ref moveCount);
            wBishopMoves(moves, ref moveCount);
            wQueenMoves(moves, ref moveCount);
        }

        private void generateBlack(Move[] moves, ref int moveCount)
        {
            bPawnMoves(moves, ref moveCount);
            bLeaperMoves(moves, ref moveCount);
            bRookMoves(moves, ref moveCount);
            bBishopMoves(moves, ref moveCount);
            bQueenMoves(moves, ref moveCount);
        }

        private void wRookMoves(Move[] moves, ref int moveCount)
        {
            ulong rooks = board.WRooks;
            if (rooks == 0) return;

            while (rooks != 0)
            {
                int from = BitOperations.TrailingZeroCount(rooks);
                ulong targets = PrecomputedMoves.GetRookAttacks(from, board.AllPieces) & ~board.WPieces;

                while (targets != 0)
                {
                    ulong targetBit = targets & (ulong)-(long)targets;
                    int to = BitOperations.TrailingZeroCount(targetBit);
                    ushort flag = (targetBit & board.BPieces) != 0 ? (ushort)4 : (ushort)0;
                    moves[moveCount++] = new Move(from, to, flag);
                    targets -= targetBit;
                }

                rooks &= rooks - 1;
            }
        }

        private void wBishopMoves(Move[] moves, ref int moveCount)
        {
            ulong bishops = board.WBishops;
            if (bishops == 0) return;
            while (bishops != 0)
            {
                int from = BitOperations.TrailingZeroCount(bishops);
                ulong targets = PrecomputedMoves.GetBishopAttacks(from, board.AllPieces) & ~board.WPieces;
                while (targets != 0)
                {
                    ulong targetBit = targets & (ulong)-(long)targets;
                    int to = BitOperations.TrailingZeroCount(targetBit);
                    ushort flag = (targetBit & board.BPieces) != 0 ? (ushort)4 : (ushort)0;
                    moves[moveCount++] = new Move(from, to, flag);
                    targets -= targetBit;
                }
                bishops &= bishops - 1;
            }
        }

        private void wQueenMoves(Move[] moves, ref int moveCount)
        {
            ulong queens = board.WQueens;
            if (queens == 0) return;
            while (queens != 0)
            {
                int from = BitOperations.TrailingZeroCount(queens);
                ulong targets = (PrecomputedMoves.GetRookAttacks(from, board.AllPieces) | PrecomputedMoves.GetBishopAttacks(from, board.AllPieces)) & ~board.WPieces;
                while (targets != 0)
                {
                    ulong targetBit = targets & (ulong)-(long)targets;
                    int to = BitOperations.TrailingZeroCount(targetBit);
                    ushort flag = (targetBit & board.BPieces) != 0 ? (ushort)4 : (ushort)0;
                    moves[moveCount++] = new Move(from, to, flag);
                    targets -= targetBit;
                }
                queens &= queens - 1;
            }
        }

        private void bRookMoves(Move[] moves, ref int moveCount)
        {
            ulong rooks = board.BRooks;
            if (rooks == 0) return;

            while (rooks != 0)
            {
                int from = BitOperations.TrailingZeroCount(rooks);
                ulong targets = PrecomputedMoves.GetRookAttacks(from, board.AllPieces) & ~board.BPieces;

                while (targets != 0)
                {
                    ulong targetBit = targets & (ulong)-(long)targets;
                    int to = BitOperations.TrailingZeroCount(targetBit);
                    ushort flag = (targetBit & board.WPieces) != 0 ? (ushort)4 : (ushort)0;
                    moves[moveCount++] = new Move(from, to, flag);
                    targets -= targetBit;
                }

                rooks &= rooks - 1;
            }
        }

        private void bBishopMoves(Move[] moves, ref int moveCount)
        {
            ulong bishops = board.BBishops;
            if (bishops == 0) return;
            while (bishops != 0)
            {
                int from = BitOperations.TrailingZeroCount(bishops);
                ulong targets = PrecomputedMoves.GetBishopAttacks(from, board.AllPieces) & ~board.BPieces;
                while (targets != 0)
                {
                    ulong targetBit = targets & (ulong)-(long)targets;
                    int to = BitOperations.TrailingZeroCount(targetBit);
                    ushort flag = (targetBit & board.WPieces) != 0 ? (ushort)4 : (ushort)0;
                    moves[moveCount++] = new Move(from, to, flag);
                    targets -= targetBit;
                }
                bishops &= bishops - 1;
            }
        }

        private void bQueenMoves(Move[] moves, ref int moveCount)
        {
            ulong queens = board.BQueens;
            if (queens == 0) return;
            while (queens != 0)
            {
                int from = BitOperations.TrailingZeroCount(queens);
                ulong targets = (PrecomputedMoves.GetRookAttacks(from, board.AllPieces) | PrecomputedMoves.GetBishopAttacks(from, board.AllPieces)) & ~board.BPieces;
                while (targets != 0)
                {
                    ulong targetBit = targets & (ulong)-(long)targets;
                    int to = BitOperations.TrailingZeroCount(targetBit);
                    ushort flag = (targetBit & board.WPieces) != 0 ? (ushort)4 : (ushort)0;
                    moves[moveCount++] = new Move(from, to, flag);
                    targets -= targetBit;
                }
                queens &= queens - 1;
            }
        }

        private void wPawnMoves(Move[] moves, ref int moveCount)
        {
            ulong push = (board.WPawns << 8) & board.FreeSquares;
            AddMove(push >> 8, 8, 0, moves, ref moveCount);

            ulong doublePush = ((push & 0x0000000000FF0000) << 8) & board.FreeSquares;
            AddMove(doublePush >> 16, 16, 1, moves, ref moveCount);

            AddMove(((board.WPawns << 7) & board.notAFile & board.BPieces) >> 7, 7, 4, moves, ref moveCount);
            AddMove(((board.WPawns << 9) & board.notHFile & board.BPieces) >> 9, 9, 4, moves, ref moveCount);

            if (board.enPassantSquare >= 0)
            {
                ulong epBB = 1UL << board.enPassantSquare;
                ulong epLeft = (board.WPawns << 7) & board.notAFile & epBB;
                if (epLeft != 0) AddMove(epLeft >> 7, 7, 5, moves, ref moveCount);
                ulong epRight = (board.WPawns << 9) & board.notHFile & epBB;
                if (epRight != 0) AddMove(epRight >> 9, 9, 5, moves, ref moveCount);
            }
        }

        private void bPawnMoves(Move[] moves, ref int moveCount)
        {
            ulong push = (board.BPawns >> 8) & board.FreeSquares;
            AddMove(push << 8, -8, 0, moves, ref moveCount);

            ulong doublePush = ((push & 0x0000FF0000000000) >> 8) & board.FreeSquares;
            AddMove(doublePush << 16, -16, 1, moves, ref moveCount);

            AddMove(((board.BPawns >> 7) & board.notHFile & board.WPieces) << 7, -7, 4, moves, ref moveCount);
            AddMove(((board.BPawns >> 9) & board.notAFile & board.WPieces) << 9, -9, 4, moves, ref moveCount);

            if (board.enPassantSquare >= 0)
            {
                ulong epBB = 1UL << board.enPassantSquare;
                ulong epLeft = (board.BPawns >> 7) & board.notAFile & epBB;
                if (epLeft != 0) AddMove(epLeft << 7, -7, 5, moves, ref moveCount);
                ulong epRight = (board.BPawns >> 9) & board.notHFile & epBB;
                if (epRight != 0) AddMove(epRight << 9, -9, 5, moves, ref moveCount);
            }
        }

        private void wLeaperMoves(Move[] moves, ref int moveCount)
        {
            ulong knights = board.WKnights;
            while (knights != 0)
            {
                int from = BitOperations.TrailingZeroCount(knights);
                ulong targets = PrecomputedMoves.KnightMoves[from] & ~board.WPieces;
                while (targets != 0)
                {
                    ulong targetBit = targets & (ulong)-(long)targets;
                    int to = BitOperations.TrailingZeroCount(targetBit);
                    ushort flag = (targetBit & board.BPieces) != 0 ? (ushort)4 : (ushort)0;
                    moves[moveCount++] = new Move(from, to, flag);
                    targets -= targetBit;
                }
                knights &= knights - 1;
            }

            ulong kings = board.WKings;
            while (kings != 0)
            {
                int from = BitOperations.TrailingZeroCount(kings);
                ulong targets = PrecomputedMoves.KingMoves[from] & ~board.WPieces;
                while (targets != 0)
                {
                    ulong targetBit = targets & (ulong)-(long)targets;
                    int to = BitOperations.TrailingZeroCount(targetBit);
                    ushort flag = (targetBit & board.BPieces) != 0 ? (ushort)4 : (ushort)0;
                    moves[moveCount++] = new Move(from, to, flag);
                    targets -= targetBit;
                }

                // FIX: Castling now checks that the king and transit squares are not attacked
                if (board.WhiteCanCastleK)
                {
                    if ((board.AllPieces & ((1UL << 5) | (1UL << 6))) == 0
                        && (board.WRooks & (1UL << 7)) != 0
                        && !board.IsSquareAttacked(4, false)  // e1 not in check
                        && !board.IsSquareAttacked(5, false)  // f1 not attacked
                        && !board.IsSquareAttacked(6, false)) // g1 not attacked
                    {
                        moves[moveCount++] = new Move(4, 6, 2);
                    }
                }
                if (board.WhiteCanCastleQ)
                {
                    if ((board.AllPieces & ((1UL << 1) | (1UL << 2) | (1UL << 3))) == 0
                        && (board.WRooks & (1UL << 0)) != 0
                        && !board.IsSquareAttacked(4, false)  // e1 not in check
                        && !board.IsSquareAttacked(3, false)  // d1 not attacked
                        && !board.IsSquareAttacked(2, false)) // c1 not attacked
                    {
                        moves[moveCount++] = new Move(4, 2, 3);
                    }
                }

                kings &= kings - 1;
            }
        }

        private void bLeaperMoves(Move[] moves, ref int moveCount)
        {
            ulong knights = board.BKnights;
            while (knights != 0)
            {
                int from = BitOperations.TrailingZeroCount(knights);
                ulong targets = PrecomputedMoves.KnightMoves[from] & ~board.BPieces;
                while (targets != 0)
                {
                    ulong targetBit = targets & (ulong)-(long)targets;
                    int to = BitOperations.TrailingZeroCount(targetBit);
                    ushort flag = (targetBit & board.WPieces) != 0 ? (ushort)4 : (ushort)0;
                    moves[moveCount++] = new Move(from, to, flag);
                    targets -= targetBit;
                }
                knights &= knights - 1;
            }

            ulong kings = board.BKings;
            while (kings != 0)
            {
                int from = BitOperations.TrailingZeroCount(kings);
                ulong targets = PrecomputedMoves.KingMoves[from] & ~board.BPieces;
                while (targets != 0)
                {
                    ulong targetBit = targets & (ulong)-(long)targets;
                    int to = BitOperations.TrailingZeroCount(targetBit);
                    ushort flag = (targetBit & board.WPieces) != 0 ? (ushort)4 : (ushort)0;
                    moves[moveCount++] = new Move(from, to, flag);
                    targets -= targetBit;
                }

                // FIX: Castling now checks that the king and transit squares are not attacked
                if (board.BlackCanCastleK)
                {
                    if ((board.AllPieces & ((1UL << 61) | (1UL << 62))) == 0
                        && (board.BRooks & (1UL << 63)) != 0
                        && !board.IsSquareAttacked(60, true)  // e8 not in check
                        && !board.IsSquareAttacked(61, true)  // f8 not attacked
                        && !board.IsSquareAttacked(62, true)) // g8 not attacked
                    {
                        moves[moveCount++] = new Move(60, 62, 2);
                    }
                }
                if (board.BlackCanCastleQ)
                {
                    if ((board.AllPieces & ((1UL << 57) | (1UL << 58) | (1UL << 59))) == 0
                        && (board.BRooks & (1UL << 56)) != 0
                        && !board.IsSquareAttacked(60, true)  // e8 not in check
                        && !board.IsSquareAttacked(59, true)  // d8 not attacked
                        && !board.IsSquareAttacked(58, true)) // c8 not attacked
                    {
                        moves[moveCount++] = new Move(60, 58, 3);
                    }
                }

                kings &= kings - 1;
            }
        }

        // FIX: flags is now a plain ushort parameter (not default) and a fresh local
        // copy is made per-move so promotion detection doesn't corrupt subsequent moves
        // in the same batch. Promotion threshold corrected to >= 56 (rank 8) or <= 7 (rank 1).
        private void AddMove(ulong fromPositions, int shift, ushort flags, Move[] moves, ref int moveCount)
        {
            while (fromPositions != 0)
            {
                int fromIndex = BitOperations.TrailingZeroCount(fromPositions);
                int toIndex = fromIndex + shift;

                // FIX: use a fresh copy of flags for every move in this batch
                ushort moveFlags = flags;
                if (toIndex >= 56 || toIndex <= 7)
                {
                    // Pawn reaching back rank — mark as promotion
                    moveFlags = (flags == 4) ? (ushort)15 : (ushort)11;
                }

                moves[moveCount++] = new Move(fromIndex, toIndex, moveFlags);
                fromPositions &= fromPositions - 1;
            }
        }
    }
}