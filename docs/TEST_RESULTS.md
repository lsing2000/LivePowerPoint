# Test results (host: Linux x64 sandbox, .NET SDK 10.0.403; no Windows, no PowerPoint)

| ID | Pri | Result | Notes |
|---|---|---|---|
| A01 | P0 | UNVERIFIED | no installer, no Windows |
| A02 | P0 | UNVERIFIED | tag persistence code (`BindingStore`) uncompiled; design keeps secrets out of deck (DPAPI per-user registry) |
| A03 | P0 | UNVERIFIED | **primary requirement; no slideshow evidence** |
| A04 | P0 | PARTIAL | unit-tested planner writes only mapped, changed, non-header cells; no visual check |
| A05 | P0 | UNVERIFIED | |
| A06 | P0 | UNVERIFIED | backoff/last-good logic unit-tested in Core only |
| A07 | P0 | UNVERIFIED | |
| A08 | P0 | UNVERIFIED | |
| A09 | P0 | UNVERIFIED | |
| A10 | P0 | PASS (backend) | tests: missing/wrong key -> 401; injection ids/keys -> 404, tables intact; Production has no demo write endpoint (404); key not logged. Slide-secret check unverified |
| A11 | P0 | UNVERIFIED | |
| A12 | P0 | UNVERIFIED | |
| A13 | P1 | UNVERIFIED | |
| A14 | P1 | UNVERIFIED | duplicate-binding detection not implemented |
| A15 | P1 | PARTIAL | parser rejects malformed data (unit-tested); throttling is polling-based |

Automated: `dotnet test` -> 31 passed, 0 failed. Manual smoke: GET snapshot v1 -> POST update -> v2 with new value; no key -> 401.
