using System.Text;

namespace Resurgent.UtilityBelt.Library.Utilities
{
    public static  class StringHelper
    {
        public static string GetUtf8String(byte[] buffer)
        {
            var result = string.Empty;
            for (var i = 0; i < buffer.Length; i++)
            {
                var value = buffer[i];
                if (value == 0)
                {
                    break;
                }
                result += (char)value;
            }
            return result;
        }

        public static string GetUnicodeString(byte[] buffer)
        {
            var result = string.Empty;
            for (var i = 0; i < buffer.Length; i += 2)
            {
                var value = (short)Encoding.Unicode.GetString(buffer, i, 2)[0];
                if (value == 0)
                {
                    break;
                }
                result += (char)value;
            }
            return result;
        }
    }
}
