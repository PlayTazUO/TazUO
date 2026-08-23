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
        public string WebHook { get; set; } = @"";

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
            httpClient.PostAsync(WebHook, form).Wait();
#endif
        }
    }
}
