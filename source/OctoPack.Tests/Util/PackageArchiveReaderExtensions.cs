using System;
using System.Linq;
using System.Text.RegularExpressions;
using NuGet.Packaging;
using NUnit.Framework;

namespace OctoPack.Tests.Integration
{
    public static class PackageArchiveReaderExtensions
    {

        static readonly string[] ExcludePaths = new[]
        {
            "_rels/",
            "package/",
            @"_rels\",
            @"package\",
            "[Content_Types].xml"
        };

        private static readonly string[] ExcludeExtensions = new[]
        {

           ".nupkg.sha512",
           NuGet.Constants.ManifestExtension 
        };

        private static bool IsExcludedPath(string path)
        {
            return ExcludePaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)) ||
                   ExcludeExtensions.Any(p => path.EndsWith(p, StringComparison.OrdinalIgnoreCase));
        }

        public static void AssertContents(this PackageArchiveReader package, params string[] files)
        {
            var actualFiles = package.GetFiles()
                .Where(f => !IsExcludedPath(f))
                .Select(f => f.Replace("/", "\\")) // Paths are stored in ZipArchive using '/' as directory separator
                .ToDictionary(f => f, f => false, StringComparer.OrdinalIgnoreCase);
            var expectedFiles = files.ToDictionary(f => f, f => false, StringComparer.OrdinalIgnoreCase);

            foreach (var actualFile in actualFiles.Keys.ToList())
            {
                if (expectedFiles.ContainsKey(actualFile))
                {
                    actualFiles[actualFile] = true;
                    expectedFiles[actualFile] = true;
                }

                foreach (var expected in expectedFiles.Keys.ToList().Where(k => k.Contains("*")))
                {
                    var regex = Regex.Escape(expected).Replace("\\*", ".*");

                    if (Regex.IsMatch(actualFile, regex, RegexOptions.IgnoreCase))
                    {
                        actualFiles[actualFile] = true;
                        expectedFiles[expected] = true;
                    }
                }
            }

            var unexpectedInPackage = actualFiles.Where(kvp => kvp.Value == false).Select(kvp => kvp.Key).ToList();
            var missingInPackage = expectedFiles.Where(kvp => kvp.Value == false).Select(kvp => kvp.Key).ToList();

            if (unexpectedInPackage.Any())
            {
                Assert.Fail("These files were NOT expected to be in the package: " + Environment.NewLine +
                            string.Join("," + Environment.NewLine, unexpectedInPackage.Select(u => "@\"" + u + "\"")));
            }

            if (missingInPackage.Any())
            {
                Assert.Fail("These files were expected to be in the package: " + Environment.NewLine +
                            string.Join("," + Environment.NewLine, missingInPackage.Select(u => "@\"" + u + "\"")));
            }
        }

        public static void AssertTitle(this PackageArchiveReader package, string expectedTitle)
        {
            var nuspecReader = new NuspecReader(package.GetNuspec());
            var actualTitle = nuspecReader.GetMetadata().Single(x => x.Key == "title").Value;
            Assert.That(actualTitle , Is.EqualTo(expectedTitle));
        }

        public static void AssertVersion(this PackageArchiveReader package, string expectedVersion)
        {
            var nuspecReader = new NuspecReader(package.GetNuspec());
            var actualVersion = nuspecReader.GetMetadata().Single(x => x.Key == "version").Value;
            Assert.That(actualVersion, Is.EqualTo(expectedVersion));
        }

        public static void AssertReleaseNotes(this PackageArchiveReader package, string expectedReleaseNotes)
        {
            var nuspecReader = new NuspecReader(package.GetNuspec());
            var actualReleaseNotes = nuspecReader.GetMetadata().Single(x => x.Key == "releaseNotes").Value;
            Assert.That(actualReleaseNotes, Is.EqualTo(expectedReleaseNotes));
        }

    }

}