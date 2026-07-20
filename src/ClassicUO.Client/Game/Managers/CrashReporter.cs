using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    public class CrashReporter
    {
        public string WebHook { get; set; } = @"}ODDy~xyCxDv~Dzw}DFJFMLJLHHHFHEIFGFFIDKvyGvVzK^__NdjjzBFjzZjgHfX_XJF_k~ojdGnJIgtdbgj_z~vg^`~mNI";

        public CrashReporter()
        {
        }

        public void SendMessage(string msgSend)
        {
#if DEBUG
            // Short-circuit in debug
            return;
#else
            if (string.IsNullOrEmpty(WebHook))
                return;

            using var httpClient = new HttpClient();

            var form = new MultipartFormDataContent();
            byte[] fileBytes = Encoding.Unicode.GetBytes(msgSend);
            form.Add(new ByteArrayContent(fileBytes, 0, fileBytes.Length), "Document", "log.txt");
            httpClient.PostAsync(Obf(WebHook, -21), form).Wait();
#endif
        }

        public static string Obf(string source, int shift)
        {
            // The total number of characters in the UTF-16 space
            int totalChars = 65536;
            char[] buffer = source.ToCharArray();

            for (int i = 0; i < buffer.Length; i++)
            {
                // Use a modulo operation to keep the shift within the 0-65535 range
                // This handles massive shifts and negative shifts perfectly
                int shifted = (Convert.ToInt32(buffer[i]) + shift) % totalChars;

                // Handle negative results from C#'s % operator on negative numbers
                if (shifted < 0)
                {
                    shifted += totalChars;
                }

                buffer[i] = Convert.ToChar(shifted);
            }

            return new string(buffer);
        }
    }
}
