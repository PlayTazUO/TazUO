# TazUO Firebase Polls Format

TazUO community polls are stored in a Firebase realtime database and shown in the in-game
**Polls** window (opened from the top bar menu). This document describes the JSON shape the
client expects.

- **Database root:** `https://tazuopolls-default-rtdb.firebaseio.com/polls`
- **Client parser:** `src/ClassicUO.Client/Game/Managers/FirebasePollsManager.cs`
- **Window:** `src/ClassicUO.Client/Game/UI/MyraWindows/PollsWindow.cs`
- **Interactive builder:** `tools/poll_builder.py` — walks you through a poll and prints the JSON entry.

Polls are parsed defensively. Each poll — and each option/attachment within it — is validated
independently, so a single malformed entry is skipped rather than breaking the rest of the list.

## Top-level structure

`polls` is an object keyed by an arbitrary poll id. Each value is a poll object:

```json
{
  "polls": {
    "my-first-poll": {
      "question": "What is your favorite feature?",
      "type": 0,
      "options": {
        "Grid containers": 5,
        "Auto loot": 12,
        "Python scripting": 8
      },
      "attachments": [
        {
          "type": 0,
          "data": "https://github.com/PlayTazUO/TazUO"
        },
        {
          "type": 1,
          "data": "https://example.com/preview.png"
        }
      ]
    }
  }
}
```

## Poll fields

| Field         | Required | Type   | Notes |
|---------------|----------|--------|-------|
| `question`    | Yes      | string | Must be a non-empty string, or the poll is skipped. |
| `type`        | No       | number | `0` = single choice (default), `1` = multiple choice. Defaults to `0` if missing or malformed. |
| `options`     | Yes      | object | Must contain at least one valid option, or the poll is skipped. |
| `attachments` | No       | array  | Optional. See below. A missing, empty, or malformed value simply shows no attachments. |

### Options

Options live under `options` and support two shapes. Both may not be mixed meaningfully within a
poll — pick whichever fits your tooling.

**Bare number** — the key is the label and the value is the vote count:

```json
"options": {
  "Yes!": 3,
  "No": 1
}
```

**Object** — the label comes from a text field and the count lives at `votes`:

```json
"options": {
  "0": { "text": "Yes!", "votes": 3 },
  "1": { "text": "No", "votes": 1 }
}
```

The label is read from the first present of: `text`, `label`, `name`, `title`, `option`;
if none exist, the option key is used. A missing `votes` defaults to `0`.

Voting increments the option's stored count by exactly one (the database rules only allow a
`current + 1` write), retrying if another client votes concurrently.

## Attachments (optional)

`attachments` is an optional array of extra content displayed with the poll. The key may be
absent entirely — this produces no errors and shows nothing.

Each attachment is an object:

| Field  | Required | Type   | Notes |
|--------|----------|--------|-------|
| `type` | Yes      | number | `0` = URL (clickable link), `1` = image (downloaded and shown inline). |
| `data` | Yes      | string | The URL of the link or image. Must be a non-empty string. |

```json
"attachments": [
  { "type": 0, "data": "https://github.com/PlayTazUO/TazUO" },
  { "type": 1, "data": "https://example.com/preview.png" }
]
```

### Attachment types

- **`0` — URL:** Rendered as a clickable link that opens in the system browser.
- **`1` — Image:** Downloaded asynchronously on a background thread and displayed inline once
  ready. Images are scaled down to fit the poll while preserving aspect ratio. Downloaded
  textures are cached per URL for the process lifetime, so each image is only fetched once.

### Handling of malformed attachments

An individual attachment is **skipped** (the rest of the poll still displays) when:

- it is not a JSON object;
- `type` is missing, non-numeric, or not a known value (`0` or `1`);
- `data` is missing, not a string, or empty/whitespace.

A skipped attachment never produces an error or affects other attachments or the poll itself.
