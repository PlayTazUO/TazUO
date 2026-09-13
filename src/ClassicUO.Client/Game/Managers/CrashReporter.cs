using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using ClassicUO.Configuration;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers;

/// <summary>
///     Turns an unhandled exception into the crash artifacts TazUO leaves behind: an HTML crash log, a
///     <c>Logs/crash.txt</c> entry and, when a webhook is configured, an upload of the same text.
///     Instances are throwaway - the only per-instance state is <see cref="WebHook"/>.
/// </summary>
public class CrashReporter
{
    /// <summary>
    ///     Endpoint <see cref="SendMessage"/> posts the crash text to as a multipart file upload.
    ///     Empty (the default) disables uploading.
    /// </summary>
    public string WebHook { get; set; } = @"";

    private const string INSTALL_ID_FILE_NAME = "installid";

    private static readonly Lazy<string> _installId = new(ResolveInstallId, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    ///     Uploads <paramref name="msgSend"/> to <see cref="WebHook"/> as <c>log.txt</c>, blocking until the
    ///     POST completes. Does nothing if no webhook is set.
    /// </summary>
    /// <remarks>
    ///     No-op in DEBUG builds so local crashes are never reported upstream.
    /// </remarks>
    /// <param name="msgSend">Crash text to upload.</param>
    public void SendMessage(string msgSend)
    {
#if DEBUG
        return;
#endif

        if (string.IsNullOrEmpty(WebHook))
            return;

        // ReSharper disable once ShortLivedHttpClient - Usually done on client death so no point in keeping instance alive
        using var httpClient = new HttpClient();

        var form = new MultipartFormDataContent();
        byte[] fileBytes = Encoding.Unicode.GetBytes(msgSend);
        form.Add(new ByteArrayContent(fileBytes, 0, fileBytes.Length), "Document", "log.txt");
        httpClient.PostAsync(WebHook, form).Wait();
    }

    /// <summary>
    ///     Handles an <see cref="AppDomain.UnhandledException"/>: builds the crash report header (build, runtime,
    ///     OS, thread, install ID, client version), writes the HTML log and <c>Logs/crash.txt</c>, and uploads the
    ///     report unless <see cref="CrashSuggestedFix"/> recognised the exception as a known user-side problem.
    /// </summary>
    /// <remarks>
    ///     Runs while the process is already dying, so it must not throw and must not depend on game state.
    /// </remarks>
    /// <param name="e">Event args from <see cref="AppDomain.UnhandledException"/>.</param>
    public static void ReportAppDomainException(UnhandledExceptionEventArgs e)
    {
        var sb = new StringBuilder();
#if DEV_BUILD || DEBUG
        sb.Append($"[TazUO - DEV (DEBUG: {CUOEnviroment.Debug}) - {CUOEnviroment.Version} - {DateTime.Now}]");
#else
        sb.Append($"[TazUO [STANDARD_BUILD] - {CUOEnviroment.Version} - {DateTime.Now}]");
#endif
        sb.Append($" [{RuntimeInformation.FrameworkDescription}] [{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})]");

        sb.Append($" [{Thread.CurrentThread.Name}]");
        sb.Append($" [InstallID - {GetUniqueInstallId()}]");


        if (Settings.GlobalSettings != null)
            sb.Append($"[{Settings.GlobalSettings.ClientVersion}]");

        sb.AppendLine();

        sb.Append($"Exception:\n{e.ExceptionObject}\n");
        sb.AppendLine();

        string suggestedFix = CrashSuggestedFix.Get(e.ExceptionObject);

        HtmlCrashLogGen.Generate(sb.ToString(), additional_notes: suggestedFix.NotNullNotEmpty() ? suggestedFix : string.Empty);

#if !DEBUG
        if (!suggestedFix.NotNullNotEmpty())
            new CrashReporter().SendMessage(sb.ToString());
#endif

        if (suggestedFix != null)
            sb.AppendLine(suggestedFix);

        Log.Panic(e.ExceptionObject.ToString());
        string path = Path.Combine(CUOEnviroment.ExecutablePath, "Logs");

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        using var crashFile = new LogFile(path, "crash.txt");
        crashFile.Write(sb.ToString());
    }

    /// <summary>
    ///     Returns this installation's anonymous identifier, used to group crash reports coming from the same client.
    ///     The value is a random GUID stored in <c>Data/installid</c>; it holds no hardware, account or network
    ///     information. Generated and written on first use, read back on every later call.
    /// </summary>
    /// <remarks>
    ///     Resolved once per process. If the file can be neither read nor written the identifier is
    ///     <c>"UNKNOWN"</c> for the rest of the session, which groups such reports together rather than per client.
    /// </remarks>
    private static string GetUniqueInstallId() => _installId.Value;

    /// <summary>
    ///     Reads the stored install ID, or generates and persists a new one. Never throws: any IO or permission
    ///     failure is logged and yields <c>"UNKNOWN"</c>, since a crash report is worth sending without an ID.
    /// </summary>
    private static string ResolveInstallId()
    {
        string path = Path.Combine(JsonSaveLocationHelper.DataDirectory, INSTALL_ID_FILE_NAME);

        try
        {
            if (File.Exists(path) && Guid.TryParse(FileSystemHelper.ReadAllTextShared(path).Trim(), out Guid stored))
                return FormatInstallId(stored);

            string generated = FormatInstallId(Guid.NewGuid());
            Directory.CreateDirectory(JsonSaveLocationHelper.DataDirectory);
            File.WriteAllText(path, generated, new UTF8Encoding(false));

            return generated;
        }
        catch (Exception e)
        {
            Log.Warn($"Could not resolve install ID from '{path}' - {e.Message}");
            return "UNKNOWN";
        }
    }

    private static string FormatInstallId(Guid value) => value.ToString("N");
}
