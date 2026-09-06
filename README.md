# onshape-featurescript-ai

A small CLI that pairs the Onshape REST API with an LLM (Claude) to generate,
explain, and debug [FeatureScript](https://cad.onshape.com/FsDoc/) — Onshape's
parametric CAD scripting language — and optionally push results straight into
a Feature Studio.

This is a standalone tool, not an Onshape product feature: it does not
"enable" anything inside the Onshape app itself. Onshape's own Labs / AI
FeatureScript features (if enabled on your account) are toggled from your
Onshape account settings, not from here.

## Status: fallback tool

Onshape now hosts its own official remote MCP server for FeatureScript AI
(`https://fs-mcp.labs.onshape.app/mcp`), connected via Settings → Connectors
in Claude. That server is the primary way to work with FeatureScript AI —
it authenticates via OAuth (no local API keys to manage) and is maintained
directly by Onshape.

This CLI is kept in the repo as a **fallback**: for offline use, scripting
outside an MCP-capable client, or if the hosted server is ever unavailable.
It requires you to manage your own Onshape API keys and Anthropic API key
locally (see Setup below).

## StackTech drawer separators

`stacktech/` holds a standalone FeatureScript feature plus the research behind it:

- `stacktech/stacktech_separators.fs` – the **StackTech Separators** custom
  feature. Pick the ToughBuilt StackTech drawer, choose which side-wall
  receiver the full-width side-to-side bar sits in, and optionally add
  front-to-back bars that T into the front/back wall mount and drop into
  T-slots in the side-to-side bar. Bars are modelled like the OEM parts: a
  0.265 in frame with a 0.095 in recessed web, T ends on the side-to-side bar
  sized to the 0.29 in / 0.166 in edge mount, and the OEM ridge-and-rib hook
  (0.285 in bump-out, 0.21 in end section, 0.1525 in ridge-to-rib) on the
  front-to-back bars' wall ends; all of those are defaults in the collapsed
  "Separator geometry" group. Every drawer's interior size and receiver
  count is a single hard-coded table (`drawerSpec()`) at the top of the file.
- `stacktech/STACKTECH_DATA.md` – interior dimensions, receiver counts and
  existing CAD/STL links for every StackTech drawer and box, with sources and
  confidence tags.

To use it: create a Feature Studio in your Onshape document, paste the `.fs`
file in (or push it with `featurescript-ai push`), then add the feature from
the Part Studio custom-feature toolbar. Tested against FeatureScript std 3070.

## Setup

1. Install dependencies:
   ```
   pip install -e .
   ```
2. Get Onshape API keys at https://dev-portal.onshape.com/keys (scoped to
   read/write documents as needed).
3. Get an Anthropic API key at https://console.anthropic.com/settings/keys.
4. Copy `.env.example` to `.env` and fill in all four values. `.env` is
   gitignored — never commit real keys.

## Usage

```
# Generate a new feature from a description
featurescript-ai generate "an extrude feature that pockets a hexagonal array of holes" -o hex_pockets.fs

# Explain existing FeatureScript
featurescript-ai explain path/to/feature.fs

# Debug a failing feature, given the error Onshape reported
featurescript-ai debug path/to/feature.fs --error "Query has no matching entities"

# Pull the current source of a Feature Studio
featurescript-ai pull --document-id <did> --workspace-id <wid> --element-id <eid>

# Push a local file into a Feature Studio
featurescript-ai push hex_pockets.fs --document-id <did> --workspace-id <wid> --element-id <eid>
```

Document/workspace/element IDs come from an Onshape document's URL:
`https://cad.onshape.com/documents/{did}/w/{wid}/e/{eid}`.

## Notes

- Onshape API authentication uses HMAC-SHA256-signed requests
  (`onshape_featurescript_ai/onshape_auth.py`), per Onshape's documented
  API-key scheme.
- The `push`/`pull` commands target Onshape's Feature Studio endpoints;
  verify the exact path against your account's API Explorer
  (Developer Portal → API Explorer) if Onshape has changed it, since API
  paths have shifted across versions.

## Tests

```
pip install -e ".[dev]" pytest 2>/dev/null || pip install pytest
pytest
```
