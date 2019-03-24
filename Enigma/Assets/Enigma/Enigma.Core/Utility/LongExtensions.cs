using System;

namespace UtilityCode.Extensions
{
    public static class LongExtensions
    {
        public static byte[] ToBytes(this long num)
        {
            return BitConverter.GetBytes(num);
        }
    }
}
