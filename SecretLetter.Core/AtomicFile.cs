// ============================================================================
// Copyright (c) 2026 Ed. Licensed under the MIT License (see LICENSE).
// AI-GENERATED CODE: This file was written by Claude (Anthropic).
// SecretLetter is a project built with AI-generated code; see README.md.
// ============================================================================
namespace SecretLetter.Core;

/// <summary>
/// Writes a file all at once: the bytes go to a temporary file in the same folder, which then
/// replaces the target. If anything fails, the target is left exactly as it was and the
/// temporary file is removed.
/// </summary>
internal static class AtomicFile
{
    /// <summary>Temporary files end with this, so a leftover one (after a power cut) is recognizable.</summary>
    internal const string TempSuffix = ".secretletter-tmp";

    public static void WriteAllBytes(string path, ReadOnlySpan<byte> bytes)
    {
        string fullPath = Path.GetFullPath(path);
        string folder = Path.GetDirectoryName(fullPath)
            ?? throw new ArgumentException($"Can't write to {path}: it has no folder.", nameof(path));
        if (!Directory.Exists(folder))
        {
            throw new DirectoryNotFoundException($"The folder doesn't exist: {folder}");
        }

        string tempPath = Path.Combine(folder, $"{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}{TempSuffix}");
        try
        {
            using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }
            File.Move(tempPath, fullPath, overwrite: true);
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // Best effort: the original error is the one worth reporting.
        }
        catch (UnauthorizedAccessException)
        {
            // Best effort, as above.
        }
    }
}
