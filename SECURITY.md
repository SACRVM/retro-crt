# Security Policy

## Supported versions

Retro.Crt is pre-1.0 and only the latest released minor version receives
security fixes. Older versions are not patched.

| Version | Status                |
|---------|-----------------------|
| `0.2.x` | Supported             |
| `< 0.2` | No longer supported   |

## Reporting a vulnerability

Please **do not** open a public GitHub issue for security problems.

Instead, report privately via GitHub's
[security advisories form](https://github.com/chloe-dream/retro-crt/security/advisories/new),
or e-mail **chloe.bernette@gmail.com** with `retro-crt` in the subject.

You can expect:

- An acknowledgement within **3 business days**.
- A fix or mitigation in a follow-up release within **14 days** for
  practical issues, sooner if a working exploit is in the wild.
- Public credit in the release notes once a patched version ships,
  unless you ask to stay anonymous.

## Scope

In scope:

- Code execution, command injection, or path traversal triggered by
  data passed through the public API.
- Bugs that can crash a host application via uncaught exceptions in
  the public API surface.
- Trim/AOT regressions that leak sensitive metadata into the published
  binary.

Out of scope:

- Bugs requiring an attacker to already control the host process.
- Misconfigured terminals or environments.
- Vulnerabilities in third-party dependencies (Retro.Crt has none in
  the runtime path; CI / bench / docs tooling is patched via
  Dependabot).
