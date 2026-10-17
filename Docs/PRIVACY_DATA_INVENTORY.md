# Privacy and data inventory

Status: initial template. No advertising or remote analytics SDK is integrated in Work Orders 00-01.

| Component | Data | Purpose | Storage/transfer | Status |
|---|---|---|---|---|
| Pure board simulation | Seed, board cells, turn count, RNG state | Reproducible local gameplay | In memory only at this milestone | Implemented |
| Unity engine | Review against actual build and enabled services | Runtime | To be audited before release | Pending |
| Advertising | None | Not integrated | None | Out of scope |
| Remote analytics | None | Not integrated | None | Out of scope |

Never commit secrets, real publisher identifiers, signing credentials or user data samples.

