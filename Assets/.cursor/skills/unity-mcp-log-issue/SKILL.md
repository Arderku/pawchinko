---
name: unity-mcp-log-issue
description: Append exactly one new numbered entry to Docs~/UNITY_MCP_HELPER.md following the Append-an-entry template. Manual only, one-shot per invocation. Use when the user wants to capture a fresh Unity MCP gotcha that surfaced in the current chat.
disable-model-invocation: true
argument-hint: <short title>
arguments: [title]
---

# /unity-mcp-log-issue $title - One-Shot Issue Append

Append **exactly one** new numbered entry to [Docs~/UNITY_MCP_HELPER.md](../../../Docs~/UNITY_MCP_HELPER.md) following the [Append-an-entry template](../../../Docs~/UNITY_MCP_HELPER.md#append-an-entry-template). After step 5 below, stop.

The user invoked this skill mid-chat because something in the current conversation is worth recording. Treat the chat history as the source material.

## Step 1 - Find the next issue number

Read `Docs~/UNITY_MCP_HELPER.md`. Find the highest existing entry under `## Known issues + fixes` (entries are headed `### NN - <title>`). The new entry's number is `NN + 1`, zero-padded to 2 digits. Confirm the number to the user inline ("Drafting as Issue NN...").

## Step 2 - Draft the entry

From the current chat context, draft a new entry titled `$title` matching the template exactly:

```markdown
### NN - $title

- **Symptom**: <what failed; copy the exact error message if helpful>
- **Cause**: <root cause>
- **Fix**: <minimal repro of the workaround>
- **Notes**: <project-specific gotcha, optional - omit the line entirely if not needed>
```

Rules for the draft:

- Pull the **exact** error message from the chat where possible. Do not paraphrase compiler/MCP errors.
- The Fix should include a minimal code snippet if the chat contains one.
- Keep the title slug-style: lowercase words separated by spaces, no trailing period.
- If you genuinely cannot extract a clear Symptom/Cause/Fix from the chat (e.g. user invoked too early), say so and ask the user to provide the missing pieces. Do not append a half-empty entry.

## Step 3 - Show and confirm

Print the drafted entry to the user verbatim, then ask:

> Append as Issue NN? (`y` to append / `edit` to revise / `cancel` to abort)

Wait for the response. Do not append before getting `y`.

## Step 4 - Append (only on `y`)

On `y`:

- Locate the position **immediately before** the closing `---` separator that comes between the last numbered issue and the `## Pre-flight checklist` heading. (The numbered issues live between `## Known issues + fixes` and that `---`.)
- Insert the drafted entry there with one blank line above and below.
- Do **not** modify any other section of the file. Specifically: do not touch `## Append-an-entry template` at the bottom, do not renumber existing entries, do not edit other entries' content.
- Use a single edit operation; verify by reading the file back and confirming the new entry is present and the trailing sections are intact.

On `edit`: ask the user what to change, redraft, return to step 3.

On `cancel`: stop without writing anything.

## Step 5 - Stop

After the append (or cancellation) completes, **stop**. Do not log further issues automatically. The user must invoke `/unity-mcp-log-issue <new title>` again to capture another entry. Do not treat this skill as enabling a "logging mode" for the rest of the chat.
