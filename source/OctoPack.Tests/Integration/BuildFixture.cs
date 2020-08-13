using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using NuGet.Packaging;
using NUnit.Framework;
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

            var root = new Uri(typeof(BuildFixture).Assembly.CodeBase).LocalPath;
            while (!root.EndsWith("source") && !root.EndsWith("source\\"))
            {
                root = Path.GetFullPath(Path.Combine(root, "..\\"));
            }

            Environment.CurrentDirectory = Path.Combine(root, "Samples");

            Clean("Sample.ConsoleApp\\bin");
            Clean("Sample.WebApp\\bin");
            Clean("Sample.WebAppWithSpec\\bin");
        }

        protected static void MsBuild(string commandLineArguments, Action<string> outputValidator = null, Dictionary<string,string> environmentVariables = null)
        {
            var msBuild = GetMsBuildPath();
            var allOutput = new StringBuilder();

            Action<string> writer = (output) =>
            {
                allOutput.AppendLine(output);
                Console.WriteLine(output);
            };

            var result = SilentProcessRunner.ExecuteCommand(msBuild, commandLineArguments, Environment.CurrentDirectory, writer, e => writer("ERROR: " + e), environmentVariables);

            if (result != 0)
            {
                Assert.Fail("MSBuild returned a non-zero exit code: " + result);
            }

            outputValidator?.Invoke(allOutput.ToString());
        }

        private static string GetMsBuildPath()
        {
            string msBuild;

            var programFilesDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            foreach (var version in new[] { "Current", "15.0", "14.0", "12.0" })
            {
                // As of Visual Studio 2017, Microsoft changed where the MSBuild tools reside. As of Visual Studio 2019, Microsoft stopped using
                // a version number, such as 15.0, in the path and now uses 'Current'.
                // This additional loop accounts for MSBuild located in the Visual Studio installation path for both VS2017 and VS2019 Professional
                // and enterprise editions. This is still fragile, as there are other editions of Visual Studio, and also does not account for
                // the Visual Studio Build Tools (for 2017 and 2019).
                // The last empty string array entry is for Visual Studio 2015 and below.
                foreach (var msBuildBasePath in new[] { "Microsoft Visual Studio\\2019\\Professional", "Microsoft Visual Studio\\2019\\Enterprise", "Microsoft Visual Studio\\2017\\Professional", "Microsoft Visual Studio\\2017\\Enterprise", "" })
                {
                    msBuild = Path.Combine(programFilesDirectory, msBuildBasePath, "MSBuild", version, "Bin", "msbuild.exe");
                    if (File.Exists(msBuild))
                        return msBuild;
                }
            }
            var netFx = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            msBuild = Path.Combine(netFx, "msbuild.exe");
            if (!File.Exists(msBuild))
            {
                Assert.Fail("Could not find MSBuild at: " + msBuild);
            }
            return msBuild;
        }

        protected static void AssertPackage(string packageFilePath, Action<PackageArchiveReader> packageAssertions)
        {
            var fullPath = Path.Combine(Environment.CurrentDirectory, packageFilePath);
            if (!File.Exists(fullPath))
            {
                Assert.Fail("Could not find package file: " + fullPath);
            }

            Trace.WriteLine("Checking package: " + fullPath);

            using (var package = new PackageArchiveReader(File.OpenRead(fullPath)))
            {
                packageAssertions(package);
            }

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