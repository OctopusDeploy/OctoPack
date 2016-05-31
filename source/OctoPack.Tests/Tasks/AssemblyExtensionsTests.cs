using System.Reflection;

using NUnit.Framework;

using OctoPack.Tasks;

namespace OctoPack.Tests.Tasks
{
    [TestFixture]
    public class AssemblyExtensionsTests
    {
        [Test]
        public void AssertAssemblyVersionGetsGitVersion()
        {
            string gitversion = Assembly.GetAssembly(typeof(AssemblyExtensionsTests)).GetNugetVersion();
            Assert.That(gitversion, Is.EqualTo("1.1.1-tests"));
        }

        [Test]
        public void AssertAssemblyVersion_WhereNoGitVersionProperty_ReturnsNull()
        {
            string gitversion = Assembly.GetAssembly(typeof(GetAssemblyVersionInfo)).GetNugetVersion();
            Assert.That(gitversion, Is.Null);
        }
    }
}