"""Anthropic-backed assistant for generating, explaining, and debugging
Onshape FeatureScript code."""

from anthropic import Anthropic

from .config import AnthropicConfig

_SYSTEM_PROMPT = """You are an expert Onshape FeatureScript engineer.
FeatureScript is Onshape's parametric CAD scripting language: strongly typed,
version-declared (e.g. `FeatureScript 2377;`), built around `export const
myFeature = defineFeature(...)` for custom features, and using Onshape's
built-in geometry, query, and pattern APIs (e.g. `qEverything`,
`opExtrude`, `opBoolean`, `Sketch`, `Plane`).

Rules:
- Always emit complete, valid FeatureScript, starting with a FeatureScript
  version pragma when producing a full file.
- Prefer built-in standard library functions over reimplementing geometry
  math by hand.
- When explaining or debugging, be concrete about which line or construct
  is at issue and why, referencing FeatureScript semantics (query
  transient IDs, parameter validation, unstable behavior with `ing` result
  status, etc.) rather than generic programming advice.
- If a request is ambiguous about geometry intent, state the assumption
  you're making rather than asking a clarifying question, since output
  should be directly usable code or explanation."""


class FeatureScriptAssistant:
    def __init__(self, config: AnthropicConfig):
        self._client = Anthropic(api_key=config.api_key)
        self._model = config.model

    def _complete(self, user_prompt: str, max_tokens: int = 4096) -> str:
        message = self._client.messages.create(
            model=self._model,
            max_tokens=max_tokens,
            system=_SYSTEM_PROMPT,
            messages=[{"role": "user", "content": user_prompt}],
        )
        return "".join(block.text for block in message.content if block.type == "text")

    def generate(self, description: str) -> str:
        """Generate a complete FeatureScript file from a natural-language description."""
        prompt = (
            "Write a complete FeatureScript custom feature for the following "
            f"request. Return only the FeatureScript code, no prose.\n\n{description}"
        )
        return self._complete(prompt)

    def explain(self, code: str) -> str:
        """Explain what a piece of FeatureScript code does."""
        prompt = f"Explain what this FeatureScript code does, section by section:\n\n{code}"
        return self._complete(prompt)

    def debug(self, code: str, error_message: str) -> str:
        """Diagnose a FeatureScript error and propose a fix."""
        prompt = (
            "This FeatureScript code fails with the error below. Identify the "
            "root cause and return the corrected code, followed by a brief "
            f"explanation of the fix.\n\nError:\n{error_message}\n\nCode:\n{code}"
        )
        return self._complete(prompt)
