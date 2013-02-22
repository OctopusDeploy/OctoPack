using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using NUnit.Framework;
using NuGet;
using OctoPack.Tasks;
using OctoPack.Tasks.Util;

namespace OctoPack.Tests.Integration
{
    public abstract class BuildFixture
    {
        private string originalDirectory;

        [SetUp]
        public void SetUp()
        {
            originalDirectory = Environment.CurrentDirectory;

            var root = new Uri(typeof(Samples).Assembly.CodeBase).LocalPath;
            while (!root.EndsWith("source") && !root.EndsWith("source\\"))
            {
                root = Path.GetFullPath(Path.Combine(root, "..\\"));
            }

            Environment.CurrentDirectory = Path.Combine(root, "Samples");

            Clean("Sample.ConsoleApp\\bin");
            Clean("Sample.WebApp\\bin");
            Clean("Sample.WebAppWithSpec\\bin");
        }

        protected static void MsBuild(string commandLineArguments)
        {
            MsBuild(commandLineArguments, null);
        }

        protected static void MsBuild(string commandLineArguments, Action<string> outputValidator)
        {
            var netFx = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            var msBuild = Path.Combine(netFx, "msbuild.exe");
            if (!File.Exists(msBuild))
            {
                Assert.Fail("Could not find MSBuild at: " + msBuild);
            }

            var allOutput = new StringBuilder();

            Action<string> writer = (output) =>
            {
                allOutput.AppendLine(output);
                Trace.WriteLine(output);
            };

            var result = SilentProcessRunner.ExecuteCommand(msBuild, commandLineArguments, Environment.CurrentDirectory, writer, e => writer("ERROR: " + e));

            if (result != 0)
            {
                Assert.Fail("MSBuild returned a non-zero exit code: " + result);
            }

            if (outputValidator != null)
            {
                outputValidator(allOutput.ToString());
            }
        }

        protected static void AssertPackage(string packageFilePath, Action<ZipPackage> packageAssertions)
        {
            var fullPath = Path.Combine(Environment.CurrentDirectory, packageFilePath);
            if (!File.Exists(fullPath))
            {
                Assert.Fail("Could not find package file: " + fullPath);
            }

            Trace.WriteLine("Checking package: " + fullPath);
            var package = new ZipPackage(fullPath);
            packageAssertions(package);

            Trace.WriteLine("Success!");
        }

        protected static void Clean(string path)
        {
            new OctopusPhysicalFileSystem().PurgeDirectory(path, DeletionOptions.TryThreeTimesIgnoreFailure);
        }

        [TearDown]
        public void TearDown()
        {
            Environment.CurrentDirectory = originalDirectory;
        }
    }
}