#!/usr/bin/env bash
set -euo pipefail

ENV_FILE="${MAHJONG_LLM_ENV_FILE:-/Users/bytedance/work/movie_making/multi_agents/.env}"
UNITY_APP="${MAHJONG_UNITY_APP:-Unity Hub}"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Missing env file: $ENV_FILE" >&2
  echo "Set MAHJONG_LLM_ENV_FILE=/path/to/.env if your key file is elsewhere." >&2
  exit 1
fi

set -a
# shellcheck disable=SC1090
source "$ENV_FILE"
set +a

if [[ -z "${GPT_API_SG_KEY:-}" ]]; then
  echo "GPT_API_SG_KEY is not set after sourcing $ENV_FILE" >&2
  exit 1
fi

launchctl setenv GPT_API_SG_KEY "$GPT_API_SG_KEY"

for name in \
  MAHJONG_LLM_MODEL \
  MAHJONG_LLM_ENDPOINT \
  MAHJONG_LLM_API_VERSION \
  MAHJONG_LLM_REASONING \
  MAHJONG_LLM_TIMEOUT_SECONDS
do
  value="${!name:-}"
  if [[ -n "$value" ]]; then
    launchctl setenv "$name" "$value"
  fi
done

echo "LLM env exported to macOS GUI session."
echo "If Unity or Unity Hub is already open, quit it fully and reopen it."
open -a "$UNITY_APP"
