using System.Reflection;
using NUnit.Framework;
using GitVerAsm = OctoPack.Tests.SampleGitVersionAssembly;
using NoGitVerAsm = OctoPack.Tests.NonGitVersionAssembly;

namespace OctoPack.Tests.Tasks
{
    [TestFixture]
    public class AssemblyExtensionsTests
    {
        [Test]
        public void AssertAssemblyVersion_WhereNoGitVersionProperty_ReturnsNull()
        {
            var assemblyPath = Assembly.GetAssembly(typeof(NoGitVerAsm.ClassReferencingDependency)).FullLocalPath();
            var gitversion = AssemblyExtensions.GetNuGetVersionFromGitVersionInformation(assemblyPath);
            Assert.That(gitversion, Is.Null);
        }

        [Test]
        public void AssertAssemblyVersion_WhereDependentAssemblyOnlyInSource_GetsGitVersion()
        {
            var assemblyPath = Assembly.GetAssembly(typeof(GitVerAsm.ClassReferencingDependency)).FullLocalPath();
            var gitversion = AssemblyExtensions.GetNuGetVersionFromGitVersionInformation(assemblyPath);
            Assert.That(gitversion, Is.EqualTo("1.1.1-tests"));
        }

        [Test]
        public void AssertAssembly_WhereDependentAssemblyNotEvenInSource_GetsGitVersion()
        {
            var assemblyPath = Assembly.GetAssembly(typeof(GitVerAsm.ClassReferencingDependency)).FullLocalPath();
            var gitversion = AssemblyExtensions.GetNuGetVersionFromGitVersionInformation(assemblyPath);
            Assert.That(gitversion, Is.EqualTo("1.1.1-tests"));
        }
    }
}