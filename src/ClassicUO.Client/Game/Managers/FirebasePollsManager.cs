using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers;

/// <summary>
/// A single poll as stored in the Firebase realtime database.
/// The JSON shape is:
/// <code>{ "options": { "No :(": 0, "Yes!": 0 }, "question": "Are you enjoying TazUO?", "type": 0 }</code>
/// </summary>
public sealed class Poll
{
    [JsonPropertyName("options")]
    public Dictionary<string, int> Options { get; set; } = new();

    [JsonPropertyName("question")]
    public string Question { get; set; } = string.Empty;

    /// <summary>0 = single choice, 1 = multiple choice.</summary>
    [JsonPropertyName("type")]
    public int Type { get; set; }
}

[JsonSerializable(typeof(Dictionary<string, Poll>))]
[JsonSerializable(typeof(Poll))]
internal sealed partial class PollsJsonContext : JsonSerializerContext { }

/// <summary>
/// Fetches available polls from the TazUO Firebase realtime database and submits votes.
///
/// The database security rules only permit a write to an option when the new value is exactly
/// <c>current + 1</c>, so a vote reads the option's current count and writes count + 1. If another
/// client votes between the read and the write, the write is rejected and we retry with a fresh count.
/// </summary>
public static class FirebasePollsManager
{
    private const string BASE_URL = "https://tazuopolls-default-rtdb.firebaseio.com/polls";
    private const int VOTE_RETRIES = 4;

    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    /// <summary>Downloads all available polls keyed by their poll id.</summary>
    public static async Task<Dictionary<string, Poll>> FetchPollsAsync()
    {
        try
        {
            string json = await _httpClient.GetStringAsync($"{BASE_URL}.json");

            if (string.IsNullOrWhiteSpace(json) || json == "null")
                return new Dictionary<string, Poll>();

            Dictionary<string, Poll> polls =
                JsonSerializer.Deserialize(json, PollsJsonContext.Default.DictionaryStringPoll);

            return polls ?? new Dictionary<string, Poll>();
        }
        catch (Exception e)
        {
            Log.Error($"Failed to fetch polls: {e}");
            return null;
        }
    }

    /// <summary>
    /// Casts a vote for <paramref name="optionName"/> on <paramref name="pollId"/> by incrementing the
    /// stored count by one. Retries a few times if a concurrent vote invalidates the +1 write rule.
    /// </summary>
    /// <returns>True if the vote was recorded, otherwise false.</returns>
    public static async Task<bool> VoteAsync(string pollId, string optionName)
    {
        string optionUrl =
            $"{BASE_URL}/{Uri.EscapeDataString(pollId)}/options/{Uri.EscapeDataString(optionName)}.json";

        for (int attempt = 0; attempt < VOTE_RETRIES; attempt++)
        {
            try
            {
                int current = await GetOptionValueAsync(optionUrl);

                var content = new StringContent((current + 1).ToString(), Encoding.UTF8, "application/json");
                HttpResponseMessage response = await _httpClient.PutAsync(optionUrl, content);

                if (response.IsSuccessStatusCode)
                    return true;

                // A rejected write (e.g. permission denied because someone else voted first) is worth
                // retrying with a freshly read count; anything else is unlikely to recover.
                if (response.StatusCode != HttpStatusCode.Unauthorized &&
                    response.StatusCode != HttpStatusCode.Forbidden &&
                    response.StatusCode != HttpStatusCode.BadRequest)
                {
                    Log.Error($"Vote for poll '{pollId}' option '{optionName}' failed: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error voting on poll '{pollId}': {e}");
                return false;
            }
        }

        return false;
    }

    private static async Task<int> GetOptionValueAsync(string optionUrl)
    {
        string json = await _httpClient.GetStringAsync(optionUrl);

        if (string.IsNullOrWhiteSpace(json) || json == "null")
            return 0;

        return int.TryParse(json.Trim(), out int value) ? value : 0;
    }
}
