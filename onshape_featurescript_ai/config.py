"""Environment-based configuration. Reads from a local .env file (never committed)."""

import os
from dataclasses import dataclass

from dotenv import load_dotenv

load_dotenv()


@dataclass(frozen=True)
class OnshapeConfig:
    access_key: str
    secret_key: str
    base_url: str


@dataclass(frozen=True)
class AnthropicConfig:
    api_key: str
    model: str


def _require(name: str) -> str:
    value = os.environ.get(name)
    if not value:
        raise RuntimeError(
            f"Missing required environment variable {name}. "
            f"Copy .env.example to .env and fill it in."
        )
    return value


def load_onshape_config() -> OnshapeConfig:
    return OnshapeConfig(
        access_key=_require("ONSHAPE_ACCESS_KEY"),
        secret_key=_require("ONSHAPE_SECRET_KEY"),
        base_url=os.environ.get("ONSHAPE_BASE_URL", "https://cad.onshape.com/api/v6"),
    )


def load_anthropic_config() -> AnthropicConfig:
    return AnthropicConfig(
        api_key=_require("ANTHROPIC_API_KEY"),
        model=os.environ.get("ANTHROPIC_MODEL", "claude-sonnet-5"),
    )
