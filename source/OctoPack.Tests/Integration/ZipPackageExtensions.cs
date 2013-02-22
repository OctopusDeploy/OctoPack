using System;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using NuGet;

namespace OctoPack.Tests.Integration
{
    public static class ZipPackageExtensions
    {
        public static void AssertContents(this ZipPackage package, params string[] files)
        {
            var actualFiles = package.GetFiles().Select(f => f.Path).ToDictionary(f => f, f => false, StringComparer.OrdinalIgnoreCase);
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
                Assert.Fail("These files were NOT expected to be in the package: " + Environment.NewLine + string.Join("," + Environment.NewLine, unexpectedInPackage.Select(u => "@\"" + u + "\"")));
            }

            if (missingInPackage.Any())
            {
                Assert.Fail("These files were expected to be in the package: " + Environment.NewLine + string.Join("," + Environment.NewLine, missingInPackage.Select(u => "@\"" + u + "\"")));
            }
        }
    }
}