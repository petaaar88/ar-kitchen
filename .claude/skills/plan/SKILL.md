---
name: plan
description: Turn a user's rough idea or solution sketch into a concrete plan by asking clarifying questions first, then break the plan into prioritized entries in TODO.md (and PROBLEMS.md for anything to review/fix). Use when the user says "I want to build X", "here's my idea", "let's plan", or presents a proposed solution they want firmed up before writing code.
---

# Plan

The user brings an idea or proposed solution. Your job is **not** to jump to code — it's to pressure-test the idea through questions, agree on a plan, then drop the steps into the project's task lists.

## Flow

### 1. Restate what you heard

One short paragraph: "So you want to [X] by doing [Y], so that [Z]. Is that right?" Let the user correct you before going further.

### 2. Ask clarifying questions

Ask 3–6 focused questions. Prioritize ambiguity with real downstream impact. Cover at least:

- **Scope** — what's in, what's explicitly out? MVP vs. full version?
- **User-facing behavior** — exactly what does the user do/see/tap? Happy path and one edge case.
- **Inputs & outputs** — what data does it consume, what does it produce, where does it live?
- **Integration points** — which existing scripts/scenes/prefabs does it touch? What must not change?
- **Constraints** — performance, platform (Android/iOS), standard measurements, units (meters), AR specifics.
- **Failure modes** — what happens if AR tracking is lost, if the user taps nothing, if dimensions are invalid?
- **Done criteria** — how will we know it works?

Ask them in a numbered list so the user can answer inline. Don't ask ones the user already answered. Don't ask questions whose answers don't change the plan.

### 3. Propose the plan

Once answers are in, produce a short plan:

```
Plan: <feature name>

Goal: <one sentence>

Steps:
1. <step> — <one-line rationale>
2. <step>
3. ...

Open questions / risks:
- <thing still unclear>

Touches: <files/systems>
```

Keep it tight. No essays. Each step should be a thing a future session can pick up as a discrete task.

### 4. Confirm and write to the lists

Ask the user: "Ship this to TODO? (y / edits / cancel)"

On yes, use the `/todo` skill's format (don't re-invent it):

- Each plan **step** → one line in `TODO.md` under `<!-- OPEN -->`, format `- [ ] [Priority] <step summary> *(YYYY-MM-DD)*`.
- Anything flagged as "needs review" or "existing code may be wrong" → line in `PROBLEMS.md` instead.
- Ask the user for an overall priority for the feature (High/Medium/Low). Apply it to all steps unless the user wants per-step priorities.
- Prefix each step's description with the feature name so they group visually, e.g. `Catalog browser: build scroll list UI`.

Confirm back with the count of lines added and the feature name.

## When to start a branch

If the planned feature is a **major feature** (new system, screen, user-visible capability), remind the user that per the `/commit` skill rules, work should start on a new branch `feat/<short-name>`. Don't create the branch unprompted — mention it as the next step.

## Don'ts

- Don't write code during planning.
- Don't skip the questions even if the idea seems clear — one confirm round is cheap and catches misreads.
- Don't produce a 20-step plan. If it's that big, the MVP is hiding inside — ask what the MVP is and plan that.
- Don't file planning artifacts anywhere other than `TODO.md` / `PROBLEMS.md` — no separate `plans/` directory.
