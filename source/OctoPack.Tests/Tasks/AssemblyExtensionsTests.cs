using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace OctoPack.Tests.Tasks
{
    [TestFixture]
    public class AssemblyExtensionsTests
    {
        private string GetAssemblyFullPath(string name)
        {
            var currentAssemblyPath = Assembly.GetExecutingAssembly().FullLocalPath();
            var configuration = new FileInfo(currentAssemblyPath).Directory.Name;
            var assemblyPath = Path.Combine(currentAssemblyPath, "..", "..", "..", "..", name, "bin", configuration, $"{name}.dll");
            return new FileInfo(assemblyPath).FullName;
        }

        private string GetAssemblyFullExceptionPath(string name)
        {
            var currentAssemblyPath = Assembly.GetExecutingAssembly().FullLocalPath();
            var configuration = new FileInfo(currentAssemblyPath).Directory.Name;
            var assemblyPath = Path.Combine(currentAssemblyPath, "..", "..", "..", "..", name, "bin", configuration, "FileNotFoundException", $"{ name}.dll");
            return new FileInfo(assemblyPath).FullName;
        }

        [Test]
        public void AssertAssemblyVersion_WhereNoGitVersionProperty_ReturnsNull()
        {
            var assemblyPath = GetAssemblyFullPath("OctoPack.Tests.NonGitVersionAssembly");
            var gitversion = AssemblyExtensions.GetNuGetVersionFromGitVersionInformation(assemblyPath);
            Assert.That(gitversion, Is.Null);
        }

        [Test]
        public void AssertAssemblyVersionGetsGitVersion()
        {
            var assemblyPath = GetAssemblyFullPath("OctoPack.Tests.SampleGitVersionAssembly");
            var gitversion = AssemblyExtensions.GetNuGetVersionFromGitVersionInformation(assemblyPath);
            Assert.That(gitversion, Is.EqualTo("1.1.1-tests"));
        }

        [Test]
        public void AssertAssemblyFileNotFoundExceptionGetsGitVersion()
        {
            var assemblyPath = GetAssemblyFullExceptionPath("OctoPack.Tests.SampleGitVersionAssembly");
            var gitversion = AssemblyExtensions.GetNuGetVersionFromGitVersionInformation(assemblyPath);
            Assert.That(gitversion, Is.EqualTo("1.1.1-tests"));
        }
    }
}