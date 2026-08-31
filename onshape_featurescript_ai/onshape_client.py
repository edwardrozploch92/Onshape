"""Minimal Onshape REST API client, scoped to what the FeatureScript AI
assistant needs: reading and writing a Feature Studio's contents.
"""

import json
from typing import Any, Dict, Optional

import requests

from .config import OnshapeConfig
from .onshape_auth import build_auth_headers


class OnshapeApiError(RuntimeError):
    def __init__(self, status_code: int, body: str):
        super().__init__(f"Onshape API returned {status_code}: {body}")
        self.status_code = status_code
        self.body = body


class OnshapeClient:
    def __init__(self, config: OnshapeConfig):
        self._config = config

    def _request(self, method: str, path: str, body: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        url = self._config.base_url.rstrip("/") + path
        payload = json.dumps(body) if body is not None else ""
        headers = build_auth_headers(
            method=method,
            url=url,
            access_key=self._config.access_key,
            secret_key=self._config.secret_key,
        )
        response = requests.request(method, url, headers=headers, data=payload, timeout=30)
        if not response.ok:
            raise OnshapeApiError(response.status_code, response.text)
        if not response.content:
            return {}
        return response.json()

    def get_feature_studio_contents(self, document_id: str, workspace_id: str, element_id: str) -> str:
        """Return the current FeatureScript source of a Feature Studio."""
        path = f"/featurestudios/d/{document_id}/w/{workspace_id}/e/{element_id}"
        result = self._request("GET", path)
        return result.get("contents", "")

    def update_feature_studio_contents(
        self, document_id: str, workspace_id: str, element_id: str, contents: str
    ) -> Dict[str, Any]:
        """Overwrite a Feature Studio's FeatureScript source.

        Note: Onshape's exact update endpoint/body shape has shifted across
        API versions; verify this against the live API explorer for your
        account (Onshape Developer Portal -> API Explorer) before relying on
        it, and adjust the path below if it has changed.
        """
        path = f"/featurestudios/d/{document_id}/w/{workspace_id}/e/{element_id}/updatefeaturestudio"
        return self._request("POST", path, {"contents": contents})

    def get_document_info(self, document_id: str) -> Dict[str, Any]:
        return self._request("GET", f"/documents/{document_id}")
