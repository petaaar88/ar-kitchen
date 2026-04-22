---
name: todo
description: Add, update, or complete an item in TODO.md (planned work/ideas) or PROBLEMS.md (things needing review/fix). Use when the user says "add to todo", "track this problem", "review this script", "mark done", or similar list-management phrasing.
---

# Todo / Problems list management

Two files at the repo root:

- `TODO.md` — planned work, ideas, features
- `PROBLEMS.md` — things that need review, fixing, or investigation (buggy code, scripts flagged for review, suspicious behavior)

Both use the same entry format and contain `<!-- OPEN -->` and `<!-- DONE -->` marker sections. Keep entries inside the markers so they stay easy to grep.

## Entry format

```
- [ ] [Priority] Short description *(YYYY-MM-DD)*
```

- **Priority:** `High`, `Medium`, or `Low` — exactly as shown, inside brackets.
- **Description:** one line, imperative-ish, ~80 chars max. Include a file path or symbol if it helps (e.g. `Review VoxelController.ResizeAxis — drift suspected`).
- **Date:** today's ISO date, in italics with parens. The skill prompt includes today's date as `currentDate` — use that.

## Which file?

- Defaults by keyword:
  - "review", "fix", "bug", "broken", "suspicious", "drift", "crash" → `PROBLEMS.md`
  - "add feature", "build", "implement", "idea", "later", "plan" → `TODO.md`
- If the user hasn't made it clear, **ask** which file before writing.

## Priority

- If the user didn't state one, ask: "High, Medium, or Low?" — don't guess.
- Accept shorthand: `h` / `m` / `l` → `High` / `Medium` / `Low`.

## Steps to add

1. Read the target file.
2. Find the `<!-- OPEN -->` / `<!-- /OPEN -->` block.
3. Insert the new line at the **top** of the open block (newest first), grouped by priority if the user wants ordering — otherwise just top.
4. Write the file. Don't touch anything outside the markers.
5. Confirm back to the user with the exact line you added and which file.

## Steps to mark done

1. Find the matching open line (ask the user to disambiguate if multiple match).
2. Change `- [ ]` to `- [x]`.
3. Move it out of the `OPEN` block and into the `DONE` block at the top.
4. Optionally append ` — done YYYY-MM-DD` to the line.

## Steps to edit / re-prioritize

1. Locate the line. If ambiguous, list matches and ask.
2. Update the priority tag or description in place. Keep the original date.

## Examples

User: "add to todo: build the catalog browser UI, medium"
→ `TODO.md`, open block, top:
```
- [ ] [Medium] Build the catalog browser UI *(2026-04-21)*
```

User: "track this: VoxelController resize math looks wrong, high"
→ `PROBLEMS.md`, open block, top:
```
- [ ] [High] VoxelController resize math looks wrong *(2026-04-21)*
```

User: "mark catalog browser UI done"
→ Move that line from OPEN to DONE in `TODO.md`, flip `[ ]` → `[x]`.

## Don'ts

- Don't reformat other entries.
- Don't drop the date.
- Don't invent a priority — ask.
- Don't sort or rewrite the whole file unless the user asks.
