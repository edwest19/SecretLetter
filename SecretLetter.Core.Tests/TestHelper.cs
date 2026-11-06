// ============================================================================
// Copyright (c) 2026 Ed. Licensed under the MIT License (see LICENSE).
// AI-GENERATED CODE: This file was written by Claude (Anthropic).
// SecretLetter is a project built with AI-generated code; see README.md.
// ============================================================================
namespace SecretLetter.Core.Tests;

/// <summary>Shared test values and helpers.</summary>
internal static class TestHelper
{
    // TEST-ONLY root key and passphrase. Never use these for real letters.
    public const string TestRootKey = "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F";
    public const string OtherRootKey = "1F1E1D1C1B1A191817161514131211100F0E0D0C0B0A09080706050403020100";
    public const string TestPassphrase = "correct horse battery staple";

    /// <summary>Finds the repository root (the folder containing SecretLetter.slnx) from the test's output folder.</summary>
    public static string RepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "SecretLetter.slnx")))
            {
                return dir.FullName;
            }
        }
        throw new InvalidOperationException("Could not find SecretLetter.slnx above " + AppContext.BaseDirectory);
    }

    /// <summary>Reads a file in the repository, given its path parts from the root.</summary>
    public static string ReadRepoFile(params string[] path) =>
        File.ReadAllText(Path.Combine(new[] { RepoRoot() }.Concat(path).ToArray()));
}

/// <summary>A private temporary folder for one test, deleted afterwards.</summary>
internal sealed class TempFolder : IDisposable
{
    public TempFolder()
    {
        FullPath = Path.Combine(Path.GetTempPath(), "secretletter-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(FullPath);
    }

    public string FullPath { get; }

    /// <summary>The path of a file named <paramref name="name"/> in this folder.</summary>
    public string PathOf(string name) => Path.Combine(FullPath, name);

    /// <summary>The names of the files in this folder, sorted.</summary>
    public string[] FileNames() =>
        Directory.GetFiles(FullPath).Select(f => Path.GetFileName(f)).Order(StringComparer.Ordinal).ToArray();

    public void Dispose()
    {
        try
        {
            Directory.Delete(FullPath, recursive: true);
        }
        catch (IOException)
        {
            // A leftover temp folder is harmless.
        }
    }
}
