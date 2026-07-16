using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ClassicUO.Configuration;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers;

/// <summary>
/// A single option within a <see cref="Poll"/>.
///
/// Two storage shapes are supported so the client tolerates schema differences:
/// <list type="bullet">
/// <item>a bare number, e.g. <c>"Yes!": 3</c> — the option key is the label and the count lives directly at
/// <c>options/&lt;key&gt;</c>;</item>
/// <item>an object, e.g. <c>"0": { "text": "Yes!", "votes": 3 }</c> — the label comes from a text field and the
/// count lives at <c>options/&lt;key&gt;/votes</c>.</item>
/// </list>
/// </summary>
public sealed class PollOption
{
    /// <summary>The Firebase key of this option under <c>options/</c>.</summary>
    public string Key { get; set; }

    /// <summary>Human-readable text shown to the user.</summary>
    public string Label { get; set; }

    /// <summary>Current vote count.</summary>
    public int Votes { get; set; }

    /// <summary>Relative Firebase path (under the poll) whose integer value is the vote count.</summary>
    public string VotePath { get; set; }
}

/// <summary>The kind of content carried by a <see cref="PollAttachment"/>.</summary>
public enum AttachmentType
{
    /// <summary>A clickable web link. <see cref="PollAttachment.Data"/> is the URL.</summary>
    Url = 0,

    /// <summary>An image to download and display. <see cref="PollAttachment.Data"/> is the image URL.</summary>
    Image = 1
}

/// <summary>
/// An optional piece of extra content attached to a <see cref="Poll"/>, such as a link or an image.
/// Attachments are parsed defensively: a malformed entry (unknown type, missing/empty data) is skipped.
/// </summary>
public sealed class PollAttachment
{
    /// <summary>What kind of content <see cref="Data"/> refers to.</summary>
    public AttachmentType Type { get; set; }

    /// <summary>The URL of the link or image.</summary>
    public string Data { get; set; }
}

/// <summary>
/// A single poll as stored in the Firebase realtime database.
/// </summary>
public sealed class Poll
{
    public List<PollOption> Options { get; set; } = new();

    public string Question { get; set; } = string.Empty;

    /// <summary>0 = single choice, 1 = multiple choice.</summary>
    public int Type { get; set; }

    /// <summary>Optional extra content (links, images) shown alongside the poll. Never null.</summary>
    public List<PollAttachment> Attachments { get; set; } = new();
}

/// <summary>Outcome of a vote attempt, carrying the server's error message when it fails.</summary>
public readonly struct VoteResult
{
    public bool Success { get; init; }
    public string Error { get; init; }

    public static VoteResult Ok() => new() { Success = true };
    public static VoteResult Fail(string error) => new() { Success = false, Error = error };
}

/// <summary>
/// Fetches available polls from the TazUO Firebase realtime database and submits votes.
///
/// The database security rules only permit a write to an option when the new value is exactly
/// <c>current + 1</c>, so a vote reads the option's current count and writes count + 1. If another
/// client votes between the read and the write, the write is rejected and we retry with a fresh count.
///
/// Poll data is parsed defensively: malformed or incomplete entries (missing question, missing/empty
/// options, non-numeric counts) are skipped so one bad entry can't hide the rest.
/// </summary>
public static class FirebasePollsManager
{
    private const string BASE_URL = "https://tazuopolls-default-rtdb.firebaseio.com/polls";
    private const int VOTE_RETRIES = 4;

    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    /// <summary>
    /// Downloads all available polls keyed by their poll id. Returns <c>null</c> only when the request
    /// itself fails; a successful request with no (or only invalid) polls returns an empty dictionary.
    /// </summary>
    public static async Task<Dictionary<string, Poll>> FetchPollsAsync()
    {
        try
        {
            string json = await _httpClient.GetStringAsync($"{BASE_URL}.json");

            Dictionary<string, Poll> polls = ParsePolls(json);

            // We only reach here on a successful fetch, so it is safe to drop saved "already voted" ids
            // for polls that no longer exist and keep the per-profile list from growing without bound.
            PruneVotedPolls(polls);

            return polls;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to fetch polls: {e}");
            return null;
        }
    }

    /// <summary>
    /// Removes saved "already voted" poll ids that are no longer present in <paramref name="polls"/> so the
    /// per-profile <see cref="Profile.VotedPolls"/> list cannot grow indefinitely. Only call this with a
    /// successfully fetched set — passing a stale or failed result would wrongly forget real votes. The
    /// write is marshalled to the main thread to match the rest of the profile-mutation code.
    /// </summary>
    private static void PruneVotedPolls(Dictionary<string, Poll> polls)
    {
        if (polls == null)
            return;

        Profile profile = ProfileManager.CurrentProfile;
        if (profile == null)
            return;

        string[] voted = (profile.VotedPolls ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries);

        if (voted.Length == 0)
            return;

        var kept = new List<string>(voted.Length);
        foreach (string id in voted)
        {
            if (polls.ContainsKey(id))
                kept.Add(id);
        }

        // Nothing stale to remove.
        if (kept.Count == voted.Length)
            return;

        string updated = string.Join(';', kept);

        MainThreadQueue.InvokeOnMainThread(() =>
        {
            // Guard against the active profile changing between fetch and write.
            if (ReferenceEquals(ProfileManager.CurrentProfile, profile))
                profile.VotedPolls = updated;
        });
    }

    /// <summary>
    /// Fetches the available polls and, if the current profile has any it hasn't voted on yet, returns a
    /// user-facing reminder message. Returns <c>null</c> when the fetch fails, there are no polls, or every
    /// poll has already been voted on. Intended to be shown once after the user is in-world.
    /// </summary>
    public static async Task<string> GetUnvotedNotificationAsync()
    {
        Dictionary<string, Poll> polls = await FetchPollsAsync();

        if (polls == null || polls.Count == 0)
            return null;

        int unvoted = CountUnvoted(polls);

        if (unvoted <= 0)
            return null;

        return unvoted == 1
            ? TazLang.Get("polls_notify_one",
                "There is a new TazUO poll you haven't voted on. Open Polls from the top bar menu to vote.")
            : string.Format(
                TazLang.Get("polls_notify_many",
                    "There are {0} TazUO polls you haven't voted on. Open Polls from the top bar menu to vote."),
                unvoted);
    }

    /// <summary>Counts how many of the given polls the current profile has not yet voted on.</summary>
    private static int CountUnvoted(Dictionary<string, Poll> polls)
    {
        Profile profile = ProfileManager.CurrentProfile;

        var voted = new HashSet<string>((profile?.VotedPolls ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries));

        int count = 0;
        foreach (string pollId in polls.Keys)
        {
            if (!voted.Contains(pollId))
                count++;
        }

        return count;
    }

    /// <summary>
    /// Parses the polls payload, keeping only entries that are well-formed enough to display and vote on.
    /// Each poll is validated independently so a single malformed entry does not break the whole list.
    /// </summary>
    private static Dictionary<string, Poll> ParsePolls(string json)
    {
        var result = new Dictionary<string, Poll>();

        if (string.IsNullOrWhiteSpace(json) || json == "null")
            return result;

        using JsonDocument doc = JsonDocument.Parse(json);

        if (doc.RootElement.ValueKind != JsonValueKind.Object)
            return result;

        foreach (JsonProperty entry in doc.RootElement.EnumerateObject())
        {
            if (TryParsePoll(entry.Value, out Poll poll))
                result[entry.Name] = poll;
        }

        return result;
    }

    private static bool TryParsePoll(JsonElement element, out Poll poll)
    {
        poll = null;

        if (element.ValueKind != JsonValueKind.Object)
            return false;

        // Question is required and must be a non-empty string.
        if (!element.TryGetProperty("question", out JsonElement questionEl) ||
            questionEl.ValueKind != JsonValueKind.String)
            return false;

        string question = questionEl.GetString();
        if (string.IsNullOrWhiteSpace(question))
            return false;

        // Type is optional; default to single-choice (0) when absent or malformed.
        int type = 0;
        if (element.TryGetProperty("type", out JsonElement typeEl) &&
            typeEl.ValueKind == JsonValueKind.Number &&
            typeEl.TryGetInt32(out int parsedType))
            type = parsedType;

        // Options are required.
        if (!element.TryGetProperty("options", out JsonElement optionsEl) ||
            optionsEl.ValueKind != JsonValueKind.Object)
            return false;

        var options = new List<PollOption>();
        foreach (JsonProperty option in optionsEl.EnumerateObject())
        {
            if (TryParseOption(option.Name, option.Value, out PollOption parsed))
                options.Add(parsed);
        }

        if (options.Count == 0)
            return false;

        // Attachments are entirely optional; a missing, malformed, or empty array yields no attachments.
        List<PollAttachment> attachments = ParseAttachments(element);

        poll = new Poll { Question = question, Type = type, Options = options, Attachments = attachments };
        return true;
    }

    /// <summary>
    /// Parses the optional <c>attachments</c> array. Individual entries are validated independently, and any
    /// malformed one (not an object, unknown/missing type, or missing/empty data) is skipped rather than
    /// failing the whole poll. Returns an empty list when the key is absent or not an array.
    /// </summary>
    private static List<PollAttachment> ParseAttachments(JsonElement element)
    {
        var attachments = new List<PollAttachment>();

        if (!element.TryGetProperty("attachments", out JsonElement attachmentsEl) ||
            attachmentsEl.ValueKind != JsonValueKind.Array)
            return attachments;

        foreach (JsonElement entry in attachmentsEl.EnumerateArray())
        {
            if (TryParseAttachment(entry, out PollAttachment attachment))
                attachments.Add(attachment);
        }

        return attachments;
    }

    private static bool TryParseAttachment(JsonElement element, out PollAttachment attachment)
    {
        attachment = null;

        if (element.ValueKind != JsonValueKind.Object)
            return false;

        // Type is required and must be a known value.
        if (!element.TryGetProperty("type", out JsonElement typeEl) ||
            typeEl.ValueKind != JsonValueKind.Number ||
            !typeEl.TryGetInt32(out int type) ||
            !Enum.IsDefined(typeof(AttachmentType), type))
            return false;

        // Data is required and must be a non-empty string.
        if (!element.TryGetProperty("data", out JsonElement dataEl) ||
            dataEl.ValueKind != JsonValueKind.String)
            return false;

        string data = dataEl.GetString();
        if (string.IsNullOrWhiteSpace(data))
            return false;

        attachment = new PollAttachment { Type = (AttachmentType)type, Data = data };
        return true;
    }

    /// <summary>Parses a single option in either the bare-number or object (<c>votes</c>) shape.</summary>
    private static bool TryParseOption(string key, JsonElement value, out PollOption option)
    {
        option = null;

        // Shape 1: bare number — key is the label, count is the value at options/<key>.
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int directCount))
        {
            option = new PollOption
            {
                Key = key,
                Label = key,
                Votes = directCount,
                VotePath = $"options/{Uri.EscapeDataString(key)}"
            };
            return true;
        }

        // Shape 2: object — count lives at options/<key>/votes, label from a text field (or the key).
        if (value.ValueKind == JsonValueKind.Object)
        {
            int votes = 0;
            if (value.TryGetProperty("votes", out JsonElement votesEl) &&
                votesEl.ValueKind == JsonValueKind.Number &&
                votesEl.TryGetInt32(out int parsedVotes))
                votes = parsedVotes;

            option = new PollOption
            {
                Key = key,
                Label = ReadLabel(value) ?? key,
                Votes = votes,
                VotePath = $"options/{Uri.EscapeDataString(key)}/votes"
            };
            return true;
        }

        return false;
    }

    private static string ReadLabel(JsonElement optionObj)
    {
        foreach (string field in new[] { "text", "label", "name", "title", "option" })
        {
            if (optionObj.TryGetProperty(field, out JsonElement el) &&
                el.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(el.GetString()))
                return el.GetString();
        }

        return null;
    }

    /// <summary>
    /// Casts a vote for <paramref name="option"/> on <paramref name="pollId"/> by incrementing its stored
    /// count by one. Retries a few times if a concurrent vote invalidates the +1 write rule.
    /// </summary>
    public static async Task<VoteResult> VoteAsync(string pollId, PollOption option)
    {
        string url = $"{BASE_URL}/{Uri.EscapeDataString(pollId)}/{option.VotePath}.json";

        string lastError = "Unknown error";

        for (int attempt = 0; attempt < VOTE_RETRIES; attempt++)
        {
            try
            {
                int current = await GetOptionValueAsync(url);

                var content = new StringContent((current + 1).ToString(), Encoding.UTF8, "application/json");
                HttpResponseMessage response = await _httpClient.PutAsync(url, content);

                if (response.IsSuccessStatusCode)
                    return VoteResult.Ok();

                string body = await response.Content.ReadAsStringAsync();
                lastError = $"{(int)response.StatusCode} {response.StatusCode}: {body}";
                Log.Error($"Vote for poll '{pollId}' option '{option.Key}' rejected ({url}): {lastError}");

                // A rejected write (e.g. permission denied because someone else voted first) is worth
                // retrying with a freshly read count; anything else is unlikely to recover.
                if (response.StatusCode != HttpStatusCode.Unauthorized &&
                    response.StatusCode != HttpStatusCode.Forbidden &&
                    response.StatusCode != HttpStatusCode.BadRequest)
                {
                    return VoteResult.Fail(lastError);
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error voting on poll '{pollId}': {e}");
                return VoteResult.Fail(e.Message);
            }
        }

        return VoteResult.Fail(lastError);
    }

    private static async Task<int> GetOptionValueAsync(string url)
    {
        string json = await _httpClient.GetStringAsync(url);

        if (string.IsNullOrWhiteSpace(json) || json == "null")
            return 0;

        return int.TryParse(json.Trim(), out int value) ? value : 0;
    }
}
