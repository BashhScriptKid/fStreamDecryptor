using System;

namespace StreamFormatDecryptor
{
    /// <summary>
    /// A fast random number generator for .NET
    /// Based on the xorshift pseudo random number generator (RNG) specified in: 
    /// Marsaglia, George. (2003). Xorshift RNGs.
    /// period of 2^128-1
    /// </summary>
    public class FastRandom
    {
        private const double REAL_UNIT_INT = 1.0 / (int.MaxValue + 1.0);
        private const double REAL_UNIT_UINT = 1.0 / (uint.MaxValue + 1.0);
        private const uint W = 273326509;
        private const uint Y = 842502087, Z = 3579807591;
        private uint w;
        private uint x, y, z;
        private uint bitBuffer;
        private int bitBufferIdx = 32;

        public FastRandom()
        {
            Reinitialise(Environment.TickCount);
        }

        public FastRandom(int seed)
        {
            Reinitialise(seed);
        }

        public void Reinitialise(int seed)
        {
            x = (uint)seed;
            y = Y;
            z = Z;
            w = W;
            bitBufferIdx = 32;
        }

        public uint NextUInt()
        {
            uint t = (x ^ (x << 11));
            x = y;
            y = z;
            z = w;
            return (w = (w ^ (w >> 19)) ^ (t ^ (t >> 8)));
        }

        public void NextBytes(byte[] buffer)
        {
            uint x = this.x, y = this.y, z = this.z, w = this.w;
            int i = 0;
            uint t;
            for (; i < buffer.Length - 3;)
            {
                t = (x ^ (x << 11));
                x = y;
                y = z;
                z = w;
                w = (w ^ (w >> 19)) ^ (t ^ (t >> 8));

                buffer[i++] = (byte)(w & 0x000000FF);
                buffer[i++] = (byte)((w & 0x0000FF00) >> 8);
                buffer[i++] = (byte)((w & 0x00FF0000) >> 16);
                buffer[i++] = (byte)((w & 0xFF000000) >> 24);
            }

            if (i < buffer.Length)
            {
                t = (x ^ (x << 11));
                x = y;
                y = z;
                z = w;
                w = (w ^ (w >> 19)) ^ (t ^ (t >> 8));

                buffer[i++] = (byte)(w & 0x000000FF);
                if (i < buffer.Length)
                {
                    buffer[i++] = (byte)((w & 0x0000FF00) >> 8);
                    if (i < buffer.Length)
                    {
                        buffer[i++] = (byte)((w & 0x00FF0000) >> 16);
                        if (i < buffer.Length)
                        {
                            buffer[i] = (byte)((w & 0xFF000000) >> 24);
                        }
                    }
                }
            }

            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
    }
}
