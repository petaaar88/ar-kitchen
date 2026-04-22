# AR Kitchen

AR mobile app that helps kitchen makers visualize a kitchen in a client's space before installation. The user places a single cuboid ("voxel") on a detected AR plane to mark the kitchen volume, adjusts its dimensions, then fills it with predefined kitchen items (fridge, counter, sink, oven, etc.) sized to standard measurements.

## Tech stack

- Unity `6000.3.13f1` (Unity 6)
- Universal Render Pipeline (URP) — `URP-Performant` preset
- AR Foundation 6.3.4 (ARCore + ARKit providers)
- XR Interaction Toolkit 3.3.1
- New Input System 1.19
- Target platforms: Android (ARCore), iOS (ARKit)

## Project state

The project was initialized from Unity's Mobile AR Template. The template's demo content has been removed so we can build the kitchen app on a clean AR baseline. What remains:

- `Assets/XR/`, `Assets/XRI/` — XR Plug-in Management and XR Interaction Toolkit settings
- `Assets/Settings/` — URP render pipeline assets
- `Assets/TextMesh Pro/` — TMP essentials
- `Assets/DefaultVolumeProfile.asset` — URP volume profile
- `Assets/Scenes/MainScene.unity` — the working scene (Main Camera + Directional Light baseline)

## Planned architecture

Code will live under `Assets/Scripts/` once we start building. 

## Conventions

- No template leftovers: do not reintroduce the `ARTemplateMenuManager` / `GoalManager` flow. Build our own minimal UI.
- Keep the dependency list lean. The manifest still has packages pulled in by the template (`com.unity.ai.assistant`, `com.unity.ai.inference`, `com.unity.learn.iet-framework`, `com.unity.multiplayer.center`, `com.unity.timeline`) — these are not needed and can be removed from `Packages/manifest.json` when convenient.
- The repo's primary working directory (from Claude's perspective) is `Assets/`. Project settings and packages live one level up.

## Task tracking

Two plain-markdown lists at the repo root, both managed by the `/todo` and `/plan` skills:

- `TODO.md` — planned work, ideas, features.
- `PROBLEMS.md` — things needing review, fixing, or investigation (e.g. *"review `VoxelController.ResizeAxis` — drift suspected"*).

Entry format in both files: `- [ ] [Priority] Short description *(YYYY-MM-DD)*`. Priority is `High`, `Medium`, or `Low`. Keep open items inside the `<!-- OPEN -->` markers and completed items inside `<!-- DONE -->`.

### Skills

- `/plan` — give it a rough idea; it asks clarifying questions, produces a short plan, then writes the steps as entries in `TODO.md` / `PROBLEMS.md`.
- `/todo` — add, complete, or re-prioritize a single entry in either file.
- `/commit` — create a commit following the project's conventions (see next section).

### Session start

`.claude/hooks/session-start.sh` runs on every session start and injects a briefing: last commit + counts and top-priority items from both lists. Open the session by asking what the user wants to do: tackle a problem, start a new feature (`/plan`), pick up a todo, or something else.

## Git workflow

Use the `/commit` skill (`.claude/skills/commit/SKILL.md`) whenever committing or starting a major feature. The rules in short:

- **Major features go on a new branch** (`feat/<short-name>`). A major feature = new system, screen, or user-visible capability (voxel placement, catalog, snapping, etc.). Fixes, tweaks, refactors, and docs stay on `main`.
- **Commit message format:**
  ```
  <short concise overview>

  - <major thing 1>
  - <major thing 2>

  Problem: <only for fixes — what was broken and why>
  ```
- Imperative, under ~70 chars on the first line. No `feat:` / `fix:` prefixes, no AI attribution footers.
- Stage specific files by name — never `git add -A` or `git add .`.
