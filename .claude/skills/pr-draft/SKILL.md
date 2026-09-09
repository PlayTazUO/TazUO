---
name: pr-draft
description: Draft or review a TazUO pull request description from the branch diff. Use when asked to write a PR body, summarize a branch for a PR, or check an existing PR description for missing items. Filters the diff down to user-observable changes and rejects internal churn.
---

# PR Draft

Audience is users reading release notes, not reviewers reading the diff.

## Procedure

1. Read `.github/pull_request_template.md`.
2. `git diff --stat dev...HEAD` (three dots). Read diffs that may change behavior; skip
   tests, docs, comment churn.
3. Apply the eligibility test, write the body.
4. Check `CHANGELOG.md` has a matching entry, right PR number and author.

## Structure

Template order is fixed; content sections follow it. Drop parenthetical qualifiers from
headings (`## Screenshots (if applicable)` → `## Screenshots`).

1. **Description** - one line, the PR's purpose. Never a list.
2. **Type of Change** - tick every box touched, including `Code refactoring`. Ineligible
   work is declared by a ticked box, never a bullet. `Documentation update` means big
   edits to existing docs, or new docs for previously undocumented code - not docs
   written alongside new code.
3. **Testing** - what was done, e.g. `Units, Local test session, Opus CR, Rabbit CR`. Ask
   rather than guess; empty if nothing was.
4. **Screenshots** - heading stays, empty unless screenshots exist.
5. **Content sections** - most important first, e.g. Features > Additions > Improvements
   > Fixes > Additional Notes. Only the ones the branch earns; a small PR takes
   Description plus Additional Notes alone. Features = standalone; Additions = new
   content to existing features.

## Eligibility

Could a user notice without reading the source?

Yes: new feature or option; changed behavior of something they use; fixed symptom;
changed default; something they must act on (config moved, settings reset, downgrade
stops reading their file); scripting API surface.

No, however large: refactors, moves, renames; robustness plumbing with no symptom; new
widgets/helpers/base classes; test infra; perf internals with no measured effect;
comments and design docs; new `language.ini` keys (one "localized X" line covers them);
migration and serialization internals.

## Writing

- Terse, not cryptic. Full sentences, no padding.
- A named feature owns its details. One bullet, not a spec.
- Fixes lead with the symptom. No statable symptom → probably ineligible.
- No file paths, line numbers, or class names the user never types.
- Group related small changes into one line.

## Auditing

Report only: **missing** (eligible change absent), **miscategorized** (fix as a note,
symptom buried in implementation wording), **unfilled section**, **out of order**.

Never list ineligible changes as missing. Never restate what is covered.
