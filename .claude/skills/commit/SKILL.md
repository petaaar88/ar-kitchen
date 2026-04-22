---
name: commit
description: Create a git commit following this project's conventions — short overview line, bullet points of major work, and a problem description below when the commit is a fix. Also enforces the branch rule: major features go on a new branch before any work starts.
---

# Commit (AR Kitchen conventions)

Use this skill whenever the user asks to commit, push, or start work on a major feature.

## Branch rule

Before doing the work for a **major feature**, create a new branch:

```bash
git checkout -b feat/<short-name>
```

"Major feature" = anything that adds a new system, screen, or user-visible capability (voxel placement, catalog browser, item snapping, etc.). Bug fixes, small tweaks, refactors, and docs changes stay on `main`.

If the user asks to start a major feature and the current branch is `main`, propose a branch name and confirm before switching.

## Commit message format

```
<short concise overview of the work>

- <major thing 1>
- <major thing 2>
- <major thing 3>

[Problem: only if this commit is a fix]
<one or two sentences describing what was broken and why>
```

Rules:
- First line: imperative mood, under ~70 characters. No scope prefix, no "feat:" / "fix:" tags.
- Blank line, then bullet points covering the major changes. Skip bullets for trivial one-file commits.
- If this is a fix, add a blank line and a `Problem:` section describing the original bug — what was wrong, not what you changed.
- No Claude/AI attribution footer. No "Generated with" lines. No `Co-Authored-By`.

## Example — feature

```
Add voxel placement on AR plane tap

- AR raycast hit turns into world-space voxel spawn
- VoxelController drives per-axis resize handles
- UI: single tap to place, drag handle to resize
```

## Example — fix

```
Stop voxel drifting after plane update

- Re-parent voxel to ARAnchor on first placement instead of tracked plane
- Cache placement pose so plane re-classification doesn't move it

Problem: when ARCore merged two detected planes the voxel jumped to the new plane's origin, breaking placement the user had already adjusted.
```

## Steps

1. Check current branch with `git status`. If the task is a major feature and branch is `main`, ask the user for a branch name or propose one, then `git checkout -b`.
2. Review `git status` and `git diff` to understand what's actually changing.
3. Stage specific files by name (not `git add -A` / `.`) — avoid accidentally committing `.env`, build artifacts, or unrelated files.
4. Draft the message per the format above. If uncertain whether this is a fix, ask.
5. Commit with a HEREDOC so the message keeps its line breaks:
   ```bash
   git commit -m "$(cat <<'EOF'
   <message here>
   EOF
   )"
   ```
6. Run `git status` after to confirm the commit landed.
7. Push to `origin`. If the branch has no upstream yet (new feature branch), use `git push -u origin HEAD`; otherwise `git push`. Do **not** force-push.
