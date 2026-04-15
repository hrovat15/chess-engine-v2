using System;
using System.Collections.Generic;
using System.Text;

namespace chess_engine_v2
{
    public readonly struct Move
    {
        // The raw 16-bit data
        public readonly ushort Value;

        // Masks for extraction
        private const ushort ToMask = 0x3F;      // 0000 0000 0011 1111
        private const ushort FromMask = 0xFC0;     // 0000 1111 1100 0000
        private const ushort FlagMask = 0xF000;    // 1111 0000 0000 0000

        public Move(ushort value) => Value = value;

        // Constructor to pack the bits
        public Move(int from, int to, ushort flags)
        {
            Value = (ushort)(to | (from << 6) | (flags << 12));
        }

        // Properties to unpack the bits on the fly
        public int To => Value & ToMask;
        public int From => (Value & FromMask) >> 6;
        public int Flags => (Value & FlagMask) >> 12;

        // Helper methods
        public bool IsPromotion => Flags >= 8;
        public bool IsCapture => (Flags & 4) != 0;

        public override string ToString() => $"{From} to {To} (Flags: {Flags})";
    }
}
