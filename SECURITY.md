# Security policy

> AI-generated: this file was written by Claude (Anthropic), following SECURITY.md in edwest19/SecretPrinter. SecretLetter is a project built with AI-generated code; see [README.md](README.md).

## Read this first

- **This software was written by an AI.** Claude, an Anthropic model, wrote the code, the tests and this document, directed by one person who reviews every change. That is the point of the project (README "About this project").
- **It is maintained by one independent developer.** There is no company, no security team and no on-call rotation.
- **It has never had an outside security review.** No audit and no penetration test. Its security rests on .NET's built-in cryptography, the specification in [docs/file-format.md](docs/file-format.md), the tests, and anyone who checks them.
- **What it protects, and what it doesn't,** is in README "What SecretLetter does not protect against".

## Supported versions

Only the latest release gets fixes. Files made before version 1.0.0 use an older format that was not secure (the same key and code always gave the same keystream, and changes weren't detected); version 1.0.0 replaced it.

## Reporting a vulnerability

Use **GitHub private security advisories**: open the **Security** tab of this repository and choose **Report a vulnerability**. The report stays private between you and the maintainer until there is something to publish.

Please don't open a public issue for a security problem. If **Report a vulnerability** isn't there, private reporting hasn't been switched on; open a public issue saying only that you have a security report and need a private channel, with no details.

A reply may take days or longer: one person reads the reports, in his own time.

Useful in a report: the version, what you did, what happened, and what you expected. Never include a real key, passphrase or letter.
