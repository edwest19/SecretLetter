// ============================================================================
// Copyright (c) 2026 Ed. Licensed under the MIT License (see LICENSE).
// AI-GENERATED CODE: This file was written by Claude (Anthropic), following
// RepositoryRulesTests in edwest19/SecretKey.
// SecretLetter is a project built with AI-generated code; see README.md.
// ============================================================================
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static SecretLetter.Core.Tests.TestHelper;

namespace SecretLetter.Core.Tests;

/// <summary>
/// Enforces the project's ground rules (README "About this project", CLAUDE.md):
/// pure .NET with only Microsoft packages, and local-only app code with no network access.
/// If one of these fails, a change broke a rule; fix the change, don't loosen the test
/// without the owner's approval.
/// </summary>
[TestClass]
public sealed class RepositoryRulesTests
{
    // Every NuGet package the repo may use. All are published by Microsoft.
    private static readonly string[] AllowedPackages =
    {
        "Microsoft.WindowsAppSDK",          // WinUI 3 runtime for SecretLetter.UI
        "Microsoft.Windows.SDK.BuildTools", // Windows SDK build tools for SecretLetter.UI (MSIX packaging)
        "MSTest",                           // Microsoft's test framework (tests only)
    };

    // The app projects; the test project is left out because it contains these words itself.
    private static readonly string[] AppProjectFolders = { "SecretLetter.Core", "SecretLetter.UI" };

    [TestMethod]
    public void OnlyAllowedMicrosoftPackagesAreReferenced()
    {
        string root = RepoRoot();
        var used = new List<string>();

        foreach (string file in BuildFiles(root))
        {
            foreach (XElement reference in XDocument.Load(file).Descendants().Where(e => e.Name.LocalName == "PackageReference"))
            {
                string id = (string?)reference.Attribute("Include") ?? (string?)reference.Attribute("Update") ?? "";
                used.Add($"{Path.GetRelativePath(root, file)}: {id}");
                Assert.IsTrue(AllowedPackages.Contains(id), $"Package '{id}' in {Path.GetRelativePath(root, file)} is not on the allowed list.");
            }
        }

        Assert.IsNotEmpty(used, "Expected to find the project's package references.");
    }

    [TestMethod]
    public void AppCodeHasNoNetworkAccess()
    {
        var network = new Regex(@"\bSystem\.Net\b|\bHttpClient\b|\bWebClient\b|\bWebRequest\b|\bSockets?\b|\bTcpClient\b|\bUdpClient\b|\bWebView2?\b|\bWebSocket\b");
        string root = RepoRoot();
        int filesChecked = 0;

        foreach (string folder in AppProjectFolders)
        {
            foreach (string file in Directory.EnumerateFiles(Path.Combine(root, folder), "*.*", SearchOption.AllDirectories)
                         .Where(f => f.EndsWith(".cs") || f.EndsWith(".xaml"))
                         .Where(f => !IsBuildOutput(root, f)))
            {
                filesChecked++;
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    Assert.IsFalse(network.IsMatch(lines[i]),
                        $"Network API found in {Path.GetRelativePath(root, file)} line {i + 1}: {lines[i].Trim()}");
                }
            }
        }

        Assert.IsGreaterThan(0, filesChecked, "Expected to scan the app's source files.");
    }

    [TestMethod]
    public void AppManifestDeclaresOnlyRunFullTrust()
    {
        // No network, device or AI capabilities. runFullTrust is required for a WinUI 3 desktop app
        // packaged as full trust (README "Install").
        string manifest = Path.Combine(RepoRoot(), "SecretLetter.UI", "Package.appxmanifest");

        string[] capabilities = XDocument.Load(manifest).Descendants()
            .Where(e => e.Name.LocalName is "Capability" or "DeviceCapability")
            .Select(e => (string?)e.Attribute("Name") ?? "")
            .ToArray();

        CollectionAssert.AreEqual(new[] { "runFullTrust" }, capabilities);
    }

    [TestMethod]
    public void AiGeneratedFiles_SayWhoWroteThem()
    {
        // CLAUDE.md rule 4: every source file starts with a header naming the AI that wrote or changed it.
        string root = RepoRoot();
        string[] sources = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !IsBuildOutput(root, f))
            .ToArray();

        Assert.IsNotEmpty(sources);
        foreach (string file in sources)
        {
            string head = string.Join('\n', File.ReadLines(file).Take(10));
            Assert.Contains("AI-GENERATED CODE", head, $"{Path.GetRelativePath(root, file)} has no AI-generated header.");
        }
    }

    private static IEnumerable<string> BuildFiles(string root) =>
        Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".csproj") || f.EndsWith(".props") || f.EndsWith(".targets"))
            .Where(f => !IsBuildOutput(root, f));

    private static bool IsBuildOutput(string root, string file)
    {
        string[] parts = Path.GetRelativePath(root, file).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Any(p => p is "bin" or "obj" or ".git" or ".vs" or "TestResults");
    }
}
