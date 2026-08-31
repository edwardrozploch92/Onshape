"""Command-line entry point for the FeatureScript AI assistant."""

import sys

import click

from .ai_assistant import FeatureScriptAssistant
from .config import load_anthropic_config, load_onshape_config
from .onshape_client import OnshapeApiError, OnshapeClient


def _read_file_or_stdin(path: str | None) -> str:
    if path is None or path == "-":
        return sys.stdin.read()
    with open(path, "r", encoding="utf-8") as f:
        return f.read()


def _write_output(text: str, out: str | None) -> None:
    if out:
        with open(out, "w", encoding="utf-8") as f:
            f.write(text)
        click.echo(f"Wrote {out}")
    else:
        click.echo(text)


@click.group()
def cli() -> None:
    """FeatureScript AI: generate, explain, debug, and push Onshape FeatureScript."""


@cli.command()
@click.argument("description")
@click.option("-o", "--out", help="File to write the generated FeatureScript to.")
def generate(description: str, out: str | None) -> None:
    """Generate a FeatureScript feature from a natural-language DESCRIPTION."""
    assistant = FeatureScriptAssistant(load_anthropic_config())
    _write_output(assistant.generate(description), out)


@cli.command()
@click.argument("file", required=False, default=None)
def explain(file: str | None) -> None:
    """Explain FeatureScript code read from FILE (or stdin)."""
    code = _read_file_or_stdin(file)
    assistant = FeatureScriptAssistant(load_anthropic_config())
    click.echo(assistant.explain(code))


@cli.command()
@click.argument("file", required=False, default=None)
@click.option("--error", "error_message", required=True, help="The error message Onshape reported.")
@click.option("-o", "--out", help="File to write the fixed FeatureScript to.")
def debug(file: str | None, error_message: str, out: str | None) -> None:
    """Diagnose and fix FeatureScript code read from FILE (or stdin)."""
    code = _read_file_or_stdin(file)
    assistant = FeatureScriptAssistant(load_anthropic_config())
    _write_output(assistant.debug(code, error_message), out)


@cli.command()
@click.option("--document-id", required=True, help="Onshape document ID.")
@click.option("--workspace-id", required=True, help="Onshape workspace ID.")
@click.option("--element-id", required=True, help="Feature Studio element ID.")
def pull(document_id: str, workspace_id: str, element_id: str) -> None:
    """Fetch a Feature Studio's current FeatureScript source."""
    client = OnshapeClient(load_onshape_config())
    try:
        click.echo(client.get_feature_studio_contents(document_id, workspace_id, element_id))
    except OnshapeApiError as e:
        raise click.ClickException(str(e))


@cli.command()
@click.argument("file")
@click.option("--document-id", required=True, help="Onshape document ID.")
@click.option("--workspace-id", required=True, help="Onshape workspace ID.")
@click.option("--element-id", required=True, help="Feature Studio element ID.")
def push(file: str, document_id: str, workspace_id: str, element_id: str) -> None:
    """Push local FeatureScript FILE into an Onshape Feature Studio."""
    contents = _read_file_or_stdin(file)
    client = OnshapeClient(load_onshape_config())
    try:
        client.update_feature_studio_contents(document_id, workspace_id, element_id, contents)
    except OnshapeApiError as e:
        raise click.ClickException(str(e))
    click.echo("Pushed.")


if __name__ == "__main__":
    cli()
