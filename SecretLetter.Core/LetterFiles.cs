// ============================================================================
// Copyright (c) 2026 Ed. Licensed under the MIT License (see LICENSE).
// AI-GENERATED CODE: This file was written by Claude (Anthropic). It holds the
// file-name rules the window used to apply itself, so they can be tested.
// SecretLetter is a project built with AI-generated code; see README.md.
// ============================================================================
namespace SecretLetter.Core;

/// <summary>What SecretLetter does with an input file, decided by its extension.</summary>
public enum LetterOperation
{
    /// <summary>The input is neither a .md letter nor a .file.</summary>
    None,

    /// <summary>A .md letter is encrypted into a .file.</summary>
    Encrypt,

    /// <summary>A .file is decrypted into a .md letter.</summary>
    Decrypt,
}

/// <summary>
/// The file-name rules: a .md letter is encrypted into a .file, and a .file is decrypted into a .md letter.
/// </summary>
public static class LetterFiles
{
    /// <summary>A letter: a Markdown file.</summary>
    public const string LetterExtension = ".md";

    /// <summary>An encrypted letter.</summary>
    public const string EncryptedExtension = ".file";

    /// <summary>Encrypt for a .md input, Decrypt for a .file input (any letter case), otherwise None.</summary>
    public static LetterOperation OperationFor(string? inputPath)
    {
        string path = (inputPath ?? string.Empty).Trim();
        if (path.EndsWith(LetterExtension, StringComparison.OrdinalIgnoreCase))
        {
            return LetterOperation.Encrypt;
        }
        if (path.EndsWith(EncryptedExtension, StringComparison.OrdinalIgnoreCase))
        {
            return LetterOperation.Decrypt;
        }
        return LetterOperation.None;
    }

    /// <summary>The extension the output must have: .file when encrypting, .md when decrypting.</summary>
    public static string OutputExtensionFor(LetterOperation operation) => operation switch
    {
        LetterOperation.Encrypt => EncryptedExtension,
        LetterOperation.Decrypt => LetterExtension,
        _ => string.Empty,
    };

    /// <summary>
    /// The output file to offer for an input file. A current output that already has the right
    /// extension is kept; otherwise it is the input file with the other extension.
    /// (The output's extension always differs from the input's, so the two can't be the same file.)
    /// </summary>
    public static string SuggestOutputPath(string? inputPath, string? currentOutputPath)
    {
        string input = (inputPath ?? string.Empty).Trim();
        string current = (currentOutputPath ?? string.Empty).Trim();
        LetterOperation operation = OperationFor(input);
        if (operation == LetterOperation.None)
        {
            return current;
        }

        string extension = OutputExtensionFor(operation);
        if (current.Length > 0 && current.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
        {
            return current;
        }
        return Path.ChangeExtension(input, extension);
    }

    /// <summary>Returns null when the input and output paths can be used, or a message saying what's wrong.</summary>
    public static string? PathsError(string? inputPath, string? outputPath)
    {
        string input = (inputPath ?? string.Empty).Trim();
        string output = (outputPath ?? string.Empty).Trim();

        if (input.Length == 0)
        {
            return "Choose an input file.";
        }
        LetterOperation operation = OperationFor(input);
        if (operation == LetterOperation.None)
        {
            return "The input file must be a .md letter (to encrypt) or a .file (to decrypt).";
        }
        if (!File.Exists(input))
        {
            return $"The input file doesn't exist: {input}";
        }
        if (output.Length == 0)
        {
            return "Choose an output file.";
        }
        // The output's extension differs from the input's, so the output can't be the input file.
        if (!output.EndsWith(OutputExtensionFor(operation), StringComparison.OrdinalIgnoreCase))
        {
            return operation == LetterOperation.Encrypt
                ? "Encrypting makes a .file, so the output file must end in .file."
                : "Decrypting makes a .md letter, so the output file must end in .md.";
        }

        string? folder = FolderOf(output);
        if (folder is null || !Directory.Exists(folder))
        {
            return $"The output folder doesn't exist: {folder ?? output}";
        }
        return null;
    }

    private static string? FolderOf(string path)
    {
        try
        {
            return Path.GetDirectoryName(Path.GetFullPath(path));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }
}
