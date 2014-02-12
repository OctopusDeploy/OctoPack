using System.IO;
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
                    "Sample.ConsoleApp.pdb"));
        }

        [Test]
        public void ShouldBuildWithSpec()
        {
            MsBuild("Sample.WebAppWithSpec\\Sample.WebAppWithSpec.csproj /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.10 /p:Configuration=Release /v:m");

            AssertPackage(@"Sample.WebAppWithSpec\obj\octopacked\Sample.WebAppWithSpec.1.0.10.nupkg",
                pkg => Assert.That(pkg.Title, Is.EqualTo("Sample application")));
        }

        [Test]
        public void ShouldSupportWierdTeamCityStuff()
        {
            File.Copy("Sample.ConsoleApp\\Sample.ConsoleApp.csproj", "Sample.ConsoleApp\\Sample.ConsoleApp.csproj.teamcity", true);

            MsBuild("Sample.ConsoleApp\\Sample.ConsoleApp.csproj.teamcity /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.10 /p:Configuration=Release /v:m");

            AssertPackage(@"Sample.ConsoleApp\obj\octopacked\Sample.ConsoleApp.1.0.10.nupkg",
                pkg => pkg.AssertContents(
                    "Sample.ConsoleApp.exe",
                    "Sample.ConsoleApp.pdb"));
        }

        [Test]
        public void ShouldCopyToFileShare()
        {
            File.WriteAllText("Notes.txt", "Hello world!");

            MsBuild("Samples.sln /p:RunOctoPack=true /p:OctoPackPackageVersion=1.1.5 /p:OctoPackPublishPackageToFileShare=..\\Packages /p:Configuration=Release /v:m");

            AssertPackage(@"Packages\Sample.ConsoleApp.1.1.5.nupkg", pkg => Assert.That(pkg.Version.ToString(), Is.EqualTo("1.1.5")));
            AssertPackage(@"Packages\Sample.WebApp.1.1.5.nupkg", pkg => Assert.That(pkg.Version.ToString(), Is.EqualTo("1.1.5")));
            AssertPackage(@"Packages\Sample.WebAppWithSpec.1.1.5.nupkg", pkg => Assert.That(pkg.Version.ToString(), Is.EqualTo("1.1.5")));
        }

        [Test]
        public void ShouldAddReleaseNotes()
        {
            File.WriteAllText("Notes.txt", "Hello world!");

            MsBuild("Samples.sln /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.9 /p:OctoPackReleaseNotesFile=..\\Notes.txt /p:Configuration=Release /v:m");

            AssertPackage(@"Sample.ConsoleApp\obj\octopacked\Sample.ConsoleApp.1.0.9.nupkg",
                pkg => Assert.That(pkg.ReleaseNotes, Is.EqualTo("Hello world!")));
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
        public void ShouldAllowCustomFilesSection()
        {
            MsBuild("Sample.WebAppWithSpecAndCustomContent\\Sample.WebAppWithSpecAndCustomContent.csproj /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.11 /p:Configuration=Release");

            AssertPackage(@"Sample.WebAppWithSpecAndCustomContent\obj\octopacked\Sample.WebAppWithSpecAndCustomContent.1.0.11.nupkg",
                pkg => pkg.AssertContents(
                    "bin\\Sample.WebAppWithSpecAndCustomContent.dll",
                    "SomeFiles\\Foo.css"));
        }
    }
}