// ============================================================================
// Copyright (c) 2026 Ed. Licensed under the MIT License (see LICENSE).
// AI-GENERATED CODE: This file was written by Claude (Anthropic).
// SecretLetter is a project built with AI-generated code; see README.md.
// ============================================================================
using System.Text;

namespace SecretLetter.Core;

/// <summary>
/// Checks the key and passphrase before anything is encrypted or decrypted. Each method returns
/// null when the value is valid, or a message for the user explaining what's wrong.
/// The limits are documented in README.md.
/// </summary>
public static class Validation
{
    /// <summary>The shortest passphrase SecretLetter accepts.</summary>
    public const int MinPassphraseLength = 8;

    /// <summary>
    /// The root key must be exactly 64 hexadecimal characters (32 bytes). Spaces around it are ignored.
    /// Only 0-9, a-f and A-F are accepted: no "0x" prefix, signs or spaces inside the key.
    /// </summary>
    public static string? RootKeyError(string? rootKey)
    {
        string hex = (rootKey ?? string.Empty).Trim();
        if (hex.Length != SecretLetterCrypto.RootKeyLength * 2 || !hex.All(char.IsAsciiHexDigit))
        {
            return "The key must be exactly 64 hexadecimal characters (0-9, A-F). Use \"Generate Key\" to make one.";
        }
        return null;
    }

    /// <summary>
    /// The passphrase must be at least <see cref="MinPassphraseLength"/> characters, not only spaces,
    /// and valid Unicode. It is used exactly as typed (after Unicode NFC normalization); it is never trimmed.
    /// </summary>
    public static string? PassphraseError(string? passphrase)
    {
        if (string.IsNullOrEmpty(passphrase))
        {
            return "Enter a passphrase.";
        }
        if (passphrase.Length < MinPassphraseLength)
        {
            return $"The passphrase must be at least {MinPassphraseLength} characters long.";
        }
        if (string.IsNullOrWhiteSpace(passphrase))
        {
            return "The passphrase can't be only spaces.";
        }
        try
        {
            _ = passphrase.Normalize(NormalizationForm.FormC);
        }
        catch (ArgumentException)
        {
            return "The passphrase contains a character that can't be used.";
        }
        return null;
    }
}
