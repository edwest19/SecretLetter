// ============================================================================
// Copyright (c) 2026 Ed. Licensed under the MIT License (see LICENSE).
// AI-GENERATED CODE: This file was written by Claude (Anthropic).
// SecretLetter is a project built with AI-generated code; see README.md.
// ============================================================================
namespace SecretLetter.Core.Tests;

/// <summary>The file-name rules (README "Using SecretLetter").</summary>
[TestClass]
public sealed class LetterFilesTests
{
    [TestMethod]
    public void Operation_IsDecidedByTheInputExtension()
    {
        Assert.AreEqual(LetterOperation.Encrypt, LetterFiles.OperationFor(@"letter.md"));
        Assert.AreEqual(LetterOperation.Encrypt, LetterFiles.OperationFor(@"LETTER.MD "));
        Assert.AreEqual(LetterOperation.Decrypt, LetterFiles.OperationFor(@"letter.file"));
        Assert.AreEqual(LetterOperation.Decrypt, LetterFiles.OperationFor(@"Letter.File"));
        Assert.AreEqual(LetterOperation.None, LetterFiles.OperationFor(@"letter.txt"));
        Assert.AreEqual(LetterOperation.None, LetterFiles.OperationFor(""));
        Assert.AreEqual(LetterOperation.None, LetterFiles.OperationFor(null));
    }

    [TestMethod]
    public void OutputExtension_IsTheOtherOne()
    {
        Assert.AreEqual(".file", LetterFiles.OutputExtensionFor(LetterOperation.Encrypt));
        Assert.AreEqual(".md", LetterFiles.OutputExtensionFor(LetterOperation.Decrypt));
        Assert.AreEqual("", LetterFiles.OutputExtensionFor(LetterOperation.None));
    }

    [TestMethod]
    public void SuggestedOutput_IsTheInputWithTheOtherExtension()
    {
        using var folder = new TempFolder();

        Assert.AreEqual(folder.PathOf("letter.file"), LetterFiles.SuggestOutputPath(folder.PathOf("letter.md"), ""));
        Assert.AreEqual(folder.PathOf("letter.md"), LetterFiles.SuggestOutputPath(folder.PathOf("letter.file"), ""));

        // A current output with the wrong extension (left from the other direction) is replaced.
        Assert.AreEqual(folder.PathOf("letter.md"), LetterFiles.SuggestOutputPath(folder.PathOf("letter.file"), folder.PathOf("letter.file")));
    }

    [TestMethod]
    public void SuggestedOutput_KeepsAChosenOutputWithTheRightExtension()
    {
        using var folder = new TempFolder();
        string chosen = Path.Combine(folder.FullPath, "sent", "for-alice.file");

        Assert.AreEqual(chosen, LetterFiles.SuggestOutputPath(folder.PathOf("letter.md"), chosen));
        Assert.AreEqual("unchanged", LetterFiles.SuggestOutputPath(folder.PathOf("notes.txt"), "unchanged"));
    }

    [TestMethod]
    public void PathsError_ExplainsEachProblem()
    {
        using var folder = new TempFolder();
        string letter = folder.PathOf("letter.md");
        string encrypted = folder.PathOf("letter.file");
        File.WriteAllText(letter, "# letter");
        File.WriteAllText(encrypted, "not really encrypted");
        File.WriteAllText(folder.PathOf("notes.txt"), "notes");

        Assert.Contains("Choose an input file", LetterFiles.PathsError("", encrypted) ?? "");
        Assert.Contains(".md letter (to encrypt) or a .file", LetterFiles.PathsError(folder.PathOf("notes.txt"), encrypted) ?? "");
        Assert.Contains("doesn't exist", LetterFiles.PathsError(folder.PathOf("missing.md"), encrypted) ?? "");
        Assert.Contains("Choose an output file", LetterFiles.PathsError(letter, " ") ?? "");
        Assert.Contains("must end in .file", LetterFiles.PathsError(letter, folder.PathOf("copy.md")) ?? "");
        Assert.Contains("must end in .md", LetterFiles.PathsError(encrypted, folder.PathOf("copy.file")) ?? "");
        Assert.Contains("output folder doesn't exist", LetterFiles.PathsError(letter, Path.Combine(folder.FullPath, "missing", "x.file")) ?? "");
    }

    [TestMethod]
    public void PathsError_IsNullForAUsablePair()
    {
        using var folder = new TempFolder();
        string letter = folder.PathOf("letter.md");
        string encrypted = folder.PathOf("letter.file");
        File.WriteAllText(letter, "# letter");
        File.WriteAllText(encrypted, "encrypted");

        Assert.IsNull(LetterFiles.PathsError(letter, encrypted));
        Assert.IsNull(LetterFiles.PathsError(encrypted, folder.PathOf("letter-decrypted.md")));
    }
}
