using System.IO;
using NUnit.Framework;

namespace OctoPack.Tests.Integration
{
	[TestFixture]
	public class SampleSolutionBuildFixture : BuildFixture
	{
		private const string PARAM_TOOLSVERSION = "/toolsversion:12.0";
		[Test]
		public void ShouldBuildAtSolutionLevel()
		{
			MsBuild(string.Format("Samples.sln /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.9 /p:Configuration=Release {0} /v:m", PARAM_TOOLSVERSION));

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
			MsBuild(string.Format("Sample.ConsoleApp\\Sample.ConsoleApp.csproj /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.10 /p:Configuration=Release {0} /v:m", PARAM_TOOLSVERSION));

			AssertPackage(@"Sample.ConsoleApp\obj\octopacked\Sample.ConsoleApp.1.0.10.nupkg",
				pkg => pkg.AssertContents(
					"Sample.ConsoleApp.exe",
					"Sample.ConsoleApp.exe.config",
					"Sample.ConsoleApp.pdb"));
		}

		[Test]
		public void ShouldPreferAssemblyVersionOverAssemblyFileVersion()
		{
			MsBuild(string.Format("Sample.ConsoleApp\\Sample.ConsoleApp.csproj /p:RunOctoPack=true /p:Configuration=Release {0} /v:m", PARAM_TOOLSVERSION));

			AssertPackage(@"Sample.ConsoleApp\obj\octopacked\Sample.ConsoleApp.2.1.0.0.nupkg",
				pkg => pkg.AssertContents(
					"Sample.ConsoleApp.exe",
					"Sample.ConsoleApp.exe.config",
					"Sample.ConsoleApp.pdb"));
		}

		[Test]
		public void ShouldPreferAssemblyInfoVersionOverAssemblyVersion()
		{
			MsBuild(string.Format("Sample.WebApp\\Sample.WebApp.csproj /p:RunOctoPack=true /p:Configuration=Release {0} /v:m", PARAM_TOOLSVERSION));

			AssertPackage(@"Sample.WebApp\obj\octopacked\Sample.WebApp.3.1.0-dev.nupkg",
				pkg => pkg.AssertContents(
					"*"));
		}

		[Test]
		public void ShouldBuildWithSpecAndAssemblyInformationalVersion()
		{
			MsBuild(string.Format("Sample.WebAppWithSpec\\Sample.WebAppWithSpec.csproj /p:RunOctoPack=true /p:Configuration=Release {0} /v:m", PARAM_TOOLSVERSION));

			AssertPackage(@"Sample.WebAppWithSpec\obj\octopacked\Sample.WebAppWithSpec.1.0.13-demo.nupkg",
				pkg => Assert.That(pkg.Title, Is.EqualTo("Sample application")));
		}

		[Test]
		public void ShouldWarnAboutNonRootScripts()
		{
			MsBuild(string.Format("Sample.WebAppWithSpec\\Sample.WebAppWithSpec.csproj /p:RunOctoPack=true /p:Configuration=Release {0} /v:m", PARAM_TOOLSVERSION),
				output => Assert.That(output, Is.StringContaining("OctoPack warning OCTNONROOT: As of Octopus Deploy 2.4, PowerShell scripts that are not at the root of the package will not be executed. The script 'Views\\Deploy.ps1' lives")));
		}

		[Test]
		public void ShouldNotWarnAboutNonRootScriptsWhenSuppressed()
		{
			MsBuild(string.Format("Sample.WebAppWithSpec\\Sample.WebAppWithSpec.csproj /p:RunOctoPack=true /p:OctoPackIgnoreNonRootScripts=true /p:Configuration=Release {0} /v:m", PARAM_TOOLSVERSION),
				output => Assert.That(output, Is.Not.StringContaining("OctoPack warning OCTNONROOT: As of Octopus Deploy 2.4, PowerShell scripts that are not at the root of the package will not be executed. The script 'Views\\Deploy.ps1' lives")));
		}

		[Test]
		public void ShouldSupportWeirdTeamCityStuff()
		{
			File.Copy("Sample.ConsoleApp\\Sample.ConsoleApp.csproj", "Sample.ConsoleApp\\Sample.ConsoleApp.csproj.teamcity", true);

			MsBuild(string.Format("Sample.ConsoleApp\\Sample.ConsoleApp.csproj.teamcity /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.10 /p:Configuration=Release {0} /v:m", PARAM_TOOLSVERSION));

			AssertPackage(@"Sample.ConsoleApp\obj\octopacked\Sample.ConsoleApp.1.0.10.nupkg",
				pkg => pkg.AssertContents(
					"Sample.ConsoleApp.exe",
					"Sample.ConsoleApp.exe.config",
					"Sample.ConsoleApp.pdb"));
		}

		[Test]
		public void ShouldCopyToFileShare()
		{
			File.WriteAllText("Notes.txt", "Hello world!");

			MsBuild(string.Format("Samples.sln /p:RunOctoPack=true /p:OctoPackPackageVersion=1.1.5 /p:OctoPackPublishPackageToFileShare=..\\Packages /p:Configuration=Release {0} /v:m", PARAM_TOOLSVERSION));

			AssertPackage(@"Packages\Sample.ConsoleApp.1.1.5.nupkg", pkg => Assert.That(pkg.Version.ToString(), Is.EqualTo("1.1.5")));
			AssertPackage(@"Packages\Sample.WebApp.1.1.5.nupkg", pkg => Assert.That(pkg.Version.ToString(), Is.EqualTo("1.1.5")));
			AssertPackage(@"Packages\Sample.WebAppWithSpec.1.1.5.nupkg", pkg => Assert.That(pkg.Version.ToString(), Is.EqualTo("1.1.5")));
		}

		[Test]
		public void ShouldAddReleaseNotes()
		{
			File.WriteAllText("Notes.txt", "Hello world!");

			MsBuild(string.Format("Samples.sln /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.9 /p:OctoPackReleaseNotesFile=..\\Notes.txt /p:Configuration=Release {0} /v:m", PARAM_TOOLSVERSION));

			AssertPackage(@"Sample.ConsoleApp\obj\octopacked\Sample.ConsoleApp.1.0.9.nupkg",
				pkg => Assert.That(pkg.ReleaseNotes, Is.EqualTo("Hello world!")));
		}

		[Test]
		public void ShouldIncludeTypeScriptSourcesWhenSpecified()
		{
			MsBuild(string.Format("Sample.TypeScriptApp\\Sample.TypeScriptApp.csproj /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.9 /p:Configuration=Release /p:OctoPackIncludeTypeScriptSourceFiles=True {0} /v:d", PARAM_TOOLSVERSION));

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
			MsBuild(string.Format("Sample.TypeScriptApp\\Sample.TypeScriptApp.csproj /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.9 /p:Configuration=Release {0} /v:d", PARAM_TOOLSVERSION));

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
			MsBuild(string.Format("Sample.WebApp\\Sample.WebApp.csproj /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.9 {0} /p:Configuration=Release", PARAM_TOOLSVERSION));

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
			MsBuild(string.Format("Sample.WebAppWithSpecAndCustomContent\\Sample.WebAppWithSpecAndCustomContent.csproj /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.11 /p:Configuration=Release {0}", PARAM_TOOLSVERSION));

			AssertPackage(@"Sample.WebAppWithSpecAndCustomContent\obj\octopacked\Sample.WebAppWithSpecAndCustomContent.1.0.11.nupkg",
				pkg => pkg.AssertContents(
					"bin\\Sample.WebAppWithSpecAndCustomContent.dll",
					"SomeFiles\\Foo.css"));
		}

		[Test]
		public void ShouldBundleLinkedAppConfigFiles()
		{
			MsBuild(string.Format("Sample.ConsoleApp\\Sample.ConsoleApp.csproj /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.9 /p:Configuration=Release {0} /v:m", PARAM_TOOLSVERSION));

			AssertPackage(@"Sample.ConsoleApp\obj\octopacked\Sample.ConsoleApp.1.0.9.nupkg",
				pkg => pkg.AssertContents(
					"Sample.ConsoleApp.exe",
					"Sample.ConsoleApp.exe.config",
					"Sample.ConsoleApp.pdb"));
		}

		[Test]
		public void ShouldAllowCustomFilesSectionWhenEnforced()
		{
			MsBuild(string.Format("Sample.WebAppWithSpecAndCustomContentEnforced\\Sample.WebAppWithSpecAndCustomContentEnforced.csproj /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.11 /p:Configuration=Release /p:OctoPackEnforceAddingFiles=true {0}", PARAM_TOOLSVERSION));

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
			MsBuild(string.Format("Samples.sln /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.9 /p:OctoPackAppendToPackageId=Foo /p:Configuration=Release {0} /v:m", PARAM_TOOLSVERSION));

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
		public void ShouldSupportRelativeOutputDirectories()
		{
			MsBuild(string.Format("Sample.ConsoleWithRelativeOutDir\\Sample.ConsoleWithRelativeOutDir.csproj /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.11 /p:Configuration=Release {0}", PARAM_TOOLSVERSION));

			AssertPackage(@"Sample.ConsoleWithRelativeOutDir\obj\octopacked\Sample.ConsoleWithRelativeOutDir.1.0.11.nupkg",
				pkg => pkg.AssertContents(
					"Sample.ConsoleWithRelativeOutDir.exe",
					"Sample.ConsoleWithRelativeOutDir.pdb",
					"Deploy.ps1"));
		}
	}
}