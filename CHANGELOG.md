# Changelog

> AI-generated: this file was written by Claude (Anthropic). SecretLetter is a project built with AI-generated code; see [README.md](README.md).

Every release has a heading `## [X.Y.Z] - YYYY-MM-DD`. The release workflow refuses a tag that has no entry here, and uses the entry as the release notes.

## [1.0.0] - 2026-11-06

The first release, as a signed MSIX installer.

**Files made by earlier builds can't be read by this version.** Their format was replaced before release; decrypt any old `.file` with the build that made it, then encrypt the letter again with this version.

### Encryption (file format version 1)
- Letters are encrypted with AES-256-GCM. Every `.file` gets a new random salt and nonce, so no two letters share a key. Before, every letter encrypted with the same key and code used the same keystream, so knowing one letter could reveal another.
- A changed or damaged file, or a wrong key or passphrase, is detected, and nothing is written. Before, it silently produced garbage.
- The 4-digit date code is replaced by a passphrase of at least 8 characters, never stored. Opening a letter takes both the key and the passphrase (PBKDF2-HMAC-SHA256 with 600,000 iterations, combined with the key by HKDF-SHA256).
- The format is specified in `docs/file-format.md`, with a test vector checked by an independent implementation.

### App
- **Generate Key** works (it wasn't connected), and asks before replacing a key.
- When encrypting, the passphrase is typed twice. The Run button says **Encrypt** or **Decrypt**.
- The app asks before replacing an existing output file, and writes files all at once, so a failure never leaves a partial or damaged file.
- The key is checked strictly: exactly 64 hexadecimal characters. Before, some invalid keys were accepted.
- The config folder is remembered between starts. Encrypting and decrypting run in the background, so the window stays responsive.
- The window is sized for the screen's scaling.

### Packaging and project
- .NET 10 (support for .NET 8 ends on 2026-11-10). Released as a signed MSIX (x64) with .NET and the Windows App SDK included, so nothing else needs to be installed.
- The app is named SecretLetter, published by Edwin West. The unused `systemAIModels` permission was removed.
- Microsoft packages only. Tests check the package list, network use, AI-generated headers, the README's facts and the release settings.
- Unused code (an old config manager and a second copy of the keystream) was removed.
