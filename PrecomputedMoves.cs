using System.Numerics;

namespace chess_engine_v2
{
    class PrecomputedMoves
    {
        public static ulong[] KnightMoves = new ulong[64];
        public static ulong[] KingMoves = new ulong[64];

        public static ulong[] RookMasks = new ulong[64];
        public static int[] RookRelevantBits = new int[64];
        public static int[][] RookMaskBits = new int[64][];
        public static ulong[] RookMagics = new ulong[64]
        {
            0x0080001020400080, 0x0040001000200040, 0x0080081000200080, 0x0080040800100080,
            0x0080020400080080, 0x0080010200040080, 0x0080008001000200, 0x0080002040800100,
            0x0000800020400080, 0x0000400020005000, 0x0000801000200080, 0x0000800800100080,
            0x0000800400080080, 0x0000800200040080, 0x0000800100020080, 0x0000800040800100,
            0x0000208000400080, 0x0000404000201000, 0x0000808010002000, 0x0000808008001000,
            0x0000808004000800, 0x0000808002000400, 0x0000010100020004, 0x0000020000408104,
            0x0000208080004000, 0x0000200040005000, 0x0000100080200080, 0x0000080080100080,
            0x0000040080080080, 0x0000020080040080, 0x0000010080800200, 0x0000800080004100,
            0x0000204000800080, 0x0000200040401000, 0x0000100080802000, 0x0000080080801000,
            0x0000040080800800, 0x0000020080800400, 0x0000020001010004, 0x0000800040800100,
            0x0000204000808000, 0x0000200040008080, 0x0000100020008080, 0x0000080010008080,
            0x0000040008008080, 0x0000020004008080, 0x0000010002008080, 0x0000004081020004,
            0x0000204000800080, 0x0000200040008080, 0x0000100020008080, 0x0000080010008080,
            0x0000040008008080, 0x0000020004008080, 0x0000800100020080, 0x0000800041000080,
            0x00FFFCDDFCED714A, 0x007FFCDDFCED714A, 0x003FFFCDFFD88096, 0x0000040810002101,
            0x0001000204080011, 0x0001000204000801, 0x0001000082000401, 0x0001FFFAABFAD1A2
        };
        public static ulong[][] RookAttackTable = new ulong[64][];

        public static ulong[] BishopMasks = new ulong[64];
        public static int[] BishopRelevantBits = new int[64];
        public static int[][] BishopMaskBits = new int[64][];
        public static ulong[] BishopMagics = new ulong[64]
        {
            0x0002020202020200, 0x0002020202020000, 0x0004010202000000, 0x0004040080000000,
            0x0001104000000000, 0x0000821040000000, 0x0000410410400000, 0x0000104104104000,
            0x0000040404040400, 0x0000020202020200, 0x0000040102020000, 0x0000040400800000,
            0x0000011040000000, 0x0000008210400000, 0x0000004104104000, 0x0000002082082000,
            0x0004000808080800, 0x0002000404040400, 0x0001000202020200, 0x0000800802004000,
            0x0000800400A00000, 0x0000200100884000, 0x0000400082082000, 0x0000200041041000,
            0x0002080010101000, 0x0001040008080800, 0x0000208004010400, 0x0000404004010200,
            0x0000840000802000, 0x0000404002011000, 0x0000808001041000, 0x0000404000820800,
            0x0001041000202000, 0x0000820800101000, 0x0000104400080800, 0x0000020080080080,
            0x0000404040040100, 0x0000808100020100, 0x0001010100020800, 0x0000808080010400,
            0x0000820820004000, 0x0000410410002000, 0x0000082088001000, 0x0000002011000800,
            0x0000080100400400, 0x0001010101000200, 0x0002020202000400, 0x0001010101000200,
            0x0000410410400000, 0x0000208208200000, 0x0000002084100000, 0x0000000020880000,
            0x0000001002020000, 0x0000040408020000, 0x0004040404040000, 0x0002020202020000,
            0x0000104104104000, 0x0000002082082000, 0x0000000020841000, 0x0000000000208800,
            0x0000000010020200, 0x0000000404080200, 0x0000040404040400, 0x0002020202020200
        };
        public static ulong[][] BishopAttackTable = new ulong[64][];

        static PrecomputedMoves()
        {
            for (int i = 0; i < 64; i++)
            {
                KnightMoves[i] = CalculateKnightMoves(i);
                KingMoves[i] = CalculateKingMoves(i);
                RookMasks[i] = CreateRookMask(i);
                BishopMasks[i] = CreateBishopMask(i);

                // build list of relevant blocker squares for the rook mask
                var maskBitsList = new List<int>();
                ulong mask = RookMasks[i];
                while (mask != 0)
                {
                    int sq = BitOperations.TrailingZeroCount(mask);
                    maskBitsList.Add(sq);
                    mask &= mask - 1;
                }

                RookRelevantBits[i] = maskBitsList.Count;
                RookMaskBits[i] = maskBitsList.ToArray();

                int bits = RookRelevantBits[i];
                int tableSize = 1 << bits;
                RookAttackTable[i] = new ulong[tableSize];

                // populate attack table. If a precomputed magic is provided (non-zero) use it,
                // otherwise fall back to direct index = subset index (correct but no magic)
                ulong magic = RookMagics[i];
                for (int index = 0; index < tableSize; index++)
                {
                    ulong occ = SetOccupancy(index, bits, RookMaskBits[i]);
                    ulong att = ComputeRookAttacks(i, occ);
                    if (magic != 0UL)
                    {
                        int idx = (int)((occ * magic) >> (64 - bits));
                        RookAttackTable[i][idx] = att;
                    }
                    else
                    {
                        // direct mapping: index already corresponds to the subset ordering
                        RookAttackTable[i][index] = att;
                    }
                }

                //bishops
                maskBitsList.Clear();
                mask = BishopMasks[i];
                while (mask != 0)
                {
                    int sq = BitOperations.TrailingZeroCount(mask);
                    maskBitsList.Add(sq);
                    mask &= mask - 1;
                }

                BishopRelevantBits[i] = maskBitsList.Count;
                BishopMaskBits[i] = maskBitsList.ToArray();

                bits = BishopRelevantBits[i];
                tableSize = 1 << bits;
                BishopAttackTable[i] = new ulong[tableSize];
                // populate attack table. If a precomputed magic is provided (non-zero) use it,
                // otherwise fall back to direct index = subset index (correct but no magic)
                magic = BishopMagics[i];
                for (int index = 0; index < tableSize; index++)
                {
                    ulong occ = SetOccupancy(index, bits, BishopMaskBits[i]);
                    ulong att = ComputeBishopAttacks(i, occ);
                    if (magic != 0UL)
                    {
                        int idx = (int)((occ * magic) >> (64 - bits));
                        BishopAttackTable[i][idx] = att;
                    }
                    else
                    {
                        // direct mapping: index already corresponds to the subset ordering
                        BishopAttackTable[i][index] = att;
                    }
                }
            }
        }

        private static ulong CalculateKnightMoves(int square)
        {
            ulong moves = 0;
            int rank = square / 8;
            int file = square % 8;
            int[] knightOffsets = { -17, -15, -10, -6, 6, 10, 15, 17 };
            foreach (int offset in knightOffsets)
            {
                int targetSquare = square + offset;
                if (targetSquare >= 0 && targetSquare < 64)
                {
                    int targetRank = targetSquare / 8;
                    int targetFile = targetSquare % 8;
                    if (Math.Abs(targetRank - rank) <= 2 && Math.Abs(targetFile - file) <= 2)
                    {
                        moves |= (1UL << targetSquare);
                    }
                }
            }
            return moves;
        }

        private static ulong CalculateKingMoves(int square)
        {
            ulong moves = 0;
            int rank = square / 8;
            int file = square % 8;
            for (int dr = -1; dr <= 1; dr++)
            {
                for (int df = -1; df <= 1; df++)
                {
                    if (dr == 0 && df == 0) continue;
                    int targetRank = rank + dr;
                    int targetFile = file + df;
                    if (targetRank >= 0 && targetRank < 8 && targetFile >= 0 && targetFile < 8)
                    {
                        int targetSquare = targetRank * 8 + targetFile;
                        moves |= (1UL << targetSquare);
                    }
                }
            }
            return moves;
        }

        private static ulong CreateRookMask(int square)
        {
            ulong mask = 0;
            int r = square / 8;
            int c = square % 8;

            // Vertical (North/South) - stop 1 square before the edge
            for (int i = r + 1; i <= 6; i++) mask |= (1UL << (i * 8 + c));
            for (int i = r - 1; i >= 1; i--) mask |= (1UL << (i * 8 + c));

            // Horizontal (East/West) - stop 1 square before the edge
            for (int i = c + 1; i <= 6; i++) mask |= (1UL << (r * 8 + i));
            for (int i = c - 1; i >= 1; i--) mask |= (1UL << (r * 8 + i));

            return mask;
        }

        private static ulong CreateBishopMask(int square)
        {
            ulong mask = 0;
            int r = square / 8;
            int c = square % 8;

            // north-east (stop one before edge)
            for (int rr = r + 1, cc = c + 1; rr <= 6 && cc <= 6; rr++, cc++) mask |= (1UL << (rr * 8 + cc));
            // north-west
            for (int rr = r + 1, cc = c - 1; rr <= 6 && cc >= 1; rr++, cc--) mask |= (1UL << (rr * 8 + cc));
            // south-east
            for (int rr = r - 1, cc = c + 1; rr >= 1 && cc <= 6; rr--, cc++) mask |= (1UL << (rr * 8 + cc));
            // south-west
            for (int rr = r - 1, cc = c - 1; rr >= 1 && cc >= 1; rr--, cc--) mask |= (1UL << (rr * 8 + cc));

            return mask;
        }

        private static ulong SetOccupancy(int index, int bitsCount, int[] bitSquares)
        {
            ulong occ = 0;
            for (int i = 0; i < bitsCount; i++)
            {
                if ((index & (1 << i)) != 0)
                    occ |= (1UL << bitSquares[i]);
            }
            return occ;
        }

        private static ulong ComputeRookAttacks(int square, ulong occ)
        {
            ulong attacks = 0;
            int r = square / 8;
            int c = square % 8;

            // north
            for (int rr = r + 1; rr < 8; rr++)
            {
                int sq = rr * 8 + c;
                attacks |= (1UL << sq);
                if ((occ & (1UL << sq)) != 0) break;
            }
            // south
            for (int rr = r - 1; rr >= 0; rr--)
            {
                int sq = rr * 8 + c;
                attacks |= (1UL << sq);
                if ((occ & (1UL << sq)) != 0) break;
            }
            // east
            for (int cc = c + 1; cc < 8; cc++)
            {
                int sq = r * 8 + cc;
                attacks |= (1UL << sq);
                if ((occ & (1UL << sq)) != 0) break;
            }
            // west
            for (int cc = c - 1; cc >= 0; cc--)
            {
                int sq = r * 8 + cc;
                attacks |= (1UL << sq);
                if ((occ & (1UL << sq)) != 0) break;
            }

            return attacks;
        }

        private static ulong ComputeBishopAttacks(int square, ulong occ)
        {
            ulong attacks = 0;
            int r = square / 8;
            int c = square % 8;
            // north-east
            for (int rr = r + 1, cc = c + 1; rr < 8 && cc < 8; rr++, cc++)
            {
                int sq = rr * 8 + cc;
                attacks |= (1UL << sq);
                if ((occ & (1UL << sq)) != 0) break;
            }
            // north-west
            for (int rr = r + 1, cc = c - 1; rr < 8 && cc >= 0; rr++, cc--)
            {
                int sq = rr * 8 + cc;
                attacks |= (1UL << sq);
                if ((occ & (1UL << sq)) != 0) break;
            }
            // south-east
            for (int rr = r - 1, cc = c + 1; rr >= 0 && cc < 8; rr--, cc++)
            {
                int sq = rr * 8 + cc;
                attacks |= (1UL << sq);
                if ((occ & (1UL << sq)) != 0) break;
            }
            // south-west
            for (int rr = r - 1, cc = c - 1; rr >= 0 && cc >= 0; rr--, cc--)
            {
                int sq = rr * 8 + cc;
                attacks |= (1UL << sq);
                if ((occ & (1UL << sq)) != 0) break;
            }

            return attacks;
        }

        public static ulong GetRookAttacks(int square, ulong occupancy)
        {
            // mask occupancy to relevant bits then use magic multiplication to index the attack table
            int bits = RookRelevantBits[square];
            if (bits == 0) return RookAttackTable[square][0];
            ulong occMasked = occupancy & RookMasks[square];
            ulong magic = RookMagics[square];
            int index = (int)((occMasked * magic) >> (64 - bits));
            return RookAttackTable[square][index];
        }

        public static ulong GetBishopAttacks(int square, ulong occupancy)
        {
            // mask occupancy to relevant bits then use magic multiplication to index the attack table
            int bits = BishopRelevantBits[square];
            if (bits == 0) return BishopAttackTable[square][0];
            ulong occMasked = occupancy & BishopMasks[square];
            ulong magic = BishopMagics[square];
            int index = (int)((occMasked * magic) >> (64 - bits));
            return BishopAttackTable[square][index];
        }
    }
}
