using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using NUnit.Framework;

namespace OctoPack.Tests.Installation
{
    [TestFixture]
    public class OctoPackInstallationTests
    {


        public static string Root { get; private set; }
        public static string SampleProjectDirectory { get; private set; }
        public static string TestProjectFile { get; private set; }

        [SetUp]
        public static void SetUp()
        {
            Root = new Uri(typeof(OctoPackInstallationTests).Assembly.CodeBase).LocalPath;
            while (!(Root.EndsWith("source") || Root.EndsWith("source\\")))
            {
                Root = Path.GetFullPath(Path.Combine(Root, "..\\"));
            }

            SampleProjectDirectory = Path.Combine(Root, "OctoPack.Tests.SampleGitVersionAssembly");

            TestProjectFile = Path.Combine(SampleProjectDirectory, "OctoPack.Tests.SampleGitVersionAssembly.csproj").CreateTestProjectFile();
        }

        [TearDown]
        public static void TearDown()
        {
            try
            {
                File.Delete(TestProjectFile);
            }
            catch
            {
                // Swallow any exception--just making an attempt to clean up after ourselves.
            }
        }

        [Test]
        public void ProperlyInstallsOctoPackIntoProjects()
        {
            var targetsFile = Path.Combine(SampleProjectDirectory, "build", "OctoPack.targets");

            bool result = TestProjectFile.InstallOctoPack();

            Assert.That(result, Is.True, "Failed to install OctoPack into the project file '{0}'.", TestProjectFile);

            var projectCollection = new ProjectCollection();
            var project = projectCollection.LoadProject(TestProjectFile);
            var imports = project.Xml.Imports.Where(i => i.Project.Contains("OctoPack"));
            var octoPackTargetsImport = imports.Last();
            Assert.That(octoPackTargetsImport.Project, Is.StringEnding(project.GetRelativePathToFile(targetsFile)));
        }

        [Test]
        public void ProperlyUninstallsOctoPackFromProjects()
        {
            TestProjectFile.InstallOctoPack();

            var result = TestProjectFile.UninstallOctoPack();

            var projectCollection = new ProjectCollection();
            var project = projectCollection.LoadProject(TestProjectFile);

            Assert.That(project.Xml.Imports.Count, Is.EqualTo(2), project.Xml.RawXml);
            Assert.That(project.Xml.Imports.First().Project, Is.StringEnding("Microsoft.Common.props"));
            Assert.That(project.Xml.Imports.Last().Project, Is.StringEnding("Microsoft.CSharp.targets"));
        }
    }

    static class TestHelpers
    {
        private const string TestScript = @"& {
                Param(
                    [Parameter(Mandatory, Position = 0)]
                    [string]$ProjectPath,
                    [Parameter(Mandatory, Position = 1)]
                    [string]$ToolsPath,
                    [Parameter(Mandatory, Position = 2)]
                    [ValidateSet('Install','Uninstall')]
                    [string]$Operation
                )
                Add-Type -AssemblyName 'Microsoft.Build, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a';
                $package = [PSCustomObject]@{ Id = 'OctoPack' };
                $project = [Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection.LoadProject($ProjectPath) |
                    Select-Object -First 1 |
                    Add-Member -MemberType AliasProperty -Name FullName -Value FullPath -PassThru;
                & """"""$ToolsPath\${Operation}.ps1"""""" -toolsPath $ToolsPath -package $package -project $project;
            }
            ";

        private const string Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Unrestricted -Command \"{0}\" \"{1}\" \"{2}\" {3}";

        public static string CreateTestProjectFile(this string projectFilePath)
        {
            var projectFileDirectory = Path.GetDirectoryName(projectFilePath);
            var projectFileFilename = Path.GetFileNameWithoutExtension(projectFilePath);

            var temporaryProjectFilePath = Path.Combine(projectFileDirectory, string.Concat(projectFileFilename, "-Temp.csproj"));
            if (File.Exists(temporaryProjectFilePath))
            {
                File.Delete(temporaryProjectFilePath);
            }

            File.Copy(projectFilePath, temporaryProjectFilePath, true);

            return temporaryProjectFilePath;
        }

        public static string GetRelativePathToFile(this Project project, string absolutePath)
        {
            return Uri.UnescapeDataString(
                new Uri(project.FullPath, UriKind.Absolute)
                    .MakeRelativeUri(new Uri(absolutePath, UriKind.Absolute))
                    .ToString()
            )
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        public static bool InstallOctoPack(this string projectFilePath) => projectFilePath.ModifyProject("Install");

        public static bool UninstallOctoPack(this string projectFilePath) => projectFilePath.ModifyProject("Uninstall");

        private static bool ModifyProject(this string projectFilePath, string operation)
        {
            var toolsDirectoryPath = Path.Combine(OctoPackInstallationTests.Root, "tools");

            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = string.Format(
                    Arguments,
                    TestScript.Replace(Environment.NewLine, " ").Trim(),
                    projectFilePath,
                    toolsDirectoryPath,
                    operation),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = OctoPackInstallationTests.SampleProjectDirectory,
                LoadUserProfile = false,
                CreateNoWindow = true
            };

            var process = Process.Start(startInfo);
            process.WaitForExit();
            Console.Out.Write(process.StandardOutput.ReadToEnd());
            Console.Error.Write(process.StandardError.ReadToEnd());

            var exitCode = process.ExitCode;

            process.Close();

            return exitCode == 0;
        }
    }
}
