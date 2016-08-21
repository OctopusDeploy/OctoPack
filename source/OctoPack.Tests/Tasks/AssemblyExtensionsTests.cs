using System.Reflection;
using NUnit.Framework;
using OctoPack.Tests.NonGitVersionAssembly;
using OctoPack.Tests.SampleGitVersionAssembly;

namespace OctoPack.Tests.Tasks
{
    [TestFixture]
    public class AssemblyExtensionsTests
    {
        [Test]
        public void AssertAssemblyVersionGetsGitVersion()
        {
            string gitversion = Assembly.GetAssembly(typeof(ClassInAssemblyWhereGitVersionIsUsed)).GetNugetVersionFromGitVersionInformation();
            Assert.That(gitversion, Is.EqualTo("1.1.1-tests"));
        }

        [Test]
        public void AssertAssemblyVersion_WhereNoGitVersionProperty_ReturnsNull()
        {
            string gitversion = Assembly.GetAssembly(typeof(ClassInAssemblyWhereGitVersionIsNotUsed)).GetNugetVersionFromGitVersionInformation();
            Assert.That(gitversion, Is.Null);
        }
    }
}