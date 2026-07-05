#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

LLM_VENV="${LLM_VENV:-/Users/bytedance/work/movie_making/micro_drama_draft/.venv}"
LLM_ENV_FILE="${LLM_ENV_FILE:-/Users/bytedance/work/movie_making/multi_agents/.env}"

if [[ ! -f "$LLM_VENV/bin/activate" ]]; then
  echo "LLM venv not found: $LLM_VENV" >&2
  echo "Override with: LLM_VENV=/path/to/.venv $0" >&2
  exit 1
fi

if [[ ! -f "$LLM_ENV_FILE" ]]; then
  echo "LLM env file not found: $LLM_ENV_FILE" >&2
  echo "Override with: LLM_ENV_FILE=/path/to/.env $0" >&2
  exit 1
fi

cd "$REPO_ROOT"
rm -f "$REPO_ROOT/llm_play_log.txt"

# Load the Python environment that contains the OpenAI SDK.
source "$LLM_VENV/bin/activate"
export MAHJONG_LLM_PYTHON="$(which python)"
export MAHJONG_LLM_TIMEOUT_SECONDS="${MAHJONG_LLM_TIMEOUT_SECONDS:-18}"

# Export variables from the existing .env file without copying secrets here.
set -a
source "$LLM_ENV_FILE"
set +a

if [[ -z "${GPT_API_SG_KEY:-}" ]]; then
  echo "GPT_API_SG_KEY is not set after sourcing: $LLM_ENV_FILE" >&2
  exit 1
fi

"$MAHJONG_LLM_PYTHON" - <<'PY'
import openai
print(f"Using OpenAI SDK {openai.__version__}")
PY

echo "Using MAHJONG_LLM_PYTHON=$MAHJONG_LLM_PYTHON"
echo "Using MAHJONG_LLM_TIMEOUT_SECONDS=$MAHJONG_LLM_TIMEOUT_SECONDS"
echo "Starting Sichuan Mahjong with LLM Play enabled by environment..."

mvn compile exec:java
