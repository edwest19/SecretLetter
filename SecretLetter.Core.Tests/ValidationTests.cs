// ============================================================================
// Copyright (c) 2026 Ed. Licensed under the MIT License (see LICENSE).
// AI-GENERATED CODE: This file was written by Claude (Anthropic).
// SecretLetter is a project built with AI-generated code; see README.md.
// ============================================================================
using static SecretLetter.Core.Tests.TestHelper;

namespace SecretLetter.Core.Tests;

/// <summary>The checks on the key and passphrase (README "Key and passphrase").</summary>
[TestClass]
public sealed class ValidationTests
{
    [TestMethod]
    public void RootKey_ValidForms_AreAccepted()
    {
        string[] valid =
        {
            TestRootKey,
            TestRootKey.ToLowerInvariant(),
            "  " + TestRootKey + "\t",
        };

        foreach (string key in valid)
        {
            Assert.IsNull(Validation.RootKeyError(key), $"'{key}' should be accepted.");
        }
    }

    [TestMethod]
    public void RootKey_InvalidForms_AreRejected()
    {
        string[] invalid =
        {
            "",
            "   ",
            TestRootKey[..^1],                    // 63 characters
            TestRootKey + "0",                    // 65 characters
            "ZZ" + TestRootKey[2..],              // not hexadecimal
            "+1" + TestRootKey[2..],              // the original parser read "+1" as the byte 01
            "0x" + TestRootKey[2..],              // no prefixes
            TestRootKey[..32] + " " + TestRootKey[33..], // no spaces inside
        };

        foreach (string key in invalid)
        {
            Assert.IsNotNull(Validation.RootKeyError(key), $"'{key}' should be rejected.");
        }
        Assert.IsNotNull(Validation.RootKeyError(null));
    }

    [TestMethod]
    public void Passphrase_MustBeAtLeastTheMinimumLength()
    {
        Assert.AreEqual(8, Validation.MinPassphraseLength);
        Assert.IsNotNull(Validation.PassphraseError(null));
        Assert.IsNotNull(Validation.PassphraseError(""));
        Assert.IsNotNull(Validation.PassphraseError("1234567"));
        Assert.IsNull(Validation.PassphraseError("12345678"));
        Assert.IsNull(Validation.PassphraseError(TestPassphrase));
    }

    [TestMethod]
    public void Passphrase_OnlySpaces_IsRejected()
    {
        Assert.IsNotNull(Validation.PassphraseError("          "));
        Assert.IsNull(Validation.PassphraseError("  spaces   inside  "));
    }

    [TestMethod]
    public void Passphrase_WithBrokenUnicode_IsRejected()
    {
        // A lone surrogate is not a character and can't be normalized.
        Assert.IsNotNull(Validation.PassphraseError("passphrase\uD800"));
    }
}
