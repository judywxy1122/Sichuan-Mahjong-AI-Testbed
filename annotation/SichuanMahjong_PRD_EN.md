# Sichuan Mahjong (SichuanMahjong) Product Requirements Document (PRD)

> Version: v1.1 (2026-07-10). v1.1: corrected the actual implementation semantics of Seven Pairs / Dragon Seven Pairs against the code (§2.1, §6, Appendix A); clarified the documentation hierarchy.
> Code baseline: `SichuanMahjong/UnityVersion` (Unity 2022.3 presentation layer + pure C# rules engine, line-by-line port of the Java original)
> Documentation hierarchy: this PRD covers the feature level only (what/why); function-level interface contracts (signature/params/returns/errors/side effects) are in `SichuanMahjong_Module_Requirements_EN.md`
> Sync status: this English PRD is not synchronized with the latest Chinese `SichuanMahjong_PRD_v2.md` yet.
> Positioning: a single-player Sichuan Mahjong game (1 human + 3 probabilistic AIs), and more importantly a **decision-data production environment for the AI mini-game Benchmark** — it already ships with LLM piloting (LlmPlayController), legal-action validation (PlayerActionContext), and headless batch play (ConsoleHarness), making it the most benchmark-ready of the two candidate games.

---

## 1. Product Overview

### 1.1 One-sentence description
Four-player Sichuan Mahjong (Bamboo/Character/Dot, 108 tiles, no honor tiles). The human sits at the bottom seat; the other three seats are driven by a probabilistic AI (an 87MB lookup-table probability model). The human seat can switch among three control modes: **Manual / Auto Play (probabilistic AI pilots) / LLM Play (large-model pilots)** — all three share the same rules engine and differ only in where decisions come from.

### 1.2 Architectural Layers (product-level commitments)
| Layer | Contents | Key constraint |
|---|---|---|
| Core rules engine | Tiles, game state machine, Hu/Pung/Kong validation, probabilistic AI, Auto/LLM controllers | **Zero Unity dependencies** (asmdef `noEngineReferences:true`); compiles standalone on .NET 8 |
| UnityApp presentation layer | Code-generated uGUI, 1920×960 logical coordinates, keyboard/mouse interaction, reward animation | Rendering and input only; contains no rules |
| ConsoleHarness | Headless .NET console: `tests` / `simulate N` / `parity hands.txt` | Not part of the Unity build; verified 300/300 parity with the Java version and 100 games with 0 errors |

### 1.3 Target Users & Usage Scenarios
1. **Players**: casual single-player games (click to discard; hotkeys H/C/P/K/S for Hu/Chow/Pung/Kong/Skip).
2. **AI researchers / Benchmark data engineers (the core role)**:
   - use LLM Play to collect "decision point → LLM choice + reason + legality" traces;
   - use ConsoleHarness for batch self-play to produce baseline data;
   - use Auto Play (probabilistic AI) as a stable baseline for comparison.

---

## 2. Game Rules Specification (actual code behavior is authoritative)

### 2.1 Implemented Rules
- Tile set: Bamboo (B) / Character (C) / Dot (D), each 1–9 × 4 = **108 tiles**, no wind/dragon/flower tiles.
- Dealing: 13 tiles per player; a random dealer gets 1 extra tile and moves first; 55 tiles remain in the wall.
- Winning hands: **standard win (4 sets + 1 pair)**, **Seven Pairs** (exactly 7 distinct pair values in the concealed hand); Pure Seven Pairs is recognized but adds no fan. ⚠️ The code's "Dragon Seven Pairs" branch is gated on "≥3 declared Kongs", mutually exclusive with the Seven-Pairs condition and therefore **unreachable**; moreover a Seven-Pairs-style hand containing a concealed quad (5 pairs + 1 quad) is **not recognized as a win** — a defect preserved from the port; details in module doc M2 and §6.
- **"Missing one suit" hard constraint**: a hand may only win with ≤2 suits (the Sichuan "que yi men" rule exists as a validation check; there is no player-declared void-suit phase).
- Pung / exposed Kong / added Kong / concealed Kong; after a Kong, draw a replacement tile and continue discarding (no kong-related scoring).
- Game end: any player wins (**first win ends the game**) / wall exhausted → draw / abnormal termination.
- Response arbitration: after a discard, poll the three other seats **in seat order starting from the next player**; within each seat, priority is Hu > Pung/Kong > Skip; **the first seat with an action cuts off the remaining seats** (note: this is not global "win priority" — if a nearer seat can Pung and a farther seat can Hu, the Pung intercepts the win. This is a rule simplification preserved from the port; the benchmark semantics must take note of it).

### 2.2 Not Implemented (roadmap-marked as Coming)
- **Blood Battle to the End** (play continues after a win), **scoreboard / fan-based scoring** (self-draw, kong-replacement win, roots, pure-suit, etc. — no fan types at all), three-tile exchange, player-declared void suit.
- The current victory reward is a novelty easter egg (randomly opens a TikTok reward video + a dollar-bill animation).

### 2.3 Three Control Modes (GAME_MODES)
| Mode | Decision source | Trigger | Notes |
|---|---|---|---|
| Player | Human clicks / hotkeys | Default | The UI shows only the currently legal action buttons |
| Auto Play | `ProbabilityAI` (local lookup tables) | Button toggle; executes with a 3-second delay | Stable baseline; no network required |
| LLM Play | Large model (HTTP chat completions) | Button toggle; 3-second delay; background thread | The LLM may only choose from the engine-provided legal action set; illegal output / timeout / missing key always falls back to Auto Play |

---

## 3. Functional Requirements

| ID | Feature | Status | Priority |
|---|---|---|---|
| F1 | Full single-player game (deal / draw-discard / Pung-Kong-Hu / draw) | Exists | P0 |
| F2 | Mutually exclusive control-mode switching (Player/Auto/LLM) | Exists | P0 |
| F3 | Dynamic legal-action buttons + hotkeys H/C/P/K/S | Exists | P0 |
| F4 | LLM decision log `llm_play_log.txt` (sectioned text) | Exists | P0 |
| F5 | ConsoleHarness: tests / simulate N / parity | Exists | P0 |
| F6 | Winning-hand display (WinningHandDialog) + reward animation | Exists | P2 |
| F7 | **Random seed / wall injection interface** (reproducible games) | Missing | **P0 (curation prerequisite)** |
| F8 | **JSONL structured decision log** (with game_id/turn_id/seat/outcome backfill) | Missing | **P0 (curation prerequisite)** |
| F9 | ConsoleHarness support for any `IPlayController` (incl. LLM) in batch play | Missing (tiny change) | P1 |
| F10 | Scoring / fan-type module (self-draw / kong-replacement / roots / pure-suit / seven-pairs fan) | Missing | P1 (gives the benchmark a dense reward signal) |
| F11 | Blood Battle to the End mode | Missing | P2 |
| F12 | Merge the three duplicate classes AI1/AI2/AI3 into one parameterized class | Tech debt | P2 |

### 3.1 LLM Play Interface Specification (current state; serves as the external contract)

**Environment variables**: `GPT_API_SG_KEY` (required), `MAHJONG_LLM_MODEL` (default `gpt-5.5-2026-04-24`), `MAHJONG_LLM_ENDPOINT`, `MAHJONG_LLM_API_VERSION` (default `2025-01-01-preview`), `MAHJONG_LLM_REASONING` (default medium), `MAHJONG_LLM_TIMEOUT_SECONDS` (default 18).

**State JSON fields sent to the LLM** (partially observable, matching real mahjong information sets):
```json
{
  "player": "YOU", "last_action": "...", "status": "...",
  "last_played_tile": "C7",
  "legal_actions": ["hu", "pung", "skip"],
  "hand": ["B1","B2","C7","..."], "new_tile": "D5",
  "your_discards": ["..."],
  "melds": [{"type":"PUNG","tiles":["C3","C3","C3"]}]
}
```

**The LLM must return**: `{"action":"discard","tile":"C7","reason":"..."}`; action ∈ {hu, chow, pung, kong, skip, discard}; parsing accepts Chinese/English synonyms (win/胡, peng/碰, gang/杠, pass/过, …).

**Safety net**: the engine validates with `PlayerActionContext.IsLegal`; anything illegal triggers a FALLBACK to Auto Play — **the LLM can never execute an illegal action**.

### 3.2 Tile Encoding Specification (three coexisting encodings; documentation must state them explicitly)
| Context | Encoding | Example |
|---|---|---|
| Domain objects | `Tile{type∈{B,C,D}, number∈1..9}` | — |
| Transport / logs / LLM | `"<B|C|D><1-9>"`, B=Bamboo, C=Character, D=Dot | `C7` = 7 of Characters |
| Algorithms / probability tables | Integers 1..27 (Characters 1-9, Dots 10-18, Bamboo 19-27) | 7 = 7 of Characters |

---

## 4. Non-Functional Requirements

| Item | Requirement |
|---|---|
| Rules-engine trustworthiness | Any rule change must pass ConsoleHarness `tests` and `parity` (300/300 agreement with the Java reference is the baseline) |
| Startup performance | Probability tables 87.4MB / 810K lines, loaded asynchronously in the background (a few seconds in Unity); headless tools load in the same jian→feng→normal order |
| LLM reliability | Timeouts/exceptions/illegal output must fall back and never block the game main loop (LLM calls run on a background thread, posted back via a main-thread queue; actionVersion + hand hash guard against stale decisions) |
| Cross-platform | Core and ConsoleHarness run on Linux without Unity (verified) |

---

## 5. AI Benchmark Data Curation Requirements (the focus of this PRD)

### 5.1 Current State: existing data capabilities

| Capability | Location | Output |
|---|---|---|
| Decision-point 5-tuples | `LlmPlayController` → `llm_play_log.txt` | (legal action set, LLM choice, reason, legal/fallback, latency ms) — the reason field works directly as a CoT label; FALLBACK rate = the model's illegal-output rate |
| Legal-action ground-truth source | `PlayerActionContext.LegalActionLabels()/IsLegal()` | Action-space annotation for every decision point; **must be reused, never re-implemented** |
| Batch self-play | `ConsoleHarness simulate N` | Win/draw/error statistics over N games + wins per seat |
| Engine consistency verification | `ConsoleHarness parity` | Line-by-line diff against the reference implementation (discards / pung-kong votes / status bits) |
| State snapshots | `Game.GetGameState()` + `BuildRequestJson` | LLM-perspective observation (partially observable) |

**Existing log format** (`llm_play_log.txt`, append-only sectioned text): each section is `=== <ISO time> <SECTION> ===`, SECTION ∈ {NEW GAME, REQUEST, RESPONSE, ACCEPTED, FALLBACK}; REQUEST contains legal_actions + the full state JSON, RESPONSE contains elapsed_ms + the raw output.

### 5.2 Gaps & New Requirements

1. **F7 Reproducibility**: `Game` uses an unseeded `System.Random`, so games cannot be reproduced. Requirement: the constructor must accept an injected `seed` or a full wall sequence; the log must record the seed. This is a hard prerequisite for curation.
2. **F8 Structured logging**: the current text log has no game_id / turn_id / seat / final-outcome backfill, making per-game aggregation hard. Requirement — add JSONL:

```json
{"t":"decision","game_id":"g-0001","turn":42,"seat":0,"controller":"llm",
 "state":{...BuildRequestJson verbatim...},"legal_actions":["hu","skip"],
 "chosen":{"action":"hu","tile":null,"reason":"..."},"legal":true,
 "fallback":null,"elapsed_ms":812}
{"t":"result","game_id":"g-0001","winner_seat":0,"winning_tile":"D5",
 "end":"win|draw|error","rounds":63,"seed":12345}
```

3. **F9 Headless LLM batch collection**: `simulate` currently pins the human seat to `AutoPlayController`; the `IPlayController` abstraction is already in place, so adding a `--controller llm` parameter enables headless batch collection of LLM traces (no Unity needed).
4. **F10 Scoring signal**: currently only sparse win/lose/draw outcomes exist; adding fan-type scoring gives the benchmark a fine-grained reward (fan count) for policy-quality evaluation.

### 5.3 Derivable Benchmark Task Set

| Task | Input | Output | Evaluation metrics |
|---|---|---|---|
| T1 Legal action selection | state JSON + legal_actions | One action | Legality rate, agreement with ProbabilityAI/search baselines, final win rate |
| T2 Discard decision | Hand + table state | discard:X | Agreement with the probabilistic AI's optimal discard (parity mode is ready-made), tenpai-progress improvement |
| T3 Win/tenpai detection | 14-tile hand (text codes) | Can it win / which tiles complete it | Exact match (HuFitter/HuUtil can synthesize unlimited labels) |
| T4 Full-game play | Complete games | Win rate / draw rate | Batch statistics via simulate, vs. the three probabilistic AIs |
| T5 Decision explanation | Decision point | reason text | Human eval / LLM eval (the existing reason field as reference) |

### 5.4 Curation Pipeline (target form)

```
seed pool ──▶ ConsoleHarness simulate --controller {auto|llm} --seed s --jsonl out/
                    │
                    ├─▶ decision event stream (per decision point: state + action space + choice + reason + legality)
                    ├─▶ result events (outcome backfilled → joined onto each decision by game_id)
                    └─▶ statistics report (legality rate / win rate / average game length / fallback rate)
```

Suggested data organization: `data/curation/mahjong/{run_id}/games.jsonl + decisions.jsonl + meta.json` (meta records controller, model, seed range, engine version, probability-table hash).

---

## 6. Known Issues & Risks

1. **No random-seed injection** (F7) — the showstopper for curation; fix first.
2. Response priority is "nearest seat first," not "global win priority" (§2.1); as a benchmark environment this rule must be explicitly declared in the data card, or a switch added for global priority.
2a. **Seven Pairs with a concealed quad cannot win / the Dragon-Seven-Pairs branch is unreachable** (§2.1): fixing this per real Sichuan rules (a quad counts as two pairs) is a rule change — it must pass `tests`+`parity` and be version-declared in the data card; if kept as-is, hands of this class must be explicitly excluded from label generation in benchmark task T3 to avoid ambiguity.
3. `ProbabilityAI.ShouldChow` is always true and the CHOW state is practically unreachable (Sichuan Mahjong has no Chow); `chow` still appears in the action enum — recommended to remove it from legal_actions generation to avoid confusing models.
4. The default LLM endpoint is a per-OS hard-coded internal domain; it must be fully overridable via environment variables (already supported) and documented.
5. The `AI1/AI2/AI3` classes are duplicated code; `Game.cs` (586 lines) mixes three responsibilities — state machine / text generation / snapshots (splitting suggestions in the module document).
6. The victory easter egg opens an external TikTok link; a configurable off switch is recommended for release/evaluation environments.

---

## 7. Suggested Milestones

| Milestone | Content |
|---|---|
| M1 | F7 seed injection + F8 JSONL decision log + F9 headless LLM simulate (minimum viable curation) |
| M2 | F10 fan-scoring module; produce the first decisions/games dataset and statistics report |
| M3 | Rule extensions — Blood Battle to the End / three-tile exchange / declared void suit (closer alignment with real Sichuan Mahjong) |

---

## Appendix A: Key Facts Quick Reference
- Wall of 108 tiles; deal 4×13 + 1 extra for the dealer; 55 remain drawable
- Win types: standard 4+1 / Seven Pairs (7 distinct pair values; a concealed quad is not recognized, the Dragon-Seven-Pairs branch is unreachable — see §2.1); "missing one suit" (suits ≤ 2) hard check (`PlayerStatusChecker.cs:115-131`)
- First win ends the game (`Game.cs:245`); no scoring / fan types / Blood Battle (LinuxVersion README marks them Coming)
- Probability tables: `majiang_ai_normal.txt` 87.4MB / 810,700 lines + the small feng/jian tables; line format `key jiang p <comment>`
- LLM decision log: `llm_play_log.txt`, sections NEW GAME/REQUEST/RESPONSE/ACCEPTED/FALLBACK (`LlmPlayController.cs:465-480`)
- Headless verification: `dotnet run -- <table dir> tests|simulate N|parity hands.txt` (verified 300/300 parity, 100 games with 0 errors)
- Human seat has a 3-second decision delay (`GamePanelBehaviour` PlayDelaySeconds=3f); LLM runs on a background thread to avoid blocking
