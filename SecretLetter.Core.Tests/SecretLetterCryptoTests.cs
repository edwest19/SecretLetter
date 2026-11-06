// ============================================================================
// Copyright (c) 2026 Ed. Licensed under the MIT License (see LICENSE).
// AI-GENERATED CODE: This file was written by Claude (Anthropic).
// SecretLetter is a project built with AI-generated code; see README.md.
// ============================================================================
using System.Security.Cryptography;
using System.Text;
using static SecretLetter.Core.Tests.TestHelper;

namespace SecretLetter.Core.Tests;

/// <summary>
/// The encryption itself (README "How a letter is protected", docs/file-format.md).
/// The known-answer test locks the format: if it fails, the format changed. Never edit the
/// expected bytes to make it pass; a deliberate change needs a new format version.
/// </summary>
[TestClass]
public sealed class SecretLetterCryptoTests
{
    // The known-answer vector is also printed in docs/file-format.md (ReadmeClaimsTests checks they match).
    // It was produced by a separate Python implementation written from docs/file-format.md alone
    // (PBKDF2 and HKDF from Python's standard library, AES-GCM from the 'cryptography' package),
    // not by this C# code.
    internal const string KnownAnswerLetter = "# Test letter\n\nThis is the SecretLetter known-answer test. Ünïcödé too.\n";
    internal const string KnownAnswerSaltHex = "A0A1A2A3A4A5A6A7A8A9AAABACADAEAF";
    internal const string KnownAnswerNonceHex = "B0B1B2B3B4B5B6B7B8B9BABB";
    internal const string KnownAnswerFileHex =
        "534C545201A0A1A2A3A4A5A6A7A8A9AAABACADAEAFB0B1B2B3B4B5B6B7B8B9BABB051400E6EA6FDC0B65EB5E267D3BBCF6" +
        "AD3A289E4FB096A8C195E552CC50736FD539D066EE597F370649C70BF5AA890D558B830915162AD4ECE5FDC0EEF213B7A63B" +
        "EABA1BAFAFEBA36C9C4FD640F7D96E9916F4E0397C5D182C474B";

    private static readonly byte[] Letter = Encoding.UTF8.GetBytes("# Dear reader\n\nMeet me at the old bridge at nine. The safe code is 4417.\n");

    private static byte[] Encrypt(byte[] letter) => SecretLetterCrypto.Encrypt(letter, TestRootKey, TestPassphrase);

    private static byte[] Decrypt(byte[] file) => SecretLetterCrypto.Decrypt(file, TestRootKey, TestPassphrase);

    [TestMethod]
    public void KnownAnswer_EncryptingGivesExactlyTheSpecifiedBytes()
    {
        byte[] file = SecretLetterCrypto.Encrypt(
            Encoding.UTF8.GetBytes(KnownAnswerLetter), TestRootKey, TestPassphrase,
            Convert.FromHexString(KnownAnswerSaltHex), Convert.FromHexString(KnownAnswerNonceHex));

        Assert.AreEqual(KnownAnswerFileHex, Convert.ToHexString(file));
    }

    [TestMethod]
    public void KnownAnswer_DecryptingGivesTheLetter()
    {
        byte[] letter = Decrypt(Convert.FromHexString(KnownAnswerFileHex));

        Assert.AreEqual(KnownAnswerLetter, Encoding.UTF8.GetString(letter));
    }

    [TestMethod]
    public void RoundTrip_ReturnsTheLetter_ForAnyLength()
    {
        foreach (int length in new[] { 0, 1, 15, 16, 17, 1000 })
        {
            byte[] letter = RandomNumberGenerator.GetBytes(length);

            CollectionAssert.AreEqual(letter, Decrypt(Encrypt(letter)), $"Length {length}");
        }
    }

    [TestMethod]
    public void File_IsHeaderThenCiphertextThenTag()
    {
        byte[] file = Encrypt(Letter);

        // The 33-byte header and 49 bytes of overhead are checked against docs/file-format.md by ReadmeClaimsTests.
        Assert.HasCount(SecretLetterCrypto.Overhead + Letter.Length, file);
        Assert.AreEqual(SecretLetterCrypto.MagicText, Encoding.ASCII.GetString(file, 0, 4));
        Assert.AreEqual(SecretLetterCrypto.FormatVersion, file[4]);
    }

    [TestMethod]
    public void SameLetterTwice_GivesDifferentFiles_WithFreshSaltAndNonce()
    {
        byte[] first = Encrypt(Letter);
        byte[] second = Encrypt(Letter);

        Assert.AreNotEqual(Convert.ToHexString(first.AsSpan(5, 16)), Convert.ToHexString(second.AsSpan(5, 16)), "Salt reused.");
        Assert.AreNotEqual(Convert.ToHexString(first.AsSpan(21, 12)), Convert.ToHexString(second.AsSpan(21, 12)), "Nonce reused.");
        Assert.AreNotEqual(Convert.ToHexString(first), Convert.ToHexString(second));
    }

    [TestMethod]
    public void TwoLettersWithTheSameKeyAndPassphrase_DoNotShareAKeystream()
    {
        // The flaw in the original design: with one keystream per key and code, XOR-ing two
        // encrypted letters gave the XOR of the two letters, so knowing one revealed the other.
        byte[] letterA = Encoding.UTF8.GetBytes("# Dear Alice\n\nMeet me at the old bridge at 9pm. Bring the documents.\n");
        byte[] letterB = Encoding.UTF8.GetBytes("# Dear Bob\n\nThe password for the safe is 4417. Do not share it with anyone.\n");
        byte[] fileA = Encrypt(letterA);
        byte[] fileB = Encrypt(letterB);

        int length = Math.Min(letterA.Length, letterB.Length);
        int h = SecretLetterCrypto.HeaderLength;
        int matching = Enumerable.Range(0, length)
            .Count(i => (fileA[h + i] ^ fileB[h + i]) == (letterA[i] ^ letterB[i]));

        // Random chance gives about length/256 matches; a shared keystream would give all of them.
        Assert.IsLessThan(length / 4, matching, $"{matching} of {length} bytes leak the XOR of the two letters.");
    }

    [TestMethod]
    public void WrongPassphrase_IsDetected()
    {
        byte[] file = Encrypt(Letter);

        var ex = Assert.ThrowsExactly<CryptographicException>(() => SecretLetterCrypto.Decrypt(file, TestRootKey, TestPassphrase + "!"));
        Assert.Contains("key or passphrase is wrong", ex.Message);
    }

    [TestMethod]
    public void WrongRootKey_IsDetected()
    {
        byte[] file = Encrypt(Letter);

        Assert.ThrowsExactly<CryptographicException>(() => SecretLetterCrypto.Decrypt(file, OtherRootKey, TestPassphrase));
    }

    [TestMethod]
    public void AChangedByte_IsDetected_InTheSaltNonceCiphertextAndTag()
    {
        byte[] file = Encrypt(Letter);
        int[] positions =
        {
            5,                                 // salt
            21,                                // nonce
            SecretLetterCrypto.HeaderLength,   // first byte of the encrypted letter
            file.Length - 1,                   // last byte of the tag
        };

        foreach (int position in positions)
        {
            byte[] changed = (byte[])file.Clone();
            changed[position] ^= 0x01;

            Assert.ThrowsExactly<CryptographicException>(() => Decrypt(changed), $"A change at byte {position} was not detected.");
        }
    }

    [TestMethod]
    public void AChangedMagicOrVersion_IsRejected()
    {
        byte[] file = Encrypt(Letter);

        byte[] badMagic = (byte[])file.Clone();
        badMagic[0] ^= 0x01;
        Assert.ThrowsExactly<InvalidDataException>(() => Decrypt(badMagic));

        byte[] badVersion = (byte[])file.Clone();
        badVersion[4] = 2;
        var ex = Assert.ThrowsExactly<InvalidDataException>(() => Decrypt(badVersion));
        Assert.Contains("format version 2", ex.Message);
    }

    [TestMethod]
    public void ATruncatedFile_IsDetected()
    {
        byte[] file = Encrypt(Letter);

        Assert.ThrowsExactly<CryptographicException>(() => Decrypt(file[..^1]));
        Assert.ThrowsExactly<InvalidDataException>(() => Decrypt(file[..(SecretLetterCrypto.Overhead - 1)]));
    }

    [TestMethod]
    public void PlainTextOrAnOldFormatFile_IsRejected()
    {
        var ex = Assert.ThrowsExactly<InvalidDataException>(() => Decrypt(Letter));
        Assert.Contains("not a SecretLetter file", ex.Message);

        Assert.ThrowsExactly<InvalidDataException>(() => Decrypt(Array.Empty<byte>()));
    }

    [TestMethod]
    public void Passphrase_IsUnicodeNormalized()
    {
        const string composed = "café au lait";    // é as one character
        const string decomposed = "café au lait"; // e followed by a combining accent

        byte[] file = SecretLetterCrypto.Encrypt(Letter, TestRootKey, composed);

        CollectionAssert.AreEqual(Letter, SecretLetterCrypto.Decrypt(file, TestRootKey, decomposed));
    }

    [TestMethod]
    public void Passphrase_IsNotTrimmed()
    {
        byte[] file = SecretLetterCrypto.Encrypt(Letter, TestRootKey, TestPassphrase + " ");

        Assert.ThrowsExactly<CryptographicException>(() => SecretLetterCrypto.Decrypt(file, TestRootKey, TestPassphrase));
    }

    [TestMethod]
    public void RootKey_LetterCaseAndSurroundingSpacesDoNotMatter()
    {
        byte[] file = SecretLetterCrypto.Encrypt(Letter, TestRootKey, TestPassphrase);

        CollectionAssert.AreEqual(Letter, SecretLetterCrypto.Decrypt(file, "  " + TestRootKey.ToLowerInvariant() + " ", TestPassphrase));
    }

    [TestMethod]
    public void InvalidRootKeyOrPassphrase_IsRefused()
    {
        Assert.ThrowsExactly<ArgumentException>(() => SecretLetterCrypto.Encrypt(Letter, "+1" + TestRootKey[2..], TestPassphrase));
        Assert.ThrowsExactly<ArgumentException>(() => SecretLetterCrypto.Encrypt(Letter, TestRootKey, "short"));
        Assert.ThrowsExactly<ArgumentException>(() => SecretLetterCrypto.Decrypt(Encrypt(Letter), TestRootKey[..^2], TestPassphrase));
    }

    [TestMethod]
    public void ALetterOverTheLimit_IsRefused()
    {
        byte[] tooLarge = new byte[SecretLetterCrypto.MaxLetterBytes + 1];

        Assert.ThrowsExactly<ArgumentException>(() => SecretLetterCrypto.Encrypt(tooLarge, TestRootKey, TestPassphrase));
    }

    [TestMethod]
    public void GenerateRootKey_Gives64UppercaseHexCharacters_DifferentEachTime()
    {
        string first = SecretLetterCrypto.GenerateRootKey();
        string second = SecretLetterCrypto.GenerateRootKey();

        Assert.AreEqual(64, first.Length);
        Assert.IsTrue(first.All(char.IsAsciiHexDigitUpper), first);
        Assert.IsNull(Validation.RootKeyError(first));
        Assert.AreNotEqual(first, second);
    }

    [TestMethod]
    public void EncryptFile_ThenDecryptFile_RestoresTheLetter()
    {
        using var folder = new TempFolder();
        string letter = folder.PathOf("letter.md");
        string encrypted = folder.PathOf("letter.file");
        string decrypted = folder.PathOf("letter-decrypted.md");
        File.WriteAllBytes(letter, Letter);

        SecretLetterCrypto.EncryptFile(letter, encrypted, TestRootKey, TestPassphrase);
        SecretLetterCrypto.DecryptFile(encrypted, decrypted, TestRootKey, TestPassphrase);

        CollectionAssert.AreEqual(Letter, File.ReadAllBytes(decrypted));
        CollectionAssert.AreEqual(new[] { "letter-decrypted.md", "letter.file", "letter.md" }, folder.FileNames());
    }

    [TestMethod]
    public void DecryptFile_WithTheWrongPassphrase_LeavesTheOutputUntouched()
    {
        using var folder = new TempFolder();
        string encrypted = folder.PathOf("letter.file");
        string output = folder.PathOf("letter.md");
        File.WriteAllBytes(encrypted, Encrypt(Letter));
        File.WriteAllText(output, "my original letter");

        Assert.ThrowsExactly<CryptographicException>(() => SecretLetterCrypto.DecryptFile(encrypted, output, TestRootKey, "a wrong passphrase"));

        Assert.AreEqual("my original letter", File.ReadAllText(output));
        CollectionAssert.AreEqual(new[] { "letter.file", "letter.md" }, folder.FileNames(), "A temporary file was left behind.");
    }

    [TestMethod]
    public void EncryptFile_ReplacesAnExistingOutput()
    {
        using var folder = new TempFolder();
        string letter = folder.PathOf("letter.md");
        string encrypted = folder.PathOf("letter.file");
        File.WriteAllBytes(letter, Letter);
        File.WriteAllText(encrypted, "an older encrypted letter");

        SecretLetterCrypto.EncryptFile(letter, encrypted, TestRootKey, TestPassphrase);

        CollectionAssert.AreEqual(Letter, Decrypt(File.ReadAllBytes(encrypted)));
        CollectionAssert.AreEqual(new[] { "letter.file", "letter.md" }, folder.FileNames());
    }

    [TestMethod]
    public void EncryptFile_RefusesTheSameFileAsInputAndOutput()
    {
        using var folder = new TempFolder();
        string letter = folder.PathOf("letter.md");
        File.WriteAllBytes(letter, Letter);

        Assert.ThrowsExactly<ArgumentException>(() => SecretLetterCrypto.EncryptFile(letter, letter, TestRootKey, TestPassphrase));
        CollectionAssert.AreEqual(Letter, File.ReadAllBytes(letter));
    }

    [TestMethod]
    public void EncryptFile_ReportsAMissingInputOrOutputFolder()
    {
        using var folder = new TempFolder();
        string letter = folder.PathOf("letter.md");

        Assert.ThrowsExactly<FileNotFoundException>(() => SecretLetterCrypto.EncryptFile(letter, folder.PathOf("letter.file"), TestRootKey, TestPassphrase));

        File.WriteAllBytes(letter, Letter);
        string missingFolder = Path.Combine(folder.FullPath, "missing", "letter.file");
        Assert.ThrowsExactly<DirectoryNotFoundException>(() => SecretLetterCrypto.EncryptFile(letter, missingFolder, TestRootKey, TestPassphrase));
    }

    [TestMethod]
    public void EncryptFile_RefusesALetterOverTheLimit_BeforeReadingIt()
    {
        using var folder = new TempFolder();
        string letter = folder.PathOf("huge.md");
        using (var stream = new FileStream(letter, FileMode.CreateNew))
        {
            stream.SetLength(SecretLetterCrypto.MaxLetterBytes + 1L);
        }

        var ex = Assert.ThrowsExactly<InvalidDataException>(() => SecretLetterCrypto.EncryptFile(letter, folder.PathOf("huge.file"), TestRootKey, TestPassphrase));
        Assert.Contains("64 MiB", ex.Message);
    }
}
