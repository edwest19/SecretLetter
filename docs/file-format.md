# SecretLetter file format, version 1

> AI-generated: this document was written by Claude (Anthropic). SecretLetter is a project built with AI-generated code; see [README.md](../README.md).

This is the exact recipe SecretLetter 1.0.0 and later use to turn a letter into a `.file` and back. It is enough to write a separate program that reads and writes the same files. `SecretLetter.Core/SecretLetterCrypto.cs` implements it, and the known-answer test at the end locks it (`SecretLetterCryptoTests`, `ReadmeClaimsTests`).

A change to anything here makes files that older versions can't read. It needs a new format version, an updated document, and the owner's approval.

## Inputs

| Input | What it is |
|---|---|
| Root key | 32 bytes, written as 64 hexadecimal characters in `config.json`. Spaces around it are ignored; upper and lower case are the same. |
| Passphrase | Text the user types, at least 8 characters. Never stored. |
| Letter | The bytes of the `.md` file, at most 64 MiB. They are not read as text. |

## Layout

A file is a 33-byte header, the encrypted letter, and a 16-byte tag. It is always 49 bytes longer than the letter.

| Offset | Length | Field |
|---|---|---|
| 0 | 4 | Magic: the ASCII bytes `SLTR` (`53 4C 54 52`) |
| 4 | 1 | Format version: `01` |
| 5 | 16 | Salt: random for every file |
| 21 | 12 | Nonce: random for every file |
| 33 | n | The encrypted letter (n = the letter's length) |
| 33 + n | 16 | AES-GCM authentication tag |

## Making the key for one file

1. **Passphrase bytes.** Normalize the passphrase to Unicode NFC, then encode it as UTF-8. Nothing is trimmed.
2. **Passphrase key.** PBKDF2-HMAC-SHA256 of the passphrase bytes, with the file's salt, 600,000 iterations, 32 bytes.
3. **File key.** HKDF-SHA256 (RFC 5869) with:
   - input key material: the 32-byte root key followed by the 32-byte passphrase key (64 bytes)
   - salt: the file's salt
   - info: the ASCII text `SecretLetter v1 file key`
   - length: 32 bytes

The file key needs both the root key and the passphrase. A new random salt for every file gives every file its own key.

## Encrypting

AES-256-GCM with the file key, the file's 12-byte nonce, and a 16-byte tag. The additional authenticated data is the whole 33-byte header (bytes 0 to 32), so changing the magic, version, salt or nonce is detected too.

## Decrypting

1. Check the magic. A file without it is not a SecretLetter file. (Files from before 1.0.0 have no header and are refused here.)
2. Check that the file is at least 49 bytes long, then check the version. Only version 1 is read.
3. Make the file key from the salt, as above.
4. Decrypt and verify the tag. If the tag doesn't match, the key or passphrase is wrong, or the file was changed or damaged. Nothing is written in that case.

## Known-answer test

These inputs, which are for testing only, must give exactly this file.

| Input | Value |
|---|---|
| Root key | `000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F` |
| Passphrase | `correct horse battery staple` |
| Salt | `A0A1A2A3A4A5A6A7A8A9AAABACADAEAF` |
| Nonce | `B0B1B2B3B4B5B6B7B8B9BABB` |
| Letter (UTF-8, shown with JSON escapes) | `"# Test letter\n\nThis is the SecretLetter known-answer test. Ünïcödé too.\n"` |

The file, in hexadecimal:

```
534C545201A0A1A2A3A4A5A6A7A8A9AAABACADAEAFB0B1B2B3B4B5B6B7B8B9BABB051400E6EA6FDC0B65EB5E267D3BBCF6AD3A289E4FB096A8C195E552CC50736FD539D066EE597F370649C70BF5AA890D558B830915162AD4ECE5FDC0EEF213B7A63BEABA1BAFAFEBA36C9C4FD640F7D96E9916F4E0397C5D182C474B
```

This value was first computed by a separate Python implementation written from this document alone (PBKDF2 and HKDF from Python's standard library, AES-GCM from the `cryptography` package). The C# code reproduces it, and the Python implementation decrypted a file the C# code made with a random salt and nonce.
