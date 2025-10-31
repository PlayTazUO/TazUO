using System;
using System.IO;
using System.Threading.Tasks;
using ClassicUO.Utility.Logging;

namespace ClassicUO;

public static class HtmlCrashLogGen
{
    private static string SafeHtmlEncode(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        try
        {
            // Try using WebUtility.HtmlEncode first
            return System.Net.WebUtility.HtmlEncode(text);
        }
        catch
        {
            // Fallback to manual encoding if WebUtility fails
            // This avoids buffer pool issues that can occur with ValueStringBuilder
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }
    }

    public static void Generate(string stackTrace, string title = "TazUO Crash Report", string description = "Oh no! TazUO crashed.")
    {
        const string TEMPLATE = """
                                <!DOCTYPE html>
                                <html lang="en">
                                <head>
                                  <meta charset="UTF-8" />
                                  <title>[TITLE]</title>
                                  <style>
                                    body {
                                      margin: 0;
                                      font-family: "Segoe UI", Tahoma, Geneva, Verdana, sans-serif;
                                      background-color: #1a1a1a;
                                      color: #e0e0e0;
                                      padding: 2rem;
                                    }

                                    h1 {
                                      font-size: 1.8rem;
                                      color: #ff8c42; /* dark orange */
                                      margin-bottom: 1rem;
                                    }


                                    a {
                                      font-family: "Segoe UI", Tahoma, Geneva, Verdana, sans-serif;
                                      color: #ff8c42;
                                    }

                                    p {
                                      margin-bottom: 1rem;
                                    }

                                    pre {
                                      background-color: #2b2b2b;
                                      color: #f8f8f2;
                                      border: 1px solid #ff8c42;
                                      padding: 1rem;
                                      border-radius: 8px;
                                      white-space: pre-wrap;
                                      word-break: break-word;
                                      overflow-x: auto;
                                    }

                                    button {
                                      margin-top: 1rem;
                                      padding: 0.5rem 1.2rem;
                                      font-size: 1rem;
                                      border: none;
                                      border-radius: 5px;
                                      background-color: #333;
                                      color: #fff;
                                      cursor: pointer;
                                      border: 1px solid #444;
                                      transition: background-color 0.2s, border-color 0.2s;
                                    }

                                    button:hover {
                                      background-color: #ff8c42;
                                      color: #000;
                                      border-color: #ff8c42;
                                    }
                                  </style>
                                </head>
                                <body>
                                  <h1>[TITLE]</h1>
                                  <p>[DESCRIPTION]<br>If you'd like support for this please copy and send this to our <a href="https://github.com/PlayTazUO/TazUO/issues">GitHub</a> or <a href="https://discord.gg/QvqzkB95G4">Discord</a>:</p>
                                  <pre id="stackTrace">
                                ```
                                [STACK TRACE]
                                ```
                                  </pre>
                                  <button onclick="copyStack()">Copy to Clipboard</button>

                                  <script>
                                    function copyStack() {
                                      const text = document.getElementById('stackTrace').textContent;

                                      // Try modern clipboard API first
                                      if (navigator.clipboard && navigator.clipboard.writeText) {
                                        navigator.clipboard.writeText(text)
                                          .then(() => alert('Stack trace copied to clipboard'))
                                          .catch(() => fallbackCopy(text));
                                      } else {
                                        fallbackCopy(text);
                                      }
                                    }

                                    function fallbackCopy(text) {
                                      // Fallback for file:// protocol and older browsers
                                      const textArea = document.createElement('textarea');
                                      textArea.value = text;
                                      textArea.style.position = 'fixed';
                                      textArea.style.opacity = '0';
                                      document.body.appendChild(textArea);
                                      textArea.select();
                                      try {
                                        document.execCommand('copy');
                                        alert('Stack trace copied to clipboard');
                                      } catch (err) {
                                        alert('Failed to copy. Please select the text and copy manually (Ctrl+C).');
                                      }
                                      document.body.removeChild(textArea);
                                    }
                                  </script>
                                </body>
                                </html>
                                """;
        stackTrace = stackTrace.Trim();

        // Use safe HTML encoding with fallback to avoid buffer pool issues
        string encodedStackTrace = SafeHtmlEncode(stackTrace);
        string encodedTitle = SafeHtmlEncode(title);
        string encodedDescription = SafeHtmlEncode(description);

        string html = TEMPLATE.Replace("[STACK TRACE]", encodedStackTrace);
        html = html.Replace("[TITLE]", encodedTitle);
        html = html.Replace("[DESCRIPTION]", encodedDescription);

        try
        {
            Log.Trace("Generating HTML Crash report...");

            // Create unique filename with proper path handling (avoids race condition)
            string filePath = Path.Combine(Path.GetTempPath(), $"TazUO_Crash_{Guid.NewGuid():N}.html");
            File.WriteAllText(filePath, html);

            // Launch browser with proper file path (skipValidation: true for local files)
            Utility.Platforms.PlatformHelper.LaunchBrowser(filePath, skipValidation: true);

            Log.Trace($"Crash report saved to: {filePath}");
        }
        catch (Exception e)
        {
            Log.Error($"Failed to generate HTML crash report: {e}");
            // Fallback to console output
            try
            {
                Console.Error.WriteLine("===== CRASH REPORT =====");
                Console.Error.WriteLine(stackTrace);
                Console.Error.WriteLine("========================");
            }
            catch
            {
                // Nothing more we can do
            }
        }
    }
}
