# SecretLetter

SecretLetter is a deterministic file encryption tool built in C# using a WinUI 3 front-end and a custom HMAC-SHA256-based keystream generator.

## Features

- Encrypt `.md` files into `.file` format
- Decrypt `.file` back into `.md`
- Deterministic keystream using:
  - 32-byte hex key (64 hex chars)
  - 4-digit date code
- Symmetric transform (same function encrypts and decrypts)
- Clean WinUI 3 interface
- JSON-based configuration

## Projects

- **SecretLetter.Core** — Crypto engine (HMAC-SHA256 keystream + XOR stream)
- **SecretLetter.UI** — WinUI 3 front-end

## Usage

Create a SecretLetter folder for the configuration file. The configuration file is created when the program is run for the first time.

1. Select input file (`.md` or `.file`)
2. Select output file (extension enforced automatically)
3. Enter 64-hex-character key
4. Enter 4-digit date code
5. Click **Run**

## Build Requirements

- .NET 8
- Windows 10/11
- Visual Studio 2022 with WinUI 3 workload

## License

MIT
