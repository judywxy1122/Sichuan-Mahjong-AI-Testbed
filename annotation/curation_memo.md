# Sichuan Mahjong Curation Memo

> Date: 2026-07-13
> Scope: lessons from evolving this project from the initial Java/Swing Sichuan Mahjong repo to the current Unity + Auto Play + LLM Play version, and a practical SOP for future outsourced curation work.

## 1. Executive Summary

This project shows a useful pattern for AI-assisted game curation:

- **Humans are strongest at product taste, creative direction, rule judgment, playtesting, and deciding what "good" feels like.**
- **AI agents are strongest at implementing well-specified changes, reading code, locating bugs, writing glue code, refactoring, and producing documentation/diffs.**
- The best workflow is not "AI builds everything alone" or "human manually codes everything", but a tight loop:

```
Human plays / observes / decides intent
        ↓
AI inspects code / proposes implementation / edits code
        ↓
Human tests the actual game and gives concrete screenshots or failures
        ↓
AI fixes, documents, and stabilizes
```

For outsourcing, the key is to make curation a repeatable process: each task should produce a playable build, screenshots, bug notes, updated docs, and a clear git diff.

## 2. Project Evolution

### 2.1 Initial State

The original project was a Java/Swing Sichuan Mahjong testbed:

- Basic Mahjong gameplay existed.
- UI was minimal and debug-like.
- Interaction was partly keyboard-driven.
- Rules and probability AI were embedded in the codebase but not fully documented.
- The project could run locally, but the user experience was rough.

At this stage, the main work was not inventing Mahjong from scratch. The real challenge was understanding the existing code, making it playable, and exposing hidden game state clearly to the player.

### 2.2 Current State

The current project has evolved into a much richer Unity version:

- Unity 2022.3 presentation layer.
- Pure C# Core rules engine.
- Manual play, Auto Play, and LLM Play modes.
- Probability AI baseline.
- LLM decision mode with fallback to Auto Play.
- Dynamic action buttons instead of keyboard-only responses.
- Winning hand dialog.
- Reward animation and TikTok reward link.
- Improved night-sky/ocean visual theme.
- Updated UI layout with AI/player avatars.
- Documentation under `annotation/`, including PRD, module specs, and version diffs.

The project is now both:

- a playable single-player Sichuan Mahjong mini-game, and
- a potential AI benchmark / decision-data production environment.

### 2.3 Future Ideal State

The ideal future state can go in several directions:

- More complete Sichuan Mahjong rules: scoring, fan types, blood battle mode, missing-suit declaration, better response priority rules.
- Better AI benchmark infrastructure: seed injection, reproducible wall generation, JSONL decision logs, result backfill, dataset cards.
- Stronger LLM/agent play: richer state summaries, better legal-action prompting, model comparison, agent memory, strategy evaluation.
- Multiplayer / human + agent rooms: real players and AI agents occupying seats in the same room, with server-authoritative rules and replay.
- Better product experience: polished visual identity, onboarding, sound, animations, mobile-friendly layout, replay/observe mode.

## 3. What Mainly Needs Humans

### 3.1 Creative Direction

Humans are best at deciding what the game should become:

- Should it feel like a traditional Mahjong table, a TikTok reward game, or an AI benchmark UI?
- Should the visual style be quiet and tool-like, or entertaining and social?
- Is the reward mechanic fun, tacky, confusing, or motivating?
- Does the UI make the player want to keep playing?

These are taste decisions. AI can propose options, but humans should choose the direction.

### 3.2 Core Product Logic

Humans should own high-level rules and product semantics:

- What does "playable" mean?
- Should Sichuan Mahjong require missing one suit?
- Should a player be allowed to skip an optional concealed Kong?
- Should win priority beat closer-seat Pung priority?
- Should LLM Play be a baseline, a novelty, or a research feature?

AI can inspect code and explain what is implemented, but the human should decide whether that behavior is acceptable.

### 3.3 Playtesting

Actual playtesting is still mainly human work:

- Notice when a state feels stuck.
- Notice when a prompt is misleading.
- Notice when the UI is ugly or visually blocked.
- Notice when a winning hand display does not match Mahjong intuition.
- Notice whether a feature is delightful or distracting.

Screenshots are especially valuable. A screenshot plus one clear sentence often gives AI enough context to fix a UI or state bug.

### 3.4 Acceptance Judgment

Humans should make the final call on:

- Is this feature good enough?
- Is the UI acceptable?
- Is the rule behavior correct enough for the intended purpose?
- Should this become a new documented version?
- Is this worth committing?

AI can test and summarize, but product acceptance should stay human-led.

## 4. What AI Can Usually Do Independently

With a clear task and access to the repo, current frontier coding agents can often independently handle:

- Reading unfamiliar codebases.
- Finding relevant files and call paths.
- Implementing UI layout changes.
- Refactoring local code without changing rules.
- Adding buttons, dialogs, logs, and config options.
- Fixing reproducible bugs.
- Adding fallback logic.
- Writing or updating docs.
- Creating version-diff markdown files.
- Searching for hard-coded secrets or unsafe config.
- Explaining how a mode works from code.
- Turning repeated manual instructions into scripts.

AI performs best when the task is concrete:

- "Move these UI regions down by the same offset."
- "Add a clickable button for actions."
- "Show the winning tile inside the winning hand dialog."
- "Write a markdown diff from v2 to v3."
- "Find why LLM Play falls back to Auto Play."

AI performs worse when the request is vague:

- "Make it fun."
- "Make the AI smart."
- "Make the UI professional."
- "Fix all Mahjong rules."

Those can still work, but they need human iteration and taste checks.

## 5. Best Human + AI Collaboration Pattern

### 5.1 The Best Loop

The most effective loop in this project was:

1. Human runs the game and observes a concrete issue.
2. Human sends a screenshot and explains the expected behavior.
3. AI reads the code path and implements a narrow fix.
4. Human tests again and reports the next issue.
5. AI patches and documents the change.

This worked well for:

- action buttons,
- status text bugs,
- Kong/Skip behavior,
- winning hand display,
- reward button,
- window sizing,
- LLM fallback,
- Unity UI layout,
- documentation versioning.

### 5.2 Human Prompting Style That Works

Good curation prompts include:

- Current screenshot.
- What is wrong.
- What should happen instead.
- Whether this is a bug, UX issue, or future idea.
- Whether to implement now or only discuss.

Example:

> "AI3 打了五万给我点炮，这张五万应该放到我的最终胡牌里。你看是不是 winning hand display 的 bug？"

This is much better than:

> "胡牌显示有问题。"

### 5.3 AI Response Pattern That Works

The AI should:

- Inspect the existing implementation first.
- Keep changes localized.
- Preserve working rules unless asked to change them.
- Avoid pretending it verified things it did not verify.
- Tell the human exactly what to test next.
- Update docs when behavior becomes part of the product.

## 6. Curation SOP for Outsourcing

If outsourcing curation work, the vendor should not simply "play randomly and report bugs." They need a structured SOP.

### 6.1 Inputs for the Outsourcer

Provide:

- Repo URL and target branch.
- Unity version.
- Run instructions.
- Current feature list.
- Known issues.
- Desired play modes to test: Manual, Auto Play, LLM Play.
- Screenshot examples of acceptable UI.
- A bug report template.
- A rule expectation sheet.

### 6.2 Setup Checklist

The outsourcer should verify:

- Project opens in Unity 2022.3.62f3.
- Game enters Play mode.
- Manual mode can complete a few turns.
- Auto Play can run at least one full game.
- LLM Play either works with configured env vars or cleanly falls back.
- No console errors during normal play.
- Window resizing does not break core layout.

### 6.3 Playtest Passes

Run separate passes:

1. **Manual Play Pass**
   - Click tiles.
   - Use Hu/Pung/Kong/Skip buttons.
   - Verify prompts match actual legal actions.

2. **Auto Play Pass**
   - Turn Auto Play on.
   - Watch at least 5 games.
   - Record stuck states, illegal wins, or strange logs.

3. **LLM Play Pass**
   - Confirm env setup.
   - Watch LLM choices and fallback reasons.
   - Save `llm_play_log.txt` when problems occur.

4. **Winning Hand Pass**
   - Inspect winning dialog for player and AI wins.
   - Check whether winning tile appears in the correct group.

5. **UI/UX Pass**
   - Check normal window size, resized window, and full-screen-like Game view.
   - Check status banner, buttons, log window, avatars, reward animation.

6. **Regression Pass**
   - Retest previously fixed bugs.
   - Verify no old stuck states returned.

### 6.4 Bug Report Template

Each issue should include:

```md
## Title

## Build / Branch

## Mode
Manual / Auto Play / LLM Play

## Steps to Reproduce
1.
2.
3.

## Expected Result

## Actual Result

## Screenshot / Video

## Logs
- Unity console:
- playing log:
- llm_play_log.txt if relevant:

## Severity
P0 blocks game / P1 wrong rule / P2 UX issue / P3 polish
```

### 6.5 Acceptance Criteria for a Curation Batch

A curation batch is complete only if:

- All reported issues are reproducible or explicitly marked non-repro.
- Screenshots/videos are attached for UI bugs.
- Logs are attached for LLM/Auto Play bugs.
- Each bug has expected vs actual behavior.
- Fixed issues are retested.
- Docs are updated if behavior or product direction changed.
- Git status is clean or all intentional changes are listed.

## 7. What to Let AI Do in the Outsourcing Workflow

AI can help the outsourcer:

- Summarize logs.
- Compare screenshots before/after.
- Turn rough bug notes into structured tickets.
- Search code for likely source files.
- Draft reproduction steps.
- Generate test matrices.
- Update markdown docs.

But AI should not be the only judge of gameplay quality. A human still needs to play and feel the game.

## 8. Recommended Division of Labor

| Work | Human | AI |
|---|---|---|
| Product direction | Owner | Suggest options |
| Rule correctness judgment | Owner | Inspect code and explain behavior |
| Playtesting | Owner | Help organize findings |
| UI taste | Owner | Implement requested layout/polish |
| Bug localization | Review | Owner |
| Code implementation | Review | Owner |
| Docs and diffs | Review | Owner |
| Regression checklist | Approve | Draft and maintain |
| Final acceptance | Owner | Provide evidence |

## 9. Practical Lessons from This Project

1. **Screenshots are a superpower.** They turn vague UI complaints into actionable implementation tasks.
2. **Playable first, polished second.** The project became valuable once the user could actually play and notice problems.
3. **Status text matters.** For turn-based games, prompts are part of the game logic from the player's perspective.
4. **Fallbacks make AI features usable.** LLM Play only became practical because illegal/timeout/no-key cases fall back to Auto Play.
5. **Docs should version alongside behavior.** Once UI or game semantics change, create v-diff docs instead of silently rewriting history.
6. **Human taste drives retention.** Reward buttons, avatars, background, and layout are not "core logic", but they affect whether users keep playing.
7. **AI is excellent at local implementation but needs clear acceptance signals.** The human should say what is good enough.

## 10. Suggested Next Curation Milestones

1. **Curation MVP**
   - Seed injection.
   - JSONL decision logs.
   - Reproducible Auto/LLM simulations.

2. **Gameplay Correctness Pass**
   - Rule review for Sichuan Mahjong edge cases.
   - Decide whether to preserve Java parity or fix known rule simplifications.

3. **User Experience Pass**
   - Avatar polish.
   - Better onboarding.
   - Stronger sound/reward feedback.
   - Responsive layout pass.

4. **Benchmark Pass**
   - Define tasks T1-T5.
   - Generate labeled datasets.
   - Compare Probability AI vs LLM Play.

5. **Future Multiplayer Prototype**
   - Room model.
   - Seat assignment.
   - Server-authoritative action validation.
   - Human + agent mixed seats.

