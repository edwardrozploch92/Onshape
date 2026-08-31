"""Request signing for Onshape's API-key authentication scheme.

Implements the HMAC-SHA256 signing algorithm documented by Onshape for
API keys (see https://onshape-public.github.io/docs/auth/apikeys/), as
used by Onshape's own apikey-sample reference clients: a canonical
string of method/nonce/date/content-type/path/query is signed with the
secret key, and the result is sent as an `Authorization: On ...` header
alongside the `Date` and `On-Nonce` headers used to build it.
"""

import base64
import hashlib
import hmac
import secrets
import string
from datetime import datetime, timezone
from typing import Dict
from urllib.parse import urlparse


def _make_nonce() -> str:
    alphabet = string.ascii_letters + string.digits
    return "".join(secrets.choice(alphabet) for _ in range(25))


def build_auth_headers(
    method: str,
    url: str,
    access_key: str,
    secret_key: str,
    content_type: str = "application/json",
    extra_headers: Dict[str, str] | None = None,
) -> Dict[str, str]:
    """Return the headers Onshape requires to authenticate `method` on `url`."""
    parsed = urlparse(url)
    nonce = _make_nonce()
    auth_date = datetime.now(timezone.utc).strftime("%a, %d %b %Y %H:%M:%S GMT")

    string_to_sign = "\n".join(
        [
            method.lower(),
            nonce,
            auth_date,
            content_type.lower(),
            parsed.path.lower(),
            (parsed.query or "").lower(),
        ]
    ) + "\n"

    signature = base64.b64encode(
        hmac.new(
            secret_key.encode("utf-8"),
            string_to_sign.encode("utf-8"),
            digestmod=hashlib.sha256,
        ).digest()
    ).decode("utf-8")

    headers = {
        "Content-Type": content_type,
        "Date": auth_date,
        "On-Nonce": nonce,
        "Authorization": f"On {access_key}:HmacSHA256:{signature}",
        "Accept": "application/json",
    }
    if extra_headers:
        headers.update(extra_headers)
    return headers
