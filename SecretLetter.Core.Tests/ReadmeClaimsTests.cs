// ============================================================================
// Copyright (c) 2026 Ed. Licensed under the MIT License (see LICENSE).
// AI-GENERATED CODE: This file was written by Claude (Anthropic).
// SecretLetter is a project built with AI-generated code; see README.md.
// ============================================================================
using System.Globalization;
using static SecretLetter.Core.Tests.TestHelper;

namespace SecretLetter.Core.Tests;

/// <summary>
/// Checks that the exact facts stated in README.md and docs/file-format.md are the ones the code uses,
/// so the documents can't quietly drift away from the code (CLAUDE.md "How to evaluate this repository").
/// </summary>
[TestClass]
public sealed class ReadmeClaimsTests
{
    // Written the way the documents write it, whatever the machine's language settings: "600,000".
    private static readonly string Iterations = SecretLetterCrypto.Pbkdf2Iterations.ToString("N0", CultureInfo.InvariantCulture);

    [TestMethod]
    public void Readme_StatesTheCryptoTheCodeUses()
    {
        string readme = ReadRepoFile("README.md");

        Assert.Contains("AES-256-GCM", readme);
        Assert.Contains("PBKDF2-HMAC-SHA256", readme);
        Assert.Contains($"{Iterations} iterations", readme);
        Assert.Contains("HKDF-SHA256", readme);
        Assert.Contains($"{SecretLetterCrypto.RootKeyLength}-byte", readme);
    }

    [TestMethod]
    public void Readme_StatesTheLimitsTheCodeEnforces()
    {
        string readme = ReadRepoFile("README.md");

        Assert.Contains($"at least {Validation.MinPassphraseLength} characters", readme);
        Assert.Contains($"{SecretLetterCrypto.MaxLetterBytes / (1024 * 1024)} MiB", readme);
        Assert.Contains($"{SecretLetterCrypto.Overhead} bytes", readme);
        Assert.Contains($"`{LetterFiles.LetterExtension}`", readme);
        Assert.Contains($"`{LetterFiles.EncryptedExtension}`", readme);
    }

    [TestMethod]
    public void Readme_NamesTheSettingsFiles()
    {
        string readme = ReadRepoFile("README.md");

        Assert.Contains($"`{AppConfig.FileName}`", readme);
        Assert.Contains($"`{UserSettings.FileName}`", readme);
        Assert.Contains(@"%LOCALAPPDATA%\SecretLetter\settings.json", readme);
    }

    [TestMethod]
    public void FileFormatSpec_MatchesTheCode()
    {
        string spec = ReadRepoFile("docs", "file-format.md");

        Assert.Contains($"`{SecretLetterCrypto.MagicText}`", spec);
        Assert.Contains($"`{SecretLetterCrypto.HkdfInfoText}`", spec);
        Assert.Contains($"{Iterations} iterations", spec);
        Assert.Contains($"{SecretLetterCrypto.HeaderLength}-byte header", spec);
        Assert.Contains($"{SecretLetterCrypto.Overhead} bytes", spec);
    }

    [TestMethod]
    public void FileFormatSpec_PrintsTheKnownAnswerVectorTheTestsUse()
    {
        string spec = ReadRepoFile("docs", "file-format.md");

        Assert.Contains(TestRootKey, spec);
        Assert.Contains(TestPassphrase, spec);
        Assert.Contains(SecretLetterCryptoTests.KnownAnswerSaltHex, spec);
        Assert.Contains(SecretLetterCryptoTests.KnownAnswerNonceHex, spec);
        Assert.Contains(SecretLetterCryptoTests.KnownAnswerLetter.Replace("\n", "\\n"), spec);
        Assert.Contains(SecretLetterCryptoTests.KnownAnswerFileHex, spec);
    }

    [TestMethod]
    public void GitIgnore_ExcludesConfigJson()
    {
        string[] lines = ReadRepoFile(".gitignore").Split('\n').Select(l => l.Trim()).ToArray();

        CollectionAssert.Contains(lines, "config.json");
    }
}
