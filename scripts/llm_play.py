#!/usr/bin/env python3
"""LLM bridge for the Java Mahjong UI.

Reads a compact game-state JSON object from stdin and returns one JSON decision
to stdout. Java validates the returned action before applying it.
"""

from __future__ import annotations

import asyncio
import json
import os
import platform
import sys


MODEL = os.environ.get("MAHJONG_LLM_MODEL", "gpt-5.5-2026-04-24")
API_VERSION = os.environ.get("MAHJONG_LLM_API_VERSION", "2025-01-01-preview")
REASONING_EFFORT = os.environ.get("MAHJONG_LLM_REASONING", "medium")
TIMEOUT_SECONDS = float(os.environ.get("MAHJONG_LLM_TIMEOUT_SECONDS", "18"))


def default_endpoint() -> str:
    if platform.system().lower() == "darwin":
        return "https://aidp-i18ntt-sg.tiktok-row.net/api/modelhub/online/multimodal/crawl"
    return "https://aidp-i18ntt-sg.byteintl.net/api/modelhub/online/multimodal/crawl"


def build_messages(state: dict) -> list[dict[str, str]]:
    legal_actions = state.get("legal_actions", [])
    legal_actions_text = ", ".join(legal_actions)
    system = (
        "You are playing Sichuan Mahjong for the human seat. "
        "Choose exactly one legal action from the provided legal_actions list. "
        "Do not choose any action that is not literally allowed by legal_actions. "
        "Return only JSON with fields: action, tile, reason. "
        "Allowed action values: hu, chow, pung, kong, skip, discard. "
        "If legal_actions contains discard:X, output action=discard and tile=X. "
        "For discard, tile must match one of the legal discard tile codes exactly. "
        "For all other actions, tile must be null. "
        "If legal_actions does not contain any discard:X item, do not discard. "
        "Prefer winning immediately. Prefer legal claims that improve the hand. "
        "If a kong is optional, take it only when it does not obviously damage the hand."
    )
    user = {
        "task": "Choose the next action for the human player.",
        "legal_actions": legal_actions,
        "legal_actions_text": legal_actions_text,
        "state": state,
        "output_examples": [
            {"action": "hu", "tile": None, "reason": "Winning is available."},
            {"action": "discard", "tile": "C7", "reason": "Discard an isolated tile."},
            {"action": "skip", "tile": None, "reason": "Claim is not worth taking."},
        ],
    }
    return [
        {"role": "system", "content": system},
        {"role": "user", "content": json.dumps(user, ensure_ascii=False)},
    ]


async def main() -> int:
    try:
        state = json.loads(sys.stdin.read())
    except json.JSONDecodeError as exc:
        print(json.dumps({"action": "fallback", "tile": None, "reason": f"bad input json: {exc}"}))
        return 2

    api_key = os.environ.get("GPT_API_SG_KEY", "")
    if not api_key:
        print(json.dumps({"action": "fallback", "tile": None, "reason": "GPT_API_SG_KEY is not set"}))
        return 3

    try:
        from openai import AsyncAzureOpenAI
    except ImportError as exc:
        print(json.dumps({"action": "fallback", "tile": None, "reason": f"openai package unavailable: {exc}"}))
        return 4

    client = AsyncAzureOpenAI(
        api_key=api_key,
        azure_endpoint=os.environ.get("MAHJONG_LLM_ENDPOINT", default_endpoint()),
        api_version=API_VERSION,
        timeout=TIMEOUT_SECONDS,
        max_retries=1,
    )

    response = await client.chat.completions.create(
        model=MODEL,
        messages=build_messages(state),
        temperature=1.0,
        top_p=1.0,
        max_tokens=800,
        response_format={"type": "json_object"},
        reasoning_effort=REASONING_EFFORT,
    )
    content = response.choices[0].message.content or "{}"
    parsed = json.loads(content)
    print(json.dumps(parsed, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(asyncio.run(main()))
    except Exception as exc:
        print(json.dumps({"action": "fallback", "tile": None, "reason": str(exc)}, ensure_ascii=False))
        raise SystemExit(5)
