# SecretLetter

<!-- AI-generated: this README was written by Claude (Anthropic). Its facts are checked against the code by ReadmeClaimsTests and ReleaseRulesTests; see CLAUDE.md. -->

SecretLetter encrypts a letter written in Markdown (a `.md` file) into a `.file`, and decrypts the `.file` back into the letter. A Windows desktop app; everything happens on your PC.

Opening a letter takes **both**:

- the **key**, a random 32-byte secret kept in `config.json`, which you share with the person you write to
- a **passphrase** that you agree on with them, which is never stored anywhere

A `.file` can then travel by email, USB stick or cloud storage. Without the key and the passphrase it can't be read, and any change to it is detected.

## About this project

- **Built with AI-generated code.** The purpose of SecretLetter is to build a real, useful app with AI-generated code. The code is written by an AI coding assistant, Claude by Anthropic. A human, the repository owner, decides what the app does, reviews every change, and merges it through a pull request.
  - Files Claude wrote or changed say so in a comment at the top of the file. A test fails if a C# file has no such comment.
  - Commits Claude helped with end with a `Co-Authored-By: Claude` line, so `git log` shows which changes were AI-generated.
- **Local only.** SecretLetter runs on your PC. Its code makes no network connections, and its app package asks for no permission except `runFullTrust` (the normal one for a desktop app). Tests check both (`RepositoryRulesTests`). GitHub is used only to store the code and to build releases.
- **Pure .NET, Microsoft packages only.** No third-party packages. The only packages are Microsoft's own:
  - the Windows App SDK and Windows SDK build tools, for the WinUI 3 window and MSIX packaging
  - MSTest, Microsoft's test framework, used by the tests only

  A test fails if any other package is added. The encryption uses only .NET's built-in cryptography.
- **Checkable by an AI.** [CLAUDE.md](CLAUDE.md) tells an AI assistant how to build, test, and check this README against the code.

## Install

1. Open the [latest release](https://github.com/edwest19/SecretLetter/releases/latest) and download `SecretLetter-<version>-x64.msix`.
2. Open the file. Windows installs SecretLetter and shows the publisher, Edwin West. The installer is signed, so Windows can confirm who made it and that it hasn't been changed.
3. Start **SecretLetter** from the Start menu.

To uninstall, go to **Settings → Apps → Installed apps → SecretLetter → Uninstall**.

- The MSIX includes .NET and the Windows App SDK, so nothing else needs to be installed. It runs on 64-bit (x64) Windows.
- Each release also has a `.sha256` file. To check your download, compare it with the output of `Get-FileHash .\SecretLetter-<version>-x64.msix` in PowerShell.

## Using SecretLetter

### The first time

1. Click **Browse...** next to **Config folder** and choose (or create) a folder for your settings, for example `Documents\SecretLetter`. Keep this folder private: it holds your key.
2. If the folder has no `config.json`, SecretLetter makes one with a newly generated key.
   - **Writing to someone who sent you a key?** Paste their key into **Key** and click **Save Config**.
3. SecretLetter reopens this folder the next time it starts.

### Encrypting a letter

1. Choose your letter (`.md`) as the **Input file**. The **Output file** becomes the same name ending in `.file`; click **Browse...** to choose another.
2. Type the passphrase twice. The second box is there so a typing mistake can't lock the letter away.
3. Click **Encrypt**.

### Decrypting a letter

1. Choose the `.file` as the **Input file**. The **Output file** becomes the same name ending in `.md`.
2. Type the passphrase, and click **Decrypt**.

If the key or passphrase is wrong, or the file was changed or damaged, SecretLetter says so and writes nothing.

When the output file already exists and isn't empty, SecretLetter asks before replacing it. **Save Config** keeps the key and the two file paths in `config.json`; the passphrase is never saved.

### Sending a letter to someone

1. Give them the key **once**, in a way nobody else can see, for example in person or on a USB stick. Never send the key in the same message as a letter.
2. Agree on a passphrase in the same careful way.
3. From then on, send only the `.file`.

## Key and passphrase

| | Key | Passphrase |
|---|---|---|
| What it is | 64 hexadecimal characters (`0`-`9`, `A`-`F`): a 32-byte random key | Any text of at least 8 characters, not only spaces |
| Where it is kept | `config.json` in your config folder, as plain text | Nowhere; you type it each time |
| Made by | **Generate Key** (from Windows' secure random number generator) | You |
| Exact rules | Spaces around it are ignored; upper and lower case are the same | Used exactly as typed (nothing is trimmed); different keyboards' forms of the same accented letter count as the same (Unicode NFC) |

- **Generate Key** asks before replacing a key that is already there: letters encrypted with the old key can be decrypted only with the old key.
- **There is no recovery.** A lost key or a forgotten passphrase means the letters can't be decrypted, by you or by anyone else. Keep a backup of `config.json` somewhere safe, such as on a USB stick in a drawer.
- **Choose a strong passphrase**, such as four or more random words. It is what protects your letters if someone gets a copy of `config.json`.

## Configuration

| File | Where | What it holds |
|---|---|---|
| `config.json` | The config folder you choose | `InputPath`, `OutputPath` and `RootKey` (the key, as plain text). Never the passphrase. |
| `settings.json` | `%LOCALAPPDATA%\SecretLetter\settings.json` | Only the path of the config folder used last, so the app can reopen it. Installed from the MSIX, Windows keeps this file in the app's own storage and removes it when the app is uninstalled. |

`config.json` files written by earlier versions of SecretLetter load unchanged. `config.json` is excluded from git (`.gitignore`), so a key can't be committed by accident.

## How a letter is protected

The exact recipe, with a test vector, is in [docs/file-format.md](docs/file-format.md). In short:

- Every `.file` gets a new random salt and nonce, so no two letters are ever encrypted with the same key, even with the same key and passphrase.
- The passphrase is stretched with **PBKDF2-HMAC-SHA256** (600,000 iterations), which makes each guess at it slow.
- That result and the 32-byte key are combined with **HKDF-SHA256** into a key for this one file. Both are needed: either one alone gives nothing.
- The letter is encrypted with **AES-256-GCM**, which also detects any change to the file (including its header) and a wrong key or passphrase.
- A `.file` is 49 bytes longer than the letter: a 33-byte header and a 16-byte check value.

All of this is .NET's built-in cryptography, which uses Windows' own (CNG). SecretLetter has not been audited by an independent security expert.

## What SecretLetter does not protect against

- **Someone who has your key and can guess your passphrase.** A weak passphrase is the weak point if `config.json` is copied.
- **Malware on your PC.** It can read the letter while you write it, and `config.json` at any time.
- **The original letter.** Encrypting doesn't delete the `.md` file. Delete it yourself if it shouldn't stay on the PC (on an SSD, a deleted file can't be guaranteed to be gone).
- **What a file reveals by itself:** its name, its date, and its size (the letter's size plus 49 bytes).
- **Files made before version 1.0.0.** They used an older, weaker format that this version doesn't read. Decrypt them with the version that made them first.

## Known limitations

- A letter can be at most 64 MiB; the whole letter is held in memory while it is encrypted or decrypted.
- Letters must be `.md` files, and encrypted letters `.file` files.
- 64-bit (x64) Windows only.

## Projects

| Project | What it is |
|---|---|
| `SecretLetter.Core` | Encryption, file rules, settings. No user interface; has all the tests |
| `SecretLetter.UI` | WinUI 3 desktop app (.NET 10), packaged as an MSIX (full trust) |
| `SecretLetter.Core.Tests` | MSTest tests |

## Building and testing

- Windows, Visual Studio 2026 (or just the .NET 10 SDK), and Git. Open `SecretLetter.slnx`. `global.json` selects a .NET 10 SDK.
- Run the tests:

  ```
  dotnet test SecretLetter.Core.Tests/SecretLetter.Core.Tests.csproj
  ```

- Build everything, including the WinUI app (Windows only):

  ```
  dotnet build SecretLetter.slnx -c Release
  ```

- `SecretLetter.UI` is packaged as an MSIX (full trust) for a clean install and uninstall. Every pull request packages it, unsigned, with Microsoft's Windows App Development CLI (`winapp pack`), and `scripts/Test-Msix.ps1` checks the package: publisher, version, self-contained (no other packages needed), and no permission except `runFullTrust`.

## Releases

Releases are built, signed and published by `.github/workflows/release.yml`, only when a version tag is pushed.

1. Set the version in **three places**. `ReleaseRulesTests` fails if they disagree.
   - `Directory.Build.props`: `X.Y.Z`
   - `SecretLetter.UI/Package.appxmanifest`: `X.Y.Z.0`
   - `CHANGELOG.md`: a heading `## [X.Y.Z] - YYYY-MM-DD`, whose text becomes the release notes
2. Push a tag `vX.Y.Z`, or `vX.Y.Z-rc.N` for a pre-release.
3. The workflow then:
   - checks the tag against the version
   - runs the tests
   - packages the MSIX and checks it with `scripts/Test-Msix.ps1`
   - **waits for the owner's approval** in the GitHub environment `release`
   - signs the MSIX with Azure Artifact Signing (account `edwest19`, profile `CodeSigning-Apps`, East US)
   - checks that the signature is valid, timestamped and made by `CN=Edwin West, O=Edwin West, L=Huntington, S=ny, C=US`
   - publishes the GitHub Release with the MSIX and its `.sha256`

Signing uses OpenID Connect, so no password or key is stored in GitHub. Every action is pinned to an exact commit. The workflows turn off the .NET CLI's and winapp's usage telemetry.

The repository uses GitHub's **immutable OIDC subject claims** (**Settings → Actions → OIDC → Use immutable subject claim**), so the sign-in token names the repository by its numeric IDs as well as its name. The Azure app registration `SecretLetter-GitHubActions-Signing` trusts exactly this subject:

`repo:edwest19@5809459/SecretLetter@1279392996:environment:release`

- A different repository that later takes the name `edwest19/SecretLetter` can't match it.
- Renaming this repository, its owner or the `release` environment changes the subject. The Azure federated credential must then be updated to match.

### One-time setup

What signing needs, done once by the owner:

- **Azure:** the app registration `SecretLetter-GitHubActions-Signing`, with no certificate and no secret, and one federated credential:
  - issuer `https://token.actions.githubusercontent.com`
  - subject exactly the line above
  - audience `api://AzureADTokenExchange`

  The registration has the role **Artifact Signing Certificate Profile Signer** on the signing account `edwest19`.
- **GitHub:** the environment `release`, which requires the owner as reviewer and accepts only tags beginning with `v`. It holds three secrets: `AZURE_CLIENT_ID` (the registration's application ID), `AZURE_TENANT_ID` and `AZURE_SUBSCRIPTION_ID`. They are identifiers, not passwords.
- **GitHub:** **Use immutable subject claim**, as above.

## Version history

See [CHANGELOG.md](CHANGELOG.md). To report a security problem, see [SECURITY.md](SECURITY.md).

## License

MIT. See `LICENSE`.
