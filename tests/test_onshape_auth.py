from onshape_featurescript_ai.onshape_auth import build_auth_headers


def test_build_auth_headers_shape():
    headers = build_auth_headers(
        method="GET",
        url="https://cad.onshape.com/api/v6/documents/abc123?q=1",
        access_key="AK_TEST",
        secret_key="SK_TEST",
    )

    assert headers["Authorization"].startswith("On AK_TEST:HmacSHA256:")
    assert "Date" in headers
    assert len(headers["On-Nonce"]) == 25
    assert headers["Content-Type"] == "application/json"


def test_signature_changes_with_path():
    common = dict(method="GET", access_key="AK", secret_key="SK")
    a = build_auth_headers(url="https://cad.onshape.com/api/v6/documents/a", **common)
    b = build_auth_headers(url="https://cad.onshape.com/api/v6/documents/b", **common)

    assert a["Authorization"] != b["Authorization"]
