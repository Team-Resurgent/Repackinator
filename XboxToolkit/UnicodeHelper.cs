using System.Text;

namespace XboxToolkit
{
    public class UnicodeHelper
    {
        public static string GetUtf8String(byte[] buffer)
        {
            var length = 0;
            for (var i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] != 0)
                {
                    length++;
                    continue;
                }
                break;
            }
            return length == 0 ? string.Empty : Encoding.UTF8.GetString(buffer, 0, length);
        }

        public static string GetUnicodeString(byte[] buffer)
        {
            var length = 0;
            for (var i = 0; i < buffer.Length; i += 2)
            {
                if (buffer[i] != 0 || buffer[i + 1] != 0)
                {
                    length += 2;
                    continue;
                }
                break;
            }
            return length == 0 ? string.Empty : Encoding.Unicode.GetString(buffer, 0, length);
        }
    }
}
