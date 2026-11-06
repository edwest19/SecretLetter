// ============================================================================
// Copyright (c) 2026 Ed. Licensed under the MIT License (see LICENSE).
// AI-GENERATED CODE: This file was written by Claude (Anthropic). It replaces the
// original AppConfig and ConfigManager, which the app never used (the window kept
// its own copy of the settings).
// SecretLetter is a project built with AI-generated code; see README.md.
// ============================================================================
using System.Text;
using System.Text.Json;

namespace SecretLetter.Core;

/// <summary>
/// The settings in config.json, in the config folder the user chooses.
/// SECURITY-SENSITIVE: config.json holds the root key in plain text. It never holds the passphrase.
/// The property names are the ones the app has always written, so existing config.json files still load.
/// </summary>
public sealed class AppConfig
{
    /// <summary>The settings file's name inside the config folder.</summary>
    public const string FileName = "config.json";

    private static readonly JsonSerializerOptions ReadOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    /// <summary>The last input file (a .md letter or a .file).</summary>
    public string InputPath { get; set; } = string.Empty;

    /// <summary>The last output file.</summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>The root key: 64 hexadecimal characters.</summary>
    public string RootKey { get; set; } = string.Empty;

    /// <summary>The path of config.json inside <paramref name="folder"/>.</summary>
    public static string PathIn(string folder) => Path.Combine(folder, FileName);

    /// <summary>Reads config.json. Missing settings come back empty.</summary>
    /// <exception cref="InvalidDataException">The file is not valid config.json.</exception>
    public static AppConfig Load(string path)
    {
        string json = File.ReadAllText(path);
        AppConfig? config;
        try
        {
            config = JsonSerializer.Deserialize<AppConfig>(json, ReadOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"{Path.GetFileName(path)} is not valid: {ex.Message}", ex);
        }
        if (config is null)
        {
            throw new InvalidDataException($"{Path.GetFileName(path)} is empty.");
        }

        // JSON null values come through as null; treat them as empty.
        config.InputPath ??= string.Empty;
        config.OutputPath ??= string.Empty;
        config.RootKey ??= string.Empty;
        return config;
    }

    /// <summary>Writes config.json all at once, replacing any existing file.</summary>
    public void Save(string path)
    {
        string json = JsonSerializer.Serialize(this, WriteOptions);
        AtomicFile.WriteAllBytes(path, Encoding.UTF8.GetBytes(json));
    }
}
