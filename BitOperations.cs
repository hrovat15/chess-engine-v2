using System;
using System.Collections.Generic;
using System.Text;

namespace chess_engine_v2
{
    class BitOperations
    {
        public static int PopCount(ulong value)
        {
            // Brian Kernighan's algorithm
            int count = 0;
            while (value != 0)
            {
                value &= value - 1;
                count++;
            }
            return count;
        }

        public static int TrailingZeroCount(ulong value)
        {
            if (value == 0) return 64;
            int count = 0;
            while ((value & 1) == 0)
            {
                value >>= 1;
                count++;
            }
            return count;
        }
    }
}
