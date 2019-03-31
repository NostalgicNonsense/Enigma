using System;

namespace Assets.Enigma.Enigma.Core.Utility
{
    public static class LongExtensions
    {
        public static byte[] ToBytes(this long num)
        {
            return BitConverter.GetBytes(num);
        }
    }
}
