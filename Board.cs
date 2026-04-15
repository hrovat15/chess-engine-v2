using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace chess_engine_v2
{
    public class Board
    {
        public ulong WPawns = 0x000000000000FF00;
        public ulong WRooks = 0x0000000000000081;
        public ulong WKnights = 0x0000000000000042;
        public ulong WBishops = 0x0000000000000024;
        public ulong WQueens = 0x0000000000000008;
        public ulong WKings = 0x0000000000000010;

        public ulong BPawns = 0x00FF000000000000;
        public ulong BRooks = 0x8100000000000000;
        public ulong BKnights = 0x4200000000000000;
        public ulong BBishops = 0x2400000000000000;
        public ulong BQueens = 0x0800000000000000;
        public ulong BKings = 0x1000000000000000;

        public ulong notAFile = 0xFEFEFEFEFEFEFEFE;
        public ulong notHFile = 0x7F7F7F7F7F7F7F7F;

        public ulong WPieces => WPawns | WRooks | WKnights | WBishops | WQueens | WKings;
        public ulong BPieces => BPawns | BRooks | BKnights | BBishops | BQueens | BKings;

        public ulong AllPieces => WPieces | BPieces;

        public ulong FreeSquares => ~AllPieces;

        public int sideToMove = 0; // 0 for white, 1 for black

        public bool WhiteCanCastleK = true;
        public bool WhiteCanCastleQ = true;
        public bool BlackCanCastleK = true;
        public bool BlackCanCastleQ = true;

        // -1 means no ep square
        public int enPassantSquare = -1;
        public ulong EnPassantBitboard => enPassantSquare >= 0 ? (1UL << enPassantSquare) : 0UL;

        public struct UndoInfo
        {
            public int CapturedPiece;
            public int CapturedSquare;
            public int MovingPiece;
            public ulong PrevAllPieces;
            public int PrevEnPassantSquare;
            public bool PrevWhiteCanCastleK;
            public bool PrevWhiteCanCastleQ;
            public bool PrevBlackCanCastleK;
            public bool PrevBlackCanCastleQ;
            public int PrevSideToMove;
            public int Promotion; // 0 = none, otherwise stores the promoted piece type
        }

        private Stack<UndoInfo> _history = new Stack<UndoInfo>();

        public void MakeMove(Move move)
        {
            // Debug logging to help track down make/unmake issues
            ulong fromMask = 1UL << move.From;
            ulong toMask = 1UL << move.To;
            bool isWhite = sideToMove == 0;

            // 1. Prepare Undo Info
            UndoInfo undo = new UndoInfo
            {
                CapturedPiece = 0,
                PrevAllPieces = this.AllPieces,
                PrevEnPassantSquare = this.enPassantSquare,
                PrevWhiteCanCastleK = this.WhiteCanCastleK,
                PrevWhiteCanCastleQ = this.WhiteCanCastleQ,
                PrevBlackCanCastleK = this.BlackCanCastleK,
                PrevBlackCanCastleQ = this.BlackCanCastleQ,
                PrevSideToMove = this.sideToMove,
                Promotion = 0
            };

            // 2. Identify the piece moving
            int movingPiece = GetPieceAt(fromMask, isWhite);
            undo.MovingPiece = movingPiece;
            undo.CapturedPiece = 0;
            undo.CapturedSquare = -1;

            // 3. Handle captures (including en-passant)
            if (move.IsCapture)
            {
                if (movingPiece == 1)
                {
                    // pawn capture might be en-passant
                    if (move.To == enPassantSquare && enPassantSquare >= 0)
                    {
                        int capturedSq = isWhite ? move.To - 8 : move.To + 8;
                        ulong capMask = 1UL << capturedSq;
                        undo.CapturedPiece = GetPieceAt(capMask, !isWhite);
                        undo.CapturedSquare = capturedSq;
                        // remove captured pawn
                        ClearSquare(capMask, !isWhite);
                    }
                    else
                    {
                        undo.CapturedPiece = GetPieceAt(toMask, !isWhite);
                        undo.CapturedSquare = move.To;
                        ClearSquare(toMask, !isWhite);
                    }
                }
                else
                {
                    undo.CapturedPiece = GetPieceAt(toMask, !isWhite);
                    undo.CapturedSquare = move.To;
                    ClearSquare(toMask, !isWhite);
                }
            }

            // 4. Move the piece: clear source and set destination for the moving piece
            ClearSquare(fromMask, isWhite);

            // FIX: Promotion handling — store the actual promoted piece type (not always 0)
            if (movingPiece == 1 && (move.Flags == 11 || move.Flags == 15))
            {
                // Determine promoted piece from move; default to queen if not specified
                int promoPiece = 5;
                SetPiece(promoPiece, isWhite, toMask);
                undo.Promotion = promoPiece; // store non-zero so UnmakeMove knows it was a promotion
            }
            else
            {
                SetPiece(movingPiece, isWhite, toMask);
                undo.Promotion = 0;
            }

            // 5. Handle pawn double-push en-passant target
            enPassantSquare = -1;
            if (movingPiece == 1)
            {
                if (isWhite && move.To - move.From == 16)
                    enPassantSquare = move.From + 8;
                else if (!isWhite && move.From - move.To == 16)
                    enPassantSquare = move.From - 8;
            }

            // 6. Handle castling rook movement
            if (move.Flags == 2)
            {
                if (isWhite)
                {
                    // white kingside castle: rook h1->f1
                    ClearSquare(1UL << 7, true);
                    SetPiece(4, true, 1UL << 5);
                }
                else
                {
                    // black kingside castle: rook h8->f8
                    ClearSquare(1UL << 63, false);
                    SetPiece(4, false, 1UL << 61);
                }
            }
            else if (move.Flags == 3)
            {
                if (isWhite)
                {
                    // white queenside castle: rook a1->d1
                    ClearSquare(1UL << 0, true);
                    SetPiece(4, true, 1UL << 3);
                }
                else
                {
                    // black queenside castle: rook a8->d8
                    ClearSquare(1UL << 56, false);
                    SetPiece(4, false, 1UL << 59);
                }
            }

            // FIX: Update castling rights based on what moved
            if (movingPiece == 6) // King moved
            {
                if (isWhite) { WhiteCanCastleK = false; WhiteCanCastleQ = false; }
                else { BlackCanCastleK = false; BlackCanCastleQ = false; }
            }
            if (move.From == 7 || move.To == 7) WhiteCanCastleK = false;
            if (move.From == 0 || move.To == 0) WhiteCanCastleQ = false;
            if (move.From == 63 || move.To == 63) BlackCanCastleK = false;
            if (move.From == 56 || move.To == 56) BlackCanCastleQ = false;
            // FIX: Also revoke castling rights if a rook is captured on its starting square
            if (undo.CapturedPiece == 4)
            {
                if (!isWhite) // white captured a black rook
                {
                    if (undo.CapturedSquare == 63) BlackCanCastleK = false;
                    if (undo.CapturedSquare == 56) BlackCanCastleQ = false;
                }
                else // black captured a white rook
                {
                    if (undo.CapturedSquare == 7) WhiteCanCastleK = false;
                    if (undo.CapturedSquare == 0) WhiteCanCastleQ = false;
                }
            }

            // 7. Finalize
            _history.Push(undo);
            sideToMove = 1 - sideToMove;
        }

        public void UnmakeMove(Move move)
        {
            UndoInfo undo = _history.Pop();

            // 1. Restore side to move from undo
            sideToMove = undo.PrevSideToMove;
            bool isWhite = sideToMove == 0;

            ulong fromMask = 1UL << move.From;
            ulong toMask = 1UL << move.To;

            // 2. Move the moving piece back to origin (handle promotion)
            ClearSquare(toMask, isWhite);
            if (undo.Promotion != 0)
            {
                // Remove promoted piece at destination and restore pawn at source
                SetPiece(1, isWhite, fromMask);
            }
            else
            {
                SetPiece(undo.MovingPiece, isWhite, fromMask);
            }

            // 3. Undo castling rook move if this was a castle
            if (move.Flags == 2)
            {
                // kingside
                if (isWhite)
                {
                    ClearSquare(1UL << 5, true);
                    SetPiece(4, true, 1UL << 7);
                }
                else
                {
                    ClearSquare(1UL << 61, false);
                    SetPiece(4, false, 1UL << 63);
                }
            }
            else if (move.Flags == 3)
            {
                // queenside
                if (isWhite)
                {
                    ClearSquare(1UL << 3, true);
                    SetPiece(4, true, 1UL << 0);
                }
                else
                {
                    ClearSquare(1UL << 59, false);
                    SetPiece(4, false, 1UL << 56);
                }
            }

            // 4. Restore captured piece if any
            if (undo.CapturedPiece != 0 && undo.CapturedSquare >= 0)
            {
                ulong capMask = 1UL << undo.CapturedSquare;
                SetPiece(undo.CapturedPiece, !isWhite, capMask);
            }

            // 5. Restore game state (en-passant and castling rights)
            this.enPassantSquare = undo.PrevEnPassantSquare;
            this.WhiteCanCastleK = undo.PrevWhiteCanCastleK;
            this.WhiteCanCastleQ = undo.PrevWhiteCanCastleQ;
            this.BlackCanCastleK = undo.PrevBlackCanCastleK;
            this.BlackCanCastleQ = undo.PrevBlackCanCastleQ;
        }

        private void ClearSquare(ulong mask, bool isWhite)
        {
            if (isWhite)
            {
                WPawns &= ~mask;
                WKnights &= ~mask;
                WBishops &= ~mask;
                WRooks &= ~mask;
                WQueens &= ~mask;
                WKings &= ~mask;
            }
            else
            {
                BPawns &= ~mask;
                BKnights &= ~mask;
                BBishops &= ~mask;
                BRooks &= ~mask;
                BQueens &= ~mask;
                BKings &= ~mask;
            }
        }

        private void SetPiece(int pieceType, bool isWhite, ulong mask)
        {
            if (pieceType == 0) return;
            if (isWhite)
            {
                switch (pieceType)
                {
                    case 1: WPawns |= mask; break;
                    case 2: WKnights |= mask; break;
                    case 3: WBishops |= mask; break;
                    case 4: WRooks |= mask; break;
                    case 5: WQueens |= mask; break;
                    case 6: WKings |= mask; break;
                }
            }
            else
            {
                switch (pieceType)
                {
                    case 1: BPawns |= mask; break;
                    case 2: BKnights |= mask; break;
                    case 3: BBishops |= mask; break;
                    case 4: BRooks |= mask; break;
                    case 5: BQueens |= mask; break;
                    case 6: BKings |= mask; break;
                }
            }
        }

        public bool IsSquareAttacked(int sq)
        {
            bool attackerIsWhite = sideToMove != 0;
            return IsSquareAttacked(sq, attackerIsWhite);
        }

        public bool IsSquareAttacked(int sq, bool attackerIsWhite)
        {
            // 1. Attacked by Pawns
            ulong targetMask = 1UL << sq;
            if (attackerIsWhite)
            {
                // If we are looking for WHITE attackers, can a white pawn 
                // reach 'sq' from below?
                ulong attackers = 0;
                attackers |= (targetMask >> 7) & notAFile; // Look south-east
                attackers |= (targetMask >> 9) & notHFile; // Look south-west
                if ((attackers & WPawns) != 0) return true;
            }
            else
            {
                // If we are looking for BLACK attackers, can a black pawn 
                // reach 'sq' from above?
                ulong attackers = 0;
                attackers |= (targetMask << 7) & notHFile; // Look north-west
                attackers |= (targetMask << 9) & notAFile; // Look north-east
                if ((attackers & BPawns) != 0) return true;
            }

            // 2. Attacked by Knights
            if ((PrecomputedMoves.KnightMoves[sq] & (attackerIsWhite ? WKnights : BKnights)) != 0) return true;

            // 3. Attacked by King (to prevent Kings from touching)
            if ((PrecomputedMoves.KingMoves[sq] & (attackerIsWhite ? WKings : BKings)) != 0) return true;

            // FIX: Check rooks/queens and bishops/queens separately, not all pieces
            ulong rookQueens = attackerIsWhite ? (WRooks | WQueens) : (BRooks | BQueens);
            ulong bishopQueens = attackerIsWhite ? (WBishops | WQueens) : (BBishops | BQueens);

            if ((PrecomputedMoves.GetRookAttacks(sq, AllPieces) & rookQueens) != 0) return true;
            if ((PrecomputedMoves.GetBishopAttacks(sq, AllPieces) & bishopQueens) != 0) return true;

            return false;
        }

        public bool IsInCheck()
        {
            ulong kingBitboard = sideToMove == 0 ? WKings : BKings;
            if (kingBitboard == 0) return false;
            int kingSq = BitOperations.TrailingZeroCount(kingBitboard);
            return IsSquareAttacked(kingSq);
        }

        public int GetPieceAt(ulong squareMask, bool isWhite)
        {
            if (isWhite)
            {
                if ((WPawns & squareMask) != 0) return 1;
                if ((WKnights & squareMask) != 0) return 2;
                if ((WBishops & squareMask) != 0) return 3;
                if ((WRooks & squareMask) != 0) return 4;
                if ((WQueens & squareMask) != 0) return 5;
                if ((WKings & squareMask) != 0) return 6;
            }
            else
            {
                if ((BPawns & squareMask) != 0) return 1;
                if ((BKnights & squareMask) != 0) return 2;
                if ((BBishops & squareMask) != 0) return 3;
                if ((BRooks & squareMask) != 0) return 4;
                if ((BQueens & squareMask) != 0) return 5;
                if ((BKings & squareMask) != 0) return 6;
            }
            return 0; // Empty
        }
    }
}