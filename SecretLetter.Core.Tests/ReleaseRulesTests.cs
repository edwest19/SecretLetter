// ============================================================================
// Copyright (c) 2026 Ed. Licensed under the MIT License (see LICENSE).
// AI-GENERATED CODE: This file was written by Claude (Anthropic), following
// ReleaseRulesTests in edwest19/SecretKey.
// SecretLetter is a project built with AI-generated code; see README.md.
// ============================================================================
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static SecretLetter.Core.Tests.TestHelper;

namespace SecretLetter.Core.Tests;

/// <summary>
/// Keeps the release settings consistent (README "Releases"), so a broken release is found in a
/// pull request instead of after a tag is pushed: the MSIX publisher, the version in three places,
/// the changelog, the signing subject, pinned workflow actions, and the solution file.
/// </summary>
[TestClass]
public sealed class ReleaseRulesTests
{
    private static XElement ManifestIdentity() =>
        XDocument.Parse(ReadRepoFile("SecretLetter.UI", "Package.appxmanifest")).Descendants().Single(e => e.Name.LocalName == "Identity");

    private static string ProjectVersion() =>
        XDocument.Parse(ReadRepoFile("Directory.Build.props")).Descendants("Version").Single().Value;

    [TestMethod]
    public void ManifestPublisher_IsTheNameReleasesAreSignedAs()
    {
        Match signer = Regex.Match(ReadRepoFile(".github", "workflows", "release.yml"), @"EXPECTED_SIGNER:\s*'([^']+)'");
        Assert.IsTrue(signer.Success, "release.yml must set EXPECTED_SIGNER.");

        // Case-sensitive: Windows refuses to sign an MSIX whose Publisher differs from the certificate subject.
        Assert.AreEqual(signer.Groups[1].Value, (string?)ManifestIdentity().Attribute("Publisher"));
    }

    [TestMethod]
    public void Readme_ReleaseSection_MatchesTheWorkflow()
    {
        string workflow = ReadRepoFile(".github", "workflows", "release.yml");
        string readme = ReadRepoFile("README.md");
        string Setting(string name) => Regex.Match(workflow, name + @":\s*'?([^'\r\n]+?)'?\s*$", RegexOptions.Multiline).Groups[1].Value;

        Assert.Contains($"made by `{Setting("EXPECTED_SIGNER")}`", readme);
        Assert.Contains($"(account `{Setting("signing-account-name")}`, profile `{Setting("certificate-profile-name")}`, East US)", readme);
        Assert.Contains("https://eus.codesigning.azure.net/", workflow, "README says East US; the endpoint must be East US's.");
    }

    [TestMethod]
    public void SigningSubject_IsStatedTheSameEverywhere_ForTheReleaseEnvironment()
    {
        // The Azure federated credential trusts exactly this immutable OIDC subject.
        var subject = new Regex(@"repo:[^@\s/]+@\d+/[^@\s:]+@\d+:environment:[A-Za-z0-9_-]+");
        string workflow = ReadRepoFile(".github", "workflows", "release.yml");
        string[] stated = new[] { ReadRepoFile("README.md"), ReadRepoFile("CLAUDE.md"), workflow }
            .Select(text => subject.Match(text).Value).ToArray();

        Assert.IsTrue(stated.All(s => s.Length > 0), "README.md, CLAUDE.md and release.yml must each state the signing subject.");
        Assert.HasCount(1, stated.Distinct(), $"The signing subject differs between files: {string.Join(" | ", stated)}");
        Assert.Contains("/SecretLetter@1279392996:", stated[0], "The subject must name this repository by its ID.");

        string environment = Regex.Match(workflow, @"^\s*environment:\s*(\S+)\s*$", RegexOptions.Multiline).Groups[1].Value;
        Assert.IsTrue(stated[0].EndsWith(":environment:" + environment, StringComparison.Ordinal),
            $"The subject {stated[0]} must name the sign job's environment '{environment}'.");
    }

    [TestMethod]
    public void ManifestVersion_IsTheProjectVersionPlusZero()
    {
        string version = ProjectVersion();

        Assert.IsTrue(Regex.IsMatch(version, @"^\d+\.\d+\.\d+$"), $"Directory.Build.props Version '{version}' must be X.Y.Z.");
        Assert.AreEqual(version + ".0", (string?)ManifestIdentity().Attribute("Version"));
    }

    [TestMethod]
    public void Changelog_HasAnEntryForTheProjectVersion()
    {
        string version = ProjectVersion();
        string[] lines = ReadRepoFile("CHANGELOG.md").Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
        var heading = new Regex(@"^## \[" + Regex.Escape(version) + @"\] - \d{4}-\d{2}-\d{2}\s*$");

        int[] starts = Enumerable.Range(0, lines.Length).Where(i => heading.IsMatch(lines[i])).ToArray();
        Assert.HasCount(1, starts, $"CHANGELOG.md needs exactly one heading '## [{version}] - YYYY-MM-DD'.");

        IEnumerable<string> notes = lines.Skip(starts[0] + 1).TakeWhile(l => !l.StartsWith("## [", StringComparison.Ordinal));
        Assert.IsTrue(notes.Any(l => !string.IsNullOrWhiteSpace(l)), $"The CHANGELOG.md entry for {version} is empty.");
    }

    [TestMethod]
    public void WorkflowActions_ArePinnedToExactCommits()
    {
        var uses = new Regex(@"^\s*(?:-\s*)?uses:\s*(\S+)");
        var pinned = new Regex(@"^[A-Za-z0-9_.-]+/[A-Za-z0-9_./-]+@[0-9a-f]{40}$");
        int checkedCount = 0;

        foreach (string file in Directory.EnumerateFiles(Path.Combine(RepoRoot(), ".github", "workflows"), "*.yml"))
        {
            foreach (string line in File.ReadLines(file))
            {
                Match match = uses.Match(line);
                if (!match.Success)
                {
                    continue;
                }
                checkedCount++;
                Assert.IsTrue(pinned.IsMatch(match.Groups[1].Value),
                    $"{Path.GetFileName(file)}: '{match.Groups[1].Value}' must be pinned to a full commit SHA.");
            }
        }

        Assert.IsGreaterThan(0, checkedCount, "Expected to find actions in the workflows.");
    }

    [TestMethod]
    public void SolutionFile_ListsOnlyFilesThatExist()
    {
        XDocument solution = XDocument.Parse(ReadRepoFile("SecretLetter.slnx"));
        string[] paths = solution.Descendants()
            .Where(e => e.Name.LocalName is "File" or "Project")
            .Select(e => (string?)e.Attribute("Path") ?? "")
            .ToArray();

        Assert.IsNotEmpty(paths);
        foreach (string path in paths)
        {
            Assert.IsTrue(File.Exists(Path.Combine(RepoRoot(), path)), $"SecretLetter.slnx lists '{path}', which doesn't exist.");
        }
    }
}
