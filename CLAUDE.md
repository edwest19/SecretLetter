# CLAUDE.md: guide for AI assistants

> AI-generated: this file was written by Claude (Anthropic), following CLAUDE.md in edwest19/SecretKey. SecretLetter is a project built with AI-generated code; see [README.md](README.md).

This file is for any AI assistant (Claude or another) that works on or evaluates this repository. Read it fully before changing anything.

## What this project is

SecretLetter is a small Windows app (WinUI 3, .NET 10) that encrypts a Markdown letter (`.md`) into a `.file` and back, using a 32-byte key kept in `config.json` and a passphrase that is never stored.

The purpose of the project is to build a real app with AI-generated code. AI assistants write the code; the repository owner decides what the app does, reviews every change and merges it through a pull request.

## Ground rules

1. **Local only.**
   - App code must not use any network API.
   - The MSIX manifest must not declare network, device or AI capabilities.
   - Enforced by `RepositoryRulesTests`.
2. **Microsoft packages only.**
   - No third-party NuGet packages. The allowed list is in `RepositoryRulesTests`.
   - Adding a package, even a Microsoft one, needs the owner's approval.
3. **Never change the file format silently.**
   - The format is specified in `docs/file-format.md` and locked by the known-answer test in `SecretLetterCryptoTests`.
   - Never edit expected values to make a test pass.
   - A deliberate change means a new format version, an updated spec, and the owner's approval. Files in the old format must stay readable or the change must say clearly that they aren't.
   - Use only .NET's built-in cryptography. Don't invent cryptography.
4. **Mark AI-generated code.**
   - A file you write gets this header (C# shown; use the file type's comment syntax):
     ```
     // ============================================================================
     // Copyright (c) 2026 Ed. Licensed under the MIT License (see LICENSE).
     // AI-GENERATED CODE: This file was written by Claude (Anthropic).
     // SecretLetter is a project built with AI-generated code; see README.md.
     // ============================================================================
     ```
   - A file you only change gets an `AI-GENERATED CODE (partial)` header naming what you wrote, plus a short `AI-generated (Claude):` comment at the changed code.
   - `RepositoryRulesTests.AiGeneratedFiles_SayWhoWroteThem` checks every C# file.
   - Commits end with a `Co-Authored-By:` line naming the AI.
5. **Keep README, docs and code in sync.** A pull request that changes behavior updates the README and docs in the same pull request.
6. **No secrets in the repository.**
   - Never commit `config.json`, a real key, a letter, a `.pfx` file, or signing credentials.
   - Tests use only the clearly marked TEST-ONLY key and passphrase in `TestHelper`.
7. **Pull requests only.** Work on a branch and open a pull request; never push to `main`.
8. **Never claim a test you didn't run.** Say what you ran and where. A Linux container is not the owner's Windows PC; the WinUI app and the MSIX are checked only by the Windows build in CI and by the owner.
9. **Releases only through the release workflow.**
   - Never sign or publish by hand.
   - The version lives in `Directory.Build.props`, `SecretLetter.UI/Package.appxmanifest` (`X.Y.Z.0`) and `CHANGELOG.md`.
   - The manifest's `Publisher` must equal `EXPECTED_SIGNER` in `release.yml`.
   - Workflow actions are pinned to full commit SHAs.
   - Signing trusts one immutable OIDC subject, `repo:edwest19@5809459/SecretLetter@1279392996:environment:release` (README "Releases"). Renaming the repo, its owner or the `release` environment breaks signing until the Azure federated credential is updated.
   - Enforced by `ReleaseRulesTests`.

## Layout

| Path | What it is |
|---|---|
| `SecretLetter.Core/SecretLetterCrypto.cs` | Encryption and decryption (spec: `docs/file-format.md`) |
| `SecretLetter.Core/Validation.cs` | Checks for the key and passphrase |
| `SecretLetter.Core/LetterFiles.cs` | `.md` / `.file` rules: what to do with an input, suggested output, path checks |
| `SecretLetter.Core/AppConfig.cs` | `config.json` (paths and key) |
| `SecretLetter.Core/UserSettings.cs` | `settings.json` (the config folder used last) |
| `SecretLetter.Core/AtomicFile.cs` | Writes a file all at once through a temporary file |
| `SecretLetter.UI/` | WinUI 3 desktop app, packaged as MSIX (full trust). Calls `SecretLetter.Core` for everything |
| `SecretLetter.Core.Tests/` | MSTest tests (see the claims map below) |
| `docs/file-format.md` | Exact file format, key derivation and test vector |
| `.github/workflows/build.yml` | CI on every pull request: tests, Windows build, unsigned MSIX packaging |
| `.github/workflows/release.yml` | Release on a `v*` tag: check, test, package, sign (after approval), publish |
| `Directory.Build.props` | The version, shared by all projects |
| `global.json` | Selects a .NET 10 SDK |
| `scripts/Test-Msix.ps1` | Checks a packaged MSIX: identity, self-contained, permissions (used by both workflows) |
| `CHANGELOG.md` | Release notes; the release workflow requires an entry for the version |

## Build and test

- Tests: `dotnet test SecretLetter.Core.Tests/SecretLetter.Core.Tests.csproj`
- Whole solution, including the WinUI app (Windows only): `dotnet build SecretLetter.slnx -c Release`

## How to evaluate this repository

1. **Run the tests.** All must pass.
2. **Check the README against the code.** For each statement in `README.md`, find:
   - the code that implements it (file and line)
   - a test that proves it, if any

   Report every statement with no code behind it, or with code that does something different.
3. **Check the spec against the code.**
   - Compare `docs/file-format.md` with `SecretLetter.Core/SecretLetterCrypto.cs`, step by step.
   - The strongest check is to implement the spec independently in another language and reproduce the known-answer vector.
4. **Check the ground rules** above, including that every file an AI wrote or changed carries the header.
5. **Report** each finding as: statement, evidence (file:line or test name), verdict (matches / differs / not implemented).

## README claims map

Where each README section is implemented and which tests prove it. `ReadmeClaimsTests` also checks the README's and the spec's exact facts against the code.

| README section | Code | Tests |
|---|---|---|
| About this project: local only, Microsoft packages, AI headers | `SecretLetter.UI/Package.appxmanifest`, all `*.csproj`, all `*.cs` | `RepositoryRulesTests` |
| Install | `release.yml` (packaging and signing), `Package.appxmanifest` | `ReleaseRulesTests`; the MSIX is packaged on every pull request |
| Using SecretLetter | `SecretLetter.UI/MainWindow.xaml.cs` (calls `LetterFiles`, `Validation`, `AppConfig`, `UserSettings`, `SecretLetterCrypto`) | `LetterFilesTests`, `SecretLetterCryptoTests.*File*`; the window itself is checked by hand on Windows |
| Key and passphrase | `Validation.cs`, `SecretLetterCrypto.GenerateRootKey`, `SecretLetterCrypto.DeriveFileKey` | `ValidationTests`, `SecretLetterCryptoTests.Passphrase_*`, `SecretLetterCryptoTests.RootKey_*` |
| Configuration | `AppConfig.cs`, `UserSettings.cs`, `.gitignore` | `SettingsTests`, `ReadmeClaimsTests.Readme_NamesTheSettingsFiles`, `ReadmeClaimsTests.GitIgnore_ExcludesConfigJson` |
| How a letter is protected | `SecretLetterCrypto.cs`, `docs/file-format.md` | `SecretLetterCryptoTests`, `ReadmeClaimsTests` |
| What SecretLetter does not protect against | (statements of limits, not features) | `SecretLetterCryptoTests.PlainTextOrAnOldFormatFile_IsRejected` |
| Known limitations | `SecretLetterCrypto.MaxLetterBytes`, `LetterFiles.cs`, `SecretLetter.UI.csproj` (x64) | `SecretLetterCryptoTests.ALetterOverTheLimit_IsRefused`, `SecretLetterCryptoTests.EncryptFile_RefusesALetterOverTheLimit_BeforeReadingIt` |
| Releases | `release.yml`, `Directory.Build.props`, `CHANGELOG.md` | `ReleaseRulesTests` |

## Known gaps (README vs code)

- The WinUI window (`SecretLetter.UI`) has no automated tests. Everything it decides is delegated to `SecretLetter.Core`, which is tested; the window itself is checked by hand on Windows.

If you find another gap, list it here and fix it in the same pull request, or in the next one.
