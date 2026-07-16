#!/usr/bin/env python3
"""Interactive builder for a TazUO Firebase poll entry.

Walks you through the question, type, options, and optional attachments, then
prints the final JSON for a single poll entry that can be pasted under the
`polls` node of the Firebase realtime database.

See docs/FirebasePollsFormat.md for the full format description.
"""

import json


def ask(prompt, default=None):
    suffix = f" [{default}]" if default is not None else ""
    value = input(f"{prompt}{suffix}: ").strip()
    if not value and default is not None:
        return default
    return value


def ask_yes_no(prompt, default=False):
    d = "Y/n" if default else "y/N"
    value = input(f"{prompt} ({d}): ").strip().lower()
    if not value:
        return default
    return value in ("y", "yes")


def ask_poll_type():
    print("\nPoll type:")
    print("  0 = single choice (default)")
    print("  1 = multiple choice")
    while True:
        value = input("Choose type [0]: ").strip()
        if not value:
            return 0
        if value in ("0", "1"):
            return int(value)
        print("  Please enter 0 or 1.")


def ask_options():
    print("\nEnter poll options. Leave the label blank to finish.")
    options = {}
    while True:
        label = input(f"  Option {len(options) + 1} label: ").strip()
        if not label:
            if len(options) == 0:
                print("  A poll needs at least one option.")
                continue
            break
        if label in options:
            print("  That option already exists, choose a different label.")
            continue

        votes_raw = input("    Starting votes [0]: ").strip()
        try:
            votes = int(votes_raw) if votes_raw else 0
        except ValueError:
            print("    Votes must be a whole number, using 0.")
            votes = 0

        # Bare-number shape: the label is the key and the value is the vote count.
        options[label] = votes

    return options


def ask_attachments():
    print("\nAttachments are optional. Types:")
    print("  0 = URL   (clickable link)")
    print("  1 = image (downloaded and shown inline)")

    if not ask_yes_no("Add attachments?", default=False):
        return None

    attachments = []
    while True:
        type_raw = input(f"  Attachment {len(attachments) + 1} type (0=url, 1=image, blank=done): ").strip()
        if not type_raw:
            break
        if type_raw not in ("0", "1"):
            print("    Type must be 0 or 1.")
            continue

        data = input("    Data (URL): ").strip()
        if not data:
            print("    Data is required, skipping this attachment.")
            continue

        attachments.append({"type": int(type_raw), "data": data})

    return attachments or None


def build_poll():
    print("--- TazUO Poll Builder ---")

    question = ""
    while not question:
        question = ask("Poll question").strip()
        if not question:
            print("  The question cannot be empty.")

    poll_type = ask_poll_type()
    options = ask_options()
    attachments = ask_attachments()

    poll = {
        "question": question,
        "type": poll_type,
        "options": options,
    }
    if attachments:
        poll["attachments"] = attachments

    poll_id = ask("\nPoll id (the key under 'polls')", default="my-poll")

    print("\n--- Poll entry JSON ---")
    print(json.dumps({poll_id: poll}, indent=2))
    print("-----------------------")


if __name__ == "__main__":
    while True:
        build_poll()
        if not ask_yes_no("\nBuild another poll?", default=False):
            break
