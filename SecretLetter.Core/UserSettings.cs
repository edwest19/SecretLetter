// ============================================================================
// Copyright (c) 2026 Ed. Licensed under the MIT License (see LICENSE).
// AI-GENERATED CODE: This file was written by Claude (Anthropic).
// SecretLetter is a project built with AI-generated code; see README.md.
// ============================================================================
using System.Text;
using System.Text.Json;

namespace SecretLetter.Core;

/// <summary>
/// Remembers which config folder was used last, so the app can reopen it at start-up.
/// It holds only that folder path: never a key, a passphrase or a letter.
/// Losing or damaging this file is harmless; the app then simply asks for the folder again.
/// </summary>
public sealed class UserSettings
{
    /// <summary>The settings file's name.</summary>
    public const string FileName = "settings.json";

    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    /// <summary>The config folder used last, or empty.</summary>
    public string LastConfigFolder { get; set; } = string.Empty;

    /// <summary>
    /// %LOCALAPPDATA%\SecretLetter\settings.json. When the app is installed from its MSIX,
    /// Windows keeps this inside the app's own storage and removes it on uninstall.
    /// </summary>
    public static string DefaultPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SecretLetter", FileName);

    /// <summary>Reads the settings. A missing, unreadable or damaged file gives empty settings.</summary>
    public static UserSettings Load(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return new UserSettings();
            }
            UserSettings? settings = JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(path));
            if (settings is null)
            {
                return new UserSettings();
            }
            settings.LastConfigFolder ??= string.Empty;
            return settings;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return new UserSettings();
        }
    }

    /// <summary>Writes the settings, creating the folder if needed.</summary>
    public void Save(string path)
    {
        string? folder = Path.GetDirectoryName(Path.GetFullPath(path));
        if (folder is not null)
        {
            Directory.CreateDirectory(folder);
        }
        AtomicFile.WriteAllBytes(path, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this, WriteOptions)));
    }
}
