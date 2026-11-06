// ============================================================================
// Copyright (c) 2026 Ed. Licensed under the MIT License (see LICENSE).
// AI-GENERATED CODE: This file was written by Claude (Anthropic).
// SecretLetter is a project built with AI-generated code; see README.md.
// ============================================================================
using System.Text.Json;
using static SecretLetter.Core.Tests.TestHelper;

namespace SecretLetter.Core.Tests;

/// <summary>config.json and the remembered config folder (README "Configuration").</summary>
[TestClass]
public sealed class SettingsTests
{
    [TestMethod]
    public void Config_SavedThenLoaded_IsTheSame()
    {
        using var folder = new TempFolder();
        string path = AppConfig.PathIn(folder.FullPath);
        var saved = new AppConfig { InputPath = @"C:\Letters\a.md", OutputPath = @"C:\Letters\a.file", RootKey = TestRootKey };

        saved.Save(path);
        AppConfig loaded = AppConfig.Load(path);

        Assert.AreEqual(saved.InputPath, loaded.InputPath);
        Assert.AreEqual(saved.OutputPath, loaded.OutputPath);
        Assert.AreEqual(saved.RootKey, loaded.RootKey);
        CollectionAssert.AreEqual(new[] { "config.json" }, folder.FileNames(), "A temporary file was left behind.");
    }

    [TestMethod]
    public void Config_HoldsOnlyThePathsAndTheKey_NeverAPassphrase()
    {
        using var folder = new TempFolder();
        string path = AppConfig.PathIn(folder.FullPath);
        new AppConfig { RootKey = TestRootKey }.Save(path);

        using JsonDocument json = JsonDocument.Parse(File.ReadAllText(path));
        string[] names = json.RootElement.EnumerateObject().Select(p => p.Name).ToArray();

        CollectionAssert.AreEqual(new[] { "InputPath", "OutputPath", "RootKey" }, names);
    }

    [TestMethod]
    public void Config_WrittenByEarlierVersions_StillLoads()
    {
        // The window before version 1.0.0 wrote exactly these three settings.
        using var folder = new TempFolder();
        string path = AppConfig.PathIn(folder.FullPath);
        File.WriteAllText(path, "{\n  \"InputPath\": \"C:\\\\Letters\\\\a.md\",\n  \"OutputPath\": \"\",\n  \"RootKey\": \"" + TestRootKey + "\"\n}");

        AppConfig loaded = AppConfig.Load(path);

        Assert.AreEqual(@"C:\Letters\a.md", loaded.InputPath);
        Assert.AreEqual("", loaded.OutputPath);
        Assert.AreEqual(TestRootKey, loaded.RootKey);
    }

    [TestMethod]
    public void Config_MissingOrNullSettings_LoadAsEmpty()
    {
        using var folder = new TempFolder();
        string path = AppConfig.PathIn(folder.FullPath);

        File.WriteAllText(path, "{}");
        AppConfig empty = AppConfig.Load(path);
        Assert.AreEqual("", empty.InputPath);
        Assert.AreEqual("", empty.RootKey);

        File.WriteAllText(path, "{\"InputPath\": null, \"OutputPath\": null, \"RootKey\": null}");
        AppConfig nulls = AppConfig.Load(path);
        Assert.AreEqual("", nulls.InputPath);
        Assert.AreEqual("", nulls.OutputPath);
        Assert.AreEqual("", nulls.RootKey);
    }

    [TestMethod]
    public void Config_ThatIsNotJson_IsReportedClearly()
    {
        using var folder = new TempFolder();
        string path = AppConfig.PathIn(folder.FullPath);
        File.WriteAllText(path, "this is not json");

        var ex = Assert.ThrowsExactly<InvalidDataException>(() => AppConfig.Load(path));
        Assert.Contains("config.json is not valid", ex.Message);

        File.WriteAllText(path, "null");
        Assert.ThrowsExactly<InvalidDataException>(() => AppConfig.Load(path));
    }

    [TestMethod]
    public void UserSettings_RememberTheConfigFolder()
    {
        using var folder = new TempFolder();
        string path = Path.Combine(folder.FullPath, "SecretLetter", UserSettings.FileName);

        new UserSettings { LastConfigFolder = @"D:\Keys" }.Save(path);

        Assert.AreEqual(@"D:\Keys", UserSettings.Load(path).LastConfigFolder);
    }

    [TestMethod]
    public void UserSettings_MissingOrDamaged_LoadAsEmpty()
    {
        using var folder = new TempFolder();
        string path = folder.PathOf(UserSettings.FileName);

        Assert.AreEqual("", UserSettings.Load(path).LastConfigFolder);

        File.WriteAllText(path, "{ damaged");
        Assert.AreEqual("", UserSettings.Load(path).LastConfigFolder);

        File.WriteAllText(path, "null");
        Assert.AreEqual("", UserSettings.Load(path).LastConfigFolder);
    }

    [TestMethod]
    public void UserSettings_DefaultPath_IsInLocalAppData()
    {
        string expected = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SecretLetter", "settings.json");

        Assert.AreEqual(expected, UserSettings.DefaultPath);
    }
}
