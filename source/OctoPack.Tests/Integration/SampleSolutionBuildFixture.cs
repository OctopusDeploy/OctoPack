using System.Collections.Generic;
using System.IO;
using NuGet.Versioning;
using NUnit.Framework;

namespace OctoPack.Tests.Integration
{
    [TestFixture]
    public class SampleSolutionBuildFixture : BuildFixture
    {
        [Test]
        public void ShouldBuildAtSolutionLevel()
        {
            MsBuild("Samples.sln /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.9 /p:Configuration=Release /v:m");

            AssertPackage(@"Sample.ConsoleApp\obj\octopacked\Sample.ConsoleApp.1.0.9.nupkg", 
                pkg => pkg.AssertContents(
                    "Sample.ConsoleApp.exe",
                    "Sample.ConsoleApp.exe.config",
                    "Sample.ConsoleApp.pdb"));

            AssertPackage(@"Sample.WebApp\obj\octopacked\Sample.WebApp.1.0.9.nupkg",
                pkg => pkg.AssertContents(
                    "bin\\*.dll",
                    "bin\\*.xml",
                    "bin\\Sample.WebApp.dll",
                    "bin\\Sample.WebApp.pdb",
                    "Content\\*.css",
                    "Content\\*.png",
                    "Content\\LinkedFile.txt",
                    "Scripts\\*.js",
                    "Views\\Web.config",
                    "Views\\*.cshtml",
                    "Global.asax",
                    "Web.config",
                    "Web.Release.config",
                    "Web.Debug.config"));

            AssertPackage(@"Sample.WebAppWithSpec\obj\octopacked\Sample.WebAppWithSpec.1.0.9.nupkg",
                pkg => pkg.AssertContents(
                    "bin\\*.dll",
                    "bin\\*.xml",
                    "bin\\Sample.WebAppWithSpec.dll",
                    "bin\\Sample.WebAppWithSpec.pdb",
                    "Content\\*.css",
                    "Content\\*.png",
                    "Content\\LinkedFile.txt",
                    "Scripts\\*.js",
                    "Views\\Web.config",
                    "Views\\*.cshtml",
                    "Views\\Deploy.ps1",
                    "Deploy.ps1",
                    "Global.asax",
                    "Web.config",
                    "Web.Release.config",
                    "Web.Debug.config"));
        }

        [Test]
        public void ShouldBuildAtProjectLevel()
        {
            MsBuild("Sample.ConsoleApp\\Sample.ConsoleApp.csproj /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.10 /p:Configuration=Release /v:m");

            AssertPackage(@"Sample.ConsoleApp\obj\octopacked\Sample.ConsoleApp.1.0.10.nupkg",
                pkg => pkg.AssertContents(
                    "Sample.ConsoleApp.exe",
                    "Sample.ConsoleApp.exe.config",
                    "Sample.ConsoleApp.pdb"));
        }

        [Test]
        [TestCase("1.0.0", "1.0.0", "1.0.0")]
        [TestCase("1.0.10-alpha.1+SHA.ANYTHING", "1.0.10-alpha.1+SHA.ANYTHING", "1.0.10-alpha.1+SHA.ANYTHING", Description = "Should support SemVer 2.0 out of the box")]
        [TestCase("2.0.0.0", "2.0.0.0", "2.0.0.0", Description = "We should rename the file to match the version, maintaining the fourth digit of the version.")]
        [TestCase("2016.03.02.01", "2016.03.02.01", "2016.03.02.01", Description = "We should rename the file to match the version, maintaining the leading zeros of the version.")]
        [TestCase("2016.03.02.01-beta.1+SHA.ANYTHING", "2016.03.02.01-beta.1+SHA.ANYTHING", "2016.03.02.01-beta.1+SHA.ANYTHING", Description = "We should rename the file to match the version, maintaining the leading zeros of the version.")]
        public void ShouldTreatVersionsCorrectly(string version, string expectedFileVersion, string expectedMetadataVersion)
        {
            MsBuild($"Sample.ConsoleApp\\Sample.ConsoleApp.csproj /p:RunOctoPack=true /p:OctoPackPackageVersion={version} /p:Configuration=Release /v:m");

            AssertPackage($"Sample.ConsoleApp\\obj\\octopacked\\Sample.ConsoleApp.{expectedFileVersion}.nupkg",
                pkg => pkg.AssertVersion(expectedMetadataVersion));
        }

        [Test]
        public void ShouldPreferAssemblyVersionOverAssemblyFileVersion()
        {
            MsBuild("Sample.ConsoleApp\\Sample.ConsoleApp.csproj /p:RunOctoPack=true /p:Configuration=Release /v:m");

            AssertPackage(@"Sample.ConsoleApp\obj\octopacked\Sample.ConsoleApp.2.1.0.1.nupkg",
                pkg => pkg.AssertContents(
                    "Sample.ConsoleApp.exe",
                    "Sample.ConsoleApp.exe.config",
                    "Sample.ConsoleApp.pdb"));
        }

        [Test]
        public void ShouldUseAssemblyFileVersionWhenForced()
        {
            MsBuild("Sample.ConsoleApp\\Sample.ConsoleApp.csproj /p:RunOctoPack=true /p:OctoPackUseFileVersion=true /p:Configuration=Release /v:m");

            AssertPackage(@"Sample.ConsoleApp\obj\octopacked\Sample.ConsoleApp.2.3.0.1.nupkg",
                pkg => pkg.AssertContents(
                    "Sample.ConsoleApp.exe",
                    "Sample.ConsoleApp.exe.config",
                    "Sample.ConsoleApp.pdb"));
        }


        [Test]
        public void ShouldPreferAssemblyInfoVersionOverAssemblyVersion()
        {
            MsBuild("Sample.WebApp\\Sample.WebApp.csproj /p:RunOctoPack=true /p:Configuration=Release /v:m");

            AssertPackage(@"Sample.WebApp\obj\octopacked\Sample.WebApp.3.1.0-dev.nupkg",
                pkg => pkg.AssertContents(
                    "*"));
        }

        [Test]
        public void ShouldPreferGitVersionTheMost()
        {
            MsBuild("Sample.ConsoleAppWithGitVersion\\Sample.ConsoleAppWithGitVersion.csproj /p:RunOctoPack=true /p:Configuration=Release /v:m");

            AssertPackage(@"Sample.ConsoleAppWithGitVersion\obj\octopacked\Sample.ConsoleAppWithGitVersion.1.4.0.nupkg",
                pkg => pkg.AssertContents(
                    "Sample.ConsoleAppWithGitVersion.exe",
                    "Sample.ConsoleAppWithGitVersion.exe.config",
                    "Sample.ConsoleAppWithGitVersion.pdb"));
        }

        [Test]
        public void ShouldFindNamespacedGitVersion()
        {
            MsBuild("Sample.ConsoleAppWithNamespacedGitVersion\\Sample.ConsoleAppWithNamespacedGitVersion.csproj /p:RunOctoPack=true /p:Configuration=Release /v:m");

            AssertPackage(@"Sample.ConsoleAppWithNamespacedGitVersion\obj\octopacked\Sample.ConsoleAppWithNamespacedGitVersion.1.4.0.nupkg",
                pkg => pkg.AssertContents(
                    "Sample.ConsoleAppWithNamespacedGitVersion.exe",
                    "Sample.ConsoleAppWithNamespacedGitVersion.exe.config",
                    "Sample.ConsoleAppWithNamespacedGitVersion.pdb"));
        }

        [Test]
        public void ShouldBuildWithSpecAndAssemblyInformationalVersion()
        {
            MsBuild("Sample.WebAppWithSpec\\Sample.WebAppWithSpec.csproj /p:RunOctoPack=true /p:Configuration=Release /v:m");

            AssertPackage(@"Sample.WebAppWithSpec\obj\octopacked\Sample.WebAppWithSpec.1.0.13-demo.nupkg",
                pkg => pkg.AssertTitle("Sample application"));
        }

        [Test]
        public void ShouldWarnAboutNonRootScripts()
        {
            MsBuild("Sample.WebAppWithSpec\\Sample.WebAppWithSpec.csproj /p:RunOctoPack=true /p:Configuration=Release /v:m",
                output => Assert.That(output, Is.StringContaining("OctoPack warning OCTNONROOT: As of Octopus Deploy 2.4, PowerShell scripts that are not at the root of the package will not be executed. The script 'Views\\Deploy.ps1' lives")));
        }

        [Test]
        public void ShouldNotWarnAboutNonRootScriptsWhenSuppressed()
        {
            MsBuild("Sample.WebAppWithSpec\\Sample.WebAppWithSpec.csproj /p:RunOctoPack=true /p:OctoPackIgnoreNonRootScripts=true /p:Configuration=Release /v:m",
                output => Assert.That(output, Is.Not.StringContaining("OctoPack warning OCTNONROOT: As of Octopus Deploy 2.4, PowerShell scripts that are not at the root of the package will not be executed. The script 'Views\\Deploy.ps1' lives")));
        }

        [Test]
        public void ShouldSupportWeirdTeamCityStuff()
        {
            File.Copy("Sample.ConsoleApp\\Sample.ConsoleApp.csproj", "Sample.ConsoleApp\\Sample.ConsoleApp.csproj.teamcity", true);

            MsBuild("Sample.ConsoleApp\\Sample.ConsoleApp.csproj.teamcity /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.10 /p:Configuration=Release /v:m");

            AssertPackage(@"Sample.ConsoleApp\obj\octopacked\Sample.ConsoleApp.1.0.10.nupkg",
                pkg => pkg.AssertContents(
                    "Sample.ConsoleApp.exe",
                    "Sample.ConsoleApp.exe.config",
                    "Sample.ConsoleApp.pdb"));
        }

        [Test]
        public void ShouldReportOutDirPackageLocationToTeamCity()
        {
            MsBuild("Sample.ConsoleApp\\Sample.ConsoleApp.csproj.teamcity /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.10 /p:OctoPackPublishPackagesToTeamCity=true /p:Configuration=Release /v:m",
                output => Assert.That(output, Is.StringMatching(@"##teamcity\[publishArtifacts .*\\bin\\Release\\Sample.ConsoleApp.1.0.10.nupkg'\]")),
                environmentVariables: new Dictionary<string, string> { { "TEAMCITY_VERSION", "10.0" } });
        }

        [Test]
        public void ShouldCopyToFileShare()
        {
            File.WriteAllText("Notes.txt", "Hello world!");

            MsBuild("Samples.sln /p:RunOctoPack=true /p:OctoPackPackageVersion=1.1.5 /p:OctoPackPublishPackageToFileShare=..\\Packages /p:Configuration=Release /v:m");

            AssertPackage(@"Packages\Sample.ConsoleApp.1.1.5.nupkg", pkg => Assert.That(pkg.GetIdentity().Version.ToString(), Is.EqualTo("1.1.5")));
            AssertPackage(@"Packages\Sample.WebApp.1.1.5.nupkg", pkg => Assert.That(pkg.GetIdentity().Version.ToString(), Is.EqualTo("1.1.5")));
            AssertPackage(@"Packages\Sample.WebAppWithSpec.1.1.5.nupkg", pkg => Assert.That(pkg.GetIdentity().Version.ToString(), Is.EqualTo("1.1.5")));
        }

        [Test]
        public void ShouldAddReleaseNotes()
        {
            File.WriteAllText("Notes.txt", "Hello world!");

            MsBuild("Samples.sln /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.9 /p:OctoPackReleaseNotesFile=..\\Notes.txt /p:Configuration=Release /v:m");

            AssertPackage(@"Sample.ConsoleApp\obj\octopacked\Sample.ConsoleApp.1.0.9.nupkg",
                pkg => pkg.AssertReleaseNotes("Hello world!"));
        }

        [Test]
        public void ShouldIncludeTypeScriptSourcesWhenSpecified()
        {
            MsBuild("Sample.TypeScriptApp\\Sample.TypeScriptApp.csproj /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.9 /p:Configuration=Release /p:OctoPackIncludeTypeScriptSourceFiles=True /v:d");

            AssertPackage(@"Sample.TypeScriptApp\obj\octopacked\Sample.TypeScriptApp.1.0.9.nupkg",
                pkg => pkg.AssertContents(
                    "bin\\*.dll",
                    "bin\\*.xml",
                    "bin\\Sample.TypeScriptApp.dll",
                    "bin\\Sample.TypeScriptApp.pdb",
                    "Scripts\\MyTypedScript.js",
                    "Scripts\\MyTypedScript.ts",
                    "Scripts\\MyTypedScript.UI.ts",
                    "Scripts\\MyTypedScript.UI.js",
                    "Views\\Web.config",
                    "Global.asax",
                    "Web.config"));
        }

        [Test]
        public void ShouldIncludeTypeScriptOutputs()
        {
            MsBuild("Sample.TypeScriptApp\\Sample.TypeScriptApp.csproj /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.9 /p:Configuration=Release /v:d");

            AssertPackage(@"Sample.TypeScriptApp\obj\octopacked\Sample.TypeScriptApp.1.0.9.nupkg",
                pkg => pkg.AssertContents(
                    "bin\\*.dll",
                    "bin\\*.xml",
                    "bin\\Sample.TypeScriptApp.dll",
                    "bin\\Sample.TypeScriptApp.pdb",
                    "Scripts\\MyTypedScript.js",
                    "Scripts\\MyTypedScript.UI.js",
                    "Views\\Web.config",
                    "Global.asax",
                    "Web.config"));
        }

        [Test]
        public void ShouldAddLinkedFiles()
        {
            MsBuild("Sample.WebApp\\Sample.WebApp.csproj /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.9 /p:Configuration=Release");

            AssertPackage(@"Sample.WebApp\obj\octopacked\Sample.WebApp.1.0.9.nupkg",
                pkg => pkg.AssertContents(
                    "bin\\*.dll",
                    "bin\\*.xml",
                    "bin\\Sample.WebApp.dll",
                    "bin\\Sample.WebApp.pdb",
                    "Content\\*.css",
                    "Content\\*.png",
                    "Content\\LinkedFile.txt",
                    "Scripts\\*.js",
                    "Views\\Web.config",
                    "Views\\*.cshtml",
                    "Global.asax",
                    "Web.config",
                    "Web.Release.config",
                    "Web.Debug.config"));
        }

        [Test]
        public void ShouldAddLinkedWebConfigFiles()
        {
            MsBuild("Sample.WebAppWithLinkedWebConfig\\Sample.WebAppWithLinkedWebConfig.csproj /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.9 /p:Configuration=Release");

            AssertPackage(@"Sample.WebAppWithLinkedWebConfig\obj\octopacked\Sample.WebAppWithLinkedWebConfig.1.0.9.nupkg",
                pkg => pkg.AssertContents(
                    "bin\\*.dll",
                    "bin\\*.xml",
                    "bin\\Sample.WebApp.dll",
                    "bin\\Sample.WebApp.pdb",
                    "Content\\*.css",
                    "Content\\*.png",
                    "Content\\LinkedFile.txt",
                    "Scripts\\*.js",
                    "Views\\Web.config",
                    "Views\\*.cshtml",
                    "Global.asax",
                    "Web.config",
                    "Web.Release.config",
                    "Web.Debug.config"));
        }

        [Test]
        public void ShouldNotBuildAsWebAppWhenLinkIsInSubfolder()
        {
            MsBuild("Sample.WebAppWithLinkedWebConfigInSubfolder\\Sample.WebAppWithLinkedWebConfigInSubfolder.csproj /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.9 /p:Configuration=Release");

            AssertPackage(
                @"Sample.WebAppWithLinkedWebConfigInSubfolder\obj\octopacked\Sample.WebAppWithLinkedWebConfigInSubfolder.1.0.9.nupkg",
                pkg => pkg.AssertContents(
                    "*.dll",
                    "*.xml",
                    "Sample.WebApp.dll",
                    "Sample.WebApp.pdb"));
        }

        [Test]
        public void ShouldWarnAboutSourceFilesThatDoNotExist()
        {
            MsBuild("Sample.WebAppWithMissingSourceFile\\Sample.WebAppWithMissingSourceFile.csproj /p:RunOctoPack=true /p:Configuration=Release /v:m",
                output => Assert.That(output, Is.StringMatching("OctoPack warning OCTNOENT: The source file '.*\\\\source\\\\Samples\\\\NonExistant\\\\LinkedFile.txt' does not exist, so it will not be included in the package")));
        }

        [Test]
        public void ShouldAllowCustomFilesSection()
        {
            MsBuild("Sample.WebAppWithSpecAndCustomContent\\Sample.WebAppWithSpecAndCustomContent.csproj /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.11 /p:Configuration=Release");

            AssertPackage(@"Sample.WebAppWithSpecAndCustomContent\obj\octopacked\Sample.WebAppWithSpecAndCustomContent.1.0.11.nupkg",
                pkg => pkg.AssertContents(
                    "bin\\Sample.WebAppWithSpecAndCustomContent.dll",
                    "SomeFiles\\Foo.css"));
        }

        [Test]
        public void ShouldBundleLinkedAppConfigFiles()
        {
            MsBuild("Sample.ConsoleApp\\Sample.ConsoleApp.csproj /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.9 /p:Configuration=Release /v:m");

            AssertPackage(@"Sample.ConsoleApp\obj\octopacked\Sample.ConsoleApp.1.0.9.nupkg",
                pkg => pkg.AssertContents(
                    "Sample.ConsoleApp.exe",
                    "Sample.ConsoleApp.exe.config",
                    "Sample.ConsoleApp.pdb"));
        }

        [Test]
        public void ShouldAllowCustomFilesSectionWhenEnforced()
        {
            MsBuild("Sample.WebAppWithSpecAndCustomContentEnforced\\Sample.WebAppWithSpecAndCustomContentEnforced.csproj /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.11 /p:Configuration=Release /p:OctoPackEnforceAddingFiles=true");

            AssertPackage(@"Sample.WebAppWithSpecAndCustomContentEnforced\obj\octopacked\Sample.WebAppWithSpecAndCustomContentEnforced.1.0.11.nupkg",
                pkg => pkg.AssertContents(
                    "bin\\Sample.WebAppWithSpecAndCustomContentEnforced.dll",
                    "bin\\Sample.WebAppWithSpecAndCustomContentEnforced.pdb",
                    "web.config",
                    "SomeFiles\\Foo.css"));
        }

        [Test]
        public void ShouldAllowAppendingValueToPackageId()
        {
            MsBuild("Samples.sln /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.9 /p:OctoPackAppendToPackageId=Foo /p:Configuration=Release /v:m");

            AssertPackage(@"Sample.ConsoleApp\obj\octopacked\Sample.ConsoleApp.Foo.1.0.9.nupkg",
                pkg => pkg.AssertContents(
                    "Sample.ConsoleApp.exe",
                    "Sample.ConsoleApp.exe.config",
                    "Sample.ConsoleApp.pdb"));

            AssertPackage(@"Sample.WebApp\obj\octopacked\Sample.WebApp.Foo.1.0.9.nupkg",
                pkg => pkg.AssertContents(
                    "bin\\*.dll",
                    "bin\\*.xml",
                    "bin\\Sample.WebApp.dll",
                    "bin\\Sample.WebApp.pdb",
                    "Content\\*.css",
                    "Content\\*.png",
                    "Content\\LinkedFile.txt",
                    "Scripts\\*.js",
                    "Views\\Web.config",
                    "Views\\*.cshtml",
                    "Global.asax",
                    "Web.config",
                    "Web.Release.config",
                    "Web.Debug.config"));

            AssertPackage(@"Sample.WebAppWithSpec\obj\octopacked\Sample.WebAppWithSpec.Foo.1.0.9.nupkg",
                pkg => pkg.AssertContents(
                    "bin\\*.dll",
                    "bin\\*.xml",
                    "bin\\Sample.WebAppWithSpec.dll",
                    "bin\\Sample.WebAppWithSpec.pdb",
                    "Content\\*.css",
                    "Content\\*.png",
                    "Content\\LinkedFile.txt",
                    "Scripts\\*.js",
                    "Views\\Web.config",
                    "Views\\*.cshtml",
                    "Views\\Deploy.ps1",
                    "Deploy.ps1",
                    "Global.asax",
                    "Web.config",
                    "Web.Release.config",
                    "Web.Debug.config"));
        }

        [Test]
        public void ShouldAllowAppendingValueToVersion()
        {
            MsBuild("Samples.sln /p:RunOctoPack=true /p:OctoPackAppendToVersion=Foo /p:Configuration=Release /v:m");

            AssertPackage(@"Sample.ConsoleApp\obj\octopacked\Sample.ConsoleApp.2.1.0.1-Foo.nupkg",
                pkg => pkg.AssertContents(
                    "Sample.ConsoleApp.exe",
                    "Sample.ConsoleApp.exe.config",
                    "Sample.ConsoleApp.pdb"));

            AssertPackage(@"Sample.WebApp\obj\octopacked\Sample.WebApp.3.1.0-dev-Foo.nupkg",
                pkg => pkg.AssertContents(
                    "bin\\*.dll",
                    "bin\\*.xml",
                    "bin\\Sample.WebApp.dll",
                    "bin\\Sample.WebApp.pdb",
                    "Content\\*.css",
                    "Content\\*.png",
                    "Content\\LinkedFile.txt",
                    "Scripts\\*.js",
                    "Views\\Web.config",
                    "Views\\*.cshtml",
                    "Global.asax",
                    "Web.config",
                    "Web.Release.config",
                    "Web.Debug.config"));

            AssertPackage(@"Sample.WebAppWithSpec\obj\octopacked\Sample.WebAppWithSpec.1.0.13-demo-Foo.nupkg",
                pkg => pkg.AssertContents(
                    "bin\\*.dll",
                    "bin\\*.xml",
                    "bin\\Sample.WebAppWithSpec.dll",
                    "bin\\Sample.WebAppWithSpec.pdb",
                    "Content\\*.css",
                    "Content\\*.png",
                    "Content\\LinkedFile.txt",
                    "Scripts\\*.js",
                    "Views\\Web.config",
                    "Views\\*.cshtml",
                    "Views\\Deploy.ps1",
                    "Deploy.ps1",
                    "Global.asax",
                    "Web.config",
                    "Web.Release.config",
                    "Web.Debug.config"));
        }

        [Test]
        public void ShouldAllowAppendingValueToVersionWithExplicitPackageVersion()
        {
            MsBuild("Samples.sln /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.9 /p:OctoPackAppendToVersion=Foo /p:Configuration=Release /v:m");

            AssertPackage(@"Sample.ConsoleApp\obj\octopacked\Sample.ConsoleApp.1.0.9-Foo.nupkg",
                pkg => pkg.AssertContents(
                    "Sample.ConsoleApp.exe",
                    "Sample.ConsoleApp.exe.config",
                    "Sample.ConsoleApp.pdb"));

            AssertPackage(@"Sample.WebApp\obj\octopacked\Sample.WebApp.1.0.9-Foo.nupkg",
                pkg => pkg.AssertContents(
                    "bin\\*.dll",
                    "bin\\*.xml",
                    "bin\\Sample.WebApp.dll",
                    "bin\\Sample.WebApp.pdb",
                    "Content\\*.css",
                    "Content\\*.png",
                    "Content\\LinkedFile.txt",
                    "Scripts\\*.js",
                    "Views\\Web.config",
                    "Views\\*.cshtml",
                    "Global.asax",
                    "Web.config",
                    "Web.Release.config",
                    "Web.Debug.config"));

            AssertPackage(@"Sample.WebAppWithSpec\obj\octopacked\Sample.WebAppWithSpec.1.0.9-Foo.nupkg",
                pkg => pkg.AssertContents(
                    "bin\\*.dll",
                    "bin\\*.xml",
                    "bin\\Sample.WebAppWithSpec.dll",
                    "bin\\Sample.WebAppWithSpec.pdb",
                    "Content\\*.css",
                    "Content\\*.png",
                    "Content\\LinkedFile.txt",
                    "Scripts\\*.js",
                    "Views\\Web.config",
                    "Views\\*.cshtml",
                    "Views\\Deploy.ps1",
                    "Deploy.ps1",
                    "Global.asax",
                    "Web.config",
                    "Web.Release.config",
                    "Web.Debug.config"));
        }

        [Test]
        public void ShouldSupportRelativeOutputDirectories()
        {
            MsBuild("Sample.ConsoleWithRelativeOutDir\\Sample.ConsoleWithRelativeOutDir.csproj /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.11 /p:Configuration=Release");

            AssertPackage(@"Sample.ConsoleWithRelativeOutDir\obj\octopacked\Sample.ConsoleWithRelativeOutDir.1.0.11.nupkg",
                pkg => pkg.AssertContents(
                    "Sample.ConsoleWithRelativeOutDir.exe",
                    "Sample.ConsoleWithRelativeOutDir.pdb",
                    "Deploy.ps1"));
        }

        [Test]
        [TestCase("2.0.0-alpha.1")]
        [TestCase("2.0.0+foo")]
        [TestCase("2.0.0+foo.bar")]
        public void ShouldSupportSemVer2Versions(string version)
        {
            MsBuild(string.Format("Sample.ConsoleApp\\Sample.ConsoleApp.csproj /p:RunOctoPack=true /p:OctoPackPackageVersion={0} /p:Configuration=Release /v:m", version));

            AssertPackage(string.Format(@"Sample.ConsoleApp\obj\octopacked\Sample.ConsoleApp.{0}.nupkg", version),
                pkg => Assert.That(pkg.GetIdentity().Version, Is.EqualTo(NuGetVersion.Parse(version))));
        }
    }
}