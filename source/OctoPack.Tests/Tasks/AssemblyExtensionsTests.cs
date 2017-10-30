using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace OctoPack.Tests.Tasks
{
    [TestFixture]
    public class AssemblyExtensionsTests
    {
        [Test]
        public void AssertAssemblyVersion_WhereNoGitVersionProperty_ReturnsNull()
        {
            var assemblyPath = GetAssemblyFullPath("OctoPack.Tests.NonGitVersionAssembly");
            var gitversion = AssemblyExtensions.GetNuGetVersionFromGitVersionInformation(assemblyPath);
            Assert.That(gitversion, Is.Null);
        }

        [Test]
        public void AssertAssemblyVersion_WhereDependentAssemblyOnlyInSource_GetsGitVersion()
        {
            var assemblyPath = GetAssemblyFullPath("OctoPack.Tests.SampleGitVersionAssembly");
            var gitversion = AssemblyExtensions.GetNuGetVersionFromGitVersionInformation(assemblyPath);
            Assert.That(gitversion, Is.EqualTo("1.1.1-tests"));
        }

        [Test]
        public void AssertAssembly_WhereDependentAssemblyNotEventInSource_GetsGitVersion()
        {
            var assemblyPath = GetAssemblyFullExceptionPath("OctoPack.Tests.SampleGitVersionAssembly");
            var gitversion = AssemblyExtensions.GetNuGetVersionFromGitVersionInformation(assemblyPath);
            Assert.That(gitversion, Is.EqualTo("1.1.1-tests"));
        }

        private string GetAsseblyLocation(string name)
        {
            var currentAssemblyPath = Assembly.GetExecutingAssembly().FullLocalPath();
            var configuration = new FileInfo(currentAssemblyPath).Directory.Name;
            return Path.Combine(currentAssemblyPath, "..", "..", "..", "..", name, "bin", configuration);
        }

        private string GetAssemblyFullPath(string name)
        {
            var assemblyPath = Path.Combine(GetAsseblyLocation(name), $"{name}.dll");
            return new FileInfo(assemblyPath).FullName;
        }

        private string GetAssemblyFullExceptionPath(string name)
        {
            var assemblyPath = Path.Combine(GetAsseblyLocation(name), "StandAloneDll", $"{ name}.dll");
            return new FileInfo(assemblyPath).FullName;
        }
    }
}