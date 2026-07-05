# Game Modes

This game currently has three ways to control the human seat: manual player mode,
Auto Play, and LLM Play. The three modes all use the same Mahjong rule engine.
Only the decision source changes.

## 1. Player Mode

Player mode is the default mode when a new game starts.

In this mode, you control the bottom seat labeled `YOU`.
The game engine deals tiles, advances AI turns, checks legal responses, and waits
for your input whenever the human seat needs to act.

Typical actions:

- Click a tile in your hand to discard it when it is your turn.
- Click `Hu`, `Chow`, `Pung`, `Kong`, or `Skip` when those buttons appear.
- Keyboard shortcuts still work for the same actions:
  `H` for Hu, `C` for Chow, `P` for Pung, `K` for Kong, and `S` for Skip.

The UI only shows action buttons that are legal in the current state. For example,
if another player discards a tile and you can only Pung or Skip, only those choices
should be offered.

When a new game starts, the game returns to Player mode. Both `Auto Play` and
`LLM Play` are off and selectable again.

## 2. Auto Play

Auto Play lets the existing probability-based AI control the human seat.

This is the baseline AI mode. It does not call an LLM. It uses the same local
statistics/probability logic used by the original AI players, through
`ProbabilityAI`.

How it works:

- Click `Auto Play` to turn it on.
- The button becomes green and shows `Auto Play: ON`.
- `LLM Play` becomes disabled and gray while Auto Play is on.
- When the human seat needs to act, the status bar shows that Auto Play will act
  in 3 seconds.
- After the delay, Auto Play chooses one legal action and the game applies it.
- Click `Auto Play: ON` again to turn it off.

Decision behavior:

- If Hu is available, Auto Play takes the win.
- If responding to another player's discard, it uses `ProbabilityAI` to decide
  whether to Chow, Pung, Kong, or Skip.
- If a self Kong is available, it decides whether to Kong or Skip Kong.
- If it needs to discard, it asks `ProbabilityAI` for a discard tile.
- If the probability AI cannot provide a valid discard, the game falls back to a
  simple legal discard.

Auto Play is useful as a stable baseline because it is local, fast, deterministic
enough for gameplay, and does not require API credentials.

## 3. LLM Play

LLM Play lets `gpt-5.5-2026-04-24` control the human seat.

This mode is experimental. It is designed to make the human seat behave more like
a reasoning player while still keeping the Java game engine in charge of rules.

How it works:

- Click `LLM Play` to turn it on.
- The button becomes green and shows `LLM Play: ON`.
- `Auto Play` becomes disabled and gray while LLM Play is on.
- When the human seat needs to act, the status bar shows that LLM Play will act
  in 3 seconds.
- After the delay, the UI shows that LLM Play is thinking.
- Java sends a compact JSON game state to `scripts/llm_play.py`.
- The Python bridge calls `gpt-5.5-2026-04-24`.
- The LLM returns one JSON decision.
- Java validates the decision before applying it.
- Click `LLM Play: ON` again to turn it off.

The LLM is not allowed to directly mutate the game state. It only chooses from a
legal action list produced by Java. This is important: even if the LLM returns a
bad answer, the game should not play an illegal move.

The LLM decision format is:

```json
{
  "action": "discard",
  "tile": "C7",
  "reason": "Discard an isolated tile and keep stronger shapes."
}
```

Supported actions:

- `hu`
- `chow`
- `pung`
- `kong`
- `skip`
- `discard`

For `discard`, `tile` must be one of the legal tile codes in the current hand.
For all other actions, `tile` should be `null`.

### API Setup

LLM Play uses the Python bridge at:

```text
scripts/llm_play.py
```

The bridge uses an Azure/OpenAI-compatible internal endpoint and expects:

```bash
export GPT_API_SG_KEY="..."
```

It also needs a Python interpreter with the `openai` package installed. By
default, Java runs:

```bash
python3 scripts/llm_play.py
```

If your `openai` package is installed in a virtual environment, point the game to
that interpreter:

```bash
export MAHJONG_LLM_PYTHON="/path/to/venv/bin/python"
```

Default model:

```text
gpt-5.5-2026-04-24
```

Default macOS endpoint:

```text
https://aidp-i18ntt-sg.tiktok-row.net/api/modelhub/online/multimodal/crawl
```

If `GPT_API_SG_KEY` is missing, the Python package is unavailable, the API times
out, or the LLM returns an illegal action, LLM Play falls back to Auto Play.

The fallback is intentional. It keeps the game playable even when the API is not
available.

## Mode Interaction

Only one assisted mode can be active at a time.

- If Auto Play is on, LLM Play is disabled and shown in gray.
- If LLM Play is on, Auto Play is disabled and shown in gray.
- If both are off, the game is in Player mode and both buttons are selectable.
- Starting a new game resets both assisted modes to off.

This gives three clear states:

| Mode | Human seat controlled by | Requires API key | Other assisted button |
| --- | --- | --- | --- |
| Player mode | You | No | Both selectable |
| Auto Play | Probability AI | No | LLM Play disabled |
| LLM Play | GPT decision + Java validation | Yes | Auto Play disabled |

## Design Principle

The rule engine owns legality. The controller only chooses.

That means every mode goes through the same game-state transitions:

1. Java determines the current legal actions.
2. The active controller chooses one action.
3. Java validates the chosen action.
4. Java applies the action and advances the game.

This structure keeps Player mode, Auto Play, and LLM Play comparable. It also
makes LLM Play safer: the LLM can reason, explain, and choose, but it cannot
invent illegal Mahjong moves.
