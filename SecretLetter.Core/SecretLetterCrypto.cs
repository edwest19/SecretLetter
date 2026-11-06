// ============================================================================
// Copyright (c) 2026 Ed. Licensed under the MIT License (see LICENSE).
// AI-GENERATED CODE: This file was written by Claude (Anthropic). It replaces the
// original HMAC-SHA256 keystream, which reused one keystream for every letter
// encrypted with the same key and code and did not detect changes or a wrong key.
// SecretLetter is a project built with AI-generated code; see README.md.
// ============================================================================
using System.Security.Cryptography;
using System.Text;

namespace SecretLetter.Core;

/// <summary>
/// Encrypts and decrypts SecretLetter files (format version 1).
/// SECURITY-SENSITIVE: the format and key derivation are specified in docs/file-format.md and
/// locked by the known-answer test in SecretLetterCryptoTests. Never change them without a new
/// format version and the owner's approval.
/// </summary>
/// <remarks>
/// A letter file is: "SLTR" | version 1 | 16-byte salt | 12-byte nonce | ciphertext | 16-byte tag.
/// The file key needs both the 32-byte root key and the passphrase:
///   passphrase key = PBKDF2-HMAC-SHA256(UTF-8(NFC(passphrase)), salt, 600,000 iterations, 32 bytes)
///   file key       = HKDF-SHA256(root key || passphrase key, salt, "SecretLetter v1 file key", 32 bytes)
/// The letter is encrypted with AES-256-GCM, which also authenticates the 33-byte header.
/// Every letter gets a fresh random salt and nonce, so no two letters share a file key.
/// Only .NET's built-in cryptography is used (Windows CNG on Windows).
/// </remarks>
public static class SecretLetterCrypto
{
    /// <summary>The four bytes every SecretLetter file starts with.</summary>
    public const string MagicText = "SLTR";

    /// <summary>The format this code writes, and the only one it reads.</summary>
    public const byte FormatVersion = 1;

    /// <summary>Length of the root key in bytes (64 hexadecimal characters).</summary>
    public const int RootKeyLength = 32;

    /// <summary>Random salt stored in each file (used by both PBKDF2 and HKDF).</summary>
    public const int SaltLength = 16;

    /// <summary>Random AES-GCM nonce stored in each file.</summary>
    public const int NonceLength = 12;

    /// <summary>AES-GCM authentication tag at the end of each file.</summary>
    public const int TagLength = 16;

    /// <summary>Magic (4) + version (1) + salt (16) + nonce (12).</summary>
    public const int HeaderLength = 4 + 1 + SaltLength + NonceLength;

    /// <summary>Bytes a letter file has beyond the letter itself: header plus tag.</summary>
    public const int Overhead = HeaderLength + TagLength;

    /// <summary>PBKDF2-HMAC-SHA256 iterations for the passphrase (format version 1).</summary>
    public const int Pbkdf2Iterations = 600_000;

    /// <summary>HKDF "info" text that ties the derived key to this format version.</summary>
    public const string HkdfInfoText = "SecretLetter v1 file key";

    /// <summary>The largest letter SecretLetter encrypts: 64 MiB. The whole letter is held in memory.</summary>
    public const int MaxLetterBytes = 64 * 1024 * 1024;

    private const int VersionOffset = 4;
    private const int SaltOffset = 5;
    private const int NonceOffset = SaltOffset + SaltLength;
    private const int PassphraseKeyLength = 32;
    private const int FileKeyLength = 32;

    private static ReadOnlySpan<byte> Magic => "SLTR"u8;

    private static ReadOnlySpan<byte> HkdfInfo => "SecretLetter v1 file key"u8;

    /// <summary>Makes a new random 32-byte root key, as 64 uppercase hexadecimal characters.</summary>
    public static string GenerateRootKey()
    {
        byte[] key = RandomNumberGenerator.GetBytes(RootKeyLength);
        try
        {
            return Convert.ToHexString(key);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    /// <summary>Encrypts a letter with a fresh random salt and nonce.</summary>
    /// <exception cref="ArgumentException">The root key or passphrase is not valid, or the letter is larger than <see cref="MaxLetterBytes"/>.</exception>
    public static byte[] Encrypt(ReadOnlySpan<byte> letter, string rootKey, string passphrase)
    {
        Span<byte> salt = stackalloc byte[SaltLength];
        Span<byte> nonce = stackalloc byte[NonceLength];
        RandomNumberGenerator.Fill(salt);
        RandomNumberGenerator.Fill(nonce);
        return Encrypt(letter, rootKey, passphrase, salt, nonce);
    }

    /// <summary>
    /// Encrypts with a given salt and nonce. Only the known-answer test calls this directly:
    /// real letters always get a fresh random salt and nonce from the public overload.
    /// </summary>
    internal static byte[] Encrypt(ReadOnlySpan<byte> letter, string rootKey, string passphrase,
                                   ReadOnlySpan<byte> salt, ReadOnlySpan<byte> nonce)
    {
        CheckRootKey(rootKey);
        CheckPassphrase(passphrase);
        if (letter.Length > MaxLetterBytes)
        {
            throw new ArgumentException($"The letter is larger than {MaxLetterBytes / (1024 * 1024)} MiB, the most SecretLetter encrypts.", nameof(letter));
        }
        if (salt.Length != SaltLength || nonce.Length != NonceLength)
        {
            throw new ArgumentException("The salt or nonce has the wrong length.");
        }

        byte[] output = new byte[Overhead + letter.Length];
        Magic.CopyTo(output);
        output[VersionOffset] = FormatVersion;
        salt.CopyTo(output.AsSpan(SaltOffset, SaltLength));
        nonce.CopyTo(output.AsSpan(NonceOffset, NonceLength));

        byte[] rootKeyBytes = Convert.FromHexString(rootKey.Trim());
        byte[] fileKey = Array.Empty<byte>();
        try
        {
            fileKey = DeriveFileKey(rootKeyBytes, passphrase, salt);
            using var aes = new AesGcm(fileKey, TagLength);
            aes.Encrypt(
                nonce,
                letter,
                output.AsSpan(HeaderLength, letter.Length),
                output.AsSpan(HeaderLength + letter.Length, TagLength),
                output.AsSpan(0, HeaderLength));
            return output;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(fileKey);
            CryptographicOperations.ZeroMemory(rootKeyBytes);
        }
    }

    /// <summary>Decrypts a letter file, after checking that it is unchanged and that the key and passphrase are right.</summary>
    /// <exception cref="ArgumentException">The root key or passphrase is not valid.</exception>
    /// <exception cref="InvalidDataException">The data is not a SecretLetter file this version can read.</exception>
    /// <exception cref="CryptographicException">The key or passphrase is wrong, or the file was changed or damaged.</exception>
    public static byte[] Decrypt(ReadOnlySpan<byte> letterFile, string rootKey, string passphrase)
    {
        CheckRootKey(rootKey);
        CheckPassphrase(passphrase);
        CheckHeader(letterFile);

        int letterLength = letterFile.Length - Overhead;
        ReadOnlySpan<byte> header = letterFile[..HeaderLength];
        ReadOnlySpan<byte> salt = letterFile.Slice(SaltOffset, SaltLength);
        ReadOnlySpan<byte> nonce = letterFile.Slice(NonceOffset, NonceLength);
        ReadOnlySpan<byte> ciphertext = letterFile.Slice(HeaderLength, letterLength);
        ReadOnlySpan<byte> tag = letterFile.Slice(HeaderLength + letterLength, TagLength);

        byte[] letter = new byte[letterLength];
        byte[] rootKeyBytes = Convert.FromHexString(rootKey.Trim());
        byte[] fileKey = Array.Empty<byte>();
        try
        {
            fileKey = DeriveFileKey(rootKeyBytes, passphrase, salt);
            using var aes = new AesGcm(fileKey, TagLength);
            aes.Decrypt(nonce, ciphertext, tag, letter, header);
            return letter;
        }
        catch (AuthenticationTagMismatchException ex)
        {
            CryptographicOperations.ZeroMemory(letter);
            throw new CryptographicException("The letter could not be decrypted: the key or passphrase is wrong, or the file was changed or damaged.", ex);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(fileKey);
            CryptographicOperations.ZeroMemory(rootKeyBytes);
        }
    }

    /// <summary>
    /// Encrypts the letter at <paramref name="inputPath"/> into <paramref name="outputPath"/>.
    /// The output is written to a temporary file first and then moved into place, so a failure
    /// never leaves a partial file or damages an existing one.
    /// </summary>
    public static void EncryptFile(string inputPath, string outputPath, string rootKey, string passphrase)
    {
        CheckFileArguments(inputPath, outputPath, rootKey, passphrase);
        if (new FileInfo(inputPath).Length > MaxLetterBytes)
        {
            throw new InvalidDataException($"{Path.GetFileName(inputPath)} is larger than {MaxLetterBytes / (1024 * 1024)} MiB, the most SecretLetter encrypts.");
        }

        byte[] letter = File.ReadAllBytes(inputPath);
        try
        {
            AtomicFile.WriteAllBytes(outputPath, Encrypt(letter, rootKey, passphrase));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(letter);
        }
    }

    /// <summary>
    /// Decrypts the letter file at <paramref name="inputPath"/> into <paramref name="outputPath"/>.
    /// Nothing is written unless the whole file decrypts and checks out.
    /// </summary>
    public static void DecryptFile(string inputPath, string outputPath, string rootKey, string passphrase)
    {
        CheckFileArguments(inputPath, outputPath, rootKey, passphrase);
        if (new FileInfo(inputPath).Length > (long)MaxLetterBytes + Overhead)
        {
            throw new InvalidDataException($"{Path.GetFileName(inputPath)} is larger than any letter SecretLetter makes.");
        }

        byte[] letter = Decrypt(File.ReadAllBytes(inputPath), rootKey, passphrase);
        try
        {
            AtomicFile.WriteAllBytes(outputPath, letter);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(letter);
        }
    }

    private static void CheckRootKey(string rootKey)
    {
        string? problem = Validation.RootKeyError(rootKey);
        if (problem is not null)
        {
            throw new ArgumentException(problem, nameof(rootKey));
        }
    }

    private static void CheckPassphrase(string passphrase)
    {
        string? problem = Validation.PassphraseError(passphrase);
        if (problem is not null)
        {
            throw new ArgumentException(problem, nameof(passphrase));
        }
    }

    private static void CheckHeader(ReadOnlySpan<byte> letterFile)
    {
        if (letterFile.Length < Magic.Length || !letterFile[..Magic.Length].SequenceEqual(Magic))
        {
            throw new InvalidDataException("This is not a SecretLetter file. (Files made before SecretLetter 1.0.0 use an older format that this version can't read.)");
        }
        if (letterFile.Length < Overhead)
        {
            throw new InvalidDataException("This SecretLetter file is too short. It is incomplete or damaged.");
        }
        if (letterFile[VersionOffset] != FormatVersion)
        {
            throw new InvalidDataException($"This SecretLetter file uses format version {letterFile[VersionOffset]}. This version of SecretLetter reads only format version {FormatVersion}.");
        }
        if (letterFile.Length - Overhead > MaxLetterBytes)
        {
            throw new InvalidDataException($"This file is larger than any letter SecretLetter makes ({MaxLetterBytes / (1024 * 1024)} MiB).");
        }
    }

    private static void CheckFileArguments(string inputPath, string outputPath, string rootKey, string passphrase)
    {
        // The cheap checks come before any file is read.
        CheckRootKey(rootKey);
        CheckPassphrase(passphrase);

        if (string.IsNullOrWhiteSpace(inputPath) || string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Both an input file and an output file are needed.");
        }
        if (string.Equals(Path.GetFullPath(inputPath), Path.GetFullPath(outputPath), StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The input and output must be different files.");
        }
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException($"The input file doesn't exist: {inputPath}", inputPath);
        }
    }

    /// <summary>
    /// file key = HKDF-SHA256(root key || PBKDF2-HMAC-SHA256(passphrase, salt), salt, info).
    /// Both the root key and the passphrase are needed; either one alone is not enough.
    /// </summary>
    private static byte[] DeriveFileKey(ReadOnlySpan<byte> rootKey, string passphrase, ReadOnlySpan<byte> salt)
    {
        // NFC normalization: the same passphrase typed on different keyboards gives the same bytes.
        byte[] passphraseBytes = Encoding.UTF8.GetBytes(passphrase.Normalize(NormalizationForm.FormC));
        byte[] keyMaterial = new byte[RootKeyLength + PassphraseKeyLength];
        try
        {
            rootKey.CopyTo(keyMaterial);
            Rfc2898DeriveBytes.Pbkdf2(passphraseBytes, salt, keyMaterial.AsSpan(RootKeyLength), Pbkdf2Iterations, HashAlgorithmName.SHA256);

            byte[] fileKey = new byte[FileKeyLength];
            HKDF.DeriveKey(HashAlgorithmName.SHA256, keyMaterial, fileKey, salt, HkdfInfo);
            return fileKey;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passphraseBytes);
            CryptographicOperations.ZeroMemory(keyMaterial);
        }
    }
}
